using UnityEngine;

public class CollisionCheck : MonoBehaviour
{
    public enum SurfaceState
    {
        Floor,
        WALL_L,
        WALL_R,
        Ceiling
    }

    RaycastHit2D groundHit1;
    RaycastHit2D groundHit2;

    RaycastHit2D ceilingHit1;
    RaycastHit2D ceilingHit2;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform raycastOrigin;
    [SerializeField] private float raycastHorizontalDistance = 0.1f;
    [SerializeField] private float groundSensorVerticalOffset;
    [SerializeField, Min(0f)] private float raycastVerticalDistance = 1f;
    [SerializeField] private float rayCastDistance = 0.1f;
    [SerializeField] private float ceilingSensorVerticalOffset = 0.5f;
    [SerializeField, Range(0f, 1f)] private float inwardRayBias = 0.25f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.2f;
    [SerializeField] private float pushRayDistance = 0.2f;
    [SerializeField] private float pushSensorHeight = 0.3f; // vertical offset above raycastOrigin, along world up
    [SerializeField, Range(0f, 15f)] private float pushSensorAngleTolerance = 5f; // slack around 0/90/180/270
    [SerializeField, Range(-1f, 1f)] private float floorAlignmentThreshold = 0.3f;
    [SerializeField, Range(-1f, 1f)] private float ceilingAlignmentThreshold = -0.5f;

    RaycastHit2D pushHitLeft;
    RaycastHit2D pushHitRight;

    public bool IsTouchingWallLeft { get; private set; }
    public bool IsTouchingWallRight { get; private set; }
    public float PushDistanceLeft { get; private set; } = float.PositiveInfinity;
    public float PushDistanceRight { get; private set; } = float.PositiveInfinity;

    public float SurfaceOffset => surfaceOffset;

    public bool isGrounded {get; private set;}
    private bool useCeilingSensors;
    private bool isCeilingDetected;
    public Vector2 SurfaceNormal { get; private set; } = Vector2.up;
    public Vector2 SurfacePoint { get; private set; }
    public float SurfaceDistance { get; private set; }
    public float CeilingDistance { get; private set; } = float.PositiveInfinity;
    public SurfaceState CurrentSurfaceState { get; private set; } = SurfaceState.Floor;

    private void FixedUpdate()
    {
        RefreshSensors();
    }

    public void RefreshSensors()
    {
        if (raycastOrigin == null)
        {
            return;
        }

        UpdateSurfaceData();
    }

    public void EnableGroundSensors()
    {
        useCeilingSensors = false;
    }

    public void EnableCeilingSensors()
    {
        useCeilingSensors = true;
    }

    public bool TryGetCeilingHit(out RaycastHit2D ceilingHit)
    {
        if (!isCeilingDetected)
        {
            ceilingHit = default;
            return false;
        }

        if (ceilingHit1.collider == null)
        {
            ceilingHit = ceilingHit2;
            return true;
        }

        if (ceilingHit2.collider == null || ceilingHit1.distance <= ceilingHit2.distance)
        {
            ceilingHit = ceilingHit1;
            return true;
        }

        ceilingHit = ceilingHit2;
        return true;
    }

    private void UpdateSurfaceData()
    {
        Vector2 right = raycastOrigin.right;
        Vector2 down = -raycastOrigin.up;
        Vector2 up = raycastOrigin.up;
        float ceilingSensorRayDistance = rayCastDistance * raycastVerticalDistance;

        if (useCeilingSensors)
        {
            groundHit1 = default;
            groundHit2 = default;

            Vector2 ceilingCenter = (Vector2)raycastOrigin.position + up * ceilingSensorVerticalOffset;
            Vector2 ceilingSensorC = ceilingCenter + right * raycastHorizontalDistance;
            Vector2 ceilingSensorD = ceilingCenter - right * raycastHorizontalDistance;
            Vector2 ceilingRayDirectionC = (up - right * inwardRayBias).normalized;
            Vector2 ceilingRayDirectionD = (up + right * inwardRayBias).normalized;

            ceilingHit1 = Physics2D.Raycast(ceilingSensorC, ceilingRayDirectionC, ceilingSensorRayDistance, groundLayer);
            ceilingHit2 = Physics2D.Raycast(ceilingSensorD, ceilingRayDirectionD, ceilingSensorRayDistance, groundLayer);

            isGrounded = false;
            bool ceilingDetected = ceilingHit1.collider != null || ceilingHit2.collider != null;
            CeilingDistance = GetNearestCeilingDistance();

            if (ceilingDetected && !isCeilingDetected)
            {
                Collider2D detectedCollider = ceilingHit1.collider != null
                    ? ceilingHit1.collider
                    : ceilingHit2.collider;

                Debug.Log($"Ceiling platform detected: {detectedCollider.name}");
            }

            isCeilingDetected = ceilingDetected;
            UpdatePushSensors();
            return;
        }

        ceilingHit1 = default;
        ceilingHit2 = default;
        isCeilingDetected = false;
        CeilingDistance = float.PositiveInfinity;

        Vector2 groundSensorCenter = (Vector2)raycastOrigin.position + up * groundSensorVerticalOffset;
        Vector2 groundSensorA = groundSensorCenter + right * raycastHorizontalDistance;
        Vector2 groundSensorB = groundSensorCenter - right * raycastHorizontalDistance;
        Vector2 rayDirectionA = (down - right * inwardRayBias).normalized;
        Vector2 rayDirectionB = (down + right * inwardRayBias).normalized;
        float groundSensorRayDistance = GetGroundSensorRayDistance();

        groundHit1 = Physics2D.Raycast(groundSensorA, rayDirectionA, groundSensorRayDistance, groundLayer);
        groundHit2 = Physics2D.Raycast(groundSensorB, rayDirectionB, groundSensorRayDistance, groundLayer);



        int hitCount = 0;
        Vector2 normalSum = Vector2.zero;
        Vector2 pointSum = Vector2.zero;

        if (groundHit1.collider != null)
        {
            hitCount++;
            normalSum += groundHit1.normal;
            pointSum += groundHit1.point;
        }

        if (groundHit2.collider != null)
        {
            hitCount++;
            normalSum += groundHit2.normal;
            pointSum += groundHit2.point;
        }

        isGrounded = hitCount > 0;
        UpdatePushSensors();

        if (!isGrounded)
        {
            return;
        }

        SurfaceNormal = (normalSum / hitCount).normalized;
        SurfacePoint = pointSum / hitCount;
        SurfaceDistance = Vector2.Dot(
            (Vector2)raycastOrigin.position - SurfacePoint,
            SurfaceNormal);

        float worldUpAlignment = Vector2.Dot(SurfaceNormal, Vector2.up);
        CurrentSurfaceState = worldUpAlignment > floorAlignmentThreshold ? SurfaceState.Floor : worldUpAlignment < ceilingAlignmentThreshold ? SurfaceState.Ceiling : SurfaceNormal.x > 0f ? SurfaceState.WALL_L : SurfaceState.WALL_R;

    }

    private float GetNearestCeilingDistance()
    {
        if (ceilingHit1.collider == null)
        {
            return ceilingHit2.collider == null ? float.PositiveInfinity : ceilingHit2.distance;
        }

        return ceilingHit2.collider == null
            ? ceilingHit1.distance
            : Mathf.Min(ceilingHit1.distance, ceilingHit2.distance);
    }

    private float GetGroundSensorRayDistance()
    {
        float configuredRayDistance = rayCastDistance * raycastVerticalDistance;
        float targetNormalDistance = Mathf.Max(0f, surfaceOffset + groundSensorVerticalOffset);
        float diagonalRayDistance = targetNormalDistance * Mathf.Sqrt(1f + inwardRayBias * inwardRayBias);
        return Mathf.Max(configuredRayDistance, diagonalRayDistance);
    }

    private void UpdatePushSensors()
    {
        pushHitLeft = default;
        pushHitRight = default;
        IsTouchingWallLeft = false;
        IsTouchingWallRight = false;
        PushDistanceLeft = float.PositiveInfinity;
        PushDistanceRight = float.PositiveInfinity;

        if (isGrounded && !IsNearMultipleOf90(Vector2.SignedAngle(Vector2.up, SurfaceNormal), pushSensorAngleTolerance))
        {
            return;
        }

        Vector2 localRight = raycastOrigin.right;
        Vector2 localUp = raycastOrigin.up;
        Vector2 origin = (Vector2)raycastOrigin.position + localUp * pushSensorHeight;

        pushHitLeft = Physics2D.Raycast(origin, -localRight, pushRayDistance, groundLayer);
        pushHitRight = Physics2D.Raycast(origin, localRight, pushRayDistance, groundLayer);

        if (pushHitLeft.collider != null)
        {
            IsTouchingWallLeft = true;
            PushDistanceLeft = pushHitLeft.distance;
        }

        if (pushHitRight.collider != null)
        {
            IsTouchingWallRight = true;
            PushDistanceRight = pushHitRight.distance;
        }
    }

    private bool IsNearMultipleOf90(float angleDegrees, float tolerance)
    {
        float normalized = Mathf.Repeat(angleDegrees, 90f);
        return normalized <= tolerance || normalized >= 90f - tolerance;
    }

    private void OnDrawGizmos()
    {
        if (raycastOrigin == null)
        {
            return;
        }

        Vector2 right = raycastOrigin.right;
        Vector2 up = raycastOrigin.up;
        Vector2 down = -raycastOrigin.up;

        Vector2 groundSensorCenter = (Vector2)raycastOrigin.position + up * groundSensorVerticalOffset;
        Vector2 groundSensorA = groundSensorCenter + right * raycastHorizontalDistance;
        Vector2 groundSensorB = groundSensorCenter - right * raycastHorizontalDistance;
        Vector2 rayDirectionA = (down - right * inwardRayBias).normalized;
        Vector2 rayDirectionB = (down + right * inwardRayBias).normalized;
        float groundSensorRayDistance = GetGroundSensorRayDistance();
        float ceilingSensorRayDistance = rayCastDistance * raycastVerticalDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(groundSensorA, groundSensorA + rayDirectionA * groundSensorRayDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(groundSensorB, groundSensorB + rayDirectionB * groundSensorRayDistance);

        Vector2 ceilingCenter = (Vector2)raycastOrigin.position + up * ceilingSensorVerticalOffset;
        Vector2 ceilingSensorC = ceilingCenter + right * raycastHorizontalDistance;
        Vector2 ceilingSensorD = ceilingCenter - right * raycastHorizontalDistance;
        Vector2 ceilingRayDirectionC = (up - right * inwardRayBias).normalized;
        Vector2 ceilingRayDirectionD = (up + right * inwardRayBias).normalized;


        Vector2 pushOrigin = (Vector2)raycastOrigin.position + up * pushSensorHeight;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pushOrigin, pushOrigin - right * pushRayDistance);
        Gizmos.DrawLine(pushOrigin, pushOrigin + right * pushRayDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(ceilingSensorC, ceilingSensorC + ceilingRayDirectionC * ceilingSensorRayDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ceilingSensorD, ceilingSensorD + ceilingRayDirectionD * ceilingSensorRayDistance);

        if (isGrounded)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(SurfacePoint, SurfacePoint + SurfaceNormal * groundSensorRayDistance);
        }

        
    }
}

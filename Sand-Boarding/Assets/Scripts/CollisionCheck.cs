using UnityEngine;


public struct SensorContact
{
    public bool hit;
    public Collider2D collider;
    public Vector2 point;
    public Vector2 normal;
    public Vector2 castDirection;
    public float signedDistance;
}

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
    [SerializeField, Min(0f)] private float sensorSwitchTolerance = 0.05f;
    [SerializeField, Min(0f)] private float surfaceOffset = 0.2f;
    [SerializeField, Min(0f)] private float penetrationRecoveryDistance = 0.25f;
    [SerializeField, Min(0f)] private float maxGroundSnapDistance = 0.25f;
    [SerializeField] private float pushRayDistance = 0.2f;
    [SerializeField] private float pushSensorHeight = 0.3f; // vertical offset above raycastOrigin, along world up
    [SerializeField, Range(0f, 15f)] private float pushSensorAngleTolerance = 5f; // slack around 0/90/180/270
    [SerializeField, Range(-1f, 1f)] private float floorAlignmentThreshold = 0.3f;
    [SerializeField, Min(0f)]
    private float normalBlendDistance = 0.25f;
    [SerializeField, Range(0f, 90f)]
    private float normalBlendMaxAngle = 60f;
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
    public SensorContact PrimaryGroundSensor { get; private set; }
    public SensorContact SecondaryGroundSensor { get; private set; }


    public void RefreshSensors(Vector2 predictedOffset)
    {
        if (raycastOrigin == null)
        {
            return;
        }

        CastGroundSensors(predictedOffset);
    }

    // Push sensors: a pair of horizontal rays (left/right) that catch a
    // genuine wall directly ahead of the character, since the ground/ceiling
    // sensors only ever look down or up and have nothing checking the
    // direction of travel. Without this, a wall the character runs into
    // (rather than walks up) is simply never detected until it's already
    // been driven through.
    public void RefreshPushSensors(Vector2 predictedOffset)
    {
        if (raycastOrigin == null)
        {
            return;
        }

        CastPushSensors(predictedOffset);
    }

    // Separate from RefreshSensors: ground sensors track the floor/wall/ceiling
    // quadrant and always run. Ceiling (head-bump) sensors are a simple pair of
    // upward rays that only matter in the air, so they're gated behind the
    // useCeilingSensors flag instead of always firing.
    public void RefreshCeilingSensors(Vector2 predictedOffset)
    {
        if (raycastOrigin == null)
        {
            return;
        }

        if (!useCeilingSensors)
        {
            isCeilingDetected = false;
            CeilingDistance = float.PositiveInfinity;
            return;
        }

        CastCeilingSensors(predictedOffset);
    }

    private void CastCeilingSensors(Vector2 positionOffset)
    {
        Vector2 origin = (Vector2)raycastOrigin.position + positionOffset + Vector2.up * ceilingSensorVerticalOffset;

        Vector2 anchorA = origin - Vector2.right * raycastHorizontalDistance;
        Vector2 anchorB = origin + Vector2.right * raycastHorizontalDistance;

        ceilingHit1 = Physics2D.Raycast(anchorA, Vector2.up, raycastVerticalDistance, groundLayer);
        ceilingHit2 = Physics2D.Raycast(anchorB, Vector2.up, raycastVerticalDistance, groundLayer);

        isCeilingDetected = ceilingHit1.collider != null || ceilingHit2.collider != null;

        if (!isCeilingDetected)
        {
            CeilingDistance = float.PositiveInfinity;
            return;
        }

        float distanceA = ceilingHit1.collider != null ? ceilingHit1.distance : float.PositiveInfinity;
        float distanceB = ceilingHit2.collider != null ? ceilingHit2.distance : float.PositiveInfinity;
        CeilingDistance = Mathf.Min(distanceA, distanceB);
    }

    public void ResetSurfaceState()
    {
        CurrentSurfaceState = SurfaceState.Floor;
        isGrounded = false;
        PrimaryGroundSensor = default;
        SecondaryGroundSensor = default;
        SurfaceNormal = Vector2.up;
        SurfacePoint = default;
        SurfaceDistance = 0f;
    }


    public void EnableGroundSensors()
    {
        useCeilingSensors = false;
        isCeilingDetected = false;
        CeilingDistance = float.PositiveInfinity;
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

    private void UpdateSurfaceState(Vector2 normal)
    {
        float angle = Vector2.SignedAngle(Vector2.up, normal);
        if(angle < 0f)
        {
            angle += 360f;
        }
        if(angle >= 315f && angle < 360f)
        {
            CurrentSurfaceState = SurfaceState.Floor;
        }
        else if(angle >= 45f && angle < 135f)
        {
            CurrentSurfaceState = SurfaceState.WALL_R;
        }
        else if(angle >= 135f && angle < 225f)
        {
            CurrentSurfaceState = SurfaceState.Ceiling;
        }
        else if(angle >= 225f && angle < 315f)
        {
            Debug.Log("CurrentSurfaceState: WALL_L");
            CurrentSurfaceState = SurfaceState.WALL_L;
        }
        else //floor right half
        {
            CurrentSurfaceState = SurfaceState.Floor;
        }
    }

    private bool IsValidSurfaceContact(SensorContact contact, Vector2 castDirection)
    {
        if (!contact.hit)
        {
            return false;
        }

        // A valid surface normal should generally face against
        // the direction in which the sensor was cast.
        float alignment = Vector2.Dot(
            contact.normal.normalized,
            -castDirection.normalized);

        if (alignment < floorAlignmentThreshold)
        {
            return false;
        }

        return contact.signedDistance >= -penetrationRecoveryDistance && contact.signedDistance <= surfaceOffset + maxGroundSnapDistance;
    }

    private void SelectGroundSensors(SensorContact sensorA, SensorContact sensorB, Vector2 castDirection)
    {
        if (!IsValidSurfaceContact(sensorA, castDirection))
        {
            sensorA = default;
        }

        if (!IsValidSurfaceContact(sensorB, castDirection))
        {
            sensorB = default;
        }

        if (!sensorA.hit && !sensorB.hit)
        {
            PrimaryGroundSensor = default;
            SecondaryGroundSensor = default;
            isGrounded = false;
            return;
        }

        bool chooseA;

        if (sensorA.hit && sensorB.hit)
        {
            float distanceDifference = Mathf.Abs(
                sensorA.signedDistance - sensorB.signedDistance);

            if (distanceDifference <= sensorSwitchTolerance)
            {
                // When the sensors are almost equally close, choose the
                // normal most similar to the previously accepted surface.
                float continuityA =
                    Vector2.Dot(sensorA.normal, SurfaceNormal);

                float continuityB =
                    Vector2.Dot(sensorB.normal, SurfaceNormal);

                chooseA = continuityA >= continuityB;
            }
            else
            {
                chooseA =
                    sensorA.signedDistance <= sensorB.signedDistance;
            }
        }
        else
        {
            chooseA = sensorA.hit;
        }

        PrimaryGroundSensor = chooseA ? sensorA : sensorB;
        SecondaryGroundSensor = chooseA ? sensorB : sensorA;
        isGrounded = true;
    }

    private void CastGroundSensors(Vector2 positionOffset)
    {
        Vector2 direction = GroundDirection;
        Vector2 across = new Vector2(-direction.y, direction.x);

        Vector2 center =
            (Vector2)raycastOrigin.position
            + positionOffset
            + direction * groundSensorVerticalOffset;

        Vector2 anchorA = center + across * raycastHorizontalDistance;
        Vector2 anchorB = center - across * raycastHorizontalDistance;

        SensorContact sensorA = CastSignedSensor(anchorA, direction);
        SensorContact sensorB = CastSignedSensor(anchorB, direction);

        SelectGroundSensors(sensorA, sensorB, direction);

        if (isGrounded)
        {
            Vector2 resolvedNormal = PrimaryGroundSensor.normal;

            bool canBlendNormals = PrimaryGroundSensor.hit && SecondaryGroundSensor.hit && PrimaryGroundSensor.collider == SecondaryGroundSensor.collider &&
                Mathf.Abs(PrimaryGroundSensor.signedDistance - SecondaryGroundSensor.signedDistance) <= normalBlendDistance &&
                Vector2.Angle(PrimaryGroundSensor.normal, SecondaryGroundSensor.normal) <= normalBlendMaxAngle;

            if (canBlendNormals)
            {
                resolvedNormal = (PrimaryGroundSensor.normal + SecondaryGroundSensor.normal).normalized;
            }

            SurfaceNormal = resolvedNormal;
            SurfacePoint = PrimaryGroundSensor.point;
            SurfaceDistance = PrimaryGroundSensor.signedDistance;

            UpdateSurfaceState(SurfaceNormal);

        }
    }

    private void CastPushSensors(Vector2 intendedDisplacement)
    {
        Vector2 anchor = (Vector2)raycastOrigin.position + Vector2.up * pushSensorHeight;

        // Cast from the CURRENT position, not the predicted destination -
        // otherwise the ray starts on the far side of anything in the travel
        // path and can never see it. Ray length is whichever is longer:
        // pushRayDistance as a baseline lookahead, or the actual distance
        // we're about to move this frame - otherwise a fixed short ray
        // simply can't reach far enough to catch a wall at high speed,
        // letting the character tunnel straight through it.
        float rightLength = Mathf.Max(pushRayDistance, Mathf.Max(0f, intendedDisplacement.x));
        float leftLength = Mathf.Max(pushRayDistance, Mathf.Max(0f, -intendedDisplacement.x));

        pushHitLeft = Physics2D.Raycast(anchor, Vector2.left, leftLength, groundLayer);
        pushHitRight = Physics2D.Raycast(anchor, Vector2.right, rightLength, groundLayer);

        IsTouchingWallLeft = IsGenuineWall(pushHitLeft);
        IsTouchingWallRight = IsGenuineWall(pushHitRight);

        const float pushSkin = 0.02f;
        PushDistanceLeft = IsTouchingWallLeft ? Mathf.Max(0f, pushHitLeft.distance - pushSkin) : float.PositiveInfinity;
        PushDistanceRight = IsTouchingWallRight ? Mathf.Max(0f, pushHitRight.distance - pushSkin) : float.PositiveInfinity;
    }

        // Only treat a push-sensor hit as a wall worth blocking on if its
    // surface is close to vertical (within pushSensorAngleTolerance of pure
    // horizontal). Without this, the sensors would also fire against
    // ordinary shallow slopes that the ground sensors already handle fine,
    // stopping the character on terrain they should just be able to climb.
    private bool IsGenuineWall(RaycastHit2D hit)
    {
        if (hit.collider == null)
        {
            return false;
        }

        float angleFromVertical = Vector2.Angle(hit.normal, Vector2.up) - 90f;
        return Mathf.Abs(angleFromVertical) <= pushSensorAngleTolerance;
    }

    private SensorContact CastSignedSensor(Vector2 anchor, Vector2 direction)
    {
        RaycastHit2D forwardHit = Physics2D.Raycast(anchor, direction, GroundProbeDistance, groundLayer);

        RaycastHit2D regressionHit = Physics2D.Raycast(anchor, -direction, penetrationRecoveryDistance, groundLayer);

        if (regressionHit.collider != null)
        {
            return CreateContact(regressionHit, direction, -regressionHit.distance);
        }

        if (forwardHit.collider != null)
        {
            return CreateContact(forwardHit, direction, forwardHit.distance);
        }

        return default;
    }

    private SensorContact CreateContact(RaycastHit2D hit, Vector2 castDirection, float signedDistance)
    {
        Vector2 normal = hit.normal;

        if (Vector2.Dot(normal, castDirection) > 0f)
        {
            normal = -normal;
        }

        return new SensorContact
        {
            hit = true,
            collider = hit.collider,
            point = hit.point,
            normal = normal,
            castDirection = castDirection,
            signedDistance = signedDistance
        };
    }

private float GroundProbeDistance
{
    get
    {
        return Mathf.Max(
            rayCastDistance,
            surfaceOffset + maxGroundSnapDistance);
    }
}

   public Vector2 GroundDirection
    {
        get
        {
            switch (CurrentSurfaceState)
            {
                case SurfaceState.Floor:
                    return Vector2.down;

                case SurfaceState.WALL_L:
                    return Vector2.left;

                case SurfaceState.WALL_R:
                    return Vector2.right;

                case SurfaceState.Ceiling:
                    return Vector2.up;

                default:
                    return Vector2.down;
            }
        }
    }
private void OnDrawGizmos()
{
    if (raycastOrigin == null)
    {
        return;
    }

    Vector2 direction = GroundDirection;
    Vector2 across = new Vector2(-direction.y, direction.x);

    Vector2 center = (Vector2)raycastOrigin.position + direction * groundSensorVerticalOffset;

    Vector2 anchorA = center + across * raycastHorizontalDistance;

    Vector2 anchorB = center - across * raycastHorizontalDistance;

    // Shows the current cardinal ground direction.
    Gizmos.color = Color.white;
    Gizmos.DrawLine(raycastOrigin.position, (Vector2)raycastOrigin.position + direction * 0.4f);

    DrawSensorGizmo(anchorA, direction, Color.green, Color.red);

    DrawSensorGizmo(anchorB, direction, Color.cyan, Color.magenta);

    DrawContactGizmo(PrimaryGroundSensor, Color.yellow);

    DrawContactGizmo(SecondaryGroundSensor, Color.white);

        // Ceiling sensors always cast straight up in world space, independent
    // of the current ground quadrant.
    Vector2 ceilingOrigin = (Vector2)raycastOrigin.position + Vector2.up * ceilingSensorVerticalOffset;

    Vector2 ceilingAnchorA = ceilingOrigin - Vector2.right * raycastHorizontalDistance;
    Vector2 ceilingAnchorB = ceilingOrigin + Vector2.right * raycastHorizontalDistance;

    // Shows where the ceiling sensor pair sits relative to raycastOrigin.
    Gizmos.color = Color.white;
    Gizmos.DrawLine(raycastOrigin.position, ceilingOrigin);

    DrawCeilingSensorGizmo(ceilingAnchorA, ceilingHit1);

    DrawCeilingSensorGizmo(ceilingAnchorB, ceilingHit2);

      // Push sensors fire horizontally left/right from a single anchor point
    // at pushSensorHeight above raycastOrigin, independent of the current
    // ground quadrant.
    Vector2 pushAnchor =
        (Vector2)raycastOrigin.position
        + Vector2.up * pushSensorHeight;

    Gizmos.color = Color.white;
    Gizmos.DrawLine(raycastOrigin.position, pushAnchor);
    Gizmos.DrawWireSphere(pushAnchor, 0.025f);

    DrawPushSensorGizmo(pushAnchor, Vector2.left, pushHitLeft, IsTouchingWallLeft);

    DrawPushSensorGizmo(pushAnchor, Vector2.right, pushHitRight, IsTouchingWallRight);
}

private void DrawSensorGizmo(Vector2 anchor, Vector2 direction, Color forwardColor, Color recoveryColor)
    {
        // Sensor anchor.
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(anchor, 0.025f);

        // Forward surface search.
        Gizmos.color = forwardColor;
        Gizmos.DrawLine(anchor, anchor + direction * GroundProbeDistance);

        // Backward penetration-recovery search.
        Gizmos.color = recoveryColor;
        Gizmos.DrawLine(anchor, anchor - direction * penetrationRecoveryDistance);

        // Desired surface-offset position.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(anchor + direction * surfaceOffset, 0.02f);
    }

        // isWall reflects IsGenuineWall's angle-tolerance filter, not just
    // whether the ray hit something - so you can see the difference between
    // "grazed a shallow slope, correctly ignored" and "found a real wall,
    // will stop movement".
    private void DrawPushSensorGizmo(Vector2 anchor, Vector2 direction, RaycastHit2D hit, bool isWall)
    {
        bool hasHit = hit.collider != null;
        float rayLength = hasHit ? hit.distance : pushRayDistance;

        Color rayColor;
        if (!hasHit)
        {
            rayColor = Color.blue;
        }
        else if (isWall)
        {
            rayColor = Color.red;
        }
        else
        {
            // Hit something, but too shallow an angle to count as a wall.
            rayColor = new Color(1f, 0.5f, 0f);
        }

        Gizmos.color = rayColor;
        Gizmos.DrawLine(anchor, anchor + direction * rayLength);

        if (hasHit)
        {
            Gizmos.color = isWall ? Color.magenta : new Color(1f, 0.5f, 0f);
            Gizmos.DrawSphere(hit.point, 0.035f);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.3f);
        }
    }

    private void DrawContactGizmo(SensorContact contact, Color color)
    {
        if (!contact.hit)
        {
            return;
        }

        Gizmos.color = color;
        Gizmos.DrawSphere(contact.point, 0.035f);

        // Contact normal.
        Gizmos.DrawLine(contact.point, contact.point + contact.normal * 0.3f);
    }

    private void DrawCeilingSensorGizmo(Vector2 anchor, RaycastHit2D hit)
    {
        // Sensor anchor.
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(anchor, 0.025f);

        bool hasHit = hit.collider != null;
        float rayLength = hasHit ? hit.distance : raycastVerticalDistance;

        // Upward search ray - orange while clear, red for the portion that
        // actually found something.
        Gizmos.color = hasHit ? Color.red : new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(anchor, anchor + Vector2.up * rayLength);

        if (hasHit)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(hit.point, 0.035f);
            Gizmos.DrawLine(hit.point, hit.point + hit.normal * 0.3f);
        }
    }
}


using System;
using UnityEngine;

public class PlayerMovementState : PlayerBaseState
{

    private Vector2 previousSurfaceNormal;
    private bool hasPreviousNormal;

    public PlayerMovementState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        playerStateMachine.inputReader.JumpEvent += OnJump;
        playerStateMachine.collisionCheck.EnableGroundSensors();
        playerStateMachine.forceReciever.verticalVelocity = 0f;
        hasPreviousNormal = false;
        
    }

    public override void Exit()
    {
         playerStateMachine.inputReader.JumpEvent -= OnJump;
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        CollisionCheck collision = playerStateMachine.collisionCheck;

            // Current-position check.
            //collision.RefreshSensors(Vector2.zero);

            if (!collision.isGrounded)
            {
                DetachFromSurface();
                playerStateMachine.SwitchState(new PlayerAirState(playerStateMachine));
                return;
            }

            // Require momentum to stay attached in two cases: a true overhang
            // (gravity pulls you away from the surface, e.g. the top of a
            // loop), or terrain steeper than maxWalkableAngle (e.g. a near-
            // vertical wall you've slowed almost to a stop on - rather than
            // let the character crawl/clip along the wall face at ~0 speed,
            // it falls away like it would in real life). Ordinary half-pipe/
            // hillside slopes below that angle always stay attached
            // regardless of speed, since gravity there pulls you along the
            // surface, not off it.
            float slopeAngle = Vector2.Angle(Vector2.up, collision.SurfaceNormal);
            bool isOverhang = Vector2.Dot(Vector2.down, collision.SurfaceNormal) > 0f;
            bool isTooSteepToStick = slopeAngle > playerStateMachine.maxWalkableAngle;
            bool requiresMomentum = isOverhang || isTooSteepToStick;

            if (requiresMomentum && Mathf.Abs(playerStateMachine.groundSpeed) < playerStateMachine.minSpeedToStick)
            {
                DetachFromSurface();
                playerStateMachine.SwitchState(new PlayerAirState(playerStateMachine));
                return;
            }

            Vector2 normal = collision.SurfaceNormal;
            Vector2 tangent = Vector2.Perpendicular(normal);

            if (Vector2.Dot(tangent, playerStateMachine.characterGameObject.transform.right) < 0f)
            {
                tangent = -tangent;
            }

            Vector2 input = CalculateMovement2D();

            UpdateGroundSpeed(input.x, tangent, fixedDeltaTime);

            Vector2 intendedDisplacement = tangent * playerStateMachine.groundSpeed * fixedDeltaTime;

            if (intendedDisplacement.x > 0f && collision.IsTouchingWallRight && collision.PushDistanceRight < intendedDisplacement.x)
            {
                intendedDisplacement.x = collision.PushDistanceRight;
                playerStateMachine.groundSpeed = 0f;
            }
            else if (intendedDisplacement.x < 0f && collision.IsTouchingWallLeft && -collision.PushDistanceRight < -intendedDisplacement.x)
            {
                intendedDisplacement.x = -collision.PushDistanceLeft;
                playerStateMachine.groundSpeed = 0f;
            };

            // Cast from where the character is about to be.
            collision.RefreshSensors(intendedDisplacement);
            collision.RefreshPushSensors(intendedDisplacement);

            if (!collision.isGrounded)
            {
                MoveInPhysicsStep(tangent * playerStateMachine.groundSpeed, fixedDeltaTime);

                DetachFromSurface();
                playerStateMachine.SwitchState(new PlayerAirState(playerStateMachine));
                return;
            }

            SensorContact ground = collision.PrimaryGroundSensor;

            float snapAmount =
                ground.signedDistance - collision.SurfaceOffset;

            Vector2 snapCorrection =
                ground.castDirection * snapAmount;

            Vector2 finalDisplacement =
                intendedDisplacement + snapCorrection;

            MoveInPhysicsStep(
                finalDisplacement / fixedDeltaTime,
                fixedDeltaTime);

            AlignToSurface(fixedDeltaTime);
    }

    private void DetachFromSurface()
    {
        Vector2 normal = playerStateMachine.collisionCheck.SurfaceNormal;
        Vector2 tangent = Vector2.Perpendicular(normal);
        if (Vector2.Dot(tangent, playerStateMachine.characterGameObject.transform.right) < 0f)
        {
            tangent = -tangent;
        }

        
        Vector2 detachVelocity = tangent * playerStateMachine.groundSpeed
            + normal * playerStateMachine.detachForce;
        playerStateMachine.forceReciever.AddForce(detachVelocity);

        playerStateMachine.detachTimer = playerStateMachine.detachDuration;
    }

    public override void Tick(float deltaTime)
    {
    }
    private Vector2 CalculateMovement2D()
    {
        // Read raw input and clamp so diagonal/analog never exceeds magnitude 1.
        Vector2 input = playerStateMachine.inputReader.MovementValue;
        input = Vector2.ClampMagnitude(input, 1f);

        return Vector2.right * input.x;
    }

    private void UpdateGroundSpeed(float input, Vector2 surfaceTangent, float fixedDeltaTime)
    {
        float inputDirection = Mathf.Sign(input);

        if (input != 0f)
        {
            bool isReversing = playerStateMachine.groundSpeed != 0f
                && Mathf.Sign(playerStateMachine.groundSpeed) != inputDirection;
            float rate = isReversing
                ? playerStateMachine.Deceleration
                : playerStateMachine.Acceleration;
            float targetSpeed = input * playerStateMachine.TopSpeed;

            playerStateMachine.groundSpeed = Mathf.MoveTowards(
                playerStateMachine.groundSpeed,
                targetSpeed,
                rate * fixedDeltaTime);
        }
        else
        {
            playerStateMachine.groundSpeed = Mathf.MoveTowards(
                playerStateMachine.groundSpeed,
                0f,
                playerStateMachine.Friction * fixedDeltaTime);
        }

                // Apply slope acceleration AFTER accel/friction so it isn't immediately
        // overwritten by MoveTowards - this is what lets gravity actually speed
        // you up going downhill and slow you down going uphill.
        Vector2 gravity = Vector2.down * playerStateMachine.gravity * playerStateMachine.slopeGravityMultiplier;
        float slopeAcceleration = Vector2.Dot(gravity, surfaceTangent);
        playerStateMachine.groundSpeed += slopeAcceleration * fixedDeltaTime;

        playerStateMachine.groundSpeed = Mathf.Clamp(
            playerStateMachine.groundSpeed,
            -playerStateMachine.maxMomentumSpeed,
            playerStateMachine.maxMomentumSpeed);
    }

    private void AlignToSurface(float deltaTime)
    {
        Transform characterTransform = playerStateMachine.characterGameObject.transform;
        Quaternion targetRotation = Quaternion.FromToRotation(
            characterTransform.up,
            playerStateMachine.collisionCheck.SurfaceNormal) * characterTransform.rotation;

        Quaternion nextRotation = Quaternion.RotateTowards(
            characterTransform.rotation,
            targetRotation,
            playerStateMachine.SurfaceRotationSpeed * deltaTime);

        Rigidbody2D rigidbody = playerStateMachine.rb;
        if (rigidbody != null)
        {
            rigidbody.MoveRotation(nextRotation.eulerAngles.z);
            if (rigidbody.transform != characterTransform)
            {
                characterTransform.rotation = nextRotation;
            }

            return;
        }

        characterTransform.rotation = nextRotation;
    }

    

    private void OnJump()
    {
        playerStateMachine.SwitchState(new PlayerJumpState(playerStateMachine));
    }
}

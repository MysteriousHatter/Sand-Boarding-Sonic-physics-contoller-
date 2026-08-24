using UnityEngine;

public class PlayerAirState : PlayerBaseState
{
    public PlayerAirState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        playerStateMachine.collisionCheck.EnableGroundSensors();
        playerStateMachine.collisionCheck.ResetSurfaceState();

        Rigidbody2D rigidbody = playerStateMachine.rb;
        if (rigidbody != null)
        {
            rigidbody.MoveRotation(0f);
            return;
        }

        playerStateMachine.characterGameObject.transform.rotation = Quaternion.identity;
    }

    public override void Tick(float deltaTime)
    {
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        Vector2 input = Vector2.ClampMagnitude(playerStateMachine.inputReader.MovementValue, 1f);
        Vector2 horizontalMovement = Vector2.right
            * input.x * playerStateMachine.FreeLookMovementSpeed;

        playerStateMachine.forceReciever.verticalVelocity -= playerStateMachine.gravity * fixedDeltaTime;
        playerStateMachine.forceReciever.verticalVelocity = Mathf.Max(playerStateMachine.forceReciever.verticalVelocity, -playerStateMachine.MaxFallSpeed);

        Vector2 airVelocity = horizontalMovement
            + playerStateMachine.forceReciever.impact
            + Vector2.up * playerStateMachine.forceReciever.verticalVelocity;

        Vector2 intendedDisplacement = airVelocity * fixedDeltaTime;
        CollisionCheck collision = playerStateMachine.collisionCheck;

        collision.RefreshPushSensors(intendedDisplacement);

        if (intendedDisplacement.x > 0f && collision.IsTouchingWallRight && collision.PushDistanceRight < intendedDisplacement.x)
        {
            intendedDisplacement.x = collision.PushDistanceRight;
            airVelocity.x = 0f;
        }
        else if (intendedDisplacement.x < 0f && collision.IsTouchingWallLeft && collision.PushDistanceLeft < -intendedDisplacement.x)
        {
            intendedDisplacement.x = -collision.PushDistanceLeft;
            airVelocity.x = 0f;
        }

                // Only worth checking for a head-bump while still moving upward.
        if (playerStateMachine.forceReciever.verticalVelocity > 0f)
        {
            collision.RefreshCeilingSensors(intendedDisplacement);

            if (collision.TryGetCeilingHit(out RaycastHit2D ceilingHit))
            {
                playerStateMachine.forceReciever.verticalVelocity = 0f;
                airVelocity.y = 0f;
                intendedDisplacement = airVelocity * fixedDeltaTime;
            }
        }

        collision.RefreshSensors(intendedDisplacement);

        SensorContact ground = collision.PrimaryGroundSensor;
        bool movingTowardGround = ground.hit
            && Vector2.Dot(airVelocity, ground.castDirection) > 0f;
        bool canLand = playerStateMachine.detachTimer <= 0f
            && collision.isGrounded
            && movingTowardGround;

        if (canLand)
        {
            float snapAmount = ground.signedDistance - collision.SurfaceOffset;
            Vector2 snapCorrection = ground.castDirection * snapAmount;
            Vector2 finalDisplacement = intendedDisplacement + snapCorrection;

            MoveInPhysicsStep(finalDisplacement / fixedDeltaTime, fixedDeltaTime);

            // Convert airborne velocity into surface-relative ground speed.
            Vector2 surfaceTangent = new Vector2(ground.normal.y, -ground.normal.x).normalized;

            playerStateMachine.groundSpeed = Vector2.Dot(airVelocity, surfaceTangent);

            // Prevent old airborne momentum from being reused next time.
            playerStateMachine.forceReciever.ClearImpact();
            playerStateMachine.forceReciever.verticalVelocity = 0f;

            playerStateMachine.SwitchState(new PlayerMovementState(playerStateMachine));

            return;
        }

        MoveInPhysicsStep(airVelocity, fixedDeltaTime);
    }

    public override void Exit()
    {
    }
}

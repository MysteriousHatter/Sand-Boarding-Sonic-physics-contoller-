using System;
using UnityEngine;

public class PlayerMovementState : PlayerBaseState
{

    public PlayerMovementState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        playerStateMachine.inputReader.JumpEvent += OnJump;
        playerStateMachine.collisionCheck.EnableGroundSensors();
        playerStateMachine.forceReciever.verticalVelocity = 0f;
        playerStateMachine.forceReciever.verticalVelocity = 0f; 
        
    }

    public override void Exit()
    {
         playerStateMachine.inputReader.JumpEvent -= OnJump;
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        playerStateMachine.collisionCheck.RefreshSensors();

        CollisionCheck.SurfaceState surfaceState = playerStateMachine.collisionCheck.CurrentSurfaceState;
        bool isWall = surfaceState == CollisionCheck.SurfaceState.WALL_L
            || surfaceState == CollisionCheck.SurfaceState.WALL_R;
        bool canStickToWall = isWall
            && Mathf.Abs(playerStateMachine.groundSpeed) >= playerStateMachine.minSpeedToStick;

        if (!playerStateMachine.collisionCheck.isGrounded || (surfaceState != CollisionCheck.SurfaceState.Floor && !canStickToWall))
        {
            Debug.Log("Switching to air state");
            DeatchFromSurface();
            playerStateMachine.SwitchState(new PlayerAirState(playerStateMachine));
            return;
        }

        float maxCorrectionDistance = 8f * fixedDeltaTime;
        float correctionDistance = Mathf.Clamp(
            playerStateMachine.collisionCheck.SurfaceOffset - playerStateMachine.collisionCheck.SurfaceDistance,
            -maxCorrectionDistance,
            maxCorrectionDistance);

        AlignToSurface(fixedDeltaTime);

        Vector2 movementInput = CalculateMovement2D();
        Vector2 surfaceTangent = Vector2.Perpendicular(playerStateMachine.collisionCheck.SurfaceNormal);
        if (Vector2.Dot(surfaceTangent, playerStateMachine.characterGameObject.transform.right) < 0f)
        {
            surfaceTangent = -surfaceTangent;
        }

        Vector2 surfaceCorrectionVelocity = playerStateMachine.collisionCheck.SurfaceNormal * (correctionDistance / fixedDeltaTime);
        UpdateGroundSpeed(movementInput.x, surfaceTangent, fixedDeltaTime);

        MoveInPhysicsStep(surfaceCorrectionVelocity + surfaceTangent * playerStateMachine.groundSpeed, fixedDeltaTime);
    }

    private void DeatchFromSurface()
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
        Vector2 gravity = Vector2.down * playerStateMachine.gravity * playerStateMachine.slopeGravityMultiplier;
        float slopeAcceleration = Vector2.Dot(gravity, surfaceTangent);

        playerStateMachine.groundSpeed += slopeAcceleration * fixedDeltaTime;

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

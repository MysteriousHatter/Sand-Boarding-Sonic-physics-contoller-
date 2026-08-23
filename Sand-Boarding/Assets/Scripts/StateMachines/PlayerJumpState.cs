using UnityEngine;

public class PlayerJumpState : PlayerBaseState
{
    private Vector2 surfaceNormal;

    public PlayerJumpState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        surfaceNormal = playerStateMachine.collisionCheck.SurfaceNormal;
        playerStateMachine.collisionCheck.EnableCeilingSensors();
        playerStateMachine.forceReciever.Jump(playerStateMachine.jumpForce);
    }

    public override void Tick(float deltaTime)
    {
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        //playerStateMachine.collisionCheck.RefreshSensors();

        Vector2 input = Vector2.ClampMagnitude(playerStateMachine.inputReader.MovementValue, 1f);
        float horizontalInput = input.x;

        if (horizontalInput > 0f && playerStateMachine.collisionCheck.IsTouchingWallRight)
        {
            horizontalInput = 0f;
        }
        else if (horizontalInput < 0f && playerStateMachine.collisionCheck.IsTouchingWallLeft)
        {
            horizontalInput = 0f;
        }

        Vector2 tangentMovement = (Vector2)playerStateMachine.characterGameObject.transform.right
            * horizontalInput * playerStateMachine.FreeLookMovementSpeed;

        playerStateMachine.forceReciever.verticalVelocity += (-playerStateMachine.gravity) * fixedDeltaTime;

        float upwardDistanceThisFrame = playerStateMachine.forceReciever.verticalVelocity * fixedDeltaTime;

        if (upwardDistanceThisFrame > 0f
            && playerStateMachine.collisionCheck.CeilingDistance <= upwardDistanceThisFrame)
        {
            playerStateMachine.forceReciever.verticalVelocity = 0f;
            playerStateMachine.collisionCheck.EnableGroundSensors();
        }

        Vector2 finalMovement = tangentMovement
            + surfaceNormal * playerStateMachine.forceReciever.verticalVelocity;

        MoveInPhysicsStep(finalMovement, fixedDeltaTime);

        // Rising → Falling transition
        if (playerStateMachine.forceReciever.verticalVelocity <= 0f)
        {
            Debug.Log("Switch to falling");
            playerStateMachine.SwitchState(new PlayerAirState(playerStateMachine));
            return;
        }
    }

    public override void Exit()
    {
    }
}

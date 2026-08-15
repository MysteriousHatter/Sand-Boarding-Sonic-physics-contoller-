using UnityEngine;

public class PlayerAirState : PlayerBaseState
{
    public PlayerAirState(PlayerStateMachine playerStateMachine) : base(playerStateMachine)
    {
    }

    public override void Enter()
    {
        playerStateMachine.collisionCheck.EnableGroundSensors();

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
        if(playerStateMachine.detachTimer > 0f)
        {
            return;
        }

        if (playerStateMachine.collisionCheck.isGrounded)
        {
            playerStateMachine.SwitchState(new PlayerMovementState(playerStateMachine));
            return;
        }
    }

    public override void FixedTick(float fixedDeltaTime)
    {
        //playerStateMachine.collisionCheck.RefreshSensors();



        Vector2 input = Vector2.ClampMagnitude(playerStateMachine.inputReader.MovementValue, 1f);
        Vector2 tangentMovement = (Vector2)playerStateMachine.characterGameObject.transform.right
            * input.x * playerStateMachine.FreeLookMovementSpeed;

        playerStateMachine.forceReciever.verticalVelocity -= playerStateMachine.gravity * fixedDeltaTime;

        Vector2 finalMovement = tangentMovement
            + playerStateMachine.forceReciever.impact
            + Vector2.up * playerStateMachine.forceReciever.verticalVelocity;

        MoveInPhysicsStep(finalMovement, fixedDeltaTime);
    }
    public override void Exit()
    {
        
    }
}

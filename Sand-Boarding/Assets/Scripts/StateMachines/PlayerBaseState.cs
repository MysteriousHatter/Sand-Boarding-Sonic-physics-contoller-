using UnityEngine;

public abstract class PlayerBaseState : State
{
    protected PlayerStateMachine playerStateMachine;
    private readonly RaycastHit2D[] movementHits = new RaycastHit2D[1];

    public PlayerBaseState(PlayerStateMachine playerStateMachine)
    {
        this.playerStateMachine = playerStateMachine;
    }

    protected void Move(UnityEngine.Vector2 motion, float deltaTime)
    {
        // Add horizontal and vertical motion to velocity.
        playerStateMachine.Velocity.x = motion.x;
        playerStateMachine.Velocity.y = motion.y;

        // Vertical velocity is already updated by gravity or jump.
        UnityEngine.Vector2 finalVelocity = playerStateMachine.Velocity;

        // Apply full precision movement to avoid step-wise jitter.
        UnityEngine.Vector3 displacement = new UnityEngine.Vector3(finalVelocity.x, finalVelocity.y, 0f) * deltaTime;
        playerStateMachine.characterGameObject.transform.position += displacement;
    }

    protected void MoveInPhysicsStep(Vector2 motion, float fixedDeltaTime)
    {
        playerStateMachine.Velocity = motion;

        Vector2 displacement = playerStateMachine.Velocity * fixedDeltaTime;
        Rigidbody2D rigidbody = playerStateMachine.rb;

        if (rigidbody != null)
        {
            float distance = displacement.magnitude;
            if (distance > 0f)
            {
                ContactFilter2D collisionFilter = new ContactFilter2D
                {
                    useLayerMask = false,
                    useTriggers = false
                };

                int hitCount = rigidbody.Cast(
                    displacement.normalized,
                    collisionFilter,
                    movementHits,
                    distance);

                if (hitCount > 0)
                {
                    const float skinWidth = 0.01f;
                    displacement = displacement.normalized
                        * Mathf.Max(0f, movementHits[0].distance - skinWidth);
                }
            }

            rigidbody.MovePosition(rigidbody.position + displacement);
            return;
        }

        playerStateMachine.characterGameObject.transform.position += (Vector3)displacement;
    }

    protected void SnapToPlatform()
    {
        
    }
    
}

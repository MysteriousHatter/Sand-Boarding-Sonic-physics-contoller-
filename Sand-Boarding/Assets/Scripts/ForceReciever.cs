using UnityEngine;

public class ForceReciever : MonoBehaviour
{
    public Vector2 impact;          // external forces
    public float verticalVelocity;  // gravity/jump
    public float drag = 0.3f;
    [SerializeField] private PlayerStateMachine playerStateMachine;

    private Vector2 dampingVelocity;

    public Vector2 Movement => impact + new Vector2(0f, verticalVelocity);

    public void FixedUpdate()
    {
        // if (verticalVelocity < 0f && playerStateMachine.collisionCheck.isGrounded)
        // {
        //     Debug.Log("Grounded");
        //     verticalVelocity = 0f * Time.fixedDeltaTime;
        // }
        // else
        // {
        //     verticalVelocity += -playerStateMachine.gravity * Time.fixedDeltaTime;
        // }
        // Smoothly remove external forces
        impact = Vector2.SmoothDamp(impact, Vector2.zero, ref dampingVelocity, drag);
    }

    public void AddForce(Vector2 force)
    {
        impact += force;
    }

    public void Jump(float jumpForce)
    {
        verticalVelocity += jumpForce;
    }
}

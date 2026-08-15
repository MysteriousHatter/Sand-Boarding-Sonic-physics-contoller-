using UnityEngine;

public class PlayerStateMachine : StateMachine
{
    [field:SerializeField] public InputReader inputReader {get; private set;}
    [field:SerializeField] public float FreeLookMovementSpeed {get; private set;}
    [field:SerializeField] public float RotationDamping {get; private set;}
    [field:SerializeField] public float jumpForce {get; private set;}
    [field:SerializeField] public float SurfaceRotationSpeed {get; private set;} = 720f;
    [field:SerializeField] public GameObject characterGameObject;
    public Animator animatorController => GetComponent<Animator>();
    public CollisionCheck collisionCheck => GetComponentInChildren<CollisionCheck>();
    [HideInInspector] public Vector2 Velocity;
    public ForceReciever forceReciever => GetComponentInChildren<ForceReciever>();
    public PhysicsCheck physicsCheck => GetComponentInChildren<PhysicsCheck>();
    public Rigidbody2D rb => GetComponentInChildren<Rigidbody2D>();


    [Header("Ground Movement")]
    [field: SerializeField, Min(0f)] public float TopSpeed { get; private set; } = 18f;
    [field: SerializeField, Min(0f)] public float Acceleration { get; private set; } = 35f;
    [field: SerializeField, Min(0f)] public float Deceleration { get; private set; } = 20f;
    [field: SerializeField, Min(0f)] public float Friction { get; private set; } = 20f;
    [field:SerializeField] public float gravity {get; private set;}
    [SerializeField] public float slopeGravityMultiplier = 1.5f;
    [SerializeField] private float maxFallSpeed = 30f;
    [SerializeField] private float airControl = 0.75f;
    [SerializeField] public float minSpeedToStick = 3f; // tweak this
    [HideInInspector] public float groundSpeed;
    public float maxMomentumSpeed = 40f;

    [SerializeField] public float detachDistance = 0.05f;
    [SerializeField] public float detachForce = 2f;
    [SerializeField] public float detachDuration = 0.1f;

    public float detachTimer;

    //public Transform MainCameraTransform {get; private set;}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //MainCameraTransform = Camera.main.transform;
        SwitchState(new PlayerMovementState(this));
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (detachTimer > 0f)
        {
            detachTimer -= Time.fixedDeltaTime;
        }
    }
}

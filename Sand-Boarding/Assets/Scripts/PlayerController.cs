// GameDev.tv Challenge Club. Got questions or want to share your nifty solution?
// Head over to - http://community.gamedev.tv

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Purchasing;
using System;

using UnityEngine.U2D;

public class PlayerController : MonoBehaviour
{
    [Header("General Requirments")]
    Rigidbody2D rb2D;
    private RaycastHit hit;
    [SerializeField] private GameObject bottom;
    [SerializeField] private LayerMask groundLayerMask;
    private bool isGrounded = false;
    private bool hasLanded = true;
    [SerializeField] private float raycastDistance = 0.1f;
    [SerializeField] private float flipUpwardSpeed = 360f;
    [SerializeField] private float moveUpDistance = 1f; // Distance to move up
    [SerializeField] private GameObject dustParticle;
    [SerializeField] private GameObject railParticle;
    private GameObject dustParticleClone;
    private GameObject railParticleClone;
    [SerializeField] private Transform dustEmiiterLocation;
    [SerializeField] private float jumpForce;
    public bool isUpsideDown { get; set; }
    enum PlayerState { Ground, Air, Grind }
    private PlayerState state;

    // Player Controls
    [Header("Player Controls")]
    private PlayerControls playerControls;
    private bool isFlipping = false;
    [SerializeField] private float flipSpeed = 360f; // Speed of the flip rotation in degrees per second
    private InputAction upTrick;
    private InputAction downTrick;
    private InputAction leftTrick;
    private InputAction rightTrick;
    private InputAction LeftFinisher;
    private InputAction RightFinisher;
    public KeyCode[] directions = { KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D }; // List of keys for each direction
    private KeyCode lastKey; // Last direction key pressed by the player


    // Boost System
    [Header("Boost Configuration")]
    [SerializeField] private float maxSpeed = 10f;        // Maximum speed
    [SerializeField] private float minSpeed = 2f;         // Minimum speed
    [SerializeField] private float acceleration = 1f;     // How fast the player speeds up
    [SerializeField] private float deceleration = 1f;     // How fast the player slows down
    [SerializeField] private float crashBurnTime = 3f;    // Time player can stay at max speed before crashing
    [SerializeField] private float debuffDuration = 5f;   // Duration of the debuff after crash and burn
    private float currentSpeed = 0f;    // Current speed of the player
    private float horizontalInput = 0f; // Input from the horizontal keys
    private float timeAtMaxSpeed = 0f;  // Time spent at max speed
    private bool isCrashed = false;     // Whether the player is in "crash and burn" state


    // Trick System
    [Header("Trick System")]
    [SerializeField] float trickHeight = 2f;
    bool canPerformTricks;
    public List<TrickAction> trickActions = new List<TrickAction>();
    private float lastTrickTime = 0f;
    private float trickDebounceTime = 0.2f; // 200 milliseconds debounce time
    public float landingHeight = 2f;
    private string lastTrickIdentifier;

    // Trick Execution Feedback
    [Header("Trick Execution Feedback")]
    public bool didFinisher = false;
    private bool failedFinisher = false;
    private bool pressingTrickButton;
    [SerializeField] private GameObject trickSparkle;
    [SerializeField] private AnimationClip[] animationNames;
    [SerializeField] private AnimationClip FinisherAnimation;

    // Trick Scoring
    [Header("Trick Scoring")]
    public float increasePointRate = 0.5f; // How much the increment rate increases when the player does a different trick
    public float decrementPointRate = 0.3f; // How much the increment rate decreases when the player repeats the same trick
    private float currentPointRate = 0f;
    private int totalNumOfTricks = 0;
    private int totalpointsAccumilated = 0;

    //Grinding
    [Header("Rail Grinding")]
    [SerializeField] private float grindingSpeed = 5f;
    [SerializeField] Transform grindPoint;
    [SerializeField] private float grindAngle = 15;
    [SerializeField] private float grindRaycastDistance = 12f;
    [SerializeField] private Vector2 directionFall;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private Transform fallOffPoint;
    [SerializeField] private float rotationSmoothin;
    [SerializeField] private LayerMask grindLayer;
    [SerializeField] private int railMultiplier = 2;
    [SerializeField] private float horizontalJumpForce = 3f;
    [SerializeField] private float verticaalJumpForce = 2f;

    [Header("Balancing")]
    [SerializeField] private float lowSafeThreshold = 4f;
    [SerializeField] private float highSafeThreshold = 8f;
    [SerializeField] private float incrementValue;
    [SerializeField] private float warningDelay = 1.0f;
    private float currentThreshold;
    private bool failedBalancing = false;


    public float currentBoostMode { get; set; }
    private Animation animationComponent => GetComponent<Animation>();
    [SerializeField] private float rotationSpeed = 300f; // Rotation speed in degrees per second
    private Vector2 initialPosition;

    private UIManager manager => FindObjectOfType<UIManager>();

    private void Awake()
    {
        playerControls = new PlayerControls();
        upTrick = playerControls.Tricks.TrickUp;
        downTrick = playerControls.Tricks.TrickDown;
        leftTrick = playerControls.Tricks.TrickLeft;
        rightTrick = playerControls.Tricks.TrickRight;
        UIManager.Instance.onHideFinisherUI.Invoke();
        manager.onHideRailCanvas.Invoke();
        UIManager.Instance.noAttemptFinisher = true;

        LeftFinisher = playerControls.Tricks.TrickFinisherLeft;
        RightFinisher = playerControls.Tricks.TrickFinisherRight;

        playerControls.Controls.UpRotation.performed += context => StartCoroutine(RotatePlayer(-1));
        playerControls.Controls.DownRotation.performed += context => StartCoroutine(RotatePlayer(1));
        // Bind input actions for moving left and right
        playerControls.Controls.SpeedUpSlowDown.performed += context => horizontalInput = context.ReadValue<float>();
        playerControls.Controls.SpeedUpSlowDown.canceled += context => horizontalInput = 0f; // Reset when no input
        playerControls.Controls.Jump.performed += OnJumpPerformed;
        playerControls.Grinding.Jump.performed += OnJumpPerformed;
        playerControls.Testing.Restart.performed += OnRestartPerfromed;

       dustParticleClone = Instantiate(dustParticle, dustEmiiterLocation.transform.position, Quaternion.identity, this.gameObject.transform);
       railParticleClone = Instantiate(railParticle, dustEmiiterLocation.transform.position, Quaternion.identity, this.gameObject.transform);
    }

    private void OnRestartPerfromed(InputAction.CallbackContext obj)
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    private void OnEnable()
    {
        upTrick.Enable();
        downTrick.Enable();
        leftTrick.Enable();
        rightTrick.Enable();
        LeftFinisher.Enable();
        RightFinisher.Enable();
        playerControls.Testing.Restart.performed += OnRestartPerfromed;

        // Subscribe to the performed events
        upTrick.performed += ctx => PerfromTricks("TrickUp");
        downTrick.performed += ctx => PerfromTricks("TrickDown");
        leftTrick.performed += ctx => PerfromTricks("TrickLeft");
        rightTrick.performed += ctx => PerfromTricks("TrickRight");
        playerControls.Controls.Enable();
        playerControls.Grinding.Enable();
    }

    private void OnDisable()
    {
        upTrick.Disable();
        downTrick.Disable();
        leftTrick.Disable();
        rightTrick.Disable();
        LeftFinisher.Disable();
        RightFinisher.Disable();
        playerControls.Testing.Restart.performed -= OnRestartPerfromed;

        // Unsubscribe from the performed events
        upTrick.performed -= ctx => PerfromTricks("TrickUp");
        downTrick.performed -= ctx => PerfromTricks("TrickDown");
        leftTrick.performed -= ctx => PerfromTricks("TrickLeft");
        rightTrick.performed -= ctx => PerfromTricks("TrickRight");
        playerControls.Controls.Disable();
        playerControls.Grinding.Disable();
    }

    // Start is called before the first frame update
    void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();

        Debug.Log("We have this amount of animation clips " + animationComponent.GetClipCount());
        Debug.Log("This is the finisher " + FinisherAnimation.name);
        animationNames = new AnimationClip[animationComponent.GetClipCount()];
        int index = 0;
        initialPosition = rb2D.transform.position;

        foreach (AnimationState state in animationComponent)
        {
            animationNames[index] = state.clip;
            index++;
        }


        trickActions.Add(new TrickAction { identifier = "TrickUp", action = upTrick, animationClip = animationNames[0] });
        trickActions.Add(new TrickAction { identifier = "TrickDown", action = downTrick, animationClip = animationNames[1] });
        trickActions.Add(new TrickAction { identifier = "TrickLeft", action = leftTrick, animationClip = animationNames[2] });
        trickActions.Add(new TrickAction { identifier = "TrickRight", action = rightTrick, animationClip = animationNames[3] });
    }


    void Update()
    {

        if(UnityEngine.Input.GetKeyDown(KeyCode.O)) { RespawnMethod(); }
        if (isGrounded && state == PlayerState.Ground)
        {
            currentPointRate = 0;
            manager.onHideTrickUI.Invoke();
            manager.onHideRailCanvas.Invoke();
            UIManager.Instance.onHideFinisherUI.Invoke();
            dustParticleClone.GetComponent<ParticleSystem>().Play();
            railParticleClone.GetComponent<ParticleSystem>().Pause();
            if (isCrashed)
            {
                // If crashed, prevent any speed increase and keep the speed at min
                currentSpeed = minSpeed;
                ApplyMovement();
                return;
            }
            Boost();
        }

        if (!isGrounded && state == PlayerState.Air)
        {
            PerfromFinisherTrick();
            dustParticleClone.GetComponent<ParticleSystem>().Pause();
            railParticleClone.GetComponent<ParticleSystem>().Pause();
            StopCoroutine("ThresholdCheckCoroutine");
            StopCoroutine("AdjustThreshold");
            currentThreshold = 0f;

        }

        RaycastHit2D hit = Physics2D.Raycast(bottom.transform.position, -Vector2.down, raycastDistance, groundLayerMask);
        Debug.DrawRay(grindPoint.transform.position, directionFall * grindRaycastDistance * 1f, Color.cyan);
        // Check if the player is on the ground
        if (hit.collider != null)
        {
            isGrounded = true;
            hasLanded = true;
            //trickSparkle.SetActive(false);
            didFinisher = false;
            // Calculate the distance between the player and the ground
            float distanceToGround = hit.distance;
            Debug.Log("Where htting the ground");
            // Allow tricks if the player is far enough from the ground
            canPerformTricks = distanceToGround >= trickHeight;

            if (hit.collider.CompareTag("Ground"))
            {
                currentPointRate = 0;
                currentThreshold = 0f;
                manager.onHideTrickUI.Invoke();
                UIManager.Instance.onHideFinisherUI.Invoke();
                manager.onHideRailCanvas.Invoke();
                state = PlayerState.Ground;
                playerControls.Grinding.Disable();
                failedBalancing = false;
                StopCoroutine("AdjustThreshold");
            }
        }
        else
        {
            Debug.Log("Ground Collider is null");
            isGrounded = false;
            state = PlayerState.Air;
            rb2D.isKinematic = false;
            hasLanded = false;
            if (canPerformTricks && !didFinisher) { manager.onShowTrickUI.Invoke(); }
        }

        RailGrindCheck();

        CheckIfWeLanded();

        // Check if the player failed a trick and reset related flags using a coroutine
        if (failedFinisher || UIManager.Instance.failedComboUI)
        {
            StartCoroutine(ResetTrickFlagsAfterDelay());
        }

        if (isUpsideDown)
        {
            // Flip the object back to its normal orientation
            FlipToUpright();
        }


    }

    private void RailGrindCheck()
    {
        if (state != PlayerState.Grind && rb2D.linearVelocity.y < 0)
        {
            RaycastHit2D grindHit = Physics2D.CircleCast(grindPoint.transform.position, grindAngle, directionFall, grindRaycastDistance, grindLayer);
            if (grindHit)
            {
                SpriteShapeController rail = grindHit.collider.GetComponent<SpriteShapeController>();

                if (rail != null)
                {
                    transform.position = grindHit.point;
                    manager.onHideTrickUI.Invoke();
                    currentPointRate = 0;
                    didFinisher = false;
                    UIManager.Instance.onHideFinisherUI.Invoke();
                    manager.onShowRailCanvas.Invoke();
                    //transform.rotation = Quaternion.identity;
                    Debug.Log("Current rail");
                    state = PlayerState.Grind;
                    playerControls.Controls.Disable();
                    playerControls.Grinding.Enable();
                    BalancingOnRail();
                    ChangeIndicator();
                    Debug.Log("We are grinding");


                }
            }
        }
        if(state == PlayerState.Grind)
        {

            RaycastHit2D hitFall = Physics2D.Raycast(fallOffPoint.position, -fallOffPoint.up, grindRaycastDistance * 2f, grindLayer);
            Debug.DrawRay(fallOffPoint.position, -fallOffPoint.up * grindRaycastDistance * 4f, Color.green);
            if (hitFall.collider == null)
            {
                Debug.Log("Fall of rail");
                rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x * horizontalJumpForce, jumpForce * verticaalJumpForce);
                state = PlayerState.Air;
                manager.onHideRailCanvas.Invoke();
                manager.onShowTrickUI.Invoke();
                playerControls.Controls.Enable();
                playerControls.Grinding.Disable();

                failedBalancing = false;
                return;
            }
            Debug.Log("We are grinding");
            Vector2 dir = (playerCenter.right * directionFall.x + -playerCenter.up * directionFall.y);
            RaycastHit2D hitGrind = Physics2D.Raycast(playerCenter.position, dir, grindRaycastDistance * 4, grindLayer);
            Debug.DrawRay(playerCenter.position, dir * grindRaycastDistance * 4f, Color.green);
            if (hitGrind && !failedBalancing) // Currently Grinding
            {
                Rail rail = hitGrind.transform.gameObject.GetComponent<Rail>();
                railParticleClone.GetComponent<ParticleSystem>().Play();
                Vector2 current = hitGrind.point;
                Quaternion newRot = rb2D.transform.rotation;
                ScoreManager.Instance.score += railMultiplier;
                GrindAngle(hitGrind, out newRot, rail);
                rb2D.transform.rotation = Quaternion.Slerp(rb2D.transform.rotation, newRot, rotationSmoothin * Time.deltaTime);
                Debug.Log("Current rotatin " + rb2D.transform.rotation);
                rb2D.position = current;

            }
            else if (failedBalancing)
            {
                hitGrind.collider.GetComponent<EdgeCollider2D>().enabled = false;
                state = PlayerState.Air;
                playerControls.Controls.Enable();
                playerControls.Grinding.Disable();

            }
        }

    }


    private void BalancingOnRail()
    {
        Vector2 inputVector = playerControls.Grinding.Balancing.ReadValue<Vector2>();
        if(inputVector.x != 0 || state == PlayerState.Grind)
        {
            StartCoroutine(AdjustThreshold(inputVector.x));
        }
        else
        {
            StopCoroutine("AdjustThreshold");
        }


    }

    private Coroutine thresholdCheckCoroutine;
    IEnumerator AdjustThreshold(float direction)
    {
        while (state == PlayerState.Grind)
        {
            // Adjust the value based on the direction
            if (direction > 0)
            {
                currentThreshold += incrementValue * Time.deltaTime;
            }
            else
            {
                currentThreshold -= incrementValue * Time.deltaTime;
            }

            if (currentThreshold >= 10) { currentThreshold = 10; }
            else if(currentThreshold < 0) {  currentThreshold = 0; }
            // Output the current value
            Debug.Log("Current Value: " + currentThreshold);

            if(currentThreshold < lowSafeThreshold || currentThreshold > highSafeThreshold) 
            {
                Debug.Log("Start check");
                if(thresholdCheckCoroutine == null)
                {
                    thresholdCheckCoroutine = StartCoroutine(ThresholdCheckCoroutine());
                }
                
            }
            else
            {
                Debug.Log("Stop check");
                // Stop the running coroutine if it exists
                if (thresholdCheckCoroutine != null)
                {
                    StopCoroutine(thresholdCheckCoroutine);
                    thresholdCheckCoroutine = null;
                    Debug.Log("Stop Coroutine");
                }
            }
            // Wait for the next frame
            yield return null;
        }
        
    }

    public void ChangeIndicator()
    {
        if(currentThreshold < lowSafeThreshold || currentThreshold > highSafeThreshold)
        {
            Debug.Log("Turn color red");
            UIManager.Instance.CheckIfPointerInSafeZone(false);
        }
        else
        {
            UIManager.Instance.CheckIfPointerInSafeZone(true);
        }
    }

    IEnumerator ThresholdCheckCoroutine()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(warningDelay);
        Debug.Log("Check threshold");

        // After the delay, if the current value is still out of the thresholds, print the warning message
        if (currentThreshold < lowSafeThreshold || currentThreshold > highSafeThreshold)
        {
            failedBalancing = true;
            
        }
        else
        {
            failedBalancing = false;
        }

        thresholdCheckCoroutine = null;
        yield return new WaitForSeconds(1.0f);
        failedBalancing = false;

    }

    private void RespawnMethod()
    {
        rb2D.transform.position = initialPosition;
    }

    [SerializeField] float speedRate = 10f;
    private void GrindAngle(RaycastHit2D hitGrind, out Quaternion newRot, Rail rail)
    {
        float angle = Mathf.Atan2(hitGrind.normal.x,hitGrind.normal.y) * Mathf.Rad2Deg * -1; //give our angle while grinding
        Debug.Log("Our angle while grinding " + angle);
        newRot = Quaternion.Euler(new Vector3(0, 0, angle));

        //Angle Stuff
        //float angleRad = angle * Mathf.PI / rotationAngle; //Convert the angle Degrees to Raidans
        float angleRad = angle * Mathf.Deg2Rad;  //same as above comment

        float angleSinRad = (Mathf.Sin(angleRad));
        float angleCosRad = (Mathf.Cos(angleRad));

        float targetX = (grindingSpeed * angleCosRad);
        float targetY = (grindingSpeed * angleSinRad);

        float newSpeedX = Mathf.Lerp(rb2D.linearVelocity.x, targetX, Time.deltaTime * speedRate);
        float newSpeedY = Mathf.Lerp(rb2D.linearVelocity.y, targetY, Time.deltaTime * speedRate);

        rb2D.linearVelocity = new Vector2(newSpeedX, newSpeedY);

    }



    // Apply movement to Rigidbody2D based on current speed
    private void ApplyMovement()
    {
        Vector2 velocity = new Vector2(currentSpeed, rb2D.linearVelocity.y);
        rb2D.linearVelocity = velocity;
    }

    // Method to flip the player back to its normal orientation
    private void FlipToUpright()
    {
        // Move the object upwards
        transform.position += Vector3.up * moveUpDistance;

        // Set the rotation of the object to be upright (0 degrees on the Z-axis)
        // In 2D, we only care about Z-axis rotation, so we reset that while keeping the current direction.
        Quaternion targetRotation = Quaternion.Euler(0, 0, 0); // Upright rotation (facing up)
        StartCoroutine(RotateToUpright(targetRotation));
    }

    // Coroutine to rotate the object smoothly back to its upright position
    private IEnumerator RotateToUpright(Quaternion targetRotation)
    {
        isUpsideDown = true;
        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, flipUpwardSpeed * Time.deltaTime);
            yield return null; // Wait for the next frame
        }

        // Snap to the exact target rotation when close enough
        transform.rotation = targetRotation;
        isUpsideDown = false;
    }



    private void Boost()
    {
        // Speed up or slow down based on input
        if (horizontalInput > 0) // Right arrow key to speed up
        {
            currentSpeed += acceleration * Time.deltaTime;
            Debug.Log("Speed up " + currentSpeed);
        }
        else if (horizontalInput < 0) // Left arrow key to slow down
        {
            Debug.Log("Slow down " + currentSpeed);
            currentSpeed -= deceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed -= deceleration * Time.deltaTime;
        }

        // Clamp the speed between minSpeed and maxSpeed
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);

        // Check if player is at max speed
        if (currentSpeed == maxSpeed)
        {
            timeAtMaxSpeed += Time.deltaTime;

            // If the player stays at max speed for too long, trigger crash and burn
            if (timeAtMaxSpeed >= crashBurnTime)
            {
                StartCoroutine(CrashAndBurn());
            }
        }
        else
        {
            // Reset the time if the player is not at max speed
            timeAtMaxSpeed = 0f;
        }

        // Apply movement to Rigidbody
        ApplyMovement();
    }

    private void OnJumpPerformed(InputAction.CallbackContext obj)
    {
        if (isGrounded || state == PlayerState.Grind)
        {
            // Apply a vertical force to make the player jump
            Debug.Log("Jump");
            manager.onHideRailCanvas.Invoke();
            rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpForce);
            ResetThreshold();
            state = PlayerState.Air;
        }
    }

    private void ResetThreshold()
    {
        currentThreshold = UnityEngine.Random.Range(lowSafeThreshold, highSafeThreshold);
        StopCoroutine("AdjustThreshold");
        StopCoroutine("ThresholdCheckCoroutine");
    }

    // RotatePlayer method to handle flips
    IEnumerator RotatePlayer(int flipDirection)
    {
        if (isFlipping) yield break;  // Prevent overlapping flips

        isFlipping = true;
        float rotationAmount = 0f;

        if (state == PlayerState.Air || state == PlayerState.Ground)
        {
            while (rotationAmount < 360f)
            {
                Debug.Log("rotation amount");
                // Rotate player by applying incremental rotation each frame
                float rotationStep = flipSpeed * Time.deltaTime;
                rb2D.MoveRotation(rb2D.rotation + (rotationStep * flipDirection));
                rotationAmount += rotationStep;

                yield return null;
            }

            isFlipping = false;
        }
    }


    private void PlayAnimationForTrick(string trickIdentifier)
    {
        // Find the TrickAction object that matches the trickIdentifier
        TrickAction trickAction = trickActions.Find(ta => ta.identifier == trickIdentifier);

        if (trickAction != null && trickAction.animationClip != null)
        {
            // Play the animation associated with the trick
            animationComponent.Play(trickAction.animationClip.name);
        }
        else
        {
            Debug.LogWarning("Animation for trick " + trickIdentifier + " not found.");
        }
    }

    private IEnumerator ResetTrickFlagsAfterDelay()
    {
        yield return new WaitForSeconds(4f); // Adjust the delay as needed
        ResetTrickFlags();
    }

    private void ResetTrickFlags()
    {
        didFinisher = false;
        failedFinisher = false;
        //UIManager.Instance.failedComboUI = false;
    }

    // Coroutine to handle crash and burn debuff
    private IEnumerator CrashAndBurn()
    {
        isCrashed = true;

        // Slow the player down to min speed
        currentSpeed = minSpeed;
        ApplyMovement();

        Debug.Log("Crash and Burn! Player is debuffed.");

        // Wait for the debuff duration to complete
        yield return new WaitForSeconds(debuffDuration);

        // Reset the crash state and allow normal speed control again
        isCrashed = false;
        Debug.Log("Debuff over, player can now speed up again.");
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // For CheckSphere
        //Gizmos.DrawWireSphere(bottom.transform.position + Vector3.down * 2f, 2f);
        // Set Gizmo color for the ray

        // Define the ray's direction (downwards)
        Vector2 rayDirection = -Vector2.down;
        Vector3 start = bottom.transform.position;
        Vector3 end = start + (Vector3)rayDirection * raycastDistance;

        // Draw the ray in the Scene view
        Gizmos.DrawLine(start, end);
        

        // Perform the raycast
        RaycastHit2D hit = Physics2D.Raycast(bottom.transform.position, rayDirection, raycastDistance, groundLayerMask);

        // If the ray hits something, draw a sphere at the hit point
        if (hit.collider != null)
        {
            Gizmos.color = Color.red; // Change color for the hit point
            Gizmos.DrawSphere(hit.point, 0.1f); // Draw a small sphere at the hit point
        }


    }

    private void CheckIfWeLanded()
    {
        Vector3 sphereCenter = bottom.transform.position + Vector3.down * 2f;
        float sphereRadius = 2f;

        if ((!hasLanded && Physics.CheckSphere(sphereCenter, sphereRadius, groundLayerMask)))
        {
            if (hit.distance < landingHeight)
            {
                this.gameObject.GetComponent<Rigidbody>().freezeRotation = false;

                if (currentPointRate > 2)
                {
                    CheckIfWereDoingTricks();
                    if (ComboFinisher() && !didFinisher)
                    {
                        currentPointRate = 0;
                        animationComponent.Play(FinisherAnimation.name);
                        didFinisher = true;
                        failedFinisher = true;
                        trickSparkle.SetActive(false);
                        Debug.Log("Failed Finisher");
                    }
                }
            }
        }

        if (hasLanded)
        {
            if (pressingTrickButton)
            {
                totalpointsAccumilated = 0;
                currentBoostMode = 0f;
            }
        }
    }

    private void CheckIfWereDoingTricks()
    {
        bool pressingAnyTrickButton = false;

        foreach (var trickAction in trickActions)
        {
            if (trickAction.action.IsPressed())
            {
                currentPointRate = 0;
                Debug.Log("Failed Finisher");
                //UIManager.Instance.failedComboUI = true;
                pressingAnyTrickButton = true;
                break;
            }
        }

        pressingTrickButton = pressingAnyTrickButton;
    }

    private void PerfromTricks(string trickIdentifier)
    {
        if (Time.time - lastTrickTime > trickDebounceTime)
        {
            Debug.Log("Performing trick: " + trickIdentifier);
            lastTrickTime = Time.time;

            // Additional logic for performing the trick...
            if ((state == PlayerState.Air) && !didFinisher)
            {
                //this.gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("You are performing a trick: " + trickIdentifier);
                totalNumOfTricks++;
                ComboPlaceholder.Instance.JumpedFromRamp = true;
                ComboPlaceholder.Instance.OnLinkCollected();

                if (trickIdentifier == lastTrickIdentifier) // if the same trick was performed again
                {
                    currentPointRate -= decrementPointRate; // decrease the increment rate
                    if (currentPointRate < 0) currentPointRate = 0f; // ensure it doesn't become negative
                }
                else // if a new trick was performed
                {
                    currentPointRate += increasePointRate; // reset the increment rate
                    currentBoostMode += 0.2f;
                    lastTrickIdentifier = trickIdentifier; // set the last trick to the new trick
                }
                // increase the point gauge and boost gauge by the current increment rate
                totalpointsAccumilated += totalNumOfTricks * Convert.ToInt32(currentPointRate);

                // ensure the boost gauge doesn't exceed the maximum
                if (currentBoostMode >= 3) currentBoostMode = 3.0f;

                // Play the animation for the trick
                PlayAnimationForTrick(trickIdentifier);


            }
        }


    }

    private bool rightFinisherPressedLastFrame = false;
    bool ComboFinisher()
    {
        // Check if the LeftFinisher is currently being pressed
        bool isLeftFinisherPressed = LeftFinisher.IsPressed();

        // Check if the RightFinisher was just pressed
        bool isRightFinisherJustPressed = RightFinisher.IsPressed() && !rightFinisherPressedLastFrame;

        // Update the state for the next frame
        rightFinisherPressedLastFrame = RightFinisher.IsPressed();

        return isLeftFinisherPressed && isRightFinisherJustPressed;
    }

    private void PerfromFinisherTrick()
    {
        if (currentPointRate > 0.9)
        {
            Debug.Log("Perfect finisher");
            UIManager.Instance.onShowFinisherUI.Invoke();
            if (ComboFinisher() && !didFinisher)
            {

                animationComponent.Play(FinisherAnimation.name);
                didFinisher = true;
                UIManager.Instance.onHideTrickUI.Invoke();
                UIManager.Instance.onHideFinisherUI.Invoke();
                failedFinisher = false;
                UIManager.Instance.noAttemptFinisher = false;
            }
        }
    }

    public float getMaxSpeed()
    {
        return maxSpeed;
    }

    public float getCurrentSpeed()
    {
        return currentSpeed;
    }

    public float getCurrentThreshold()
    {
        return currentThreshold;
    }

    public float getMinThreshold()
    {
        return lowSafeThreshold;
    }    

    public float getMaxThreshold()
    {
        return highSafeThreshold;
    }

}



[System.Serializable]
public class TrickAction
{
    public string identifier;
    public InputAction action;
    public AnimationClip animationClip;
}
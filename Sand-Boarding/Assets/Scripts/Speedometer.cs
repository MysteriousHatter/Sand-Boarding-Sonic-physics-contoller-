using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{

    public Rigidbody2D Bert;
    private float maxSpeed = 0.0f;
    public float minSpeedPointerAngle;
    public float maxSpeedPointerAngle;
    //object we need to rotate other then the pointer itself
    public RectTransform pointerHolder;
    //Text Varaible
    public TMPro.TMP_Text speedLabel;

    private bool isFlashing = false;  // To track if flashing is ongoing
    [SerializeField] private float flashDuration = 0.5f;  // Duration between flashes


    private void Start()
    {
        maxSpeed = Bert.GetComponent<PlayerController>().getMaxSpeed();
    }
    void Update()
    {
        //Get's speed of the car and multiplies it by 3.6 to convert it to kilometers an hour
        float speed = Bert.GetComponent<PlayerController>().getCurrentSpeed();
        Debug.Log("The speed from car " + speed);

        //Converts speed into an interger; Add a quotation to convert it again to a string since our variable can only hold text
        //Alings the text to the center
        speedLabel.text = (int)speed + "";
        speedLabel.alignment = TMPro.TextAlignmentOptions.Center;

        Debug.Log("The speed calcualted " + speed / maxSpeed);
        //Where the rotation happens. We use lerp for the smooth transitioning
        pointerHolder.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(minSpeedPointerAngle, maxSpeedPointerAngle, speed / maxSpeed));

        // Check if the player is at max speed and start flashing the needle
        if (speed >= maxSpeed && !isFlashing)
        {
            StartCoroutine(FlashNeedle());
        }
    }

    IEnumerator FlashNeedle()
    {
        isFlashing = true;
        Image needleImage = pointerHolder.GetComponentInChildren<Image>();  // Assuming the pointer has an Image component

        while (Bert.GetComponent<PlayerController>().getCurrentSpeed() >= maxSpeed)
        {
            // Toggle the needle's visibility on and off
            needleImage.enabled = !needleImage.enabled;

            // Wait for the specified flash duration
            yield return new WaitForSeconds(flashDuration);
        }

        // Ensure the needle is visible after flashing stops
        needleImage.enabled = true;

        // Stop flashing once the speed is below max speed
        isFlashing = false;
    }
}

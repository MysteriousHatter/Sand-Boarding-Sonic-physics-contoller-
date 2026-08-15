using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BalaneGauge : MonoBehaviour
{
    public Rigidbody2D Bert;
    private float currentBalanceValue;  // The current balance value
    private float minSafeThreshold;     // The low threshold
    private float maxSafeThreshold;     // The high threshold
    [SerializeField] private float minPointerAngle;      // Angle when at min balance
    [SerializeField] private float maxPointerAngle;      // Angle when at max balance
    [SerializeField] private float pointerSmoothingSpeed = 5f;  // Speed of the smoothing transition
    public RectTransform pointerHolder; // Object we need to rotate other than the pointer itself

    private bool isFlashing = false;  // To track if flashing is ongoing
    [SerializeField] private float flashDuration = 0.5f;  // Duration between flashes

    private void Start()
    {
        // Assuming there's a method to get the initial balance value
        currentBalanceValue = Bert.GetComponent<PlayerController>().getCurrentThreshold();
        minSafeThreshold = Bert.GetComponent<PlayerController>().getMinThreshold();
        maxSafeThreshold = Bert.GetComponent<PlayerController>().getMaxThreshold();
    }

    void Update()
    {
        // Get the current balance value
        currentBalanceValue = Bert.GetComponent<PlayerController>().getCurrentThreshold();
        Debug.Log("The current balance value: " + currentBalanceValue);

        // Map the balance value to the target rotation of the pointer
        float normalizedBalance = Mathf.InverseLerp(minSafeThreshold, maxSafeThreshold, currentBalanceValue);
        float targetAngle = Mathf.Lerp(minPointerAngle, maxPointerAngle, normalizedBalance);

        // Get the current rotation of the pointer
        float currentAngle = pointerHolder.localEulerAngles.z;

        // Smoothly rotate the pointer towards the target angle
        float smoothedAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * pointerSmoothingSpeed);

        // Apply the smoothed rotation to the pointer
        pointerHolder.localEulerAngles = new Vector3(0, 0, smoothedAngle);

        // Check if the balance is outside the safe thresholds and start flashing the needle
        if ((currentBalanceValue < minSafeThreshold || currentBalanceValue > maxSafeThreshold) && !isFlashing)
        {
            StartCoroutine(FlashNeedle());
        }
    }

    IEnumerator FlashNeedle()
    {
        isFlashing = true;
        Image needleImage = pointerHolder.GetComponentInChildren<Image>();  // Assuming the pointer has an Image component

        // Flash while balance is outside the safe thresholds
        while (currentBalanceValue < minSafeThreshold || currentBalanceValue > maxSafeThreshold)
        {
            // Toggle the needle's visibility on and off
            //needleImage.enabled = !needleImage.enabled;

            // Wait for the specified flash duration
            yield return new WaitForSeconds(flashDuration);

            // Update balance value
            currentBalanceValue = Bert.GetComponent<PlayerController>().getCurrentThreshold();
        }

        // Ensure the needle is visible after flashing stops
        needleImage.enabled = true;

        // Stop flashing once balance is back within safe thresholds
        isFlashing = false;
    }


}

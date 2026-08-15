using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    [SerializeField] public float timeRemaining = 5f;
    [SerializeField] private bool timeIsRunning = false;
    public float miniTimer { get; set; }

    [field: SerializeField] TMP_Text timeText => GetComponent<TMP_Text>();

    // Start is called before the first frame update
    void Start()
    {
        //Starts the timer automatically
        timeIsRunning = true;

    }

    // Update is called once per frame
    void Update()
    {

        if (timeIsRunning && this.gameObject.name == "Timer")
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
                DisplayTime(timeRemaining);
            }
            else
            {
                Debug.Log("Time has run out!");
                timeRemaining = 0;
                timeIsRunning = false;
                GameManager.instance.ResetScene();
            }

        }
    }

    void DisplayTime(float timeToDisplay)
    {
        timeToDisplay += 1;
        float minutes = Mathf.FloorToInt(timeToDisplay / 60);
        float seconds = Mathf.FloorToInt(timeToDisplay % 60);
        float milliseconds = (timeToDisplay % 1) * 1000;


        //00 is used as a placeholder for formatting option
        //0 in the first string represents minutes, while 1 in the second half represents seconds, and 2 represents miliseconds
        timeText.text = string.Format("{0:00}:{1:00}:{2:00}", minutes, seconds, milliseconds);
    }



}

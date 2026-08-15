using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// This will be a placeholder for the combo system until we place it in the Game/Score Manager 
/// </summary>
public class ComboPlaceholder : MonoBehaviour
{
    public int ComboCount { get; set; }
    [SerializeField] private float comboTimerPlaceholder = 3f;
    public float ComboTimer { get; set; }
    private static ComboPlaceholder instance;
    public int totalComboCount { get; private set; }
    private PlayerController trick;
    private float lastShotTime = 0f;
    private bool comboResultOn = false;
    public bool JumpedFromRamp { get; set; }

    public static ComboPlaceholder Instance
    {
        get
        {
            instance = GameObject.FindObjectOfType<ComboPlaceholder>();
            if (instance == null)
            {

                GameObject a = new GameObject("a");
                a.AddComponent<ComboPlaceholder>();
                instance = a.GetComponent<ComboPlaceholder>();

            }
            return instance;
        }
    }

    // Start is called before the first frame update

    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        trick = FindObjectOfType<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        lastShotTime = Time.time;
        TurnOffResult();
        if (ComboCount > 0)
        {
            LinkTimeLeft();
        }
    }

    private void LinkTimeLeft()
    {
        ComboTimer -= Time.deltaTime;
        if (ComboTimer <= 0 || trick.didFinisher)
        {
            //1. Turn Of UI for link Counter
            UIManager.Instance.showComboNumber(false);
            //2. Mutiply total linkCountScore by ten
            int totalLinkCOuntScore = ComboCount;
            //3. Update to scoreManager - Multiply the combo count by a thousand
            totalComboCount = totalLinkCOuntScore * 1000;
            //3.1 show score - 
            UIManager.Instance.ShowComboScore();
            ScoreManager.Instance.UpdateScore(totalComboCount);
            //4. Return LinkCount to zero
            ComboCount = 0;
            totalComboCount = 0;
            comboResultOn = false;
        }
        else if (UIManager.Instance.failedComboUI)
        {
            UIManager.Instance.showComboNumber(false);
            int failedComboCOuntScore = 0;
            totalComboCount = failedComboCOuntScore;
            UIManager.Instance.ShowComboScore();
            //Add to score
            ComboCount = 0;
            ScoreManager.Instance.UpdateScore(ComboCount);
            //UIManager.Instance.failedComboUI = false;
        }
    }

    public void OnLinkCollected(int linkCollected = 1)
    {
        ComboCount += linkCollected;
        if (UIManager.Instance.finisherUIIsOn) { comboResultOn = true; }
        if (ComboCount > 0)
        {
            ComboTimer = comboTimerPlaceholder;
        }
        if (ComboCount >= 2 || JumpedFromRamp)
        {
            //Show UI
            Debug.Log("Show combo meter");
            //TurnOffResult();
            UIManager.Instance.showComboNumber(true);
            UIManager.Instance.OnComboPerformed();

        }

    }

    bool CheckForExsistingComboResult()
    {
        float comboTimeWindow = 1.4f;
        if (Time.time - lastShotTime < comboTimeWindow)
        {

            // The hit is within the time window, so it's part of the existing combo
            return true;
        }
        else
        {
            // Too much time has passed since the last hit, so this is a new combo
            return false;
        }
    }


    //Create another method that checks if result is on if so, we can turn it off
    void TurnOffResult()
    {
        if (comboResultOn)
        {
            UIManager.Instance.TurnOffFinisher();
        }
    }
}

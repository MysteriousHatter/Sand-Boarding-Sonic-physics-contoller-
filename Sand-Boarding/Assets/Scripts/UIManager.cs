using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Reflection;
using UnityEngine.U2D;

public class UIManager : MonoBehaviour
{

    [SerializeField] TextMeshProUGUI scoreText;
    ScoreManager scoreManager;

    public UnityEvent onShowTrickUI;
    public UnityEvent onHideTrickUI;
    public UnityEvent onShowFinisherUI;
    public UnityEvent onHideFinisherUI;
    public UnityEvent onShowRailCanvas;
    public UnityEvent onHideRailCanvas;



    [Tooltip("The temp text that pops up when the player destorys an enemy/obstacle")]
    [SerializeField] private GameObject comboNumText;
    [SerializeField] private GameObject comboText;
    [SerializeField] private GameObject comboScore;
    [SerializeField] private GameObject comboFinisherText;
    [SerializeField] private float normalSize = 36f; // Size when combo is performed
    [SerializeField] private float minimumSize = 1f; // Minimum size of text
    [SerializeField] private float elapsed = 1f;
    [SerializeField] private float textGrowTime = 12f;

    private bool isShrinking = false;
    private bool isGrowing = false;
    public bool failedComboUI { get; set; }
    public bool noAttemptFinisher { get; set; }

    public bool finisherUIIsOn { get; set; }

    private PlayerControls playerControls;
    private InputAction pauseAction;

    [SerializeField] private RectTransform pointerUI;


    public static UIManager Instance
    {
        get
        {
            instance = GameObject.FindObjectOfType<UIManager>();
            if (instance == null)
            {

                GameObject a = new GameObject("UIBehaviorManager");
                a.AddComponent<UIManager>();
                instance = a.GetComponent<UIManager>();

            }
            return instance;
        }
    }

    static UIManager instance;
    private void Awake()
    {
        //We'll uncommit after implmeneting Car in game scene
        scoreManager = FindObjectOfType<ScoreManager>();
        playerControls = new PlayerControls();
        finisherUIIsOn = false;
        onHideFinisherUI.Invoke();
        onHideTrickUI.Invoke();
        onHideRailCanvas.Invoke();
    }

    public void TurnOffFinisher()
    {
        comboFinisherText.SetActive(false);
        comboScore.SetActive(false);
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }
    // Start is called before the first frame update
    void Start()
    {
        if (comboNumText != null) { comboNumText.GetComponent<TMP_Text>().fontSize = normalSize; }
        if (comboText != null) { comboText.GetComponent<TMP_Text>().fontSize = normalSize; }
        pauseAction = playerControls.FindAction("Pause");
        if (pauseAction == null)
        {
            Debug.LogError("Pause action not found in playerControls");
            // Handle the null case here
        }
       // if (rankImage != null) { rankImage.GetComponent<Image>().sprite = scoreManager.getRankSprite(); }
    }

    // Update is called once per frame
    void Update()
    {
        //We'll uncommit after implmeneting Car in game scene
        if (scoreText != null) { scoreText.text = scoreManager.score.ToString("000000000"); }
        //PauseUnpause();

        //if (Input.GetButtonDown("Submit"))
        //{
        //    PauseUnpause();
        //}
    }

    public void showComboNumber(bool show)
    {
        if (show)
        {
            comboNumText.SetActive(true);
            comboText.SetActive(true);
            comboNumText.GetComponent<TMP_Text>().text = "x" + ComboPlaceholder.Instance.ComboCount.ToString();
        }
        else
        {
            comboNumText.SetActive(false);
            comboText.SetActive(false);
        }
    }

    IEnumerator ShrinkText()
    {
        isShrinking = false;


        while (ComboPlaceholder.Instance.ComboTimer > 0)
        {
            // Calculate the desired font size based on the elapsed time
            float t = elapsed / ComboPlaceholder.Instance.ComboTimer;
            comboText.GetComponent<TMP_Text>().fontSize = Mathf.Lerp(normalSize, minimumSize, t);
            comboNumText.GetComponent<TMP_Text>().fontSize = Mathf.Lerp(normalSize, minimumSize, t);

            yield return null;
        }

        comboNumText.GetComponent<TMP_Text>().fontSize = minimumSize;
        comboText.GetComponent<TMP_Text>().fontSize = minimumSize;
        isShrinking = false;
    }

    // Call this method when a combo is performed
    public void OnComboPerformed()
    {
        if (isShrinking)
        {
            StopCoroutine(ShrinkText());
            isShrinking = false;
        }

        comboNumText.GetComponent<TMP_Text>().fontSize = normalSize;
        comboText.GetComponent<TMP_Text>().fontSize = normalSize;
        StartCoroutine(ShrinkText());
    }

    IEnumerator GrowText()
    {
        isGrowing = true;
        if (failedComboUI)
        {
            comboFinisherText.GetComponent<TMP_Text>().text = "You have failed the combo!!";
        }
        else
        {
            comboScore.GetComponent<TMP_Text>().text = ComboPlaceholder.Instance.totalComboCount.ToString();
            if (noAttemptFinisher)
            {
                comboFinisherText.GetComponent<TMP_Text>().text = "Sweet Tricks";
            }
            else
            {
                comboFinisherText.GetComponent<TMP_Text>().text = "Perfect Combo";
                noAttemptFinisher = true;

            }
        }
        comboFinisherText.SetActive(true);
        comboScore.SetActive(true);

        float elapsed = 0;

        while (elapsed < textGrowTime)
        {
            elapsed += Time.deltaTime;

            // Calculate the desired font size based on the elapsed time
            float t = elapsed / textGrowTime;
            comboScore.GetComponent<TMP_Text>().fontSize = Mathf.Lerp(minimumSize, normalSize, t);
            comboFinisherText.GetComponent<TMP_Text>().fontSize = Mathf.Lerp(minimumSize, normalSize, t);
            yield return null;
        }

        yield return new WaitForSeconds(2.0f);
        comboScore.GetComponent<TMP_Text>().fontSize = normalSize;
        comboFinisherText.GetComponent<TMP_Text>().fontSize = normalSize;
        isGrowing = false;
        comboScore.SetActive(false);
        comboFinisherText.SetActive(false);
    }

    // Call this method when a combo is performed
    public void ShowComboScore()
    {
        if (isGrowing)
        {
            StopCoroutine(GrowText());
            isGrowing = false;
            comboScore.SetActive(false);
            finisherUIIsOn = false;
        }

        finisherUIIsOn = true;
        comboScore.GetComponent<TMP_Text>().fontSize = normalSize;
        StartCoroutine(GrowText());
    }


    // Check if the pointer is inside the safe zone based on external thresholds
    public void CheckIfPointerInSafeZone(bool WithinThreshold)
    {
        // Check if the pointer is within the low and high threshold from the BalanceManager
        if (WithinThreshold)
        {
            pointerUI.GetComponent<Image>().color = Color.green; // Optional: Change pointer color when safe
        }
        else
        {
            pointerUI.GetComponent<Image>().color = Color.red; // Optional: Change pointer color when out of bounds
        }
    }

}

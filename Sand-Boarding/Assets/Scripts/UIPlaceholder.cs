using TMPro;
using UnityEngine;

public class UIPlaceholder : MonoBehaviour
{
    [SerializeField] private PlayerStateMachine playerStateMachine;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private string speedLabel = "Speed: ";

    private void Awake()
    {
        if (speedText == null)
        {
            speedText = GetComponent<TMP_Text>();
        }

        if (playerStateMachine == null)
        {
            playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        }
    }

    private void Update()
    {
        if (playerStateMachine == null || speedText == null)
        {
            return;
        }

        speedText.text = $"{speedLabel}{Mathf.Abs(playerStateMachine.groundSpeed):0.0}";
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

public class ScoreManager : MonoBehaviour
{
    private static ScoreManager instance;

    [SerializeField] GameObject scoreText;
    public int score { get; set; }


    [Header("Rank scores")]
    [SerializeField] private int SRankScore = 1000;
    [SerializeField] private int ARankScore = 1000;
    [SerializeField] private int BRankScore = 1000;
    [SerializeField] private int CRankScore = 1000;
    [SerializeField] private int DRankScore = 1000;
    [SerializeField] private int ERankScore = 1000;

    [Header("Rank scores images")]
    [SerializeField] private Sprite SRankImage;
    [SerializeField] private Sprite ARankImage;
    [SerializeField] private Sprite BRankImage;
    [SerializeField] private Sprite CRankImage;
    [SerializeField] private Sprite DRankImage;
    [SerializeField] private Sprite ERankImage;



    public static ScoreManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("ScoreManager is Null");
            }
            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
        DontDestroyOnLoad(instance);
        // scoreText = GameObject.Find("Score");
    }

    void Start()
    {
        scoreText = GameObject.Find("Score");
        scoreText.GetComponent<TMP_Text>().text = score.ToString();
    }

    public Sprite getRankSprite()
    {
        Sprite sprite;
        switch (score)
        {
            case var scoreTemp when scoreTemp >= SRankScore:
                sprite = SRankImage;
                break;
            case var scoreTemp when scoreTemp < SRankScore && scoreTemp >= ARankScore:
                sprite = ARankImage;
                break;
            case var scoreTemp when scoreTemp < ARankScore && scoreTemp >= BRankScore:
                sprite = BRankImage;
                break;
            case var scoreTemp when scoreTemp < CRankScore && scoreTemp >= DRankScore:
                sprite = CRankImage;
                break;
            case var scoreTemp when scoreTemp < DRankScore && scoreTemp >= ERankScore:
                sprite = DRankImage;
                break;
            case var scoreTemp when scoreTemp < ERankScore:
                sprite = ERankImage;
                break;
            default:
                sprite = ERankImage;
                break;
        }

        return sprite;
    }


    public void UpdateScore(int scoreBonus)
    {
        score += scoreBonus;
        scoreText.GetComponent<TMP_Text>().text = score.ToString();
    }


}

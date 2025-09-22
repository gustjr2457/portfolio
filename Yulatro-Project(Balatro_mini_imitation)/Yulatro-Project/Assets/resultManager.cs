using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Attributes;
using DG.Tweening;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class resultManager : MonoBehaviour
{
    public HorizontalCardHolder cardPlayManager;
    public GameObject black;
    Image image;

    public RectTransform rsltImg;

    public Text rsltText;
    public Text scoreText;
    int score;

    float showImageTime = 0.5f;

    public GameObject reBtn;
    public GameObject exBtn;

    void Start()
    {
        image = black.GetComponent<Image>();
        black.gameObject.SetActive(false);
        rsltImg.gameObject.SetActive(false);
        rsltText.gameObject.SetActive(false);
        scoreText.gameObject.SetActive(false);
        reBtn.SetActive(false);
        exBtn.SetActive(false);
    }


    void Update()
    {
        
    }

    public void GameEnd()
    {
        StopAllCoroutines();
        StartCoroutine(ShowEndPage());

    }

    IEnumerator ShowEndPage()
    {
        score = cardPlayManager.nowScore;
        if (cardPlayManager.nowScore >= 600)
        {
            rsltText.text = "WIN!!";
        }
        else
        {
            rsltText.text = "LOSE...";
        }
        scoreText.text = "SCORE: " + score.ToString();

        black.gameObject.SetActive(true);
        image.DOFade(0.8f, showImageTime);
        yield return new WaitForSeconds(showImageTime);

        rsltImg.gameObject.SetActive(true);
        rsltImg.DOSizeDelta(new Vector2(10, 900), showImageTime);
        yield return new WaitForSeconds(showImageTime);

        rsltImg.DOSizeDelta(new Vector2(1500, 900), showImageTime);
        yield return new WaitForSeconds(1.0f);

        rsltText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        scoreText.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        reBtn.gameObject.SetActive(true);
        exBtn.gameObject.SetActive(true);
    }

    public void RetryBtn()
    {
        Scene thisScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(thisScene.name);
    }

    public void ExitBtn()
    {
        Debug.Log("게임 종료");

        #if UNITY_EDITOR
        EditorApplication.isPlaying = false; // 에디터 실행 중지
        #else
        Application.Quit(); // 빌드된 게임 종료
        #endif
    }
}

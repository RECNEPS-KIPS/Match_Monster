﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager instance {
        get { return _instance; }
    }
    private static UIManager _instance;
    public GameObject settingPanel;
    public GameObject aboutUsPanel;
    public GameObject audioOff;
    public AudioClip[] audios;
    public bool canAudio;
    public Animator[] anims;
    public MapController curMap;

    public GameObject mapPanel;
    public GameObject achievementPanel;

    public GameObject crossLock;
    public GameObject rainbowLock;
    public GameObject timeAddLock;
    public GameObject monsterKillerLock;
    public GameObject playTimeLock;
    public GameObject totalScoreLock;
    public GameObject luckDogLock;
    public GameObject wallBreakLock;
    
    void Awake() {
        _instance = this;
        canAudio = PlayerPrefs.GetInt("Audio",1) == 1;
        if (canAudio) {
            Camera.main.GetComponent<AudioSource>().Play();
        }
        else {
            Camera.main.GetComponent<AudioSource>().Stop();
        }
    }
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        canAudio = PlayerPrefs.GetInt("Audio",1) == 1;
    }
    //更换地图
    public void MapChange() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
        //EventSystem.current.currentSelectedGameObject.GetComponent<Animator>().SetTrigger("select");
        curMap = EventSystem.current.currentSelectedGameObject.GetComponent<MapController>();
        Debug.Log(curMap.Index);
        for (int i = 0; i < 3; i++) {
            if ( i != curMap.Index) {
                anims[i].SetTrigger("hide");
            }
            else {
                anims[i].transform.GetComponent<UnityEngine.UI.Button>().enabled = false;
                anims[i].SetTrigger("select");
            }
        }
    }

    public void MapClose() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[2], Camera.main.transform.position, 1);
        }
        for (int i = 0; i < 3; i++) {
            if ( i != curMap.Index) {
                anims[i].SetTrigger("show");
                anims[i].transform.GetComponent<UnityEngine.UI.Button>().enabled = true;
            }
            else {
                anims[i].transform.GetComponent<UnityEngine.UI.Button>().enabled = true;
                anims[i].SetTrigger("close");
            }
        }
    }
    public void QuitGame() {
        Application.Quit();
    }
    public void StartGame() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
        mapPanel.GetComponent<Animator>().SetTrigger("tomap");
        
    }

    public void Setting() {
        settingPanel.SetActive(true);
        Debug.Log(canAudio+" "+PlayerPrefs.GetInt("CanAudio"));
        if (canAudio) {
            audioOff.SetActive(false);
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
        else {
            audioOff.SetActive(true);
        }
        settingPanel.GetComponent<Animator>().SetTrigger("open");
    }

    public void SettingPanelClose() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[2], Camera.main.transform.position, 1);
        }
        settingPanel.GetComponent<Animator>().SetTrigger("close");
    }

    public void AboutUs() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
        
        settingPanel.SetActive(false);
        aboutUsPanel.GetComponent<Animator>().SetTrigger("open");
    }
    public void AboutUsClose() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[2], Camera.main.transform.position, 1);
        }
        
        aboutUsPanel.GetComponent<Animator>().SetTrigger("close");
    }

    public void AudioOnOff() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
        }
        if (canAudio) {
            audioOff.SetActive(true);
            PlayerPrefs.SetInt("Audio",0);
            Camera.main.GetComponent<AudioSource>().Stop();
        }
        else {
            audioOff.SetActive(false);
            PlayerPrefs.SetInt("Audio", 1);
            Camera.main.GetComponent<AudioSource>().Play();
        }
    }

    public void BackToMenu() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[2], Camera.main.transform.position, 1);
        }

        if (curMap!=null) {
            for (int i = 0; i < 3; i++) {
                if (i != curMap.Index) {
                    anims[i].SetTrigger("show");
                    anims[i].transform.GetComponent<UnityEngine.UI.Button>().enabled = true;
                }
                else {
                    anims[i].transform.GetComponent<UnityEngine.UI.Button>().enabled = true;
                    anims[i].SetTrigger("close");
                }
            }
        }
        
        mapPanel.GetComponent<Animator>().SetTrigger("tomenu");
    }

    public void SelectLevel() {
        PlayerPrefs.SetInt("PlayTimes", PlayerPrefs.GetInt("PlayTimes", 0) + 1);//记录游玩次数
        //Debug.Log(EventSystem.current.currentSelectedGameObject.name);
        SceneManager.LoadScene(1);
    }

    public void ClearHistoryScore() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
        }
        PlayerPrefs.DeleteAll();
        audioOff.SetActive(false);
        Camera.main.GetComponent<AudioSource>().Play();
    }

    public Text playTimeNumsText;
    public Text clearModelNumsText;
    public Text totalScoreText;
    //public Text 

    //成就界面的展示
    public void AchievementOpen() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[1], Camera.main.transform.position, 1);
        }
        //成就数据读取
        playTimeNumsText.text = PlayerPrefs.GetInt("PlayTimes", 0) + "";
        clearModelNumsText.text = PlayerPrefs.GetInt("ClearModelNums", 0) + "";
        totalScoreText.text = PlayerPrefs.GetInt("TotalScore", 0) + "";
        Debug.Log(PlayerPrefs.GetInt("TotalScore", 0));
        
        crossLock.SetActive(PlayerPrefs.GetInt("CrossNums", 0) < 10);
        rainbowLock.SetActive(PlayerPrefs.GetInt("RainbowNums", 0) < 10);
        timeAddLock.SetActive(PlayerPrefs.GetInt("TimeAddNums", 0) < 30);
        monsterKillerLock.SetActive(PlayerPrefs.GetInt("ClearModelNums", 0) <100);
        playTimeLock.SetActive(PlayerPrefs.GetInt("PlayTimes", 0) < 5);
        totalScoreLock.SetActive(PlayerPrefs.GetInt("TotalScore", 0) < 1000);
        luckDogLock.SetActive(PlayerPrefs.GetInt("LuckDog", 0) < 10);
        wallBreakLock.SetActive(PlayerPrefs.GetInt("WallNums", 0) < 100);
        //
        achievementPanel.SetActive(true);
        achievementPanel.GetComponent<Animator>().SetTrigger("open");
    }
    public void AchievementClose() {
        if (canAudio) {
            AudioSource.PlayClipAtPoint(audios[0], Camera.main.transform.position, 0.5f);
            AudioSource.PlayClipAtPoint(audios[2], Camera.main.transform.position, 1);
        }
        achievementPanel.GetComponent<Animator>().SetTrigger("close");
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{

    public Action onConnected;
    public Action onMatchCreated;

    public GameObject ConnectionUIObject;
    public TextMeshProUGUI statusText;
    public Button CreateBtn;


    private void OnEnable()
    {
        onConnected += ConnectedSuccefull;
        onMatchCreated += OnMatchCreated;
    }
 

    private void ConnectedSuccefull()
    {
        statusText.text = "Connected";
        statusText.color = Color.green;
        CreateBtn.interactable = true;
        
    }

   
    private void OnMatchCreated()
    {
        ConnectionUIObject.SetActive(false);
    }

    private void OnDisable()
    {
        onConnected -= ConnectedSuccefull;
        onMatchCreated -= OnMatchCreated;
       
    }
}

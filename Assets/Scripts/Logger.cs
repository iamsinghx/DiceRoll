using TMPro;
using UnityEngine;

/// <summary>
/// A class to show the logs on screen in the build
/// </summary>
public class Logger : MonoBehaviour
{
    public static Logger Instance { get; private set; }

    [SerializeField]
    private TextMeshProUGUI logText; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
        }
    }
    /// <summary>
    /// Show logs on screen
    /// </summary>
    /// <param name="message">message to show</param>
    public void Log(string message)
    {
        if (logText != null)
        {
            logText.text = message;
        }
        Debug.Log(message);
    }
}

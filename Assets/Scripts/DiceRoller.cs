using Nakama;
using Nakama.TinyJson;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dice Roller Class to handle the SyncResult of dice and result
/// sync of animation and result done in this class
/// </summary>
public class DiceRoller : MonoBehaviour
{
   
    public float rotationSpeed = 10f; 
    public bool isRolling = false;
    
    public Button rollButton;
    public TextMeshProUGUI scoreText;

    private Vector3 targetRotation;
    private float rollTime = 2f;
    private float elapsedTime = 0f;

    private ISocket socket;
    private IMatch match;

    private void Start()
    {
        rollButton.onClick.AddListener(OnRollButtonClick);

        socket = NakamaCon.Instance.GetSocket();
        match = NakamaCon.Instance.GetMatch();
    }

    public void SetRollButtonInteractable(bool interactable)
    {
        rollButton.interactable = interactable;
    }

    async void OnRollButtonClick()
    {
        if (!isRolling)
        {
            isRolling = true;
            rollButton.interactable = false;

            // Reset time and rotation for new roll
            elapsedTime = 0f;
            targetRotation = Vector3.zero;

            Logger.Instance.Log("Sending Roll Event "+ NakamaCon.Instance.myUserId);
            Dictionary<string, string> AliignData = new Dictionary<string, string>
            {
                {"senderId",NakamaCon.Instance.myUserId}
            };
            Debug.Log(socket);
            Debug.Log(match.Id);
            Debug.Log(OpCodes.DiceRoll.ToString());
            Debug.Log(AliignData.ToJson());
            await socket.SendMatchStateAsync(match.Id, OpCodes.DiceRoll, AliignData.ToJson());

        }
    }

    private void FixedUpdate()
    {
        if (isRolling)
        {
            RollDice();
        }
    }
    // roll the dice if event came from server 
    public void StartRoll()
    {
        isRolling = true;
        elapsedTime = 0;
        rollTime = 100; // sating the time to a large number because we never know how long it will take the server to reflect the result.
    }


    // roll the dice locally
    async private void RollDice()
    {
        if (elapsedTime < rollTime)
        {
            // Rotate the dice gradually on all axes
            transform.Rotate(Vector3.right, rotationSpeed * 2 * Time.fixedDeltaTime);
            transform.Rotate(Vector3.up, rotationSpeed * 2 * Time.fixedDeltaTime);
            transform.Rotate(Vector3.forward, rotationSpeed * 2 * Time.fixedDeltaTime);


            elapsedTime += Time.fixedDeltaTime;
        }
        else
        {
            // Stop rolling and align the dice
            isRolling = false;
            int randomNumber = Random.Range(1, 7);
            AlignDice(randomNumber, true);
        }
    }
    /// <summary>
    /// align the Dice to a specific roation based on result
    /// </summary>
    /// <param name="resultNumber">result number that should visible on dice</param>
    /// <param name="isLocal">if this dice animation is caused by local player or remote layer </param>
    public void AlignDice(int resultNumber, bool isLocal)
    {

        isRolling = false;
        elapsedTime = 0;
        rollTime = 2f;

        // Set the target rotation for the specified number
        Dictionary<int, Vector3> numberRotations = new Dictionary<int, Vector3>()
        {
            { 2, new Vector3(0, 0, 0) },
            { 4, new Vector3(0, 0, 90) },
            { 5, new Vector3(0, 0, 180) },
            { 3, new Vector3(0, 0, -90) },
            { 6, new Vector3(90, 0, 0) },
            { 1, new Vector3(-90, 0, 0) }
        };

        if (numberRotations.TryGetValue(resultNumber, out targetRotation))
        {
           
            transform.eulerAngles = targetRotation;
            if (isLocal)
            {
                SyncResult(resultNumber);
            }
        }
        else
        {
            Debug.LogError("Invalid target number: " + resultNumber);
        }
        Logger.Instance.Log("Number on top: " + resultNumber);
        scoreText.text = resultNumber.ToString();
    }

    // share the result with other player
    async void SyncResult(int randomNumber)
    {
        Dictionary<string, string> AliignData = new Dictionary<string, string>
            {
                { "result", randomNumber.ToString() },
                { "senderId", NakamaCon.Instance.myUserId }
            };
        await socket.SendMatchStateAsync(match.Id, OpCodes.DiceRollResult, AliignData.ToJson());

    }
}


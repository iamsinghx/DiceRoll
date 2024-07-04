using Nakama;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Nakama.TinyJson;
using System.Text;
using System;

public class NakamaCon : MonoBehaviour
{
    private static NakamaCon instance;
    public static NakamaCon Instance { get { return instance; } }

    [SerializeField]
    int port = 7350;
    string host = "localhost";
    string schema = "http";
    string serverKey = "defaultkey";

    private List<string> players;
    ISession session;
    ISocket socket;
    IClient client;
    IMatch match;

    [SerializeField]
    private MainUI mainUI;

    public DiceRoller dice;

    private bool isMyTurn = false;
    public string myUserId;

    private void Awake()
    {
        // Singleton 
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // make a initial connecction with server
    async void Start()
    {
        players = new List<string>();
        var mainThread = UnityMainThreadDispatcher.Instance();

        client = new Client(schema, host, port, serverKey, UnityWebRequestAdapter.Instance);

        string uniqueId = Guid.NewGuid().ToString();
        session = await client.AuthenticateCustomAsync(uniqueId);

        socket = client.NewSocket();

        await socket.ConnectAsync(session, true);

        socket.ReceivedMatchmakerMatched += m => mainThread.Enqueue(() => OnReceiveMatch(m));
        socket.ReceivedMatchPresence += m => mainThread.Enqueue(() => OnReceivedMatchPresence(m));
        socket.ReceivedMatchState += m => mainThread.Enqueue(async () => await OnReceivedMatchState(m));

        Logger.Instance.Log("Server is connected");

        mainUI.onConnected.Invoke();
    }

    private void OnReceivedMatchPresence(IMatchPresenceEvent matchPresenceEvent)
    {
        foreach (var user in matchPresenceEvent.Joins)
        {
            if (!players.Contains(user.UserId))
            {
                players.Add(user.UserId);
            }
        }

        foreach (var user in matchPresenceEvent.Leaves)
        {
            if (players.Contains(user.UserId))
            {
                players.Remove(user.UserId);
            }
        }
    }

    private IDictionary<string, string> GetStateAsDictionary(byte[] state)
    {
        if (state == null || state.Length == 0)
        {
            return null;
        }

        string jsonString = Encoding.UTF8.GetString(state);

        Dictionary<string, string> stateDictionary;
        try
        {
            stateDictionary = jsonString.FromJson<Dictionary<string, string>>();
        }
        catch (Exception ex)
        {
            return null;
        }

        return stateDictionary;
    }



    private async Task OnReceivedMatchState(IMatchState matchState)
    {
        var stateDictionary = GetStateAsDictionary(matchState.State);

        Logger.Instance.Log("Match State Received " + stateDictionary.ToJson());

        switch (matchState.OpCode)
        {
            case OpCodes.DiceRoll:
                Logger.Instance.Log("Dice is Rolling Received Test");
                if (stateDictionary.TryGetValue("senderId", out string senderId))
                {
                    if (myUserId != senderId)
                    {
                        dice.StartRoll();
                    }
                }
                break;
            case OpCodes.DiceRollResult:
                Logger.Instance.Log($"Dice Roll Result: {stateDictionary}");
                if (stateDictionary.TryGetValue("result", out string result))
                {
                    int diceResult = int.Parse(result);
                    if (stateDictionary.TryGetValue("senderId", out string resultSenderId))
                    {
                        if (myUserId != resultSenderId)
                        {
                            dice.AlignDice(diceResult, false);
                            dice.SetRollButtonInteractable(true);
                        }
                    }
                }
                break;
            case OpCodes.TurnChange:
                Logger.Instance.Log("Turn Change Received " + stateDictionary.ToJson());
                if (stateDictionary.TryGetValue("turn", out string turn))
                {
                    isMyTurn = turn == myUserId;
                    dice.SetRollButtonInteractable(isMyTurn);
                }
                break;
            default:
                break;
        }
    }

    async void OnReceiveMatch(IMatchmakerMatched matched)
    {
        myUserId = matched.Self.Presence.UserId;

        mainUI.onMatchCreated.Invoke();
        match = await socket.JoinMatchAsync(matched);

        Logger.Instance.Log("Match Received " + myUserId);

        dice.gameObject.SetActive(true);

        foreach (var user in match.Presences)
        {
            players.Add(user.UserId);
            Logger.Instance.Log(user.UserId.ToString());
        }

        var initialTurnData = new Dictionary<string, string>
        {
            { "turn", players[0] }
        };
        await socket.SendMatchStateAsync(match.Id, OpCodes.TurnChange, initialTurnData.ToJson());

        isMyTurn = players[0] == myUserId;
        dice.SetRollButtonInteractable(isMyTurn);
    }

    // Create Match Btn Click Event
    public async void CreateMatch()
    {
        Logger.Instance.Log("Creating Match");
        await socket.AddMatchmakerAsync("*", 2, 2);

    }

    public ISocket GetSocket()
    {
        return socket;
    }

    public IMatch GetMatch()
    {
        return match;
    }
}

public class OpCodes
{
    public const long DiceRoll = 1;
    public const long DiceRollResult = 2;
    public const long TurnChange = 3;
}

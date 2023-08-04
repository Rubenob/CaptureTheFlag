using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    public static Dictionary<string, Player> Players = new Dictionary<string, Player>();

    public static GameManager Instance;

    public static GameObject[] SpawnPoints;
    public RuntimeAnimatorController[] Skins;
    public static bool viewScore;

    public int maxPlayers = 3;
    public int minPlayers = 2;
    public int defaultCountdown = 10;

    [Space]
    public NetworkVariable<Status> status = new NetworkVariable<Status>(Status.Lobby);
    public NetworkVariable<int> countdown;
    public static NetworkVariable<bool> Victory;

    public HashSet<Player> playerList = new HashSet<Player>();



    public void Awake()
    {
        //Se buscan todos los GameObjects SpawnPoints y se llena la lista 
        SpawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoints");
        viewScore = false;
        Instance = this;
        countdown = new NetworkVariable<int>(defaultCountdown);
        Victory = new NetworkVariable<bool>(false);
    }

    private void OnEnable()
    {
        Victory.OnValueChanged += OnPlayerWin;
    }

    private void OnDisable()
    {
        Victory.OnValueChanged -= OnPlayerWin;
    }

    //Metodo que agrega al delegado "ConnectionApprovalCallback" el metodo ApprovalCheck
    public void EnableApprovalCallback()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }
    
    //Metodo que registra a un nuevo jugador al diccionario
    public static void RegisterPlayer(string _netID, Player _player)
    {
        string _playerID = "Player " + _netID;
        Players.Add(_playerID, _player);
        _player.transform.name = _playerID;
    }

    //Metodo que elimina a un jugador del diccionario
    public static void UnRegisterPlayer(string _playerID)
    {
        Players.Remove(_playerID);
    }

    public static Player GetPlayer(string _playerID)
    {
        return Players[_playerID];
    }

    //Metodo que coge un jugador aleatorio
    public static string TakeRandom()
    {
        Dictionary<string, Player> players = GetPlayers();
        List <string> keyList = new List<string>(players.Keys);
        string randomKey = keyList[Random.Range(0, keyList.Count - 1)];
        return randomKey;
    }

    public static Dictionary<string, Player> GetPlayers()
    {
        return Players;
    }


    public static void SetPlayerNames()
    {
        Dictionary<string, Player> players = GetPlayers();
        foreach(string _playerID in players.Keys)
        {
            players[_playerID].playerName.text = players[_playerID].Name.Value.ToString();
            players[_playerID].playerName.color = players[_playerID].Color.Value;
        }
    }

    public static Vector3 GetSpawnPoint()
    {
        int aux = Random.Range(0, SpawnPoints.Length - 1);
        Vector3 pos = SpawnPoints[aux].transform.position;
        return pos;
    }

    // Alterna el valor del canvas del score entre true y false
    public static void ChangeScoreView()
    {
        if(viewScore == true)
        {
            viewScore = false;
        }
        else
        {
            viewScore = true;
        }
    }

    //Metodo que llama al metodo FinishGame de todos los player
    void OnPlayerWin(bool previous, bool current)
    {
        Dictionary<string, Player> players = GetPlayers();
        foreach (string _playerID in players.Keys)
        {
            players[_playerID].FinishGame();
        }
    }

    public void AddPlayer(Player player)
    {
        playerList.Add(player);
        //Frezeamos el jugador para que no pueda moverse hasta que comience la partida
        player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        if (IsServer)
        {
            CheckStartingConditions();
        }
    }

    public void RemovePlayer(Player player)
    {
        playerList.Remove(player);

        if (IsServer)
        {
            //Comprueba que si se desconecta un jugador queden al menos el numero minimo de jugadores para poder comenzar la partida
            if (status.Value == Status.Starting && playerList.Count < minPlayers)
            {
                status.Value = Status.Lobby;
                countdown.Value = defaultCountdown;
            }
            //Si esta jugadno y se desconceta llama a FinishedCondition()
            else if (status.Value == Status.Playing)
            {
                FinishedCondition();
            }
        }
    }


    public void FinishedCondition()
    {
        //Guarda el numero de jugadores que estan vivos
        var count = playerList.Count(player => !player.Dead.Value);

        //Si quedan solo 1 o 0 vivos se acaba la partida
        if(count <= 1)
        {
            status.Value = Status.Finished;
        }
    }


    //Metodo que se ejecuta cuando comienza el juego
    private IEnumerator StartingCoroutine()
    {
        while(countdown.Value > 0)
        {
            //Se llama a un metodo que actualiza la cuenta atras en cada cliente
            ShowCountDownClientRpc(countdown.Value.ToString());

            yield return new WaitForSeconds(1);

            if(status.Value != Status.Starting)
            {
                yield break;
            }

            countdown.Value--; 
        }

        //Empieza el juego
        status.Value = Status.Playing;
        ShowCountDownClientRpc("");

        //Una vez comienza la partida se desbloquean los jugadores y se hace que caigan 
        if (IsServer)
        {
            foreach (var player in playerList)
            {
                player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.None;
                player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                player.GetComponent<Rigidbody2D>().gravityScale = 1.501f;
            }
        }
    }

    //Metodo para actualizar la cuenta atras en cada cliente
    [ClientRpc]
    void ShowCountDownClientRpc(string text)
    {
        foreach (var player in playerList)
        {
            player.countDown.text = text;
        }
        
    }

    //Metodo que comprueba que pueda empezar la partida
    private void CheckStartingConditions()
    {
        if (status.Value == Status.Lobby && playerList.Count == minPlayers)
        {
            status.Value = Status.Starting;
            StartCoroutine(StartingCoroutine());
        }
    }

    //Metodo que comprueba si acepta o no una nueva conexion
    public void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
    {
        var approved = playerList.Count < maxPlayers && (status.Value == Status.Lobby || status.Value == Status.Starting);

        print(playerList.Count);
        print(status.Value);
        print(approved);

        // Vector 3 al ser una estructura no tiene valor nulo y con ? se le a�ade
        Vector3? position = null;
        if (SpawnPoints.Length > 0)
        {
            position = GetSpawnPoint();
        }

        callback(true, null, approved, position, null);
        
    }
    

    public enum Status
    {
        Lobby,
        Starting,
        Playing,
        Finished
    }

}

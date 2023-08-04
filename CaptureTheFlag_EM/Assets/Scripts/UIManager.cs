using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Linq;

public class UIManager : MonoBehaviour
{
    #region Variables

    public static UIManager Instance;

    [SerializeField] NetworkManager networkManager; // Cambio de Julio
    UnityTransport transport;

    readonly ushort port = 7777; // Cambio de Julio

    [SerializeField] Sprite[] hearts = new Sprite[3];

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;

    [Header("In-Game HUD")]
    [SerializeField] public GameObject inGameHUD;
    [SerializeField] RawImage[] heartsUI = new RawImage[3];
    [SerializeField] Text playerText;
    [SerializeField] public Text countDownSeconds;

    // Canvas de seleccion de nombre y skin
    [Header("Player selector")]
    [SerializeField] private GameObject nameSelector;
    [SerializeField] public InputField namePlayer;

    private Button buttonSkinAmarilla;
    private Button buttonSkinVerde;
    private Button buttonSkinAzul;
    private Button buttonSkinRosa;
    private Button buttonSkinCarne;

    [SerializeField] private Button[] skinColor = new Button[5];

    [SerializeField] private Button buttonForward;
    [SerializeField] private Button buttonBackward;
    [SerializeField] private Button buttonPlay;

    //Canvas de victoria y derrota
    [Header("Finish")]
    [SerializeField] private GameObject youDied;
    [SerializeField] private GameObject youWin;

    public bool hostMode = false;
    public int skin = 0;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        transport = (UnityTransport)networkManager.NetworkConfig.NetworkTransport; // Cambio de Julio
        Instance = this;
    }

    private void Start()
    {
        //Funcionalidades de los botones  
        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        buttonPlay.onClick.AddListener(() => EnterGame());

        //Funcionalidad de los botones de cambiar skin en el selector de skins
        buttonForward.onClick.AddListener(() => SkinForward());
        buttonBackward.onClick.AddListener(() => SkinBackward());

        ActivateMainMenu();
    }

    #endregion

    #region UI Related Methods

    //Metodo para pasar uns skin adelante
    private void SkinForward()
    {
        if (skin < 4)
        {
            skinColor[skin].gameObject.SetActive(false);
            skin++;
            skinColor[skin].gameObject.SetActive(true);
        }
    }

    //Metodo para pasar uns skin atras
    private void SkinBackward()
    {
        if (skin > 0)
        {
            skinColor[skin].gameObject.SetActive(false);
            skin--;
            skinColor[skin].gameObject.SetActive(true);
        }
    }
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        nameSelector.SetActive(false); 
        youDied.SetActive(false);
        youWin.SetActive(false);
    }

    public void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(true);
        nameSelector.SetActive(false);
        youDied.SetActive(false);
        youWin.SetActive(false);


        UpdateLifeUI(0);

    }

    //Activa el canvas de seleccion de skin y nombre
    private void ActivatePlayerSelector()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        nameSelector.SetActive(true);
        youDied.SetActive(false);
        youWin.SetActive(false);

    }

    //Activa el canvas de seleccion de morir
    public void ActivateDeath()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        nameSelector.SetActive(false);
        youDied.SetActive(true);
        youWin.SetActive(false);

    }

    //Metodo que actualiza los corazones
    public void UpdateLifeUI(int hitpoints)
    {
        //Actuliza la vida
        switch (hitpoints)
        {
            case 6:
                heartsUI[0].texture = hearts[2].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 5:
                heartsUI[0].texture = hearts[1].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 4:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[2].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 3:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[1].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 2:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[2].texture;
                break;
            case 1:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[1].texture;
                break;
            case 0:
                heartsUI[0].texture = hearts[0].texture;
                heartsUI[1].texture = hearts[0].texture;
                heartsUI[2].texture = hearts[0].texture;
                break;
        }
    }

    #endregion

    #region Netcode Related Methods

    private void StartHost()
    {
        hostMode = true;
        ActivatePlayerSelector();
    }

    private void StartClient()
    {
        hostMode = false;
        ActivatePlayerSelector(); 
    }

    private void StartServer()
    {
        //Comienza el servidor y activa el menu in game
        //GameManager.Instance.EnableApprovalCallback();
        var ip = inputFieldIP.text;
        if (!string.IsNullOrEmpty(ip))
        {
            transport.SetConnectionData(ip, port);
        }
        GameManager.Instance.EnableApprovalCallback();
        NetworkManager.Singleton.StartServer();
        ActivateInGameHUD();
        
    }

    public void EnterGame()
    {
        if (hostMode)
        {
            //Comienza el servidor y activa el menu in game
            GameManager.Instance.EnableApprovalCallback(); // Metodo que añade otro metodo al delegado que se ejecuta cada vez que se conecta alguien
            NetworkManager.Singleton.StartHost();
            ActivateInGameHUD();
            
        }
        else
        {
            //Comienza el cliente con la ip seleccionada y activa el menu in game
            var ip = inputFieldIP.text;
            if (!string.IsNullOrEmpty(ip))
            {
                transport.SetConnectionData(ip, port);
            }
            if (NetworkManager.Singleton.StartClient())
            {
                ActivateInGameHUD();
            }
        }
    }

    #endregion

    private void OnGUI()
    {
        //Comprueba que el HUD este activado, es decir, que estes jugando
        if (inGameHUD.activeSelf)
        {
            //Se llama a renombrar a los jugadores desde la GUI
            GameManager.SetPlayerNames();
            Dictionary<string, Player> players = GameManager.GetPlayers();

            //Comprueba que se haya pulsado el "Tab" o que la partida haya acabado con al menos un jugador
            if (GameManager.viewScore == true || (players.Count > 0 && players[GameManager.TakeRandom()].Finish.Value))
            {                
                playerText.gameObject.SetActive(true);
                string text = null;

                //Si la partida ha terminado
                if (players[GameManager.TakeRandom()].Finish.Value)
                {
                    youWin.SetActive(true);
                    text += "~~ FINISH ~~\n";
                    text += "---------------\n";
                    text += "FINAL SCORE\n";
                    text += "---------------\n";
                    playerText.fontSize = 35;
                }
                else //Si se ha pulsado el "TAB"
                {
                    text += "~~ [ CONNECTED PLAYERS - POINTS ] ~~\n";
                }

                Dictionary<string, int> info = new Dictionary<string, int>();
                foreach (string _playerID in players.Keys)
                {
                    info.Add(players[_playerID].Name.Value.ToString(), players[_playerID].Points.Value);
                }
                //Ordena el nuevo diccionario en funcion de la puntuacion
                var sortedInfo = from entry in info orderby entry.Value descending select entry;
                foreach (var key in sortedInfo)
                {
                    text += key.ToString() + "\n";
                }
                
                playerText.text = text;
            }
            else // Se desactiva el score
            {
                playerText.gameObject.SetActive(false);
            }
        }
    }
}

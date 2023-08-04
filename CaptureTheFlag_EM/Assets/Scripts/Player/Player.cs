using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using Unity.Netcode;
using Unity.Collections;

public class Player : NetworkBehaviour
{
    #region Variables

    //Variable compartida del estado del player (Grounded, Jumping or Hooked)
    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    public NetworkVariable<PlayerState> State;
    public NetworkVariable<int> Health;
    public NetworkVariable<int> Lives;
    public NetworkVariable<int> Points;
    public NetworkVariable<bool> Flag;
    public NetworkVariable<int> Skin;
    public NetworkVariable<bool> Dead;
    public NetworkVariable<bool> Finish;
    public NetworkVariable<FixedString64Bytes> Name;
    public NetworkVariable<Color> Color;
    public Flag flag;
    public PlayerController controller;

    public const int MaxHealth = 6;
    public const int MaxLives = 3;

    public TextMesh playerName;
    public TextMesh countDown;

    UIManager uiManager;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        //Configura los jugadores cuando se inicia ela conexion
        NetworkManager.OnClientConnectedCallback += ConfigurePlayer;

        //Inicializa el estado del player
        State = new NetworkVariable<PlayerState>();
        Health = new NetworkVariable<int>(MaxHealth);
        Lives = new NetworkVariable<int>(MaxLives);
        Points = new NetworkVariable<int>(0);
        Name = new NetworkVariable<FixedString64Bytes>("");
        Color = new NetworkVariable<Color>(Random.ColorHSV());
        Flag = new NetworkVariable<bool>(false);
        Dead = new NetworkVariable<bool>(false);
        Finish = new NetworkVariable<bool>(false);
        flag = null;
        
        uiManager = UIManager.Instance;

        GameManager.Instance.AddPlayer(this);
    }

    private void OnEnable()
    {
        //Asigna los delegados 
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged += OnPlayerStateValueChanged;
        Skin.OnValueChanged += OnPlayerSkinValueChange;

        //M�todo para cambiar la vida del jugador
        Health.OnValueChanged += OnPlayerHealthValueChange;
        Dead.OnValueChanged += OnPlayerDeath; // Delegado para mensaje de Has Muerto
    }

    private void OnDisable()
    {
        //Desasigna los delegados
        // https://docs-multiplayer.unity3d.com/netcode/current/api/Unity.Netcode.NetworkVariable-1.OnValueChangedDelegate
        State.OnValueChanged -= OnPlayerStateValueChanged;
        Skin.OnValueChanged -= OnPlayerSkinValueChange;
        Health.OnValueChanged -= OnPlayerHealthValueChange;
        Dead.OnValueChanged -= OnPlayerDeath; // Delegado para mensaje de Has Muerto

        //Cuando se destrulle el jugador, se borra de la lista de jugadores a trav�s de su ID
        GameManager.UnRegisterPlayer(transform.name);
    }

    public void OnDestroy()
    {
        base.OnDestroy();
        GameManager.Instance.RemovePlayer(this);
    }

    #endregion

    #region Config Methods

    //A�ade el jugador creado al diccionario de jugadores
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        int _netID = GetComponent<NetworkObject>().GetInstanceID();
        Player _player = GetComponent<Player>();

        //Se llama al metodo Registrar para añadir el jugador a un diccionario
        GameManager.RegisterPlayer(_netID.ToString(), _player);

    }

    void ConfigurePlayer(ulong clientID)
    {

        //Configura el player si es el due�o
        if (IsLocalPlayer)
        {
            ConfigurePlayer();
            ConfigureCamera();
            ConfigureControls();
            SkinSelectionServerRpc(UIManager.Instance.skin);
            PlayerNameServerRpc(UIManager.Instance.namePlayer.text);
            UIManager.Instance.UpdateLifeUI(MaxHealth - Health.Value);
        }

        //Metodo que cambia la skin del personje
        InternalSetSkin(Skin.Value);
    }

    void ConfigurePlayer()
    {
        //Inicializa el estado del jugador en Grounded
        UpdatePlayerStateServerRpc(PlayerState.Grounded);
    }

    void ConfigureCamera()
    {
        //Inicializa la camara
        // https://docs.unity3d.com/Packages/com.unity.cinemachine@2.6/manual/CinemachineBrainProperties.html
        var virtualCam = Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;

        virtualCam.LookAt = transform;
        virtualCam.Follow = transform;
    }

    void ConfigureControls()
    {
        //Activa los controles con InputHandler
        GetComponent<InputHandler>().enabled = true;
    }

    #endregion

    #region RPC

    #region ServerRPC

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    public void UpdatePlayerStateServerRpc(PlayerState state)
    {
        //Metodo que actualiza el estado del player en el servidor
        State.Value = state;
    }

    // Metodo para actualizar en el servidor la skin del jugador
    [ServerRpc]
    void SkinSelectionServerRpc(int skin)
    {
        Skin.Value = skin;
    }

    // Metodo para actualizar en el servidor el nombre del jugador
    [ServerRpc]
    public void PlayerNameServerRpc(string name)
    {
        Name.Value = name;
    }

    //Metodo para despawnear el jugador
    [ServerRpc]
    public void FarDestroyServerRpc()
    {
        GetComponent<NetworkObject>().Despawn();
    }

    #endregion

    #region ClientRPC

    //Metodo que asocia el nombre del jugador al cliente
    [ClientRpc]
    public void PlayerNameClientRpc(string name)
    {
        playerName.text = name;
    }

    #endregion

    #endregion

    #region Netcode Related Methods

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    void OnPlayerStateValueChanged(PlayerState previous, PlayerState current)
    {
        //Metodo que cambia el estado del player
        State.Value = current;
    }

    //Metodo para cambiar la skin en funcion de la que le llega 
    void OnPlayerSkinValueChange(int previous, int current)
    {
        InternalSetSkin(current);
    }

    //Metodo para cambiar la vida en funcion de la que le llega
    void OnPlayerHealthValueChange(int previous, int current)
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.UpdateLifeUI(6 - current);
        }
    }

    //Metodo para cambiar activar el canvas de muerte cuando mueres
    void OnPlayerDeath(bool previous, bool current)  
    {
        if (IsLocalPlayer)
        {
            UIManager.Instance.ActivateDeath();
        }
    }

    #endregion

    //Metodo para matar 
    public void Death()
    {
        if (Dead.Value) return;
        var aux = Health.Value;
        if (aux == 0)
        {
            Health.Value = MaxHealth;
            if(GameManager.Instance.status.Value == GameManager.Status.Playing)
            {
                Lives.Value--;
                if(Lives.Value == 0)
                {
                    GameManager.Instance.FinishedCondition();
                    //this.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
                    Dead.Value = true;
                    //controller.enabled = false;
                    GetComponent<NetworkObject>().Despawn();
                    //this.gameObject.SetActive(false);
                }
            }            
            if (!Dead.Value)
            {
                State.Value = PlayerState.Grounded;
                var points = GameManager.SpawnPoints;
                var point = points[Random.Range(0, points.Length)];
                transform.position = point.transform.position;
            }
        }
        else
        {
            Health.Value = aux;
        }
    }

    //Metodo que se llama al acabar el juego
    public void FinishGame()
    {
        Finish.Value = true;
        this.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezePosition;
        GameManager.Instance.FinishedCondition();
    }

    //Metodo que cambia la skin del jugador
    private void InternalSetSkin(int skin)
    {
        var animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = GameManager.Instance.Skins[skin];
    }


}

public enum PlayerState
{
    Grounded = 0,
    Jumping = 1,
    Hooked = 2
}

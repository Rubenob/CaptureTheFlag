using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(InputHandler))]
public class PlayerController : NetworkBehaviour
{

    #region Variables

    readonly float speed = 3.4f;
    readonly float jumpHeight = 6.5f;
    readonly float gravity = 1.5f;
    readonly int maxJumps = 2;


    LayerMask _layer;
    public int _jumpsLeft = 2;

    // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/ContactFilter2D.html
    ContactFilter2D filter;
    InputHandler handler;
    Player player;
    Rigidbody2D rb;
    new CapsuleCollider2D collider;
    Animator anim;
    SpriteRenderer spriteRenderer;

    // https://docs-multiplayer.unity3d.com/netcode/current/basics/networkvariable
    NetworkVariable<bool> FlipSprite;

    //Prefab para el disparo
    [SerializeField] GameObject bulletPrefab;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        handler = GetComponent<InputHandler>();
        player = GetComponent<Player>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        _jumpsLeft = 2;

        FlipSprite = new NetworkVariable<bool>();
    }

    private void OnEnable()
    {
        //Se asigna la configuracion del personaje
        handler.OnMove.AddListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.AddListener(PerformJumpServerRpc);
        handler.OnMoveFixedUpdate.AddListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged += OnFlipSpriteValueChanged;
    }

    private void OnDisable()
    {
        //Se desasigna la configuracion del personaje
        handler.OnMove.RemoveListener(UpdatePlayerVisualsServerRpc);
        handler.OnJump.RemoveListener(PerformJumpServerRpc);
        handler.OnMoveFixedUpdate.RemoveListener(UpdatePlayerPositionServerRpc);

        FlipSprite.OnValueChanged -= OnFlipSpriteValueChanged;
    }

    void Start()
    {
        // Configure Rigidbody2D
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravity;

        // Configure LayerMask
        _layer = LayerMask.GetMask("Obstacles");

        // Configure ContactFilter2D
        filter.minNormalAngle = 45;
        filter.maxNormalAngle = 135;
        filter.useNormalAngle = true;
        filter.layerMask = _layer;
    }

    #endregion

    #region RPC

    #region ServerRPC    

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerVisualsServerRpc(Vector2 input)
    {
        //Metodo que actuliza la animacion del player en el servidor
        UpdateAnimatorStateServerRpc();
        UpdateSpriteOrientation(input);
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdateAnimatorStateServerRpc()
    {
        //Metodo que actuliza el sprite en el server
        if (IsGrounded)
        {
            anim.SetBool("isGrounded", true);
            anim.SetBool("isJumping", false);
        }
        else
        {
            anim.SetBool("isGrounded", false);
        }
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void PerformJumpServerRpc()
    {
        //Detecta cuantos saltos le quedan al player y si son 0 sale del metodo        

        if (player.State.Value == PlayerState.Grounded) 
        {
            _jumpsLeft = maxJumps;
        }
        else if (_jumpsLeft == 0)
        {
            return;
        }

        
        //Activa el estado de saltar y su animacion
        player.State.Value = PlayerState.Jumping;
        anim.SetBool("isJumping", true);
        rb.velocity = new Vector2(rb.velocity.x, jumpHeight);
        _jumpsLeft--; // Resta un salto
    }

    // https://docs-multiplayer.unity3d.com/netcode/current/advanced-topics/message-system/serverrpc
    [ServerRpc]
    void UpdatePlayerPositionServerRpc(Vector2 input)
    {
        //Actualiza el estado del player en el servidor y si no estas Hooked se mueve 
        if (IsGrounded)
        {
            player.State.Value = PlayerState.Grounded;
        }
        if ((player.State.Value != PlayerState.Hooked))
        {
            rb.velocity = new Vector2(input.x * speed, rb.velocity.y);
        }
    }

    // Se llama a este metodo desde WeaponAim cuando se dispara
    [ServerRpc]
    public void PerformShootServerRpc( Vector2 crossHair, Quaternion weapon)
    {
        GameObject bullet = Instantiate(bulletPrefab, crossHair, weapon);
        bullet.GetComponent<Bullet>().owner = player.OwnerClientId;
        bullet.GetComponent<NetworkObject>().Spawn(true);
    }

    #endregion

    #endregion

    #region Methods


    void UpdateSpriteOrientation(Vector2 input)
    {
        //Actuliza la orientacion del sprite en el servidor
        if (input.x < 0)
        {
            FlipSprite.Value = false;
        }
        else if (input.x > 0)
        {
            FlipSprite.Value = true;
        }
    }

    void OnFlipSpriteValueChanged(bool previous, bool current)
    {
        //Asigna la orientacion del player
        spriteRenderer.flipX = current;
    }

    // Comprueba que el jugador este tocando el suelo y que su velocidad en el eje Y sea 0 o negativo
    bool IsGrounded => collider.IsTouching(filter) && rb.velocity.y <= 0;

    #endregion

}
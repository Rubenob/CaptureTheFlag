using UnityEngine;
using Unity.Netcode;

public class WeaponAim : NetworkBehaviour
{

    #region Variables

    [SerializeField] Transform crossHair;
    [SerializeField] Transform weapon;
    SpriteRenderer weaponRenderer;
    InputHandler handler;
    PlayerController player;

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        //Asigna el InputHandler a handler y coge el sprite del arma
        handler = GetComponent<InputHandler>();
        player = GetComponent<PlayerController>();
        weaponRenderer = weapon.gameObject.GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        handler.OnMousePosition.AddListener(UpdateCrosshairPosition);
        handler.OnFire.AddListener(Shoot);
    }

    private void OnDisable()
    {
        handler.OnMousePosition.RemoveListener(UpdateCrosshairPosition);
        handler.OnFire.RemoveListener(Shoot);
    }

    #endregion

    #region Methods

    // LLama al servidor para que ejecute un disparo desde el jugador en la direccion del CrossHair
    void Shoot(Vector2 input)
    {
        player.PerformShootServerRpc(crossHair.position, weapon.rotation);
    }

    void UpdateCrosshairPosition(Vector2 input)
    {
        //Actualiza la mira en funcion del raton
        // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
        var worldMousePosition = Camera.main.ScreenToWorldPoint(input);
        var facingDirection = worldMousePosition - transform.position;
        var aimAngle = Mathf.Atan2(facingDirection.y, facingDirection.x);
        if (aimAngle < 0f)
        {
            aimAngle = Mathf.PI * 2 + aimAngle;
        }

        SetCrossHairPosition(aimAngle);

        UpdateWeaponOrientation();

    }

    void UpdateWeaponOrientation()
    {
        //Actualiza la orientacion del sprite del arma
        weapon.right = crossHair.position - weapon.position;

        if (crossHair.localPosition.x > 0)
        {
            weaponRenderer.flipY = false;
        }
        else
        {
            weaponRenderer.flipY = true;
        }
    }

    void SetCrossHairPosition(float aimAngle)
    {
        //Calcula las posiciones en pantalla de la mira
        var x = transform.position.x + .5f * Mathf.Cos(aimAngle);
        var y = transform.position.y + .5f * Mathf.Sin(aimAngle);

        var crossHairPosition = new Vector3(x, y, 0);
        crossHair.transform.position = crossHairPosition;
    }
    #endregion
}

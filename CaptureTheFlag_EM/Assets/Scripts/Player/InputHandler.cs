using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using Unity.Netcode;
using System.Threading;

public class InputHandler : NetworkBehaviour
{

    #region Variables

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/index.html
    [SerializeField] InputAction _move;
    [SerializeField] InputAction _jump;
    [SerializeField] InputAction _hook;
    [SerializeField] InputAction _mousePosition;

    //Eventos para poder disparar y mostrar el score
    [SerializeField] InputAction _fire;
    [SerializeField] InputAction _score;

    // https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html
    public UnityEvent<Vector2> OnMove;
    public UnityEvent<Vector2> OnMoveFixedUpdate;
    public UnityEvent<Vector2> OnMousePosition;
    public UnityEvent<Vector2> OnHook;
    public UnityEvent<Vector2> OnHookRender;
    public UnityEvent OnJump;

    //Se crea un evento de unity para disparar
    public UnityEvent<Vector2> OnFire;
    

    Vector2 CachedMoveInput { get; set; }

    #endregion

    #region Unity Event Functions

    private void Awake()
    {
        //Se asignan las teclas a los movimientos
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Left", "<Keyboard>/a")
            .With("Down", "<Keyboard>/s")
            .With("Right", "<Keyboard>/d");

        _jump.AddBinding("<Keyboard>/space");
        _hook.AddBinding("<Mouse>/middleButton");
        _mousePosition.AddBinding("<Mouse>/position");

        // Boton izquierdo para disparar
        _fire.AddBinding("<Mouse>/leftButton");
        //"tab" para ver las puntiaciones
        _score.AddBinding("<Keyboard>/tab");
    }

    private void OnEnable()
    {
        //Se activan los movimientos
        _move.Enable();
        _jump.Enable();
        _hook.Enable();
        _fire.Enable();
        _mousePosition.Enable();
        _score.Enable();
    }

    private void OnDisable()
    {
        //Se desactivan los movimientos
        _move.Disable();
        _jump.Disable();
        _hook.Disable();
        _fire.Disable();
        _mousePosition.Disable();
        _score.Disable();
    }

    private void Update()
    {
        if (IsLocalPlayer)
        {
            //Lee los movimientos y el raton y los guarda en variables para luego actualizar el player
            CachedMoveInput = _move.ReadValue<Vector2>();
            var mousePosition = _mousePosition.ReadValue<Vector2>();

            var hookPerformed = _hook.WasPerformedThisFrame();

            var jumpPerformed = _jump.WasPerformedThisFrame();                

            //Funcionalidad del disparo 
            var firePerformed = _fire.WasPerformedThisFrame();

            //Para ver la puntuación
            var scorePerformed = _score.WasPerformedThisFrame();


            Move(CachedMoveInput);
            MousePosition(mousePosition);


            //Control de la mira con el raton
            // https://docs.unity3d.com/2020.3/Documentation/ScriptReference/Camera.ScreenToWorldPoint.html
            var screenPoint = Camera.main.ScreenToWorldPoint(mousePosition);
            if (hookPerformed) { Hook(screenPoint); }
            if (firePerformed) { Fire(screenPoint); }

            if (jumpPerformed) { Jump(); }
            
            //Si se pulsa "Tab" en el frame actual se avtiva en la partida 
            if (scorePerformed)
            {
                GameManager.ChangeScoreView();
            }

            HookRender(CachedMoveInput);
        }
    }

    private void FixedUpdate()
    {
        //LLama a actualizar las fisicas del player
        MoveFixedUpdate(CachedMoveInput);
    }

    #endregion

    #region InputSystem Related Methods

    void Move(Vector2 input)
    {
        OnMove?.Invoke(input);
    }

    void MoveFixedUpdate(Vector2 input)
    {
        OnMoveFixedUpdate?.Invoke(input);
    }

    void Jump()
    {
        OnJump?.Invoke();
    }

    void Hook(Vector2 input)
    {
        OnHook?.Invoke(input);
    }

    void HookRender(Vector2 input)
    {
        OnHookRender?.Invoke(input);
    }

    void Fire(Vector2 input)
    {
        OnFire?.Invoke(input);
    }

    void MousePosition(Vector2 input)
    {
        OnMousePosition?.Invoke(input);
    }

    #endregion

}
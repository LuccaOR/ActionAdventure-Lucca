using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Lê todos os controles do protagonista (teclado/mouse + gamepad).
    /// As ações são criadas em código para o player funcionar sem depender de asset externo.
    ///
    /// Teclado/Mouse: WASD andar · Mouse olhar · Espaço pular/pulo duplo · Shift dash
    ///                Clique esq. atirar · E ou clique dir. (segurar) absorver
    ///                Ctrl ou C (no ar) impacto · F interagir · Esc soltar o mouse
    /// Gamepad:       LS andar · RS olhar · A pular · B dash · RT atirar · LT absorver
    ///                RB impacto · X interagir · Start soltar
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        InputAction move, lookMouse, lookStick, jump, dash, fire, absorb, slam, interact, cancel;
        bool gameplayEnabled = true;

        public Vector2 Move => gameplayEnabled ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        /// <summary>Delta do mouse em pixels neste frame.</summary>
        public Vector2 LookMouse => gameplayEnabled ? lookMouse.ReadValue<Vector2>() : Vector2.zero;
        /// <summary>Analógico direito, -1..1.</summary>
        public Vector2 LookStick => gameplayEnabled ? lookStick.ReadValue<Vector2>() : Vector2.zero;
        public bool JumpPressed => gameplayEnabled && jump.WasPressedThisFrame();
        public bool JumpHeld => gameplayEnabled && jump.IsPressed();
        public bool DashPressed => gameplayEnabled && dash.WasPressedThisFrame();
        public bool FireHeld => gameplayEnabled && fire.IsPressed();
        public bool FirePressed => fire.WasPressedThisFrame();
        public bool AbsorbHeld => gameplayEnabled && absorb.IsPressed();
        public bool SlamPressed => gameplayEnabled && slam.WasPressedThisFrame();
        public bool InteractPressed => gameplayEnabled && interact.WasPressedThisFrame();
        public bool CancelPressed => cancel.WasPressedThisFrame();
        public bool GameplayEnabled => gameplayEnabled;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            lookMouse = new InputAction("LookMouse", InputActionType.PassThrough, "<Mouse>/delta");
            lookStick = new InputAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            dash = Button("Dash", "<Keyboard>/leftShift", "<Gamepad>/buttonEast");
            fire = Button("Fire", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
            absorb = Button("Absorb", "<Keyboard>/e", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
            slam = Button("Slam", "<Keyboard>/leftCtrl", "<Keyboard>/c", "<Gamepad>/rightShoulder");
            interact = Button("Interact", "<Keyboard>/f", "<Gamepad>/buttonWest");
            cancel = Button("Cancel", "<Keyboard>/escape", "<Gamepad>/start");
        }

        static InputAction Button(string name, params string[] bindings)
        {
            var action = new InputAction(name, InputActionType.Button);
            foreach (var b in bindings) action.AddBinding(b);
            return action;
        }

        IEnumerable<InputAction> All()
        {
            yield return move; yield return lookMouse; yield return lookStick; yield return jump;
            yield return dash; yield return fire; yield return absorb; yield return slam;
            yield return interact; yield return cancel;
        }

        void OnEnable() { foreach (var a in All()) a.Enable(); }
        void OnDisable() { foreach (var a in All()) a.Disable(); }
        void OnDestroy() { foreach (var a in All()) a.Dispose(); }

        /// <summary>Desliga o controle (cutscenes, morte, menus).</summary>
        public void SetGameplayEnabled(bool value) => gameplayEnabled = value;
    }
}

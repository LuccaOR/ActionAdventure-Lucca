using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Câmera em primeira pessoa: gira o corpo (yaw) e o pivô da câmera (pitch).
    /// Também cuida do "feel": FOV aumenta no dash e a câmera afunda ao pousar (squash).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] Transform cameraPivot;
        [SerializeField] Camera playerCamera;

        [Header("Sensibilidade")]
        [Tooltip("Graus por pixel de mouse.")]
        [SerializeField] float mouseSensitivity = 0.1f;
        [Tooltip("Graus por segundo no analógico.")]
        [SerializeField] float stickSensitivity = 200f;
        [SerializeField] bool invertY;
        [SerializeField] float minPitch = -85f;
        [SerializeField] float maxPitch = 85f;

        [Header("Feel")]
        [SerializeField] float baseFov = 80f;
        [SerializeField] float dashFovBoost = 12f;
        [SerializeField] float fovLerpSpeed = 10f;
        [Tooltip("Quanto a câmera afunda por unidade de velocidade de queda (squash do slime).")]
        [SerializeField] float landDipPerSpeed = 0.012f;
        [SerializeField] float maxLandDip = 0.3f;
        [SerializeField] float dipRecoverTime = 0.12f;

        PlayerInputReader input;
        PlayerMotor motor;
        float pitch;
        float dip;
        float dipVelocity;
        Vector3 pivotBaseLocalPos;

        public Camera Camera => playerCamera;
        public float Pitch => pitch;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (cameraPivot == null && playerCamera != null) cameraPivot = playerCamera.transform.parent;
            pivotBaseLocalPos = cameraPivot.localPosition;
        }

        void OnEnable()
        {
            if (motor != null) motor.Landed += OnLanded;
        }

        void OnDisable()
        {
            if (motor != null) motor.Landed -= OnLanded;
        }

        void Start()
        {
            SetCursorLocked(true);
            if (playerCamera != null) playerCamera.fieldOfView = baseFov;
        }

        void Update()
        {
            HandleCursor();

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = input.LookMouse * mouseSensitivity
                              + input.LookStick * (stickSensitivity * Time.deltaTime);
                transform.Rotate(0f, delta.x, 0f, Space.Self);
                pitch = Mathf.Clamp(pitch + (invertY ? delta.y : -delta.y), minPitch, maxPitch);
                cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            if (playerCamera != null)
            {
                float targetFov = baseFov + (motor != null && motor.IsDashing ? dashFovBoost : 0f);
                playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, 1f - Mathf.Exp(-fovLerpSpeed * Time.deltaTime));
            }

            dip = Mathf.SmoothDamp(dip, 0f, ref dipVelocity, dipRecoverTime);
            cameraPivot.localPosition = pivotBaseLocalPos + Vector3.down * dip;
        }

        void HandleCursor()
        {
            if (input.CancelPressed) SetCursorLocked(false);
            else if (Cursor.lockState != CursorLockMode.Locked && input.FirePressed && input.GameplayEnabled) SetCursorLocked(true);
        }

        public void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void OnLanded(float impactSpeed)
        {
            dip = Mathf.Max(dip, Mathf.Min(maxLandDip, impactSpeed * landDipPerSpeed));
        }

        /// <summary>Raio saindo do centro da tela.</summary>
        public Ray AimRay() => playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
}

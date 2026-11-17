using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Vidas do protagonista: quando o tamanho zera (ou cai do mapa), perde uma vida
    /// e volta ao tamanho inicial. Sem vidas = Game Over (R/Enter recomeça a fase).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(PlayerSize))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField, Min(1)] int maxLives = 3;
        [Tooltip("Pausa ao perder uma vida antes de voltar ao tamanho inicial.")]
        [SerializeField] float lifeLostDelay = 0.6f;
        [Tooltip("Pausa ao cair do mapa antes de voltar no checkpoint.")]
        [SerializeField] float respawnDelay = 1.2f;
        [SerializeField] float invulnerabilityAfterLifeLost = 2f;
        [Tooltip("Abaixo dessa altura o slime perde uma vida (caiu do mapa).")]
        [SerializeField] float killHeight = -30f;

        PlayerMotor motor;
        PlayerInputReader input;
        PlayerSize size;
        PlayerLook look;

        Vector3 checkpointPosition;
        Quaternion checkpointRotation;

        public int Lives { get; private set; }
        public int MaxLives => maxLives;
        public bool IsRecovering { get; private set; }
        public bool IsGameOver { get; private set; }

        /// <summary>Vidas restantes.</summary>
        public event Action<int> LifeLost;
        public event Action GameOver;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>();
            size = GetComponent<PlayerSize>();
            look = GetComponent<PlayerLook>();
            Lives = maxLives;
            SetCheckpoint(transform.position, transform.rotation);
        }

        void OnEnable() => size.Depleted += OnSizeDepleted;
        void OnDisable() => size.Depleted -= OnSizeDepleted;

        void Update()
        {
            if (IsGameOver)
            {
                if (input.RestartPressed) Restart();
                return;
            }

            if (!IsRecovering && transform.position.y < killHeight) LoseLife(true);
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
        }

        void OnSizeDepleted() => LoseLife(false);

        void LoseLife(bool backToCheckpoint)
        {
            if (IsRecovering || IsGameOver) return;

            Lives = Mathf.Max(0, Lives - 1);
            LifeLost?.Invoke(Lives);

            if (Lives <= 0)
            {
                IsGameOver = true;
                input.SetGameplayEnabled(false);
                if (look != null) look.SetCursorLocked(false);
                GameOver?.Invoke();
                return;
            }

            StartCoroutine(RecoverRoutine(backToCheckpoint));
        }

        IEnumerator RecoverRoutine(bool backToCheckpoint)
        {
            IsRecovering = true;
            input.SetGameplayEnabled(false);
            yield return new WaitForSeconds(backToCheckpoint ? respawnDelay : lifeLostDelay);

            if (backToCheckpoint) motor.Teleport(checkpointPosition, checkpointRotation);
            size.ResetToStart();
            size.SetInvulnerableFor(invulnerabilityAfterLifeLost);
            input.SetGameplayEnabled(true);
            IsRecovering = false;
        }

        void Restart()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else SceneManager.LoadScene(scene.name);
        }
    }
}

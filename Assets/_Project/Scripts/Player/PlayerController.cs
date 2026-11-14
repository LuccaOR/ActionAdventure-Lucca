using System.Collections;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Ponto central do protagonista: morte, respawn no checkpoint e queda para fora do mapa.
    /// Os outros componentes (motor, tiro, absorção...) funcionam sozinhos.
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 1.5f;
        [Tooltip("Abaixo dessa altura o slime morre (caiu do mapa).")]
        [SerializeField] float killHeight = -30f;

        PlayerMotor motor;
        PlayerInputReader input;
        Health health;
        SlimeMass mass;

        Vector3 checkpointPosition;
        Quaternion checkpointRotation;

        public bool IsDead { get; private set; }

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
            mass = GetComponent<SlimeMass>();
            SetCheckpoint(transform.position, transform.rotation);
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void Update()
        {
            if (!IsDead && transform.position.y < killHeight) health.Kill();
        }

        public void SetCheckpoint(Vector3 position, Quaternion rotation)
        {
            checkpointPosition = position;
            checkpointRotation = rotation;
        }

        void OnDied()
        {
            if (IsDead) return;
            IsDead = true;
            input.SetGameplayEnabled(false);
            StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            motor.Teleport(checkpointPosition, checkpointRotation);
            health.ResetToFull();
            if (mass != null) mass.ResetToFull();
            input.SetGameplayEnabled(true);
            IsDead = false;
        }
    }
}

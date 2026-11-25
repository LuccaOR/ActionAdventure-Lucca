using SlimeHunter.Player;
using SlimeHunter.Puzzles;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>
    /// Perigo (espinhos, riacho, água): tira tamanho e devolve o jogador ao ponto seguro.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HazardZone : MonoBehaviour
    {
        [Tooltip("Para onde o jogador volta.")]
        [SerializeField] Transform resetPoint;
        [Tooltip("Ignora o resetPoint e volta ao último checkpoint (rede de segurança sob o mapa).")]
        [SerializeField] bool returnToCheckpoint;
        [SerializeField] float damage = 10f;
        [SerializeField] string message = "Ai! Espinhos!";

        Collider zone;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => zone = GetComponent<Collider>();

        void Update()
        {
            PlayerSize player = PlayerZone.Find(zone);
            if (player == null) return;

            var motor = player.GetComponent<PlayerMotor>();
            var controller = player.GetComponent<PlayerController>();
            if (returnToCheckpoint && controller != null) controller.ReturnToCheckpoint();
            else if (resetPoint != null) motor.Teleport(resetPoint.position, resetPoint.rotation);
            else return;
            player.TakeDamage(damage, player.transform.position, Vector3.zero);
            if (!string.IsNullOrEmpty(message)) PlayerHUD.ShowHint(message, 2f);
        }
    }
}

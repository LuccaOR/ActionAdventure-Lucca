using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Interaction
{
    /// <summary>Ao passar por aqui, o protagonista renasce neste ponto.</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.SetCheckpoint(transform.position, transform.rotation);
        }
    }
}

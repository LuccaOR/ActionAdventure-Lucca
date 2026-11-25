using SlimeHunter.Player;
using SlimeHunter.Puzzles;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>Área que mostra uma dica de tutorial enquanto o jogador está dentro.</summary>
    [RequireComponent(typeof(Collider))]
    public class HintZone : MonoBehaviour
    {
        [TextArea] [SerializeField] string text = "Dica";

        Collider zone;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => zone = GetComponent<Collider>();

        void Update()
        {
            if (PlayerZone.Find(zone) != null) PlayerHUD.ShowHint(text);
        }
    }
}

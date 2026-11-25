using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Área (trigger) que avisa quando o tamanho do jogador não serve. Usada na frente das
    /// passagens estreitas: a passagem em si é só geometria baixa, já que o collider do
    /// jogador encolhe junto com o tamanho.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SizeHintZone : MonoBehaviour
    {
        public enum Rule { AtMost, AtLeast }

        [SerializeField] Rule rule = Rule.AtMost;
        [SerializeField] float size = 1.3f;
        [SerializeField] string tooBigMessage = "Grande demais para passar! Atire para diminuir (tamanho {0} ou menos)";
        [SerializeField] string tooSmallMessage = "Pequeno demais! Absorva slimes para crescer (tamanho {0} ou mais)";
        [SerializeField] string okMessage = "Tamanho certo, pode ir!";

        Collider zone;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake() => zone = GetComponent<Collider>();

        void Update()
        {
            PlayerSize player = PlayerZone.Find(zone);
            if (player == null) return;

            bool ok = rule == Rule.AtMost ? player.Current <= size : player.Current >= size;
            string msg = ok ? okMessage : string.Format(rule == Rule.AtMost ? tooBigMessage : tooSmallMessage, size.ToString("0.#"));
            PlayerHUD.ShowHint($"{msg}   (você: {player.Current:0.0})");
        }
    }
}

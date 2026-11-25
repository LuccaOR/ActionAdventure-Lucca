using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>Mostra uma dica quando um evento chama Show() (ex.: Rei derrotado, portão aberto).</summary>
    public class EventHint : MonoBehaviour
    {
        [TextArea] [SerializeField] string text = "";
        [SerializeField] float seconds = 4f;

        public void Show() => PlayerHUD.ShowHint(text, seconds);
    }
}

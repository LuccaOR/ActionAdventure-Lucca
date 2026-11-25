using SlimeHunter.Player;
using SlimeHunter.Puzzles;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>Mostra o nome da área no topo da tela quando o jogador entra nela.</summary>
    [RequireComponent(typeof(Collider))]
    public class AreaBanner : MonoBehaviour
    {
        [SerializeField] string title = "Área";
        [SerializeField] string subtitle = "";
        [SerializeField] float seconds = 3f;

        Collider zone;
        bool inside;

        /// <summary>Última área em que o jogador entrou (útil para testes e para salvar progresso).</summary>
        public static string CurrentArea { get; private set; }

        public string Title => title;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => zone = GetComponent<Collider>();

        void Update()
        {
            bool now = PlayerZone.Find(zone) != null;
            if (now && !inside && CurrentArea != title)
            {
                CurrentArea = title;
                PlayerHUD.ShowAreaTitle(title, subtitle, seconds);
            }
            inside = now;
        }
    }
}

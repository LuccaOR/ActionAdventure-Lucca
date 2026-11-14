using System.Text;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// HUD de protótipo (IMGUI): mira, vida, gosma, dash, absorção, prompts e itens.
    /// Trocar por uma UI definitiva (UI Toolkit/uGUI) quando a direção de arte estiver pronta.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] bool showHud = true;
        [SerializeField] bool showControls = true;

        Health health;
        SlimeMass mass;
        PlayerMotor motor;
        PlayerAbsorb absorb;
        PlayerInteractor interactor;
        PlayerInventory inventory;
        PlayerController controller;

        GUIStyle label;
        GUIStyle centerLabel;
        readonly StringBuilder sb = new StringBuilder();

        static readonly Color HealthColor = new Color(0.95f, 0.3f, 0.35f);
        static readonly Color MassColor = new Color(0.35f, 0.95f, 0.45f);
        static readonly Color DashColor = new Color(0.4f, 0.75f, 1f);
        static readonly Color BackColor = new Color(0f, 0f, 0f, 0.55f);

        void Awake()
        {
            health = GetComponent<Health>();
            mass = GetComponent<SlimeMass>();
            motor = GetComponent<PlayerMotor>();
            absorb = GetComponent<PlayerAbsorb>();
            interactor = GetComponent<PlayerInteractor>();
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<PlayerController>();
        }

        void OnGUI()
        {
            if (!showHud) return;
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
                label.normal.textColor = Color.white;
                centerLabel = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            }

            float w = Screen.width, h = Screen.height;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);

            // Mira (fica verde quando dá pra absorver algo)
            bool canAbsorb = absorb != null && absorb.LookTarget != null;
            Rect(new Rect(c.x - 3, c.y - 3, 6, 6), canAbsorb ? MassColor : Color.white);

            // Barras
            float x = 24, y = h - 90, bw = 260;
            if (health != null) Bar(new Rect(x, y, bw, 18), health.Normalized, HealthColor, $"Vida {Mathf.CeilToInt(health.Current)}");
            if (mass != null) Bar(new Rect(x, y + 24, bw, 18), mass.Normalized, MassColor, $"Gosma {Mathf.FloorToInt(mass.Current)}");
            if (motor != null) Bar(new Rect(x, y + 48, bw * 0.5f, 10), motor.DashReady01, DashColor, null);

            // Absorção
            if (absorb != null)
            {
                if (absorb.IsAbsorbing)
                {
                    Bar(new Rect(c.x - 100, c.y + 40, 200, 14), absorb.Progress01, MassColor, null);
                    GUI.Label(new Rect(c.x - 150, c.y + 58, 300, 30), "Glunb... glunb...", centerLabel);
                }
                else if (canAbsorb)
                {
                    GUI.Label(new Rect(c.x - 200, c.y + 40, 400, 30), "[E] Segure para absorver", centerLabel);
                }
                GUI.Label(new Rect(24, 20, 300, 26), $"Slimes absorvidos: {absorb.TotalAbsorbed}", label);
            }

            // Interação
            if (interactor != null && !canAbsorb)
            {
                string prompt = interactor.CurrentPrompt;
                if (!string.IsNullOrEmpty(prompt))
                    GUI.Label(new Rect(c.x - 250, c.y + 40, 500, 30), prompt, centerLabel);
            }

            // Inventário
            if (inventory != null && inventory.Items.Count > 0)
            {
                sb.Clear();
                foreach (var kv in inventory.Items)
                    sb.Append(inventory.GetDisplayName(kv.Key)).Append(" x").Append(kv.Value).Append('\n');
                GUI.Label(new Rect(w - 280, 20, 260, 200), sb.ToString(), label);
            }

            if (controller != null && controller.IsDead)
                GUI.Label(new Rect(0, c.y - 60, w, 40), "BLORP... (renascendo)", centerLabel);

            if (showControls)
            {
                GUI.Label(new Rect(24, 48, 700, 26),
                    "WASD andar · Espaço pular (2x) · Shift dash · Clique atirar · E absorver · Ctrl/C impacto (no ar) · F interagir · Esc mouse",
                    new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Normal });
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                GUI.Label(new Rect(0, c.y - 20, w, 40), "Clique para jogar", centerLabel);
        }

        static void Rect(Rect r, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        void Bar(Rect r, float value01, Color color, string text)
        {
            Rect(r, BackColor);
            Rect(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(value01), r.height - 4), color);
            if (!string.IsNullOrEmpty(text))
                GUI.Label(new Rect(r.x + 6, r.y - 3, r.width, r.height + 6), text, new GUIStyle(label) { fontSize = 13 });
        }
    }
}

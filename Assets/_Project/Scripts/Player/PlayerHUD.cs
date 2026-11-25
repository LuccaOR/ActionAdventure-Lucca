using System.Text;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// HUD de protótipo (IMGUI): mira, barras de tamanho (= vida), vidas, dash, carga do tiro,
    /// absorção, prompts, itens e Game Over.
    /// Trocar por uma UI definitiva (UI Toolkit/uGUI) quando a direção de arte estiver pronta.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] bool showHud = true;
        [SerializeField] bool showControls = true;

        PlayerSize size;
        PlayerMotor motor;
        PlayerShooter shooter;
        PlayerAbsorb absorb;
        PlayerInteractor interactor;
        PlayerInventory inventory;
        PlayerController controller;

        GUIStyle label;
        GUIStyle centerLabel;
        GUIStyle bigLabel;
        GUIStyle titleLabel;
        GUIStyle smallLabel;
        readonly StringBuilder sb = new StringBuilder();

        static string hint;
        static float hintUntil;
        static float fadeStart = float.NegativeInfinity;
        static float fadeDuration = 1f;

        static string areaTitle, areaSubtitle;
        static float areaStart = float.NegativeInfinity, areaDuration = 3f;

        /// <summary>Nome da área em letras grandes no topo da tela (entra e some com fade).</summary>
        public static void ShowAreaTitle(string title, string subtitle, float seconds = 3f)
        {
            areaTitle = title;
            areaSubtitle = subtitle;
            areaStart = Time.time;
            areaDuration = Mathf.Max(0.5f, seconds);
        }

        /// <summary>Tela preta que clareia em 'seconds' (portais, teletransporte).</summary>
        public static void Fade(float seconds)
        {
            fadeStart = Time.time;
            fadeDuration = Mathf.Max(0.05f, seconds);
        }

        /// <summary>Mostra uma dica na tela por alguns instantes (puzzles chamam enquanto o jogador está perto).</summary>
        public static void ShowHint(string text, float seconds = 0.25f)
        {
            hint = text;
            hintUntil = Time.time + seconds;
        }

        static readonly Color SizeColor = new Color(0.45f, 0.85f, 1f);
        static readonly Color GrowColor = new Color(0.35f, 0.95f, 0.5f);
        static readonly Color LifeColor = new Color(0.95f, 0.35f, 0.45f);
        static readonly Color DashColor = new Color(0.75f, 0.6f, 1f);
        static readonly Color ChargeColor = new Color(1f, 0.8f, 0.25f);
        static readonly Color BackColor = new Color(0f, 0f, 0f, 0.55f);

        void Awake()
        {
            size = GetComponent<PlayerSize>();
            motor = GetComponent<PlayerMotor>();
            shooter = GetComponent<PlayerShooter>();
            absorb = GetComponent<PlayerAbsorb>();
            interactor = GetComponent<PlayerInteractor>();
            inventory = GetComponent<PlayerInventory>();
            controller = GetComponent<PlayerController>();
        }

        void OnGUI()
        {
            if (!showHud) return;
            EnsureStyles();
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.backgroundColor = Color.white;

            float w = Screen.width, h = Screen.height;
            Vector2 c = new Vector2(w * 0.5f, h * 0.5f);

            if (controller != null && controller.IsGameOver)
            {
                Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, 0.7f));
                GUI.Label(new Rect(0, c.y - 60, w, 60), "GAME OVER", bigLabel);
                GUI.Label(new Rect(0, c.y + 10, w, 30), "Glunb... (R ou Enter para recomeçar)", centerLabel);
                return;
            }

            // Mira (fica verde quando dá pra absorver algo)
            bool canAbsorb = absorb != null && absorb.Available != null;
            Fill(new Rect(c.x - 3, c.y - 3, 6, 6), canAbsorb ? GrowColor : Color.white);

            DrawSize(new Vector2(24, h - 70));
            DrawLives(new Vector2(24, h - 118));
            if (motor != null) Bar(new Rect(24, h - 30, 120, 8), motor.DashReady01, DashColor);

            // Carga do tiro
            if (shooter != null && shooter.IsCharging)
            {
                bool full = shooter.Charge01 >= 1f;
                Color col = !shooter.CanAffordCharged ? Color.gray : full ? GrowColor : ChargeColor;
                Bar(new Rect(c.x - 60, c.y + 22, 120, 8), shooter.Charge01, col);
                if (!shooter.CanAffordCharged)
                    GUI.Label(new Rect(c.x - 150, c.y + 32, 300, 24), "Pequeno demais para o tiro carregado", centerLabel);
            }

            // Absorção
            if (absorb != null)
            {
                if (absorb.IsAbsorbing)
                {
                    Bar(new Rect(c.x - 100, c.y + 40, 200, 14), absorb.Progress01, GrowColor);
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

            // Dica de puzzle
            if (!string.IsNullOrEmpty(hint) && Time.time < hintUntil)
            {
                var r = new Rect(c.x - 320, h - 170, 640, 34);
                Fill(r, BackColor);
                ShadowLabel(r, hint, centerLabel);
            }

            if (controller != null && controller.IsRecovering)
                GUI.Label(new Rect(0, c.y - 60, w, 40), "BLORP! Perdeu uma vida", centerLabel);

            if (showControls)
            {
                GUI.Label(new Rect(24, 48, 900, 22),
                    "WASD mover (saltinhos) · Espaço pular (2x no ar) · Shift dash (atravessa barreiras) · Clique tiro rápido · Segure/solte tiro carregado",
                    smallLabel);
                GUI.Label(new Rect(24, 66, 900, 22),
                    "E (em cima da poça) absorver · Ctrl/C impacto (no ar) · F interagir · Esc soltar o mouse",
                    smallLabel);
            }

            // Nome da área
            float at = (Time.time - areaStart) / areaDuration;
            if (!string.IsNullOrEmpty(areaTitle) && at >= 0f && at < 1f)
            {
                float alpha = Mathf.Clamp01(Mathf.Min(at * 6f, (1f - at) * 4f));
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);
                ShadowLabel(new Rect(0, 90, w, 60), areaTitle, titleLabel);
                if (!string.IsNullOrEmpty(areaSubtitle))
                    ShadowLabel(new Rect(0, 145, w, 30), areaSubtitle, centerLabel);
                GUI.color = oldColor;
            }

            float fade = 1f - (Time.time - fadeStart) / fadeDuration;
            if (fade > 0f) Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, fade));

            if (Cursor.lockState != CursorLockMode.Locked && (controller == null || !controller.IsGameOver))
                GUI.Label(new Rect(0, c.y - 20, w, 40), "Clique para jogar", centerLabel);
        }

        void DrawSize(Vector2 pos)
        {
            if (size == null) return;
            int bars = Mathf.CeilToInt(size.Max);
            const float bw = 46f, bh = 18f, gap = 4f;
            for (int i = 0; i < bars; i++)
            {
                var r = new Rect(pos.x + i * (bw + gap), pos.y, bw, bh);
                float fill = Mathf.Clamp01(size.Current - i);
                // Barras acima do tamanho inicial = crescimento extra (cor diferente).
                Bar(r, fill, i < size.Start ? SizeColor : GrowColor);
            }
            sb.Clear();
            sb.Append("Tamanho ").Append(size.Current.ToString("0.0"));
            if (motor != null)
                sb.Append("   ·   Velocidade x").Append(motor.SizeSpeed.ToString("0.00"))
                  .Append("   ·   Impacto x").Append(motor.SizeSlamPower.ToString("0.00"));
            if (shooter != null)
                sb.Append("   ·   Tiro x").Append(shooter.ShotPace.ToString("0.00"))
                  .Append("   ·   Bolas (carregado) ").Append(shooter.ChargedPellets);
            GUI.Label(new Rect(pos.x, pos.y - 22, 600, 22), sb.ToString(), smallLabel);
        }

        void DrawLives(Vector2 pos)
        {
            if (controller == null) return;
            for (int i = 0; i < controller.MaxLives; i++)
            {
                var r = new Rect(pos.x + i * 22f, pos.y, 16f, 16f);
                Fill(r, i < controller.Lives ? LifeColor : BackColor);
            }
        }

        void EnsureStyles()
        {
            // No Unity 6.6 os GUIStyle podem voltar para o padrão (tamanho 0, texto preto) depois de
            // criados; se isso acontecer, recria. Detectado pelo tamanho da fonte.
            if (label != null && label.fontSize == 16 && centerLabel.fontSize == 18) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold };
            // Branco em todos os estados (o skin padrão pode trocar a cor em hover/focus).
            label.normal.textColor = label.hover.textColor = label.active.textColor =
                label.focused.textColor = label.onNormal.textColor = Color.white;
            label.normal.background = null;
            centerLabel = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            bigLabel = new GUIStyle(centerLabel) { fontSize = 56 };
            titleLabel = new GUIStyle(centerLabel) { fontSize = 40 };
            smallLabel = new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Normal };
        }

        /// <summary>Texto com sombra preta: legível em qualquer fundo.</summary>
        static void ShadowLabel(Rect r, string text, GUIStyle style)
        {
            Color old = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = old;
            GUI.Label(r, text, style);
        }

        static void Fill(Rect r, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        static void Bar(Rect r, float value01, Color color)
        {
            Fill(r, BackColor);
            Fill(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(value01), r.height - 4), color);
        }
    }
}

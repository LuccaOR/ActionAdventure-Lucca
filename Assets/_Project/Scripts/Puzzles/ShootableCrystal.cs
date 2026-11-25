using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Cristal que reage a projéteis (IDamageable) e avisa o CrystalSequencePuzzle.
    /// Visual: apagado → aceso na sua cor → pisca vermelho no erro → verde quando resolvido.
    /// </summary>
    public class ShootableCrystal : MonoBehaviour, IDamageable
    {
        [SerializeField] Color color = Color.cyan;
        [Tooltip("Intensidade da cor apagado (0..1).")]
        [SerializeField] float dimAmount = 0.25f;
        [SerializeField] float spinSpeed = 40f;
        [Tooltip("Ignora tiros repetidos dentro desse tempo (o tiro carregado solta várias bolas).")]
        [SerializeField] float hitCooldown = 0.35f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color ErrorColor = new Color(1f, 0.15f, 0.1f);
        static readonly Color SolvedColor = new Color(0.4f, 1f, 0.5f);

        Renderer rend;
        MaterialPropertyBlock mpb;
        float lastHit = float.NegativeInfinity;
        float flashUntil;
        Color flashColor;
        bool lit;
        bool solved;

        internal CrystalSequencePuzzle Puzzle { get; set; }
        public Color Color => color;
        public bool IsLit => lit;

        void Awake()
        {
            rend = GetComponentInChildren<Renderer>();
            Apply(Dim);
        }

        Color Dim => Color.Lerp(Color.black, color, dimAmount);

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (Time.time - lastHit < hitCooldown) return;
            lastHit = Time.time;
            if (Puzzle != null) Puzzle.OnCrystalHit(this);
        }

        internal void SetLit(bool value) => lit = value;
        internal void SetSolved() => solved = true;

        internal void Flash(Color c, float seconds)
        {
            flashColor = c;
            flashUntil = Time.time + seconds;
        }

        internal static Color Error => ErrorColor;

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

            Color c;
            if (Time.time < flashUntil) c = flashColor;
            else if (solved) c = SolvedColor;
            else if (lit) c = color * 1.4f;
            else c = Dim;
            Apply(c);
        }

        void Apply(Color c)
        {
            if (rend == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            rend.SetPropertyBlock(mpb);
        }
    }
}

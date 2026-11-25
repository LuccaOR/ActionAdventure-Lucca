using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Grade atravessável: um túnel de grades que abre só por um instante, em ciclo.
    /// A janela é curta demais para atravessar aos saltinhos; com o Dash no momento certo dá.
    /// Antes de abrir as barras tremem e mudam de cor (aviso). Se a grade fechar com o jogador
    /// dentro, ele é empurrado de volta para a entrada e perde um pouco de tamanho.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class TimedGrate : MonoBehaviour
    {
        [Header("Ciclo")]
        [SerializeField] float closedTime = 2.2f;
        [Tooltip("Aviso (barras tremendo) no fim do tempo fechada.")]
        [SerializeField] float warnTime = 0.5f;
        [Tooltip("Janela aberta: curta o bastante para só o Dash passar.")]
        [SerializeField] float openTime = 0.45f;
        [SerializeField] float barTravel = 2.7f;
        [SerializeField] float barSpeed = 40f;

        [Header("Visual")]
        [SerializeField] Transform[] bars = new Transform[0];
        [SerializeField] Color closedColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] Color warnColor = new Color(1f, 0.8f, 0.2f);

        [Header("Pego dentro")]
        [SerializeField] float caughtDamage = 10f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        BoxCollider blocker;
        Vector3[] barClosedPos;
        Renderer[] barRenderers;
        MaterialPropertyBlock mpb;
        float cycleStart;

        public bool IsOpen { get; private set; }

        float Cycle => closedTime + openTime;

        void Awake()
        {
            blocker = GetComponent<BoxCollider>();
            barClosedPos = new Vector3[bars.Length];
            barRenderers = new Renderer[bars.Length];
            for (int i = 0; i < bars.Length; i++)
            {
                barClosedPos[i] = bars[i].localPosition;
                barRenderers[i] = bars[i].GetComponent<Renderer>();
            }
            cycleStart = Time.time;
        }

        void Update()
        {
            float t = (Time.time - cycleStart) % Cycle;
            bool open = t >= closedTime;
            bool warning = !open && t >= closedTime - warnTime;

            if (open != IsOpen)
            {
                IsOpen = open;
                blocker.enabled = !open;
                if (!open) EjectCaughtPlayer();
            }

            for (int i = 0; i < bars.Length; i++)
            {
                Vector3 target = barClosedPos[i] + (open ? Vector3.down * barTravel : Vector3.zero);
                if (warning) target += new Vector3(Mathf.Sin(Time.time * 60f + i) * 0.03f, 0f, 0f);
                bars[i].localPosition = Vector3.MoveTowards(bars[i].localPosition, target, barSpeed * Time.deltaTime);
                Paint(barRenderers[i], warning ? warnColor : closedColor);
            }
        }

        void EjectCaughtPlayer()
        {
            // O blocker acabou de ligar: o volume dele é o túnel.
            Bounds b = blocker.bounds;
            Collider[] hits = Physics.OverlapBox(b.center, b.extents, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            foreach (var h in hits)
            {
                var motor = h.GetComponentInParent<PlayerMotor>();
                if (motor == null) continue;

                // Volta pela ponta mais próxima do túnel (eixo forward da grade).
                Vector3 local = transform.InverseTransformPoint(motor.transform.position);
                float halfDepth = blocker.size.z * 0.5f + 0.8f;
                Vector3 exitLocal = new Vector3(0f, 0.1f, local.z >= 0f ? halfDepth : -halfDepth);
                Vector3 exit = transform.TransformPoint(exitLocal);
                motor.Teleport(exit, motor.transform.rotation);

                var size = motor.GetComponent<PlayerSize>();
                if (size != null) size.TakeDamage(caughtDamage, exit, Vector3.zero);
                PlayerHUD.ShowHint("A grade fechou! Use o Dash quando ela abrir.", 2f);
                return;
            }
        }

        void Paint(Renderer r, Color c)
        {
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }
}

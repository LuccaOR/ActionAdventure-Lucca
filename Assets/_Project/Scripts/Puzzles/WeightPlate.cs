using SlimeHunter.Player;
using UnityEngine;
using UnityEngine.Events;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Placa de peso: só afunda se o jogador estiver em cima E tiver o tamanho mínimo.
    /// O jogador precisa absorver inimigos/gosma para ficar pesado o bastante.
    /// Com latch = true, fica ativada para sempre (libera passagem); senão solta ao sair.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeightPlate : MonoBehaviour
    {
        [Tooltip("Tamanho mínimo (barras) para a placa afundar.")]
        [SerializeField] float requiredSize = 4f;
        [SerializeField] bool latch = true;
        [Tooltip("Parte que afunda (visual).")]
        [SerializeField] Transform plate;
        [SerializeField] float pressDepth = 0.12f;
        [SerializeField] Color idleColor = new Color(0.85f, 0.35f, 0.3f);
        [SerializeField] Color pressedColor = new Color(0.35f, 0.95f, 0.45f);
        [SerializeField] UnityEvent onActivated = new UnityEvent();
        [SerializeField] UnityEvent onReleased = new UnityEvent();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Vector3 plateUp;
        Renderer plateRenderer;
        MaterialPropertyBlock mpb;
        Collider zone;

        public UnityEvent OnActivated => onActivated;
        public UnityEvent OnReleased => onReleased;
        public bool IsPressed { get; private set; }
        public float RequiredSize => requiredSize;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            zone = GetComponent<Collider>();
            if (plate != null)
            {
                plateUp = plate.localPosition;
                plateRenderer = plate.GetComponent<Renderer>();
            }
            SetColor(idleColor);
        }

        void Update()
        {
            PlayerSize playerOnTop = PlayerZone.Find(zone);
            if (playerOnTop != null && !IsPressed && playerOnTop.Current < requiredSize)
                PlayerHUD.ShowHint($"Leve demais! A placa precisa de tamanho {requiredSize:0.#}   (você: {playerOnTop.Current:0.0})");

            bool heavy = playerOnTop != null && playerOnTop.Current >= requiredSize;
            if (heavy && !IsPressed)
            {
                IsPressed = true;
                SetColor(pressedColor);
                onActivated.Invoke();
            }
            else if (!heavy && IsPressed && !latch)
            {
                IsPressed = false;
                SetColor(idleColor);
                onReleased.Invoke();
            }

            if (plate != null)
            {
                Vector3 target = IsPressed ? plateUp + Vector3.down * pressDepth : plateUp;
                plate.localPosition = Vector3.MoveTowards(plate.localPosition, target, Time.deltaTime);
            }
        }

        void SetColor(Color c)
        {
            if (plateRenderer == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            plateRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            plateRenderer.SetPropertyBlock(mpb);
        }
    }
}

using SlimeHunter.Player;
using SlimeHunter.Puzzles;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>
    /// Portal: leva o jogador até target (outro ponto da mesma cena) com um fade e marca
    /// o destino como checkpoint. Pode começar desligado e ser ligado por evento (Activate),
    /// ex.: arena do tutorial limpa, portão das 3 chaves aberto.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] bool startActive;
        [Tooltip("Parte visual que gira e muda de cor.")]
        [SerializeField] Renderer visual;
        [SerializeField] Color activeColor = new Color(0.25f, 0.45f, 1f);
        [SerializeField] Color inactiveColor = new Color(0.2f, 0.2f, 0.25f);
        [SerializeField] string activatedMessage = "Um portal se abriu!";
        [SerializeField] float spinSpeed = 90f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        Collider zone;
        MaterialPropertyBlock mpb;
        float cooldownUntil;

        public bool IsActive { get; private set; }
        public Transform Target => target;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            zone = GetComponent<Collider>();
            IsActive = startActive;
            Paint();
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            Paint();
            if (!string.IsNullOrEmpty(activatedMessage)) PlayerHUD.ShowHint(activatedMessage, 3f);
        }

        void Update()
        {
            if (visual != null && IsActive) visual.transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime, Space.Self);
            if (!IsActive || target == null || Time.time < cooldownUntil) return;

            PlayerSize player = PlayerZone.Find(zone);
            if (player == null) return;

            cooldownUntil = Time.time + 1f;
            player.GetComponent<PlayerMotor>().Teleport(target.position, target.rotation);
            var controller = player.GetComponent<PlayerController>();
            if (controller != null) controller.SetCheckpoint(target.position, target.rotation);
            PlayerHUD.Fade(0.6f);
        }

        void Paint()
        {
            if (visual == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            visual.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, IsActive ? activeColor : inactiveColor);
            visual.SetPropertyBlock(mpb);
        }
    }
}

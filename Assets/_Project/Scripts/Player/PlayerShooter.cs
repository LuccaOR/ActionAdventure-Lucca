using System;
using SlimeHunter.Combat;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Ataque com projéteis de slime, feitos com o próprio corpo (gasta tamanho):
    /// - Disparo rápido: clique (toque curto). Cada barra de tamanho rende 6 disparos.
    /// - Disparo carregado: segure até carregar e solte. Dispersão de projéteis, custa 1 barra.
    ///   Quanto maior o slime, mais bolas saem no disparo carregado.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] SlimeProjectile projectilePrefab;
        [SerializeField] Transform muzzle;
        [SerializeField] Camera aimCamera;
        [SerializeField] float maxAimDistance = 150f;
        [SerializeField] LayerMask aimMask = ~0;

        [Header("Disparo rápido")]
        [SerializeField] float quickDamage = 12f;
        [SerializeField] float quickSpeed = 40f;
        [SerializeField, Min(1)] int shotsPerBar = 6;
        [Tooltip("Intervalo mínimo entre cliques.")]
        [SerializeField] float quickMinInterval = 0.12f;

        [Header("Disparo carregado")]
        [Tooltip("Tempo segurando para o disparo carregar por completo.")]
        [SerializeField] float chargeTime = 0.6f;
        [Tooltip("Abaixo desse tempo o clique conta como disparo rápido e a barra de carga nem aparece.")]
        [SerializeField] float chargeDisplayDelay = 0.15f;
        [SerializeField] float chargedCostBars = 1f;
        [Tooltip("Quantidade de bolas no tamanho inicial.")]
        [SerializeField, Min(1)] int pelletCount = 8;
        [Tooltip("Quantidade de bolas no menor tamanho.")]
        [SerializeField, Min(1)] int pelletsAtSmallest = 4;
        [Tooltip("Quantidade de bolas no maior tamanho.")]
        [SerializeField, Min(1)] int pelletsAtLargest = 16;
        [Tooltip("Abertura do cone de dispersão (graus).")]
        [SerializeField] float spreadAngle = 9f;
        [SerializeField] float pelletDamage = 9f;
        [SerializeField] float pelletSpeed = 35f;
        [SerializeField] float pelletScale = 0.75f;

        [Header("Efeito do tamanho (1 = tamanho inicial)")]
        [Tooltip("Ritmo dos tiros no menor tamanho: velocidade do projétil, cadência e tempo de carga.")]
        [SerializeField] float shotPaceAtSmallest = 1.4f;
        [Tooltip("Ritmo dos tiros no maior tamanho.")]
        [SerializeField] float shotPaceAtLargest = 0.75f;

        static readonly RaycastHit[] AimHits = new RaycastHit[16];

        PlayerInputReader input;
        PlayerSize size;
        PlayerAbsorb absorb;
        float nextQuickTime;
        bool charging;
        float chargeStart;

        public float QuickShotCost => 1f / shotsPerBar;
        public bool IsCharging => charging && Time.time - chargeStart >= chargeDisplayDelay;
        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStart) / ChargeTime) : 0f;

        /// <summary>Pequeno = tiros mais rápidos, grande = mais lentos.</summary>
        public float ShotPace => size != null ? size.Modifier(shotPaceAtSmallest, shotPaceAtLargest) : 1f;
        float ChargeTime => chargeTime / ShotPace;

        /// <summary>Bolas do tiro carregado: quanto maior o slime, mais bolas.</summary>
        public int ChargedPellets
        {
            get
            {
                if (size == null) return pelletCount;
                float m = size.Modifier((float)pelletsAtSmallest / pelletCount, (float)pelletsAtLargest / pelletCount);
                return Mathf.Max(1, Mathf.RoundToInt(pelletCount * m));
            }
        }
        public bool CanAffordCharged => size == null || size.CanSpend(chargedCostBars);

        /// <summary>true = carregado.</summary>
        public event Action<bool> Fired;
        /// <summary>Tentou atirar sem tamanho suficiente.</summary>
        public event Action OutOfSize;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            size = GetComponent<PlayerSize>();
            absorb = GetComponent<PlayerAbsorb>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
            if (muzzle == null && aimCamera != null) muzzle = aimCamera.transform;
        }

        void Update()
        {
            bool blocked = !input.GameplayEnabled || Cursor.lockState != CursorLockMode.Locked
                           || (absorb != null && absorb.IsAbsorbing);
            if (blocked)
            {
                charging = false;
                return;
            }

            if (input.FirePressed)
            {
                charging = true;
                chargeStart = Time.time;
            }

            if (charging && input.FireReleased)
            {
                charging = false;
                if (Time.time - chargeStart >= ChargeTime) FireCharged();
                else FireQuick();
            }
        }

        void FireQuick()
        {
            if (projectilePrefab == null) { MissingPrefab(); return; }
            if (Time.time < nextQuickTime) return;
            if (size != null && !size.TrySpend(QuickShotCost)) { OutOfSize?.Invoke(); return; }

            float pace = ShotPace;
            nextQuickTime = Time.time + quickMinInterval / pace;
            Vector3 dir = AimDirection();
            Spawn(dir, quickSpeed * pace, quickDamage, 1f);
            Fired?.Invoke(false);
        }

        void FireCharged()
        {
            if (projectilePrefab == null) { MissingPrefab(); return; }

            // Quantidade e ritmo usam o tamanho de antes do disparo (o que o HUD mostrava).
            int pellets = ChargedPellets;
            float pace = ShotPace;
            if (size != null && !size.TrySpend(chargedCostBars)) { OutOfSize?.Invoke(); return; }

            Quaternion aim = Quaternion.LookRotation(AimDirection());
            for (int i = 0; i < pellets; i++)
            {
                Vector2 offset = i == 0 ? Vector2.zero : UnityEngine.Random.insideUnitCircle * spreadAngle;
                Vector3 dir = aim * (Quaternion.Euler(offset.y, offset.x, 0f) * Vector3.forward);
                Spawn(dir, pelletSpeed * pace, pelletDamage, pelletScale);
            }
            nextQuickTime = Time.time + quickMinInterval / pace;
            Fired?.Invoke(true);
        }

        void Spawn(Vector3 dir, float speed, float damage, float scale)
        {
            SlimeProjectile p = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
            p.transform.localScale *= scale;
            p.Launch(dir * speed, damage, gameObject);
        }

        void MissingPrefab()
        {
            Debug.LogWarning("[PlayerShooter] Nenhum projectilePrefab atribuído.", this);
        }

        Vector3 AimDirection() => (FindAimPoint() - muzzle.position).normalized;

        /// <summary>Ponto onde o centro da tela está mirando (ignorando o próprio player).</summary>
        Vector3 FindAimPoint()
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, AimHits, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore);
            float best = maxAimDistance;
            for (int i = 0; i < count; i++)
            {
                if (AimHits[i].collider.transform.IsChildOf(transform)) continue;
                if (AimHits[i].distance < best) best = AimHits[i].distance;
            }
            return ray.GetPoint(best);
        }
    }
}

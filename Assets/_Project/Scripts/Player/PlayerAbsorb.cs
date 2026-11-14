using System;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Absorver inimigos derrotados: segure o botão olhando para a poça de slime.
    /// Ao completar, recupera vida e gosma e conta para o crescimento do protagonista.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerAbsorb : MonoBehaviour
    {
        [SerializeField] Camera aimCamera;
        [SerializeField] float range = 3.5f;
        [SerializeField] float probeRadius = 0.5f;
        [Tooltip("Ângulo (graus) para achar poças perto dos pés mesmo sem mirar exatamente nelas.")]
        [SerializeField] float proximityAngle = 60f;
        [SerializeField] LayerMask mask = ~0;
        [Tooltip("Multiplicador de velocidade enquanto absorve.")]
        [SerializeField] float moveSpeedWhileAbsorbing = 0.35f;

        static readonly RaycastHit[] CastHits = new RaycastHit[16];
        static readonly Collider[] OverlapHits = new Collider[32];

        PlayerInputReader input;
        PlayerMotor motor;
        Health health;
        SlimeMass mass;

        IAbsorbable target;
        float progress;

        public bool IsAbsorbing => IsAlive(target);
        public float Progress01 => IsAbsorbing ? Mathf.Clamp01(progress) : 0f;
        public int TotalAbsorbed { get; private set; }
        /// <summary>O que o jogador está olhando e pode absorver (para o HUD).</summary>
        public IAbsorbable LookTarget { get; private set; }

        public event Action<IAbsorbable> AbsorbStarted;
        public event Action AbsorbCancelled;
        /// <summary>(recompensa, total absorvido)</summary>
        public event Action<AbsorbReward, int> Absorbed;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            health = GetComponent<Health>();
            mass = GetComponent<SlimeMass>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            LookTarget = FindTarget();

            if (input.AbsorbHeld && LookTarget != null)
            {
                if (!ReferenceEquals(target, LookTarget))
                {
                    Cancel();
                    target = LookTarget;
                    progress = 0f;
                    AbsorbStarted?.Invoke(target);
                }

                progress += Time.deltaTime / Mathf.Max(0.05f, target.AbsorbDuration);
                target.OnAbsorbProgress(Mathf.Clamp01(progress), transform);
                if (progress >= 1f) Complete();
            }
            else if (target != null)
            {
                Cancel();
            }

            if (motor != null) motor.SpeedMultiplier = IsAbsorbing ? moveSpeedWhileAbsorbing : 1f;
        }

        void Complete()
        {
            AbsorbReward reward = target.CompleteAbsorb();
            target = null;
            progress = 0f;
            TotalAbsorbed++;
            if (health != null) health.Heal(reward.Health);
            if (mass != null) mass.Add(reward.Mass);
            Absorbed?.Invoke(reward, TotalAbsorbed);
        }

        void Cancel()
        {
            if (IsAlive(target))
            {
                target.OnAbsorbCancelled();
                AbsorbCancelled?.Invoke();
            }
            target = null;
            progress = 0f;
        }

        IAbsorbable FindTarget()
        {
            // 1) O que está na mira.
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.SphereCastNonAlloc(ray, probeRadius, CastHits, range, mask, QueryTriggerInteraction.Collide);
            IAbsorbable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var a = Absorbable(CastHits[i].collider);
                if (a != null && CastHits[i].distance < bestDist)
                {
                    best = a;
                    bestDist = CastHits[i].distance;
                }
            }
            if (best != null) return best;

            // 2) Poça perto, na frente do jogador (um slime pequeno não precisa olhar pro chão).
            Vector3 forward = Vector3.ProjectOnPlane(aimCamera.transform.forward, Vector3.up).normalized;
            count = Physics.OverlapSphereNonAlloc(transform.position, range, OverlapHits, mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var a = Absorbable(OverlapHits[i]);
                if (a == null) continue;
                Vector3 to = OverlapHits[i].bounds.center - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f && Vector3.Angle(forward, to) > proximityAngle) continue;
                if (to.sqrMagnitude < bestDist)
                {
                    best = a;
                    bestDist = to.sqrMagnitude;
                }
            }
            return best;
        }

        IAbsorbable Absorbable(Collider col)
        {
            if (col.transform.IsChildOf(transform)) return null;
            var a = col.GetComponentInParent<IAbsorbable>();
            return a != null && a.CanBeAbsorbed ? a : null;
        }

        static bool IsAlive(IAbsorbable a) => a is UnityEngine.Object o && o != null;
    }
}

using System;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Absorver inimigos derrotados: o protagonista precisa estar EM CIMA do corpo (poça)
    /// e segurar o botão. Ao completar, recupera vida e aumenta de tamanho.
    /// </summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerAbsorb : MonoBehaviour
    {
        [Tooltip("Raio da área sob o slime que detecta a poça (escala com o tamanho).")]
        [SerializeField] float feetRadius = 0.55f;
        [Tooltip("Altura do centro da detecção acima dos pés.")]
        [SerializeField] float feetHeight = 0.15f;
        [SerializeField] LayerMask mask = ~0;
        [Tooltip("Multiplicador de velocidade enquanto absorve.")]
        [SerializeField] float moveSpeedWhileAbsorbing = 0.35f;

        static readonly Collider[] OverlapHits = new Collider[32];

        PlayerInputReader input;
        PlayerMotor motor;
        PlayerSize size;

        IAbsorbable target;
        float progress;

        public bool IsAbsorbing => IsAlive(target);
        public float Progress01 => IsAbsorbing ? Mathf.Clamp01(progress) : 0f;
        public int TotalAbsorbed { get; private set; }
        /// <summary>Poça debaixo do jogador que pode ser absorvida (para o HUD).</summary>
        public IAbsorbable Available { get; private set; }

        public event Action<IAbsorbable> AbsorbStarted;
        public event Action AbsorbCancelled;
        /// <summary>(recompensa, total absorvido)</summary>
        public event Action<AbsorbReward, int> Absorbed;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            motor = GetComponent<PlayerMotor>();
            size = GetComponent<PlayerSize>();
        }

        void Update()
        {
            Available = FindUnderFeet();

            if (input.AbsorbHeld && Available != null)
            {
                if (!ReferenceEquals(target, Available))
                {
                    Cancel();
                    target = Available;
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
            if (size != null) size.Grow(reward.SizeGain);
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

        IAbsorbable FindUnderFeet()
        {
            float scale = transform.lossyScale.y;
            Vector3 center = transform.position + Vector3.up * (feetHeight * scale);
            int count = Physics.OverlapSphereNonAlloc(center, feetRadius * scale, OverlapHits, mask, QueryTriggerInteraction.Collide);

            IAbsorbable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider col = OverlapHits[i];
                if (col.transform.IsChildOf(transform)) continue;
                var a = col.GetComponentInParent<IAbsorbable>();
                if (a == null || !a.CanBeAbsorbed) continue;

                Vector3 to = col.bounds.center - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < bestDist)
                {
                    best = a;
                    bestDist = to.sqrMagnitude;
                }
            }
            return best;
        }

        static bool IsAlive(IAbsorbable a) => a is UnityEngine.Object o && o != null;

        void OnDrawGizmosSelected()
        {
            float scale = transform.lossyScale.y;
            Gizmos.color = new Color(0.4f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * (feetHeight * scale), feetRadius * scale);
        }
    }
}

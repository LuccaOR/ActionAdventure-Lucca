using SlimeHunter.Combat;
using UnityEngine;

namespace SlimeHunter.Enemies
{
    /// <summary>
    /// Slime ranged: mantém distância do jogador aos saltinhos e ataca inflando o corpo e
    /// vomitando gosma. Aviso: incha e muda de cor. Se o jogador chega perto, foge.
    /// O projétil faz arco e a mira compensa a gravidade e antecipa o movimento do jogador.
    /// </summary>
    public class RangedSlime : SlimeEnemy
    {
        [Header("Distância")]
        [Tooltip("Abaixo dessa distância ele foge do jogador.")]
        [SerializeField] float preferredMin = 6f;
        [Tooltip("Acima dessa distância ele se aproxima.")]
        [SerializeField] float preferredMax = 11f;
        [SerializeField] float attackRange = 16f;
        [SerializeField] float retreatSpeedScale = 0.9f;

        [Header("Ataque: vomita gosma")]
        [SerializeField] SlimeProjectile projectilePrefab;
        [Tooltip("De onde sai a gosma (boca). Vazio = frente do corpo.")]
        [SerializeField] Transform mouth;
        [SerializeField] float projectileSpeed = 16f;
        [SerializeField] float damage = 10f;
        [SerializeField, Min(1)] int projectilesPerAttack = 1;
        [Tooltip("Abertura entre os projéteis quando atira mais de um (graus).")]
        [SerializeField] float volleySpread = 6f;
        [Tooltip("Quanto antecipa o movimento do jogador (0 = mira onde ele está).")]
        [SerializeField, Range(0f, 1f)] float leadFactor = 0.6f;
        [Tooltip("Tempo da 'golfada' depois de atirar.")]
        [SerializeField] float vomitTime = 0.3f;

        public int ShotsFired { get; private set; }

        protected override void TickChase()
        {
            Vector3 to = FlatToPlayer();
            float dist = to.magnitude;
            Face(to);

            if (grounded && AttackReady && dist <= attackRange && CanSeePlayer(attackRange))
            {
                Enter(State.Windup);
                return;
            }

            if (dist > preferredMax) TryHop(to, 1f);                    // aproxima
            else if (dist < preferredMin) TryHop(-to, retreatSpeedScale); // foge
        }

        protected override void TickWindup()
        {
            StopHorizontal();
            Face(FlatToPlayer());
            if (StateTime >= windupTime)
            {
                Fire();
                Enter(State.Attack);
            }
        }

        protected override void TickAttack()
        {
            if (StateTime >= vomitTime) Enter(State.Recover);
        }

        void Fire()
        {
            lastAttackTime = Time.time;
            if (projectilePrefab == null || player == null)
            {
                if (projectilePrefab == null) Debug.LogWarning("[RangedSlime] Sem projectilePrefab.", this);
                return;
            }

            Vector3 origin = mouth != null ? mouth.position : Center + transform.forward * 0.6f * transform.lossyScale.x;
            Vector3 dir = AimDirection(origin);

            for (int i = 0; i < projectilesPerAttack; i++)
            {
                float yaw = projectilesPerAttack == 1 ? 0f : Mathf.Lerp(-volleySpread, volleySpread, i / (projectilesPerAttack - 1f));
                Vector3 d = Quaternion.AngleAxis(yaw, Vector3.up) * dir;
                SlimeProjectile p = Instantiate(projectilePrefab, origin, Quaternion.LookRotation(d));
                p.Launch(d * projectileSpeed, damage, gameObject);
            }
            ShotsFired++;

            // "Golfada": um tranco para trás.
            rb.AddForce(-transform.forward * 2f + Vector3.up * 1.5f, ForceMode.VelocityChange);
        }

        /// <summary>Mira com antecipação do movimento e compensação da queda da gosma.</summary>
        Vector3 AimDirection(Vector3 origin)
        {
            Vector3 target = PlayerAimPoint;
            float t = Vector3.Distance(origin, target) / projectileSpeed;
            if (playerMotor != null)
            {
                Vector3 v = playerMotor.Velocity;
                v.y = 0f;
                target += v * (t * leadFactor);
                t = Vector3.Distance(origin, target) / projectileSpeed;
            }
            target.y += 0.5f * projectilePrefab.Gravity * t * t;
            return (target - origin).normalized;
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(0.3f, 1f, 0.8f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, preferredMin);
            Gizmos.DrawWireSphere(transform.position, preferredMax);
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

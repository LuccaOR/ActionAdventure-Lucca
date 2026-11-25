using UnityEngine;

namespace SlimeHunter.Enemies
{
    /// <summary>
    /// Slime melee: persegue aos saltinhos e ataca arremessando o próprio corpo contra o jogador.
    /// Aviso: se achata e fica vermelho. Ataque: arco balístico que cai em cima do jogador.
    /// </summary>
    public class MeleeSlime : SlimeEnemy
    {
        [Header("Ataque: arremessa o corpo")]
        [SerializeField] float attackRange = 5f;
        [SerializeField] float lungeUp = 4.5f;
        [SerializeField] float lungeMinSpeed = 6f;
        [SerializeField] float lungeMaxSpeed = 14f;
        [SerializeField] float lungeMaxTime = 1.2f;
        [SerializeField] float damage = 15f;
        [SerializeField] float playerKnockback = 8f;
        [Tooltip("Raio (relativo à escala) em que o corpo arremessado acerta o jogador.")]
        [SerializeField] float hitRadius = 0.75f;

        static readonly Collider[] Hits = new Collider[16];

        bool hitThisLunge;
        Vector3 lungeDir;

        protected override void TickChase()
        {
            Vector3 to = FlatToPlayer();
            Face(to);
            if (grounded && to.magnitude <= attackRange && AttackReady)
            {
                Enter(State.Windup);
                return;
            }
            TryHop(to, 1f);
        }

        protected override void TickWindup()
        {
            // Para no lugar e mira: o aviso dá tempo do jogador reagir (o dash atravessa!).
            StopHorizontal();
            Vector3 to = FlatToPlayer();
            Face(to);
            if (StateTime >= windupTime) Launch(to);
        }

        void Launch(Vector3 toPlayer)
        {
            float dist = toPlayer.magnitude;
            lungeDir = dist > 0.01f ? toPlayer / dist : transform.forward;

            // Arco balístico: velocidade horizontal para cair em cima do jogador.
            float g = Mathf.Abs(Physics.gravity.y) * (1f + extraGravity);
            float flightTime = 2f * lungeUp / g;
            float speed = Mathf.Clamp(dist / flightTime, lungeMinSpeed, lungeMaxSpeed);

            rb.linearVelocity = lungeDir * speed + Vector3.up * lungeUp;
            hitThisLunge = false;
            lastAttackTime = Time.time;
            Enter(State.Attack);
        }

        protected override void TickAttack()
        {
            if (!hitThisLunge) TryHitPlayer();
            if ((grounded && StateTime > 0.15f) || StateTime > lungeMaxTime) Enter(State.Recover);
        }

        void TryHitPlayer()
        {
            if (playerSize == null) return;
            float radius = hitRadius * transform.lossyScale.x;
            Vector3 center = Center;
            int n = Physics.OverlapSphereNonAlloc(center, radius, Hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                if (Hits[i].GetComponentInParent<Player.PlayerSize>() != playerSize) continue;

                hitThisLunge = true;
                float before = playerSize.Current;
                playerSize.TakeDamage(damage, Hits[i].ClosestPoint(center), lungeDir);

                // Só empurra se o dano entrou (no dash o jogador é invencível e atravessa).
                if (playerSize.Current < before && playerMotor != null)
                    playerMotor.AddImpulse(lungeDir * playerKnockback + Vector3.up * 3f);

                // Quica para trás depois de acertar.
                rb.linearVelocity = -lungeDir * 3f + Vector3.up * 3f;
                return;
            }
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}

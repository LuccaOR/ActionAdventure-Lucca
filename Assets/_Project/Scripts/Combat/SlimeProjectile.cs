using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Combat
{
    /// <summary>
    /// Bola de gosma. Usada pelo player e reaproveitável pelos slimes ranged e pelo Rei Slime.
    /// Move por SphereCast a cada frame, então não atravessa paredes em alta velocidade.
    /// </summary>
    public class SlimeProjectile : MonoBehaviour
    {
        [SerializeField] float radius = 0.15f;
        [SerializeField] float lifetime = 4f;
        [Tooltip("Gravidade leve para a gosma fazer um arco.")]
        [SerializeField] float gravity = 6f;
        [SerializeField] float impactForce = 4f;
        [SerializeField] LayerMask hitMask = ~0;
        [SerializeField] GameObject impactEffectPrefab;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        Vector3 velocity;
        float damage;
        GameObject owner;
        float spawnTime;

        public GameObject Owner => owner;

        public void Launch(Vector3 initialVelocity, float damageAmount, GameObject shooter)
        {
            velocity = initialVelocity;
            damage = damageAmount;
            owner = shooter;
            spawnTime = Time.time;
            if (velocity.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(velocity);
        }

        void Update()
        {
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float dt = Time.deltaTime;
            velocity += Vector3.down * (gravity * dt);
            Vector3 step = velocity * dt;
            float distance = step.magnitude;
            if (distance <= Mathf.Epsilon) return;

            Vector3 dir = step / distance;
            int count = Physics.SphereCastNonAlloc(transform.position, radius, dir, Hits, distance, hitMask, QueryTriggerInteraction.Ignore);

            int best = -1;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (owner != null && Hits[i].collider.transform.IsChildOf(owner.transform)) continue;
                if (Hits[i].distance < bestDist)
                {
                    bestDist = Hits[i].distance;
                    best = i;
                }
            }

            if (best >= 0)
            {
                Impact(Hits[best], dir);
                return;
            }

            transform.position += step;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        void Impact(RaycastHit hit, Vector3 dir)
        {
            // distance == 0 significa que já começou encostado no collider.
            Vector3 point = hit.distance > 0f ? hit.point : transform.position;
            Vector3 normal = hit.distance > 0f ? hit.normal : -dir;

            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            damageable?.TakeDamage(damage, point, dir);

            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null && !rb.isKinematic) rb.AddForceAtPosition(dir * impactForce, point, ForceMode.Impulse);

            if (impactEffectPrefab != null)
                Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));

            Destroy(gameObject);
        }
    }
}

using System;
using UnityEngine;

namespace SlimeHunter.Core
{
    /// <summary>Vida genérica, usada pelo player e pelos slimes.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] float maxHealth = 100f;
        [Tooltip("Tempo de invencibilidade depois de levar dano (segundos).")]
        [SerializeField, Min(0f)] float invulnerabilityAfterHit = 0f;

        float lastHitTime = float.NegativeInfinity;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public float Normalized => Current / maxHealth;
        public bool IsDead => Current <= 0f;

        /// <summary>Invencibilidade controlada por outros sistemas (ex.: dash).</summary>
        public bool IsInvulnerable { get; set; }

        /// <summary>(atual, máximo)</summary>
        public event Action<float, float> Changed;
        /// <summary>(quantidade, direção do golpe)</summary>
        public event Action<float, Vector3> Damaged;
        public event Action Died;

        void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDead || amount <= 0f || IsInvulnerable) return;
            if (Time.time < lastHitTime + invulnerabilityAfterHit) return;

            lastHitTime = Time.time;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount, hitDirection);
            Changed?.Invoke(Current, maxHealth);
            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void Kill()
        {
            if (IsDead) return;
            Current = 0f;
            Changed?.Invoke(Current, maxHealth);
            Died?.Invoke();
        }

        public void ResetToFull()
        {
            Current = maxHealth;
            lastHitTime = float.NegativeInfinity;
            Changed?.Invoke(Current, maxHealth);
        }
    }
}

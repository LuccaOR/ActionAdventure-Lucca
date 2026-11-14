using System;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// "Gosma": munição do protagonista. Atirar gasta, absorver inimigos recupera,
    /// e regenera devagar sozinho. Liga o combate ao loop de absorção.
    /// </summary>
    public class SlimeMass : MonoBehaviour
    {
        [SerializeField, Min(1f)] float maxMass = 100f;
        [SerializeField] float regenPerSecond = 6f;
        [Tooltip("Espera depois de gastar antes de começar a regenerar.")]
        [SerializeField] float regenDelay = 1.2f;

        float lastSpendTime = float.NegativeInfinity;

        public float Max => maxMass;
        public float Current { get; private set; }
        public float Normalized => Current / maxMass;

        /// <summary>(atual, máximo)</summary>
        public event Action<float, float> Changed;

        void Awake() => Current = maxMass;

        void Update()
        {
            if (Current < maxMass && Time.time - lastSpendTime >= regenDelay)
                Set(Current + regenPerSecond * Time.deltaTime);
        }

        public bool TrySpend(float amount)
        {
            if (Current < amount) return false;
            lastSpendTime = Time.time;
            Set(Current - amount);
            return true;
        }

        public void Add(float amount)
        {
            if (amount > 0f) Set(Current + amount);
        }

        public void ResetToFull() => Set(maxMass);

        void Set(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxMass);
            if (Mathf.Approximately(clamped, Current)) return;
            Current = clamped;
            Changed?.Invoke(Current, maxMass);
        }
    }
}

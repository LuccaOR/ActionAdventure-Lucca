using System;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Vida = tamanho. O tamanho é medido em barras: levar dano e atirar diminuem,
    /// absorver inimigos aumenta. O corpo (e a câmera, que é filha dele) escala junto.
    /// Quando chega a zero, dispara Depleted (o PlayerController tira uma vida).
    /// </summary>
    public class PlayerSize : MonoBehaviour, IDamageable
    {
        [Header("Tamanho (em barras)")]
        [SerializeField, Min(0.1f)] float startSize = 3f;
        [SerializeField, Min(0.1f)] float maxSize = 5f;
        [Tooltip("Quanto dano (de inimigos) equivale a 1 barra de tamanho.")]
        [SerializeField, Min(0.01f)] float damagePerBar = 20f;
        [SerializeField, Min(0f)] float invulnerabilityAfterHit = 0.6f;

        [Header("Escala visual")]
        [Tooltip("Escala do corpo quando o tamanho está quase zerando.")]
        [SerializeField] float minScale = 0.45f;
        [Tooltip("Escala no tamanho máximo (tamanho inicial = escala 1).")]
        [SerializeField] float maxScale = 1.5f;
        [SerializeField] float scaleLerpSpeed = 8f;

        float lastHitTime = float.NegativeInfinity;
        float invulnerableUntil = float.NegativeInfinity;
        float visualScale = 1f;

        public float Current { get; private set; }
        public float Max => maxSize;
        public float Start => startSize;
        public bool IsDepleted { get; private set; }
        public float VisualScale => visualScale;

        /// <summary>Invencibilidade controlada por outros sistemas (ex.: dash).</summary>
        public bool IsInvulnerable { get; set; }

        /// <summary>(atual, máximo)</summary>
        public event Action<float, float> Changed;
        /// <summary>(barras perdidas, direção do golpe)</summary>
        public event Action<float, Vector3> Damaged;
        public event Action Depleted;

        void Awake()
        {
            Current = startSize;
            visualScale = ScaleFor(Current);
            transform.localScale = Vector3.one * visualScale;
        }

        void Update()
        {
            float target = ScaleFor(Current);
            visualScale = Mathf.Lerp(visualScale, target, 1f - Mathf.Exp(-scaleLerpSpeed * Time.deltaTime));
            transform.localScale = Vector3.one * visualScale;
        }

        /// <summary>Escala alvo para o tamanho atual (sem a suavização visual).</summary>
        public float TargetScale => ScaleFor(Current);

        /// <summary>
        /// Interpola um modificador de atributo pelo tamanho:
        /// menor tamanho = atSmallest, tamanho inicial = 1, maior tamanho = atLargest.
        /// Ex.: Modifier(1.35f, 0.7f) -> mais rápido pequeno, mais lento grande.
        /// </summary>
        public float Modifier(float atSmallest, float atLargest)
        {
            float s = TargetScale;
            if (s <= 1f) return Mathf.Lerp(atSmallest, 1f, Mathf.InverseLerp(minScale, 1f, s));
            return Mathf.Lerp(1f, atLargest, Mathf.InverseLerp(1f, maxScale, s));
        }

        float ScaleFor(float size)
        {
            if (size <= startSize) return Mathf.Lerp(minScale, 1f, Mathf.Clamp01(size / startSize));
            return Mathf.Lerp(1f, maxScale, Mathf.Clamp01((size - startSize) / Mathf.Max(0.01f, maxSize - startSize)));
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection)
        {
            if (IsDepleted || amount <= 0f || IsInvulnerable || Time.time < invulnerableUntil) return;
            if (Time.time < lastHitTime + invulnerabilityAfterHit) return;

            lastHitTime = Time.time;
            float bars = amount / damagePerBar;
            Set(Current - bars);
            Damaged?.Invoke(bars, hitDirection);
            if (Current <= 0f) Deplete();
        }

        /// <summary>Gasta tamanho (tiros). Nunca zera o slime: precisa sobrar minRemaining.</summary>
        public bool TrySpend(float bars, float minRemaining = 0.05f)
        {
            if (IsDepleted || Current - bars < minRemaining) return false;
            Set(Current - bars);
            return true;
        }

        public bool CanSpend(float bars, float minRemaining = 0.05f) => !IsDepleted && Current - bars >= minRemaining;

        /// <summary>Recupera/aumenta tamanho (absorção, itens).</summary>
        public void Grow(float bars)
        {
            if (IsDepleted || bars <= 0f) return;
            Set(Current + bars);
        }

        public void Kill()
        {
            if (IsDepleted) return;
            Set(0f);
            Deplete();
        }

        public void ResetToStart()
        {
            IsDepleted = false;
            lastHitTime = float.NegativeInfinity;
            Set(startSize);
        }

        public void SetInvulnerableFor(float seconds) => invulnerableUntil = Time.time + seconds;

        void Deplete()
        {
            IsDepleted = true;
            Depleted?.Invoke();
        }

        void Set(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, maxSize);
            if (Mathf.Approximately(clamped, Current)) return;
            Current = clamped;
            Changed?.Invoke(Current, maxSize);
        }
    }
}

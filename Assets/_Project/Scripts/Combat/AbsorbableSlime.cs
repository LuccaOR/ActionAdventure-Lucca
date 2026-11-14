using System.Collections;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Combat
{
    /// <summary>
    /// Vai em qualquer slime inimigo (inclusive o Rei Slime).
    /// Quando a vida zera, o corpo vira uma poça; a poça pode ser absorvida pelo protagonista.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class AbsorbableSlime : MonoBehaviour, IAbsorbable
    {
        [Header("Absorção")]
        [SerializeField] float absorbDuration = 1.2f;
        [SerializeField] float healthReward = 20f;
        [SerializeField] float massReward = 30f;

        [Header("Poça (morte)")]
        [SerializeField] Vector3 puddleScaleMultiplier = new Vector3(1.5f, 0.2f, 1.5f);
        [SerializeField] float puddleTime = 0.3f;
        [Tooltip("Componentes desligados ao morrer (IA, NavMeshAgent, ataques...).")]
        [SerializeField] Behaviour[] disableOnDeath;
        [Tooltip("0 = a poça fica até ser absorvida.")]
        [SerializeField] float despawnAfter = 0f;

        Health health;
        Collider[] colliders;
        Vector3 aliveScale;
        Vector3 puddleScale;
        float baseHeight;
        float groundY;
        bool isPuddle;
        bool absorbed;

        public bool CanBeAbsorbed => isPuddle && !absorbed;
        public float AbsorbDuration => absorbDuration;

        void Awake()
        {
            health = GetComponent<Health>();
            colliders = GetComponentsInChildren<Collider>();
            aliveScale = transform.localScale;
        }

        void OnEnable() => health.Died += OnDied;
        void OnDisable() => health.Died -= OnDied;

        void OnDied()
        {
            if (disableOnDeath != null)
                foreach (var b in disableOnDeath) if (b != null) b.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Bounds b0 = ComputeBounds();
            baseHeight = Mathf.Max(0.01f, b0.size.y);
            groundY = b0.min.y;
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

            // Poça é atravessável (trigger), mas continua detectável pela absorção.
            foreach (var c in colliders) c.isTrigger = true;

            puddleScale = Vector3.Scale(aliveScale, puddleScaleMultiplier);
            StartCoroutine(BecomePuddle());
        }

        IEnumerator BecomePuddle()
        {
            Vector3 from = transform.localScale;
            for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(0.01f, puddleTime))
            {
                // Overshoot para dar o "splash" de gosma.
                float e = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
                SetScaleKeepingBottom(Vector3.LerpUnclamped(from, puddleScale, e));
                yield return null;
            }
            SetScaleKeepingBottom(puddleScale);
            isPuddle = true;

            if (despawnAfter > 0f) Destroy(gameObject, despawnAfter);
        }

        void SetScaleKeepingBottom(Vector3 scale)
        {
            transform.localScale = scale;
            float h = baseHeight * (scale.y / Mathf.Max(0.0001f, aliveScale.y));
            Vector3 p = transform.position;
            p.y = groundY + h * 0.5f; // assume pivô no centro (primitivas / modelos centralizados)
            transform.position = p;
        }

        Bounds ComputeBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, transform.lossyScale);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }

        public void OnAbsorbProgress(float progress01, Transform absorber)
        {
            if (!CanBeAbsorbed) return;
            // Encolhe e treme enquanto é sugado.
            float shrink = Mathf.Lerp(1f, 0.15f, progress01);
            float wobble = 1f + Mathf.Sin(Time.time * 40f) * 0.06f * progress01;
            transform.localScale = new Vector3(puddleScale.x * shrink * wobble, puddleScale.y, puddleScale.z * shrink / wobble);
        }

        public void OnAbsorbCancelled()
        {
            if (!absorbed) transform.localScale = puddleScale;
        }

        public AbsorbReward CompleteAbsorb()
        {
            absorbed = true;
            Destroy(gameObject);
            return new AbsorbReward(healthReward, massReward);
        }
    }
}

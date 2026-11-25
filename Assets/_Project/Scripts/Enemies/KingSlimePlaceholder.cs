using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Enemies
{
    /// <summary>
    /// Rei Slime PROVISÓRIO: corpo grande com coroa, muita vida e respiração (squash & stretch),
    /// sem ataques. A IA com os 3 ataques do GDD (vômito de projéteis, salto com dano em área,
    /// esmagar) vem numa tarefa separada. Ao morrer vira poça (AbsorbableSlime) para o final.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class KingSlimePlaceholder : MonoBehaviour
    {
        [SerializeField] Transform body;
        [SerializeField] float breatheSpeed = 1.6f;
        [SerializeField] float breatheAmount = 0.06f;

        Vector3 baseScale;
        Health health;

        void Awake()
        {
            health = GetComponent<Health>();
            if (body != null) baseScale = body.localScale;
        }

        void Update()
        {
            if (body == null || health.IsDead) return;
            float s = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            body.localScale = new Vector3(baseScale.x * (1f + s), baseScale.y * (1f - s), baseScale.z * (1f + s));
        }

        void OnDisable()
        {
            if (body != null && baseScale != Vector3.zero) body.localScale = baseScale;
        }
    }
}

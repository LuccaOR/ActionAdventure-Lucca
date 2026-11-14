using System;
using SlimeHunter.Combat;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>Ataque com projéteis de gosma, mirando pelo centro da tela. Gasta SlimeMass.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerShooter : MonoBehaviour
    {
        [SerializeField] SlimeProjectile projectilePrefab;
        [SerializeField] Transform muzzle;
        [SerializeField] Camera aimCamera;

        [Header("Tiro")]
        [Tooltip("Tiros por segundo segurando o botão.")]
        [SerializeField] float fireRate = 5f;
        [SerializeField] float projectileSpeed = 40f;
        [SerializeField] float damage = 12f;
        [SerializeField] float massCost = 4f;
        [SerializeField] float maxAimDistance = 150f;
        [SerializeField] LayerMask aimMask = ~0;

        static readonly RaycastHit[] AimHits = new RaycastHit[16];

        PlayerInputReader input;
        SlimeMass mass;
        PlayerAbsorb absorb;
        float nextFireTime;

        public event Action Fired;
        /// <summary>Tentou atirar sem gosma suficiente.</summary>
        public event Action OutOfMass;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            mass = GetComponent<SlimeMass>();
            absorb = GetComponent<PlayerAbsorb>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
            if (muzzle == null && aimCamera != null) muzzle = aimCamera.transform;
        }

        void Update()
        {
            if (!input.FireHeld || Time.time < nextFireTime) return;
            if (absorb != null && absorb.IsAbsorbing) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;
            TryFire();
        }

        void TryFire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[PlayerShooter] Nenhum projectilePrefab atribuído.", this);
                nextFireTime = Time.time + 1f;
                return;
            }

            if (mass != null && !mass.TrySpend(massCost))
            {
                nextFireTime = Time.time + 0.25f;
                OutOfMass?.Invoke();
                return;
            }

            nextFireTime = Time.time + 1f / fireRate;

            Vector3 target = FindAimPoint();
            Vector3 dir = (target - muzzle.position).normalized;
            SlimeProjectile p = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(dir));
            p.Launch(dir * projectileSpeed, damage, gameObject);
            Fired?.Invoke();
        }

        /// <summary>Ponto onde o centro da tela está mirando (ignorando o próprio player).</summary>
        Vector3 FindAimPoint()
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, AimHits, maxAimDistance, aimMask, QueryTriggerInteraction.Ignore);
            float best = maxAimDistance;
            for (int i = 0; i < count; i++)
            {
                if (AimHits[i].collider.transform.IsChildOf(transform)) continue;
                if (AimHits[i].distance < best) best = AimHits[i].distance;
            }
            return ray.GetPoint(best);
        }
    }
}

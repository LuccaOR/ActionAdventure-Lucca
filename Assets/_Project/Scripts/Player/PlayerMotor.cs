using System;
using System.Collections.Generic;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Movimento do protagonista: andar, pular (com coyote time e buffer), pulo duplo,
    /// dash (chão e ar) e Impacto (cai com força e causa dano em área ao aterrissar).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Andar")]
        [SerializeField] float moveSpeed = 7f;
        [SerializeField] float groundAcceleration = 70f;
        [SerializeField] float groundDeceleration = 90f;
        [SerializeField] float airAcceleration = 25f;

        [Header("Pulo")]
        [SerializeField] float jumpHeight = 1.8f;
        [SerializeField] float doubleJumpHeight = 1.4f;
        [SerializeField, Min(0)] int maxAirJumps = 1;
        [SerializeField] float gravity = -28f;
        [Tooltip("Gravidade extra ao cair, pulo fica mais 'pesado' e gostoso.")]
        [SerializeField] float fallGravityMultiplier = 1.6f;
        [Tooltip("Gravidade extra ao soltar o botão de pulo cedo (pulo curto).")]
        [SerializeField] float shortHopGravityMultiplier = 2.2f;
        [SerializeField] float maxFallSpeed = 45f;
        [SerializeField] float coyoteTime = 0.12f;
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Dash")]
        [SerializeField] float dashSpeed = 24f;
        [SerializeField] float dashDuration = 0.18f;
        [SerializeField] float dashCooldown = 0.55f;
        [SerializeField, Min(0)] int maxAirDashes = 1;
        [SerializeField] bool dashGrantsInvulnerability = true;

        [Header("Impacto (ground slam)")]
        [SerializeField] float slamSpeed = 40f;
        [Tooltip("Tempo mínimo no ar antes de poder usar o impacto.")]
        [SerializeField] float slamMinAirTime = 0.15f;
        [SerializeField] float slamRadius = 4f;
        [SerializeField] float slamBaseDamage = 25f;
        [Tooltip("Dano extra por metro de queda.")]
        [SerializeField] float slamDamagePerMeter = 3f;
        [SerializeField] float slamMaxDamage = 80f;
        [SerializeField] float slamKnockback = 10f;
        [SerializeField] float slamBounceHeight = 0.6f;
        [SerializeField] LayerMask slamHitMask = ~0;

        CharacterController controller;
        PlayerInputReader input;
        Health health;

        Vector3 planarVelocity;
        float verticalVelocity;
        Vector3 externalVelocity;

        bool isGrounded;
        float airTime;
        float coyoteTimer;
        float jumpBufferTimer;
        int airJumpsUsed;
        int airDashesUsed;

        float dashTimer;
        float dashCooldownTimer;
        Vector3 dashDirection;

        bool isSlamming;
        float slamStartHeight;

        readonly Collider[] slamHits = new Collider[32];
        readonly HashSet<object> slamProcessed = new HashSet<object>();

        /// <summary>true = pulo duplo.</summary>
        public event Action<bool> Jumped;
        /// <summary>Velocidade de queda no momento do pouso (positiva).</summary>
        public event Action<float> Landed;
        public event Action Dashed;
        public event Action SlamStarted;
        /// <summary>(posição, dano aplicado)</summary>
        public event Action<Vector3, float> SlamImpact;

        public bool IsGrounded => isGrounded;
        public bool IsDashing => dashTimer > 0f;
        public bool IsSlamming => isSlamming;
        public int AirJumpsRemaining => Mathf.Max(0, maxAirJumps - airJumpsUsed);
        public Vector3 Velocity => planarVelocity + Vector3.up * verticalVelocity + externalVelocity;
        public float DashReady01 => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(dashCooldownTimer / dashCooldown);
        public float SlamRadius => slamRadius;

        /// <summary>Multiplicador de velocidade (ex.: mais lento enquanto absorve).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
        }

        void Update()
        {
            float dt = Time.deltaTime;

            // Timers
            jumpBufferTimer = input.JumpPressed ? jumpBufferTime : jumpBufferTimer - dt;
            coyoteTimer = isGrounded ? coyoteTime : coyoteTimer - dt;
            if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;

            // Ações
            if (input.DashPressed) TryDash();
            if (input.SlamPressed) TrySlam();
            if (jumpBufferTimer > 0f && !isSlamming) TryJump();

            UpdateVelocity(dt);

            float verticalBeforeMove = verticalVelocity;
            CollisionFlags flags = controller.Move(Velocity * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;

            bool wasGrounded = isGrounded;
            isGrounded = controller.isGrounded;
            if (isGrounded && !wasGrounded) HandleLanding(-verticalBeforeMove);

            if (isGrounded)
            {
                airTime = 0f;
                airJumpsUsed = 0;
                airDashesUsed = 0;
                if (verticalVelocity < 0f) verticalVelocity = -2f; // mantém colado no chão
            }
            else
            {
                airTime += dt;
            }

            if (health != null && dashGrantsInvulnerability) health.IsInvulnerable = IsDashing;
        }

        Vector3 WishDirection()
        {
            Vector2 m = input.Move;
            Vector3 wish = transform.right * m.x + transform.forward * m.y;
            wish.y = 0f;
            return Vector3.ClampMagnitude(wish, 1f);
        }

        void UpdateVelocity(float dt)
        {
            if (IsDashing)
            {
                dashTimer -= dt;
                planarVelocity = dashDirection * dashSpeed;
                verticalVelocity = 0f;
                if (dashTimer <= 0f) planarVelocity = dashDirection * moveSpeed * SpeedMultiplier;
            }
            else if (isSlamming)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 60f * dt);
                verticalVelocity = -slamSpeed;
            }
            else
            {
                Vector3 wish = WishDirection();
                Vector3 target = wish * (moveSpeed * SpeedMultiplier);
                float accel = isGrounded
                    ? (wish.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration)
                    : airAcceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, target, accel * dt);

                float g = gravity;
                if (verticalVelocity < 0f) g *= fallGravityMultiplier;
                else if (verticalVelocity > 0f && !input.JumpHeld) g *= shortHopGravityMultiplier;
                verticalVelocity = Mathf.Max(verticalVelocity + g * dt, -maxFallSpeed);
            }

            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, (isGrounded ? 30f : 8f) * dt);
        }

        void TryJump()
        {
            if (coyoteTimer > 0f)
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
                coyoteTimer = 0f;
                Jumped?.Invoke(false);
            }
            else if (airJumpsUsed < maxAirJumps)
            {
                airJumpsUsed++;
                verticalVelocity = Mathf.Sqrt(2f * doubleJumpHeight * -gravity);
                Jumped?.Invoke(true);
            }
            else
            {
                return; // mantém o buffer: se pousar logo em seguida, pula
            }

            jumpBufferTimer = 0f;
            if (IsDashing)
            {
                // Pular durante o dash cancela o dash mas mantém um pouco do embalo.
                dashTimer = 0f;
                planarVelocity = dashDirection * moveSpeed * 1.25f;
            }
        }

        void TryDash()
        {
            if (dashCooldownTimer > 0f || IsDashing || isSlamming) return;
            if (!isGrounded)
            {
                if (airDashesUsed >= maxAirDashes) return;
                airDashesUsed++;
            }

            Vector3 wish = WishDirection();
            dashDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            verticalVelocity = 0f;
            Dashed?.Invoke();
        }

        void TrySlam()
        {
            if (isGrounded || isSlamming || airTime < slamMinAirTime) return;
            isSlamming = true;
            dashTimer = 0f;
            slamStartHeight = transform.position.y;
            SlamStarted?.Invoke();
        }

        void HandleLanding(float impactSpeed)
        {
            Landed?.Invoke(Mathf.Max(0f, impactSpeed));
            if (isSlamming) DoSlamImpact();
        }

        void DoSlamImpact()
        {
            isSlamming = false;

            Vector3 center = transform.position;
            float fall = Mathf.Max(0f, slamStartHeight - center.y);
            float damage = Mathf.Min(slamMaxDamage, slamBaseDamage + fall * slamDamagePerMeter);

            slamProcessed.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, slamRadius, slamHits, slamHitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = slamHits[i];
                if (col.transform.IsChildOf(transform)) continue;

                Vector3 closest = col.ClosestPoint(center);
                Vector3 dir = col.bounds.center - center;
                dir.y = Mathf.Max(dir.y, 0.2f);
                dir.Normalize();

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && slamProcessed.Add(damageable))
                {
                    float falloff = Mathf.Lerp(1f, 0.5f, Vector3.Distance(center, closest) / slamRadius);
                    damageable.TakeDamage(damage * falloff, closest, dir);
                }

                Rigidbody rb = col.attachedRigidbody;
                if (rb != null && !rb.isKinematic && slamProcessed.Add(rb))
                    rb.AddExplosionForce(slamKnockback, center, slamRadius, 0.5f, ForceMode.VelocityChange);
            }

            verticalVelocity = Mathf.Sqrt(2f * slamBounceHeight * -gravity);
            SlamImpact?.Invoke(center, damage);
        }

        /// <summary>Empurrão externo (knockback, plataformas de salto).</summary>
        public void AddImpulse(Vector3 velocityChange)
        {
            externalVelocity += new Vector3(velocityChange.x, 0f, velocityChange.z);
            verticalVelocity += velocityChange.y;
            isSlamming = false;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            planarVelocity = externalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            dashTimer = 0f;
            isSlamming = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, slamRadius);
        }
    }
}

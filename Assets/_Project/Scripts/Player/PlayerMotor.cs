using System;
using System.Collections.Generic;
using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>
    /// Movimento do protagonista:
    /// - Movimentação por saltinhos consecutivos (slime não anda).
    /// - Pulo (com coyote time e buffer) e pulo duplo só no ar.
    /// - Dash que atravessa obstáculos/inimigos das camadas em dashPassThroughMask.
    /// - Impacto: cai com força, causa dano em área e afasta inimigos próximos.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Movimento por saltinhos")]
        [SerializeField] float moveSpeed = 6f;
        [Tooltip("Altura de cada saltinho de locomoção.")]
        [SerializeField] float hopHeight = 0.35f;
        [Tooltip("Tempo encostado no chão entre um saltinho e outro.")]
        [SerializeField] float hopGroundTime = 0.04f;
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
        [Tooltip("Camadas que o dash atravessa (barreiras, inimigos).")]
        [SerializeField] LayerMask dashPassThroughMask;
        [Tooltip("Se o dash terminar dentro de um obstáculo, continua por até esse tempo para sair dele.")]
        [SerializeField] float dashMaxOverrun = 0.3f;

        [Header("Impacto (ground slam)")]
        [SerializeField] float slamSpeed = 40f;
        [Tooltip("Tempo mínimo no ar antes de poder usar o impacto.")]
        [SerializeField] float slamMinAirTime = 0.15f;
        [SerializeField] float slamRadius = 4f;
        [SerializeField] float slamBaseDamage = 25f;
        [Tooltip("Dano extra por metro de queda.")]
        [SerializeField] float slamDamagePerMeter = 3f;
        [SerializeField] float slamMaxDamage = 80f;
        [Tooltip("Velocidade com que os inimigos são afastados.")]
        [SerializeField] float slamKnockback = 12f;
        [SerializeField] float slamKnockbackUp = 4f;
        [SerializeField] float slamBounceHeight = 0.6f;
        [SerializeField] LayerMask slamHitMask = ~0;

        [Header("Efeito do tamanho (1 = tamanho inicial)")]
        [Tooltip("Multiplicador de velocidade (andar e dash) no menor tamanho.")]
        [SerializeField] float speedAtSmallest = 1.35f;
        [Tooltip("Multiplicador de velocidade (andar e dash) no maior tamanho.")]
        [SerializeField] float speedAtLargest = 0.7f;
        [Tooltip("Multiplicador de dano e empurrão do impacto no menor tamanho.")]
        [SerializeField] float slamPowerAtSmallest = 0.6f;
        [Tooltip("Multiplicador de dano e empurrão do impacto no maior tamanho.")]
        [SerializeField] float slamPowerAtLargest = 1.8f;
        [Tooltip("Multiplicador do raio do impacto no menor tamanho.")]
        [SerializeField] float slamRadiusAtSmallest = 0.75f;
        [Tooltip("Multiplicador do raio do impacto no maior tamanho.")]
        [SerializeField] float slamRadiusAtLargest = 1.4f;

        CharacterController controller;
        PlayerInputReader input;
        PlayerSize size;

        Vector3 planarVelocity;
        float verticalVelocity;
        Vector3 externalVelocity;

        bool isGrounded;
        bool hopAirborne;
        float groundedTime;
        float airTime;
        float coyoteTimer;
        float jumpBufferTimer;
        int airJumpsUsed;
        int airDashesUsed;

        float dashTimer;
        float dashCooldownTimer;
        float dashOverrun;
        Vector3 dashDirection;
        bool dashIgnoringLayers;

        bool isSlamming;
        float slamStartHeight;

        readonly Collider[] slamHits = new Collider[32];
        readonly Collider[] overlapHits = new Collider[8];
        readonly HashSet<object> slamProcessed = new HashSet<object>();

        /// <summary>true = pulo duplo.</summary>
        public event Action<bool> Jumped;
        /// <summary>Saltinho de locomoção.</summary>
        public event Action Hopped;
        /// <summary>Velocidade de queda no momento do pouso (positiva).</summary>
        public event Action<float> Landed;
        public event Action Dashed;
        public event Action SlamStarted;
        /// <summary>(posição, dano aplicado)</summary>
        public event Action<Vector3, float> SlamImpact;

        public bool IsGrounded => isGrounded;
        public bool IsHopping => hopAirborne;
        public bool IsDashing => dashTimer > 0f;
        public bool IsSlamming => isSlamming;
        public int AirJumpsRemaining => Mathf.Max(0, maxAirJumps - airJumpsUsed);
        public Vector3 Velocity => planarVelocity + Vector3.up * verticalVelocity + externalVelocity;
        public float DashReady01 => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(dashCooldownTimer / dashCooldown);
        public float SlamRadius => slamRadius * SizeSlamRadius;

        /// <summary>Grande = lento, pequeno = rápido.</summary>
        public float SizeSpeed => size != null ? size.Modifier(speedAtSmallest, speedAtLargest) : 1f;
        /// <summary>Grande = impacto mais forte, pequeno = mais fraco.</summary>
        public float SizeSlamPower => size != null ? size.Modifier(slamPowerAtSmallest, slamPowerAtLargest) : 1f;
        public float SizeSlamRadius => size != null ? size.Modifier(slamRadiusAtSmallest, slamRadiusAtLargest) : 1f;

        /// <summary>Multiplicador de velocidade (ex.: mais lento enquanto absorve).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            input = GetComponent<PlayerInputReader>();
            size = GetComponent<PlayerSize>();
        }

        void OnDisable() => SetDashPassThrough(false);

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
            TryHop();

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
                groundedTime += dt;
                hopAirborne = false;
                airJumpsUsed = 0;
                airDashesUsed = 0;
                if (verticalVelocity < 0f) verticalVelocity = -2f; // mantém colado no chão
            }
            else
            {
                airTime += dt;
                groundedTime = 0f;
            }

            if (size != null && dashGrantsInvulnerability) size.IsInvulnerable = IsDashing;
        }

        Vector3 WishDirection()
        {
            Vector2 m = input.Move;
            Vector3 wish = transform.right * m.x + transform.forward * m.y;
            wish.y = 0f;
            return Vector3.ClampMagnitude(wish, 1f);
        }

        // ---------- Locomoção por saltinhos ----------

        void TryHop()
        {
            if (!isGrounded || IsDashing || isSlamming || verticalVelocity > 0f) return;
            if (jumpBufferTimer > 0f || groundedTime < hopGroundTime) return;
            if (WishDirection().sqrMagnitude < 0.01f) return;

            verticalVelocity = Mathf.Sqrt(2f * hopHeight * -gravity);
            hopAirborne = true;
            Hopped?.Invoke();
        }

        void UpdateVelocity(float dt)
        {
            if (IsDashing)
            {
                dashTimer -= dt;
                planarVelocity = dashDirection * (dashSpeed * SizeSpeed);
                verticalVelocity = 0f;
                if (dashTimer <= 0f)
                {
                    // Terminou dentro de uma barreira? Continua um pouco para sair dela.
                    if (dashIgnoringLayers && dashOverrun < dashMaxOverrun && OverlapsPassThrough())
                    {
                        dashTimer = 0.02f;
                        dashOverrun += 0.02f;
                    }
                    else
                    {
                        EndDash();
                    }
                }
            }
            else if (isSlamming)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, 60f * dt);
                verticalVelocity = -slamSpeed;
            }
            else
            {
                Vector3 wish = WishDirection();
                Vector3 target = wish * (moveSpeed * SpeedMultiplier * SizeSpeed);
                float accel;
                if (isGrounded || hopAirborne)
                    accel = wish.sqrMagnitude > 0.01f ? groundAcceleration : groundDeceleration;
                else
                    accel = airAcceleration;
                planarVelocity = Vector3.MoveTowards(planarVelocity, target, accel * dt);

                float g = gravity;
                if (verticalVelocity < 0f) g *= fallGravityMultiplier;
                else if (verticalVelocity > 0f && !hopAirborne && !input.JumpHeld) g *= shortHopGravityMultiplier;
                verticalVelocity = Mathf.Max(verticalVelocity + g * dt, -maxFallSpeed);
            }

            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero, (isGrounded ? 30f : 8f) * dt);
        }

        // ---------- Pulo ----------

        void TryJump()
        {
            if (IsDashing && OverlapsPassThrough()) return; // não cancela o dash dentro de uma barreira

            // Saltinho de locomoção conta como "no chão": o pulo de verdade sai normal.
            if (coyoteTimer > 0f || hopAirborne)
            {
                verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
                coyoteTimer = 0f;
                hopAirborne = false;
                Jumped?.Invoke(false);
            }
            else if (airJumpsUsed < maxAirJumps)
            {
                // Pulo duplo: só no ar.
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
                EndDash();
                planarVelocity = dashDirection * (moveSpeed * SizeSpeed * 1.25f);
            }
        }

        // ---------- Dash ----------

        void TryDash()
        {
            if (dashCooldownTimer > 0f || IsDashing || isSlamming) return;
            if (!isGrounded && !hopAirborne)
            {
                if (airDashesUsed >= maxAirDashes) return;
                airDashesUsed++;
            }

            Vector3 wish = WishDirection();
            dashDirection = wish.sqrMagnitude > 0.01f ? wish.normalized : transform.forward;
            dashTimer = dashDuration;
            dashOverrun = 0f;
            dashCooldownTimer = dashCooldown;
            verticalVelocity = 0f;
            hopAirborne = false;
            SetDashPassThrough(true);
            Dashed?.Invoke();
        }

        void EndDash()
        {
            dashTimer = 0f;
            planarVelocity = dashDirection * (moveSpeed * SpeedMultiplier * SizeSpeed);
            SetDashPassThrough(false);
        }

        void SetDashPassThrough(bool on)
        {
            if (dashIgnoringLayers == on || dashPassThroughMask.value == 0) return;
            dashIgnoringLayers = on;
            controller.excludeLayers = on ? dashPassThroughMask : (LayerMask)0;

            // Garantia extra pela matriz de colisão (só se o player tiver camada própria).
            int playerLayer = gameObject.layer;
            if (playerLayer == 0) return;
            for (int layer = 0; layer < 32; layer++)
                if ((dashPassThroughMask.value & (1 << layer)) != 0)
                    Physics.IgnoreLayerCollision(playerLayer, layer, on);
        }

        bool OverlapsPassThrough()
        {
            float scale = transform.lossyScale.y;
            float radius = controller.radius * scale;
            float half = Mathf.Max(0f, controller.height * 0.5f * scale - radius);
            Vector3 center = transform.TransformPoint(controller.center);
            Vector3 a = center + Vector3.up * half, b = center - Vector3.up * half;
            int n = Physics.OverlapCapsuleNonAlloc(a, b, radius * 0.95f, overlapHits, dashPassThroughMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!overlapHits[i].transform.IsChildOf(transform)) return true;
            return false;
        }

        // ---------- Impacto ----------

        void TrySlam()
        {
            if (isGrounded || isSlamming || hopAirborne || airTime < slamMinAirTime) return;
            if (IsDashing)
            {
                if (OverlapsPassThrough()) return;
                EndDash();
            }
            isSlamming = true;
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
            float power = SizeSlamPower;
            float radius = SlamRadius;
            float damage = Mathf.Min(slamMaxDamage, slamBaseDamage + fall * slamDamagePerMeter) * power;

            slamProcessed.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, slamHits, slamHitMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = slamHits[i];
                if (col.transform.IsChildOf(transform)) continue;

                Vector3 closest = col.ClosestPoint(center);
                Vector3 away = col.bounds.center - center;
                away.y = 0f;
                away = away.sqrMagnitude > 0.001f ? away.normalized : transform.forward;
                float falloff = Mathf.Lerp(1f, 0.5f, Vector3.Distance(center, closest) / radius);

                var damageable = col.GetComponentInParent<IDamageable>();
                if (damageable != null && slamProcessed.Add(damageable))
                    damageable.TakeDamage(damage * falloff, closest, away);

                // Afasta inimigos próximos.
                Vector3 push = (away * (slamKnockback * falloff) + Vector3.up * slamKnockbackUp) * power;
                var knock = col.GetComponentInParent<IKnockbackable>();
                if (knock != null)
                {
                    if (slamProcessed.Add(knock)) knock.ApplyKnockback(push);
                }
                else
                {
                    Rigidbody rb = col.attachedRigidbody;
                    if (rb != null && !rb.isKinematic && slamProcessed.Add(rb))
                        rb.AddForce(push, ForceMode.VelocityChange);
                }
            }

            verticalVelocity = Mathf.Sqrt(2f * slamBounceHeight * -gravity);
            SlamImpact?.Invoke(center, damage);
        }

        // ---------- Externo ----------

        /// <summary>Empurrão externo (knockback, plataformas de salto).</summary>
        public void AddImpulse(Vector3 velocityChange)
        {
            externalVelocity += new Vector3(velocityChange.x, 0f, velocityChange.z);
            verticalVelocity += velocityChange.y;
            isSlamming = false;
            hopAirborne = false;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            planarVelocity = externalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            dashTimer = 0f;
            SetDashPassThrough(false);
            isSlamming = false;
            hopAirborne = false;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, Application.isPlaying ? SlamRadius : slamRadius);
        }
    }
}

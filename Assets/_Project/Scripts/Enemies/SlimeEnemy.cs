using SlimeHunter.Core;
using SlimeHunter.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace SlimeHunter.Enemies
{
    /// <summary>
    /// Base de todos os slimes inimigos: movimentação aos saltinhos (Rigidbody), percepção,
    /// atordoamento, empurrão (IKnockbackable) e visual de squash & stretch com aviso de ataque.
    ///
    /// Ciclo: Idle (vaga) → Chase → Windup (aviso) → Attack → Recover → Chase...
    /// Cada tipo implementa TickChase / TickWindup / TickAttack.
    /// A morte (poça + absorção) é do AbsorbableSlime, que desliga este componente.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Health))]
    public abstract class SlimeEnemy : MonoBehaviour, IKnockbackable
    {
        public enum State { Idle, Chase, Windup, Attack, Recover, Stunned }

        [Header("Percepção")]
        [SerializeField] protected float detectRange = 14f;
        [SerializeField] protected float loseRange = 22f;
        [Tooltip("O que bloqueia a visão (não inclua a camada Enemy).")]
        [SerializeField] protected LayerMask sightMask = ~0;

        [Header("Movimento (saltinhos)")]
        [SerializeField] protected float hopInterval = 0.5f;
        [SerializeField] protected float hopSpeed = 3.5f;
        [SerializeField] protected float hopUp = 3.5f;
        [SerializeField] protected float wanderRadius = 4f;
        [Tooltip("Gravidade extra (somada à normal) para os pulos não ficarem flutuantes.")]
        [SerializeField] protected float extraGravity = 1.5f;

        [Header("Ritmo do ataque")]
        [SerializeField] protected float attackCooldown = 1.5f;
        [Tooltip("Tempo de aviso antes do ataque.")]
        [SerializeField] protected float windupTime = 0.55f;
        [SerializeField] protected float recoverTime = 1.1f;

        [Header("Reação")]
        [SerializeField] protected float stunOnHit = 0.2f;
        [SerializeField] protected float stunOnKnockback = 0.7f;

        [Header("Visual")]
        [SerializeField] protected Transform body;
        [FormerlySerializedAs("windupColor")]
        [SerializeField] protected Color telegraphColor = new Color(1f, 0.25f, 0.15f);
        [Tooltip("Escala do corpo no fim do aviso (achatar = melee, inflar = ranged).")]
        [FormerlySerializedAs("windupSquash")]
        [SerializeField] protected Vector3 telegraphScale = new Vector3(1.3f, 0.65f, 1.3f);
        [SerializeField] protected Vector3 airStretch = new Vector3(0.85f, 1.2f, 0.85f);
        [SerializeField] protected float squashSpeed = 14f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        protected Rigidbody rb;
        protected Health health;
        protected Collider ownCollider;
        Renderer bodyRenderer;
        MaterialPropertyBlock mpb;
        Color baseColor = Color.white;
        protected Vector3 bodyBaseScale = Vector3.one;

        protected Transform player;
        protected PlayerSize playerSize;
        protected PlayerMotor playerMotor;

        protected State state;
        float stateStart;
        float nextHop;
        protected float lastAttackTime = float.NegativeInfinity;
        float stunUntil;
        protected bool grounded;
        Vector3 home;
        Vector3 wanderTarget;

        public State CurrentState => state;
        protected float StateTime => Time.time - stateStart;
        protected bool AttackReady => Time.time >= lastAttackTime + attackCooldown;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            health = GetComponent<Health>();
            ownCollider = GetComponent<Collider>();
            if (body == null && transform.childCount > 0) body = transform.GetChild(0);
            if (body != null)
            {
                bodyBaseScale = body.localScale;
                bodyRenderer = body.GetComponent<Renderer>();
            }
            if (bodyRenderer != null && bodyRenderer.sharedMaterial != null && bodyRenderer.sharedMaterial.HasProperty(BaseColorId))
                baseColor = bodyRenderer.sharedMaterial.GetColor(BaseColorId);
            home = transform.position;
            PickWanderTarget();
        }

        protected virtual void Start()
        {
            var pc = FindAnyObjectByType<PlayerController>();
            if (pc == null) return;
            player = pc.transform;
            playerSize = pc.GetComponent<PlayerSize>();
            playerMotor = pc.GetComponent<PlayerMotor>();
        }

        protected virtual void OnEnable() => health.Damaged += OnDamaged;

        protected virtual void OnDisable()
        {
            health.Damaged -= OnDamaged;
            // Volta o visual ao normal (ex.: morreu no meio do aviso).
            if (body != null) body.localScale = bodyBaseScale;
            SetColor(baseColor);
        }

        // ---------- Máquina de estados ----------

        void FixedUpdate()
        {
            rb.AddForce(Physics.gravity * extraGravity, ForceMode.Acceleration);
            grounded = CheckGrounded();

            switch (state)
            {
                case State.Idle: TickIdle(); break;
                case State.Chase:
                    if (player == null || PlayerDistance() > loseRange) Enter(State.Idle);
                    else TickChase();
                    break;
                case State.Windup: TickWindup(); break;
                case State.Attack: TickAttack(); break;
                case State.Recover: if (StateTime >= recoverTime) Enter(State.Chase); break;
                case State.Stunned: if (Time.time >= stunUntil && grounded) Enter(State.Chase); break;
            }
        }

        protected abstract void TickChase();
        protected abstract void TickWindup();
        protected abstract void TickAttack();

        void TickIdle()
        {
            if (CanSeePlayer(detectRange)) { Enter(State.Chase); return; }

            Vector3 to = wanderTarget - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.6f) PickWanderTarget();
            Face(to);
            TryHop(to, 0.6f);
        }

        protected void Enter(State next)
        {
            state = next;
            stateStart = Time.time;
        }

        // ---------- Reações ----------

        protected virtual void OnDamaged(float amount, Vector3 dir)
        {
            if (health.IsDead) return;
            if (state == State.Idle) Enter(State.Chase); // levou tiro: vai atrás do jogador
            if (state == State.Windup || state == State.Chase)
            {
                stunUntil = Time.time + stunOnHit; // interrompe o ataque
                Enter(State.Stunned);
            }
        }

        public void ApplyKnockback(Vector3 velocityChange)
        {
            if (health.IsDead) return;
            rb.AddForce(velocityChange, ForceMode.VelocityChange);
            stunUntil = Time.time + stunOnKnockback;
            Enter(State.Stunned);
        }

        // ---------- Utilidades para as subclasses ----------

        protected void TryHop(Vector3 flatDir, float speedScale)
        {
            if (!grounded || Time.time < nextHop || flatDir.sqrMagnitude < 0.01f) return;
            Vector3 dir = flatDir.normalized;
            rb.linearVelocity = dir * (hopSpeed * speedScale) + Vector3.up * hopUp;
            nextHop = Time.time + hopInterval;
        }

        protected void StopHorizontal() => rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        protected bool CheckGrounded()
        {
            if (ownCollider == null) return true;
            Bounds b = ownCollider.bounds;
            float r = b.extents.x * 0.5f;
            return Physics.SphereCast(b.center, r, Vector3.down, out _, b.extents.y - r + 0.15f,
                ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore);
        }

        protected Vector3 Center => ownCollider != null ? ownCollider.bounds.center : transform.position;

        protected Vector3 PlayerAimPoint => player.position + Vector3.up * (0.5f * player.lossyScale.y);

        protected bool CanSeePlayer(float range)
        {
            if (player == null || PlayerDistance() > range) return false;
            Vector3 dir = PlayerAimPoint - Center;
            if (!Physics.Raycast(Center, dir.normalized, out RaycastHit hit, dir.magnitude, sightMask, QueryTriggerInteraction.Ignore))
                return true;
            return hit.collider.transform.IsChildOf(player);
        }

        protected float PlayerDistance() => player == null ? float.MaxValue : Vector3.Distance(transform.position, player.position);

        protected Vector3 FlatToPlayer()
        {
            if (player == null) return Vector3.zero;
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            return to;
        }

        protected void Face(Vector3 flatDir)
        {
            if (flatDir.sqrMagnitude < 0.0001f) return;
            rb.MoveRotation(Quaternion.LookRotation(flatDir.normalized, Vector3.up));
        }

        void PickWanderTarget()
        {
            Vector2 r = Random.insideUnitCircle * wanderRadius;
            wanderTarget = home + new Vector3(r.x, 0f, r.y);
        }

        // ---------- Visual (squash & stretch + aviso) ----------

        protected virtual void Update()
        {
            if (body == null) return;

            Vector3 target = bodyBaseScale;
            float telegraph01 = 0f;
            if (state == State.Windup)
            {
                telegraph01 = Mathf.Clamp01(StateTime / windupTime);
                target = Vector3.Scale(bodyBaseScale, Vector3.Lerp(Vector3.one, telegraphScale, telegraph01));
            }
            else if (!grounded && rb.linearVelocity.y > 1f)
            {
                target = Vector3.Scale(bodyBaseScale, airStretch);
            }

            body.localScale = Vector3.Lerp(body.localScale, target, 1f - Mathf.Exp(-squashSpeed * Time.deltaTime));
            SetColor(Color.Lerp(baseColor, telegraphColor, state == State.Attack ? 1f : telegraph01));
        }

        void SetColor(Color c)
        {
            if (bodyRenderer == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock(); // some depois de um reload de domínio
            bodyRenderer.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            bodyRenderer.SetPropertyBlock(mpb);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, detectRange);
        }
    }
}

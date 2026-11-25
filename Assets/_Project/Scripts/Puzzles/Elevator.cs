using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Elevador: depois de ativado (Activate() pelo evento de um puzzle), sobe e desce entre o
    /// ponto inicial e travel, esperando nas pontas. Carrega o jogador que estiver em cima.
    /// Roda antes do PlayerMotor para o jogador acompanhar no mesmo frame.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class Elevator : MonoBehaviour
    {
        [SerializeField] Vector3 travel = new Vector3(0f, 5.5f, 0f);
        [SerializeField] float speed = 2f;
        [SerializeField] float waitAtEnds = 1.5f;
        [SerializeField] bool startActive;

        static readonly Collider[] Hits = new Collider[8];

        Collider platform;
        Vector3 startPos;
        float t;
        int direction = 1;
        float waitUntil;

        public bool IsActive { get; private set; }

        void Awake()
        {
            platform = GetComponent<Collider>();
            startPos = transform.position;
            IsActive = startActive;
        }

        public void Activate() => IsActive = true;

        void Update()
        {
            if (!IsActive || Time.time < waitUntil || travel.sqrMagnitude < 0.0001f) return;

            Vector3 before = transform.position;
            t += direction * speed * Time.deltaTime / travel.magnitude;
            if (t >= 1f || t <= 0f)
            {
                t = Mathf.Clamp01(t);
                direction = -direction;
                waitUntil = Time.time + waitAtEnds;
            }

            transform.position = startPos + travel * t;
            CarryPlayer(transform.position - before);
        }

        void CarryPlayer(Vector3 delta)
        {
            if (platform == null || delta == Vector3.zero) return;
            Bounds b = platform.bounds;
            Vector3 center = new Vector3(b.center.x, b.max.y + 0.3f, b.center.z);
            Vector3 half = new Vector3(b.extents.x, 0.35f, b.extents.z);
            int n = Physics.OverlapBoxNonAlloc(center, half, Hits, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var motor = Hits[i].GetComponentInParent<PlayerMotor>();
                if (motor == null) continue;
                motor.AddPlatformMotion(delta);
                return;
            }
        }
    }
}

using SlimeHunter.Player;
using SlimeHunter.Puzzles;
using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>
    /// Corrente de vento ascendente: enquanto o jogador está no volume, sobe na velocidade do vento
    /// (e recupera o pulo duplo e o dash). Os anéis visuais sobem em loop.
    /// Roda antes do PlayerMotor para o empurrão valer no mesmo frame.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [RequireComponent(typeof(Collider))]
    public class WindCurrent : MonoBehaviour
    {
        [SerializeField] float upSpeed = 8f;
        [Tooltip("Anéis que sobem (só visual).")]
        [SerializeField] Transform[] rings = new Transform[0];
        [SerializeField] float ringSpeed = 3f;

        Collider zone;
        float bottom, height;

        public bool PlayerInside { get; private set; }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            zone = GetComponent<Collider>();
            Bounds b = zone.bounds;
            bottom = b.min.y;
            height = b.size.y;
        }

        void Update()
        {
            PlayerSize player = PlayerZone.Find(zone);
            PlayerInside = player != null;
            if (PlayerInside) player.GetComponent<PlayerMotor>().ApplyUpdraft(upSpeed);

            for (int i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                Vector3 p = rings[i].position;
                p.y += ringSpeed * Time.deltaTime;
                if (p.y > bottom + height) p.y = bottom;
                rings[i].position = p;
            }
        }
    }
}

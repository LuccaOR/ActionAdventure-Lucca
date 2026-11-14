using SlimeHunter.Core;
using UnityEngine;

namespace SlimeHunter.Player
{
    /// <summary>Interage com mecanismos (portas, alavancas, elevadores) olhando para eles.</summary>
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] Camera aimCamera;
        [SerializeField] float range = 3f;
        [SerializeField] LayerMask mask = ~0;

        static readonly RaycastHit[] Hits = new RaycastHit[16];

        PlayerInputReader input;

        public IInteractable Current { get; private set; }

        public string CurrentPrompt => IsAlive(Current) ? Current.GetPrompt(gameObject) : null;

        void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        }

        void Update()
        {
            Current = FindTarget();
            if (input.InteractPressed && IsAlive(Current) && Current.CanInteract(gameObject))
                Current.Interact(gameObject);
        }

        IInteractable FindTarget()
        {
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            int count = Physics.RaycastNonAlloc(ray, Hits, range, mask, QueryTriggerInteraction.Collide);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (Hits[i].collider.transform.IsChildOf(transform)) continue;
                var it = Hits[i].collider.GetComponentInParent<IInteractable>();
                if (it != null && Hits[i].distance < bestDist)
                {
                    best = it;
                    bestDist = Hits[i].distance;
                }
            }
            return best;
        }

        static bool IsAlive(IInteractable i) => i is Object o && o != null;
    }
}

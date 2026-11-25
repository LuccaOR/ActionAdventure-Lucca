using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Interaction
{
    /// <summary>Item coletável ao encostar: item de puzzle (chave...) ou gosma que recupera tamanho.</summary>
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour
    {
        public enum Kind { PuzzleItem, Size }

        [SerializeField] Kind kind = Kind.PuzzleItem;
        [Tooltip("Id usado pelas portas/mecanismos, ex.: chave_dourada")]
        [SerializeField] string itemId = "chave_dourada";
        [SerializeField] string displayName = "Chave Dourada";
        [SerializeField] int amount = 1;
        [Tooltip("Barras de tamanho recuperadas (Kind = Size).")]
        [SerializeField] float sizeAmount = 1f;

        [Tooltip("0 = some para sempre. >0 = reaparece depois desse tempo (gosma em áreas que exigem tamanho: evita o jogador ficar travado).")]
        [SerializeField] float respawnSeconds = 0f;

        [Header("Visual")]
        [SerializeField] float spinSpeed = 90f;
        [SerializeField] float bobHeight = 0.15f;
        [SerializeField] float bobSpeed = 2f;

        Vector3 startPos;
        Collider trigger;
        Renderer[] renderers;

        public bool IsAvailable => trigger != null && trigger.enabled;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            startPos = transform.position;
            trigger = GetComponent<Collider>();
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
            transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;

            switch (kind)
            {
                case Kind.PuzzleItem:
                    var inv = player.GetComponent<PlayerInventory>();
                    if (inv == null) return;
                    inv.Add(itemId, amount, displayName);
                    break;
                case Kind.Size:
                    var s = player.GetComponent<PlayerSize>();
                    if (s == null || s.Current >= s.Max) return;
                    s.Grow(sizeAmount);
                    break;
            }

            if (respawnSeconds > 0f)
            {
                SetAvailable(false);
                Invoke(nameof(Respawn), respawnSeconds);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Respawn() => SetAvailable(true);

        void SetAvailable(bool on)
        {
            trigger.enabled = on;
            foreach (var r in renderers) r.enabled = on;
        }
    }
}

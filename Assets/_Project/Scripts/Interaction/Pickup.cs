using SlimeHunter.Core;
using SlimeHunter.Player;
using UnityEngine;

namespace SlimeHunter.Interaction
{
    /// <summary>Item coletável ao encostar: chave de puzzle, vida ou gosma.</summary>
    [RequireComponent(typeof(Collider))]
    public class Pickup : MonoBehaviour
    {
        public enum Kind { PuzzleItem, Health, Mass }

        [SerializeField] Kind kind = Kind.PuzzleItem;
        [Tooltip("Id usado pelas portas/mecanismos, ex.: chave_dourada")]
        [SerializeField] string itemId = "chave_dourada";
        [SerializeField] string displayName = "Chave Dourada";
        [SerializeField] int amount = 1;
        [Tooltip("Vida/gosma recuperada (para Kind Health/Mass).")]
        [SerializeField] float restoreAmount = 25f;

        [Header("Visual")]
        [SerializeField] float spinSpeed = 90f;
        [SerializeField] float bobHeight = 0.15f;
        [SerializeField] float bobSpeed = 2f;

        Vector3 startPos;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake() => startPos = transform.position;

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
                case Kind.Health:
                    var h = player.GetComponent<Health>();
                    if (h == null || h.Current >= h.Max) return;
                    h.Heal(restoreAmount);
                    break;
                case Kind.Mass:
                    var m = player.GetComponent<SlimeMass>();
                    if (m == null || m.Current >= m.Max) return;
                    m.Add(restoreAmount);
                    break;
            }

            Destroy(gameObject);
        }
    }
}

using System.Collections.Generic;
using System.Text;
using SlimeHunter.Core;
using SlimeHunter.Player;
using UnityEngine;
using UnityEngine.Events;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Portão com várias fechaduras (ex.: Portão das 3 Chaves do Rei Slime).
    /// Interagir encaixa as chaves que o jogador tiver; cada encaixe acende. Com todas, abre.
    /// </summary>
    public class KeyGate : MonoBehaviour, IInteractable
    {
        [System.Serializable]
        public struct KeySlot
        {
            public string itemId;
            public string displayName;
            [Tooltip("Encaixe visual que acende quando a chave entra.")]
            public Renderer socket;
            public Color litColor;
        }

        [SerializeField] KeySlot[] slots = new KeySlot[0];
        [SerializeField] Vector3 openOffset = new Vector3(0f, 4.6f, 0f);
        [SerializeField] float openSpeed = 1.5f;
        [SerializeField] UnityEvent onOpened = new UnityEvent();

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color EmptySocket = new Color(0.15f, 0.15f, 0.18f);

        readonly HashSet<int> inserted = new HashSet<int>();
        readonly StringBuilder sb = new StringBuilder();
        MaterialPropertyBlock mpb;
        Vector3 closedPos;

        public UnityEvent OnOpened => onOpened;
        public bool IsOpen { get; private set; }
        public int InsertedCount => inserted.Count;
        public int SlotCount => slots.Length;

        void Awake()
        {
            closedPos = transform.localPosition;
            for (int i = 0; i < slots.Length; i++) Paint(i, EmptySocket);
        }

        void Update()
        {
            Vector3 target = IsOpen ? closedPos + openOffset : closedPos;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, openSpeed * Time.deltaTime);
        }

        public string GetPrompt(GameObject interactor)
        {
            if (IsOpen) return null;
            if (CanInteract(interactor)) return $"[F] Encaixar chaves ({inserted.Count}/{slots.Length})";

            sb.Clear().Append("Portão trancado. Faltam: ");
            bool first = true;
            for (int i = 0; i < slots.Length; i++)
            {
                if (inserted.Contains(i)) continue;
                if (!first) sb.Append(", ");
                sb.Append(slots[i].displayName);
                first = false;
            }
            return sb.ToString();
        }

        public bool CanInteract(GameObject interactor)
        {
            if (IsOpen) return false;
            var inv = interactor.GetComponent<PlayerInventory>();
            if (inv == null) return false;
            for (int i = 0; i < slots.Length; i++)
                if (!inserted.Contains(i) && inv.Has(slots[i].itemId)) return true;
            return false;
        }

        public void Interact(GameObject interactor)
        {
            var inv = interactor.GetComponent<PlayerInventory>();
            if (inv == null || IsOpen) return;

            for (int i = 0; i < slots.Length; i++)
            {
                if (inserted.Contains(i) || !inv.Has(slots[i].itemId)) continue;
                inv.Remove(slots[i].itemId);
                inserted.Add(i);
                Paint(i, slots[i].litColor);
            }

            if (inserted.Count >= slots.Length)
            {
                IsOpen = true;
                PlayerHUD.ShowHint("O portão se abre... GLUUUNB!", 3f);
                onOpened.Invoke();
            }
        }

        void Paint(int i, Color c)
        {
            var r = slots[i].socket;
            if (r == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor(BaseColorId, c);
            r.SetPropertyBlock(mpb);
        }
    }
}

using SlimeHunter.Core;
using SlimeHunter.Player;
using UnityEngine;
using UnityEngine.Events;

namespace SlimeHunter.Interaction
{
    /// <summary>Porta/passagem/elevador que precisa de um item para abrir. Desliza até openOffset.</summary>
    public class KeyLockedDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] string requiredItemId = "chave_dourada";
        [SerializeField] string requiredItemName = "Chave Dourada";
        [SerializeField] bool consumeItem = true;
        [SerializeField] Vector3 openOffset = new Vector3(0f, 3.2f, 0f);
        [SerializeField] float openSpeed = 2.5f;
        [Tooltip("Ligue mecanismos extras aqui (elevador, ponte, luz...).")]
        [SerializeField] UnityEvent onOpened;

        Vector3 closedPos;
        bool isOpen;

        public bool IsOpen => isOpen;

        void Awake() => closedPos = transform.localPosition;

        void Update()
        {
            Vector3 target = isOpen ? closedPos + openOffset : closedPos;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, openSpeed * Time.deltaTime);
        }

        public string GetPrompt(GameObject interactor)
        {
            if (isOpen) return null;
            return CanInteract(interactor)
                ? $"[F] Usar {requiredItemName}"
                : $"Trancado: precisa de {requiredItemName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            if (isOpen) return false;
            var inv = interactor.GetComponent<PlayerInventory>();
            return inv != null && inv.Has(requiredItemId);
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (consumeItem) interactor.GetComponent<PlayerInventory>().Remove(requiredItemId);
            isOpen = true;
            onOpened?.Invoke();
        }
    }
}

using UnityEngine;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Porta/grade/passagem acionada por mecanismo (placa, cristais...). Desliza até openOffset.
    /// Ligue Open() no evento do puzzle pelo Inspector.
    /// </summary>
    public class MechanismDoor : MonoBehaviour
    {
        [SerializeField] Vector3 openOffset = new Vector3(0f, -3.6f, 0f);
        [SerializeField] float speed = 3f;
        [SerializeField] bool startOpen;

        Vector3 closedPos;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            closedPos = transform.localPosition;
            IsOpen = startOpen;
            if (startOpen) transform.localPosition = closedPos + openOffset;
        }

        void Update()
        {
            Vector3 target = IsOpen ? closedPos + openOffset : closedPos;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }

        public void Open() => IsOpen = true;
        public void Close() => IsOpen = false;
        public void Toggle() => IsOpen = !IsOpen;
    }
}

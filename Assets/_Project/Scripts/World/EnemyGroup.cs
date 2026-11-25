using SlimeHunter.Core;
using UnityEngine;
using UnityEngine.Events;

namespace SlimeHunter.World
{
    /// <summary>Vigia um grupo de inimigos e dispara OnCleared quando todos morrem (ex.: arena do tutorial).</summary>
    public class EnemyGroup : MonoBehaviour
    {
        [SerializeField] Health[] enemies = new Health[0];
        [SerializeField] UnityEvent onCleared = new UnityEvent();

        int alive;

        public UnityEvent OnCleared => onCleared;
        public bool IsCleared { get; private set; }
        public int Alive => alive;

        void Start()
        {
            alive = 0;
            foreach (var e in enemies)
            {
                if (e == null || e.IsDead) continue;
                alive++;
                e.Died += OnEnemyDied;
            }
            if (alive == 0) Clear();
        }

        void OnDestroy()
        {
            foreach (var e in enemies) if (e != null) e.Died -= OnEnemyDied;
        }

        void OnEnemyDied()
        {
            alive--;
            if (alive <= 0) Clear();
        }

        void Clear()
        {
            if (IsCleared) return;
            IsCleared = true;
            onCleared.Invoke();
        }
    }
}

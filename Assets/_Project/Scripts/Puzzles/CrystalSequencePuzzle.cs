using SlimeHunter.Player;
using UnityEngine;
using UnityEngine.Events;

namespace SlimeHunter.Puzzles
{
    /// <summary>
    /// Tiro em sequência: os cristais precisam ser atingidos na ordem da lista.
    /// Acertou o próximo: ele acende. Errou: todos piscam vermelho e a sequência recomeça.
    /// Com 1 cristal vira o "atire no cristal para abrir a grade" do tutorial.
    /// </summary>
    public class CrystalSequencePuzzle : MonoBehaviour
    {
        [Tooltip("Ordem correta (indicada pelas runas no chão).")]
        [SerializeField] ShootableCrystal[] order = new ShootableCrystal[0];
        [SerializeField] float errorFlashTime = 0.6f;
        [SerializeField] UnityEvent onSolved = new UnityEvent();

        int progress;

        public UnityEvent OnSolved => onSolved;
        public bool IsSolved { get; private set; }
        public int Progress => progress;
        public int Length => order.Length;

        void Awake()
        {
            foreach (var c in order) if (c != null) c.Puzzle = this;
        }

        internal void OnCrystalHit(ShootableCrystal crystal)
        {
            if (IsSolved || order.Length == 0) return;
            if (crystal.IsLit) return; // acertar de novo um aceso não conta nem erra

            if (crystal == order[progress])
            {
                crystal.SetLit(true);
                progress++;
                if (progress >= order.Length) Solve();
                return;
            }

            // Errou a ordem: reinicia.
            progress = 0;
            foreach (var c in order)
            {
                c.SetLit(false);
                c.Flash(ShootableCrystal.Error, errorFlashTime);
            }
            PlayerHUD.ShowHint("Ordem errada! Veja as runas no chão.", 2f);
        }

        void Solve()
        {
            IsSolved = true;
            foreach (var c in order) c.SetSolved();
            PlayerHUD.ShowHint("Sequência correta! Um mecanismo foi ativado.", 2.5f);
            onSolved.Invoke();
        }
    }
}

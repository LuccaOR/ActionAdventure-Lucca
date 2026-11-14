using UnityEngine;

namespace SlimeHunter.Core
{
    /// <summary>O que o jogador ganha ao terminar de absorver algo.</summary>
    public struct AbsorbReward
    {
        public float Health;
        public float Mass;

        public AbsorbReward(float health, float mass)
        {
            Health = health;
            Mass = mass;
        }
    }

    /// <summary>Inimigos derrotados (poças) que o protagonista pode absorver.</summary>
    public interface IAbsorbable
    {
        bool CanBeAbsorbed { get; }
        float AbsorbDuration { get; }

        /// <param name="progress01">0..1</param>
        /// <param name="absorber">Transform de quem está absorvendo.</param>
        void OnAbsorbProgress(float progress01, Transform absorber);
        void OnAbsorbCancelled();
        AbsorbReward CompleteAbsorb();
    }
}

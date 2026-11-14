using UnityEngine;

namespace SlimeHunter.Core
{
    /// <summary>Qualquer coisa que pode levar dano (player, slimes, objetos quebráveis).</summary>
    public interface IDamageable
    {
        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection);
    }
}

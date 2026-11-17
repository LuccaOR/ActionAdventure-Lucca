using UnityEngine;

namespace SlimeHunter.Core
{
    /// <summary>Inimigos que podem ser empurrados (ex.: pelo Impacto do protagonista).</summary>
    public interface IKnockbackable
    {
        void ApplyKnockback(Vector3 velocityChange);
    }
}

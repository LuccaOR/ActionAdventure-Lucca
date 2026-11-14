namespace SlimeHunter.Core
{
    /// <summary>Mecanismos de puzzle: portas, alavancas, elevadores.</summary>
    public interface IInteractable
    {
        string GetPrompt(UnityEngine.GameObject interactor);
        bool CanInteract(UnityEngine.GameObject interactor);
        void Interact(UnityEngine.GameObject interactor);
    }
}

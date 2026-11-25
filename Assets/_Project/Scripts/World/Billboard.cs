using UnityEngine;

namespace SlimeHunter.World
{
    /// <summary>Mantém o objeto virado para a câmera (placas e rótulos legíveis de qualquer ângulo).</summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("Só gira no eixo Y (fica em pé).")]
        [SerializeField] bool yawOnly = true;

        void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 dir = transform.position - cam.transform.position;
            if (yawOnly) dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}

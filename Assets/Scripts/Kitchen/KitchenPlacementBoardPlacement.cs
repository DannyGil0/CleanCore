using UnityEngine;

namespace CleanCore.Kitchen
{
    /// <summary>
    /// Keeps the kitchen scoreboard at a fixed world position in the scene (environmental UI).
    /// Unlike InWorldMenuVR, this panel does not follow the player.
    /// </summary>
    public class KitchenPlacementBoardPlacement : MonoBehaviour
    {
        [SerializeField] Vector3 _worldPosition = new Vector3(2f, 1.6f, -3f);
        [SerializeField] float _yawDegrees = 180f;

        void Start()
        {
            transform.SetPositionAndRotation(_worldPosition, Quaternion.Euler(0f, _yawDegrees, 0f));

            Transform canvas = transform.Find("BoardCanvas");
            if (canvas != null)
            {
                canvas.localRotation = Quaternion.identity;
                WorldSpaceCanvasBuilder.FinalizeCanvas(canvas.gameObject);
            }

            Debug.Log($"[KitchenBoard] Tablero colocado en {_worldPosition} (yaw {_yawDegrees}).");
        }
    }
}

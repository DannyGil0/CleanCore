using UnityEngine;

namespace CleanCore.Kitchen
{
    public class KitchenItemInteractable : MonoBehaviour
    {
        [Header("Blink Settings")]
        [SerializeField] private Color _blinkColor = new Color(0.3f, 0.5f, 1f, 1f);
        [SerializeField] private float _blinkSpeed = 2f;

        private Renderer _renderer;
        private MaterialPropertyBlock _propBlock;
        private Color _originalColor;
        private Coroutine _blinkRoutine;
        private bool _placed;

        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int MainTex = Shader.PropertyToID("_BaseMap");

        private void Awake()
        {
            _renderer = GetComponentInChildren<Renderer>();
            _propBlock = new MaterialPropertyBlock();
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _originalColor = _renderer.sharedMaterial.GetColor(BaseColor);
            }
            StartBlinking();
        }

        public void SetPlaced(bool placed)
        {
            _placed = placed;
            if (placed)
                StopBlinking();
            else
                StartBlinking();
        }

        private void StartBlinking()
        {
            if (_blinkRoutine != null) return;
            if (!gameObject.activeInHierarchy) return;
            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        private void StopBlinking()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
            if (_renderer != null)
            {
                _propBlock.SetColor(BaseColor, _originalColor);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private System.Collections.IEnumerator BlinkLoop()
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * _blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                Color c = Color.Lerp(_originalColor, _blinkColor, t);
                _propBlock.SetColor(BaseColor, c);
                _renderer.SetPropertyBlock(_propBlock);
                yield return null;
            }
        }
    }
}

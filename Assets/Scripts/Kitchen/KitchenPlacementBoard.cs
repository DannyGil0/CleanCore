using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CleanCore.Kitchen
{
    public class KitchenPlacementBoard : MonoBehaviour
    {
        Dictionary<string, TextMeshProUGUI> _itemTexts = new Dictionary<string, TextMeshProUGUI>();
        TextMeshProUGUI _counterText;
        TextMeshProUGUI _victoryText;
        int _totalItems;
        int _placedCount;
        bool _built;

        void Awake()
        {
            if (!_built)
            {
                KitchenPlacementBoardFactory.Build(transform, this);
                _built = true;
            }
        }

        void OnEnable()
        {
            KitchenSocketController.OnPlacementChanged += OnItemPlacementChanged;
        }

        void OnDisable()
        {
            KitchenSocketController.OnPlacementChanged -= OnItemPlacementChanged;
        }

        public void Configure(Dictionary<string, TextMeshProUGUI> itemTexts, TextMeshProUGUI counterText,
            TextMeshProUGUI victoryText, int totalItems, int placedCount)
        {
            _itemTexts = itemTexts;
            _counterText = counterText;
            _victoryText = victoryText;
            _totalItems = totalItems;
            _placedCount = placedCount;
        }

        void OnItemPlacementChanged(string objectName, bool placed)
        {
            if (_itemTexts.TryGetValue(objectName, out var txt))
            {
                string displayName = FormatDisplayName(objectName);
                txt.text = placed ? $"[X] {displayName}" : $"[ ] {displayName}";
                txt.color = placed
                    ? new Color(0.15f, 0.55f, 0.25f, 1f)
                    : new Color(0.75f, 0.55f, 0.1f, 1f);
            }

            _placedCount = 0;
            var controllers = FindObjectsByType<KitchenSocketController>(FindObjectsSortMode.None);
            foreach (var ctrl in controllers)
                if (ctrl.IsPlaced) _placedCount++;

            if (_counterText != null)
                _counterText.text = $"{_placedCount} / {_totalItems} acomodados";
        }

        public void ShowVictoryMessage(string message)
        {
            if (_victoryText != null)
            {
                _victoryText.text = message;
                _victoryText.color = new Color(0.15f, 0.55f, 0.25f, 1f);
            }
        }

        static string FormatDisplayName(string objectName)
        {
            return objectName
                .Replace("SM_", "")
                .Replace("Kitchen_Props_A_", "")
                .Replace("Kitchen_Props_B_", "")
                .Replace("_", " ");
        }
    }
}

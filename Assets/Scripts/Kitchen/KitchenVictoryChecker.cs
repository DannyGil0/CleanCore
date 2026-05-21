using UnityEngine;

namespace CleanCore.Kitchen
{
    public class KitchenVictoryChecker : MonoBehaviour
    {
        private KitchenPlacementBoard _board;
        private bool _victoryShown;

        private void Start()
        {
            _board = GetComponent<KitchenPlacementBoard>();
            KitchenSocketController.OnPlacementChanged += OnChanged;
        }

        private void OnDestroy()
        {
            KitchenSocketController.OnPlacementChanged -= OnChanged;
        }

        private void OnChanged(string name, bool placed)
        {
            if (_victoryShown) return;
            CheckVictory();
        }

        private void Update()
        {
            if (_victoryShown) return;
            CheckVictory();
        }

        private void CheckVictory()
        {
            var controllers = FindObjectsByType<KitchenSocketController>(FindObjectsSortMode.None);
            if (controllers.Length == 0) return;

            bool allPlaced = true;
            foreach (var ctrl in controllers)
            {
                if (!ctrl.IsPlaced) { allPlaced = false; break; }
            }

            if (!allPlaced) return;

            var aggregator = FindAnyObjectByType<CleaningStatsAggregator>();
            bool houseClean = aggregator != null && aggregator.GlobalCleanlinessPercent >= 99.5f;

            if (allPlaced && houseClean)
            {
                _victoryShown = true;
                if (_board != null)
                    _board.ShowVictoryMessage("FELICIDADES\nCasa limpia y ordenada");
                Debug.Log("[KitchenSocket] VICTORIA: todos los objetos acomodados + casa limpia!");
            }
            else if (allPlaced)
            {
                if (_board != null)
                    _board.ShowVictoryMessage("Objetos acomodados!\nFalta limpiar la casa...");
            }
        }
    }
}

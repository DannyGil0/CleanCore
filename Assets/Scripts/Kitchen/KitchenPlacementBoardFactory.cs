using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CleanCore.Kitchen
{
    public static class KitchenPlacementBoardFactory
    {
        static readonly Color PanelBg = new Color(0.95f, 0.98f, 1f, 0.92f);
        static readonly Color TitleColor = new Color(0.15f, 0.35f, 0.45f, 1f);
        static readonly Color PendingColor = new Color(0.75f, 0.55f, 0.1f, 1f);
        static readonly Color DoneColor = new Color(0.15f, 0.55f, 0.25f, 1f);

        public static void Build(Transform root, KitchenPlacementBoard board)
        {
            var controllers = Object.FindObjectsByType<KitchenSocketController>(FindObjectsSortMode.None);
            var itemTexts = new Dictionary<string, TextMeshProUGUI>();
            int placedCount = 0;

            GameObject canvasGo = WorldSpaceCanvasBuilder.CreateCanvas(
                root, "BoardCanvas", new Vector2(700, 900), sortingOrder: 15, interactive: false);

            GameObject panel = WorldSpaceCanvasBuilder.CreatePanel(
                canvasGo.transform, "Panel", PanelBg, new Vector2(16, 16), new Vector2(-16, -16));
            WorldSpaceCanvasBuilder.AddVerticalLayout(panel, new RectOffset(28, 28, 28, 28), 10);

            WorldSpaceCanvasBuilder.CreateTMP(panel.transform, "Title", "OBJETOS POR ACOMODAR", 36,
                FontStyles.Bold, TitleColor, 48, TextAlignmentOptions.Center);

            foreach (var ctrl in controllers)
            {
                string name = ctrl.ObjectName;
                string displayName = FormatDisplayName(name);
                string prefix = ctrl.IsPlaced ? "[X]" : "[ ]";
                Color color = ctrl.IsPlaced ? DoneColor : PendingColor;
                if (ctrl.IsPlaced) placedCount++;

                var txt = (TextMeshProUGUI)WorldSpaceCanvasBuilder.CreateTMP(panel.transform, $"Item_{name}",
                    $"{prefix} {displayName}", 24, FontStyles.Normal, color, 32, TextAlignmentOptions.Left);
                itemTexts[name] = txt;
            }

            var counterText = (TextMeshProUGUI)WorldSpaceCanvasBuilder.CreateTMP(panel.transform, "Counter",
                $"{placedCount} / {controllers.Length} acomodados", 28, FontStyles.Bold, TitleColor, 40,
                TextAlignmentOptions.Center);

            var victoryText = (TextMeshProUGUI)WorldSpaceCanvasBuilder.CreateTMP(panel.transform, "Victory", "", 26,
                FontStyles.Bold, DoneColor, 80, TextAlignmentOptions.Center);

            WorldSpaceCanvasBuilder.FinalizeCanvas(canvasGo);
            board.Configure(itemTexts, counterText, victoryText, controllers.Length, placedCount);
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

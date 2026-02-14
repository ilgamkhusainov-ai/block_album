using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.Pieces
{
    public static class PiecePreviewRenderer
    {
        public static void Render(IList<Image> cells, int previewGridSize, Color emptyColor, PieceShapeDefinition shape, Color fillColor)
        {
            for (var i = 0; i < cells.Count; i++)
            {
                cells[i].color = emptyColor;
            }

            if (shape == null)
            {
                return;
            }

            GetBounds(shape.Cells, out var minX, out var maxX, out var minY, out var maxY);
            var width = maxX - minX + 1;
            var height = maxY - minY + 1;
            var originX = (previewGridSize - width) / 2 - minX;
            var originY = (previewGridSize - height) / 2 - minY;

            for (var i = 0; i < shape.Cells.Length; i++)
            {
                var src = shape.Cells[i];
                var x = src.x + originX;
                var y = src.y + originY;

                if (x < 0 || y < 0 || x >= previewGridSize || y >= previewGridSize)
                {
                    continue;
                }

                var index = (previewGridSize - 1 - y) * previewGridSize + x;
                cells[index].color = fillColor;
            }
        }

        public static Vector2Int CalculatePlacementPivot(IReadOnlyList<Vector2Int> offsets)
        {
            if (offsets == null || offsets.Count == 0)
            {
                return Vector2Int.zero;
            }

            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            for (var i = 0; i < offsets.Count; i++)
            {
                var cell = offsets[i];
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            var pivotX = Mathf.FloorToInt((minX + maxX) * 0.5f);
            var pivotY = Mathf.FloorToInt((minY + maxY) * 0.5f);
            return new Vector2Int(pivotX, pivotY);
        }

        private static void GetBounds(IReadOnlyList<Vector2Int> offsets, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;
            minY = int.MaxValue;
            maxY = int.MinValue;

            for (var i = 0; i < offsets.Count; i++)
            {
                var cell = offsets[i];
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }
        }
    }
}

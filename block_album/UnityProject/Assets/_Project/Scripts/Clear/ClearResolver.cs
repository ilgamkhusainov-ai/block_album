using System.Collections.Generic;
using BlockAlbum.Grid;
using UnityEngine;

namespace BlockAlbum.Clear
{
    public sealed class ClearResolution
    {
        public HashSet<Vector2Int> CellsToClear { get; } = new HashSet<Vector2Int>();
        public int LinesCleared { get; set; }
        public int ZonesCleared { get; set; }
    }

    public static class ClearResolver
    {
        public static ClearResolution Resolve(BoardModel model, int zoneSize)
        {
            var result = new ClearResolution();
            if (model == null)
            {
                return result;
            }

            var size = model.Size;

            for (var y = 0; y < size; y++)
            {
                if (!model.IsRowFull(y))
                {
                    continue;
                }

                result.LinesCleared++;
                for (var x = 0; x < size; x++)
                {
                    result.CellsToClear.Add(new Vector2Int(x, y));
                }
            }

            for (var x = 0; x < size; x++)
            {
                if (!model.IsColumnFull(x))
                {
                    continue;
                }

                result.LinesCleared++;
                for (var y = 0; y < size; y++)
                {
                    result.CellsToClear.Add(new Vector2Int(x, y));
                }
            }

            zoneSize = Mathf.Max(1, zoneSize);
            for (var startY = 0; startY + zoneSize <= size; startY += zoneSize)
            {
                for (var startX = 0; startX + zoneSize <= size; startX += zoneSize)
                {
                    if (!IsZoneFull(model, startX, startY, zoneSize))
                    {
                        continue;
                    }

                    result.ZonesCleared++;
                    for (var localY = 0; localY < zoneSize; localY++)
                    {
                        for (var localX = 0; localX < zoneSize; localX++)
                        {
                            result.CellsToClear.Add(new Vector2Int(startX + localX, startY + localY));
                        }
                    }
                }
            }

            return result;
        }

        private static bool IsZoneFull(BoardModel model, int startX, int startY, int zoneSize)
        {
            for (var y = 0; y < zoneSize; y++)
            {
                for (var x = 0; x < zoneSize; x++)
                {
                    if (!model.IsFilledForClear(new Vector2Int(startX + x, startY + y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}

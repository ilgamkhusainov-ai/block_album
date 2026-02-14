using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAlbum.Pieces
{
    public static class PieceShapeLibrary
    {
        private const int FullPoolTier = 13;
        private static readonly IReadOnlyList<PieceShapeDefinition> Shapes = BuildShapes();
        private static readonly IReadOnlyList<PieceShapeDefinition> FutureShapes41Plus = BuildFutureShapes41Plus();
        private static readonly IReadOnlyList<IReadOnlyList<PieceShapeDefinition>> TierPools = BuildTierPools();

        private static IReadOnlyList<PieceShapeDefinition> BuildShapes()
        {
            var currentBaseShapes = new List<PieceShapeDefinition>
            {
                new PieceShapeDefinition("dot", new Vector2Int(0, 0)),
                new PieceShapeDefinition("line2", new Vector2Int(0, 0), new Vector2Int(1, 0)),
                new PieceShapeDefinition("line3", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0)),
                new PieceShapeDefinition("square2", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1)),
                new PieceShapeDefinition("l3", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 0)),
                new PieceShapeDefinition("l4", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0)),
                new PieceShapeDefinition("t4", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(1, 1)),
                new PieceShapeDefinition("z4", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1)),
            };

            return BuildVariants(currentBaseShapes);
        }

        private static IReadOnlyList<PieceShapeDefinition> BuildFutureShapes41Plus()
        {
            var futureBaseShapes = new List<PieceShapeDefinition>
            {
                // Reserved for level 41+ rollout. Not used in active pools yet.
                new PieceShapeDefinition("u5", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 0)),
                new PieceShapeDefinition("plus5", new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 2)),
                new PieceShapeDefinition("w5", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(2, 2)),
                new PieceShapeDefinition("v5", new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0), new Vector2Int(2, 0)),
                new PieceShapeDefinition("p5", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(0, 2)),
                new PieceShapeDefinition("i4", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0)),
                new PieceShapeDefinition("i5", new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0)),
            };

            return BuildVariants(futureBaseShapes);
        }

        private static IReadOnlyList<PieceShapeDefinition> BuildVariants(IReadOnlyList<PieceShapeDefinition> baseShapes)
        {
            var uniqueBySignature = new HashSet<string>();
            var expanded = new List<PieceShapeDefinition>(48);
            var variantIndex = 0;

            for (var i = 0; i < baseShapes.Count; i++)
            {
                var baseShape = baseShapes[i];
                for (var rotation = 0; rotation < 4; rotation++)
                {
                    var rotated = Rotate(baseShape.Cells, rotation);
                    AddVariant(baseShape.Id, rotated, uniqueBySignature, expanded, ref variantIndex);

                    var mirrored = MirrorX(rotated);
                    AddVariant(baseShape.Id, mirrored, uniqueBySignature, expanded, ref variantIndex);
                }
            }

            return expanded;
        }

        private static void AddVariant(
            string baseId,
            Vector2Int[] rawCells,
            HashSet<string> uniqueBySignature,
            List<PieceShapeDefinition> output,
            ref int variantIndex)
        {
            var normalized = Normalize(rawCells);
            var signature = BuildSignature(normalized);
            if (!uniqueBySignature.Add(signature))
            {
                return;
            }

            output.Add(new PieceShapeDefinition($"{baseId}_{variantIndex:00}", normalized));
            variantIndex++;
        }

        private static Vector2Int[] Rotate(Vector2Int[] cells, int times90)
        {
            var result = new Vector2Int[cells.Length];
            var turns = ((times90 % 4) + 4) % 4;
            for (var i = 0; i < cells.Length; i++)
            {
                var value = cells[i];
                for (var turn = 0; turn < turns; turn++)
                {
                    value = new Vector2Int(value.y, -value.x);
                }

                result[i] = value;
            }

            return result;
        }

        private static Vector2Int[] MirrorX(Vector2Int[] cells)
        {
            var result = new Vector2Int[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                result[i] = new Vector2Int(-c.x, c.y);
            }

            return result;
        }

        private static Vector2Int[] Normalize(Vector2Int[] cells)
        {
            var minX = int.MaxValue;
            var minY = int.MaxValue;
            for (var i = 0; i < cells.Length; i++)
            {
                var c = cells[i];
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
            }

            var normalized = new Vector2Int[cells.Length];
            for (var i = 0; i < cells.Length; i++)
            {
                normalized[i] = new Vector2Int(cells[i].x - minX, cells[i].y - minY);
            }

            Array.Sort(normalized, CompareCells);
            return normalized;
        }

        private static string BuildSignature(IReadOnlyList<Vector2Int> cells)
        {
            var signature = string.Empty;
            for (var i = 0; i < cells.Count; i++)
            {
                signature += cells[i].x;
                signature += ":";
                signature += cells[i].y;
                signature += ";";
            }

            return signature;
        }

        private static int CompareCells(Vector2Int a, Vector2Int b)
        {
            var byY = a.y.CompareTo(b.y);
            return byY != 0 ? byY : a.x.CompareTo(b.x);
        }

        public static IReadOnlyList<PieceShapeDefinition> GetDefaults()
        {
            return Shapes;
        }

        public static IReadOnlyList<PieceShapeDefinition> GetReservedForLevel41Plus()
        {
            return FutureShapes41Plus;
        }

        public static IReadOnlyList<PieceShapeDefinition> GetPoolForVarietyTier(int tier)
        {
            if (tier >= FullPoolTier)
            {
                return Shapes;
            }

            var safeTier = Mathf.Clamp(tier, 1, FullPoolTier - 1);
            return TierPools[safeTier - 1];
        }

        private static IReadOnlyList<IReadOnlyList<PieceShapeDefinition>> BuildTierPools()
        {
            var pools = new List<IReadOnlyList<PieceShapeDefinition>>(FullPoolTier);
            for (var tier = 1; tier < FullPoolTier; tier++)
            {
                pools.Add(BuildTierPool(tier));
            }

            pools.Add(Shapes);
            return pools;
        }

        private static IReadOnlyList<PieceShapeDefinition> BuildTierPool(int tier)
        {
            var pool = new List<PieceShapeDefinition>(Shapes.Count * 4);
            for (var i = 0; i < Shapes.Count; i++)
            {
                var shape = Shapes[i];
                var baseId = ExtractBaseId(shape.Id);
                var weight = GetWeight(baseId, tier);
                for (var rep = 0; rep < weight; rep++)
                {
                    pool.Add(shape);
                }
            }

            if (pool.Count == 0)
            {
                return Shapes;
            }

            return pool;
        }

        private static int GetWeight(string baseId, int tier)
        {
            var t = Mathf.Clamp01((tier - 1f) / (FullPoolTier - 2f)); // 1..12 -> 0..1
            switch (baseId)
            {
                case "dot":
                    if (tier > 5) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(10f, 1f, t)));
                case "line2":
                    if (tier > 8) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(9f, 2f, t)));
                case "line3":
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(8f, 4f, t)));
                case "square2":
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(7f, 5f, t)));
                case "l3":
                    if (tier < 2) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(2f, 7f, t)));
                case "l4":
                    if (tier < 3) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 7f, t)));
                case "t4":
                    if (tier < 5) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 7f, t)));
                case "z4":
                    if (tier < 7) return 0;
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(1f, 6f, t)));
                default:
                    return 0;
            }
        }

        private static string ExtractBaseId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            var split = id.LastIndexOf('_');
            if (split <= 0 || split >= id.Length - 1)
            {
                return id;
            }

            for (var i = split + 1; i < id.Length; i++)
            {
                if (id[i] < '0' || id[i] > '9')
                {
                    return id;
                }
            }

            return id.Substring(0, split);
        }
    }
}

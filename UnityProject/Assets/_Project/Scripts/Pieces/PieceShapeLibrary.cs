using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAlbum.Pieces
{
    public static class PieceShapeLibrary
    {
        private static readonly IReadOnlyList<PieceShapeDefinition> Shapes = BuildShapes();

        private static IReadOnlyList<PieceShapeDefinition> BuildShapes()
        {
            var baseShapes = new List<PieceShapeDefinition>
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
    }
}

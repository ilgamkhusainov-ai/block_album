using UnityEngine;

namespace BlockAlbum.Pieces
{
    public sealed class PieceShapeDefinition
    {
        public string Id { get; }
        public Vector2Int[] Cells { get; }

        public PieceShapeDefinition(string id, params Vector2Int[] cells)
        {
            Id = id;
            Cells = cells;
        }
    }
}

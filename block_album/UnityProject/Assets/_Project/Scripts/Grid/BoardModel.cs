using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockAlbum.Grid
{
    public sealed class BoardModel
    {
        private readonly bool[,] _occupied;
        private readonly bool[,] _blocked;
        private readonly bool[,] _blockedOnOccupied;

        public int Size { get; }

        public BoardModel(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Board size must be positive.");
            }

            Size = size;
            _occupied = new bool[size, size];
            _blocked = new bool[size, size];
            _blockedOnOccupied = new bool[size, size];
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Size && cell.y >= 0 && cell.y < Size;
        }

        public bool IsOccupied(Vector2Int cell)
        {
            return InBounds(cell) && _occupied[cell.x, cell.y];
        }

        public bool IsBlocked(Vector2Int cell)
        {
            return InBounds(cell) && _blocked[cell.x, cell.y];
        }

        public bool IsBlockedOnOccupied(Vector2Int cell)
        {
            return InBounds(cell) && _blocked[cell.x, cell.y] && _blockedOnOccupied[cell.x, cell.y];
        }

        public bool IsFilledForClear(Vector2Int cell)
        {
            return InBounds(cell) && _occupied[cell.x, cell.y];
        }

        public void SetOccupied(Vector2Int cell, bool occupied)
        {
            if (!InBounds(cell))
            {
                return;
            }

            _occupied[cell.x, cell.y] = occupied;
        }

        public void SetBlocked(Vector2Int cell, bool blocked)
        {
            if (!InBounds(cell))
            {
                return;
            }

            _blocked[cell.x, cell.y] = blocked;
            _blockedOnOccupied[cell.x, cell.y] = blocked && _occupied[cell.x, cell.y];
        }

        public void PlaceBlocker(Vector2Int cell)
        {
            if (!InBounds(cell))
            {
                return;
            }

            _blocked[cell.x, cell.y] = true;
            _blockedOnOccupied[cell.x, cell.y] = _occupied[cell.x, cell.y];
        }

        public void ClearBlocker(Vector2Int cell)
        {
            if (!InBounds(cell))
            {
                return;
            }

            _blocked[cell.x, cell.y] = false;
            _blockedOnOccupied[cell.x, cell.y] = false;
        }

        public bool CanPlace(IReadOnlyList<Vector2Int> offsets, Vector2Int origin)
        {
            for (var i = 0; i < offsets.Count; i++)
            {
                var target = origin + offsets[i];
                if (!InBounds(target) || IsOccupied(target) || IsBlocked(target))
                {
                    return false;
                }
            }

            return true;
        }

        public bool Place(IReadOnlyList<Vector2Int> offsets, Vector2Int origin)
        {
            if (!CanPlace(offsets, origin))
            {
                return false;
            }

            for (var i = 0; i < offsets.Count; i++)
            {
                SetOccupied(origin + offsets[i], true);
            }

            return true;
        }

        public void ClearAll()
        {
            Array.Clear(_occupied, 0, _occupied.Length);
            Array.Clear(_blocked, 0, _blocked.Length);
            Array.Clear(_blockedOnOccupied, 0, _blockedOnOccupied.Length);
        }

        public List<Vector2Int> GetBlockedCells(List<Vector2Int> buffer = null)
        {
            var result = buffer ?? new List<Vector2Int>();
            result.Clear();

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (_blocked[x, y])
                    {
                        result.Add(new Vector2Int(x, y));
                    }
                }
            }

            return result;
        }

        public int CountBlockedOnOccupied()
        {
            var count = 0;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    if (_blocked[x, y] && _blockedOnOccupied[x, y])
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public bool IsRowFull(int y)
        {
            if (y < 0 || y >= Size)
            {
                return false;
            }

            for (var x = 0; x < Size; x++)
            {
                if (!IsFilledForClear(new Vector2Int(x, y)))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsColumnFull(int x)
        {
            if (x < 0 || x >= Size)
            {
                return false;
            }

            for (var y = 0; y < Size; y++)
            {
                if (!IsFilledForClear(new Vector2Int(x, y)))
                {
                    return false;
                }
            }

            return true;
        }

        public int ClearCells(IEnumerable<Vector2Int> cells)
        {
            if (cells == null)
            {
                return 0;
            }

            var cleared = 0;
            foreach (var cell in cells)
            {
                if (!InBounds(cell) || !_occupied[cell.x, cell.y])
                {
                    continue;
                }

                _occupied[cell.x, cell.y] = false;
                cleared++;
            }

            return cleared;
        }

        public ClearWithBlockersResult ClearCellsWithBlockerRules(IEnumerable<Vector2Int> cells)
        {
            var result = new ClearWithBlockersResult();
            if (cells == null)
            {
                return result;
            }

            foreach (var cell in cells)
            {
                if (!InBounds(cell))
                {
                    continue;
                }

                var x = cell.x;
                var y = cell.y;
                var hasBlocker = _blocked[x, y];
                var preserveFigure = hasBlocker && _blockedOnOccupied[x, y];

                if (hasBlocker)
                {
                    _blocked[x, y] = false;
                    _blockedOnOccupied[x, y] = false;
                    result.ClearedBlockers++;
                }

                if (_occupied[x, y] && !preserveFigure)
                {
                    _occupied[x, y] = false;
                    result.ClearedOccupiedCells++;
                }

                if (preserveFigure)
                {
                    result.PreservedOccupiedCells++;
                }
            }

            return result;
        }

        public struct ClearWithBlockersResult
        {
            public int ClearedOccupiedCells;
            public int ClearedBlockers;
            public int PreservedOccupiedCells;
        }
    }
}

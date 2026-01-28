using System.Collections.Generic;
using UnityEngine;
using CollapseGame.Core;

namespace CollapseGame.Managers
{
    public class GroupDetector
    {
        private readonly int _rows;
        private readonly int _columns;
        
        private readonly Queue<GridPosition> _searchQueue;
        private readonly HashSet<GridPosition> _visited;
        private readonly List<GridPosition> _currentGroup;
        
        private static readonly int[] RowOffsets = { -1, 1, 0, 0 };
        private static readonly int[] ColOffsets = { 0, 0, -1, 1 };

        public GroupDetector(int rows, int columns)
        {
            _rows = rows;
            _columns = columns;
            
            int maxSize = rows * columns;
            _searchQueue = new Queue<GridPosition>(maxSize);
            _visited = new HashSet<GridPosition>(maxSize);
            _currentGroup = new List<GridPosition>(maxSize);
        }

        public List<GridPosition> FindGroup(Block[,] grid, GridPosition startPosition)
        {
            _currentGroup.Clear();
            _visited.Clear();
            _searchQueue.Clear();

            Block startBlock = grid[startPosition.Row, startPosition.Column];
            if (startBlock == null || !startBlock.IsActive) return _currentGroup;

            BlockColor targetColor = startBlock.Color;

            _searchQueue.Enqueue(startPosition);
            _visited.Add(startPosition);

            while (_searchQueue.Count > 0)
            {
                GridPosition current = _searchQueue.Dequeue();
                _currentGroup.Add(current);

                for (int i = 0; i < 4; i++)
                {
                    int newRow = current.Row + RowOffsets[i];
                    int newCol = current.Column + ColOffsets[i];

                    if (!IsValidPosition(newRow, newCol)) continue;

                    GridPosition neighborPos = new GridPosition(newRow, newCol);
                    if (_visited.Contains(neighborPos)) continue;

                    Block neighbor = grid[newRow, newCol];
                    if (neighbor == null || !neighbor.IsActive) continue;
                    if (neighbor.Color != targetColor) continue;

                    _visited.Add(neighborPos);
                    _searchQueue.Enqueue(neighborPos);
                }
            }

            return _currentGroup;
        }

        public Dictionary<GridPosition, int> FindAllGroupSizes(Block[,] grid)
        {
            Dictionary<GridPosition, int> groupSizes = new Dictionary<GridPosition, int>();
            HashSet<GridPosition> processedPositions = new HashSet<GridPosition>();

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    GridPosition pos = new GridPosition(row, col);
                    if (processedPositions.Contains(pos)) continue;

                    Block block = grid[row, col];
                    if (block == null || !block.IsActive) continue;

                    List<GridPosition> group = FindGroup(grid, pos);
                    int groupSize = group.Count;

                    foreach (var groupPos in group)
                    {
                        groupSizes[groupPos] = groupSize;
                        processedPositions.Add(groupPos);
                    }
                }
            }

            return groupSizes;
        }

        public bool HasValidGroup(Block[,] grid, int minGroupSize)
        {
            HashSet<GridPosition> processed = new HashSet<GridPosition>();

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    GridPosition pos = new GridPosition(row, col);
                    if (processed.Contains(pos)) continue;

                    Block block = grid[row, col];
                    if (block == null || !block.IsActive) continue;

                    List<GridPosition> group = FindGroup(grid, pos);
                    
                    foreach (var groupPos in group)
                    {
                        processed.Add(groupPos);
                    }

                    if (group.Count >= minGroupSize)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsValidPosition(int row, int col)
        {
            return row >= 0 && row < _rows && col >= 0 && col < _columns;
        }
    }
}

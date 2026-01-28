using System.Collections.Generic;
using UnityEngine;
using CollapseGame.Core;

namespace CollapseGame.Managers
{
    public class SmartShuffler
    {
        private readonly int _rows;
        private readonly int _columns;
        private readonly int _minGroupSize;

        public SmartShuffler(int rows, int columns, int minGroupSize)
        {
            _rows = rows;
            _columns = columns;
            _minGroupSize = minGroupSize;
        }

        public void ShuffleWithGuaranteedMoves(Block[,] grid, System.Func<BlockColor, BlockData> getBlockData, int thresholdA, int thresholdB, int thresholdC)
        {
            List<BlockData> allBlockData = CollectBlockData(grid);
            if (allBlockData.Count < _minGroupSize) return;

            Dictionary<BlockColor, List<BlockData>> colorGroups = GroupByColor(allBlockData);
            BlockData[,] arrangement = CreateDeterministicArrangement(colorGroups, allBlockData.Count);

            ApplyArrangement(grid, arrangement, thresholdA, thresholdB, thresholdC);
        }

        private List<BlockData> CollectBlockData(Block[,] grid)
        {
            List<BlockData> result = new List<BlockData>();
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Block block = grid[row, col];
                    if (block != null && block.IsActive && block.Data != null)
                    {
                        result.Add(block.Data);
                    }
                }
            }
            return result;
        }

        private Dictionary<BlockColor, List<BlockData>> GroupByColor(List<BlockData> dataList)
        {
            Dictionary<BlockColor, List<BlockData>> groups = new Dictionary<BlockColor, List<BlockData>>();
            foreach (var data in dataList)
            {
                if (!groups.ContainsKey(data.color))
                    groups[data.color] = new List<BlockData>();
                groups[data.color].Add(data);
            }
            return groups;
        }

        private BlockData[,] CreateDeterministicArrangement(Dictionary<BlockColor, List<BlockData>> colorGroups, int totalCount)
        {
            BlockData[,] arrangement = new BlockData[_rows, _columns];
            bool[,] placed = new bool[_rows, _columns];

            List<BlockColor> sortedColors = new List<BlockColor>(colorGroups.Keys);
            sortedColors.Sort((a, b) => colorGroups[b].Count.CompareTo(colorGroups[a].Count));

            foreach (BlockColor color in sortedColors)
            {
                List<BlockData> colorData = colorGroups[color];
                if (colorData.Count < _minGroupSize) continue;

                List<GridPosition> clusterPositions = FindBestClusterPosition(placed, _minGroupSize);
                if (clusterPositions.Count >= _minGroupSize)
                {
                    for (int i = 0; i < clusterPositions.Count && colorData.Count > 0; i++)
                    {
                        GridPosition pos = clusterPositions[i];
                        arrangement[pos.Row, pos.Column] = colorData[0];
                        colorData.RemoveAt(0);
                        placed[pos.Row, pos.Column] = true;
                    }
                }
            }

            List<BlockData> remaining = new List<BlockData>();
            foreach (var colorData in colorGroups.Values)
            {
                remaining.AddRange(colorData);
            }

            int remainingIndex = 0;
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (!placed[row, col] && remainingIndex < remaining.Count)
                    {
                        arrangement[row, col] = remaining[remainingIndex++];
                        placed[row, col] = true;
                    }
                }
            }

            if (!HasValidGroup(arrangement))
            {
                ForceCreateValidGroup(arrangement, colorGroups);
            }

            return arrangement;
        }

        private List<GridPosition> FindBestClusterPosition(bool[,] placed, int size)
        {
            List<GridPosition> bestCluster = new List<GridPosition>();
            int bestScore = -1;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (placed[row, col]) continue;

                    List<GridPosition> cluster = GetConnectedEmptyPositions(placed, row, col, size);
                    if (cluster.Count >= size)
                    {
                        int score = CalculateClusterScore(cluster);
                        if (score > bestScore)
                        {
                            bestScore = score;
                            bestCluster = cluster;
                        }
                    }
                }
            }

            return bestCluster;
        }

        private static readonly int[] RowOffsets = { 0, 0, 1, -1 };
        private static readonly int[] ColOffsets = { 1, -1, 0, 0 };

        private List<GridPosition> GetConnectedEmptyPositions(bool[,] placed, int startRow, int startCol, int maxCount)
        {
            List<GridPosition> result = new List<GridPosition>(maxCount);
            Queue<GridPosition> queue = new Queue<GridPosition>();
            HashSet<GridPosition> visited = new HashSet<GridPosition>();

            GridPosition start = new GridPosition(startRow, startCol);
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0 && result.Count < maxCount)
            {
                GridPosition current = queue.Dequeue();
                result.Add(current);

                for (int i = 0; i < 4; i++)
                {
                    int newRow = current.Row + RowOffsets[i];
                    int newCol = current.Column + ColOffsets[i];

                    if (newRow < 0 || newRow >= _rows || newCol < 0 || newCol >= _columns)
                        continue;

                    GridPosition neighbor = new GridPosition(newRow, newCol);
                    if (visited.Contains(neighbor) || placed[newRow, newCol])
                        continue;

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return result;
        }

        private int CalculateClusterScore(List<GridPosition> cluster)
        {
            int score = cluster.Count * 10;

            int minRow = int.MaxValue, maxRow = int.MinValue;
            int minCol = int.MaxValue, maxCol = int.MinValue;

            foreach (var pos in cluster)
            {
                minRow = Mathf.Min(minRow, pos.Row);
                maxRow = Mathf.Max(maxRow, pos.Row);
                minCol = Mathf.Min(minCol, pos.Column);
                maxCol = Mathf.Max(maxCol, pos.Column);
            }

            int width = maxCol - minCol + 1;
            int height = maxRow - minRow + 1;
            int compactness = cluster.Count - (width * height - cluster.Count);
            score += compactness;

            return score;
        }

        private bool HasValidGroup(BlockData[,] arrangement)
        {
            bool[,] visited = new bool[_rows, _columns];

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (visited[row, col] || arrangement[row, col] == null)
                        continue;

                    int groupSize = FloodFill(arrangement, visited, row, col, arrangement[row, col].color);
                    if (groupSize >= _minGroupSize)
                        return true;
                }
            }

            return false;
        }

        private int FloodFill(BlockData[,] arrangement, bool[,] visited, int row, int col, BlockColor color)
        {
            if (row < 0 || row >= _rows || col < 0 || col >= _columns)
                return 0;
            if (visited[row, col] || arrangement[row, col] == null)
                return 0;
            if (arrangement[row, col].color != color)
                return 0;

            visited[row, col] = true;
            int count = 1;

            count += FloodFill(arrangement, visited, row + 1, col, color);
            count += FloodFill(arrangement, visited, row - 1, col, color);
            count += FloodFill(arrangement, visited, row, col + 1, color);
            count += FloodFill(arrangement, visited, row, col - 1, color);

            return count;
        }

        private void ForceCreateValidGroup(BlockData[,] arrangement, Dictionary<BlockColor, List<BlockData>> originalGroups)
        {
            BlockColor targetColor = BlockColor.Blue;
            int maxCount = 0;

            foreach (var kvp in originalGroups)
            {
                if (kvp.Value.Count > maxCount)
                {
                    maxCount = kvp.Value.Count;
                    targetColor = kvp.Key;
                }
            }

            List<GridPosition> targetPositions = new List<GridPosition>();
            if (_columns >= _minGroupSize)
            {
                for (int i = 0; i < _minGroupSize; i++)
                    targetPositions.Add(new GridPosition(0, i));
            }
            else
            {
                for (int i = 0; i < _minGroupSize; i++)
                    targetPositions.Add(new GridPosition(i, 0));
            }

            List<GridPosition> sourcePositions = new List<GridPosition>();
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (arrangement[row, col] != null && arrangement[row, col].color == targetColor)
                    {
                        GridPosition pos = new GridPosition(row, col);
                        if (!targetPositions.Contains(pos))
                            sourcePositions.Add(pos);
                    }
                }
            }

            for (int i = 0; i < targetPositions.Count && i < sourcePositions.Count; i++)
            {
                GridPosition target = targetPositions[i];
                GridPosition source = sourcePositions[i];

                if (arrangement[target.Row, target.Column]?.color == targetColor)
                    continue;

                BlockData temp = arrangement[target.Row, target.Column];
                arrangement[target.Row, target.Column] = arrangement[source.Row, source.Column];
                arrangement[source.Row, source.Column] = temp;
            }
        }

        private void ApplyArrangement(Block[,] grid, BlockData[,] arrangement, int thresholdA, int thresholdB, int thresholdC)
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Block block = grid[row, col];
                    BlockData data = arrangement[row, col];

                    if (block != null && data != null)
                    {
                        block.Initialize(data, new GridPosition(row, col), thresholdA, thresholdB, thresholdC);
                    }
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CollapseGame.Core;
using CollapseGame.Pooling;

namespace CollapseGame.Managers
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        
        [Header("Block Colors")]
        [SerializeField] private List<BlockData> allColors;
        
        [Header("References")]
        [SerializeField] private BlockPool blockPool;
        [SerializeField] private Transform boardParent;

        private Block[,] _grid;
        private int _rows;
        private int _columns;
        private int _colorCount;

        private GroupDetector _groupDetector;
        private SmartShuffler _shuffler;

        private bool _isProcessing;
        private Vector3 _boardOffset;

        public event System.Action<int> OnBlocksDestroyed;
        public event System.Action OnBoardSettled;
        public event System.Action OnDeadlockDetected;
        public event System.Action OnShuffleComplete;

        public bool IsProcessing => _isProcessing;
        public List<BlockData> AllColors => allColors;

        private void Awake()
        {
            ValidateColorData();
        }

        private void ValidateColorData()
        {
            if (allColors == null || allColors.Count == 0)
            {
                Debug.LogError("BoardManager: No BlockData assets assigned!");
            }
        }

        public void InitializeBoard()
        {
            _rows = gameConfig.rows;
            _columns = gameConfig.columns;
            _colorCount = Mathf.Min(gameConfig.colorCount, allColors.Count);

            if (_colorCount == 0)
            {
                Debug.LogError("No colors available!");
                return;
            }

            _groupDetector = new GroupDetector(_rows, _columns);
            _shuffler = new SmartShuffler(_rows, _columns, gameConfig.minGroupSize);

            int poolSize = Mathf.CeilToInt(_rows * _columns * gameConfig.poolSizeMultiplier);
            blockPool.Initialize(poolSize);

            _grid = new Block[_rows, _columns];

            _boardOffset = new Vector3(
                -(_columns - 1) * gameConfig.blockSpacing / 2f,
                -(_rows - 1) * gameConfig.blockSpacing / 2f,
                0
            );

            FillBoard();
            StartCoroutine(CheckAndResolveDeadlock());
        }

        public void ClearBoard()
        {
            if (_grid == null) return;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    if (_grid[row, col] != null)
                    {
                        blockPool.ReturnBlock(_grid[row, col]);
                        _grid[row, col] = null;
                    }
                }
            }
        }

        public void OnBlockClicked(Block block)
        {
            if (_isProcessing || block == null || !block.IsActive) return;
            StartCoroutine(ProcessBlockClick(block));
        }

        private IEnumerator ProcessBlockClick(Block block)
        {
            _isProcessing = true;

            List<GridPosition> group = _groupDetector.FindGroup(_grid, block.GridPosition);

            if (group.Count < gameConfig.minGroupSize)
            {
                _isProcessing = false;
                yield break;
            }

            yield return StartCoroutine(DestroyGroup(group));
            yield return StartCoroutine(ApplyGravity());
            yield return StartCoroutine(FillEmptySpaces());

            UpdateAllIcons();
            yield return StartCoroutine(CheckAndResolveDeadlock());

            _isProcessing = false;
            OnBoardSettled?.Invoke();
        }

        private IEnumerator DestroyGroup(List<GridPosition> group)
        {
            int destroyedCount = group.Count;

            group.Sort((a, b) => 
            {
                int rowCompare = a.Row.CompareTo(b.Row);
                return rowCompare != 0 ? rowCompare : a.Column.CompareTo(b.Column);
            });

            foreach (var pos in group)
            {
                Block block = _grid[pos.Row, pos.Column];
                if (block != null)
                {
                    StartCoroutine(PopBlock(block));
                    _grid[pos.Row, pos.Column] = null;
                }

                if (gameConfig.popDelay > 0)
                {
                    yield return new WaitForSeconds(gameConfig.popDelay);
                }
            }

            yield return new WaitForSeconds(gameConfig.popDuration);
            OnBlocksDestroyed?.Invoke(destroyedCount);
        }

        private IEnumerator PopBlock(Block block)
        {
            float elapsed = 0f;
            Vector3 originalScale = block.transform.localScale;

            while (elapsed < gameConfig.popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / gameConfig.popDuration;
                float scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
                block.transform.localScale = originalScale * scale * (1f - t);
                yield return null;
            }

            blockPool.ReturnBlock(block);
        }

        private IEnumerator ApplyGravity()
        {
            bool blocksMoving = true;
            List<(Block block, Vector3 target)> movingBlocks = new List<(Block, Vector3)>();

            while (blocksMoving)
            {
                blocksMoving = false;
                movingBlocks.Clear();

                for (int col = 0; col < _columns; col++)
                {
                    int writeRow = 0;

                    for (int row = 0; row < _rows; row++)
                    {
                        if (_grid[row, col] != null)
                        {
                            if (writeRow != row)
                            {
                                Block block = _grid[row, col];
                                _grid[writeRow, col] = block;
                                _grid[row, col] = null;

                                GridPosition newPos = new GridPosition(writeRow, col);
                                block.SetGridPosition(newPos);

                                Vector3 targetWorldPos = GridToWorldPosition(newPos);
                                movingBlocks.Add((block, targetWorldPos));

                                blocksMoving = true;
                            }
                            writeRow++;
                        }
                    }
                }

                if (movingBlocks.Count > 0)
                {
                    yield return StartCoroutine(AnimateBlocksFalling(movingBlocks));
                }
            }
        }

        private IEnumerator AnimateBlocksFalling(List<(Block block, Vector3 target)> blocks)
        {
            float elapsed = 0f;
            List<Vector3> startPositions = new List<Vector3>();

            foreach (var (block, _) in blocks)
            {
                startPositions.Add(block.transform.position);
            }

            while (elapsed < gameConfig.fallSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / gameConfig.fallSpeed);
                float easedT = t * t;

                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].block != null)
                    {
                        blocks[i].block.transform.position = Vector3.Lerp(
                            startPositions[i],
                            blocks[i].target,
                            easedT
                        );
                    }
                }

                yield return null;
            }

            foreach (var (block, target) in blocks)
            {
                if (block != null)
                {
                    block.transform.position = target;
                }
            }
        }

        private IEnumerator FillEmptySpaces()
        {
            List<(Block block, Vector3 start, Vector3 target)> newBlocks = 
                new List<(Block, Vector3, Vector3)>();

            for (int col = 0; col < _columns; col++)
            {
                int emptyCount = 0;

                for (int row = _rows - 1; row >= 0; row--)
                {
                    if (_grid[row, col] == null)
                    {
                        emptyCount++;
                    }
                }

                int spawnOffset = 1;
                for (int row = 0; row < _rows; row++)
                {
                    if (_grid[row, col] == null)
                    {
                        GridPosition gridPos = new GridPosition(row, col);
                        Block newBlock = SpawnBlock(gridPos);
                        
                        Vector3 startPos = GridToWorldPosition(new GridPosition(_rows + spawnOffset, col));
                        Vector3 targetPos = GridToWorldPosition(gridPos);
                        
                        newBlock.transform.position = startPos;
                        newBlocks.Add((newBlock, startPos, targetPos));
                        
                        spawnOffset++;
                    }
                }
            }

            if (newBlocks.Count > 0)
            {
                float elapsed = 0f;
                float duration = gameConfig.fallSpeed * 2f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = t * t;

                    foreach (var (block, start, target) in newBlocks)
                    {
                        if (block != null)
                        {
                            block.transform.position = Vector3.Lerp(start, target, easedT);
                        }
                    }

                    yield return null;
                }

                foreach (var (block, _, target) in newBlocks)
                {
                    if (block != null)
                    {
                        block.transform.position = target;
                    }
                }
            }
        }

        private IEnumerator CheckAndResolveDeadlock()
        {
            bool hasValidMoves = _groupDetector.HasValidGroup(_grid, gameConfig.minGroupSize);

            if (!hasValidMoves)
            {
                OnDeadlockDetected?.Invoke();
                
                yield return new WaitForSeconds(0.5f);
                
                _shuffler.ShuffleWithGuaranteedMoves(
                    _grid, 
                    GetBlockDataForColor,
                    gameConfig.thresholdA,
                    gameConfig.thresholdB,
                    gameConfig.thresholdC
                );
                
                RefreshBlockPositions();
                UpdateAllIcons();
                
                OnShuffleComplete?.Invoke();
            }
        }

        private void FillBoard()
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    GridPosition pos = new GridPosition(row, col);
                    SpawnBlock(pos);
                }
            }

            UpdateAllIcons();
        }

        private Block SpawnBlock(GridPosition position)
        {
            Block block = blockPool.GetBlock();
            BlockData colorData = GetRandomColorData();
            
            block.Initialize(
                colorData, 
                position,
                gameConfig.thresholdA,
                gameConfig.thresholdB,
                gameConfig.thresholdC
            );
            block.transform.SetParent(boardParent);
            block.transform.position = GridToWorldPosition(position);
            block.OnBlockClicked += OnBlockClicked;
            
            _grid[position.Row, position.Column] = block;

            return block;
        }

        private void UpdateAllIcons()
        {
            Dictionary<GridPosition, int> groupSizes = _groupDetector.FindAllGroupSizes(_grid);

            foreach (var kvp in groupSizes)
            {
                Block block = _grid[kvp.Key.Row, kvp.Key.Column];
                if (block != null)
                {
                    block.UpdateVisual(kvp.Value);
                }
            }
        }

        private BlockData GetRandomColorData()
        {
            int randomIndex = Random.Range(0, _colorCount);
            return allColors[randomIndex];
        }

        public BlockData GetBlockDataForColor(BlockColor color)
        {
            foreach (var data in allColors)
            {
                if (data.color == color)
                    return data;
            }
            return allColors.Count > 0 ? allColors[0] : null;
        }

        private void RefreshBlockPositions()
        {
            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    Block block = _grid[row, col];
                    if (block != null)
                    {
                        block.transform.position = GridToWorldPosition(new GridPosition(row, col));
                    }
                }
            }
        }

        private Vector3 GridToWorldPosition(GridPosition gridPos)
        {
            return new Vector3(
                gridPos.Column * gameConfig.blockSpacing,
                gridPos.Row * gameConfig.blockSpacing,
                0
            ) + _boardOffset + boardParent.position;
        }

        private BlockColor GetRandomColor()
        {
            return (BlockColor)Random.Range(0, _colorCount);
        }
    }
}

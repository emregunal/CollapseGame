using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CollapseGame.Core;

namespace CollapseGame.Managers
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [Range(2, 10)]
        public int M = 8;
        
        [Range(2, 10)]
        public int N = 8;
        
        [Range(1, 6)]
        public int K = 4;

        [Header("Icon Thresholds")]
        public int thresholdA = 5;
        public int thresholdB = 8;
        public int thresholdC = 10;

        [Header("Prefab and Colors")]
        public Block blockPrefab;
        public List<BlockData> allColorData;

        [Header("Animation Settings")]
        public float fallSpeed = 0.15f;
        public float popDuration = 0.2f;
        public float cellSize = 1.5f;
        
        [Range(0.5f, 1f)]
        public float blockSizeRatio = 0.95f;

        private Block[,] board;
        private bool isProcessing;
        private SmartShuffler _shuffler;

        public event System.Action<int> OnBlocksDestroyed;
        public event System.Action OnBoardSettled;

        void Start()
        {
            GenerateGrid();
        }

        public void GenerateGrid()
        {
            ClearBoard();
            board = new Block[M, N];
            _shuffler = new SmartShuffler(M, N, 2);
            
            for (int row = 0; row < M; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    SpawnBlock(row, col);
                }
            }

            UpdateAllIcons();
            CheckForDeadlock();
        }

        private Block SpawnBlock(int row, int col)
        {
            Vector2 position = GetWorldPosition(row, col);
            Block newBlock = Instantiate(blockPrefab, position, Quaternion.identity, transform);
            
            int randomColorIndex = Random.Range(0, Mathf.Min(K, allColorData.Count));
            BlockData colorData = allColorData[randomColorIndex];
            
            newBlock.Initialize(colorData, new GridPosition(row, col), thresholdA, thresholdB, thresholdC);
            
            SpriteRenderer sr = newBlock.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteWidth = sr.sprite.bounds.size.x;
                float spriteHeight = sr.sprite.bounds.size.y;
                float targetSize = cellSize * blockSizeRatio;
                float scaleX = targetSize / spriteWidth;
                float scaleY = targetSize / spriteHeight;
                float scale = Mathf.Min(scaleX, scaleY);
                newBlock.transform.localScale = new Vector3(scale, scale, 1f);
            }
            
            newBlock.OnBlockClicked += OnBlockClicked;
            board[row, col] = newBlock;
            
            return newBlock;
        }

        private Vector2 GetWorldPosition(int row, int col)
        {
            float offsetX = -(N - 1) * cellSize / 2f;
            float offsetY = -(M - 1) * cellSize / 2f;
            
            return new Vector2(col * cellSize + offsetX, row * cellSize + offsetY);
        }

        public List<Block> GetMatch(Block startBlock)
        {
            List<Block> matchGroup = new List<Block>();
            Queue<Block> checkQueue = new Queue<Block>();
            HashSet<Block> visited = new HashSet<Block>();

            checkQueue.Enqueue(startBlock);
            matchGroup.Add(startBlock);
            visited.Add(startBlock);

            while (checkQueue.Count > 0)
            {
                Block current = checkQueue.Dequeue();
                
                foreach (Block neighbor in GetNeighbors(current))
                {
                    if (neighbor.Color == startBlock.Color && !visited.Contains(neighbor))
                    {
                        matchGroup.Add(neighbor);
                        checkQueue.Enqueue(neighbor);
                        visited.Add(neighbor);
                    }
                }
            }

            return matchGroup;
        }

        private List<Block> GetNeighbors(Block block)
        {
            List<Block> neighbors = new List<Block>();
            GridPosition pos = block.GridPosition;

            int[] rowOffsets = { 1, -1, 0, 0 };
            int[] colOffsets = { 0, 0, -1, 1 };

            for (int i = 0; i < 4; i++)
            {
                int newRow = pos.Row + rowOffsets[i];
                int newCol = pos.Column + colOffsets[i];

                if (newRow >= 0 && newRow < M && newCol >= 0 && newCol < N)
                {
                    Block neighbor = board[newRow, newCol];
                    if (neighbor != null && neighbor.IsActive)
                        neighbors.Add(neighbor);
                }
            }

            return neighbors;
        }

        public void UpdateAllIcons()
        {
            HashSet<Block> processed = new HashSet<Block>();

            for (int row = 0; row < M; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    Block block = board[row, col];
                    
                    if (block == null || !block.IsActive || processed.Contains(block))
                        continue;

                    List<Block> group = GetMatch(block);
                    int groupSize = group.Count;

                    foreach (Block member in group)
                    {
                        member.UpdateVisual(groupSize);
                        processed.Add(member);
                    }
                }
            }
        }

        private void OnBlockClicked(Block clickedBlock)
        {
            if (isProcessing) return;

            List<Block> matchGroup = GetMatch(clickedBlock);

            if (matchGroup.Count >= 2)
            {
                StartCoroutine(ProcessMatch(matchGroup));
            }
        }

        private IEnumerator ProcessMatch(List<Block> matchGroup)
        {
            isProcessing = true;

            yield return StartCoroutine(DestroyBlocks(matchGroup));
            yield return StartCoroutine(ApplyGravity());
            yield return StartCoroutine(FillEmptySpaces());

            UpdateAllIcons();
            CheckForDeadlock();

            isProcessing = false;
            OnBoardSettled?.Invoke();
        }

        private IEnumerator DestroyBlocks(List<Block> blocks)
        {
            int count = blocks.Count;

            foreach (Block block in blocks)
            {
                GridPosition pos = block.GridPosition;
                board[pos.Row, pos.Column] = null;
                StartCoroutine(PopAnimation(block));
            }

            yield return new WaitForSeconds(popDuration);
            OnBlocksDestroyed?.Invoke(count);
        }

        private IEnumerator PopAnimation(Block block)
        {
            float elapsed = 0;
            Vector3 originalScale = block.transform.localScale;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;
                float scale = 1f + 0.3f * Mathf.Sin(t * Mathf.PI) * (1f - t);
                block.transform.localScale = originalScale * scale;
                yield return null;
            }

            ReturnBlock(block);
        }

        private void ReturnBlock(Block block)
        {
            Destroy(block.gameObject);
        }

        private IEnumerator ApplyGravity()
        {
            bool blocksMoved;

            do
            {
                blocksMoved = false;
                List<(Block block, Vector2 target)> movingBlocks = new List<(Block, Vector2)>();

                for (int col = 0; col < N; col++)
                {
                    for (int row = 0; row < M - 1; row++)
                    {
                        if (board[row, col] == null)
                        {
                            for (int aboveRow = row + 1; aboveRow < M; aboveRow++)
                            {
                                if (board[aboveRow, col] != null)
                                {
                                    Block block = board[aboveRow, col];
                                    board[row, col] = block;
                                    board[aboveRow, col] = null;
                                    
                                    block.SetGridPosition(new GridPosition(row, col));
                                    movingBlocks.Add((block, GetWorldPosition(row, col)));
                                    
                                    blocksMoved = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (movingBlocks.Count > 0)
                {
                    yield return StartCoroutine(AnimateBlocks(movingBlocks));
                }

            } while (blocksMoved);
        }

        private IEnumerator FillEmptySpaces()
        {
            List<(Block block, Vector2 start, Vector2 target)> newBlocks = 
                new List<(Block, Vector2, Vector2)>();

            for (int col = 0; col < N; col++)
            {
                int spawnOffset = 1;

                for (int row = 0; row < M; row++)
                {
                    if (board[row, col] == null)
                    {
                        Block newBlock = SpawnBlock(row, col);
                        
                        Vector2 startPos = GetWorldPosition(M + spawnOffset, col);
                        Vector2 targetPos = GetWorldPosition(row, col);
                        
                        newBlock.transform.position = startPos;
                        newBlocks.Add((newBlock, startPos, targetPos));
                        
                        spawnOffset++;
                    }
                }
            }

            if (newBlocks.Count > 0)
            {
                float elapsed = 0;
                float duration = fallSpeed * 2f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float easedT = t * t;

                    foreach (var (block, start, target) in newBlocks)
                    {
                        if (block != null)
                        {
                            block.transform.position = Vector2.Lerp(start, target, easedT);
                        }
                    }

                    yield return null;
                }
            }
        }

        private IEnumerator AnimateBlocks(List<(Block block, Vector2 target)> blocks)
        {
            float elapsed = 0;
            List<Vector2> startPositions = new List<Vector2>();

            foreach (var (block, _) in blocks)
            {
                startPositions.Add(block.transform.position);
            }

            while (elapsed < fallSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallSpeed);
                float easedT = t * t;

                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].block != null)
                    {
                        blocks[i].block.transform.position = Vector2.Lerp(
                            startPositions[i],
                            blocks[i].target,
                            easedT
                        );
                    }
                }

                yield return null;
            }
        }

        private void CheckForDeadlock()
        {
            if (!HasValidMove())
            {
                StartCoroutine(ShuffleBoard());
            }
        }

        private bool HasValidMove()
        {
            HashSet<Block> checkedBlocks = new HashSet<Block>();

            for (int row = 0; row < M; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    Block block = board[row, col];
                    if (block == null || checkedBlocks.Contains(block)) continue;

                    List<Block> group = GetMatch(block);
                    
                    foreach (var b in group)
                        checkedBlocks.Add(b);

                    if (group.Count >= 2)
                        return true;
                }
            }

            return false;
        }

        private IEnumerator ShuffleBoard()
        {
            yield return new WaitForSeconds(0.5f);

            _shuffler.ShuffleWithGuaranteedMoves(
                board,
                GetBlockDataForColor,
                thresholdA,
                thresholdB,
                thresholdC
            );

            UpdateAllIcons();
        }

        private BlockData GetBlockDataForColor(BlockColor color)
        {
            foreach (var data in allColorData)
            {
                if (data.color == color)
                    return data;
            }
            return allColorData.Count > 0 ? allColorData[0] : null;
        }

        public void ClearBoard()
        {
            if (board == null) return;

            for (int row = 0; row < M; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    if (board[row, col] != null)
                    {
                        ReturnBlock(board[row, col]);
                        board[row, col] = null;
                    }
                }
            }
        }

        public void RestartGame()
        {
            GenerateGrid();
        }

        public void RestartGame(int rows, int cols, int colors)
        {
            M = Mathf.Clamp(rows, 2, 10);
            N = Mathf.Clamp(cols, 2, 10);
            K = Mathf.Clamp(colors, 1, 6);
            GenerateGrid();
        }
    }
}

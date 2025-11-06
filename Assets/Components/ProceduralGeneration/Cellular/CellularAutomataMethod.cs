using UnityEngine;
using Components.ProceduralGeneration; 
using Cysharp.Threading.Tasks;          
using System.Threading;                 

namespace Components.ProceduralGeneration.Cellular
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Cellular Automata", fileName = "CellularAutomata")]
    public class CellularAutomataMethod : ProceduralGenerationMethod
    {
        [Header("Automaton Settings")]
        [SerializeField, Range(0,100)] private int randomFillPercent = 48; 
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int seed = 12345;         
        [SerializeField] private int iterations = 5;        
        [SerializeField] private int groundThreshold = 4;   

        private bool[,] _grid;
        private bool[,] _buffer;
        private bool[,] _applied; 
        private bool _hasAppliedOnce;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            int width = Grid.Width;
            int height = Grid.Lenght;
            EnsureAllocated(width, height);
            _hasAppliedOnce = false;

            
            int initSeed = useRandomSeed ? System.DateTime.Now.Ticks.GetHashCode() : seed;
            Random.InitState(initSeed);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _grid[x, y] = (Random.value * 100f) < randomFillPercent;
                    
                }
            }

            await ApplyToCellsAndDelay(width, height, cancellationToken);

            
            for (int i = 0; i < iterations; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                StepOnce(width, height);
                await ApplyToCellsAndDelay(width, height, cancellationToken);
                //ApplyToCellsAndDelay(width, height, cancellationToken);
            }
        }

        private async UniTask ApplyToCellsAndDelay(int width, int height, CancellationToken cancellationToken)
        {
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    bool desired = _grid[x, y];
                    bool mustApply = !_hasAppliedOnce || _applied[x, y] != desired;
                    if (!mustApply) continue;

                    if (!Grid.TryGetCellByCoordinates(x, y, out var cell))
                        continue;

                    AddTileToCell(cell, desired ? GRASS_TILE_NAME : WATER_TILE_NAME, true);
                    _applied[x, y] = desired;
                    
                    
                }
            }

            _hasAppliedOnce = true;

            
            if (GridGenerator != null && GridGenerator.StepDelay > 0)
            {
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }
            else
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        private void EnsureAllocated(int width, int height)
        {
            if (_grid == null || _grid.GetLength(0) != width || _grid.GetLength(1) != height)
                _grid = new bool[width, height];
            if (_buffer == null || _buffer.GetLength(0) != width || _buffer.GetLength(1) != height)
                _buffer = new bool[width, height];
            if (_applied == null || _applied.GetLength(0) != width || _applied.GetLength(1) != height)
                _applied = new bool[width, height];
        }

        private void StepOnce(int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int neighbors = CountGroundNeighbors(x, y, width, height);
                    _buffer[x, y] = neighbors >= groundThreshold;
                }
            }
            var tmp = _grid; _grid = _buffer; _buffer = tmp;
        }

        private int CountGroundNeighbors(int x, int y, int width, int height)
        {
            int count = 0;
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = y + dy;
                if (ny < 0 || ny >= height) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx;
                    if (nx < 0 || nx >= width) continue;
                    if (_grid[nx, ny]) count++;
                }
            }
            return count;
        }
    }
}

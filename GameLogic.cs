using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    public class GameLogic
    {
        private int _gridSize = 10;
        public int GridSize { get => _gridSize; private set => _gridSize = value; }

        private int _bombCount = 5;
        public int BombCount { get => _bombCount; private set => _bombCount = value; }

        private bool ClearScreenOnDisplay = false;

        private int _currentTurn = 0;
        public int CurrentTurn { get => _currentTurn; private set => _currentTurn = value; }

        private int _coveredTiles = 0;
        public int CoveredTiles { get => _coveredTiles; private set => _coveredTiles = value; }

        private GridTile[,] Tiles;

        // the first tile you uncover
        private (int x, int y) safeTile = (0, 0);

        private bool isGameOver = false;
        private bool isGameWin = false;

        private const int MaxGridSize = 26;
        private const int GridAlignment = -2;
        private const string DeathMessage = "YOU DEAD!";
        private const string WinMessage = "You win!";

        private Random random = new Random();
        public GameLogic(int gridSize = 10, int bombCount = 5, bool clearScreenOnDisplay = false)
        {
            GridSize = gridSize;
            BombCount = bombCount;
            ClearScreenOnDisplay = clearScreenOnDisplay;
            Normalize();
        }

        private void Normalize()
        {
            GridSize = Math.Min(GridSize, MaxGridSize);
            BombCount = Math.Min(BombCount, GridSize * GridSize - 1);
        }

        public void StartGame()
        {
            CurrentTurn = 0;
            CoveredTiles = GridSize * GridSize;
            InitializeGrid();

            #region Game Loop
            do
            {
                DisplayGrid();
                var chosenTile = AwaitPlayerInput();
                UncoverTile(chosenTile);
                CheckWinCondition();
            }
            while (!isGameOver && !isGameWin);
            #endregion
            
            if (isGameOver)
            {
                DisplayGrid(displayAll: true);
                Console.WriteLine("\n\n" + DeathMessage);
            }
            else if(isGameWin)
            {
                DisplayGrid(displayAll: true);
                Console.WriteLine("\n\n" + WinMessage);
            }

            EndGame();
        }

        private void EndGame()
        {
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();
        }

        private void CheckWinCondition()
        {
            if(CoveredTiles == BombCount)
                isGameWin = true;
        }

        public (int x, int y) AwaitPlayerInput()
        {
            (int x, int y) targetTile;
            bool isValid = false;
            do
            {
                targetTile = ParseInputTile(Console.ReadLine());
                isValid = IsPlayerInputValid(targetTile);
            }
            while (!isValid);

            CurrentTurn++;
            if(CurrentTurn <= 1)
            {
                safeTile = targetTile;
                GenerateGrid();
            }

            return targetTile;
        }

        private (int x, int y) ParseInputTile(string input)
        {
            input = input.ToUpper().Trim();

            int? x = null, y = null;
            (int, int) errorTuple = (-1, -1);

            StringBuilder sb = new StringBuilder();
            foreach(char c in input)
            {
                if (x == null)
                {
                    if (!char.IsDigit(c))
                        x = LetterToNumber(c);
                    else
                        return errorTuple;
                }
                else
                {
                    if (char.IsDigit(c))
                        sb.Append(c);
                    else
                        return errorTuple;
                        
                }
            }

            if(!int.TryParse(sb.ToString(), out int sbParse))
                return errorTuple;

            y = sbParse;

            if(x == null || y == null)
                return errorTuple;

            return (x: x.Value - 1, y: y.Value - 1); 
        }

        private bool IsPlayerInputValid((int x, int y) targetTile)
        {
            return targetTile.x >= 0 && targetTile.x < GridSize && targetTile.y >= 0 && targetTile.y < GridSize;
        }

        private void UncoverEdgeTile((int x, int y) tileCoords)
        {
            Tiles[tileCoords.y, tileCoords.x].IsUncovered = true;
            CoveredTiles--;
        }

        public void UncoverTile((int x, int y) tileCoords)
        {
            var targetTile = Tiles[tileCoords.y, tileCoords.x];
            targetTile.IsUncovered = true;
            CoveredTiles--;

            if (targetTile.IsBomb)
            {
                isGameOver = true;
                return;
            }

            Func<(int x, int y), bool> neighboringTileCondition = ((int x, int y) coords) => 
                !Tiles[coords.y, coords.x].IsBomb
                && Tiles[coords.y, coords.x].ContactCount == 0
                && !Tiles[coords.y, coords.x].IsUncovered;

            Func<(int x, int y), bool> neighoringTileEdgeCondition = ((int x, int y) coords) =>
                !Tiles[coords.y, coords.x].IsBomb
                && !Tiles[coords.y, coords.x].IsUncovered;

            // uncover neighboring tiles in 8 directions
            for(int dy = -1; dy <= 1; dy++)
            {
                for(int dx = -1; dx <= 1; dx++)
                {
                    // skip checking this tile
                    if (dx == 0 && dy == 0)
                        continue;

                    int neighborX = tileCoords.x + dx;
                    int neighborY = tileCoords.y + dy;

                    // check if neighboring tile is within the array (edge case check)
                    if(neighborX >= 0 && neighborX < GridSize && neighborY >= 0 && neighborY < GridSize)
                    {
                        var neighborCoords = (x: neighborX, y: neighborY);

                        if (neighboringTileCondition(neighborCoords))
                        {
                            UncoverTile(neighborCoords);
                        }
                        else if (neighoringTileEdgeCondition(neighborCoords))
                        {
                            UncoverEdgeTile(neighborCoords);
                        }
                    }
                }
            }
        }

        public void InitializeGrid()
        {
            // create all tiles
            Tiles = new GridTile[GridSize, GridSize];
            for (int i = 0; i < GridSize; i++)
            {
                for (int j = 0; j < GridSize; j++)
                {
                    GridTile currTile = new GridTile();
                    Tiles[i, j] = currTile;
                }
            }
        }

        public void GenerateGrid()
        {
            List<(int x, int y)> bombTiles = GetBombLocations();
               
            // setup bombs and contact tiles
            foreach((int x, int y) currBomb in bombTiles)
            {
                Tiles[currBomb.y, currBomb.x].IsBomb = true;

                for(int dy = -1; dy <= 1; dy++)
                {
                    for(int dx = -1; dx <= 1; dx++)
                    {
                        if (dy == 0 && dx == 0)
                            continue;

                        int neighborX = currBomb.x + dx;
                        int neighborY = currBomb.y + dy;

                        if(neighborX >= 0 && neighborX < GridSize && neighborY >= 0 && neighborY < GridSize)
                        {
                            Tiles[neighborY, neighborX].ContactCount++;
                        }
                    }
                }
            }
                    
        }

        private char NumberToLetter(int n)
        {
            return (char)('A' + n - 1);
        }

        private int LetterToNumber(char c)
        {
            return (c - 'A') + 1;
        }

        private void WriteTopLine(bool writeColumns = false)
        {
            if (writeColumns)
            {
                Console.Write("    ");
                for (int i = 1; i < GridSize + 1; i++)
                    Console.Write($"{NumberToLetter(i),GridAlignment}");
                Console.WriteLine();
            }

            Console.Write("   ─");
            for (int i = 0; i < GridSize; i++)
                Console.Write($"{"──",GridAlignment}");
            Console.WriteLine();
        }

        private void WriteSideLine(bool writeRows = false, int currentRow = 0)
        {
            if (writeRows) Console.Write($"{currentRow, GridAlignment}");
            Console.Write($"{"|", GridAlignment}");
        }

        public void DisplayGrid(bool displayAll = false)
        {
            Console.WriteLine();
            if (ClearScreenOnDisplay)
                Console.Clear();

            WriteTopLine(writeColumns: true);

            for(int i = 0; i < GridSize; i++)
            {
                WriteSideLine(writeRows: true, currentRow: i + 1);

                for(int j = 0; j < GridSize; j++)
                {
                    var currTile = Tiles[i, j];

                    if (currTile.IsUncovered || displayAll)
                    {
                        if(!currTile.IsBomb)
                            Console.Write($"{currTile.ContactCount, GridAlignment}");
                        else
                            Console.Write($"{"*", GridAlignment}");
                    }
                    else
                    {
                        Console.Write($"{".", GridAlignment}");
                    }
                }

                WriteSideLine();
                Console.WriteLine();
            }

            WriteTopLine();
        }

        private List<(int x, int y)> GetBombLocations()
        {
            List<(int x, int y)> bombTiles = new List<(int x, int y)>();
            do
            {
                int x = random.Next(0, GridSize);
                int y = random.Next(0, GridSize);
                (int x, int y) coords = (x, y);

                if (bombTiles.Contains(coords) || coords == safeTile)
                    continue;

                bombTiles.Add(coords);
            }
            while(bombTiles.Count != BombCount);
            return bombTiles;
        }

        public class GridTile
        {
            public bool IsUncovered;
            public bool IsBomb;
            public int ContactCount;

            public GridTile(bool isUncovered = false, bool isBomb = false, int contactCount = 0)
            {
                IsUncovered = isUncovered;
                IsBomb = isBomb;
                ContactCount = contactCount;
            }
        }
    }
}

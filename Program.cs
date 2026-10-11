using Minesweeper;

class Program
{
    public static void Main(string[] args)
    {
        int bombCount, gridSize;
        bool clearOnDisplay;

        if (args.Length < 1 || !int.TryParse(args[0], out gridSize))
            gridSize = 10;
        if (args.Length < 2 || !int.TryParse(args[1], out bombCount))
            bombCount = 6;
        if(args.Length < 3 || !bool.TryParse(args[2], out clearOnDisplay))
            clearOnDisplay = false;

        GameLogic gameInstance = new GameLogic(gridSize, bombCount, clearOnDisplay);
        gameInstance.StartGame();
    }
}
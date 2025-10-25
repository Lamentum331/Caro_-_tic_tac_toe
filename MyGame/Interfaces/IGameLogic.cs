using System.Drawing;

namespace MyGame.Interfaces;

public interface IGameLogic
{
    void Reset();
    bool MakeMove(Point position, bool isPlayer1Turn, string[,] board);
    bool CheckWin(int row, int col, string[,] board);
    Point? GetAIMove(string[,] board);
}
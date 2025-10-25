using System.Drawing;
using MyGame.Interfaces;
using MyGame.Models;

namespace MyGame.GameLogic;

public class CaroLogic : IGameLogic
{
    private readonly Random random = new();

    public void Reset()
    {
        // No state to reset
    }

    public bool MakeMove(Point position, bool isPlayer1Turn, string[,] board)
    {
        if (!string.IsNullOrEmpty(board[position.X, position.Y]))
            return false;

        board[position.X, position.Y] = isPlayer1Turn ? "X" : "O";
        return true;
    }

    public bool CheckWin(int row, int col, string[,] board)
    {
        string symbol = board[row, col];
        if (string.IsNullOrEmpty(symbol))
            return false;

        // Check all directions
        int[][] directions = new int[][]
        {
            new int[] { 1, 0 },   // vertical
            new int[] { 0, 1 },   // horizontal
            new int[] { 1, 1 },   // diagonal
            new int[] { 1, -1 }   // anti-diagonal
        };

        foreach (var dir in directions)
        {
            if (CheckDirection(row, col, dir[0], dir[1], symbol, board))
                return true;
        }

        return false;
    }

    private bool CheckDirection(int row, int col, int deltaRow, int deltaCol, string symbol, string[,] board)
    {
        int count = 1;
        
        // Check forward direction
        int r = row + deltaRow;
        int c = col + deltaCol;
        while (IsValidPosition(r, c) && board[r, c] == symbol)
        {
            count++;
            if (count == 5) return true;
            r += deltaRow;
            c += deltaCol;
        }

        // Check backward direction
        r = row - deltaRow;
        c = col - deltaCol;
        while (IsValidPosition(r, c) && board[r, c] == symbol)
        {
            count++;
            if (count == 5) return true;
            r -= deltaRow;
            c -= deltaCol;
        }

        return false;
    }

    private bool IsValidPosition(int row, int col)
    {
        return row >= 0 && row < GameSettings.BOARD_SIZE_CARO && 
               col >= 0 && col < GameSettings.BOARD_SIZE_CARO;
    }

    public Point? GetAIMove(string[,] board)
    {
        // Check for winning move (5 in a row)
        Point? winMove = FindWinningMove("O", 5, board);
        if (winMove.HasValue) return winMove;

        // Block opponent's winning move
        Point? blockMove = FindWinningMove("X", 4, board);
        if (blockMove.HasValue) return blockMove;

        // Look for offensive opportunities
        Point? offensiveMove = FindWinningMove("O", 3, board);
        if (offensiveMove.HasValue) return offensiveMove;

        // Look for defensive moves
        Point? defensiveMove = FindWinningMove("X", 3, board);
        if (defensiveMove.HasValue) return defensiveMove;

        // Make a strategic move
        Point? strategicMove = FindStrategicMove(board);
        if (strategicMove.HasValue) return strategicMove;

        return FindRandomMove(board);
    }

    private Point? FindWinningMove(string symbol, int targetCount, string[,] board)
    {
        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                if (!string.IsNullOrEmpty(board[i, j]))
                    continue;

                if (CheckConsecutive(i, j, symbol, targetCount, board))
                {
                    return new Point(i, j);
                }
            }
        }
        return null;
    }

    private bool CheckConsecutive(int row, int col, string symbol, int targetCount, string[,] board)
    {
        int[][] directions = new int[][]
        {
            new int[] { 1, 0 },   // vertical
            new int[] { 0, 1 },   // horizontal
            new int[] { 1, 1 },   // diagonal
            new int[] { 1, -1 }   // anti-diagonal
        };

        foreach (var dir in directions)
        {
            int count = 1;
            
            // Check forward direction
            int r = row + dir[0];
            int c = col + dir[1];
            while (IsValidPosition(r, c) && board[r, c] == symbol)
            {
                count++;
                if (count >= targetCount) return true;
                r += dir[0];
                c += dir[1];
            }

            // Check backward direction
            r = row - dir[0];
            c = col - dir[1];
            while (IsValidPosition(r, c) && board[r, c] == symbol)
            {
                count++;
                if (count >= targetCount) return true;
                r -= dir[0];
                c -= dir[1];
            }

            if (count >= targetCount)
                return true;
        }
        return false;
    }

    private Point? FindStrategicMove(string[,] board)
    {
        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                if (!string.IsNullOrEmpty(board[i, j]))
                {
                    // Check adjacent cells
                    for (int di = -1; di <= 1; di++)
                    {
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            if (di == 0 && dj == 0) continue;
                            
                            int ni = i + di;
                            int nj = j + dj;
                            if (IsValidPosition(ni, nj) && 
                                string.IsNullOrEmpty(board[ni, nj]))
                            {
                                return new Point(ni, nj);
                            }
                        }
                    }
                }
            }
        }
        return null;
    }

    private Point? FindRandomMove(string[,] board)
    {
        var validMoves = new List<Point>();
        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                if (string.IsNullOrEmpty(board[i, j]))
                {
                    validMoves.Add(new Point(i, j));
                }
            }
        }

        return validMoves.Count > 0 ? validMoves[random.Next(validMoves.Count)] : null;
    }
}
using System.Drawing;
using MyGame.Interfaces;
using MyGame.Models;

namespace MyGame.GameLogic;

public class TicTacToeLogic : IGameLogic
{
    private readonly Random random = new();
    private Queue<Point> player1Moves = new();
    private Queue<Point> player2Moves = new();
    private const int MAX_MOVES = 3;

    public void Reset()
    {
        player1Moves.Clear();
        player2Moves.Clear();
    }

    public bool MakeMove(Point position, bool isPlayer1Turn, string[,] board)
    {
        if (!string.IsNullOrEmpty(board[position.X, position.Y]))
            return false;

        Queue<Point> currentPlayerMoves = isPlayer1Turn ? player1Moves : player2Moves;
        string symbol = isPlayer1Turn ? "X" : "O";

        // Nếu đã đánh đủ 3 quân, xóa quân cũ nhất
        if (currentPlayerMoves.Count >= MAX_MOVES)
        {
            Point oldestMove = currentPlayerMoves.Dequeue();
            board[oldestMove.X, oldestMove.Y] = "";
        }

        // Đánh quân mới
        board[position.X, position.Y] = symbol;
        currentPlayerMoves.Enqueue(position);

        return true;
    }

    public bool CheckWin(int row, int col, string[,] board)
    {
        string symbol = board[row, col];
        if (string.IsNullOrEmpty(symbol))
            return false;

        // Check horizontal
        if (board[row, 0] == symbol && board[row, 1] == symbol && board[row, 2] == symbol)
            return true;

        // Check vertical
        if (board[0, col] == symbol && board[1, col] == symbol && board[2, col] == symbol)
            return true;

        // Check diagonal
        if (row == col && board[0, 0] == symbol && board[1, 1] == symbol && board[2, 2] == symbol)
            return true;

        // Check anti-diagonal
        if (row + col == 2 && board[0, 2] == symbol && board[1, 1] == symbol && board[2, 0] == symbol)
            return true;

        return false;
    }

    public Point? GetAIMove(string[,] board)
    {
        // Check for winning move
        Point? winMove = FindWinningMove("O", board);
        if (winMove.HasValue) return winMove;

        // Block opponent's winning move
        Point? blockMove = FindWinningMove("X", board);
        if (blockMove.HasValue) return blockMove;

        // Try center if available
        if (string.IsNullOrEmpty(board[1, 1]))
            return new Point(1, 1);

        // Try corners
        Point[] corners = new Point[] 
        { 
            new Point(0, 0), 
            new Point(0, 2), 
            new Point(2, 0), 
            new Point(2, 2) 
        };

        foreach (var corner in corners)
        {
            if (string.IsNullOrEmpty(board[corner.X, corner.Y]))
                return corner;
        }

        // Try edges
        Point[] edges = new Point[] 
        { 
            new Point(0, 1), 
            new Point(1, 0), 
            new Point(1, 2), 
            new Point(2, 1) 
        };

        foreach (var edge in edges)
        {
            if (string.IsNullOrEmpty(board[edge.X, edge.Y]))
                return edge;
        }

        return FindRandomMove(board);
    }

    private Point? FindWinningMove(string symbol, string[,] board)
    {
        for (int i = 0; i < GameSettings.BOARD_SIZE_TIC_TAC_TOE; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_TIC_TAC_TOE; j++)
            {
                if (string.IsNullOrEmpty(board[i, j]))
                {
                    // Try the move
                    board[i, j] = symbol;
                    
                    // Check if this move wins
                    if (CheckWin(i, j, board))
                    {
                        board[i, j] = ""; // Reset the cell
                        return new Point(i, j);
                    }
                    
                    // Reset the cell
                    board[i, j] = "";
                }
            }
        }
        return null;
    }

    private Point? FindRandomMove(string[,] board)
    {
        var validMoves = new List<Point>();
        for (int i = 0; i < GameSettings.BOARD_SIZE_TIC_TAC_TOE; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_TIC_TAC_TOE; j++)
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
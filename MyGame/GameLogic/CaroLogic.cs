using System.Drawing;
using System.Text.Json;
using MyGame.Interfaces;
using MyGame.Models;

namespace MyGame.GameLogic;

public class CaroLogic : IGameLogic
{
    private readonly Random random = new();
    private readonly MLCaroEngine mlEngine;
    private List<GameState> currentGameStates = new();
    private bool isLearningMode = true;
    private string? gameWinner = null; //xem ai thắng để học

    public CaroLogic()
    {
        mlEngine = new MLCaroEngine();
    }

    public void SetLearningMode(bool enabled)
    {
        isLearningMode = enabled;
    }

    public void Reset()
    {
        // lưu trạng thái trò chơi nếu có người thắng
        if (currentGameStates.Count > 0 && gameWinner != null)
        {
            mlEngine.SaveGameData(currentGameStates, gameWinner);
        }
        currentGameStates.Clear();
        gameWinner = null;
    }

    public void SetGameWinner(string winner)
    {
        gameWinner = winner;
    }

    public bool MakeMove(Point position, bool isPlayer1Turn, string[,] board)
    {
        if (!string.IsNullOrEmpty(board[position.X, position.Y]))
            return false;

        // ghi lại trạng thái trò chơi để học
        if (isLearningMode)
        {
            var state = new GameState
            {
                Board = CloneBoard(board),
                Move = position,
                IsPlayerMove = isPlayer1Turn,
                Timestamp = DateTime.Now
            };
            currentGameStates.Add(state);
        }

        board[position.X, position.Y] = isPlayer1Turn ? "X" : "O";
        return true;
    }

    public bool CheckWin(int row, int col, string[,] board)
    {
        string symbol = board[row, col];
        if (string.IsNullOrEmpty(symbol))
            return false;

        int[][] directions = new int[][]
        {
            new int[] { 1, 0 },   // ngang
            new int[] { 0, 1 },   // dọc
            new int[] { 1, 1 },   // chéo
            new int[] { 1, -1 }   // chéo ngược
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
        
        int r = row + deltaRow;
        int c = col + deltaCol;
        while (IsValidPosition(r, c) && board[r, c] == symbol)
        {
            count++;
            if (count == 5) return true;
            r += deltaRow;
            c += deltaCol;
        }

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
        // luôn luôn ưu tiên các nước đi quan trọng
        Point? criticalMove = GetCriticalMove(board);
        if (criticalMove.HasValue)
            return criticalMove;

        // thử dùng học máy nếu có dữ liệu đã học
        if (isLearningMode && mlEngine.HasLearnedData() && random.NextDouble() < 0.4)
        {
            Point? mlMove = mlEngine.PredictMove(board);
            if (mlMove.HasValue && IsValidMove(mlMove.Value, board))
            {
                return mlMove;
            }
        }

        // dùng AI nâng cao để chọn nước đi
        return GetAdvancedAIMove(board);
    }

    private Point? GetCriticalMove(string[,] board)
    {
        // 1. kiểm tra thắng trong một nước (5 trong một hàng)
        Point? winMove = FindWinningMove("O", 5, board);
        if (winMove.HasValue) return winMove;

        // 2. chặn đối phương thắng trong một nước (5 trong một hàng)
        Point? blockWin = FindWinningMove("X", 5, board);
        if (blockWin.HasValue) return blockWin;

        // 3. chặn đối phương tạo cơ hội thắng với 4 trong một hàng
        Point? block4 = FindThreatMove("X", 4, board);
        if (block4.HasValue) return block4;

        // 4.tạo cơ hội thắng với 4 trong một hàng
        Point? create4 = FindThreatMove("O", 4, board);
        if (create4.HasValue) return create4;

        return null;
    }

    private Point? GetAdvancedAIMove(string[,] board)
    {
        // Đánh giá tất cả các nước đi tiềm năng và chấm điểm chúng
        var moveScores = new Dictionary<Point, int>();

        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                if (string.IsNullOrEmpty(board[i, j]))
                {
                    int score = EvaluateMove(i, j, board);
                    if (score > 0)
                    {
                        moveScores[new Point(i, j)] = score;
                    }
                }
            }
        }

        if (moveScores.Count > 0)
        {
            // lấy 3 nước đi hàng đầu và chọn ngẫu nhiên trong số đó để tránh lặp lại
            var topMoves = moveScores.OrderByDescending(kvp => kvp.Value)
                                     .Take(3)
                                     .ToList();
            return topMoves[random.Next(topMoves.Count)].Key;
        }

        // rút về trung tâm hoặc ngẫu nhiên nếu không có gì nổi bật
        return GetStrategicMove(board);
    }

    private int EvaluateMove(int row, int col, string[,] board)
    {
        int score = 0;

        // Cho ai (O)
        score += EvaluatePosition(row, col, "O", board) * 2; // ưu tiên tấn công

        // chặn cho đối phương (X)
        score += EvaluatePosition(row, col, "X", board);

        // thêm điểm cho vị trí trung tâm
        int center = GameSettings.BOARD_SIZE_CARO / 2;
        int distFromCenter = Math.Abs(row - center) + Math.Abs(col - center);
        score += (GameSettings.BOARD_SIZE_CARO - distFromCenter) * 2;

        return score;
    }

    private int EvaluatePosition(int row, int col, string symbol, string[,] board)
    {
        int score = 0;
        int[][] directions = new int[][]
        {
            new int[] { 1, 0 }, new int[] { 0, 1 },
            new int[] { 1, 1 }, new int[] { 1, -1 }
        };

        foreach (var dir in directions)
        {
            int count = CountInDirection(row, col, dir[0], dir[1], symbol, board);
            
            // Chấm điểm dựa trên số quân liên tiếp
            if (count >= 4) score += 10000; // gần thắng
            else if (count == 3) score += 1000;
            else if (count == 2) score += 100;
            else if (count == 1) score += 10;
        }

        return score;
    }

    private int CountInDirection(int row, int col, int deltaRow, int deltaCol, string symbol, string[,] board)
    {
        int count = 1; // đếm quân hiện tại

        // đếm về phía trước
        int r = row + deltaRow;
        int c = col + deltaCol;
        while (IsValidPosition(r, c) && board[r, c] == symbol)
        {
            count++;
            r += deltaRow;
            c += deltaCol;
        }

        // đếm ngược lại
        r = row - deltaRow;
        c = col - deltaCol;
        while (IsValidPosition(r, c) && board[r, c] == symbol)
        {
            count++;
            r -= deltaRow;
            c -= deltaCol;
        }

        return count;
    }

    private Point? FindThreatMove(string symbol, int targetCount, string[,] board)
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

    private Point? GetStrategicMove(string[,] board)
    {
        int center = GameSettings.BOARD_SIZE_CARO / 2;
        
        // thử trung tâm đầu tiên
        if (string.IsNullOrEmpty(board[center, center]))
            return new Point(center, center);

        // thử các vị trí xung quanh trung tâm
        int[][] offsets = new int[][]
        {
            new int[] {0, 1}, new int[] {1, 0}, new int[] {0, -1}, new int[] {-1, 0},
            new int[] {1, 1}, new int[] {1, -1}, new int[] {-1, 1}, new int[] {-1, -1}
        };

        foreach (var offset in offsets)
        {
            int r = center + offset[0];
            int c = center + offset[1];
            if (IsValidPosition(r, c) && string.IsNullOrEmpty(board[r, c]))
                return new Point(r, c);
        }

        // tìm bất kỳ vị trí nào gần các quân đã đặt
        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                if (!string.IsNullOrEmpty(board[i, j]))
                {
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

        return FindRandomMove(board);
    }

    private bool IsValidMove(Point move, string[,] board)
    {
        return IsValidPosition(move.X, move.Y) && 
               string.IsNullOrEmpty(board[move.X, move.Y]);
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
            new int[] { 1, 0 },   // ngang
            new int[] { 0, 1 },   // dọc
            new int[] { 1, 1 },   // chéo
            new int[] { 1, -1 }   // chéo ngược
        };

        foreach (var dir in directions)
        {
            int count = 1;
            
            int r = row + dir[0];
            int c = col + dir[1];
            while (IsValidPosition(r, c) && board[r, c] == symbol)
            {
                count++;
                if (count >= targetCount) return true;
                r += dir[0];
                c += dir[1];
            }

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

    private string[,] CloneBoard(string[,] board)
    {
        string[,] clone = new string[GameSettings.BOARD_SIZE_CARO, GameSettings.BOARD_SIZE_CARO];
        for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
        {
            for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
            {
                clone[i, j] = board[i, j];
            }
        }
        return clone;
    }

    public MLCaroEngine GetMLEngine() => mlEngine;
}

// trajng thái trò chơi để học máy
public class GameState
{
    public string[,] Board { get; set; }
    public Point Move { get; set; }
    public bool IsPlayerMove { get; set; }
    public DateTime Timestamp { get; set; }
}

// cải thiện trí tuệ nhân tạo học máy cho Caro
public class MLCaroEngine
{
    private readonly string dataFilePath;
    private List<PatternData> learnedPatterns = new();
    private readonly int patternSize = 7; // tăng lêbn 7x7 để nắm bắt bối cảnh rộng hơn

    public MLCaroEngine()
    {
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MyGame"
        );
        Directory.CreateDirectory(appDataPath);
        dataFilePath = Path.Combine(appDataPath, "caro_ml_data.json");
        LoadLearnedData();
    }

    public bool HasLearnedData()
    {
        return learnedPatterns.Count > 10; // Cần ít nhất 10 mẫu để dự đoán
    }

    public void SaveGameData(List<GameState> gameStates, string winner)
    {
        try
        {
            // chỉ học khi người chơi thắng
            if (winner != "X") return;

            // phân tích bước của người chơi
            foreach (var state in gameStates.Where(s => s.IsPlayerMove))
            {
                var pattern = ExtractPattern(state.Board, state.Move);
                if (pattern != null && HasNearbyPieces(state.Board, state.Move))
                {
                    var existingPattern = learnedPatterns
                        .FirstOrDefault(p => PatternsMatch(p.Pattern, pattern));
                    
                    if (existingPattern != null)
                    {
                        existingPattern.Frequency++;
                        existingPattern.SuccessRate += 0.1; // tăng dần tỷ lệ thành công
                        existingPattern.LastUsed = DateTime.Now;
                    }
                    else
                    {
                        learnedPatterns.Add(new PatternData
                        {
                            Pattern = pattern,
                            Move = state.Move,
                            Frequency = 1,
                            SuccessRate = 1.0,
                            LastUsed = DateTime.Now
                        });
                    }
                }
            }

            // tiếp tục duy trì kích thước dữ liệu học tập hợp hợp lý
            if (learnedPatterns.Count > 2000)
            {
                learnedPatterns = learnedPatterns
                    .OrderByDescending(p => p.SuccessRate * p.Frequency)
                    .Take(2000)
                    .ToList();
            }

            SaveLearnedData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi không học được: {ex.Message}");
        }
    }

    private bool HasNearbyPieces(string[,] board, Point move)
    {
        // tìm kiếm các quân cờ lân cận trong phạm vi 2 ô
        for (int di = -2; di <= 2; di++)
        {
            for (int dj = -2; dj <= 2; dj++)
            {
                if (di == 0 && dj == 0) continue;
                
                int r = move.X + di;
                int c = move.Y + dj;
                
                if (r >= 0 && r < GameSettings.BOARD_SIZE_CARO &&
                    c >= 0 && c < GameSettings.BOARD_SIZE_CARO &&
                    !string.IsNullOrEmpty(board[r, c]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public Point? PredictMove(string[,] board)
    {
        try
        {
            var candidates = new Dictionary<Point, double>();

            // tìm kiếm tất cả các vị trí trống và đánh giá dựa trên mẫu đã học
            for (int i = 0; i < GameSettings.BOARD_SIZE_CARO; i++)
            {
                for (int j = 0; j < GameSettings.BOARD_SIZE_CARO; j++)
                {
                    if (string.IsNullOrEmpty(board[i, j]) && 
                        HasNearbyPieces(board, new Point(i, j)))
                    {
                        var currentPattern = ExtractPattern(board, new Point(i, j));
                        if (currentPattern == null) continue;

                        double score = 0;
                        int matchCount = 0;

                        foreach (var learned in learnedPatterns)
                        {
                            double similarity = CalculatePatternSimilarity(
                                currentPattern, 
                                learned.Pattern
                            );
                            
                            if (similarity > 0.7) // điểm ngưỡng tương đồng
                            {
                                score += similarity * learned.Frequency * learned.SuccessRate;
                                matchCount++;
                            }
                        }

                        if (matchCount > 0)
                        {
                            candidates[new Point(i, j)] = score / matchCount; // điểm trung bình
                        }
                    }
                }
            }

            if (candidates.Count > 0)
            {
                return candidates.OrderByDescending(kvp => kvp.Value).First().Key;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi không đoán được hướng đánh: {ex.Message}");
        }

        return null;
    }

    private string[,] ExtractPattern(string[,] board, Point center)
    {
        int halfSize = patternSize / 2;
        string[,] pattern = new string[patternSize, patternSize];

        for (int i = 0; i < patternSize; i++)
        {
            for (int j = 0; j < patternSize; j++)
            {
                int boardRow = center.X - halfSize + i;
                int boardCol = center.Y - halfSize + j;

                if (boardRow >= 0 && boardRow < GameSettings.BOARD_SIZE_CARO &&
                    boardCol >= 0 && boardCol < GameSettings.BOARD_SIZE_CARO)
                {
                    pattern[i, j] = board[boardRow, boardCol] ?? "";
                }
                else
                {
                    pattern[i, j] = "EDGE";
                }
            }
        }

        return pattern;
    }

    private bool PatternsMatch(string[,] p1, string[,] p2)
    {
        if (p1.GetLength(0) != p2.GetLength(0) || p1.GetLength(1) != p2.GetLength(1))
            return false;

        for (int i = 0; i < p1.GetLength(0); i++)
        {
            for (int j = 0; j < p1.GetLength(1); j++)
            {
                if (p1[i, j] != p2[i, j])
                    return false;
            }
        }
        return true;
    }

    private double CalculatePatternSimilarity(string[,] p1, string[,] p2)
    {
        int matches = 0;
        int total = p1.GetLength(0) * p1.GetLength(1);
        int centerWeight = 3; // trọng số cao hơn cho ô trung tâm

        int center = p1.GetLength(0) / 2;

        for (int i = 0; i < p1.GetLength(0); i++)
        {
            for (int j = 0; j < p1.GetLength(1); j++)
            {
                int weight = 1;
                
                // đánh trọng số cao hơn cho các ô gần trung tâm
                int distFromCenter = Math.Abs(i - center) + Math.Abs(j - center);
                if (distFromCenter <= 1)
                    weight = centerWeight;

                if (p1[i, j] == p2[i, j])
                    matches += weight;
                
                total += (weight - 1); // điều chỉnh tổng trọng số
            }
        }

        return (double)matches / total;
    }

    private void SaveLearnedData()
    {
        try
        {
            var serializable = learnedPatterns.Select(p => new
            {
                Pattern = ConvertPatternToArray(p.Pattern),
                Move = new { p.Move.X, p.Move.Y },
                p.Frequency,
                p.SuccessRate,
                p.LastUsed
            }).ToList();

            string json = JsonSerializer.Serialize(serializable, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(dataFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Lỗi không lưu trữ được bước đi đã học: {ex.Message}");
        }
    }

    private void LoadLearnedData()
    {
        try
        {
            if (File.Exists(dataFilePath))
            {
                string json = File.ReadAllText(dataFilePath);
                var data = JsonSerializer.Deserialize<List<JsonElement>>(json);
                
                if (data != null)
                {
                    learnedPatterns = data.Select(item => new PatternData
                    {
                        Pattern = ConvertArrayToPattern(
                            JsonSerializer.Deserialize<string[][]>(
                                item.GetProperty("Pattern").GetRawText()
                            )
                        ),
                        Move = new Point(
                            item.GetProperty("Move").GetProperty("X").GetInt32(),
                            item.GetProperty("Move").GetProperty("Y").GetInt32()
                        ),
                        Frequency = item.GetProperty("Frequency").GetInt32(),
                        SuccessRate = item.TryGetProperty("SuccessRate", out var sr) 
                            ? sr.GetDouble() 
                            : 1.0,
                        LastUsed = item.GetProperty("LastUsed").GetDateTime()
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Không thể tải được dữ liệu đã học: {ex.Message}");
            learnedPatterns = new List<PatternData>();
        }
    }

    private string[][] ConvertPatternToArray(string[,] pattern)
    {
        var result = new string[pattern.GetLength(0)][];
        for (int i = 0; i < pattern.GetLength(0); i++)
        {
            result[i] = new string[pattern.GetLength(1)];
            for (int j = 0; j < pattern.GetLength(1); j++)
            {
                result[i][j] = pattern[i, j];
            }
        }
        return result;
    }

    private string[,] ConvertArrayToPattern(string[][] array)
    {
        var result = new string[array.Length, array[0].Length];
        for (int i = 0; i < array.Length; i++)
        {
            for (int j = 0; j < array[i].Length; j++)
            {
                result[i, j] = array[i][j];
            }
        }
        return result;
    }

    public int GetPatternCount() => learnedPatterns.Count;
    
    public List<string> GetStats()
    {
        return new List<string>
        {
            $"Total Patterns: {learnedPatterns.Count}",
            $"Avg Frequency: {(learnedPatterns.Count > 0 ? learnedPatterns.Average(p => p.Frequency) : 0):F1}",
            $"Avg Success Rate: {(learnedPatterns.Count > 0 ? learnedPatterns.Average(p => p.SuccessRate) : 0):F2}"
        };
    }
}

public class PatternData
{
    public string[,] Pattern { get; set; }
    public Point Move { get; set; }
    public int Frequency { get; set; }
    public double SuccessRate { get; set; }
    public DateTime LastUsed { get; set; }
}
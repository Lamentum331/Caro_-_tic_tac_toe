using System.Drawing;
using MyGame.GameLogic;
using MyGame.Interfaces;
using MyGame.Models;

namespace MyGame.Forms;

public partial class GameForm : Form
{
    private Button[,] boardButtons;
    private int currentBoardSize;
    private bool isCaroMode = false;
    private bool isPlayerVsAI = false;
    private bool isPlayer1Turn = true;
    private readonly Random random = new();
    
    private readonly IGameLogic ticTacToeLogic = new TicTacToeLogic();
    private readonly IGameLogic caroLogic = new CaroLogic();

    public GameForm()
    {
        InitializeComponent();
        InitializeMainMenu();
    }

    private void InitializeMainMenu()
    {
        this.Controls.Clear();
        this.Size = new Size(800, 1000);
        this.StartPosition = FormStartPosition.CenterScreen;

        var btnTicTacToe = CreateMenuButton("Tic Tac Toe\n(3 quân cờ)", new Point(250, 100), Color.LightSkyBlue);
        btnTicTacToe.Click += (s, e) => ShowGameModeSelection(false);

        var btnCaro = CreateMenuButton("Caro", new Point(250, 200), Color.LightGreen);
        btnCaro.Click += (s, e) => ShowGameModeSelection(true);

        this.Controls.AddRange(new Control[] { btnTicTacToe, btnCaro });
    }

    private Button CreateMenuButton(string text, Point location, Color backColor)
    {
        return new Button
        {
            Text = text,
            Size = new Size(300, 80),
            Location = location,
            Font = new Font("Arial", 16, FontStyle.Bold),
            BackColor = backColor,
            FlatStyle = FlatStyle.Flat
        };
    }

    private void ShowGameModeSelection(bool isCaro)
    {
        this.Controls.Clear();
        isCaroMode = isCaro;
        currentBoardSize = isCaro ? GameSettings.BOARD_SIZE_CARO : GameSettings.BOARD_SIZE_TIC_TAC_TOE;

        var lblTitle = CreateLabel(
            isCaro ? "Caro Game Mode" : "Tic Tac Toe Game Mode\n(Mỗi bên chỉ có 3 quân)\n",
            new Point(100, 30),
            new Size(600, 90)
        );

        var btnPvP = CreateMenuButton("Player vs Player", new Point(250, 120), Color.LightSalmon);
        btnPvP.Click += (s, e) => StartGame(false);

        var btnPvAI = CreateMenuButton("Player vs AI", new Point(250, 220), Color.LightPink);
        btnPvAI.Click += (s, e) => StartGame(true);

        var btnBack = CreateMenuButton("Back to Main Menu", new Point(250, 320), Color.LightGray);
        btnBack.Click += (s, e) => InitializeMainMenu();

        this.Controls.AddRange(new Control[] { lblTitle, btnPvP, btnPvAI, btnBack });
    }

    private Label CreateLabel(string text, Point location, Size size)
    {
        return new Label
        {
            Text = text,
            Location = location,
            Size = size,
            Font = new Font("Arial", 20, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
    }

    private void StartGame(bool vsAI)
    {
        this.Controls.Clear();
        isPlayerVsAI = vsAI;

        // Reset game logic
        if (isCaroMode)
            caroLogic.Reset();
        else
            ticTacToeLogic.Reset();

        currentBoardSize = isCaroMode ? GameSettings.BOARD_SIZE_CARO : GameSettings.BOARD_SIZE_TIC_TAC_TOE;
        InitializeGameBoard();
    }

    private void InitializeGameBoard()
    {
        boardButtons = new Button[currentBoardSize, currentBoardSize];

        int boardPixelSize = currentBoardSize * GameSettings.CELL_SIZE;
        int padding = 50;
        this.Size = new Size(
            Math.Max(800, boardPixelSize + padding * 2),
            Math.Max(1000, boardPixelSize + padding * 2 + 100)
        );

        var boardPanel = CreateBoardPanel(boardPixelSize, padding);
        CreateGameButtons(boardPanel, padding);
        CreateControlButtons(boardPanel);

        // Determine first player
        isPlayer1Turn = random.Next(2) == 0;
        var lblTurn = CreateTurnLabel(boardPanel);
        
        this.Controls.Add(boardPanel);

        // Show who goes first
        string firstPlayer = isPlayer1Turn ? "Player 1 (X)" : (isPlayerVsAI ? "AI (O)" : "Player 2 (O)");
        string gameInfo = isCaroMode ? "" : "\n(Mỗi bên chỉ có 3 quân)";
        MessageBox.Show($"{firstPlayer} sẽ đi trước!{gameInfo}", "Kết quả tung đồng xu");

        // Update turn label
        lblTurn.Text = $"Current Turn: {firstPlayer}";

        // If AI goes first, make its move
        if (isPlayerVsAI && !isPlayer1Turn)
        {
            Application.DoEvents();
            System.Threading.Tasks.Task.Delay(300).ContinueWith(t => 
            {
                this.Invoke(new Action(() => MakeAIMove()));
            });
        }
    }

    private Panel CreateBoardPanel(int boardPixelSize, int padding)
    {
        return new Panel
        {
            Size = new Size(boardPixelSize + padding * 2, boardPixelSize + padding * 2),
            Location = new Point(
                (this.ClientSize.Width - (boardPixelSize + padding * 2)) / 2,
                (this.ClientSize.Height - (boardPixelSize + padding * 2 + 100)) / 2
            )
        };
    }

    private void CreateGameButtons(Panel boardPanel, int padding)
    {
        for (int i = 0; i < currentBoardSize; i++)
        {
            for (int j = 0; j < currentBoardSize; j++)
            {
                var btn = new Button
                {
                    Size = new Size(GameSettings.CELL_SIZE, GameSettings.CELL_SIZE),
                    Location = new Point(padding + j * GameSettings.CELL_SIZE, 
                                       padding + i * GameSettings.CELL_SIZE),
                    Tag = new Point(i, j),
                    Font = new Font("Arial", 24, FontStyle.Bold),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.White
                };
                btn.Click += BoardButton_Click;
                boardButtons[i, j] = btn;
                boardPanel.Controls.Add(btn);
            }
        }
    }

    private void CreateControlButtons(Panel boardPanel)
    {
        var btnRestart = CreateMenuButton("Restart Game", 
            new Point(boardPanel.Left + (boardPanel.Width - 420) / 2, boardPanel.Bottom + 20),
            Color.LightBlue);
        btnRestart.Size = new Size(200, 50);
        btnRestart.Font = new Font("Arial", 12, FontStyle.Bold);
        btnRestart.Click += (s, e) => StartGame(isPlayerVsAI);

        var btnMenu = CreateMenuButton("Back to Menu",
            new Point(btnRestart.Right + 20, btnRestart.Top),
            Color.LightGray);
        btnMenu.Size = new Size(200, 50);
        btnMenu.Font = new Font("Arial", 12, FontStyle.Bold);
        btnMenu.Click += (s, e) => InitializeMainMenu();

        this.Controls.AddRange(new Control[] { btnRestart, btnMenu });
    }

    private Label CreateTurnLabel(Panel boardPanel)
    {
        var lblTurn = new Label
        {
            Size = new Size(400, 30),
            Location = new Point(
                boardPanel.Left + (boardPanel.Width - 400) / 2,
                boardPanel.Top - 40
            ),
            Font = new Font("Arial", 14, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        
        this.Controls.Add(lblTurn);
        return lblTurn;
    }

    private void BoardButton_Click(object? sender, EventArgs e)
    {
        if (sender == null) return;
        Button btn = (Button)sender;

        // Kiểm tra xem có phải lượt của người chơi không
        if (isPlayerVsAI && !isPlayer1Turn)
        {
            return; // Không cho phép đánh khi đến lượt AI
        }
        
        // Với Caro: kiểm tra nếu ô đã được đánh
        // Với Tic Tac Toe: cho phép đánh vào bất kỳ ô nào (logic sẽ xóa quân cũ)
        if (isCaroMode && !string.IsNullOrEmpty(btn.Text))
        {
            return;
        }

        var pos = (Point?)btn.Tag ?? new Point(0, 0);
        var board = GetBoardState();
        IGameLogic currentLogic = isCaroMode ? caroLogic : ticTacToeLogic;
        
        // SỬA LẠI: Gọi MakeMove từ logic (logic sẽ tự động xóa quân cũ nếu cần)
        if (!currentLogic.MakeMove(pos, isPlayer1Turn, board))
        {
            // Move không hợp lệ (xảy ra khi đánh vào ô đã có quân trong Caro)
            if (isCaroMode) return;
        }

        // Cập nhật toàn bộ UI từ board state (quan trọng!)
        UpdateBoardUI(board);

        // Kiểm tra thắng
        if (currentLogic.CheckWin(pos.X, pos.Y, board))
        {
            MessageBox.Show($"{(isPlayer1Turn ? "Player 1 (X)" : "Player 2 (O)")} wins!");
            StartGame(isPlayerVsAI);
            return;
        }

        // Kiểm tra hòa (chỉ với Caro, Tic Tac Toe 3 quân không có hòa)
        if (isCaroMode && IsBoardFull())
        {
            MessageBox.Show("Game is a draw!");
            StartGame(isPlayerVsAI);
            return;
        }

        isPlayer1Turn = !isPlayer1Turn;
        UpdateTurnLabel();

        if (isPlayerVsAI && !isPlayer1Turn)
        {
            SetBoardEnabled(false);
            System.Threading.Tasks.Task.Delay(300).ContinueWith(t => 
            {
                this.Invoke(new Action(() => 
                {
                    MakeAIMove();
                    SetBoardEnabled(true);
                }));
            });
        }
    }

    private string[,] GetBoardState()
    {
        var board = new string[currentBoardSize, currentBoardSize];
        for (int i = 0; i < currentBoardSize; i++)
            for (int j = 0; j < currentBoardSize; j++)
                board[i, j] = boardButtons[i, j].Text;
        return board;
    }

    private void UpdateBoardUI(string[,] board)
    {
        // Cập nhật toàn bộ UI từ board state
        for (int i = 0; i < currentBoardSize; i++)
            for (int j = 0; j < currentBoardSize; j++)
                boardButtons[i, j].Text = board[i, j];
    }

    private void UpdateTurnLabel()
    {
        var turnLabel = this.Controls.OfType<Label>().FirstOrDefault();
        if (turnLabel != null)
        {
            turnLabel.Text = $"Current Turn: {(isPlayer1Turn ? "Player 1 (X)" : (isPlayerVsAI ? "AI (O)" : "Player 2 (O)"))}";
        }
    }

    private void SetBoardEnabled(bool enabled)
    {
        foreach (Button btn in boardButtons)
            btn.Enabled = enabled;
    }

    private bool IsBoardFull()
    {
        for (int i = 0; i < currentBoardSize; i++)
        {
            for (int j = 0; j < currentBoardSize; j++)
            {
                if (string.IsNullOrEmpty(boardButtons[i, j].Text))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void MakeAIMove()
    {
        try
        {
            var board = GetBoardState();
            IGameLogic currentLogic = isCaroMode ? caroLogic : ticTacToeLogic;
            Point? movePosition = currentLogic.GetAIMove(board);

            if (movePosition.HasValue)
            {
                // SỬA LẠI: Gọi MakeMove từ logic (logic sẽ tự động xóa quân cũ nếu cần)
                currentLogic.MakeMove(movePosition.Value, false, board);

                // Cập nhật toàn bộ UI từ board state (quan trọng!)
                UpdateBoardUI(board);

                // Kiểm tra thắng
                if (currentLogic.CheckWin(movePosition.Value.X, movePosition.Value.Y, board))
                {
                    MessageBox.Show("AI wins!");
                    StartGame(isPlayerVsAI);
                    return;
                }

                // Kiểm tra hòa (chỉ với Caro)
                if (isCaroMode && IsBoardFull())
                {
                    MessageBox.Show("Game is a draw!");
                    StartGame(isPlayerVsAI);
                    return;
                }

                isPlayer1Turn = true;
                UpdateTurnLabel();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"AI Error: {ex.Message}");
        }
    }
}
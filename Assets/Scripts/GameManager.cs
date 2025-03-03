using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // 经典和限制模式用的 3x3 按钮
    public Button[] gridButtons;
    // Super 模式用的 9x9 按钮（需在场景中配置 81 个按钮，按子棋盘连续排列）
    public Button[] superGridButtons;

    public TextMeshProUGUI statusText;   // 经典/限制模式当前回合提示
    public TextMeshProUGUI status3Text;    // Super 模式当前回合提示
    public TextMeshProUGUI settleText;     // 结算状态提示
    public Button playerFirstButton, aiFirstButton; // 选择先手按钮
    public GameObject Menu;
    public TMP_Dropdown modeDropdown;      // 模式下拉框
    public GameObject[] modeDescs;
    public TMP_Dropdown difficultyDropdown; // 难度下拉框
    public GameObject[] difficultyDescs;
    public GameObject Mode12Panels;        // 经典/限制模式面板
    public GameObject Mode3Panels;         // Super 模式面板

    public enum Mode { Classic, Limit, Super }
    public Mode mode = Mode.Classic;

    // 难度枚举（Classic 模式下采用 minimax 随机概率控制）
    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty aiDifficulty = Difficulty.Hard;

    // 经典/限制模式 3x3 棋盘状态：0空，1玩家，2AI
    private int[] board;
    // Super 模式 9x9 棋盘状态（81 个格子）
    private int[] superBoard;
    // 记录 9 个子棋盘状态：0未决，1玩家赢，2AI赢，3平局
    private int[] subBoardStatus;

    private int currentPlayer; // 1=玩家, 2=AI
    private bool gameOver;
    private bool playerFirst = true; // 玩家是否先手

    // 用于 Limit 模式：记录各自落子顺序
    private List<int> playerMoves;
    private List<int> aiMoves;

    // 用于 Super 模式：记录允许落子区域（-1 表示不限）
    private int allowedSubBoard = -1;

    void Start()
    {
        // 绑定先手选择按钮
        playerFirstButton.onClick.AddListener(() => StartGame(true));
        aiFirstButton.onClick.AddListener(() => StartGame(false));

        // 绑定 Dropdown 事件
        if (modeDropdown != null)
        {
            modeDropdown.onValueChanged.AddListener(OnModeChanged);
            mode = (Mode)modeDropdown.value;
            for (int i = 0; i < modeDescs.Length; i++)
            {
                modeDescs[i].SetActive(modeDropdown.value == i);
            }
        }
        if (difficultyDropdown != null)
        {
            difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            aiDifficulty = (Difficulty)difficultyDropdown.value;
            for (int i = 0; i < difficultyDescs.Length; i++)
            {
                difficultyDescs[i].SetActive(difficultyDropdown.value == i);
            }
        }

        // 显示选择界面
        Menu.SetActive(true);
    }

    // Dropdown 数值变化事件处理
    public void OnModeChanged(int index)
    {
        mode = (Mode)index;
        for (int i = 0; i < modeDescs.Length; i++)
        {
            modeDescs[i].SetActive(index == i);
        }
    }

    public void OnDifficultyChanged(int index)
    {
        aiDifficulty = (Difficulty)index;
        for (int i = 0; i < difficultyDescs.Length; i++)
        {
            difficultyDescs[i].SetActive(index == i);
        }
    }

    void StartGame(bool isPlayerFirst)
    {
        playerFirst = isPlayerFirst;
        Menu.SetActive(false);
        if (mode == Mode.Super)
        {
            InitializeSuperGame();
        }
        else
        {
            InitializeGame();
        }
    }

    #region 经典/限制模式相关

    // 经典/限制模式的初始化
    void InitializeGame()
    {
        board = new int[9];
        gameOver = false;
        currentPlayer = playerFirst ? 1 : 2;
        statusText.text = playerFirst ? "玩家回合" : "AI回合";

        Mode3Panels.SetActive(false);
        Mode12Panels.SetActive(true);

        if (mode == Mode.Limit)
        {
            playerMoves = new List<int>();
            aiMoves = new List<int>();
        }

        for (int i = 0; i < gridButtons.Length; i++)
        {
            int index = i;
            gridButtons[i].onClick.RemoveAllListeners();
            gridButtons[i].onClick.AddListener(() => OnGridButtonClick(index));
            gridButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "";
            gridButtons[i].interactable = true;
        }

        if (!playerFirst)
        {
            MakeAIMove();
        }
    }

    // 玩家点击 3x3 棋盘按钮（经典/限制模式）
    void OnGridButtonClick(int index)
    {
        if (gameOver || board[index] != 0)
            return;

        if (mode == Mode.Limit)
        {
            // 若已有 3 个落子，则移除最早的一个
            if (playerMoves.Count == 3)
            {
                int removeIndex = playerMoves[0];
                board[removeIndex] = 0;
                gridButtons[removeIndex].GetComponentInChildren<TextMeshProUGUI>().text = "";
                gridButtons[removeIndex].interactable = true;
                playerMoves.RemoveAt(0);
            }
            board[index] = 1;
            gridButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "X";
            gridButtons[index].interactable = false;
            playerMoves.Add(index);
        }
        else // 经典模式
        {
            board[index] = 1;
            gridButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "X";
            gridButtons[index].interactable = false;
        }

        if (CheckGameOverClassic())
            return;

        MakeAIMove();
    }

    // AI 落子（经典/限制模式）
    void MakeAIMove()
    {
        int bestMove = GetBestMove();
        if (bestMove == -1)
            return;

        if (mode == Mode.Limit)
        {
            if (aiMoves.Count == 3)
            {
                int removeIndex = aiMoves[0];
                board[removeIndex] = 0;
                gridButtons[removeIndex].GetComponentInChildren<TextMeshProUGUI>().text = "";
                gridButtons[removeIndex].interactable = true;
                aiMoves.RemoveAt(0);
            }
            board[bestMove] = 2;
            gridButtons[bestMove].GetComponentInChildren<TextMeshProUGUI>().text = "O";
            gridButtons[bestMove].interactable = false;
            aiMoves.Add(bestMove);
        }
        else
        {
            board[bestMove] = 2;
            gridButtons[bestMove].GetComponentInChildren<TextMeshProUGUI>().text = "O";
            gridButtons[bestMove].interactable = false;
        }

        CheckGameOverClassic();
    }

    // 经典/限制模式结束判定
    bool CheckGameOverClassic()
    {
        if (CheckWin(board))
        {
            settleText.text = currentPlayer == 1 ? "赢了！" : "输了！";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        else if (CheckDraw(board))
        {
            settleText.text = "平局！";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        statusText.text = currentPlayer == 1 ? "玩家回合" : "AI回合";
        return false;
    }

    #endregion

    #region Super 模式相关

    // Super 模式初始化：设置 9x9 棋盘及子棋盘状态
    void InitializeSuperGame()
    {
        superBoard = new int[81];
        subBoardStatus = new int[9];
        gameOver = false;
        currentPlayer = playerFirst ? 1 : 2;
        status3Text.text = playerFirst ? "玩家回合" : "AI回合";

        Mode3Panels.SetActive(true);
        Mode12Panels.SetActive(false);

        // 重置允许落子区域（第一步不限）
        allowedSubBoard = -1;

        // 初始化 9x9 按钮（假定按钮顺序已按子棋盘连续排列）
        for (int i = 0; i < superGridButtons.Length; i++)
        {
            int index = i;
            superGridButtons[i].onClick.RemoveAllListeners();
            superGridButtons[i].onClick.AddListener(() => OnSuperGridButtonClick(index));
            superGridButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "";
            // 初始时先全部置为 true，后续统一更新
            superGridButtons[i].interactable = true;
        }

        // 初始化后更新按钮的交互状态
        UpdateSuperBoardInteractableButtons();

        if (!playerFirst)
        {
            MakeAISuperMove();
        }
    }

    // 更新 Super 模式下各按钮的可交互状态
    void UpdateSuperBoardInteractableButtons()
    {
        for (int i = 0; i < superGridButtons.Length; i++)
        {
            int subBoard = GetSubBoardIndex(i);
            // 当前格子空且对应子棋盘未决出结果
            if (superBoard[i] == 0 && subBoardStatus[subBoard] == 0)
            {
                // 如果没有限制或者当前按钮所在子棋盘是允许落子区域，则按钮可交互
                if (allowedSubBoard == -1 || allowedSubBoard == subBoard)
                    superGridButtons[i].interactable = true;
                else
                    superGridButtons[i].interactable = false;
            }
            else
            {
                superGridButtons[i].interactable = false;
            }
        }
    }

    // 玩家在 Super 模式下点击 9x9 棋盘按钮
    void OnSuperGridButtonClick(int index)
    {
        if (gameOver || superBoard[index] != 0)
            return;

        int clickedSubBoard = GetSubBoardIndex(index);
        // 如果有限制且落子不在允许区域，则直接返回（此时该按钮应为不可交互）
        if (allowedSubBoard != -1 && clickedSubBoard != allowedSubBoard)
            return;

        // 玩家落子
        superBoard[index] = 1;
        superGridButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "X";
        superGridButtons[index].interactable = false;

        // 检查当前子棋盘是否产生胜局或平局
        if (subBoardStatus[clickedSubBoard] == 0)
        {
            if (CheckSubBoardWinner(clickedSubBoard, 1))
            {
                subBoardStatus[clickedSubBoard] = 1;
                // 可更新 UI 标记，如改变子棋盘背景颜色等
            }
            else if (CheckSubBoardDraw(clickedSubBoard))
            {
                subBoardStatus[clickedSubBoard] = 3;
                // 更新 UI 标记为平局
            }
        }

        // 根据玩家落子更新允许落子区域：取当前子棋盘内的位置（0~8）作为下一个子棋盘编号
        int nextAllowed = GetNextAllowedSubBoard(index);
        if (subBoardStatus[nextAllowed] != 0 || CheckSubBoardDraw(nextAllowed))
            allowedSubBoard = -1;
        else
            allowedSubBoard = nextAllowed;

        // 更新所有按钮的可交互状态
        UpdateSuperBoardInteractableButtons();

        if (CheckSuperGameOver())
            return;

        currentPlayer = 2;
        status3Text.text = "AI回合";
        MakeAISuperMove();
    }

    // AI 在 Super 模式下的落子
    void MakeAISuperMove()
    {
        int bestMove = GetBestSuperMove();
        if (bestMove == -1)
            return;

        superBoard[bestMove] = 2;
        superGridButtons[bestMove].GetComponentInChildren<TextMeshProUGUI>().text = "O";
        superGridButtons[bestMove].interactable = false;

        int moveSubBoard = GetSubBoardIndex(bestMove);
        if (subBoardStatus[moveSubBoard] == 0)
        {
            if (CheckSubBoardWinner(moveSubBoard, 2))
            {
                subBoardStatus[moveSubBoard] = 2;
            }
            else if (CheckSubBoardDraw(moveSubBoard))
            {
                subBoardStatus[moveSubBoard] = 3;
            }
        }

        // 根据 AI 落子更新允许落子区域
        int nextAllowed = GetNextAllowedSubBoard(bestMove);
        if (subBoardStatus[nextAllowed] != 0 || CheckSubBoardDraw(nextAllowed))
            allowedSubBoard = -1;
        else
            allowedSubBoard = nextAllowed;

        // 更新所有按钮的可交互状态
        UpdateSuperBoardInteractableButtons();

        if (CheckSuperGameOver())
            return;

        currentPlayer = 1;
        status3Text.text = "玩家回合";
    }

    // Super 模式结束判定：以 subBoardStatus 作为 3x3 棋盘判断整体胜负
    bool CheckSuperGameOver()
    {
        if (CheckWin(subBoardStatus))
        {
            settleText.text = currentPlayer == 1 ? "赢了！" : "输了！";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        // 若所有子棋盘均已决出结果（包括平局），则为平局
        bool allDecided = true;
        for (int i = 0; i < subBoardStatus.Length; i++)
        {
            if (subBoardStatus[i] == 0)
            {
                allDecided = false;
                break;
            }
        }
        if (allDecided)
        {
            settleText.text = "平局！";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        return false;
    }

    #endregion

    #region 通用函数

    // 经典 3x3 胜利判断（适用于 board 或 subBoardStatus 数组）
    bool CheckWin(int[] boardState)
    {
        int[,] winConditions = new int[,]
        {
            {0,1,2}, {3,4,5}, {6,7,8},
            {0,3,6}, {1,4,7}, {2,5,8},
            {0,4,8}, {2,4,6}
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            int a = winConditions[i, 0];
            int b = winConditions[i, 1];
            int c = winConditions[i, 2];
            if (boardState[a] != 0 && boardState[a] == boardState[b] && boardState[a] == boardState[c])
                return true;
        }
        return false;
    }

    bool CheckDraw(int[] boardState)
    {
        foreach (int cell in boardState)
        {
            if (cell == 0)
                return false;
        }
        return true;
    }

    // 根据新的引用方式：索引连续，每 9 个为一个子棋盘
    int GetSubBoardIndex(int index)
    {
        return index / 9;
    }

    // 根据新方式：取当前格在其子棋盘内的局部索引（0~8），即决定下一个允许落子子棋盘
    int GetNextAllowedSubBoard(int index)
    {
        return index % 9;
    }

    // 检查某个子棋盘（Super 模式）是否被指定玩家赢取
    bool CheckSubBoardWinner(int subBoardIndex, int player)
    {
        int startIndex = subBoardIndex * 9;
        int[] subBoard = new int[9];
        for (int i = 0; i < 9; i++)
        {
            subBoard[i] = superBoard[startIndex + i];
        }
        return CheckWinner(subBoard, player);
    }

    // 判断指定子棋盘是否已填满（平局情况）
    bool CheckSubBoardDraw(int subBoardIndex)
    {
        int startIndex = subBoardIndex * 9;
        for (int i = 0; i < 9; i++)
        {
            if (superBoard[startIndex + i] == 0)
                return false;
        }
        return true;
    }

    // 经典模式下的 minimax 算法（用于难度设定）
    int Minimax(int[] boardState, bool isMaximizing)
    {
        if (CheckWinner(boardState, 2)) return 1;
        if (CheckWinner(boardState, 1)) return -1;
        if (CheckDraw(boardState)) return 0;

        if (isMaximizing)
        {
            int bestScore = int.MinValue;
            for (int i = 0; i < 9; i++)
            {
                if (boardState[i] == 0)
                {
                    boardState[i] = 2;
                    int score = Minimax(boardState, false);
                    boardState[i] = 0;
                    bestScore = Mathf.Max(score, bestScore);
                }
            }
            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            for (int i = 0; i < 9; i++)
            {
                if (boardState[i] == 0)
                {
                    boardState[i] = 1;
                    int score = Minimax(boardState, true);
                    boardState[i] = 0;
                    bestScore = Mathf.Min(score, bestScore);
                }
            }
            return bestScore;
        }
    }

    // 修改后的 CheckWinner：检查 boardState 中是否存在玩家的胜局
    bool CheckWinner(int[] boardState, int player)
    {
        int[,] winConditions = new int[,]
        {
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8},
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8},
            {0, 4, 8}, {2, 4, 6}
        };

        for (int i = 0; i < winConditions.GetLength(0); i++)
        {
            int a = winConditions[i, 0];
            int b = winConditions[i, 1];
            int c = winConditions[i, 2];

            if (boardState[a] == player && boardState[b] == player && boardState[c] == player)
            {
                return true;
            }
        }
        return false;
    }

    int GetRandomMove()
    {
        List<int> availableMoves = new List<int>();
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
                availableMoves.Add(i);
        }
        if (availableMoves.Count > 0)
        {
            int randomIndex = Random.Range(0, availableMoves.Count);
            return availableMoves[randomIndex];
        }
        return -1;
    }

    // 获取最佳走法（经典/限制模式）
    int GetBestMove()
    {
        if (mode == Mode.Classic)
        {
            float randomChance = Random.value;
            if (aiDifficulty == Difficulty.Easy && randomChance < 0.7f)
            {
                return GetRandomMove();
            }
            else if (aiDifficulty == Difficulty.Medium && randomChance < 0.3f)
            {
                return GetRandomMove();
            }

            int bestScore = int.MinValue;
            int move = -1;

            for (int i = 0; i < 9; i++)
            {
                if (board[i] == 0)
                {
                    board[i] = 2;
                    int score = Minimax(board, false);
                    board[i] = 0;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        move = i;
                    }
                }
            }
            return move;
        }
        else if (mode == Mode.Limit)
        {
            float randomChance = Random.value;
            if (aiDifficulty == Difficulty.Easy && randomChance < 0.7f)
            {
                return GetRandomMove();
            }
            else if (aiDifficulty == Difficulty.Medium && randomChance < 0.3f)
            {
                return GetRandomMove();
            }
            return GetBestMoveLimit();
        }
        else // Super 模式不调用此方法
        {
            return GetRandomMove();
        }
    }

    // 限制模式下的最佳走法
    int GetBestMoveLimit()
    {
        List<int> availableMoves = new List<int>();
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] == 0)
                availableMoves.Add(i);
        }

        bool aiLimited = (aiMoves != null && aiMoves.Count == 3);
        foreach (int move in availableMoves)
        {
            int[] simulatedBoard = (int[])board.Clone();
            if (aiLimited)
            {
                int removeIndex = aiMoves[0];
                simulatedBoard[removeIndex] = 0;
            }
            simulatedBoard[move] = 2;
            if (CheckWinner(simulatedBoard, 2))
            {
                return move;
            }
        }

        bool playerLimited = (playerMoves != null && playerMoves.Count == 3);
        foreach (int move in availableMoves)
        {
            int[] simulatedBoard = (int[])board.Clone();
            if (playerLimited)
            {
                int removeIndex = playerMoves[0];
                simulatedBoard[removeIndex] = 0;
            }
            simulatedBoard[move] = 1;
            if (CheckWinner(simulatedBoard, 1))
            {
                return move;
            }
        }

        if (board[4] == 0)
            return 4;

        int[] corners = new int[] { 0, 2, 6, 8 };
        foreach (int corner in corners)
        {
            if (board[corner] == 0)
                return corner;
        }

        if (availableMoves.Count > 0)
            return availableMoves[Random.Range(0, availableMoves.Count)];

        return -1;
    }

    // AI 在 Super 模式下的策略：仅考虑允许区域内的空格
    int GetBestSuperMove()
    {
        List<int> availableMoves = new List<int>();
        for (int i = 0; i < superBoard.Length; i++)
        {
            int subBoardIndex = GetSubBoardIndex(i);
            if (allowedSubBoard != -1 && subBoardIndex != allowedSubBoard)
                continue;

            if (superBoard[i] == 0 && subBoardStatus[subBoardIndex] == 0)
                availableMoves.Add(i);
        }
        if (availableMoves.Count == 0)
            return -1;

        // 尝试直接赢取当前子棋盘
        foreach (int move in availableMoves)
        {
            int subBoardIndex = GetSubBoardIndex(move);
            int original = superBoard[move];
            superBoard[move] = 2;
            if (CheckSubBoardWinner(subBoardIndex, 2))
            {
                superBoard[move] = original;
                return move;
            }
            superBoard[move] = original;
        }
        // 尝试阻挡对手必胜走法
        foreach (int move in availableMoves)
        {
            int subBoardIndex = GetSubBoardIndex(move);
            int original = superBoard[move];
            superBoard[move] = 1;
            if (CheckSubBoardWinner(subBoardIndex, 1))
            {
                superBoard[move] = original;
                return move;
            }
            superBoard[move] = original;
        }
        // 尝试选择子棋盘的中心位置（中心格索引为 4）
        foreach (int move in availableMoves)
        {
            if (move % 9 == 4)
                return move;
        }
        // 若无特殊策略，则随机选择
        int randIndex = Random.Range(0, availableMoves.Count);
        return availableMoves[randIndex];
    }

    #endregion
}

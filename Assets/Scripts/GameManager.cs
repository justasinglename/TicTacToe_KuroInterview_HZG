using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // ���������ģʽ�õ� 3x3 ��ť
    public Button[] gridButtons;
    // Super ģʽ�õ� 9x9 ��ť�����ڳ��������� 81 ����ť�����������������У�
    public Button[] superGridButtons;

    public TextMeshProUGUI statusText;   // ����/����ģʽ��ǰ�غ���ʾ
    public TextMeshProUGUI status3Text;    // Super ģʽ��ǰ�غ���ʾ
    public TextMeshProUGUI settleText;     // ����״̬��ʾ
    public Button playerFirstButton, aiFirstButton; // ѡ�����ְ�ť
    public GameObject Menu;
    public TMP_Dropdown modeDropdown;      // ģʽ������
    public GameObject[] modeDescs;
    public TMP_Dropdown difficultyDropdown; // �Ѷ�������
    public GameObject[] difficultyDescs;
    public GameObject Mode12Panels;        // ����/����ģʽ���
    public GameObject Mode3Panels;         // Super ģʽ���

    public enum Mode { Classic, Limit, Super }
    public Mode mode = Mode.Classic;

    // �Ѷ�ö�٣�Classic ģʽ�²��� minimax ������ʿ��ƣ�
    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty aiDifficulty = Difficulty.Hard;

    // ����/����ģʽ 3x3 ����״̬��0�գ�1��ң�2AI
    private int[] board;
    // Super ģʽ 9x9 ����״̬��81 �����ӣ�
    private int[] superBoard;
    // ��¼ 9 ��������״̬��0δ����1���Ӯ��2AIӮ��3ƽ��
    private int[] subBoardStatus;

    private int currentPlayer; // 1=���, 2=AI
    private bool gameOver;
    private bool playerFirst = true; // ����Ƿ�����

    // ���� Limit ģʽ����¼��������˳��
    private List<int> playerMoves;
    private List<int> aiMoves;

    // ���� Super ģʽ����¼������������-1 ��ʾ���ޣ�
    private int allowedSubBoard = -1;

    void Start()
    {
        // ������ѡ��ť
        playerFirstButton.onClick.AddListener(() => StartGame(true));
        aiFirstButton.onClick.AddListener(() => StartGame(false));

        // �� Dropdown �¼�
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

        // ��ʾѡ�����
        Menu.SetActive(true);
    }

    // Dropdown ��ֵ�仯�¼�����
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

    #region ����/����ģʽ���

    // ����/����ģʽ�ĳ�ʼ��
    void InitializeGame()
    {
        board = new int[9];
        gameOver = false;
        currentPlayer = playerFirst ? 1 : 2;
        statusText.text = playerFirst ? "��һغ�" : "AI�غ�";

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

    // ��ҵ�� 3x3 ���̰�ť������/����ģʽ��
    void OnGridButtonClick(int index)
    {
        if (gameOver || board[index] != 0)
            return;

        if (mode == Mode.Limit)
        {
            // ������ 3 �����ӣ����Ƴ������һ��
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
        else // ����ģʽ
        {
            board[index] = 1;
            gridButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "X";
            gridButtons[index].interactable = false;
        }

        if (CheckGameOverClassic())
            return;

        MakeAIMove();
    }

    // AI ���ӣ�����/����ģʽ��
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

    // ����/����ģʽ�����ж�
    bool CheckGameOverClassic()
    {
        if (CheckWin(board))
        {
            settleText.text = currentPlayer == 1 ? "Ӯ�ˣ�" : "���ˣ�";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        else if (CheckDraw(board))
        {
            settleText.text = "ƽ�֣�";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        statusText.text = currentPlayer == 1 ? "��һغ�" : "AI�غ�";
        return false;
    }

    #endregion

    #region Super ģʽ���

    // Super ģʽ��ʼ�������� 9x9 ���̼�������״̬
    void InitializeSuperGame()
    {
        superBoard = new int[81];
        subBoardStatus = new int[9];
        gameOver = false;
        currentPlayer = playerFirst ? 1 : 2;
        status3Text.text = playerFirst ? "��һغ�" : "AI�غ�";

        Mode3Panels.SetActive(true);
        Mode12Panels.SetActive(false);

        // ���������������򣨵�һ�����ޣ�
        allowedSubBoard = -1;

        // ��ʼ�� 9x9 ��ť���ٶ���ť˳���Ѱ��������������У�
        for (int i = 0; i < superGridButtons.Length; i++)
        {
            int index = i;
            superGridButtons[i].onClick.RemoveAllListeners();
            superGridButtons[i].onClick.AddListener(() => OnSuperGridButtonClick(index));
            superGridButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = "";
            // ��ʼʱ��ȫ����Ϊ true������ͳһ����
            superGridButtons[i].interactable = true;
        }

        // ��ʼ������°�ť�Ľ���״̬
        UpdateSuperBoardInteractableButtons();

        if (!playerFirst)
        {
            MakeAISuperMove();
        }
    }

    // ���� Super ģʽ�¸���ť�Ŀɽ���״̬
    void UpdateSuperBoardInteractableButtons()
    {
        for (int i = 0; i < superGridButtons.Length; i++)
        {
            int subBoard = GetSubBoardIndex(i);
            // ��ǰ���ӿ��Ҷ�Ӧ������δ�������
            if (superBoard[i] == 0 && subBoardStatus[subBoard] == 0)
            {
                // ���û�����ƻ��ߵ�ǰ��ť��������������������������ť�ɽ���
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

    // ����� Super ģʽ�µ�� 9x9 ���̰�ť
    void OnSuperGridButtonClick(int index)
    {
        if (gameOver || superBoard[index] != 0)
            return;

        int clickedSubBoard = GetSubBoardIndex(index);
        // ��������������Ӳ�������������ֱ�ӷ��أ���ʱ�ð�ťӦΪ���ɽ�����
        if (allowedSubBoard != -1 && clickedSubBoard != allowedSubBoard)
            return;

        // �������
        superBoard[index] = 1;
        superGridButtons[index].GetComponentInChildren<TextMeshProUGUI>().text = "X";
        superGridButtons[index].interactable = false;

        // ��鵱ǰ�������Ƿ����ʤ�ֻ�ƽ��
        if (subBoardStatus[clickedSubBoard] == 0)
        {
            if (CheckSubBoardWinner(clickedSubBoard, 1))
            {
                subBoardStatus[clickedSubBoard] = 1;
                // �ɸ��� UI ��ǣ���ı������̱�����ɫ��
            }
            else if (CheckSubBoardDraw(clickedSubBoard))
            {
                subBoardStatus[clickedSubBoard] = 3;
                // ���� UI ���Ϊƽ��
            }
        }

        // ����������Ӹ���������������ȡ��ǰ�������ڵ�λ�ã�0~8����Ϊ��һ�������̱��
        int nextAllowed = GetNextAllowedSubBoard(index);
        if (subBoardStatus[nextAllowed] != 0 || CheckSubBoardDraw(nextAllowed))
            allowedSubBoard = -1;
        else
            allowedSubBoard = nextAllowed;

        // �������а�ť�Ŀɽ���״̬
        UpdateSuperBoardInteractableButtons();

        if (CheckSuperGameOver())
            return;

        currentPlayer = 2;
        status3Text.text = "AI�غ�";
        MakeAISuperMove();
    }

    // AI �� Super ģʽ�µ�����
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

        // ���� AI ���Ӹ���������������
        int nextAllowed = GetNextAllowedSubBoard(bestMove);
        if (subBoardStatus[nextAllowed] != 0 || CheckSubBoardDraw(nextAllowed))
            allowedSubBoard = -1;
        else
            allowedSubBoard = nextAllowed;

        // �������а�ť�Ŀɽ���״̬
        UpdateSuperBoardInteractableButtons();

        if (CheckSuperGameOver())
            return;

        currentPlayer = 1;
        status3Text.text = "��һغ�";
    }

    // Super ģʽ�����ж����� subBoardStatus ��Ϊ 3x3 �����ж�����ʤ��
    bool CheckSuperGameOver()
    {
        if (CheckWin(subBoardStatus))
        {
            settleText.text = currentPlayer == 1 ? "Ӯ�ˣ�" : "���ˣ�";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        // �����������̾��Ѿ������������ƽ�֣�����Ϊƽ��
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
            settleText.text = "ƽ�֣�";
            gameOver = true;
            settleText.transform.parent.gameObject.SetActive(true);
            return true;
        }
        return false;
    }

    #endregion

    #region ͨ�ú���

    // ���� 3x3 ʤ���жϣ������� board �� subBoardStatus ���飩
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

    // �����µ����÷�ʽ������������ÿ 9 ��Ϊһ��������
    int GetSubBoardIndex(int index)
    {
        return index / 9;
    }

    // �����·�ʽ��ȡ��ǰ�������������ڵľֲ�������0~8������������һ����������������
    int GetNextAllowedSubBoard(int index)
    {
        return index % 9;
    }

    // ���ĳ�������̣�Super ģʽ���Ƿ�ָ�����Ӯȡ
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

    // �ж�ָ���������Ƿ���������ƽ�������
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

    // ����ģʽ�µ� minimax �㷨�������Ѷ��趨��
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

    // �޸ĺ�� CheckWinner����� boardState ���Ƿ������ҵ�ʤ��
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

    // ��ȡ����߷�������/����ģʽ��
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
        else // Super ģʽ�����ô˷���
        {
            return GetRandomMove();
        }
    }

    // ����ģʽ�µ�����߷�
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

    // AI �� Super ģʽ�µĲ��ԣ����������������ڵĿո�
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

        // ����ֱ��Ӯȡ��ǰ������
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
        // �����赲���ֱ�ʤ�߷�
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
        // ����ѡ�������̵�����λ�ã����ĸ�����Ϊ 4��
        foreach (int move in availableMoves)
        {
            if (move % 9 == 4)
                return move;
        }
        // ����������ԣ������ѡ��
        int randIndex = Random.Range(0, availableMoves.Count);
        return availableMoves[randIndex];
    }

    #endregion
}

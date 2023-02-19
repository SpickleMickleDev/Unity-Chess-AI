using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class GameManager : MonoBehaviour
    {
        public bool loadCustomFen;
        public string customFen = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";
        public TMPro.TMP_Text EvaluationText;

        public Board gameBoard;
        UserInterface boardUI;
        FenLoader fenutil;
        Player humanPlayer;
        Player computerPlayer;
        bool gamePlaying = false;


        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 45;
            // Setting max framerate to 45 allows me to record the game using OBS, whilst 60 frames per second is too high.

            
            gameBoard = new Board();
            boardUI = gameObject.AddComponent<UserInterface>(); // stack overflow said this is how you should add a monobehaviour class since that's how they're designed to run off of a gameObject.
            fenutil = new FenLoader();
            gamePlaying = false;

            humanPlayer = new HumanPlayer(gameBoard);
            computerPlayer = new AIPlayer(gameBoard);

        }

        private void Update()
        {
            
            // player only plays as white in this implementation
            if (!gameBoard.gameFinished)
            {
                if (gameBoard.turnToPlay == 0)
                {
                    humanPlayer.Update();
                }
            }
            else
            {
                gamePlaying = false;
            }

            /*
            if (gamePlaying && gameBoard.turnToPlay == 0) // player can only input if it is their turn
            {
                humanPlayer.Update();
            }
            if (gameBoard.gameFinished)
            {
                gamePlaying = false;
            }*/

        }

        private void UpdateEvaluationText()
        {
            // fetch evaluation and update evaluationText.text = ...
            if (!gameBoard.gameFinished)
            {
                float advantage = gameBoard.CalculateMaterialAdvantage();
                char sign = (advantage > 0) ? '+' : ' ';
                EvaluationText.text = $"Evaluation : \n       {sign}{advantage}";
            }
        }

        private void UpdateEvaluationTextUponWin(int winState)
        {
            string text = string.Empty;
            switch (winState)
            {
                case Board.WhiteWins:
                    text = "White Wins!";
                    break;
                case Board.BlackWins:
                    text = "Black Wins!";
                    break;
                case Board.Draw:
                    text = "It's a Draw!";
                    break;
            }
            EvaluationText.text = text;
        }

        public delegate void MoveMade();

        // need some way of knowing when to switch to the other player's input.
        void MakeMove(Move moveMade)
        {
            // can check for winstates here OR do it separately in HumanPlayer and AIPlayer after their move generation as they both also have access to the board, since the downside to a GameManager approach is that it would need to be called in an Event-Driven way, which I'd need to research.
            // Researched event driven programming using c# and discovered something called delegates.
            // https://www.c-sharpcorner.com/UploadFile/subhendude/event-driven-programming-in-C-Sharp/

            // would need an event within the player class for calling to the GameManager when a move is made.
            // Game manager can reference players and add the SwitchPlayer() procedure to their delegate.

            // Check if game is over

            if (moveMade.isValid)
            {
                // executes move on board within this procedure.
                gameBoard.MakeMove(moveMade);
                boardUI.ChangeBoardFromMove(gameBoard, moveMade, gameBoard.turnToPlay);
                UpdateEvaluationText();
            }

            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(gameBoard);
            List<Move> moves = moveGenerator.GenerateMoves();

            if (moves.Count == 0)
            {
                gameBoard.gameFinished = true;
                gamePlaying = false;
                if (moveGenerator.playerInCheck)
                {
                    gameBoard.winState = (gameBoard.turnToPlay == 1) ? Board.WhiteWins : Board.BlackWins;
                    string winner = (gameBoard.turnToPlay == 1) ? "White" : "Black";
                    Debug.Log($"{winner} wins!");

                }
                else
                {
                    gameBoard.winState = Board.Draw;
                    Debug.Log("It's a draw!");
                }
                GameOver();
            }

            if (gameBoard.fiftyMoveCounter >= 100)
            {
                Debug.Log("It's a draw!");
            }


            if (!gameBoard.gameFinished && gameBoard.turnToPlay == 1)
            {
                computerPlayer.NotifyToPlay();
            }

            



        }


        public void StartGame()
        {
            gamePlaying = true;
            if (loadCustomFen)
            {
                gameBoard.loadFromFen = true;
                gameBoard.initialFen = customFen;
            }
            gameBoard.Setup();

            boardUI.RedrawPieces(gameBoard);
            boardUI.ResetSquares();

            humanPlayer.MoveSelected += MakeMove;
            computerPlayer.MoveSelected += MakeMove;

            // if starting from a custom position in which black plays first, the AI will need to be notified. 
            if (gameBoard.turnToPlay == 1)
            {
                computerPlayer.NotifyToPlay();
            }

            //MovementLookupTesting test = new MovementLookupTesting(); //for testing movement lookups but fully functional now
            //test.PerformTestInConsole();
            //test.PerformTestOnBoard(boardUI);
        }

        public void ExportGame()
        {
            // referencing unity documentation https://docs.unity3d.com/ScriptReference/Application.OpenURL.html

            if (gamePlaying)
            {
                string fen = fenutil.GetCurrentFen(gameBoard);
                string url = "https://www.lichess.org/paste?pgn=" + fen;
                Application.OpenURL(url);
            }
        }

        public void GameOver() // 0 - white, 1 - black, 2 - stalemate
        {
            int winState = gameBoard.winState;
            UpdateEvaluationTextUponWin(winState);
            gameBoard.gameFinished = true;
            gamePlaying = false;

            //StartGame();
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ChessAI
{
    class GameManager : MonoBehaviour
    {
        // ability to input a custom board state from within the game editor, allowing for much easier debugging.
        // (when the variable is made global and public, the editor, Unity, recognises it and allows you to modify it within the editor by the way)
        public bool loadCustomFen;
        public string customFen = "1rbq1r1k/2pp2pp/p1n3p1/2b1p3/R3P3/1BP2N2/1P3PPP/1NBQ1RK1 w - - 0 1";

        // variables for within the game editor Unity. Allows me to modify on-screen text variables and call audio files in events of captures and moves etc. 
        // button inputs are also handled within this .cs file, however they are represented as public void procedures at the end of the script, which are called by the buttons directly.
        public TMPro.TMP_Text EvaluationText;
        public TMPro.TMP_Text DepthText;
        public TMPro.TMP_Text MaterialText;
        public GameObject coordsDisplay;
        public Slider depthSlider;
        private AudioSource speaker;
        public AudioClip capturePieceSound;
        public AudioClip selectPieceSound;
        public AudioClip gameOverSound;
        public AudioClip movePieceSound;
        public AudioClip checkSound;

        // constant values to represent what type of audio call is being made.
        private const int selectPieceValue = 1;
        private const int movePieceValue = 2;
        private const int capturePieceValue = 3;
        private const int gameOverValue = 4;
        private const int checkSoundValue = 5;

        // specifies the computer depth, modifiable by the depth slider within the game.
        private int computerDepth = 3;

        public Board gameBoard;
        UserInterface boardUI;
        Player humanPlayer;
        Player computerPlayer;

        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 45;
            // Setting max framerate to 45 allows me to record the game using OBS, whilst 60 frames per second is too high and the output is laggy and unclear.

            // gets access to audio source within the game editor
            speaker = FindObjectOfType<AudioSource>();
            speaker.loop = false;
           
            gameBoard = new Board(); // instantiates a new board for the game to be played on
            boardUI = gameObject.AddComponent<UserInterface>(); // stack overflow said this is how you should add a monobehaviour class since that's how they're designed to run off of a gameObject. instantiating it as a new UserInterface produces errors in accessing variables and calling functions.
           
            // initialise the player classes
            humanPlayer = new HumanPlayer(gameBoard);
            computerPlayer = new AIPlayer(gameBoard, computerDepth);

            // adds functions to be called when each player raises the MoveSelected event, allowing the user and AI to choose when to make their moves without halted code.
            humanPlayer.MoveSelected += MakeMove;
            computerPlayer.MoveSelected += MakeMove;
            computerPlayer.UpdateEvalCall += UpdateEvaluationText; // updates the evaluation of the board when the AI has chosen their move.
        }

        public void PlaySound(int eventValue)
        {
            // plays sound to player when a piece is selected, moved, captured etc. 
            bool soundSelected = true;
            switch (eventValue)
            {
                // selects sound to play
                case selectPieceValue:
                    speaker.clip = selectPieceSound;
                    break;
                case movePieceValue:
                    speaker.clip = movePieceSound;
                    break;
                case capturePieceValue:
                    speaker.clip = capturePieceSound;
                    break;
                case gameOverValue:
                    speaker.clip = gameOverSound;
                    break;
                case checkSoundValue:
                    speaker.clip = checkSound;
                    break;
                default:
                    soundSelected = false;
                    break;
            }
            if (soundSelected)
            {
                // plays sound
                speaker.Play();
            }
        }

        private void Update()
        {
            
            // player only plays as white in this implementation
            if (!gameBoard.gameFinished)
            {
                if (gameBoard.turnToPlay == 0)
                {
                    // if it is the player's turn to play then allow them to input moves / select things with their mouse.
                    humanPlayer.Update();
                }
            }
        }

        private void UpdateEvaluationText(float eval)
        {
            // updates the text on the screen with the current evaluation.
            // if evaluation < 0 then black is winning.
            // if evaluation > 0 then white is winning.
            // if evaluation == 0 then the game is even.

            // for clarification, all evaluations are from the AI's perspective so they aren't absolute.
            eval = (float)Math.Round((float)eval, 2);
            if (!gameBoard.gameFinished)
            {
                char sign = (eval > 0) ? '+' : ' ';
                EvaluationText.text = $"Evaluation : \n       {sign}{eval}";
            }
        }

        private void UpdateEvaluationTextUponWin(int winState)
        {
            // when the game is over, the evaluation text updates to indicate who has won or if it was a draw.
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

        private void UpdateMaterialAdvantageText()
        {
            // if material advantage == 0, display = string.empty;
            // if material advantage < 0, display -(advantage)
            // if material advantage > 0, display +(advantage)

            int materialAdvantage = (int)gameBoard.CalculateMaterialAdvantage();

            if (materialAdvantage > 0)
            {
                MaterialText.text = '+' + materialAdvantage.ToString();
            }
            else if (materialAdvantage < 0)
            {
                MaterialText.text = materialAdvantage.ToString();
            }
            else
            {
                MaterialText.text = string.Empty;
            }
        }

        private void ResetDisplayTexts()
        {
            // When the player undoes a move or starts a new game, I didn't like how it kept the same material counter in the top right (e.g. '+2') and kept the same evaluation text, so I instead reset them both when the player undoes a move or starts a new game.
            // The reason that the evaluation text is reset to being empty, as opposed to showing the previous evaluation, this is because the evaluation text is updated when the AI player makes their move, and the evaluation is not saved. Can be easily fixed by adding the evaluations on a stack, however it didn't feel necessary or helpful so I didn't implement it.
            EvaluationText.text = "Evaluation : ";
            UpdateMaterialAdvantageText();
        }

        void MakeMove(Move moveMade)
        {

            // can check for winstates here OR do it separately in HumanPlayer and AIPlayer after their move generation as they both also have access to the board, since the downside to a GameManager approach is that it would need to be called in an Event-Driven way, which I'd need to research.
            // Advantage to handling winstates in game manager is that it is much cleaner handling it within the move making process and allows for an event-driven move making system, which would open up the possiblity to multithreading the AI's moves in the future.
            // ( I did try multithreading in the end, however couldn't get it to work well with Unity. )
            // Researched event driven programming using c# and discovered something called delegates.
            // https://www.c-sharpcorner.com/UploadFile/subhendude/event-driven-programming-in-C-Sharp/

            // would need an event within the player class for calling to the GameManager when a move is made.
            // Game manager can reference players and add the MakeMove() procedure to their delegate.


            int takenPiece = gameBoard.boardState[moveMade.targetSquare];

            if (moveMade.isValid)
            {
                // executes move on board within this procedure.
                
                gameBoard.MakeMove(moveMade);
                boardUI.ChangeBoardFromMove(gameBoard, moveMade, gameBoard.turnToPlay);
                // animates move
            }

            // updates text displaying material advantage in the top right (e.g. '+2')
            UpdateMaterialAdvantageText();


            // Creates list of legal moves for new player to check if game is over
            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(gameBoard);
            List<Move> moves = moveGenerator.GenerateMoves();

            // handles sounds being played if the move being made is a check, capture or simply just a move
            if (moveGenerator.playerInCheck) // even if move captured a piece, prioritises playing the check sound as it is a more important threat for the player to know about
            {
                PlaySound(checkSoundValue);
            }
            else if (takenPiece == Piece.empty) // if the move is just a move to another square and not a capture
            {
                PlaySound(movePieceValue);
            }
            else // if the move captured a piece and wasn't a check
            {
                PlaySound(capturePieceValue);
            }

            if (moves.Count == 0 || gameBoard.fiftyMoveCounter >= 100) // checks if there are no legal moves or the 50 move rule has been triggered, in which no pawns have moved and no pieces have been captured in the last 50 moves.
            {
                gameBoard.gameFinished = true;
                if (moveGenerator.playerInCheck && (gameBoard.fiftyMoveCounter < 100)) // if player is in check and has no legal moves, therefore is checkmate.
                {
                    gameBoard.winState = (gameBoard.turnToPlay == 1) ? Board.WhiteWins : Board.BlackWins;

                }
                else // the game is a draw if the player has no legal moves but isn't in check (stalemate) or a 50 move rule has been triggered / met.
                {
                    gameBoard.winState = Board.Draw;
                }
                GameOver();
            }

            // insufficient material draw
            // i.e. recognises that the game is over and a draw if neither player has enough pieces to actually checkmate the opponent, meaning it must be a draw regardless of what they play.

            // Minimum material to checkmate opponent is as follows:
            // At least 1 pawn means they can checkmate the opponent (can promote to queen)
            // At least 1 queen or rook means they can checkmate the opponent
            // 2 Bishops together can checkmate the opponent
            // A bishop and a knight together can checkmate the opponent
            // 2 knights together can checkmate the opponent ONLY if the opponent has more than just a king. This is because in order to checkmate with 2 knights, you must trap the king and then checkmate them afterwards. If the opponent has just a king, this will lead to a stalemate as they have no legal moves but aren't in check. If the opponent can play in that position, however, they will then be checkmated on the next move.

            // if there are no pawns on the board
            if (gameBoard.pawns[0].Count + gameBoard.pawns[1].Count == 0)
            {
                // only needed checking the counts of each side's pawns, rooks, queens etc. once so didn't feel it was worth it to save them as integer variables of the piece counts.
                // if no queens or rooks on the board
                if (gameBoard.rooks[0].Count + gameBoard.rooks[1].Count + gameBoard.queens[0].Count + gameBoard.queens[1].Count == 0)
                {
                    // can win with two bishops
                    // can win with bishop and knight
                    // can win with two knights ONLY IF opponent has another piece
                    // HOWEVER, at this point we know opponent won't have any piece other than a knight or bishop so only need to check for those.

                    // insufficient material draw is only met when neither player has the minimum material required to checkmate their opponent. As such, I am assuming that it is a draw and then if either of the players can checkmate their opponent, it is no longer considered a draw and the game can continue.
                    bool gameDraw = true;
                    for (int i = 0; i <= 1; i++) // check for both players
                    {
                        int numBishops = gameBoard.bishops[i].Count;
                        int numKnights = gameBoard.knights[i].Count;

                        if (numBishops > 0)
                        {
                            if (numKnights > 0 || numBishops > 1)// If player has Knight + Bishop or 2 x Bishops, player has sufficient material to checkmate
                            {
                                gameDraw = false;
                            }
                            
                        }
                        if (numKnights > 1) // if player has 2 knights
                        {
                            int numOpponentPieces = gameBoard.bishops[1 - i].Count + gameBoard.knights[1 - i].Count;
                            if (numOpponentPieces > 0) // if opponent has a piece
                            {
                                gameDraw = false;
                            }

                            // DISCLAIMER : 
                            // In chess, having 3 knights is also considered a forced checkmate, without the opponent needing to have a piece. As a result, I would make the above if statement :
                            // if (numOpponentPieces > 0 || numKnights > 2) {}
                            // However, I have not implemented promoting pawns to knights, bishops and rooks, so the maximum knights that a player can have is 2 anyway.
                        }

                    }

                    if (gameDraw)
                    {
                        // if insufficient material to mate, end the game and declare it a draw.
                        gameBoard.winState = Board.Draw;
                        GameOver();
                    }

                }
            }


            // draw by repetition not accounted for yet, would be if zobrist hashing is introduced.

            // if the game is still running and it is now the AI's turn to play, notify the AI to choose their move.
            // In the future, I would like to add Computer vs Computer playing, so would make this a more modular format such as if (players[gameBoard.turnToPlay] is AIPlayer) {}, with the players[] array storing the two players for black and white, which could each be either human player or AI player
            if (!gameBoard.gameFinished && gameBoard.turnToPlay == 1)
            {
                computerPlayer.NotifyToPlay();
            }
        }

        public void ChangeDepth()
        {
            // When the depth slider on the right of the screen is changed, changes the depth that the AI will search to.
            int depth = (int)Math.Round(depthSlider.value, 2);
            DepthText.text = $"Depth : {depth}";
            computerPlayer.depth = depth;
            computerPlayer.ChangeDepth(depth);
            computerDepth = depth;
        }

        public void StartGame()
        {
            // When the game starts, turn on the coordinates at the side of the board
            coordsDisplay.SetActive(true);
            if (loadCustomFen)
            {
                // if loading from a custom position within the editor for debugging, set the initial board state to load as the one inputted, rather than the standard starting position.
                gameBoard.loadFromFen = true;
                gameBoard.initialFen = customFen;
            }
            // sets up the board's piecelists, grid, etc.
            gameBoard.Setup();

            // draw the board's squares and pieces
            boardUI.RedrawPieces(gameBoard);
            boardUI.ResetSquares();

            // when a new game starts, reset the evaluation and material advantage counters
            ResetDisplayTexts();

            // if starting from a custom position in which black plays first, the AI will need to be notified. 
            if (gameBoard.turnToPlay == 1)
            {
                computerPlayer.NotifyToPlay();
            }

            //MovementLookupTesting test = new MovementLookupTesting(); //for testing movement square lookups but fully functional now
            //test.PerformTestInConsole();
            //test.PerformTestOnBoard(boardUI);
        }

        public void ExportGame()
        {
            // referencing unity documentation https://docs.unity3d.com/ScriptReference/Application.OpenURL.html
            
            
            // str = fenutil.GetCurrentFen(gameBoard);
            // DID provide the final fen state, however now preferring to provide the whole game's moves.

            
            // list of moves played
            string listOfMoves = string.Empty;
            Stack<string> movesList = gameBoard.gameMovesAsString;
            Stack<string> reverseStack = new Stack<string>();
            Stack<string> revertedStack = new Stack<string>();

            // reverses the stack containing the list of moves, by popping each item and pushing them onto another stack. This will then allow me to read the moves throughout the game from the start to the end.
            while (movesList.Count > 0)
            {
                reverseStack.Push(movesList.Pop()); // reverses stack
            }

            // loops through the list of moves on the stack
            int moveCounter = 0;
            while (reverseStack.Count > 0)
            {
                // The list of moves in a game goes in the following format:
                // 1. e4 e5 2. Nc3 Nf6 3. d4
                // {The counter of which move was played, e.g. move 1} {white's move in readable move notation}. {black's move in readable move notation} {... next moves ...} 
                // Something important that I wanted to make sure of was that if white won on their last move, it would still display white's last move, even though that's where the stack of moves ended so attempting to read black's next move could lead to an error. That's why I then checked if the stack was empty or not before listing black's move.
                revertedStack.Push(reverseStack.Peek()); // adding back the moves to a reverted stack, recovering the stack in the order that it was previously, in order to allow the user to export multiple times.

                listOfMoves += $"{moveCounter + 1}. {reverseStack.Pop()} ";
                if (reverseStack.Count > 0)
                {
                    revertedStack.Push(reverseStack.Peek());
                    listOfMoves += $"{reverseStack.Pop()} ";
                }
                moveCounter++;
            }

            gameBoard.gameMovesAsString = revertedStack;
            // opens the url to paste the game's moves to a lichess analysis board.
            string url = "https://www.lichess.org/paste?pgn=" + listOfMoves;
            System.Diagnostics.Process.Start(url);
        }

        public void TestUnmakeMove()
        {
            // unmakes the last two moves in the chess game.
            // This is because the AI doesn't include randomness and therefore will always evaluate the same position in the same way each time and will always choose the same move.
            // As a result, unmaking one move will then just make the AI play the exact same move again, so unmaking two moves is necessary in order to give the human player a chance to change their move in which they played.

            if (gameBoard.movesPlayed > 1) // count starts from 1 in standard chess fen notation. Therefore before any move has been played in the game, it considers itself to be ON move 1, not having 1 move already passed.
            {
                // unmakes AI's move
                Move move = gameBoard.gameMoves.Peek();
                gameBoard.UnmakeMove(move);
                // if the game finished (e.g. from checkmate), allows the player to go back to when the game was still in play.
                gameBoard.gameFinished = false;
                gameBoard.winState = 0;
                boardUI.ChangeBoardFromMove(gameBoard, new Move(move.targetSquare, move.initialSquare), 0); // animate return to position

                // unmakes player's move
                Move move2 = gameBoard.gameMoves.Peek();
                gameBoard.UnmakeMove(move2);
                boardUI.ChangeBoardFromMove(gameBoard, new Move(move2.targetSquare, move2.initialSquare), 0); // animate return to position

                if (gameBoard.turnToPlay == 1)
                {
                    computerPlayer.NotifyToPlay();
                }
                ResetDisplayTexts(); // resets the evaluation display and updates the material advantage counter to the previous move's material advantage.
            }
        }

        public void QuitApp()
        {
            // The X button in the top right will call this function to quit the game.
            Application.Quit();
        }

        public void GameOver()
        {
            // When the game is over, plays a sound that indicates the game is over, updates the evaluation text and declares the end of the game by changing the board's gameFinished value.
            PlaySound(gameOverValue);
            int winState = gameBoard.winState;
            UpdateEvaluationTextUponWin(winState);
            gameBoard.gameFinished = true;

            // Could automatically start the next game, however that wouldn't allow the player to export their game's moves or see who won.
            //StartGame();
        }
    }
}

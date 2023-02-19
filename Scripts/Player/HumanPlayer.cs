using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class HumanPlayer : Player
    {
        // deal with player input

        // https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html
        // Unity documentation for mouse input handling

        bool isSquareSelected = false;
        bool dragging = false;
        Coord selectedSquare;
        Camera camera;
        Board board;
        UserInterface UI;

        public HumanPlayer(Board board)
        {
            this.board = board;
            this.UI = GameObject.FindObjectOfType<UserInterface>();
            this.camera = Camera.main;
        }

        // Different input types:
        // - Select one square
        //   - Select second square to move piece
        // - Drag piece

        // After dragging piece, check if move legal
        // - If false, keep selected square but cancel move
        // - If true, make move and remove selected square
        // i.e. Check if move legal, if so then change board, RedrawPieceS() then only remove selection if move legal

        // After selecting piece, if different piece selected before then check if move legal
        // - If false, remove selected square and cancel move
        // - If true, make move and remove selected square
        // i.e. Check if move legal, if so then change board, RedrawPieces() then remove selection regardless



        // Since class doesn't derive from MonoBehaviour, update will have to be manually called by GameManager per update. Will only need to run whilst it is player's turn to move, stopping the player from being able to play when it is not their move too.

        public override event MoveMade MoveSelected;

        public override void Update()
        {
            // deselect everything if mouse is pressed off board 
            // If clicks

            if (Input.GetMouseButtonDown(1))
            {
                if (dragging)
                {
                    dragging = false;
                    isSquareSelected = false;
                    UI.RedrawPieces(board);
                    UI.ResetSquares();
                }
            }

            if (Input.GetMouseButton(0))
            {
                if (dragging)
                {
                    UI.DragPiece(selectedSquare, camera.ScreenToWorldPoint(Input.mousePosition));
                }
                else
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (IsMouseOnBoard())
                        {
                            // debugging
                            int square = GetSquareAtMousePos().CoordAsGridNum();
                            int piece = board.boardState[square];
                            if (Piece.GetPieceType(piece) != Piece.empty && Piece.GetPieceColour(piece) == Piece.white)
                            {
                                /*LegalMovesGenerator generator = new LegalMovesGenerator(board);
                                List<Move> moves = generator.GenerateMoves();
                                List<int> squaresTo = new List<int>();
                                foreach (Move move in moves)
                                {
                                    if (move.initialSquare == square)
                                    {
                                        squaresTo.Add(move.targetSquare);
                                    }
                                }
                                squaresTo.Add(square); // highlight self too so it doesn't look weird
                                UI.HighlightSquaresForMoveGenerationTesting(squaresTo);
                                */
                                UI.HighlightSquareSelection(board, square);
                            }
                            // end of debugging
                            if (isSquareSelected)
                            {
                                MoveBySelection(selectedSquare, GetSquareAtMousePos());
                            }
                            else
                            {
                                piece = board.boardState[GetSquareAtMousePos().CoordAsGridNum()];
                                if ((Piece.GetPieceType(piece) != Piece.empty) && (Piece.GetPieceColour(piece) == board.turnToPlay))
                                {
                                    selectedSquare = GetSquareAtMousePos();
                                    isSquareSelected = true;
                                    dragging = true;
                                    //UI.SelectSquare(selectedSquare.CoordAsGridNum());
                                }
                            }
                        }
                    }
                }

            }
            else
            {
                if (dragging)
                {
                    dragging = false;
                    ReleaseDrag(selectedSquare, GetSquareAtMousePos());
                }
            }

        }

        private bool IsMouseOnBoard()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    return true;
                }
            }
            return false;
        }



        // Tried making GetSquareAtMousePos function type 'Coord?' as microsoft said that makes the value nullable but didn't work 
        // Therefore resorting to checking if mouse is within board in separate function
        private Coord GetSquareAtMousePos()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);

            // positions go from 0 - 3.5f to 7 - 3.5f with width 1 and centre as pivot
            // Therefore grid ranges from:
            // -4f -> +4f
            // If I add 4f then index is fetchable as values range from 0 to 8f.
            // Index is position + 4f rounded down / truncated
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    // Truncating the x and y coordinates + 4 requires lots of conversion too decimal, int etc.
                    // Therefore adding 3.5f and rounding it to nearest integer as functions the same but more convenient / efficient
                    int xCoord = (int)Math.Round(mousePos.x + 3.5f);
                    int yCoord = (int)Math.Round(mousePos.y + 3.5f);

                    return new Coord(xCoord, yCoord);
                }
            }
            return new Coord();
        }

        private void ReleaseDrag(Coord initialSquare, Coord targetSquare)
        {
            if (initialSquare.Equals(targetSquare) || !IsMouseOnBoard())
            {
                UI.RedrawPieces(board);
                return;
            }
            // Check if move legal, if so then change board, RedrawPieceS() then only remove selection if move legal
            bool isMoveLegal = TryMove(initialSquare, targetSquare);

            if (isMoveLegal)
            {
                isSquareSelected = false;
            }
            else
            {
                UI.RedrawPieces(board);
            }

        }

        private void MoveBySelection(Coord initialSquare, Coord targetSquare)
        {
            // Check if move legal, if so then change board, RedrawPieces() then remove selection regardless
            TryMove(initialSquare, targetSquare);
            isSquareSelected = false;
        }

        public override void NotifyToPlay()
        {

        }

        private bool TryMove(Coord initialSquare, Coord targetSquare)
        {
            // since this function holds the legal move generator, moves can be made quicker within this so I will make the move in this function and only return whether or not it went through to the Drag/Select release functions
            // Therefore can still modify the square selection as it should function, whilst making the move execution more efficient.

            UI.ResetSquares();

            // square selected still true
            if (Piece.GetPieceColour(board.boardState[selectedSquare.CoordAsGridNum()]) != 0)
            {
                return false;
            }

            // generate moves

            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(board);
            List<Move> moves = moveGenerator.GenerateMoves();
            //List<Move> moves = moveGenerator.FetchPieceBitmapAsMoveList(initialSquare);
            
            
            //Debug.Log($"Player has {moves.Count} moves");
            
            Move move = Move.nullMove;
            // did use a boolean variable to track if the move is valid but changed it to implement a 'nullable' option in the move struct as there was an error where move wasn't defined as it couldn't be made null.

            // check if legal
            foreach (Move mv in moves)
            {
                //Debug.Log($"{Coord.IndexToString(mv.initialSquare)}-{Coord.IndexToString(mv.targetSquare)}");
                if (mv.initialSquare == initialSquare.CoordAsGridNum() && mv.targetSquare == targetSquare.CoordAsGridNum())
                {
                    move = mv;
                    //Debug.Log($"{Coord.IndexToString(mv.initialSquare)}-{Coord.IndexToString(mv.targetSquare)}");
                }

                // My main method of testing possible legal moves by putting it into a readable format and analysing each piece's possible moves to narrow down any issues to a specific piece's behaviour
                //Debug.Log($"{board.ConvertMoveToNotation(mv)}, {Coord.IndexToString(mv.initialSquare)}, {Coord.IndexToString(mv.targetSquare)}");
            }

            /*
             * Removed since this function will only be called when the human attempts a move, meaning if white loses then they'd have to try and move another piece that they can't, which I don't find very polished.
             * Will instead handle in either the AIPlayer or GameManager.
             * 
            if (moves.Count == 0) // no legal moves so it's the end of the game
            {
                int winState = Board.Stalemate;
                if (moveGenerator.playerInCheck)
                {
                    winState = Board.BlackWins;
                }
                // checking for if black wins can be done in the AIPlayer.
            }*/

            // execute move

            if (move.isValid)
            {
                // temp replacement, normally add to piecelists etc too
                if (MoveSelected != null) // makes move, THEN notifies AI to play.
                {
                    MoveSelected(move);
                }
            }


            // - change board
            // - change to AI's move
            // - call for AI to make move
            // - mark move in list of moves
            // - store fen
            // - zobrist hashing shenanigans

            return move.isValid;
        }


    }
}


/*
 * using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    public class MouseInput
    {

        // https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html
        // Unity documentation for mouse input handling

        bool isSquareSelected = false;
        bool selectedOnLastUpdate = false;
        bool dragging = false;
        Coord selectedSquare;
        Camera camera;
        Board board;
        UserInterface UI;


        public MouseInput(Board board)
        {
            this.board = board;
            this.UI = GameObject.FindObjectOfType<UserInterface>();
        }

        // Different input types:
        // - Select one square
        //   - Select second square to move piece
        // - Drag piece
        
        // After dragging piece, check if move legal
        // - If false, keep selected square but cancel move
        // - If true, make move and remove selected square

        // After selecting piece, if different piece selected before then check if move legal
        // - If false, remove selected square and cancel move
        // - If true, make move and remove selected square


        void Update()
        {

            // deselect everything if mouse is pressed off board 

            if (Input.GetMouseButtonDown(0)) // if mouse is down
            {
                if (IsMouseOnBoard())
                {
                    if (isSquareSelected) // if square already selected
                    {
                        if (selectedOnLastUpdate) // if player was holding down input last frame and they are holding it now then we can know they are dragging it
                        {
                            dragging = true;
                            UpdateDraggingAnimation();
                        }
                        else // if  player just selected another square
                        {
                            if (IsMouseOnBoard())
                            {
                                Coord targetSquare = GetSquareAtMousePos();
                                MoveBySelection(selectedSquare, targetSquare);

                            }
                        }
                        selectedOnLastUpdate = false;
                    }
                    else // first time selecting
                    {
                        selectedOnLastUpdate = true;
                        isSquareSelected = true;
                        selectedSquare = GetSquareAtMousePos();

                    }
                }
                else // if mouse isn't on board then deselect piece
                {
                    if (!dragging)
                    {
                        isSquareSelected = false;
                    }
                }

            }
            else
            {
                if (dragging)
                {
                    Coord targetSquare = GetSquareAtMousePos();
                    if (!targetSquare.Equals(selectedSquare))
                    {
                        ReleaseDrag(selectedSquare, targetSquare);
                    }
                }
                dragging = false;
                selectedOnLastUpdate = false;
            }
        }

        private void UpdateDraggingAnimation()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);



        }


        // Tried making GetSquareAtMousePos function type 'Coord?' as microsoft said that makes the value nullable but didn't work 
        // Therefore resorting to checking if mouse is within board in separate function
        private bool IsMouseOnBoard()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    return true;
                }
            }
            return false;
        }



        private Coord GetSquareAtMousePos() 
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            
            // positions go from 0 - 3.5f to 7 - 3.5f with width 1 and centre as pivot
            // Therefore grid ranges from:
            // -4f -> +4f
            // If I add 4f then index is fetchable as values range from 0 to 8f.
            // Index is position + 4f rounded down / truncated
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    // Truncating the x and y coordinates + 4 requires lots of conversion too decimal, int etc.
                    // Therefore adding 3.5f and rounding it to nearest integer as functions the same but more convenient / efficient
                    int xCoord = (int)Math.Round(mousePos.x + 3.5f);
                    int yCoord = (int)Math.Round(mousePos.y + 3.5f);

                    return new Coord(xCoord, yCoord);
                }
            }
            return new Coord();
        }

        private void ReleaseDrag(Coord initialSquare, Coord targetSquare)
        {

        }

        private void MoveBySelection(Coord initialSquare, Coord targetSquare)
        {

        }

    }
}

 * 
 * 
 */
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
        public override event UpdateEvaluation UpdateEvalCall;

        public override void ChangeDepth(int depthVal) {}
        public override void NotifyToPlay() {}

        public override void Update()
        {
            // deselect everything if mouse is pressed off board 

            // If right clicks, deselects piece and sends it back
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

            if (Input.GetMouseButton(0)) // if left click is being held down for this frame
            {
                if (dragging) // if the piece is being dragged, animate that
                {
                    UI.DragPiece(selectedSquare, camera.ScreenToWorldPoint(Input.mousePosition));
                }
                else
                {
                    if (Input.GetMouseButtonDown(0)) // if left click just first pressed down on this frame, i.e. just selected something or started dragging
                    {
                        if (IsMouseOnBoard()) // if the mouse is on the board
                        {
                            int square = GetSquareAtMousePos().CoordAsGridNum(); // gets square that the mouse is over
                            int piece = board.boardState[square]; // gets the piece selected
                            
                            if (Piece.GetPieceType(piece) != Piece.empty && Piece.GetPieceColour(piece) == Piece.white) // if the square isn't empty and the piece is white
                            {
                                UI.HighlightSquareSelection(board, square); // highlights possible legal moves for the piece
                            }
                            if (isSquareSelected) // if a square has already previously been selected, try to move this piece there
                            {
                                MoveBySelection(selectedSquare, GetSquareAtMousePos());
                            }
                            else // if this is the first time selecting a piece
                            {
                                piece = board.boardState[GetSquareAtMousePos().CoordAsGridNum()];
                                if ((Piece.GetPieceType(piece) != Piece.empty) && (Piece.GetPieceColour(piece) == board.turnToPlay))
                                {
                                    selectedSquare = GetSquareAtMousePos(); // set this square to be the selected square
                                    isSquareSelected = true;
                                    dragging = true; // start dragging the piece. If only intending to select the piece and then click another square to move it, stopping selecting on the same square will try to drag, immediately fail and then keep the square selected, allowing another square to be selected and it moving the piece there.
                                }
                            }
                        }
                    }
                }

            }
            else // if not holding down left click
            {
                if (dragging) // if was dragging, try to move the piece by dragging
                { 
                    dragging = false;
                    ReleaseDrag(selectedSquare, GetSquareAtMousePos());
                }
            }

        }

        private bool IsMouseOnBoard()
        {
            // gets mouse position in coordinates and checks whether or not that lies over the board's area
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
            if (initialSquare.Equals(targetSquare) || !IsMouseOnBoard()) // if mouse isn't on board or trying to drag to the same square, reject it and redraw the pieces to where they should be
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
            if (initialSquare.Equals(targetSquare) || !IsMouseOnBoard())
            {
                UI.RedrawPieces(board); // if you double click a piece, it deselects it
                UI.ResetSquares();
                isSquareSelected = false;
                return;
            }
            // Check if move legal, if so then change board, RedrawPieces() then remove selection regardless
            TryMove(initialSquare, targetSquare);
            isSquareSelected = false;
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
            
            Move move = Move.nullMove;
            // did use a boolean variable to track if the move is valid but changed it to implement a 'nullable' option in the move struct as there was an error where move wasn't defined as it couldn't be made null.

            // check if legal
            foreach (Move mv in moves)
            {
                if (mv.initialSquare == initialSquare.CoordAsGridNum() && mv.targetSquare == targetSquare.CoordAsGridNum())
                {
                    move = mv;
                }

                // My main initial method of testing possible legal moves by putting it into a readable format and analysing each piece's possible moves to narrow down any issues to a specific piece's behaviour
                //Debug.Log($"{board.ConvertMoveToNotation(mv)}, {Coord.IndexToString(mv.initialSquare)}, {Coord.IndexToString(mv.targetSquare)}");
            }

            // execute move

            if (move.isValid)
            {
                if (MoveSelected != null) // if event call exists
                {
                    MoveSelected(move); // call event to make the selected move
                }
            }


            return move.isValid;
        }

    }

}
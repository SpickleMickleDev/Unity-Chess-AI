using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class BoardTest
    {
        private int[] grid;

        public BoardTest(int[] boardGrid)
        {
            this.grid = boardGrid;
            RunTest();
        }

        // For purpose of testing the state of the board that it is as intended / correct
        // Lay out the current state of the board in the format BRook BKnight BBishop BQueen..
        //                                                      BPawn BPawn BPawn...
        //                                                      - - - - ...

        public string GetPieceDisplay(int piece) // converts the piece's value into a string representing what type of piece it is
        {
            bool isPieceWhite = Piece.IsPieceWhite(piece);
            string pieceType;
            switch (Piece.GetPieceType(piece))
            {
                case Piece.pawn:
                    pieceType = "Pawn";
                    break;
                case Piece.rook:
                    pieceType = "Rook";
                    break;
                case Piece.knight:
                    pieceType = "Knight";
                    break;
                case Piece.bishop:
                    pieceType = "Bishop";
                    break;
                case Piece.queen:
                    pieceType = "Queen";
                    break;
                case Piece.king:
                    pieceType = "King";
                    break;
                default:
                    return "-";
            }

            return ((isPieceWhite) ? "W" : "B") + pieceType; // adds the colour of the piece to the string
        }


        void RunTest()
        {
            // save state of board as array of strings and then output said strings in the form of a debug log
            int row = 7;
            int col = 0;
            bool finishedOutput = false;
            string[] thisRow = new string[8];

            while (!finishedOutput)
            {
                thisRow[col] = GetPieceDisplay(grid[col + (row * 8)]);

                col++;
                if (col >= 8)
                {
                    Debug.Log(string.Join(" ", thisRow)); // outputs each row as the piece with a space in between
                    thisRow = new string[8];
                    row--;
                    col = 0;
                }
                if (row < 0)
                {
                    finishedOutput = true;
                }
            }
        }
    }
}

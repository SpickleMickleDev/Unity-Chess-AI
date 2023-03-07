using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class TestPieceLists
    {
        // returns whether or not piece lists are working correctly
        // This is for when creating the AI's search algorithm, the board's unmake move procedure wasn't working correctly and mishandled the piece lists, so this was to figure out exactly at which stage it would go wrong.
        // This then allowed me to identify that the issue was with the pawns' perceived positions being different to where they actually were, since the en passant's piece list handling was incorrect and would reference the wrong team's pawn piece list.
        public bool PerformTest(Board board)
        {
            PieceList[] piecelists = board.pieceLists;
            int[] pieceTypes = new int[5] {Piece.pawn, Piece.rook, Piece.knight, Piece.bishop, Piece.queen};
            int[] pieceColours = new int[2] { Piece.white, Piece.black };
            int[] boardState = board.boardState;
            bool allLocationsCorrect = true;

            foreach (int colour in pieceColours)
            {
                foreach (int type in pieceTypes)
                {
                    PieceList pieces = piecelists[colour | type];
                    for (int i = 0; i < pieces.Count; i++)
                    {
                        if (boardState[pieces.CoordinateArray[i]] != (type | colour))
                        {
                            allLocationsCorrect = false;
                            Debug.Log($"Piece list {type | colour} thinks a piece is at {Coord.IndexToString(pieces.CoordinateArray[i])} but it is instead {boardState[pieces.CoordinateArray[i]]}");
                        }
                    }
                }
            }

            return allLocationsCorrect;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public readonly struct Piece
    {
        // Using a nibble to represent a piece

        const int typeMask = 0b111; // 0111
        const int colourMask = 0b1000; // 1000
        const int draggablePieceMask = 0b100; // 0100
        const int verticalDraggingMask = 0b101; // 101
        const int diagonalDraggingMask = 0b110; // 110

        public const int white = 0b0;
        public const int black = 0b1000; // 1000, 8
        public const int empty = 0b0; //  000, 0
        public const int pawn = 0b1; //   001, 1
        public const int king = 0b10; //   010, 2
        public const int knight = 0b011; // 011, 3

        // not using 4 (0b100) as it caused complications for being able to quickly check if a piece can drag vertically or diagonally, so then switched to using the queen as 0b111, which allowed for checking it way easier.
        public const int rook = 0b101; //   101, 5
        public const int bishop = 0b110; // 110, 6
        public const int queen = 0b111; //  111, 7

        public static int GetPieceType(int piece)
        {
            return piece & typeMask; // returns the type of the piece
        }

        public static int GetPieceValue(int piece) // gets the relative value of each piece type.
        {
            // This is explained in the start of my analysis, going through the relative 'value' of each piece.
            // 1 = pawn
            // 3 = Bishop or Knight
            // 5 = Rook
            // 9 = Queen
            // Hence, the queen is the most valuable piece and the pawn is the least valuable.

            switch (GetPieceType(piece))
            {
                case Piece.empty:
                    return 0;
                case Piece.king:
                    return 0;
                case Piece.pawn:
                    return 1;
                case Piece.rook:
                    return 5;
                case Piece.queen:
                    return 9;
                default:
                    return 3; // bishop or knight
            }
        }

        public static int IndexToColour(int index)
        {
            // this function is used to return the colour value of the piece's colour index.
            // For example, white - 0 and black - 1 in determining who's move it is to play etc, however the piece struct's value for black is 8 (0b1000) since the colour mask is 8. 
            // As a result, when trying to make a new piece (e.g. black queen), I need to convert the colour index (0 or 1) into the piece's colour value (0 or 8).
            return (index == Piece.white) ? Piece.white : Piece.black;
        }

        public static bool IsSameColour(int piece1, int piece2) // checks if two pieces are of the same colour
        {
            if (GetPieceType(piece1) == Piece.empty || GetPieceType(piece2) == Piece.empty) // since white's colour value is 0, an empty square has the same colour value as a white piece. As a result, if a square is empty it is rejected
            {
                return false;
            }
            return ((piece1 & colourMask) == (piece2 & colourMask));
        }

        public static int GetPieceColour(int piece)
        {
            // Converts a piece's colour value to its colour index (0 or 1) which is used in the board's tracking of who's turn it is to move.
            return (piece & colourMask) >> 3; // 0 - white, 1 - black
        }

        public static bool IsPieceWhite(int piece)
        {
            return (piece & colourMask) == white;
        }

        public static bool IsSlidingPiece(int piece)
        {
            return (piece & draggablePieceMask) == draggablePieceMask;
        }

        public static bool DoesVerticalDrag(int piece)
        {
            return (piece & verticalDraggingMask) == verticalDraggingMask;
        }

        public static bool DoesDiagonalDrag(int piece)
        {
            return (piece & diagonalDraggingMask) == diagonalDraggingMask;
        }

    }
}

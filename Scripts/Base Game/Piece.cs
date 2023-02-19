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
        public const int rook = 0b101; //   101, 5
        public const int bishop = 0b110; // 110, 6
        public const int queen = 0b111; //  111, 7

        public static int GetPieceType(int piece)
        {
            return piece & typeMask;
        }

        public static bool IsSameColour(int piece1, int piece2)
        {
            if (GetPieceType(piece1) == Piece.empty || GetPieceType(piece2) == Piece.empty)
            {
                return false;
            }
            return ((piece1 & colourMask) == (piece2 & colourMask));
        }

        public static int GetPieceColour(int piece)
        {
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

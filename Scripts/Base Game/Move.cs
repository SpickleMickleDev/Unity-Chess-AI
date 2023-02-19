using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public readonly struct Move
    {
        // Referencing : https://www.chessprogramming.org/Move_Generation

        //2^6 represents up to 64 squares
        private const ushort initialSquareMask = 0b111111000000;
        private const ushort targetSquareMask = 0b111111;

        // special move 'code'
        public const ushort nothing = 0;
        public const ushort castling = 1;
        public const ushort promotion = 2;
        public const ushort doublePawnMove = 3;
        public const ushort enPassant = 4;
        // 0 - nothing
        // 1 - castling
        // 2 - promoting
        // 3 - double pawn move
        // 4 - en passant
        // 5 options needed = 2^3, allowing for 3 spare possible special moves.
        // Could implement promotion to a piece other than a queen, however that would require additional GUI creation and handling for a relatively small feature, featuring a 'COULD' on my project objectives that I may implement last.
        
        // Only needed to show what part of the mask ushort value represents the special move value, however mask not necessary for implementation as the value of the special move can simply be retrieved by right shifting the move value. 
        //const ushort specialMoveMask = 0b111000000000000;


        readonly ushort move;

        public Move(Coord initialCoords, Coord targetCoords)
        {
            int initialGridNum = initialCoords.CoordAsGridNum();
            int targetGridNum = initialCoords.CoordAsGridNum();
            move = (ushort)((initialGridNum << 6) | targetGridNum);
        }

        public Move (int initialCoords, int targetCoords)
        {
            move = (ushort)((initialCoords << 6) | targetCoords);
        }

        public Move (int initialCoords, int targetCoords, int specialMoveCode)
        {
            move = (ushort)((((specialMoveCode << 6) | initialCoords) << 6) | targetCoords);
        }

        // need a way of having a move be null to show it is invalid
        public Move (bool isValid)
        {
            this.move = 0;
        }
        public static Move nullMove { get { return new Move(false); } }

        public bool isValid { get { return move != 0; } }

        public int initialSquare { get { return (move & initialSquareMask) >> 6; } }

        public int targetSquare { get { return move & targetSquareMask; } }

        public int specialMoveValue { get { return move >> 12; } } // did use return (move & specialMoveMask) >> 12; but due to overflow, mask AND operation unnecessary

        public bool isSpecialMove { get { return specialMoveValue > 0; } } // if special move value isn't nothing then return true.

        // Needs to be low level as it will be called frequently
        // initial square
        // final square

        // https://www.chessprogramming.org/Algebraic_Chess_Notation
        // Implementation of standard algebraic notation

    }
}

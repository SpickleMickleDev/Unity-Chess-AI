using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public readonly struct Move
    {
        // found this https://www.chessprogramming.org/Encoding_Moves
        // however didn't actually use or reference it for any of my move struct, but it is cool to know that I discovered the same implementation that is commonly used.
        // the link would probably provide further explanation if necessary, though.

        // Needs to be low level as it will be called frequently
        // initial square
        // final square

        //2^6 represents up to 64 squares
        private const ushort initialSquareMask = 0b111111000000; // 6 bits per square = 12 bits for square start and target
        private const ushort targetSquareMask = 0b111111;

        // special move 'code'
        // 3 bits for special move code
        public const ushort nothing = 0;
        public const ushort castling = 1;
        public const ushort promotion = 2;
        public const ushort doublePawnMove = 3;
        public const ushort enPassant = 4;

        // With a total of 15 bits, leaving 1 spare. Allows the whole move to be stored as a single ushort, making storing opening move sequences as moves easier.

        // 5 options needed = 2^3, allowing for 3 spare possible special moves.
        // Could implement promotion to a piece other than a queen, however that would require additional GUI creation and handling for a relatively small feature, featuring a 'COULD' on my project objectives that I may implement last.
        
        // Only needed to show what part of the mask ushort value represents the special move value, however mask not necessary for implementation as the value of the special move can simply be retrieved by right shifting the move value. 
        //const ushort specialMoveMask = 0b111000000000000;


        readonly ushort move;

        public Move (int initialCoords, int targetCoords)
        {
            move = (ushort)((initialCoords << 6) | targetCoords); // stores the initial square and target square as one 12 bit long number, with the initial square being the first 6 bits and the target square being the last 6 bits.
        }

        public Move (int initialCoords, int targetCoords, int specialMoveCode) // if the move has a special aspect i.e. castling, promotion etc, stores this value in 3 bits in the move value, 3 bits before the square values
        {
            move = (ushort)((((specialMoveCode << 6) | initialCoords) << 6) | targetCoords); // Move value (3 bits) InitialSquare (6 bits) TargetSquare (6 bits) = 15 bits used of the 16 bit ushort.
        }

        // need a way of having a move be null to show it is an invalid move
        public Move (bool isValid)
        {
            this.move = 0;
        }
        public static Move nullMove { get { return new Move(false); } } // creates a null move, to represent a default move value like setting a string to null, so that the move can be referenced even if it isn't allocated a value.

        public bool isValid { get { return move != 0; } } // returns whether or not this move is valid.

        public int initialSquare { get { return (move & initialSquareMask) >> 6; } } // gets the intial square of the move

        public int targetSquare { get { return move & targetSquareMask; } } // gets the target square of the move

        public int specialMoveValue { get { return move >> 12; } } // did use return (move & specialMoveMask) >> 12; but due to the bits representing the squares being underflowed, applying a mask is unnecessary.

        public bool isSpecialMove { get { return specialMoveValue > 0; } } // if special move value isn't nothing then return true.

    }
}

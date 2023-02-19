using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class MovementLookup
    {
        //  +7 +8 +9
        //  -1  0  1
        //  -9 -8 -7


        public static readonly int[] diagonalDirections = {-9, -7, 7, 9}; // SW SE NW NE
        public static readonly int[] straightDirections = {-8, -1, 1, 8}; // S W E N
        public static readonly int[] totalDirections = { -9, -7, 7, 9, -8, -1, 1, 8 }; // diagonal then straight

        public readonly static int[][] diagonalDistanceToEdge;
        public readonly static int[][] straightDistanceToEdge;

        public readonly static ulong[][] pawnPassiveMoves; // 0 - white, 1 - black
        public readonly static ulong[][] pawnAttackMoves; // 0 - white, 1 - black
        public readonly static int[][][] pawnAttackOffsets; // 0 - white, 1 - black
        public readonly static ulong[] rookMoves;
        public readonly static ulong[] knightMoves;
        public readonly static ulong[] bishopMoves;
        public readonly static ulong[] queenMoves;
        public readonly static ulong[] kingMoves;

        public readonly static int[][] knightMovesIndexes;


        // was going to use array of 8 bytes or 64 bits but ulong is more suitable with being 64 bits long

        static MovementLookup()
        {
            diagonalDistanceToEdge = new int[64][];
            straightDistanceToEdge = new int[64][];
            pawnPassiveMoves = new ulong[64][]; 
            pawnAttackMoves = new ulong[64][];
            pawnAttackOffsets = new int[64][][];
            rookMoves = new ulong[64];
            knightMoves = new ulong[64];
            bishopMoves = new ulong[64];
            queenMoves = new ulong[64];
            kingMoves = new ulong[64];
            knightMovesIndexes = new int[64][];

            int[] knightJumpDisplacements = new int[8] {-17, -10, 6, 15, -6, -15, 10, 17}; // SW NW SE NE
            int[] diagonalDstToEdge = new int[4];
            int[] straightDstToEdge = new int[4];
            ulong[] pawnMovesPassive = new ulong[2] { 0, 0 };
            ulong[] pawnMovesAttack = new ulong[2] { 0, 0 }; // can never attack first 2 rows so could save space? For convenience of mapping it over the board and since it's precomputed data, still storing it as 64 bits.
            ulong rookMovesOnSquare = 0;
            ulong knightMovesOnSquare = 0;
            ulong bishopMovesOnSquare = 0;
            //ulong queenMoves = 0; no need as just using bishop and rook moves
            ulong kingMovesOnSquare = 0;

            for (int index = 0; index < 64; index++)
            {
                pawnMovesPassive[0] = 0; pawnMovesPassive[1] = 0;
                pawnMovesAttack[0] = 0; pawnMovesAttack[1] = 0;
                rookMovesOnSquare = 0;
                knightMovesOnSquare = 0;
                bishopMovesOnSquare = 0;
                kingMovesOnSquare = 0;


                // https://www.chessprogramming.org/Knight_Pattern#by_Lookup
                // https://www.chessprogramming.org/Table-driven_Move_Generation
                // Using this technique for faster lookup of moves and to be able to identify pins on pieces as calculating each piece's sliding move by stopping the ray when colliding with another piece creates issues for identifying pins or squares to block check.

                // bitboard lookup implementation
                // | OR
                // << LSHIFT
                // & AND


                // maximum number of squares each piece can move to from wherever they are on the board

                int x = index % 8;
                int y = index / 8;

                diagonalDstToEdge[0] = Math.Min(x, y); // SW
                diagonalDstToEdge[1] = Math.Min(7 - x, y); // SE
                diagonalDstToEdge[2] = Math.Min(x, 7 - y); // NW
                diagonalDstToEdge[3] = Math.Min(7 - x, 7 - y); //  NE
                straightDstToEdge[0] = y; // south
                straightDstToEdge[1] = x; // west
                straightDstToEdge[2] = 7 - x; // east
                straightDstToEdge[3] = 7 - y; // north

                diagonalDistanceToEdge[index] = (int[])diagonalDstToEdge.Clone();
                straightDistanceToEdge[index] = (int[])straightDstToEdge.Clone();

                // handling pawn moves may work separately in the actual move handling but computing it here instead for convenience of ANDing the pawn move mask with a check mask etc.
                if (index + 8 < 64)
                {
                    pawnMovesPassive[0] |= (1ul << (index + 8));
                }
                if (y == 1)
                {
                    pawnMovesPassive[0] |= (1ul << (index + 16)); // can move two if hasn't moved yet
                }
                if (index - 8 >= 0)
                {
                    pawnMovesPassive[1] |= (1ul << (index - 8));
                }
                if (y == 6)
                {
                    pawnMovesPassive[1] |= (1ul << (index - 16));
                }

                // 5 = 101 shifted to the diagonal includes both attacks
                // pawnMovesAttack[0] |= (5ul << (index + 7));
                // removed as it doesn't account for wrapping around the board

                pawnPassiveMoves[index] = (ulong[])pawnMovesPassive.Clone();


                // pawn attack moves

                List<int> pawnAttacksWhite = new List<int>();
                List<int> pawnAttacksBlack = new List<int>();
                if (y < 7) // checks that white pawn isn't on the last row, pawn can't be on the last row as it would promote but avoids overflowing the ulong by attempting to bitshift to beyond 64 bits.
                {
                    if (x > 0)
                    {
                        pawnMovesAttack[0] |= (1ul << (index + 7));
                        pawnAttacksWhite.Add(index + 7);
                    }
                    if (x < 7)
                    {
                        pawnMovesAttack[0] |= (1ul << (index + 9));
                        pawnAttacksWhite.Add(index + 9);
                    }
                }
                if (y > 0) // checks that black pawn isn't on the bottom row to avoid attempting to LSHIFT to a negative number off the board.
                {
                    if (x > 0) // avoids overlapping to the right
                    {
                        pawnMovesAttack[1] |= (1ul << (index - 9));
                        pawnAttacksBlack.Add(index - 9);
                    }
                    if (x < 7) // avoids overlapping to the left
                    {
                        pawnMovesAttack[1] |= (1ul << (index - 7));
                        pawnAttacksBlack.Add(index - 7);
                    }
                }

                pawnAttackMoves[index] = (ulong[])pawnMovesAttack.Clone();
                pawnAttackOffsets[index] = new int[2][] {pawnAttacksWhite.ToArray(), pawnAttacksBlack.ToArray()};

                // looping through straight ray directions to each edge and adding squares to rook moves bitmap
                for (int dirIndex = 0; dirIndex < straightDirections.Length; dirIndex++)
                {
                    int directionOffset = straightDirections[dirIndex];
                    int distanceToEdge = straightDistanceToEdge[index][dirIndex];
                    
                    for (int n = 0; n < distanceToEdge; n++)
                    {
                        if (n < 1)
                        {
                            kingMovesOnSquare |= (1ul << index + (directionOffset));
                        }
                        rookMovesOnSquare |= (1ul << index + (directionOffset * (n+1)));
                    }
                }

                for (int dirIndex = 0; dirIndex < diagonalDirections.Length; dirIndex++)
                {
                    int directionOffset = diagonalDirections[dirIndex];
                    int distanceToEdge = diagonalDistanceToEdge[index][dirIndex];
                    
                    for (int n = 0; n < distanceToEdge; n++)
                    {
                        if (n < 1)
                        {
                            kingMovesOnSquare |= (1ul << index + (directionOffset));
                        }
                        bishopMovesOnSquare |= (1ul << (index + (directionOffset * (n+1))));// would normally goo from n=1 to n<= distanceToEdge but distancetoedge can be 0 so it would make the code logically unsound.
                    }
                }

                rookMoves[index] = rookMovesOnSquare;
                bishopMoves[index] = bishopMovesOnSquare;

                queenMoves[index] = bishopMovesOnSquare | rookMovesOnSquare;

                List<int> knightMovesList = new List<int>();
                for (int dirIndex = 0; dirIndex < knightJumpDisplacements.Length; dirIndex++)
                {
                    int directionOffset = knightJumpDisplacements[dirIndex];
                    int knightSquare = index + directionOffset;

                    // SW NW SE NE
                    
                    // Check that square index + south displacements are greater than 0
                    // check that square index + north displacements are less than 64

                    // check that index + west displacements mod 8 are less than square index mod 8
                    // check that index + east displacements mod 8 are more than square index mod 8

                    if (knightSquare >= 0 && knightSquare < 64)
                    {
                        if (dirIndex < 4) // 0, 1, 2, 3 - all move west
                        {
                            if (knightSquare % 8 < index % 8)
                            {
                                knightMovesList.Add(knightSquare);
                                knightMovesOnSquare |= (1ul << knightSquare);
                            }
                        }
                        else // east
                        {
                            if (knightSquare % 8 > index % 8)
                            {
                                knightMovesList.Add(knightSquare);
                                knightMovesOnSquare |= (1ul << knightSquare);
                            }
                        }
                    }
                }

                knightMoves[index] = knightMovesOnSquare;
                knightMovesIndexes[index] = knightMovesList.ToArray();

                // king moves -9, -8, -7, -1, +1, +7, +8, +9
                // same as direction offsets
                
                /* OLD CODE FOR KING, NOW KING MOVEMENTS ARE MADE IN BISHOP AND ROOK CALCULATIONS TO A LIMIT OF 1 IN EACH DIRECTION
                for (int dirIndex = 0; dirIndex < diagonalDirections.Length; dirIndex++)
                {
                    int targetSquare = index + dirIndex;
                    if (targetSquare >= 0 && targetSquare < 64)
                    {
                        int newX = targetSquare % 8;
                        int newY = targetSquare / 8;
                        // ensure king hasn't wrapped around board by checking that new X axis and new Y axis haven't moved by more than 1 each.
                        if (Math.Max(Math.Abs(x - newX), Math.Abs(y - newY)) <= 1)
                        {
                            kingMovesOnSquare |= (1ul << targetSquare);
                        }
                    }
                }
                for (int dirIndex = 0; dirIndex < straightDirections.Length; dirIndex++)
                {
                    int targetSquare = index + dirIndex;
                    if (targetSquare >= 0 && targetSquare < 64)
                    {
                        int newX = targetSquare % 8;
                        int newY = targetSquare / 8;
                        // ensure king hasn't wrapped around board by checking that new X axis and new Y axis haven't moved by more than 1 each.
                        if (Math.Max(Math.Abs(x - newX), Math.Abs(y - newY)) == 1)
                        {
                            kingMovesOnSquare |= (1ul << targetSquare);
                        }
                    }
                }*/


                kingMoves[index] = kingMovesOnSquare;

            }
        }







    }
}

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

        // The movement lookup allows me to calculate where each piece can move from each square before the game.
        // Pre-calculating the moves is a very efficient method of speeding up move generation, due to the lack of computation needed when generating the moves in realtime.
        // The idea for this came from the chess programming wiki here https://www.chessprogramming.org/Table-driven_Move_Generation and here https://www.chessprogramming.org/Knight_Pattern#by_Lookup
        // However, they often referenced the idea of linked lists, presumably for dragging pieces, however I'm not fully generating my moves through movement lookups and instead creating this for simply assisting in making my move generation quicker.
        // The actual implementation was more inspired by Sebastian Lague's Chess AI's PrecalculatedMoveData, to which I liked the format of using ulong values to store a bitmap of the squares that each piece targets from each square.
        // I implemented lookups for each piece, however didn't end up using queens, bishops or rooks' movement lookups often, since they need to check in a row in case they collide with something anyway.
        // The ability to lookup king moves, knight moves and pawns' attacks proved very helpful, however.
        // When creating my move generator, I also added the ability to lookup the knight moves and pawn attacks as an array of integers containing the target squares' indexes, which I found helped massively.

        // When making the AI's evaluation, I also needed some way of measuring the position of each piece on the board, and therefore opted for a matrix of position values for each piece.
        // I stored this in the movement lookup, as you will find.


        public static readonly int[] diagonalDirections = {-9, -7, 7, 9}; // SW SE NW NE
        public static readonly int[] straightDirections = {-8, -1, 1, 8}; // S W E N
        public static readonly int[] totalDirections = { -9, -7, 7, 9, -8, -1, 1, 8 }; // diagonal directions then straight directions

        public readonly static int[][] diagonalDistanceToEdge; // gets the distance to the edge in each diagonal direction from each square
        public readonly static int[][] straightDistanceToEdge;

        public readonly static ulong[][] pawnPassiveMoves; // 0 - white, 1 - black
        public readonly static ulong[][] pawnAttackMoves; // 0 - white, 1 - black
        public readonly static int[][][] pawnAttackOffsets; // 0 - white, 1 - black
        public readonly static ulong[] rookMoves;
        public readonly static ulong[] knightMoves;
        public readonly static ulong[] bishopMoves;
        public readonly static ulong[] queenMoves;
        public readonly static ulong[] kingMoves;

        public readonly static int[][] knightMovesIndexes; // gets the actual indexes of the squares that the knight can move to, for ease of movement lookup instead of having to enumerate through the bitmap each time.

        public readonly static int[] pawnPositionValues; // matrices of the position values for each piece, for lookup in the evaluation function.
        public readonly static int[] pawnEndGamePositionValues;
        public readonly static int[] rookPositionValues;
        public readonly static int[] knightPositionValues;
        public readonly static int[] bishopPositionValues;
        public readonly static int[] queenPositionValues;
        public readonly static int[] kingPositionValues;





        // was going to use array of 8 bytes or 64 bits but ulong is more suitable with being 64 bits long

        static MovementLookup()
        {
            // inputting values from black's perspective, since the grid will actually be read from top to bottom but represents white's best positions.
            pawnPositionValues = new int[64]
            {
                0, 0, 0, 0, 0, 0, 0, 0, // pawn can't be on first row
                1, 1, -1, -2, -2, 1, 1, 1,
                2, -1, 1, 1, 1, 1, -1, 2,
                1, 1, 1, 4, 4, 1, 1, 1,
                2, 2, 2, 3, 3, 2, 2, 2,
                3, 3, 3, 4, 4, 3, 3, 3,
                5, 5, 5, 5, 5, 5, 5, 5,
                0, 0, 0, 0, 0, 0, 0, 0, // pawn can't be on last row either as would promote to queen so no point storing
            };
            pawnEndGamePositionValues = new int[64]
            {
                0, 0, 0, 0, 0, 0, 0, 0, // pawn can't be on first row
                0, 0, 0, 0, 0, 0, 0, 0,
                1, 1, 1, 1, 1, 1, 1, 1,
                2, 2, 2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3, 3, 3,
                5, 5, 5, 5, 5, 5, 5, 5,
                7, 7, 7, 7, 7, 7, 7, 7,
                0, 0, 0, 0, 0, 0, 0, 0, // pawn can't be on last row either as would promote to queen so no point storing
            };
            rookPositionValues = new int[64]
            {
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                4, 4, 4, 4, 4, 4, 4, 4,
                0, 0, 0, 0, 0, 0, 0, 0, // 'pigs on the seventh' when rooks get on the 7th rank, cutting off the king. Open files should also be prioritied, however can't really add that here so could after in the main evaluation.
            };
            knightPositionValues = new int[64]
            {
                -5, -4, -3, -2, -2, -3, -4, -5, // much better in the middle as they can reach more pieces. In corners they only attack 1/4 of the possible pieces
                -4, -3, -2, -1, -1, -2, -3, -4, // in further evaluation, I would also add the priority of 'outposts' (safe spots behind opposing pawns) and blocking pawns being pushed forward using the rook
                -3, -2, -1,  1,  1, -1, -2, -3,
                -2, -1,  1,  3,  3,  1, -1, -2,
                -2, -1,  1,  3,  3,  1, -1, -2,
                -3, -2, -1,  1,  1, -1, -2, -3,
                -4, -3, -2, -1, -1, -2, -3, -4,
                -5, -4, -3, -2, -2, -3, -4, -5,
            };
            bishopPositionValues = new int[64]
            {
                0, 0, 0, 0, 0, 0, 0, 0, // fiancetto where the bishop hides behind pushed friendly pawn is better, as well as developing the bishop at the start and defending it with pawns. Therefore paying
                2, 4, 0, 2, 2, 0, 4, 2, // close attention to pawn map when making this.
                2, 1, 2, 1, 1, 2, 1, 2,
                2, 3, 2, 1, 1, 2, 3, 2,
                2, 2, 1, 1, 1, 0, 2, 2,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0, 0, 0,
            };
            queenPositionValues = new int[64]
            {
                -3, -2, -1, -1, -1, -1, -2, -3, // better in the middle as it attacks more squares, like the knight. Just not as badly affected
                -2, -1, 1, 1, 1, 1, -1, -2,
                -1, 0, 1, 2, 2, 1, 0, -1,
                -1, 1, 2, 3, 3, 2, 1, -1,
                -1, 1, 2, 3, 3, 2, 1, -1,
                -1, 0, 1, 2, 2, 1, 0, -1,
                -2, -1, 1, 1, 1, 1, -1, -2,
                -3, -2, -1, -1, -1, -1, -2, -3,
            };
            kingPositionValues = new int[64]
            {
                2, 3, 1, 0, 0, 1, 3, 2,
                2, 1, 0, 0, 0, 0, 1, 2,
                -2, -2, -2, -2, -2, -2, -2, -2,
                -3, -2, -2, -2, -2, -2, -2, -3,
                -3, -3, -2, -2, -2, -2, -3, -3,
                -4, -4, -3, -3, -3, -3, -4, -4,
                -5, -4, -4, -4, -4, -4, -4, -5,
                -5, -5, -4, -4, -4, -4, -5, -5,
            };



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
            ulong rookMovesOnSquare;
            ulong knightMovesOnSquare;
            ulong bishopMovesOnSquare;
            ulong kingMovesOnSquare;
            //ulong queenMoves = 0; no need as just using bishop and rook moves together

            for (int index = 0; index < 64; index++)
            {
                pawnMovesPassive[0] = 0; pawnMovesPassive[1] = 0;
                pawnMovesAttack[0] = 0; pawnMovesAttack[1] = 0;
                rookMovesOnSquare = 0;
                knightMovesOnSquare = 0;
                bishopMovesOnSquare = 0;
                kingMovesOnSquare = 0;

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

                // 5 = 101 shifted to the diagonal includes both pawn attacks
                // pawnMovesAttack[0] |= (5ul << (index + 7));
                // removed as it doesn't account for one attack wrapping around the board

                pawnPassiveMoves[index] = (ulong[])pawnMovesPassive.Clone(); // stores the passive move bitmap for this square

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
                pawnAttackOffsets[index] = new int[2][] {pawnAttacksWhite.ToArray(), pawnAttacksBlack.ToArray()}; // adds the square indexes of the squares the pawn attacks

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
                            kingMovesOnSquare |= (1ul << index + (directionOffset)); // if only checking 1 in this direction, add to king's moves
                        }
                        bishopMovesOnSquare |= (1ul << (index + (directionOffset * (n+1))));// would normally go from n=1 to n<= distanceToEdge but distancetoedge can be 0 so it would make the code check beyond the edge.
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

                kingMoves[index] = kingMovesOnSquare; // king moves are calculated as 1 in each direction for the rook and bishops' moves

            }
        }
    }
}

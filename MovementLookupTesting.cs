using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class MovementLookupTesting
    {

        public string ulongToString(ulong input) // converts an ulong value into a readable string that I can analyse in the console
        {
            string str = string.Empty;
            for (int i = 0; i < 64; i++)
            {
                if (i % 8 == 0)
                {
                    str += '\n';
                }
                if ((input >> i & 1ul )== 1ul) // or could mod by 2 but less efficient so no need
                {
                    str += "1";
                }
                else
                {
                    str += "0";
                }
            }
            return str;
        }

        public void PerformTestInConsole()
        {
            // checking square offsets
            int[] arr = MovementLookup.pawnAttackOffsets[30][1]; // fully functional
            foreach (int i in arr)
            {
                Debug.Log(i);
            }
            // checking the ulong matrix of bishop and knight moves
            //Debug.Log(ulongToString(MovementLookup.bishopMoves[30]));
            //Debug.Log(ulongToString(MovementLookup.knightMoves[30]));

            // checking the diagonal distances to each edge from each square
            /*for (int i = 0; i < 64; i++)
            {
                Debug.Log(String.Join(" ", MovementLookup.diagonalDistanceToEdge[i]));
            }*/
        }

        public void PerformTestOnBoard(UserInterface BoardUI)
        {
            // list of matrices to test
            List<ulong> testingMatrices = new List<ulong>();

            for (int i = 0; i < 64; i++) // for each square on the board
            {
                // All fully functioning now

                //testingMatrices.Add(MovementLookup.kingMoves[i]);
                //testingMatrices.Add(MovementLookup.queenMoves[i]);
                
                //testingMatrices.Add(MovementLookup.pawnAttackMoves[i][0]);
                //testingMatrices.Add(MovementLookup.pawnPassiveMoves[i][0]);
                testingMatrices.Add(MovementLookup.pawnAttackMoves[i][1]); // will display the pawn attack moves bitmap for each square from black's pawns
                //testingMatrices.Add(MovementLookup.pawnPassiveMoves[i][1]);
                
                //testingMatrices.Add(MovementLookup.bishopMoves[i]);
                //testingMatrices.Add(MovementLookup.knightMoves[i]);
                //testingMatrices.Add(MovementLookup.rookMoves[i]);
            }

            BoardUI.HighlightSquaresForMovementLookupTesting(testingMatrices); // tells the board UI (which handles displaying all of the squares and pieces) to display the matrix on the board as to which squares it thinks it can move to.
        }
    }
}

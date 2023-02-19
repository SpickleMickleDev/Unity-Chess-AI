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

        public string ulongToString(ulong input)
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
            int[] arr = MovementLookup.pawnAttackOffsets[30][1]; // fully functional
            foreach (int i in arr)
            {
                Debug.Log(i);
            }

            //Debug.Log(ulongToString(MovementLookup.bishopMoves[30]));
            //Debug.Log(ulongToString(MovementLookup.knightMoves[30]));

            /*for (int i = 0; i < 64; i++)
            {
                Debug.Log(String.Join(" ", MovementLookup.diagonalDistanceToEdge[i]));
            }*/
        }

        public void PerformTestOnBoard(UserInterface BoardUI)
        {
            List<ulong> testingMatrices = new List<ulong>();

            for (int i = 0; i < 64; i++)
            {
                // All fully functioning now

                //testingMatrices.Add(MovementLookup.kingMoves[i]);
                //testingMatrices.Add(MovementLookup.queenMoves[i]);
                
                //testingMatrices.Add(MovementLookup.pawnAttackMoves[i][0]);
                //testingMatrices.Add(MovementLookup.pawnPassiveMoves[i][0]);
                testingMatrices.Add(MovementLookup.pawnAttackMoves[i][1]);
                //testingMatrices.Add(MovementLookup.pawnPassiveMoves[i][1]);
                
                //testingMatrices.Add(MovementLookup.bishopMoves[i]);
                //testingMatrices.Add(MovementLookup.knightMoves[i]);
                //testingMatrices.Add(MovementLookup.rookMoves[i]);

            }
            
            BoardUI.HighlightSquaresForMovementLookupTesting(testingMatrices);
            
        }
    }
}

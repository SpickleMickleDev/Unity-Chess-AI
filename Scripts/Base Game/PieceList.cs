using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public class PieceList
    {
        // PIECE-CENTRIC BITBOARD IMPLEMENTATION

        // keep a counter of how many chess pieces there are in the list
        // keep an array of coordinates to where the pieces are
        // keep a 2D array of the chess board simulating where the pieces are
        // When moving a piece, remove it and replace the coord in the piece array with the new coord
        // Then replace the index on the grid and add the new index to the grid

        // When simply removing a piece, remove the coord from the array and index from the grid slot and move the farthest coordinate in the array to the open slot and decrement the counter

        public int[] CoordinateArray;
        private int[] grid;
        private int Counter;

        public PieceList(int maxCapacity = 16)
        {
            CoordinateArray = new int[maxCapacity];
            grid = new int[64];
            Counter = 0;
        }

        public int Count
        {
            get
            {
                return Counter;
            }
        }

        public void AddPieceToList(int squareIndex)
        {
            if (!CoordinateArray.Contains(squareIndex))
            {
                CoordinateArray[Counter] = squareIndex;
            }
            grid[squareIndex] = Counter;
            Counter++;
        }

        public void RemovePieceFromList(int squareIndex)
        {
            if (Counter > 0)
            {
                int index = grid[squareIndex];
                CoordinateArray[index] = CoordinateArray[Counter - 1];
                grid[CoordinateArray[index]] = index;
                Counter--;
            }
        }

        public void MovePiece(int pieceSquareIndex, int targetSquareIndex)
        {
            int index = grid[pieceSquareIndex];
            CoordinateArray[index] = targetSquareIndex;
            grid[targetSquareIndex] = index;
        }
    }
}

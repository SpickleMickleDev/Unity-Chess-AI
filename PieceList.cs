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
        // Reference : https://www.chessprogramming.org/Piece-Lists, took me quite a bit to get my head around it

        // keep a counter of how many chess pieces there are in the list
        // keep an array of coordinates to where the pieces are
        // keep a 2D array of the chess board simulating where the pieces are
        // When moving a piece, remove it and replace the coord in the piece array with the new coord
        // Then replace the index on the grid and add the new index to the grid

        // When simply removing a piece, remove the coord from the array and index from the grid slot and move the farthest coordinate in the array to the open slot and decrement the counter

        // The purpose of the piece lists : 
        // To keep an array of all of the squares that a certain type of piece is (e.g. white rooks), allowing for quickly looking up the locations of the white rooks when generating moves for white to play, then being able to calculate their moves from that square.
        // In hindsight, would implement as a struct, however at this point there isn't really a need to change it.

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
            CoordinateArray[Counter] = squareIndex;
            grid[squareIndex] = Counter;
            Counter++;
        }

        public void RemovePieceFromList(int squareIndex)
        {
            if (Counter > 0)
            {
                int index = grid[squareIndex];
                // empty slot now points to the square at the end of the array
                CoordinateArray[index] = CoordinateArray[Counter - 1];
                // square that is now focused on is then redirected to its new index in the array
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

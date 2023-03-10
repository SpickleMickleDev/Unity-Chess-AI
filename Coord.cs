using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    public struct Coord
    {
        public readonly int column;
        public readonly int row;

        public Coord(int column, int row)
        {
            this.column = column;
            this.row = row;
        }

        public Coord(string squareName)
        {
            this.column = "abcdefgh".IndexOf(squareName[0]);
            this.row = Convert.ToInt32(squareName[1]);
        }

        public Coord(int squareNum)
        {
            this.column = squareNum % 8;
            this.row = squareNum / 8;
        }

        public static int StringToIndex(string squareName)
        {
            if (squareName == "-")
            {
                return -1;
            }
            return "abcdefgh".IndexOf(Char.ToLower(squareName[0])) + ( 8 * (Convert.ToInt32(squareName[1].ToString()) - 1));
        }

        public static string IndexToString(int index) // translates a square index from 0 - 63 into readable user notation of the board square e.g. e4
        {
            return $"{"abcdefgh"[index % 8]}{(index / 8) + 1}";
        }

        public bool Equals(Coord comparison) // compares coords
        {
            return this.column == comparison.column && this.row == comparison.row ;
        }

        public int CoordAsGridNum() // translates the coordinate into a square index from 0 - 63
        {
            return this.column + (8 * this.row);
        }

        public string CoordAsString()
        {
            string str = "abcdefgh"[this.column].ToString() + Convert.ToString(this.row + 1);
            return str;
        }

        public Vector2 CoordToWorldCoordinates() // converts coordinate into its corresponding coordinate within the game's 2D space.
        {
            return new Vector2(this.column - 3.5f, this.row - 3.5f);
        }
        
        public bool IsWhite()
        {
            // 0 0 is black
            // 10101010
            // 01010101
            // 10101010
            // 01010101
            // If both even or both odd then black
            // Therefore if col + row == even then black 

            return (this.column + this.row) % 2 != 0;

        }
    }
}

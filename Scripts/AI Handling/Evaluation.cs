using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public class Evaluation
    {
        Board board;

        public float Evaluate(Board board)
        {
            this.board = board;
            return board.CalculateMaterialAdvantage();
        }



    }
}

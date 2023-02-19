using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    abstract class Player
    {
        public delegate void MoveMade(Move move);
        public abstract event MoveMade MoveSelected;

        public abstract void Update();

        public abstract void NotifyToPlay();

    }
}

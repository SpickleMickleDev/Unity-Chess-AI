using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    public abstract class Player
    {
        // As explained in the AI player, game manager can't reference / call functions contained within the AI player (even when public) without it being an abstract function contained within the player class.
        // Planning to figure out the way to solve this.

        // AI player handling
        public int depth;
        public abstract void ChangeDepth(int depthVal); // procedure to change the depth of the AI's search
        public abstract void NotifyToPlay(); // Notifies the AI that it is their turn to play, so they should begin processing.
        public delegate void UpdateEvaluation(float eval);
        public abstract event UpdateEvaluation UpdateEvalCall; // when a search is finished, the AI calls this event to trigger the game manager to change the evaluation on the display

        // human player handling
        public abstract void Update(); // On every frame update, if it is the human player's turn then it will call the human player to handle the mouse movement this frame.
        
        // both players
        public delegate void MoveMade(Move move);
        public abstract event MoveMade MoveSelected; // when a move is ready to be made by either player, it calls this event to then trigger the game manager to handle the move's execution


    }
}

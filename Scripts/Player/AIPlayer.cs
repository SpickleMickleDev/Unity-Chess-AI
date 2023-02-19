using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class AIPlayer : Player
    {
        public float currentEval;
        Board board;
        Evaluation evaluation;
        Search search;
        bool moveChosen;
        public AIPlayer(Board board)
        {
            this.board = board;
            this.evaluation = new Evaluation();
            this.currentEval = 0;
            this.search = new Search(board);
        }

        public override event MoveMade MoveSelected;

        public override void Update()
        {
            // For checking when the move is done and then choosing said move.
            // No need to allocate as Event Oriented, simply takes up more storage and processing unnecessarily.
            


        }

        public override void NotifyToPlay()
        {
            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(board);
            List<Move> moves = moveGenerator.GenerateMoves();

            moveChosen = false;
            //Debug.Log($"Computer has {moves.Count} moves");

            System.Random random = new System.Random();
            if (moves.Count > 0)
            {
                Move moveSelected = moves[random.Next(0, moves.Count - 1)];
                NotifyMoveIsChosen(moveSelected);
            }
            else
            {
                NotifyMoveIsChosen(Move.nullMove);
            }
        }

        private void NotifyMoveIsChosen(Move move)
        {
            if (MoveSelected != null)
            {
                MoveSelected(move);
            }
        }

    }
}

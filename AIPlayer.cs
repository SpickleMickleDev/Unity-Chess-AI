using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace ChessAI
{
    public class AIPlayer : Player
    {
        float currentEval;
        Board board;
        Search search;
        //bool moveChosen;
        //Thread processingThread;
        public new int depth;
        public AIPlayer(Board board, int depth)
        {
            this.depth = depth;
            this.board = board;
            this.currentEval = 0;
            this.search = new Search(board, depth);
            this.search.AlertMove += NotifyMoveIsChosen;
        }

        public override event MoveMade MoveSelected; // when a move is selected by the search, it triggers an event to call the NotifyMoveIsChosen procedure.
        public override event UpdateEvaluation UpdateEvalCall;
        public override void Update() { } // isn't used, however derives from the Player class and the human player needs an update function for calculating mouse handling. Don't know how to not need this, since it won't let me call the Update function within the human player unless it is also present within the Player class as an abstract function. I do want to know how to do this to a professional standard.

        private Move SelectMoveAsPrimitiveChecker() // Would likely consider a low elo rating of ~300-400
        {
            // This is a temporary AI that I used to try out the move generation before I had implemented an AI to see into the future. Still able to defeat some people at Chess, however.

            // Temporary computer which evaluates some threats, however can't see into the future.
            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(board);
            List<Move> moves = moveGenerator.GenerateMoves();

            currentEval = board.CalculateMaterialAdvantage();

            ulong opponentPawnAttackMap = moveGenerator.opponentPawnAttackMap;
            ulong opponentAttackMap = moveGenerator.opponentAttackMap;

            bool underAttackByPawn = false;
            List<Move> evadingMoves = new List<Move>();
            List<Move> movesWithPawnAttackMap = new List<Move>();
            System.Random random = new System.Random();


            List<Move> captureMoves = new List<Move>();
            for (int i = 0; i < moves.Count; i++)
            {
                if (((opponentPawnAttackMap >> moves[i].targetSquare) & 1ul) != 1) // if move doesn't go infront of pawn attack square
                {
                    movesWithPawnAttackMap.Add(moves[i]);
                }

                if (board.boardState[moves[i].targetSquare] != Piece.empty || (moves[i].isSpecialMove && moves[i].specialMoveValue != Move.doublePawnMove))
                {
                    // add capture to list unless it means taking a not very valuable piece on an opponent pawn attack
                    int materialGain = Piece.GetPieceValue(board.boardState[moves[i].targetSquare]) - Piece.GetPieceValue(board.boardState[moves[i].initialSquare]);

                    if (materialGain >= 0 || moves[i].isSpecialMove || ((opponentAttackMap >> moves[i].targetSquare) & 1ul) == 0) // piece is undefended
                    {
                        captureMoves.Add(moves[i]);
                    }
                }
                if (((opponentPawnAttackMap >> moves[i].initialSquare) & 1ul) != 0 && Piece.GetPieceType(board.boardState[moves[i].initialSquare]) != Piece.pawn)
                {
                    underAttackByPawn = true;
                    evadingMoves.Add(moves[i]);
                    // piece is under attack
                }
            }
            if (captureMoves.Count == 0)
            {
                captureMoves = movesWithPawnAttackMap;
            }

            if (underAttackByPawn)
            {
                if (evadingMoves.Count > 0)
                {
                    captureMoves = evadingMoves;
                }
            }

            return captureMoves[random.Next(0, captureMoves.Count - 1)];
        }

        private void SelectMoveUsingAI()
        {
            search.StartSearch(); // starts a search
            currentEval = -search.producedEvaluation; // when search result is produced, sets the evaluation (from white's perspective) as the evaluation from the search.
        }

        public override void ChangeDepth(int depthVal)
        {
            this.depth = depthVal;
        }

        public override void NotifyToPlay()
        {
            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(board);
            List<Move> moves = moveGenerator.GenerateMoves();

            if (moves.Count > 0)
            {
                // If wishing to use the primitive AI that doesn't see into the future
                //moveSelected = SelectMoveAsPrimitiveChecker();
                //NotifyMoveIsChosen(moveSelected);
                
                search.searchDepth = depth;
                SelectMoveUsingAI();
            }
            else
            {
                NotifyMoveIsChosen(Move.nullMove); // if no moves, send in a null move and then the game manager will recognise that there are no legal moves and end the game.
            }
        }

        private void NotifyMoveIsChosen(Move move)
        {
            if (MoveSelected != null)
            {
                if (UpdateEvalCall != null)
                {
                    UpdateEvalCall(currentEval);
                }
                MoveSelected(move);
            }
        }
    }
}

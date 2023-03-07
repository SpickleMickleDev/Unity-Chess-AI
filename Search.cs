using System.Collections.Generic;

namespace ChessAI
{
    public class Search
    {
        // Implementing a Depth-First search algorithm called 'Minimax'
        // https://www.chessprogramming.org/Minimax
        // https://www.chessprogramming.org/Alpha-Beta
        // https://www.chessprogramming.org/Negamax

        /* Pseudocode basis provided:
         * Will add my own comments for my own and your own better concept understanding
         * 
         * int maxi( int depth ) 
         * {
              if ( depth == 0 ) return evaluate(); // bottom of tree node, returns evaluation of position
              int max = -oo;
              for ( all moves) 
              {
                  score = mini( depth - 1 ); // gets position relative to how the opponent would see it on their move
                  if( score > max )
                      max = score;
              }
              return max;
          }

          int mini( int depth ) 
          {
              if ( depth == 0 ) return -evaluate();
              int min = +oo;
              for ( all moves) 
              {
                  score = maxi( depth - 1 );
                  if( score < min )
                      min = score;
              }
              return min;
          }
         * https://www.youtube.com/watch?v=l-hh51ncgDI
         * also for further / visual understanding
         * 
         * 
         * 
         * Additions : 
         * - Alpha Beta Pruning
         * - Zobrist Hashing
         * - Transposition Table (Zobrist Hashing necessary)
         * - Move ordering
         * - 'Quiet' move separation
         * 
         * 
         */

        Board board;
        public int searchDepth;
        Evaluation evaluation;
        LegalMovesGenerator moveGenerator;
        float infinity;
        float negativeInfinity;

        public delegate void SearchFinished(Move selectedMove);
        public event SearchFinished AlertMove;


        // https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch?view=net-7.0
        // Stopwatch for testing purposes
        // Can save time elapsed as stopwatch.Elapsed.seconds;
        //System.Diagnostics.Stopwatch stopwatch;
        //float timeElapsed;
        public float producedEvaluation;
        public Move bestMove;

        public Search(Board board, int depth)
        {
            this.board = board;
            this.searchDepth = depth;
            evaluation = new Evaluation();

            infinity = 999; // evaluation won't be greater than 999 other than forced mate
            negativeInfinity = -infinity;

        }


        public void StartSearch()
        {

            //timeElapsed = 0f;
            //stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // searches for and selects the best move in the position to the set depth
            bestMove = Move.nullMove;
            producedEvaluation =  SearchMoves(board, searchDepth, negativeInfinity, infinity);
            
            //timeElapsed = stopwatch.Elapsed.Milliseconds;
            //Debug.Log($"time elapsed in search : {timeElapsed}");
            // Depth 1 - 0ms
            // Depth 2 - 1ms
            // Depth 3 - 20ms
            // Depth 4 - 100-200ms
            // Depth 5 - 500-2500ms
            //stopwatch.Stop();


            // Once a move is found, that move is alerted to the AI Player's NotifyMoveIsChosen function which then makes the move.
            AlertMove(bestMove);
        }

        private float SearchMoves(Board boardState, int depth, float alpha, float beta)
        {    
            // Generates list of all possible moves on the board for the current player and checks etc.
            moveGenerator = new LegalMovesGenerator(board);
            List<Move> moves = moveGenerator.GenerateMoves();

            // If player has no legal moves
            if (moves.Count == 0)
            {
                // to differentiate between stalemate and checkmate because that's quite a big difference
                if (moveGenerator.playerInCheck)
                {
                    return negativeInfinity; // float.PositiveInfinity exists, however that is used for comparing numbers divided by 0 and felt unnecessary so stored the worst case scenario as -999.
                }
                return 0; // draw
            }
            if (board.fiftyMoveCounter >= 100) // draws if 50 move counter is reached
            {
                return 0;
            }

            if (depth == 0) // At the leaf nodes of the search tree, evaluate the position.
            { 
                return evaluation.Evaluate(boardState);
            }


            // search through every move and recursively call search
            // Will need to define an 'Unmake move' procedure to be able to manipulate the boardState and search those positions.

            for (int i = 0; i < moves.Count; i++)
            {

                //Debug.Log($"before {new TestPieceLists().PerformTest(board)}"); 
                // tests the piece lists and board states before each move, after the move is made and after the move is unmade, in order to check where the error was in which en passant and castling's piece lists weren't handled correctly in the UnmakeMove function.

                boardState.MakeMove(moves[i]);

                // Implementing Negamax algorithm https://www.chessprogramming.org/Negamax

                // https://www.geeksforgeeks.org/finding-optimal-move-in-tic-tac-toe-using-minimax-algorithm-in-game-theory/
                // faced issue with the AI not choosing the most efficient move as it would see a mate, however wouldn't go for the most efficient mate. Therefore taking this advice of subtracting the depth from the evaluated score.
                // Implementing this doesn't seem to go well, however, so I've decided to remove it and I believe finding the solution to this would be something I can do in future changes to the project, when working on managing move comparisons better.
                float eval = -SearchMoves(boardState, depth - 1, -beta, -alpha);
                
                //Debug.Log($"depth {depth} checking move {boardState.gameMovesAsString.Peek()} eval {eval}");

                // Step by step process showing how negamax works : 
                // Black sends search for white position
                // White sends search for black position
                // Black evaluates the position for black's benefit (e.g. evaluates it as +6 for black)
                // White receives evaluation for black and takes it as -6, along with another move evaluation of (example) -4 for white.
                // White's search has alpha of negative infinity and beta of infinity
                // White's evaluation of -6 is greater than infinity so alpha becomes -6
                // White's evaluation of -4 is greater than its previous alpha (-6) so alpha becomes -4
                // White returns an evaluation of -4
                // Black receives that evaluation but negates it, seeing the evaluation as +4 being the evaluation of their best outcome after white plays their best move.
                // Treats the algorithm as a minimax algorithm, without the need for separate functions.

                //Debug.Log($"mid {new TestPieceLists().PerformTest(board)}");
                // tests piece list and board state after move is made

                boardState.UnmakeMove(moves[i]);
                
                //Debug.Log($"after : {new TestPieceLists().PerformTest(board)}");
                // tests piece list and board state after move is unmade


                // new best move, replace alpha ( alpha indicates the evaluation of the best move that the player can play in this position )
                if (eval > alpha)
                {
                    if (depth == searchDepth)
                    {
                        bestMove = moves[i]; // will return the best move for black to play based on their first selection of moves.
                    }
                    alpha = eval;
                }
                if (!bestMove.isValid && depth == searchDepth)
                {
                    bestMove = moves[0]; // if the AI refuses to play any moves because all it sees is imminent checkmates, simply choose the first move available.
                    // in the future, I would like to change this to where it attempts to choose the checkmate farthest away, increasing the likelihood of survival as the human player may play imperfectly.
                }
                if (eval >= beta) // opponent won't choose this position as it's better for us than beta is anyway.
                {
                    return beta;
                }
            }
            return alpha;
        }

    }
}

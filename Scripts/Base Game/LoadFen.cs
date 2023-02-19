using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessAI
{
    class FenLoader
    {
        public const string initialLayoutFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        private string[] fenStates;
        private int moveCounter;
        private Dictionary<char, int> PieceCharToInt = new Dictionary<char, int>()
        {
            {'r', Piece.rook },
            {'n', Piece.knight},
            {'b', Piece.bishop},
            {'q', Piece.queen},
            {'k', Piece.king},
            {'p', Piece.pawn}
        };


       // <FEN> ::=  <Piece Placement>              An array of pieces
       //        ' ' <Side to move>                 (w = white, b = black)
       //        ' ' <Castling ability>             (K = kingside white, Q = queenside white, k = kingside black, q = queenside white)
       //        ' ' <En passant target square>     (says if a square can be captured by en passant e.g. white plays e4, fen of black's move says e3 instead of - )
       //        ' ' <Halfmove clock>               (50 move rule)
       //        ' ' <Fullmove counter>             (counter of how many moves have been played)


        public FenLoader()
        {
            fenStates = new string[100];
            fenStates[0] = initialLayoutFen;
        }


        // https://www.c-sharpcorner.com/UploadFile/9b86d4/how-to-return-multiple-values-from-a-function-in-C-Sharp/
        // Referencing this for ideas on how to return multiple data points, choosing between referencing variables or returning a dictionary or array with the variables
        // I think I'm going to stick with referencing variables as the variables would need to be defined beforehand regardless and this function would mainly just be called for completely resetting the board, however if I believed that wouldn't be the sole purpose of it, I would likely use an array instead, but doing it like this doesn't restrict me from doing that anyway as I can simply create new variables when calling the function.
        public int[] GetBoardStateFromFen(string fen, ref int turnToMove, ref bool[] castlingPerms, ref string enPassantSquare, ref int fiftyMoveCounter, ref int moveCounter)
        {
            this.moveCounter = moveCounter;
            string[] fenData = fen.Split(' ');
            
            int[] grid = new int[64];

            string pieceLayout = fenData[0];
            
            
            // fen reads from top left along to bottom right but my grid goes from BL to TR so will have to read it row by row.

            // grid from fen
            int column = 0;
            int row = 7;
            foreach (char c in pieceLayout)
            {
                if (c == '/')
                {
                    row--;
                    column = 0;
                }
                else
                {
                    if (char.IsDigit(c))
                    {
                        column += Convert.ToInt32(c.ToString());
                    }
                    else
                    {
                        int pieceColour = (char.IsUpper(c)) ? Piece.white : Piece.black;
                        int pieceType = PieceCharToInt[char.ToLower(c)];
                        grid[(column++) + (8 * row)] = pieceColour | pieceType;
                    }
                }
            }

            turnToMove = (fenData[1][0] == 'w') ? 0 : 1; // 0 - white, 1 - black
            castlingPerms = new bool[4] {fenData[2].Contains('K'), fenData[2].Contains('Q'), fenData[2].Contains('k'), fenData[2].Contains('q')}; // [White castle kingside, white castle queenside, black castle kingside, black castle queenside]
            enPassantSquare = fenData[3]; // dealing with this in the actual move management as can't define as Coord for now with the possibility of en passant square not existing, therefore will check for if it is a '-' when calculating for en passants and will convert from 'e3' to relevant coordinate format when necessary in due course.
            fiftyMoveCounter = Convert.ToInt32(fenData[4]);
            moveCounter = Convert.ToInt32(fenData[5]);

            return grid;
        }

        public void StoreFen(Board currentBoard)
        {
            fenStates[moveCounter++] = GetCurrentFen(currentBoard); 
        }

        public string GetCurrentFen(Board board)
        {
            string[] fenData = new string[6];

            // pieces
            string pieces = string.Empty;
            int[] grid = board.boardState;
            
            int row = 7;
            int column = 0;
            bool boardSearched = false;
            int emptyCounter = 0;

            while (!boardSearched)
            {
                int boardIndex = column + (8 * row); // convenient switching between column,row and 0-63
                
                // if square is empty then it counts up how many empty squares in a row it has processed
                if (grid[boardIndex] == Piece.empty)
                {
                    emptyCounter++;
                }
                else
                {
                    if (emptyCounter > 0) // if it has counted empty squares till now then it resets that counter and adds it to the fen ( Because of the pppppppp/8/8/8/...
                    {
                        pieces += Convert.ToString(emptyCounter);
                        emptyCounter = 0;
                    }
                    
                    bool colourIndex = Piece.IsPieceWhite(grid[boardIndex]); // if piece is white then true, can be used later to make letter capital or not

                    char pieceChar = ' ';

                    switch (Piece.GetPieceType(grid[boardIndex]))
                    {
                        case Piece.pawn:
                            pieceChar = 'p';
                            break;
                        case Piece.rook:
                            pieceChar = 'r';
                            break;
                        case Piece.knight:
                            pieceChar = 'n';
                            break;
                        case Piece.bishop:
                            pieceChar = 'b';
                            break;
                        case Piece.queen:
                            pieceChar = 'q';
                            break;
                        case Piece.king:
                            pieceChar = 'k';
                            break;
                    }

                    pieces += (colourIndex) ? char.ToUpper(pieceChar) : pieceChar; 
                }

                column++; // moving along the row


                // this if statement was at the start of the while loop, however I have moved it to the back so that I can easily get out of the while loop once the end of the grid has been reached.

                if (column == 8) // if reaches end of the row
                {
                    if (emptyCounter > 0) // checks if eempty squares have been processed
                    {
                        pieces += Convert.ToString(emptyCounter);
                        emptyCounter = 0;
                    }
                    
                    row--;
                    if (row >= 0) // ensures it doesn't add a / at the end
                    {
                        column = 0;
                        pieces += '/';
                    }
                }
                if (row < 0) // if finished searching the bottom row then declare end of search
                {
                    boardSearched = true;
                }
            }

            fenData[0] = pieces;

            // side to move
            fenData[1] = (board.turnToPlay % 2 == 0) ? "w" : "b";

            // castling abilities
            string castlingRights = string.Empty;
            string fullCastlingRights = "KQkq";
            for (int i = 0; i < 4; i++)
            {
                if (board.castlingPerms[i])
                {
                    castlingRights += fullCastlingRights[i];
                }
            }


            fenData[2] = castlingRights;

            // en passant square
            fenData[3] = board.enPassantSquare;

            // halfmove clock
            fenData[4] = board.fiftyMoveCounter.ToString();

            // fullmove counter
            fenData[5] = board.movesPlayed.ToString();

            return string.Join(" ", fenData);
        }

        private int[] ReversePieceGrid(int[] input)
        {
            for (int i = 0; i < 32; i++)
            {
                int firstPiece = input[i]; // stores piece of first index
                input[i] = input[64 - i];    // replaces first index with piece on mirroring index of array
                input[64 - i] = firstPiece;  // replaces mirroring index with stored first index
            }

            // or would it be quicker to make a new array and loop through input? Good to test later
            
            // Piece[] output = new Piece[64];
            // for (int i = 0; i < 64; i++)
            // {
            //     output[i] = input[64 - i];
            // }
            // return output;


            return input;
        }
    }
}

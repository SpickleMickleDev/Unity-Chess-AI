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
        private Dictionary<char, int> PieceCharToInt = new Dictionary<char, int>() // retrieves each piece's integer value counterpart for their piece type
        {
            {'r', Piece.rook },
            {'n', Piece.knight},
            {'b', Piece.bishop},
            {'q', Piece.queen},
            {'k', Piece.king},
            {'p', Piece.pawn}
        };

        // Definition of each section in a fen string coming from https://www.chessprogramming.org/Forsyth-Edwards_Notation
        // <FEN> ::=  <Piece Placement>              An array of pieces
        //        ' ' <Side to move>                 (w = white, b = black)
        //        ' ' <Castling ability>             (K = kingside white, Q = queenside white, k = kingside black, q = queenside white)
        //        ' ' <En passant target square>     (says if a square can be captured by en passant e.g. white plays e4, fen of black's move says e3 instead of - )
        //        ' ' <Halfmove clock>               (50 move rule)
        //        ' ' <Fullmove counter>             (counter of how many moves have been played)

        // Guide to understanding how the fen state works :
        // lowercase letter indicates a black piece, whilst uppercase letter indicates a white piece
        // The piece's letter represents what type of piece it is too, e.g. p = pawn, k = king, b = bishop
        // Numbers within the fen state indicate that there are x empty spaces in a row. Can be seen like a compression algorithm, storing the information that there are x empty squares in a row.

        // The fen string's state of the board section (the first, long part of it) contains 8 sections, each separated by a /
        // Each section represents a row of the board, starting from the top left. The / indicates that it is moving on to the next line.
        // This goes from top to bottom. Using the initialLayoutFen above, this indicates that the top row contains 'rnbqkbnr' and the second to top row contains 'pppppppp', whilst the third to top row is simply 8 empty squares.

        // https://www.c-sharpcorner.com/UploadFile/9b86d4/how-to-return-multiple-values-from-a-function-in-C-Sharp/
        // Referencing this for ideas on how to return multiple data points (since the fen string contains multiple slots of data needed), choosing between referencing variables or returning a dictionary or array with the variables
        // I think I'm going to stick with referencing variables as the variables would need to be defined beforehand regardless and this function would mainly just be called for completely resetting the board, however if I believed that wouldn't be the sole purpose of it, I would likely use an array instead, but doing it like this doesn't restrict me from doing that anyway as I can simply create new variables when calling the function.
        public int[] GetBoardStateFromFen(string fen, ref int turnToMove, ref bool[] castlingPerms, ref string enPassantSquare, ref int fiftyMoveCounter, ref int moveCounter)
        {
            string[] fenData = fen.Split(' '); // separates each data point of the fen string
            
            int[] grid = new int[64]; // allocates new grid representing the board's pieces, for which this function will return to the board that calls the fenloader

            string pieceLayout = fenData[0];
            
            // fen states read from top left along to bottom right but my grid implementation goes from BL to TR so will have to read it row by row.

            // grid from fen
            int column = 0;
            int row = 7;
            foreach (char c in pieceLayout)
            {
                if (c == '/') // A / represents starting a new line in the fen string
                {
                    row--; // goes down to the next row and starts from the left
                    column = 0;
                }
                else
                {
                    if (char.IsDigit(c))
                    {
                        column += Convert.ToInt32(c.ToString()); // if the character is a number (x), skips x columns and then resumes as normal.
                    }
                    else // if the character is a piece
                    {
                        int pieceColour = (char.IsUpper(c)) ? Piece.white : Piece.black; // if the character is uppercase then the piece is white
                        int pieceType = PieceCharToInt[char.ToLower(c)]; // gets the integer value of the piece type
                        grid[(column++) + (8 * row)] = pieceColour | pieceType; // allocates this square in the grid as the piece read from the fen state.
                    }
                }
            }

            turnToMove = (fenData[1][0] == 'w') ? 0 : 1; // if the fen state says that it is white to move, turnToMove = 0, if it is black to move, turnToMove = 1
            castlingPerms = new bool[4] {fenData[2].Contains('K'), fenData[2].Contains('Q'), fenData[2].Contains('k'), fenData[2].Contains('q')}; // [White castle kingside, white castle queenside, black castle kingside, black castle queenside]
            enPassantSquare = fenData[3]; // dealing with this in the actual move management as can't define as Coord for now with the possibility of en passant square not existing, therefore will check for if it is a '-' when calculating for en passants and will convert from 'e3' to relevant coordinate format when necessary in due course.
            fiftyMoveCounter = Convert.ToInt32(fenData[4]);
            moveCounter = Convert.ToInt32(fenData[5]);

            return grid;
        }

        public string GetCurrentFen(Board board)
        {
            // function to get the current fen, however isn't used anymore since when exporting the game, I decided to switch to representing each individual move rather than just the fen state of the final position.

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
    }
}

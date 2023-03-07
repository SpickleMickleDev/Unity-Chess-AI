using System.Collections.Generic;

namespace ChessAI
{
    public class Board
    {
        public int turnToPlay; // keep to 0 - white, 1 - black
        public bool[] castlingPerms;
        public string enPassantSquare; // '-' means no possible en passant square
        public int fiftyMoveCounter;
        public int movesPlayed;

        public const int Draw = 0;
        public const int WhiteWins = 1;
        public const int BlackWins = 2;
        public bool gameFinished = false;
        public int winState = -1;

        public int[] boardState = new int[64];

        //Dictionary<int, PieceList> whitePieceLists;
        //Dictionary<int, PieceList> blackPieceLists;
        // Being able to reference the index of the colour is much easier, rather than checking (isPieceWhite(piece)) ? whitePieceLists.rook : blackPieceLists.rook; etc.
        public PieceList[] pieceLists;

        public PieceList[] rooks;
        public PieceList[] knights;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] pawns;

        public int[] kingPositions;
        public bool loadFromFen = false;
        public string initialFen = string.Empty;
        
        // stores the states of the board throughout the game, allowing for unmaking moves up to the very start.
        public Stack<string> gameMovesAsString;
        public Stack<Move> gameMoves;
        private Stack<bool[]> savedCastlingPerms;
        private Stack<int> fiftyMoveCounters;
        private Stack<int> valueOfPiecesTaken; // easier to unmake moves

        public void Setup()
        {
            gameFinished = false;
            winState = -1;
            gameMovesAsString = new Stack<string>();
            gameMoves = new Stack<Move>();
            savedCastlingPerms = new Stack<bool[]>();
            fiftyMoveCounters = new Stack<int>();
            valueOfPiecesTaken = new Stack<int>();

            // maximum number of pieces tend to be initial number of pieces + number of pawns in game since they can promote to other pieces, however I only have the queen promotion so the max number of knights, rooks and bishops are each 2. Want to be able to include custom positions, lesser promotions and chess960 later on, however.
            rooks = new PieceList[2] { new PieceList(10), new PieceList(10) };
            knights = new PieceList[2] { new PieceList(10), new PieceList(10) };
            bishops = new PieceList[2] { new PieceList(10), new PieceList(10) };
            queens = new PieceList[2] { new PieceList(10), new PieceList(10) };
            pawns = new PieceList[2] { new PieceList(8), new PieceList(8) };

            // kept having 'nested array initialiser' issues when trying to make the piecelists array 2D so instead going to make the array indexes represent the piece's corresponding integer value.
            // check Piece.cs for more clarity on how the pieces are represented as numbers.
            // Lowest value = white pawn at 1
            // Highest value = black queen at 15
            // pawn (1), knight(3), rook(5), bishop(6), queen(7), black(+8)
            pieceLists = new PieceList[16]
            {
                null,
                pawns[0],
                null,
                knights[0],
                null,
                rooks[0],
                bishops[0],
                queens[0],
                null,
                pawns[1],
                null,
                knights[1],
                null,
                rooks[1],
                bishops[1],
                queens[1]
            };

            // gets an array of pieces from the fen handler and calls procedure to set up board state from that
            // fen goes opposite direction to my layout (63 -> 0) so must reverse that within fenhandler function
            if (!loadFromFen)
            {
                LoadBoardFromFen(FenLoader.initialLayoutFen);
            }
            else
            {
                LoadBoardFromFen(initialFen);
            }

            // stores the positions of the king of each player, index 0 is white and index 1 is black, as they will be for the rest of the game's design.
            kingPositions = new int[2];
            for (int i = 0; i < 64; i++)
            {
                if (Piece.GetPieceType(boardState[i]) == Piece.king)
                {
                    kingPositions[Piece.GetPieceColour(boardState[i])] = i;
                }
            }

            //BoardTest test = new BoardTest(boardState); // testing initial layout is stored in grid as intended
        }

        void LoadBoardFromFen(string fen)
        {
            FenLoader fenhandler = new FenLoader();

            // function returns the int[] grid of the board's piece values, whilst also returning other important game management values like castling permissions etc. as referenced variables.
            boardState = fenhandler.GetBoardStateFromFen(fen, ref turnToPlay, ref castlingPerms, ref enPassantSquare, ref fiftyMoveCounter, ref movesPlayed);
            
            // add pieces to piece lists to keep track of where each type of piece for each player is on the board, very convenient for quickly referencing every white queen and their positions on the board in order to calculate their possible moves, for example.
            for (int index = 0; index < 64; index++)
            {
                // The boardState is an array of integers, with each index representing a square on the board.
                // Each integer on the board represents a piece value and therefore what piece type is on the board and what colour they are, as you can find in the Piece class.
                // The function GetPieceList(int pieceValue) takes a piece value and finds the respective piece list of that colour player's piece list for that type of piece, e.g. white pawn list.

                PieceList pieceList = GetPieceList(boardState[index]);
                if (pieceList != null)
                {
                    // boardState[index] gets the integer value of the piece represented at this square (it is looping through every square on the board)
                    pieceList.AddPieceToList(index);
                }
            }
        }

        public float CalculateMaterialAdvantage()
        {
            // Calculates the total value of each player's pieces and compares who therefore has more material / pieces with a greater total value.
            int whiteMaterial = pawns[0].Count + (3 * knights[0].Count) + (3 * bishops[0].Count) + (5 * rooks[0].Count) + (9 * queens[0].Count);
            int blackMaterial = pawns[1].Count + (3 * knights[1].Count) + (3 * bishops[1].Count) + (5 * rooks[1].Count) + (9 * queens[1].Count);

            return whiteMaterial - blackMaterial;
        }



        public PieceList GetPieceList(int pieceValue)
        {
            // gets the piece list corresponding to the piece type's value
            // the index of the pieceLists array is the integer value of the piece.
            return pieceLists[pieceValue];
        }

        public void GameOver(int winState)
        {
            // declares game as over
            gameFinished = true;
            this.winState = winState;
        }

        public string ConvertMoveToNotation(Move move)
        {
            // need to implement that if two pieces of the same type and colour can reach the same square, specify what the file or rank were of the piece that moved.

            // if sliding piece
            // if has multiple of said piece
            // if bitmaps of both pieces collide with square
            // if row == same then add column of piece
            // if column == same then add row of piece

            string str = string.Empty;
            int piece = boardState[move.initialSquare];
            int pieceType = Piece.GetPieceType(piece);

            if (move.specialMoveValue == Move.castling) // long castle / queenside castle is notated by 'O-O-O'
            {
                if (move.initialSquare > move.targetSquare)
                {
                    return "O-O-O";
                }
                // short castle / kingside castle notated by 'O-O'
                return "O-O";
            }
            if (move.specialMoveValue == Move.promotion)
            {
                // promotion notated by square promoted at = piece type
                return $"{Coord.IndexToString(move.targetSquare)}=Q";
            }

            switch (pieceType)
            {
                // pawn doesn't say what type of piece it is, just says the square it moves to
                case Piece.rook:
                    str += 'R';
                    break;
                case Piece.knight:
                    str += 'N';
                    break;
                case Piece.bishop:
                    str += 'B';
                    break;
                case Piece.queen:
                    str += 'Q';
                    break;
                case Piece.king:
                    str += 'K';
                    break;
            }

            if (Piece.IsSlidingPiece(piece)) // if sliding piece
            {
                PieceList pieceList = GetPieceList(piece);
                if (pieceList.Count > 1) // multiple of said piece
                {
                    bool collision = false;
                    int collisionSquare = -1;
                    for (int pieceIndex = 0; pieceIndex < pieceList.Count; pieceIndex++)
                    {
                        int square = pieceList.CoordinateArray[pieceIndex];
                        if (square != move.initialSquare) // doesn't include moving piece
                        {
                            ulong bitmap = 0;
                            switch (pieceType)
                            {
                                case Piece.rook:
                                    bitmap = MovementLookup.rookMoves[square];
                                    break;
                                case Piece.bishop:
                                    bitmap = MovementLookup.bishopMoves[square];
                                    break;
                                case Piece.queen:
                                    bitmap = MovementLookup.queenMoves[square];
                                    break;
                            }
                            if (((bitmap >> move.targetSquare) & 1ul) != 0) // if piece can move there on bitmap, specify which row or column it's from. Even if it's unnecessary, lichess will input it correctly
                            {
                                collision = true;
                                collisionSquare = square;
                            }
                        }
                    }
                    if (collision)
                    {
                        if (collisionSquare / 8 == move.initialSquare / 8) // same row, therefore specify column
                        {
                            str += "abcdefgh"[move.initialSquare % 8];
                        }
                        else
                        {
                            str += (1 + (move.initialSquare / 8)).ToString(); // specifies column. adds 1 to start at 1 rather than 0.
                        }
                    }
                }
            }

            bool capture = boardState[move.targetSquare] != Piece.empty; // checks if it is a capture
            if (capture)
            {
                if (pieceType == Piece.pawn)
                {
                    // original file of the pawn needs stating if it is a capture
                    str += "abcdefgh"[move.initialSquare % 8];
                }
                str += 'x'; // adds x to show that it is a capture
            }
            str += Coord.IndexToString(move.targetSquare); // adds square moving to

            // Examples : 
            // e4    =  pawn to e4
            // Ne3   =  knight to e3
            // Bxc5  =  bishop takes piece on c5
            // e8=Q  =  pawn promotes to queen on e8
            // O-O-O =  long castles queenside


            return str;
        }


        public void MakeMove(Move move)
        {
            int initialSquare = move.initialSquare;
            int targetSquare = move.targetSquare;
            int piece = boardState[initialSquare];
            int targetPiece = boardState[move.targetSquare]; // can just be empty of course

            // adds the state of the board before move was made, allowing for unmaking the move afterwards
            gameMovesAsString.Push(ConvertMoveToNotation(move));
            gameMoves.Push(move);
            savedCastlingPerms.Push((bool[])castlingPerms.Clone());
            valueOfPiecesTaken.Push(targetPiece);
            fiftyMoveCounters.Push(fiftyMoveCounter);

            // moves piece list, king doesn't have a piece list as only one is kept and those positions are changed if the king moves.
            if (Piece.GetPieceType(piece) != Piece.king)
            {
                pieceLists[piece].MovePiece(initialSquare, targetSquare);
            }

            // piece list stuff if capture
            if (boardState[targetSquare] != Piece.empty)
            {
                // if piece is captured then remove that piece from its piece list
                int target = boardState[targetSquare];
                pieceLists[target].RemovePieceFromList(targetSquare);
            }

            // remove castling rights if rook taken or moved
            // white queenside rook index 0
            // white kingside rook index 7
            // black queenside rook index 56
            // black kingside rook index 63
            if (initialSquare == 0 || targetSquare == 0)
            {
                castlingPerms[1] = false; // white queenside no longer legal
            }
            else if (initialSquare == 7 || targetSquare == 7)
            {
                castlingPerms[0] = false; // white kingside no longer legal
            }
            if (initialSquare == 56 || targetSquare == 56)
            {
                castlingPerms[3] = false; // black queenside no longer legal
            }
            else if (initialSquare == 63 || targetSquare == 63)
            {
                castlingPerms[2] = false; // black kingside no longer legal
            }


            if (move.isSpecialMove) // if move has special actions that need executing alongside it
            {
                switch (move.specialMoveValue)
                {

                    // remove castling rights if move is to castle
                    case Move.castling:
                        // no need to check if queenside or kingside castling as doing either removes the right to do either again
                        
                        castlingPerms[turnToPlay * 2] = false; // set kingside castle perms to false as if white (0) to play, set castlingPerms[0] to false, if black(1) to play, set castlingPerms[2] to false.
                        castlingPerms[(turnToPlay * 2) + 1] = false; // set queenside castle perms to false

                        // since castling, move rook to castled position
                        // In castling, king moves along 2 squares and the rook moves to the other side of the king.
                        // King - - Rook
                        // ->
                        // - Rook King -
                        bool kingsideCastle = (targetSquare - initialSquare) > 0; // Since kingside index is to the right and along the same row, targetSquare > initialSquare when kingside castling and the opposite when queenside.

                        // rook is originally 4 to the left of the king when castling queenside and 3 to the right of the king when castling kingside
                        // rook ends up 1 more than the target square in queenside castling and 1 less than the target square when kingside castling
                        int rookOriginalSquare = (kingsideCastle) ? initialSquare + 3 : initialSquare - 4;
                        int rookNewSquare = (kingsideCastle) ? targetSquare - 1 : targetSquare + 1; // rook moves to other side of king after castling
                        int rookPiece = boardState[rookOriginalSquare];
                        boardState[rookOriginalSquare] = Piece.empty;
                        boardState[rookNewSquare] = rookPiece; // rook moves to other side of king after castling
                        rooks[turnToPlay].MovePiece(rookOriginalSquare, rookNewSquare); // moves rook in piece list
                        break;
                    case Move.doublePawnMove:
                        enPassantSquare = Coord.IndexToString(targetSquare + (8 * ((turnToPlay * 2) - 1))); // - 8 from targetSquare if white(0), + 8 if black(1)
                        // sets square that was double pushed as possible en passant square for opponent
                        break;
                    case Move.enPassant:
                        // removes en passant pawn being taken from board and piece list
                        int epPawnSquare = targetSquare + ((turnToPlay == 0) ? -8 : 8);// (8 * ((turnToPlay * 2) - 1)); // needs to take away 8 if white or add 8 if black
                        boardState[epPawnSquare] = Piece.empty;
                        pawns[1 - turnToPlay].RemovePieceFromList(epPawnSquare);

                        // remove en passant capture as possibility on future moves
                        enPassantSquare = "-";
                        break;
                    case Move.promotion:
                        // remove pawn being promoted from piece list
                        pawns[turnToPlay].RemovePieceFromList(targetSquare); // targets target square as piece isn't capturing so piece list has already been moved from initial square to target square earlier
                        queens[turnToPlay].AddPieceToList(targetSquare); // adds queen to target square piece list and square in grid
                        boardState[initialSquare] = (turnToPlay * Piece.black) | Piece.queen;
                        break;
                }
            }

            boardState[targetSquare] = boardState[initialSquare]; // move over the piece in the board state grid
            boardState[initialSquare] = Piece.empty;

            // en passant only possible for the one move after a double pawn push is played, so this is to reset that back to normal
            if (enPassantSquare != "-")
            {
                if (move.specialMoveValue != Move.doublePawnMove) // in the case that a double pawn push was just played, therefore making the new enPassantSquare value the friendly en passant square, avoids immediately resetting it.
                {
                    enPassantSquare = "-";
                }
            }


            // if king is moving, change kingpos array and remove castling perms
            if (initialSquare == kingPositions[0]) // can't take king so no point in checking targetSquare for if king was taken
            {
                castlingPerms[0] = false; // if king moves, can't castle anymore
                castlingPerms[1] = false;
                kingPositions[0] = targetSquare; // moves saved king position on the board
            }
            if (initialSquare == kingPositions[1])
            {
                castlingPerms[2] = false;
                castlingPerms[3] = false;
                kingPositions[1] = targetSquare;
            }


            turnToPlay = 1 - turnToPlay; // change player turn to move after move is executed
            movesPlayed++;
            fiftyMoveCounter++;

            // fifty move counter is reset when a piece is captured or a pawn is moved
            // only case where a piece is captured without targetting the piece in the move is en passant, which is a pawn move anyway so 50 move is still recorded logically.
            if (Piece.GetPieceType(piece) == Piece.pawn || Piece.GetPieceType(targetPiece) != Piece.empty)
            {
                fiftyMoveCounter = 0;
            }
        }

        // unmake move function for in a search, where it needs to make and then unmake the move after in order to search different positions
        public void UnmakeMove(Move move)
        {
            if (movesPlayed <= 0) // don't attempt to unmake move to before the game started
            {
                return;
            }

            int playerLastTurn = 1 - turnToPlay;
            int initialSquare = move.initialSquare;
            int targetSquare = move.targetSquare;
            int pieceMoved = (move.specialMoveValue == Move.promotion) ? Piece.pawn | Piece.IndexToColour(playerLastTurn) : boardState[targetSquare];
            int pieceTaken = valueOfPiecesTaken.Pop();
            int specialMoveValue = move.specialMoveValue;

            
            // move pieces back within board 2D array of pieces, doesn't apply to en passant but that gets handled later
            boardState[initialSquare] = pieceMoved;

            // move the piece list or king position value back to before
            if (move.specialMoveValue != Move.promotion)
            {
                if (Piece.GetPieceType(pieceMoved) != Piece.king)
                {
                    pieceLists[pieceMoved].MovePiece(targetSquare, initialSquare);
                }
                else
                {
                    kingPositions[playerLastTurn] = initialSquare;
                }
            }
            boardState[targetSquare] = pieceTaken;
            if (pieceTaken != Piece.empty) // if it was a capture, replace the piece that was taken
            {
                pieceLists[pieceTaken].AddPieceToList(targetSquare);
            }

            // revert the state of the stacks that keep track of moves in the game, castling states etc. 
            movesPlayed--;
            gameMoves.Pop();
            gameMovesAsString.Pop();
            castlingPerms = savedCastlingPerms.Peek();
            savedCastlingPerms.Pop();
            fiftyMoveCounter = fiftyMoveCounters.Pop();

            // deals with reverting special moves' effects
            switch (specialMoveValue)
            {
                case Move.enPassant:
                    enPassantSquare = Coord.IndexToString(move.targetSquare);

                    // piece taken is considered Piece.empty earlier in the code so piece list and piece replacement can be added here
                    pieceTaken = Piece.IndexToColour(turnToPlay) | Piece.pawn;

                    // if pawn pushed twice was by white, the position the pawn was should be 1 forward of the square taken 
                    int pawnTakenSquare = targetSquare + ((turnToPlay == 0) ? 8 : -8);
                    boardState[pawnTakenSquare] = pieceTaken;
                    pawns[turnToPlay].AddPieceToList(pawnTakenSquare);
                    break;
                case Move.castling:
                    // replace rook to where it was and re-enable castling permissions

                    // castling perms are done outside of this as they could be affected by other things like a rook or king moving too
                    bool kingsideCastle = (targetSquare > initialSquare);
                    int originalRookSquare = targetSquare + ((kingsideCastle) ? 1 : -2);
                    int rookCastleSquare = targetSquare + ((kingsideCastle) ? -1 : 1);
                    int rookValue = boardState[rookCastleSquare]; //Piece.GetPieceColour(playerLastTurn) | Piece.rook; // this comment is the previous code, both work, however I changed it during debugging testing.
                    pieceLists[rookValue].MovePiece(rookCastleSquare, originalRookSquare);
                    boardState[rookCastleSquare] = Piece.empty;
                    boardState[originalRookSquare] = rookValue;
                    break;
                case Move.doublePawnMove:
                    enPassantSquare = "-"; // en passant no longer possible as double pawn move is taken back
                    break;
                case Move.promotion:
                    // change the piece lists back
                    pawns[playerLastTurn].AddPieceToList(initialSquare);
                    queens[playerLastTurn].RemovePieceFromList(targetSquare);
                    break;
            }

            // revert back to opposing player's turn.
            turnToPlay = playerLastTurn;

        }
    }
}

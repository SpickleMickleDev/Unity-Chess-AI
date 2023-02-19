using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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


        // instance contains the players
        // instance contains the current board setup
        // instance contains the current evaluation
        // instance contains piecelists of all pieces
        // keeping track of king position

        public int[] boardState = new int[64];
        //Dictionary<int, PieceList> whitePieceLists;
        //Dictionary<int, PieceList> blackPieceLists;
        // Being able to reference the index of the colour is much easier, rather than checking (isPieceWhite(piece)) ? whitePieceLists.rook : blackPieceLists.rook; etc.

        public PieceList[] rooks;
        public PieceList[] knights;
        public PieceList[] bishops;
        public PieceList[] queens;
        public PieceList[] pawns;

        public PieceList[] pieceLists;
        public int[] kingPositions;
        public bool loadFromFen = false;
        public string initialFen = string.Empty;

        public void Setup()
        {
            gameFinished = false;
            winState = -1;
            rooks = new PieceList[2] { new PieceList(10), new PieceList(10) };
            knights = new PieceList[2] { new PieceList(10), new PieceList(10) };
            bishops = new PieceList[2] { new PieceList(10), new PieceList(10) };
            queens = new PieceList[2] { new PieceList(10), new PieceList(10) };
            pawns = new PieceList[2] { new PieceList(8), new PieceList(8) };

            // kept having 'nested array initialiser' issues when trying to make the array 2D so instead going to make the array indexes represent the piece's corresponding integer value.
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

            kingPositions = new int[2];
            for (int i = 0; i < 64; i++)
            {
                if (Piece.GetPieceType(boardState[i]) == Piece.king)
                {
                    kingPositions[Piece.GetPieceColour(boardState[i])] = i;
                }
            }

            //BoardTest test = new BoardTest(boardState); // testing initial layout
        }

        void LoadBoardFromFen(string fen)
        {
            FenLoader fenhandler = new FenLoader();
            boardState = fenhandler.GetBoardStateFromFen(fen, ref turnToPlay, ref castlingPerms, ref enPassantSquare, ref fiftyMoveCounter, ref movesPlayed);
            // add to piece lists
            for (int index = 0; index < 64; index++)
            {
                PieceList pieceList = GetPieceList(boardState[index]);
                if (pieceList != null)
                {
                    pieceList.AddPieceToList(index);
                }
            }
        }

        public float CalculateMaterialAdvantage()
        {
            int whiteMaterial = pawns[0].Count + (3 * knights[0].Count) + (3 * bishops[0].Count) + (5 * rooks[0].Count) + (9 * queens[0].Count);
            int blackMaterial = pawns[1].Count + (3 * knights[1].Count) + (3 * bishops[1].Count) + (5 * rooks[1].Count) + (9 * queens[1].Count);

            return whiteMaterial - blackMaterial;
        }



        public PieceList GetPieceList(int pieceValue)
        {
            return pieceLists[pieceValue];
        }

        public void GameOver(int winState)
        {
            gameFinished = true;
            this.winState = winState;
        }

        public string ConvertMoveToNotation(Move move)
        {
            string str = string.Empty;
            int pieceType = Piece.GetPieceType(boardState[move.initialSquare]);

            switch (pieceType)
            {
                // pawn doesn't say what type of piece it is
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
            bool capture = boardState[move.targetSquare] != Piece.empty;
            if (capture)
            {
                if (pieceType == Piece.pawn)
                {
                    // original file of the pawn
                    str += "abcdefgh"[move.initialSquare % 8];
                }
                str += 'x';
            }
            str += Coord.IndexToString(move.targetSquare);

            return str;
        }


        public void MakeMove(Move move)
        {
            int initialSquare = move.initialSquare;
            int targetSquare = move.targetSquare;
            int piece = boardState[initialSquare];

            // piece list stuff
            if (Piece.GetPieceType(piece) != Piece.king)
            {
                pieceLists[piece].MovePiece(initialSquare, targetSquare);
            }

            // piece list stuff if capture
            if (boardState[targetSquare] != Piece.empty)
            {
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

                        // move rook
                        bool kingsideCastle = (targetSquare - initialSquare) > 0; // Since kingside index is to the right and along the same row, targetSquare > initialSquare when kingside castling and the opposite when queenside.

                        // rook is originally 4 to the left of the king when castling queenside and 3 to the right of the king when castling kingside
                        // rook ends up 1 more than the target square in queenside castling and 1 less than the target square when kingside castling
                        int rookOriginalSquare = (kingsideCastle) ? initialSquare + 3 : initialSquare - 4;
                        int rookNewSquare = (kingsideCastle) ? targetSquare - 1 : targetSquare + 1; // rook moves to other side of king after castling
                        int rookPiece = boardState[rookOriginalSquare];
                        boardState[rookOriginalSquare] = Piece.empty;
                        boardState[rookNewSquare] = rookPiece; // rook moves to other side of king after castling
                        rooks[turnToPlay].MovePiece(rookOriginalSquare, rookNewSquare);

                        break;
                    case Move.doublePawnMove:
                        enPassantSquare = Coord.IndexToString(targetSquare + (8 * ((turnToPlay * 2) - 1))); // - 8 from targetSquare if white(0), + 8 if black(1)
                        break;
                    case Move.enPassant:
                        int epPawnSquare = targetSquare + (8 * ((turnToPlay * 2) - 1)); // needs to take away 8 if white or add 8 if black
                        boardState[epPawnSquare] = Piece.empty;
                        pawns[1 - turnToPlay].RemovePieceFromList(epPawnSquare);
                        // remove possible en passant capture
                        enPassantSquare = "-";
                        break;
                    case Move.promotion:
                        pawns[turnToPlay].RemovePieceFromList(targetSquare);
                        queens[turnToPlay].AddPieceToList(targetSquare);
                        boardState[initialSquare] = (turnToPlay * Piece.black) | Piece.queen;
                        break;
                }
            }

            boardState[targetSquare] = boardState[initialSquare]; // move over the piece in the board state
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
            if (initialSquare == kingPositions[0]) // can't take king so no point in targetSquare
            {
                castlingPerms[0] = false;
                castlingPerms[1] = false;
                kingPositions[0] = targetSquare;
            }
            if (initialSquare == kingPositions[1])
            {
                castlingPerms[2] = false;
                castlingPerms[3] = false;
                kingPositions[1] = targetSquare;
            } // put near the end of the move function incase kingPositions needed referencing


            turnToPlay = 1 - turnToPlay; // change player turn to move after move is executed
            movesPlayed++;
            fiftyMoveCounter++;
            if (Piece.GetPieceType(piece) == Piece.pawn)
            {
                fiftyMoveCounter = 0;
            }
        }




    }
}

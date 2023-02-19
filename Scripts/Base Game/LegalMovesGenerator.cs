using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    class LegalMovesGenerator
    {

        // will only check for the player who's turn it is to move although will need to account for pin rays and checks


        // COULD include enemy attack map
        // Advantages : 
        // - Quick lookup for whether or not castling is illegal due to opponent attacking a square the king would slide through
        // Disadvantages:
        // - A lot of work processing the attack map for just that when I have an alternative idea:
        
        // Design function to check if an individual square is attacked
        // Call said function for each square the king moves through when castling IF it can castle
        // Benefits:
        // - Saves processing time, especially when king can't even castle so no point in attack map

        // CODED THIS FUNCTION
        // HAVENT TESTED YET
        // How the CheckSquareUnderAttack() function would work:
        // - Check if any pawns are diagonal
        //    - If white player then checking indexes +7 and +9, if black player then checking indexes -7 and -9
        //    - Therefore check 7 and 9 multiplied by playerTurnToMove * -1 to get the indexes to check.
        //    - Check if the squares wrap around the board or are out of bounds [0, 64) in advance
        // - Check all diagonals until reaching the end (using MovementLookup to find distances to each end) and check if they find any bishops or queens along the way.
        //    - If a piece is found that isn't a queen or bishop then cancel looking in that direction
        //    - If a piece is found that is, cancel search and return negative for castling
        // - Check all straight directions following the same basis as the diagonal search but for rooks and queens instead.
       
        
        List<Move> moves; // Best to store a move class so that it can store the initial piece / square moving too as just storing the target squares won't work when collecting moves for all pieces together.
        Board board;
        public int playerTurnToMove;
        int halfMoveRule; // 50 move rule
        // no need to account for 3 move repititons as that would be in the game manager and AI evaluations.
        public bool playerInCheck;
        bool doubleCheck;
        ulong pinMap;
        ulong checkMap;
        List<int> pinnedPieceIndexes;
        ulong opponentPieceMap; // for checking if a pawn can attack
        ulong friendlyPieceMap; // for checking pieces blocking the way of the king in possible moves
        ulong opponentAttackMapNoPawns;
        ulong opponentPawnAttackMap;
        ulong opponentSlidingPieceAttackMap;
        ulong opponentAttackMap;





        public LegalMovesGenerator(Board board)
        {
            this.board = board;
            this.playerTurnToMove = board.turnToPlay % 2;
            moves = new List<Move>();
            checkMap = 0;
            pinMap = 0;
        }

        public List<Move> GenerateMoves()
        {
            CalculatePieceMaps(); // retrieves positions of friendly and opponent pieces
            CalculateThreats(); // retrieves pin and check data

            // calculate king moves
            // will need to know opponent attack data before calculating king moves, incorporated in CalculateThreats();

            CalculateKingMoves(); // including castling

            if (doubleCheck)
            {
                // return only king moves
                return moves;
            }

            /*if (pinnedPieceIndexes.Count > 0)
            {
                foreach (int index in pinnedPieceIndexes)
                {
                    Debug.Log($"Pinned : {Coord.IndexToString(index)}");
                }
            }*/

            // calculate knight moves
            //Debug.Log($"Moves before knights calculated : {moves.Count}");
            CalculateKnightMoves();
            //Debug.Log($"Moves after knights calculated : {moves.Count}");

            // calculate sliding moves
            //Debug.Log($"Moves before sliding calculated : {moves.Count}");
            CalculateSlidingMoves();
            //Debug.Log($"Moves after sliding calculated : {moves.Count}");

            // calculate pawn moves (en passant etc. included)
            //Debug.Log($"Moves before pawns calculated : {moves.Count}");
            CalculatePawnMoves();
            //Debug.Log($"Moves after pawns calculated : {moves.Count}");


            return moves;
        }

        void CalculatePieceMaps()
        {
            int[] boardState = board.boardState;
            ulong whitePieceMap = 0;
            ulong blackPieceMap = 0;
            for (int i = 0; i < 64; i++)
            {
                if (Piece.GetPieceType(boardState[i]) != Piece.empty)
                {
                    if (Piece.IsPieceWhite(boardState[i]))
                    {
                        whitePieceMap |= (1ul << i);
                    }
                    else
                    {
                        blackPieceMap |= (1ul << i);
                    }
                }
            }
            friendlyPieceMap = (playerTurnToMove == 0) ? whitePieceMap : blackPieceMap;
            opponentPieceMap = (playerTurnToMove == 1) ? whitePieceMap : blackPieceMap;
        }

        // Yet to test
        bool CheckSquareUnderAttack(int squareIndex)
        {
            int[] diagonalDistances = MovementLookup.diagonalDistanceToEdge[squareIndex];
            int[] straightDistances = MovementLookup.straightDistanceToEdge[squareIndex];

            // Distances = SW SE NW NE S W E N
            int[] diagonalOffsets = MovementLookup.diagonalDirections;
            int[] straightOffsets = MovementLookup.straightDirections;

            // check for immediate pawns outside of loop
            int[] pawnAttacks = MovementLookup.pawnAttackOffsets[squareIndex][playerTurnToMove];
            foreach (int attackSquare in pawnAttacks)
            {
                if (board.boardState[attackSquare] == (((playerTurnToMove == Piece.white) ? Piece.black : Piece.white) | Piece.pawn)) // if opponent pawn is attacking square
                {
                    return true;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                // diagonal checking
                // straight checking

                int diagonalDistance = diagonalDistances[i];
                int diagonalOffset = diagonalOffsets[i];
                int straightDistance = straightDistances[i];
                int straightOffset = straightOffsets[i];

                // Due to wanting to check in each direction until found a piece that is not an opponent bishop or queen, two main options available:
                // - Run a loop in each direction, straight and diagonal separate (two loops, move processing)
                // - Keep a boolean value registering whether or not each piece is still checking
                // - Due to repeatedly having to check whether the boolean is  true before calculating the targetSquare, I have opted for two separate loops

                // go as far as distance in current selected diagonal direction
                for (int counter = 0; counter < diagonalDistance; counter++)
                {
                    int targetSquarePiece = board.boardState[squareIndex + (diagonalOffset * (counter + 1))];
                    if (Piece.GetPieceType(targetSquarePiece) != Piece.empty)
                    {
                        if (Piece.GetPieceColour(targetSquarePiece) == playerTurnToMove)
                        {
                            break;
                        }
                        else
                        {
                            if (Piece.DoesDiagonalDrag(targetSquarePiece))
                            {
                                return true;
                            }
                            break;
                        }
                    }
                }
                // go as far as distance in current selected straight direction
                for (int counter = 0; counter < straightDistance; counter++)
                {
                    int targetSquarePiece = board.boardState[squareIndex + (straightOffset * (counter + 1))];
                    if (Piece.GetPieceType(targetSquarePiece) != Piece.empty)
                    {
                        if (Piece.GetPieceColour(targetSquarePiece) == playerTurnToMove)
                        {
                            break;
                        }
                        else
                        {
                            if (Piece.DoesVerticalDrag(targetSquarePiece))
                            {
                                return true;
                            }
                            break;
                        }
                    }
                }
            }
            // no need to check a variable as can immediately return final result in loop if true in any case.
            return false;
        }

        private void CalculateThreats()
        {
            CalculatePinnedPieces();
            // sliding checks calculated

            int opponentColourIndex = 1 - playerTurnToMove;
            PieceList rooks = board.rooks[opponentColourIndex];
            PieceList knights = board.knights[opponentColourIndex];
            PieceList bishops = board.bishops[opponentColourIndex];
            PieceList queens = board.queens[opponentColourIndex];
            PieceList pawns = board.pawns[opponentColourIndex];
            int kingPos = board.kingPositions[playerTurnToMove];

            opponentAttackMap = 0;

            // calculating checks that aren't sliding pieces and adding their attacks to the opponent attack map

            // pawns

            opponentPawnAttackMap = 0;
            for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
            {
                opponentPawnAttackMap |= MovementLookup.pawnAttackMoves[pawns.CoordinateArray[pawnIndex]][opponentColourIndex]; // from perspective of opponent

                if (((1ul << kingPos) & opponentPawnAttackMap) != 0)
                {
                    doubleCheck = playerInCheck;
                    playerInCheck = true;
                    checkMap |= (1ul << pawns.CoordinateArray[pawnIndex]);
                }
            }


            // knights

            ulong knightAttackMap = 0;
            for (int knightIndex = 0; knightIndex < knights.Count; knightIndex++)
            {
                // making a knight attack map as the sliding piece attack maps and pawn attack maps are separate so knight map wouldn't fit in either
                knightAttackMap |= MovementLookup.knightMoves[knights.CoordinateArray[knightIndex]]; // CoordinateArray provides the index of each knight

                // check for checks

                if (((1ul << kingPos) & knightAttackMap) != 0) // if king pos and knight attack map overlap, indicating the king is under attack
                {
                    doubleCheck = playerInCheck;
                    playerInCheck = true;
                    checkMap |= (1ul << knights.CoordinateArray[knightIndex]);
                }
            }


            // calculating attack map for sliding pieces, checking for sliding checks unnecessary as already calculated in the check for pins procedure called at the beginning.

            opponentSlidingPieceAttackMap = 0;
            int friendlyKingSquare = board.kingPositions[playerTurnToMove];
            // IGNORE THE FRIENDLY KING
            // Faced bug where the king can simply move backwards after receiving a check as the opponent attack map stops at the king, whilst it should go through the king to stop the king from being able to do that.


            // no need for indivual attack maps so incorporating them into one for efficiency and simplicity.

            // rooks

            for (int rookIndex = 0; rookIndex < rooks.Count; rookIndex++)
            {
                // check each direction
                int rookSquare = rooks.CoordinateArray[rookIndex];
                for (int dirIndex = 0; dirIndex < MovementLookup.straightDirections.Length; dirIndex++)
                {
                    int dirOffset = MovementLookup.straightDirections[dirIndex];
                    int distance = MovementLookup.straightDistanceToEdge[rookSquare][dirIndex];

                    // enumerating through the squares the direction to the edge
                    for (int n = 0; n < distance; n++)
                    {
                        // normally when calculating moves, slide along in direction of movement and stop before a friendly piece, but add an enemy piece to list of moves if collide with one.
                        // However, since this is for checking possible king moves, it shouldn't stop when meeting a friendly piece because that means the opponent is defending a piece, rendering it untakeable by the king.
                        // Therefore, simply check until a piece is met and include that in the search
                        int targetSquare = rookSquare + (dirOffset * (n + 1)); // may as well precalculate as used twice
                        int piece = board.boardState[targetSquare];
                        opponentSlidingPieceAttackMap |= (1ul << targetSquare); // adds square to map
                        if (Piece.GetPieceType(piece) != Piece.empty && targetSquare != friendlyKingSquare)
                        {
                            // if collides with something, end search as no piece afterwards can be reached
                            break;
                        }
                    }
                }
            }
            
            // bishops

            for (int bishopIndex = 0; bishopIndex < bishops.Count; bishopIndex++)
            {
                int bishopSquare = bishops.CoordinateArray[bishopIndex];
                for (int dirIndex = 0; dirIndex < MovementLookup.diagonalDirections.Length; dirIndex++)
                {
                    int dirOffset = MovementLookup.diagonalDirections[dirIndex];
                    int distance = MovementLookup.diagonalDistanceToEdge[bishopSquare][dirIndex];

                    for (int n = 0; n < distance; n++)
                    {
                        int targetSquare = bishopSquare + (dirOffset * (n + 1));
                        int piece = board.boardState[targetSquare];
                        opponentSlidingPieceAttackMap |= (1ul << targetSquare);
                        if (Piece.GetPieceType(piece) != Piece.empty && targetSquare != friendlyKingSquare)
                        {
                            break;
                        }
                    }
                }
            }
            
            // queens
    
            for (int queenIndex = 0; queenIndex < queens.Count; queenIndex++)
            {
                int queenSquare = queens.CoordinateArray[queenIndex];
                for (int dirIndex = 0; dirIndex < MovementLookup.totalDirections.Length; dirIndex++)
                {
                    bool isDiagonal = dirIndex < 4;
                    int dirOffset = MovementLookup.totalDirections[dirIndex];
                    // if (isDiagonal) is true, then distance = the ? statement. If (isDiagonal) is false, then distance = the : statement.
                    int distance = (isDiagonal) ? MovementLookup.diagonalDistanceToEdge[queenSquare][dirIndex] : MovementLookup.straightDistanceToEdge[queenSquare][dirIndex - 4]; // gets distance to edge in selected direction
                    
                    for (int n = 0; n < distance; n++)
                    {
                        int targetSquare = queenSquare + (dirOffset * (n + 1));
                        int piece = board.boardState[targetSquare];
                        opponentSlidingPieceAttackMap |= (1ul << targetSquare);
                        if (Piece.GetPieceType(piece) != Piece.empty && targetSquare != friendlyKingSquare)
                        {
                            break;
                        }
                    }
                }
            }



            // Creating an attack map for where the king can't move
            // Sebastian Lague discovered an issue within his project where an pawn wouldn't be identified as pinned, however after capturing with en passant, the player's king would then be exposed to the king, making it an illegal move.
            // As a way to resolve this, he calculated his attack map with and without pawns on the map and added them together after.
            // Will also need to check if the king is in check after an enpassant is played, incorporating a pseudo-legal method to the move legality, however it appears to be the most efficient approach given the way my code is structured already, and performance efficiency shouldn't take a hit as en passants are very rare and the legality will only need to be checked a small number of times.

            opponentAttackMapNoPawns = knightAttackMap | opponentSlidingPieceAttackMap | MovementLookup.kingMoves[board.kingPositions[opponentColourIndex]]; // include opponent king cutting off friendly king 
            opponentAttackMap = opponentAttackMapNoPawns | opponentPawnAttackMap;

        }

        private void CalculatePinnedPieces()
        {
            // Since checking in each direction from the king, can also use this pin calculation to get whether or not the king is in check


            // loop through each direction from king position
            // keep a ulong of the ray as progressing in the direction

            // added totalDirections to MovementLookup for easy scanning in directions like this
            // directions = diagonal then straight directions
            int kingIndex = board.kingPositions[playerTurnToMove];
            pinnedPieceIndexes = new List<int>();

            for (int dirIndex = 0; dirIndex < MovementLookup.totalDirections.Length; dirIndex++)
            {
                int dirOffset = MovementLookup.totalDirections[dirIndex];
                bool isDiagonal = dirIndex < 4;
                ulong rayMap = 0;
                bool friendlyPieceMet = false;
                int friendlyIndex = -1;

                // gets the distance to the edge in the direction from the king position
                int distanceToEdge = (isDiagonal) ? MovementLookup.diagonalDistanceToEdge[kingIndex][dirIndex] : MovementLookup.straightDistanceToEdge[kingIndex][dirIndex - 4];

                for (int n = 0; n < distanceToEdge; n++)
                {
                    // since projecting from the king pos, would normally start the for loop from n = 1 to n<= distanceToEdge, however then if the king is on the edge of the board, it would attempt to calculate beyond the board, so using n = 0 instead.
                    int targetIndex = kingIndex + (dirOffset * (n + 1));
                    rayMap |= (1ul << targetIndex);
                    int piece = board.boardState[targetIndex];

                    if (Piece.GetPieceType(piece) != Piece.empty)
                    {
                        if (Piece.GetPieceColour(piece) == board.turnToPlay)
                        {
                            // if friendly piece
                            if (!friendlyPieceMet)
                            {
                                friendlyPieceMet = true;
                                friendlyIndex = targetIndex;
                            }
                            else
                            {
                                // two friendly pieces already met, pieces aren't pinned.
                                break; // breaks entire search in current direction
                            }

                        }
                        else
                        {
                            // if opponent piece
                            // To factor in if the opponent piece is instead blocking an attack ray, checking both diagonal and straight attacks in one statement

                            if ((isDiagonal && Piece.DoesDiagonalDrag(piece)) || (Piece.DoesVerticalDrag(piece) && !isDiagonal))
                            {
                                // is a threat

                                // if friendly piece met, piece added to pinned pieces and ray map added to pin map
                                if (friendlyPieceMet)
                                {
                                    pinMap |= rayMap;
                                    pinnedPieceIndexes.Add(friendlyIndex);
                                }

                                else
                                {
                                    // nothing intercepting attack
                                    
                                    // if no friendly piece met, in check
                                    // if already in check, in double check
                                    // can surely only be in check by two pieces at a maximum (due to a discovered check), however will implement as if there weren't a maximum of 2 for safety and since it's no extra cost to performance
                                    checkMap |= rayMap;
                                    doubleCheck = playerInCheck;
                                    playerInCheck = true;
                                }
                            }
                            else
                            {
                                // blocked by other opponent piece i.e. pawn
                                break;
                            }
                        }

                    }

                }


            }


            // go through each direction from king position
            // If only one friendly piece spotted before finding a threatening piece, add path to pin mask and piece to pinned pieces
            // If no friendly piece spotted before finding a threatening piece, add path to check mask


        }

        private void CalculateKingMoves()
        {
            // use opponent attack map and bitmap of possible moves from king position
            int kingIndex = board.kingPositions[playerTurnToMove];
            // for each square, where A = king bitmap, B = opponent attack map and C = friendly piece map
            // Output = A . !B . !C
            // or A . !( B + C )
            ulong possibleSquares = (MovementLookup.kingMoves[kingIndex]) & (~(opponentAttackMap | friendlyPieceMap));
            for (int i = 0; i < 64; i++)
            {
                if ((1ul & (possibleSquares >> i)) == 1ul) // right shifts the number x times and checks if there is a 1 in the last position, basically getting a 0-63 index number for every 1 in the bitmap.
                {
                    moves.Add(new Move(kingIndex, i)); // adds move to list of possible moves
                }
            }

            // castling only works when not in check
            if (!playerInCheck)
            {
                bool[] castlingPerms = board.castlingPerms; // [White castle kingside, white castle queenside, black castle kingside, black castle queenside]

                // kingside castling paths as adding numbers to king position (-1, -2 for queenside or +1, +2 for kingside)
                bool[] colourCastlingPerms = (playerTurnToMove == 0) ? new bool[2] { castlingPerms[0], castlingPerms[1] } : new bool[2] { castlingPerms[2], castlingPerms[3] }; // split into kingside and queenside permissions of respective colour

                // precautions:
                // - check squares between are completely empty
                // - check squares between are free from possible opponent attacks

                // facing issue here of how to specify that the move is a castling move?
                // new Move (kingIndex, kingIndex + 2) doesn't trigger a castling move, perhaps add a move parameter like isEnPassant and isCastling?
                // Would then need 4 possible conditions:
                // - enPassant
                // - Castling
                // - Promotion
                // - Double pawn move
                // Researched implementation using https://www.chessprogramming.org/Encoding_Moves
                // Resulting idea is to implement a 'code' in the Move struct that can represent a different special type of move, similar to the Piece class

                if (colourCastlingPerms[0])
                {
                    // for kingside castling, index + 1 and + 2
                    bool kingsideLegal = true;

                    // check if squares empty
                    if (board.boardState[kingIndex + 1] != Piece.empty)
                    {
                        kingsideLegal = false;
                    }
                    if (board.boardState[kingIndex + 2] != Piece.empty)
                    {
                        kingsideLegal = false;
                    }

                    // check if squares are safe

                    if ( ( ( opponentAttackMap  >>  ( kingIndex + 1 ) )  &  1 )  !=  0 )
                    {
                        kingsideLegal = false;
                    }

                    if (((opponentAttackMap >> (kingIndex + 2)) & 1) != 0)
                    {
                        kingsideLegal = false;
                    }


                    if (kingsideLegal)
                    {
                        moves.Add(new Move(kingIndex, kingIndex + 2, Move.castling));
                    }

                }
                if (colourCastlingPerms[1])
                {
                    // for queenside castling, index - 1 and - 2
                    bool queensideLegal = true;

                    // check if squares empty
                    if (board.boardState[kingIndex - 1] != Piece.empty)
                    {
                        queensideLegal = false;
                    }
                    if (board.boardState[kingIndex - 2] != Piece.empty)
                    {
                        queensideLegal = false;
                    }
                    if (board.boardState[kingIndex - 3] != Piece.empty)
                    {
                        queensideLegal = false;
                    }

                    // check if squares are safe

                    if (((opponentAttackMap >> (kingIndex - 1)) & 1) != 0)
                    {
                        queensideLegal = false;
                    }
                    if (((opponentAttackMap >> (kingIndex - 2)) & 1) != 0)
                    {
                        queensideLegal = false;
                    }


                    if (queensideLegal)
                    {

                        moves.Add(new Move(kingIndex, kingIndex - 2, Move.castling));
                    }
                }

            }

        }

        private void CalculateKnightMoves()
        {
            PieceList knights = board.knights[playerTurnToMove];
            for (int n = 0; n < knights.Count; n++)
            {
                int knightSquare = knights.CoordinateArray[n];
                
                if (isPiecePinned(knightSquare))
                {
                    continue; // if this knight is pinned then move onto the next knight for moves to add as this knight cannot move.
                }
                /* PREVIOUS CODE
                ulong knightAttackMap = MovementLookup.knightMoves[knightSquare];
                for (int targetSquare = 0; targetSquare < 64; targetSquare++)
                {
                    if (((knightAttackMap >> targetSquare) & 1ul) != 0)
                    {
                        // if knight can move to this square

                    }
                }*/
                // I did not like how I had to process so much information in loops for discovering where the piece could move, figuring that it would be significantly more efficient to store this in the MovementLookup too.
                int[] knightSquares = MovementLookup.knightMovesIndexes[knightSquare];
                foreach (int square in knightSquares)
                {
                    // skip if player is in check and knight does not block or if piece moving to contains a friendly piece
                    if (playerInCheck)
                    {
                        if (((checkMap >> square) & 1ul) != 0) // if knight move can intercept or capture check
                        {
                            moves.Add(new Move(knightSquare, square));
                        }
                        // continue; // move onto next square to check although don't need the code as else statement is skipped anyway, leaving the continue statement no functionality.
                    }
                    else if (!Piece.IsSameColour(board.boardState[knightSquare], board.boardState[square]))
                    {
                        moves.Add(new Move(knightSquare, square));
                    }
                }

            }

        }

        private void CalculateSlidingMoves()
        {
            PieceList rooks = board.rooks[playerTurnToMove];
            PieceList bishops = board.bishops[playerTurnToMove];
            PieceList queens = board.queens[playerTurnToMove];
            
            // can't simply concatinate array as they only go up to a certain count.
            // Instead I have the option of manually going through and copying the arrays or using Array.Copy() since that gives a possible range within the array to clone.
            int[] slidingPieces = new int[rooks.Count + queens.Count];
            int[] diagonalPieces = new int[bishops.Count + queens.Count];

            Array.Copy(queens.CoordinateArray, 0, slidingPieces, 0, queens.Count);
            Array.Copy(queens.CoordinateArray, 0, diagonalPieces, 0, queens.Count);
            Array.Copy(rooks.CoordinateArray, 0, slidingPieces, queens.Count, rooks.Count);
            Array.Copy(bishops.CoordinateArray, 0, diagonalPieces, queens.Count, bishops.Count);

            int[] boardState = board.boardState; // will be referenced often
            int friendlyKingPos = board.kingPositions[playerTurnToMove];
            
            foreach (int square in slidingPieces)
            {
                int[] directions = MovementLookup.straightDirections;
                int[] distances = MovementLookup.straightDistanceToEdge[square];
                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                {
                    int dirOffset = directions[dirIndex];
                    int distance = distances[dirIndex];
                    for (int n = 0; n < distance; n++)
                    {
                        // if friendly or opponent king, break
                        // if opponent piece, add square and then break
                        // else, just add square
                        // Therefore structuring it as check if friendly , add move and then check if opponent piece
                        // On later note, adding a check case and accounting for the piece being pinned

                        int targetSquare = square + (dirOffset * (n + 1));
                        int pieceType = Piece.GetPieceType(boardState[targetSquare]); // both referenced twice so felt better to just define them
                        int pieceColour = Piece.GetPieceColour(boardState[targetSquare]);

                        if (pieceType == Piece.king || (pieceType != Piece.empty && pieceColour == playerTurnToMove))
                        {
                            break;
                        }

                        // account for piece pins
                        // could check if square is along pin map ray, however that would allow for it being able to move to another square on a second pin, opening up their pin which should be illegal.
                        // Therefore needs to check if the direction offset between the target square and initial square lines up with the king.
                        // Will do this in a function
                        if (isPiecePinned(square))
                        {
                            if (!IsMoveAlongPinRay(square, targetSquare, friendlyKingPos))
                            {
                                break; // if doesn't move along pin ray, move onto next direction. No point moving onto next square as since they're in the same direction, neither will be along pin ray regardless.
                            }
                        }



                        // check if in check then move is valid still
                        if (playerInCheck)
                        {
                            if (((checkMap >> targetSquare) & 1ul) == 0) // if move isn't along check map then isn't legal, so move onto checking the next square. Do not need to have a condition for double checks as that is done in the initial GenerateMoves() function and only returns king moves.
                            {
                                continue;
                            }
                        }

                        moves.Add(new Move(square, targetSquare));

                        if (pieceType != Piece.empty && pieceColour != playerTurnToMove)
                        {
                            break;
                        }   
                    }
                }
            }
            foreach (int square in diagonalPieces)
            {
                // functionality is the same as straight directions, just with different direction offsets so code can simply be copied over. I don't like this repetition of code, however, so in future implementation I would standardise this better.
                int[] directions = MovementLookup.diagonalDirections;
                int[] distances = MovementLookup.diagonalDistanceToEdge[square];
                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                {
                    int dirOffset = directions[dirIndex];
                    int distance = distances[dirIndex];
                    for (int n = 0; n < distance; n++)
                    {

                        // if friendly or opponent king, break
                        // if opponent piece, add square and then break
                        // else, just add square
                        // Therefore structuring it as check if friendly , add move and then check if opponent piece

                        int targetSquare = square + (dirOffset * (n + 1));
                        int pieceType = Piece.GetPieceType(boardState[targetSquare]); // both referenced twice so felt better to just define them
                        int pieceColour = Piece.GetPieceColour(boardState[targetSquare]);

                        if (pieceType == Piece.king || (pieceType != Piece.empty && pieceColour == playerTurnToMove))
                        {
                            break;
                        }

                        if (isPiecePinned(square))
                        {
                            if (!IsMoveAlongPinRay(square, targetSquare, friendlyKingPos))
                            {
                                break;
                            }
                        }


                        // check if in check then move is valid still
                        if (playerInCheck)
                        {
                            if (((checkMap >> targetSquare) & 1ul) == 0) // if move isn't along check map then isn't legal, so move onto checking the next square. Do not need to have a condition for double checks as that is done in the initial GenerateMoves() function and only returns king moves.
                            {
                                continue;
                            }
                        }


                        moves.Add(new Move(square, targetSquare));

                        if (pieceType != Piece.empty && pieceColour != playerTurnToMove)
                        {
                            break;
                        }
                    }
                }
            }
        }


        private void CalculatePawnMoves()
        {
            PieceList pawnsList = board.pawns[playerTurnToMove];
            int[] pawns = new int[pawnsList.Count];
            Array.Copy(pawnsList.CoordinateArray, pawns, pawnsList.Count);
            int[] boardState = board.boardState;
            int friendlyKingSquare = board.kingPositions[playerTurnToMove];
            int rowToDoublePush = (playerTurnToMove == Piece.white) ? 1 : 6;
            int rowToPromote = (playerTurnToMove == Piece.white) ? 7 : 0;
            bool enPassantPossible = board.enPassantSquare != "-";
            int enPassantSquare = Coord.StringToIndex(board.enPassantSquare);

            foreach (int square in pawns)
            {
                // if piece is pinned it cannot move except in the direction of the pin. Check this in each type of move individually
                
                // single move case
                // +8 if white, -8 if black
                // int directionMultiplier = (playerTurnToMove == Piece.white) ? 1 : -1;
                
                int squareForward = (playerTurnToMove == Piece.white) ? 8 : -8;
                int singlePushSquare = square + squareForward;//(8 * directionMultiplier);

                // Criteria:
                // - Single pawn push must be legal under check
                // - Single pawn push square infront must be empty
                // - Single pawn push on last row must turn into promotion
                // - Double pawn push must be legal under check
                // - Double pawn push must have both squares empty
                // - Double pawn push must have initial square on starting row
                // - Double pawn push must be marked as a double pawn push move
                // - Regular capture only possible if opponent piece occupies attacking square
                // - Regular capture must be legal under check
                // - En passant must only be when possible
                // - Pawn must be directly next to en passant pawn (horizontally)
                // - En passant must only be when avoids check if in check
                // - En passant must not put king in check after, which I will do through a pseudo-legal method when an en passant is legal via all other standards.

                if (boardState[singlePushSquare] == Piece.empty)
                {
                    if (!isPiecePinned(square) || IsMoveAlongPinRay(square, singlePushSquare, friendlyKingSquare)) // can't move if pinned, unless is moving along pin ray.
                    {
                        // if piece isn't pinned or can move forward one in pin, the same can be said about moving two forward. To save processing power, the double pawn push can be checked and executed simply as an extention of the single pawn push.
                        if (!playerInCheck || ((checkMap >> singlePushSquare) & 1ul) != 0) // if player not in check or move intercepts check map
                        {
                            if (singlePushSquare / 8 == rowToPromote)
                            {
                                moves.Add(new Move(square, singlePushSquare, Move.promotion)); // promote if on last row
                            }
                            else
                            {
                                moves.Add(new Move(square, singlePushSquare));
                            }
                        }


                        if (square / 8 == rowToDoublePush) // if pawn hasn't moved yet so is on the starting row and is able to double push
                        {
                            // no need to check for promotion as row is confirmed to be on double push line which is too far away.
                            int doublePushSquare = singlePushSquare + squareForward;
                            if (boardState[singlePushSquare] == Piece.empty && boardState[doublePushSquare] == Piece.empty) // if both squares forward are empty
                            {
                                if (!playerInCheck || ((checkMap >> doublePushSquare) & 1ul) != 0) // if not in check OR movement intercepts check map / ray
                                {
                                    moves.Add(new Move(square, doublePushSquare, Move.doublePawnMove));
                                }
                            }
                        }
                    }
                }

                // captures
                // en passant is an extention of capturing so will include as an extention of the case checking for regular captures
                // captures diagonally forward with offsets +7 or +9 if white and -7 or -9 if black
                // will need to check that attack doesn't wrap around board so could instead simply use pawn attack bitmap and overlap it with opponentpiecemap.
                // Did this by adding a set of integer attack squares in the pre calculated movement lookup for ease of referencing.
                int[] attacks = MovementLookup.pawnAttackOffsets[square][playerTurnToMove];
                foreach (int attackSquare in attacks)
                {
                    // checking if an attack is legal under pin
                    if (isPiecePinned(square) && !IsMoveAlongPinRay(square, attackSquare, friendlyKingSquare))
                    {
                        continue;
                    }

                    // en passant check legality needs checking after en passant move is calculated and other wise legal
                    // since attacking square would be empty, need to put before checking if attacking square is empty.
                    // No need to check for if a friendly piece is there etc. as that isn't possible to double pawn push through.
                    if (attackSquare == enPassantSquare)
                    {
                        int capturedPawnSquare = enPassantSquare - squareForward;
                        if (EPCaptureLegal(square, attackSquare, capturedPawnSquare))
                        {
                            Debug.Log(true);
                            moves.Add(new Move(square, attackSquare, Move.enPassant));
                        }
                        else
                        {
                            Debug.Log(false);
                        }
                    }

                    // A + (!A . B) = A + B
                    if (boardState[attackSquare] == Piece.empty || Piece.GetPieceColour(boardState[attackSquare]) == playerTurnToMove)
                    {
                        continue;
                    }


                    // if square is occupied by opponent piece
                    if (((opponentPieceMap >> attackSquare) & 1ul) != 0)
                    {
                        // if is legal under checks
                        if (!playerInCheck || ((checkMap >> attackSquare) & 1ul) != 0)
                        {
                            // check for promotion when taking piece on the promotion row.
                            if (attackSquare / 8 == rowToPromote)
                            {
                                moves.Add(new Move(square, attackSquare, Move.promotion));
                            }
                            else
                            {
                                moves.Add(new Move(square, attackSquare));
                            }

                        }
                    }
                }
            }
        }

        private bool IsMoveAlongPinRay(int startSquare, int targetSquare, int friendlyKingSquare)
        {
            int offset = Math.Abs(targetSquare - startSquare);
            // need to make this the smallest offset possible (-9, -8, -7, -1, 1, 7, 8, 9)
            // could check offset from king to both start and target squares?

            // might simply check if offset % 9, 8, 7 etc. == 0 and therefore it is moving along that ray
            // then will check if offset from start square or target square to friendly king square % offset == 0
            // if so then it lies along said ray.

            int simpleOffset = 1;
            bool isAlongRay = false;
            if (offset % 9 == 0)
            {
                simpleOffset = 9;
            }
            else if (offset % 8 == 0)
            {
                simpleOffset = 8;
            }
            else if (offset % 7 == 0)
            {
                simpleOffset = 7;
            }

            int offsetFromKing = targetSquare - friendlyKingSquare;
            // case where offset doesn't lie along any of these and therefore is assumed the offset of 1 even though it is a horse direction away e.g. 17, in which case if they aren't on the same row then invalid.
            // offset from king can only be in these specified directions, not something like a knight's move away so the king must follow the rules of being a multiple of the simpleOffset or it isn't within the ray.
            if (offsetFromKing % simpleOffset == 0)
            {
                isAlongRay = true;
                if (simpleOffset == 1 && friendlyKingSquare / 8 != targetSquare / 8)
                {
                    isAlongRay = false;
                }
            }


            return isAlongRay;
        } 

        private bool EPCaptureLegal(int startSquare, int targetSquare, int capturedPawnSquare)
        {
            // simulate EP capture / remove the two pawns from board
            board.boardState[targetSquare] = board.boardState[startSquare];
            int takenPiece = board.boardState[capturedPawnSquare];
            board.boardState[startSquare] = Piece.empty;
            board.boardState[capturedPawnSquare] = Piece.empty;
            
            // I was referencing Sebastian Lague's implementation of the en passant legality in the case of checks after en passant is captured, since he faced the same difficulty.
            // I then analysed his code and discovered a bug in which it allowed the user to take an en passant which discovered a diagonal attack on the king if the pawn taken in en passant was blocking the diagonal.
            // Therefore, I will be implementing a full check as to whether the king is in check, rather than a smaller one that attempts to cut corners by only checking the horizontal.

            // check if king is in check after en passant move
            bool kingInCheck = CheckSquareUnderAttack(board.kingPositions[playerTurnToMove]);
            


            // return state to normal
            board.boardState[startSquare] = board.boardState[targetSquare];
            board.boardState[targetSquare] = Piece.empty;
            board.boardState[capturedPawnSquare] = takenPiece;


            return !kingInCheck;
        }

        private bool isPiecePinned(int squareNum)
        {
            return ((1ul << squareNum) & pinMap) > 0;
        }

        // for initial move checking before moves generator has been finished, running only on pins and move data assuming the rest of the board is empty.
        public List<Move> FetchPieceBitmapAsMoveList(Coord initialSquare)
        {
            CalculatePieceMaps(); // retrieves positions of friendly and opponent pieces
            CalculatePinnedPieces(); // retrieves pin and check data


            bool canPieceMove = true;
            int piece = board.boardState[initialSquare.CoordAsGridNum()];
            int isBlack = Piece.GetPieceColour(piece);
            int pieceType = Piece.GetPieceType(piece);

            ulong movesOnBoard;
            int squareAsGrid = initialSquare.CoordAsGridNum();

            switch (pieceType)
            {
                case Piece.pawn:
                    // Adds the passive pawn moves on the board
                    movesOnBoard = MovementLookup.pawnPassiveMoves[squareAsGrid][isBlack];

                    // calculate attack moves for pawn
                    ulong pawnAttacks = MovementLookup.pawnAttackMoves[squareAsGrid][isBlack];
                    movesOnBoard |= (pawnAttacks & opponentPieceMap); // where a pawn can take an opposing piece, add it to map of moves on board
                    break;
                case Piece.rook:
                    movesOnBoard = MovementLookup.rookMoves[squareAsGrid];
                    break;
                case Piece.knight:
                    movesOnBoard = MovementLookup.knightMoves[squareAsGrid];
                    break;
                case Piece.bishop:
                    movesOnBoard = MovementLookup.bishopMoves[squareAsGrid];
                    break;
                case Piece.queen:
                    movesOnBoard = MovementLookup.queenMoves[squareAsGrid];
                    break;
                case Piece.king:
                    movesOnBoard = MovementLookup.kingMoves[squareAsGrid];
                    break;
                default:
                    movesOnBoard = 0;
                    break;
            }

            // is piece pinned
            Debug.Log($"Piece pinned : {isPiecePinned(squareAsGrid)}");
            if (isPiecePinned(squareAsGrid))
            {
                canPieceMove = false;
            }

            // is in check?
            // -> Can it block or take?



            // Goes through the list and adds a new move to each square with a 1
            List<Move> moves = new List<Move>();
            
            for (int i = 0; i < 64; i++)
            {
                if (((movesOnBoard >> i) & 1ul) == 1ul)
                {
                    moves.Add(new Move(squareAsGrid, i));
                }
            }

            if (!canPieceMove)
            {
                return new List<Move>(); // essentially null
            }
            return moves;

        }

    }
}

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
        // Function generates all legal moves for just the player to move's perspective in the board's current position.
        // Will account for the opponent's possible moves, however those will be independant to calculating the king's possible moves and calculating pinned pieces (i.e. they can't move as they're blocking an attack on their friendly king, so moving out of the way would open up an attack on their king which is illegal).

        // COULD include enemy attack map
        // Advantages : 
        // - Quick lookup for whether or not castling is illegal due to opponent attacking a square the king would slide through
        // Disadvantages:
        // - A lot of work processing the attack map for just that when I have an alternative idea:
        
        // Design function to check if an individual square is attacked
        // Call said function for each square the king moves through when castling IF it can castle
        // Benefits:
        // - Saves processing time, especially when king can't even castle so no point in attack map

        // How the CheckSquareUnderAttack() function would work:
        // - Check if any pawns are diagonal
        //    - If white player then checking indexes +7 and +9, if black player then checking indexes -7 and -9
        //    - Therefore check 7 and 9 multiplied by playerTurnToMove * -1 to get the indexes to check.
        //    - Check if the squares wrap around the board or are out of bounds [0, 64) in advance
        // - Check all diagonals until reaching the end (using MovementLookup to find distances to each end) and check if they find any bishops or queens along the way.
        //    - If a piece is found that isn't a queen or bishop then cancel looking in that direction
        //    - If a piece is found that is, cancel search and return negative for castling
        // - Check all straight directions following the same basis as the diagonal search but for rooks and queens instead.
       
        
        List<Move> moves; // Best to store a move class so that it can store the initial piece / square moving too as just storing the target squares won't tell us which piece is moving there, since we are collecting all possible moves for all of the player to move's pieces.
        Board board;

        // no need to account for 3 move repititons as that would be in the game manager and AI evaluations.
        
        public int playerTurnToMove;
        public bool playerInCheck;
        bool doubleCheck;

        // keeping track of the rays from the opponent's pieces to the king in which pin pieces, so that I can check if when the pinned piece is moving, if it still blocks that check, without having to do pseudo-legal move generation, in which it makes the move and then checks afterwards if the king would be in check, as that is very inefficient.
        // visual example : 
        // 0 0 0 0 0 0 0 0
        // 0 0 0 0 0 0 0 0
        // 0 0 0 0 0 0 0 0
        // 0 B 0 0 0 0 0 0
        // 0 0 1 0 0 0 0 0
        // 0 0 0 1 0 0 0 0
        // 0 R 1 1 K 0 0 0
        // 0 0 0 0 0 0 0 0
        // K = king, B = Enemy Bishop, R = Enemy Rook
        // on the actual bitmap, the B and R would both be represented as 1s, since to remove the pin or check you can take the attacking piece when looking for moves that can block the attack.
        ulong pinMap;
        // does the same for keeping track of check rays from opponent pieces, allowing for checking if a piece is able to block the check, without needing to look through each of their moves and then re-calculate whether or not the king would be in check after, since that would be VERY inefficient.
        public ulong checkMap;
        // keeps a track of which friendly pieces are pinned
        List<int> pinnedPieceIndexes;

        ulong opponentPieceMap; // for checking if a pawn can attack a square, since an opponent's piece must be occupying that square and it can't just passively move to that square instead.
        ulong friendlyPieceMap; // for checking if pieces block off the king's possible moves.
        ulong opponentAttackMapNoPawns;
        public ulong opponentPawnAttackMap;
        ulong opponentSlidingPieceAttackMap;
        public ulong opponentAttackMap; // keeps track of where the opponent is attacking, so that the king cannot move there otherwise it would be in check.





        public LegalMovesGenerator(Board board)
        {
            this.board = board;
            this.playerTurnToMove = board.turnToPlay % 2; // the player's turn to move is either 0 or 1, 0 - white, 1 - black
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
                // return only king moves if the king is in check from two different sources at once, since using any other piece can only block one attack, therefore meaning the only way of moving out of both checks is to move the king away.
                return moves;
            }

            // calculate knight moves
            CalculateKnightMoves();

            // calculate sliding moves
            CalculateSlidingMoves();

            // calculate pawn moves (en passant etc. included)
            CalculatePawnMoves();

            // Used to log the number of moves between each procedure, in order to check how many moves the generator saw for each of the separate pieces, to help in debugging specifically where any errors came from.

            return moves;
        }

        void CalculatePieceMaps()
        {
            // checks each square, if occupied by a white piece then add it to the white piecemap and if occupied by a black piece then add it to the black piecemap
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
            // if white to move, friendly piece map is the white piece map and vice versa.
            friendlyPieceMap = (playerTurnToMove == 0) ? whitePieceMap : blackPieceMap;
            opponentPieceMap = (playerTurnToMove == 1) ? whitePieceMap : blackPieceMap;
        }

        bool CheckSquareUnderAttack(int squareIndex)
        {
            int[] diagonalDistances = MovementLookup.diagonalDistanceToEdge[squareIndex];
            int[] straightDistances = MovementLookup.straightDistanceToEdge[squareIndex];

            // Distances = SW SE NW NE S W E N
            int[] diagonalOffsets = MovementLookup.diagonalDirections;
            int[] straightOffsets = MovementLookup.straightDirections;

            /*
            // check for immediate pawns outside of loop
            int[] pawnAttacks = MovementLookup.pawnAttackOffsets[squareIndex][playerTurnToMove];
            foreach (int attackSquare in pawnAttacks)
            {
                // if an opponent pawn is attacking this square, this square is under attack
                if (board.boardState[attackSquare] == (((playerTurnToMove == Piece.white) ? Piece.black : Piece.white) | Piece.pawn)) // if opponent pawn is attacking square
                {
                    return true;
                }
            }
            */ // did check if pawn attacked the square, then discovered that this doesn't actually need to be accounted for, since this function is only for checking if the king is in check after exposed en passant captures, as I explain at the end of this function.

            for (int i = 0; i < 4; i++)
            {
                // diagonal checking
                // straight checking
                // both in the same loop, since there are 4 straight directions to check and 4 diagonal directions to check.

                int straightOffset = straightOffsets[i]; // gets the offset that this direction is adding to each square, e.g. with square 0 at the bottom left and 63 at the top right, adding 1 goes to the right and adding 8 goes 1 up
                int straightDistance = straightDistances[i]; // gets the distance to the edge of the board in this direction
                int diagonalOffset = diagonalOffsets[i]; // does the same as above but for diagonal directions instead.
                int diagonalDistance = diagonalDistances[i]; 

                // Due to wanting to check in each direction until found a piece that is not an opponent bishop or queen, two main options available:
                // - Run a loop in each direction, straight and diagonal separate (two loops, move processing)
                // - Keep a boolean value registering whether or not each piece is still checking
                // - Due to repeatedly having to check whether the boolean is true before calculating the targetSquare, I have opted for two separate loops

                // go as far as distance in current selected diagonal direction
                for (int counter = 0; counter < diagonalDistance; counter++)
                {
                    // gets index of target square in the grid's size 64 integer array 
                    int targetSquarePiece = board.boardState[squareIndex + (diagonalOffset * (counter + 1))];

                    // if the target square is empty then move on to the next square in the direction.
                    if (Piece.GetPieceType(targetSquarePiece) != Piece.empty) // if the target square is not empty, check if it is friendly or not.
                    {
                        if (Piece.GetPieceColour(targetSquarePiece) == playerTurnToMove) // if piece in direction is friendly, this square can't be attacked in this direction so break.
                        {
                            break;
                        }
                        else // if piece in direction isn't friendly
                        {
                            if (Piece.DoesDiagonalDrag(targetSquarePiece)) // if opponent piece attacks diagonally, this square must be under attack as we're looking in a diagonal direction with no obstacles in between.
                            {
                                return true;
                            }
                            break;
                        }
                    }
                }

                // go as far as distance in current selected straight direction
                // same as above code, just for straight directions instead.
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

            // no need to check a variable as can immediately return final result in loop if true in any case above.
            // Don't need to check for knights or kings since this function is called in order to check if the king is under attack after an en passant capture, due to legality explained later on in the en passant move handling, and only straight or diagonal attacks need to be accounted for.
            return false;
        }

        private void CalculateThreats()
        {
            CalculatePinnedPieces();
            // sliding checks calculated, as well as pins. Therefore only need to check non-sliding checks in the rest of this procedure.

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
            ulong opponentPawnIndividualAttackMap = 0;
            for (int pawnIndex = 0; pawnIndex < pawns.Count; pawnIndex++)
            {
                // I'm storing the individual pawn's attack map each time, so that checks are only being checked for each individual pawn and then they're added to the overall pawn attack map afterwards.
                // This is because before, I checked if the overall opponent pawn attack map put the king in check for each pawn, and as a result it would identify a single pawn check as a double check on the king as it would check if the overall pawn attack map put the king in check each time, and not just the individual pawn's attack map that is being added.
                opponentPawnIndividualAttackMap = MovementLookup.pawnAttackMoves[pawns.CoordinateArray[pawnIndex]][opponentColourIndex]; // from perspective of opponent

                if (((1ul << kingPos) & opponentPawnIndividualAttackMap) != 0)
                {
                    doubleCheck = playerInCheck;
                    playerInCheck = true;
                    checkMap |= (1ul << pawns.CoordinateArray[pawnIndex]);
                }
                opponentPawnAttackMap |= opponentPawnIndividualAttackMap;
            }


            // knights


            // keeping an individual attack map saves from calling repeat checks due to checking against the whole attack map each time, leading to false double checks.
            ulong knightAttackMap = 0;
            ulong individualKnightAttackMap = 0;
            for (int knightIndex = 0; knightIndex < knights.Count; knightIndex++)
            {
                // making a knight attack map as the sliding piece attack maps and pawn attack maps are separate so knight map wouldn't fit in either
                individualKnightAttackMap = MovementLookup.knightMoves[knights.CoordinateArray[knightIndex]]; // CoordinateArray provides the index of each knight

                // check for checks

                if (((1ul << kingPos) & individualKnightAttackMap) != 0) // if king pos and knight attack map overlap, indicating the king is under attack
                {
                    doubleCheck = playerInCheck;
                    playerInCheck = true;
                    checkMap |= (1ul << knights.CoordinateArray[knightIndex]);
                }
                knightAttackMap |= individualKnightAttackMap;
            }


            // calculating attack map for sliding pieces, checking for sliding checks unnecessary as already calculated in the check for pins procedure called at the beginning.

            opponentSlidingPieceAttackMap = 0;
            int friendlyKingSquare = board.kingPositions[playerTurnToMove];
            // IGNORE THE FRIENDLY KING
            // Faced bug where the king can simply move backwards after receiving a check as the opponent attack map stops at the king, whilst it should go through the king to stop the king from being able to do that.


            // no need for individual attack maps so incorporating them into one for efficiency and simplicity.

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

            // added totalDirections to MovementLookup for easy scanning in directions like this, as it allows for checking all of the diagonal and straight directions in one loop.
            // directions = diagonal then straight directions
            int kingIndex = board.kingPositions[playerTurnToMove];
            pinnedPieceIndexes = new List<int>();

            for (int dirIndex = 0; dirIndex < MovementLookup.totalDirections.Length; dirIndex++)
            {
                int dirOffset = MovementLookup.totalDirections[dirIndex];
                bool isDiagonal = dirIndex < 4; // gets if the direction being searched is diagonal or not
                ulong rayMap = 0;
                bool friendlyPieceMet = false;
                int friendlyIndex = -1;

                // gets the distance to the edge in the direction from the king position
                int distanceToEdge = (isDiagonal) ? MovementLookup.diagonalDistanceToEdge[kingIndex][dirIndex] : MovementLookup.straightDistanceToEdge[kingIndex][dirIndex - 4];

                for (int n = 0; n < distanceToEdge; n++)
                {
                    // since projecting from the king pos, would normally start the for loop from n = 1 to n<= distanceToEdge, however then if the king is on the edge of the board, it would attempt to calculate beyond the board, so using n = 0 instead, since then if there's 0 distance to the edge, it doesn't check it and then I can simply add 1 to the n value when looking at the target square and n is only used once so it doesn't add significant complication.
                    int targetIndex = kingIndex + (dirOffset * (n + 1));
                    rayMap |= (1ul << targetIndex); // keeps track of the direction being looked in. Therefore if a check of pin is found, it can record the path it takes.
                    int piece = board.boardState[targetIndex];


                    // if square this far into this direction isn't empty
                    if (Piece.GetPieceType(piece) != Piece.empty)
                    {
                        if (Piece.GetPieceColour(piece) == board.turnToPlay) // if friendly piece
                        {
                            if (!friendlyPieceMet) // if this is the first friendly piece, continue looking as this piece may get pinned
                            {
                                friendlyPieceMet = true;
                                friendlyIndex = targetIndex;
                            }
                            else // if this is the second friendly piece met
                            {
                                // two friendly pieces already met, pieces aren't pinned.
                                break; // breaks entire search in current direction
                            }

                        }
                        else
                        {
                            // if opponent piece
                            // To factor in if the opponent piece is instead blocking an attack ray, checking both diagonal and straight attacks in one statement

                            if ((isDiagonal && Piece.DoesDiagonalDrag(piece)) || (Piece.DoesVerticalDrag(piece) && !isDiagonal)) // if opponent piece attacks in this direction
                            {
                                // is a threat

                                // if friendly piece met, piece added to pinned pieces and ray map added to pin map
                                if (friendlyPieceMet)
                                {
                                    pinMap |= rayMap;
                                    pinnedPieceIndexes.Add(friendlyIndex);
                                }

                                else // if a friendly piece hasn't been met
                                {
                                    // nothing intercepting attack
                                    
                                    // if no friendly piece met, in check
                                    // if already in check, in double check
                                    // can surely only be in check by two pieces at a maximum (due to a 'discovered check' being the only form of double check), however will implement as if there weren't a maximum of 2 for safety and since it's no extra cost to performance
                                    checkMap |= rayMap;
                                    doubleCheck = playerInCheck;
                                    playerInCheck = true;
                                }
                                break; // stop looking in this direction, avoids a check from two pieces in a row being considered a double check, when blocking the check would block both.
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
            // or A . !( B + C )     (boolean algebra notation, I'm not just smashing my head into my keyboard)
            
            // checking the moves around the king, since it can move 1 in each direction UNLESS the square is under attack from an opponent piece or occupied by a friendly piece.
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
                // (squares between king and rook, since after castling, they will both move to the squares in between them).

                // facing issue here of how to specify that the move is a castling move?
                // new Move (kingIndex, kingIndex + 2) doesn't trigger a castling move, perhaps add a move parameter like isEnPassant and isCastling?
                // Would then need 4 possible conditions:
                // - enPassant
                // - Castling
                // - Promotion
                // - Double pawn move
                // Researched implementation using https://www.chessprogramming.org/Encoding_Moves
                // Resulting idea is to implement a 'code' in the Move struct that can represent a different special type of move, similar to the Piece class

                if (colourCastlingPerms[0]) // if king can castle on their kingside, i.e. on the right, also known as 'short castling' since the gap is only 2 squares between king and rook
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

                    // check if squares are safe / not under attack by opponent pieces

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
                if (colourCastlingPerms[1]) // if king can castle on their queenside, i.e. on their left, also known as 'long castling' since the gap is 3 squares between king and rook
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
            for (int n = 0; n < knights.Count; n++) // for each knight on the player to move's side
            {
                int knightSquare = knights.CoordinateArray[n]; // gets the index at which the knight is sat at
                
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
                
                int[] knightSquares = MovementLookup.knightMovesIndexes[knightSquare]; // gets the square number for each square that it can move to, for ease of looking up moves.
                foreach (int square in knightSquares)
                {
                    // skip if player is in check and knight does not block OR if piece moving to contains a friendly piece
                    if (playerInCheck) // I think this line of code is pretty self explanatory
                    {
                        if (((checkMap >> square) & 1ul) != 0) // if knight move can intercept check or capture piece delivering the check
                        {
                            moves.Add(new Move(knightSquare, square));
                        }
                    }
                    else if (!Piece.IsSameColour(board.boardState[knightSquare], board.boardState[square])) // if target square is a friendly piece, returns false if square is empty too btw.
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
            
            // can't simply concatinate arrays together to combine rooks and queens into a sliding pieces array, since the PieceList is an abstract data type and therefore the array containing the squares that the pieces reside at doesn't use the full array (most of the time) since it is dependant on the variable count of pieces stored within the array.
            // Basically the PieceList of the rooks holds an array which contains the squares of all of the friendly rooks on the board, however this array is static whilst the number of rooks on the board is dynamic (can be taken, leading to fewer than the initial number of rooks), therefore a count variable is used to measure how many rooks are on the board and in the array.
            // Therefore can't simply add the full rook and full queen arrays together, since not all of the array may be in use.

            // Instead I have the option of manually going through and copying the arrays or using Array.Copy() since that gives a possible range within the array to clone.  (and hard copies the array, transferring the objects within the array and not just the pointers to those objects)

            int[] slidingPieces = new int[rooks.Count + queens.Count]; // sorting the lists of squares containing rooks, queens and bishops into pieces that can slide sideways and pieces that can slide diagonally. 
            int[] diagonalPieces = new int[bishops.Count + queens.Count];// this allows me to calculate diagonal moves on all bishops and queens within the same array, and the same for rooks and queens performing straight moves.

            Array.Copy(queens.CoordinateArray, 0, slidingPieces, 0, queens.Count); // adds queens to both sliding pieces and diagonal pieces arrays
            Array.Copy(queens.CoordinateArray, 0, diagonalPieces, 0, queens.Count);
            Array.Copy(rooks.CoordinateArray, 0, slidingPieces, queens.Count, rooks.Count); // adds rooks to sliding pieces array
            Array.Copy(bishops.CoordinateArray, 0, diagonalPieces, queens.Count, bishops.Count); // adds bishops to diagonal pieces array

            int[] boardState = board.boardState; // the board will be referenced often so simply saving it to avoid needlessly accessing the board.boardState multiple times and taking an extra navigational step.
            int friendlyKingPos = board.kingPositions[playerTurnToMove];

            int[] directions = MovementLookup.straightDirections;
            int[] distances;
            foreach (int square in slidingPieces) // for each friendly sliding piece's location
            {
                distances = MovementLookup.straightDistanceToEdge[square]; // distances to edges in each direction from this square
                
                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++) // for each straight direction
                {
                    int dirOffset = directions[dirIndex]; // gets the square offset for each direction, i.e. going to the square 1 north has a square index of + 8, whilst going 1 West has a square index of - 1 (unless you're at an edge)
                    int distance = distances[dirIndex];
                    for (int n = 0; n < distance; n++) // enumerates through squares in this direction until it meets the edge
                    {
                        // if meets friendly or opponent king, break
                        // if opponent piece, add square (to capture) and then break
                        // else (square is empty), just add square

                        // update : previous structure ^ here needed adding to for accounting for piece pins and checks etc, which can be seen below. 

                        int targetSquare = square + (dirOffset * (n + 1));
                        int pieceType = Piece.GetPieceType(boardState[targetSquare]); // both referenced twice so felt better to just define them
                        int pieceColour = Piece.GetPieceColour(boardState[targetSquare]);

                        if (pieceType == Piece.king || (pieceType != Piece.empty && pieceColour == playerTurnToMove)) // if target is a king or friendly piece, break and stop looking in this direction
                        {
                            break;
                        }

                        // account for piece pins
                        // when a piece is pinned, in some cases, it is allowed to move.
                        // this is because if a rook is attacking your king, however one of your rooks is blocking that attack, moving your rook towards their rook (or taking it) wouldn't actually open up your king to a check, even though your rook is pinned.
                        // as a result, I will need to add that if this rook is pinned, if the direction that the rook is checking is in line with the pin, this move is legal (unless the other conditions are met). 

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

                        // if in check then check if move blocks the check and is therefore still valid
                        // since I haven't yet checked for if the target collides with an opponent piece (since I want to add that you can move to that square to take it and THEN break the search), the code used to check if the player was in check, and if the move didn't collide with the check ray, it would continue to the next square in this direction.
                        // That resulted in illegally moving through opponent pieces in order to block the check.
                        // As a result, now checking if the move collides with an opponent piece and if in check, breaks beforehand instead.

                        // would still be allowed to slide through an opponent piece to block the check, which should be illegal. Therefore also check if collides with opponent piece before.
                        bool collidesWithOpponent = (pieceType != Piece.empty) && (pieceColour != playerTurnToMove);
                        
                        if (playerInCheck)
                        {
                            if (collidesWithOpponent && ((checkMap >> targetSquare) & 1ul) == 0)
                            {
                                break;
                            }
                            else if (((checkMap >> targetSquare) & 1ul) == 0) // if move isn't along check map then isn't legal, so move onto checking the next square. Do not need to have a condition for double checks as that is done in the initial GenerateMoves() function and only returns king moves.
                            {
                                continue;
                            }
                        }

                        moves.Add(new Move(square, targetSquare));

                        if (collidesWithOpponent)
                        {
                            break;
                        }   
                    }
                }
            }
            // now searches in diagonal directions
            directions = MovementLookup.diagonalDirections;
            foreach (int square in diagonalPieces)
            {
                // functionality is the same as straight directions, just with different direction offsets so code can simply be copied over. I don't like this repetition of code, however, so in future implementation I would standardise this better.
                distances = MovementLookup.diagonalDistanceToEdge[square];
                for (int dirIndex = 0; dirIndex < directions.Length; dirIndex++)
                {
                    int dirOffset = directions[dirIndex];
                    int distance = distances[dirIndex];
                    for (int n = 0; n < distance; n++)
                    {
                        int targetSquare = square + (dirOffset * (n + 1));
                        int pieceType = Piece.GetPieceType(boardState[targetSquare]);
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

                        bool collidesWithOpponent = pieceType != Piece.empty && pieceColour != playerTurnToMove;
                        
                        if (playerInCheck)
                        {
                            if (collidesWithOpponent && ((checkMap >> targetSquare) & 1ul ) == 0)
                            {
                                break;
                            }
                            else if (((checkMap >> targetSquare) & 1ul) == 0)
                            {
                                continue;
                            }
                        }


                        moves.Add(new Move(square, targetSquare));

                        if (collidesWithOpponent)
                        {
                            break;
                        }
                    }
                }
            }
        }


        private void CalculatePawnMoves()
        {
            PieceList pawnsList = board.pawns[playerTurnToMove]; // gets list of pawns

            int[] pawns = new int[pawnsList.Count]; // gets array of the squares at which those pawns lay
            Array.Copy(pawnsList.CoordinateArray, pawns, pawnsList.Count);

            int[] boardState = board.boardState;
            int friendlyKingSquare = board.kingPositions[playerTurnToMove];
            
            int rowToDoublePush = (playerTurnToMove == Piece.white) ? 1 : 6; // gets which row the pawn must be on in order to double push forward
            int rowToPromote = (playerTurnToMove == Piece.white) ? 7 : 0; // gets which row the pawn must be on in order to promote
            
            int enPassantSquare = Coord.StringToIndex(board.enPassantSquare); // returns -1 if the en passant square input is '-' meaning en passant not possible.

            foreach (int square in pawns)
            {
                // if piece is pinned it cannot move except in the direction of the pin. Check this in each type of move individually, since attacks and passive pushes move in different directions
                
                // single move case
                // +8 if white, -8 if black
                
                int squareForward = (playerTurnToMove == Piece.white) ? 8 : -8; // adding this value to the pawn's square will get the index of the square 1 forward for the pawn, which is the opposite direction for black.
                int singlePushSquare = square + squareForward;

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
                        if (!playerInCheck || ((checkMap >> singlePushSquare) & 1ul) != 0) // if player not in check OR move intercepts check map
                        {
                            if (singlePushSquare / 8 == rowToPromote) // singlePushSquare / 8 gets the row of the square index since the column is index MOD 8 and the row is index DIV 8.
                            {
                                moves.Add(new Move(square, singlePushSquare, Move.promotion)); // promote if on last row
                            }
                            else
                            {
                                moves.Add(new Move(square, singlePushSquare)); // if next move isn't onto a promotion square then just add the move to push one forward
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
                        if (EPCaptureLegal(square, attackSquare, capturedPawnSquare)) // checks if En Passant capture is legal, reference function below for implementation and explanation
                        {
                            moves.Add(new Move(square, attackSquare, Move.enPassant));
                        }
                    }

                    // A + (!A . B) = A + B
                    if (boardState[attackSquare] == Piece.empty || Piece.GetPieceType(boardState[attackSquare]) == Piece.king|| Piece.GetPieceColour(boardState[attackSquare]) == playerTurnToMove)
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
            // need to make this the smallest offset possible (-9, -8, -7, -1, 1, 7, 8, 9) so that enumerating in this direction to check if the ray collides with the king does not jump over the king instead
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
                if (simpleOffset == 1 && friendlyKingSquare / 8 != targetSquare / 8) // checks that an offset of 1 or -1 (moving left or right)'s target is actually on the same row, otherwise reject.
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
            // Therefore, I will be implementing a full analysis as to whether the king is in check, rather than a smaller one that attempts to cut corners by only checking the horizontal.
            // This is a pseudo-legal implementation which I really don't like, however it actually appears to be one of the quickest ways to go about it, since it is only pseudo-checking a single move and therefore doesn't have the significant drawbacks of a full pseudolegal implementation.

            // check if king is in check after en passant move
            bool kingInCheck = CheckSquareUnderAttack(board.kingPositions[playerTurnToMove]);
            


            // return state to normal
            board.boardState[startSquare] = board.boardState[targetSquare];
            board.boardState[targetSquare] = Piece.empty;
            board.boardState[capturedPawnSquare] = takenPiece;

            return !kingInCheck; // capture is legal if king is not in check afterwards
        }

        private bool isPiecePinned(int squareNum)
        {
            return ((1ul << squareNum) & pinMap) > 0; // checks if piece is in the pin map
        }
    }
}

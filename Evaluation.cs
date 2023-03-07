using System;

namespace ChessAI
{
    public class Evaluation
    {
        Board board;
        int whiteTotalMaterialValue;
        int blackTotalMaterialValue;

        public float Evaluate(Board board)
        {
            this.board = board;
            float whiteAdvantage = board.CalculateMaterialAdvantage();

            // I would like it to incorporate a few things:
            // - Material advantage
            // - Whether or not they then have castle permissions, whether they're castled etc.
            // - Motive / Technique to checkmate opponent
            // - Piece positioning (stop moving the king out into the open, prefer options where king is castled and away from action
            // - ^^ Also put pawns in middle etc.

            // Will create an array of integers with a number to represent each square on the board for each piece, similarly to how Zobrist Hashing uses a 2D array separating the square from the piece type on it.
            // Black's moves will be from the perspective of the opposite side of the board, therefore will use the row (7 - row) to calculate the black piece's position multiplier and the column won't need changing as the position values will be symmetrical on the board.

            // material will decrease as time progresses.. In the case that it doesnt (i.e. promotions), we want to dissuade end game evaluations as it's actually gone further away from the endgame, so multiplier still applies.
            // did have it where multiplier would be 32 / (32 - total material count) so that it would start at a multiplier of 1 and would then slowly increase, however pieces like a queen would matter significantly more than a pawn. Therefore will calculate total material value for each side within the position value calculations.

            whiteTotalMaterialValue = 0;
            blackTotalMaterialValue = 0;
            whiteAdvantage += CalculateWhitePositionAdvantage();

            // negative whiteAdvantage means black has the advantage and if it's black's analysis of the board state, that's preferred so the advantage is made positive.
            return whiteAdvantage * ((board.turnToPlay == 0) ? 1 : -1);
        }

        private float CalculateWhitePositionAdvantage()
        {
            // calculate position values of each piece list
            float whiteAdvantage = 0f;
            whiteAdvantage += CalculatePieceListPositionValue(board.rooks, MovementLookup.pawnPositionValues, 5);
            whiteAdvantage += CalculatePieceListPositionValue(board.knights, MovementLookup.pawnPositionValues, 3);
            whiteAdvantage += CalculatePieceListPositionValue(board.bishops, MovementLookup.pawnPositionValues, 3);
            whiteAdvantage += CalculatePieceListPositionValue(board.queens, MovementLookup.pawnPositionValues, 9);


            // calculates end game weight based on the total value of the weakest side, so that it still recognises it as an end game if one side has lots of material and is trying to checkmate the opponent.
            float endGameWeight = 10;
            if (Math.Min(whiteTotalMaterialValue, blackTotalMaterialValue) > 0)
            {
                endGameWeight = 0.1f * (60 / Math.Min(whiteTotalMaterialValue, blackTotalMaterialValue));

            }
            bool isEndgame = endGameWeight > 2;

            // is end game when total material < a certain amount
            // Would normally be when queens get exchanged, however then need to discern between an endgame with a queen and without (i.e. promote pawn to queen)
            // Also needs to recognise promoting pawns as being way better, therefore when the AI considers that the game state has reached the endgame stage, the pawn position map values will instead transition to one that prioritises promoting significantly more than it does the initial structure of the pawns like in the opening and middle game.
            if (isEndgame)
            {
                // in the end game, pawn position values prioritise promoting, whilst in the opening and middle game phases, it prioritises maintaining a strong position and taking control of the centre of the board.
                whiteAdvantage += CalculatePieceListPositionValue(board.pawns, MovementLookup.pawnEndGamePositionValues, 1);
            }
            else
            {
                whiteAdvantage += CalculatePieceListPositionValue(board.pawns, MovementLookup.pawnPositionValues, 1);
            }

            // invert king position values if in the endgame since the king should get active and try to stay in the middle as to not get mated
            // However, if you want to mate the opponent, needs to push the opponent to the edge and move with them to do that.
            // I don't like how the evaluation would jump when it recognises that it's the endgame because of the doubling of the pawns' pieces. Therefore will simply multiply all material and positional advantages by a slow multiplier throughout the game as it reaches more of an endgame.

            int wKingPos = board.kingPositions[0];
            int bKingPos = board.kingPositions[1];
            whiteAdvantage += MovementLookup.kingPositionValues[wKingPos] * (isEndgame ? 1 : -1);
            whiteAdvantage -= MovementLookup.kingPositionValues[bKingPos] * (isEndgame ? 1 : -1);

            if (whiteTotalMaterialValue > blackTotalMaterialValue && endGameWeight > 1.5f) // material inbalance becomes more crucial in later game
            {
                whiteAdvantage += (whiteTotalMaterialValue - blackTotalMaterialValue) * endGameWeight * 0.1f;
            }


            // multiply by endgame weight
            whiteAdvantage *= endGameWeight * 0.2f; // don't want it to increase too much forever, will therefore multiply on a scale from 0 - 1 instead.


            return whiteAdvantage;
        }

        private float CalculatePieceListPositionValue(PieceList[] piecelists, int[] positionValueGrid, int pieceValue) // Treats white as + value and black as - value
        {
            // a lot of data to handle so rather than initialising it on every evaluation (a lot of times), will reference it as pre-initialised data like MovementLookup.
            // Also unnecessary to do a switch case on deciding which piece table to use so will take it as a parameter instead as I'll be looping through the piecelists and will input them manually already.
            // Taking input of piecelist as array of piece lists so that I can easily decipher between white piece lists and black piecelists, in order to invert the table.
            float totalValue = 0f;
            for (int i = 0; i < piecelists[0].Count; i++) // for each white piece of this type
            {
                totalValue += positionValueGrid[piecelists[0].CoordinateArray[i]]; // retrieve the position value for this piece
                whiteTotalMaterialValue += pieceValue; // add the position value to white's advantage
            }
            for (int i = 0; i < piecelists[1].Count; i++) // for each black piece of this type
            {
                int index = piecelists[1].CoordinateArray[i];
                totalValue -= positionValueGrid[(7 - (index % 8)) + (7 - (index / 8))]; // reflects position on table, since the position value matrix is from white's perspective. Then subtracts this value from white's advantage.
                blackTotalMaterialValue += pieceValue;
            }
            return totalValue;
        }


    }
}

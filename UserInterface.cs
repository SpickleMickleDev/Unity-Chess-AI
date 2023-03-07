using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessAI
{
    class UserInterface : MonoBehaviour
    {
        // BOARD MANAGEMENT AND CREATION SECTION
        static GameObject[,] tiles;
        static SpriteRenderer[,] pieceObjects;
        PieceSpriteHandler spriteHandler; // no instance to be set as it is a scriptable object, not one to actually be instantiated.
        LegalMovesGenerator movesGenerator;
        Material lightHighlightMat; // material that the squares turn to if they're being highlighted
        Material darkHighlightMat;
        Material lightMovesMat; // material that the squares turn to if it's displaying the player's possible legal moves
        Material darkMovesMat;

        bool boardInstantiated = false; // whether or not the board has been instantiated yet

        private void CreateBoard()
        {
            // The tiles are a set of unmoving square sprites, each representing a square on the board and their material will be changed to represent being highlighted etc.
            // The pieceObjects are a set of sprite renderers, each located on the same coordinates as their corresponding square, however, these can be dragged around by the player.
            // The pieceObjects' sprite value represents the piece that is at that square to the player, however it isn't actually where the piece is stored and when a piece is moved on the board, that sprite renderer doesn't actually permnanently move to the target square.
            // When a move is made, the board's grid of pieces is updated to the new board state and then the sprite renderers visible to the human player are reset.
            // When these sprite renderers are reset, each sprite renderer finds the piece at its square on the grid and then sets that to its sprite, retrieving the actual sprite from the PieceSpriteHandler.
            // Therefore, when a piece is moved, it isn't snapping to the location of the square it's dragged to, but instead all sprites are reset to their original squares and then each sprite is redrawn to the new board's grid.
            // It's slightly more complicated than just moving the piece sprite to a new square, however it's more logic tight and better separates the display of the game from the actual back-end calculation side of it, also prevenging issues like having to then handle the destruction of sprites when a piece is taken etc (including en passants).

            tiles = new GameObject[8, 8];
            pieceObjects = new SpriteRenderer[8, 8];
            GameObject blackReferenceTile = (GameObject)Instantiate(Resources.Load("blacktile")); // loads the base black tile example
            GameObject whiteReferenceTile = (GameObject)Instantiate(Resources.Load("whitetile")); // loads the base white tile example

            lightHighlightMat = (Material)Resources.Load("lightHighlight"); // retrieves the materials to be used for highlighting etc. 
            darkHighlightMat = (Material)Resources.Load("darkHighlight");
            lightMovesMat = (Material)Resources.Load("lightMovesMat");
            darkMovesMat = (Material)Resources.Load("darkMovesMat");

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++) // loops through the whole grid
                {
                    // make 
                    tiles[col, row] = (GameObject)Instantiate(((row + col) % 2 != 0) ? whiteReferenceTile : blackReferenceTile, GameObject.Find("Tiles").transform); // Discovered that it didn't like the calling of the MonoBehaviour transform variable since I was instantiating a new UserInterface within GameManager so it hadn't declared a transform value. Fixed this inside the GameManager by instead instantiating the User Interface with gameObject.AddComponent<UserInterface>(); https://docs.unity3d.com/ScriptReference/GameObject.AddComponent.html
                    tiles[col, row].GetComponent<SpriteRenderer>().sortingOrder = 1; // Makes it so that the tiles are underneath the pieces in the camera's view
                    tiles[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);
                    tiles[col, row].transform.localScale = new Vector2(1, 1);
                    tiles[col, row].name = $"{"abcdefgh"[col]}{row+1}";
                    
                    pieceObjects[col, row] = new GameObject().AddComponent<SpriteRenderer>();
                    pieceObjects[col, row].transform.parent = tiles[col, row].transform;
                    pieceObjects[col, row].GetComponent<SpriteRenderer>().sortingOrder = 2; // makes it so taht the pieces are above the tiles in the camera's view
                    pieceObjects[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);
                    pieceObjects[col, row].transform.localScale = new Vector2(0.75f, 0.75f);
                }
            }
            
            Destroy(blackReferenceTile);
            Destroy(whiteReferenceTile);
        }
        
        public void ChangeBoardFromMove(Board board, Move move, int playerTurn)
        {
            if (playerTurn == 0) // was it the computer making the move
            {
                // Reason that it is playerTurn == 0 (the player's colour) is because the move was made on the board just before this, so it has already flipped to the next person's move, however this is for animating the move that was just played.
                StartCoroutine(AnimateComputerMove(board, move));
            }
            else
            {
                // redrawing pieces and resetting squares is done in the computer animation too as it needs to be called after the animation finishes.
                // Needs to reset the pieces after the player has made their move too so that it shows the board after the move has been made
                RedrawPieces(board);
                ResetSquares();
            }

            // Did have the squares highlight where a piece is moving from and to, however it didn't look great
            //HighlightSquare(move.initialSquare, lightHighlightMat.color, darkHighlightMat.color);
            //HighlightSquare(move.targetSquare, lightHighlightMat.color, darkHighlightMat.color);
        }

        public void SelectSquare(int squareIndex) // highlights a square that has been selected. Got rid of it because it didn't look right, but might implement later anyways if it fits, however certainly not needed.
        {
            HighlightSquare(squareIndex, lightHighlightMat.color, darkHighlightMat.color);
        }
        public void ResetSquares() // resets the squares to their original colours
        {
            GameObject blackReferenceTile = (GameObject)Instantiate(Resources.Load("blacktile"));
            GameObject whiteReferenceTile = (GameObject)Instantiate(Resources.Load("whitetile"));
            Color blackColour = blackReferenceTile.GetComponent<SpriteRenderer>().color; // get the original colours of the 'base' example sprites
            Color whiteColour = whiteReferenceTile.GetComponent<SpriteRenderer>().color;
            Destroy(blackReferenceTile);
            Destroy(whiteReferenceTile);

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    tiles[col, row].GetComponent<SpriteRenderer>().color = ((col + row) % 2 != 0) ? whiteColour : blackColour;
                }
            }
        }

        public void RedrawPieces(Board board) // can't find a way to access the board globally since no way to reference the GameManager (Unless maybe searching for the game object containing the game manager and selecting its component, but this way is much easier)
        {
            // I found that it would try to draw the grid before it had even initialised the board tiles or pieces even though the order of initialising them first made sense to me.
            // As a result, I decided to restructure how the CreateBoard function would be called, such that it would instantiate before the board would be updated.
            if (!boardInstantiated)
            {
                CreateBoard();
                boardInstantiated = true;
            }
            
            // tests the initial board state
            //BoardTest boardTest = new BoardTest(board.boardState);
            
            spriteHandler = ScriptableObject.CreateInstance<PieceSpriteHandler>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    // resets all of the pieces back to their original squares
                    pieceObjects[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);

                    // allocates each sprite sprite handler their new sprite (is null if no piece is present)
                    pieceObjects[col, row].sprite = spriteHandler.GetSpriteFromPieceType(board.boardState[col + (row * 8)]);

                    // resets the pieces' height to normal (above the tiles)
                    // This is because when dragging pieces, their sorting order is set to 3, making them higher than the other pieces.
                    pieceObjects[col, row].sortingOrder = 2;
                }
            }

            // TESTING

            // When allocating sprites to the pieceObjects, the sprites would register as null, indicating that the allocation of the sprites was wrong or the sprites themselves were null
            // Through further testing of logging to the console menu with checking the SpriteRenderer in pieceObjects[,], I discovered that there was nothing wrong with the SpriteRenderers
            // I then changed the script that returns the sprites so that it would log what sprite is being returned, and found no issue there.
            // I then introduced a public integer variable 'test' that can be modified within unity, where I also input the sprites of the chess pieces.
            // From that, I discovered that regardless of what I make that test variable, the script only has it saved as its default value of 0.
            // This led me to diagnose that the issue was that none of the sprites within the script point to the Sprites saved within the Assets folder of the Unity project.
            // After lots of research and trying different methods like directly grabbing the sprites from the resources folder, nothing seemed to work so now I'm going to try and move the GetSpriteFromPieceType() function into another script which doesn't derive from MonoBehaviour as I believe that may be the issue at hand.
            // This once again didn't work, however it turns out that the sprites had a default value forbidding them from being read or written to.
            // After changing this, I can finally reference them using Resources.Load()!!        

        }

        private IEnumerator AnimateComputerMove(Board board, Move move)
        {
            // Intending to use lerping to animate the piece moving.
            // https://docs.unity3d.com/ScriptReference/Vector3.Lerp.html

            float t = 0;
            const float timeDuration = 0.2f; // takes 0.2s to animate the piece's movement.
            int initialSquare = move.initialSquare;
            int targetSquare = move.targetSquare;

            SpriteRenderer piece = pieceObjects[initialSquare % 8, initialSquare / 8];
            piece.sortingOrder = 3;

            Vector2 initialCoords = new Vector2((initialSquare % 8) - 3.5f, (initialSquare / 8) - 3.5f);
            Vector2 targetCoords = new Vector2((targetSquare % 8) - 3.5f, (targetSquare / 8) - 3.5f);



            while (t <= 1f) // over time, increases the t variable until it reaches the time it's meant to take to move the pieces.
            {
                yield return null;
                t += Time.deltaTime / timeDuration;
                piece.transform.position = Vector2.Lerp(initialCoords, targetCoords, t); // t represents a value from 0 to 1 and using Lerp, the position of the piece is the fraction of t from 0 to 1 between the initialCoords and targetCoords.
            }

            // board then resets to show the new board position (with the moved piece in the target square)
            RedrawPieces(board);
            ResetSquares();
        }

        public void DragPiece(Coord pieceCoord, Vector2 mousePos) // shows pieces being dragged by the mouse. Position of the piece is constantly updated to be equal to the mouse position each frame.
        {
            pieceObjects[pieceCoord.column, pieceCoord.row].transform.position = mousePos;
            pieceObjects[pieceCoord.column, pieceCoord.row].sortingOrder = 3; // lifted higher than other pieces for visibility of dragging above the board, piece is reset back to normal sorting order in RedrawPieces which will be called after the move is attempted
        }

        public void HighlightSquareSelection(Board board, int initialSquare)
        {
            movesGenerator = new LegalMovesGenerator(board);
            List<Move> moves = movesGenerator.GenerateMoves(); // makes a list of all possible moves by the player
            HighlightSquare(initialSquare, lightMovesMat.color, darkMovesMat.color); // highlights the square being selected because it looked weird every target square being highlighted but not the selected square
            
            foreach (Move mv in moves) // for each possible move
            {
                if (mv.initialSquare == initialSquare) // if the move involves moving the selected square then highlight the target square
                {
                    HighlightSquare(mv.targetSquare, lightMovesMat.color, darkMovesMat.color);
                }
            }
        }
        
        private void HighlightSquare(int squareIndex, Color lightColour, Color darkColour) // changes the colour of the tile's sprite renderer
        {
            int row = squareIndex / 8;
            int col = squareIndex % 8;
            Color colour = ((row + col) % 2 != 0) ? lightColour : darkColour;//new Color(246,246,105) : new Color(186,202,43);
            // it won't change the colour when I try to change the color property of the sprite renderer, however for some reason it works in the resetSquares function, so I will try to take a similar approach to that.
            tiles[col, row].GetComponent<SpriteRenderer>().color = colour;
        }

        // these are all for displaying the movementlookup and movementgeneration testing on the board. Can pretty much ignore.
        
        public void HighlightSquaresForMoveGenerationTesting(List<int> data)
        {
            StartCoroutine(DisplayTestingMoves(data, 5f));
        }
        // knight movement testing
        public void HighlightSquaresForMoveGenerationTesting()
        {
            StartCoroutine(DisplayTestingMoves(0.5f));
        }
        public IEnumerator DisplayTestingMoves(List<int> data, float timePerMatrix)
        {
            Color dark = new Color(0, 100, 100);
            Color light = new Color(0, 150, 150);
            foreach (int square in data)
            {
                tiles[square % 8, square / 8].GetComponent<SpriteRenderer>().color = ((square % 8) + (square / 8) % 2 != 0) ? light : dark;
            }
            yield return new WaitForSecondsRealtime(timePerMatrix);
        }

        // knight movement testing
        public IEnumerator DisplayTestingMoves(float timePerMatrix)
        {
            foreach (int[] arr in MovementLookup.knightMovesIndexes)
            {
                StartCoroutine(WaitThenResetTiles(arr, timePerMatrix));
                yield return new WaitForSecondsRealtime(timePerMatrix);
            }
        }
        // knight movement testing was very successful and yielded the result that I wasn't checking if knight movements overlapped, just if they fit on the board.
        public IEnumerator WaitThenResetTiles(int[] squares, float t)
        {
            foreach (int square in squares)
            {
                tiles[square % 8, square / 8].GetComponent<SpriteRenderer>().color = new Color(0, 0, 255);
            }
            yield return new WaitForSecondsRealtime(t);
            ResetSquares();
        }

        public void HighlightSquaresForMovementLookupTesting(List<ulong> data)
        {
            StartCoroutine(DisplayTestingMatrices(data, 0.3f)); // starts coroutine to display the data
        }

        public IEnumerator DisplayTestingMatrices(List<ulong> data, float timePerMatrix)
        {
            foreach (ulong matrix in data)
            {
                StartCoroutine(WaitThenResetTiles(matrix, timePerMatrix));
                yield return new WaitForSecondsRealtime(timePerMatrix);
            }
        }

        public IEnumerator WaitThenResetTiles(ulong matrix, float t)
        {
            for (int i = 0; i < 64; i++)
            {
                if (((matrix >> i) & 1ul) == 1ul)
                {
                    tiles[i % 8, i / 8].GetComponent<SpriteRenderer>().color = new Color(0, 0, 255);
                }
            }
            yield return new WaitForSecondsRealtime(t);
            ResetSquares();
        }

    }
}

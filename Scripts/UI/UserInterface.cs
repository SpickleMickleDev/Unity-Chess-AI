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
        Material lightHighlightMat;
        Material darkHighlightMat;
        Material lightMovesMat;
        Material darkMovesMat;

        bool boardInstantiated = false;


        private void CreateBoard()
        {
            tiles = new GameObject[8, 8];
            pieceObjects = new SpriteRenderer[8, 8];
            GameObject blackReferenceTile = (GameObject)Instantiate(Resources.Load("blacktile"));
            GameObject whiteReferenceTile = (GameObject)Instantiate(Resources.Load("whitetile"));

            lightHighlightMat = (Material)Resources.Load("lightHighlight");
            darkHighlightMat = (Material)Resources.Load("darkHighlight");
            lightMovesMat = (Material)Resources.Load("lightMovesMat");
            darkMovesMat = (Material)Resources.Load("darkMovesMat");

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    tiles[col, row] = (GameObject)Instantiate(((row + col) % 2 != 0) ? whiteReferenceTile : blackReferenceTile, GameObject.Find("Tiles").transform); // Discovered that it didn't like the calling of the MonoBehaviour transform variable since I was instantiating a new UserInterface within GameManager so it hadn't declared a transform value.
                    tiles[col, row].GetComponent<SpriteRenderer>().sortingOrder = 1;
                    tiles[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);
                    tiles[col, row].transform.localScale = new Vector2(1, 1);
                    tiles[col, row].name = $"{"abcdefgh"[col]}{row+1}";
                    
                    pieceObjects[col, row] = new GameObject().AddComponent<SpriteRenderer>();
                    pieceObjects[col, row].transform.parent = tiles[col, row].transform;
                    pieceObjects[col, row].GetComponent<SpriteRenderer>().sortingOrder = 2;
                    pieceObjects[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);
                    pieceObjects[col, row].transform.localScale = new Vector2(0.75f, 0.75f);
                }
            }
            
            Destroy(blackReferenceTile);
            Destroy(whiteReferenceTile);

            // knight movement testing
            //HighlightSquaresForMoveGenerationTesting();
        }
        
        public void ChangeBoardFromMove(Board board, Move move, int playerTurn)
        {
            if (playerTurn == 0) // was it the computer making the move           -- Reason that it is playerTurn == 0 (the player's colour) is because the move was made on the board just before this, so it has already flipped to the next person's move, however this is for animating the move that was just played.
            {
                StartCoroutine(AnimateComputerMove(board, move));
            }
            else
            {
                // is done in the computer move animation coroutine after the animation is finished, so not needing to repeat it here.
                RedrawPieces(board);
                ResetSquares();
            }
            //HighlightSquare(move.initialSquare, lightHighlightMat.color, darkHighlightMat.color);
            //HighlightSquare(move.targetSquare, lightHighlightMat.color, darkHighlightMat.color);
        }

        public void SelectSquare(int squareIndex)
        {
            HighlightSquare(squareIndex, lightHighlightMat.color, darkHighlightMat.color);
        }

        public void RedrawPieces(Board board) // can't find a way to access the board globally since no way to reference the GameManager (Unless maybe searching for the game object containing the game manager and selecting its component, but this way is much easier)
            // Also won't allow me to get the board 
        {

            // I found that it would try to draw the grid before it had even initialised the board tiles or pieces even though the order of initialising them first made sense to me.
            // As a result, I decided to restructure how the CreateBoard function would be called, such that it would instantiate before the board would be updated.
            if (!boardInstantiated)
            {
                CreateBoard();
                boardInstantiated = true;
            }
            
            //BoardTest boardTest = new BoardTest(board.boardState);
            spriteHandler = ScriptableObject.CreateInstance<PieceSpriteHandler>();

            for (int row = 0; row < 8; row++)
            {
                for (int col = 0; col < 8; col++)
                {
                    pieceObjects[col, row].transform.position = new Vector3(col - 3.5f, row - 3.5f, 50f);
                    //pieceObjects[col, row].name = boardTest.GetPieceDisplay(board.boardState[col + (row * 8)]);
                    pieceObjects[col, row].sprite = spriteHandler.GetSpriteFromPieceType(board.boardState[col + (row * 8)]);
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
            const float timeDuration = 0.2f;
            int initialSquare = move.initialSquare;
            int targetSquare = move.targetSquare;

            SpriteRenderer piece = pieceObjects[initialSquare % 8, initialSquare / 8];
            piece.sortingOrder = 3;

            Vector2 initialCoords = new Vector2((initialSquare % 8) - 3.5f, (initialSquare / 8) - 3.5f);
            Vector2 targetCoords = new Vector2((targetSquare % 8) - 3.5f, (targetSquare / 8) - 3.5f);



            while (t <= 1f)
            {
                yield return null;
                t += Time.deltaTime / timeDuration;
                piece.transform.position = Vector2.Lerp(initialCoords, targetCoords, t);
            }

            // make move here since then the board resets to its static position after the animation has been finished.
            RedrawPieces(board);
            ResetSquares();

            piece.transform.position = initialCoords;
            piece.sortingOrder = 2;
            // need to return renderer to original position afterwards since they just carry the pieces temporarily and it isn't the piece actually moving.
            // Best to do this after updating the board's pieces since the square being reset will be empty anyway, avoiding the piece visually glitching back after the computer makes a move.

        }

        public void DragPiece(Coord pieceCoord, Vector2 mousePos)
        {
            pieceObjects[pieceCoord.column, pieceCoord.row].transform.position = mousePos;
            pieceObjects[pieceCoord.column, pieceCoord.row].sortingOrder = 3; // lifted higher than other pieces for visibility of dragging above the board, piece is reset back to normal sorting order in RedrawPieces which will be called after the move is attempted
        }

        public void HighlightSquareSelection(Board board, int initialSquare)
        {
            movesGenerator = new LegalMovesGenerator(board);
            List<Move> moves = new List<Move>();
            moves = movesGenerator.GenerateMoves();
            HighlightSquare(initialSquare, lightMovesMat.color, darkMovesMat.color);
            foreach (Move mv in moves)
            {
                if (mv.initialSquare == initialSquare)
                {
                    HighlightSquare(mv.targetSquare, lightMovesMat.color, darkMovesMat.color);
                }
            }
        }
        
        private void HighlightSquare(int squareIndex, Color lightColour, Color darkColour)
        {
            int row = squareIndex / 8;
            int col = squareIndex % 8;
            Color colour = ((row + col) % 2 != 0) ? lightColour : darkColour;//new Color(246,246,105) : new Color(186,202,43);
            // it won't change the colour when I try to change the color property of the sprite renderer, however for some reason it works in the resetSquares function, so I will try to take a similar approach to that.
            tiles[col, row].GetComponent<SpriteRenderer>().color = colour;
        }

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
            Debug.Log("Started second coroutine");
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

        public void ResetSquares()
        {
            GameObject blackReferenceTile = (GameObject)Instantiate(Resources.Load("blacktile"));
            GameObject whiteReferenceTile = (GameObject)Instantiate(Resources.Load("whitetile"));
            Color blackColour = blackReferenceTile.GetComponent<SpriteRenderer>().color;
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



        // https://docs.unity3d.com/ScriptReference/Input.GetMouseButtonDown.html
        // Unity documentation for mouse input handling


        // DID PREVIOUSLY HANDLE MOUSE HANDLING HERE, HOWEVER COULDN'T ACCESS BOARD SO NOW MOVED TO A HUMAN PLAYER WHICH DOESN'T DERIVE FROM MONOBEHAVIOUR

        /*

        // MOUSE INPUT HANDLING SECTION

        bool isSquareSelected = false;
        bool selectedOnLastUpdate = false;
        bool dragging = false;
        Coord selectedSquare;
        Camera camera;

        private void Start()
        {
            camera = Camera.main;
        }

        // Different input types:
        // - Select one square
        //   - Select second square to move piece
        // - Drag piece

        // After dragging piece, check if move legal
        // - If false, keep selected square but cancel move
        // - If true, make move and remove selected square

        // After selecting piece, if different piece selected before then check if move legal
        // - If false, remove selected square and cancel move
        // - If true, make move and remove selected square


        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (isSquareSelected)
                {
                    if (IsMouseOnBoard())
                    {
                        MoveBySelection(selectedSquare, GetSquareAtMousePos());
                    }
                }
                if (IsMouseOnBoard())
                {
                    isSquareSelected = true;
                    selectedSquare = GetSquareAtMousePos();
                }
                else
                {
                    isSquareSelected = false;
                }
            }
            else if (Input.GetMouseButton(0))
            {
                dragging = true;
                UpdateDraggingAnimation();
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (dragging)
                {
                    if (IsMouseOnBoard())
                    {
                        ReleaseDrag(selectedSquare, GetSquareAtMousePos());
                    }
                }
                dragging = false;
                isSquareSelected = false;
            }

            /*
            // deselect everything if mouse is pressed off board 
            if (Input.GetMouseButton(0)) // if mouse is down
            {
                Debug.Log($"Mouse down\n{isSquareSelected}\n{selectedOnLastUpdate}\n{dragging}\n{selectedSquare.column},{selectedSquare.row}");
                if (IsMouseOnBoard())
                {
                    if (isSquareSelected) // if square already selected
                    {
                        if (selectedOnLastUpdate) // if player was holding down input last frame and they are holding it now then we can know they are dragging it
                        {
                            dragging = true;
                            UpdateDraggingAnimation();
                        }
                        else // if  player just selected another square
                        {
                            if (IsMouseOnBoard())
                            {
                                Coord targetSquare = GetSquareAtMousePos();
                                MoveBySelection(selectedSquare, targetSquare);

                            }
                        }
                        selectedOnLastUpdate = false;
                    }
                    else // first time selecting
                    {
                        Debug.Log("First time selecting");
                        selectedOnLastUpdate = true;
                        isSquareSelected = true;
                        selectedSquare = GetSquareAtMousePos();

                    }
                }
                else // if mouse isn't on board then deselect piece
                {
                    if (!dragging)
                    {
                        isSquareSelected = false;
                    }
                }

            }
            else
            {
                if (dragging)
                {
                    Coord targetSquare = GetSquareAtMousePos();
                    if (!targetSquare.Equals(selectedSquare))
                    {
                        ReleaseDrag(selectedSquare, targetSquare);
                    }
                }
                dragging = false;
                selectedOnLastUpdate = false;
            }
        }

        void UpdateDraggingAnimation()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            pieceObjects[selectedSquare.column, selectedSquare.row].transform.position = mousePos;
            pieceObjects[selectedSquare.column, selectedSquare.row].sortingOrder = 3;

        }



        // Tried making GetSquareAtMousePos function type 'Coord?' as microsoft said that makes the value nullable but didn't work 
        // Therefore resorting to checking if mouse is within board in separate function
        bool IsMouseOnBoard()
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    return true;
                }
            }
            return false;
        }

        // to do:
        // make select move to another square functional
        // make 'move' class
        // make procedure to highlight squares / move
        // make human player or something to allow for referencing the board instance and the tiles / pieces
        // make move generator
        // make legal vs illegal moves actually play on the board
        // make moves change the board state and redraw the pieces
        // handle the checking winning of the game etc.
        // make AI stuff

        Coord GetSquareAtMousePos() 
        {
            Vector2 mousePos = camera.ScreenToWorldPoint(Input.mousePosition);
            
            // positions go from 0 - 3.5f to 7 - 3.5f with width 1 and centre as pivot
            // Therefore grid ranges from:
            // -4f -> +4f
            // If I add 4f then index is fetchable as values range from 0 to 8f.
            // Index is position + 4f rounded down / truncated
            if (Math.Abs(mousePos.x) < 4f)
            {
                if (Math.Abs(mousePos.y) < 4f)
                {
                    // Truncating the x and y coordinates + 4 requires lots of conversion too decimal, int etc.
                    // Therefore adding 3.5f and rounding it to nearest integer as functions the same but more convenient / efficient
                    int xCoord = (int)Math.Round(mousePos.x + 3.5f);
                    int yCoord = (int)Math.Round(mousePos.y + 3.5f);

                    return new Coord(xCoord, yCoord);
                }
            }
            return new Coord();
        }

        void ReleaseDrag(Coord initialSquare, Coord targetSquare)
        {
            if (!initialSquare.Equals(targetSquare))
            {
                Debug.Log($"{initialSquare.CoordAsString()}->{targetSquare.CoordAsString()}");
                TryMakeMove(initialSquare, targetSquare);
            }
            ReturnSpriteToOrigin(initialSquare);
        }

        void ReturnSpriteToOrigin(Coord square)
        {
            pieceObjects[square.column, square.row].transform.position = square.CoordToWorldCoordinates();
            pieceObjects[square.column, square.row].sortingOrder = 2;
        }

        void MoveBySelection(Coord initialSquare, Coord targetSquare)
        {
            // Testing, not actually meant to move piece. pieceObjects[initialSquare.column, initialSquare.row].transform.position = targetSquare.CoordToWorldCoordinates();
        }

        void TryMakeMove(Coord initialSquare, Coord targetSquare)
        {
            Board newboard = new Board();
            newboard.Setup();
            LegalMovesGenerator moveGenerator = new LegalMovesGenerator(newboard);
            List<Move> moves = moveGenerator.GenerateMoves(initialSquare);
            Debug.Log(moves.Count);
            Debug.Log(new Coord(moves[0].initialSquare).CoordAsString());
            Debug.Log(new Coord(moves[0].targetSquare).CoordAsString());
            Debug.Log(moves[0].targetSquare);
            bool moveLegal = false;
            foreach (Move mv in moves)
            {
                if (mv.targetSquare == targetSquare.CoordAsGridNum())
                {
                    moveLegal = true;
                }
            }
            if (moveLegal)
            {
                pieceObjects[targetSquare.column, targetSquare.row].transform.position = targetSquare.CoordToWorldCoordinates();
            }

            // try make move on board, if successful then update original board and redraw pieces
        }*/
    }
}

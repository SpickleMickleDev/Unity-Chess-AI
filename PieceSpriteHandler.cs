using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    //https://gamedevbeginner.com/scriptable-objects-in-unity
    //https://www.youtube.com/watch?v=yk-zpy2txsQ
    // Was hard to understand the niches around scriptable objects, however these references helped to teach me how to set one up.

    // Couldn't retrieve the piece sprites from within the User Interface, so had to store it elsewhere
    // Using scriptable object to save on memory as it only stores one copy of the sprites, however knew nothing about them so finding out how to actually reference it and set it up was a challenge.
    [CreateAssetMenu(menuName = "Resources/SpriteHandler")]
    public class PieceSpriteHandler : ScriptableObject
    {
        //https://docs.unity3d.com/ScriptReference/Serializable.html
        [System.Serializable]
        public struct Pieces
        {
            public Sprite Pawn, Rook, Knight, Bishop, Queen, King;
        }

        private Pieces WhitePieces;
        private Pieces BlackPieces;
        
        private void Awake() // as soon as the program starts, retrieves the pieces' sprites
        {
            WhitePieces.Pawn = Resources.Load<Sprite>("Pieces/WPawn");
            WhitePieces.Rook = Resources.Load<Sprite>("Pieces/WRook");
            WhitePieces.Knight = Resources.Load<Sprite>("Pieces/WKnight");
            WhitePieces.Bishop = Resources.Load<Sprite>("Pieces/WBishop");
            WhitePieces.Queen = Resources.Load<Sprite>("Pieces/WQueen");
            WhitePieces.King = Resources.Load<Sprite>("Pieces/WKing");
            BlackPieces.Pawn = Resources.Load<Sprite>("Pieces/BPawn");
            BlackPieces.Rook = Resources.Load<Sprite>("Pieces/BRook");
            BlackPieces.Knight = Resources.Load<Sprite>("Pieces/BKnight");
            BlackPieces.Bishop = Resources.Load<Sprite>("Pieces/BBishop");
            BlackPieces.Queen = Resources.Load<Sprite>("Pieces/BQueen");
            BlackPieces.King = Resources.Load<Sprite>("Pieces/BKing");
        }

        public Sprite GetSpriteFromPieceType(int piece) // returns the sprite corresponding to each piece's colour and type
        {
            int pieceType = Piece.GetPieceType(piece);
            Pieces spriteSet = (Piece.IsPieceWhite(piece)) ? WhitePieces : BlackPieces; // gets the colour's set of sprites
            
            switch (pieceType) // returns the piece type of that colour's sprite
            {
                case Piece.pawn:
                    return spriteSet.Pawn;
                case Piece.rook:
                    return spriteSet.Rook;
                case Piece.knight:
                    return spriteSet.Knight;
                case Piece.bishop:
                    return spriteSet.Bishop;
                case Piece.queen:
                    return spriteSet.Queen;
                case Piece.king:
                    return spriteSet.King;
                default:
                    return null;
            }
        }
    }
}

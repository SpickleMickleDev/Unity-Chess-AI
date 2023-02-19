using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ChessAI
{
    //https://gamedevbeginner.com/scriptable-objects-in-unity/#:~:text=To%20create%20a%20scriptable%20object%20in%20your%20project%2C%20you'll,instances%20from%20the%20Create%20Menu.&text=A%20lot%20of%20the%20time,all%20you%20need%20to%20do.
    //https://www.youtube.com/watch?v=yk-zpy2txsQ
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
        
        private void Awake()
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

        public Sprite GetSpriteFromPieceType(int piece)
        {
            int pieceType = Piece.GetPieceType(piece);
            Pieces spriteSet = (Piece.IsPieceWhite(piece)) ? WhitePieces : BlackPieces;
            

            switch (pieceType)
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

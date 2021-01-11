﻿using System.Collections.Generic;

namespace ChessGame
{
    /// <summary>
    /// The color of a piece.
    /// </summary>
    public enum TeamColor
    {
        Black,
        White
    }

    public class Piece
    {
        public bool hasMoved;
        /// <summary>
        /// The character used to notate the piece in algebraic notation.
        /// </summary>
        public char Notation { get; protected set; }
        /// <summary>
        /// Whether a piece is White or Black.
        /// </summary>
        public TeamColor Color { get; set; }
        /// <summary>
        /// The different movement patterns the piece uses.
        /// </summary>
        public readonly int MaterialValue;
        protected IMovementPattern[] MovementPatternList;

        /// <summary>
        /// Returns enumerable of all available moves of a given piece.
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public IEnumerable<Move> GetMoves(Board board)
        {
            if (MovementPatternList is null)
            {
                yield break;
            }

            if (!board.TryGetCoordinate(this, out Coordinate position))
            { 
                yield break;
            }

            foreach (var item in MovementPatternList) 
            {
                foreach (var move in item.GetMoves(this, position, board))
                {
                    yield return move;
                }
            }
        }

        public override string ToString() => Notation.ToString();
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using ChessGame.Pieces;

namespace ChessGame
{
    /// <summary>
    /// A class that describes a game of chess.
    /// </summary>
    public class Chessboard
    {
        public enum GameState
        {
            InProgress,
            Stalemate,
            Checkmate
        }

        public readonly int Height;
        public readonly int Width;
        public readonly Dictionary<Coordinate, Piece> Pieces;
        /// <summary>
        /// Describes intersection squares.
        /// </summary>
        public readonly Dictionary<Coordinate, List<Piece>> Dangerzone;
        public readonly Stack<Move> Moves;
        public readonly HashSet<Piece> MovedPieces;
        public readonly Player PlayerWhite;
        public readonly Player PlayerBlack;
        public bool isGameInProgress;
        public Player Winner;

        public event Action onKingChecked;
        public event Action<GameState> onGameStateUpdated;

        /// <summary>
        /// True if the king, who's turn it is, is in check.
        /// </summary>
        private bool isKingChecked;
        private readonly Gamemode gamemode;
        public TeamColor CurrentTurn { get; private set; } // changes on next turn start
        public Player CurrentPlayerTurn
        {
            get
            {
                return CurrentTurn == TeamColor.White ? PlayerWhite : PlayerBlack;
            }
        }
        public int MaterialSum
        {
            get
            {
                return Pieces.Values.Sum(p => p.Color == TeamColor.Black ? -p.MaterialValue : p.MaterialValue);
            }
        }

        /// <summary>
        /// Makes a copy of <c>board</c>, player references stay the same.
        /// </summary>
        /// <param name="board"></param>
        public Chessboard(Chessboard board)
        {
            Height = board.Height;
            Width = board.Width;
            Pieces = new Dictionary<Coordinate, Piece>(board.Pieces);
            Dangerzone = new Dictionary<Coordinate, List<Piece>>(board.Dangerzone);
            CurrentTurn = board.CurrentTurn;
            Moves = new Stack<Move>(board.Moves);
            PlayerBlack = board.PlayerBlack;
            PlayerWhite = board.PlayerWhite;
            gamemode = board.gamemode;
            isKingChecked = board.isKingChecked;
            MovedPieces = board.MovedPieces;
        }

        public Chessboard(int width, int height, Gamemode gamemode, Player playerWhite, Player playerBlack)
        {
            Width = width;
            Height = height;
            Pieces = new Dictionary<Coordinate, Piece>();
            Dangerzone = new Dictionary<Coordinate, List<Piece>>();
            Moves = new Stack<Move>();
            this.gamemode = gamemode;
            PlayerWhite = playerWhite;
            PlayerBlack = playerBlack;
            CurrentTurn = TeamColor.Black;
            MovedPieces = new HashSet<Piece>();
        }

        /// <summary>
        /// Starts a game by calling <c>StartNextTurn</c>.
        /// </summary>
        public void StartGame()
        {
            isGameInProgress = true;
            StartNextTurn();
        }

        /// <summary>
        /// Makes a move based on move notation. Calls <c>MakeMove(Move)</c> once move is translated.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public bool PerformMove(string move)
        {
            Move pieceMove = GetMoveByNotation(move, CurrentTurn);

            if (pieceMove is null)
                return false;

            return PerformMove(pieceMove);
        }

        /// <summary>
        /// Updates chessboard state based on move, adds the move to the move stack, and calls <c>StartNextTurn</c>.
        /// </summary>
        /// <param name="move"></param>
        /// <returns></returns>
        public bool PerformMove(Move move)
        {
            // make the actual move change the chessboard state.
            ExecuteMove(move);
            // add the move to the list of moves.
            Moves.Push(move);

            StartNextTurn();

            return true;
        }

        /// <summary>
        /// Refreshes dangerzone, updates turn, checks for check.
        /// </summary>
        public void StartNextTurn()
        {
            // refresh dangersquares
            UpdateDangerzones();

            Player previousPlayer = CurrentPlayerTurn;

            // change turn
            CurrentTurn = CurrentTurn == TeamColor.Black ? TeamColor.White : TeamColor.Black;

            // check for whether king is in check.
            if (IsKingInCheck(CurrentTurn))
            {
                isKingChecked = true;
                onKingChecked?.Invoke();
            }
            else
            {
                isKingChecked = false;
            }

            // no more legal moves, game is over either by stalemate or checkmate.
            if (!GetMoves(CurrentTurn).Any())
            {
                isGameInProgress = false;
                GameState gameState;

                // checkmate
                if (isKingChecked)
                {
                    Winner = previousPlayer;
                    gameState = GameState.Checkmate;
                }
                else
                {
                    gameState = GameState.Stalemate;
                }

                onGameStateUpdated?.Invoke(gameState);
                return;
            }

            CurrentPlayerTurn.TurnStarted(this);
        }

        public IEnumerable<Move> GetMoves(TeamColor teamColor)
        {
            foreach (var piece in Pieces.Values.ToList())
            {
                if (piece.Color != teamColor)
                {
                    continue;
                }

                foreach (var move in piece.GetMoves(this))
                {
                    if (!gamemode.ValidateMove(move, this))
                    {
                        continue;
                    }

                    yield return move;
                }
            }
        }

        /// <summary>
        /// Executes a move by updating the pieces accordingly.
        /// </summary>
        /// <param name="move"></param>
        public void ExecuteMove(Move move)
        {
            if (move is null)
            {
                return;
            }

            foreach (var singleMove in move.Moves)
            {
                if (singleMove.Captures)
                {
                    Pieces.Remove(singleMove.Destination);
                }

                try
                {
                    Pieces.Remove(Pieces.First(piece => piece.Value == singleMove.Piece).Key);
                }
                catch (InvalidOperationException)
                {
                    // Piece doesn't exist already, doesn't matter.
                }

                Pieces[singleMove.Destination] = singleMove.Piece;

                MovedPieces.Add(singleMove.Piece);
            }
        }

        public bool IsKingInCheck(TeamColor color)
        {
            Piece king = null;
            
            foreach (var item in GetPieces<King>())
            {
                if (item.Color != color)
                {
                    continue;
                }

                king = item;
            }

            if (king is null)
            {
                throw new ChessExceptions.KingNotFoundChessException();
            }

            Coordinate position = GetCoordinate(king);

            if (Dangerzone.TryGetValue(position, out List<Piece> pieces))
            {
                // returns true if any of the pieces are of a different color.
                return pieces.Any(p => p.Color != color);
            }
            else
            {
                return false;
            }
        }

        // GetMoves
        /// <summary>
        /// Translates algebraic notation into move.
        /// </summary>
        /// <param name="notation"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public Move GetMoveByNotation(string notation, TeamColor player)
        {
            char pieceNotation = ' ';
            char promotionTarget;
            string customNotation = null;
            Coordinate destination = new Coordinate();

            try
            {
                // read destination square
                int lastNumberIndex = 0;
                for (int i = 0; i < notation.Length; i++)
                {
                    if (char.IsDigit(notation[i]))
                    {
                        lastNumberIndex = i;
                    }
                }

                destination = new Coordinate(notation.Substring(lastNumberIndex - 1, 2));

                // more at the end
                if (notation.Length - 1 > lastNumberIndex)
                {
                    // promotion
                    if (notation[lastNumberIndex + 1] == '=')
                    {
                        promotionTarget = notation[lastNumberIndex + 2];
                        pieceNotation = '\0';
                    }
                }

                if (lastNumberIndex - 1 == 0)
                {
                    pieceNotation = '\0';
                }
                else
                {
                    pieceNotation = notation[0];

                    if (notation[lastNumberIndex - 2] == 'x')
                    {

                    }
                }

                

                /*

                // pawn move
                if (char.IsDigit(notation[1]))
                {
                    pieceNotation = '\0';
                    destination = new Coordinate(notation);
                }
                else if (char.IsDigit(notation[2]))
                {
                    pieceNotation = notation[0];
                    destination = new Coordinate(notation.Substring(1));
                }
                else
                {
                    customNotation = notation;
                }*/
            }catch (Exception)
            {
                customNotation = notation;
            }
            

            foreach (var piece in Pieces)
            {
                if (piece.Value.Color != player)
                    continue;

                if (piece.Value.Notation != pieceNotation && customNotation is null)
                    continue;

                foreach (var targetMove in piece.Value.GetMoves(this))
                {
                    if (!gamemode.ValidateMove(targetMove, this))
                    {
                        // Rules of algebraic notation states that invalid moves
                        // should not be included when trying to disambiguate moves.
                        continue;
                    }

                    if (customNotation == targetMove.ToString())
                    {
                        return targetMove;
                    }

                    foreach (var singleMove in targetMove.Moves)
                    {
                        if (singleMove.Piece != piece.Value)
                            continue;

                        if (singleMove.Destination != destination)
                            continue;

                        return targetMove;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Returns count of how many pieces with <c>color</c> aiming on square.
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public int IsDangerSquare(Coordinate position, TeamColor color)
        {
            if (Dangerzone.TryGetValue(position, out List<Piece> pieces))
            {
                return pieces is null ? 0 : pieces.Count(p => p.Color == color);
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Removes old references of piece, and adds new.
        /// </summary>
        /// <param name="piece"></param>
        private void UpdateDangerzones(Piece piece)
        {
            // remove all instances
            foreach (var item in Dangerzone)
            {
                if (item.Value is null)
                {
                    continue;
                }

                item.Value.Remove(piece);
            }

            // update dangersquares.
            foreach (var move in piece.GetMoves(this, true))
            {
                foreach (var singleMove in move.Moves)
                {
                    if (!Dangerzone.ContainsKey(singleMove.Destination))
                    {
                        Dangerzone[singleMove.Destination] = new List<Piece>();
                    }

                    Dangerzone[singleMove.Destination].Add(singleMove.Piece);
                }
            }
        }

        public void UpdateDangerzones()
        {
            Dangerzone.Clear();

            foreach (var piece in Pieces)
            {
                UpdateDangerzones(piece.Value);
            }
        }

        /// <summary>
        /// Gets piece by position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Piece this[Coordinate position]
        {
            get { return GetPiece(position); }
            set { Pieces[position] = value; }
        }

        public Coordinate GetCoordinate(Piece piece) => Pieces.FirstOrDefault(p => p.Value == piece).Key;

        public bool TryGetCoordinate(Piece piece, out Coordinate position)
        {
            try
            {
                position = Pieces.FirstOrDefault(x => x.Value == piece).Key;
                return true;

            }
            catch (ArgumentNullException)
            {
                position = new Coordinate();
                return false;
            }
        }

        public Piece GetPiece(Coordinate position)
        {
            if (Pieces.TryGetValue(position, out Piece piece))
                return piece;

            return null;
        }

        public List<Piece> GetPieces<T>() => (from piece in Pieces.Values
                                         where piece is T
                                         select piece).ToList();
    }
}
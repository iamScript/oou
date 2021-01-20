﻿using System.Collections.Generic;
using System.Linq;

namespace ChessGame.Bots
{
    public class SimpletronBot : Chessbot
    {
        /*
        private List<(int, Move)> CheckMovesRecursive(Chessboard board, TeamColor color, int depth, List<Move> moveStack = null)
        {
            List<(int, Move)> moves = new List<(int, Move)>();

            foreach (var move in board.GetMoves(color))
            {
                Chessboard boardSimulation = new Chessboard(board);
                boardSimulation.ExecuteMove(move);

                if (depth == 0)
                {
                    moves.Add((boardSimulation.MaterialSum, moveStack[0]));
                }
                else
                {
                    if (moveStack is null)
                    {
                        moveStack = new List<Move>();
                    }

                    moveStack.Add(move);

                    
                    moves.InsertRange(0, CheckMovesRecursive(boardSimulation, GetOppositeColor(color), depth - 1, moveStack));
                }
            }

            return moves;
        }*/

        private List<(int, Move)> CheckMoves4Deep(Chessboard board)
        {
            List<(int, Move)> moves = new List<(int, Move)>();
            Chessboard boardInstance = new Chessboard(board);

            TeamColor botColor = boardInstance.CurrentTurn;
            TeamColor enemyColor = (TeamColor)(((int)boardInstance.CurrentTurn + 1) % 2);

            foreach (var mainBotMove in boardInstance.GetMoves(boardInstance.CurrentTurn))
            {
                Chessboard boardMain1 = new Chessboard(boardInstance);
                boardMain1.ExecuteMove(mainBotMove);

                foreach (var enemyResponse in boardMain1.GetMoves(enemyColor))
                {
                    Chessboard boardMain2 = new Chessboard(boardMain1);
                    boardMain2.ExecuteMove(enemyResponse);

                    foreach (var secondBotMove in boardMain2.GetMoves(botColor))
                    {
                        Chessboard boardMain3 = new Chessboard(boardMain2);
                        boardMain3.ExecuteMove(secondBotMove);

                        boardMain3.ExecuteMove(FindBestMoves(boardMain3, enemyColor).First());
                        moves.Add((boardMain3.MaterialSum, mainBotMove));
                    }
                }

                /*
                foreach (var enemyResponseMove in board.GetMoves(board.CurrentTurn))
                {
                    Chessboard boardSecond = new Chessboard(boardMain);
                    boardSecond.ExecuteMove(enemyResponseMove);


                }*/
            }

            return moves;
        }

        protected override Move GenerateMove(Chessboard board)
        {

            List<(int, Move)> longerMoves = CheckMoves4Deep(board);

            List<(int, Move)> sortedMoves = longerMoves.OrderByDescending(material => material.Item1).ToList();

            int newLuckyNumber;
            if (board.CurrentTurn == TeamColor.Black)
            {
                newLuckyNumber = sortedMoves[sortedMoves.Count - 1].Item1;
            }
            else
            {
                newLuckyNumber = sortedMoves[0].Item1;
            }

            List<Move> viableMoves = (from singleMove in sortedMoves 
                                     where singleMove.Item1 == newLuckyNumber 
                                     select singleMove.Item2).ToList();

            foreach (var item in viableMoves)
            {
                if (!board.MovedPieces.Contains(item.Moves[0].Piece))
                {
                    return item;
                }
            }

            return viableMoves[0];
        }

        private List<Move> FindBestMoves(Chessboard board, TeamColor teamColor)
        {
            List<(int, Move)> moves = new List<(int, Move)>();

            foreach (var move in board.GetMoves(teamColor))
            {
                Chessboard boardCheck = new Chessboard(board);
                boardCheck.ExecuteMove(move);
                moves.Add((boardCheck.MaterialSum, move));
            }

            if (moves.Count == 0)
            {
                return null;
            }

            List<(int, Move)> sortedMoves = moves.OrderByDescending(material => material.Item1).ToList();

            int luckyNumber;

            if (teamColor == TeamColor.Black)
            {
                luckyNumber = sortedMoves[sortedMoves.Count - 1].Item1;
            }
            else
            {
                luckyNumber = sortedMoves[0].Item1;
            }

            return (from move in sortedMoves where move.Item1 == luckyNumber select move.Item2).ToList();
        }
    }
}

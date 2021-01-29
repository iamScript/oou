﻿using ChessGame;
using ChessGame.Pieces;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChessForms
{
    public partial class BoardDisplay : Form
    {
        private TilePictureControl[,] boardcells;
        private Coordinate? fromPosition = null;
        private Piece selectedPiece;
        private readonly Gamemode gamemode;
        private Chessboard chessboard;
        private readonly bool whiteLocal, blackLocal;
        private bool unFlipped = false;

        public BoardDisplay(Gamemode gamemode, bool whiteLocal, bool blackLocal)
        {
            InitializeComponent();

            this.gamemode = gamemode;
            this.whiteLocal = whiteLocal;
            this.blackLocal = blackLocal;

            // flip board if black is only local player
            //unFlipped = !(blackLocal && !whiteLocal);
        }

        private void BoardDisplay_Load(object sender, EventArgs e)
        {
            gamemode.onTurnChanged += onTurnStarted;
            gamemode.onGameStateUpdated += onGameStateUpdated;

            chessboard = gamemode.GenerateBoard();
            InstantiateUIBoard();

            UpdateBoard();

            Task.Run(() => chessboard.StartGame());
        }

        private void onGameStateUpdated(GameState e)
        {
            string outputMsg = string.Empty;
            switch (e)
            {
                case GameState.Stalemate:
                    outputMsg = "The game has stalemated, Game is over";
                    break;
                case GameState.Checkmate:
                    outputMsg = $"{gamemode.Winner} has delivered checkmate!";
                    break;
                case GameState.Check:
                    outputMsg = $"{chessboard.CurrentPlayerTurn} is in check!";
                    break;
            } 

            if (outputMsg == string.Empty)
            {
                return;
            }

            Invoke((MethodInvoker)delegate
            {
                Text = outputMsg;
            });
        }

        private void onTurnStarted()
        {
            Invoke((MethodInvoker)delegate
           {
               Text = $"{chessboard.CurrentPlayerTurn}'s turn";
           });

            if (MatchMaker.PlaySoundOnMove)
            {
                Console.Beep();
            }
            UpdateBoard();
        }

        private void ResetTableStyling()
        {
            for (int y = 0; y < chessboard.Height; y++)
            {
                for (int x = 0; x < chessboard.Width; x++)
                {
                    ResetTileColor(x, y);
                    boardcells[x, y].BorderStyle = BorderStyle.None;
                }
            }
        }

        public void ResetTileColor(int x, int y)
        {
            boardcells[x, y].BackColor = (x % 2) == (y % 2) ? Color.White : Color.CornflowerBlue;
        }

        public void UpdateBoard()
        {
            for (int y = 0; y < chessboard.Height; y++)
            {
                for (int x = 0; x < chessboard.Width; x++)
                {
                    Coordinate pieceCoordinate;

                    if (unFlipped)
                    {
                        pieceCoordinate = new Coordinate(chessboard.Width - 1 - x, chessboard.Height - 1 - y); //Det her burde også fikse noget
                    } //#442
                    else
                    {
                        pieceCoordinate = new Coordinate(x, y);
                    }

                    Piece cellPiece = chessboard.GetPiece(pieceCoordinate);

                    if (cellPiece is null)
                    {
                        ClearPiece(x, y);
                    }
                    else
                    {
                        PlacePiece(x, y, cellPiece);
                    }
                }
            }
        }

        public void InstantiateUIBoard()
        {
            tableLayoutPanel1.ColumnCount = chessboard.Width + 1;
            tableLayoutPanel1.ColumnStyles.Clear();
            int i;
            for (i = 0; i < chessboard.Width; i++)
            {
                // set size to any percent, doesnt matter
                tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            }

            tableLayoutPanel1.RowCount = chessboard.Height + 1;
            tableLayoutPanel1.RowStyles.Clear();
            for (i = 0; i < chessboard.Height; i++)
            {
                tableLayoutPanel1.RowStyles.Add(new ColumnStyle(SizeType.Percent, 1));
            }

            // Coordinate row and column
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 25));

            boardcells = new TilePictureControl[chessboard.Width, chessboard.Height];

            for (int y = 0; y < chessboard.Height; y++)
            {
                for (int x = 0; x < chessboard.Width; x++)
                {
                    TilePictureControl box = new TilePictureControl();

                    box.Click += CellClicked;

                    if (unFlipped)
                    {
                        boardcells[chessboard.Width - 1 - x, (chessboard.Height - 1) - y] = box;
                    } //#442
                    else
                    {
                        boardcells[x, y] = box;
                    }

                    tableLayoutPanel1.Controls.Add(box, x, y);
                }
            }

            Font labelFont = new Font("ariel", 15, FontStyle.Bold);
            // Instantiate coordinates
            for (int x = 0; x < tableLayoutPanel1.ColumnCount - 1; x++)
            {
                Label label = new Label
                {
                    Text = ((char)(65 + x)).ToString(),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = labelFont
                };

                tableLayoutPanel1.Controls.Add(label, x, tableLayoutPanel1.RowCount - 1);
            }
            for (int y = 0; y < tableLayoutPanel1.RowCount - 1; y++)
            {
                Label label = new Label
                {
                    Text =  (chessboard.Height - y).ToString(),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    Font = labelFont
                };

                tableLayoutPanel1.Controls.Add(label, tableLayoutPanel1.ColumnCount - 1, y);
            }

            ResetTableStyling();
        }

        private void DrawDangerzone()
        {
            foreach (var item in chessboard.Dangerzone)
            {
                if (!chessboard.InsideBoard(item.Key))
                {
                    continue;
                }

                ColorSquare(item.Key.File, item.Key.Rank, Color.Red);
            }
        }

        private void MakeMove(Coordinate from, Coordinate to)
        {
            string move = from.ToString() + to.ToString();

            chessboard.PerformMove(move, MoveNotation.UCI);
            //Thread moveThread = new Thread(() => chessboard.PerformMove(move, MoveNotation.UCI));
            //moveThread.Start();
        }

        private void CellClicked(object sender, EventArgs e)
        {
            MouseButtons button = ((MouseEventArgs)e).Button;

            // translate window coordinates to table-cell coordinates
            Point click = tableLayoutPanel1.PointToClient(MousePosition);
            int windowX = click.X;
            int windowY = click.Y;

            int cellX = windowX / (tableLayoutPanel1.Width / chessboard.Width);
            int cellY = windowY / (tableLayoutPanel1.Height / chessboard.Height);

            if (unFlipped)
            {
                cellY = (chessboard.Height - 1) - cellY;
            }
            else
            {
                //cellX = (chessboard.Width - 1) - cellX; //Det her fikser lidt et problem
            }

            Coordinate clickTarget = new Coordinate(cellX, cellY);

            // handle click
            switch (button)
            {
                case MouseButtons.Left:
                    ResetTableStyling();

                    Piece piece = chessboard[clickTarget];

                    if (!(fromPosition is null) && piece?.Color == chessboard.CurrentTeamTurn && piece != selectedPiece)
                    {
                        DeselectPiece(fromPosition.Value.File, fromPosition.Value.Rank);
                        UpdateBoard();
                        fromPosition = null;
                    }

                    if (fromPosition is null)
                    {
                        // wrong color piece selected
                        if (piece is null || piece.Color != chessboard.CurrentTeamTurn)
                        {
                            return;
                        }

                        // only allow selection of local players
                        if (chessboard.CurrentTeamTurn == TeamColor.Black && !blackLocal ||
                            chessboard.CurrentTeamTurn == TeamColor.White && !whiteLocal)
                        {
                            return;
                        }

                        fromPosition = clickTarget;
                        SelectPiece(cellX, cellY);
                        selectedPiece = piece;

                        foreach (var item in piece.GetMoves(chessboard))
                        {
                            if (item.Moves[0].Destination is null)
                            {
                                continue;
                            }

                            Coordinate guardedSquare = item.Moves[0].Destination.Value;

                            // TODO: Patrick fixer brættet, cirka her omkring
                            Image cellImage = boardcells[guardedSquare.File, guardedSquare.Rank].Image;

                            if (cellImage is null)
                            {
                                boardcells[guardedSquare.File, guardedSquare.Rank].Image = Properties.Resources.MuligtTrækBrik;
                            }
                            else
                            {
                                boardcells[guardedSquare.File, guardedSquare.Rank].BackColor = Color.Red;
                            }
                        }
                    }
                    else
                    {
                        // select target
                        if (clickTarget != fromPosition)
                        {
                            DeselectPiece(cellX, cellY);
                            MakeMove(fromPosition.Value, clickTarget);
                        }

                        UpdateBoard();

                        DeselectPiece(cellX, cellY);
                        selectedPiece = null;
                        fromPosition = null;
                    }
                    break;
                case MouseButtons.None:
                    break;
                case MouseButtons.Right:
                    if (boardcells[cellX, cellY].BackColor == Color.Green)
                    {
                        ResetTileColor(cellX, cellY);
                    }
                    else
                    {
                        boardcells[cellX, cellY].BackColor = Color.Green;
                    }
                    break;
                case MouseButtons.Middle:
                    break;
                case MouseButtons.XButton1:
                    break;
                case MouseButtons.XButton2:
                    break;
                default:
                    break;
            }
        }

        private void DeselectPiece(int x, int y)
        {
            if (unFlipped)
            {
                y = (chessboard.Height - 1) - y;
            }
            else
            {
                //x = (chessboard.Width - 1) - x;
            }

            boardcells[x, y].BorderStyle = BorderStyle.None;
        }

        private void SelectPiece(int x, int y)
        {
            /*if (flipped) //flip fiks
            {
                y = (chessboard.Height - 1) - y;
            }
            else
            {
                x = (chessboard.Width - 1) - x;
            }*/

            boardcells[x, y].BorderStyle = BorderStyle.FixedSingle;
        }

        public void ClearPiece(int x, int y)
        {
            if (unFlipped)
            {
                y = (chessboard.Height - 1) - y;
            }
            else
            {
                //x = (chessboard.Width - 1) - x;
            }

            boardcells[x, y].Image = null;
        }

        public void PlacePiece(int x, int y, Piece piece)
        {
            if (unFlipped)
            {
                y = (chessboard.Height - 1) - y;
            }
            else
            {
                //x = (chessboard.Width - 1) - x;
            }

            boardcells[x, y].Image = GetPieceImage(piece);
        }

        public void ColorSquare(int x, int y, Color color)
        {
            if (unFlipped)
            {
                y = (chessboard.Height - 1) - y;
            }
            else
            {
                //x = (chessboard.Width - 1) - x;
            }

            boardcells[x, y].BackColor = color;
        }

        private Image GetPieceImage(Piece piece)
        {
            if (piece.Color == TeamColor.White)
            {
                switch (piece)
                {
                    case Bishop _:
                        return Properties.Resources.LøberHvid;
                    case King _:
                        return Properties.Resources.KongeHvid;
                    case Pawn _:
                        return Properties.Resources.BondeHvid;
                    case Rook _:
                        return Properties.Resources.TårnHvid;
                    case Queen _:
                        return Properties.Resources.DronningHvid;
                    case Knight _:
                        return Properties.Resources.HestHvid;
                }
            }
            else
            {
                switch (piece)
                {
                    case Bishop _:
                        return Properties.Resources.LøberSort;
                    case King _:
                        return Properties.Resources.KongeSort;
                    case Pawn _:
                        return Properties.Resources.BondeSort;
                    case Rook _:
                        return Properties.Resources.TårnSort;
                    case Queen _:
                        return Properties.Resources.DronningSort;
                    case Knight _:
                        return Properties.Resources.HestSort;
                }
            }


            return null;
        }

        private void BoardDisplay_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                    break;
                case Keys.Right:
                    break;
                case Keys.R:
                    chessboard.UpdateDangerzones();
                    break;
                case Keys.Space:
                    DrawDangerzone();
                    break;
                default:
                    break;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}

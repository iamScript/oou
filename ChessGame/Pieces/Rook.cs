﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessGame.Pieces
{
    public class Rook : Piece
    {
        public Rook()
        {
            Notation = 'R';
            MovementPatternList = new IMovementPattern[] { 
                new MovementPatterns.CardinalPattern() 
            };
        }
    }
}

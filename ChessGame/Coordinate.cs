﻿namespace ChessGame;

using System;
using System.Diagnostics;
using System.Text;

/// <summary>
/// In Chess, a coordinate consists of files and ranks, which represents respectively the x and y coordinate of a position.
/// The file is typically expressed in letters and goes first, e.g. (0, 0) would be 'A1'.
/// </summary>
public readonly struct Coordinate : IEquatable<Coordinate>
{
    /// <summary>
    /// Zero-indexed. e.g. A would be 0.
    /// </summary>
    public readonly byte File;

    /// <summary>
    /// Zero-indexed. Row 1 is actually row 0.
    /// </summary>
    public readonly byte Rank;

    // TODO: Null can be used to show ambiguity(not implemented)

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinate"/> struct. Describes a position on a chessboard with byte-type.
    /// </summary>
    /// <param name="file">The x-coordinate, the column. Usually expressed using the letters A-G.</param>
    /// <param name="rank">The y-coordinate, the row.</param>
    public Coordinate(byte file, byte rank)
    {
        File = file;
        Rank = rank;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinate"/> struct. Describes a position on a chessboard.
    /// </summary>
    /// <param name="file">The x-coordinate, the column. Usually expressed using the letters A-G.</param>
    /// <param name="rank">The y-coordinate, the row.</param>
    public Coordinate(int file, int rank)
    {
        File = (byte)file;
        Rank = (byte)rank;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Coordinate"/> struct by notation.
    /// Converts coordinate notation into <c>Coordinate</c>.
    /// </summary>
    /// <param name="notation">Examples: 'a7' 'b2'.</param>
    public Coordinate(string notation)
    {
        File = (byte)(char.ToLower(notation[0]) - 97);
        Rank = (byte)(notation[1] - 49);
    }

    [DebuggerStepThrough]
    public static bool operator !=(Coordinate coordinate1, Coordinate coordinate2) => coordinate1.Rank != coordinate2.Rank || coordinate1.File != coordinate2.File;

    [DebuggerStepThrough]
    public static bool operator ==(Coordinate coordinate1, Coordinate coordinate2) => coordinate1.Rank == coordinate2.Rank && coordinate1.File == coordinate2.File;

    [DebuggerStepThrough]
    public static Coordinate operator +(Coordinate coordinate1, Coordinate coordinate2) => new Coordinate((byte)(coordinate1.File + coordinate2.File), (byte)(coordinate1.Rank + coordinate2.Rank));

    [DebuggerStepThrough]
    public static Coordinate operator -(Coordinate coordinate1, Coordinate coordinate2) => new Coordinate((byte)(coordinate1.File - coordinate2.File), (byte)(coordinate1.Rank - coordinate2.Rank));

    /// <summary>
    /// Converts a coordinate to one with file as letter and rank as one-indexed number.
    /// </summary>
    /// <returns>A position represented with standard notation.</returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append((char)(97 + File)); // tilføj bogstavs modpart til værdien af File.
        sb.Append(Rank + 1);

        return sb.ToString();
    }

    /// <summary>
    /// Autogenereret af visual studio.
    /// </summary>
    /// <param name="obj">Compared object.</param>
    /// <returns>Equality.</returns>
    [DebuggerStepThrough]
    public override bool Equals(object obj)
    {
        return obj is Coordinate coordinate &&
               File == coordinate.File &&
               Rank == coordinate.Rank;
    }

    /// <summary>
    /// Autogenereret af visual studio.
    /// </summary>
    /// <param name="other">Compared struct.</param>
    /// <returns>Equality.</returns>
    [DebuggerStepThrough]
    public bool Equals(Coordinate other) => other == this;

    /// <summary>
    /// Autogenereret af visual studio.
    /// </summary>
    /// <returns>Equality.</returns>
    [DebuggerStepThrough]
    public override int GetHashCode()
    {
        int hashCode = -73919966;
        hashCode = (hashCode * -1521134295) + File.GetHashCode();
        hashCode = (hashCode * -1521134295) + Rank.GetHashCode();
        return hashCode;
    }
}

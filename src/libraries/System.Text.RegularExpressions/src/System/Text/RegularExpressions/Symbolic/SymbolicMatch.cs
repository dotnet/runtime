// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions.Symbolic
{
    internal readonly struct SymbolicMatch : IEquatable<SymbolicMatch>
    {
        /// <summary>Indicates failure to find a match.</summary>
        internal static SymbolicMatch NoMatch => new SymbolicMatch(-1, -1);

        /// <summary>Indicates a match was found but without meaningful details about where.</summary>
        internal static SymbolicMatch QuickMatch => new SymbolicMatch(0, 0);

        public SymbolicMatch(int index, int length, int[]? captureStarts = null, int[]? captureEnds = null)
        {
            Index = index;
            Length = length;
            CaptureStarts = captureStarts;
            CaptureEnds = captureEnds;
        }

        public int Index { get; }

        public int Length { get; }

        public bool Success => Index >= 0;

        /// <summary>
        /// Array of capture start indices for each capture group. Each is a valid index into the input if the group
        /// was captured, or -1 otherwise.
        /// </summary>
        public readonly int[]? CaptureStarts;

        /// <summary>
        /// Array of capture end indices for each capture group. Each is a valid index into the input and greater than
        /// the corresponding start position if the group was captured, or -1 otherwise.
        /// </summary>
        public readonly int[]? CaptureEnds;

        public static bool operator ==(SymbolicMatch left, SymbolicMatch right) =>
            left.Equals(right);

        public static bool operator !=(SymbolicMatch left, SymbolicMatch right) =>
            !left.Equals(right);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is SymbolicMatch other && Equals(other);

        public bool Equals(SymbolicMatch other) => Index == other.Index && Length == other.Length;

        public override int GetHashCode() => HashCode.Combine(Index, Length);
    }
}

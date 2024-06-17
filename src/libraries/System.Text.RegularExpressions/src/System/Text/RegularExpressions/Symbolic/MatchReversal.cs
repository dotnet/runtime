// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class MatchReversal<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
{
    public MatchReversal(MatchReversalKind kind, int fixedLength, MatchingState<TSet>? adjustedStartState = null)
    {
        Kind = kind;
        FixedLength = fixedLength;
        AdjustedStartState = adjustedStartState;
    }
    internal MatchReversalKind Kind { get; }
    internal int FixedLength { get; }
    internal MatchingState<TSet>? AdjustedStartState { get; }
}

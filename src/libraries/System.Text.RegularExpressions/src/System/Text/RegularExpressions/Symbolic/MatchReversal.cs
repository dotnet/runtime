// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic;

internal sealed class MatchReversal<TSet>(
    MatchReversalKind kind,
    int fixedLength,
    MatchingState<TSet>? adjustedStartState = null)
    where TSet : IComparable<TSet>, IEquatable<TSet>
{
    internal MatchReversalKind Kind { get; } = kind;
    internal int FixedLength { get; } = fixedLength;
    internal MatchingState<TSet>? AdjustedStartState { get; } = adjustedStartState;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Provides details on how a match may be processed in reverse to find the beginning of a match once a match's existence has been confirmed.</summary>
    internal readonly struct MatchReversalInfo<TSet> where TSet : IComparable<TSet>, IEquatable<TSet>
    {
        /// <summary>Initializes the match reversal details.</summary>
        internal MatchReversalInfo(MatchReversalKind kind, int fixedLength, MatchingState<TSet>? adjustedStartState = null)
        {
            Debug.Assert(kind is MatchReversalKind.MatchStart or MatchReversalKind.FixedLength or MatchReversalKind.PartialFixedLength);
            Debug.Assert(fixedLength >= 0);
            Debug.Assert((adjustedStartState is not null) == (kind is MatchReversalKind.PartialFixedLength));

            Kind = kind;
            FixedLength = fixedLength;
            AdjustedStartState = adjustedStartState;
        }

        /// <summary>Gets the kind of the match reversal processing required.</summary>
        internal MatchReversalKind Kind { get; }

        /// <summary>Gets the fixed length of the match, if one is known.</summary>
        /// <remarks>
        /// For <see cref="MatchReversalKind.MatchStart"/>, this is ignored.
        /// For <see cref="MatchReversalKind.FixedLength"/>, this is the full length of the match. The beginning may be found simply
        /// by subtracting this length from the end.
        /// For <see cref="MatchReversalKind.PartialFixedLength"/>, this is the length of fixed portion of the match.
        /// </remarks>
        internal int FixedLength { get; }

        /// <summary>Gets the adjusted start state to use for partial fixed-length matches.</summary>
        /// <remarks>This will be non-null iff <see cref="Kind"/> is <see cref="MatchReversalKind.PartialFixedLength"/>.</remarks>
        internal MatchingState<TSet>? AdjustedStartState { get; }
    }
}

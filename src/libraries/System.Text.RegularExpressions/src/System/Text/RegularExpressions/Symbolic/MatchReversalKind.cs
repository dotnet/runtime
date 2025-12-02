// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Specifies the kind of a <see cref="MatchReversalInfo{TSet}"/>.</summary>
    internal enum MatchReversalKind
    {
        /// <summary>The regex should be run in reverse to find beginning of the match.</summary>
        MatchStart,

        /// <summary>The end of the pattern is of a fixed length and can be skipped as part of running a regex in reverse to find the beginning of the match.</summary>
        /// <remarks>
        /// Reverse execution is not necessary for a subset of the match.
        /// <see cref="MatchReversalInfo{TSet}.FixedLength"/> will contain the length of the fixed portion.
        /// </remarks>
        PartialFixedLength,

        /// <summary>The entire pattern is of a fixed length.</summary>
        /// <remarks>
        /// Reverse execution is not necessary to find the beginning of the match.
        /// <see cref="MatchReversalInfo{TSet}.FixedLength"/> will contain the length of the match.
        /// </remarks>
        FixedLength
    }
}

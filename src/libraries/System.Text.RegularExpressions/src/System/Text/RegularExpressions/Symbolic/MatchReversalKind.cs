// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic;

internal enum MatchReversalKind
{
    /// <summary>The most generic option, run the regex backwards to find beginning of match</summary>
    MatchStart,
    /// <summary>Part of the reversal is fixed length and can be skipped</summary>
    PartialFixedLength,
    /// <summary>The entire pattern is fixed length, reversal not necessary</summary>
    FixedLength
}

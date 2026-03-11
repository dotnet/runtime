// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of the regex find optimizations.
    /// Mirrors the data in <see cref="RegexFindOptimizations"/> to enable effective
    /// incremental caching by the Roslyn pipeline.
    /// </summary>
    internal sealed record FindOptimizationsSpec(
        FindNextStartingPositionMode FindMode,
        RegexNodeKind LeadingAnchor,
        RegexNodeKind TrailingAnchor,
        int MinRequiredLength,
        int? MaxPossibleLength,
        string LeadingPrefix,
        ImmutableEquatableArray<string> LeadingPrefixes,
        (char Char, string? String, int Distance) FixedDistanceLiteral,
        ImmutableEquatableArray<FixedDistanceSetSpec>? FixedDistanceSets,
        (RegexNodeSpec LoopNode, (char Char, string? String, StringComparison StringComparison, ImmutableEquatableArray<char>? Chars) Literal)? LiteralAfterLoop);
}

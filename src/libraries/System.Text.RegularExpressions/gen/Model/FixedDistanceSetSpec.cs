// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a fixed-distance character set
    /// used by the regex find optimizations.
    /// </summary>
    internal sealed record FixedDistanceSetSpec(
        string Set,
        ImmutableEquatableArray<char>? Chars,
        bool Negated,
        int Distance,
        (char LowInclusive, char HighInclusive)? Range);
}

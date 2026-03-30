// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable top-level model representing the complete
    /// set of regex methods to be generated. This is the incremental cache boundary:
    /// Roslyn compares successive instances using value equality to determine whether
    /// the source output callback needs to re-run.
    /// </summary>
    internal sealed record RegexGenerationSpec
    {
        public required ImmutableEquatableSet<RegexMethodSpec> RegexMethods { get; init; }
    }
}

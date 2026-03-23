// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a parsed regex tree.
    /// Mirrors the data in <see cref="RegexTree"/> with baked-in analysis results
    /// to enable effective incremental caching by the Roslyn pipeline.
    /// </summary>
    internal sealed record RegexTreeSpec(
        RegexNodeSpec Root,
        RegexOptions Options,
        int CaptureCount,
        string? CultureName,
        ImmutableEquatableArray<string>? CaptureNames,
        ImmutableEquatableDictionary<string, int>? CaptureNameToNumberMapping,
        ImmutableEquatableDictionary<int, int>? CaptureNumberSparseMapping,
        FindOptimizationsSpec FindOptimizations,
        bool HasIgnoreCase,
        bool HasRightToLeft);
}

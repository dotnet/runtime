// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a regex parse tree node.
    /// Mirrors the data in <see cref="RegexNode"/> with baked-in analysis results
    /// to enable effective incremental caching by the Roslyn pipeline.
    /// </summary>
    internal sealed record RegexNodeSpec(
        RegexNodeKind Kind,
        RegexOptions Options,
        char Ch,
        string? Str,
        int M,
        int N,
        ImmutableEquatableArray<RegexNodeSpec> Children,
        bool IsAtomicByAncestor,
        bool MayBacktrack,
        bool MayContainCapture,
        bool IsInLoop);
}

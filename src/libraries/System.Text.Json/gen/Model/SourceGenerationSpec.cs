// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models all output produced by the source generator
    /// </summary>
    /// <remarks>
    /// Type needs to be cacheable as a Roslyn incremental value so it must be
    ///
    /// 1) immutable and
    /// 2) implement structural (pointwise) equality comparison.
    ///
    /// We can get these properties for free provided that we
    ///
    /// a) define the type as an immutable C# record and
    /// b) ensure all nested members are also immutable and implement structural equality.
    ///
    /// When adding new members to the type, please ensure that these properties
    /// are satisfied otherwise we risk breaking incremental caching in the source generator!
    /// </remarks>
    public sealed record SourceGenerationSpec
    {
        public required ImmutableEquatableArray<ContextGenerationSpec> ContextGenerationSpecs { get; init; }

        public required ImmutableEquatableArray<Diagnostic> Diagnostics { get; init; }
    }
}

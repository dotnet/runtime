// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        /// <summary>
        /// Top-level incremental model. The regular <see cref="RegexMethod"/> instances are wrapped
        /// in regex-specific cache-key types so Roslyn can compare successive results using structural
        /// equality over the parsed <see cref="RegexTree"/> and <see cref="AnalysisResults"/>
        /// objects, without us needing to maintain a mirrored immutable object graph.
        /// </summary>
        private sealed record RegexGenerationSpec
        {
            public required ImmutableEquatableSet<RegexMethodKey> RegexMethods { get; init; }
        }
    }
}

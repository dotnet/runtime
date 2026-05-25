// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a single union case for source-generated union metadata.
    /// </summary>
    /// <remarks>
    /// Type needs to be cacheable as a Roslyn incremental value so it must be
    ///
    /// 1) immutable and
    /// 2) implement structural (pointwise) equality comparison.
    /// </remarks>
    public sealed record UnionCaseSpec
    {
        public required TypeRef CaseType { get; init; }

        public required bool IsNullable { get; init; }
    }
}

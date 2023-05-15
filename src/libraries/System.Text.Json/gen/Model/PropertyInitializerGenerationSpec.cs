// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a member initializer for a generated type.
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
    public sealed record PropertyInitializerGenerationSpec
    {
        public required PropertyGenerationSpec Property { get; init; }

        public required int ParameterIndex { get; init; }

        public required bool MatchesConstructorParameter { get; init; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Models a constructor parameter for a generated type.
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
    public sealed record ParameterGenerationSpec
    {
        public required TypeRef ParameterType { get; init; }
        public required string Name { get; init; }
        public required bool HasDefaultValue { get; init; }

        // The default value of a constructor parameter can only be a constant
        // so it always satisfies the structural equality requirement for the record.
        public required object? DefaultValue { get; init; }

        /// <summary>
        /// The zero-based position of the parameter in the constructor's formal parameter list.
        /// </summary>
        public required int ParameterIndex { get; init; }

        /// <summary>
        /// The zero-based index into the args array for this parameter.
        /// For out parameters, this is -1 since they don't receive values from the args array.
        /// </summary>
        public required int ArgsIndex { get; init; }

        public required bool IsNullable { get; init; }

        /// <summary>
        /// The ref kind of the parameter: None (0), Ref (1), Out (2), In (3), RefReadOnlyParameter (4).
        /// Using int instead of Microsoft.CodeAnalysis.RefKind to avoid dependency issues.
        /// </summary>
        public required int RefKind { get; init; }
    }
}

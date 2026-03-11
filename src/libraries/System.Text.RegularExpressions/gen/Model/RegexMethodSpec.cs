// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a single regex method or property
    /// decorated with <c>[GeneratedRegex]</c>. Contains all data needed by the emitter
    /// to generate the corresponding source code.
    /// </summary>
    internal sealed record RegexMethodSpec
    {
        public required RegexTypeSpec DeclaringType { get; init; }
        public required bool IsProperty { get; init; }
        public required string MemberName { get; init; }
        public required string Modifiers { get; init; }
        public required bool NullableRegex { get; init; }
        public required string Pattern { get; init; }
        public required RegexOptions Options { get; init; }
        public required int? MatchTimeout { get; init; }
        public required RegexTreeSpec? Tree { get; init; }
        public required string? LimitedSupportReason { get; init; }
        public required RegexGenerator.CompilationData CompilationData { get; init; }
    }
}

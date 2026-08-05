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

        /// <summary>
        /// Type symbol used in the generated <c>value switch</c> arm pattern.
        /// For a value-type <c>Nullable&lt;T&gt;</c> case this is the underlying
        /// <c>T</c> (C# rejects <c>Nullable&lt;T&gt;</c> in a pattern with CS8116
        /// — at the CLR layer a boxed <c>Nullable&lt;T&gt;</c> with HasValue=true
        /// is bit-identical to a boxed <c>T</c>, so the underlying-type arm covers
        /// both <c>Foo(T)</c> and <c>Foo(Nullable&lt;T&gt;)</c> non-null payloads).
        /// For every other shape this equals <see cref="CaseType"/>.
        /// </summary>
        public required TypeRef PatternType { get; init; }

        public required bool IsNullable { get; init; }

        /// <summary>
        /// Whether this case contributes a <c>value switch</c> arm in the generated
        /// union constructor/deconstructor. When a union declares both <c>Foo(T)</c>
        /// and <c>Foo(Nullable&lt;T&gt;)</c> the two ctors translate to the same C#
        /// pattern (CS8116 rejects <c>Nullable&lt;T&gt;</c> in patterns; at the CLR
        /// layer a boxed <c>Nullable&lt;T&gt;</c> with HasValue=true is bit-identical
        /// to a boxed <c>T</c>). The parser keeps both as distinct union cases but
        /// marks only one as the canonical switch arm; the non-<c>Nullable&lt;T&gt;</c>
        /// sibling is preferred so most-derived dispatch reports <c>typeof(T)</c>
        /// rather than <c>typeof(Nullable&lt;T&gt;)</c>.
        /// </summary>
        public required bool IsSwitchArm { get; init; }

        /// <summary>
        /// Whether the union instance itself, rather than the value it wraps, can match a
        /// type pattern over <see cref="PatternType"/>.
        /// </summary>
        /// <remarks>
        /// C# union matching tests a type pattern against the union instance before the
        /// union's value, so for such a case a bare <c>PatternType caseValue</c> arm binds
        /// the incoming union rather than its payload — or, where the compiler cannot tell
        /// the two apart, fails to compile at all. The emitter keeps <c>value</c> as the
        /// switch operand and emits a <c>{ Value: PatternType }</c> property pattern for
        /// these cases, which forces the test onto the payload. The most common shape is a
        /// union declaring itself as one of its case types, e.g. <c>union Nat(bool, Nat);</c>,
        /// but an unsealed class union whose case type derives from it has the same problem.
        /// </remarks>
        public required bool MatchesUnionInstance { get; init; }
    }
}

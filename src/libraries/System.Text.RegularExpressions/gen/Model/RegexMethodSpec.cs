// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>
    /// Immutable, structurally equatable representation of a single regex method or property
    /// decorated with <c>[GeneratedRegex]</c>. Contains all data needed by the emitter
    /// to generate the corresponding source code.
    /// </summary>
    internal sealed record RegexMethodSpec : IComparable<RegexMethodSpec>
    {
        public required RegexTypeSpec DeclaringType { get; init; }
        public required bool IsProperty { get; init; }
        public required string MemberName { get; init; }
        public required string Modifiers { get; init; }
        public required bool NullableRegex { get; init; }
        public required string Pattern { get; init; }
        public required RegexOptions Options { get; init; }
        public required int? MatchTimeout { get; init; }
        public required string? CultureName { get; init; }
        public required RegexTreeSpec? Tree { get; init; }
        public required string? LimitedSupportReason { get; init; }
        public required CompilationData CompilationData { get; init; }

        public int CompareTo(RegexMethodSpec? other)
        {
            if (other is null)
            {
                return 1;
            }

            int cmp;

            // Sort by declaring type hierarchy first.
            cmp = string.Compare(DeclaringType.Namespace, other.DeclaringType.Namespace, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            cmp = CompareTypeSpec(DeclaringType, other.DeclaringType);
            if (cmp != 0) return cmp;

            cmp = string.Compare(MemberName, other.MemberName, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            cmp = string.Compare(Pattern, other.Pattern, StringComparison.Ordinal);
            if (cmp != 0) return cmp;

            cmp = ((int)Options).CompareTo((int)other.Options);
            if (cmp != 0) return cmp;

            cmp = Nullable.Compare(MatchTimeout, other.MatchTimeout);
            if (cmp != 0) return cmp;

            return string.Compare(CultureName, other.CultureName, StringComparison.Ordinal);
        }

        private static int CompareTypeSpec(RegexTypeSpec left, RegexTypeSpec right)
        {
            // Compare parent chains first (outermost to innermost).
            if (left.Parent is not null && right.Parent is not null)
            {
                int cmp = CompareTypeSpec(left.Parent, right.Parent);
                if (cmp != 0) return cmp;
            }
            else if (left.Parent is not null)
            {
                return 1;
            }
            else if (right.Parent is not null)
            {
                return -1;
            }

            return string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics.Hashing;

namespace System.Text.RegularExpressions.Generator
{
    public partial class RegexGenerator
    {
        /// <summary>
        /// Incremental cache key for a parsed regex method.
        /// </summary>
        private readonly struct RegexMethodSpec(RegexMethod method) : IEquatable<RegexMethodSpec>
        {
            private static readonly RegexTypeComparer s_typeComparer = new();
            private static readonly RegexTreeComparer s_treeComparer = new();
            private static readonly AnalysisResultsComparer s_analysisComparer = new();

            public RegexMethod Method { get; } = method;

            public bool Equals(RegexMethodSpec other)
            {
                RegexMethod? x = Method;
                RegexMethod? y = other.Method;

                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return s_typeComparer.Equals(x.DeclaringType, y.DeclaringType) &&
                    x.IsProperty == y.IsProperty &&
                    StringComparer.Ordinal.Equals(x.MemberName, y.MemberName) &&
                    StringComparer.Ordinal.Equals(x.Modifiers, y.Modifiers) &&
                    x.NullableRegex == y.NullableRegex &&
                    StringComparer.Ordinal.Equals(x.Pattern, y.Pattern) &&
                    x.Options == y.Options &&
                    x.MatchTimeout == y.MatchTimeout &&
                    StringComparer.Ordinal.Equals(x.CultureName, y.CultureName) &&
                    s_treeComparer.Equals(x.Tree, y.Tree) &&
                    s_analysisComparer.Equals(x.Analysis, y.Analysis) &&
                    StringComparer.Ordinal.Equals(x.LimitedSupportReason, y.LimitedSupportReason) &&
                    x.CompilationData.Equals(y.CompilationData);
            }

            public override bool Equals(object? obj) => obj is RegexMethodSpec other && Equals(other);

            public override int GetHashCode()
            {
                RegexMethod? regexMethod = Method;
                if (regexMethod is null)
                {
                    return 0;
                }

                int hash = s_typeComparer.GetHashCode(regexMethod.DeclaringType);
                hash = HashHelpers.Combine(hash, regexMethod.IsProperty.GetHashCode());
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.MemberName));
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.Modifiers));
                hash = HashHelpers.Combine(hash, regexMethod.NullableRegex.GetHashCode());
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.Pattern));
                hash = HashHelpers.Combine(hash, regexMethod.Options.GetHashCode());
                hash = HashHelpers.Combine(hash, regexMethod.MatchTimeout.GetHashCode());
                hash = HashHelpers.Combine(hash, regexMethod.CultureName is null ? 0 : StringComparer.Ordinal.GetHashCode(regexMethod.CultureName));
                hash = HashHelpers.Combine(hash, s_treeComparer.GetHashCode(regexMethod.Tree));
                hash = HashHelpers.Combine(hash, s_analysisComparer.GetHashCode(regexMethod.Analysis));
                hash = HashHelpers.Combine(hash, regexMethod.LimitedSupportReason is null ? 0 : StringComparer.Ordinal.GetHashCode(regexMethod.LimitedSupportReason));
                hash = HashHelpers.Combine(hash, regexMethod.CompilationData.GetHashCode());
                return hash;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics.Hashing;

namespace System.Text.RegularExpressions.Generator
{
    public readonly partial struct RegexMethodSpec : IEquatable<RegexMethodSpec>
    {
        public bool Equals(RegexMethodSpec other)
        {
            RegexGenerator.RegexMethod? x = Method;
            RegexGenerator.RegexMethod? y = other.Method;

            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return RegexTypeEquals(x.DeclaringType, y.DeclaringType) &&
                x.IsProperty == y.IsProperty &&
                StringComparer.Ordinal.Equals(x.MemberName, y.MemberName) &&
                StringComparer.Ordinal.Equals(x.Modifiers, y.Modifiers) &&
                x.NullableRegex == y.NullableRegex &&
                StringComparer.Ordinal.Equals(x.Pattern, y.Pattern) &&
                x.Options == y.Options &&
                x.MatchTimeout == y.MatchTimeout &&
                StringComparer.Ordinal.Equals(x.CultureName, y.CultureName) &&
                RegexTreeEquals(x.Tree, y.Tree) &&
                AnalysisEquals(x.Analysis, y.Analysis) &&
                StringComparer.Ordinal.Equals(x.LimitedSupportReason, y.LimitedSupportReason) &&
                x.CompilationData.Equals(y.CompilationData);
        }

        public override bool Equals(object? obj) => obj is RegexMethodSpec other && Equals(other);

        public override int GetHashCode()
        {
            RegexGenerator.RegexMethod? regexMethod = Method;
            if (regexMethod is null)
            {
                return 0;
            }

            int hash = GetRegexTypeHashCode(regexMethod.DeclaringType);
            hash = HashHelpers.Combine(hash, regexMethod.IsProperty.GetHashCode());
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.MemberName));
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.Modifiers));
            hash = HashHelpers.Combine(hash, regexMethod.NullableRegex.GetHashCode());
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(regexMethod.Pattern));
            hash = HashHelpers.Combine(hash, regexMethod.Options.GetHashCode());
            hash = HashHelpers.Combine(hash, regexMethod.MatchTimeout.GetHashCode());
            hash = HashHelpers.Combine(hash, regexMethod.CultureName is null ? 0 : StringComparer.Ordinal.GetHashCode(regexMethod.CultureName));
            hash = HashHelpers.Combine(hash, GetRegexTreeHashCode(regexMethod.Tree));
            hash = HashHelpers.Combine(hash, GetAnalysisHashCode(regexMethod.Analysis));
            hash = HashHelpers.Combine(hash, regexMethod.LimitedSupportReason is null ? 0 : StringComparer.Ordinal.GetHashCode(regexMethod.LimitedSupportReason));
            hash = HashHelpers.Combine(hash, regexMethod.CompilationData.GetHashCode());
            return hash;
        }

        private static bool RegexTypeEquals(RegexGenerator.RegexType? x, RegexGenerator.RegexType? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return StringComparer.Ordinal.Equals(x.Keyword, y.Keyword) &&
                StringComparer.Ordinal.Equals(x.Namespace, y.Namespace) &&
                StringComparer.Ordinal.Equals(x.Name, y.Name) &&
                RegexTypeEquals(x.Parent, y.Parent);
        }

        private static int GetRegexTypeHashCode(RegexGenerator.RegexType obj)
        {
            int hash = StringComparer.Ordinal.GetHashCode(obj.Keyword);
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.Namespace));
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.Name));
            hash = HashHelpers.Combine(hash, obj.Parent is null ? 0 : GetRegexTypeHashCode(obj.Parent));
            return hash;
        }

        private static bool RegexTreeEquals(RegexTree? x, RegexTree? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.Options == y.Options &&
                x.CaptureCount == y.CaptureCount &&
                StringComparer.Ordinal.Equals(x.Culture?.Name, y.Culture?.Name) &&
                StringArrayEquals(x.CaptureNames, y.CaptureNames) &&
                HashtableEquals(x.CaptureNameToNumberMapping, y.CaptureNameToNumberMapping) &&
                HashtableEquals(x.CaptureNumberSparseMapping, y.CaptureNumberSparseMapping) &&
                RegexNodeEquals(x.Root, y.Root) &&
                RegexFindOptimizationsEquals(x.FindOptimizations, y.FindOptimizations);
        }

        private static int GetRegexTreeHashCode(RegexTree obj)
        {
            int hash = obj.Options.GetHashCode();
            hash = HashHelpers.Combine(hash, obj.CaptureCount);
            hash = HashHelpers.Combine(hash, obj.Culture?.Name is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.Culture.Name));
            hash = HashHelpers.Combine(hash, GetStringArrayHashCode(obj.CaptureNames));
            hash = HashHelpers.Combine(hash, GetHashtableHashCode(obj.CaptureNameToNumberMapping));
            hash = HashHelpers.Combine(hash, GetHashtableHashCode(obj.CaptureNumberSparseMapping));
            hash = HashHelpers.Combine(hash, GetRegexNodeHashCode(obj.Root));
            hash = HashHelpers.Combine(hash, GetRegexFindOptimizationsHashCode(obj.FindOptimizations));
            return hash;
        }

        private static bool AnalysisEquals(AnalysisResults? x, AnalysisResults? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (!RegexTreeEquals(x.RegexTree, y.RegexTree) ||
                x.HasIgnoreCase != y.HasIgnoreCase ||
                x.HasRightToLeft != y.HasRightToLeft)
            {
                return false;
            }

            Stack<(RegexNode Left, RegexNode Right)> pending = new();
            pending.Push((x.RegexTree.Root, y.RegexTree.Root));

            while (pending.Count != 0)
            {
                (RegexNode left, RegexNode right) = pending.Pop();

                if (!RegexNodeEquals(left, right) ||
                    x.IsAtomicByAncestor(left) != y.IsAtomicByAncestor(right) ||
                    x.MayContainCapture(left) != y.MayContainCapture(right) ||
                    x.MayBacktrack(left) != y.MayBacktrack(right) ||
                    x.IsInLoop(left) != y.IsInLoop(right))
                {
                    return false;
                }

                for (int i = left.ChildCount() - 1; i >= 0; i--)
                {
                    pending.Push((left.Child(i), right.Child(i)));
                }
            }

            return true;
        }

        private static int GetAnalysisHashCode(AnalysisResults obj)
        {
            int hash = GetRegexTreeHashCode(obj.RegexTree);
            hash = HashHelpers.Combine(hash, obj.HasIgnoreCase.GetHashCode());
            hash = HashHelpers.Combine(hash, obj.HasRightToLeft.GetHashCode());

            Stack<RegexNode> pending = new();
            pending.Push(obj.RegexTree.Root);

            while (pending.Count != 0)
            {
                RegexNode node = pending.Pop();
                hash = HashHelpers.Combine(hash, obj.IsAtomicByAncestor(node).GetHashCode());
                hash = HashHelpers.Combine(hash, obj.MayContainCapture(node).GetHashCode());
                hash = HashHelpers.Combine(hash, obj.MayBacktrack(node).GetHashCode());
                hash = HashHelpers.Combine(hash, obj.IsInLoop(node).GetHashCode());

                for (int i = node.ChildCount() - 1; i >= 0; i--)
                {
                    pending.Push(node.Child(i));
                }
            }

            return hash;
        }

        private static bool RegexFindOptimizationsEquals(RegexFindOptimizations? x, RegexFindOptimizations? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (x.FindMode != y.FindMode ||
                x.LeadingAnchor != y.LeadingAnchor ||
                x.TrailingAnchor != y.TrailingAnchor ||
                x.MinRequiredLength != y.MinRequiredLength ||
                x.MaxPossibleLength != y.MaxPossibleLength ||
                !StringComparer.Ordinal.Equals(x.LeadingPrefix, y.LeadingPrefix) ||
                !StringArrayEquals(x.LeadingPrefixes, y.LeadingPrefixes) ||
                x.FixedDistanceLiteral.Char != y.FixedDistanceLiteral.Char ||
                !StringComparer.Ordinal.Equals(x.FixedDistanceLiteral.String, y.FixedDistanceLiteral.String) ||
                x.FixedDistanceLiteral.Distance != y.FixedDistanceLiteral.Distance ||
                !FixedDistanceSetsEquals(x.FixedDistanceSets, y.FixedDistanceSets))
            {
                return false;
            }

            if (x.LiteralAfterLoop is null || y.LiteralAfterLoop is null)
            {
                return x.LiteralAfterLoop is null && y.LiteralAfterLoop is null;
            }

            (RegexNode xLoopNode, (char Char, string? String, StringComparison StringComparison, char[]? Chars) xLiteral) = x.LiteralAfterLoop.GetValueOrDefault();
            (RegexNode yLoopNode, (char Char, string? String, StringComparison StringComparison, char[]? Chars) yLiteral) = y.LiteralAfterLoop.GetValueOrDefault();

            return RegexNodeEquals(xLoopNode, yLoopNode) &&
                xLiteral.Char == yLiteral.Char &&
                StringComparer.Ordinal.Equals(xLiteral.String, yLiteral.String) &&
                xLiteral.StringComparison == yLiteral.StringComparison &&
                CharArrayEquals(xLiteral.Chars, yLiteral.Chars);
        }

        private static int GetRegexFindOptimizationsHashCode(RegexFindOptimizations obj)
        {
            int hash = obj.FindMode.GetHashCode();
            hash = HashHelpers.Combine(hash, obj.LeadingAnchor.GetHashCode());
            hash = HashHelpers.Combine(hash, obj.TrailingAnchor.GetHashCode());
            hash = HashHelpers.Combine(hash, obj.MinRequiredLength);
            hash = HashHelpers.Combine(hash, obj.MaxPossibleLength.GetHashCode());
            hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(obj.LeadingPrefix));
            hash = HashHelpers.Combine(hash, GetStringArrayHashCode(obj.LeadingPrefixes));
            hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.Char.GetHashCode());
            hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.String is null ? 0 : StringComparer.Ordinal.GetHashCode(obj.FixedDistanceLiteral.String));
            hash = HashHelpers.Combine(hash, obj.FixedDistanceLiteral.Distance);
            hash = HashHelpers.Combine(hash, GetFixedDistanceSetsHashCode(obj.FixedDistanceSets));

            if (obj.LiteralAfterLoop is { } literalAfterLoop)
            {
                hash = HashHelpers.Combine(hash, GetRegexNodeHashCode(literalAfterLoop.LoopNode));
                hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.Char.GetHashCode());
                hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.String is null ? 0 : StringComparer.Ordinal.GetHashCode(literalAfterLoop.Literal.String));
                hash = HashHelpers.Combine(hash, literalAfterLoop.Literal.StringComparison.GetHashCode());
                hash = HashHelpers.Combine(hash, GetCharArrayHashCode(literalAfterLoop.Literal.Chars));
            }

            return hash;
        }

        private static bool RegexNodeEquals(RegexNode? x, RegexNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            Stack<(RegexNode Left, RegexNode Right)> pending = new();
            pending.Push((x, y));

            while (pending.Count != 0)
            {
                (RegexNode left, RegexNode right) = pending.Pop();

                if (left.Kind != right.Kind ||
                    !StringComparer.Ordinal.Equals(left.Str, right.Str) ||
                    left.Ch != right.Ch ||
                    left.M != right.M ||
                    left.N != right.N ||
                    left.Options != right.Options)
                {
                    return false;
                }

                int childCount = left.ChildCount();
                if (childCount != right.ChildCount())
                {
                    return false;
                }

                for (int i = childCount - 1; i >= 0; i--)
                {
                    pending.Push((left.Child(i), right.Child(i)));
                }
            }

            return true;
        }

        private static int GetRegexNodeHashCode(RegexNode obj)
        {
            int hash = 0;
            Stack<RegexNode> pending = new();
            pending.Push(obj);

            while (pending.Count != 0)
            {
                RegexNode current = pending.Pop();
                hash = HashHelpers.Combine(hash, current.Kind.GetHashCode());
                hash = HashHelpers.Combine(hash, current.Str is null ? 0 : StringComparer.Ordinal.GetHashCode(current.Str));
                hash = HashHelpers.Combine(hash, current.Ch.GetHashCode());
                hash = HashHelpers.Combine(hash, current.M);
                hash = HashHelpers.Combine(hash, current.N);
                hash = HashHelpers.Combine(hash, current.Options.GetHashCode());
                hash = HashHelpers.Combine(hash, current.ChildCount());

                for (int i = current.ChildCount() - 1; i >= 0; i--)
                {
                    pending.Push(current.Child(i));
                }
            }

            return hash;
        }

        private static bool StringArrayEquals(string[]? x, string[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (!StringComparer.Ordinal.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CharArrayEquals(char[]? x, char[]? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Length != y.Length)
            {
                return false;
            }

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool HashtableEquals(Hashtable? x, Hashtable? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Count != y.Count)
            {
                return false;
            }

            foreach (DictionaryEntry entry in x)
            {
                if (!y.ContainsKey(entry.Key) ||
                    !Equals(entry.Value, y[entry.Key]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool FixedDistanceSetsEquals(List<RegexFindOptimizations.FixedDistanceSet>? x, List<RegexFindOptimizations.FixedDistanceSet>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null || x.Count != y.Count)
            {
                return false;
            }

            for (int i = 0; i < x.Count; i++)
            {
                RegexFindOptimizations.FixedDistanceSet left = x[i];
                RegexFindOptimizations.FixedDistanceSet right = y[i];

                if (!StringComparer.Ordinal.Equals(left.Set, right.Set) ||
                    left.Negated != right.Negated ||
                    !CharArrayEquals(left.Chars, right.Chars) ||
                    left.Distance != right.Distance ||
                    left.Range != right.Range)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetStringArrayHashCode(string[]? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Length;
            foreach (string value in values)
            {
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(value));
            }

            return hash;
        }

        private static int GetCharArrayHashCode(char[]? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Length;
            foreach (char value in values)
            {
                hash = HashHelpers.Combine(hash, value.GetHashCode());
            }

            return hash;
        }

        private static int GetHashtableHashCode(Hashtable? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Count;
            foreach (DictionaryEntry entry in values)
            {
                int entryHash = entry.Key?.GetHashCode() ?? 0;
                entryHash = HashHelpers.Combine(entryHash, entry.Value?.GetHashCode() ?? 0);
                hash ^= entryHash;
            }

            return hash;
        }

        private static int GetFixedDistanceSetsHashCode(List<RegexFindOptimizations.FixedDistanceSet>? values)
        {
            if (values is null)
            {
                return 0;
            }

            int hash = values.Count;
            foreach (RegexFindOptimizations.FixedDistanceSet value in values)
            {
                hash = HashHelpers.Combine(hash, StringComparer.Ordinal.GetHashCode(value.Set));
                hash = HashHelpers.Combine(hash, value.Negated.GetHashCode());
                hash = HashHelpers.Combine(hash, GetCharArrayHashCode(value.Chars));
                hash = HashHelpers.Combine(hash, value.Distance);
                hash = HashHelpers.Combine(hash, value.Range.GetHashCode());
            }

            return hash;
        }
    }
}

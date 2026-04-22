// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    internal static class ReferenceParser
    {
        internal static bool ContainsReference(string? value)
        {
            if (value is null)
            {
                return false;
            }

            for (int i = 0; i < value.Length - 1; i++)
            {
                if (value[i] != '$' || value[i + 1] != '{')
                {
                    continue;
                }

                // ${{ is a literal-${ shorthand; skip past the second '{' so we don't re-match on it.
                if (i + 2 < value.Length && value[i + 2] == '{')
                {
                    i += 2;
                    continue;
                }

                return true;
            }

            return false;
        }

        internal static IReadOnlyList<ValueToken> Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var tokens = new List<ValueToken>();
            var literal = new StringBuilder();

            int i = 0;
            while (i < value.Length)
            {
                if (i < value.Length - 2 && value[i] == '$' && value[i + 1] == '{' && value[i + 2] == '{')
                {
                    literal.Append("${");
                    i += 3;
                    continue;
                }

                if (i < value.Length - 1 && value[i] == '$' && value[i + 1] == '{')
                {
                    if (literal.Length > 0)
                    {
                        tokens.Add(ValueToken.Literal(literal.ToString()));
                        literal.Clear();
                    }

                    int expressionStart = i + 2;
                    int expressionEnd = FindExpressionEnd(value, expressionStart, i);
                    string expression = value.Substring(expressionStart, expressionEnd - expressionStart);

                    tokens.Add(ParseExpression(expression, i));

                    i = expressionEnd + 1;
                    continue;
                }

                literal.Append(value[i]);
                i++;
            }

            if (literal.Length > 0)
            {
                tokens.Add(ValueToken.Literal(literal.ToString()));
            }

            return tokens;
        }

        private static int FindExpressionEnd(string value, int start, int tokenStart)
        {
            for (int j = start; j < value.Length; j++)
            {
                if (value[j] == '}')
                {
                    return j;
                }
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, tokenStart));
        }

        private static ValueToken ParseExpression(string expression, int tokenStart)
        {
            expression = expression.Trim();
            if (expression.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, tokenStart));
            }

            // Trailing '!' marks a section reference as strict: the resulting section is exactly the
            // aliased target, with no merging of earlier providers' keys under the aliased path.
            bool isStrict = expression[expression.Length - 1] == '!';
            if (isStrict)
            {
                expression = expression.Substring(0, expression.Length - 1).TrimEnd();
                if (expression.Length == 0)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, tokenStart));
                }
            }

            // Trailing '?' marks the chain as optional: if all references fail to resolve, the expression
            // collapses to empty string instead of throwing. Only valid as a terminal marker.
            bool isOptional = expression[expression.Length - 1] == '?';
            if (isOptional)
            {
                expression = expression.Substring(0, expression.Length - 1).TrimEnd();
                if (expression.Length == 0)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, tokenStart));
                }
            }

            var items = new List<ReferenceItem>();
            int i = 0;
            while (i <= expression.Length)
            {
                while (i < expression.Length && char.IsWhiteSpace(expression[i]))
                {
                    i++;
                }

                if (i >= expression.Length)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
                }

                i = ParseReferenceItem(expression, i, tokenStart, items);

                while (i < expression.Length && char.IsWhiteSpace(expression[i]))
                {
                    i++;
                }

                if (i >= expression.Length)
                {
                    break;
                }

                if (expression[i] != '?')
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
                }

                i++;
            }

            return ValueToken.Reference(items, isOptional, isStrict);
        }

        private static int ParseReferenceItem(string expression, int i, int tokenStart, List<ReferenceItem> items)
        {
            int start = i;
            while (i < expression.Length && expression[i] != '?')
            {
                i++;
            }

            string refPath = expression.Substring(start, i - start).Trim();
            if (refPath.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

            // Parse leading ".." segments as parent hops. The remaining text (if any) is an absolute
            // suffix appended to the Nth ancestor of the literal's storage key at resolution time.
            int parentHops = 0;
            int cursor = 0;
            while (cursor + 2 <= refPath.Length && refPath[cursor] == '.' && refPath[cursor + 1] == '.')
            {
                int next = cursor + 2;
                if (next == refPath.Length)
                {
                    parentHops++;
                    cursor = next;
                    break;
                }

                if (refPath[next] != ConfigurationPath.KeyDelimiter[0])
                {
                    break;
                }

                parentHops++;
                cursor = next + 1;
            }

            string suffix = cursor == 0 ? refPath : refPath.Substring(cursor);

            if (suffix.Contains(".."))
            {
                foreach (string segment in suffix.Split(ConfigurationPath.KeyDelimiter[0]))
                {
                    if (segment == "..")
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
                    }
                }
            }

            if (parentHops == 0 && suffix.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

            items.Add(new ReferenceItem(suffix, parentHops));
            return i;
        }
    }

    internal enum ValueTokenKind
    {
        Literal,
        Reference,
    }

    internal readonly struct ReferenceItem
    {
        public ReferenceItem(string path, int parentHops = 0)
        {
            Value = path;
            ParentHops = parentHops;
        }

        internal string Value { get; }

        internal int ParentHops { get; }
    }

    internal readonly struct ValueToken
    {
        private static readonly IReadOnlyList<ReferenceItem> s_noItems = Array.Empty<ReferenceItem>();

        private ValueToken(ValueTokenKind kind, string value, IReadOnlyList<ReferenceItem> items, bool isOptional, bool isStrict)
        {
            Kind = kind;
            Value = value;
            Items = items;
            IsOptional = isOptional;
            IsStrict = isStrict;
        }

        internal static ValueToken Literal(string text) =>
            new(ValueTokenKind.Literal, text, s_noItems, isOptional: false, isStrict: false);

        internal static ValueToken Reference(IReadOnlyList<ReferenceItem> items, bool isOptional, bool isStrict = false)
        {
            string first = items.Count > 0 ? items[0].Value : string.Empty;
            return new ValueToken(ValueTokenKind.Reference, first, items, isOptional, isStrict);
        }

        internal ValueTokenKind Kind { get; }

        // For Literal tokens: the literal text.
        // For Reference tokens: the first reference path (convenience).
        internal string Value { get; }

        internal IReadOnlyList<ReferenceItem> Items { get; }

        internal bool IsOptional { get; }

        internal bool IsStrict { get; }
    }
}

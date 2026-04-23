// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    // Value syntax:
    //   ref(<path-expression>)     — the whole value is a reference to another key. Eligible as a
    //                                 section-alias target when the path names a section.
    //   fmt(<template>)            — the value is a template with {<path-expression>} placeholders.
    //                                 Braces are escaped by doubling: '{{' -> '{', '}}' -> '}'.
    //
    // A value is processed only if it matches exactly /^(ref|fmt)\(.*\)$/ — the opening directive is
    // at position 0 and the matching ')' is the final character. Anything else is inert and returned
    // as-is. This keeps values like "${message}" (NLog), "$(Foo)" (MSBuild), "$100" (currency), or
    // "ref(literal)" in sources that never opt in, untouched.
    //
    // Path expressions inside ref() and inside fmt() placeholders share the same grammar:
    //   path (? path)*  (!)? (?)?
    // where each path may have leading ".." segments meaning parent hops. A trailing '!' marks the
    // (single-item) reference as a strict section alias, i.e. the resulting section is exactly the
    // aliased target with no merging from earlier providers. A trailing '?' makes the whole chain
    // optional — if every item misses, the expression resolves to the empty string.
    internal static class ReferenceParser
    {
        private const string RefPrefix = "ref(";
        private const string FmtPrefix = "fmt(";
        private const int PrefixLength = 4;     // "ref(" and "fmt(" are both 4 chars
        private const int MinGatedLength = 5;   // "ref()" / "fmt()"

        internal static bool ContainsReference(string? value) =>
            TryGetGate(value, out _);

        internal static IReadOnlyList<ValueToken> Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!TryGetGate(value, out bool isRef))
            {
                return new[] { ValueToken.Literal(value) };
            }

            string payload = value.Substring(PrefixLength, value.Length - PrefixLength - 1);

            if (isRef)
            {
                return new[] { ParseRefExpression(payload, tokenStart: 0, isFromRef: true) };
            }

            return ParseFmtTemplate(payload, outerStart: PrefixLength);
        }

        private static bool TryGetGate(string? value, out bool isRef)
        {
            isRef = false;
            if (value is null || value.Length < MinGatedLength)
            {
                return false;
            }

            if (value[value.Length - 1] != ')')
            {
                return false;
            }

            if (StartsWithOrdinal(value, RefPrefix))
            {
                isRef = true;
                return true;
            }

            return StartsWithOrdinal(value, FmtPrefix);
        }

        private static bool StartsWithOrdinal(string value, string prefix)
        {
            if (value.Length < prefix.Length)
            {
                return false;
            }

            for (int i = 0; i < prefix.Length; i++)
            {
                if (value[i] != prefix[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static List<ValueToken> ParseFmtTemplate(string template, int outerStart)
        {
            var tokens = new List<ValueToken>();
            var literal = new StringBuilder();

            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];

                if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
                {
                    literal.Append('{');
                    i += 2;
                    continue;
                }

                if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                {
                    literal.Append('}');
                    i += 2;
                    continue;
                }

                if (c == '{')
                {
                    if (literal.Length > 0)
                    {
                        tokens.Add(ValueToken.Literal(literal.ToString()));
                        literal.Clear();
                    }

                    int placeholderStart = i + 1;
                    int placeholderEnd = FindPlaceholderEnd(template, placeholderStart, outerStart + i);
                    string expression = template.Substring(placeholderStart, placeholderEnd - placeholderStart);

                    tokens.Add(ParseRefExpression(expression, tokenStart: outerStart + i, isFromRef: false));

                    i = placeholderEnd + 1;
                    continue;
                }

                if (c == '}')
                {
                    // Unmatched closing brace in template.
                    throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, outerStart + i));
                }

                literal.Append(c);
                i++;
            }

            if (literal.Length > 0)
            {
                tokens.Add(ValueToken.Literal(literal.ToString()));
            }

            if (tokens.Count == 0)
            {
                tokens.Add(ValueToken.Literal(string.Empty));
            }

            return tokens;
        }

        private static int FindPlaceholderEnd(string template, int start, int tokenStart)
        {
            for (int j = start; j < template.Length; j++)
            {
                if (template[j] == '}')
                {
                    return j;
                }
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, tokenStart));
        }

        private static ValueToken ParseRefExpression(string expression, int tokenStart, bool isFromRef)
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

            return ValueToken.Reference(items, isOptional, isStrict, isFromRef);
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

        private ValueToken(ValueTokenKind kind, string value, IReadOnlyList<ReferenceItem> items, bool isOptional, bool isStrict, bool isFromRef)
        {
            Kind = kind;
            Value = value;
            Items = items;
            IsOptional = isOptional;
            IsStrict = isStrict;
            IsFromRef = isFromRef;
        }

        internal static ValueToken Literal(string text) =>
            new(ValueTokenKind.Literal, text, s_noItems, isOptional: false, isStrict: false, isFromRef: false);

        internal static ValueToken Reference(IReadOnlyList<ReferenceItem> items, bool isOptional, bool isStrict, bool isFromRef)
        {
            string first = items.Count > 0 ? items[0].Value : string.Empty;
            return new ValueToken(ValueTokenKind.Reference, first, items, isOptional, isStrict, isFromRef);
        }

        internal ValueTokenKind Kind { get; }

        // For Literal tokens: the literal text.
        // For Reference tokens: the first reference path (convenience).
        internal string Value { get; }

        internal IReadOnlyList<ReferenceItem> Items { get; }

        internal bool IsOptional { get; }

        internal bool IsStrict { get; }

        // True only for the single Reference token produced by a top-level ref(...) value. Placeholders
        // inside fmt(...) never set this flag — they are always string interpolations, even when the
        // template consists of a single {Key} placeholder.
        internal bool IsFromRef { get; }
    }
}

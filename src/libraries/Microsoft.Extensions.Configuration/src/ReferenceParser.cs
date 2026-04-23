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
    //   path ('?' path)*  ('!')?  ('|' literal-tail)?
    // where each path may have leading ".." segments meaning parent hops. A trailing '!' marks the
    // (single-item) reference as a strict section alias, i.e. the resulting section is exactly the
    // aliased target with no merging from earlier providers. A '|' introduces an explicit literal
    // fallback — if every reference in the chain misses, the expression resolves to the text after
    // the '|'. A bare '|' with nothing after it makes the expression optional: the result is the
    // empty string when no reference resolves.
    //
    // Path segments and the literal tail may both be quoted with single or double quotes to embed
    // characters that would otherwise be interpreted as syntax ('?', '!', '|', ':', '(', ')', '{',
    // '}', and '..'). The quote character is doubled to represent itself inside the same quote
    // style — e.g. 'it''s' and "say ""hi""" yield the raw values "it's" and 'say "hi"'. Single
    // and double quotes are fully interchangeable, and either quote style is literal inside the
    // other ('say "hi"' needs no escape). Quoting can be partial: foo"?"bar yields foo?bar.
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
                char ch = template[j];
                if (ch == '\'' || ch == '"')
                {
                    j = SkipQuoted(template, j, tokenStart) - 1;
                    continue;
                }

                if (ch == '}')
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

            // '|' introduces the literal fallback. Everything after the first unquoted '|' is the
            // default value; an empty tail makes the expression optional (empty on miss). The tail
            // may use the same '…' / "…" quoting that path segments use — quoting is the way to
            // embed characters that would otherwise terminate the enclosing context (e.g. a '}'
            // inside a fmt placeholder) or preserve explicit leading/trailing whitespace.
            string? literalDefault = null;
            int pipeIndex = IndexOfUnquoted(expression, '|', 0, tokenStart);
            if (pipeIndex >= 0)
            {
                string rawTail = expression.Substring(pipeIndex + 1);
                // Validate that any quotes in the literal tail are balanced (throws on unterminated).
                IndexOfUnquoted(rawTail, '\0', 0, tokenStart);
                literalDefault = UnquoteSegment(rawTail);
                expression = expression.Substring(0, pipeIndex).TrimEnd();
                if (expression.Length == 0)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, tokenStart));
                }
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

            return ValueToken.Reference(items, literalDefault, isStrict, isFromRef);
        }

        private static int ParseReferenceItem(string expression, int i, int tokenStart, List<ReferenceItem> items)
        {
            int start = i;
            while (i < expression.Length && expression[i] != '?')
            {
                char ch = expression[i];
                if (ch == '\'' || ch == '"')
                {
                    i = SkipQuoted(expression, i, tokenStart);
                    continue;
                }
                i++;
            }

            string refPath = expression.Substring(start, i - start).Trim();
            if (refPath.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

            // Parse leading ".." segments as parent hops. The remaining text (if any) is an absolute
            // suffix appended to the Nth ancestor of the literal's storage key at resolution time.
            // A quoted segment never starts with an unquoted '.', so quoted ".." is correctly
            // treated as a literal segment rather than a hop.
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

            string suffix = BuildSuffix(refPath, cursor, tokenStart);

            if (parentHops == 0 && suffix.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

            items.Add(new ReferenceItem(suffix, parentHops));
            return i;
        }

        // Walks path[start..], splits on unquoted ':' delimiters, rejects literal ".." segments
        // (quoted ".." is allowed and becomes a literal segment value), and returns the raw
        // (unquoted) path joined by ':' — ready for lookup against a provider's underlying keys.
        private static string BuildSuffix(string path, int start, int tokenStart)
        {
            bool anyQuotes = false;
            for (int k = start; k < path.Length; k++)
            {
                if (path[k] == '\'' || path[k] == '"')
                {
                    anyQuotes = true;
                    break;
                }
            }

            if (!anyQuotes)
            {
                string suffix = start == 0 ? path : path.Substring(start);
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

                return suffix;
            }

            var result = new StringBuilder();
            int segStart = start;
            int j = start;
            while (j <= path.Length)
            {
                if (j == path.Length || path[j] == ':')
                {
                    string raw = path.Substring(segStart, j - segStart);
                    if (raw == "..")
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
                    }

                    if (result.Length > 0)
                    {
                        result.Append(ConfigurationPath.KeyDelimiter[0]);
                    }

                    result.Append(UnquoteSegment(raw));
                    segStart = j + 1;
                    j++;
                    continue;
                }

                if (path[j] == '\'' || path[j] == '"')
                {
                    j = SkipQuoted(path, j, tokenStart);
                    continue;
                }

                j++;
            }

            return result.ToString();
        }

        // Advance past a single- or double-quoted literal starting at expression[i]. Returns the
        // index immediately after the closing quote. Doubled quotes inside the same style are
        // treated as an escape and do not terminate the literal.
        private static int SkipQuoted(string expression, int i, int tokenStart)
        {
            char quote = expression[i];
            i++;
            while (i < expression.Length)
            {
                if (expression[i] == quote)
                {
                    if (i + 1 < expression.Length && expression[i + 1] == quote)
                    {
                        i += 2;
                        continue;
                    }

                    return i + 1;
                }

                i++;
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
        }

        // Returns the index of the first occurrence of target in expression at or after start,
        // skipping characters inside quoted regions. Returns -1 if not found.
        private static int IndexOfUnquoted(string expression, char target, int start, int tokenStart)
        {
            int j = start;
            while (j < expression.Length)
            {
                char ch = expression[j];
                if (ch == '\'' || ch == '"')
                {
                    j = SkipQuoted(expression, j, tokenStart);
                    continue;
                }

                if (ch == target)
                {
                    return j;
                }

                j++;
            }

            return -1;
        }

        // Strip quoting from a path segment. Quotes may be fully surrounding (e.g. "abc") or
        // partial (e.g. foo"?"bar). Within a quoted run, a doubled quote character yields one
        // literal quote. The other quote style is literal inside a run.
        private static string UnquoteSegment(string segment)
        {
            if (segment.Length == 0 || (segment.IndexOf('\'') < 0 && segment.IndexOf('"') < 0))
            {
                return segment;
            }

            var sb = new StringBuilder(segment.Length);
            int i = 0;
            while (i < segment.Length)
            {
                char c = segment[i];
                if (c != '\'' && c != '"')
                {
                    sb.Append(c);
                    i++;
                    continue;
                }

                char quote = c;
                i++;
                while (i < segment.Length)
                {
                    if (segment[i] == quote)
                    {
                        if (i + 1 < segment.Length && segment[i + 1] == quote)
                        {
                            sb.Append(quote);
                            i += 2;
                            continue;
                        }

                        i++;
                        break;
                    }

                    sb.Append(segment[i]);
                    i++;
                }
            }

            return sb.ToString();
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

        private ValueToken(ValueTokenKind kind, string value, IReadOnlyList<ReferenceItem> items, string? literalDefault, bool isStrict, bool isFromRef)
        {
            Kind = kind;
            Value = value;
            Items = items;
            LiteralDefault = literalDefault;
            IsStrict = isStrict;
            IsFromRef = isFromRef;
        }

        internal static ValueToken Literal(string text) =>
            new(ValueTokenKind.Literal, text, s_noItems, literalDefault: null, isStrict: false, isFromRef: false);

        internal static ValueToken Reference(IReadOnlyList<ReferenceItem> items, string? literalDefault, bool isStrict, bool isFromRef)
        {
            string first = items.Count > 0 ? items[0].Value : string.Empty;
            return new ValueToken(ValueTokenKind.Reference, first, items, literalDefault, isStrict, isFromRef);
        }

        internal ValueTokenKind Kind { get; }

        // For Literal tokens: the literal text.
        // For Reference tokens: the first reference path (convenience).
        internal string Value { get; }

        internal IReadOnlyList<ReferenceItem> Items { get; }

        // Non-null iff the expression contained an explicit '|' literal-default tail. The empty
        // string corresponds to a bare '|' (equivalent to the old "optional" marker — empty on miss).
        // A non-empty value is emitted verbatim when every reference in Items misses.
        internal string? LiteralDefault { get; }

        // True when LiteralDefault is non-null: the expression cannot be "unresolvable", it falls
        // back to LiteralDefault (possibly empty).
        internal bool HasDefault => LiteralDefault is not null;

        internal bool IsStrict { get; }

        // True only for the single Reference token produced by a top-level ref(...) value. Placeholders
        // inside fmt(...) never set this flag — they are always string interpolations, even when the
        // template consists of a single {Key} placeholder.
        internal bool IsFromRef { get; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    // Value syntax:
    //   ref(<chain>)         — reference; may resolve to a section.
    //   format(<template>)   — string template.
    //   \ref(...) | \format(...)   — literal text; the leading backslash is stripped.
    //   any other value      — verbatim literal.
    //
    // chain (inside ref(...)):
    //   member ('?' member)* '?'?
    //   member := path | quoted-literal | format-call
    //   - quoted-literal and format-call are valid only as the LAST member.
    //   - A trailing bare '?' (after the last path member, no trailing literal/format)
    //     means "missing chain → empty string".
    //   - A chain ending with a literal/format member cannot also end with a bare '?'
    //     (parse error: redundant default).
    //   - An empty chain (`ref()`) is a parse error.
    //   - A chain consisting only of a trailing '?' (`ref(?)`) means empty-string fallback.
    //
    // path:
    //   Bare key. Use ':' (or '.') to separate segments. Use '..' segments
    //   to walk up from the value's own section. Reserved characters
    //   (`?`, `(`, `)`, `'`, `"`, `,`, ` `, `\`) inside a key must be backslash-escaped.
    //   `\` followed by any character emits that character verbatim into the key.
    //
    // quoted-literal:
    //   '...' or "...". Doubled-quote escapes (e.g. 'it''s'). Always succeeds as a
    //   chain default.
    //
    // format-call (inside chain):
    //   format(<template>). Always succeeds as a chain default.
    //
    // template (inside format(...)):
    //   Verbatim text with embedded ref(...) placeholders.
    //   - `\ref(` emits literal `ref(`.
    //   - `\\` emits literal `\`.
    //   - `\` followed by any other character emits that backslash and the next
    //     character verbatim.
    //   - All other characters (including parens, quotes, the bare keyword `format(`)
    //     are verbatim.
    //   - Embedded ref(...) placeholders MUST resolve to a scalar value;
    //     resolving to a section throws at resolution time.
    //
    // Sections:
    //   At the top level, ref(<chain>) may resolve to a section (alias). Sections
    //   are always strict — earlier-provider keys are not merged under the alias path.
    //   Embedded ref(...) inside format(...) MUST resolve to a scalar value.
    internal static class ReferenceParser
    {
        private const string RefOpen = "ref(";
        private const string FormatOpen = "format(";
        private const string EscapedRefOpen = "\\ref(";
        private const string EscapedFormatOpen = "\\format(";

        internal static bool ContainsReference(string? value)
        {
            if (value is null)
            {
                return false;
            }

            return StartsWith(value, RefOpen)
                || StartsWith(value, FormatOpen)
                || StartsWith(value, EscapedRefOpen)
                || StartsWith(value, EscapedFormatOpen);
        }

        internal static ValueToken Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // Top-level escape: \ref(...) and \format(...) → literal text with the leading
            // backslash stripped. Validate the parens balance and consume the entire value;
            // if not, throw to surface the typo rather than silently leaving a half-formed
            // escape verbatim.
            if (StartsWith(value, EscapedRefOpen))
            {
                ValidateBalancedToEnd(value, openParenIndex: 4, tokenStart: 0);
                return ValueToken.Literal(value.Substring(1));
            }

            if (StartsWith(value, EscapedFormatOpen))
            {
                ValidateBalancedToEnd(value, openParenIndex: 7, tokenStart: 0);
                return ValueToken.Literal(value.Substring(1));
            }

            if (StartsWith(value, RefOpen))
            {
                return ParseRef(value, openParenIndex: 3, tokenStart: 0);
            }

            if (StartsWith(value, FormatOpen))
            {
                return ParseFormat(value, openParenIndex: 6, tokenStart: 0);
            }

            return ValueToken.Literal(value);
        }

        private static bool StartsWith(string value, string prefix) =>
            value.Length >= prefix.Length && value.AsSpan(0, prefix.Length).SequenceEqual(prefix.AsSpan());

        // Parse a top-level (or chain-default) ref(<chain>) call. `openParenIndex` is the index
        // of the '('; the matching ')' must be the last character of `value` (we don't allow
        // trailing content after the close).
        private static ValueToken ParseRef(string value, int openParenIndex, int tokenStart)
        {
            int closeIndex = FindMatchingClose(value, openParenIndex, tokenStart);
            if (closeIndex != value.Length - 1)
            {
                throw new FormatException(
                    SR.Format(SR.ReferenceResolution_TrailingContentAfterClose, tokenStart + closeIndex + 1));
            }

            string content = value.Substring(openParenIndex + 1, closeIndex - openParenIndex - 1);
            return ParseChain(content, contentStart: tokenStart + openParenIndex + 1);
        }

        // Parse a top-level (or chain-default) format(<template>) call.
        private static ValueToken ParseFormat(string value, int openParenIndex, int tokenStart)
        {
            int closeIndex = FindMatchingClose(value, openParenIndex, tokenStart);
            if (closeIndex != value.Length - 1)
            {
                throw new FormatException(
                    SR.Format(SR.ReferenceResolution_TrailingContentAfterClose, tokenStart + closeIndex + 1));
            }

            string template = value.Substring(openParenIndex + 1, closeIndex - openParenIndex - 1);
            IReadOnlyList<ValueToken> tokens = ParseTemplate(template, templateStart: tokenStart + openParenIndex + 1);
            return ValueToken.Format(tokens);
        }

        // Find the ')' that matches the '(' at openParenIndex. Tracks nested parens; skips over
        // quoted literals and backslash escapes so they don't mismatch the brace counter.
        private static int FindMatchingClose(string value, int openParenIndex, int tokenStart)
        {
            int depth = 1;
            int i = openParenIndex + 1;
            while (i < value.Length)
            {
                char c = value[i];

                if (c == '\\')
                {
                    // Skip the escaped character (if any). Lone trailing '\' is consumed too;
                    // segment-level parsers will surface specific errors when they actually try
                    // to interpret the path/template body.
                    i += (i + 1 < value.Length) ? 2 : 1;
                    continue;
                }

                if (c == '\'' || c == '"')
                {
                    i = SkipQuoted(value, i, tokenStart + i);
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                    i++;
                    continue;
                }

                i++;
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, tokenStart + openParenIndex));
        }

        // Validate that an escaped form (\ref(...) / \format(...)) has balanced parens that
        // consume the entire value.
        private static void ValidateBalancedToEnd(string value, int openParenIndex, int tokenStart)
        {
            int closeIndex = FindMatchingClose(value, openParenIndex, tokenStart);
            if (closeIndex != value.Length - 1)
            {
                throw new FormatException(
                    SR.Format(SR.ReferenceResolution_TrailingContentAfterClose, tokenStart + closeIndex + 1));
            }
        }

        // Parse the chain content between ref's parens. `content` is the substring; `contentStart`
        // is its absolute offset for diagnostic positions.
        private static ValueToken ParseChain(string content, int contentStart)
        {
            // Empty chain: ref() → error.
            if (TrimWhitespace(content, out int trimmedStart, out int trimmedEnd) == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, contentStart));
            }

            var paths = new List<ReferenceItem>();
            ValueToken? defaultMember = null;
            bool hasEmptyDefault = false;

            int i = trimmedStart;
            int end = trimmedEnd;

            // Special form: ref(?) — a chain consisting solely of '?' (with optional surrounding
            // whitespace) means "empty string fallback, no path members".
            if (end - i == 1 && content[i] == '?')
            {
                return ValueToken.Reference(Array.Empty<ReferenceItem>(), @default: null, hasEmptyDefault: true);
            }

            while (i <= end)
            {
                // Skip leading whitespace before a member.
                while (i < end && IsWhite(content[i]))
                {
                    i++;
                }

                if (i == end)
                {
                    // Empty trailing member → trailing '?'.
                    hasEmptyDefault = true;
                    break;
                }

                char first = content[i];

                if (first == '?')
                {
                    // Empty member at start or between '?'s.
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + i));
                }

                if (first == '\'' || first == '"')
                {
                    int quoteEnd = SkipQuoted(content, i, contentStart + i);
                    string raw = content.Substring(i, quoteEnd - i);
                    string lit = UnquoteLiteral(raw);
                    defaultMember = ValueToken.Literal(lit);
                    i = SkipWhite(content, quoteEnd, end);
                    break;
                }

                if (IsFormatCallStart(content, i, end))
                {
                    int formatOpenIndex = i + FormatOpen.Length - 1; // index of '('
                    int formatClose = FindMatchingCloseInRange(content, formatOpenIndex, end, contentStart + i);
                    string template = content.Substring(formatOpenIndex + 1, formatClose - formatOpenIndex - 1);
                    IReadOnlyList<ValueToken> tokens = ParseTemplate(template, templateStart: contentStart + formatOpenIndex + 1);
                    defaultMember = ValueToken.Format(tokens);
                    i = SkipWhite(content, formatClose + 1, end);
                    break;
                }

                // Path member.
                int pathStart = i;
                while (i < end)
                {
                    char ch = content[i];
                    if (ch == '\\' && i + 1 < end)
                    {
                        i += 2;
                        continue;
                    }
                    if (ch == '?')
                    {
                        break;
                    }
                    i++;
                }

                int rawStart = pathStart;
                int rawEnd = i;
                // Trim trailing whitespace on the path text.
                while (rawEnd > rawStart && IsWhite(content[rawEnd - 1]))
                {
                    rawEnd--;
                }

                if (rawEnd == rawStart)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + pathStart));
                }

                ReferenceItem item = ParsePath(content, rawStart, rawEnd, contentStart + rawStart);
                paths.Add(item);

                if (i == end)
                {
                    break;
                }

                // content[i] == '?'
                i++;
            }

            // Trailing '?' after a literal/format default is redundant → error.
            if (defaultMember.HasValue)
            {
                int j = i;
                while (j < end && IsWhite(content[j]))
                {
                    j++;
                }
                if (j < end)
                {
                    if (content[j] == '?')
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_RedundantTrailingQuestion, contentStart + j));
                    }

                    // Anything else after a literal/format member is unexpected: literal/format
                    // is required to be the last chain member.
                    throw new FormatException(SR.Format(SR.ReferenceResolution_DefaultMustBeLast, contentStart + j));
                }
            }

            // Validate: cannot have both a trailing '?' and a literal/format default (would be
            // hasEmptyDefault==true after defaultMember was set, which the loop already handled
            // via the redundant-question branch above; this is defensive).
            if (hasEmptyDefault && defaultMember.HasValue)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_RedundantTrailingQuestion, contentStart));
            }

            IReadOnlyList<ValueToken>? defaultList = defaultMember.HasValue
                ? new[] { defaultMember.Value }
                : null;

            return ValueToken.Reference(paths, defaultList, hasEmptyDefault);
        }

        // Detect whether `content[i..]` begins with the literal text `format(` (i.e. a format-call
        // member). The `(` need not be balanced here; only its presence is required.
        private static bool IsFormatCallStart(string content, int i, int end)
        {
            int n = FormatOpen.Length;
            if (i + n > end)
            {
                return false;
            }
            for (int k = 0; k < n; k++)
            {
                if (content[i + k] != FormatOpen[k])
                {
                    return false;
                }
            }
            return true;
        }

        // Variant of FindMatchingClose that operates on a substring window [openParenIndex .. end).
        private static int FindMatchingCloseInRange(string content, int openParenIndex, int end, int tokenStart)
        {
            int depth = 1;
            int i = openParenIndex + 1;
            while (i < end)
            {
                char c = content[i];

                if (c == '\\')
                {
                    i += (i + 1 < end) ? 2 : 1;
                    continue;
                }

                if (c == '\'' || c == '"')
                {
                    i = SkipQuoted(content, i, tokenStart + (i - openParenIndex));
                    continue;
                }

                if (c == '(')
                {
                    depth++;
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                    i++;
                    continue;
                }

                i++;
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, tokenStart));
        }

        // Parse a path member from content[rawStart..rawEnd). Honors leading '..' segments and
        // backslash escapes. Returns a ReferenceItem with the suffix (':' delimited) and parent
        // hop count.
        private static ReferenceItem ParsePath(string content, int rawStart, int rawEnd, int tokenStart)
        {
            // Count leading '..' (followed by ':' or '.' or end) for parent hops.
            int parentHops = 0;
            int cursor = rawStart;
            while (cursor + 2 <= rawEnd && content[cursor] == '.' && content[cursor + 1] == '.')
            {
                int next = cursor + 2;
                if (next == rawEnd)
                {
                    parentHops++;
                    cursor = next;
                    break;
                }

                char sep = content[next];
                if (sep != ':' && sep != '.')
                {
                    break;
                }

                parentHops++;
                cursor = next + 1;
            }

            string suffix = BuildPathSuffix(content, cursor, rawEnd, tokenStart);

            if (parentHops == 0 && suffix.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

            return new ReferenceItem(suffix, parentHops);
        }

        // Build the post-parent-hops portion of a path: split on ':' or '.', apply backslash
        // escape, normalize to ConfigurationPath.KeyDelimiter.
        private static string BuildPathSuffix(string content, int start, int end, int tokenStart)
        {
            if (start >= end)
            {
                return string.Empty;
            }

            var result = new StringBuilder();
            var segment = new StringBuilder();
            int i = start;

            while (i < end)
            {
                char c = content[i];

                if (c == '\\')
                {
                    if (i + 1 >= end)
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart + i));
                    }
                    segment.Append(content[i + 1]);
                    i += 2;
                    continue;
                }

                if (c == ':' || c == '.')
                {
                    // '..' segments are only valid as a leading parent-hop marker (consumed in
                    // ParsePath before BuildPathSuffix runs). Detect a mid-path '..' as a '.'
                    // with an empty current segment whose next char is also '.'.
                    if (c == '.' && segment.Length == 0 && i + 1 < end && content[i + 1] == '.')
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart + i));
                    }

                    string seg = segment.ToString();
                    AppendSegment(result, seg, tokenStart + i);
                    segment.Clear();
                    i++;
                    continue;
                }

                segment.Append(c);
                i++;
            }

            string lastSeg = segment.ToString();
            AppendSegment(result, lastSeg, tokenStart + end);

            return result.ToString();
        }

        private static void AppendSegment(StringBuilder result, string segment, int tokenStart)
        {
            if (segment == "..")
            {
                // '..' is only valid as a leading parent-hop marker, not in the middle of a path.
                throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
            }

            if (result.Length > 0)
            {
                result.Append(ConfigurationPath.KeyDelimiter[0]);
            }

            result.Append(segment);
        }

        // Parse a format(<template>) body into a sequence of Literal/Reference tokens.
        // Anything except an unescaped `ref(` is verbatim. `\ref(` emits literal `ref(`,
        // `\\` emits literal `\`, `\X` for any other X emits `\X` verbatim.
        private static List<ValueToken> ParseTemplate(string template, int templateStart)
        {
            var tokens = new List<ValueToken>();
            var literal = new StringBuilder();

            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];

                if (c == '\\')
                {
                    if (i + 1 >= template.Length)
                    {
                        // Trailing lone backslash: emit verbatim.
                        literal.Append('\\');
                        i++;
                        continue;
                    }

                    if (template[i + 1] == '\\')
                    {
                        literal.Append('\\');
                        i += 2;
                        continue;
                    }

                    // \ref( → literal "ref(".
                    if (i + RefOpen.Length < template.Length + 1 &&
                        i + 1 + RefOpen.Length <= template.Length &&
                        template.AsSpan(i + 1, RefOpen.Length).SequenceEqual(RefOpen.AsSpan()))
                    {
                        literal.Append(RefOpen);
                        i += 1 + RefOpen.Length;
                        continue;
                    }

                    // \X for any other X: emit '\' and X verbatim.
                    literal.Append('\\');
                    literal.Append(template[i + 1]);
                    i += 2;
                    continue;
                }

                // Detect ref( placeholder.
                if (c == 'r' && i + RefOpen.Length <= template.Length &&
                    template.AsSpan(i, RefOpen.Length).SequenceEqual(RefOpen.AsSpan()))
                {
                    if (literal.Length > 0)
                    {
                        tokens.Add(ValueToken.Literal(literal.ToString()));
                        literal.Clear();
                    }

                    int openParenIndex = i + RefOpen.Length - 1;
                    int closeIndex = FindMatchingCloseInRange(template, openParenIndex, template.Length, templateStart + i);
                    string content = template.Substring(openParenIndex + 1, closeIndex - openParenIndex - 1);
                    ValueToken refToken = ParseChain(content, contentStart: templateStart + openParenIndex + 1);
                    tokens.Add(refToken);
                    i = closeIndex + 1;
                    continue;
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

        private static int SkipQuoted(string s, int i, int tokenStart)
        {
            char quote = s[i];
            i++;
            while (i < s.Length)
            {
                if (s[i] == quote)
                {
                    if (i + 1 < s.Length && s[i + 1] == quote)
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

        private static string UnquoteLiteral(string raw)
        {
            // raw includes the enclosing quotes.
            if (raw.Length < 2)
            {
                return string.Empty;
            }

            char quote = raw[0];
            var sb = new StringBuilder(raw.Length - 2);
            int i = 1;
            int last = raw.Length - 1;
            while (i < last)
            {
                if (raw[i] == quote && i + 1 < last && raw[i + 1] == quote)
                {
                    sb.Append(quote);
                    i += 2;
                    continue;
                }

                sb.Append(raw[i]);
                i++;
            }

            return sb.ToString();
        }

        private static int TrimWhitespace(string s, out int start, out int end)
        {
            start = 0;
            end = s.Length;
            while (start < end && IsWhite(s[start]))
            {
                start++;
            }
            while (end > start && IsWhite(s[end - 1]))
            {
                end--;
            }
            return end - start;
        }

        private static int SkipWhite(string s, int i, int end)
        {
            while (i < end && IsWhite(s[i]))
            {
                i++;
            }
            return i;
        }

        private static bool IsWhite(char c) => c == ' ' || c == '\t' || c == '\r' || c == '\n';
    }

    internal enum ValueTokenKind
    {
        Literal,
        Reference,
        Format,
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
        private static readonly IReadOnlyList<ValueToken> s_noTokens = Array.Empty<ValueToken>();

        private ValueToken(
            ValueTokenKind kind,
            string value,
            IReadOnlyList<ReferenceItem> items,
            IReadOnlyList<ValueToken>? @default,
            bool hasEmptyDefault,
            IReadOnlyList<ValueToken> tokens)
        {
            Kind = kind;
            Value = value;
            Items = items;
            Default = @default;
            HasEmptyDefault = hasEmptyDefault;
            Tokens = tokens;
        }

        internal static ValueToken Literal(string text) =>
            new(ValueTokenKind.Literal, text, s_noItems, @default: null, hasEmptyDefault: false, s_noTokens);

        internal static ValueToken Reference(
            IReadOnlyList<ReferenceItem> items,
            IReadOnlyList<ValueToken>? @default,
            bool hasEmptyDefault)
        {
            string first = items.Count > 0 ? items[0].Value : string.Empty;
            return new ValueToken(ValueTokenKind.Reference, first, items, @default, hasEmptyDefault, s_noTokens);
        }

        internal static ValueToken Format(IReadOnlyList<ValueToken> tokens) =>
            new(ValueTokenKind.Format, string.Empty, s_noItems, @default: null, hasEmptyDefault: false, tokens);

        internal ValueTokenKind Kind { get; }

        // For Literal tokens: the literal text.
        // For Reference tokens: the first chain path's textual value (convenience for diagnostics).
        // For Format tokens: empty.
        internal string Value { get; }

        // Reference: chain path members in left-to-right coalesce order.
        internal IReadOnlyList<ReferenceItem> Items { get; }

        // Reference: non-null when the chain ends with a quoted-literal or format-call default.
        // The list always contains exactly one element (a Literal or a Format token), encoded
        // as a list to share the resolution code path with templates. Null when the chain has
        // no explicit default.
        internal IReadOnlyList<ValueToken>? Default { get; }

        // Reference: true when the chain ends with a bare '?' (empty-string fallback). Mutually
        // exclusive with Default at parse time.
        internal bool HasEmptyDefault { get; }

        // Format: inner Literal / Reference tokens.
        internal IReadOnlyList<ValueToken> Tokens { get; }

        // True when this Reference token has any path member to try.
        internal bool HasHead => Items.Count > 0;

        internal bool HasDefault => Default is not null || HasEmptyDefault;
    }
}

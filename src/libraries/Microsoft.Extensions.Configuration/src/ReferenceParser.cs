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
    //   any other value      — verbatim literal.
    //
    // chain (inside ref(...) and inside `{...}` placeholders):
    //   member ('?' member)* ('??' default)?
    //   - '?' separates member lookups (left-to-right; first existing wins).
    //   - '??' (double question mark) introduces a single trailing default. The default body
    //     uses template grammar (literal text + `{<chain>}` placeholders + `{{`/`}}` brace
    //     escapes). A '??' with no body means empty-string default.
    //   - At least one member is required before '??'.
    //   - An empty chain (`ref()`) is a parse error.
    //   - A bare trailing single '?' (no second '?') is a parse error: use '??' for empty default.
    //
    // member:
    //   - Unquoted: a path. Use ':' (or '.') to separate segments. Use '..' segments to walk
    //     up from the value's own section. Reserved characters (`?`, `(`, `)`, `'`, `"`,
    //     `{`, `}`, whitespace) cannot appear in an unquoted path — use a quoted member to
    //     embed them. Surrounding whitespace is trimmed.
    //   - Quoted: `'…'` or `"…"`. The body is a single literal key — no ':' splitting. The
    //     same quote char doubled inside the body escapes itself (e.g. `'it''s'` → `it's`).
    //
    // default (after '??'):
    //   - Unquoted: leading and trailing whitespace are trimmed; the trimmed body is parsed
    //     as a template.
    //   - Quoted: `'…'` or `"…"`. The body is preserved verbatim (whitespace included);
    //     doubled-quote inside escapes itself; the body is then parsed as a template.
    //
    // template (inside format(...) and after '??' inside a chain):
    //   Verbatim text with embedded {<chain>} placeholders.
    //   - `{{` emits a literal `{`; `}}` emits a literal `}` (composite-format-style doubling).
    //   - A single `{` opens a placeholder; the matching `}` closes it. The content is a
    //     chain (same grammar as inside ref(...)). Embedded placeholders MUST resolve to a
    //     scalar value; resolving to a section throws at resolution time.
    //   - All other characters are verbatim.
    //
    // format(...) body wrapping:
    //   - Unquoted: leading and trailing whitespace are trimmed before template parsing.
    //   - Quoted: `'…'` or `"…"`. The body is preserved verbatim (whitespace included);
    //     doubled-quote inside escapes itself; the body is then parsed as a template.
    //
    // Sections:
    //   At the top level, ref(<chain>) may resolve to a section (alias). Sections
    //   are always strict — earlier-provider keys are not merged under the alias path.
    //   Embedded `{...}` placeholders inside format(...) MUST resolve to a scalar value.
    internal static class ReferenceParser
    {
        private const string RefOpen = "ref(";
        private const string FormatOpen = "format(";

        internal static bool ContainsReference(string? value)
        {
            if (value is null)
            {
                return false;
            }

            return StartsWith(value, RefOpen)
                || StartsWith(value, FormatOpen);
        }

        internal static ValueToken Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

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

            int bodyStart = openParenIndex + 1;
            int bodyEnd = closeIndex;
            int contentStart = tokenStart;

            ParseTrimmableTemplateBody(value, bodyStart, bodyEnd, contentStart, out IReadOnlyList<ValueToken> tokens);
            return ValueToken.Format(tokens);
        }

        // Apply trim-or-quote logic shared by format(...) bodies and chain defaults.
        // - If body (after skipping leading whitespace) starts with a quote, the entire body
        //   must be a single quoted string (whitespace allowed before/after quotes only); the
        //   quoted text is preserved verbatim.
        // - Otherwise, leading and trailing whitespace are trimmed.
        // The resulting body is parsed as a template.
        private static void ParseTrimmableTemplateBody(
            string content,
            int bodyStart,
            int bodyEnd,
            int contentStart,
            out IReadOnlyList<ValueToken> tokens)
        {
            int i = bodyStart;
            while (i < bodyEnd && IsWhite(content[i]))
            {
                i++;
            }

            if (i < bodyEnd && (content[i] == '\'' || content[i] == '"'))
            {
                int quoteStart = i;
                string body = ScanQuotedBody(content, ref i, contentStart);

                while (i < bodyEnd && IsWhite(content[i]))
                {
                    i++;
                }

                if (i < bodyEnd)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_TrailingContentAfterQuotedDefault, contentStart + i));
                }

                // Use the position just past the opening quote as the diagnostic base. Doubled
                // quote escapes inside the body may shift offsets by ±1 per occurrence.
                tokens = ParseTemplate(body, templateStart: contentStart + quoteStart + 1);
                return;
            }

            int unquotedStart = i;
            int unquotedEnd = bodyEnd;
            while (unquotedEnd > unquotedStart && IsWhite(content[unquotedEnd - 1]))
            {
                unquotedEnd--;
            }

            string unquoted = unquotedStart >= unquotedEnd
                ? string.Empty
                : content.Substring(unquotedStart, unquotedEnd - unquotedStart);
            tokens = ParseTemplate(unquoted, templateStart: contentStart + unquotedStart);
        }

        // Find the ')' that matches the '(' at openParenIndex. Tracks nested parens and skips
        // over quoted regions ('...' or "...") so a `)` inside a quoted chain member is literal.
        private static int FindMatchingClose(string value, int openParenIndex, int tokenStart)
        {
            int depth = 1;
            int i = openParenIndex + 1;
            while (i < value.Length)
            {
                char c = value[i];

                if (c == '\'' || c == '"')
                {
                    i = SkipQuoted(value, i, tokenStart);
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

        // Advances past a quoted region starting at i (which must be at the opening quote).
        // Returns the index just past the closing quote. Doubled quote chars are part of the
        // body. Throws on unterminated quote.
        private static int SkipQuoted(string s, int i, int tokenStart)
        {
            char q = s[i];
            int openPos = i;
            i++;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == q)
                {
                    if (i + 1 < s.Length && s[i + 1] == q)
                    {
                        i += 2;
                        continue;
                    }
                    return i + 1;
                }
                i++;
            }
            throw new FormatException(SR.Format(SR.ReferenceResolution_UnterminatedQuote, tokenStart + openPos));
        }

        // Scans a quoted member starting at content[i] (must be a quote). On success returns
        // the unescaped body and advances i past the closing quote.
        private static string ScanQuotedBody(string content, ref int i, int contentStart)
        {
            char q = content[i];
            int openPos = i;
            i++;
            var sb = new StringBuilder();
            while (i < content.Length)
            {
                char c = content[i];
                if (c == q)
                {
                    if (i + 1 < content.Length && content[i + 1] == q)
                    {
                        sb.Append(q);
                        i += 2;
                        continue;
                    }
                    i++;
                    return sb.ToString();
                }
                sb.Append(c);
                i++;
            }
            throw new FormatException(SR.Format(SR.ReferenceResolution_UnterminatedQuote, contentStart + openPos));
        }

        // Parse the chain content between ref's parens. `content` is the substring; `contentStart`
        // is its absolute offset for diagnostic positions.
        private static ValueToken ParseChain(string content, int contentStart)
        {
            // Empty chain: ref() → error. We only reject all-whitespace here; we DO NOT trim
            // the content overall, because trailing whitespace after '??' is part of the
            // default-template body and must be preserved.
            int leadingWhite = 0;
            while (leadingWhite < content.Length && IsWhite(content[leadingWhite]))
            {
                leadingWhite++;
            }
            if (leadingWhite == content.Length)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, contentStart));
            }

            var paths = new List<ReferenceItem>();
            IReadOnlyList<ValueToken>? defaultTokens = null;
            bool hasEmptyDefault = false;

            int i = leadingWhite;
            int end = content.Length;

            while (i < end)
            {
                // Skip leading whitespace before a path member.
                while (i < end && IsWhite(content[i]))
                {
                    i++;
                }

                if (i >= end)
                {
                    // Trailing single '?' (no second '?'): error — '??' is required for empty default.
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + i));
                }

                if (content[i] == '?')
                {
                    // Leading '?' or '??' before any path member: at least one path is required.
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + i));
                }

                if (content[i] == '\'' || content[i] == '"')
                {
                    // Quoted key member: body is the literal key (no ':' splitting, no '\' escape;
                    // the doubled-quote escape consumed by ScanQuotedBody is the only special form).
                    int quoteStart = i;
                    string quotedBody = ScanQuotedBody(content, ref i, contentStart);
                    if (quotedBody.Length == 0)
                    {
                        throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + quoteStart));
                    }
                    paths.Add(new ReferenceItem(quotedBody, parentHops: 0));
                }
                else
                {
                    // Unquoted path member: scan until '?'. Reserved characters cannot appear
                    // in an unquoted path; use a quoted member ('…' / "…") to embed them.
                    int pathStart = i;
                    while (i < end)
                    {
                        char ch = content[i];
                        if (ch == '?')
                        {
                            break;
                        }
                        i++;
                    }

                    int rawStart = pathStart;
                    int rawEnd = i;
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
                }

                // Allow whitespace between a member (especially a quoted one) and the next separator.
                while (i < end && IsWhite(content[i]))
                {
                    i++;
                }

                if (i >= end)
                {
                    break;
                }

                // content[i] should be '?' here. Anything else (e.g., extra content after a
                // quoted member) is malformed.
                if (content[i] != '?')
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, contentStart + i));
                }

                // Check for '??' (default introducer).
                if (i + 1 < end && content[i + 1] == '?')
                {
                    int defaultBodyStart = i + 2;
                    ParseDefaultBody(content, defaultBodyStart, end, contentStart, out defaultTokens, out hasEmptyDefault);
                    break;
                }

                // Single '?' separator → continue to next path. Track the position of this
                // separator so we can report a clear error if the chain ends here (a bare
                // trailing single '?' is not a valid chain — '??' is required for an empty default).
                int separatorPos = i;
                i++;
                if (i >= end)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, contentStart + separatorPos));
                }
            }

            return ValueToken.Reference(paths, defaultTokens, hasEmptyDefault);
        }

        // Parse the default-template body that follows '??'. May be quoted (preserves verbatim
        // body, including leading/trailing whitespace; doubled-quote escapes the quote char) or
        // Parse a chain default body into a token sequence. Defaults to a trimmed unquoted body
        // unless quoted (`'…'` or `"…"`), in which case the body is preserved verbatim. Either
        // way, the body is then parsed as a composite-format template ({...} placeholders +
        // {{ }} brace escapes).
        private static void ParseDefaultBody(
            string content,
            int bodyStart,
            int end,
            int contentStart,
            out IReadOnlyList<ValueToken>? defaultTokens,
            out bool hasEmptyDefault)
        {
            ParseTrimmableTemplateBody(content, bodyStart, end, contentStart, out IReadOnlyList<ValueToken> tokens);
            if (tokens.Count == 0)
            {
                defaultTokens = null;
                hasEmptyDefault = true;
            }
            else
            {
                defaultTokens = tokens;
                hasEmptyDefault = false;
            }
        }

        // Parse a path member from content[rawStart..rawEnd). Honors leading '..' segments.
        // Returns a ReferenceItem with the suffix (':' delimited) and parent hop count.
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

        // Build the post-parent-hops portion of a path: split on ':' or '.', normalize to
        // ConfigurationPath.KeyDelimiter.
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
        // - `{{` and `}}` are composite-format-style escapes for literal `{` and `}`.
        // - A single `{` opens a placeholder whose content is a chain (same grammar as
        //   inside ref(...)); the matching `}` closes it.
        // - All other characters are verbatim.
        private static List<ValueToken> ParseTemplate(string template, int templateStart)
        {
            var tokens = new List<ValueToken>();
            var literal = new StringBuilder();

            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];

                if (c == '{')
                {
                    // `{{` → literal `{`.
                    if (i + 1 < template.Length && template[i + 1] == '{')
                    {
                        literal.Append('{');
                        i += 2;
                        continue;
                    }

                    if (literal.Length > 0)
                    {
                        tokens.Add(ValueToken.Literal(literal.ToString()));
                        literal.Clear();
                    }

                    int braceClose = FindMatchingBrace(template, i, templateStart + i);
                    string content = template.Substring(i + 1, braceClose - i - 1);
                    ValueToken refToken = ParseChain(content, contentStart: templateStart + i + 1);
                    tokens.Add(refToken);
                    i = braceClose + 1;
                    continue;
                }

                if (c == '}')
                {
                    // `}}` → literal `}`. A bare `}` is an error: unbalanced placeholder close.
                    if (i + 1 < template.Length && template[i + 1] == '}')
                    {
                        literal.Append('}');
                        i += 2;
                        continue;
                    }

                    throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, templateStart + i));
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

        // Find the `}` that matches the `{` at openIndex within a format template.
        // Inside a placeholder body the grammar is a CHAIN: paths separated by '?',
        // optionally followed by '??' which switches the rest of that level into TEMPLATE
        // mode (where '{{' / '}}' are escapes for literal '{' / '}'). Nested '{...}'
        // placeholders push a fresh chain scope.
        private static int FindMatchingBrace(string template, int openIndex, int tokenStart)
        {
            // templateModeStack[top] == false → currently in path-part of the chain at this depth.
            // templateModeStack[top] == true  → past '??' at this depth, in template-part.
            var templateMode = new Stack<bool>();
            templateMode.Push(false);
            int i = openIndex + 1;

            while (i < template.Length)
            {
                char c = template[i];

                if (c == '\'' || c == '"')
                {
                    // Quoted region (member or default body) — skip opaquely so any '{', '}',
                    // '?' or paren inside is literal at this lexer level.
                    i = SkipQuoted(template, i, tokenStart);
                    continue;
                }

                bool inTemplate = templateMode.Peek();

                if (inTemplate)
                {
                    // `{{` → literal `{`.
                    if (c == '{' && i + 1 < template.Length && template[i + 1] == '{')
                    {
                        i += 2;
                        continue;
                    }
                    // `}}` → literal `}`.
                    if (c == '}' && i + 1 < template.Length && template[i + 1] == '}')
                    {
                        i += 2;
                        continue;
                    }
                }
                else if (c == '?'
                    && i + 1 < template.Length
                    && template[i + 1] == '?')
                {
                    // First '??' at this depth: switch to template-part for the remainder
                    // of this chain level.
                    templateMode.Pop();
                    templateMode.Push(true);
                    i += 2;
                    continue;
                }

                if (c == '{')
                {
                    templateMode.Push(false);
                    i++;
                    continue;
                }

                if (c == '}')
                {
                    templateMode.Pop();
                    if (templateMode.Count == 0)
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

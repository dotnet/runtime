// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    // Value syntax:
    //   {{ path-expression }}      — the whole value is a reference / template expression.
    //                                A value starting with "{{" is always parsed as a gate;
    //                                if the braces cannot be balanced a FormatException is
    //                                thrown. Values that don't start with "{{" are verbatim
    //                                literals.
    //
    // Brace rule inside gate / template bodies:
    //   1 consecutive '{'  → literal '{'
    //   2 consecutive '{{' → structural opener (nested placeholder)
    //   n ≥ 3 consecutive  → escape; emits (n-1) literal '{' characters
    //   Same symmetrically for '}', with one exception: at the gate-close boundary
    //   (depth 0) a run of n ≥ 2 '}' consumes the final two as the closing '}}' and
    //   emits the preceding (n-2) as literal '}' content. That lets a template end with
    //   a literal '}' immediately before the gate close — e.g. "{{|{Host}}}" yields the
    //   literal "{Host}" without needing a quoted separator.
    //
    // A literal '{' or '}' in config data never needs escaping on its own — only runs
    // that could be confused with structural markers do.
    //
    // Inside the gate, the expression grammar is:
    //   (path ('?' path)*)?  ('!')?  ('|' template-tail)?
    //
    //   - A non-empty head is one or more paths joined by '?'. Paths are tried left-to-right;
    //     the first one that resolves wins.
    //   - A trailing '!' on the head marks the reference as a strict section alias (when the
    //     referent is a section, no earlier-provider keys are merged under the alias path).
    //   - '|' introduces a fallback template. If the head is empty or every path in the head
    //     misses, the template is evaluated and its result is returned.
    //   - The template grammar is the same as the body of a top-level "{{|…}}": literal
    //     characters plus nested "{{path-expression}}" placeholders, with the same brace
    //     counting / escaping rule recursively.
    //
    // Path segments and template literals may both be quoted with single or double quotes to
    // embed characters that would otherwise be interpreted as syntax ('?', '!', '|', ':',
    // '..'). The quote character is doubled to represent itself inside the same quote style —
    // e.g. 'it''s' and "say ""hi""" yield the raw values "it's" and 'say "hi"'. Single and
    // double quotes are fully interchangeable, and either style is literal inside the other
    // ('say "hi"' needs no escape). Quoting can be partial: foo"?"bar yields foo?bar.
    internal static class ReferenceParser
    {
        internal static bool ContainsReference(string? value) => IsGated(value);

        internal static IReadOnlyList<ValueToken> Parse(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            if (!IsGated(value))
            {
                return new[] { ValueToken.Literal(value) };
            }

            // Value starts with "{{". Commit to gate parsing; any brace imbalance will throw.
            int close = FindPlaceholderEnd(value, start: 2, tokenStart: 0);
            if (close + 2 != value.Length)
            {
                throw new FormatException(
                    SR.Format(SR.ReferenceResolution_TrailingContentAfterClose, close + 2));
            }

            string content = value.Substring(2, close - 2);
            return new[] { ParseGateContent(content, tokenStart: 2) };
        }

        private static bool IsGated(string? value) =>
            value is not null && value.Length >= 4 && value[0] == '{' && value[1] == '{';

        // Parses the content between the outermost {{ and }} of a gated value. Produces a single
        // ValueToken whose Items is the coalesce chain (possibly empty) and whose LiteralDefault
        // is the parsed template tail after '|' (null if no '|' is present).
        private static ValueToken ParseGateContent(string content, int tokenStart)
        {
            // Split head / template-tail on the first unquoted '|' at depth 0.
            int pipeIndex = IndexOfTopLevelPipe(content, tokenStart);

            string head;
            IReadOnlyList<ValueToken>? templateTail;
            if (pipeIndex >= 0)
            {
                head = content.Substring(0, pipeIndex).Trim();
                string rawTail = content.Substring(pipeIndex + 1);
                templateTail = ParseTemplate(rawTail, outerStart: tokenStart + pipeIndex + 1);
            }
            else
            {
                head = content.Trim();
                templateTail = null;
            }

            bool isStrict = false;
            if (head.Length > 0 && head[head.Length - 1] == '!')
            {
                isStrict = true;
                head = head.Substring(0, head.Length - 1).TrimEnd();
            }

            var items = new List<ReferenceItem>();
            if (head.Length > 0)
            {
                ParseCoalesceChain(head, tokenStart, items);
            }
            else if (templateTail is null)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsEmpty, tokenStart));
            }

            return ValueToken.Reference(items, templateTail, isStrict);
        }

        private static void ParseCoalesceChain(string head, int tokenStart, List<ReferenceItem> items)
        {
            int i = 0;
            while (i <= head.Length)
            {
                while (i < head.Length && char.IsWhiteSpace(head[i]))
                {
                    i++;
                }

                if (i >= head.Length)
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
                }

                i = ParseReferenceItem(head, i, tokenStart, items);

                while (i < head.Length && char.IsWhiteSpace(head[i]))
                {
                    i++;
                }

                if (i >= head.Length)
                {
                    break;
                }

                if (head[i] != '?')
                {
                    throw new FormatException(SR.Format(SR.ReferenceResolution_InvalidExpression, tokenStart));
                }

                i++;
            }
        }

        private static int ParseReferenceItem(string head, int i, int tokenStart, List<ReferenceItem> items)
        {
            int start = i;
            while (i < head.Length && head[i] != '?')
            {
                char ch = head[i];
                if (ch == '\'' || ch == '"')
                {
                    i = SkipQuoted(head, i, tokenStart);
                    continue;
                }
                i++;
            }

            string refPath = head.Substring(start, i - start).Trim();
            if (refPath.Length == 0)
            {
                throw new FormatException(SR.Format(SR.ReferenceResolution_KeyIsEmpty, tokenStart));
            }

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

        // Scan a template body, producing a sequence of Literal and Reference tokens.
        // Applies the unified brace rule:
        //   '{' solo  → literal
        //   '{{'      → opens a placeholder (found-end recursed)
        //   '{{{'+    → escape: emit (n-1) literal '{', skip n chars
        //   mirror for '}'
        private static List<ValueToken> ParseTemplate(string template, int outerStart)
        {
            var tokens = new List<ValueToken>();
            var literal = new StringBuilder();

            int i = 0;
            while (i < template.Length)
            {
                char c = template[i];

                if (c == '\'' || c == '"')
                {
                    int quoteEnd = SkipQuoted(template, i, outerStart + i);
                    literal.Append(UnquoteSegment(template.Substring(i, quoteEnd - i)));
                    i = quoteEnd;
                    continue;
                }

                if (c == '{')
                {
                    int run = CountRun(template, i, '{');
                    if (run == 1)
                    {
                        literal.Append('{');
                        i++;
                        continue;
                    }

                    if (run >= 3)
                    {
                        // Escape: emit (run-1) literal '{' and advance past the whole run.
                        literal.Append('{', run - 1);
                        i += run;
                        continue;
                    }

                    // run == 2 → placeholder opens
                    if (literal.Length > 0)
                    {
                        tokens.Add(ValueToken.Literal(literal.ToString()));
                        literal.Clear();
                    }

                    int placeholderStart = i + 2;
                    int placeholderEnd = FindPlaceholderEnd(template, placeholderStart, outerStart + i);
                    string expression = template.Substring(placeholderStart, placeholderEnd - placeholderStart);

                    tokens.Add(ParseGateContent(expression, tokenStart: outerStart + i + 2));

                    i = placeholderEnd + 2; // skip the closing "}}"
                    continue;
                }

                if (c == '}')
                {
                    int run = CountRun(template, i, '}');
                    if (run == 1)
                    {
                        literal.Append('}');
                        i++;
                        continue;
                    }

                    if (run >= 3)
                    {
                        literal.Append('}', run - 1);
                        i += run;
                        continue;
                    }

                    // run == 2 with no matching opener — unbalanced.
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

        // Find the closing '}}' for a placeholder that opened at template[start-2..start]. Returns
        // the index of the first '}' of the closing pair. Tracks depth for nested '{{…}}' placeholders
        // and skips quoted regions and brace-escape runs (3+).
        private static int FindPlaceholderEnd(string template, int start, int tokenStart)
        {
            int depth = 0;
            int j = start;
            while (j < template.Length)
            {
                char ch = template[j];

                if (ch == '\'' || ch == '"')
                {
                    j = SkipQuoted(template, j, tokenStart);
                    continue;
                }

                if (ch == '{')
                {
                    int run = CountRun(template, j, '{');
                    if (run == 1)
                    {
                        j++;
                        continue;
                    }
                    if (run >= 3)
                    {
                        j += run; // escape
                        continue;
                    }
                    depth++;
                    j += 2;
                    continue;
                }

                if (ch == '}')
                {
                    int run = CountRun(template, j, '}');
                    if (run == 1)
                    {
                        j++;
                        continue;
                    }

                    // A run of n ≥ 2 '}' can close the current placeholder and, when long
                    // enough, additionally close surrounding placeholders up to the outer
                    // gate. A run of n ≥ 2(depth+1) closes everything including the outer
                    // gate; the final two chars are the gate close, anything in between is
                    // literal '}' content. A shorter run only closes inner levels.
                    int needed = 2 * (depth + 1);
                    if (run >= needed)
                    {
                        return j + (run - 2);
                    }

                    depth -= run / 2;
                    j += run;
                    continue;
                }

                j++;
            }

            throw new FormatException(SR.Format(SR.ReferenceResolution_ExpressionIsUnclosed, tokenStart));
        }

        private static int CountRun(string s, int start, char c)
        {
            int n = 0;
            while (start + n < s.Length && s[start + n] == c)
            {
                n++;
            }
            return n;
        }

        private static int IndexOfTopLevelPipe(string content, int tokenStart)
        {
            int j = 0;
            while (j < content.Length)
            {
                char ch = content[j];
                if (ch == '\'' || ch == '"')
                {
                    j = SkipQuoted(content, j, tokenStart);
                    continue;
                }

                if (ch == '{')
                {
                    int run = CountRun(content, j, '{');
                    if (run >= 2)
                    {
                        // Nested placeholder or escape — skip it wholesale. For an escape we
                        // simply advance past the run; for a real placeholder we find the
                        // matching '}}' to stay out of its interior.
                        if (run == 2)
                        {
                            int endInner = FindPlaceholderEnd(content, j + 2, tokenStart + j);
                            j = endInner + 2;
                            continue;
                        }
                        j += run;
                        continue;
                    }
                    j++;
                    continue;
                }

                if (ch == '|')
                {
                    return j;
                }

                j++;
            }

            return -1;
        }

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

        private ValueToken(ValueTokenKind kind, string value, IReadOnlyList<ReferenceItem> items, IReadOnlyList<ValueToken>? literalDefault, bool isStrict)
        {
            Kind = kind;
            Value = value;
            Items = items;
            LiteralDefault = literalDefault;
            IsStrict = isStrict;
        }

        internal static ValueToken Literal(string text) =>
            new(ValueTokenKind.Literal, text, s_noItems, literalDefault: null, isStrict: false);

        internal static ValueToken Reference(IReadOnlyList<ReferenceItem> items, IReadOnlyList<ValueToken>? literalDefault, bool isStrict)
        {
            string first = items.Count > 0 ? items[0].Value : string.Empty;
            return new ValueToken(ValueTokenKind.Reference, first, items, literalDefault, isStrict);
        }

        internal ValueTokenKind Kind { get; }

        // For Literal tokens: the literal text.
        // For Reference tokens: the first reference path (convenience; empty when the head is empty).
        internal string Value { get; }

        internal IReadOnlyList<ReferenceItem> Items { get; }

        // Non-null iff the expression contained an explicit '|' template tail. The parsed tokens
        // are resolved lazily when every reference in Items misses (or when Items is empty, i.e.
        // the gate was "{{|…}}"). Literal text is emitted verbatim; nested placeholders run
        // through the same resolution machinery.
        internal IReadOnlyList<ValueToken>? LiteralDefault { get; }

        internal bool HasDefault => LiteralDefault is not null;

        internal bool IsStrict { get; }

        // True when this reference has a non-empty head — i.e. it can resolve via a referenced
        // key and therefore may preserve section semantics when it's the sole top-level token.
        internal bool HasHead => Items.Count > 0;
    }
}

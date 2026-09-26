// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    // Phase-1 prototype reference-resolution engine. A value of the form
    //
    //     ref(key1, key2, ..., keyN)
    //
    // is interpreted as a fallback chain: the first listed key whose own value resolves to a
    // non-null string supplies the result. Whitespace around each key is trimmed. Resolved
    // values are themselves run through the engine, so chains compose. All failure modes —
    // missing keys, malformed expressions, and detected cycles — degrade to the original raw
    // string verbatim. The engine never throws.
    //
    // Anything that is not a strict whole-string match of the ref(...) shape is returned as
    // a literal: prefixes, suffixes, nested parens, and embedded references are all left alone.
    internal sealed class ReferenceResolutionEngine : IDisposable
    {
        // Number of nested resolutions we will perform without any cycle tracking. Below this
        // threshold the chain is assumed acyclic and we pay nothing for tracking. At/above the
        // threshold a HashSet is allocated and every subsequent target key is added to it,
        // so any cycle — regardless of period — is caught after at most one full revolution
        // past the threshold and degrades to the verbatim raw value.
        private const int CycleTrackingThreshold = 32;
        private const string RefOpen = "ref(";
        private const string EscapeOpen = @"\ref(";

        private readonly IReadOnlyList<IConfigurationProvider> _providers;
        private readonly IDisposable[] _changeTokenRegistrations;

        // Replaced wholesale on Invalidate so in-flight readers keep seeing a consistent map.
        private volatile ConcurrentDictionary<string, string?> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        public ReferenceResolutionEngine(IReadOnlyList<IConfigurationProvider> providers)
        {
            ArgumentNullException.ThrowIfNull(providers);

            _providers = providers;
            _changeTokenRegistrations = new IDisposable[providers.Count];
            for (int i = 0; i < providers.Count; i++)
            {
                IConfigurationProvider provider = providers[i];
                _changeTokenRegistrations[i] = ChangeToken.OnChange(provider.GetReloadToken, Invalidate);
            }
        }

        public bool TryGet(string key, out string? value)
        {
            ArgumentNullException.ThrowIfNull(key);

            ConcurrentDictionary<string, string?> cache = _cache;
            if (cache.TryGetValue(key, out value))
            {
                return true;
            }

            if (!TryReadRaw(key, out string? raw))
            {
                value = null;
                return false;
            }

            HashSet<string>? visited = null;
            value = ResolveValue(raw, key, ref visited, count: 0);
            cache.TryAdd(key, value);
            return true;
        }

        public void Invalidate()
        {
            _cache = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            foreach (IDisposable registration in _changeTokenRegistrations)
            {
                registration.Dispose();
            }
        }

        // Walks the provider stack in reverse (last-added wins) returning the first hit.
        // Returns false when no provider knows the key. A leaf with a null value still counts
        // as a hit and surfaces as `value = null`.
        private bool TryReadRaw(string key, out string? value)
        {
            for (int i = _providers.Count - 1; i >= 0; i--)
            {
                if (_providers[i].TryGet(key, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }

        // The recursive resolution step. `raw` is the unresolved value of some key; `anchorKey`
        // is the absolute key whose value is being resolved (used to anchor leading `..:`
        // segments inside referenced keys); `count` is the number of nested resolutions
        // performed so far. Below the cycle-tracking threshold we run unbounded with no
        // tracking allocation; once count reaches the threshold we lazily allocate `visited`
        // and start adding every (resolved, absolute) target key. From that point on, a key
        // already on the post-threshold path produces a cycle and we return the raw expression
        // verbatim.
        private string? ResolveValue(string? raw, string anchorKey, ref HashSet<string>? visited, int count)
        {
            if (raw is null)
            {
                return null;
            }

            // Escape: a value starting with "\ref(" is treated as a literal whose leading '\'
            // is stripped. The result is returned verbatim and (via TryGet's outer cache)
            // memoized in the stripped form, so subsequent reads see the literal directly.
            // The check is intentionally narrow — only the exact prefix "\ref(" is touched, so
            // unrelated values starting with '\' (Windows paths, escape sequences elsewhere)
            // are unaffected.
            if (TryStripBackslashEscape(raw, out string? literal))
            {
                return literal;
            }

            if (!TryParseRef(raw, out List<(string Key, bool IsLiteral)>? keys))
            {
                return raw;
            }

            // Lazy escalation: only once we've consumed the grace budget do we pay for
            // a tracking set. From here down every targetKey is added/removed in lockstep
            // with the resolution path, and a re-encountered key terminates that branch
            // verbatim.
            if (visited is null && count >= CycleTrackingThreshold)
            {
                visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            foreach ((string targetKey, bool isLiteral) in keys)
            {
                string absoluteKey;
                if (isLiteral)
                {
                    if (targetKey.Length == 0)
                    {
                        continue;
                    }

                    absoluteKey = targetKey;
                }
                else if (!TryResolveRelativeKey(targetKey, anchorKey, out string? resolved))
                {
                    // Walked past root — skip this fallback and let the chain continue.
                    continue;
                }
                else
                {
                    absoluteKey = resolved;
                }

                if (visited is not null && !visited.Add(absoluteKey))
                {
                    continue;
                }

                try
                {
                    if (!TryReadRaw(absoluteKey, out string? targetRaw))
                    {
                        continue;
                    }

                    return ResolveValue(targetRaw, absoluteKey, ref visited, count + 1);
                }
                finally
                {
                    visited?.Remove(absoluteKey);
                }
            }

            // No fallback resolved — leave the original ref expression intact.
            return raw;
        }

        // Resolves a target key (as written inside `ref(...)`) into an absolute configuration
        // key using `anchorKey` (the key currently being resolved) as the base for any leading
        // `..` segments. Each leading `..` (followed by `:` or end-of-key) drops one trailing
        // colon-segment from the anchor. Walking past root, or producing an empty absolute
        // key, returns false so the caller can skip this fallback.
        private static bool TryResolveRelativeKey(string targetKey, string anchorKey, [NotNullWhen(true)] out string? absoluteKey)
        {
            absoluteKey = null;

            int up = 0;
            int idx = 0;
            while (idx + 1 < targetKey.Length
                && targetKey[idx] == '.' && targetKey[idx + 1] == '.'
                && (idx + 2 == targetKey.Length || targetKey[idx + 2] == ':'))
            {
                up++;
                idx += 2;
                if (idx < targetKey.Length)
                {
                    idx++; // skip the ':'
                }
            }

            if (up == 0)
            {
                if (targetKey.Length == 0)
                {
                    return false;
                }

                absoluteKey = targetKey;
                return true;
            }

            // The remainder of the target key after consuming all leading "..[:]" segments.
            ReadOnlySpan<char> suffix = targetKey.AsSpan(idx);

            // Drop `up` colon-segments from the right of the anchor key.
            ReadOnlySpan<char> prefix = anchorKey.AsSpan();
            for (int i = 0; i < up; i++)
            {
                if (prefix.IsEmpty)
                {
                    return false; // walked past root
                }

                int lastColon = prefix.LastIndexOf(':');
                prefix = lastColon < 0 ? default : prefix.Slice(0, lastColon);
            }

            if (prefix.IsEmpty && suffix.IsEmpty)
            {
                return false; // pure "..[:..]*" landing on root with no key to look up
            }

            if (prefix.IsEmpty)
            {
                absoluteKey = suffix.ToString();
            }
            else if (suffix.IsEmpty)
            {
                absoluteKey = prefix.ToString();
            }
            else
            {
                absoluteKey = prefix.ToString() + ":" + suffix.ToString();
            }

            return true;
        }

        // Detects the escape form: a value beginning with "\ref(". Strips the leading '\' and
        // returns the remainder as a literal. The check is intentionally narrow — only values
        // that start with the exact prefix "\ref(" are touched, so unrelated strings (Windows
        // paths, prose, escape sequences elsewhere) are unaffected. Closing-paren balance is
        // irrelevant: the escape applies whether or not the rest is a well-formed expression.
        private static bool TryStripBackslashEscape(string value, [NotNullWhen(true)] out string? literal)
        {
            if (value.Length >= EscapeOpen.Length
                && value.AsSpan(0, EscapeOpen.Length).SequenceEqual(EscapeOpen.AsSpan()))
            {
                literal = value.Substring(1);
                return true;
            }

            literal = null;
            return false;
        }

        // Recognises a strict whole-string `ref(<keys>)` shape:
        //   - prefix is exactly "ref(" (case-sensitive),
        //   - last character is ')',
        //   - body is one or more comma-separated key segments (whitespace around each
        //     segment is trimmed),
        //   - a bare segment may not contain '(', ')', '\'', '"', or ',',
        //   - a segment may instead be wrapped in single or double quotes; inside the quotes
        //     any character is legal and the chosen quote is escaped by doubling (e.g. 'a''b'
        //     denotes the key a'b). A quoted segment is treated as a literal absolute key —
        //     leading "..[:]" segments inside it are NOT interpreted as relative-up.
        // Returns false on any deviation; callers translate that to "verbatim".
        private static bool TryParseRef(string value, [NotNullWhen(true)] out List<(string Key, bool IsLiteral)>? keys)
        {
            keys = null;

            if (value.Length < RefOpen.Length + 1)
            {
                return false;
            }

            if (!value.AsSpan(0, RefOpen.Length).SequenceEqual(RefOpen.AsSpan()))
            {
                return false;
            }

            if (value[value.Length - 1] != ')')
            {
                return false;
            }

            ReadOnlySpan<char> body = value.AsSpan(RefOpen.Length, value.Length - RefOpen.Length - 1);
            var result = new List<(string, bool)>();
            int i = 0;

            while (true)
            {
                while (i < body.Length && char.IsWhiteSpace(body[i]))
                {
                    i++;
                }

                if (i == body.Length)
                {
                    // Either the body is empty or we just consumed a trailing comma — both
                    // count as an empty segment, which is invalid.
                    return false;
                }

                string key;
                bool isLiteral;
                if (body[i] == '\'' || body[i] == '"')
                {
                    char quote = body[i++];
                    var sb = new System.Text.StringBuilder();
                    bool closed = false;
                    while (i < body.Length)
                    {
                        char c = body[i];
                        if (c == quote)
                        {
                            if (i + 1 < body.Length && body[i + 1] == quote)
                            {
                                sb.Append(quote);
                                i += 2;
                                continue;
                            }

                            i++;
                            closed = true;
                            break;
                        }

                        sb.Append(c);
                        i++;
                    }

                    if (!closed || sb.Length == 0)
                    {
                        return false;
                    }

                    key = sb.ToString();
                    isLiteral = true;
                }
                else
                {
                    int start = i;
                    while (i < body.Length && body[i] != ',')
                    {
                        char c = body[i];
                        if (c == '(' || c == ')' || c == '\'' || c == '"')
                        {
                            return false;
                        }

                        i++;
                    }

                    int end = i;
                    while (end > start && char.IsWhiteSpace(body[end - 1]))
                    {
                        end--;
                    }

                    if (end == start)
                    {
                        return false;
                    }

                    key = body.Slice(start, end - start).ToString();
                    isLiteral = false;
                }

                result.Add((key, isLiteral));

                while (i < body.Length && char.IsWhiteSpace(body[i]))
                {
                    i++;
                }

                if (i == body.Length)
                {
                    break;
                }

                if (body[i] != ',')
                {
                    return false;
                }

                i++;
            }

            keys = result;
            return result.Count > 0;
        }
    }
}

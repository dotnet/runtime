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
            value = ResolveValue(raw, ref visited, count: 0);
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

        // The recursive resolution step. `raw` is the unresolved value of some key; `count`
        // is the number of nested resolutions performed so far. Below the cycle-tracking
        // threshold we run unbounded with no tracking allocation; once count reaches the
        // threshold we lazily allocate `visited` and start adding every target key. From
        // that point on, a key already on the post-threshold path produces a cycle and we
        // return the raw expression verbatim.
        private string? ResolveValue(string? raw, ref HashSet<string>? visited, int count)
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

            if (!TryParseRef(raw, out List<string>? keys))
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

            foreach (string targetKey in keys)
            {
                if (visited is not null && !visited.Add(targetKey))
                {
                    continue;
                }

                try
                {
                    if (!TryReadRaw(targetKey, out string? targetRaw))
                    {
                        continue;
                    }

                    return ResolveValue(targetRaw, ref visited, count + 1);
                }
                finally
                {
                    visited?.Remove(targetKey);
                }
            }

            // No fallback resolved — leave the original ref expression intact.
            return raw;
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
        //   - body contains no nested parens (phase 1 keeps the grammar flat),
        //   - body splits into one or more comma-separated, non-empty (after trim) segments.
        // Returns false on any deviation; callers translate that to "verbatim".
        private static bool TryParseRef(string value, [NotNullWhen(true)] out List<string>? keys)
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
            foreach (char c in body)
            {
                if (c == '(' || c == ')')
                {
                    return false;
                }
            }

            var result = new List<string>();
            int start = 0;
            for (int i = 0; i <= body.Length; i++)
            {
                if (i == body.Length || body[i] == ',')
                {
                    ReadOnlySpan<char> segment = body.Slice(start, i - start).Trim();
                    if (segment.IsEmpty)
                    {
                        return false;
                    }

                    result.Add(segment.ToString());
                    start = i + 1;
                }
            }

            keys = result;
            return true;
        }
    }
}

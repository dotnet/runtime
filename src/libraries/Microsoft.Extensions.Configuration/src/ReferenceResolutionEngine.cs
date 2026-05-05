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
    // missing keys, malformed expressions, max-depth, and detected cycles — degrade to the
    // original raw string verbatim. The engine never throws.
    //
    // Anything that is not a strict whole-string match of the ref(...) shape is returned as
    // a literal: prefixes, suffixes, nested parens, and embedded references are all left alone.
    internal sealed class ReferenceResolutionEngine : IDisposable
    {
        private const int MaxDepth = 32;
        private const string RefOpen = "ref(";

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

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { key };
            value = ResolveValue(raw, visited, depth: 0);
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

        // The recursive resolution step. `raw` is the unresolved value of some key; `visited`
        // tracks keys currently being resolved on this call stack so we can break cycles.
        // The depth guard handles long-but-non-cyclic chains the cycle set would otherwise miss
        // (e.g. when a value re-enters via a key not in the visited set due to TryAdd skip).
        private string? ResolveValue(string? raw, HashSet<string> visited, int depth)
        {
            if (raw is null)
            {
                return null;
            }

            if (depth >= MaxDepth)
            {
                return raw;
            }

            if (!TryParseRef(raw, out List<string>? keys))
            {
                return raw;
            }

            foreach (string targetKey in keys)
            {
                // Skip keys already on the resolution stack — that's a cycle. Because the key
                // is removed from `visited` after the recursive call, the same key may still
                // appear as a sibling fallback for an unrelated outer ref.
                if (!visited.Add(targetKey))
                {
                    continue;
                }

                try
                {
                    if (!TryReadRaw(targetKey, out string? targetRaw))
                    {
                        continue;
                    }

                    return ResolveValue(targetRaw, visited, depth + 1);
                }
                finally
                {
                    visited.Remove(targetKey);
                }
            }

            // No fallback resolved — leave the original ref expression intact.
            return raw;
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

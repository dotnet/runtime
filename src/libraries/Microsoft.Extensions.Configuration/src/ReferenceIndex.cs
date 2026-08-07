// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    // A snapshot of every configuration path that is a reference, mapping the path to its immediate target. Built once
    // per provider generation by scanning the providers (see Build). A reference is declared by a reserved "$ref" child
    // key - "<path>:$ref = <target>" - so a plain value is never mistaken for a reference and no escaping is needed; the
    // highest-precedence provider that declares a path's $ref wins, and an empty target drops it. Entries are kept
    // unflattened - each maps to its immediate target - so resolution follows the chain hop by hop and can apply a
    // higher provider's override at every hop; a chain that never terminates (a cycle, or a self-reference that grows
    // without bound) is caught by the resolution walk itself (see ReferenceEngine.CycleGuard).
    internal sealed class ReferenceIndex
    {
        public static readonly ReferenceIndex Empty = new ReferenceIndex(new Dictionary<string, (string Target, int Level)>(StringComparer.OrdinalIgnoreCase));

        // The reserved final segment that declares a reference: "<path>:$ref = <target>" makes <path> a reference to
        // <target>. Because the marker is a key, not a value, values are always literal and never need escaping.
        internal const string RefSegment = "$ref";

        // Each reference key maps to its target and the level (provider index) of the provider that declared it. The
        // level lets resolution honour a higher-precedence provider that overrides a mirrored value. Entries are kept
        // unflattened: resolution follows the chain hop by hop so it can apply overrides at every hop.
        private readonly Dictionary<string, (string Target, int Level)> _targets;
#if NET9_0_OR_GREATER
        private readonly Dictionary<string, (string Target, int Level)>.AlternateLookup<ReadOnlySpan<char>> _bySpan;
#endif

        public ReferenceIndex(Dictionary<string, (string Target, int Level)> targets)
        {
            _targets = targets;
#if NET9_0_OR_GREATER
            _bySpan = targets.GetAlternateLookup<ReadOnlySpan<char>>();
#endif
        }

        public bool IsEmpty => _targets.Count == 0;

        // Finds the prefix of <paramref name="key"/> - including the key itself - that governs it, returning its
        // <paramref name="target"/>, the <paramref name="level"/> (provider index) of the provider that declared it,
        // and the length of the matched prefix. Among the prefixes that are references, the one declared by the
        // highest-precedence provider governs; ties (references declared at the same level) are broken by the
        // shallowest (outermost) prefix, so a nested reference under the same provider's alias is ignored while a more
        // specific reference declared by a higher provider still wins over a lower provider's ancestor reference.
        // Returns false when no prefix is a reference. Resolution walks these hop by hop (see ReferenceEngine.TryRead),
        // applying a higher provider's override at each hop.
        internal bool TryGetGoverningRef(string key, [NotNullWhen(true)] out string? target, out int level, out int prefixLength)
        {
            target = null;
            level = -1;
            prefixLength = 0;

            if (_targets.Count != 0)
            {
                int start = 0;
                while (true)
                {
                    int delimiter = key.IndexOf(ConfigurationPath.KeyDelimiter[0], start);
                    int end = delimiter < 0 ? key.Length : delimiter;
#if NET9_0_OR_GREATER
                    if (_bySpan.TryGetValue(key.AsSpan(0, end), out (string Target, int Level) entry) && entry.Level > level)
#else
                    if (_targets.TryGetValue(key.Substring(0, end), out (string Target, int Level) entry) && entry.Level > level)
#endif
                    {
                        // Only a strictly higher level replaces the running best, so on a tie the first (shallowest)
                        // match is kept.
                        target = entry.Target;
                        level = entry.Level;
                        prefixLength = end;
                    }

                    if (delimiter < 0)
                    {
                        break;
                    }
                    start = delimiter + 1;
                }
            }

            return target is not null;
        }

        // Builds the index for a provider generation. Scans every provider (a ChainedConfigurationProvider resolves its
        // own references and hides its inner providers, so it is skipped) from highest to lowest precedence for "$ref"
        // keys. Each "<path>:$ref = <target>" records <path> -> (<target>, declaring level); the highest provider that
        // declares a path's $ref wins (a lower provider's $ref for the same path is ignored) and an empty target drops
        // the reference. Entries are kept unflattened so resolution can apply a higher provider's override at each hop
        // of a chain; a chain that never terminates is caught later, by the resolution walk (see ReferenceEngine).
        public static ReferenceIndex Build(IList<IConfigurationProvider> providers)
        {
            Dictionary<string, (string Target, int Level)>? targets = null;
            HashSet<string>? claimed = null;

            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IConfigurationProvider provider = providers[i];
                if (provider is ChainedConfigurationProvider)
                {
                    // A ChainedConfigurationProvider wraps a whole IConfiguration that already resolves its own
                    // references, and its merged view hides the inner providers. Its values are still read through the
                    // normal provider path, but it contributes none of its own references to this index.
                    continue;
                }

                foreach (KeyValuePair<string, string?> entry in ScanProvider(provider))
                {
                    // The highest provider that declares a path's $ref decides it; claim the path here so a lower
                    // provider's $ref for the same path is ignored. An empty target claims the path but records no
                    // reference, so a higher provider can drop a lower reference by setting its $ref to empty.
                    if (IsRefKey(entry.Key, out string? referencePath)
                        && (claimed ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(referencePath)
                        && !string.IsNullOrEmpty(entry.Value))
                    {
                        (targets ??= new Dictionary<string, (string Target, int Level)>(StringComparer.OrdinalIgnoreCase))[referencePath] = (entry.Value!, i);
                    }
                }
            }

            return targets is null ? Empty : new ReferenceIndex(targets);
        }

        // The key/value pairs of a provider for the reference scan: a ConfigurationProvider exposes its loaded
        // dictionary directly; any other provider is enumerated through GetChildKeys.
        private static IEnumerable<KeyValuePair<string, string?>> ScanProvider(IConfigurationProvider provider)
            => provider is ConfigurationProvider concrete ? concrete.GetEntriesForReferenceScan() : EnumerateProvider(provider);

        // Walks every key of a non-scannable provider via GetChildKeys, collecting those that hold a value. A provider
        // disposed mid-walk (concurrently, or after the ConfigurationManager itself was disposed) contributes whatever
        // was collected before it threw, matching the plain read path that skips a provider throwing
        // ObjectDisposedException. Collected eagerly because Build consumes the whole enumeration anyway.
        private static List<KeyValuePair<string, string?>> EnumerateProvider(IConfigurationProvider provider)
        {
            var entries = new List<KeyValuePair<string, string?>>();
            var pending = new Stack<string?>();
            pending.Push(null);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                while (pending.Count > 0)
                {
                    string? parent = pending.Pop();
                    foreach (string child in provider.GetChildKeys(Enumerable.Empty<string>(), parent))
                    {
                        string childKey = parent is null ? child : parent + ConfigurationPath.KeyDelimiter + child;
                        if (!seen.Add(childKey))
                        {
                            continue;
                        }

                        if (provider.TryGet(childKey, out string? value))
                        {
                            entries.Add(new KeyValuePair<string, string?>(childKey, value));
                        }
                        pending.Push(childKey);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
            }

            return entries;
        }

        // Whether <paramref name="key"/> declares a reference - its final segment is the reserved "$ref" - and, if so,
        // the <paramref name="referencePath"/> it governs (the key without the trailing ":$ref"). A bare "$ref" at the
        // root is not a reference, as there is no path for it to govern.
        internal static bool IsRefKey(string key, [NotNullWhen(true)] out string? referencePath)
        {
            int suffixLength = ConfigurationPath.KeyDelimiter.Length + RefSegment.Length;
            if (key.Length > suffixLength
                && key[key.Length - suffixLength] == ConfigurationPath.KeyDelimiter[0]
                && string.Compare(key, key.Length - RefSegment.Length, RefSegment, 0, RefSegment.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                referencePath = key.Substring(0, key.Length - suffixLength);
                return true;
            }

            referencePath = null;
            return false;
        }

        // Whether a single key segment is the reserved "$ref" marker, so enumeration can hide it.
        internal static bool IsRefSegment(string segment) => string.Equals(segment, RefSegment, StringComparison.OrdinalIgnoreCase);
    }
}

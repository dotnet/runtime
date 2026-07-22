// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    // A snapshot of every configuration key whose effective value is a reference, mapping the key to its immediate
    // target. Built once per provider generation by scanning the opted-in providers (see Build). Only opted-in
    // providers (ConfigurationReferences:Enabled = true) contribute references, and a reference is recorded only when no
    // higher-precedence provider overrides its key, so every entry is the effective reference for that key. Entries are
    // kept unflattened - each maps to its immediate target - so resolution follows the chain hop by hop and can apply a
    // higher provider's override at every hop; a chain that never terminates (a cycle, or a self-reference that grows
    // without bound) is caught by the resolution walk itself (see ReferenceEngine.CycleGuard).
    internal sealed class ReferenceIndex
    {
        public static readonly ReferenceIndex Empty = new ReferenceIndex(new Dictionary<string, (string Target, int Level)>(StringComparer.OrdinalIgnoreCase));

        // The per-provider opt-in key. A provider whose value for this key parses as true has its ref(...) values
        // interpreted as references.
        internal const string EnabledKey = "ConfigurationReferences:Enabled";

        private const string ReferenceMarkerPrefix = "ref(";

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

        // Builds the index for a provider generation. First it records every key whose effective value is a reference,
        // mapped to that reference's target and the level of the provider that declared it: only opted-in providers contribute (ConfigurationProvider inheritors are read from their
        // loaded dictionary, others are walked via GetChildKeys); providers are visited from highest to
        // lowest precedence and the first occurrence of a key wins, so a key overridden by a higher opted-in provider is
        // never recorded from a lower one; and a reference is recorded only when no higher provider holds its key at all
        // (a higher holder can only be non-opted - an opted one would have claimed the key first - and its literal
        // shadows the reference). Entries are kept unflattened so resolution can apply a higher provider's override at
        // each hop of a chain; a chain that never terminates is caught later, by the resolution walk (see ReferenceEngine).
        public static ReferenceIndex Build(IList<IConfigurationProvider> providers)
        {
            Dictionary<string, (string Target, int Level)>? targets = null;
            HashSet<string>? claimed = null;

            for (int i = providers.Count - 1; i >= 0; i--)
            {
                IConfigurationProvider provider = providers[i];
                if (provider is ChainedConfigurationProvider || !IsOptedIn(provider))
                {
                    // A ChainedConfigurationProvider wraps a whole IConfiguration that already resolves its own
                    // references, and its merged view hides the inner providers, so we cannot tell which of them opted
                    // in. Skip scanning it for references; its values are still read through the normal provider path
                    // (and can still shadow a lower reference), it just contributes none of its own to this index.
                    continue;
                }

                foreach (KeyValuePair<string, string?> entry in ScanProvider(provider))
                {
                    // The highest opted-in provider holding the key decides it, so claim it here and ignore any lower
                    // opted-in value. Record a reference only when the value is ref(...) and no higher provider holds
                    // the key: a higher holder can only be non-opted (an opted one would have claimed the key first)
                    // and its literal shadows the reference. A shadowed key stays shadowed for every lower provider
                    // too, so there is nothing to reconsider below.
                    if ((claimed ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(entry.Key)
                        && TryParseReference(entry.Value, out string? target)
                        && !HasHigherHolder(providers, entry.Key, i))
                    {
                        (targets ??= new Dictionary<string, (string Target, int Level)>(StringComparer.OrdinalIgnoreCase))[entry.Key] = (target, i);
                    }
                }
            }

            if (targets is null)
            {
                return Empty;
            }

            return new ReferenceIndex(targets);
        }

        // Whether a provider opted into references: its ConfigurationReferences:Enabled value parses as true. A provider
        // disposed concurrently (or after the ConfigurationManager itself was disposed) is treated as not opted in,
        // matching the plain read path that skips a provider throwing ObjectDisposedException.
        private static bool IsOptedIn(IConfigurationProvider provider)
        {
            try
            {
                return provider.TryGet(EnabledKey, out string? value) && bool.TryParse(value, out bool enabled) && enabled;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        // Whether any provider above <paramref name="level"/> holds <paramref name="key"/>. During Build such a holder
        // is necessarily non-opted (an opted one would have claimed the key first), so its literal shadows a reference
        // recorded at <paramref name="level"/>.
        private static bool HasHigherHolder(IList<IConfigurationProvider> providers, string key, int level)
        {
            for (int i = providers.Count - 1; i > level; i--)
            {
                try
                {
                    if (providers[i].TryGet(key, out _))
                    {
                        return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // A provider disposed concurrently holds nothing; skip it, as the plain read path does.
                }
            }

            return false;
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

        // Parses ref(target) into its single target key. Returns false (a literal) for a null value, a non-marker, or an
        // empty ref(). The target is the trimmed body between the parentheses.
        private static bool TryParseReference([NotNullWhen(true)] string? value, [NotNullWhen(true)] out string? target)
        {
            if (value is not null
                && value.Length > ReferenceMarkerPrefix.Length
                && value[value.Length - 1] == ')'
                && value.StartsWith(ReferenceMarkerPrefix, StringComparison.Ordinal))
            {
                ReadOnlySpan<char> body = value.AsSpan(ReferenceMarkerPrefix.Length, value.Length - ReferenceMarkerPrefix.Length - 1).Trim();
                if (body.Length != 0)
                {
                    target = body.ToString();
                    return true;
                }
            }

            target = null;
            return false;
        }
    }
}

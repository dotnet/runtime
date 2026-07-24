// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.Configuration
{
    // The reference engine for one provider generation: it lazily builds and memoizes a ReferenceIndex from the
    // generation's providers, then resolves keys against it. Bound to the exact provider list it serves and swapped for
    // a fresh instance whenever that list or the providers' data change (a new ReferenceCountedProviders generation, or
    // a ConfigurationRoot reload), so resolution always runs against the very list the index was built from - the two
    // can never be paired with a different (for example, newer) provider list, and a change can never surface stale
    // reference information.
    internal sealed class ReferenceEngine
    {
        // A process-wide opt-out for apps that do not want configuration references at all. The value is a feature
        // switch so the resolution paths can be trimmed away when set. Read as the outermost gate at every call site, so
        // a set switch lets the trimmer drop the whole reference path.
        [FeatureSwitchDefinition("Microsoft.Extensions.Configuration.DisableConfigurationReferences")]
        internal static bool Disabled { get; } =
            AppContext.TryGetSwitch("Microsoft.Extensions.Configuration.DisableConfigurationReferences", out bool disabled) && disabled;

        private readonly object _lock = new object();

        // Volatile so the lock-free fast-path read publishes the fully-built index (release on assignment, acquire on
        // read) on weak memory models. Null means "not computed yet"; ReferenceIndex.Empty means "computed, nothing to
        // resolve".
        private volatile ReferenceIndex? _index;

        private ReferenceEngine(IList<IConfigurationProvider> providers) => Providers = providers;

        public static ReferenceEngine? Create(IList<IConfigurationProvider> providers) => Disabled ? null : new ReferenceEngine(providers);

        public IList<IConfigurationProvider> Providers { get; }

        // The memoized reference index for this generation, built once from Providers. ReferenceIndex.Build returns the
        // empty sentinel when no provider declares any $ref, so that "nothing to resolve" case lands on IsEmpty.
        private ReferenceIndex Index()
        {
            ReferenceIndex? computed = _index;
            if (computed is null)
            {
                lock (_lock)
                {
                    computed = _index ??= ReferenceIndex.Build(Providers);
                }
            }

            return computed;
        }

        // Whether this generation resolves nothing (no provider declares a $ref). Lets enumeration take the plain
        // provider path - allocating like a reference-free configuration - instead of the merge path. The index is
        // built (once, memoized) to answer this, which the first read of the generation does anyway.
        internal bool IndexIsEmpty => Index().IsEmpty;

        // Reads <paramref name="key"/>, applying recursive-merge reference resolution. The key is followed hop by hop
        // through the reference chain; at each hop a provider strictly above the reference's declaring level may
        // override the mirrored value at that exact path, otherwise the mirror is followed. When no reference governs
        // the running key it is read across all providers. Returns whether a value was found.
        public bool TryRead(string key, out string? value)
        {
            ReferenceIndex index = Index();
            // A $ref key is structural metadata, not data: read it raw without resolving, so it neither redirects
            // through its own parent's reference nor joins the walk. An empty index likewise means nothing resolves.
            if (index.IsEmpty || ReferenceIndex.IsRefKey(key, out _))
            {
                // This generation holds no references (or the key is a marker), so read the key directly.
                return TryGetAbove(key, minLevelExclusive: -1, out value);
            }

            string current = key;
            CycleGuard guard = default;
            while (index.TryGetGoverningRef(current, out string? target, out int level, out int prefixLength))
            {
                // A higher-precedence provider (strictly above the one that declared the reference) can override the
                // mirrored value at this exact path; that override wins over following the reference.
                if (TryGetAbove(current, level, out value))
                {
                    return true;
                }

                guard.Advance(current, prefixLength);
                current = prefixLength == current.Length
                    ? target
                    : target + current.Substring(prefixLength);
            }

            return TryGetAbove(current, minLevelExclusive: -1, out value);
        }

        // Collects the immediate child key segments of <paramref name="path"/> under recursive-merge resolution: the
        // union, deduplicated, of the override children each higher provider defines at every reference hop and the
        // mirror children of the final target.
        public List<string> ChildKeys(string? path)
        {
            ReferenceIndex index = Index();
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string? current = path;
            bool merged = false;
            if (!index.IsEmpty)
            {
                // Only walk the reference chain when there is something to resolve; otherwise fall straight through to
                // the plain enumeration of the path below.
                CycleGuard guard = default;
                while (current is not null && index.TryGetGoverningRef(current, out string? target, out int level, out int prefixLength))
                {
                    AddChildKeysAbove(result, seen, current, level);
                    guard.Advance(current, prefixLength);
                    current = prefixLength == current.Length
                        ? target
                        : target + current.Substring(prefixLength);
                    merged = true;
                }
            }

            AddChildKeysAbove(result, seen, current, minLevelExclusive: -1);

            if (merged)
            {
                // A resolved section unions children drawn from several providers across the reference hops; each block
                // arrives sorted on its own but the concatenation is not, so restore the ConfigurationKeyComparer order
                // GetChildren guarantees. The plain (no-hop) path already comes out ordered from the providers, so it
                // is left untouched.
                result.Sort(ConfigurationKeyComparer.Comparison);
            }

            return result;
        }

        // Reads <paramref name="key"/> from the providers strictly above <paramref name="minLevelExclusive"/>, highest
        // precedence first, tolerating a provider disposed concurrently (as the plain read path does).
        private bool TryGetAbove(string key, int minLevelExclusive, out string? value)
        {
            for (int i = Providers.Count - 1; i > minLevelExclusive; i--)
            {
                try
                {
                    if (Providers[i].TryGet(key, out value))
                    {
                        return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                }
            }

            value = null;
            return false;
        }

        // Adds the immediate child segments of <paramref name="path"/> drawn from the providers strictly above
        // <paramref name="minLevelExclusive"/>, skipping segments already collected and tolerating a disposed provider.
        private void AddChildKeysAbove(List<string> result, HashSet<string> seen, string? path, int minLevelExclusive)
        {
            IEnumerable<string> keys = Enumerable.Empty<string>();
            for (int i = minLevelExclusive + 1; i < Providers.Count; i++)
            {
                try
                {
                    keys = Providers[i].GetChildKeys(keys, path);
                }
                catch (ObjectDisposedException)
                {
                }
            }

            foreach (string key in keys)
            {
                // Hide the reserved $ref marker: it declares the reference, it is not a child of the section.
                if (!ReferenceIndex.IsRefSegment(key) && seen.Add(key))
                {
                    result.Add(key);
                }
            }
        }

        // Guards a single reference-resolution walk against a chain that never terminates. Every hop redirects through
        // a governing reference key drawn from a finite set, so a walk that does not terminate - whether it repeats
        // (A -> B -> A) or grows without bound (A -> A:B) - must fire some governing key twice, while a terminating
        // walk fires each at most once. The first GraceHops hops are not recorded, so a chain that resolves quickly
        // (the common case) allocates nothing; the fired-key set is created only once a walk outlives the grace period.
        private struct CycleGuard
        {
            // A reference chain resolves in as many hops as it has links, a handful for any real configuration, so only
            // a cycle or a pathologically deep chain outlasts this.
            private const int GraceHops = 32;

            private int _hops;
            private HashSet<string>? _fired;

            public void Advance(string key, int prefixLength)
            {
                if (++_hops <= GraceHops)
                {
                    return;
                }

                string governing = key.Substring(0, prefixLength);
                if (!(_fired ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase)).Add(governing))
                {
                    throw new InvalidOperationException(
                        SR.Format(SR.Error_ReferenceCycle, string.Join(" -> ", _fired) + " -> " + governing));
                }
            }
        }
    }
}

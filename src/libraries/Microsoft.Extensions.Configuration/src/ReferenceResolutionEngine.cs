// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    // Root-owned resolution engine. Backs IConfigurationRoot.this[key] and GetChildren when a
    // builder has marked at least one source with ReferenceMode.Scan. Holds the ordered provider
    // set and the resolution cache; subscribes to the composite reload token so the cache is
    // dropped whenever any inner provider reloads.
    //
    // Per-source modes (ReferenceMode): each provider carries a mode derived from its source's
    // configuration. Default is Read. Providers with mode Ignore are invisible to the engine as
    // substitution targets (GetRawValue skips them). Providers whose mode is not Scan are not
    // parsed for ref(...) / fmt(...) — their values are returned sealed, so the engine returns
    // them as literals. At least one provider must be in Scan mode for the engine to be attached
    // at Build time.
    internal sealed class ReferenceResolutionEngine : IDisposable
    {
        private const int MaxDepth = 1024;
        private const int CycleCheckThreshold = 32;

        private static readonly IChangeToken s_neverChangedToken = new CancellationChangeToken(CancellationToken.None);

        private readonly ProviderSet _providers;
        private readonly AliasFinder _aliasFinder;
        private readonly ReferenceResolver _resolver;
        private readonly IDisposable _changeTokenRegistration;

        private volatile ConcurrentDictionary<string, string?> _cache = new(StringComparer.OrdinalIgnoreCase);

        internal ReferenceResolutionEngine(
            IReadOnlyList<IConfigurationProvider> providers,
            IReadOnlyDictionary<IConfigurationProvider, ReferenceMode>? providerModes = null)
        {
            ArgumentNullException.ThrowIfNull(providers);

            _providers = new ProviderSet(providers, providerModes);
            _aliasFinder = new AliasFinder(_providers);
            _resolver = new ReferenceResolver(_providers, _aliasFinder);
            _changeTokenRegistration = ChangeToken.OnChange(_providers.GetCompositeReloadToken, Invalidate);
        }

        public bool TryGet(string key, out string? value)
        {
            ConcurrentDictionary<string, string?> cache = _cache;
            var path = new Path(key);

            if (cache.TryGetValue(path.Value, out value))
            {
                return true;
            }

            bool hasTopLayer = _providers.TryGetTopLayer(path, out string? topValue, out int topIndex, out ReferenceMode topMode);

            // Literal short-circuit: the top provider is not scanned. Return its value as-is and
            // skip alias discovery. Covers Ignore / Read uniformly — sources outside the scan set
            // don't participate as alias targets in either direction.
            if (hasTopLayer && topMode != ReferenceMode.Scan)
            {
                value = topValue;
                cache.TryAdd(path.Value, value);
                return true;
            }

            Value raw = hasTopLayer
                ? (topValue is null ? Value.Section(topIndex) : Value.Leaf(topValue, topIndex))
                : Value.Missing;

            // An ancestor section alias defined at a later provider shadows this key;
            // rebase the key into the aliased target and read from there. If the alias target
            // is empty (no value and no children), fall through to raw handling so a value from
            // an earlier provider below the alias still surfaces.
            if (_aliasFinder.TryFindAncestor(path, out SectionAlias alias) &&
                (!raw.Exists || alias.SourceIndex > raw.ProviderIndex))
            {
                Path targetKey = alias.Rebase(path);
                Value target = _providers.GetRawValue(targetKey, alias.SourceIndex);

                if (target.Exists)
                {
                    if (target.NeedsResolving)
                    {
                        if (!_resolver.TryResolve(targetKey, target.AsString!, target.ProviderIndex, out value))
                        {
                            // Soft fail: surface the unresolved literal from the alias target.
                            value = target.AsString;
                        }
                    }
                    else
                    {
                        value = target.AsString;
                    }

                    cache.TryAdd(path.Value, value);
                    return true;
                }

                if (_providers.GetDirectChildKeys(targetKey, alias.SourceIndex)?.Any() is true)
                {
                    value = null;
                    cache.TryAdd(path.Value, value);
                    return true;
                }

                // Alias target is empty. For a strict alias, do not fall through: earlier providers'
                // values under the aliased path are hidden by construction. Return no value.
                if (alias.Strict)
                {
                    value = null;
                    cache.TryAdd(path.Value, value);
                    return false;
                }

                // Non-strict: fall through; earlier providers' raw value (if any) wins.
            }

            if (!raw.Exists)
            {
                value = null;
                return false;
            }

            if (!raw.NeedsResolving)
            {
                value = raw.AsString;
                return true;
            }

            // A single-token section alias at this exact key surfaces as a section (null leaf).
            if (_aliasFinder.IsDirectSectionAlias(path, raw, out _))
            {
                value = null;
                cache.TryAdd(path.Value, value);
                return true;
            }

            if (!_resolver.TryResolve(path, raw.AsString!, _providers.LastIndex, out value))
            {
                // Soft fail: surface the unresolved literal rather than forcing the caller to
                // re-walk the providers.
                value = raw.AsString;
                return true;
            }

            cache.TryAdd(path.Value, value);
            return true;
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            ArgumentNullException.ThrowIfNull(earlierKeys);

            if (string.IsNullOrEmpty(parentPath) || _providers.IsEmpty)
            {
                return earlierKeys;
            }

            var parent = new Path(parentPath);
            Value raw = _providers.GetRawValue(parent);
            SectionAlias alias = SectionAlias.None;

            if (raw.NeedsResolving)
            {
                _aliasFinder.IsDirectSectionAlias(parent, raw, out alias);
            }

            if (alias.IsEmpty && !_aliasFinder.TryFindAncestor(parent, out alias))
            {
                return earlierKeys;
            }

            Path targetParent = alias.Rebase(parent);
            IEnumerable<string>? aliasedChildren = _providers.GetDirectChildKeys(targetParent, alias.SourceIndex);
            if (aliasedChildren is null)
            {
                return earlierKeys;
            }

            // Dedupe while preserving first occurrence order, then sort via ConfigurationKeyComparer.
            var merged = new List<string>();
            merged.AddRange(aliasedChildren);
            // For a strict alias, the aliased target is the sole source under this section.
            // Discard earlierKeys (which may include contributions from providers at or below the
            // alias) and re-merge only the children contributed by providers strictly above the
            // alias — those later providers shadow the alias and must still participate.
            if (alias.Strict)
            {
                IEnumerable<string>? postAliasChildren = _providers.GetDirectChildKeysAbove(parent, alias.SourceIndex);
                if (postAliasChildren is not null)
                {
                    merged.AddRange(postAliasChildren);
                }
            }
            else
            {
                merged.AddRange(earlierKeys);
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new List<string>(merged.Count);
            foreach (string s in merged)
            {
                if (seen.Add(s))
                {
                    result.Add(s);
                }
            }

            result.Sort(ConfigurationKeyComparer.Comparison);
            return result;
        }

        public void Dispose() => _changeTokenRegistration.Dispose();

        public void Invalidate()
        {
            _cache = new ConcurrentDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        private readonly struct ProviderSet
        {
            private readonly IConfigurationProvider[] _providers;
            private readonly ReferenceMode[] _modes;

            public ProviderSet(
                IReadOnlyList<IConfigurationProvider> providers,
                IReadOnlyDictionary<IConfigurationProvider, ReferenceMode>? providerModes)
            {
                _providers = [.. providers];
                _modes = new ReferenceMode[_providers.Length];
                for (int i = 0; i < _providers.Length; i++)
                {
                    _modes[i] = providerModes is not null && providerModes.TryGetValue(_providers[i], out ReferenceMode mode)
                        ? mode
                        : ReferenceMode.Read;
                }
            }

            public bool IsEmpty => _providers.Length == 0;

            public int LastIndex => _providers.Length - 1;

            private bool IsReadable(int index) => _modes[index] != ReferenceMode.Ignore;

            private bool IsScanned(int index) => _modes[index] == ReferenceMode.Scan;

            // Walks providers from upperIndex down to 0 and returns the first hit, SKIPPING
            // providers whose mode lacks the Read flag. Used by substitution and alias scans
            // so that values from non-readable sources never leak into resolved output.
            // Values from providers without the Scan flag are returned sealed so the engine
            // treats their ref(...) / fmt(...) text as literal.
            public Value GetRawValue(Path key, int? upperIndex = null)
            {
                for (int i = upperIndex ?? LastIndex; i >= 0; i--)
                {
                    if (!IsReadable(i))
                    {
                        continue;
                    }

                    if (_providers[i].TryGet(key.Value, out string? value))
                    {
                        return value is null
                            ? Value.Section(i)
                            : Value.Leaf(value, i, isSealed: !IsScanned(i));
                    }
                }

                return Value.Missing;
            }

            // Walks ALL providers top-down (regardless of mode) to find which provider owns the
            // top-most hit at the engine's entry point. Returns true with the hit's value, index,
            // and mode, false when no provider has the key.
            public bool TryGetTopLayer(Path key, out string? value, out int providerIndex, out ReferenceMode mode)
            {
                for (int i = LastIndex; i >= 0; i--)
                {
                    if (_providers[i].TryGet(key.Value, out string? v))
                    {
                        value = v;
                        providerIndex = i;
                        mode = _modes[i];
                        return true;
                    }
                }

                value = null;
                providerIndex = -1;
                mode = ReferenceMode.Ignore;
                return false;
            }

            // Merges GetChildKeys across readable providers [0..upperIndex] in ascending order.
            // Non-readable providers are skipped to keep the engine's merged child view consistent
            // with GetRawValue's skipping behavior.
            public IEnumerable<string>? GetDirectChildKeys(Path parentPath, int upperIndex)
            {
                if (upperIndex < 0)
                {
                    return null;
                }

                IEnumerable<string> childKeys = Array.Empty<string>();
                for (int i = 0; i <= upperIndex; i++)
                {
                    if (!IsReadable(i))
                    {
                        continue;
                    }

                    childKeys = _providers[i].GetChildKeys(childKeys, parentPath.Value);
                }

                return childKeys;
            }

            // Merges GetChildKeys across readable providers (lowerIndexExclusive..LastIndex].
            // Used by strict section aliases to admit only children contributed by providers
            // strictly above the alias declaration.
            public IEnumerable<string>? GetDirectChildKeysAbove(Path parentPath, int lowerIndexExclusive)
            {
                int lastIndex = LastIndex;
                if (lowerIndexExclusive >= lastIndex)
                {
                    return null;
                }

                IEnumerable<string> childKeys = Array.Empty<string>();
                for (int i = lowerIndexExclusive + 1; i <= lastIndex; i++)
                {
                    if (!IsReadable(i))
                    {
                        continue;
                    }

                    childKeys = _providers[i].GetChildKeys(childKeys, parentPath.Value);
                }

                return childKeys;
            }

            // A path is a "section" if it has no direct value but does have children.
            public bool IsSectionPath(Path path, int upperIndex)
            {
                Value raw = GetRawValue(path, upperIndex);
                if (raw.Exists)
                {
                    return raw.IsSection;
                }

                return GetDirectChildKeys(path, upperIndex)?.Any() is true;
            }

            public IChangeToken GetCompositeReloadToken()
            {
                var tokens = new List<IChangeToken>(_providers.Length);

                foreach (IConfigurationProvider provider in _providers)
                {
                    IChangeToken token = provider.GetReloadToken();
                    if (token is not null)
                    {
                        tokens.Add(token);
                    }
                }

                return tokens.Count switch
                {
                    0 => s_neverChangedToken,
                    1 => tokens[0],
                    _ => new CompositeChangeToken(tokens),
                };
            }
        }

        private readonly struct AliasFinder
        {
            private readonly ProviderSet _providers;

            public AliasFinder(ProviderSet providers)
            {
                _providers = providers;
            }

            // Detects whether `raw` at `path` is a single-token section alias (e.g. ref(Other:Section)).
            public bool IsDirectSectionAlias(Path path, Value raw, out SectionAlias alias)
            {
                if (!raw.IsLeaf)
                {
                    alias = SectionAlias.None;
                    return false;
                }

                IReadOnlyList<ValueToken> tokens = ReferenceParser.Parse(raw.AsString!);

                if (tokens.Count == 1 &&
                    tokens[0].Kind == ValueTokenKind.Reference &&
                    tokens[0].IsFromRef &&
                    TryDetectSectionTarget(tokens[0], raw.ProviderIndex, out Path targetPath))
                {
                    alias = new SectionAlias(path, targetPath, raw.ProviderIndex, tokens[0].IsStrict);
                    return true;
                }

                alias = SectionAlias.None;
                return false;
            }

            // Walks ancestors of `key` looking for a section alias that rebases the key.
            public bool TryFindAncestor(Path key, out SectionAlias alias)
            {
                int lastIndex = _providers.LastIndex;

                Path current = key;
                while (current.TryGetParent(out Path ancestor))
                {
                    Value raw = _providers.GetRawValue(ancestor, lastIndex);
                    if (raw.NeedsResolving && IsDirectSectionAlias(ancestor, raw, out alias))
                    {
                        return true;
                    }

                    current = ancestor;
                }

                alias = SectionAlias.None;
                return false;
            }

            // A token targets a section if any reference item in its chain resolves to a section
            // (order preserved; the first matching reference wins).
            private bool TryDetectSectionTarget(ValueToken token, int sourceProviderIndex, out Path targetPath)
            {
                foreach (ReferenceItem item in token.Items)
                {
                    var candidate = new Path(item.Value);
                    if (!candidate.IsEmpty && _providers.IsSectionPath(candidate, sourceProviderIndex))
                    {
                        targetPath = candidate;
                        return true;
                    }
                }

                targetPath = default;
                return false;
            }

            // Shadowing lookup via ancestor section alias. Returns true with the aliased `result` and the
            // `effectivePath` where it physically lives when:
            //   - an ancestor of `key` is a section alias visible within `upperIndex`, AND
            //   - either `raw` is missing or the alias was declared at a later provider (shadowing rule), AND
            //   - the rebased target has a direct value or children.
            // Returns false when no alias applies or the alias target is absent (caller uses `raw`).
            public bool TryResolveViaAncestor(Path key, int upperIndex, Value raw, out Value result, out Path effectivePath)
            {
                if (TryFindAncestor(key, out SectionAlias alias) &&
                    alias.SourceIndex <= upperIndex &&
                    (!raw.Exists || alias.SourceIndex > raw.ProviderIndex))
                {
                    effectivePath = alias.Rebase(key);
                    Value target = _providers.GetRawValue(effectivePath, alias.SourceIndex);
                    if (target.Exists)
                    {
                        result = target;
                        return true;
                    }

                    if (_providers.GetDirectChildKeys(effectivePath, alias.SourceIndex)?.Any() is true)
                    {
                        result = Value.Section(alias.SourceIndex);
                        return true;
                    }
                }

                result = default;
                effectivePath = default;
                return false;
            }
        }

        private readonly struct ReferenceResolver
        {
            private readonly ProviderSet _providers;
            private readonly AliasFinder _aliasFinder;

            public ReferenceResolver(ProviderSet providers, AliasFinder aliasFinder)
            {
                _providers = providers;
                _aliasFinder = aliasFinder;
            }

            public bool TryResolve(Path originKey, string rawValue, int upperIndex, out string? value)
                => TryResolveValue(originKey, rawValue, resolutionStack: null, depth: 0, upperIndex, out value);

            private bool TryResolveValue(Path originKey, string rawValue, HashSet<Path>? resolutionStack, int depth, int upperIndex, out string? value)
            {
                if (depth > MaxDepth)
                {
                    throw new InvalidOperationException(SR.Format(SR.ReferenceResolution_MaxDepthExceeded, MaxDepth, originKey));
                }

                // Only start paying for cycle tracking once recursion is deep enough to suggest a loop.
                if (depth >= CycleCheckThreshold && resolutionStack is null)
                {
                    resolutionStack = new HashSet<Path>();
                }

                if (resolutionStack is not null && !resolutionStack.Add(originKey))
                {
                    throw new InvalidOperationException(SR.Format(SR.ReferenceResolution_CircularReference, originKey));
                }

                try
                {
                    IReadOnlyList<ValueToken> tokens = ReferenceParser.Parse(rawValue);
                    if (tokens.Count == 1 && tokens[0].Kind == ValueTokenKind.Reference && tokens[0].IsFromRef)
                    {
                        return TryResolveToken(originKey, tokens[0], resolutionStack, depth + 1, upperIndex, out value);
                    }

                    var builder = new StringBuilder();
                    foreach (ValueToken token in tokens)
                    {
                        if (token.Kind == ValueTokenKind.Literal)
                        {
                            builder.Append(token.Value);
                            continue;
                        }

                        if (!TryResolveToken(originKey, token, resolutionStack, depth + 1, upperIndex, out string? resolvedTokenValue))
                        {
                            value = null;
                            return false;
                        }

                        builder.Append(resolvedTokenValue ?? string.Empty);
                    }

                    value = builder.ToString();
                    return true;
                }
                finally
                {
                    resolutionStack?.Remove(originKey);
                }
            }

            private bool TryResolveToken(Path storageKey, ValueToken token, HashSet<Path>? resolutionStack, int depth, int upperIndex, out string? value)
            {
                foreach (ReferenceItem item in token.Items)
                {
                    if (!TryBuildAbsolutePath(storageKey, item, out Path tokenPath))
                    {
                        continue;
                    }

                    Value raw = _providers.GetRawValue(tokenPath, upperIndex);

                    // Honor ancestor section aliases so tokens inside a value see the same key space as
                    // direct reads. Matches TryGet's shadowing rules: an alias at a later provider wins
                    // over a raw value at an earlier one; a missing alias target falls back to `raw`.
                    if (_aliasFinder.TryResolveViaAncestor(tokenPath, upperIndex, raw, out Value aliased, out _))
                    {
                        raw = aliased;
                    }

                    if (!raw.Exists)
                    {
                        continue;
                    }

                    if (raw.NeedsResolving)
                    {
                        return TryResolveValue(tokenPath, raw.AsString!, resolutionStack, depth, raw.ProviderIndex, out value);
                    }

                    value = raw.AsString;
                    return true;
                }

                if (token.HasDefault)
                {
                    value = token.LiteralDefault!;
                    return true;
                }

                value = null;
                return false;
            }

            private static bool TryBuildAbsolutePath(Path storageKey, ReferenceItem item, out Path absolute)
            {
                if (item.ParentHops == 0)
                {
                    absolute = new Path(item.Value);
                    return true;
                }

                if (!storageKey.TryGetAncestor(item.ParentHops, out Path anchor))
                {
                    absolute = Path.Empty;
                    return false;
                }

                absolute = item.Value.Length == 0 ? anchor : anchor.Combine(item.Value);
                return true;
            }
        }

        private readonly struct Path : IComparable<Path>, IEquatable<Path>
        {
            public static Path Empty => default;

            public string Value { get; }

            public int Length => Value?.Length ?? 0;

            public bool IsEmpty => string.IsNullOrEmpty(Value);

            public Path(string value)
            {
                Value = value.Trim().Replace('.', ConfigurationPath.KeyDelimiter[0]);
            }

            public bool IsParentOf(Path candidate)
            {
                return candidate.Length > Length &&
                    candidate.Value.StartsWith(Value, StringComparison.OrdinalIgnoreCase) &&
                    candidate.Value[Length] == ConfigurationPath.KeyDelimiter[0];
            }

            public bool IsEqualTo(Path candidate) => Equals(candidate);

            public bool IsChildOf(Path candidate) => candidate.IsParentOf(this);

            public Path Combine(string child)
            {
                if (IsEmpty)
                {
                    return new Path(child);
                }

                return new Path(Value + ConfigurationPath.KeyDelimiter + child);
            }

            public bool TryGetParent(out Path parent)
            {
                int idx = Value?.LastIndexOf(ConfigurationPath.KeyDelimiter[0]) ?? -1;
                if (idx < 0)
                {
                    parent = Empty;
                    return false;
                }

                parent = new Path(Value!.Substring(0, idx));
                return true;
            }

            public bool TryGetAncestor(int hops, out Path ancestor)
            {
                ancestor = this;
                for (int n = 0; n < hops; n++)
                {
                    if (!ancestor.TryGetParent(out Path next))
                    {
                        ancestor = Empty;
                        return false;
                    }

                    ancestor = next;
                }

                return true;
            }

            public Path Rebase(Path fromRoot, Path toRoot)
            {
                if (Length == fromRoot.Length)
                {
                    return toRoot;
                }

                return new Path(toRoot.Value + Value.Substring(fromRoot.Length));
            }

            public bool Equals(Path other) => string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

            public override bool Equals(object? obj) => obj is Path other && Equals(other);

            public override int GetHashCode() => Value is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

            public override string ToString() => Value ?? string.Empty;

            public int CompareTo(Path other) => ConfigurationKeyComparer.Instance.Compare(Value, other.Value);
        }

        private readonly struct SectionAlias
        {
            public static SectionAlias None => new(default, default, -1, strict: false);

            public Path Source { get; }

            public Path Target { get; }

            public int SourceIndex { get; }

            public bool Strict { get; }

            public SectionAlias(Path source, Path target, int sourceIndex, bool strict)
            {
                Source = source;
                Target = target;
                SourceIndex = sourceIndex;
                Strict = strict;
            }

            public bool IsEmpty => SourceIndex < 0;

            public Path Rebase(Path key) => key.Rebase(Source, Target);
        }

        private readonly struct Value
        {
            public static Value Missing => default;

            public static Value Section(int providerIndex) => new(null, providerIndex, isLeaf: false, exists: true, isSealed: false);

            public static Value Leaf(string? value, int providerIndex, bool isSealed = false) => new(value, providerIndex, isLeaf: true, exists: true, isSealed: isSealed);

            private Value(string? value, int providerIndex, bool isLeaf, bool exists, bool isSealed)
            {
                AsString = value;
                ProviderIndex = providerIndex;
                IsLeaf = isLeaf;
                Exists = exists;
                IsSealed = isSealed;
            }

            public string? AsString { get; }

            public int ProviderIndex { get; }

            public bool Exists { get; }

            public bool IsLeaf { get; }

            // True when the value came from another ReferenceResolutionConfigurationProvider and is
            // already fully resolved. Later reference-resolution providers treat it as a literal and do
            // not rescan it for tokens.
            public bool IsSealed { get; }

            public bool IsSection => Exists && !IsLeaf;

            public bool NeedsResolving =>
                IsLeaf && !IsSealed && AsString is not null && ReferenceParser.ContainsReference(AsString);
        }
    }
}

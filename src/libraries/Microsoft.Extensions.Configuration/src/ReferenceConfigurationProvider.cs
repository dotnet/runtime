// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
#if NET9_0_OR_GREATER
using System.Buffers;
#endif
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    // Internal provider that materialises reference rules against an upstream snapshot.
    // Appended last to a configuration root's provider list so its TryGet beats the upstream
    // providers it depends on. The materialiser deliberately omits keys where a direct
    // upstream literal should win, so the "last wins" precedence still produces the right
    // observable behaviour.
    //
    // Reload-on-upstream-change: this provider subscribes to each upstream's reload token.
    // When any upstream reloads, the provider re-materialises and raises its own token, which
    // the root's per-provider OnChange registration picks up.
    internal sealed class ReferenceConfigurationProvider : ConfigurationProvider, IDisposable
    {
        private readonly IReadOnlyDictionary<string, ReferenceRule> _concreteRules;
        private readonly List<ReferenceRule> _templateRules;
        private readonly Func<string, ConfigurationExpansion?> _parser;
        private readonly IReadOnlyList<IConfigurationProvider> _upstream;
        private readonly List<IDisposable> _upstreamRegistrations;
        private bool _disposed;

        internal ReferenceConfigurationProvider(
            ReferenceConfigurationSource source,
            IReadOnlyList<IConfigurationProvider> upstream)
        {
            _concreteRules = source.ConcreteRules;
            // Templates are sorted by specificity descending so the most-specific template
            // matches first when several would match the same concrete key.
            var templates = new List<ReferenceRule>(source.TemplateRules);
            templates.Sort(static (a, b) =>
            {
                int byLit = b.SubjectPattern.LiteralCharCount.CompareTo(a.SubjectPattern.LiteralCharCount);
                if (byLit != 0) return byLit;
                int byDoubleStar = a.SubjectPattern.DoubleStarCount.CompareTo(b.SubjectPattern.DoubleStarCount);
                if (byDoubleStar != 0) return byDoubleStar;
                return a.SubjectPattern.StarCount.CompareTo(b.SubjectPattern.StarCount);
            });
            _templateRules = templates;
            _parser = source.Parser;
            _upstream = upstream;
            _upstreamRegistrations = new List<IDisposable>(upstream.Count);
            foreach (IConfigurationProvider p in upstream)
            {
                _upstreamRegistrations.Add(ChangeToken.OnChange(p.GetReloadToken, OnUpstreamChanged));
            }
        }

        public override void Load()
        {
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Phase 1: concrete rules resolve by direct key lookup.
            var matched = new Dictionary<string, ConfigurationExpansion>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, ReferenceRule> kv in _concreteRules)
            {
                if (kv.Value.Resolve(kv.Key, ReadUpstream(kv.Key), _parser) is { } spec)
                {
                    matched[kv.Key] = spec;
                }
            }

            // Phase 2: template rules match against the upstream key space; concrete
            // matches always win, and among templates the most-specific declared rule
            // wins (templates are pre-sorted by specificity descending).
            if (_templateRules.Count > 0)
            {
                foreach (string upstreamKey in EnumerateUpstreamKeys())
                {
                    if (matched.ContainsKey(upstreamKey))
                    {
                        continue;
                    }
                    string[] segs = upstreamKey.Split(ConfigurationPath.KeyDelimiter[0]);
                    foreach (ReferenceRule env in _templateRules)
                    {
                        if (!env.SubjectPattern.TryMatch(segs))
                        {
                            continue;
                        }
                        if (env.Resolve(upstreamKey, ReadUpstream(upstreamKey), _parser) is { } spec)
                        {
                            matched[upstreamKey] = spec;
                        }
                        break;
                    }
                }
            }

            // Materialise resolved subjects, deferring any whose referenced subjects are not yet
            // materialised so a dependency's entries are populated before any subject that resolves
            // through them. Each sweep advances at least one subject; a sweep that cannot advance means
            // the remaining subjects form a cycle.
            string[] resolvedByLengthDesc = ByLengthDesc(matched.Keys);
            var pending = new List<string>(matched.Keys);
            while (pending.Count > 0)
            {
                bool progress = false;
                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    string subject = pending[i];
                    if (FirstUnresolvedDependency(subject, matched[subject], resolvedByLengthDesc, values) is null)
                    {
                        Materialise(subject, matched[subject], values);
                        pending.RemoveAt(i);
                        progress = true;
                    }
                }

                if (!progress)
                {
                    string stuck = pending[0];
                    string revisits = FirstUnresolvedDependency(stuck, matched[stuck], resolvedByLengthDesc, values) ?? stuck;
                    throw new InvalidOperationException(SR.Format(SR.Error_ReferenceCycle, stuck, revisits));
                }
            }

            Data = values;
        }

        private void Materialise(string subject, ConfigurationExpansion spec, Dictionary<string, string?> values)
        {
            if (spec.Template is null)
            {
                string target = spec.ReferencedKeys[0]!;
                // Subject itself: always populated so we mask the raw reference literal.
                values[subject] = OverlayAwareRead(target, values);
                MirrorSubtree(target, subject, values);
            }
            else if (spec.ReferencedKeys.Count == 0)
            {
                values[subject] = spec.Template;
            }
            else
            {
                values[subject] = Compose(subject, spec, values);
            }
        }

        private string Compose(string subject, ConfigurationExpansion spec, Dictionary<string, string?> values)
        {
            StringValues references = spec.ReferencedKeys;
            int count = references.Count;
            // Compose only runs for the Format kind, so the template is non-null.
            string template = spec.Template!;

#if NET9_0_OR_GREATER
            // Modern path: format from an exact-length span over a pooled buffer, so no per-composition array is
            // allocated. The span is exactly `count` long, so an over-indexing template still throws (the buffer
            // may be larger than needed, but string.Format never sees past the span).
            object?[] rented = count == 0 ? Array.Empty<object?>() : ArrayPool<object?>.Shared.Rent(count);
            try
            {
                FillComposeArgs(references, subject, values, rented);
                return string.Format(CultureInfo.InvariantCulture, template, rented.AsSpan(0, count));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_ReferenceTemplateInvalid, subject), ex);
            }
            finally
            {
                if (count != 0)
                {
                    // clearArray so resolved values (possibly secrets) don't linger in the shared pool.
                    ArrayPool<object?>.Shared.Return(rented, clearArray: true);
                }
            }
#else
            var args = new object?[count];
            FillComposeArgs(references, subject, values, args);
            try
            {
                return string.Format(CultureInfo.InvariantCulture, template, args);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException(SR.Format(SR.Error_ReferenceTemplateInvalid, subject), ex);
            }
#endif
        }

        private void FillComposeArgs(StringValues references, string subject, Dictionary<string, string?> values, object?[] args)
        {
            int i = 0;
            foreach (string? reference in references)
            {
                string? value = OverlayAwareRead(reference!, values);
                if (value is null)
                {
                    throw new InvalidOperationException(
                        SR.Format(SR.Error_ReferenceComposedValueMissing, reference, subject));
                }
                args[i++] = value;
            }
        }

        // Indexer-based writes flow through ConfigurationRoot.SetConfiguration, which calls
        // Set on every provider in declaration order. Reference providers always live at the
        // tail, so by the time Set lands here every upstream provider has already absorbed the
        // new value. Re-materialising now picks it up; Data is computed, never user-set.
        public override void Set(string key, string? value)
        {
            Load();
        }

        private void OnUpstreamChanged()
        {
            if (_disposed)
            {
                return;
            }
            Load();
            OnReload();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            foreach (IDisposable r in _upstreamRegistrations)
            {
                r.Dispose();
            }
            _upstreamRegistrations.Clear();
        }

        private static string[] ByLengthDesc(IEnumerable<string> keys)
        {
            var list = new List<string>(keys);
            list.Sort(static (a, b) => b.Length.CompareTo(a.Length));
            return list.ToArray();
        }

        private IEnumerable<string> EnumerateUpstreamKeys()
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<string?>();
            queue.Enqueue(null);

            while (queue.Count > 0)
            {
                string? parent = queue.Dequeue();
                IEnumerable<string> children = Array.Empty<string>();
                for (int i = 0; i < _upstream.Count; i++)
                {
                    children = _upstream[i].GetChildKeys(children, parent);
                }
                foreach (string seg in children)
                {
                    string full = parent is null ? seg : parent + ConfigurationPath.KeyDelimiter + seg;
                    if (visited.Add(full))
                    {
                        yield return full;
                        queue.Enqueue(full);
                    }
                }
            }
        }

        // Returns null when every subject this spec references is already materialised; otherwise the
        // first referenced subject still awaiting materialisation. Drives both the readiness test for the
        // materialisation worklist and the subject named when the remaining subjects form a cycle. A direct
        // self-reference is not treated as a dependency, matching its resolve-to-literal behaviour.
        private static string? FirstUnresolvedDependency(
            string subject,
            ConfigurationExpansion spec,
            string[] subjectsByLengthDesc,
            Dictionary<string, string?> values)
        {
            foreach (string? referenced in spec.ReferencedKeys)
            {
                if (TryFindSubject(subjectsByLengthDesc, referenced!, out string? dep)
                    && !string.Equals(dep, subject, StringComparison.OrdinalIgnoreCase)
                    && !values.ContainsKey(dep))
                {
                    return dep;
                }
            }
            return null;
        }

        private void MirrorSubtree(string target, string subject, Dictionary<string, string?> values)
        {
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { target };
            var queue = new Queue<string>();
            EnqueueChildren(target, values, visited, queue);

            while (queue.Count > 0)
            {
                string childKey = queue.Dequeue();
                string suffix = childKey.Substring(target.Length + 1);
                string overlayKey = subject + ConfigurationPath.KeyDelimiter + suffix;

                if (!values.ContainsKey(overlayKey))
                {
                    // A non-empty upstream literal at the overlay key should win on read;
                    // skip the overlay entry so it does.
                    string? sourceAtOverlay = ReadUpstream(overlayKey);
                    if (string.IsNullOrEmpty(sourceAtOverlay))
                    {
                        string? targetValue = OverlayAwareRead(childKey, values);
                        if (targetValue is not null)
                        {
                            values[overlayKey] = targetValue;
                        }
                    }
                }
                EnqueueChildren(childKey, values, visited, queue);
            }
        }

        private void EnqueueChildren(
            string parentKey,
            Dictionary<string, string?> values,
            HashSet<string> visited,
            Queue<string> queue)
        {
            IEnumerable<string> chained = Array.Empty<string>();
            for (int i = 0; i < _upstream.Count; i++)
            {
                chained = _upstream[i].GetChildKeys(chained, parentKey);
            }
            foreach (string seg in chained)
            {
                string child = parentKey + ConfigurationPath.KeyDelimiter + seg;
                if (visited.Add(child))
                {
                    queue.Enqueue(child);
                }
            }

            string prefix = parentKey + ConfigurationPath.KeyDelimiter;
            foreach (string k in values.Keys)
            {
                if (k.Length <= prefix.Length || !k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                int next = k.IndexOf(ConfigurationPath.KeyDelimiter[0], prefix.Length);
                string child = next < 0 ? k : k.Substring(0, next);
                if (visited.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        private string? ReadUpstream(string key)
        {
            for (int i = _upstream.Count - 1; i >= 0; i--)
            {
                if (_upstream[i].TryGet(key, out string? value))
                {
                    return value;
                }
            }
            return null;
        }

        // Reads the value already materialised for <paramref name="key"/> if any; otherwise
        // falls back to the first non-empty upstream literal at the same key.
        private string? OverlayAwareRead(string key, Dictionary<string, string?> values)
        {
            if (values.TryGetValue(key, out string? ov))
            {
                return ov;
            }
            for (int i = _upstream.Count - 1; i >= 0; i--)
            {
                if (_upstream[i].TryGet(key, out string? pv) && !string.IsNullOrEmpty(pv))
                {
                    return pv;
                }
            }
            return null;
        }

        private static bool TryFindSubject(string[] subjectsByLengthDesc, string key, [NotNullWhen(true)] out string? subject)
        {
            for (int i = 0; i < subjectsByLengthDesc.Length; i++)
            {
                string s = subjectsByLengthDesc[i];
                if (key.Length >= s.Length
                    && key.StartsWith(s, StringComparison.OrdinalIgnoreCase)
                    && (key.Length == s.Length || key[s.Length] == ConfigurationPath.KeyDelimiter[0]))
                {
                    subject = s;
                    return true;
                }
            }
            subject = null;
            return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration
{
    // A single reference rule: the canonical subject pattern plus the permitted (and vetoed)
    // target patterns. Resolution and validation flow through this type; the materialiser walks
    // rules, matching concrete rules by direct lookup and template rules by glob.
    internal sealed class ReferenceRule
    {
        private readonly List<KeyPattern> _targets = new();
        private readonly List<KeyPattern> _disallowedTargets = new();

        internal ReferenceRule(string subject)
        {
            Subject = subject;
            SubjectPattern = new KeyPattern(subject);
        }

        internal string Subject { get; }

        internal KeyPattern SubjectPattern { get; }

        internal bool IsConcrete => !SubjectPattern.HasWildcard;

        internal void AddTarget(string target)
        {
            var pattern = new KeyPattern(target);
            foreach (KeyPattern existing in _targets)
            {
                if (string.Equals(existing.Pattern, pattern.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            _targets.Add(pattern);
        }

        internal void AddDisallowedTarget(string target)
        {
            var pattern = new KeyPattern(target);
            foreach (KeyPattern existing in _disallowedTargets)
            {
                if (string.Equals(existing.Pattern, pattern.Pattern, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            _disallowedTargets.Add(pattern);
        }

        // Translates a raw literal at <paramref name="subjectKey"/> (the matched subject key) into
        // the reference it resolves to, or <see langword="null"/> if no resolution applies:
        //   * Empty/null selection: null.
        //   * A literal the parser does not recognise as a reference: null (a direct value at the
        //     subject wins on read naturally).
        //   * A recognised reference: every key it points at must be a permitted target of this
        //     rule; otherwise resolution throws. A Value spec names no targets, so it is never
        //     subject to the target check.
        internal ConfigurationExpansion? Resolve(
            string subjectKey,
            string? selection,
            Func<ConfigurationReferenceContext, ConfigurationExpansion?> parser,
            IReadOnlyList<IConfigurationProvider> providers)
        {
            if (string.IsNullOrEmpty(selection))
            {
                return null;
            }

            if (parser(new ConfigurationReferenceContext(subjectKey, selection!, providers)) is not { } spec)
            {
                return null;
            }

            foreach (string? reference in spec.Keys)
            {
                if (!IsTargetAllowed(reference!))
                {
                    throw new InvalidOperationException(
                        SR.Format(SR.Error_ReferenceTargetNotAllowed, subjectKey, selection, reference));
                }
            }

            return spec;
        }

        private bool IsTargetAllowed(string canonical)
        {
            string[] segs = canonical.Split(ConfigurationPath.KeyDelimiter[0]);

            bool allowed = false;
            foreach (KeyPattern p in _targets)
            {
                if (p.TryMatch(segs))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
            {
                return false;
            }

            foreach (KeyPattern p in _disallowedTargets)
            {
                if (p.TryMatch(segs))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Declares which configuration keys may hold references and which targets those references may point at, and
    /// optionally swaps the default reference recogniser. The rule set declared here is the security boundary: a
    /// reference resolves only if a rule admits both its subject and its target.
    /// </summary>
    public sealed class ConfigurationReferenceBuilder
    {
        private const string ReferenceMarkerPrefix = "ref(";
        private const string FormatMarkerPrefix = "format(";

        private readonly Dictionary<string, ReferenceRule> _concreteRules =
            new Dictionary<string, ReferenceRule>(StringComparer.OrdinalIgnoreCase);
        private readonly List<ReferenceRule> _templateRules = new List<ReferenceRule>();
        private Func<string, ConfigurationExpansion?> _parser = DefaultParse;

        internal ConfigurationReferenceBuilder() { }

        /// <summary>
        /// Permits keys matching <paramref name="subject"/> to reference keys matching <paramref name="target"/>
        /// (or any of <paramref name="additionalTargets"/>). Patterns may use <c>*</c> (within a segment) and
        /// <c>**</c> (across segments). Calling <see cref="Allow"/> repeatedly for the same subject extends its
        /// permitted target set.
        /// </summary>
        /// <param name="subject">The subject key or pattern that may hold a reference.</param>
        /// <param name="target">A target key or pattern that may be referenced.</param>
        /// <param name="additionalTargets">Further target keys or patterns that may be referenced.</param>
        /// <returns>The same <see cref="ConfigurationReferenceBuilder"/> so calls can be chained.</returns>
        public ConfigurationReferenceBuilder Allow(string subject, string target, params string[] additionalTargets)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(additionalTargets);

            ReferenceRule rule = FindOrCreateRule(NormaliseSubject(subject));
            rule.AddTarget(NormaliseTarget(target));
            foreach (string additionalTarget in additionalTargets)
            {
                rule.AddTarget(NormaliseTarget(additionalTarget));
            }
            return this;
        }

        /// <summary>
        /// Vetoes references from keys matching <paramref name="subject"/> to keys matching <paramref name="target"/>
        /// (or any of <paramref name="additionalTargets"/>), overriding any <see cref="Allow"/> that would otherwise
        /// permit them.
        /// </summary>
        /// <param name="subject">The subject key or pattern to veto.</param>
        /// <param name="target">A target key or pattern to veto.</param>
        /// <param name="additionalTargets">Further target keys or patterns to veto.</param>
        /// <returns>The same <see cref="ConfigurationReferenceBuilder"/> so calls can be chained.</returns>
        public ConfigurationReferenceBuilder Deny(string subject, string target, params string[] additionalTargets)
        {
            ArgumentNullException.ThrowIfNull(subject);
            ArgumentNullException.ThrowIfNull(target);
            ArgumentNullException.ThrowIfNull(additionalTargets);

            ReferenceRule rule = FindOrCreateRule(NormaliseSubject(subject));
            rule.AddDisallowedTarget(NormaliseTarget(target));
            foreach (string additionalTarget in additionalTargets)
            {
                rule.AddDisallowedTarget(NormaliseTarget(additionalTarget));
            }
            return this;
        }

        /// <summary>
        /// Gets or sets the recogniser that maps a raw configuration value to a <see cref="ConfigurationExpansion"/>,
        /// or to <see langword="null"/> when the value is not a reference. It starts as the default recogniser, which
        /// maps <c>ref(target)</c> to a reference and <c>format(template, ref1, ref2, ...)</c> to a composed value.
        /// Replace it to recognise a different syntax; capture the current value first to fall back to it.
        /// </summary>
        public Func<string, ConfigurationExpansion?> Parser
        {
            get => _parser;
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                _parser = value;
            }
        }

        internal IReadOnlyDictionary<string, ReferenceRule> ConcreteRules => _concreteRules;

        internal IReadOnlyList<ReferenceRule> TemplateRules => _templateRules;

        private ReferenceRule FindOrCreateRule(string normalisedSubject)
        {
            if (_concreteRules.TryGetValue(normalisedSubject, out ReferenceRule? existing))
            {
                return existing;
            }

            foreach (ReferenceRule rule in _templateRules)
            {
                if (string.Equals(rule.Subject, normalisedSubject, StringComparison.OrdinalIgnoreCase))
                {
                    return rule;
                }
            }

            var created = new ReferenceRule(normalisedSubject);
            if (created.IsConcrete)
            {
                _concreteRules[normalisedSubject] = created;
            }
            else
            {
                _templateRules.Add(created);
            }
            return created;
        }

        private static string NormaliseSubject(string subject)
        {
            string normalised = Normalise(subject);
            if (normalised.Length == 0)
            {
                throw new ArgumentException(SR.Error_ReferenceSubjectEmpty, nameof(subject));
            }
            return normalised;
        }

        private static string NormaliseTarget(string target)
        {
            string normalised = Normalise(target);
            if (normalised.Length == 0)
            {
                throw new ArgumentException(SR.Format(SR.Error_ReferenceTargetInvalid, target), nameof(target));
            }
            return normalised;
        }

        // Trims the leading and trailing key delimiters from a raw subject or target string.
        private static string Normalise(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }
            int start = 0;
            int end = raw.Length;
            while (start < end && raw[start] == ConfigurationPath.KeyDelimiter[0])
            {
                start++;
            }
            while (end > start && raw[end - 1] == ConfigurationPath.KeyDelimiter[0])
            {
                end--;
            }
            return start == 0 && end == raw.Length ? raw : raw.Substring(start, end - start);
        }

        private static ConfigurationExpansion? DefaultParse(string value)
        {
            // An escaped marker (\ref(...) or \format(...)) is taken as the literal text with one backslash removed.
            if (value.StartsWith("\\" + ReferenceMarkerPrefix, StringComparison.Ordinal) ||
                value.StartsWith("\\" + FormatMarkerPrefix, StringComparison.Ordinal))
            {
                return ConfigurationExpansion.Literal(value.Substring(1));
            }

            if (TryGetMarkerBody(value, ReferenceMarkerPrefix, out string? referenceBody))
            {
                string target = referenceBody.Trim();
                return target.Length == 0 ? (ConfigurationExpansion?)null : ConfigurationExpansion.Reference(target);
            }

            if (TryGetMarkerBody(value, FormatMarkerPrefix, out string? formatBody))
            {
                return ParseFormat(formatBody);
            }

            return null;
        }

        // Extracts the text between a "marker(" prefix and the trailing ")"; returns false when the value is not such
        // a marker.
        private static bool TryGetMarkerBody(string value, string prefix, [NotNullWhen(true)] out string? body)
        {
            if (value.Length > prefix.Length &&
                value[value.Length - 1] == ')' &&
                value.StartsWith(prefix, StringComparison.Ordinal))
            {
                body = value.Substring(prefix.Length, value.Length - prefix.Length - 1);
                return true;
            }

            body = null;
            return false;
        }

        // format(template, ref1, ref2, ...): the first unescaped comma separates the template from the
        // comma-delimited reference keys. A literal comma in the template is written as "\," (reference keys are
        // configuration keys and need no escaping). With no references the template is taken verbatim.
        private static ConfigurationExpansion? ParseFormat(string body)
        {
            if (body.Length == 0)
            {
                return null;
            }

            int split = IndexOfUnescapedComma(body);
            if (split < 0)
            {
                return ConfigurationExpansion.Format(UnescapeCommas(body));
            }

            string template = UnescapeCommas(body.Substring(0, split));

            string[] rawReferences = body.Substring(split + 1).Split(',');
            var references = new List<string>(rawReferences.Length);
            foreach (string rawReference in rawReferences)
            {
                string reference = rawReference.Trim();
                if (reference.Length != 0)
                {
                    references.Add(reference);
                }
            }

            return references.Count == 0
                ? ConfigurationExpansion.Format(template)
                : ConfigurationExpansion.Format(template, references.ToArray());
        }

        private static int IndexOfUnescapedComma(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == ',' && (i == 0 || value[i - 1] != '\\'))
                {
                    return i;
                }
            }

            return -1;
        }

        private static string UnescapeCommas(string value) =>
            value.IndexOf('\\') < 0 ? value : value.Replace("\\,", ",");
    }
}

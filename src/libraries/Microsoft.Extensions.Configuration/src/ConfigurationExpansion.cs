// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Describes how the framework expands a configuration value into its effective value: a redirect to another
    /// key (a reference), a verbatim literal, or a formatted composition. An expansion is produced by a parser
    /// (see <see cref="ConfigurationReferenceBuilder.Parser"/>) and consumed by the configuration reference
    /// machinery; it never reads configuration itself, it only names the keys the framework should resolve.
    /// </summary>
    public readonly struct ConfigurationExpansion
    {
        private ConfigurationExpansion(string? template, StringValues keys)
        {
            Template = template;
            Keys = keys;
        }

        /// <summary>
        /// Creates an expansion that passes through the value of <paramref name="key"/>. If the target is a leaf, the
        /// subject takes its scalar value; if it is a section, the subject mirrors the whole subtree.
        /// </summary>
        /// <param name="key">The target key to resolve and pass through.</param>
        public static ConfigurationExpansion Reference(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new ConfigurationExpansion(template: null, new StringValues(key));
        }

        /// <summary>
        /// Creates an expansion whose resolved value is <paramref name="value"/> verbatim. The value is taken as-is and is
        /// never passed through <see cref="string.Format(string, object[])"/>, so braces are safe.
        /// </summary>
        /// <param name="value">The literal value the subject resolves to, or <see langword="null"/> for no value.</param>
        public static ConfigurationExpansion Literal(string? value) => new ConfigurationExpansion(value, StringValues.Empty);

        /// <summary>
        /// Creates an expansion whose resolved value is <see cref="string.Format(string, object[])"/> applied to
        /// <paramref name="template"/> with the resolved value of <paramref name="key"/> as the single argument.
        /// </summary>
        /// <param name="template">A composite format string (for example <c>"Server={0}"</c>).</param>
        /// <param name="key">The target key whose resolved value fills the placeholder.</param>
        public static ConfigurationExpansion Format(string template, string key)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(key);
            return new ConfigurationExpansion(template, new StringValues(key));
        }

        /// <summary>
        /// Creates an expansion whose resolved value is <see cref="string.Format(string, object[])"/> applied to
        /// <paramref name="template"/> with the resolved values of <paramref name="keys"/> as arguments.
        /// </summary>
        /// <param name="template">A composite format string (for example <c>"Server={0};Port={1}"</c>).</param>
        /// <param name="keys">The target keys whose resolved values fill the placeholders, in order.</param>
        /// <remarks>If no keys are supplied, the template is taken verbatim, without a formatting pass.</remarks>
        public static ConfigurationExpansion Format(string template, params string[] keys)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(keys);
            return new ConfigurationExpansion(template, keys.Length switch
            {
                0 => StringValues.Empty,
                1 => new StringValues(keys[0]),
                _ => new StringValues(keys),
            });
        }

        /// <summary>
        /// Gets the literal value (for an expansion created by <see cref="Literal"/>) or the composite format
        /// template (for one created by <see cref="Format(string, string[])"/>), or <see langword="null"/> for a
        /// reference created by <see cref="Reference"/>.
        /// </summary>
        public string? Template { get; }

        /// <summary>
        /// Gets the keys this expansion refers to: empty for a verbatim literal, the single key for a reference or
        /// single-key format, or several keys for a multi-key format.
        /// </summary>
        public StringValues Keys { get; }
    }
}

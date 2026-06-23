// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Describes how the framework expands a configuration value into its effective value: a redirect to another
    /// key (a reference), a verbatim literal, or a formatted composition. An expansion is produced by a parser
    /// (see <see cref="AllowedReferencesBuilder.Parser"/>) and consumed by the configuration reference machinery;
    /// it never reads configuration itself, it only names the targets the framework should resolve.
    /// </summary>
    public readonly struct Expansion
    {
        private Expansion(string? template, StringValues references)
        {
            Template = template;
            References = references;
        }

        /// <summary>
        /// Creates an expansion that passes through the value of <paramref name="key"/>. If the target is a leaf, the
        /// subject takes its scalar value; if it is a section, the subject mirrors the whole subtree.
        /// </summary>
        /// <param name="key">The target key to resolve and pass through.</param>
        public static Expansion Reference(string key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return new Expansion(template: null, new StringValues(key));
        }

        /// <summary>
        /// Creates an expansion whose resolved value is <paramref name="value"/> verbatim. The value is taken as-is and is
        /// never passed through <see cref="string.Format(string, object[])"/>, so braces are safe.
        /// </summary>
        /// <param name="value">The literal value the subject resolves to.</param>
        public static Expansion Value(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new Expansion(value, StringValues.Empty);
        }

        /// <summary>
        /// Creates an expansion whose resolved value is <see cref="string.Format(string, object[])"/> applied to
        /// <paramref name="template"/> with the resolved values of <paramref name="references"/> as arguments.
        /// </summary>
        /// <param name="template">A composite format string (for example <c>"Server={0};Port={1}"</c>).</param>
        /// <param name="references">The target keys whose resolved values fill the placeholders, in order.</param>
        /// <remarks>If no references are supplied, the template is taken verbatim, without a formatting pass.</remarks>
        public static Expansion Format(string template, params string[] references)
        {
            ArgumentNullException.ThrowIfNull(template);
            ArgumentNullException.ThrowIfNull(references);
            return new Expansion(template, references.Length switch
            {
                0 => StringValues.Empty,
                1 => new StringValues(references[0]),
                _ => new StringValues(references),
            });
        }

        /// <summary>
        /// Gets the literal value (for an expansion created by <see cref="Value"/>) or the composite format
        /// template (for one created by <see cref="Format"/>), or <see langword="null"/> for a reference created
        /// by <see cref="Reference"/>.
        /// </summary>
        public string? Template { get; }

        /// <summary>
        /// Gets the target keys this expansion refers to: empty for a verbatim value, the single target for a
        /// reference or single-target format, or several targets for a multi-target format.
        /// </summary>
        public StringValues References { get; }
    }
}

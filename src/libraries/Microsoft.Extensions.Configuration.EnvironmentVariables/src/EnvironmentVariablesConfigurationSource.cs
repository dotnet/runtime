// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    /// <summary>
    /// Represents environment variables as an <see cref="IConfigurationSource"/>.
    /// </summary>
    public class EnvironmentVariablesConfigurationSource : IConfigurationSource
    {
        /// <summary>
        /// The default transformation that replaces double underscores (<c>__</c>) with the
        /// configuration key delimiter (<c>:</c>).
        /// </summary>
        public static Func<string, string> DefaultTransformation { get; } =
            static name =>
            {
                ArgumentNullException.ThrowIfNull(name);
                return name.Replace("__", ConfigurationPath.KeyDelimiter);
            };

        /// <summary>
        /// A transformation that replaces triple underscores (<c>___</c>) with a dot (<c>.</c>)
        /// and double underscores (<c>__</c>) with the configuration key delimiter (<c>:</c>).
        /// </summary>
        /// <remarks>
        /// Runs of underscores are processed greedily from left to right: each <c>___</c> match
        /// is consumed before any remaining <c>__</c>. As a result, a run of four underscores
        /// (<c>____</c>) is interpreted as one triple followed by a single literal underscore
        /// and produces <c>._</c>, not <c>::</c>.
        /// </remarks>
        public static Func<string, string> ColonAndDotTransformation { get; } = TransformUnderscoresToColonAndDot;

        private static string TransformUnderscoresToColonAndDot(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            int first = name.IndexOf("__", StringComparison.Ordinal);

            if (first < 0)
            {
                return name;
            }

            return Core(name, first);

            // Local function to avoid the overhead of stackalloc when there are no double underscores in the string.
            static string Core(string name, int index)
            {
                var builder = new ValueStringBuilder(stackalloc char[256]);

                builder.Append(name.AsSpan(0, index));

                while (index < name.Length)
                {
                    if (name[index] == '_' && index + 1 < name.Length && name[index + 1] == '_')
                    {
                        if (index + 2 < name.Length && name[index + 2] == '_')
                        {
                            builder.Append('.');
                            index += 3;
                        }
                        else
                        {
                            builder.Append(':');
                            index += 2;
                        }
                    }
                    else
                    {
                        builder.Append(name[index++]);
                    }
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// A prefix used to filter environment variables.
        /// </summary>
        /// <remarks>
        /// The prefix is itself passed through <see cref="VariableNameTransformation"/> before
        /// matching, and the matching (and stripping) is performed against the transformed
        /// environment variable name.
        /// </remarks>
        public string? Prefix { get; set; }

        /// <summary>
        /// Gets or sets a function that transforms environment variable names. When set, this
        /// transformation replaces the default behavior of converting double
        /// underscores to the configuration key delimiter. When <see langword="null"/>,
        /// <see cref="DefaultTransformation"/> is used.
        /// </summary>
        /// <seealso cref="ColonAndDotTransformation"/>
        public Func<string, string>? VariableNameTransformation { get; set; }

        /// <summary>
        /// Builds the <see cref="EnvironmentVariablesConfigurationProvider"/> for this source.
        /// </summary>
        /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>A <see cref="EnvironmentVariablesConfigurationProvider"/>.</returns>
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new EnvironmentVariablesConfigurationProvider(Prefix, VariableNameTransformation);
        }
    }
}

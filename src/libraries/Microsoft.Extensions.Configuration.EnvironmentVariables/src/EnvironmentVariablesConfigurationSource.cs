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
            static name => name.Replace("__", ConfigurationPath.KeyDelimiter);

        /// <summary>
        /// A transformation that replaces triple underscores (<c>___</c>) with a dot (<c>.</c>)
        /// and double underscores (<c>__</c>) with the configuration key delimiter (<c>:</c>).
        /// </summary>
        public static Func<string, string> ColonAndDotTransformation { get; } = TransformUnderscoresToColonAndDot;

        private static string TransformUnderscoresToColonAndDot(string name)
        {
            int first = name.IndexOf("__", StringComparison.Ordinal);

            if (first < 0)
            {
                return name;
            }

            return Core(name, first);

            // Local function to avoid the overhead of stackalloc when there are no double underscores in the string.
            static string Core(string name, int first)
            {
#if NET
                var builder = new ValueStringBuilder(stackalloc char[256]);
#else
                var builder = new StringBuilder(name.Length);
#endif

                // For short strings, this seems to be faster than Append(name.Slice(0, first)).
                for (int j = 0; j < first; j++)
                {
                    builder.Append(name[j]);
                }

                int i = first;

                while (i < name.Length)
                {
                    if (name[i] == '_' && i + 1 < name.Length && name[i + 1] == '_')
                    {
                        if (i + 2 < name.Length && name[i + 2] == '_')
                        {
                            builder.Append('.');
                            i += 3;
                        }
                        else
                        {
                            builder.Append(':');
                            i += 2;
                        }
                    }
                    else
                    {
                        builder.Append(name[i++]);
                    }
                }

                return builder.ToString();
            }
        }

        /// <summary>
        /// A prefix used to filter environment variables.
        /// </summary>
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

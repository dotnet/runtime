// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Extension methods for <see cref="IConfigurationRoot"/>.
    /// </summary>
    public static class ConfigurationRootExtensions
    {
        /// <summary>
        /// Generates a human-readable view of the configuration showing where each value came from.
        /// </summary>
        /// <returns> The debug view. </returns>
        public static string GetDebugView(this IConfigurationRoot root)
        {
            return GetDebugView(root, processValue: null);
        }

        /// <summary>
        /// Generates a human-readable view of the configuration showing where each value came from.
        /// </summary>
        /// <param name="root">Configuration root</param>
        /// <param name="processValue">
        /// Function for processing the value e.g. hiding secrets
        /// Parameters:
        ///   ConfigurationDebugViewContext: Context of the current configuration item
        ///   returns: A string value is used to assign as the Value of the configuration section
        /// </param>
        /// <returns> The debug view. </returns>
        public static string GetDebugView(this IConfigurationRoot root, Func<ConfigurationDebugViewContext, string>? processValue)
        {
            void RecurseChildren(
                StringBuilder stringBuilder,
                IEnumerable<IConfigurationSection> children,
                string indent)
            {
                foreach (IConfigurationSection child in children)
                {
                    (string? Value, IConfigurationProvider? Provider) valueAndProvider = GetValueAndProvider(root, child.Path);

                    if (valueAndProvider.Provider != null)
                    {
                        string? value = processValue != null
                            ? processValue(new ConfigurationDebugViewContext(child.Path, child.Key, valueAndProvider.Value, valueAndProvider.Provider))
                            : valueAndProvider.Value;

                        stringBuilder
                            .Append(indent)
                            .Append(child.Key)
                            .Append('=')
                            .Append(value)
                            .Append(" (")
                            .Append(valueAndProvider.Provider)
                            .AppendLine(")");
                    }
                    else
                    {
                        stringBuilder
                            .Append(indent)
                            .Append(child.Key)
                            .AppendLine(":");
                    }

                    RecurseChildren(stringBuilder, child.GetChildren(), indent + "  ");
                }
            }

            var builder = new StringBuilder();

            RecurseChildren(builder, root.GetChildren(), "");

            return builder.ToString();
        }

        private static (string? Value, IConfigurationProvider? Provider) GetValueAndProvider(
            IConfigurationRoot root,
            string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out string? value))
                {
                    return (value, provider);
                }
            }

            return (null, null);
        }
    }
}

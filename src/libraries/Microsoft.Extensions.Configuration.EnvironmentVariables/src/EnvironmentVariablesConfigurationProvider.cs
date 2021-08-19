// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    /// <summary>
    /// An environment variable based <see cref="ConfigurationProvider"/>.
    /// </summary>
    public class EnvironmentVariablesConfigurationProvider : ConfigurationProvider
    {
        private const string MySqlServerPrefix = "MYSQLCONNSTR_";
        private const string SqlAzureServerPrefix = "SQLAZURECONNSTR_";
        private const string SqlServerPrefix = "SQLCONNSTR_";
        private const string CustomPrefix = "CUSTOMCONNSTR_";

        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesConfigurationProvider() =>
            _prefix = string.Empty;

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesConfigurationProvider(string prefix) =>
            _prefix = prefix ?? string.Empty;

        /// <summary>
        /// Loads the environment variables.
        /// </summary>
        public override void Load() =>
            Load(Environment.GetEnvironmentVariables());

        /// <summary>
        /// Generates a string representing this provider name and relevant details.
        /// </summary>
        /// <returns> The configuration name. </returns>
        public override string ToString()
            => $"{GetType().Name} Prefix: '{_prefix}'";

        internal void Load(IDictionary envVariables)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            IDictionaryEnumerator e = envVariables.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    DictionaryEntry entry = e.Entry;
                    string key = (string)entry.Key;
                    string provider = null;
                    string prefix;

                    if (key.StartsWith(MySqlServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = MySqlServerPrefix;
                        provider = "MySql.Data.MySqlClient";
                    }
                    else if (key.StartsWith(SqlAzureServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = SqlAzureServerPrefix;
                        provider = "System.Data.SqlClient";
                    }
                    else if (key.StartsWith(SqlServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = SqlServerPrefix;
                        provider = "System.Data.SqlClient";
                    }
                    else if (key.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        prefix = CustomPrefix;
                    }
                    else if (key.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        // This prevents the prefix from being normalized.
                        // We can also do a fast path branch, I guess? No point in reallocating if the prefix is empty.
                        key = NormalizeKey(key.Substring(_prefix.Length));
                        data[key] = entry.Value as string;

                        continue;
                    }
                    else
                    {
                        continue;
                    }

                    // Add the key-value pair for connection string, and optionally provider name
                    key = NormalizeKey(key.Substring(prefix.Length));
                    AddIfPrefixed(data, $"ConnectionStrings:{key}", (string)entry.Value);
                    if (provider != null)
                    {
                        AddIfPrefixed(data, $"ConnectionStrings:{key}_ProviderName", provider);
                    }
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }

            Data = data;
        }

        private void AddIfPrefixed(Dictionary<string, string> data, string key, string value)
        {
            if (key.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
            {
                key = key.Substring(_prefix.Length);
                data[key] = value;
            }
        }

        private static string NormalizeKey(string key) => key.Replace("__", ConfigurationPath.KeyDelimiter);
    }
}

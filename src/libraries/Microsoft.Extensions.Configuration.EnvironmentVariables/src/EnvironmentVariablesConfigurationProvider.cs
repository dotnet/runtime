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
        private const string CustomConnectionStringPrefix = "CUSTOMCONNSTR_";

        private readonly string _prefix;
        private readonly string _normalizedPrefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesConfigurationProvider()
        {
            _prefix = string.Empty;
            _normalizedPrefix = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesConfigurationProvider(string? prefix)
        {
            _prefix = prefix ?? string.Empty;
            _normalizedPrefix = Normalize(_prefix);
        }

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
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            IDictionaryEnumerator e = envVariables.GetEnumerator();
            try
            {
                while (e.MoveNext())
                {
                    string key = (string)e.Entry.Key;
                    string? value = (string?)e.Entry.Value;

                    if (key.StartsWith(MySqlServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, MySqlServerPrefix, "MySql.Data.MySqlClient", key, value);
                    }
                    else if (key.StartsWith(SqlAzureServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, SqlAzureServerPrefix, "System.Data.SqlClient", key, value);
                    }
                    else if (key.StartsWith(SqlServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, SqlServerPrefix, "System.Data.SqlClient", key, value);
                    }
                    else if (key.StartsWith(CustomConnectionStringPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, CustomConnectionStringPrefix, null, key, value);
                    }
                    else
                    {
                        AddIfNormalizedKeyMatchesPrefix(data, Normalize(key), value);
                    }
                }
            }
            finally
            {
                (e as IDisposable)?.Dispose();
            }

            Data = data;
        }

        private void HandleMatchedConnectionStringPrefix(Dictionary<string, string?> data, string connectionStringPrefix, string? provider, string fullKey, string? value)
        {
            string normalizedKeyWithoutConnectionStringPrefix = Normalize(fullKey.Substring(connectionStringPrefix.Length));

            // Add the key-value pair for connection string, and optionally provider name
            AddIfNormalizedKeyMatchesPrefix(data, $"ConnectionStrings:{normalizedKeyWithoutConnectionStringPrefix}", value);
            if (provider != null)
            {
                AddIfNormalizedKeyMatchesPrefix(data, $"ConnectionStrings:{normalizedKeyWithoutConnectionStringPrefix}_ProviderName", provider);
            }
        }

        private void AddIfNormalizedKeyMatchesPrefix(Dictionary<string, string?> data, string normalizedKey, string? value)
        {
            if (normalizedKey.StartsWith(_normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                data[normalizedKey.Substring(_normalizedPrefix.Length)] = value;
            }
        }

        private static string Normalize(string key) => key.Replace("__", ConfigurationPath.KeyDelimiter);
    }
}

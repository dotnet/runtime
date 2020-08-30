// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        private const string ConnStrKeyFormat = "ConnectionStrings:{0}";
        private const string ProviderKeyFormat = "ConnectionStrings:{0}_ProviderName";

        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public EnvironmentVariablesConfigurationProvider() : this(string.Empty)
        { }

        /// <summary>
        /// Initializes a new instance with the specified prefix.
        /// </summary>
        /// <param name="prefix">A prefix used to filter the environment variables.</param>
        public EnvironmentVariablesConfigurationProvider(string prefix)
        {
            _prefix = prefix ?? string.Empty;
        }

        /// <summary>
        /// Loads the environment variables.
        /// </summary>
        public override void Load()
        {
            Load(Environment.GetEnvironmentVariables());
        }

        internal void Load(IDictionary envVariables)
        {
            var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<DictionaryEntry> filteredEnvVariables = envVariables
                .Cast<DictionaryEntry>()
                .SelectMany(AzureEnvToAppEnv)
                .Where(entry => ((string)entry.Key).StartsWith(_prefix, StringComparison.OrdinalIgnoreCase));

            foreach (DictionaryEntry envVariable in filteredEnvVariables)
            {
                string key = ((string)envVariable.Key).Substring(_prefix.Length);
                data[key] = (string)envVariable.Value;
            }

            Data = data;
        }

        private static string NormalizeKey(string key)
        {
            return key.Replace("__", ConfigurationPath.KeyDelimiter);
        }

        private static IEnumerable<DictionaryEntry> AzureEnvToAppEnv(DictionaryEntry entry)
        {
            string key = (string)entry.Key;
            string prefix = string.Empty;
            string provider = string.Empty;

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
            else
            {
                entry.Key = NormalizeKey(key);
                yield return entry;
                yield break;
            }

            // Return the key-value pair for connection string
            yield return new DictionaryEntry(
                string.Format(ConnStrKeyFormat, NormalizeKey(key.Substring(prefix.Length))),
                entry.Value);

            if (!string.IsNullOrEmpty(provider))
            {
                // Return the key-value pair for provider name
                yield return new DictionaryEntry(
                    string.Format(ProviderKeyFormat, NormalizeKey(key.Substring(prefix.Length))),
                    provider);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.EnvironmentVariables
{
    /// <summary>
    /// Provides configuration key-value pairs that are obtained from environment variables.
    /// </summary>
    public class EnvironmentVariablesConfigurationProvider : ConfigurationProvider
    {
        // Connection string prefixes for various services. These prefixes are used to identify connection strings in environment variables.
        // az webapp config connection-string set: https://learn.microsoft.com/en-us/cli/azure/webapp/config/connection-string?view=azure-cli-latest#az-webapp-config-connection-string-set
        // Environment variables and app settings in Azure App Service: https://learn.microsoft.com/en-us/azure/app-service/reference-app-settings?tabs=kudu%2Cdotnet#variable-prefixes
        private const string MySqlServerPrefix = "MYSQLCONNSTR_";
        private const string SqlAzureServerPrefix = "SQLAZURECONNSTR_";
        private const string SqlServerPrefix = "SQLCONNSTR_";
        private const string CustomConnectionStringPrefix = "CUSTOMCONNSTR_";
        private const string PostgreSqlServerPrefix = "POSTGRESQLCONNSTR_";
        private const string ApiHubPrefix = "APIHUBCONNSTR_";
        private const string DocDbPrefix = "DOCDBCONNSTR_";
        private const string EventHubPrefix = "EVENTHUBCONNSTR_";
        private const string NotificationHubPrefix = "NOTIFICATIONHUBCONNSTR_";
        private const string RedisCachePrefix = "REDISCACHECONNSTR_";
        private const string ServiceBusPrefix = "SERVICEBUSCONNSTR_";

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
        /// <returns>The configuration name.</returns>
        public override string ToString()
        {
            string s = GetType().Name;
            if (!string.IsNullOrEmpty(_prefix))
            {
                s += $" Prefix: '{_prefix}'";
            }
            return s;
        }

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
                    else if (key.StartsWith(PostgreSqlServerPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, PostgreSqlServerPrefix, "Npgsql", key, value);
                    }
                    else if (key.StartsWith(ApiHubPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, ApiHubPrefix, null, key, value);
                    }
                    else if (key.StartsWith(DocDbPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, DocDbPrefix, null, key, value);
                    }
                    else if (key.StartsWith(EventHubPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, EventHubPrefix, null, key, value);
                    }
                    else if (key.StartsWith(NotificationHubPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, NotificationHubPrefix, null, key, value);
                    }
                    else if (key.StartsWith(RedisCachePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, RedisCachePrefix, null, key, value);
                    }
                    else if (key.StartsWith(ServiceBusPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        HandleMatchedConnectionStringPrefix(data, ServiceBusPrefix, null, key, value);
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

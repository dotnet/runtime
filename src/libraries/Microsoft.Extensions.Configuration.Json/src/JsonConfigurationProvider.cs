// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Microsoft.Extensions.Configuration.Json
{
    /// <summary>
    /// A JSON file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class JsonConfigurationProvider : FileConfigurationProvider
    {
        private static string _keyDelimiter = "`";

        /// <summary>
        /// Initializes a new instance with the specified source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public JsonConfigurationProvider(JsonConfigurationSource source) : base(source) { }

        /// <summary>
        /// Loads the JSON data from a stream.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        public override void Load(Stream stream)
        {
            try
            {
                Data = JsonConfigurationFileParser.Parse(stream);
            }
            catch (JsonException e)
            {
                throw new FormatException(SR.Error_JSONParseError, e);
            }
        }

        public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            var results = new List<string>();

            if (parentPath is null)
            {
                foreach (KeyValuePair<string, string?> kv in Data)
                {
                    results.Add(Segment(kv.Key, 0));
                }
            }
            else
            {
                // Debug.Assert(ConfigurationPath.KeyDelimiter == ":");

                foreach (KeyValuePair<string, string?> kv in Data)
                {
                    if (kv.Key.Length > parentPath.Length &&
                        kv.Key.StartsWith(parentPath, StringComparison.OrdinalIgnoreCase) &&
                        kv.Key[parentPath.Length] == '`')
                    {
                        results.Add(Segment(kv.Key, parentPath.Length + 1));
                    }
                }
            }

            results.AddRange(earlierKeys);

            results.Sort(Microsoft.Extensions.Configuration.ConfigurationKeyComparer.Instance.Compare);

            return results;
        }

        private static string Segment(string key, int prefixLength)
        {
            int indexOf = key.IndexOf(_keyDelimiter, prefixLength, StringComparison.OrdinalIgnoreCase);
            return indexOf < 0 ? key.Substring(prefixLength) : key.Substring(prefixLength, indexOf - prefixLength);
        }

    }
}

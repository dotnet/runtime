// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Memory
{
    /// <summary>
    /// In-memory implementation of <see cref="IConfigurationProvider"/>
    /// </summary>
    public class MemoryConfigurationProvider : ConfigurationProvider, IEnumerable<KeyValuePair<string, string>>
    {
        private readonly MemoryConfigurationSource _source;

        /// <summary>
        /// Initialize a new instance from the source.
        /// </summary>
        /// <param name="source">The source settings.</param>
        public MemoryConfigurationProvider(MemoryConfigurationSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _source = source;

            if (_source.InitialData != null)
            {
                foreach (var pair in _source.InitialData)
                {
                    Data.Add(pair.Key, pair.Value);
                }
            }
        }

        /// <summary>
        /// Add a new key and value pair.
        /// </summary>
        /// <param name="key">The configuration key.</param>
        /// <param name="value">The configuration value.</param>
        public void Add(string key, string value)
        {
            Data.Add(key, value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return Data.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

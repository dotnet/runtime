// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace GCPerfTestFramework.Metrics.Builders
{
    internal static class DictionaryExtensions
    {
        public static V GetOrCreate<K, V>(this IDictionary<K, V> dict, K key) where V : new()
        {
            V value;
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }

            value = new V();
            dict[key] = value;
            return value;
        }
    }
}

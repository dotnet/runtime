// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

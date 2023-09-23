// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    /// <summary>
    /// Defines a key/value pair for displaying an item of a dictionary by a debugger.
    /// </summary>
    [DebuggerDisplay("{Value}", Name = "[{Key}]")]
    internal readonly struct DebugViewDictionaryItem<K, V>
    {
        public DebugViewDictionaryItem(K key, V value)
        {
            Key = key;
            Value = value;
        }

        public DebugViewDictionaryItem(KeyValuePair<K, V> keyValue)
        {
            Key = keyValue.Key;
            Value = keyValue.Value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public K Key { get; init; }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public V Value { get; init; }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class OrderedDictionaryDebugView<TKey, TValue> where TKey : notnull
    {
        private readonly OrderedDictionary<TKey, TValue> _dictionary;

        public OrderedDictionaryDebugView(OrderedDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            _dictionary = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewOrderedDictionaryItem<TKey, TValue>[] Items
        {
            get
            {
                var items = new DebugViewOrderedDictionaryItem<TKey, TValue>[_dictionary.Count];
                int index = 0;
                foreach (KeyValuePair<TKey, TValue> kvp in _dictionary)
                {
                    items[index] = new DebugViewOrderedDictionaryItem<TKey, TValue>(index, kvp.Key, kvp.Value);
                    index++;
                }
                return items;
            }
        }
    }

    /// <summary>
    /// Defines an index/key/value triple for displaying an item of an ordered dictionary by a debugger.
    /// </summary>
    [DebuggerDisplay("{Value}", Name = "[{Index}/{Key}]")]
    internal readonly struct DebugViewOrderedDictionaryItem<TKey, TValue>
    {
        public DebugViewOrderedDictionaryItem(int index, TKey key, TValue value)
        {
            Index = index;
            Key = key;
            Value = value;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public int Index { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public TKey Key { get; }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public TValue Value { get; }
    }
}

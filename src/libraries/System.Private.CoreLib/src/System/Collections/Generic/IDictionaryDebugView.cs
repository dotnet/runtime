// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Collections.Generic
{
    internal sealed class IDictionaryDebugView<TKey, TValue> where TKey : notnull
    {
        private readonly IDictionary<TKey, TValue> _dict;

        public IDictionaryDebugView(IDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            _dict = dictionary;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DebugViewDictionaryItem<TKey, TValue>[] Items
        {
            get
            {
                var keyValuePairs = new KeyValuePair<TKey, TValue>[_dict.Count];
                _dict.CopyTo(keyValuePairs, 0);
                var items = new DebugViewDictionaryItem<TKey, TValue>[keyValuePairs.Length];
                for (int i = 0; i < items.Length; i++)
                {
                    items[i] = new DebugViewDictionaryItem<TKey, TValue>(keyValuePairs[i]);
                }
                return items;
            }
        }
    }

    internal sealed class DictionaryKeyCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TKey> _collection;

        public DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TKey[] Items
        {
            get
            {
                TKey[] items = new TKey[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }

    internal sealed class DictionaryValueCollectionDebugView<TKey, TValue>
    {
        private readonly ICollection<TValue> _collection;

        public DictionaryValueCollectionDebugView(ICollection<TValue> collection)
        {
            ArgumentNullException.ThrowIfNull(collection);

            _collection = collection;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public TValue[] Items
        {
            get
            {
                TValue[] items = new TValue[_collection.Count];
                _collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}

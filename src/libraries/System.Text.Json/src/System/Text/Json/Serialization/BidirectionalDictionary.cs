// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed class BidirectionalDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _keyValueDictionary;
        private Dictionary<TValue, TKey> _valueKeyDictionary;

        public BidirectionalDictionary()
        {
            _keyValueDictionary = new Dictionary<TKey, TValue>();
            _valueKeyDictionary = new Dictionary<TValue, TKey>(ReferenceEqualsEqualityComparer<TValue>.Comparer);
        }

        public void Add(TKey key, TValue value)
        {
            _keyValueDictionary[key] = value;
            _valueKeyDictionary[value] = key;
        }

        public bool TryGetByKey(TKey key, out TValue value)
        {
            return _keyValueDictionary.TryGetValue(key, out value);
        }

        public bool TryGetByValue(TValue value, out TKey key)
        {
            return _valueKeyDictionary.TryGetValue(value, out key);
        }
    }
}

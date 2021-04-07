// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.IO.Packaging
{
    /// <summary>
    /// A collection that ensures uniqueness among a list of elements while maintaining the order in which the elements were added.
    /// This is similar to <see cref="OrderedDictionary{TKey, TValue}"/>, but the items will not be sorted by a comparer but rather retain the
    /// order in which they were added while still retaining good lookup, insertion, and removal.
    /// </summary>
    internal sealed class OrderedDictionary<TKey, TValue> : IEnumerable<TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, LinkedListNode<TValue>> _dictionary;
        private readonly LinkedList<TValue> _order;

        public OrderedDictionary(int initialCapacity)
        {
            _dictionary = new Dictionary<TKey, LinkedListNode<TValue>>(initialCapacity);
            _order = new LinkedList<TValue>();
        }

        public bool Contains(TKey key) => _dictionary.ContainsKey(key);

        public bool Add(TKey key, TValue value)
        {
            if (_dictionary.ContainsKey(key))
            {
                return false;
            }

            _dictionary.Add(key, _order.AddLast(value));
            return true;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _order.Clear();
        }

        public bool Remove(TKey key)
        {
            if (_dictionary.TryGetValue(key, out LinkedListNode<TValue>? value))
            {
                _order.Remove(value);
                _dictionary.Remove(key);
                return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (_dictionary.TryGetValue(key, out var node))
            {
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        public int Count => _dictionary.Count;

        public IEnumerator<TValue> GetEnumerator() => _order.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

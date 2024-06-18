// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed partial class OrderedDictionary<TKey, TValue>
    {
        private sealed class ValueCollection : IList<TValue>
        {
            private readonly OrderedDictionary<TKey, TValue> _parent;

            public ValueCollection(OrderedDictionary<TKey, TValue> parent)
            {
                _parent = parent;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            public TValue this[int index]
            {
                get => _parent.GetAt(index).Value;
                set => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    yield return item.Value;
                }
            }

            public void Add(TValue value) => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public void Clear() => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public bool Contains(TValue value)
            {
                EqualityComparer<TValue> comparer = _parent._valueComparer;
                foreach (KeyValuePair<TKey, TValue> item in _parent._propertyList)
                {
                    if (comparer.Equals(item.Value, value))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void CopyTo(TValue[] destination, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    if (index >= destination.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(destination));
                    }

                    destination[index++] = item.Value;
                }
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    yield return item.Value;
                }
            }

            bool ICollection<TValue>.Remove(TValue value) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public int IndexOf(TValue item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void Insert(int index, TValue value) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void RemoveAt(int index) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
        }
    }
}

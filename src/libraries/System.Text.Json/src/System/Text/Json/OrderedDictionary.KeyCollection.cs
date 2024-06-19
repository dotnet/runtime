// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed partial class OrderedDictionary<TKey, TValue>
    {
        private sealed class KeyCollection : IList<TKey>
        {
            private readonly OrderedDictionary<TKey, TValue> _parent;

            public KeyCollection(OrderedDictionary<TKey, TValue> parent)
            {
                _parent = parent;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            public TKey this[int index]
            {
                get => _parent.GetAt(index).Key;
                set => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    yield return item.Key;
                }
            }

            public void Add(TKey propertyName) => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public void Clear() => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public bool Contains(TKey propertyName) => _parent.ContainsKey(propertyName);

            public void CopyTo(TKey[] propertyNameArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    if (index >= propertyNameArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(propertyNameArray));
                    }

                    propertyNameArray[index++] = item.Key;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> item in _parent)
                {
                    yield return item.Key;
                }
            }

            bool ICollection<TKey>.Remove(TKey propertyName) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public int IndexOf(TKey item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void Insert(int index, TKey item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void RemoveAt(int index) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
        }
    }
}

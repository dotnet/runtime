// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed partial class JsonPropertyDictionary<T>
    {
        private sealed class ValueCollection : IList<T>
        {
            private readonly JsonPropertyDictionary<T> _parent;

            public ValueCollection(JsonPropertyDictionary<T> jsonObject)
            {
                _parent = jsonObject;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            public T this[int index]
            {
                get => _parent.GetAt(index).Value;
                set => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Value;
                }
            }

            public void Add(T value) => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public void Clear() => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public bool Contains(T value)
            {
                EqualityComparer<T> comparer = _parent._valueComparer;
                foreach (KeyValuePair<string, T> item in _parent._propertyList)
                {
                    if (comparer.Equals(item.Value, value))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void CopyTo(T[] destination, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, T> item in _parent)
                {
                    if (index >= destination.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(destination));
                    }

                    destination[index++] = item.Value;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Value;
                }
            }

            bool ICollection<T>.Remove(T value) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public int IndexOf(T item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void Insert(int index, T value) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void RemoveAt(int index) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
        }
    }
}

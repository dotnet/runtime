// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed partial class JsonPropertyDictionary<T>
    {
        private ValueCollection? _valueCollection;

        public IList<T> GetValueCollection()
        {
            return _valueCollection ??= new ValueCollection(this);
        }

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
                get => _parent.List[index].Value;
                set => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Value;
                }
            }

            public void Add(T jsonNode) => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public void Clear() => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public bool Contains(T jsonNode) => _parent.ContainsValue(jsonNode);

            public void CopyTo(T[] nodeArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, T> item in _parent)
                {
                    if (index >= nodeArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(nodeArray));
                    }

                    nodeArray[index++] = item.Value;
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Value;
                }
            }

            bool ICollection<T>.Remove(T node) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public int IndexOf(T item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void Insert(int index, T item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void RemoveAt(int index) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
        }
    }
}

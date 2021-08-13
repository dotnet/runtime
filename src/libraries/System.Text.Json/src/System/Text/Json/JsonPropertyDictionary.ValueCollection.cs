// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal partial class JsonPropertyDictionary<T>
    {
        private ValueCollection? _valueCollection;

        public ICollection<T?> GetValueCollection()
        {
            return _valueCollection ??= new ValueCollection(this);
        }

        private sealed class ValueCollection : ICollection<T?>
        {
            private readonly JsonPropertyDictionary<T> _parent;

            public ValueCollection(JsonPropertyDictionary<T> jsonObject)
            {
                _parent = jsonObject;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    yield return item.Value;
                }
            }

            public void Add(T? jsonNode) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();

            public void Clear() => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();

            public bool Contains(T? jsonNode) => _parent.ContainsValue(jsonNode);

            public void CopyTo(T?[] nodeArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    if (index >= nodeArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_NodeArrayTooSmall(nameof(nodeArray));
                    }

                    nodeArray[index++] = item.Value;
                }
            }

            public IEnumerator<T?> GetEnumerator()
            {
                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    yield return item.Value;
                }
            }

            bool ICollection<T?>.Remove(T? node) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
        }
    }
}

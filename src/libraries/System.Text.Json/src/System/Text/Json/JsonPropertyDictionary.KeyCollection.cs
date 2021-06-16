// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal partial class JsonPropertyDictionary<T>
    {
        private KeyCollection? _keyCollection;

        public ICollection<string> GetKeyCollection()
        {
            return _keyCollection ??= new KeyCollection(this);
        }

        private sealed class KeyCollection : ICollection<string>
        {
            private readonly JsonPropertyDictionary<T> _parent;

            public KeyCollection(JsonPropertyDictionary<T> jsonObject)
            {
                _parent = jsonObject;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    yield return item.Key;
                }
            }

            public void Add(string propertyName) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();

            public void Clear() => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();

            public bool Contains(string propertyName) => _parent.ContainsProperty(propertyName);

            public void CopyTo(string[] propertyNameArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    if (index >= propertyNameArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_NodeArrayTooSmall(nameof(propertyNameArray));
                    }

                    propertyNameArray[index++] = item.Key;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (KeyValuePair<string, T?> item in _parent)
                {
                    yield return item.Key;
                }
            }

            bool ICollection<string>.Remove(string propertyName) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
        }
    }
}

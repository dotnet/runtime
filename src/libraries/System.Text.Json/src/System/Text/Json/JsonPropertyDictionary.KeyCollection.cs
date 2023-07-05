// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json
{
    internal sealed partial class JsonPropertyDictionary<T>
    {
        private KeyCollection? _keyCollection;

        public IList<string> GetKeyCollection()
        {
            return _keyCollection ??= new KeyCollection(this);
        }

        private sealed class KeyCollection : IList<string>
        {
            private readonly JsonPropertyDictionary<T> _parent;

            public KeyCollection(JsonPropertyDictionary<T> jsonObject)
            {
                _parent = jsonObject;
            }

            public int Count => _parent.Count;

            public bool IsReadOnly => true;

            public string this[int index]
            {
                get => _parent.List[index].Key;
                set => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Key;
                }
            }

            public void Add(string propertyName) => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public void Clear() => ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();

            public bool Contains(string propertyName) => _parent.ContainsProperty(propertyName);

            public void CopyTo(string[] propertyNameArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, T> item in _parent)
                {
                    if (index >= propertyNameArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(propertyNameArray));
                    }

                    propertyNameArray[index++] = item.Key;
                }
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (KeyValuePair<string, T> item in _parent)
                {
                    yield return item.Key;
                }
            }

            bool ICollection<string>.Remove(string propertyName) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public int IndexOf(string item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void Insert(int index, string item) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
            public void RemoveAt(int index) => throw ThrowHelper.GetNotSupportedException_CollectionIsReadOnly();
        }
    }
}

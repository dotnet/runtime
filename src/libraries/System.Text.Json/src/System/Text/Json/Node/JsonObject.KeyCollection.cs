// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Node
{
    public partial class JsonObject
    {
        private KeyCollection? _keyCollection;

        private KeyCollection GetKeyCollection(JsonObject jsonObject)
        {
            CreateList();
            return _keyCollection ??= new KeyCollection(jsonObject);
        }

        private sealed class KeyCollection : ICollection<string>
        {
            private readonly JsonObject _jObject;

            public KeyCollection(JsonObject jsonObject)
            {
                _jObject = jsonObject;
            }

            public int Count => _jObject.Count;

            public bool IsReadOnly => true;

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
                {
                    yield return item.Key;
                }
            }

            public void Add(string propertyName) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();


            public void Clear() => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();


            public bool Contains(string propertyName) => _jObject.ContainsNode(propertyName);


            public void CopyTo(string[] propertyNameArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
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
                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
                {
                    yield return item.Key;
                }
            }

            bool ICollection<string>.Remove(string propertyName) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
        }
    }
}

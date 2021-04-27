// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Node
{
    public partial class JsonObject
    {
        private ValueCollection? _valueCollection;

        private ValueCollection GetValueCollection(JsonObject jsonObject)
        {
            CreateList();
            return _valueCollection ??= new ValueCollection(jsonObject);
        }

        private sealed class ValueCollection : ICollection<JsonNode?>
        {
            private readonly JsonObject _jObject;

            public ValueCollection(JsonObject jsonObject)
            {
                _jObject = jsonObject;
            }

            public int Count => _jObject.Count;

            public bool IsReadOnly => true;

            IEnumerator IEnumerable.GetEnumerator()
            {
                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
                {
                    yield return item.Value;
                }
            }

            public void Add(JsonNode? jsonNode) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();


            public void Clear() => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();


            public bool Contains(JsonNode? jsonNode) => _jObject.ContainsNode(jsonNode);

            public void CopyTo(JsonNode?[] nodeArray, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
                }

                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
                {
                    if (index >= nodeArray.Length)
                    {
                        ThrowHelper.ThrowArgumentException_NodeArrayTooSmall(nameof(nodeArray));
                    }

                    nodeArray[index++] = item.Value;
                }
            }

            public IEnumerator<JsonNode?> GetEnumerator()
            {
                foreach (KeyValuePair<string, JsonNode?> item in _jObject)
                {
                    yield return item.Value;
                }
            }

            bool ICollection<JsonNode?>.Remove(JsonNode? node) => throw ThrowHelper.NotSupportedException_NodeCollectionIsReadOnly();
        }
    }
}

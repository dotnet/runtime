// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace System.Text.Json
{
    internal sealed class JsonPropertyInfoDictionaryValueList : IList<JsonPropertyInfo>
    {
        private readonly JsonPropertyDictionary<JsonPropertyInfo> _parent;
        private List<JsonPropertyInfo>? _items;
        private JsonTypeInfo _parentTypeInfo;

        [MemberNotNullWhen(false, nameof(_items))]
        public bool IsReadOnly => _items == null;
        public int Count => IsReadOnly ? _parent.Count : _items.Count;

        public JsonPropertyInfo this[int index]
        {
            get => IsReadOnly ? _parent.List[index].Value! : _items[index];
            set
            {
                if (IsReadOnly)
                    ThrowCollectionIsReadOnly();

                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                value.EnsureChildOf(_parentTypeInfo);
                _items[index] = value;
            }
        }

        public JsonPropertyInfoDictionaryValueList(JsonPropertyDictionary<JsonPropertyInfo> parent, JsonTypeInfo parentTypeInfo, bool isReadOnly)
        {
            _parent = parent;
            _parentTypeInfo = parentTypeInfo;

            Debug.Assert(!_parent.IsReadOnly, $"{nameof(JsonPropertyDictionary<JsonPropertyInfo>)} is read-only but editable value list is created");

            if (!isReadOnly)
            {
                // We cannot ensure keys won't change while editing therefore we operate on the internal copy.
                // Once we're done editing FinishEditingAndMakeReadOnly should be called then we switch to operating directly on _parent
                _items = new List<JsonPropertyInfo>(_parent.Count);
                foreach (var kv in _parent.List)
                {
                    Debug.Assert(kv.Value != null, $"{nameof(JsonPropertyDictionary<JsonPropertyInfo>)} contains null value");

                    // we need to do this so that property cannot be copied over elsewhere
                    // since source gen properties do not have parents by default
                    kv.Value.EnsureChildOf(parentTypeInfo);
                    _items.Add(kv.Value);
                }
            }
        }

        public void FinishEditingAndMakeReadOnly(Type parentType)
        {
            Debug.Assert(!IsReadOnly, $"{nameof(FinishEditingAndMakeReadOnly)} called on read-only ValueList");

            // We do not know if any of the keys needs to be updated therefore we need to re-create cache
            _parent.Clear();

            foreach (var item in _items)
            {
                string key = item.Name;
                if (!_parent.TryAddValue(key, item))
                {
                    ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(parentType, key);
                }
            }

            // clearing those so that we don't keep GC from freeing and also mark it as read-only
            _items = null;
        }

        public void Add(JsonPropertyInfo item)
        {
            if (IsReadOnly)
                ThrowCollectionIsReadOnly();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.EnsureChildOf(_parentTypeInfo);
            _items.Add(item);
        }

        public void Clear()
        {
            if (IsReadOnly)
                ThrowCollectionIsReadOnly();

            _items.Clear();
        }

        public bool Contains(JsonPropertyInfo item) => IsReadOnly ? _parent.ContainsValue(item) : _items.Contains(item);

        public void CopyTo(JsonPropertyInfo[] array, int index)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
            }

            if (IsReadOnly)
            {
                foreach (KeyValuePair<string, JsonPropertyInfo?> item in _parent)
                {
                    if (index >= array.Length)
                    {
                        ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(array));
                    }

                    array[index++] = item.Value!;
                }
            }
            else
            {
                _items.CopyTo(array, index);
            }
        }

        public int IndexOf(JsonPropertyInfo item)
        {
            if (IsReadOnly)
            {
                int index = 0;
                foreach (var kv in _parent.List)
                {
                    if (kv.Value == item)
                    {
                        return index;
                    }

                    index++;
                }

                return -1;
            }
            else
            {
                return _items.IndexOf(item);
            }
        }

        public void Insert(int index, JsonPropertyInfo item)
        {
            if (IsReadOnly)
                ThrowCollectionIsReadOnly();

            if (item == null)
                throw new ArgumentNullException(nameof(item));

            item.EnsureChildOf(_parentTypeInfo);
            _items.Insert(index, item);
        }

        public bool Remove(JsonPropertyInfo item)
        {
            if (IsReadOnly)
                ThrowCollectionIsReadOnly();

            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            if (IsReadOnly)
                ThrowCollectionIsReadOnly();

            _items.RemoveAt(index);
        }

        public IEnumerator<JsonPropertyInfo> GetEnumerator()
        {
            if (IsReadOnly)
            {
                foreach (KeyValuePair<string, JsonPropertyInfo?> item in _parent)
                {
                    yield return item.Value!;
                }
            }
            else
            {
                foreach (JsonPropertyInfo item in _items)
                {
                    yield return item;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [DoesNotReturn]
        private static void ThrowCollectionIsReadOnly()
        {
            ThrowHelper.ThrowInvalidOperationException_CollectionIsReadOnly();
        }
    }
}

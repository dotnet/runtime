// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    internal sealed partial class JsonPropertyDictionary<T>
    {
#if DEBUG
        // CreateEditableValueList should be called at most once by JsonTypeInfo
        private ValueList? _editableValueList;
#endif

        public ValueList CreateValueList(Func<T, string>? getKey)
        {
            ValueList ret = new ValueList(this, getKey);
#if DEBUG
            Debug.Assert(_editableValueList == null, "More than one ValueList created");
            return _editableValueList ??= ret;
#else
            return ret;
#endif
        }

        internal sealed class ValueList : IList<T>
        {
            private readonly JsonPropertyDictionary<T> _parent;
            private Func<T, string>? _getKey;
            private List<T>? _items;

            [MemberNotNullWhen(false, nameof(_getKey))]
            [MemberNotNullWhen(false, nameof(_items))]
            public bool IsReadOnly => _getKey == null;
            public int Count => IsReadOnly ? _parent.Count : _items.Count;

            public T this[int index]
            {
                get => IsReadOnly ? _parent.List[index].Value! : _items[index];
                set
                {
                    if (IsReadOnly)
                        ThrowCollectionIsReadOnly();

                    _items[index] = value;
                }
            }

            public ValueList(JsonPropertyDictionary<T> jsonObject, Func<T, string>? getKey)
            {
                // _getKey == null is equivalent to being read-only
                _parent = jsonObject;
                _getKey = getKey;

                Debug.Assert(!_parent.IsReadOnly, $"{nameof(JsonPropertyDictionary<T>)} is read-only but editable value list is created");

                if (!IsReadOnly)
                {
                    // We cannot ensure keys won't change while editing therefore we operate on the internal copy.
                    // Once we're done editing FinishEditingAndMakeReadOnly should be called then we switch to operating directly on _parent
                    _items = new List<T>(_parent.Count);
                    foreach (var kv in _parent.List)
                    {
                        Debug.Assert(kv.Value != null, $"{nameof(JsonPropertyDictionary<T>)} contains null value");
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
                    string key = _getKey(item);
                    if (!_parent.TryAddValue(key, item))
                    {
                        ThrowHelper.ThrowInvalidOperationException_SerializerPropertyNameConflict(parentType, key);
                    }
                }

                // clearing those so that we don't keep GC from freeing
                _items = null;
                _getKey = null;
            }

            public void Add(T item)
            {
                if (IsReadOnly)
                    ThrowCollectionIsReadOnly();

                _items.Add(item);
            }

            public void Clear()
            {
                if (IsReadOnly)
                    ThrowCollectionIsReadOnly();

                _items.Clear();
            }

            public bool Contains(T item) => IsReadOnly ? _parent.ContainsValue(item) : _items.Contains(item);

            public void CopyTo(T[] array, int index)
            {
                if (index < 0)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
                }

                if (IsReadOnly)
                {
                    foreach (KeyValuePair<string, T?> item in _parent)
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

            public int IndexOf(T item)
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

            public void Insert(int index, T item)
            {
                if (IsReadOnly)
                    ThrowCollectionIsReadOnly();

                _items.Insert(index, item);
            }

            public bool Remove(T item)
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

            public IEnumerator<T> GetEnumerator()
            {
                if (IsReadOnly)
                {
                    foreach (KeyValuePair<string, T?> item in _parent)
                    {
                        yield return item.Value!;
                    }
                }
                else
                {
                    foreach (T item in _items)
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
}

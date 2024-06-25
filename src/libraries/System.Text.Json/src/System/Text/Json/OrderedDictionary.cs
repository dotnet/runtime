// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    /// <summary>
    /// Polyfill for System.Collections.Generic.OrderedDictionary added in .NET 9.
    /// </summary>
    internal sealed partial class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<TKey, TValue>? _propertyDictionary;
        private readonly List<KeyValuePair<TKey, TValue>> _propertyList;
        private readonly IEqualityComparer<TKey> _keyComparer;
        private readonly EqualityComparer<TValue> _valueComparer = EqualityComparer<TValue>.Default;

        public OrderedDictionary(int capacity, IEqualityComparer<TKey>? keyComparer = null)
        {
            _keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
            _propertyList = new(capacity);
            if (capacity > ListToDictionaryThreshold)
            {
                _propertyDictionary = new(capacity, _keyComparer);
            }
        }

        public void Add(TKey key, TValue value)
        {
            if (!TryAdd(key, value))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(nameof(key), key);
            }
        }

        public void Clear()
        {
            _propertyList.Clear();
            _propertyDictionary?.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _propertyDictionary is { } dict
                ? dict.ContainsKey(key)
                : IndexOf(key) >= 0;
        }

        public int Count => _propertyList.Count;
        public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator() => _propertyList.GetEnumerator();

        public ICollection<TKey> Keys => _keys ??= new(this);
        private KeyCollection? _keys;

        public ICollection<TValue> Values => _values ??= new(this);
        private ValueCollection? _values;

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (key is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(key));
            }

            if (_propertyDictionary is { } dict)
            {
                return dict.TryGetValue(key, out value);
            }
            else
            {
                IEqualityComparer<TKey> comparer = _keyComparer;
                foreach (KeyValuePair<TKey, TValue> item in _propertyList)
                {
                    if (comparer.Equals(key, item.Key))
                    {
                        value = item.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                if (!TryGetValue(key, out TValue? value))
                {
                    ThrowHelper.ThrowKeyNotFoundException();
                }

                return value;
            }

            set
            {
                if (_propertyDictionary is { } dict)
                {
                    dict[key] = value;
                }

                KeyValuePair<TKey, TValue> item = new(key, value);
                int i = IndexOf(key);
                if (i < 0)
                {
                    _propertyList.Add(item);
                }
                else
                {
                    _propertyList[i] = item;
                }
            }
        }

        public bool TryAdd(TKey key, TValue value)
        {
            if (key is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(key));
            }

            CreateDictionaryIfThresholdMet();

            if (_propertyDictionary is { } dict)
            {
                if (!dict.TryAdd(key, value))
                {
                    return false;
                }
            }
            else if (IndexOf(key) >= 0)
            {
                return false;
            }

            _propertyList.Add(new(key, value));
            return true;
        }

        private void CreateDictionaryIfThresholdMet()
        {
            if (_propertyDictionary == null && _propertyList.Count > ListToDictionaryThreshold)
            {
                _propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _keyComparer);
            }
        }

        public int IndexOf(TKey key)
        {
            if (key is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(key));
            }

            List<KeyValuePair<TKey, TValue>> propertyList = _propertyList;
            IEqualityComparer<TKey> keyComparer = _keyComparer;

            for (int i = 0; i < propertyList.Count; i++)
            {
                if (keyComparer.Equals(key, propertyList[i].Key))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue existing)
        {
            if (_propertyDictionary != null)
            {
                if (!_propertyDictionary.TryGetValue(key, out existing))
                {
                    return false;
                }

                bool success = _propertyDictionary.Remove(key);
                Debug.Assert(success);
            }

            for (int i = 0; i < _propertyList.Count; i++)
            {
                KeyValuePair<TKey, TValue> current = _propertyList[i];

                if (_keyComparer.Equals(current.Key, key))
                {
                    _propertyList.RemoveAt(i);
                    existing = current.Value;
                    return true;
                }
            }

            existing = default;
            return false;
        }

        public KeyValuePair<TKey, TValue> GetAt(int index) => _propertyList[index];

        public void SetAt(int index, TKey key, TValue value)
        {
            TKey existingKey = _propertyList[index].Key;
            if (!_keyComparer.Equals(existingKey, key))
            {
                if (ContainsKey(key))
                {
                    // The key already exists in a different position, throw an exception.
                    ThrowHelper.ThrowArgumentException_DuplicateKey(nameof(key), key);
                }

                _propertyDictionary?.Remove(existingKey);
            }

            if (_propertyDictionary != null)
            {
                _propertyDictionary[key] = value;
            }

            _propertyList[index] = new(key, value);
        }

        public void SetAt(int index, TValue value)
        {
            TKey key = _propertyList[index].Key;
            if (_propertyDictionary != null)
            {
                _propertyDictionary[key] = value;
            }

            _propertyList[index] = new(key, value);
        }

        public void Insert(int index, TKey key, TValue value)
        {
            if (key is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(key));
            }

            if (ContainsKey(key))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(nameof(key), key);
            }

            _propertyList.Insert(index, new(key, value));
            _propertyDictionary?.Add(key, value);
        }

        public void RemoveAt(int index)
        {
            KeyValuePair<TKey, TValue> item = _propertyList[index];
            _propertyList.RemoveAt(index);
            _propertyDictionary?.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item) => TryGetValue(item.Key, out TValue? existingValue) && _valueComparer.Equals(item.Value, existingValue);
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return TryGetValue(item.Key, out TValue? existingValue) && _valueComparer.Equals(existingValue, item.Value)
                ? Remove(item.Key, out _)
                : false;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(arrayIndex));
            }

            foreach (KeyValuePair<TKey, TValue> item in _propertyList)
            {
                if (arrayIndex >= array.Length)
                {
                    ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(array));
                }

                array[arrayIndex++] = item;
            }
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key) => Remove(key, out _);
        void IList<KeyValuePair<TKey, TValue>>.Insert(int index, KeyValuePair<TKey, TValue> item) => Insert(index, item.Key, item.Value);
        int IList<KeyValuePair<TKey, TValue>>.IndexOf(KeyValuePair<TKey, TValue> item)
        {
            List<KeyValuePair<TKey, TValue>> propertyList = _propertyList;
            IEqualityComparer<TKey> keyComparer = _keyComparer;
            EqualityComparer<TValue> valueComparer = _valueComparer;

            for (int i = 0; i < propertyList.Count; i++)
            {
                KeyValuePair<TKey, TValue> entry = propertyList[i];
                if (keyComparer.Equals(entry.Key, item.Key) && valueComparer.Equals(item.Value, entry.Value))
                {
                    return i;
                }
            }

            return -1;
        }

        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
        {
            get => GetAt(index);
            set => SetAt(index, value.Key, value.Value);
        }
    }
}

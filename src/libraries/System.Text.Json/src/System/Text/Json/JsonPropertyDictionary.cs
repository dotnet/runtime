// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    /// <summary>
    /// Defines an ordered dictionary for storing JSON property metadata.
    /// </summary>
    internal sealed partial class JsonPropertyDictionary<T> : IDictionary<string, T>, IList<KeyValuePair<string, T>>
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, T>? _propertyDictionary;
        private readonly List<KeyValuePair<string, T>> _propertyList;
        private readonly StringComparer _stringComparer;
        private readonly EqualityComparer<T> _valueComparer = EqualityComparer<T>.Default;

        public JsonPropertyDictionary(StringComparer stringComparer, int capacity = 0)
        {
            _stringComparer = stringComparer;
            _propertyList = new List<KeyValuePair<string, T>>(capacity);
            if (capacity > ListToDictionaryThreshold)
            {
                _propertyDictionary = new(capacity, _stringComparer);
            }
        }

        public void Add(string propertyName, T value)
        {
            if (!TryAdd(propertyName, value))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(nameof(propertyName), propertyName);
            }
        }

        public void Clear()
        {
            _propertyList.Clear();
            _propertyDictionary?.Clear();
        }

        public bool ContainsKey(string propertyName)
        {
            return _propertyDictionary is { } dict
                ? dict.ContainsKey(propertyName)
                : IndexOf(propertyName) >= 0;
        }

        public int Count => _propertyList.Count;
        public List<KeyValuePair<string, T>>.Enumerator GetEnumerator() => _propertyList.GetEnumerator();

        public ICollection<string> Keys => _keys ??= new(this);
        private KeyCollection? _keys;

        public ICollection<T> Values => _values ??= new(this);
        private ValueCollection? _values;

        public bool TryGetValue(string propertyName, [MaybeNullWhen(false)] out T value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            if (_propertyDictionary is { } dict)
            {
                return dict.TryGetValue(propertyName, out value);
            }
            else
            {
                StringComparer comparer = _stringComparer;
                foreach (KeyValuePair<string, T> item in _propertyList)
                {
                    if (comparer.Equals(propertyName, item.Key))
                    {
                        value = item.Value;
                        return true;
                    }
                }
            }

            value = default;
            return false;
        }

        public T this[string propertyName]
        {
            get
            {
                if (!TryGetValue(propertyName, out T? value))
                {
                    ThrowHelper.ThrowKeyNotFoundException();
                }

                return value;
            }

            set
            {
                if (_propertyDictionary is { } dict)
                {
                    dict[propertyName] = value;
                }

                KeyValuePair<string, T> item = new(propertyName, value);
                int i = IndexOf(propertyName);
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

        public bool TryAdd(string propertyName, T value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            CreateDictionaryIfThresholdMet();

            if (_propertyDictionary is { } dict)
            {
                if (!dict.TryAdd(propertyName, value))
                {
                    return false;
                }
            }
            else if (IndexOf(propertyName) >= 0)
            {
                return false;
            }

            _propertyList.Add(new(propertyName, value));
            return true;
        }

        private void CreateDictionaryIfThresholdMet()
        {
            if (_propertyDictionary == null && _propertyList.Count > ListToDictionaryThreshold)
            {
                _propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _stringComparer);
            }
        }

        public int IndexOf(string propertyName)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            List<KeyValuePair<string, T>> propertyList = _propertyList;
            StringComparer keyComparer = _stringComparer;

            for (int i = 0; i < propertyList.Count; i++)
            {
                if (keyComparer.Equals(propertyName, propertyList[i].Key))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool Remove(string propertyName, [MaybeNullWhen(false)] out T existing)
        {
            if (_propertyDictionary != null)
            {
                if (!_propertyDictionary.TryGetValue(propertyName, out existing))
                {
                    return false;
                }

                bool success = _propertyDictionary.Remove(propertyName);
                Debug.Assert(success);
            }

            for (int i = 0; i < _propertyList.Count; i++)
            {
                KeyValuePair<string, T> current = _propertyList[i];

                if (_stringComparer.Equals(current.Key, propertyName))
                {
                    _propertyList.RemoveAt(i);
                    existing = current.Value;
                    return true;
                }
            }

            existing = default;
            return false;
        }

        public KeyValuePair<string, T> GetAt(int index) => _propertyList[index];

        public void SetAt(int index, string key, T value)
        {
            string existingKey = _propertyList[index].Key;
            if (!_stringComparer.Equals(existingKey, key))
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

        public void SetAt(int index, T value)
        {
            string key = _propertyList[index].Key;
            if (_propertyDictionary != null)
            {
                _propertyDictionary[key] = value;
            }

            _propertyList[index] = new(key, value);
        }

        public void Insert(int index, string key, T value)
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
            KeyValuePair<string, T> item = _propertyList[index];
            _propertyList.RemoveAt(index);
            _propertyDictionary?.Remove(item.Key);
        }

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        bool ICollection<KeyValuePair<string, T>>.IsReadOnly => false;
        void ICollection<KeyValuePair<string, T>>.Add(KeyValuePair<string, T> item) => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<string, T>>.Contains(KeyValuePair<string, T> item) => TryGetValue(item.Key, out T? existingValue) && _valueComparer.Equals(item.Value, existingValue);
        bool ICollection<KeyValuePair<string, T>>.Remove(KeyValuePair<string, T> item)
        {
            return TryGetValue(item.Key, out T? existingValue) && _valueComparer.Equals(existingValue, item.Value)
                ? Remove(item.Key, out _)
                : false;
        }

        void ICollection<KeyValuePair<string, T>>.CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(arrayIndex));
            }

            foreach (KeyValuePair<string, T> item in _propertyList)
            {
                if (arrayIndex >= array.Length)
                {
                    ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(array));
                }

                array[arrayIndex++] = item;
            }
        }

        bool IDictionary<string, T>.Remove(string key) => Remove(key, out _);
        void IList<KeyValuePair<string, T>>.Insert(int index, KeyValuePair<string, T> item) => Insert(index, item.Key, item.Value);
        int IList<KeyValuePair<string, T>>.IndexOf(KeyValuePair<string, T> item)
        {
            List<KeyValuePair<string, T>> propertyList = _propertyList;
            StringComparer keyComparer = _stringComparer;
            EqualityComparer<T> valueComparer = _valueComparer;

            for (int i = 0; i < propertyList.Count; i++)
            {
                KeyValuePair<string, T> entry = propertyList[i];
                if (keyComparer.Equals(entry.Key, item.Key) && valueComparer.Equals(item.Value, entry.Value))
                {
                    return i;
                }
            }

            return -1;
        }

        KeyValuePair<string, T> IList<KeyValuePair<string, T>>.this[int index]
        {
            get => GetAt(index);
            set => SetAt(index, value.Key, value.Value);
        }
    }
}

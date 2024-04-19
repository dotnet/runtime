// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json
{
    /// <summary>
    /// Keeps both a List and Dictionary in sync to enable deterministic enumeration ordering of List
    /// and performance benefits of Dictionary once a threshold is hit.
    /// </summary>
    internal sealed partial class JsonPropertyDictionary<T> where T : class?
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, T>? _propertyDictionary;
        private readonly List<KeyValuePair<string, T>> _propertyList;

        private readonly StringComparer _stringComparer;

        public JsonPropertyDictionary(bool caseInsensitive)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _propertyList = new List<KeyValuePair<string, T>>();
        }

        public JsonPropertyDictionary(bool caseInsensitive, int capacity)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            if (capacity > ListToDictionaryThreshold)
            {
                _propertyDictionary = new(capacity, _stringComparer);
            }

            _propertyList = new(capacity);
        }

        // Enable direct access to the List for performance reasons.
        public List<KeyValuePair<string, T>> List => _propertyList;

        public void Add(string propertyName, T value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            AddValue(propertyName, value);
        }

        public void Add(KeyValuePair<string, T> property)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            Add(property.Key, property.Value);
        }

        public bool TryAdd(string propertyName, T value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            // A check for a null propertyName is not required since this method is only called by internal code.
            Debug.Assert(propertyName != null);

            return TryAddValue(propertyName, value);
        }

        public void Clear()
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            _propertyList.Clear();
            _propertyDictionary?.Clear();
        }

        public bool ContainsKey(string propertyName)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            return ContainsProperty(propertyName);
        }

        public int Count
        {
            get
            {
                return _propertyList.Count;
            }
        }

        public bool Remove(string propertyName)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            return TryRemoveProperty(propertyName, out _);
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            foreach (KeyValuePair<string, T> existing in this)
            {
                if (ReferenceEquals(item.Value, existing.Value) && _stringComparer.Equals(item.Key, existing.Key))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int index)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_ArrayIndexNegative(nameof(index));
            }

            foreach (KeyValuePair<string, T> item in _propertyList)
            {
                if (index >= array.Length)
                {
                    ThrowHelper.ThrowArgumentException_ArrayTooSmall(nameof(array));
                }

                array[index++] = item;
            }
        }

        public List<KeyValuePair<string, T>>.Enumerator GetEnumerator() => _propertyList.GetEnumerator();

        public IList<string> Keys => GetKeyCollection();

        public IList<T> Values => GetValueCollection();

        public bool TryGetValue(string propertyName, [MaybeNullWhen(false)] out T value)
        {
            if (propertyName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            if (_propertyDictionary != null)
            {
                return _propertyDictionary.TryGetValue(propertyName, out value);
            }
            else
            {
                foreach (KeyValuePair<string, T> item in _propertyList)
                {
                    if (_stringComparer.Equals(propertyName, item.Key))
                    {
                        value = item.Value;
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }

        public bool IsReadOnly { get; set; }

        [DisallowNull]
        public T? this[string propertyName]
        {
            get
            {
                if (TryGetPropertyValue(propertyName, out T? value))
                {
                    return value;
                }

                // Return null for missing properties.
                return null;
            }

            set
            {
                SetValue(propertyName, value, out bool _);
            }
        }

        public T? SetValue(string propertyName, T value, out bool valueAlreadyInDictionary)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(propertyName));
            }

            CreateDictionaryIfThresholdMet();

            valueAlreadyInDictionary = false;
            T? existing = null;

            if (_propertyDictionary != null)
            {
                // Fast path if item doesn't exist in dictionary.
                if (_propertyDictionary.TryAdd(propertyName, value))
                {
                    _propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
                    return null;
                }

                existing = _propertyDictionary[propertyName];
                if (ReferenceEquals(existing, value))
                {
                    // Ignore if the same value.
                    valueAlreadyInDictionary = true;
                    return null;
                }
            }

            int i = FindValueIndex(propertyName);
            if (i >= 0)
            {
                if (_propertyDictionary != null)
                {
                    _propertyDictionary[propertyName] = value;
                }
                else
                {
                    KeyValuePair<string, T> current = _propertyList[i];
                    if (ReferenceEquals(current.Value, value))
                    {
                        // Ignore if the same value.
                        valueAlreadyInDictionary = true;
                        return null;
                    }

                    existing = current.Value;
                }

                _propertyList[i] = new KeyValuePair<string, T>(propertyName, value);
            }
            else
            {
                _propertyDictionary?.Add(propertyName, value);
                _propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
                Debug.Assert(existing == null);
            }

            return existing;
        }

        private void AddValue(string propertyName, T value)
        {
            if (!TryAddValue(propertyName, value))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(nameof(propertyName), propertyName);
            }
        }

        internal bool TryAddValue(string propertyName, T value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

            CreateDictionaryIfThresholdMet();

            if (_propertyDictionary == null)
            {
                // Verify there are no duplicates before adding.
                if (ContainsProperty(propertyName))
                {
                    return false;
                }
            }
            else
            {
                if (!_propertyDictionary.TryAdd(propertyName, value))
                {
                    return false;
                }
            }

            _propertyList.Add(new KeyValuePair<string, T>(propertyName, value));
            return true;
        }

        private void CreateDictionaryIfThresholdMet()
        {
            if (_propertyDictionary == null && _propertyList.Count > ListToDictionaryThreshold)
            {
                _propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _stringComparer);
            }
        }

        internal bool ContainsValue(T value)
        {
            foreach (T item in GetValueCollection())
            {
                if (ReferenceEquals(item, value))
                {
                    return true;
                }
            }

            return false;
        }

        public KeyValuePair<string, T>? FindValue(T value)
        {
            foreach (KeyValuePair<string, T> item in this)
            {
                if (ReferenceEquals(item.Value, value))
                {
                    return item;
                }
            }

            return null;
        }

        private bool ContainsProperty(string propertyName)
        {
            if (_propertyDictionary != null)
            {
                return _propertyDictionary.ContainsKey(propertyName);
            }

            foreach (KeyValuePair<string, T> item in _propertyList)
            {
                if (_stringComparer.Equals(propertyName, item.Key))
                {
                    return true;
                }
            }

            return false;
        }

        private int FindValueIndex(string propertyName)
        {
            for (int i = 0; i < _propertyList.Count; i++)
            {
                KeyValuePair<string, T> current = _propertyList[i];
                if (_stringComparer.Equals(propertyName, current.Key))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryGetPropertyValue(string propertyName, [MaybeNullWhen(false)] out T value) => TryGetValue(propertyName, out value);

        public bool TryRemoveProperty(string propertyName, [MaybeNullWhen(false)] out T existing)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_CollectionIsReadOnly();
            }

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

            existing = null;
            return false;
        }
    }
}

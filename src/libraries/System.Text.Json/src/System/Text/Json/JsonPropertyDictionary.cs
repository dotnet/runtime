// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.Json
{
    /// <summary>
    /// Keeps both a List and Dictionary in sync to enable determinstic enumeration ordering of List
    /// and performance benefits of Dictionary once a threshold is hit.
    /// </summary>
    internal sealed partial class JsonPropertyDictionary<T> where T : class
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, T?>? _propertyDictionary;
        private readonly List<KeyValuePair<string, T?>> _propertyList;

        private StringComparer _stringComparer;

        public JsonPropertyDictionary(bool caseInsensitive)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _propertyList = new List<KeyValuePair<string, T?>>();
        }

        public JsonPropertyDictionary(bool caseInsensitive, int capacity)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _propertyList = new List<KeyValuePair<string, T?>>(capacity);
        }

        // Enable direct access to the List for performance reasons.
        public List<KeyValuePair<string, T?>> List => _propertyList;

        public void Add(string propertyName, T? value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            AddValue(propertyName, value);
        }

        public void Add(KeyValuePair<string, T?> property)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            Add(property.Key, property.Value);
        }

        public bool TryAdd(string propertyName, T value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            // A check for a null propertyName is not required since this method is only called by internal code.
            Debug.Assert(propertyName != null);

            return TryAddValue(propertyName, value);
        }

        public void Clear()
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
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
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return TryRemoveProperty(propertyName, out _);
        }

        public bool Contains(KeyValuePair<string, T?> item)
        {
            foreach (KeyValuePair<string, T?> existing in this)
            {
                if (ReferenceEquals(item.Value, existing.Value) && _stringComparer.Equals(item.Key, existing.Key))
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(KeyValuePair<string, T?>[] array, int index)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
            }

            foreach (KeyValuePair<string, T?> item in _propertyList)
            {
                if (index >= array.Length)
                {
                    ThrowHelper.ThrowArgumentException_NodeArrayTooSmall(nameof(array));
                }

                array[index++] = item;
            }
        }

        public IEnumerator<KeyValuePair<string, T?>> GetEnumerator()
        {
            foreach (KeyValuePair<string, T?> item in _propertyList)
            {
                yield return item;
            }
        }

        public ICollection<string> Keys => GetKeyCollection();

        public ICollection<T?> Values => GetValueCollection();

        public bool TryGetValue(string propertyName, out T? value)
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
                foreach (KeyValuePair<string, T?> item in _propertyList)
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
                SetValue(propertyName, value);
            }
        }

        public T? SetValue(string propertyName, T? value, Action? assignParent = null)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            CreateDictionaryIfThresholdMet();

            T? existing = null;

            if (_propertyDictionary != null)
            {
                // Fast path if item doesn't exist in dictionary.
                if (JsonHelpers.TryAdd(_propertyDictionary, propertyName, value))
                {
                    assignParent?.Invoke();
                    _propertyList.Add(new KeyValuePair<string, T?>(propertyName, value));
                    return null;
                }

                existing = _propertyDictionary[propertyName];
                if (ReferenceEquals(existing, value))
                {
                    // Ignore if the same value.
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
                    KeyValuePair<string, T?> current = _propertyList[i];
                    if (ReferenceEquals(current.Value, value))
                    {
                        // Ignore if the same value.
                        return null;
                    }

                    existing = current.Value;
                }

                assignParent?.Invoke();
                _propertyList[i] = new KeyValuePair<string, T?>(propertyName, value);
            }
            else
            {
                assignParent?.Invoke();
                _propertyDictionary?.Add(propertyName, value);
                _propertyList.Add(new KeyValuePair<string, T?>(propertyName, value));
                Debug.Assert(existing == null);
            }

            return existing;
        }

        private void AddValue(string propertyName, T? value)
        {
            if (!TryAddValue(propertyName, value))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(propertyName);
            }
        }

        private bool TryAddValue(string propertyName, T? value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
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
                if (!JsonHelpers.TryAdd(_propertyDictionary, propertyName, value))
                {
                    return false;
                }
            }

            _propertyList.Add(new KeyValuePair<string, T?>(propertyName, value));
            return true;
        }

        private void CreateDictionaryIfThresholdMet()
        {
            if (_propertyDictionary == null && _propertyList.Count > ListToDictionaryThreshold)
            {
                _propertyDictionary = JsonHelpers.CreateDictionaryFromCollection(_propertyList, _stringComparer);
            }
        }

        private bool ContainsValue(T? value)
        {
            foreach (T? item in GetValueCollection())
            {
                if (ReferenceEquals(item, value))
                {
                    return true;
                }
            }

            return false;
        }

        public KeyValuePair<string, T?>? FindValue(T? value)
        {
            foreach (KeyValuePair<string, T?> item in this)
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

            foreach (KeyValuePair<string, T?> item in _propertyList)
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
                KeyValuePair<string, T?> current = _propertyList[i];
                if (_stringComparer.Equals(propertyName, current.Key))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool TryGetPropertyValue(string propertyName, out T? value) => TryGetValue(propertyName, out value);

        public bool TryRemoveProperty(string propertyName, out T? existing)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
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
                KeyValuePair<string, T?> current = _propertyList[i];

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

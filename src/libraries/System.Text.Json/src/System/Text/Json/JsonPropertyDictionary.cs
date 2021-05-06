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
    internal partial class JsonPropertyDictionary<T> where T : class
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, T?>? _dictionary;
        private readonly List<KeyValuePair<string, T?>> _list;

        private StringComparer _stringComparer;

        public JsonPropertyDictionary(bool caseInsensitive)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _list = new List<KeyValuePair<string, T?>>();
        }

        public JsonPropertyDictionary(bool caseInsensitive, int capacity)
        {
            _stringComparer = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _list = new List<KeyValuePair<string, T?>>(capacity);
        }

        // Enable direct access to the List for performance reasons.
        public List<KeyValuePair<string, T?>> List => _list;

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

            _list.Clear();
            _dictionary?.Clear();
        }

        public bool ContainsKey(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return ContainsProperty(propertyName);
        }

        public int Count
        {
            get
            {
                return _list.Count;
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

            return TryRemoveProperty(propertyName, out T? removedValue);
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

            foreach (KeyValuePair<string, T?> item in _list)
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
            foreach (KeyValuePair<string, T?> item in _list)
            {
                yield return item;
            }
        }

        public ICollection<string> Keys => GetKeyCollection();

        public ICollection<T?> Values => GetValueCollection();

        public bool TryGetValue(string propertyName, out T? value)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (_dictionary != null)
            {
                return _dictionary.TryGetValue(propertyName, out value);
            }
            else
            {
                foreach (KeyValuePair<string, T?> item in _list)
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
                SetValue(propertyName, value, null);
            }
        }

        public T? SetValue(string propertyName, T? value, Action? assignParent)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            CreateDictionaryIfThreshold();

            T? existing = null;

            if (_dictionary != null)
            {
                // Fast path if item doesn't exist in dictionary.
                if (JsonHelpers.TryAdd(_dictionary, propertyName, value))
                {
                    assignParent?.Invoke();
                    _list.Add(new KeyValuePair<string, T?>(propertyName, value));
                    return null;
                }

                existing = _dictionary[propertyName];
                if (ReferenceEquals(existing, value))
                {
                    // Ignore if the same value.
                    return null;
                }
            }

            int i = FindValueIndex(propertyName);
            if (i >= 0)
            {
                if (_dictionary != null)
                {
                    _dictionary[propertyName] = value;
                }
                else
                {
                    KeyValuePair<string, T?> current = _list[i];
                    if (ReferenceEquals(current.Value, value))
                    {
                        // Ignore if the same value.
                        return null;
                    }

                    existing = current.Value;
                }

                assignParent?.Invoke();
                _list[i] = new KeyValuePair<string, T?>(propertyName, value);
            }
            else
            {
                assignParent?.Invoke();
                _dictionary?.Add(propertyName, value);
                _list.Add(new KeyValuePair<string, T?>(propertyName, value));
                Debug.Assert(existing == null);
            }

            return existing;
        }

        private void AddValue(string propertyName, T? value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            CreateDictionaryIfThreshold();

            if (_dictionary == null)
            {
                // Verify there are no duplicates before adding.
                if (ContainsProperty(propertyName))
                {
                    ThrowHelper.ThrowArgumentException_DuplicateKey(propertyName);
                }
            }
            else
            {
                _dictionary.Add(propertyName, value);
            }

            _list.Add(new KeyValuePair<string, T?>(propertyName, value));
        }

        private bool TryAddValue(string propertyName, T? value)
        {
            if (IsReadOnly)
            {
                ThrowHelper.ThrowNotSupportedException_NodeCollectionIsReadOnly();
            }

            CreateDictionaryIfThreshold();

            if (_dictionary == null)
            {
                // Verify there are no duplicates before adding.
                if (ContainsProperty(propertyName))
                {
                    return false;
                }
            }
            else
            {
               if (!JsonHelpers.TryAdd(_dictionary, propertyName, value))
                {
                    return false;
                }
            }

            _list.Add(new KeyValuePair<string, T?>(propertyName, value));
            return true;
        }

        public void CreateDictionaryIfThreshold()
        {
            if (_dictionary == null && _list.Count > ListToDictionaryThreshold)
            {
                _dictionary = JsonHelpers.CreateDictionaryFromCollection(_list, _stringComparer);
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
            if (_dictionary != null)
            {
                return _dictionary.ContainsKey(propertyName);
            }

            foreach (KeyValuePair<string, T?> item in _list)
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
            for (int i = 0; i < _list.Count; i++)
            {
                KeyValuePair<string, T?> current = _list[i];
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

            if (_dictionary != null)
            {
                if (!_dictionary.TryGetValue(propertyName, out existing))
                {
                    return false;
                }

                bool success = _dictionary.Remove(propertyName);
                Debug.Assert(success);
            }

            for (int i = 0; i < _list.Count; i++)
            {
                KeyValuePair<string, T?> current = _list[i];

                if (_stringComparer.Equals(current.Key, propertyName))
                {
                    _list.RemoveAt(i);
                    existing = current.Value;
                    return true;
                }
            }

            existing = null;
            return false;
        }
    }
}

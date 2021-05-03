// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Node
{
    public partial class JsonObject : IDictionary<string, JsonNode?>
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, JsonNode?>? _dictionary;
        private List<KeyValuePair<string, JsonNode?>>? _list;

        /// We defer creating the comparer as long as possible in case no options were specified during creation.
        /// In that case if later we are added to a parent with a non-null options, we use the parent options.
        private StringComparer? _stringComparer;

        private string? _lastKey;
        private JsonNode? _lastValue;

        /// <summary>
        ///   Adds an element with the provided property name and value to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="propertyName">The property name of the element to add.</param>
        /// <param name="value">The value of the element to add.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/>is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   An element with the same property name already exists in the <see cref="JsonObject"/>.
        /// </exception>
        public void Add(string propertyName, JsonNode? value)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            AddNode(propertyName, value);
            value?.AssignParent(this);
        }

        /// <summary>
        ///   Adds the specified property to the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="property">
        ///   The KeyValuePair structure representing the property name and value to add to the <see cref="JsonObject"/>.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   An element with the same property name already exists in the <see cref="JsonObject"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        ///   The property name of <paramref name="property"/> is <see langword="null"/>.
        /// </exception>
        public void Add(KeyValuePair<string, JsonNode?> property)
        {
            Add(property.Key, property.Value);
        }

        /// <summary>
        ///   Removes all elements from the <see cref="JsonObject"/>.
        /// </summary>
        public void Clear()
        {
            if (_jsonElement != null)
            {
                Debug.Assert(_list == null);
                Debug.Assert(_dictionary == null);
                _jsonElement = null;
                return;
            }

            foreach (JsonNode? node in GetValueCollection(this))
            {
                DetachParent(node);
            }

            _list?.Clear();
            _dictionary?.Clear();
            ClearLastValueCache();
        }

        /// <summary>
        ///   Determines whether the <see cref="JsonObject"/> contains an element with the specified property name.
        /// </summary>
        /// <param name="propertyName">The property name to locate in the <see cref="JsonObject"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the specified property name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        public bool ContainsKey(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            return ContainsNode(propertyName);
        }

        /// <summary>
        ///   Gets the number of elements contained in <see cref="JsonObject"/>.
        /// </summary>
        public int Count
        {
            get
            {
                CreateList();
                Debug.Assert(_list != null);
                return _list.Count;
            }
        }

        /// <summary>
        ///   Removes the element with the specified property name from the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="propertyName">The property name of the element to remove.</param>
        /// <returns>
        ///   <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        public bool Remove(string propertyName)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            bool success = TryRemoveNode(propertyName, out JsonNode? removedNode);
            if (success)
            {
                DetachParent(removedNode);
            }

            return success;
        }

        /// <summary>
        ///   Determines whether the <see cref="JsonObject"/> contains a specific property name and <see cref="JsonNode"/> reference.
        /// </summary>
        /// <param name="item">The element to locate in the <see cref="JsonObject"/>.</param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the property name; otherwise, <see langword="false"/>.
        /// </returns>
        bool ICollection<KeyValuePair<string, JsonNode?>>.Contains(KeyValuePair<string, JsonNode?> item)
        {
            foreach (KeyValuePair<string, JsonNode?> existing in this)
            {
                Debug.Assert(_stringComparer != null);
                if (ReferenceEquals(item.Value, existing.Value) && _stringComparer.Equals(item.Key, existing.Key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///   Copies the elements of the <see cref="JsonObject"/> to an array of type KeyValuePair starting at the specified array index.
        /// </summary>
        /// <param name="array">
        ///   The one-dimensional Array that is the destination of the elements copied from <see cref="JsonObject"/>.
        /// </param>
        /// <param name="index">The zero-based index in <paramref name="array"/> at which copying begins.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="array"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="index"/> is less than 0.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   The number of elements in the source ICollection is greater than the available space from <paramref name="index"/>
        ///   to the end of the destination <paramref name="array"/>.
        /// </exception>
        void ICollection<KeyValuePair<string, JsonNode?>>.CopyTo(KeyValuePair<string, JsonNode?>[] array, int index)
        {
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException_NodeArrayIndexNegative(nameof(index));
            }

            CreateList();
            Debug.Assert(_list != null);

            foreach (KeyValuePair<string, JsonNode?> item in _list)
            {
                if (index >= array.Length)
                {
                    ThrowHelper.ThrowArgumentException_NodeArrayTooSmall(nameof(array));
                }

                array[index++] = item;
            }
        }

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonObject"/>.
        /// </summary>
        /// <returns>
        ///   An enumerator that iterates through the <see cref="JsonObject"/>.
        /// </returns>
        public IEnumerator<KeyValuePair<string, JsonNode?>> GetEnumerator()
        {
            CreateList();
            Debug.Assert(_list != null);

            foreach (KeyValuePair<string, JsonNode?> item in _list)
            {
                yield return item;
            }
        }

        /// <summary>
        ///   Removes a key and value from the <see cref="JsonObject"/>.
        /// </summary>
        /// <param name="item">
        ///   The KeyValuePair structure representing the property name and value to remove from the <see cref="JsonObject"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the element is successfully removed; otherwise, <see langword="false"/>.
        /// </returns>
        bool ICollection<KeyValuePair<string, JsonNode?>>.Remove(KeyValuePair<string, JsonNode?> item) => Remove(item.Key);

        /// <summary>
        ///   Gets a collection containing the property names in the <see cref="JsonObject"/>.
        /// </summary>
        ICollection<string> IDictionary<string, JsonNode?>.Keys => GetKeyCollection(this);

        /// <summary>
        ///   Gets a collection containing the property values in the <see cref="JsonObject"/>.
        /// </summary>
        ICollection<JsonNode?> IDictionary<string, JsonNode?>.Values => GetValueCollection(this);

        /// <summary>
        ///   Gets the value associated with the specified property name.
        /// </summary>
        /// <param name="propertyName">The property name of the value to get.</param>
        /// <param name="jsonNode">
        ///   When this method returns, contains the value associated with the specified property name, if the property name is found;
        ///   otherwise, <see langword="null"/>.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the <see cref="JsonObject"/> contains an element with the specified property name; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        bool IDictionary<string, JsonNode?>.TryGetValue(string propertyName, out JsonNode? jsonNode)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            CreateList();
            Debug.Assert(_list != null);
            Debug.Assert(_stringComparer != null);

            if (propertyName == _lastKey)
            {
                // Optimize for repeating sections in code:
                // obj.Foo.Bar.FirstProperty = value1;
                // obj.Foo.Bar.SecondProperty = value2;
                jsonNode = _lastValue;
                return true;
            }

            if (_dictionary != null)
            {
                bool success = _dictionary.TryGetValue(propertyName, out jsonNode);
                if (success)
                {
                    _lastKey = propertyName;
                    _lastValue = jsonNode;
                }

                return success;
            }
            else
            {
                foreach (KeyValuePair<string, JsonNode?> item in _list)
                {
                    if (_stringComparer.Equals(propertyName, item.Key))
                    {
                        jsonNode = item.Value;
                        _lastKey = propertyName;
                        _lastValue = jsonNode;
                        return true;
                    }
                }
            }

            jsonNode = null;
            return false;
        }

        /// <summary>
        ///   Returns <see langword="false"/>.
        /// </summary>
        bool ICollection<KeyValuePair<string, JsonNode?>>.IsReadOnly => false;

        /// <summary>
        ///   Returns an enumerator that iterates through the <see cref="JsonObject"/>.
        /// </summary>
        /// <returns>
        ///   An enumerator that iterates through the <see cref="JsonObject"/>.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            CreateList();
            Debug.Assert(_list != null);

            foreach (KeyValuePair<string, JsonNode?> item in _list)
            {
                yield return item;
            }
        }

        private void CreateStringComparer()
        {
            bool caseInsensitive = Options?.PropertyNameCaseInsensitive == true;
            if (caseInsensitive)
            {
                _stringComparer = StringComparer.OrdinalIgnoreCase;
            }
            else
            {
                _stringComparer = StringComparer.Ordinal;
            }
        }

        private void AddNode(string propertyName, JsonNode? node)
        {
            CreateList();
            Debug.Assert(_list != null);

            CreateDictionaryIfThreshold();

            if (_dictionary == null)
            {
                // Verify there are no duplicates before adding.
                VerifyListItemMissing(propertyName);
            }
            else
            {
                _dictionary.Add(propertyName, node);
            }

            _list.Add(new KeyValuePair<string, JsonNode?>(propertyName, node));
        }

        private void ClearLastValueCache()
        {
            _lastKey = null;
            _lastValue = null;
        }

        private void CreateList()
        {
            if (_list != null)
            {
                return;
            }

            CreateStringComparer();
            var list = new List<KeyValuePair<string, JsonNode?>>();
            if (_jsonElement.HasValue)
            {
                JsonElement jElement = _jsonElement.Value;

                foreach (JsonProperty jElementProperty in jElement.EnumerateObject())
                {
                    JsonNode? node = JsonNodeConverter.Create(jElementProperty.Value, Options);
                    if (node != null)
                    {
                        node.Parent = this;
                    }

                    list.Add(new KeyValuePair<string, JsonNode?>(jElementProperty.Name, node));
                }

                _jsonElement = null;
            }

            _list = list;
            CreateDictionaryIfThreshold();
        }

        private JsonNode? SetNode(string propertyName, JsonNode? node)
        {
            CreateList();
            Debug.Assert(_list != null);

            CreateDictionaryIfThreshold();

            JsonNode? existing = null;

            if (_dictionary != null)
            {
                // Fast path if item doesn't exist in dictionary.
                if (JsonHelpers.TryAdd(_dictionary, propertyName, node))
                {
                    node?.AssignParent(this);
                    _list.Add(new KeyValuePair<string, JsonNode?>(propertyName, node));
                    return null;
                }

                existing = _dictionary[propertyName];
                if (ReferenceEquals(existing, node))
                {
                    _lastKey = propertyName;
                    _lastValue = node;

                    // Ignore if the same value.
                    return null;
                }
            }

            int i = FindNodeIndex(propertyName);
            if (i >= 0)
            {
                if (_dictionary != null)
                {
                    _dictionary[propertyName] = node;
                }
                else
                {
                    KeyValuePair<string, JsonNode?> current = _list[i];
                    if (ReferenceEquals(current.Value, node))
                    {
                        // Ignore if the same value.
                        return null;
                    }

                    existing = current.Value;
                }

                node?.AssignParent(this);
                _list[i] = new KeyValuePair<string, JsonNode?>(propertyName, node);
            }
            else
            {
                node?.AssignParent(this);
                _dictionary?.Add(propertyName, node);
                _list.Add(new KeyValuePair<string, JsonNode?>(propertyName, node));
                Debug.Assert(existing == null);
            }

            _lastKey = propertyName;
            _lastValue = node;

            return existing;
        }

        private int FindNodeIndex(string propertyName)
        {
            Debug.Assert(_list != null);
            Debug.Assert(_stringComparer != null);

            for (int i = 0; i < _list.Count; i++)
            {
                KeyValuePair<string, JsonNode?> current = _list[i];
                if (_stringComparer.Equals(propertyName, current.Key))
                {
                    return i;
                }
            }

            return -1;
        }

        private void CreateDictionaryIfThreshold()
        {
            Debug.Assert(_list != null);
            Debug.Assert(_stringComparer != null);

            if (_dictionary == null && _list.Count > ListToDictionaryThreshold)
            {
                _dictionary = JsonHelpers.CreateDictionaryFromCollection(_list, _stringComparer);
            }
        }

        private bool ContainsNode(JsonNode? node)
        {
            CreateList();

            foreach (JsonNode? item in GetValueCollection(this))
            {
                if (ReferenceEquals(item, node))
                {
                    return true;
                }
            }

            return false;
        }

        private KeyValuePair<string, JsonNode?>? FindNode(JsonNode? node)
        {
            CreateList();

            foreach (KeyValuePair<string, JsonNode?> item in this)
            {
                if (ReferenceEquals(item.Value, node))
                {
                    return item;
                }
            }

            return null;
        }

        private bool ContainsNode(string propertyName)
        {
            if (_dictionary != null)
            {
                return _dictionary.ContainsKey(propertyName);
            }

            foreach (string item in GetKeyCollection(this))
            {
                Debug.Assert(_stringComparer != null);
                if (_stringComparer.Equals(item, propertyName))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryRemoveNode(string propertyName, out JsonNode? existing)
        {
            CreateList();
            Debug.Assert(_list != null);
            Debug.Assert(_stringComparer != null);

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
                KeyValuePair<string, JsonNode?> current = _list[i];

                if (_stringComparer.Equals(current.Key, propertyName))
                {
                    _list.RemoveAt(i);
                    existing = current.Value;
                    DetachParent(existing);
                    return true;
                }
            }

            existing = null;
            return false;
        }

        private void VerifyListItemMissing(string propertyName)
        {
            Debug.Assert(_dictionary == null);
            Debug.Assert(_list != null);

            if (ContainsNode(propertyName))
            {
                ThrowHelper.ThrowArgumentException_DuplicateKey(propertyName);
            }
        }
    }
}

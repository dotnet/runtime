// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization.Converters;

namespace System.Text.Json.Node
{
    public partial class JsonObject
    {
        private const int ListToDictionaryThreshold = 9;

        private Dictionary<string, JsonNode?>? _dictionary;
        private List<KeyValuePair<string, JsonNode?>>? _list;

        /// We defer creating the comparer as long as possible in case no options were specified during creation.
        /// In that case if later we are added to a parent with a non-null options, we use the parent options.
        private StringComparer? _stringComparer;

        private string? _lastKey;
        private JsonNode? _lastValue;

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

        private int NodeCount
        {
            get
            {
                CreateList();
                Debug.Assert(_list != null);
                return _list.Count;
            }
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

        private void ClearNodes()
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
        /// This is based on ICollection.Contains(KeyValuePair) and is explicitely implemented and
        /// is not expected to be used often since IDictionary.ContainsKey(key) is more useful.
        /// </summary>
        private bool ContainsNode(KeyValuePair<string, JsonNode?> node)
        {
            foreach (KeyValuePair<string, JsonNode?> item in this)
            {
                Debug.Assert(_stringComparer != null);
                if (ReferenceEquals(item.Value, node.Value) && _stringComparer.Equals(item.Key, node.Key))
                {
                    return true;
                }
            }

            return false;
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

        private bool TryFindNode(string propertyName, out JsonNode? property)
        {
            CreateList();
            Debug.Assert(_list != null);
            Debug.Assert(_stringComparer != null);

            if (propertyName == _lastKey)
            {
                // Optimize for repeating sections in code:
                // obj.Foo.Bar.FirstProperty = value1;
                // obj.Foo.Bar.SecondProperty = value2;
                property = _lastValue;
                return true;
            }

            if (_dictionary != null)
            {
                bool success = _dictionary.TryGetValue(propertyName, out property);
                if (success)
                {
                    _lastKey = propertyName;
                    _lastValue = property;
                }

                return success;
            }
            else
            {
                foreach (KeyValuePair<string, JsonNode?> item in _list)
                {
                    if (_stringComparer.Equals(propertyName, item.Key))
                    {
                        property = item.Value;
                        _lastKey = propertyName;
                        _lastValue = property;
                        return true;
                    }
                }
            }

            property = null;
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

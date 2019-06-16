// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Text.Json.Serialization.Tests
{
    public class IImmutableDictionaryWrapper : IImmutableDictionary<string, int>
    {
        public int this[string key] => throw new NotImplementedException();

        public IEnumerable<string> Keys => throw new NotImplementedException();

        public IEnumerable<int> Values => throw new NotImplementedException();

        public int Count => throw new NotImplementedException();

        public IImmutableDictionary<string, int> Add(string key, int value)
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> AddRange(IEnumerable<KeyValuePair<string, int>> pairs)
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<string, int> pair)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(string key)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> Remove(string key)
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> RemoveRange(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> SetItem(string key, int value)
        {
            throw new NotImplementedException();
        }

        public IImmutableDictionary<string, int> SetItems(IEnumerable<KeyValuePair<string, int>> items)
        {
            throw new NotImplementedException();
        }

        public bool TryGetKey(string equalKey, out string actualKey)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(string key, out int value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }

    public class StringToObjectIDictionaryWrapper : IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _dict;

        public StringToObjectIDictionaryWrapper(IDictionary<string, object> dict)
        {
            _dict = dict;
        }

        public object this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<object> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dict.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _dict.Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class StringToStringIDictionaryWrapper : IDictionary<string, string>
    {
        private readonly IDictionary<string, string> _dict;

        public StringToStringIDictionaryWrapper(IDictionary<string, string> dict)
        {
            _dict = dict;
        }

        public string this[string key] { get => _dict[key]; set => _dict[key] = value; }

        public ICollection<string> Keys => _dict.Keys;

        public ICollection<string> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => false;

        public void Add(string key, string value)
        {
            _dict.Add(key, value);
        }

        public void Add(KeyValuePair<string, string> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _dict.Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            _dict.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            return _dict.Remove(item);
        }

        public bool TryGetValue(string key, out string value)
        {
            return _dict.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

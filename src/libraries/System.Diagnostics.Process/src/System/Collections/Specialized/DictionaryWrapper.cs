// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Collections.Specialized
{
    internal sealed class DictionaryWrapper : IDictionary<string, string>, IDictionary
    {
        private readonly Dictionary<string, string> _contents;

        public DictionaryWrapper(Dictionary<string, string> contents)
        {
            _contents = contents;
        }

        public string this[string key]
        {
            get => _contents[key];
            set => _contents[key] = value;
        }

        public object this[object key]
        {
            get => this[(string)key];
            set => this[(string)key] = (string)value;
        }

        public ICollection<string> Keys => _contents.Keys;
        public ICollection<string> Values => _contents.Values;

        ICollection IDictionary.Keys => _contents.Keys;
        ICollection IDictionary.Values => _contents.Values;

        public int Count => _contents.Count;

        public bool IsReadOnly => ((IDictionary)_contents).IsReadOnly;
        public bool IsSynchronized => ((IDictionary)_contents).IsSynchronized;
        public bool IsFixedSize => ((IDictionary)_contents).IsFixedSize;
        public object SyncRoot => ((IDictionary)_contents).SyncRoot;

        public void Add(string key, string value) => this[key] = value;

        public void Add(KeyValuePair<string, string> item) => Add(item.Key, item.Value);

        public void Add(object key, object value) => Add((string)key, (string)value);

        public void Clear() => _contents.Clear();

        public bool Contains(KeyValuePair<string, string> item)
        {
            return _contents.ContainsKey(item.Key) && _contents[item.Key] == item.Value;
        }

        public bool Contains(object key) => ContainsKey((string)key);
        public bool ContainsKey(string key) => _contents.ContainsKey(key);
        public bool ContainsValue(string value) => _contents.ContainsValue(value);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            ((IDictionary<string, string>)_contents).CopyTo(array, arrayIndex);
        }

        public void CopyTo(Array array, int index) => ((IDictionary)_contents).CopyTo(array, index);

        public bool Remove(string key) => _contents.Remove(key);
        public void Remove(object key) => Remove((string)key);

        public bool Remove(KeyValuePair<string, string> item)
        {
            if (!Contains(item))
            {
                return false;
            }

            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out string value) => _contents.TryGetValue(key, out value);

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _contents.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _contents.GetEnumerator();
        IDictionaryEnumerator IDictionary.GetEnumerator() => _contents.GetEnumerator();
    }
}

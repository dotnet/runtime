// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public sealed class HttpRequestOptions : IDictionary<string, object?>
    {
        private IDictionary<string, object?> Options { get; } = new Dictionary<string, object?>();

        public void Add(string key, object? value)
        {
            Options.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return Options.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return Options.Keys; }
        }

        public bool Remove(string key)
        {
            return Options.Remove(key);
        }

        public bool TryGetValue(string key, out object? value)
        {
            return Options.TryGetValue(key, out value);
        }

        public ICollection<object?> Values
        {
            get { return Options.Values; }
        }

        public object? this[string key]
        {
            get
            {
                return Options[key];
            }
            set
            {
                Options[key] = value;
            }
        }

        public void Add(KeyValuePair<string, object?> item)
        {
            Options.Add(item);
        }

        public void Clear()
        {
            Options.Clear();
        }

        public bool Contains(KeyValuePair<string, object?> item)
        {
            return Options.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            Options.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Options.Count; }
        }

        public bool IsReadOnly
        {
            get { return Options.IsReadOnly; }
        }

        public bool Remove(KeyValuePair<string, object?> item)
        {
            return Options.Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            return Options.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)Options).GetEnumerator();
        }

        public bool TryGetValue<TValue>(HttpRequestOptionsKey<TValue> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (TryGetValue(key.Key, out object? _value) && _value is TValue tvalue)
            {
                value = tvalue;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value)
        {
            Add(key.Key, value);
        }
    }
}
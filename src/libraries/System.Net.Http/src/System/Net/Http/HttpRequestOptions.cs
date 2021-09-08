// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    public sealed class HttpRequestOptions : IDictionary<string, object?>
    {
        private Dictionary<string, object?> Options { get; } = new Dictionary<string, object?>();
        object? IDictionary<string, object?>.this[string key]
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
        ICollection<string> IDictionary<string, object?>.Keys => Options.Keys;
        ICollection<object?> IDictionary<string, object?>.Values => Options.Values;
        int ICollection<KeyValuePair<string, object?>>.Count => Options.Count;
        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => ((IDictionary<string, object?>)Options).IsReadOnly;
        void IDictionary<string, object?>.Add(string key, object? value) => Options.Add(key, value);
        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)Options).Add(item);
        void ICollection<KeyValuePair<string, object?>>.Clear() => Options.Clear();
        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)Options).Contains(item);
        bool IDictionary<string, object?>.ContainsKey(string key) => Options.ContainsKey(key);
        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) =>
            ((IDictionary<string, object?>)Options).CopyTo(array, arrayIndex);
        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() => Options.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)Options).GetEnumerator();
        bool IDictionary<string, object?>.Remove(string key) => Options.Remove(key);
        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) => ((IDictionary<string, object?>)Options).Remove(item);
        bool IDictionary<string, object?>.TryGetValue(string key, out object? value) => Options.TryGetValue(key, out value);
        public bool TryGetValue<TValue>(HttpRequestOptionsKey<TValue> key, [MaybeNullWhen(false)] out TValue value)
        {
            if (Options.TryGetValue(key.Key, out object? _value) && _value is TValue tvalue)
            {
                value = tvalue;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value)
        {
            Options[key.Key] = value;
        }
    }
}
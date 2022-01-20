// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    /// <summary>
    /// Represents a collection of options for an HTTP request.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the HttpRequestOptions class.
        /// </summary>
        public HttpRequestOptions() { }

        /// <summary>
        /// Gets the value of a given HTTP request option.
        /// </summary>
        /// <param name="key">Strongly typed key to get the value of HTTP request option. For example <code>new HttpRequestOptionsKey&lt;bool&gt;("WebAssemblyEnableStreamingResponse")</code></param>
        /// <param name="value">Returns the value of HTTP request option.</param>
        /// <typeparam name="TValue">The type of the HTTP value as defined by <code>key</code> parameter.</typeparam>
        /// <returns>True, if an option is retrieved.</returns>
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

        /// <summary>
        /// Sets the value of a given request option.
        /// </summary>
        /// <param name="key">Strongly typed key to get the value of HTTP request option. For example <code>new HttpRequestOptionsKey&lt;bool&gt;("WebAssemblyEnableStreamingResponse")</code></param>
        /// <param name="value">The value of the HTTP request option.</param>
        /// <typeparam name="TValue">The type of the HTTP value as defined by <code>key</code> parameter.</typeparam>
        public void Set<TValue>(HttpRequestOptionsKey<TValue> key, TValue value)
        {
            Options[key.Key] = value;
        }
    }
}

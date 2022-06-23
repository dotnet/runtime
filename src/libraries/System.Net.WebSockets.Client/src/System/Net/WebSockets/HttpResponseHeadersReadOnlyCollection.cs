// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace System.Net.WebSockets
{
    internal sealed class HttpResponseHeadersReadOnlyCollection : IReadOnlyDictionary<string, IEnumerable<string>>
    {
        private readonly Dictionary<string, IEnumerable<string>> _headers = new Dictionary<string, IEnumerable<string>>();

        public HttpResponseHeadersReadOnlyCollection(HttpResponseHeaders headers)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
            {
                _headers.Add(header.Key, header.Value);
            }
        }

        public IEnumerable<string> this[string key] => _headers[key];

        public IEnumerable<string> Keys => _headers.Keys;

        public IEnumerable<IEnumerable<string>> Values => _headers.Values;

        public int Count => _headers.Count;

        public bool ContainsKey(string key) => _headers.ContainsKey(key);
        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator() => _headers.GetEnumerator();
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IEnumerable<string> value) => _headers.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _headers.GetEnumerator();
    }
}

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
        private readonly HttpHeadersNonValidated _headers;

        public HttpResponseHeadersReadOnlyCollection(HttpResponseHeaders headers) => _headers = headers.NonValidated;

        public IEnumerable<string> this[string key] => _headers[key];

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
                {
                    yield return header.Key;
                }
            }
        }

        public IEnumerable<IEnumerable<string>> Values
        {
            get
            {
                foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
                {
                    yield return header.Value;
                }
            }
        }

        public int Count => _headers.Count;

        public bool ContainsKey(string key) => _headers.Contains(key);

        public IEnumerator<KeyValuePair<string, IEnumerable<string>>> GetEnumerator()
        {
            foreach (KeyValuePair<string, HeaderStringValues> header in _headers)
            {
                yield return new KeyValuePair<string, IEnumerable<string>>(header.Key, header.Value);
            }
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out IEnumerable<string> value)
        {
            if (_headers.TryGetValues(key, out HeaderStringValues values))
            {
                value = values;
                return true;
            }

            value = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

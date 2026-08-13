// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.Extensions.Http.Logging
{
    internal sealed class HttpHeadersLogValue : IReadOnlyList<KeyValuePair<string, object>>
    {
        private const string RedactedValue = "*";

        private readonly Kind _kind;
        private readonly Func<string, bool> _shouldRedactHeaderValue;

        private string? _formatted;
        private List<KeyValuePair<string, object>>? _values;

        public HttpHeadersLogValue(Kind kind, HttpHeaders headers, HttpHeaders? contentHeaders, Func<string, bool> shouldRedactHeaderValue)
        {
            _kind = kind;
            _shouldRedactHeaderValue = shouldRedactHeaderValue;

            Headers = headers;
            ContentHeaders = contentHeaders;
        }

        public HttpHeaders Headers { get; }

        public HttpHeaders? ContentHeaders { get; }

        private List<KeyValuePair<string, object>> Values
        {
            get
            {
                if (_values == null)
                {
                    var values = new List<KeyValuePair<string, object>>(GetHeaderCount(Headers) + GetHeaderCount(ContentHeaders));

                    AddHeaders(values, Headers);

                    if (ContentHeaders != null)
                    {
                        AddHeaders(values, ContentHeaders);
                    }

                    _values = values;
                }

                return _values;
            }
        }

        public KeyValuePair<string, object> this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }

                return Values[index];
            }
        }

        public int Count => Values.Count;

        // Enumerate the headers without triggering validation/parsing of the values, so that logging
        // doesn't alter how the headers are subsequently serialized on the wire.
        private void AddHeaders(List<KeyValuePair<string, object>> values, HttpHeaders headers)
        {
#if NET
            foreach (KeyValuePair<string, HeaderStringValues> kvp in headers.NonValidated)
#else
            foreach (KeyValuePair<string, IEnumerable<string>> kvp in headers)
#endif
            {
                string value = _shouldRedactHeaderValue(kvp.Key)
                    ? RedactedValue
#if NET
                    : kvp.Value.ToString();
#else
                    : string.Join(", ", kvp.Value);
#endif
                values.Add(new KeyValuePair<string, object>(kvp.Key, value));
            }
        }

        private static int GetHeaderCount(HttpHeaders? headers)
        {
#if NET
            return headers?.NonValidated.Count ?? 0;
#else
            return 0;
#endif
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Values.GetEnumerator();
        }

        public override string ToString()
        {
            if (_formatted == null)
            {
                var builder = new StringBuilder();
                builder.AppendLine(_kind == Kind.Request ? "Request Headers:" : "Response Headers:");

                for (int i = 0; i < Values.Count; i++)
                {
                    KeyValuePair<string, object> kvp = Values[i];
                    builder.Append(kvp.Key);
                    builder.Append(": ");
                    builder.AppendLine((string)kvp.Value);
                }

                _formatted = builder.ToString();
            }

            return _formatted;
        }

        public enum Kind
        {
            Request,
            Response,
        }
    }
}

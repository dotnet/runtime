// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.QPack;

namespace System.Net.Http
{
    public partial class HttpMethod : IEquatable<HttpMethod>
    {
        private readonly string _method;
        private readonly int? _http3Index;

        private int _hashcode;

        private static readonly HttpMethod s_getMethod = new HttpMethod("GET", http3StaticTableIndex: H3StaticTable.MethodGet);
        private static readonly HttpMethod s_putMethod = new HttpMethod("PUT", http3StaticTableIndex: H3StaticTable.MethodPut);
        private static readonly HttpMethod s_postMethod = new HttpMethod("POST", http3StaticTableIndex: H3StaticTable.MethodPost);
        private static readonly HttpMethod s_deleteMethod = new HttpMethod("DELETE", http3StaticTableIndex: H3StaticTable.MethodDelete);
        private static readonly HttpMethod s_headMethod = new HttpMethod("HEAD", http3StaticTableIndex: H3StaticTable.MethodHead);
        private static readonly HttpMethod s_optionsMethod = new HttpMethod("OPTIONS", http3StaticTableIndex: H3StaticTable.MethodOptions);
        private static readonly HttpMethod s_traceMethod = new HttpMethod("TRACE", -1);
        private static readonly HttpMethod s_patchMethod = new HttpMethod("PATCH", -1);
        private static readonly HttpMethod s_connectMethod = new HttpMethod("CONNECT", http3StaticTableIndex: H3StaticTable.MethodConnect);

        public static HttpMethod Get
        {
            get { return s_getMethod; }
        }

        public static HttpMethod Put
        {
            get { return s_putMethod; }
        }

        public static HttpMethod Post
        {
            get { return s_postMethod; }
        }

        public static HttpMethod Delete
        {
            get { return s_deleteMethod; }
        }

        public static HttpMethod Head
        {
            get { return s_headMethod; }
        }

        public static HttpMethod Options
        {
            get { return s_optionsMethod; }
        }

        public static HttpMethod Trace
        {
            get { return s_traceMethod; }
        }

        public static HttpMethod Patch
        {
            get { return s_patchMethod; }
        }

        /// <summary>Gets the HTTP CONNECT protocol method.</summary>
        /// <value>The HTTP CONNECT method.</value>
        public static HttpMethod Connect
        {
            get { return s_connectMethod; }
        }

        public string Method
        {
            get { return _method; }
        }

        public HttpMethod(string method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(method);
            if (!HttpRuleParser.IsToken(method))
            {
                throw new FormatException(SR.net_http_httpmethod_format_error);
            }

            _method = method;
        }

        private HttpMethod(string method, int http3StaticTableIndex)
        {
            _method = method;
            _http3Index = http3StaticTableIndex;
        }

        #region IEquatable<HttpMethod> Members

        public bool Equals([NotNullWhen(true)] HttpMethod? other)
        {
            if (other is null)
            {
                return false;
            }

            if (object.ReferenceEquals(_method, other._method))
            {
                // Strings are static, so there is a good chance that two equal methods use the same reference
                // (unless they differ in case).
                return true;
            }

            return string.Equals(_method, other._method, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as HttpMethod);
        }

        public override int GetHashCode()
        {
            if (_hashcode == 0)
            {
                _hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(_method);
            }

            return _hashcode;
        }

        public override string ToString()
        {
            return _method;
        }

        public static bool operator ==(HttpMethod? left, HttpMethod? right)
        {
            return left is null || right is null ?
                ReferenceEquals(left, right) :
                left.Equals(right);
        }

        public static bool operator !=(HttpMethod? left, HttpMethod? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Returns a singleton method instance with a capitalized method name for the supplied method
        /// if it's known; otherwise, returns the original.
        /// </summary>
        internal static HttpMethod Normalize(HttpMethod method)
        {
            Debug.Assert(method != null);
            Debug.Assert(!string.IsNullOrEmpty(method._method));

            // _http3Index is only set for the singleton instances, so if it's not null,
            // we can avoid the lookup.  Otherwise, look up the method instance and return the
            // normalized instance if it's found.

            if (method._http3Index is null && method._method.Length >= 3) // 3 == smallest known method
            {
                HttpMethod? match = (method._method[0] | 0x20) switch
                {
                    'c' => s_connectMethod,
                    'd' => s_deleteMethod,
                    'g' => s_getMethod,
                    'h' => s_headMethod,
                    'o' => s_optionsMethod,
                    'p' => method._method.Length switch
                    {
                        3 => s_putMethod,
                        4 => s_postMethod,
                        _ => s_patchMethod,
                    },
                    't' => s_traceMethod,
                    _ => null,
                };

                if (match is not null && string.Equals(method._method, match._method, StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }
            }

            return method;
        }

        internal bool MustHaveRequestBody
        {
            get
            {
                // Normalize before calling this
                Debug.Assert(ReferenceEquals(this, Normalize(this)));

                return !ReferenceEquals(this, HttpMethod.Get) && !ReferenceEquals(this, HttpMethod.Head) && !ReferenceEquals(this, HttpMethod.Connect) &&
                       !ReferenceEquals(this, HttpMethod.Options) && !ReferenceEquals(this, HttpMethod.Delete);
            }
        }
    }
}

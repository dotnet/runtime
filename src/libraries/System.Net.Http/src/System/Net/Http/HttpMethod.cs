// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http
{
    public class HttpMethod : IEquatable<HttpMethod>
    {
        private readonly string _method;
        private int _hashcode;

        private static readonly HttpMethod s_getMethod = new HttpMethod("GET");
        private static readonly HttpMethod s_putMethod = new HttpMethod("PUT");
        private static readonly HttpMethod s_postMethod = new HttpMethod("POST");
        private static readonly HttpMethod s_deleteMethod = new HttpMethod("DELETE");
        private static readonly HttpMethod s_headMethod = new HttpMethod("HEAD");
        private static readonly HttpMethod s_optionsMethod = new HttpMethod("OPTIONS");
        private static readonly HttpMethod s_traceMethod = new HttpMethod("TRACE");

        // Don't expose CONNECT as static property, since it's used by the transport to connect to a proxy.
        // CONNECT is not used by users directly.

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

        public string Method
        {
            get { return _method; }
        }

        public HttpMethod(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                throw new ArgumentException(SR.net_http_argument_empty_string, nameof(method));
            }
            if (HttpRuleParser.GetTokenLength(method, 0) != method.Length)
            {
                throw new FormatException(SR.net_http_httpmethod_format_error);
            }

            _method = method;
        }

        #region IEquatable<HttpMethod> Members

        public bool Equals(HttpMethod other)
        {
            if ((object)other == null)
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

        public override bool Equals(object obj)
        {
            return Equals(obj as HttpMethod);
        }

        public override int GetHashCode()
        {
            if (_hashcode == 0)
            {
                // If _method is already uppercase, _method.GetHashCode() can be
                // used instead of _method.ToUpperInvariant().GetHashCode(),
                // avoiding the unnecessary extra string allocation.
                _hashcode = IsUpperAscii(_method) ?
                    _method.GetHashCode() :
                    _method.ToUpperInvariant().GetHashCode();
            }

            return _hashcode;
        }

        public override string ToString()
        {
            return _method.ToString();
        }

        public static bool operator ==(HttpMethod left, HttpMethod right)
        {
            if ((object)left == null)
            {
                return ((object)right == null);
            }
            else if ((object)right == null)
            {
                return ((object)left == null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(HttpMethod left, HttpMethod right)
        {
            return !(left == right);
        }

        private static bool IsUpperAscii(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(c >= 'A' && c <= 'Z'))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

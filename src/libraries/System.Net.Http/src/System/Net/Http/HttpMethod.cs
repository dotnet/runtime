// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
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
        private static readonly HttpMethod s_patchMethod = new HttpMethod("PATCH");
        private static readonly HttpMethod s_connectMethod = new HttpMethod("CONNECT");

        private static readonly Dictionary<HttpMethod, HttpMethod> s_knownMethods = new Dictionary<HttpMethod, HttpMethod>(9)
        {
            { s_getMethod, s_getMethod },
            { s_putMethod, s_putMethod },
            { s_postMethod, s_postMethod },
            { s_deleteMethod, s_deleteMethod },
            { s_headMethod, s_headMethod },
            { s_optionsMethod, s_optionsMethod },
            { s_traceMethod, s_traceMethod },
            { s_patchMethod, s_patchMethod },
            { s_connectMethod, s_connectMethod },
        };

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

        // Don't expose CONNECT as static property, since it's used by the transport to connect to a proxy.
        // CONNECT is not used by users directly.

        internal static HttpMethod Connect
        {
            get { return s_connectMethod; }
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
                _hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(_method);
            }

            return _hashcode;
        }

        public override string ToString()
        {
            return _method;
        }

        public static bool operator ==(HttpMethod left, HttpMethod right)
        {
            return (object)left == null || (object)right == null ?
                ReferenceEquals(left, right) :
                left.Equals(right);
        }

        public static bool operator !=(HttpMethod left, HttpMethod right)
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
            return s_knownMethods.TryGetValue(method, out HttpMethod normalized) ?
                normalized :
                method;
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

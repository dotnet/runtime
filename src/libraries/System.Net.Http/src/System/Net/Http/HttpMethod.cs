// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http.QPack;

namespace System.Net.Http
{
    public partial class HttpMethod : IEquatable<HttpMethod>
    {
        private readonly string _method;
        private int _hashcode;

        public static HttpMethod Get { get; } = new("GET", H3StaticTable.MethodGet);
        public static HttpMethod Put { get; } = new("PUT", H3StaticTable.MethodPut);
        public static HttpMethod Post { get; } = new("POST", H3StaticTable.MethodPost);
        public static HttpMethod Delete { get; } = new("DELETE", H3StaticTable.MethodDelete);
        public static HttpMethod Head { get; } = new("HEAD", H3StaticTable.MethodHead);
        public static HttpMethod Options { get; } = new("OPTIONS", H3StaticTable.MethodOptions);
        public static HttpMethod Trace { get; } = new("TRACE", http3StaticTableIndex: -1);
        public static HttpMethod Patch { get; } = new("PATCH", http3StaticTableIndex: -1);

        /// <summary>Gets the HTTP CONNECT protocol method.</summary>
        /// <value>The HTTP CONNECT method.</value>
        public static HttpMethod Connect { get; } = new("CONNECT", H3StaticTable.MethodConnect);

        public string Method => _method;

        public HttpMethod(string method)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(method);
            if (!HttpRuleParser.IsToken(method))
            {
                throw new FormatException(SR.net_http_httpmethod_format_error);
            }

            _method = method;
            Initialize(method);
        }

        private HttpMethod(string method, int http3StaticTableIndex)
        {
            _method = method;
            Initialize(http3StaticTableIndex);
        }

        // SocketsHttpHandler-specific implementation has extra init logic.
        partial void Initialize(int http3Index);
        partial void Initialize(string method);

        public bool Equals([NotNullWhen(true)] HttpMethod? other) =>
            other is not null &&
            string.Equals(_method, other._method, StringComparison.OrdinalIgnoreCase);

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is HttpMethod method &&
            Equals(method);

        public override int GetHashCode()
        {
            if (_hashcode == 0)
            {
                _hashcode = StringComparer.OrdinalIgnoreCase.GetHashCode(_method);
            }

            return _hashcode;
        }

        public override string ToString() => _method;

        public static bool operator ==(HttpMethod? left, HttpMethod? right) =>
            left is null || right is null
                ? ReferenceEquals(left, right)
                : left.Equals(right);

        public static bool operator !=(HttpMethod? left, HttpMethod? right) =>
            !(left == right);

        /// <summary>Parses the provided <paramref name="method"/> into an <see cref="HttpMethod"/> instance.</summary>
        /// <param name="method">The method to parse.</param>
        /// <returns>An <see cref="HttpMethod"/> instance for the provided <paramref name="method"/>.</returns>
        /// <remarks>
        /// This method may return a singleton instance for known methods; for example, it may return <see cref="Get"/>
        /// if "GET" is specified. The parsing is performed in a case-insensitive manner, so it may also return <see cref="Get"/>
        /// if "get" is specified. For unknown methods, a new <see cref="HttpMethod"/> instance is returned, with the
        /// same validation being performed as by the <see cref="HttpMethod(string)"/> constructor.
        /// </remarks>
        public static HttpMethod Parse(ReadOnlySpan<char> method) =>
            GetKnownMethod(method) ??
            new HttpMethod(method.ToString());

        internal static HttpMethod? GetKnownMethod(ReadOnlySpan<char> method)
        {
            if (method.Length >= 3) // 3 == smallest known method
            {
                HttpMethod? match = (method[0] | 0x20) switch
                {
                    'c' => Connect,
                    'd' => Delete,
                    'g' => Get,
                    'h' => Head,
                    'o' => Options,
                    'p' => method.Length switch
                    {
                        3 => Put,
                        4 => Post,
                        _ => Patch,
                    },
                    't' => Trace,
                    _ => null,
                };

                if (match is not null &&
                    method.Equals(match._method, StringComparison.OrdinalIgnoreCase))
                {
                    return match;
                }
            }

            return null;
        }
    }
}

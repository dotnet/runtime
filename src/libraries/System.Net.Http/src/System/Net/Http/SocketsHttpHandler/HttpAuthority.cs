// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.Http
{

    internal sealed class HttpAuthority : IEquatable<HttpAuthority>
    {
        // ALPN Protocol Name should also be part of an authority, but we are special-casing for HTTP/3, so this can be assumed to be "H3".
        // public string AlpnProtocolName { get; }

        public string IdnHost { get; }
        public int Port { get; }

        public HttpAuthority(string host, int port)
        {
            Debug.Assert(host != null);

            // This is very rarely called, but could be optimized to avoid the URI-specific stuff by bringing in DomainNameHelpers from System.Private.Uri.
            var builder = new UriBuilder(Uri.UriSchemeHttp, host, port);
            Uri uri = builder.Uri;

            // TODO https://github.com/dotnet/runtime/issues/25782:
            // Uri.IdnHost is missing '[', ']' characters around IPv6 address.
            // So, we need to add them manually for now.
            IdnHost = uri.HostNameType == UriHostNameType.IPv6 ? "[" + uri.IdnHost + "]" : uri.IdnHost;
            Port = port;
        }

        public bool Equals(HttpAuthority? other)
        {
            return other != null && string.Equals(IdnHost, other.IdnHost) && Port == other.Port;
        }

        public override bool Equals(object? obj)
        {
            return obj is HttpAuthority other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IdnHost, Port);
        }

        // For diagnostics
        public override string ToString()
        {
            return IdnHost != null ? $"{IdnHost}:{Port}" : "<empty>";
        }
    }
}

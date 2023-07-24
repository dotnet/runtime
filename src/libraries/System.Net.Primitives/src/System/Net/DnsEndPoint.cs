// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace System.Net
{
    public class DnsEndPoint : EndPoint
    {
        private readonly string _host;
        private readonly int _port;
        private readonly AddressFamily _family;

        public DnsEndPoint(string host, int port) : this(host, port, AddressFamily.Unspecified) { }

        public DnsEndPoint(string host, int port, AddressFamily addressFamily)
        {
            ArgumentException.ThrowIfNullOrEmpty(host);

            ArgumentOutOfRangeException.ThrowIfLessThan(port, IPEndPoint.MinPort);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(port, IPEndPoint.MaxPort);

            if (addressFamily != AddressFamily.InterNetwork &&
                addressFamily != AddressFamily.InterNetworkV6 &&
                addressFamily != AddressFamily.Unspecified)
            {
                throw new ArgumentException(SR.net_sockets_invalid_optionValue_all, nameof(addressFamily));
            }

            _host = host;
            _port = port;
            _family = addressFamily;
        }

        public override bool Equals([NotNullWhen(true)] object? comparand) =>
            comparand is DnsEndPoint dnsComparand &&
            _family == dnsComparand._family &&
            _port == dnsComparand._port &&
            StringComparer.OrdinalIgnoreCase.Equals(_host, dnsComparand._host);

        public override int GetHashCode() =>
            HashCode.Combine(
                (int)_family,
                _port,
                StringComparer.OrdinalIgnoreCase.GetHashCode(_host));

        public override string ToString() => $"{_family}/{_host}:{_port}";

        public string Host => _host;

        public override AddressFamily AddressFamily => _family;

        public int Port => _port;
    }
}

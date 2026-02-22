// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Most of these are available from kstats.
// May implement later.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class SunOSIPGlobalProperties : UnixIPGlobalProperties
    {
        public override TcpConnectionInformation[] GetActiveTcpConnections() => throw new PlatformNotSupportedException();

        public override IPEndPoint[] GetActiveTcpListeners() => throw new PlatformNotSupportedException();

        public override IPEndPoint[] GetActiveUdpListeners() => throw new PlatformNotSupportedException();

        public override IcmpV4Statistics GetIcmpV4Statistics() => throw new PlatformNotSupportedException();

        public override IcmpV6Statistics GetIcmpV6Statistics() => throw new PlatformNotSupportedException();

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
            => new SunOSIPGlobalStatistics(ipv4: true);

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
            => new SunOSIPGlobalStatistics(ipv4: false);

        public override TcpStatistics GetTcpIPv4Statistics() => throw new PlatformNotSupportedException();

        public override TcpStatistics GetTcpIPv6Statistics() => throw new PlatformNotSupportedException();

        public override UdpStatistics GetUdpIPv4Statistics() => throw new PlatformNotSupportedException();

        public override UdpStatistics GetUdpIPv6Statistics() => throw new PlatformNotSupportedException();
    }
}

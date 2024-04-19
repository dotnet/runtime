// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Net.NetworkInformation
{
    internal sealed class BsdIPGlobalProperties : UnixIPGlobalProperties
    {
        private unsafe TcpConnectionInformation[] GetTcpConnections(bool listeners)
        {
            int realCount = Interop.Sys.GetEstimatedTcpConnectionCount();
            int infoCount = realCount * 2;
            Interop.Sys.NativeTcpConnectionInformation[] infos = new Interop.Sys.NativeTcpConnectionInformation[infoCount];
            fixed (Interop.Sys.NativeTcpConnectionInformation* infosPtr = infos)
            {
                if (Interop.Sys.GetActiveTcpConnectionInfos(infosPtr, &infoCount) == -1)
                {
                    throw new NetworkInformationException(SR.net_PInvokeError);
                }
            }

            TcpConnectionInformation[] connectionInformations = new TcpConnectionInformation[infoCount];
            int nextResultIndex = 0;
            for (int i = 0; i < infoCount; i++)
            {
                Interop.Sys.NativeTcpConnectionInformation nativeInfo = infos[i];
                TcpState state = nativeInfo.State;

                if (listeners != (state == TcpState.Listen))
                {
                    continue;
                }

                IPAddress localIPAddress = new IPAddress(new ReadOnlySpan<byte>(nativeInfo.LocalEndPoint.AddressBytes, checked((int)nativeInfo.LocalEndPoint.NumAddressBytes)));
                IPEndPoint local = new IPEndPoint(localIPAddress, (int)nativeInfo.LocalEndPoint.Port);

                IPAddress remoteIPAddress = nativeInfo.RemoteEndPoint.NumAddressBytes == 0 ?
                    IPAddress.Any :
                    new IPAddress(new ReadOnlySpan<byte>(nativeInfo.RemoteEndPoint.AddressBytes, checked((int)nativeInfo.RemoteEndPoint.NumAddressBytes)));

                IPEndPoint remote = new IPEndPoint(remoteIPAddress, (int)nativeInfo.RemoteEndPoint.Port);
                connectionInformations[nextResultIndex++] = new SimpleTcpConnectionInformation(local, remote, state);
            }

            if (nextResultIndex != connectionInformations.Length)
            {
                Array.Resize(ref connectionInformations, nextResultIndex);
            }

            return connectionInformations;
        }

        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            return GetTcpConnections(listeners: false);
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            TcpConnectionInformation[] allConnections = GetTcpConnections(listeners: true);
            var endPoints = new IPEndPoint[allConnections.Length];
            for (int i = 0; i < allConnections.Length; i++)
            {
                endPoints[i] = allConnections[i].LocalEndPoint;
            }
            return endPoints;
        }

        public override unsafe IPEndPoint[] GetActiveUdpListeners()
        {
            int realCount = Interop.Sys.GetEstimatedUdpListenerCount();
            int infoCount = realCount * 2;
            Interop.Sys.IPEndPointInfo[] infos = new Interop.Sys.IPEndPointInfo[infoCount];
            fixed (Interop.Sys.IPEndPointInfo* infosPtr = infos)
            {
                if (Interop.Sys.GetActiveUdpListeners(infosPtr, &infoCount) == -1)
                {
                    throw new NetworkInformationException(SR.net_PInvokeError);
                }
            }

            IPEndPoint[] endPoints = new IPEndPoint[infoCount];
            for (int i = 0; i < infoCount; i++)
            {
                Interop.Sys.IPEndPointInfo endPointInfo = infos[i];
                int port = (int)endPointInfo.Port;
                IPAddress ipAddress = endPointInfo.NumAddressBytes == 0 ?
                    IPAddress.Any :
                    new IPAddress(new ReadOnlySpan<byte>(endPointInfo.AddressBytes, checked((int)endPointInfo.NumAddressBytes)));

                endPoints[i] = new IPEndPoint(ipAddress, port);
            }

            return endPoints;
        }

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            return new BsdIcmpV4Statistics();
        }

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            return new BsdIcmpV6Statistics();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            return new BsdIPv4GlobalStatistics();
        }

        [UnsupportedOSPlatform("osx")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [UnsupportedOSPlatform("freebsd")]
        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            // Although there is a 'net.inet6.ip6.stats' sysctl variable, there
            // is no header for the ip6stat structure and therefore isn't available.
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            // OSX does not provide separated TCP-IPv4 and TCP-IPv6 stats.
            return new BsdTcpStatistics();
        }

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            // OSX does not provide separated TCP-IPv4 and TCP-IPv6 stats.
            return new BsdTcpStatistics();
        }

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            // OSX does not provide separated UDP-IPv4 and UDP-IPv6 stats.
            return new BsdUdpStatistics();
        }

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            // OSX does not provide separated UDP-IPv4 and UDP-IPv6 stats.
            return new BsdUdpStatistics();
        }
    }
}

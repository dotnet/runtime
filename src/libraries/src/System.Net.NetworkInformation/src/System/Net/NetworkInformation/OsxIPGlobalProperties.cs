// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal class OsxIPGlobalProperties : UnixIPGlobalProperties
    {
        public unsafe override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            int realCount = Interop.Sys.GetEstimatedTcpConnectionCount();
            int infoCount = realCount * 2;
            Interop.Sys.NativeTcpConnectionInformation* infos = stackalloc Interop.Sys.NativeTcpConnectionInformation[infoCount];
            if (Interop.Sys.GetActiveTcpConnectionInfos(infos, &infoCount) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            TcpConnectionInformation[] connectionInformations = new TcpConnectionInformation[infoCount];
            for (int i = 0; i < infoCount; i++)
            {
                Interop.Sys.NativeTcpConnectionInformation nativeInfo = infos[i];
                TcpState state = nativeInfo.State;

                byte[] localBytes = new byte[nativeInfo.LocalEndPoint.NumAddressBytes];
                fixed (byte* localBytesPtr = localBytes)
                {
                    Buffer.MemoryCopy(nativeInfo.LocalEndPoint.AddressBytes, localBytesPtr, localBytes.Length, localBytes.Length);
                }
                IPAddress localIPAddress = new IPAddress(localBytes);
                IPEndPoint local = new IPEndPoint(localIPAddress, (int)nativeInfo.LocalEndPoint.Port);

                IPAddress remoteIPAddress;
                if (nativeInfo.RemoteEndPoint.NumAddressBytes == 0)
                {
                    remoteIPAddress = IPAddress.Any;
                }
                else
                {
                    byte[] remoteBytes = new byte[nativeInfo.RemoteEndPoint.NumAddressBytes];
                    fixed (byte* remoteBytesPtr = &remoteBytes[0])
                    {
                        Buffer.MemoryCopy(nativeInfo.RemoteEndPoint.AddressBytes, remoteBytesPtr, remoteBytes.Length, remoteBytes.Length);
                    }
                    remoteIPAddress = new IPAddress(remoteBytes);
                }

                IPEndPoint remote = new IPEndPoint(remoteIPAddress, (int)nativeInfo.RemoteEndPoint.Port);
                connectionInformations[i] = new SimpleTcpConnectionInformation(local, remote, state);
            }

            return connectionInformations;
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            TcpConnectionInformation[] allConnections = GetActiveTcpConnections();
            return allConnections.Where(tci => tci.State != TcpState.Listen).Select(tci => tci.RemoteEndPoint).ToArray();
        }

        public unsafe override IPEndPoint[] GetActiveUdpListeners()
        {
            int realCount = Interop.Sys.GetEstimatedUdpListenerCount();
            int infoCount = realCount * 2;
            Interop.Sys.IPEndPointInfo* infos = stackalloc Interop.Sys.IPEndPointInfo[infoCount];
            if (Interop.Sys.GetActiveUdpListeners(infos, &infoCount) == -1)
            {
                throw new NetworkInformationException(SR.net_PInvokeError);
            }

            IPEndPoint[] endPoints = new IPEndPoint[infoCount];
            for (int i = 0; i < infoCount; i++)
            {
                Interop.Sys.IPEndPointInfo endPointInfo = infos[i];
                int port = (int)endPointInfo.Port;
                IPAddress ipAddress;
                if (endPointInfo.NumAddressBytes == 0)
                {
                    ipAddress = IPAddress.Any;
                }
                else
                {
                    byte[] bytes = new byte[endPointInfo.NumAddressBytes];
                    fixed (byte* bytesPtr = &bytes[0])
                    {
                        Buffer.MemoryCopy(endPointInfo.AddressBytes, bytesPtr, bytes.Length, bytes.Length);
                    }
                    ipAddress = new IPAddress(bytes);
                }

                endPoints[i] = new IPEndPoint(ipAddress, port);
            }

            return endPoints;
        }

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            return new OsxIcmpV4Statistics();
        }

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            return new OsxIcmpV6Statistics();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            return new OsxIPv4GlobalStatistics();
        }

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            // Although there is a 'net.inet6.ip6.stats' sysctl variable, there
            // is no header for the ip6stat structure and therefore isn't available.
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            // OSX does not provide separated TCP-IPv4 and TCP-IPv6 stats.
            throw new PlatformNotSupportedException(SR.net_InformationUnavailableOnPlatform);
        }

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            // OSX does not provide separated TCP-IPv4 and TCP-IPv6 stats.
            return new OsxTcpStatistics();
        }

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            // OSX does not provide separated UDP-IPv4 and UDP-IPv6 stats.
            return new OsxUdpStatistics();
        }

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            // OSX does not provide separated UDP-IPv4 and UDP-IPv6 stats.
            return new OsxUdpStatistics();
        }
    }
}

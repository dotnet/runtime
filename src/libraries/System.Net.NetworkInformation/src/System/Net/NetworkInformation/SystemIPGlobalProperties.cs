// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal sealed class SystemIPGlobalProperties : IPGlobalProperties
    {
        internal SystemIPGlobalProperties()
        {
        }

        /// Specifies the host name for the local computer.
        public override string HostName
        {
            get
            {
                return HostInformationPal.FixedInfo.hostName;
            }
        }

        /// Specifies the domain in which the local computer is registered.
        public override string DomainName
        {
            get
            {
                return HostInformationPal.FixedInfo.domainName;
            }
        }

        ///
        /// The type of node.
        ///
        /// <remarks>
        /// The exact mechanism by which NetBIOS names are resolved to IP addresses
        /// depends on the node's configured NetBIOS Node Type. Broadcast - uses broadcast
        /// NetBIOS Name Queries for name registration and resolution.
        /// PeerToPeer - uses a NetBIOS name server (NBNS), such as Windows Internet
        /// Name Service (WINS), to resolve NetBIOS names.
        /// Mixed - uses Broadcast then PeerToPeer.
        /// Hybrid - uses PeerToPeer then Broadcast.
        /// </remarks>
        public override NetBiosNodeType NodeType
        {
            get
            {
                return (NetBiosNodeType)HostInformationPal.FixedInfo.nodeType;
            }
        }

        /// Specifies the DHCP scope name.
        public override string DhcpScopeName
        {
            get
            {
                return HostInformationPal.FixedInfo.scopeId;
            }
        }

        /// Specifies whether the local computer is acting as an WINS proxy.
        public override bool IsWinsProxy
        {
            get
            {
                return (HostInformationPal.FixedInfo.enableProxy);
            }
        }

        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            List<TcpConnectionInformation> list = new List<TcpConnectionInformation>();
            List<SystemTcpConnectionInformation> connections = GetAllTcpConnections();
            foreach (TcpConnectionInformation connection in connections)
            {
                if (connection.State != TcpState.Listen)
                {
                    list.Add(connection);
                }
            }

            return list.ToArray();
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            List<IPEndPoint> list = new List<IPEndPoint>();
            List<SystemTcpConnectionInformation> connections = GetAllTcpConnections();
            foreach (TcpConnectionInformation connection in connections)
            {
                if (connection.State == TcpState.Listen)
                {
                    list.Add(connection.LocalEndPoint);
                }
            }

            return list.ToArray();
        }

        ///
        /// Gets the active TCP connections. Uses the native GetTcpTable API.
        private unsafe List<SystemTcpConnectionInformation> GetAllTcpConnections()
        {
            uint size = 0;
            uint result = 0;
            List<SystemTcpConnectionInformation> tcpConnections = new List<SystemTcpConnectionInformation>();

            // Check if it supports IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetTcpTable(IntPtr.Zero, ref size, true);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetTcpTable(buffer, ref size, true);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            // The table info just gives us the number of rows.
                            ref readonly Interop.IpHlpApi.MibTcpTable tcpTableInfo = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcpTable>(span);

                            if (tcpTableInfo.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                span = span.Slice(sizeof(Interop.IpHlpApi.MibTcpTable));

                                for (int i = 0; i < tcpTableInfo.numberOfEntries; i++)
                                {
                                    tcpConnections.Add(new SystemTcpConnectionInformation(in MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcpRow>(span)));
                                    span = span.Slice(sizeof(Interop.IpHlpApi.MibTcpRow));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                // If we don't have any ipv4 interfaces detected, just continue.
                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            if (Socket.OSSupportsIPv6)
            {
                // Get the buffer size needed.
                size = 0;
                result = Interop.IpHlpApi.GetExtendedTcpTable(IntPtr.Zero, ref size, true,
                                                                        (uint)AddressFamily.InterNetworkV6,
                                                                        Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedTcpTable(buffer, ref size, true,
                                                                                (uint)AddressFamily.InterNetworkV6,
                                                                                Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            // The table info just gives us the number of rows.
                            ref readonly Interop.IpHlpApi.MibTcp6TableOwnerPid tcpTable6OwnerPid = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcp6TableOwnerPid>(span);

                            if (tcpTable6OwnerPid.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                span = span.Slice(sizeof(Interop.IpHlpApi.MibTcp6TableOwnerPid));

                                for (int i = 0; i < tcpTable6OwnerPid.numberOfEntries; i++)
                                {
                                    tcpConnections.Add(new SystemTcpConnectionInformation(in MemoryMarshal.AsRef<Interop.IpHlpApi.MibTcp6RowOwnerPid>(span)));

                                    // We increment the pointer to the next row.
                                    span = span.Slice(sizeof(Interop.IpHlpApi.MibTcp6RowOwnerPid));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                // If we don't have any ipv6 interfaces detected, just continue.
                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            return tcpConnections;
        }

        /// Gets the active UDP listeners. Uses the native GetUdpTable API.
        public unsafe override IPEndPoint[] GetActiveUdpListeners()
        {
            uint size = 0;
            uint result = 0;
            List<IPEndPoint> udpListeners = new List<IPEndPoint>();

            // Check if it support IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetUdpTable(IntPtr.Zero, ref size, true);
                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);

                    try
                    {
                        result = Interop.IpHlpApi.GetUdpTable(buffer, ref size, true);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            // The table info just gives us the number of rows.
                            ref readonly Interop.IpHlpApi.MibUdpTable udpTableInfo = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibUdpTable>(span);

                            if (udpTableInfo.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                span = span.Slice(sizeof(Interop.IpHlpApi.MibUdpTable));

                                for (int i = 0; i < udpTableInfo.numberOfEntries; i++)
                                {
                                    ref readonly Interop.IpHlpApi.MibUdpRow udpRow = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibUdpRow>(span);
                                    int localPort = udpRow.localPort1 << 8 | udpRow.localPort2;

                                    udpListeners.Add(new IPEndPoint(udpRow.localAddr, (int)localPort));

                                    // We increment the pointer to the next row.
                                    span = span.Slice(sizeof(Interop.IpHlpApi.MibUdpRow));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }

                // If we don't have any ipv4 interfaces detected, just continue.
                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            if (Socket.OSSupportsIPv6)
            {
                // Get the buffer size needed.
                size = 0;
                result = Interop.IpHlpApi.GetExtendedUdpTable(IntPtr.Zero, ref size, true,
                                                                        (uint)AddressFamily.InterNetworkV6,
                                                                        Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);
                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedUdpTable(buffer, ref size, true,
                                                                                (uint)AddressFamily.InterNetworkV6,
                                                                                Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var span = new ReadOnlySpan<byte>((byte*)buffer, (int)size);

                            // The table info just gives us the number of rows.
                            ref readonly Interop.IpHlpApi.MibUdp6TableOwnerPid udp6TableOwnerPid = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibUdp6TableOwnerPid>(span);

                            if (udp6TableOwnerPid.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                span = span.Slice(sizeof(Interop.IpHlpApi.MibUdp6TableOwnerPid));

                                for (int i = 0; i < udp6TableOwnerPid.numberOfEntries; i++)
                                {
                                    ref readonly Interop.IpHlpApi.MibUdp6RowOwnerPid udp6RowOwnerPid = ref MemoryMarshal.AsRef<Interop.IpHlpApi.MibUdp6RowOwnerPid>(span);
                                    int localPort = udp6RowOwnerPid.localPort1 << 8 | udp6RowOwnerPid.localPort2;

                                    udpListeners.Add(new IPEndPoint(new IPAddress(udp6RowOwnerPid.localAddrAsSpan,
                                        udp6RowOwnerPid.localScopeId), localPort));

                                    // We increment the pointer to the next row.
                                    span = span.Slice(sizeof(Interop.IpHlpApi.MibUdp6RowOwnerPid));
                                }
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                // If we don't have any ipv6 interfaces detected, just continue.
                if (result != Interop.IpHlpApi.ERROR_SUCCESS && result != Interop.IpHlpApi.ERROR_NO_DATA)
                {
                    throw new NetworkInformationException((int)result);
                }
            }

            return udpListeners.ToArray();
        }

        public override IPGlobalStatistics GetIPv4GlobalStatistics()
        {
            return new SystemIPGlobalStatistics(AddressFamily.InterNetwork);
        }

        public override IPGlobalStatistics GetIPv6GlobalStatistics()
        {
            return new SystemIPGlobalStatistics(AddressFamily.InterNetworkV6);
        }

        public override TcpStatistics GetTcpIPv4Statistics()
        {
            return new SystemTcpStatistics(AddressFamily.InterNetwork);
        }

        public override TcpStatistics GetTcpIPv6Statistics()
        {
            return new SystemTcpStatistics(AddressFamily.InterNetworkV6);
        }

        public override UdpStatistics GetUdpIPv4Statistics()
        {
            return new SystemUdpStatistics(AddressFamily.InterNetwork);
        }

        public override UdpStatistics GetUdpIPv6Statistics()
        {
            return new SystemUdpStatistics(AddressFamily.InterNetworkV6);
        }

        public override IcmpV4Statistics GetIcmpV4Statistics()
        {
            return new SystemIcmpV4Statistics();
        }

        public override IcmpV6Statistics GetIcmpV6Statistics()
        {
            return new SystemIcmpV6Statistics();
        }

        public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback? callback, object? state) =>
            TaskToApm.Begin(GetUnicastAddressesAsync(), callback, state);

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult) =>
            TaskToApm.End<UnicastIPAddressInformationCollection>(asyncResult);

        public override UnicastIPAddressInformationCollection GetUnicastAddresses() =>
            GetUnicastAddressesAsync().GetAwaiter().GetResult();

        public override async Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            // Wait for the address table to stabilize.
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!TeredoHelper.UnsafeNotifyStableUnicastIpAddressTable(s => ((TaskCompletionSource<bool>)s).TrySetResult(true), tcs))
            {
                await tcs.Task.ConfigureAwait(false);
            }

            // Get the address table.
            var addresses = new UnicastIPAddressInformationCollection();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (UnicastIPAddressInformation address in ni.GetIPProperties().UnicastAddresses)
                {
                    if (!addresses.Contains(address))
                    {
                        addresses.InternalAdd(address);
                    }
                }
            }

            return addresses;
        }
    }
}

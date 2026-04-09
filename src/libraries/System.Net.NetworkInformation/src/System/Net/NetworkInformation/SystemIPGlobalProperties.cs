// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
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
                return HostInformationPal.GetHostName();
            }
        }

        /// Specifies the domain in which the local computer is registered.
        public override string DomainName
        {
            get
            {
                return HostInformationPal.GetDomainName();
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
                return (NetBiosNodeType)HostInformationPal.GetNodeType();
            }
        }

        /// Specifies the DHCP scope name.
        public override string DhcpScopeName
        {
            get
            {
                return HostInformationPal.GetScopeId();
            }
        }

        /// Specifies whether the local computer is acting as an WINS proxy.
        public override bool IsWinsProxy
        {
            get
            {
                return HostInformationPal.GetEnableProxy();
            }
        }

        public override TcpConnectionInformation[] GetActiveTcpConnections()
        {
            List<TcpConnectionInformation> list = new List<TcpConnectionInformation>();
            GetAllTcpConnections(list, null);
            return list.ToArray();
        }

        public override IPEndPoint[] GetActiveTcpListeners()
        {
            List<IPEndPoint> list = new List<IPEndPoint>();
            GetAllTcpConnections(null, list);
            return list.ToArray();
        }

        /// Gets the active TCP connections. Uses the native GetExtendedTcpTable API.
        private static unsafe void GetAllTcpConnections(List<TcpConnectionInformation>? connections, List<IPEndPoint>? listening)
        {
            uint size = 0;
            uint result;

            // Check if it supports IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetExtendedTcpTable(0, &size, order: true, (uint)AddressFamily.InterNetwork,
                    connections is null ? Interop.IpHlpApi.TcpTableClass.TcpTableBasicListener : Interop.IpHlpApi.TcpTableClass.TcpTableBasicAll, 0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedTcpTable(buffer, &size, order: true, (uint)AddressFamily.InterNetwork,
                            connections is null ? Interop.IpHlpApi.TcpTableClass.TcpTableBasicListener : Interop.IpHlpApi.TcpTableClass.TcpTableBasicAll, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var table = (Interop.IpHlpApi.MibTcpTable*)buffer;
                            if (table->NumEntries > 0)
                            {
                                var span = new ReadOnlySpan<Interop.IpHlpApi.MibTcpRow>(&table->FirstEntry, (int)table->NumEntries);
                                Debug.Assert(sizeof(uint) + sizeof(Interop.IpHlpApi.MibTcpRow) * span.Length <= size);
                                foreach (ref readonly Interop.IpHlpApi.MibTcpRow entry in span)
                                {
                                    if (entry.State == TcpState.Listen)
                                    {
                                        listening?.Add(entry.LocalEndPoint);
                                    }
                                    else
                                    {
                                        connections?.Add(new SystemTcpConnectionInformation(in entry));
                                    }
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
                result = Interop.IpHlpApi.GetExtendedTcpTable(0, &size, order: true, (uint)AddressFamily.InterNetworkV6,
                    connections is null ? Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidListener : Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedTcpTable(buffer, &size, order: true, (uint)AddressFamily.InterNetworkV6,
                            connections is null ? Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidListener : Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var table = (Interop.IpHlpApi.MibTcp6TableOwnerPid*)buffer;
                            if (table->NumEntries > 0)
                            {
                                var span = new ReadOnlySpan<Interop.IpHlpApi.MibTcp6RowOwnerPid>(&table->FirstEntry, (int)table->NumEntries);
                                Debug.Assert(sizeof(uint) + sizeof(Interop.IpHlpApi.MibTcp6RowOwnerPid) * span.Length <= size);
                                foreach (ref readonly Interop.IpHlpApi.MibTcp6RowOwnerPid entry in span)
                                {
                                    if (entry.State == TcpState.Listen)
                                    {
                                        listening?.Add(entry.LocalEndPoint);
                                    }
                                    else
                                    {
                                        connections?.Add(new SystemTcpConnectionInformation(in entry));
                                    }
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
        }

        /// Gets the active UDP listeners. Uses the native GetExtendedUdpTable API.
        public override unsafe IPEndPoint[] GetActiveUdpListeners()
        {
            uint size = 0;
            uint result;
            List<IPEndPoint> udpListeners = new List<IPEndPoint>();

            // Check if it support IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetExtendedUdpTable(0, &size, order: true, (uint)AddressFamily.InterNetwork,
                    Interop.IpHlpApi.UdpTableClass.UdpTableBasic, 0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedUdpTable(buffer, &size, order: true, (uint)AddressFamily.InterNetwork,
                            Interop.IpHlpApi.UdpTableClass.UdpTableBasic, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var table = (Interop.IpHlpApi.MibUdpTable*)buffer;
                            if (table->NumEntries > 0)
                            {
                                var span = new ReadOnlySpan<Interop.IpHlpApi.MibUdpRow>(&table->FirstEntry, (int)table->NumEntries);
                                Debug.Assert(sizeof(uint) + sizeof(Interop.IpHlpApi.MibUdpRow) * span.Length <= size);
                                foreach (ref readonly Interop.IpHlpApi.MibUdpRow entry in span)
                                {
                                    udpListeners.Add(entry.LocalEndPoint);
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
                result = Interop.IpHlpApi.GetExtendedUdpTable(IntPtr.Zero, &size, order: true,
                                                                        (uint)AddressFamily.InterNetworkV6,
                                                                        Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);
                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    IntPtr buffer = Marshal.AllocHGlobal((int)size);
                    try
                    {
                        result = Interop.IpHlpApi.GetExtendedUdpTable(buffer, &size, order: true,
                                                                                (uint)AddressFamily.InterNetworkV6,
                                                                                Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            var table = (Interop.IpHlpApi.MibUdp6TableOwnerPid*)buffer;
                            if (table->NumEntries > 0)
                            {
                                var span = new ReadOnlySpan<Interop.IpHlpApi.MibUdp6RowOwnerPid>(&table->FirstEntry, (int)table->NumEntries);
                                Debug.Assert(sizeof(uint) + sizeof(Interop.IpHlpApi.MibUdp6RowOwnerPid) * span.Length <= size);
                                foreach (ref readonly Interop.IpHlpApi.MibUdp6RowOwnerPid entry in span)
                                {
                                    udpListeners.Add(entry.LocalEndPoint);
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
            TaskToAsyncResult.Begin(GetUnicastAddressesAsync(), callback, state);

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult) =>
            TaskToAsyncResult.End<UnicastIPAddressInformationCollection>(asyncResult);

        public override UnicastIPAddressInformationCollection GetUnicastAddresses() =>
            GetUnicastAddressesAsync().GetAwaiter().GetResult();

        public override async Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            // Wait for the address table to stabilize.
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            if (!TeredoHelper.UnsafeNotifyStableUnicastIpAddressTable(s => ((TaskCompletionSource)s).TrySetResult(), tcs))
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

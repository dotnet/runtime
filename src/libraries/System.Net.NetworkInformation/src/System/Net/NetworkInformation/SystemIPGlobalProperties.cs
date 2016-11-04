// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.NetworkInformation
{
    internal class SystemIPGlobalProperties : IPGlobalProperties
    {
        internal SystemIPGlobalProperties()
        {
        }

        internal Interop.IpHlpApi.FIXED_INFO FixedInfo
        {
            get
            {
                return HostInformationPal.GetFixedInfo();
            }
        }

        /// Specifies the host name for the local computer.
        public override string HostName
        {
            get
            {
                return FixedInfo.hostName;
            }
        }

        /// Specifies the domain in which the local computer is registered.
        public override string DomainName
        {
            get
            {
                return FixedInfo.domainName;
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
                return (NetBiosNodeType)FixedInfo.nodeType;
            }
        }

        /// Specifies the DHCP scope name.
        public override string DhcpScopeName
        {
            get
            {
                return FixedInfo.scopeId;
            }
        }

        /// Specifies whether the local computer is acting as an WINS proxy.
        public override bool IsWinsProxy
        {
            get
            {
                return (FixedInfo.enableProxy);
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
        private List<SystemTcpConnectionInformation> GetAllTcpConnections()
        {
            uint size = 0;
            uint result = 0;
            SafeLocalAllocHandle buffer = null;
            List<SystemTcpConnectionInformation> tcpConnections = new List<SystemTcpConnectionInformation>();

            // Check if it supports IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetTcpTable(SafeLocalAllocHandle.Zero, ref size, true);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    using (buffer = SafeLocalAllocHandle.LocalAlloc((int)size))
                    {
                        result = Interop.IpHlpApi.GetTcpTable(buffer, ref size, true);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            // The table info just gives us the number of rows.
                            IntPtr newPtr = buffer.DangerousGetHandle();
                            Interop.IpHlpApi.MibTcpTable tcpTableInfo = Marshal.PtrToStructure<Interop.IpHlpApi.MibTcpTable>(newPtr);

                            if (tcpTableInfo.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpTableInfo.numberOfEntries));

                                for (int i = 0; i < tcpTableInfo.numberOfEntries; i++)
                                {
                                    Interop.IpHlpApi.MibTcpRow tcpRow = Marshal.PtrToStructure<Interop.IpHlpApi.MibTcpRow>(newPtr);
                                    tcpConnections.Add(new SystemTcpConnectionInformation(tcpRow));

                                    // Increment the pointer to the next row.
                                    newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpRow));
                                }
                            }
                        }
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
                result = Interop.IpHlpApi.GetExtendedTcpTable(SafeLocalAllocHandle.Zero, ref size, true,
                                                                        (uint)AddressFamily.InterNetworkV6,
                                                                        Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);

                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the TCP table.
                    using (buffer = SafeLocalAllocHandle.LocalAlloc((int)size))
                    {
                        result = Interop.IpHlpApi.GetExtendedTcpTable(buffer, ref size, true,
                                                                                (uint)AddressFamily.InterNetworkV6,
                                                                                Interop.IpHlpApi.TcpTableClass.TcpTableOwnerPidAll, 0);
                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            // The table info just gives us the number of rows.
                            IntPtr newPtr = buffer.DangerousGetHandle();

                            Interop.IpHlpApi.MibTcp6TableOwnerPid tcpTable6OwnerPid = Marshal.PtrToStructure<Interop.IpHlpApi.MibTcp6TableOwnerPid>(newPtr);

                            if (tcpTable6OwnerPid.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcpTable6OwnerPid.numberOfEntries));

                                for (int i = 0; i < tcpTable6OwnerPid.numberOfEntries; i++)
                                {
                                    Interop.IpHlpApi.MibTcp6RowOwnerPid tcp6RowOwnerPid = Marshal.PtrToStructure<Interop.IpHlpApi.MibTcp6RowOwnerPid>(newPtr);
                                    tcpConnections.Add(new SystemTcpConnectionInformation(tcp6RowOwnerPid));

                                    // We increment the pointer to the next row.
                                    newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(tcp6RowOwnerPid));
                                }
                            }
                        }
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
        public override IPEndPoint[] GetActiveUdpListeners()
        {
            uint size = 0;
            uint result = 0;
            SafeLocalAllocHandle buffer = null;
            List<IPEndPoint> udpListeners = new List<IPEndPoint>();

            // Check if it support IPv4 for IPv6 only modes.
            if (Socket.OSSupportsIPv4)
            {
                // Get the buffer size needed.
                result = Interop.IpHlpApi.GetUdpTable(SafeLocalAllocHandle.Zero, ref size, true);
                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    using (buffer = SafeLocalAllocHandle.LocalAlloc((int)size))
                    {
                        result = Interop.IpHlpApi.GetUdpTable(buffer, ref size, true);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            // The table info just gives us the number of rows.
                            IntPtr newPtr = buffer.DangerousGetHandle();
                            Interop.IpHlpApi.MibUdpTable udpTableInfo = Marshal.PtrToStructure<Interop.IpHlpApi.MibUdpTable>(newPtr);

                            if (udpTableInfo.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpTableInfo.numberOfEntries));
                                for (int i = 0; i < udpTableInfo.numberOfEntries; i++)
                                {
                                    Interop.IpHlpApi.MibUdpRow udpRow = Marshal.PtrToStructure<Interop.IpHlpApi.MibUdpRow>(newPtr);
                                    int localPort = udpRow.localPort1 << 8 | udpRow.localPort2;

                                    udpListeners.Add(new IPEndPoint(udpRow.localAddr, (int)localPort));

                                    // We increment the pointer to the next row.
                                    newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udpRow));
                                }
                            }
                        }
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
                result = Interop.IpHlpApi.GetExtendedUdpTable(SafeLocalAllocHandle.Zero, ref size, true,
                                                                        (uint)AddressFamily.InterNetworkV6,
                                                                        Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);
                while (result == Interop.IpHlpApi.ERROR_INSUFFICIENT_BUFFER)
                {
                    // Allocate the buffer and get the UDP table.
                    using (buffer = SafeLocalAllocHandle.LocalAlloc((int)size))
                    {
                        result = Interop.IpHlpApi.GetExtendedUdpTable(buffer, ref size, true,
                                                                                (uint)AddressFamily.InterNetworkV6,
                                                                                Interop.IpHlpApi.UdpTableClass.UdpTableOwnerPid, 0);

                        if (result == Interop.IpHlpApi.ERROR_SUCCESS)
                        {
                            // The table info just gives us the number of rows.
                            IntPtr newPtr = buffer.DangerousGetHandle();
                            Interop.IpHlpApi.MibUdp6TableOwnerPid udp6TableOwnerPid = Marshal.PtrToStructure<Interop.IpHlpApi.MibUdp6TableOwnerPid>(newPtr);

                            if (udp6TableOwnerPid.numberOfEntries > 0)
                            {
                                // Skip over the tableinfo to get the inline rows.
                                newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udp6TableOwnerPid.numberOfEntries));
                                for (int i = 0; i < udp6TableOwnerPid.numberOfEntries; i++)
                                {
                                    Interop.IpHlpApi.MibUdp6RowOwnerPid udp6RowOwnerPid = Marshal.PtrToStructure<Interop.IpHlpApi.MibUdp6RowOwnerPid>(newPtr);
                                    int localPort = udp6RowOwnerPid.localPort1 << 8 | udp6RowOwnerPid.localPort2;

                                    udpListeners.Add(new IPEndPoint(new IPAddress(udp6RowOwnerPid.localAddr,
                                        udp6RowOwnerPid.localScopeId), localPort));

                                    // We increment the pointer to the next row.
                                    newPtr = (IntPtr)((long)newPtr + Marshal.SizeOf(udp6RowOwnerPid));
                                }
                            }
                        }
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

        public override IAsyncResult BeginGetUnicastAddresses(AsyncCallback callback, object state)
        {
            ContextAwareResult asyncResult = new ContextAwareResult(false, false, this, state, callback);
            asyncResult.StartPostingAsyncOp(false);
            if (TeredoHelper.UnsafeNotifyStableUnicastIpAddressTable(StableUnicastAddressTableCallback, asyncResult))
            {
                asyncResult.InvokeCallback();
            }

            asyncResult.FinishPostingAsyncOp();

            return asyncResult;
        }

        public override UnicastIPAddressInformationCollection EndGetUnicastAddresses(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException(nameof(asyncResult));
            }

            ContextAwareResult result = asyncResult as ContextAwareResult;
            if (result == null || result.AsyncObject == null || result.AsyncObject.GetType() != typeof(SystemIPGlobalProperties))
            {
                throw new ArgumentException(SR.net_io_invalidasyncresult);
            }

            if (result.EndCalled)
            {
                throw new InvalidOperationException(SR.Format(SR.net_io_invalidendcall, "EndGetStableUnicastAddresses"));
            }

            result.InternalWaitForCompletion();

            result.EndCalled = true;
            return GetUnicastAddressTable();
        }

        public override UnicastIPAddressInformationCollection GetUnicastAddresses()
        {
            // Wait for the Address Table to stabilize
            using (ManualResetEvent stable = new ManualResetEvent(false))
            {
                if (!TeredoHelper.UnsafeNotifyStableUnicastIpAddressTable(StableUnicastAddressTableCallback, stable))
                {
                    stable.WaitOne();
                }
            }

            return GetUnicastAddressTable();
        }

        public override Task<UnicastIPAddressInformationCollection> GetUnicastAddressesAsync()
        {
            return Task<UnicastIPAddressInformationCollection>.Factory.FromAsync(BeginGetUnicastAddresses, EndGetUnicastAddresses, null);
        }

        private static void StableUnicastAddressTableCallback(object param)
        {
            EventWaitHandle handle = param as EventWaitHandle;
            if (handle != null)
            {
                handle.Set();
            }
            else
            {
                LazyAsyncResult asyncResult = (LazyAsyncResult)param;
                asyncResult.InvokeCallback();
            }
        }

        private static UnicastIPAddressInformationCollection GetUnicastAddressTable()
        {
            UnicastIPAddressInformationCollection rval = new UnicastIPAddressInformationCollection();

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            for (int i = 0; i < interfaces.Length; ++i)
            {
                UnicastIPAddressInformationCollection addresses = interfaces[i].GetIPProperties().UnicastAddresses;

                foreach (UnicastIPAddressInformation address in addresses)
                {
                    if (!rval.Contains(address))
                    {
                        rval.InternalAdd(address);
                    }
                }
            }

            return rval;
        }
    }
}

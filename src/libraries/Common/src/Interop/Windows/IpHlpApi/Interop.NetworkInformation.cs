// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class IpHlpApi
    {
        [Flags]
        internal enum AdapterFlags
        {
            DnsEnabled = 0x01,
            RegisterAdapterSuffix = 0x02,
            DhcpEnabled = 0x04,
            ReceiveOnly = 0x08,
            NoMulticast = 0x10,
            Ipv6OtherStatefulConfig = 0x20,
            NetBiosOverTcp = 0x40,
            IPv4Enabled = 0x80,
            IPv6Enabled = 0x100,
            IPv6ManagedAddressConfigurationSupported = 0x200,
        }

        [Flags]
        internal enum AdapterAddressFlags
        {
            DnsEligible = 0x1,
            Transient = 0x2
        }

        [Flags]
        internal enum GetAdaptersAddressesFlags
        {
            SkipUnicast = 0x0001,
            SkipAnycast = 0x0002,
            SkipMulticast = 0x0004,
            SkipDnsServer = 0x0008,
            IncludePrefix = 0x0010,
            SkipFriendlyName = 0x0020,
            IncludeWins = 0x0040,
            IncludeGateways = 0x0080,
            IncludeAllInterfaces = 0x0100,
            IncludeAllCompartments = 0x0200,
            IncludeTunnelBindingOrder = 0x0400,
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IpSocketAddress
        {
            internal IntPtr address;
            internal int addressLength;

            internal IPAddress MarshalIPAddress()
            {
                return IPEndPointExtensions.GetIPAddress(new Span<byte>((void*)address, addressLength));
            }
        }

        // IP_ADAPTER_ANYCAST_ADDRESS
        // IP_ADAPTER_MULTICAST_ADDRESS
        // IP_ADAPTER_DNS_SERVER_ADDRESS
        // IP_ADAPTER_WINS_SERVER_ADDRESS
        // IP_ADAPTER_GATEWAY_ADDRESS
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IpAdapterAddress
        {
            internal uint length;
            internal AdapterAddressFlags flags;
            internal IpAdapterAddress* next;
            internal IpSocketAddress address;

            internal static InternalIPAddressCollection MarshalIpAddressCollection(IntPtr ptr)
            {
                InternalIPAddressCollection addressList = new InternalIPAddressCollection();

                IpAdapterAddress* pIpAdapterAddress = (IpAdapterAddress*)ptr;
                while (pIpAdapterAddress != null)
                {
                    addressList.InternalAdd(pIpAdapterAddress->address.MarshalIPAddress());
                    pIpAdapterAddress = pIpAdapterAddress->next;
                }

                return addressList;
            }

            internal static IPAddressInformationCollection MarshalIpAddressInformationCollection(IntPtr ptr)
            {
                IPAddressInformationCollection addressList = new IPAddressInformationCollection();

                IpAdapterAddress* pIpAdapterAddress = (IpAdapterAddress*)ptr;
                while (pIpAdapterAddress != null)
                {
                    addressList.InternalAdd(new SystemIPAddressInformation(
                        pIpAdapterAddress->address.MarshalIPAddress(), pIpAdapterAddress->flags));
                    pIpAdapterAddress = pIpAdapterAddress->next;
                }

                return addressList;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IpAdapterUnicastAddress
        {
            internal uint length;
            internal AdapterAddressFlags flags;
            internal IpAdapterUnicastAddress* next;
            internal IpSocketAddress address;
            internal PrefixOrigin prefixOrigin;
            internal SuffixOrigin suffixOrigin;
            internal DuplicateAddressDetectionState dadState;
            internal uint validLifetime;
            internal uint preferredLifetime;
            internal uint leaseLifetime;
            internal byte prefixLength;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IpAdapterAddresses
        {
            internal const int MAX_ADAPTER_ADDRESS_LENGTH = 8;

            internal uint length;
            internal uint index;
            internal IpAdapterAddresses* next;

            private IntPtr _adapterName; // ANSI string
            internal string AdapterName => Marshal.PtrToStringAnsi(_adapterName)!;

            internal IntPtr firstUnicastAddress;
            internal IntPtr firstAnycastAddress;
            internal IntPtr firstMulticastAddress;
            internal IntPtr firstDnsServerAddress;

            private IntPtr _dnsSuffix;
            internal string DnsSuffix => Marshal.PtrToStringUni(_dnsSuffix)!;

            private IntPtr _description;
            internal string Description => Marshal.PtrToStringUni(_description)!;

            private IntPtr _friendlyName;
            internal string FriendlyName => Marshal.PtrToStringUni(_friendlyName)!;

            private fixed byte _address[MAX_ADAPTER_ADDRESS_LENGTH];
            private uint _addressLength;
            internal byte[] Address => MemoryMarshal.CreateReadOnlySpan<byte>(ref _address[0], (int)_addressLength).ToArray();

            internal AdapterFlags flags;
            internal uint mtu;
            internal NetworkInterfaceType type;
            internal OperationalStatus operStatus;
            internal uint ipv6Index;

            private fixed uint _zoneIndices[16];
            internal uint[] ZoneIndices => MemoryMarshal.CreateReadOnlySpan<uint>(ref _zoneIndices[0], 16).ToArray();

            internal IntPtr firstPrefix;

            internal ulong transmitLinkSpeed;
            internal ulong receiveLinkSpeed;
            internal IntPtr firstWinsServerAddress;
            internal IntPtr firstGatewayAddress;
            internal uint ipv4Metric;
            internal uint ipv6Metric;
            internal ulong luid;
            internal IpSocketAddress dhcpv4Server;
            internal uint compartmentId;
            internal fixed byte networkGuid[16];
            internal InterfaceConnectionType connectionType;
            internal InterfaceTunnelType tunnelType;
            internal IpSocketAddress dhcpv6Server; // Never available in Windows.
            internal fixed byte dhcpv6ClientDuid[130];
            internal uint dhcpv6ClientDuidLength;
            internal uint dhcpV6Iaid;

            /* Windows 2008 +
                  PIP_ADAPTER_DNS_SUFFIX             FirstDnsSuffix;
             * */
        }

        internal enum InterfaceConnectionType : int
        {
            Dedicated = 1,
            Passive = 2,
            Demand = 3,
            Maximum = 4,
        }

        internal enum InterfaceTunnelType : int
        {
            None = 0,
            Other = 1,
            Direct = 2,
            SixToFour = 11,
            Isatap = 13,
            Teredo = 14,
            IpHttps = 15,
        }

        /// <summary>
        ///   IP_PER_ADAPTER_INFO - per-adapter IP information such as DNS server list.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct IpPerAdapterInfo
        {
            internal uint autoconfigEnabled;
            internal uint autoconfigActive;
            internal IntPtr currentDnsServer; /* IpAddressList* */
            internal IpAddrString dnsServerList;
        };

        /// <summary>
        ///   Store an IP address with its corresponding subnet mask,
        ///   both as dotted decimal strings.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct IpAddrString
        {
            internal IpAddrString* Next;      /* struct _IpAddressList* */
            internal fixed byte IpAddress[16];
            internal fixed byte IpMask[16];
            internal uint Context;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal unsafe struct MibIfRow2 // MIB_IF_ROW2
        {
            private const int GuidLength = 16;
            private const int IfMaxStringSize = 256;
            private const int IfMaxPhysAddressLength = 32;

            internal ulong interfaceLuid;
            internal uint interfaceIndex;
            internal Guid interfaceGuid;
            internal fixed char alias[IfMaxStringSize + 1]; // Null terminated string.
            internal fixed char description[IfMaxStringSize + 1]; // Null terminated string.
            internal uint physicalAddressLength;
            internal fixed byte physicalAddress[IfMaxPhysAddressLength]; // ANSI
            internal fixed byte permanentPhysicalAddress[IfMaxPhysAddressLength]; // ANSI
            internal uint mtu;
            internal NetworkInterfaceType type;
            internal InterfaceTunnelType tunnelType;
            internal uint mediaType; // Enum
            internal uint physicalMediumType; // Enum
            internal uint accessType; // Enum
            internal uint directionType; // Enum
            internal byte interfaceAndOperStatusFlags; // Flags Enum
            internal OperationalStatus operStatus;
            internal uint adminStatus; // Enum
            internal uint mediaConnectState; // Enum
            internal Guid networkGuid;
            internal InterfaceConnectionType connectionType;
            internal ulong transmitLinkSpeed;
            internal ulong receiveLinkSpeed;
            internal ulong inOctets;
            internal ulong inUcastPkts;
            internal ulong inNUcastPkts;
            internal ulong inDiscards;
            internal ulong inErrors;
            internal ulong inUnknownProtos;
            internal ulong inUcastOctets;
            internal ulong inMulticastOctets;
            internal ulong inBroadcastOctets;
            internal ulong outOctets;
            internal ulong outUcastPkts;
            internal ulong outNUcastPkts;
            internal ulong outDiscards;
            internal ulong outErrors;
            internal ulong outUcastOctets;
            internal ulong outMulticastOctets;
            internal ulong outBroadcastOctets;
            internal ulong outQLen;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibUdpStats
        {
            internal uint datagramsReceived;
            internal uint incomingDatagramsDiscarded;
            internal uint incomingDatagramsWithErrors;
            internal uint datagramsSent;
            internal uint udpListeners;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcpStats
        {
            internal uint reTransmissionAlgorithm;
            internal uint minimumRetransmissionTimeOut;
            internal uint maximumRetransmissionTimeOut;
            internal uint maximumConnections;
            internal uint activeOpens;
            internal uint passiveOpens;
            internal uint failedConnectionAttempts;
            internal uint resetConnections;
            internal uint currentConnections;
            internal uint segmentsReceived;
            internal uint segmentsSent;
            internal uint segmentsResent;
            internal uint errorsReceived;
            internal uint segmentsSentWithReset;
            internal uint cumulativeConnections;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibIpStats
        {
            internal int forwardingEnabled;
            internal uint defaultTtl;
            internal uint packetsReceived;
            internal uint receivedPacketsWithHeaderErrors;
            internal uint receivedPacketsWithAddressErrors;
            internal uint packetsForwarded;
            internal uint receivedPacketsWithUnknownProtocols;
            internal uint receivedPacketsDiscarded;
            internal uint receivedPacketsDelivered;
            internal uint packetOutputRequests;
            internal uint outputPacketRoutingDiscards;
            internal uint outputPacketsDiscarded;
            internal uint outputPacketsWithNoRoute;
            internal uint packetReassemblyTimeout;
            internal uint packetsReassemblyRequired;
            internal uint packetsReassembled;
            internal uint packetsReassemblyFailed;
            internal uint packetsFragmented;
            internal uint packetsFragmentFailed;
            internal uint packetsFragmentCreated;
            internal uint interfaces;
            internal uint ipAddresses;
            internal uint routes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibIcmpInfo
        {
            internal MibIcmpStats inStats;
            internal MibIcmpStats outStats;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibIcmpStats
        {
            internal uint messages;
            internal uint errors;
            internal uint destinationUnreachables;
            internal uint timeExceeds;
            internal uint parameterProblems;
            internal uint sourceQuenches;
            internal uint redirects;
            internal uint echoRequests;
            internal uint echoReplies;
            internal uint timestampRequests;
            internal uint timestampReplies;
            internal uint addressMaskRequests;
            internal uint addressMaskReplies;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibIcmpInfoEx
        {
            internal MibIcmpStatsEx inStats;
            internal MibIcmpStatsEx outStats;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MibIcmpStatsEx
        {
            internal uint dwMsgs;
            internal uint dwErrors;
            internal fixed uint rgdwTypeCount[256];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcpTable
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcpRow
        {
            internal TcpState state;
            internal uint localAddr;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
            internal uint remoteAddr;
            internal byte remotePort1;
            internal byte remotePort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreRemotePort3;
            internal byte ignoreRemotePort4;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibTcp6TableOwnerPid
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MibTcp6RowOwnerPid
        {
            internal fixed byte localAddr[16];
            internal uint localScopeId;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
            internal fixed byte remoteAddr[16];
            internal uint remoteScopeId;
            internal byte remotePort1;
            internal byte remotePort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreRemotePort3;
            internal byte ignoreRemotePort4;
            internal TcpState state;
            internal uint owningPid;

            internal ReadOnlySpan<byte> localAddrAsSpan => MemoryMarshal.CreateSpan(ref localAddr[0], 16);
            internal ReadOnlySpan<byte> remoteAddrAsSpan => MemoryMarshal.CreateSpan(ref remoteAddr[0], 16);
        }

        internal enum TcpTableClass
        {
            TcpTableBasicListener = 0,
            TcpTableBasicConnections = 1,
            TcpTableBasicAll = 2,
            TcpTableOwnerPidListener = 3,
            TcpTableOwnerPidConnections = 4,
            TcpTableOwnerPidAll = 5,
            TcpTableOwnerModuleListener = 6,
            TcpTableOwnerModuleConnections = 7,
            TcpTableOwnerModuleAll = 8
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibUdpTable
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibUdpRow
        {
            internal uint localAddr;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
        }

        internal enum UdpTableClass
        {
            UdpTableBasic = 0,
            UdpTableOwnerPid = 1,
            UdpTableOwnerModule = 2
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MibUdp6TableOwnerPid
        {
            internal uint numberOfEntries;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct MibUdp6RowOwnerPid
        {
            internal fixed byte localAddr[16];
            internal uint localScopeId;
            internal byte localPort1;
            internal byte localPort2;
            // Ports are only 16 bit values (in network WORD order, 3,4,1,2).
            // There are reports where the high order bytes have garbage in them.
            internal byte ignoreLocalPort3;
            internal byte ignoreLocalPort4;
            internal uint owningPid;

            internal ReadOnlySpan<byte> localAddrAsSpan => MemoryMarshal.CreateSpan(ref localAddr[0], 16);
        }

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetAdaptersAddresses(
            AddressFamily family,
            uint flags,
            IntPtr pReserved,
            IntPtr adapterAddresses,
            uint* outBufLen);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetBestInterfaceEx(Span<byte> ipAddress, int* index);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static partial uint GetIfEntry2(ref MibIfRow2 pIfRow);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetIpStatisticsEx(MibIpStats* statistics, AddressFamily family);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetTcpStatisticsEx(MibTcpStats* statistics, AddressFamily family);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetUdpStatisticsEx(MibUdpStats* statistics, AddressFamily family);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetIcmpStatistics(MibIcmpInfo* statistics);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static partial uint GetIcmpStatisticsEx(out MibIcmpInfoEx statistics, AddressFamily family);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetTcpTable(IntPtr pTcpTable, uint* dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool order);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetExtendedTcpTable(IntPtr pTcpTable, uint* dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool order,
                                                        uint IPVersion, TcpTableClass tableClass, uint reserved);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetUdpTable(IntPtr pUdpTable, uint* dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool order);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetExtendedUdpTable(IntPtr pUdpTable, uint* dwOutBufLen, [MarshalAs(UnmanagedType.Bool)] bool order,
                                                        uint IPVersion, UdpTableClass tableClass, uint reserved);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint GetPerAdapterInfo(uint IfIndex, IntPtr pPerAdapterInfo, uint* pOutBufLen);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static partial void FreeMibTable(IntPtr handle);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static partial uint CancelMibChangeNotify2(IntPtr notificationHandle);

        [LibraryImport(Interop.Libraries.IpHlpApi)]
        internal static unsafe partial uint NotifyStableUnicastIpAddressTable(
            AddressFamily addressFamily,
            out SafeFreeMibTable table,
            delegate* unmanaged<IntPtr, IntPtr, void> callback,
            IntPtr context,
            out SafeCancelMibChangeNotify notificationHandle);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net.NetworkInformation
{
    public enum DuplicateAddressDetectionState
    {
        Invalid = 0,
        Tentative = 1,
        Duplicate = 2,
        Deprecated = 3,
        Preferred = 4,
    }
    public abstract partial class GatewayIPAddressInformation
    {
        protected GatewayIPAddressInformation() { }
        public abstract System.Net.IPAddress Address { get; }
    }
    public partial class GatewayIPAddressInformationCollection : System.Collections.Generic.ICollection<System.Net.NetworkInformation.GatewayIPAddressInformation>, System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.GatewayIPAddressInformation>, System.Collections.IEnumerable
    {
        protected internal GatewayIPAddressInformationCollection() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Net.NetworkInformation.GatewayIPAddressInformation this[int index] { get { throw null; } }
        public virtual void Add(System.Net.NetworkInformation.GatewayIPAddressInformation address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.NetworkInformation.GatewayIPAddressInformation address) { throw null; }
        public virtual void CopyTo(System.Net.NetworkInformation.GatewayIPAddressInformation[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.NetworkInformation.GatewayIPAddressInformation> GetEnumerator() { throw null; }
        public virtual bool Remove(System.Net.NetworkInformation.GatewayIPAddressInformation address) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public abstract partial class IcmpV4Statistics
    {
        protected IcmpV4Statistics() { }
        public abstract long AddressMaskRepliesReceived { get; }
        public abstract long AddressMaskRepliesSent { get; }
        public abstract long AddressMaskRequestsReceived { get; }
        public abstract long AddressMaskRequestsSent { get; }
        public abstract long DestinationUnreachableMessagesReceived { get; }
        public abstract long DestinationUnreachableMessagesSent { get; }
        public abstract long EchoRepliesReceived { get; }
        public abstract long EchoRepliesSent { get; }
        public abstract long EchoRequestsReceived { get; }
        public abstract long EchoRequestsSent { get; }
        public abstract long ErrorsReceived { get; }
        public abstract long ErrorsSent { get; }
        public abstract long MessagesReceived { get; }
        public abstract long MessagesSent { get; }
        public abstract long ParameterProblemsReceived { get; }
        public abstract long ParameterProblemsSent { get; }
        public abstract long RedirectsReceived { get; }
        public abstract long RedirectsSent { get; }
        public abstract long SourceQuenchesReceived { get; }
        public abstract long SourceQuenchesSent { get; }
        public abstract long TimeExceededMessagesReceived { get; }
        public abstract long TimeExceededMessagesSent { get; }
        public abstract long TimestampRepliesReceived { get; }
        public abstract long TimestampRepliesSent { get; }
        public abstract long TimestampRequestsReceived { get; }
        public abstract long TimestampRequestsSent { get; }
    }
    public abstract partial class IcmpV6Statistics
    {
        protected IcmpV6Statistics() { }
        public abstract long DestinationUnreachableMessagesReceived { get; }
        public abstract long DestinationUnreachableMessagesSent { get; }
        public abstract long EchoRepliesReceived { get; }
        public abstract long EchoRepliesSent { get; }
        public abstract long EchoRequestsReceived { get; }
        public abstract long EchoRequestsSent { get; }
        public abstract long ErrorsReceived { get; }
        public abstract long ErrorsSent { get; }
        public abstract long MembershipQueriesReceived { get; }
        public abstract long MembershipQueriesSent { get; }
        public abstract long MembershipReductionsReceived { get; }
        public abstract long MembershipReductionsSent { get; }
        public abstract long MembershipReportsReceived { get; }
        public abstract long MembershipReportsSent { get; }
        public abstract long MessagesReceived { get; }
        public abstract long MessagesSent { get; }
        public abstract long NeighborAdvertisementsReceived { get; }
        public abstract long NeighborAdvertisementsSent { get; }
        public abstract long NeighborSolicitsReceived { get; }
        public abstract long NeighborSolicitsSent { get; }
        public abstract long PacketTooBigMessagesReceived { get; }
        public abstract long PacketTooBigMessagesSent { get; }
        public abstract long ParameterProblemsReceived { get; }
        public abstract long ParameterProblemsSent { get; }
        public abstract long RedirectsReceived { get; }
        public abstract long RedirectsSent { get; }
        public abstract long RouterAdvertisementsReceived { get; }
        public abstract long RouterAdvertisementsSent { get; }
        public abstract long RouterSolicitsReceived { get; }
        public abstract long RouterSolicitsSent { get; }
        public abstract long TimeExceededMessagesReceived { get; }
        public abstract long TimeExceededMessagesSent { get; }
    }
    public abstract partial class IPAddressInformation
    {
        protected IPAddressInformation() { }
        public abstract System.Net.IPAddress Address { get; }
        public abstract bool IsDnsEligible { get; }
        public abstract bool IsTransient { get; }
    }
    public partial class IPAddressInformationCollection : System.Collections.Generic.ICollection<System.Net.NetworkInformation.IPAddressInformation>, System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.IPAddressInformation>, System.Collections.IEnumerable
    {
        internal IPAddressInformationCollection() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Net.NetworkInformation.IPAddressInformation this[int index] { get { throw null; } }
        public virtual void Add(System.Net.NetworkInformation.IPAddressInformation address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.NetworkInformation.IPAddressInformation address) { throw null; }
        public virtual void CopyTo(System.Net.NetworkInformation.IPAddressInformation[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.NetworkInformation.IPAddressInformation> GetEnumerator() { throw null; }
        public virtual bool Remove(System.Net.NetworkInformation.IPAddressInformation address) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public abstract partial class IPGlobalProperties
    {
        protected IPGlobalProperties() { }
        public abstract string DhcpScopeName { get; }
        public abstract string DomainName { get; }
        public abstract string HostName { get; }
        public abstract bool IsWinsProxy { get; }
        public abstract System.Net.NetworkInformation.NetBiosNodeType NodeType { get; }
        public virtual System.IAsyncResult BeginGetUnicastAddresses(System.AsyncCallback callback, object state) { throw null; }
        public virtual System.Net.NetworkInformation.UnicastIPAddressInformationCollection EndGetUnicastAddresses(System.IAsyncResult asyncResult) { throw null; }
        public abstract System.Net.NetworkInformation.TcpConnectionInformation[] GetActiveTcpConnections();
        public abstract System.Net.IPEndPoint[] GetActiveTcpListeners();
        public abstract System.Net.IPEndPoint[] GetActiveUdpListeners();
        public abstract System.Net.NetworkInformation.IcmpV4Statistics GetIcmpV4Statistics();
        public abstract System.Net.NetworkInformation.IcmpV6Statistics GetIcmpV6Statistics();
        public static System.Net.NetworkInformation.IPGlobalProperties GetIPGlobalProperties() { throw null; }
        public abstract System.Net.NetworkInformation.IPGlobalStatistics GetIPv4GlobalStatistics();
        public abstract System.Net.NetworkInformation.IPGlobalStatistics GetIPv6GlobalStatistics();
        public abstract System.Net.NetworkInformation.TcpStatistics GetTcpIPv4Statistics();
        public abstract System.Net.NetworkInformation.TcpStatistics GetTcpIPv6Statistics();
        public abstract System.Net.NetworkInformation.UdpStatistics GetUdpIPv4Statistics();
        public abstract System.Net.NetworkInformation.UdpStatistics GetUdpIPv6Statistics();
        public virtual System.Net.NetworkInformation.UnicastIPAddressInformationCollection GetUnicastAddresses() { throw null; }
        public virtual System.Threading.Tasks.Task<System.Net.NetworkInformation.UnicastIPAddressInformationCollection> GetUnicastAddressesAsync() { throw null; }
    }
    public abstract partial class IPGlobalStatistics
    {
        protected IPGlobalStatistics() { }
        public abstract int DefaultTtl { get; }
        public abstract bool ForwardingEnabled { get; }
        public abstract int NumberOfInterfaces { get; }
        public abstract int NumberOfIPAddresses { get; }
        public abstract int NumberOfRoutes { get; }
        public abstract long OutputPacketRequests { get; }
        public abstract long OutputPacketRoutingDiscards { get; }
        public abstract long OutputPacketsDiscarded { get; }
        public abstract long OutputPacketsWithNoRoute { get; }
        public abstract long PacketFragmentFailures { get; }
        public abstract long PacketReassembliesRequired { get; }
        public abstract long PacketReassemblyFailures { get; }
        public abstract long PacketReassemblyTimeout { get; }
        public abstract long PacketsFragmented { get; }
        public abstract long PacketsReassembled { get; }
        public abstract long ReceivedPackets { get; }
        public abstract long ReceivedPacketsDelivered { get; }
        public abstract long ReceivedPacketsDiscarded { get; }
        public abstract long ReceivedPacketsForwarded { get; }
        public abstract long ReceivedPacketsWithAddressErrors { get; }
        public abstract long ReceivedPacketsWithHeadersErrors { get; }
        public abstract long ReceivedPacketsWithUnknownProtocol { get; }
    }
    public abstract partial class IPInterfaceProperties
    {
        protected IPInterfaceProperties() { }
        public abstract System.Net.NetworkInformation.IPAddressInformationCollection AnycastAddresses { get; }
        public abstract System.Net.NetworkInformation.IPAddressCollection DhcpServerAddresses { get; }
        public abstract System.Net.NetworkInformation.IPAddressCollection DnsAddresses { get; }
        public abstract string DnsSuffix { get; }
        public abstract System.Net.NetworkInformation.GatewayIPAddressInformationCollection GatewayAddresses { get; }
        public abstract bool IsDnsEnabled { get; }
        public abstract bool IsDynamicDnsEnabled { get; }
        public abstract System.Net.NetworkInformation.MulticastIPAddressInformationCollection MulticastAddresses { get; }
        public abstract System.Net.NetworkInformation.UnicastIPAddressInformationCollection UnicastAddresses { get; }
        public abstract System.Net.NetworkInformation.IPAddressCollection WinsServersAddresses { get; }
        public abstract System.Net.NetworkInformation.IPv4InterfaceProperties GetIPv4Properties();
        public abstract System.Net.NetworkInformation.IPv6InterfaceProperties GetIPv6Properties();
    }
    public abstract partial class IPInterfaceStatistics
    {
        protected IPInterfaceStatistics() { }
        public abstract long BytesReceived { get; }
        public abstract long BytesSent { get; }
        public abstract long IncomingPacketsDiscarded { get; }
        public abstract long IncomingPacketsWithErrors { get; }
        public abstract long IncomingUnknownProtocolPackets { get; }
        public abstract long NonUnicastPacketsReceived { get; }
        public abstract long NonUnicastPacketsSent { get; }
        public abstract long OutgoingPacketsDiscarded { get; }
        public abstract long OutgoingPacketsWithErrors { get; }
        public abstract long OutputQueueLength { get; }
        public abstract long UnicastPacketsReceived { get; }
        public abstract long UnicastPacketsSent { get; }
    }
    public abstract partial class IPv4InterfaceProperties
    {
        protected IPv4InterfaceProperties() { }
        public abstract int Index { get; }
        public abstract bool IsAutomaticPrivateAddressingActive { get; }
        public abstract bool IsAutomaticPrivateAddressingEnabled { get; }
        public abstract bool IsDhcpEnabled { get; }
        public abstract bool IsForwardingEnabled { get; }
        public abstract int Mtu { get; }
        public abstract bool UsesWins { get; }
    }
    public abstract partial class IPv4InterfaceStatistics
    {
        protected IPv4InterfaceStatistics() { }
        public abstract long BytesReceived { get; }
        public abstract long BytesSent { get; }
        public abstract long IncomingPacketsDiscarded { get; }
        public abstract long IncomingPacketsWithErrors { get; }
        public abstract long IncomingUnknownProtocolPackets { get; }
        public abstract long NonUnicastPacketsReceived { get; }
        public abstract long NonUnicastPacketsSent { get; }
        public abstract long OutgoingPacketsDiscarded { get; }
        public abstract long OutgoingPacketsWithErrors { get; }
        public abstract long OutputQueueLength { get; }
        public abstract long UnicastPacketsReceived { get; }
        public abstract long UnicastPacketsSent { get; }
    }
    public abstract partial class IPv6InterfaceProperties
    {
        protected IPv6InterfaceProperties() { }
        public abstract int Index { get; }
        public abstract int Mtu { get; }
        public virtual long GetScopeId(System.Net.NetworkInformation.ScopeLevel scopeLevel) { throw null; }
    }
    public abstract partial class MulticastIPAddressInformation : System.Net.NetworkInformation.IPAddressInformation
    {
        protected MulticastIPAddressInformation() { }
        public abstract long AddressPreferredLifetime { get; }
        public abstract long AddressValidLifetime { get; }
        public abstract long DhcpLeaseLifetime { get; }
        public abstract System.Net.NetworkInformation.DuplicateAddressDetectionState DuplicateAddressDetectionState { get; }
        public abstract System.Net.NetworkInformation.PrefixOrigin PrefixOrigin { get; }
        public abstract System.Net.NetworkInformation.SuffixOrigin SuffixOrigin { get; }
    }
    public partial class MulticastIPAddressInformationCollection : System.Collections.Generic.ICollection<System.Net.NetworkInformation.MulticastIPAddressInformation>, System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.MulticastIPAddressInformation>, System.Collections.IEnumerable
    {
        protected internal MulticastIPAddressInformationCollection() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Net.NetworkInformation.MulticastIPAddressInformation this[int index] { get { throw null; } }
        public virtual void Add(System.Net.NetworkInformation.MulticastIPAddressInformation address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.NetworkInformation.MulticastIPAddressInformation address) { throw null; }
        public virtual void CopyTo(System.Net.NetworkInformation.MulticastIPAddressInformation[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.NetworkInformation.MulticastIPAddressInformation> GetEnumerator() { throw null; }
        public virtual bool Remove(System.Net.NetworkInformation.MulticastIPAddressInformation address) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public enum NetBiosNodeType
    {
        Unknown = 0,
        Broadcast = 1,
        Peer2Peer = 2,
        Mixed = 4,
        Hybrid = 8,
    }
    public delegate void NetworkAddressChangedEventHandler(object sender, System.EventArgs e);
    public delegate void NetworkAvailabilityChangedEventHandler(object sender, System.Net.NetworkInformation.NetworkAvailabilityEventArgs e);
    public partial class NetworkAvailabilityEventArgs : System.EventArgs
    {
        internal NetworkAvailabilityEventArgs() { }
        public bool IsAvailable { get { throw null; } }
    }
    public partial class NetworkChange
    {
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public NetworkChange() { }
        public static event System.Net.NetworkInformation.NetworkAddressChangedEventHandler NetworkAddressChanged { add { } remove { } }
        public static event System.Net.NetworkInformation.NetworkAvailabilityChangedEventHandler NetworkAvailabilityChanged { add { } remove { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.", true)]
        public static void RegisterNetworkChange(System.Net.NetworkInformation.NetworkChange nc) { }
    }
    public partial class NetworkInformationException : System.ComponentModel.Win32Exception
    {
        public NetworkInformationException() { }
        public NetworkInformationException(int errorCode) { }
        protected NetworkInformationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override int ErrorCode { get { throw null; } }
    }
    public abstract partial class NetworkInterface
    {
        protected NetworkInterface() { }
        public virtual string Description { get { throw null; } }
        public virtual string Id { get { throw null; } }
        public static int IPv6LoopbackInterfaceIndex { get { throw null; } }
        public virtual bool IsReceiveOnly { get { throw null; } }
        public static int LoopbackInterfaceIndex { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual System.Net.NetworkInformation.NetworkInterfaceType NetworkInterfaceType { get { throw null; } }
        public virtual System.Net.NetworkInformation.OperationalStatus OperationalStatus { get { throw null; } }
        public virtual long Speed { get { throw null; } }
        public virtual bool SupportsMulticast { get { throw null; } }
        public static System.Net.NetworkInformation.NetworkInterface[] GetAllNetworkInterfaces() { throw null; }
        public virtual System.Net.NetworkInformation.IPInterfaceProperties GetIPProperties() { throw null; }
        public virtual System.Net.NetworkInformation.IPInterfaceStatistics GetIPStatistics() { throw null; }
        public virtual System.Net.NetworkInformation.IPv4InterfaceStatistics GetIPv4Statistics() { throw null; }
        public static bool GetIsNetworkAvailable() { throw null; }
        public virtual System.Net.NetworkInformation.PhysicalAddress GetPhysicalAddress() { throw null; }
        public virtual bool Supports(System.Net.NetworkInformation.NetworkInterfaceComponent networkInterfaceComponent) { throw null; }
    }
    public enum NetworkInterfaceComponent
    {
        IPv4 = 0,
        IPv6 = 1,
    }
    public enum NetworkInterfaceType
    {
        Unknown = 1,
        Ethernet = 6,
        TokenRing = 9,
        Fddi = 15,
        BasicIsdn = 20,
        PrimaryIsdn = 21,
        Ppp = 23,
        Loopback = 24,
        Ethernet3Megabit = 26,
        Slip = 28,
        Atm = 37,
        GenericModem = 48,
        FastEthernetT = 62,
        Isdn = 63,
        FastEthernetFx = 69,
        Wireless80211 = 71,
        AsymmetricDsl = 94,
        RateAdaptDsl = 95,
        SymmetricDsl = 96,
        VeryHighSpeedDsl = 97,
        IPOverAtm = 114,
        GigabitEthernet = 117,
        Tunnel = 131,
        MultiRateSymmetricDsl = 143,
        HighPerformanceSerialBus = 144,
        Wman = 237,
        Wwanpp = 243,
        Wwanpp2 = 244,
    }
    public enum OperationalStatus
    {
        Up = 1,
        Down = 2,
        Testing = 3,
        Unknown = 4,
        Dormant = 5,
        NotPresent = 6,
        LowerLayerDown = 7,
    }
    public partial class PhysicalAddress
    {
        public static readonly System.Net.NetworkInformation.PhysicalAddress None;
        public PhysicalAddress(byte[] address) { }
        public override bool Equals(object comparand) { throw null; }
        public byte[] GetAddressBytes() { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.NetworkInformation.PhysicalAddress Parse(string address) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum PrefixOrigin
    {
        Other = 0,
        Manual = 1,
        WellKnown = 2,
        Dhcp = 3,
        RouterAdvertisement = 4,
    }
    public enum ScopeLevel
    {
        None = 0,
        Interface = 1,
        Link = 2,
        Subnet = 3,
        Admin = 4,
        Site = 5,
        Organization = 8,
        Global = 14,
    }
    public enum SuffixOrigin
    {
        Other = 0,
        Manual = 1,
        WellKnown = 2,
        OriginDhcp = 3,
        LinkLayerAddress = 4,
        Random = 5,
    }
    public abstract partial class TcpConnectionInformation
    {
        protected TcpConnectionInformation() { }
        public abstract System.Net.IPEndPoint LocalEndPoint { get; }
        public abstract System.Net.IPEndPoint RemoteEndPoint { get; }
        public abstract System.Net.NetworkInformation.TcpState State { get; }
    }
    public enum TcpState
    {
        Unknown = 0,
        Closed = 1,
        Listen = 2,
        SynSent = 3,
        SynReceived = 4,
        Established = 5,
        FinWait1 = 6,
        FinWait2 = 7,
        CloseWait = 8,
        Closing = 9,
        LastAck = 10,
        TimeWait = 11,
        DeleteTcb = 12,
    }
    public abstract partial class TcpStatistics
    {
        protected TcpStatistics() { }
        public abstract long ConnectionsAccepted { get; }
        public abstract long ConnectionsInitiated { get; }
        public abstract long CumulativeConnections { get; }
        public abstract long CurrentConnections { get; }
        public abstract long ErrorsReceived { get; }
        public abstract long FailedConnectionAttempts { get; }
        public abstract long MaximumConnections { get; }
        public abstract long MaximumTransmissionTimeout { get; }
        public abstract long MinimumTransmissionTimeout { get; }
        public abstract long ResetConnections { get; }
        public abstract long ResetsSent { get; }
        public abstract long SegmentsReceived { get; }
        public abstract long SegmentsResent { get; }
        public abstract long SegmentsSent { get; }
    }
    public abstract partial class UdpStatistics
    {
        protected UdpStatistics() { }
        public abstract long DatagramsReceived { get; }
        public abstract long DatagramsSent { get; }
        public abstract long IncomingDatagramsDiscarded { get; }
        public abstract long IncomingDatagramsWithErrors { get; }
        public abstract int UdpListeners { get; }
    }
    public abstract partial class UnicastIPAddressInformation : System.Net.NetworkInformation.IPAddressInformation
    {
        protected UnicastIPAddressInformation() { }
        public abstract long AddressPreferredLifetime { get; }
        public abstract long AddressValidLifetime { get; }
        public abstract long DhcpLeaseLifetime { get; }
        public abstract System.Net.NetworkInformation.DuplicateAddressDetectionState DuplicateAddressDetectionState { get; }
        public abstract System.Net.IPAddress IPv4Mask { get; }
        public virtual int PrefixLength { get { throw null; } }
        public abstract System.Net.NetworkInformation.PrefixOrigin PrefixOrigin { get; }
        public abstract System.Net.NetworkInformation.SuffixOrigin SuffixOrigin { get; }
    }
    public partial class UnicastIPAddressInformationCollection : System.Collections.Generic.ICollection<System.Net.NetworkInformation.UnicastIPAddressInformation>, System.Collections.Generic.IEnumerable<System.Net.NetworkInformation.UnicastIPAddressInformation>, System.Collections.IEnumerable
    {
        protected internal UnicastIPAddressInformationCollection() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Net.NetworkInformation.UnicastIPAddressInformation this[int index] { get { throw null; } }
        public virtual void Add(System.Net.NetworkInformation.UnicastIPAddressInformation address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.NetworkInformation.UnicastIPAddressInformation address) { throw null; }
        public virtual void CopyTo(System.Net.NetworkInformation.UnicastIPAddressInformation[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.NetworkInformation.UnicastIPAddressInformation> GetEnumerator() { throw null; }
        public virtual bool Remove(System.Net.NetworkInformation.UnicastIPAddressInformation address) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
}

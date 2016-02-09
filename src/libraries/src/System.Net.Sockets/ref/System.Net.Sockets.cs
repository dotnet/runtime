// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Net.Sockets
{
    public enum IOControlCode : long
    {
        AbsorbRouterAlert = (long)2550136837,
        AddMulticastGroupOnInterface = (long)2550136842,
        AddressListChange = (long)671088663,
        AddressListQuery = (long)1207959574,
        AddressListSort = (long)3355443225,
        AssociateHandle = (long)2281701377,
        AsyncIO = (long)2147772029,
        BindToInterface = (long)2550136840,
        DataToRead = (long)1074030207,
        DeleteMulticastGroupFromInterface = (long)2550136843,
        EnableCircularQueuing = (long)671088642,
        Flush = (long)671088644,
        GetBroadcastAddress = (long)1207959557,
        GetExtensionFunctionPointer = (long)3355443206,
        GetGroupQos = (long)3355443208,
        GetQos = (long)3355443207,
        KeepAliveValues = (long)2550136836,
        LimitBroadcasts = (long)2550136839,
        MulticastInterface = (long)2550136841,
        MulticastScope = (long)2281701386,
        MultipointLoopback = (long)2281701385,
        NamespaceChange = (long)2281701401,
        NonBlockingIO = (long)2147772030,
        OobDataRead = (long)1074033415,
        QueryTargetPnpHandle = (long)1207959576,
        ReceiveAll = (long)2550136833,
        ReceiveAllIgmpMulticast = (long)2550136835,
        ReceiveAllMulticast = (long)2550136834,
        RoutingInterfaceChange = (long)2281701397,
        RoutingInterfaceQuery = (long)3355443220,
        SetGroupQos = (long)2281701388,
        SetQos = (long)2281701387,
        TranslateHandle = (long)3355443213,
        UnicastInterface = (long)2550136838,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct IPPacketInformation
    {
        public System.Net.IPAddress Address { get { return default(System.Net.IPAddress); } }
        public int Interface { get { return default(int); } }
        public override bool Equals(object comparand) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Net.Sockets.IPPacketInformation packetInformation1, System.Net.Sockets.IPPacketInformation packetInformation2) { return default(bool); }
        public static bool operator !=(System.Net.Sockets.IPPacketInformation packetInformation1, System.Net.Sockets.IPPacketInformation packetInformation2) { return default(bool); }
    }
    public enum IPProtectionLevel
    {
        EdgeRestricted = 20,
        Restricted = 30,
        Unrestricted = 10,
        Unspecified = -1,
    }
    public partial class IPv6MulticastOption
    {
        public IPv6MulticastOption(System.Net.IPAddress group) { }
        public IPv6MulticastOption(System.Net.IPAddress group, long ifindex) { }
        public System.Net.IPAddress Group { get { return default(System.Net.IPAddress); } set { } }
        public long InterfaceIndex { get { return default(long); } set { } }
    }
    public partial class LingerOption
    {
        public LingerOption(bool enable, int seconds) { }
        public bool Enabled { get { return default(bool); } set { } }
        public int LingerTime { get { return default(int); } set { } }
    }
    public partial class MulticastOption
    {
        public MulticastOption(System.Net.IPAddress group) { }
        public MulticastOption(System.Net.IPAddress group, int interfaceIndex) { }
        public MulticastOption(System.Net.IPAddress group, System.Net.IPAddress mcint) { }
        public System.Net.IPAddress Group { get { return default(System.Net.IPAddress); } set { } }
        public int InterfaceIndex { get { return default(int); } set { } }
        public System.Net.IPAddress LocalAddress { get { return default(System.Net.IPAddress); } set { } }
    }
    public partial class NetworkStream : System.IO.Stream
    {
        public NetworkStream(System.Net.Sockets.Socket socket) { }
        public NetworkStream(System.Net.Sockets.Socket socket, bool ownsSocket) { }
        public override bool CanRead { get { return default(bool); } }
        public override bool CanSeek { get { return default(bool); } }
        public override bool CanTimeout { get { return default(bool); } }
        public override bool CanWrite { get { return default(bool); } }
        public virtual bool DataAvailable { get { return default(bool); } }
        public override long Length { get { return default(long); } }
        public override long Position { get { return default(long); } set { } }
        public override int ReadTimeout { get { return default(int); } set { } }
        public override int WriteTimeout { get { return default(int); } set { } }
        protected override void Dispose(bool disposing) { }
        ~NetworkStream() { }
        public override void Flush() { }
        public override System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override int Read(byte[] buffer, int offset, int size) { buffer = default(byte[]); return default(int); }
        public override System.Threading.Tasks.Task<int> ReadAsync(byte[] buffer, int offset, int size, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { return default(long); }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int size) { }
        public override System.Threading.Tasks.Task WriteAsync(byte[] buffer, int offset, int size, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public enum ProtocolType
    {
        Ggp = 3,
        Icmp = 1,
        IcmpV6 = 58,
        Idp = 22,
        Igmp = 2,
        IP = 0,
        IPSecAuthenticationHeader = 51,
        IPSecEncapsulatingSecurityPayload = 50,
        IPv4 = 4,
        IPv6 = 41,
        IPv6DestinationOptions = 60,
        IPv6FragmentHeader = 44,
        IPv6HopByHopOptions = 0,
        IPv6NoNextHeader = 59,
        IPv6RoutingHeader = 43,
        Ipx = 1000,
        ND = 77,
        Pup = 12,
        Raw = 255,
        Spx = 1256,
        SpxII = 1257,
        Tcp = 6,
        Udp = 17,
        Unknown = -1,
        Unspecified = 0,
    }
    public enum SelectMode
    {
        SelectError = 2,
        SelectRead = 0,
        SelectWrite = 1,
    }
    public partial class SendPacketsElement
    {
        public SendPacketsElement(byte[] buffer) { }
        public SendPacketsElement(byte[] buffer, int offset, int count) { }
        public SendPacketsElement(byte[] buffer, int offset, int count, bool endOfPacket) { }
        public SendPacketsElement(string filepath) { }
        public SendPacketsElement(string filepath, int offset, int count) { }
        public SendPacketsElement(string filepath, int offset, int count, bool endOfPacket) { }
        public byte[] Buffer { get { return default(byte[]); } }
        public int Count { get { return default(int); } }
        public bool EndOfPacket { get { return default(bool); } }
        public string FilePath { get { return default(string); } }
        public int Offset { get { return default(int); } }
    }
    public partial class Socket : System.IDisposable
    {
        public Socket(System.Net.Sockets.AddressFamily addressFamily, System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType) { }
        public Socket(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType) { }
        public System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public int Available { get { return default(int); } }
        public bool Blocking { get { return default(bool); } set { } }
        public bool Connected { get { return default(bool); } }
        public bool DontFragment { get { return default(bool); } set { } }
        public bool DualMode { get { return default(bool); } set { } }
        public bool EnableBroadcast { get { return default(bool); } set { } }
        public bool ExclusiveAddressUse { get { return default(bool); } set { } }
        public bool IsBound { get { return default(bool); } }
        public System.Net.Sockets.LingerOption LingerState { get { return default(System.Net.Sockets.LingerOption); } set { } }
        public System.Net.EndPoint LocalEndPoint { get { return default(System.Net.EndPoint); } }
        public bool MulticastLoopback { get { return default(bool); } set { } }
        public bool NoDelay { get { return default(bool); } set { } }
        public static bool OSSupportsIPv4 { get { return default(bool); } }
        public static bool OSSupportsIPv6 { get { return default(bool); } }
        public System.Net.Sockets.ProtocolType ProtocolType { get { return default(System.Net.Sockets.ProtocolType); } }
        public int ReceiveBufferSize { get { return default(int); } set { } }
        public int ReceiveTimeout { get { return default(int); } set { } }
        public System.Net.EndPoint RemoteEndPoint { get { return default(System.Net.EndPoint); } }
        public int SendBufferSize { get { return default(int); } set { } }
        public int SendTimeout { get { return default(int); } set { } }
        public System.Net.Sockets.SocketType SocketType { get { return default(System.Net.Sockets.SocketType); } }
        public short Ttl { get { return default(short); } set { } }
        public System.Net.Sockets.Socket Accept() { return default(System.Net.Sockets.Socket); }
        public bool AcceptAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void Bind(System.Net.EndPoint localEP) { }
        public static void CancelConnectAsync(System.Net.Sockets.SocketAsyncEventArgs e) { }
        public void Connect(System.Net.EndPoint remoteEP) { }
        public void Connect(System.Net.IPAddress address, int port) { }
        public void Connect(System.Net.IPAddress[] addresses, int port) { }
        public void Connect(string host, int port) { }
        public bool ConnectAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public static bool ConnectAsync(System.Net.Sockets.SocketType socketType, System.Net.Sockets.ProtocolType protocolType, System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Socket() { }
        public object GetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName) { return default(object); }
        public void GetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, byte[] optionValue) { }
        public byte[] GetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, int optionLength) { return default(byte[]); }
        public int IOControl(int ioControlCode, byte[] optionInValue, byte[] optionOutValue) { return default(int); }
        public int IOControl(System.Net.Sockets.IOControlCode ioControlCode, byte[] optionInValue, byte[] optionOutValue) { return default(int); }
        public void Listen(int backlog) { }
        public bool Poll(int microSeconds, System.Net.Sockets.SelectMode mode) { return default(bool); }
        public int Receive(byte[] buffer) { return default(int); }
        public int Receive(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Receive(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { errorCode = default(System.Net.Sockets.SocketError); return default(int); }
        public int Receive(byte[] buffer, int size, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Receive(byte[] buffer, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Receive(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers) { return default(int); }
        public int Receive(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Receive(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { errorCode = default(System.Net.Sockets.SocketError); return default(int); }
        public bool ReceiveAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public int ReceiveFrom(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags, ref System.Net.EndPoint remoteEP) { return default(int); }
        public int ReceiveFrom(byte[] buffer, int size, System.Net.Sockets.SocketFlags socketFlags, ref System.Net.EndPoint remoteEP) { return default(int); }
        public int ReceiveFrom(byte[] buffer, ref System.Net.EndPoint remoteEP) { return default(int); }
        public int ReceiveFrom(byte[] buffer, System.Net.Sockets.SocketFlags socketFlags, ref System.Net.EndPoint remoteEP) { return default(int); }
        public bool ReceiveFromAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref System.Net.Sockets.SocketFlags socketFlags, ref System.Net.EndPoint remoteEP, out System.Net.Sockets.IPPacketInformation ipPacketInformation) { ipPacketInformation = default(System.Net.Sockets.IPPacketInformation); return default(int); }
        public bool ReceiveMessageFromAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public static void Select(System.Collections.IList checkRead, System.Collections.IList checkWrite, System.Collections.IList checkError, int microSeconds) { }
        public int Send(byte[] buffer) { return default(int); }
        public int Send(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Send(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { errorCode = default(System.Net.Sockets.SocketError); return default(int); }
        public int Send(byte[] buffer, int size, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Send(byte[] buffer, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Send(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers) { return default(int); }
        public int Send(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags) { return default(int); }
        public int Send(System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags, out System.Net.Sockets.SocketError errorCode) { errorCode = default(System.Net.Sockets.SocketError); return default(int); }
        public bool SendAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public bool SendPacketsAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public int SendTo(byte[] buffer, int offset, int size, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEP) { return default(int); }
        public int SendTo(byte[] buffer, int size, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEP) { return default(int); }
        public int SendTo(byte[] buffer, System.Net.EndPoint remoteEP) { return default(int); }
        public int SendTo(byte[] buffer, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEP) { return default(int); }
        public bool SendToAsync(System.Net.Sockets.SocketAsyncEventArgs e) { return default(bool); }
        public void SetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, bool optionValue) { }
        public void SetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, byte[] optionValue) { }
        public void SetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, int optionValue) { }
        public void SetSocketOption(System.Net.Sockets.SocketOptionLevel optionLevel, System.Net.Sockets.SocketOptionName optionName, object optionValue) { }
        public void Shutdown(System.Net.Sockets.SocketShutdown how) { }
    }
    public partial class SocketAsyncEventArgs : System.EventArgs, System.IDisposable
    {
        public SocketAsyncEventArgs() { }
        public System.Net.Sockets.Socket AcceptSocket { get { return default(System.Net.Sockets.Socket); } set { } }
        public byte[] Buffer { get { return default(byte[]); } }
        public System.Collections.Generic.IList<System.ArraySegment<byte>> BufferList { get { return default(System.Collections.Generic.IList<System.ArraySegment<byte>>); } set { } }
        public int BytesTransferred { get { return default(int); } }
        public System.Exception ConnectByNameError { get { return default(System.Exception); } }
        public System.Net.Sockets.Socket ConnectSocket { get { return default(System.Net.Sockets.Socket); } }
        public int Count { get { return default(int); } }
        public System.Net.Sockets.SocketAsyncOperation LastOperation { get { return default(System.Net.Sockets.SocketAsyncOperation); } }
        public int Offset { get { return default(int); } }
        public System.Net.Sockets.IPPacketInformation ReceiveMessageFromPacketInfo { get { return default(System.Net.Sockets.IPPacketInformation); } }
        public System.Net.EndPoint RemoteEndPoint { get { return default(System.Net.EndPoint); } set { } }
        public System.Net.Sockets.SendPacketsElement[] SendPacketsElements { get { return default(System.Net.Sockets.SendPacketsElement[]); } set { } }
        public int SendPacketsSendSize { get { return default(int); } set { } }
        public System.Net.Sockets.SocketError SocketError { get { return default(System.Net.Sockets.SocketError); } set { } }
        public System.Net.Sockets.SocketFlags SocketFlags { get { return default(System.Net.Sockets.SocketFlags); } set { } }
        public object UserToken { get { return default(object); } set { } }
        public event System.EventHandler<System.Net.Sockets.SocketAsyncEventArgs> Completed { add { } remove { } }
        public void Dispose() { }
        ~SocketAsyncEventArgs() { }
        protected virtual void OnCompleted(System.Net.Sockets.SocketAsyncEventArgs e) { }
        public void SetBuffer(byte[] buffer, int offset, int count) { }
        public void SetBuffer(int offset, int count) { }
    }
    public enum SocketAsyncOperation
    {
        Accept = 1,
        Connect = 2,
        Disconnect = 3,
        None = 0,
        Receive = 4,
        ReceiveFrom = 5,
        ReceiveMessageFrom = 6,
        Send = 7,
        SendPackets = 8,
        SendTo = 9,
    }
    [System.FlagsAttribute]
    public enum SocketFlags
    {
        Broadcast = 1024,
        ControlDataTruncated = 512,
        DontRoute = 4,
        Multicast = 2048,
        None = 0,
        OutOfBand = 1,
        Partial = 32768,
        Peek = 2,
        Truncated = 256,
    }
    public enum SocketOptionLevel
    {
        IP = 0,
        IPv6 = 41,
        Socket = 65535,
        Tcp = 6,
        Udp = 17,
    }
    public enum SocketOptionName
    {
        AcceptConnection = 2,
        AddMembership = 12,
        AddSourceMembership = 15,
        BlockSource = 17,
        Broadcast = 32,
        BsdUrgent = 2,
        ChecksumCoverage = 20,
        Debug = 1,
        DontFragment = 14,
        DontLinger = -129,
        DontRoute = 16,
        DropMembership = 13,
        DropSourceMembership = 16,
        Error = 4103,
        ExclusiveAddressUse = -5,
        Expedited = 2,
        HeaderIncluded = 2,
        HopLimit = 21,
        IPOptions = 1,
        IPProtectionLevel = 23,
        IpTimeToLive = 4,
        IPv6Only = 27,
        KeepAlive = 8,
        Linger = 128,
        MaxConnections = 2147483647,
        MulticastInterface = 9,
        MulticastLoopback = 11,
        MulticastTimeToLive = 10,
        NoChecksum = 1,
        NoDelay = 1,
        OutOfBandInline = 256,
        PacketInformation = 19,
        ReceiveBuffer = 4098,
        ReceiveLowWater = 4100,
        ReceiveTimeout = 4102,
        ReuseAddress = 4,
        ReuseUnicastPort = 12295, 
        SendBuffer = 4097,
        SendLowWater = 4099,
        SendTimeout = 4101,
        Type = 4104,
        TypeOfService = 3,
        UnblockSource = 18,
        UpdateAcceptContext = 28683,
        UpdateConnectContext = 28688,
        UseLoopback = 64,
    }
    // TODO: Review note: RemoteEndPoint definition includes the Address and Port.
    //       PacketInformation includes Address and Interface (physical interface number).
    //       The redundancy could be removed by replacing RemoteEndPoint with Port.
    
    // Alternative:
    //    public struct SocketReceiveFromResult
    //    {
    //        public int ReceivedBytes;
    //        public IPAddress Address;
    //        public int Port;
    //    }
    public struct SocketReceiveFromResult
    {
        public int ReceivedBytes;
        public EndPoint RemoteEndPoint;
    }
    // Alternative:
    //    public struct SocketReceiveMessageFromResult
    //    {
    //        public int ReceivedBytes;
    //        public SocketFlags SocketFlags;
    //        public IPAddress Address;
    //        public int Port;
    //        public int Interface;
    //    }
    public struct SocketReceiveMessageFromResult
    {
        public int ReceivedBytes;
        public SocketFlags SocketFlags;
        public EndPoint RemoteEndPoint;
        public IPPacketInformation PacketInformation;
    }
    public enum SocketShutdown
    {
        Both = 2,
        Receive = 0,
        Send = 1,
    }

    public static partial class SocketTaskExtensions
    {
        public static System.Threading.Tasks.Task<Socket> AcceptAsync(this System.Net.Sockets.Socket socket) { return default(System.Threading.Tasks.Task<Socket>); }
        public static System.Threading.Tasks.Task<Socket> AcceptAsync(this System.Net.Sockets.Socket socket, System.Net.Sockets.Socket acceptSocket) { return default(System.Threading.Tasks.Task<Socket>); }
        public static System.Threading.Tasks.Task ConnectAsync(this System.Net.Sockets.Socket socket, System.Net.EndPoint remoteEP) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task ConnectAsync(this System.Net.Sockets.Socket socket, System.Net.IPAddress address, int port) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task ConnectAsync(this System.Net.Sockets.Socket socket, System.Net.IPAddress[] addresses, int port) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task ConnectAsync(this System.Net.Sockets.Socket socket, string host, int port) { return default(System.Threading.Tasks.Task); }
        public static System.Threading.Tasks.Task<int> ReceiveAsync(this System.Net.Sockets.Socket socket, System.ArraySegment<byte> buffer, System.Net.Sockets.SocketFlags socketFlags) { return default(System.Threading.Tasks.Task<int>); }
        public static System.Threading.Tasks.Task<int> ReceiveAsync(this System.Net.Sockets.Socket socket, System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags) { return default(System.Threading.Tasks.Task<int>); }
        public static System.Threading.Tasks.Task<System.Net.Sockets.SocketReceiveFromResult> ReceiveFromAsync(this System.Net.Sockets.Socket socket, System.ArraySegment<byte> buffer, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEndPoint) { return default(System.Threading.Tasks.Task<System.Net.Sockets.SocketReceiveFromResult>); }
        public static System.Threading.Tasks.Task<System.Net.Sockets.SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this System.Net.Sockets.Socket socket, System.ArraySegment<byte> buffer, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEndPoint) { return default(System.Threading.Tasks.Task<System.Net.Sockets.SocketReceiveMessageFromResult>); }
        public static System.Threading.Tasks.Task<int> SendAsync(this System.Net.Sockets.Socket socket, System.ArraySegment<byte> buffer, System.Net.Sockets.SocketFlags socketFlags) { return default(System.Threading.Tasks.Task<int>); }
        public static System.Threading.Tasks.Task<int> SendAsync(this System.Net.Sockets.Socket socket, System.Collections.Generic.IList<System.ArraySegment<byte>> buffers, System.Net.Sockets.SocketFlags socketFlags) { return default(System.Threading.Tasks.Task<int>); }
        public static System.Threading.Tasks.Task<int> SendToAsync(this System.Net.Sockets.Socket socket, System.ArraySegment<byte> buffer, System.Net.Sockets.SocketFlags socketFlags, System.Net.EndPoint remoteEP) { return default(System.Threading.Tasks.Task<int>); }
    }
    public enum SocketType
    {
        Dgram = 2,
        Raw = 3,
        Rdm = 4,
        Seqpacket = 5,
        Stream = 1,
        Unknown = -1,
    }
    public partial class TcpClient : System.IDisposable
    {
        public TcpClient() { }
        public TcpClient(System.Net.Sockets.AddressFamily family) { }
        protected bool Active { get { return default(bool); } set { } }
        public int Available { get { return default(int); } }
        public bool Connected { get { return default(bool); } }
        public bool ExclusiveAddressUse { get { return default(bool); } set { } }
        public System.Net.Sockets.LingerOption LingerState { get { return default(System.Net.Sockets.LingerOption); } set { } }
        public bool NoDelay { get { return default(bool); } set { } }
        public int ReceiveBufferSize { get { return default(int); } set { } }
        public int ReceiveTimeout { get { return default(int); } set { } }
        public int SendBufferSize { get { return default(int); } set { } }
        public int SendTimeout { get { return default(int); } set { } }
        public System.Threading.Tasks.Task ConnectAsync(System.Net.IPAddress address, int port) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(System.Net.IPAddress[] addresses, int port) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(string host, int port) { return default(System.Threading.Tasks.Task); }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~TcpClient() { }
        public System.Net.Sockets.NetworkStream GetStream() { return default(System.Net.Sockets.NetworkStream); }
    }
    public partial class TcpListener
    {
        public TcpListener(System.Net.IPAddress localaddr, int port) { }
        public TcpListener(System.Net.IPEndPoint localEP) { }
        protected bool Active { get { return default(bool); } }
        public bool ExclusiveAddressUse { get { return default(bool); } set { } }
        public System.Net.EndPoint LocalEndpoint { get { return default(System.Net.EndPoint); } }
        public System.Net.Sockets.Socket Server { get { return default(System.Net.Sockets.Socket); } }
        public System.Threading.Tasks.Task<System.Net.Sockets.Socket> AcceptSocketAsync() { return default(System.Threading.Tasks.Task<System.Net.Sockets.Socket>); }
        public System.Threading.Tasks.Task<System.Net.Sockets.TcpClient> AcceptTcpClientAsync() { return default(System.Threading.Tasks.Task<System.Net.Sockets.TcpClient>); }
        public bool Pending() { return default(bool); }
        public void Start() { }
        public void Start(int backlog) { }
        public void Stop() { }
    }
    public partial class UdpClient : System.IDisposable
    {
        public UdpClient() { }
        public UdpClient(int port) { }
        public UdpClient(int port, System.Net.Sockets.AddressFamily family) { }
        public UdpClient(System.Net.IPEndPoint localEP) { }
        public UdpClient(System.Net.Sockets.AddressFamily family) { }
        protected bool Active { get { return default(bool); } set { } }
        public int Available { get { return default(int); } }
        public bool DontFragment { get { return default(bool); } set { } }
        public bool EnableBroadcast { get { return default(bool); } set { } }
        public bool ExclusiveAddressUse { get { return default(bool); } set { } }
        public bool MulticastLoopback { get { return default(bool); } set { } }
        public short Ttl { get { return default(short); } set { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public void DropMulticastGroup(System.Net.IPAddress multicastAddr) { }
        public void DropMulticastGroup(System.Net.IPAddress multicastAddr, int ifindex) { }
        public void JoinMulticastGroup(int ifindex, System.Net.IPAddress multicastAddr) { }
        public void JoinMulticastGroup(System.Net.IPAddress multicastAddr) { }
        public void JoinMulticastGroup(System.Net.IPAddress multicastAddr, int timeToLive) { }
        public void JoinMulticastGroup(System.Net.IPAddress multicastAddr, System.Net.IPAddress localAddress) { }
        public System.Threading.Tasks.Task<System.Net.Sockets.UdpReceiveResult> ReceiveAsync() { return default(System.Threading.Tasks.Task<System.Net.Sockets.UdpReceiveResult>); }
        public System.Threading.Tasks.Task<int> SendAsync(byte[] datagram, int bytes, System.Net.IPEndPoint endPoint) { return default(System.Threading.Tasks.Task<int>); }
        public System.Threading.Tasks.Task<int> SendAsync(byte[] datagram, int bytes, string hostname, int port) { return default(System.Threading.Tasks.Task<int>); }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct UdpReceiveResult : System.IEquatable<System.Net.Sockets.UdpReceiveResult>
    {
        public UdpReceiveResult(byte[] buffer, System.Net.IPEndPoint remoteEndPoint) { throw new System.NotImplementedException(); }
        public byte[] Buffer { get { return default(byte[]); } }
        public System.Net.IPEndPoint RemoteEndPoint { get { return default(System.Net.IPEndPoint); } }
        public bool Equals(System.Net.Sockets.UdpReceiveResult other) { return default(bool); }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Net.Sockets.UdpReceiveResult left, System.Net.Sockets.UdpReceiveResult right) { return default(bool); }
        public static bool operator !=(System.Net.Sockets.UdpReceiveResult left, System.Net.Sockets.UdpReceiveResult right) { return default(bool); }
    }
}

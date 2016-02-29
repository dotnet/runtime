// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
    [System.FlagsAttribute]
    public enum AuthenticationSchemes
    {
        Anonymous = 32768,
        Basic = 8,
        Digest = 1,
        IntegratedWindowsAuthentication = 6,
        Negotiate = 2,
        None = 0,
        Ntlm = 4,
    }
    public sealed partial class Cookie
    {
        public Cookie() { }
        public Cookie(string name, string value) { }
        public Cookie(string name, string value, string path) { }
        public Cookie(string name, string value, string path, string domain) { }
        public string Comment { get { return default(string); } set { } }
        public System.Uri CommentUri { get { return default(System.Uri); } set { } }
        public bool Discard { get { return default(bool); } set { } }
        public string Domain { get { return default(string); } set { } }
        public bool Expired { get { return default(bool); } set { } }
        public System.DateTime Expires { get { return default(System.DateTime); } set { } }
        public bool HttpOnly { get { return default(bool); } set { } }
        public string Name { get { return default(string); } set { } }
        public string Path { get { return default(string); } set { } }
        public string Port { get { return default(string); } set { } }
        public bool Secure { get { return default(bool); } set { } }
        public System.DateTime TimeStamp { get { return default(System.DateTime); } }
        public string Value { get { return default(string); } set { } }
        public int Version { get { return default(int); } set { } }
        public override bool Equals(object comparand) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class CookieCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CookieCollection() { }
        public int Count { get { return default(int); } }
        public System.Net.Cookie this[string name] { get { return default(System.Net.Cookie); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void Add(System.Net.Cookie cookie) { }
        public void Add(System.Net.CookieCollection cookies) { }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public partial class CookieContainer
    {
        public const int DefaultCookieLengthLimit = 4096;
        public const int DefaultCookieLimit = 300;
        public const int DefaultPerDomainCookieLimit = 20;
        public CookieContainer() { }
        public int Capacity { get { return default(int); } set { } }
        public int Count { get { return default(int); } }
        public int MaxCookieSize { get { return default(int); } set { } }
        public int PerDomainCapacity { get { return default(int); } set { } }
        public void Add(System.Uri uri, System.Net.Cookie cookie) { }
        public void Add(System.Uri uri, System.Net.CookieCollection cookies) { }
        public string GetCookieHeader(System.Uri uri) { return default(string); }
        public System.Net.CookieCollection GetCookies(System.Uri uri) { return default(System.Net.CookieCollection); }
        public void SetCookies(System.Uri uri, string cookieHeader) { }
    }
    public partial class CookieException : System.FormatException
    {
        public CookieException() { }
    }
    public partial class CredentialCache : System.Collections.IEnumerable, System.Net.ICredentials, System.Net.ICredentialsByHost
    {
        public CredentialCache() { }
        public static System.Net.ICredentials DefaultCredentials { get { return default(System.Net.ICredentials); } }
        public static System.Net.NetworkCredential DefaultNetworkCredentials { get { return default(System.Net.NetworkCredential); } }
        public void Add(string host, int port, string authenticationType, System.Net.NetworkCredential credential) { }
        public void Add(System.Uri uriPrefix, string authType, System.Net.NetworkCredential cred) { }
        public System.Net.NetworkCredential GetCredential(string host, int port, string authenticationType) { return default(System.Net.NetworkCredential); }
        public System.Net.NetworkCredential GetCredential(System.Uri uriPrefix, string authType) { return default(System.Net.NetworkCredential); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public void Remove(string host, int port, string authenticationType) { }
        public void Remove(System.Uri uriPrefix, string authType) { }
    }
    [System.FlagsAttribute]
    public enum DecompressionMethods
    {
        Deflate = 2,
        GZip = 1,
        None = 0,
    }
    public partial class DnsEndPoint : System.Net.EndPoint
    {
        public DnsEndPoint(string host, int port) { }
        public DnsEndPoint(string host, int port, System.Net.Sockets.AddressFamily addressFamily) { }
        public override System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public string Host { get { return default(string); } }
        public int Port { get { return default(int); } }
        public override bool Equals(object comparand) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public abstract partial class EndPoint
    {
        protected EndPoint() { }
        public virtual System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public virtual System.Net.EndPoint Create(System.Net.SocketAddress socketAddress) { return default(System.Net.EndPoint); }
        public virtual System.Net.SocketAddress Serialize() { return default(System.Net.SocketAddress); }
    }
    public enum HttpStatusCode
    {
        Accepted = 202,
        Ambiguous = 300,
        BadGateway = 502,
        BadRequest = 400,
        Conflict = 409,
        Continue = 100,
        Created = 201,
        ExpectationFailed = 417,
        Forbidden = 403,
        Found = 302,
        GatewayTimeout = 504,
        Gone = 410,
        HttpVersionNotSupported = 505,
        InternalServerError = 500,
        LengthRequired = 411,
        MethodNotAllowed = 405,
        Moved = 301,
        MovedPermanently = 301,
        MultipleChoices = 300,
        NoContent = 204,
        NonAuthoritativeInformation = 203,
        NotAcceptable = 406,
        NotFound = 404,
        NotImplemented = 501,
        NotModified = 304,
        OK = 200,
        PartialContent = 206,
        PaymentRequired = 402,
        PreconditionFailed = 412,
        ProxyAuthenticationRequired = 407,
        Redirect = 302,
        RedirectKeepVerb = 307,
        RedirectMethod = 303,
        RequestedRangeNotSatisfiable = 416,
        RequestEntityTooLarge = 413,
        RequestTimeout = 408,
        RequestUriTooLong = 414,
        ResetContent = 205,
        SeeOther = 303,
        ServiceUnavailable = 503,
        SwitchingProtocols = 101,
        TemporaryRedirect = 307,
        Unauthorized = 401,
        UnsupportedMediaType = 415,
        Unused = 306,
        UpgradeRequired = 426,
        UseProxy = 305,
    }
    public partial interface ICredentials
    {
        System.Net.NetworkCredential GetCredential(System.Uri uri, string authType);
    }
    public partial interface ICredentialsByHost
    {
        System.Net.NetworkCredential GetCredential(string host, int port, string authenticationType);
    }
    public partial class IPAddress
    {
        public static readonly System.Net.IPAddress Any;
        public static readonly System.Net.IPAddress Broadcast;
        public static readonly System.Net.IPAddress IPv6Any;
        public static readonly System.Net.IPAddress IPv6Loopback;
        public static readonly System.Net.IPAddress IPv6None;
        public static readonly System.Net.IPAddress Loopback;
        public static readonly System.Net.IPAddress None;
        public IPAddress(byte[] address) { }
        public IPAddress(byte[] address, long scopeid) { }
        public IPAddress(long newAddress) { }
        public System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public bool IsIPv4MappedToIPv6 { get { return default(bool); } }
        public bool IsIPv6LinkLocal { get { return default(bool); } }
        public bool IsIPv6Multicast { get { return default(bool); } }
        public bool IsIPv6SiteLocal { get { return default(bool); } }
        public bool IsIPv6Teredo { get { return default(bool); } }
        public long ScopeId { get { return default(long); } set { } }
        public override bool Equals(object comparand) { return default(bool); }
        public byte[] GetAddressBytes() { return default(byte[]); }
        public override int GetHashCode() { return default(int); }
        public static short HostToNetworkOrder(short host) { return default(short); }
        public static int HostToNetworkOrder(int host) { return default(int); }
        public static long HostToNetworkOrder(long host) { return default(long); }
        public static bool IsLoopback(System.Net.IPAddress address) { return default(bool); }
        public System.Net.IPAddress MapToIPv4() { return default(System.Net.IPAddress); }
        public System.Net.IPAddress MapToIPv6() { return default(System.Net.IPAddress); }
        public static short NetworkToHostOrder(short network) { return default(short); }
        public static int NetworkToHostOrder(int network) { return default(int); }
        public static long NetworkToHostOrder(long network) { return default(long); }
        public static System.Net.IPAddress Parse(string ipString) { return default(System.Net.IPAddress); }
        public override string ToString() { return default(string); }
        public static bool TryParse(string ipString, out System.Net.IPAddress address) { address = default(System.Net.IPAddress); return default(bool); }
    }
    public partial class IPEndPoint : System.Net.EndPoint
    {
        public const int MaxPort = 65535;
        public const int MinPort = 0;
        public IPEndPoint(long address, int port) { }
        public IPEndPoint(System.Net.IPAddress address, int port) { }
        public System.Net.IPAddress Address { get { return default(System.Net.IPAddress); } set { } }
        public override System.Net.Sockets.AddressFamily AddressFamily { get { return default(System.Net.Sockets.AddressFamily); } }
        public int Port { get { return default(int); } set { } }
        public override System.Net.EndPoint Create(System.Net.SocketAddress socketAddress) { return default(System.Net.EndPoint); }
        public override bool Equals(object comparand) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override System.Net.SocketAddress Serialize() { return default(System.Net.SocketAddress); }
        public override string ToString() { return default(string); }
    }
    public partial interface IWebProxy
    {
        System.Net.ICredentials Credentials { get; set; }
        System.Uri GetProxy(System.Uri destination);
        bool IsBypassed(System.Uri host);
    }
    public partial class NetworkCredential : System.Net.ICredentials, System.Net.ICredentialsByHost
    {
        public NetworkCredential() { }
        public NetworkCredential(string userName, string password) { }
        public NetworkCredential(string userName, string password, string domain) { }
        public string Domain { get { return default(string); } set { } }
        public string Password { get { return default(string); } set { } }
        public string UserName { get { return default(string); } set { } }
        public System.Net.NetworkCredential GetCredential(string host, int port, string authenticationType) { return default(System.Net.NetworkCredential); }
        public System.Net.NetworkCredential GetCredential(System.Uri uri, string authType) { return default(System.Net.NetworkCredential); }
    }
    public partial class SocketAddress
    {
        public SocketAddress(System.Net.Sockets.AddressFamily family) { }
        public SocketAddress(System.Net.Sockets.AddressFamily family, int size) { }
        public System.Net.Sockets.AddressFamily Family { get { return default(System.Net.Sockets.AddressFamily); } }
        public byte this[int offset] { get { return default(byte); } set { } }
        public int Size { get { return default(int); } }
        public override bool Equals(object comparand) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public abstract partial class TransportContext
    {
        protected TransportContext() { }
        public abstract System.Security.Authentication.ExtendedProtection.ChannelBinding GetChannelBinding(System.Security.Authentication.ExtendedProtection.ChannelBindingKind kind);
    }
}
namespace System.Net.NetworkInformation
{
    public partial class IPAddressCollection : System.Collections.Generic.ICollection<System.Net.IPAddress>, System.Collections.Generic.IEnumerable<System.Net.IPAddress>, System.Collections.IEnumerable
    {
        protected internal IPAddressCollection() { }
        public virtual int Count { get { return default(int); } }
        public virtual bool IsReadOnly { get { return default(bool); } }
        public virtual System.Net.IPAddress this[int index] { get { return default(System.Net.IPAddress); } }
        public virtual void Add(System.Net.IPAddress address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.IPAddress address) { return default(bool); }
        public virtual void CopyTo(System.Net.IPAddress[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.IPAddress> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Net.IPAddress>); }
        public virtual bool Remove(System.Net.IPAddress address) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
}
namespace System.Net.Security
{
    public enum AuthenticationLevel
    {
        MutualAuthRequested = 1,
        MutualAuthRequired = 2,
        None = 0,
    }
    [System.FlagsAttribute]
    public enum SslPolicyErrors
    {
        None = 0,
        RemoteCertificateChainErrors = 4,
        RemoteCertificateNameMismatch = 2,
        RemoteCertificateNotAvailable = 1,
    }
}
namespace System.Net.Sockets
{
    public enum AddressFamily
    {
        AppleTalk = 16,
        Atm = 22,
        Banyan = 21,
        Ccitt = 10,
        Chaos = 5,
        Cluster = 24,
        DataKit = 9,
        DataLink = 13,
        DecNet = 12,
        Ecma = 8,
        FireFox = 19,
        HyperChannel = 15,
        Ieee12844 = 25,
        ImpLink = 3,
        InterNetwork = 2,
        InterNetworkV6 = 23,
        Ipx = 6,
        Irda = 26,
        Iso = 7,
        Lat = 14,
        NetBios = 17,
        NetworkDesigners = 28,
        NS = 6,
        Osi = 7,
        Pup = 4,
        Sna = 11,
        Unix = 1,
        Unknown = -1,
        Unspecified = 0,
        VoiceView = 18,
    }
    public enum SocketError
    {
        AccessDenied = 10013,
        AddressAlreadyInUse = 10048,
        AddressFamilyNotSupported = 10047,
        AddressNotAvailable = 10049,
        AlreadyInProgress = 10037,
        ConnectionAborted = 10053,
        ConnectionRefused = 10061,
        ConnectionReset = 10054,
        DestinationAddressRequired = 10039,
        Disconnecting = 10101,
        Fault = 10014,
        HostDown = 10064,
        HostNotFound = 11001,
        HostUnreachable = 10065,
        InProgress = 10036,
        Interrupted = 10004,
        InvalidArgument = 10022,
        IOPending = 997,
        IsConnected = 10056,
        MessageSize = 10040,
        NetworkDown = 10050,
        NetworkReset = 10052,
        NetworkUnreachable = 10051,
        NoBufferSpaceAvailable = 10055,
        NoData = 11004,
        NoRecovery = 11003,
        NotConnected = 10057,
        NotInitialized = 10093,
        NotSocket = 10038,
        OperationAborted = 995,
        OperationNotSupported = 10045,
        ProcessLimit = 10067,
        ProtocolFamilyNotSupported = 10046,
        ProtocolNotSupported = 10043,
        ProtocolOption = 10042,
        ProtocolType = 10041,
        Shutdown = 10058,
        SocketError = -1,
        SocketNotSupported = 10044,
        Success = 0,
        SystemNotReady = 10091,
        TimedOut = 10060,
        TooManyOpenSockets = 10024,
        TryAgain = 11002,
        TypeNotFound = 10109,
        VersionNotSupported = 10092,
        WouldBlock = 10035,
    }
    public partial class SocketException : System.Exception
    {
        public SocketException() { }
        public SocketException(int errorCode) { }
        public System.Net.Sockets.SocketError SocketErrorCode { get { return default(System.Net.Sockets.SocketError); } }
    }
}
namespace System.Security.Authentication
{
    public enum CipherAlgorithmType
    {
        Aes = 26129,
        Aes128 = 26126,
        Aes192 = 26127,
        Aes256 = 26128,
        Des = 26113,
        None = 0,
        Null = 24576,
        Rc2 = 26114,
        Rc4 = 26625,
        TripleDes = 26115,
    }
    public enum ExchangeAlgorithmType
    {
        DiffieHellman = 43522,
        None = 0,
        RsaKeyX = 41984,
        RsaSign = 9216,
    }
    public enum HashAlgorithmType
    {
        Md5 = 32771,
        None = 0,
        Sha1 = 32772,
    }
    [System.FlagsAttribute]
    public enum SslProtocols
    {
        None = 0,
        [Obsolete("This value has been deprecated.  It is no longer supported. http://go.microsoft.com/fwlink/?linkid=14202")]
        Ssl2 = 12,
        [Obsolete("This value has been deprecated.  It is no longer supported. http://go.microsoft.com/fwlink/?linkid=14202")]
        Ssl3 = 48,
        Tls = 192,
        Tls11 = 768,
        Tls12 = 3072,
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public abstract partial class ChannelBinding : System.Runtime.InteropServices.SafeHandle
    {
        protected ChannelBinding() : base(default(System.IntPtr), default(bool)) { }
        protected ChannelBinding(bool ownsHandle) : base(default(System.IntPtr), default(bool)) { }
        public abstract int Size { get; }
    }
    public enum ChannelBindingKind
    {
        Endpoint = 26,
        Unique = 25,
        Unknown = 0,
    }
}

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
        public string Comment { get { throw null; } set { } }
        public System.Uri CommentUri { get { throw null; } set { } }
        public bool Discard { get { throw null; } set { } }
        public string Domain { get { throw null; } set { } }
        public bool Expired { get { throw null; } set { } }
        public System.DateTime Expires { get { throw null; } set { } }
        public bool HttpOnly { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Path { get { throw null; } set { } }
        public string Port { get { throw null; } set { } }
        public bool Secure { get { throw null; } set { } }
        public System.DateTime TimeStamp { get { throw null; } }
        public string Value { get { throw null; } set { } }
        public int Version { get { throw null; } set { } }
        public override bool Equals(object comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class CookieCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CookieCollection() { }
        public int Count { get { throw null; } }
        public System.Net.Cookie this[string name] { get { throw null; } }
        public System.Net.Cookie this[int index] { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void Add(System.Net.Cookie cookie) { }
        public void Add(System.Net.CookieCollection cookies) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(Cookie[] array, int index) { }
        public bool IsReadOnly { get { throw null; } }
    }
    public partial class CookieContainer
    {
        public const int DefaultCookieLengthLimit = 4096;
        public const int DefaultCookieLimit = 300;
        public const int DefaultPerDomainCookieLimit = 20;
        public CookieContainer() { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public int MaxCookieSize { get { throw null; } set { } }
        public int PerDomainCapacity { get { throw null; } set { } }
        public void Add(System.Uri uri, System.Net.Cookie cookie) { }
        public void Add(System.Uri uri, System.Net.CookieCollection cookies) { }
        public string GetCookieHeader(System.Uri uri) { throw null; }
        public System.Net.CookieCollection GetCookies(System.Uri uri) { throw null; }
        public void SetCookies(System.Uri uri, string cookieHeader) { }
        public CookieContainer(int capacity) { }
        public CookieContainer(int capacity, int perDomainCapacity, int maxCookieSize) { }
        public void Add(CookieCollection cookies) { }
        public void Add(Cookie cookie) { }
    }
    public partial class CookieException : System.FormatException, System.Runtime.Serialization.ISerializable
    {
        public CookieException() { }
        protected CookieException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class CredentialCache : System.Collections.IEnumerable, System.Net.ICredentials, System.Net.ICredentialsByHost
    {
        public CredentialCache() { }
        public static System.Net.ICredentials DefaultCredentials { get { throw null; } }
        public static System.Net.NetworkCredential DefaultNetworkCredentials { get { throw null; } }
        public void Add(string host, int port, string authenticationType, System.Net.NetworkCredential credential) { }
        public void Add(System.Uri uriPrefix, string authType, System.Net.NetworkCredential cred) { }
        public System.Net.NetworkCredential GetCredential(string host, int port, string authenticationType) { throw null; }
        public System.Net.NetworkCredential GetCredential(System.Uri uriPrefix, string authType) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
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
        public override System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public string Host { get { throw null; } }
        public int Port { get { throw null; } }
        public override bool Equals(object comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class EndPoint
    {
        protected EndPoint() { }
        public virtual System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public virtual System.Net.EndPoint Create(System.Net.SocketAddress socketAddress) { throw null; }
        public virtual System.Net.SocketAddress Serialize() { throw null; }
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
        public System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public bool IsIPv4MappedToIPv6 { get { throw null; } }
        public bool IsIPv6LinkLocal { get { throw null; } }
        public bool IsIPv6Multicast { get { throw null; } }
        public bool IsIPv6SiteLocal { get { throw null; } }
        public bool IsIPv6Teredo { get { throw null; } }
        public long ScopeId { get { throw null; } set { } }
        public override bool Equals(object comparand) { throw null; }
        public byte[] GetAddressBytes() { throw null; }
        public override int GetHashCode() { throw null; }
        public static short HostToNetworkOrder(short host) { throw null; }
        public static int HostToNetworkOrder(int host) { throw null; }
        public static long HostToNetworkOrder(long host) { throw null; }
        public static bool IsLoopback(System.Net.IPAddress address) { throw null; }
        public System.Net.IPAddress MapToIPv4() { throw null; }
        public System.Net.IPAddress MapToIPv6() { throw null; }
        public static short NetworkToHostOrder(short network) { throw null; }
        public static int NetworkToHostOrder(int network) { throw null; }
        public static long NetworkToHostOrder(long network) { throw null; }
        public static System.Net.IPAddress Parse(string ipString) { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse(string ipString, out System.Net.IPAddress address) { throw null; }
        [Obsolete("This property has been deprecated. It is address family dependent. Please use IPAddress.Equals method to perform comparisons.http://go.microsoft.com/fwlink/?linkid=14202")]
        public long Address { get { throw null; } set { } }
    }
    public partial class IPEndPoint : System.Net.EndPoint
    {
        public const int MaxPort = 65535;
        public const int MinPort = 0;
        public IPEndPoint(long address, int port) { }
        public IPEndPoint(System.Net.IPAddress address, int port) { }
        public System.Net.IPAddress Address { get { throw null; } set { } }
        public override System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public int Port { get { throw null; } set { } }
        public override System.Net.EndPoint Create(System.Net.SocketAddress socketAddress) { throw null; }
        public override bool Equals(object comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override System.Net.SocketAddress Serialize() { throw null; }
        public override string ToString() { throw null; }
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
        public string Domain { get { throw null; } set { } }
        public string Password { get { throw null; } set { } }
        public string UserName { get { throw null; } set { } }
        public System.Net.NetworkCredential GetCredential(string host, int port, string authenticationType) { throw null; }
        public System.Net.NetworkCredential GetCredential(System.Uri uri, string authType) { throw null; }
    }
    public partial class SocketAddress
    {
        public SocketAddress(System.Net.Sockets.AddressFamily family) { }
        public SocketAddress(System.Net.Sockets.AddressFamily family, int size) { }
        public System.Net.Sockets.AddressFamily Family { get { throw null; } }
        public byte this[int offset] { get { throw null; } set { } }
        public int Size { get { throw null; } }
        public override bool Equals(object comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class TransportContext
    {
        protected TransportContext() { }
        public abstract System.Security.Authentication.ExtendedProtection.ChannelBinding GetChannelBinding(System.Security.Authentication.ExtendedProtection.ChannelBindingKind kind);
    }

    public static class HttpVersion
    {
#if netcoreapp11
        public static readonly Version Unknown = new Version(0, 0);
#endif
        public static readonly Version Version10 = new Version(1, 0);
        public static readonly Version Version11 = new Version(1, 1);
#if netcoreapp11
        public static readonly Version Version20 = new Version(2, 0);
#endif
    }
}
namespace System.Net.Cache
{
    public enum RequestCacheLevel
    {
        Default = 0,
        BypassCache = 1,
        CacheOnly = 2,
        CacheIfAvailable = 3,
        Revalidate = 4,
        Reload = 5,
        NoCacheNoStore = 6
    }
    public class RequestCachePolicy
    {
        public RequestCachePolicy() { }
        public RequestCachePolicy(System.Net.Cache.RequestCacheLevel level) { }
        public System.Net.Cache.RequestCacheLevel Level { get { throw null; } }
        public override string ToString() { throw null; }
    }
}
namespace System.Net.NetworkInformation
{
    public partial class IPAddressCollection : System.Collections.Generic.ICollection<System.Net.IPAddress>, System.Collections.Generic.IEnumerable<System.Net.IPAddress>, System.Collections.IEnumerable
    {
        protected internal IPAddressCollection() { }
        public virtual int Count { get { throw null; } }
        public virtual bool IsReadOnly { get { throw null; } }
        public virtual System.Net.IPAddress this[int index] { get { throw null; } }
        public virtual void Add(System.Net.IPAddress address) { }
        public virtual void Clear() { }
        public virtual bool Contains(System.Net.IPAddress address) { throw null; }
        public virtual void CopyTo(System.Net.IPAddress[] array, int offset) { }
        public virtual System.Collections.Generic.IEnumerator<System.Net.IPAddress> GetEnumerator() { throw null; }
        public virtual bool Remove(System.Net.IPAddress address) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
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
    public partial class SocketException : System.ComponentModel.Win32Exception
    {
        public SocketException() { }
        public SocketException(int errorCode) { }
        protected SocketException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override int ErrorCode { get { throw null; } }
        public override string Message { get { throw null; } }
        public System.Net.Sockets.SocketError SocketErrorCode { get { throw null; } }
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
#if netcoreapp11
        Sha256 = 32780,
        Sha384 = 32781,
        Sha512 = 32782
#endif
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
    public abstract partial class ChannelBinding : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected ChannelBinding() : base(default(bool)) { }
        protected ChannelBinding(bool ownsHandle) : base(default(bool)) { }
        public abstract int Size { get; }
    }
    public enum ChannelBindingKind
    {
        Endpoint = 26,
        Unique = 25,
        Unknown = 0,
    }
}

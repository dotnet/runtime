// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    [System.FlagsAttribute]
    public enum AuthenticationSchemes
    {
        None = 0,
        Digest = 1,
        Negotiate = 2,
        Ntlm = 4,
        IntegratedWindowsAuthentication = 6,
        Basic = 8,
        Anonymous = 32768,
    }
    public sealed partial class Cookie
    {
        public Cookie() { }
        public Cookie(string name, string? value) { }
        public Cookie(string name, string? value, string? path) { }
        public Cookie(string name, string? value, string? path, string? domain) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Comment { get { throw null; } set { } }
        public System.Uri? CommentUri { get { throw null; } set { } }
        public bool Discard { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Domain { get { throw null; } set { } }
        public bool Expired { get { throw null; } set { } }
        public System.DateTime Expires { get { throw null; } set { } }
        public bool HttpOnly { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Name { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Path { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Port { get { throw null; } set { } }
        public bool Secure { get { throw null; } set { } }
        public System.DateTime TimeStamp { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Value { get { throw null; } set { } }
        public int Version { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class CookieCollection : System.Collections.Generic.ICollection<System.Net.Cookie>, System.Collections.Generic.IEnumerable<System.Net.Cookie>, System.Collections.Generic.IReadOnlyCollection<System.Net.Cookie>, System.Collections.ICollection, System.Collections.IEnumerable
    {
        public CookieCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Net.Cookie this[int index] { get { throw null; } }
        public System.Net.Cookie? this[string name] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void Add(System.Net.Cookie cookie) { }
        public void Add(System.Net.CookieCollection cookies) { }
        public void Clear() { }
        public bool Contains(System.Net.Cookie cookie) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Net.Cookie[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public bool Remove(System.Net.Cookie cookie) { throw null; }
        System.Collections.Generic.IEnumerator<System.Net.Cookie> System.Collections.Generic.IEnumerable<System.Net.Cookie>.GetEnumerator() { throw null; }
    }
    public partial class CookieContainer
    {
        public const int DefaultCookieLengthLimit = 4096;
        public const int DefaultCookieLimit = 300;
        public const int DefaultPerDomainCookieLimit = 20;
        public CookieContainer() { }
        public CookieContainer(int capacity) { }
        public CookieContainer(int capacity, int perDomainCapacity, int maxCookieSize) { }
        public int Capacity { get { throw null; } set { } }
        public int Count { get { throw null; } }
        public int MaxCookieSize { get { throw null; } set { } }
        public int PerDomainCapacity { get { throw null; } set { } }
        public void Add(System.Net.Cookie cookie) { }
        public void Add(System.Net.CookieCollection cookies) { }
        public void Add(System.Uri uri, System.Net.Cookie cookie) { }
        public void Add(System.Uri uri, System.Net.CookieCollection cookies) { }
        public string GetCookieHeader(System.Uri uri) { throw null; }
        public System.Net.CookieCollection GetCookies(System.Uri uri) { throw null; }
        public void SetCookies(System.Uri uri, string cookieHeader) { }
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
        public System.Net.NetworkCredential? GetCredential(string host, int port, string authenticationType) { throw null; }
        public System.Net.NetworkCredential? GetCredential(System.Uri uriPrefix, string authType) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public void Remove(string? host, int port, string? authenticationType) { }
        public void Remove(System.Uri? uriPrefix, string? authType) { }
    }
    [System.FlagsAttribute]
    public enum DecompressionMethods
    {
        All = -1,
        None = 0,
        GZip = 1,
        Deflate = 2,
        Brotli = 4,
    }
    public partial class DnsEndPoint : System.Net.EndPoint
    {
        public DnsEndPoint(string host, int port) { }
        public DnsEndPoint(string host, int port, System.Net.Sockets.AddressFamily addressFamily) { }
        public override System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public string Host { get { throw null; } }
        public int Port { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? comparand) { throw null; }
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
        Continue = 100,
        SwitchingProtocols = 101,
        Processing = 102,
        EarlyHints = 103,
        OK = 200,
        Created = 201,
        Accepted = 202,
        NonAuthoritativeInformation = 203,
        NoContent = 204,
        ResetContent = 205,
        PartialContent = 206,
        MultiStatus = 207,
        AlreadyReported = 208,
        IMUsed = 226,
        Ambiguous = 300,
        MultipleChoices = 300,
        Moved = 301,
        MovedPermanently = 301,
        Found = 302,
        Redirect = 302,
        RedirectMethod = 303,
        SeeOther = 303,
        NotModified = 304,
        UseProxy = 305,
        Unused = 306,
        RedirectKeepVerb = 307,
        TemporaryRedirect = 307,
        PermanentRedirect = 308,
        BadRequest = 400,
        Unauthorized = 401,
        PaymentRequired = 402,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        NotAcceptable = 406,
        ProxyAuthenticationRequired = 407,
        RequestTimeout = 408,
        Conflict = 409,
        Gone = 410,
        LengthRequired = 411,
        PreconditionFailed = 412,
        RequestEntityTooLarge = 413,
        RequestUriTooLong = 414,
        UnsupportedMediaType = 415,
        RequestedRangeNotSatisfiable = 416,
        ExpectationFailed = 417,
        MisdirectedRequest = 421,
        UnprocessableEntity = 422,
        Locked = 423,
        FailedDependency = 424,
        UpgradeRequired = 426,
        PreconditionRequired = 428,
        TooManyRequests = 429,
        RequestHeaderFieldsTooLarge = 431,
        UnavailableForLegalReasons = 451,
        InternalServerError = 500,
        NotImplemented = 501,
        BadGateway = 502,
        ServiceUnavailable = 503,
        GatewayTimeout = 504,
        HttpVersionNotSupported = 505,
        VariantAlsoNegotiates = 506,
        InsufficientStorage = 507,
        LoopDetected = 508,
        NotExtended = 510,
        NetworkAuthenticationRequired = 511,
    }
    public static partial class HttpVersion
    {
        public static readonly System.Version Unknown;
        public static readonly System.Version Version10;
        public static readonly System.Version Version11;
        public static readonly System.Version Version20;
        public static readonly System.Version Version30;
    }
    public partial interface ICredentials
    {
        System.Net.NetworkCredential? GetCredential(System.Uri uri, string authType);
    }
    public partial interface ICredentialsByHost
    {
        System.Net.NetworkCredential? GetCredential(string host, int port, string authenticationType);
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
        public IPAddress(System.ReadOnlySpan<byte> address) { }
        public IPAddress(System.ReadOnlySpan<byte> address, long scopeid) { }
        [System.ObsoleteAttribute("This property has been deprecated. It is address family dependent. Please use IPAddress.Equals method to perform comparisons. https://go.microsoft.com/fwlink/?linkid=14202")]
        public long Address { get { throw null; } set { } }
        public System.Net.Sockets.AddressFamily AddressFamily { get { throw null; } }
        public bool IsIPv4MappedToIPv6 { get { throw null; } }
        public bool IsIPv6LinkLocal { get { throw null; } }
        public bool IsIPv6Multicast { get { throw null; } }
        public bool IsIPv6SiteLocal { get { throw null; } }
        public bool IsIPv6Teredo { get { throw null; } }
        public bool IsIPv6UniqueLocal { get { throw null; } }
        public long ScopeId { get { throw null; } set { } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? comparand) { throw null; }
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
        public static System.Net.IPAddress Parse(System.ReadOnlySpan<char> ipSpan) { throw null; }
        public static System.Net.IPAddress Parse(string ipString) { throw null; }
        public override string ToString() { throw null; }
        public bool TryFormat(System.Span<char> destination, out int charsWritten) { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> ipSpan, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.IPAddress? address) { throw null; }
        public static bool TryParse([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] string? ipString, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.IPAddress? address) { throw null; }
        public bool TryWriteBytes(System.Span<byte> destination, out int bytesWritten) { throw null; }
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
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Net.IPEndPoint Parse(System.ReadOnlySpan<char> s) { throw null; }
        public static System.Net.IPEndPoint Parse(string s) { throw null; }
        public override System.Net.SocketAddress Serialize() { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> s, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.IPEndPoint? result) { throw null; }
        public static bool TryParse(string s, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Net.IPEndPoint? result) { throw null; }
    }
    public partial interface IWebProxy
    {
        System.Net.ICredentials? Credentials { get; set; }
        System.Uri? GetProxy(System.Uri destination);
        bool IsBypassed(System.Uri host);
    }
    public partial class NetworkCredential : System.Net.ICredentials, System.Net.ICredentialsByHost
    {
        public NetworkCredential() { }
        [System.CLSCompliantAttribute(false)]
        public NetworkCredential(string? userName, System.Security.SecureString? password) { }
        [System.CLSCompliantAttribute(false)]
        public NetworkCredential(string? userName, System.Security.SecureString? password, string? domain) { }
        public NetworkCredential(string? userName, string? password) { }
        public NetworkCredential(string? userName, string? password, string? domain) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Domain { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Password { get { throw null; } set { } }
        [System.CLSCompliantAttribute(false)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public System.Security.SecureString SecurePassword { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string UserName { get { throw null; } set { } }
        public System.Net.NetworkCredential GetCredential(string? host, int port, string? authenticationType) { throw null; }
        public System.Net.NetworkCredential GetCredential(System.Uri? uri, string? authenticationType) { throw null; }
    }
    public partial class SocketAddress
    {
        public SocketAddress(System.Net.Sockets.AddressFamily family) { }
        public SocketAddress(System.Net.Sockets.AddressFamily family, int size) { }
        public System.Net.Sockets.AddressFamily Family { get { throw null; } }
        public byte this[int offset] { get { throw null; } set { } }
        public int Size { get { throw null; } }
        public override bool Equals(object? comparand) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public abstract partial class TransportContext
    {
        protected TransportContext() { }
        public abstract System.Security.Authentication.ExtendedProtection.ChannelBinding? GetChannelBinding(System.Security.Authentication.ExtendedProtection.ChannelBindingKind kind);
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
        NoCacheNoStore = 6,
    }
    public partial class RequestCachePolicy
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
        None = 0,
        MutualAuthRequested = 1,
        MutualAuthRequired = 2,
    }
    [System.FlagsAttribute]
    public enum SslPolicyErrors
    {
        None = 0,
        RemoteCertificateNotAvailable = 1,
        RemoteCertificateNameMismatch = 2,
        RemoteCertificateChainErrors = 4,
    }
}
namespace System.Net.Sockets
{
    public enum AddressFamily
    {
        Unknown = -1,
        Unspecified = 0,
        Unix = 1,
        InterNetwork = 2,
        ImpLink = 3,
        Pup = 4,
        Chaos = 5,
        Ipx = 6,
        NS = 6,
        Iso = 7,
        Osi = 7,
        Ecma = 8,
        DataKit = 9,
        Ccitt = 10,
        Sna = 11,
        DecNet = 12,
        DataLink = 13,
        Lat = 14,
        HyperChannel = 15,
        AppleTalk = 16,
        NetBios = 17,
        VoiceView = 18,
        FireFox = 19,
        Banyan = 21,
        Atm = 22,
        InterNetworkV6 = 23,
        Cluster = 24,
        Ieee12844 = 25,
        Irda = 26,
        NetworkDesigners = 28,
        Max = 29,
        Packet = 65536,
        ControllerAreaNetwork = 65537,
    }
    public enum SocketError
    {
        SocketError = -1,
        Success = 0,
        OperationAborted = 995,
        IOPending = 997,
        Interrupted = 10004,
        AccessDenied = 10013,
        Fault = 10014,
        InvalidArgument = 10022,
        TooManyOpenSockets = 10024,
        WouldBlock = 10035,
        InProgress = 10036,
        AlreadyInProgress = 10037,
        NotSocket = 10038,
        DestinationAddressRequired = 10039,
        MessageSize = 10040,
        ProtocolType = 10041,
        ProtocolOption = 10042,
        ProtocolNotSupported = 10043,
        SocketNotSupported = 10044,
        OperationNotSupported = 10045,
        ProtocolFamilyNotSupported = 10046,
        AddressFamilyNotSupported = 10047,
        AddressAlreadyInUse = 10048,
        AddressNotAvailable = 10049,
        NetworkDown = 10050,
        NetworkUnreachable = 10051,
        NetworkReset = 10052,
        ConnectionAborted = 10053,
        ConnectionReset = 10054,
        NoBufferSpaceAvailable = 10055,
        IsConnected = 10056,
        NotConnected = 10057,
        Shutdown = 10058,
        TimedOut = 10060,
        ConnectionRefused = 10061,
        HostDown = 10064,
        HostUnreachable = 10065,
        ProcessLimit = 10067,
        SystemNotReady = 10091,
        VersionNotSupported = 10092,
        NotInitialized = 10093,
        Disconnecting = 10101,
        TypeNotFound = 10109,
        HostNotFound = 11001,
        TryAgain = 11002,
        NoRecovery = 11003,
        NoData = 11004,
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
        None = 0,
        Null = 24576,
        Des = 26113,
        Rc2 = 26114,
        TripleDes = 26115,
        Aes128 = 26126,
        Aes192 = 26127,
        Aes256 = 26128,
        Aes = 26129,
        Rc4 = 26625,
    }
    public enum ExchangeAlgorithmType
    {
        None = 0,
        RsaSign = 9216,
        RsaKeyX = 41984,
        DiffieHellman = 43522,
    }
    public enum HashAlgorithmType
    {
        None = 0,
        Md5 = 32771,
        Sha1 = 32772,
        Sha256 = 32780,
        Sha384 = 32781,
        Sha512 = 32782,
    }
    [System.FlagsAttribute]
    public enum SslProtocols
    {
        None = 0,
        [System.ObsoleteAttribute("This value has been deprecated.  It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202")]
        Ssl2 = 12,
        [System.ObsoleteAttribute("This value has been deprecated.  It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202")]
        Ssl3 = 48,
        Tls = 192,
        [System.ObsoleteAttribute("This value has been deprecated.  It is no longer supported. https://go.microsoft.com/fwlink/?linkid=14202")]
        Default = 240,
        Tls11 = 768,
        Tls12 = 3072,
        Tls13 = 12288,
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public abstract partial class ChannelBinding : Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
    {
        protected ChannelBinding() : base (default(bool)) { }
        protected ChannelBinding(bool ownsHandle) : base (default(bool)) { }
        public abstract int Size { get; }
    }
    public enum ChannelBindingKind
    {
        Unknown = 0,
        Unique = 25,
        Endpoint = 26,
    }
}

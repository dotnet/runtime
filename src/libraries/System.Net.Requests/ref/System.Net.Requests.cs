// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public partial class AuthenticationManager
    {
        internal AuthenticationManager() { }
        public static System.Net.ICredentialPolicy? CredentialPolicy { get { throw null; } set { } }
        public static System.Collections.Specialized.StringDictionary CustomTargetNameDictionary { get { throw null; } }
        public static System.Collections.IEnumerator RegisteredModules { get { throw null; } }
        [System.ObsoleteAttribute("The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0009", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.Authorization? Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials) { throw null; }
        [System.ObsoleteAttribute("The AuthenticationManager Authenticate and PreAuthenticate methods are not supported and throw PlatformNotSupportedException.", DiagnosticId = "SYSLIB0009", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static System.Net.Authorization? PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials) { throw null; }
        public static void Register(System.Net.IAuthenticationModule authenticationModule) { }
        public static void Unregister(System.Net.IAuthenticationModule authenticationModule) { }
        public static void Unregister(string authenticationScheme) { }
    }
    public partial class Authorization
    {
        public Authorization(string? token) { }
        public Authorization(string? token, bool finished) { }
        public Authorization(string? token, bool finished, string? connectionGroupId) { }
        public bool Complete { get { throw null; } }
        public string? ConnectionGroupId { get { throw null; } }
        public string? Message { get { throw null; } }
        public bool MutuallyAuthenticated { get { throw null; } set { } }
        public string[]? ProtectionRealm { get { throw null; } set { } }
    }
    public partial class FileWebRequest : System.Net.WebRequest, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("Serialization is obsoleted for this type. https://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override string? ConnectionGroupName { get { throw null; } set { } }
        public override long ContentLength { get { throw null; } set { } }
        public override string? ContentType { get { throw null; } set { } }
        public override System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public override string Method { get { throw null; } set { } }
        public override bool PreAuthenticate { get { throw null; } set { } }
        public override System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public override int Timeout { get { throw null; } set { } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public override System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
        public override System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class FileWebResponse : System.Net.WebResponse, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("Serialization is obsoleted for this type. https://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override long ContentLength { get { throw null; } }
        public override string ContentType { get { throw null; } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public override System.Uri ResponseUri { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        public override void Close() { }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetResponseStream() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum FtpStatusCode
    {
        Undefined = 0,
        RestartMarker = 110,
        ServiceTemporarilyNotAvailable = 120,
        DataAlreadyOpen = 125,
        OpeningData = 150,
        CommandOK = 200,
        CommandExtraneous = 202,
        DirectoryStatus = 212,
        FileStatus = 213,
        SystemType = 215,
        SendUserCommand = 220,
        ClosingControl = 221,
        ClosingData = 226,
        EnteringPassive = 227,
        LoggedInProceed = 230,
        ServerWantsSecureSession = 234,
        FileActionOK = 250,
        PathnameCreated = 257,
        SendPasswordCommand = 331,
        NeedLoginAccount = 332,
        FileCommandPending = 350,
        ServiceNotAvailable = 421,
        CantOpenData = 425,
        ConnectionClosed = 426,
        ActionNotTakenFileUnavailableOrBusy = 450,
        ActionAbortedLocalProcessingError = 451,
        ActionNotTakenInsufficientSpace = 452,
        CommandSyntaxError = 500,
        ArgumentSyntaxError = 501,
        CommandNotImplemented = 502,
        BadCommandSequence = 503,
        NotLoggedIn = 530,
        AccountNeeded = 532,
        ActionNotTakenFileUnavailable = 550,
        ActionAbortedUnknownPageType = 551,
        FileActionAborted = 552,
        ActionNotTakenFilenameNotAllowed = 553,
    }
    public sealed partial class FtpWebRequest : System.Net.WebRequest
    {
        internal FtpWebRequest() { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public override string? ConnectionGroupName { get { throw null; } set { } }
        public override long ContentLength { get { throw null; } set { } }
        public long ContentOffset { get { throw null; } set { } }
        public override string? ContentType { get { throw null; } set { } }
        public override System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public static new System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw null; } set { } }
        public bool EnableSsl { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        public override string Method { get { throw null; } set { } }
        public override bool PreAuthenticate { get { throw null; } set { } }
        public override System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        public int ReadWriteTimeout { get { throw null; } set { } }
        public string? RenameTo { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public System.Net.ServicePoint ServicePoint { get { throw null; } }
        public override int Timeout { get { throw null; } set { } }
        public bool UseBinary { get { throw null; } set { } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public bool UsePassive { get { throw null; } set { } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
    }
    public partial class FtpWebResponse : System.Net.WebResponse, System.IDisposable
    {
        internal FtpWebResponse() { }
        public string? BannerMessage { get { throw null; } }
        public override long ContentLength { get { throw null; } }
        public string? ExitMessage { get { throw null; } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public System.DateTime LastModified { get { throw null; } }
        public override System.Uri ResponseUri { get { throw null; } }
        public System.Net.FtpStatusCode StatusCode { get { throw null; } }
        public string? StatusDescription { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        public string? WelcomeMessage { get { throw null; } }
        public override void Close() { }
        public override System.IO.Stream GetResponseStream() { throw null; }
    }
    [System.ObsoleteAttribute("This class has been deprecated. Please use WebRequest.DefaultWebProxy instead to access and set the global default proxy. Use 'null' instead of GetEmptyWebProxy. https://go.microsoft.com/fwlink/?linkid=14202")]
    public partial class GlobalProxySelection
    {
        public GlobalProxySelection() { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public static System.Net.IWebProxy Select { get { throw null; } set { } }
        public static System.Net.IWebProxy GetEmptyWebProxy() { throw null; }
    }
    public delegate void HttpContinueDelegate(int StatusCode, System.Net.WebHeaderCollection httpHeaders);
    public partial class HttpWebRequest : System.Net.WebRequest, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected HttpWebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string? Accept { get { throw null; } set { } }
        public System.Uri Address { get { throw null; } }
        public virtual bool AllowAutoRedirect { get { throw null; } set { } }
        public virtual bool AllowReadStreamBuffering { get { throw null; } set { } }
        public virtual bool AllowWriteStreamBuffering { get { throw null; } set { } }
        public System.Net.DecompressionMethods AutomaticDecompression { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public string? Connection { get { throw null; } set { } }
        public override string? ConnectionGroupName { get { throw null; } set { } }
        public override long ContentLength { get { throw null; } set { } }
        public override string? ContentType { get { throw null; } set { } }
        public System.Net.HttpContinueDelegate? ContinueDelegate { get { throw null; } set { } }
        public int ContinueTimeout { get { throw null; } set { } }
        public virtual System.Net.CookieContainer? CookieContainer { get { throw null; } set { } }
        public override System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public System.DateTime Date { get { throw null; } set { } }
        public static new System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw null; } set { } }
        public static int DefaultMaximumErrorResponseLength { get { throw null; } set { } }
        public static int DefaultMaximumResponseHeadersLength { get { throw null; } set { } }
        public string? Expect { get { throw null; } set { } }
        public virtual bool HaveResponse { get { throw null; } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public string Host { get { throw null; } set { } }
        public System.DateTime IfModifiedSince { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        public int MaximumAutomaticRedirections { get { throw null; } set { } }
        public int MaximumResponseHeadersLength { get { throw null; } set { } }
        public string? MediaType { get { throw null; } set { } }
        public override string Method { get { throw null; } set { } }
        public bool Pipelined { get { throw null; } set { } }
        public override bool PreAuthenticate { get { throw null; } set { } }
        public System.Version ProtocolVersion { get { throw null; } set { } }
        public override System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        public int ReadWriteTimeout { get { throw null; } set { } }
        public string? Referer { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public bool SendChunked { get { throw null; } set { } }
        public System.Net.Security.RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get { throw null; } set { } }
        public System.Net.ServicePoint ServicePoint { get { throw null; } }
        public virtual bool SupportsCookieContainer { get { throw null; } }
        public override int Timeout { get { throw null; } set { } }
        public string? TransferEncoding { get { throw null; } set { } }
        public bool UnsafeAuthenticatedConnectionSharing { get { throw null; } set { } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public string? UserAgent { get { throw null; } set { } }
        public override void Abort() { }
        public void AddRange(int range) { }
        public void AddRange(int from, int to) { }
        public void AddRange(long range) { }
        public void AddRange(long from, long to) { }
        public void AddRange(string rangeSpecifier, int range) { }
        public void AddRange(string rangeSpecifier, int from, int to) { }
        public void AddRange(string rangeSpecifier, long range) { }
        public void AddRange(string rangeSpecifier, long from, long to) { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult, out System.Net.TransportContext? context) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public System.IO.Stream GetRequestStream(out System.Net.TransportContext? context) { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class HttpWebResponse : System.Net.WebResponse, System.Runtime.Serialization.ISerializable
    {
        [System.ObsoleteAttribute("This API supports the .NET infrastructure and is not intended to be used directly from your code.", true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public HttpWebResponse() { }
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected HttpWebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string? CharacterSet { get { throw null; } }
        public string ContentEncoding { get { throw null; } }
        public override long ContentLength { get { throw null; } }
        public override string ContentType { get { throw null; } }
        public virtual System.Net.CookieCollection Cookies { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public override bool IsMutuallyAuthenticated { get { throw null; } }
        public System.DateTime LastModified { get { throw null; } }
        public virtual string Method { get { throw null; } }
        public System.Version ProtocolVersion { get { throw null; } }
        public override System.Uri ResponseUri { get { throw null; } }
        public string Server { get { throw null; } }
        public virtual System.Net.HttpStatusCode StatusCode { get { throw null; } }
        public virtual string StatusDescription { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public string GetResponseHeader(string headerName) { throw null; }
        public override System.IO.Stream GetResponseStream() { throw null; }
        [System.ObsoleteAttribute("Serialization is obsoleted for this type.  https://go.microsoft.com/fwlink/?linkid=14202")]
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial interface IAuthenticationModule
    {
        string AuthenticationType { get; }
        bool CanPreAuthenticate { get; }
        System.Net.Authorization? Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials);
        System.Net.Authorization? PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials);
    }
    public partial interface ICredentialPolicy
    {
        bool ShouldSendCredential(System.Uri challengeUri, System.Net.WebRequest request, System.Net.NetworkCredential credential, System.Net.IAuthenticationModule authenticationModule);
    }
    public partial interface IWebRequestCreate
    {
        System.Net.WebRequest Create(System.Uri uri);
    }
    public partial class ProtocolViolationException : System.InvalidOperationException, System.Runtime.Serialization.ISerializable
    {
        public ProtocolViolationException() { }
        protected ProtocolViolationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public ProtocolViolationException(string? message) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class WebException : System.InvalidOperationException, System.Runtime.Serialization.ISerializable
    {
        public WebException() { }
        protected WebException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public WebException(string? message) { }
        public WebException(string? message, System.Exception? innerException) { }
        public WebException(string? message, System.Exception? innerException, System.Net.WebExceptionStatus status, System.Net.WebResponse? response) { }
        public WebException(string? message, System.Net.WebExceptionStatus status) { }
        public System.Net.WebResponse? Response { get { throw null; } }
        public System.Net.WebExceptionStatus Status { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum WebExceptionStatus
    {
        Success = 0,
        NameResolutionFailure = 1,
        ConnectFailure = 2,
        ReceiveFailure = 3,
        SendFailure = 4,
        PipelineFailure = 5,
        RequestCanceled = 6,
        ProtocolError = 7,
        ConnectionClosed = 8,
        TrustFailure = 9,
        SecureChannelFailure = 10,
        ServerProtocolViolation = 11,
        KeepAliveFailure = 12,
        Pending = 13,
        Timeout = 14,
        ProxyNameResolutionFailure = 15,
        UnknownError = 16,
        MessageLengthLimitExceeded = 17,
        CacheEntryNotFound = 18,
        RequestProhibitedByCachePolicy = 19,
        RequestProhibitedByProxy = 20,
    }
    public abstract partial class WebRequest : System.MarshalByRefObject, System.Runtime.Serialization.ISerializable
    {
        protected WebRequest() { }
        protected WebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Net.Security.AuthenticationLevel AuthenticationLevel { get { throw null; } set { } }
        public virtual System.Net.Cache.RequestCachePolicy? CachePolicy { get { throw null; } set { } }
        public virtual string? ConnectionGroupName { get { throw null; } set { } }
        public virtual long ContentLength { get { throw null; } set { } }
        public virtual string? ContentType { get { throw null; } set { } }
        public virtual System.Net.ICredentials? Credentials { get { throw null; } set { } }
        public static System.Net.Cache.RequestCachePolicy? DefaultCachePolicy { get { throw null; } set { } }
        public static System.Net.IWebProxy? DefaultWebProxy { get { throw null; } set { } }
        public virtual System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw null; } set { } }
        public virtual string Method { get { throw null; } set { } }
        public virtual bool PreAuthenticate { get { throw null; } set { } }
        public virtual System.Net.IWebProxy? Proxy { get { throw null; } set { } }
        public virtual System.Uri RequestUri { get { throw null; } }
        public virtual int Timeout { get { throw null; } set { } }
        public virtual bool UseDefaultCredentials { get { throw null; } set { } }
        public virtual void Abort() { }
        public virtual System.IAsyncResult BeginGetRequestStream(System.AsyncCallback? callback, object? state) { throw null; }
        public virtual System.IAsyncResult BeginGetResponse(System.AsyncCallback? callback, object? state) { throw null; }
        public static System.Net.WebRequest Create(string requestUriString) { throw null; }
        public static System.Net.WebRequest Create(System.Uri requestUri) { throw null; }
        public static System.Net.WebRequest CreateDefault(System.Uri requestUri) { throw null; }
        public static System.Net.HttpWebRequest CreateHttp(string requestUriString) { throw null; }
        public static System.Net.HttpWebRequest CreateHttp(System.Uri requestUri) { throw null; }
        public virtual System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public virtual System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual System.IO.Stream GetRequestStream() { throw null; }
        public virtual System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { throw null; }
        public virtual System.Net.WebResponse GetResponse() { throw null; }
        public virtual System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { throw null; }
        public static System.Net.IWebProxy GetSystemWebProxy() { throw null; }
        public static bool RegisterPrefix(string prefix, System.Net.IWebRequestCreate creator) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public static partial class WebRequestMethods
    {
        public static partial class File
        {
            public const string DownloadFile = "GET";
            public const string UploadFile = "PUT";
        }
        public static partial class Ftp
        {
            public const string AppendFile = "APPE";
            public const string DeleteFile = "DELE";
            public const string DownloadFile = "RETR";
            public const string GetDateTimestamp = "MDTM";
            public const string GetFileSize = "SIZE";
            public const string ListDirectory = "NLST";
            public const string ListDirectoryDetails = "LIST";
            public const string MakeDirectory = "MKD";
            public const string PrintWorkingDirectory = "PWD";
            public const string RemoveDirectory = "RMD";
            public const string Rename = "RENAME";
            public const string UploadFile = "STOR";
            public const string UploadFileWithUniqueName = "STOU";
        }
        public static partial class Http
        {
            public const string Connect = "CONNECT";
            public const string Get = "GET";
            public const string Head = "HEAD";
            public const string MkCol = "MKCOL";
            public const string Post = "POST";
            public const string Put = "PUT";
        }
    }
    public abstract partial class WebResponse : System.MarshalByRefObject, System.IDisposable, System.Runtime.Serialization.ISerializable
    {
        protected WebResponse() { }
        protected WebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual long ContentLength { get { throw null; } set { } }
        public virtual string ContentType { get { throw null; } set { } }
        public virtual System.Net.WebHeaderCollection Headers { get { throw null; } }
        public virtual bool IsFromCache { get { throw null; } }
        public virtual bool IsMutuallyAuthenticated { get { throw null; } }
        public virtual System.Uri ResponseUri { get { throw null; } }
        public virtual bool SupportsHeaders { get { throw null; } }
        public virtual void Close() { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public virtual System.IO.Stream GetResponseStream() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
}
namespace System.Net.Cache
{
    public enum HttpCacheAgeControl
    {
        None = 0,
        MinFresh = 1,
        MaxAge = 2,
        MaxAgeAndMinFresh = 3,
        MaxStale = 4,
        MaxAgeAndMaxStale = 6,
    }
    public enum HttpRequestCacheLevel
    {
        Default = 0,
        BypassCache = 1,
        CacheOnly = 2,
        CacheIfAvailable = 3,
        Revalidate = 4,
        Reload = 5,
        NoCacheNoStore = 6,
        CacheOrNextCacheOnly = 7,
        Refresh = 8,
    }
    public partial class HttpRequestCachePolicy : System.Net.Cache.RequestCachePolicy
    {
        public HttpRequestCachePolicy() { }
        public HttpRequestCachePolicy(System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan ageOrFreshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale, System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel level) { }
        public System.DateTime CacheSyncDate { get { throw null; } }
        public new System.Net.Cache.HttpRequestCacheLevel Level { get { throw null; } }
        public System.TimeSpan MaxAge { get { throw null; } }
        public System.TimeSpan MaxStale { get { throw null; } }
        public System.TimeSpan MinFresh { get { throw null; } }
        public override string ToString() { throw null; }
    }
}

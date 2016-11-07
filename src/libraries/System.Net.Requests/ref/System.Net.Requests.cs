// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
    public delegate void HttpContinueDelegate(int StatusCode, WebHeaderCollection httpHeaders);
    public class AuthenticationManager
    {
        private AuthenticationManager() { }
        public static System.Net.ICredentialPolicy CredentialPolicy { get { throw null; } set { } }
        public static System.Collections.Specialized.StringDictionary CustomTargetNameDictionary { get { throw null; } }
        public static Authorization Authenticate(string challenge, System.Net.WebRequest request, ICredentials credentials) { throw null; }
        public static Authorization PreAuthenticate(System.Net.WebRequest request, ICredentials credentials) { throw null; }
        public static void Register(IAuthenticationModule authenticationModule) { }
        public static void Unregister(IAuthenticationModule authenticationModule) { }
        public static void Unregister(string authenticationScheme) { }
        public static System.Collections.IEnumerator RegisteredModules { get { throw null; } }
    }
    public class Authorization
    {
        public Authorization(string token) { }
        public Authorization(string token, bool finished) { }
        public Authorization(string token, bool finished, string connectionGroupId) { }

        public string Message { get { throw null; } }
        public string ConnectionGroupId { get { throw null; } }
        public bool Complete { get { throw null; } }
        public string[] ProtectionRealm { get { throw null; } set { } }
        public bool MutuallyAuthenticated { get { throw null; } set { } }
    }
    public class FileWebRequest : WebRequest, System.Runtime.Serialization.ISerializable
    {
        internal FileWebRequest() { }
        [Obsolete("Serialization is obsoleted for this type. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
        public override string ConnectionGroupName { get { throw null; } set { } }
        public override long ContentLength { get { throw null; } set { } }
        public override string ContentType { get { throw null; } set { } }
        public override System.Net.ICredentials Credentials { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public override string Method { get { throw null; } set { } }
        public override bool PreAuthenticate { get { throw null; } set { } }
        public override System.Net.IWebProxy Proxy { get { throw null; } set { } }
        public override int Timeout { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public override void Abort() { throw null; }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public class FileWebResponse : WebResponse, System.Runtime.Serialization.ISerializable
    {
        internal FileWebResponse() { }
        [Obsolete("Serialization is obsoleted for this type. http://go.microsoft.com/fwlink/?linkid=14202")]
        protected FileWebResponse(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) : base(serializationInfo, streamingContext) { }
        public override long ContentLength { get { throw null; } }
        public override string ContentType { get { throw null; } }
        public override WebHeaderCollection Headers { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        public override Uri ResponseUri { get { throw null; } }
        public override void Close() { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        protected override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override System.IO.Stream GetResponseStream() { throw null; }
    }
    public sealed class FtpWebRequest : WebRequest
    {
        internal FtpWebRequest() { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { throw null; } set { } }
        public override string ConnectionGroupName { get { throw null; } set { } }
        public override long ContentLength { get { throw null; } set { } }
        public long ContentOffset { get { throw null; } set { } }
        public override string ContentType { get { throw null; } set { } }
        public override System.Net.ICredentials Credentials { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public override string Method { get { throw null; } set { } }
        public override bool PreAuthenticate { get { throw null; } set { } }
        public override System.Net.IWebProxy Proxy { get { throw null; } set { } }
        public string RenameTo { get { throw null; } set { } }
        public override int Timeout { get { throw null; } set { } }
        public bool EnableSsl { get { throw null; } set { } }
        public bool UsePassive { get { throw null; } set { } }
        public bool UseBinary { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        public int ReadWriteTimeout { get { throw null; } set { } }
        public System.Net.ServicePoint ServicePoint { get { throw null; } }
        public static new System.Net.Cache.RequestCachePolicy DefaultCachePolicy { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public override void Abort() { throw null; }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
    }
    public class FtpWebResponse : WebResponse
    {
        internal FtpWebResponse() { }
        public override long ContentLength { get { throw null; } }
        public override string ContentType { get { throw null; } }
        public override WebHeaderCollection Headers { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        public override Uri ResponseUri { get { throw null; } }
        public override void Close() { throw null; }
        public override System.IO.Stream GetResponseStream() { throw null; }
        public string BannerMessage { get { throw null; } }
        public string ExitMessage { get { throw null; } }
        public string WelcomeMessage { get { throw null; } }
        public FtpStatusCode StatusCode { get { throw null; } } 
        public string StatusDescription { get { throw null; } }
        public DateTime LastModified { get { throw null; } }
    }
    public partial class HttpWebRequest : System.Net.WebRequest
    {
        internal HttpWebRequest() { }
        public string Accept { get { throw null; } set { } }
        public virtual bool AllowReadStreamBuffering { get { throw null; } set { } }
        public override string ContentType { get { throw null; } set { } }
        public int ContinueTimeout { get { throw null; } set { } }
        public virtual System.Net.CookieContainer CookieContainer { get { throw null; } set { } }
        public override System.Net.ICredentials Credentials { get { throw null; } set { } }
        public virtual bool HaveResponse { get { throw null; } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public override string Method { get { throw null; } set { } }
        public override System.Uri RequestUri { get { throw null; } }
        public virtual bool SupportsCookieContainer { get { throw null; } }
        public override bool UseDefaultCredentials { get { throw null; } set { } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { throw null; }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { throw null; }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult, out System.Net.TransportContext context) { throw null; }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        public override System.Net.WebResponse GetResponse() { throw null; }
        public virtual bool AllowWriteStreamBuffering { get { throw null; } set { } }        
        public virtual bool AllowAutoRedirect { get { throw null; } set { } }
        public Uri Address { get { throw null; } }
        public DecompressionMethods AutomaticDecompression { get { throw null; } set { } }
        public DateTime Date { get { throw null; } set { } }
        public static int DefaultMaximumResponseHeadersLength { get { throw null; } set { } }
        public static int DefaultMaximumErrorResponseLength { get { throw null; } set { } }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates {
            get { throw null; } set { } }
        public string Expect { get { throw null; } set { } }
        public DateTime IfModifiedSince { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        public int MaximumAutomaticRedirections { get { throw null; } set { } }
        public int MaximumResponseHeadersLength { get { throw null; } set { } }
        public string MediaType { get { throw null; } set { } }        
        public System.IO.Stream GetRequestStream(out TransportContext context) { throw null; }
        public bool Pipelined { get { throw null; } set { } }
        public Version ProtocolVersion { get { throw null; } set { } }
        public int ReadWriteTimeout { get { throw null; } set { } }
        public string Referer { get { throw null; } set { } }
        public bool SendChunked { get { throw null; } set { } }
        public ServicePoint ServicePoint { get { throw null; } }
        public string TransferEncoding { get { throw null; } set { } }
        public string UserAgent { get { throw null; } set { } }
        public bool UnsafeAuthenticatedConnectionSharing { get { throw null; } set { } }
        public override System.IO.Stream GetRequestStream() { throw null; }
        public void AddRange(int range) { }
        public void AddRange(int from,int to) { }
        public void AddRange(long range) { }
        public void AddRange(long from,long to) { }
        public void AddRange(string rangeSpecifier, int range) { }
        public void AddRange(string rangeSpecifier, int from,int to) { }
        public void AddRange(string rangeSpecifier, long range) { }
        public void AddRange(string rangeSpecifier, long from,long to) { }
        public string Host { get { throw null; } set { } }
        public override string ConnectionGroupName { get { throw null; } set { } }
        public System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get; set; }
        public HttpContinueDelegate ContinueDelegate { get { throw null; } set { } }
    }
    public partial class HttpWebResponse : System.Net.WebResponse
    {
        public HttpWebResponse() { }
        public override long ContentLength { get { throw null; } }
        public override string ContentType { get { throw null; } }
        public virtual System.Net.CookieCollection Cookies { get { throw null; } set { } }
        public override System.Net.WebHeaderCollection Headers { get { throw null; } }
        public virtual string Method { get { throw null; } }
        public override System.Uri ResponseUri { get { throw null; } }
        public virtual System.Net.HttpStatusCode StatusCode { get { throw null; } }
        public virtual string StatusDescription { get { throw null; } }
        public override bool SupportsHeaders { get { throw null; } }
        protected override void Dispose(bool disposing) { }
        public override System.IO.Stream GetResponseStream() { throw null; }
        public string GetResponseHeader(string headerName) { throw null; }        
        public Version ProtocolVersion { get { throw null; } }        
        public DateTime LastModified { get { throw null; } }        
        public String ContentEncoding { get { throw null; } }
        public string CharacterSet { get { throw null; } }
        public string Server { get { throw null; } }
        public override void Close() { throw null; }
    }
    public interface IAuthenticationModule
    {
        System.Net.Authorization Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials);
        System.Net.Authorization PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials);
        bool CanPreAuthenticate { get; }
        string AuthenticationType { get; }
    }
    public interface ICredentialPolicy
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
        public ProtocolViolationException(string message) { }
        protected ProtocolViolationException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public partial class WebException : System.InvalidOperationException , System.Runtime.Serialization.ISerializable
    {
        public WebException() { }
        public WebException(string message) { }
        public WebException(string message, System.Exception innerException) { }
        public WebException(string message, System.Exception innerException, System.Net.WebExceptionStatus status, System.Net.WebResponse response) { }
        public WebException(string message, System.Net.WebExceptionStatus status) { }
        public System.Net.WebResponse Response { get { throw null; } }
        public System.Net.WebExceptionStatus Status { get { throw null; } }
        protected WebException(System.Runtime.Serialization.SerializationInfo serializationInfo, 
            System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, 
            System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo,
            System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public enum WebExceptionStatus
    {
        CacheEntryNotFound = 18,
        ConnectFailure = 2,
        ConnectionClosed = 8,
        KeepAliveFailure = 12,
        MessageLengthLimitExceeded = 17,
        NameResolutionFailure = 1,
        Pending = 13,
        PipelineFailure = 5,
        ProtocolError = 7,
        ProxyNameResolutionFailure = 15,
        ReceiveFailure = 3,
        RequestCanceled = 6,
        RequestProhibitedByCachePolicy = 19,
        RequestProhibitedByProxy = 20,
        SecureChannelFailure = 10,
        SendFailure = 4,
        ServerProtocolViolation = 11,
        Success = 0,
        Timeout = 14,
        TrustFailure = 9,
        UnknownError = 16
    }
    public abstract partial class WebRequest : System.MarshalByRefObject, System.Runtime.Serialization.ISerializable
    {
        protected WebRequest() { }
        protected WebRequest(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public System.Net.Security.AuthenticationLevel AuthenticationLevel { get { throw null; } set { } }
        public virtual long ContentLength { get { throw null; } set { } }
        public virtual string ContentType { get { throw null; } set { } }
        public virtual System.Net.Cache.RequestCachePolicy CachePolicy { get { throw null; } set { } }
        public virtual string ConnectionGroupName { get { throw null; } set { } }
        public virtual System.Net.ICredentials Credentials { get { throw null; } set { } }
        public static System.Net.Cache.RequestCachePolicy DefaultCachePolicy { get { throw null; } set { } }
        public static System.Net.IWebProxy DefaultWebProxy { get { throw null; } set { } }
        public virtual System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public System.Security.Principal.TokenImpersonationLevel ImpersonationLevel { get { throw null; } set { } }
        public virtual string Method { get { throw null; } set { } }
        public virtual bool PreAuthenticate { get { throw null; } set { } }
        public virtual System.Net.IWebProxy Proxy { get { throw null; } set { } }
        public virtual System.Uri RequestUri { get { throw null; } }
        public virtual int Timeout { get { throw null; } set { } }
        public virtual bool UseDefaultCredentials { get { throw null; } set { } }
        public virtual void Abort() { throw null; }
        public virtual System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { throw null; }
        public virtual System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { throw null; }
        public static System.Net.WebRequest Create(string requestUriString) { throw null; }
        public static System.Net.WebRequest Create(System.Uri requestUri) { throw null; }
        public static System.Net.WebRequest CreateDefault(Uri requestUri) { throw null; }
        public static System.Net.HttpWebRequest CreateHttp(string requestUriString) { throw null; }
        public static System.Net.HttpWebRequest CreateHttp(System.Uri requestUri) { throw null; }
        public virtual System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { throw null; }
        public virtual System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { throw null; }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { throw null; }
        public virtual System.IO.Stream GetRequestStream() { throw null; }
        public virtual System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { throw null; }
        public virtual System.Net.WebResponse GetResponse() { throw null; }
        public virtual System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { throw null; }        
        public static System.Net.IWebProxy GetSystemWebProxy() { throw null; }
        public static bool RegisterPrefix(string prefix, System.Net.IWebRequestCreate creator) { throw null; }
    }
    public static class WebRequestMethods
    {
        public static class Ftp
        {
            public const string DownloadFile = "RETR";
            public const string ListDirectory = "NLST";
            public const string UploadFile = "STOR";
            public const string DeleteFile = "DELE";
            public const string AppendFile = "APPE";
            public const string GetFileSize = "SIZE";
            public const string UploadFileWithUniqueName = "STOU";
            public const string MakeDirectory = "MKD";
            public const string RemoveDirectory = "RMD";
            public const string ListDirectoryDetails = "LIST";
            public const string GetDateTimestamp = "MDTM";
            public const string PrintWorkingDirectory = "PWD";
            public const string Rename = "RENAME";
        }
        public static class Http
        {
            public const string Get = "GET";
            public const string Connect = "CONNECT";
            public const string Head = "HEAD";
            public const string Put = "PUT";
            public const string Post = "POST";
            public const string MkCol = "MKCOL";
        }
        public static class File
        {
            public const string DownloadFile = "GET";
            public const string UploadFile = "PUT";
        }
    }
    public abstract partial class WebResponse : System.MarshalByRefObject, System.Runtime.Serialization.ISerializable, System.IDisposable
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
        public virtual System.IO.Stream GetResponseStream() { throw null; }
        protected virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
    }
    public class GlobalProxySelection
    {
        public static IWebProxy Select { get { throw null; } set { } }
        public static IWebProxy GetEmptyWebProxy() { throw null; }
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
        ActionNotTakenFilenameNotAllowed = 553
    }
}
namespace System.Net.Cache
{
    public enum HttpCacheAgeControl
    {
        None = 0x0,
        MinFresh = 0x1,
        MaxAge = 0x2,
        MaxStale = 0x4,
        MaxAgeAndMinFresh = 0x3,
        MaxAgeAndMaxStale = 0x6
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
        Refresh = 8
    }
    public class HttpRequestCachePolicy : RequestCachePolicy
    {
        public HttpRequestCachePolicy() { }
        public HttpRequestCachePolicy(System.DateTime cacheSyncDate) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpRequestCacheLevel level) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan ageOrFreshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale) { }
        public HttpRequestCachePolicy(System.Net.Cache.HttpCacheAgeControl cacheAgeControl, System.TimeSpan maxAge, System.TimeSpan freshOrStale, System.DateTime cacheSyncDate) { }
        public new System.Net.Cache.HttpRequestCacheLevel Level { get { throw null; } }
        public System.DateTime CacheSyncDate { get { throw null; } }
        public System.TimeSpan MaxAge { get { throw null; } }
        public System.TimeSpan MinFresh { get { throw null; } }
        public System.TimeSpan MaxStale { get { throw null; } }
        public override string ToString() { throw null; }
    }
}

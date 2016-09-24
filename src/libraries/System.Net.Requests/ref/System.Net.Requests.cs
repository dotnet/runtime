// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net
{
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
    public delegate System.Net.IPEndPoint BindIPEndPoint(System.Net.ServicePoint servicePoint, System.Net.IPEndPoint remoteEndPoint, int retryCount);
    public partial class HttpWebRequest : System.Net.WebRequest
    {
        internal HttpWebRequest() { }
        public string Accept { get { return default(string); } set { } }
        public virtual bool AllowReadStreamBuffering { get { return default(bool); } set { } }
        public override string ContentType { get { return default(string); } set { } }
        public int ContinueTimeout { get { return default(int); } set { } }
        public virtual System.Net.CookieContainer CookieContainer { get { return default(System.Net.CookieContainer); } set { } }
        public override System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public virtual bool HaveResponse { get { return default(bool); } }
        public override System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } set { } }
        public override string Method { get { return default(string); } set { } }
        public override System.Uri RequestUri { get { return default(System.Uri); } }
        public virtual bool SupportsCookieContainer { get { return default(bool); } }
        public override bool UseDefaultCredentials { get { return default(bool); } set { } }
        public override void Abort() { }
        public override System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public override System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public override System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult) { return default(System.IO.Stream); }
        public override System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult) { return default(System.Net.WebResponse); }
    }
    public partial class HttpWebResponse : System.Net.WebResponse
    {
        internal HttpWebResponse() { }
        public override long ContentLength { get { return default(long); } }
        public override string ContentType { get { return default(string); } }
        public virtual System.Net.CookieCollection Cookies { get { return default(System.Net.CookieCollection); } }
        public override System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public virtual string Method { get { return default(string); } }
        public override System.Uri ResponseUri { get { return default(System.Uri); } }
        public virtual System.Net.HttpStatusCode StatusCode { get { return default(System.Net.HttpStatusCode); } }
        public virtual string StatusDescription { get { return default(string); } }
        public override bool SupportsHeaders { get { return default(bool); } }
        protected override void Dispose(bool disposing) { }
        public override System.IO.Stream GetResponseStream() { return default(System.IO.Stream); }
    }
    public interface IAuthenticationModule
    {
        System.Net.Authorization Authenticate(string challenge, System.Net.WebRequest request, System.Net.ICredentials credentials);
        System.Net.Authorization PreAuthenticate(System.Net.WebRequest request, System.Net.ICredentials credentials);
        bool CanPreAuthenticate { get; }
        string AuthenticationType { get; }
    }
    public interface ICertificatePolicy
    {
        bool CheckValidationResult(System.Net.ServicePoint srvPoint, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Net.WebRequest request, int certificateProblem);
    }
    public interface ICredentialPolicy
    {
        bool ShouldSendCredential(System.Uri challengeUri, System.Net.WebRequest request, System.Net.NetworkCredential credential, System.Net.IAuthenticationModule authenticationModule);
    }
    public partial interface IWebRequestCreate
    {
        System.Net.WebRequest Create(System.Uri uri);
    }
    public partial class ProtocolViolationException : System.InvalidOperationException
    {
        public ProtocolViolationException() { }
        public ProtocolViolationException(string message) { }
    }
    public partial class WebException : System.InvalidOperationException
    {
        public WebException() { }
        public WebException(string message) { }
        public WebException(string message, System.Exception innerException) { }
        public WebException(string message, System.Exception innerException, System.Net.WebExceptionStatus status, System.Net.WebResponse response) { }
        public WebException(string message, System.Net.WebExceptionStatus status) { }
        public System.Net.WebResponse Response { get { return default(System.Net.WebResponse); } }
        public System.Net.WebExceptionStatus Status { get { return default(System.Net.WebExceptionStatus); } }
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
    public abstract partial class WebRequest
    {
        protected WebRequest() { }
        public abstract string ContentType { get; set; }
        public virtual System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public static System.Net.IWebProxy DefaultWebProxy { get { return default(System.Net.IWebProxy); } set { } }
        public abstract System.Net.WebHeaderCollection Headers { get; set; }
        public abstract string Method { get; set; }
        public virtual System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        public abstract System.Uri RequestUri { get; }
        public virtual bool UseDefaultCredentials { get { return default(bool); } set { } }
        public abstract void Abort();
        public abstract System.IAsyncResult BeginGetRequestStream(System.AsyncCallback callback, object state);
        public abstract System.IAsyncResult BeginGetResponse(System.AsyncCallback callback, object state);
        public static System.Net.WebRequest Create(string requestUriString) { return default(System.Net.WebRequest); }
        public static System.Net.WebRequest Create(System.Uri requestUri) { return default(System.Net.WebRequest); }
        public static System.Net.HttpWebRequest CreateHttp(string requestUriString) { return default(System.Net.HttpWebRequest); }
        public static System.Net.HttpWebRequest CreateHttp(System.Uri requestUri) { return default(System.Net.HttpWebRequest); }
        public abstract System.IO.Stream EndGetRequestStream(System.IAsyncResult asyncResult);
        public abstract System.Net.WebResponse EndGetResponse(System.IAsyncResult asyncResult);
        public virtual System.Threading.Tasks.Task<System.IO.Stream> GetRequestStreamAsync() { return default(System.Threading.Tasks.Task<System.IO.Stream>); }
        public virtual System.Threading.Tasks.Task<System.Net.WebResponse> GetResponseAsync() { return default(System.Threading.Tasks.Task<System.Net.WebResponse>); }
        public static bool RegisterPrefix(string prefix, System.Net.IWebRequestCreate creator) { return default(bool); }
    }
    public abstract partial class WebResponse : System.IDisposable
    {
        protected WebResponse() { }
        public abstract long ContentLength { get; }
        public abstract string ContentType { get; }
        public virtual System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } }
        public abstract System.Uri ResponseUri { get; }
        public virtual bool SupportsHeaders { get { return default(bool); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract System.IO.Stream GetResponseStream();
    }

    [Flags]
    public enum SecurityProtocolType
    {
#pragma warning disable CS0618
        Ssl3 = System.Security.Authentication.SslProtocols.Ssl3,
#pragma warning restore CS0618
        Tls = System.Security.Authentication.SslProtocols.Tls,
        Tls11 = System.Security.Authentication.SslProtocols.Tls11,
        Tls12 = System.Security.Authentication.SslProtocols.Tls12,
    }
    public class ServicePoint
    {
        internal ServicePoint() { }
        public System.Net.BindIPEndPoint BindIPEndPointDelegate { get { throw null; } set { } }
        public int ConnectionLeaseTimeout { get { throw null; } set { } }
        public System.Uri Address { get { throw null; } }
        public int MaxIdleTime { get { throw null; } set { } }
        public bool UseNagleAlgorithm { get { throw null; } set { } }
        public int ReceiveBufferSize { get { throw null; } set { } }
        public bool Expect100Continue { get { throw null; } set { } }
        public System.DateTime IdleSince { get { throw null; } }
        public virtual System.Version ProtocolVersion { get { throw null; } }
        public string ConnectionName { get { throw null; } }
        public bool CloseConnectionGroup(string connectionGroupName) { throw null; }
        public int ConnectionLimit { get { throw null; } set { } }
        public int CurrentConnections { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate Certificate { get { throw null; } }
        public System.Security.Cryptography.X509Certificates.X509Certificate ClientCertificate { get { throw null; } }
        public bool SupportsPipelining { get { throw null; } }
        public void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { throw null; }
    }
    public class ServicePointManager
    {
        public const int DefaultNonPersistentConnectionLimit = 4;
        public const int DefaultPersistentConnectionLimit = 2;
        private ServicePointManager() { }
        public static System.Net.SecurityProtocolType SecurityProtocol { get { throw null; } set { } }
        public static int MaxServicePoints { get { throw null; } set { } }
        public static int DefaultConnectionLimit { get { throw null; } set { } }
        public static int MaxServicePointIdleTime { get { throw null; } set { } }
        public static bool UseNagleAlgorithm { get { throw null; } set { } }
        public static bool Expect100Continue { get { throw null; } set { } }
        public static bool EnableDnsRoundRobin { get { throw null; } set { } }
        public static int DnsRefreshTimeout { get { throw null; } set { } }
        [Obsolete("CertificatePolicy is obsoleted for this type, please use ServerCertificateValidationCallback instead. http://go.microsoft.com/fwlink/?linkid=14202")]
        public static System.Net.ICertificatePolicy CertificatePolicy { get { throw null; } set { } }
        public static System.Net.Security.RemoteCertificateValidationCallback ServerCertificateValidationCallback { get { throw null; } set { } }
        public static bool ReusePort { get { throw null; } set { } }
        public static bool CheckCertificateRevocationList { get { throw null; } set { } }
        public static System.Net.Security.EncryptionPolicy EncryptionPolicy { get { throw null; } }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address) { throw null; }
        public static System.Net.ServicePoint FindServicePoint(string uriString, System.Net.IWebProxy proxy) { throw null; }
        public static System.Net.ServicePoint FindServicePoint(System.Uri address, System.Net.IWebProxy proxy) { throw null; }
        public static void SetTcpKeepAlive(bool enabled, int keepAliveTime, int keepAliveInterval) { throw null; }
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public delegate System.Net.AuthenticationSchemes AuthenticationSchemeSelector(System.Net.HttpListenerRequest httpRequest);
    public sealed partial class HttpListener : System.IDisposable
    {
        public HttpListener() { }
        public System.Net.AuthenticationSchemes AuthenticationSchemes { get { throw null; } set { } }
        public System.Net.AuthenticationSchemeSelector? AuthenticationSchemeSelectorDelegate { get { throw null; } set { } }
        public System.Security.Authentication.ExtendedProtection.ServiceNameCollection DefaultServiceNames { get { throw null; } }
        public System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionPolicy { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public System.Net.HttpListener.ExtendedProtectionSelector? ExtendedProtectionSelectorDelegate { get { throw null; } set { } }
        public bool IgnoreWriteExceptions { get { throw null; } set { } }
        public bool IsListening { get { throw null; } }
        public static bool IsSupported { get { throw null; } }
        public System.Net.HttpListenerPrefixCollection Prefixes { get { throw null; } }
        public string? Realm { get { throw null; } set { } }
        public System.Net.HttpListenerTimeoutManager TimeoutManager { get { throw null; } }
        public bool UnsafeConnectionNtlmAuthentication { get { throw null; } set { } }
        public void Abort() { }
        public System.IAsyncResult BeginGetContext(System.AsyncCallback? callback, object? state) { throw null; }
        public void Close() { }
        public System.Net.HttpListenerContext EndGetContext(System.IAsyncResult asyncResult) { throw null; }
        public System.Net.HttpListenerContext GetContext() { throw null; }
        public System.Threading.Tasks.Task<System.Net.HttpListenerContext> GetContextAsync() { throw null; }
        public void Start() { }
        public void Stop() { }
        void System.IDisposable.Dispose() { }
        public delegate System.Security.Authentication.ExtendedProtection.ExtendedProtectionPolicy ExtendedProtectionSelector(System.Net.HttpListenerRequest request);
    }
    public partial class HttpListenerBasicIdentity : System.Security.Principal.GenericIdentity
    {
        public HttpListenerBasicIdentity(string username, string password) : base (default(System.Security.Principal.GenericIdentity)) { }
        public virtual string Password { get { throw null; } }
    }
    public sealed partial class HttpListenerContext
    {
        internal HttpListenerContext() { }
        public System.Net.HttpListenerRequest Request { get { throw null; } }
        public System.Net.HttpListenerResponse Response { get { throw null; } }
        public System.Security.Principal.IPrincipal? User { get { throw null; } }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol) { throw null; }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, int receiveBufferSize, System.TimeSpan keepAliveInterval) { throw null; }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, int receiveBufferSize, System.TimeSpan keepAliveInterval, System.ArraySegment<byte> internalBuffer) { throw null; }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol, System.TimeSpan keepAliveInterval) { throw null; }
    }
    public partial class HttpListenerException : System.ComponentModel.Win32Exception
    {
        public HttpListenerException() { }
        public HttpListenerException(int errorCode) { }
        public HttpListenerException(int errorCode, string message) { }
        [System.ObsoleteAttribute("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.", DiagnosticId = "SYSLIB0051", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        protected HttpListenerException(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext) { }
        public override int ErrorCode { get { throw null; } }
    }
    public partial class HttpListenerPrefixCollection : System.Collections.Generic.ICollection<string>, System.Collections.Generic.IEnumerable<string>, System.Collections.IEnumerable
    {
        internal HttpListenerPrefixCollection() { }
        public int Count { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public void Add(string uriPrefix) { }
        public void Clear() { }
        public bool Contains(string uriPrefix) { throw null; }
        public void CopyTo(System.Array array, int offset) { }
        public void CopyTo(string[] array, int offset) { }
        public System.Collections.Generic.IEnumerator<string> GetEnumerator() { throw null; }
        public bool Remove(string uriPrefix) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public sealed partial class HttpListenerRequest
    {
        internal HttpListenerRequest() { }
        public string[]? AcceptTypes { get { throw null; } }
        public int ClientCertificateError { get { throw null; } }
        public System.Text.Encoding ContentEncoding { get { throw null; } }
        public long ContentLength64 { get { throw null; } }
        public string? ContentType { get { throw null; } }
        public System.Net.CookieCollection Cookies { get { throw null; } }
        public bool HasEntityBody { get { throw null; } }
        public System.Collections.Specialized.NameValueCollection Headers { get { throw null; } }
        public string HttpMethod { get { throw null; } }
        public System.IO.Stream InputStream { get { throw null; } }
        public bool IsAuthenticated { get { throw null; } }
        public bool IsLocal { get { throw null; } }
        public bool IsSecureConnection { get { throw null; } }
        public bool IsWebSocketRequest { get { throw null; } }
        public bool KeepAlive { get { throw null; } }
        public System.Net.IPEndPoint LocalEndPoint { get { throw null; } }
        public System.Version ProtocolVersion { get { throw null; } }
        public System.Collections.Specialized.NameValueCollection QueryString { get { throw null; } }
        public string? RawUrl { get { throw null; } }
        public System.Net.IPEndPoint RemoteEndPoint { get { throw null; } }
        public System.Guid RequestTraceIdentifier { get { throw null; } }
        public string? ServiceName { get { throw null; } }
        public System.Net.TransportContext TransportContext { get { throw null; } }
        public System.Uri? Url { get { throw null; } }
        public System.Uri? UrlReferrer { get { throw null; } }
        public string? UserAgent { get { throw null; } }
        public string UserHostAddress { get { throw null; } }
        public string UserHostName { get { throw null; } }
        public string[]? UserLanguages { get { throw null; } }
        public System.IAsyncResult BeginGetClientCertificate(System.AsyncCallback? requestCallback, object? state) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2? EndGetClientCertificate(System.IAsyncResult asyncResult) { throw null; }
        public System.Security.Cryptography.X509Certificates.X509Certificate2? GetClientCertificate() { throw null; }
        public System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2?> GetClientCertificateAsync() { throw null; }
    }
    public sealed partial class HttpListenerResponse : System.IDisposable
    {
        internal HttpListenerResponse() { }
        public System.Text.Encoding? ContentEncoding { get { throw null; } set { } }
        public long ContentLength64 { get { throw null; } set { } }
        public string? ContentType { get { throw null; } set { } }
        public System.Net.CookieCollection Cookies { get { throw null; } set { } }
        public System.Net.WebHeaderCollection Headers { get { throw null; } set { } }
        public bool KeepAlive { get { throw null; } set { } }
        public System.IO.Stream OutputStream { get { throw null; } }
        public System.Version ProtocolVersion { get { throw null; } set { } }
        public string? RedirectLocation { get { throw null; } set { } }
        public bool SendChunked { get { throw null; } set { } }
        public int StatusCode { get { throw null; } set { } }
        public string StatusDescription { get { throw null; } set { } }
        public void Abort() { }
        public void AddHeader(string name, string value) { }
        public void AppendCookie(System.Net.Cookie cookie) { }
        public void AppendHeader(string name, string value) { }
        public void Close() { }
        public void Close(byte[] responseEntity, bool willBlock) { }
        public void CopyFrom(System.Net.HttpListenerResponse templateResponse) { }
        public void Redirect([System.Diagnostics.CodeAnalysis.StringSyntaxAttribute("Uri")] string url) { }
        public void SetCookie(System.Net.Cookie cookie) { }
        void System.IDisposable.Dispose() { }
    }
    public partial class HttpListenerTimeoutManager
    {
        internal HttpListenerTimeoutManager() { }
        public System.TimeSpan DrainEntityBody { get { throw null; } set { } }
        public System.TimeSpan EntityBody { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public System.TimeSpan HeaderWait { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public System.TimeSpan IdleConnection { get { throw null; } set { } }
        public long MinSendBytesPerSecond { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
        public System.TimeSpan RequestQueue { get { throw null; } [System.Runtime.Versioning.SupportedOSPlatformAttribute("windows")] set { } }
    }
}
namespace System.Net.WebSockets
{
    public partial class HttpListenerWebSocketContext : System.Net.WebSockets.WebSocketContext
    {
        internal HttpListenerWebSocketContext() { }
        public override System.Net.CookieCollection CookieCollection { get { throw null; } }
        public override System.Collections.Specialized.NameValueCollection Headers { get { throw null; } }
        public override bool IsAuthenticated { get { throw null; } }
        public override bool IsLocal { get { throw null; } }
        public override bool IsSecureConnection { get { throw null; } }
        public override string Origin { get { throw null; } }
        public override System.Uri RequestUri { get { throw null; } }
        public override string SecWebSocketKey { get { throw null; } }
        public override System.Collections.Generic.IEnumerable<string> SecWebSocketProtocols { get { throw null; } }
        public override string SecWebSocketVersion { get { throw null; } }
        public override System.Security.Principal.IPrincipal User { get { throw null; } }
        public override System.Net.WebSockets.WebSocket WebSocket { get { throw null; } }
    }
}

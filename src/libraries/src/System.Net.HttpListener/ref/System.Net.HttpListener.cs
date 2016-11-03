// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Net
{
    public delegate System.Net.AuthenticationSchemes AuthenticationSchemeSelector(System.Net.HttpListenerRequest httpRequest);
    public sealed partial class HttpListener : System.IDisposable
    {
        public HttpListener() { }
        public System.Net.AuthenticationSchemes AuthenticationSchemes { get { return default(System.Net.AuthenticationSchemes); } set { } }
        public System.Net.AuthenticationSchemeSelector AuthenticationSchemeSelectorDelegate { get { return default(System.Net.AuthenticationSchemeSelector); } set { } }
        public bool IgnoreWriteExceptions { get { return default(bool); } set { } }
        public bool IsListening { get { return default(bool); } }
        public static bool IsSupported { get { return default(bool); } }
        public System.Net.HttpListenerPrefixCollection Prefixes { get { return default(System.Net.HttpListenerPrefixCollection); } }
        public string Realm { get { return default(string); } set { } }
        public bool UnsafeConnectionNtlmAuthentication { get { return default(bool); } set { } }
        public void Abort() { }
        public System.IAsyncResult BeginGetContext(System.AsyncCallback callback, object state) { return default(System.IAsyncResult); }
        public void Close() { }
        public System.Net.HttpListenerContext EndGetContext(System.IAsyncResult asyncResult) { return default(System.Net.HttpListenerContext); }
        public System.Net.HttpListenerContext GetContext() { return default(System.Net.HttpListenerContext); }
        public System.Threading.Tasks.Task<System.Net.HttpListenerContext> GetContextAsync() { return default(System.Threading.Tasks.Task<System.Net.HttpListenerContext>); }
        public void Start() { }
        public void Stop() { }
        void System.IDisposable.Dispose() { }
    }
    public partial class HttpListenerBasicIdentity : System.Security.Principal.GenericIdentity
    {
        public HttpListenerBasicIdentity(string username, string password) : base(default(string)) { }
        public virtual string Password { get { return default(string); } }
    }
    public sealed partial class HttpListenerContext
    {
        internal HttpListenerContext() { }
        public System.Net.HttpListenerRequest Request { get { return default(System.Net.HttpListenerRequest); } }
        public System.Net.HttpListenerResponse Response { get { return default(System.Net.HttpListenerResponse); } }
        public System.Security.Principal.IPrincipal User { get { return default(System.Security.Principal.IPrincipal); } }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol) { return default(System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext>); }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, System.TimeSpan keepAliveInterval) { return default(System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext>); }
        public System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, int receiveBufferSize, System.TimeSpan keepAliveInterval, System.ArraySegment<byte> internalBuffer) { return default(System.Threading.Tasks.Task<System.Net.WebSockets.HttpListenerWebSocketContext>); }
    }
    public partial class HttpListenerException : System.ComponentModel.Win32Exception
    {
        public HttpListenerException() { }
        public HttpListenerException(int errorCode) { }
        public HttpListenerException(int errorCode, string message) { }
    }
    public partial class HttpListenerPrefixCollection : System.Collections.Generic.ICollection<string>, System.Collections.Generic.IEnumerable<string>, System.Collections.IEnumerable
    {
        internal HttpListenerPrefixCollection() { }
        public int Count { get { return default(int); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsSynchronized { get { return default(bool); } }
        public void Add(string uriPrefix) { }
        public void Clear() { }
        public bool Contains(string uriPrefix) { return default(bool); }
        public void CopyTo(System.Array array, int offset) { }
        public void CopyTo(string[] array, int offset) { }
        public System.Collections.Generic.IEnumerator<string> GetEnumerator() { return default(System.Collections.Generic.IEnumerator<string>); }
        public bool Remove(string uriPrefix) { return default(bool); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class HttpListenerRequest
    {
        internal HttpListenerRequest() { }
        public string[] AcceptTypes { get { return default(string[]); } }
        public int ClientCertificateError { get { return default(int); } }
        public System.Text.Encoding ContentEncoding { get { return default(System.Text.Encoding); } }
        public long ContentLength64 { get { return default(long); } }
        public string ContentType { get { return default(string); } }
        public System.Net.CookieCollection Cookies { get { return default(System.Net.CookieCollection); } }
        public bool HasEntityBody { get { return default(bool); } }
        public System.Collections.Specialized.NameValueCollection Headers { get { return default(System.Collections.Specialized.NameValueCollection); } }
        public string HttpMethod { get { return default(string); } }
        public System.IO.Stream InputStream { get { return default(System.IO.Stream); } }
        public bool IsAuthenticated { get { return default(bool); } }
        public bool IsLocal { get { return default(bool); } }
        public bool IsSecureConnection { get { return default(bool); } }
        public bool IsWebSocketRequest { get { return default(bool); } }
        public bool KeepAlive { get { return default(bool); } }
        public System.Net.IPEndPoint LocalEndPoint { get { return default(System.Net.IPEndPoint); } }
        public System.Version ProtocolVersion { get { return default(System.Version); } }
        public System.Collections.Specialized.NameValueCollection QueryString { get { return default(System.Collections.Specialized.NameValueCollection); } }
        public string RawUrl { get { return default(string); } }
        public System.Net.IPEndPoint RemoteEndPoint { get { return default(System.Net.IPEndPoint); } }
        public System.Guid RequestTraceIdentifier { get { return default(System.Guid); } }
        public string ServiceName { get { return default(string); } }
        public System.Net.TransportContext TransportContext { get { return default(System.Net.TransportContext); } }
        public System.Uri Url { get { return default(System.Uri); } }
        public System.Uri UrlReferrer { get { return default(System.Uri); } }
        public string UserAgent { get { return default(string); } }
        public string UserHostAddress { get { return default(string); } }
        public string UserHostName { get { return default(string); } }
        public string[] UserLanguages { get { return default(string[]); } }
        public System.IAsyncResult BeginGetClientCertificate(System.AsyncCallback requestCallback, object state) { return default(System.IAsyncResult); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 EndGetClientCertificate(System.IAsyncResult asyncResult) { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); }
        public System.Security.Cryptography.X509Certificates.X509Certificate2 GetClientCertificate() { return default(System.Security.Cryptography.X509Certificates.X509Certificate2); }
        public System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2> GetClientCertificateAsync() { return default(System.Threading.Tasks.Task<System.Security.Cryptography.X509Certificates.X509Certificate2>); }
    }
    public sealed partial class HttpListenerResponse : System.IDisposable
    {
        internal HttpListenerResponse() { }
        public System.Text.Encoding ContentEncoding { get { return default(System.Text.Encoding); } set { } }
        public long ContentLength64 { get { return default(long); } set { } }
        public string ContentType { get { return default(string); } set { } }
        public System.Net.CookieCollection Cookies { get { return default(System.Net.CookieCollection); } set { } }
        public System.Net.WebHeaderCollection Headers { get { return default(System.Net.WebHeaderCollection); } set { } }
        public bool KeepAlive { get { return default(bool); } set { } }
        public System.IO.Stream OutputStream { get { return default(System.IO.Stream); } }
        public System.Version ProtocolVersion { get { return default(System.Version); } set { } }
        public string RedirectLocation { get { return default(string); } set { } }
        public bool SendChunked { get { return default(bool); } set { } }
        public int StatusCode { get { return default(int); } set { } }
        public string StatusDescription { get { return default(string); } set { } }
        public void Abort() { }
        public void AddHeader(string name, string value) { }
        public void AppendCookie(System.Net.Cookie cookie) { }
        public void AppendHeader(string name, string value) { }
        public void Close() { }
        public void Close(byte[] responseEntity, bool willBlock) { }
        public void CopyFrom(System.Net.HttpListenerResponse templateResponse) { }
        public void Redirect(string url) { }
        public void SetCookie(System.Net.Cookie cookie) { }
        void System.IDisposable.Dispose() { }
    }
}
namespace System.Net.WebSockets
{
    public partial class HttpListenerWebSocketContext : System.Net.WebSockets.WebSocketContext
    {
        private HttpListenerWebSocketContext() { }
        public override System.Net.CookieCollection CookieCollection { get { return default(System.Net.CookieCollection); } }
        public override System.Collections.Specialized.NameValueCollection Headers { get { return default(System.Collections.Specialized.NameValueCollection); } }
        public override bool IsAuthenticated { get { return default(bool); } }
        public override bool IsLocal { get { return default(bool); } }
        public override bool IsSecureConnection { get { return default(bool); } }
        public override string Origin { get { return default(string); } }
        public override System.Uri RequestUri { get { return default(System.Uri); } }
        public override string SecWebSocketKey { get { return default(string); } }
        public override System.Collections.Generic.IEnumerable<string> SecWebSocketProtocols { get { return default(System.Collections.Generic.IEnumerable<string>); } }
        public override string SecWebSocketVersion { get { return default(string); } }
        public override System.Security.Principal.IPrincipal User { get { return default(System.Security.Principal.IPrincipal); } }
        public override System.Net.WebSockets.WebSocket WebSocket { get { return default(System.Net.WebSockets.WebSocket); } }
    }
}

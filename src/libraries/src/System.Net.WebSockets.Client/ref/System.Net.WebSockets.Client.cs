// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Net.WebSockets
{
    public sealed partial class ClientWebSocket : System.Net.WebSockets.WebSocket
    {
        public ClientWebSocket() { }
        public override System.Nullable<System.Net.WebSockets.WebSocketCloseStatus> CloseStatus { get { return default(System.Nullable<System.Net.WebSockets.WebSocketCloseStatus>); } }
        public override string CloseStatusDescription { get { return default(string); } }
        public System.Net.WebSockets.ClientWebSocketOptions Options { get { return default(System.Net.WebSockets.ClientWebSocketOptions); } }
        public override System.Net.WebSockets.WebSocketState State { get { return default(System.Net.WebSockets.WebSocketState); } }
        public override string SubProtocol { get { return default(string); } }
        public override void Abort() { }
        public override System.Threading.Tasks.Task CloseAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override System.Threading.Tasks.Task CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus closeStatus, string statusDescription, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task ConnectAsync(System.Uri uri, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public override void Dispose() { }
        public override System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult> ReceiveAsync(System.ArraySegment<byte> buffer, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Net.WebSockets.WebSocketReceiveResult>); }
        public override System.Threading.Tasks.Task SendAsync(System.ArraySegment<byte> buffer, System.Net.WebSockets.WebSocketMessageType messageType, bool endOfMessage, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public sealed partial class ClientWebSocketOptions
    {
        internal ClientWebSocketOptions() { }
        public System.Security.Cryptography.X509Certificates.X509CertificateCollection ClientCertificates { get { return default(System.Security.Cryptography.X509Certificates.X509CertificateCollection); } set { } }
        public System.Net.CookieContainer Cookies { get { return default(System.Net.CookieContainer); } set { } }
        public System.Net.ICredentials Credentials { get { return default(System.Net.ICredentials); } set { } }
        public System.TimeSpan KeepAliveInterval { get { return default(System.TimeSpan); } set { } }
        public System.Net.IWebProxy Proxy { get { return default(System.Net.IWebProxy); } set { } }
        public void AddSubProtocol(string subProtocol) { }
        public void SetRequestHeader(string headerName, string headerValue) { }
    }
}

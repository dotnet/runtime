// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed unsafe partial class HttpListenerContext
    {
        private string? _mutualAuthentication;
        internal HttpListenerSession ListenerSession { get; private set; }

        internal HttpListenerContext(HttpListenerSession session, RequestContextBase memoryBlob)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"httpListener {session.Listener} requestBlob={((IntPtr)memoryBlob.RequestBlob)}");
            _listener = session.Listener;
            ListenerSession = session;
            Request = new HttpListenerRequest(this, memoryBlob);
            AuthenticationSchemes = _listener.AuthenticationSchemes;
            ExtendedProtectionPolicy = _listener.ExtendedProtectionPolicy;
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"HttpListener: {_listener} HttpListenerRequest: {Request}");
        }

        // Call this right after construction, and only once!  Not after it's been handed to a user.
        internal void SetIdentity(IPrincipal principal, string? mutualAuthentication)
        {
            _mutualAuthentication = mutualAuthentication;
            _user = principal;
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"mutual: {mutualAuthentication ?? "<null>"}, Principal: {principal}");
        }

        // This can be used to cache the results of HttpListener.ExtendedProtectionSelectorDelegate.
        internal ExtendedProtectionPolicy ExtendedProtectionPolicy { get; set; }

        internal string? MutualAuthentication => _mutualAuthentication;

        internal HttpListener? Listener => _listener;

        internal SafeHandle RequestQueueHandle => ListenerSession.RequestQueueHandle;

        internal ThreadPoolBoundHandle RequestQueueBoundHandle => ListenerSession.RequestQueueBoundHandle;

        internal ulong RequestId => Request.RequestId;

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            HttpWebSocket.ValidateOptions(subProtocol, receiveBufferSize, HttpWebSocket.MinSendBufferSize, keepAliveInterval);

            ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, HttpWebSocket.MinSendBufferSize, true);
            return this.AcceptWebSocketAsync(subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string? subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            return HttpWebSocket.AcceptWebSocketAsync(this,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        internal void Close()
        {
            try
            {
                _response?.Close();
            }
            finally
            {
                try
                {
                    Request.Close();
                }
                finally
                {
                    IDisposable? user = _user == null ? null : _user.Identity as IDisposable;

                    // For unsafe connection ntlm auth we dont dispose this identity as yet since its cached
                    if ((user != null) &&
                        (_user!.Identity!.AuthenticationType != NegotiationInfoClass.NTLM) &&
                        (!_listener!.UnsafeConnectionNtlmAuthentication))
                    {
                        user.Dispose();
                    }
                }
            }
        }

        internal void Abort()
        {
            ForceCancelRequest(RequestQueueHandle, Request.RequestId);
            try
            {
                Request.Close();
            }
            finally
            {
                (_user?.Identity as IDisposable)?.Dispose();
            }
        }

        internal Interop.HttpApi.HTTP_VERB GetKnownMethod()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(this, $"Visited {nameof(GetKnownMethod)}()");
            return Interop.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
        }

        // This is only called while processing incoming requests.  We don't have to worry about cancelling
        // any response writes.
        internal static void CancelRequest(SafeHandle requestQueueHandle, ulong requestId)
        {
            // It is safe to ignore the return value on a cancel operation because the connection is being closed
            Interop.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId, IntPtr.Zero);
        }

        // The request is being aborted, but large writes may be in progress. Cancel them.
        internal void ForceCancelRequest(SafeHandle requestQueueHandle, ulong requestId)
        {
            uint statusCode = Interop.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId,
                IntPtr.Zero);

            // Either the connection has already dropped, or the last write is in progress.
            // The requestId becomes invalid as soon as the last Content-Length write starts.
            // The only way to cancel now is with CancelIoEx.
            if (statusCode == Interop.HttpApi.ERROR_CONNECTION_INVALID)
            {
                _response!.CancelLastWrite(requestQueueHandle);
            }
        }

        internal void SetAuthenticationHeaders()
        {
            Listener!.SetAuthenticationHeaders(this);
        }
    }
}

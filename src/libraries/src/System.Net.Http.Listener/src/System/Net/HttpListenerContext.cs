// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Security.Authentication.ExtendedProtection;
using System.ComponentModel;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Threading;

namespace System.Net
{

    public sealed unsafe class HttpListenerContext
    {
        private HttpListener m_Listener;
        private HttpListenerRequest m_Request;
        private HttpListenerResponse m_Response;
        private IPrincipal m_User;
        private string m_MutualAuthentication;
        private AuthenticationSchemes m_AuthenticationSchemes;
        private ExtendedProtectionPolicy m_ExtendedProtectionPolicy;

        internal const string NTLM = "NTLM";

        internal HttpListenerContext(HttpListener httpListener, RequestContextBase memoryBlob)
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.PrintInfo(NetEventSource.ComponentType.HttpListener, this, ".ctor", "httpListener#" + LoggingHash.HashString(httpListener) + " requestBlob=" + LoggingHash.HashString((IntPtr)memoryBlob.RequestBlob));
            m_Listener = httpListener;
            m_Request = new HttpListenerRequest(this, memoryBlob);
            m_AuthenticationSchemes = httpListener.AuthenticationSchemes;
            m_ExtendedProtectionPolicy = httpListener.ExtendedProtectionPolicy;
            GlobalLog.Print("HttpListenerContext#" + LoggingHash.HashString(this) + "::.ctor() HttpListener#" + LoggingHash.HashString(m_Listener) + " HttpListenerRequest#" + LoggingHash.HashString(m_Request));
        }

        // Call this right after construction, and only once!  Not after it's been handed to a user.
        internal void SetIdentity(IPrincipal principal, string mutualAuthentication)
        {
            m_MutualAuthentication = mutualAuthentication;
            m_User = principal;
            GlobalLog.Print("HttpListenerContext#" + LoggingHash.HashString(this) + "::SetIdentity() mutual:" + (mutualAuthentication == null ? "<null>" : mutualAuthentication) + " Principal#" + LoggingHash.HashString(principal));
        }

        public /* new */ HttpListenerRequest Request
        {
            get
            {
                return m_Request;
            }
        }

        public /* new */ HttpListenerResponse Response
        {
            get
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Response", "");
                if (m_Response == null)
                {
                    m_Response = new HttpListenerResponse(this);
                    GlobalLog.Print("HttpListenerContext#" + LoggingHash.HashString(this) + "::.Response_get() HttpListener#" + LoggingHash.HashString(m_Listener) + " HttpListenerRequest#" + LoggingHash.HashString(m_Request) + " HttpListenerResponse#" + LoggingHash.HashString(m_Response));
                }
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Response", "");
                return m_Response;
            }
        }

        public IPrincipal User => m_User;

        // What auth schemes were expected of this request?
        // This can be used to cache the results of HttpListener.AuthenticationSchemeSelectorDelegate.
        internal AuthenticationSchemes AuthenticationSchemes
        {
            get
            {
                return m_AuthenticationSchemes;
            }
            set
            {
                m_AuthenticationSchemes = value;
            }
        }

        // This can be used to cache the results of HttpListener.ExtendedProtectionSelectorDelegate.
        internal ExtendedProtectionPolicy ExtendedProtectionPolicy
        {
            get
            {
                return m_ExtendedProtectionPolicy;
            }
            set
            {
                m_ExtendedProtectionPolicy = value;
            }
        }

        internal string MutualAuthentication
        {
            get
            {
                return m_MutualAuthentication;
            }
        }

        internal HttpListener Listener
        {
            get
            {
                return m_Listener;
            }
        }

        internal SafeHandle RequestQueueHandle
        {
            get
            {
                return m_Listener.RequestQueueHandle;
            }
        }

        internal ThreadPoolBoundHandle RequestQueueBoundHandle
        {
            get
            {
                return m_Listener.RequestQueueBoundHandle;
            }
        }

        internal void EnsureBoundHandle()
        {
            m_Listener.EnsureBoundHandle();
        }

        internal ulong RequestId
        {
            get
            {
                return Request.RequestId;
            }
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol)
        {
            return this.AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                WebSocket.DefaultKeepAliveInterval);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol, TimeSpan keepAliveInterval)
        {
            return this.AcceptWebSocketAsync(subProtocol,
                WebSocketHelpers.DefaultReceiveBufferSize,
                keepAliveInterval);
        }

        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval)
        {
            WebSocketHelpers.ValidateOptions(subProtocol, receiveBufferSize, WebSocketBuffer.MinSendBufferSize, keepAliveInterval);

            ArraySegment<byte> internalBuffer = WebSocketBuffer.CreateInternalBufferArraySegment(receiveBufferSize, WebSocketBuffer.MinSendBufferSize, true);
            return this.AcceptWebSocketAsync(subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task<HttpListenerWebSocketContext> AcceptWebSocketAsync(string subProtocol,
            int receiveBufferSize,
            TimeSpan keepAliveInterval,
            ArraySegment<byte> internalBuffer)
        {
            return WebSocketHelpers.AcceptWebSocketAsync(this,
                subProtocol,
                receiveBufferSize,
                keepAliveInterval,
                internalBuffer);
        }

        internal void Close()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Close()", "");

            try
            {
                if (m_Response != null)
                {
                    m_Response.Close();
                }
            }
            finally
            {
                try
                {
                    m_Request.Close();
                }
                finally
                {
                    IDisposable user = m_User == null ? null : m_User.Identity as IDisposable;

                    // For unsafe connection ntlm auth we dont dispose this identity as yet since its cached
                    if ((user != null) &&
                        (m_User.Identity.AuthenticationType != NTLM) &&
                        (!m_Listener.UnsafeConnectionNtlmAuthentication))
                    {
                        user.Dispose();
                    }
                }
            }
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Close", "");
        }

        internal void Abort()
        {
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Enter(NetEventSource.ComponentType.HttpListener, this, "Abort", "");
            ForceCancelRequest(RequestQueueHandle, m_Request.RequestId);
            try
            {
                m_Request.Close();
            }
            finally
            {
                IDisposable user = m_User == null ? null : m_User.Identity as IDisposable;
                if (user != null)
                {
                    user.Dispose();
                }
            }
            if (NetEventSource.Log.IsEnabled()) NetEventSource.Exit(NetEventSource.ComponentType.HttpListener, this, "Abort", "");
        }


        internal Interop.HttpApi.HTTP_VERB GetKnownMethod()
        {
            GlobalLog.Print("HttpListenerContext::GetKnownMethod()");
            return Interop.HttpApi.GetKnownVerb(Request.RequestBuffer, Request.OriginalBlobAddress);
        }

        // This is only called while processing incoming requests.  We don't have to worry about cancelling 
        // any response writes.
        internal static void CancelRequest(SafeHandle requestQueueHandle, ulong requestId)
        {
            // It is safe to ignore the return value on a cancel operation because the connection is being closed
            uint statusCode = Interop.HttpApi.HttpCancelHttpRequest(requestQueueHandle, requestId,
                IntPtr.Zero);
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
                m_Response.CancelLastWrite(requestQueueHandle);
            }
        }

        internal void SetAuthenticationHeaders()
        {
            Listener.SetAuthenticationHeaders(this);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    public abstract class WebSocket : IDisposable
    {
        public abstract WebSocketCloseStatus? CloseStatus { get; }
        public abstract string CloseStatusDescription { get; }
        public abstract string SubProtocol { get; }
        public abstract WebSocketState State { get; }

        public abstract void Abort();
        public abstract Task CloseAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken);
        public abstract Task CloseOutputAsync(WebSocketCloseStatus closeStatus,
            string statusDescription,
            CancellationToken cancellationToken);
        public abstract void Dispose();
        public abstract Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer,
            CancellationToken cancellationToken);
        public abstract Task SendAsync(ArraySegment<byte> buffer,
            WebSocketMessageType messageType,
            bool endOfMessage,
            CancellationToken cancellationToken);

        public static TimeSpan DefaultKeepAliveInterval
        {
            // In the .NET Framework, this pulls the value from a P/Invoke.  Here we just hardcode it to a reasonable default.
            get { return TimeSpan.FromSeconds(30); }
        }

        protected static void ThrowOnInvalidState(WebSocketState state, params WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState currentState in validStates)
                {
                    if (state == currentState)
                    {
                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(SR.Format(SR.net_WebSockets_InvalidState, state, validStatesText));
        }

        protected static bool IsStateTerminal(WebSocketState state) => 
            state == WebSocketState.Closed || state == WebSocketState.Aborted;

        public static ArraySegment<byte> CreateClientBuffer(int receiveBufferSize, int sendBufferSize)
        {
            if (receiveBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }
            if (sendBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sendBufferSize), sendBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }
            return new ArraySegment<byte>(new byte[Math.Max(receiveBufferSize, sendBufferSize)]);
        }

        public static ArraySegment<byte> CreateServerBuffer(int receiveBufferSize)
        {
            if (receiveBufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(receiveBufferSize), receiveBufferSize, SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
            }
            return new ArraySegment<byte>(new byte[receiveBufferSize]);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.")]
        public static bool IsApplicationTargeting45() => true;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void RegisterPrefixes()
        {
            // The current WebRequest implementation in corefx does not support upgrading
            // web socket connections.  For now, we throw.
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static WebSocket CreateClientWebSocket(Stream innerStream,
            string subProtocol, int receiveBufferSize, int sendBufferSize,
            TimeSpan keepAliveInterval, bool useZeroMaskingKey, ArraySegment<byte> internalBuffer)
        {
            // ClientWebSocket on Unix is implemented in managed code and can be constructed (internally)
            // for an arbitrary stream. We could use that implementation here, building it in to the WebSocket
            // library as well, or accessing it from the client library via reflection.  For now, we throw.
            throw new PlatformNotSupportedException();
        }
    }
}

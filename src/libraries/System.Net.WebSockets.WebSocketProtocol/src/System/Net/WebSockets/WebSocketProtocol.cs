// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Net.WebSockets
{
    [UnsupportedOSPlatform("browser")]
    public static class WebSocketProtocol
    {
        public static WebSocket CreateFromStream(
            Stream stream,
            bool isServer,
            string? subProtocol,
            TimeSpan keepAliveInterval)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (!stream.CanRead || !stream.CanWrite)
            {
                throw new ArgumentException(!stream.CanRead ? SR.NotReadableStream : SR.NotWriteableStream, nameof(stream));
            }

            if (subProtocol != null)
            {
                WebSocketValidate.ValidateSubprotocol(subProtocol);
            }

            if (keepAliveInterval != Timeout.InfiniteTimeSpan && keepAliveInterval < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(keepAliveInterval), keepAliveInterval,
                    SR.Format(SR.net_WebSockets_ArgumentOutOfRange_TooSmall,
                    0));
            }

            return ManagedWebSocket.CreateFromConnectedStream(stream, isServer, subProtocol, keepAliveInterval);
        }
    }
}

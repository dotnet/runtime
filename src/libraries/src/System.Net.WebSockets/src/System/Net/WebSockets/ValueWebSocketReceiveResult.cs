// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.WebSockets
{
    /// <summary>Represents the result of performing a single <see cref="WebSocket.ReceiveAsync(Memory{byte}, System.Threading.CancellationToken)"/> operation on a <see cref="WebSocket"/>.</summary>
    public readonly struct ValueWebSocketReceiveResult
    {
        private readonly uint _countAndEndOfMessage;
        private readonly WebSocketMessageType _messageType;

        /// <summary>Creates an instance of the <see cref="ValueWebSocketReceiveResult"/> value type.</summary>
        /// <param name="count">The number of bytes received.</param>
        /// <param name="messageType">The type of message that was received.</param>
        /// <param name="endOfMessage">Whether this is the final message.</param>
        public ValueWebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
        {
            if (count < 0) ThrowCountOutOfRange();
            if ((uint)messageType > (uint)WebSocketMessageType.Close) ThrowMessageTypeOutOfRange();

            _countAndEndOfMessage = (uint)count | (uint)(endOfMessage ? 1 << 31 : 0);
            _messageType = messageType;

            Debug.Assert(count == Count);
            Debug.Assert(messageType == MessageType);
            Debug.Assert(endOfMessage == EndOfMessage);
        }

        public int Count => (int)(_countAndEndOfMessage & 0x7FFFFFFF);
        public bool EndOfMessage => (_countAndEndOfMessage & 0x80000000) == 0x80000000;
        public WebSocketMessageType MessageType => _messageType;

        private static void ThrowCountOutOfRange() => throw new ArgumentOutOfRangeException("count");
        private static void ThrowMessageTypeOutOfRange() => throw new ArgumentOutOfRangeException("messageType");
    }
}

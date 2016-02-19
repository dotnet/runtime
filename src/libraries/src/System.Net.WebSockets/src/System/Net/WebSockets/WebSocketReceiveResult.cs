// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.WebSockets
{
    public class WebSocketReceiveResult
    {
        public WebSocketReceiveResult(int count, WebSocketMessageType messageType, bool endOfMessage)
            : this(count, messageType, endOfMessage, null, null)
        {
        }

        public WebSocketReceiveResult(int count,
            WebSocketMessageType messageType,
            bool endOfMessage,
            WebSocketCloseStatus? closeStatus,
            string closeStatusDescription)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Count = count;
            EndOfMessage = endOfMessage;
            MessageType = messageType;
            CloseStatus = closeStatus;
            CloseStatusDescription = closeStatusDescription;
        }

        public int Count { get; private set; }
        public bool EndOfMessage { get; private set; }
        public WebSocketMessageType MessageType { get; private set; }
        public WebSocketCloseStatus? CloseStatus { get; private set; }
        public string CloseStatusDescription { get; private set; }

        internal WebSocketReceiveResult Copy(int count)
        {
            Debug.Assert(count >= 0, "'count' MUST NOT be negative.");
            Debug.Assert(count <= Count, "'count' MUST NOT be bigger than 'this.Count'.");

            Count -= count;
            return new WebSocketReceiveResult(count,
                MessageType,
                Count == 0 && this.EndOfMessage,
                CloseStatus,
                CloseStatusDescription);
        }
    }
}

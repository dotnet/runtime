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

        public int Count { get; }
        public bool EndOfMessage { get; }
        public WebSocketMessageType MessageType { get; }
        public WebSocketCloseStatus? CloseStatus { get; }
        public string CloseStatusDescription { get; }
    }
}

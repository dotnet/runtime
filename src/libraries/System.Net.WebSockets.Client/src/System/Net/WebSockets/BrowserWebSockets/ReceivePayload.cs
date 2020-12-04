// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;

namespace System.Net.WebSockets
{
    internal sealed class ReceivePayload
    {
        private readonly byte[] _dataMessageReceived;
        private readonly WebSocketMessageType _messageType;
        private int _unconsumedDataOffset;

        public ReceivePayload(ArrayBuffer arrayBuffer, WebSocketMessageType messageType)
        {
            using (var bin = new Uint8Array(arrayBuffer))
            {
                _dataMessageReceived = bin.ToArray();
                _messageType = messageType;
            }
        }

        public ReceivePayload(ArraySegment<byte> payload, WebSocketMessageType messageType)
        {
            _dataMessageReceived = payload.Array ?? Array.Empty<byte>();
            _messageType = messageType;
        }

        public bool BufferPayload(ArraySegment<byte> arraySegment, out WebSocketReceiveResult receiveResult)
        {
            int bytesTransferred = Math.Min(_dataMessageReceived.Length - _unconsumedDataOffset, arraySegment.Count);
            bool endOfMessage = (_dataMessageReceived.Length - _unconsumedDataOffset) <= arraySegment.Count;
            Buffer.BlockCopy(_dataMessageReceived, _unconsumedDataOffset, arraySegment.Array!, arraySegment.Offset, bytesTransferred);
            _unconsumedDataOffset += arraySegment.Count;
            receiveResult = new WebSocketReceiveResult(bytesTransferred, _messageType, endOfMessage);
            return endOfMessage;
        }
    }

}

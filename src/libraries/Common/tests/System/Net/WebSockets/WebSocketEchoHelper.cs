// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public static class WebSocketEchoHelper
    {
        public static class EchoControlMessage
        {
            public const string Close = ".close";
            public const string Shutdown = ".shutdown";
            public const string Abort = ".abort";
            public const string Delay5Sec = ".delay5sec";
            public const string ReceiveMessageAfterClose = ".receiveMessageAfterClose";
            public const string CloseAfter = ".closeafter";
            public const string ShutdownAfter = ".shutdownafter";
        }

        public static async Task RunEchoAll(WebSocket socket, bool replyWithPartialMessages, bool replyWithEnhancedCloseMessage, CancellationToken cancellationToken = default)
        {
            const int MaxBufferSize = 128 * 1024;

            var receiveBuffer = new byte[MaxBufferSize];
            var throwAwayBuffer = new byte[MaxBufferSize];

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    if (receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken);
                    }
                    else
                    {
                        WebSocketCloseStatus closeStatus = receiveResult.CloseStatus.GetValueOrDefault();
                        await socket.CloseAsync(
                            closeStatus,
                            replyWithEnhancedCloseMessage ?
                                ("Server received: " + (int)closeStatus + " " + receiveResult.CloseStatusDescription) :
                                receiveResult.CloseStatusDescription,
                            cancellationToken);
                    }

                    continue;
                }

                // Keep reading until we get an entire message.
                int offset = receiveResult.Count;
                while (receiveResult.EndOfMessage == false)
                {
                    if (offset < MaxBufferSize)
                    {
                        receiveResult = await socket.ReceiveAsync(
                            new ArraySegment<byte>(receiveBuffer, offset, MaxBufferSize - offset),
                            cancellationToken);
                    }
                    else
                    {
                        receiveResult = await socket.ReceiveAsync(
                            new ArraySegment<byte>(throwAwayBuffer),
                            cancellationToken);
                    }

                    offset += receiveResult.Count;
                }

                // Close socket if the message was too big.
                if (offset > MaxBufferSize)
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.MessageTooBig,
                        string.Format("{0}: {1} > {2}", WebSocketCloseStatus.MessageTooBig.ToString(), offset, MaxBufferSize),
                        cancellationToken);

                    continue;
                }

                bool sendMessage = false;
                string receivedMessage = null;
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, offset);
                    if (receivedMessage == EchoControlMessage.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                    }
                    else if (receivedMessage == EchoControlMessage.Shutdown)
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                    }
                    else if (receivedMessage == EchoControlMessage.Abort)
                    {
                        socket.Abort();
                    }
                    else if (receivedMessage == EchoControlMessage.Delay5Sec)
                    {
                        await Task.Delay(5000);
                    }
                    else if (receivedMessage == EchoControlMessage.ReceiveMessageAfterClose)
                    {
                        string message = $"{receivedMessage} {DateTime.Now:HH:mm:ss}";
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, message.Length),
                            WebSocketMessageType.Text,
                            true,
                            cancellationToken);
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                    }
                    else if (socket.State == WebSocketState.Open)
                    {
                        sendMessage = true;
                    }
                }
                else
                {
                    sendMessage = true;
                }

                if (sendMessage)
                {
                    await socket.SendAsync(
                            new ArraySegment<byte>(receiveBuffer, 0, offset),
                            receiveResult.MessageType,
                            !replyWithPartialMessages,
                            cancellationToken);
                }
                if (receivedMessage == EchoControlMessage.CloseAfter)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                }
                else if (receivedMessage == EchoControlMessage.ShutdownAfter)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                }
            }
        }

        public static async Task RunEchoHeaders(WebSocket socket, IEnumerable<KeyValuePair<string, string>> headers, CancellationToken cancellationToken = default)
        {
            const int MaxBufferSize = 1024;
            var receiveBuffer = new byte[MaxBufferSize];

            // Reflect all headers and cookies
            var sb = new StringBuilder();
            sb.AppendLine("Headers:");

            foreach (KeyValuePair<string, string> pair in headers)
            {
                sb.Append(pair.Key);
                sb.Append(":");
                sb.AppendLine(pair.Value);
            }

            byte[] sendBuffer = Encoding.UTF8.GetBytes(sb.ToString());
            await socket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, cancellationToken);

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    if (receiveResult.CloseStatus == WebSocketCloseStatus.Empty)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.Empty, null, cancellationToken);
                    }
                    else
                    {
                        await socket.CloseAsync(
                            receiveResult.CloseStatus.GetValueOrDefault(),
                            receiveResult.CloseStatusDescription,
                            cancellationToken);
                    }

                    continue;
                }
            }
        }

        public static async ValueTask<WebSocketEchoOptions> ProcessOptions(string queryString, CancellationToken cancellationToken = default)
        {
            WebSocketEchoOptions options = WebSocketEchoOptions.Parse(queryString);
            if (options.Delay is TimeSpan d)
            {
                await Task.Delay(d, cancellationToken).ConfigureAwait(false);
            }
            return options;
        }
    }
}

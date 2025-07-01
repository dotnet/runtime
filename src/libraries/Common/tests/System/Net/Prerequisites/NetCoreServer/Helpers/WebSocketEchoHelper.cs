// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public static class WebSocketEchoHelper
    {
        public static async Task RunEchoAll(WebSocket socket, bool replyWithPartialMessages, bool replyWithEnhancedCloseMessage, TextWriter? logger = null, CancellationToken cancellationToken = default)
        {
            const int MaxBufferSize = 128 * 1024;

            var receiveBuffer = new byte[MaxBufferSize];
            var throwAwayBuffer = new byte[MaxBufferSize];

            //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] State={socket.State}, starting receive loop...");

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] State={socket.State}, waiting for messages...");
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);

                //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Received {receiveResult.MessageType} frame, Count={receiveResult.Count}, EOM={receiveResult.EndOfMessage}");

                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] CloseStatus={receiveResult.CloseStatus}, CloseStatusDescription={receiveResult.CloseStatusDescription}");
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
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Closed");

                    continue;
                }

                // Keep reading until we get an entire message.
                int offset = receiveResult.Count;
                while (receiveResult.EndOfMessage == false)
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] EOM=false, waiting for continuation (offset={offset})...");
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
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Bad message, closing (MessageTooBig)");
                    await socket.CloseAsync(
                        WebSocketCloseStatus.MessageTooBig,
                        string.Format("{0}: {1} > {2}", WebSocketCloseStatus.MessageTooBig.ToString(), offset, MaxBufferSize),
                        cancellationToken);
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Closed");

                    continue;
                }

                bool sendMessage = false;
                string receivedMessage = null;
                if (receiveResult.MessageType == WebSocketMessageType.Text)
                {
                    receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, offset);
                    if (receivedMessage == ".close")
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Close...");
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Closed");
                    }
                    else if (receivedMessage == ".shutdown")
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Shutdown writes...");
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Writes closed");
                    }
                    else if (receivedMessage == ".abort")
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Abort...");
                        socket.Abort();
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Aborted");
                    }
                    else if (receivedMessage == ".delay5sec")
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Delay 5s...");
                        await Task.Delay(5000);
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Wait completed.");
                    }
                    else if (receivedMessage == ".receiveMessageAfterClose")
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Echo timestamp and close...");
                        string message = $"{receivedMessage} {DateTime.Now:HH:mm:ss}";
                        byte[] buffer = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(
                            new ArraySegment<byte>(buffer, 0, message.Length),
                            WebSocketMessageType.Text,
                            true,
                            cancellationToken);
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Sent: '{message}', closing...");
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Closed");
                    }
                    else if (socket.State == WebSocketState.Open)
                    {
                        //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Text message: '{receivedMessage}'");
                        sendMessage = true;
                    }
                }
                else
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Binary message (size='{offset}')");
                    sendMessage = true;
                }

                if (sendMessage)
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Echo...");
                    await socket.SendAsync(
                            new ArraySegment<byte>(receiveBuffer, 0, offset),
                            receiveResult.MessageType,
                            !replyWithPartialMessages,
                            cancellationToken);
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Sent");
                }
                if (receivedMessage == ".closeafter")
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Close after echo...");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Closed");
                }
                else if (receivedMessage == ".shutdownafter")
                {
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Requested: Shutdown writes after echo...");
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, receivedMessage, cancellationToken);
                    //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] Writes closed");
                }
            }

            //logger?.WriteLine($"[Server - {nameof(RunEchoAll)}] State={socket.State}, Receive loop completed.");
        }

        public static async Task RunEchoHeaders(WebSocket socket, IEnumerable<KeyValuePair<string, string>> headers, TextWriter? logger = null, CancellationToken cancellationToken = default)
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

            //logger?.WriteLine($"[Server - {nameof(RunEchoHeaders)}] Sending headers: {sb}");

            byte[] sendBuffer = Encoding.UTF8.GetBytes(sb.ToString());
            await socket.SendAsync(new ArraySegment<byte>(sendBuffer), WebSocketMessageType.Text, true, cancellationToken);

            //logger?.WriteLine($"[Server - {nameof(RunEchoHeaders)}] State={socket.State}, starting receive loop...");

            // Stay in loop while websocket is open
            while (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseSent)
            {
                logger?.WriteLine($"[Server - {nameof(RunEchoHeaders)}] State={socket.State}, waiting for messages...");
                var receiveResult = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                if (receiveResult.MessageType == WebSocketMessageType.Close)
                {
                    logger?.WriteLine($"[Server - {nameof(RunEchoHeaders)}] Received close frame, CloseStatus={receiveResult.CloseStatus}, CloseStatusDescription={receiveResult.CloseStatusDescription}");
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

            logger?.WriteLine($"[Server - {nameof(RunEchoHeaders)}] State={socket.State}, Receive loop completed.");
        }

        public static async ValueTask<WebSocketEchoOptions> ProcessOptions(string queryString, TextWriter? logger = null, CancellationToken cancellationToken = default)
        {
            //logger?.WriteLine($"[Server - {nameof(ProcessOptions)}] Processing echo options from query string = '{queryString}'");
            WebSocketEchoOptions options = WebSocketEchoOptions.Parse(queryString);
            if (options.Delay is TimeSpan d)
            {
                //logger?.WriteLine($"[Server - {nameof(ProcessOptions)}] delay = {d}");
                await Task.Delay(d, cancellationToken).ConfigureAwait(false);
            }
            return options;
        }
    }
}

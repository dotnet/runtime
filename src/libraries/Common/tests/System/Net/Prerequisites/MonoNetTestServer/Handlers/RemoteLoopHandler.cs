// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MonoNetTestServer
{
    public class RemoteLoopHandler
    {
        private const int MaxBufferSize = 128 * 1024;

        public static async Task InvokeAsync(HttpContext context)
        {
            try
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    context.Response.StatusCode = 400;
                    context.Response.ContentType = "text/plain";
                    await context.Response.WriteAsync("Not a websocket request");

                    return;
                }

                using (WebSocket socket = await context.WebSockets.AcceptWebSocketAsync())
                {
                    await ProcessWebSocketRequest(context, socket);
                }

            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
        }

        enum CommandType
        {
            Listen,
            Open,
            Send,
            Receive,
            Close
        }

        class Command
        {
            public CommandType Type { get; set; }
            public byte[] Data { get; set; }
            public int Port { get; set; }
            public int ListenBacklog { get; set; }
            public IPAddress Address { get; set; }
        }

        private static async Task ProcessWebSocketRequest(HttpContext context, WebSocket webSocket)
        {
            Socket listenSocket = null;
            Socket current = null;
            Memory<byte> ms = new Memory<byte>();

            ValueWebSocketReceiveResult result = await webSocket.ReceiveAsync(ms, CancellationToken.None);
            while (result.MessageType != WebSocketMessageType.Close)
            {
                Command command = null; // get bytes=> json=>command
                switch (command.Type)
                {
                    case CommandType.Listen:
                        listenSocket = new Socket(command.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                        listenSocket.Bind(new IPEndPoint(command.Address, 0));
                        listenSocket.Listen(command.ListenBacklog);
                        break;
                    case CommandType.Open:
                        current = await listenSocket.AcceptAsync().ConfigureAwait(false);
                        break;
                    case CommandType.Close:
                        current.Close();
                        break;
                }

                result = await webSocket.ReceiveAsync(ms, CancellationToken.None);
            }
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing remoteLoop", CancellationToken.None);

        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace RemoteLoopServer
{
    public partial class RemoteLoopHandler
    {
        public static async Task InvokeAsync(HttpContext context, ILogger logger)
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
                    await ProcessWebSocketRequest(context, socket, logger);
                }

            }
            catch (Exception ex)
            {
                logger.LogError("RemoteLoopHandler failed", ex);
            }
        }

        private static async Task ProcessWebSocketRequest(HttpContext context, WebSocket control, ILogger logger)
        {
            byte[] controlBuffer = new byte[128 * 1024];
            byte[] testedBuffer = new byte[128 * 1024];
            Socket listenSocket = null;
            Socket tested = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                WebSocketReceiveResult first = await control.ReceiveAsync(controlBuffer, cts.Token).ConfigureAwait(false);
                if (first.Count <= 0 || first.MessageType != WebSocketMessageType.Binary || control.State != WebSocketState.Open)
                {
                    throw new Exception("Unexpected close");
                }

                // parse setup request
                var message = Encoding.ASCII.GetString(controlBuffer, 0, first.Count);
                var split = message.Split(',');
                var listenBacklog = int.Parse(split[0]);
                var address = IPAddress.Parse(split[1]);

                listenSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(new IPEndPoint(address, 0));
                listenSocket.Listen(listenBacklog);
                EndPoint endPoint = listenSocket.LocalEndPoint;

                // respond with what we have done
                await control.SendAsync(Encoding.ASCII.GetBytes(endPoint.ToString()), WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(false);

                // wait for the tested client to connect
                tested = await listenSocket.AcceptAsync().ConfigureAwait(false);

                // now we are connected, pump messages
                bool close = false;

                Task<int> testedNext = tested.ReceiveAsync(new Memory<byte>(testedBuffer), SocketFlags.None, cts.Token).AsTask();
                Task<WebSocketReceiveResult> controlNext = control.ReceiveAsync(controlBuffer, cts.Token);
                while (!close)
                {
                    // wait for either message
                    await Task.WhenAny(testedNext, controlNext).ConfigureAwait(false);

                    if (testedNext.IsCompleted)
                    {
                        if (testedNext.IsCanceled || testedNext.IsFaulted)
                        {
                            close = true;
                        }
                        else
                        {
                            if (!tested.Connected)
                            {
                                close = true;
                            }
                            if (testedNext.Result > 0)
                            {
                                var slice = new ArraySegment<byte>(testedBuffer, 0, testedNext.Result);
                                await control.SendAsync(slice, WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(false);
                            }
                            if (!close)
                            {
                                testedNext = tested.ReceiveAsync(new Memory<byte>(testedBuffer), SocketFlags.None, cts.Token).AsTask();
                            }
                        }
                    }
                    if (controlNext.IsCompleted)
                    {
                        if (controlNext.IsCanceled || controlNext.IsFaulted)
                        {
                            close = true;
                        }
                        else
                        {
                            if (controlNext.Result.MessageType == WebSocketMessageType.Close)
                            {
                                close = true;
                            }
                            if (controlNext.Result.Count > 0)
                            {
                                var slice = new ArraySegment<byte>(controlBuffer, 0, controlNext.Result.Count);
                                await tested.SendAsync(slice, SocketFlags.None, cts.Token).ConfigureAwait(false);
                            }
                            if (!close)
                            {
                                controlNext = control.ReceiveAsync(new ArraySegment<byte>(controlBuffer), cts.Token);
                            }
                        }
                    }
                }
                if (tested.Connected)
                {
                    tested.Disconnect(false);
                }
                if (control.State == WebSocketState.Open || control.State == WebSocketState.Connecting || control.State == WebSocketState.None)
                {
                    try
                    {
                        await control.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing remoteLoop", cts.Token).ConfigureAwait(false);
                    }
                    catch (WebSocketException ex)
                    {
                        logger.LogWarning("ProcessWebSocketRequest closing failed", ex);
                    }
                }
                cts.Cancel();
            }
            catch (Exception ex)
            {
                logger.LogError("ProcessWebSocketRequest failed", ex);
            }
            finally
            {
                tested?.Dispose();
                control?.Dispose();
            }
        }
    }
}

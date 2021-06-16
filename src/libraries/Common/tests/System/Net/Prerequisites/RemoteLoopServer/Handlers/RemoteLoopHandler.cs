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

namespace RemoteLoopServer
{
    public partial class RemoteLoopHandler
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

        private static async Task ProcessWebSocketRequest(HttpContext context, WebSocket control)
        {
            byte[] controlBuffer = new byte[128 * 1024];
            byte[] testedBuffer = new byte[128 * 1024];
            Socket listenSocket = null;
            Socket tested = null;
            CancellationTokenSource cts = new CancellationTokenSource();
            try
            {
                WebSocketReceiveResult first = await control.ReceiveAsync(controlBuffer, cts.Token).ConfigureAwait(false);

                // parse request
                var message = Encoding.ASCII.GetString(controlBuffer, 0, first.Count);
                var split = message.Split(',');
                var listenBacklog = int.Parse(split[0]);
                var address = IPAddress.Parse(split[1]);

                listenSocket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(new IPEndPoint(address, 0));
                listenSocket.Listen(listenBacklog);
                EndPoint endPoint = listenSocket.RemoteEndPoint ?? listenSocket.LocalEndPoint;

                // respond with what we have done
                await control.SendAsync(Encoding.ASCII.GetBytes(endPoint.ToString()), WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(false);

                // wait for the tested client to connect
                tested = await listenSocket.AcceptAsync().ConfigureAwait(false);

                Task<int> testedNext = tested.ReceiveAsync(new Memory<byte>(testedBuffer), SocketFlags.None, cts.Token).AsTask();
                Task<WebSocketReceiveResult> controlNext = control.ReceiveAsync(controlBuffer, cts.Token);

                // now we are connected, pump messages
                bool close = false;
                while (!close)
                {
                    // wait for either message
                    await Task.WhenAny(testedNext, controlNext).ConfigureAwait(false);

                    if (testedNext.IsCompleted)
                    {
                        if (testedNext.Result > 0)
                        {
                            var slice = new ArraySegment<byte>(testedBuffer, 0, testedNext.Result);
                            await control.SendAsync(slice, WebSocketMessageType.Binary, true, cts.Token).ConfigureAwait(false);
                        }
                        if (testedNext.IsCanceled || testedNext.IsFaulted || !tested.Connected)
                        {
                            close = true;
                        }
                        else
                        {
                            testedNext = tested.ReceiveAsync(new Memory<byte>(testedBuffer), SocketFlags.None, cts.Token).AsTask();
                        }
                    }
                    if (controlNext.IsCompleted)
                    {
                        if (controlNext.Result.Count > 0)
                        {
                            var slice = new ArraySegment<byte>(controlBuffer, 0, controlNext.Result.Count);
                            await tested.SendAsync(slice, SocketFlags.None, cts.Token).ConfigureAwait(false);
                        }
                        if (controlNext.IsCanceled || controlNext.IsFaulted || controlNext.Result.MessageType == WebSocketMessageType.Close)
                        {
                            close = true;
                        }
                        else
                        {
                            controlNext = control.ReceiveAsync(new ArraySegment<byte>(controlBuffer), cts.Token);
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
                    catch (WebSocketException)
                    {
                        // We might want to log these exceptions. But for now we ignore them.
                    }
                }
                cts.Cancel();
            }
            catch (Exception)
            {
                // We might want to log these exceptions. But for now we ignore them.
            }
            finally
            {
                tested?.Dispose();
                control?.Dispose();
            }
        }

        private static (int, IPAddress) ReadListener(byte[] controlBuffer, int length)
        {
            ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(controlBuffer, 0, length);
            var req = MemoryMarshal.Cast<byte, long>(span);
            var listenBacklog = (int)req[0];
            var addrLength = (int)req[0];
            var addrBytes = span.Slice(2 * sizeof(long), addrLength);
            IPAddress address = new IPAddress(addrBytes);
            return (listenBacklog, address);
        }
    }
}

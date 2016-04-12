// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Xunit;

namespace System.Net.Sockets.Tests
{
    internal static partial class SocketAsyncExtensions
    {
        public static void AcceptAsync(this Socket socket, SocketAsyncEventArgs eventArgs, Action<Socket> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                Socket remote = args.AcceptSocket;
                args.AcceptSocket = null;
                args.Completed -= wrappedHandler;

                handler(remote);
            };

            eventArgs.Completed += wrappedHandler;
            if (!socket.AcceptAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void ConnectAsync(this Socket socket, SocketAsyncEventArgs eventArgs, EndPoint remoteEndpoint, Action handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                args.RemoteEndPoint = null;
                args.Completed -= wrappedHandler;

                handler();
            };

            eventArgs.RemoteEndPoint = remoteEndpoint;
            eventArgs.Completed += wrappedHandler;
            if (!socket.ConnectAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void ReceiveAsync(this Socket socket, SocketAsyncEventArgs eventArgs, byte[] buffer, int offset, int count, SocketFlags flags, Action<int> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int received = args.BytesTransferred;
                args.SetBuffer(null, 0, 0);
                args.SocketFlags = SocketFlags.None;
                args.Completed -= wrappedHandler;

                handler(received);
            };

            eventArgs.SetBuffer(buffer, offset, count);
            eventArgs.SocketFlags = flags;
            eventArgs.Completed += wrappedHandler;
            if (!socket.ReceiveAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void ReceiveAsync(this Socket socket, SocketAsyncEventArgs eventArgs, IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int received = args.BytesTransferred;
                args.BufferList = null;
                args.SocketFlags = SocketFlags.None;
                args.Completed -= wrappedHandler;

                handler(received);
            };

            eventArgs.BufferList = buffers;
            eventArgs.SocketFlags = flags;
            eventArgs.Completed += wrappedHandler;
            if (!socket.ReceiveAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void ReceiveFromAsync(this Socket socket, SocketAsyncEventArgs eventArgs, byte[] buffer, int offset, int count, SocketFlags flags, EndPoint remoteEndpoint, Action<int, EndPoint> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int received = args.BytesTransferred;
                EndPoint remote = args.RemoteEndPoint;
                args.SetBuffer(null, 0, 0);
                args.SocketFlags = SocketFlags.None;
                args.RemoteEndPoint = null;
                args.Completed -= wrappedHandler;

                handler(received, remote);
            };

            eventArgs.SetBuffer(buffer, offset, count);
            eventArgs.SocketFlags = flags;
            eventArgs.RemoteEndPoint = remoteEndpoint;
            eventArgs.Completed += wrappedHandler;
            if (!socket.ReceiveFromAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void SendAsync(this Socket socket, SocketAsyncEventArgs eventArgs, byte[] buffer, int offset, int count, SocketFlags flags, Action<int> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int sent = args.BytesTransferred;
                args.SetBuffer(null, 0, 0);
                args.SocketFlags = SocketFlags.None;
                args.Completed -= wrappedHandler;

                handler(sent);
            };

            eventArgs.SetBuffer(buffer, offset, count);
            eventArgs.SocketFlags = flags;
            eventArgs.Completed += wrappedHandler;
            if (!socket.SendAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void SendAsync(this Socket socket, SocketAsyncEventArgs eventArgs, IList<ArraySegment<byte>> buffers, SocketFlags flags, Action<int> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int sent = args.BytesTransferred;
                args.BufferList = null;
                args.SocketFlags = SocketFlags.None;
                args.Completed -= wrappedHandler;

                handler(sent);
            };

            eventArgs.BufferList = buffers;
            eventArgs.SocketFlags = flags;
            eventArgs.Completed += wrappedHandler;
            if (!socket.SendAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }

        public static void SendToAsync(this Socket socket, SocketAsyncEventArgs eventArgs, byte[] buffer, int offset, int count, SocketFlags flags, EndPoint remoteEndpoint, Action<int> handler)
        {
            EventHandler<SocketAsyncEventArgs> wrappedHandler = null;
            wrappedHandler = (_, args) =>
            {
                Assert.Equal(SocketError.Success, args.SocketError);

                int sent = args.BytesTransferred;
                args.SetBuffer(null, 0, 0);
                args.SocketFlags = SocketFlags.None;
                args.RemoteEndPoint = null;
                args.Completed -= wrappedHandler;

                handler(sent);
            };

            eventArgs.SetBuffer(buffer, offset, count);
            eventArgs.SocketFlags = flags;
            eventArgs.RemoteEndPoint = remoteEndpoint;
            eventArgs.Completed += wrappedHandler;
            if (!socket.SendToAsync(eventArgs))
            {
                wrappedHandler(null, eventArgs);
            }
        }
    }
}

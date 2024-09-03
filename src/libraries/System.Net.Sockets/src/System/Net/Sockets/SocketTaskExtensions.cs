// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SocketTaskExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<Socket> AcceptAsync(this Socket socket) =>
            socket.AcceptAsync();
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<Socket> AcceptAsync(this Socket socket, Socket? acceptSocket) =>
            socket.AcceptAsync(acceptSocket);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ConnectAsync(this Socket socket, EndPoint remoteEP) =>
            socket.ConnectAsync(remoteEP);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken) =>
            socket.ConnectAsync(remoteEP, cancellationToken);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ConnectAsync(this Socket socket, IPAddress address, int port) =>
            socket.ConnectAsync(address, port);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask ConnectAsync(this Socket socket, IPAddress address, int port, CancellationToken cancellationToken) =>
            socket.ConnectAsync(address, port, cancellationToken);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port) =>
            socket.ConnectAsync(addresses, port);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask ConnectAsync(this Socket socket, IPAddress[] addresses, int port, CancellationToken cancellationToken) =>
            socket.ConnectAsync(addresses, port, cancellationToken);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ConnectAsync(this Socket socket, string host, int port) =>
            socket.ConnectAsync(host, port);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask ConnectAsync(this Socket socket, string host, int port, CancellationToken cancellationToken) =>
            socket.ConnectAsync(host, port, cancellationToken);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            socket.ReceiveAsync(buffer, socketFlags);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) =>
            socket.ReceiveAsync(buffer, socketFlags, cancellationToken);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<int> ReceiveAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            socket.ReceiveAsync(buffers, socketFlags);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<SocketReceiveFromResult> ReceiveFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            socket.ReceiveFromAsync(buffer, socketFlags, remoteEndPoint);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            socket.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            socket.SendAsync(buffer, socketFlags);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) =>
            socket.SendAsync(buffer, socketFlags, cancellationToken);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<int> SendAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            socket.SendAsync(buffers, socketFlags);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<int> SendToAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP) =>
            socket.SendToAsync(buffer, socketFlags, remoteEP);
    }
}

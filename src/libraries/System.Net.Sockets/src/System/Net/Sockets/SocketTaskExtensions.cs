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
        public static async Task<Socket> AcceptAsync(this Socket socket) =>
            await socket.AcceptAsync().ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<Socket> AcceptAsync(this Socket socket, Socket? acceptSocket) =>
            await socket.AcceptAsync(acceptSocket).ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task ConnectAsync(this Socket socket, EndPoint remoteEP) =>
            await socket.ConnectAsync(remoteEP).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken) =>
            await socket.ConnectAsync(remoteEP, cancellationToken).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task ConnectAsync(this Socket socket, IPAddress address, int port) =>
            await socket.ConnectAsync(address, port).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask ConnectAsync(this Socket socket, IPAddress address, int port, CancellationToken cancellationToken) =>
            await socket.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port) =>
            await socket.ConnectAsync(addresses, port).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask ConnectAsync(this Socket socket, IPAddress[] addresses, int port, CancellationToken cancellationToken) =>
            await socket.ConnectAsync(addresses, port, cancellationToken).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task ConnectAsync(this Socket socket, string host, int port) =>
            await socket.ConnectAsync(host, port).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask ConnectAsync(this Socket socket, string host, int port, CancellationToken cancellationToken) =>
            await socket.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            await socket.ReceiveAsync(buffer, socketFlags).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) =>
            await socket.ReceiveAsync(buffer, socketFlags, cancellationToken).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<int> ReceiveAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            await socket.ReceiveAsync(buffers, socketFlags).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<SocketReceiveFromResult> ReceiveFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            await socket.ReceiveFromAsync(buffer, socketFlags, remoteEndPoint).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            await socket.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint).ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            await socket.SendAsync(buffer, socketFlags).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default) =>
            await socket.SendAsync(buffer, socketFlags, cancellationToken).ConfigureAwait(false);
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<int> SendAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            await socket.SendAsync(buffers, socketFlags).ConfigureAwait(false);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static async Task<int> SendToAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP) =>
            await socket.SendToAsync(buffer, socketFlags, remoteEP).ConfigureAwait(false);
    }
}

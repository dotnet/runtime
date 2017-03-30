// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public static class SocketTaskExtensions
    {
        public static Task<Socket> AcceptAsync(this Socket socket) =>
            socket.AcceptAsync((Socket)null);
        public static Task<Socket> AcceptAsync(this Socket socket, Socket acceptSocket) =>
            socket.AcceptAsync(acceptSocket);

        public static Task ConnectAsync(this Socket socket, EndPoint remoteEP) =>
            socket.ConnectAsync(remoteEP);
        public static Task ConnectAsync(this Socket socket, IPAddress address, int port) =>
            socket.ConnectAsync(address, port);
        public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port) =>
            socket.ConnectAsync(addresses, port);
        public static Task ConnectAsync(this Socket socket, string host, int port) => 
            socket.ConnectAsync(host, port);

        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            socket.ReceiveAsync(buffer, socketFlags, fromNetworkStream: false);
        public static Task<int> ReceiveAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            socket.ReceiveAsync(buffers, socketFlags);
        public static Task<SocketReceiveFromResult> ReceiveFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            socket.ReceiveFromAsync(buffer, socketFlags, remoteEndPoint);
        public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint) =>
            socket.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint);

        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags) =>
            socket.SendAsync(buffer, socketFlags, fromNetworkStream: false);
        public static Task<int> SendAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags) =>
            socket.SendAsync(buffers, socketFlags);
        public static Task<int> SendToAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP) =>
            socket.SendToAsync(buffer, socketFlags, remoteEP);
    }
}

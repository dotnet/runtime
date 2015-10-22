// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    // TODO #4052: Optimize delegate allocations.
    public static class SocketTaskExtensions
    {
        public static Task<Socket> AcceptAsync(this Socket socket)
        {
            return Task<Socket>.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, null);
        }

        public static Task<Socket> AcceptAsync(this Socket socket, Socket acceptSocket)
        {
            return Task<Socket>.Factory.FromAsync(socket.BeginAccept, socket.EndAccept, acceptSocket, 0, null);
        }

        public static Task ConnectAsync(this Socket socket, EndPoint remoteEndPoint)
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, remoteEndPoint, null);
        }

        public static Task ConnectAsync(this Socket socket, IPAddress address, int port)
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, address, port, null);
        }

        public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port)
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, addresses, port, null);
        }

        public static Task ConnectAsync(this Socket socket, string host, int port)
        {
            return Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, host, port, null);
        }

        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (callback, state) => socket.BeginReceive(
                                                buffer.Array, 
                                                buffer.Offset, 
                                                buffer.Count, 
                                                socketFlags, 
                                                callback, 
                                                state), 
                socket.EndReceive, null);
        }

        public static Task<int> ReceiveAsync(
            this Socket socket, 
            IList<ArraySegment<byte>> buffers, 
            SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(socket.BeginReceive, socket.EndReceive, buffers, socketFlags, null);
        }

        public static Task<SocketReceiveFromResult> ReceiveFromAsync(
            this Socket socket, 
            ArraySegment<byte> buffer, 
            SocketFlags socketFlags, 
            EndPoint remoteEndPoint)
        {
            return Task<SocketReceiveFromResult>.Factory.FromAsync(
                (callback, state) => socket.BeginReceiveFrom(
                    buffer.Array, 
                    buffer.Offset, 
                    buffer.Count, 
                    socketFlags, 
                    ref remoteEndPoint, 
                    callback, 
                    state),
                asyncResult => {
                    int bytesReceived = socket.EndReceiveFrom(asyncResult, ref remoteEndPoint);

                    return new SocketReceiveFromResult() {
                        ReceivedBytes = bytesReceived,
                        RemoteEndPoint = remoteEndPoint };
                },
                null);
        }

        public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(
            this Socket socket, 
            ArraySegment<byte> buffer, 
            SocketFlags socketFlags, 
            EndPoint remoteEndPoint)
        {
            return Task<SocketReceiveMessageFromResult>.Factory.FromAsync(
                (callback, state) => socket.BeginReceiveMessageFrom(
                    buffer.Array, 
                    buffer.Offset, 
                    buffer.Count, 
                    socketFlags, 
                    ref remoteEndPoint, 
                    callback, 
                    state),
                asyncResult => {
                    IPPacketInformation ipPacket;
                    int bytesReceived = socket.EndReceiveMessageFrom(
                        asyncResult, 
                        ref socketFlags, 
                        ref remoteEndPoint, 
                        out ipPacket);

                    return new SocketReceiveMessageFromResult() {
                        PacketInformation = ipPacket,
                        ReceivedBytes = bytesReceived,
                        RemoteEndPoint = remoteEndPoint,
                        SocketFlags = socketFlags };
                },
                null);
        }

        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (callback, state) => socket.BeginSend(
                    buffer.Array, 
                    buffer.Offset, 
                    buffer.Count, 
                    socketFlags, 
                    callback, 
                    state),
                socket.EndSend, 
                null);
        }

        public static Task<int> SendAsync(
            this Socket socket, 
            IList<ArraySegment<byte>> buffers, 
            SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(socket.BeginSend, socket.EndSend, buffers, socketFlags, null);
        }

        public static Task<int> SendToAsync(
            this Socket socket, 
            ArraySegment<byte> buffer, 
            SocketFlags socketFlags, 
            EndPoint remoteEndPoint)
        {
            return Task<int>.Factory.FromAsync(
                (callback, state) => socket.BeginSendTo(
                    buffer.Array, 
                    buffer.Offset, 
                    buffer.Count, 
                    socketFlags, 
                    remoteEndPoint, 
                    callback, 
                    state),
                socket.EndSendTo,
                null);
        }
    }
}

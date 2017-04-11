// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Net.Sockets
{
    public static class SocketTaskExtensions
    {
        public static Task<Socket> AcceptAsync(this Socket socket)
        {
            return Task<Socket>.Factory.FromAsync(
                (callback, state) => ((Socket)state).BeginAccept(callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndAccept(asyncResult),
                state: socket);
        }

        public static Task<Socket> AcceptAsync(this Socket socket, Socket acceptSocket)
        {
            const int ReceiveSize = 0;
            return Task<Socket>.Factory.FromAsync(
                (socketForAccept, receiveSize, callback, state) => ((Socket)state).BeginAccept(socketForAccept, receiveSize, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndAccept(asyncResult),
                acceptSocket,
                ReceiveSize,
                state: socket);
        }

        public static Task ConnectAsync(this Socket socket, EndPoint remoteEndPoint)
        {
            return Task.Factory.FromAsync(
                (targetEndPoint, callback, state) => ((Socket)state).BeginConnect(targetEndPoint, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                remoteEndPoint,
                state: socket);
        }

        public static Task ConnectAsync(this Socket socket, IPAddress address, int port)
        {
            return Task.Factory.FromAsync(
                (targetAddress, targetPort, callback, state) => ((Socket)state).BeginConnect(targetAddress, targetPort, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                address,
                port,
                state: socket);
        }

        public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port)
        {
            return Task.Factory.FromAsync(
                (targetAddresses, targetPort, callback, state) => ((Socket)state).BeginConnect(targetAddresses, targetPort, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                addresses,
                port,
                state: socket);
        }

        public static Task ConnectAsync(this Socket socket, string host, int port)
        {
            return Task.Factory.FromAsync(
                (targetHost, targetPort, callback, state) => ((Socket)state).BeginConnect(targetHost, targetPort, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                host,
                port,
                state: socket);
        }

        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) => ((Socket)state).BeginReceive(
                                                              targetBuffer.Array,
                                                              targetBuffer.Offset,
                                                              targetBuffer.Count,
                                                              flags,
                                                              callback,
                                                              state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndReceive(asyncResult),
                buffer,
                socketFlags,
                state: socket);
        }

        public static Task<int> ReceiveAsync(
            this Socket socket,
            IList<ArraySegment<byte>> buffers,
            SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffers, flags, callback, state) => ((Socket)state).BeginReceive(targetBuffers, flags, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndReceive(asyncResult),
                buffers,
                socketFlags,
                state: socket);
        }

        public static Task<SocketReceiveFromResult> ReceiveFromAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            object[] packedArguments = new object[] { socket, remoteEndPoint };

            return Task<SocketReceiveFromResult>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) =>
                {
                    var arguments = (object[])state;
                    var s = (Socket)arguments[0];
                    var e = (EndPoint)arguments[1];

                    IAsyncResult result = s.BeginReceiveFrom(
                        targetBuffer.Array,
                        targetBuffer.Offset,
                        targetBuffer.Count,
                        flags,
                        ref e,
                        callback,
                        state);

                    arguments[1] = e;
                    return result;
                },
                asyncResult =>
                {
                    var arguments = (object[])asyncResult.AsyncState;
                    var s = (Socket)arguments[0];
                    var e = (EndPoint)arguments[1];

                    int bytesReceived = s.EndReceiveFrom(asyncResult, ref e);

                    return new SocketReceiveFromResult()
                    {
                        ReceivedBytes = bytesReceived,
                        RemoteEndPoint = e
                    };
                },
                buffer,
                socketFlags,
                state: packedArguments);
        }

        public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            object[] packedArguments = new object[] { socket, socketFlags, remoteEndPoint };

            return Task<SocketReceiveMessageFromResult>.Factory.FromAsync(
                (targetBuffer, callback, state) =>
                {
                    var arguments = (object[])state;
                    var s = (Socket)arguments[0];
                    var f = (SocketFlags)arguments[1];
                    var e = (EndPoint)arguments[2];

                    IAsyncResult result = s.BeginReceiveMessageFrom(
                        targetBuffer.Array,
                        targetBuffer.Offset,
                        targetBuffer.Count,
                        f,
                        ref e,
                        callback,
                        state);

                    arguments[2] = e;
                    return result;
                },
                asyncResult =>
                {
                    var arguments = (object[])asyncResult.AsyncState;
                    var s = (Socket)arguments[0];
                    var f = (SocketFlags)arguments[1];
                    var e = (EndPoint)arguments[2];
                    IPPacketInformation ipPacket;

                    int bytesReceived = s.EndReceiveMessageFrom(
                        asyncResult,
                        ref f,
                        ref e,
                        out ipPacket);

                    return new SocketReceiveMessageFromResult()
                    {
                        PacketInformation = ipPacket,
                        ReceivedBytes = bytesReceived,
                        RemoteEndPoint = e,
                        SocketFlags = f
                    };
                },
                buffer,
                state: packedArguments);
        }

        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, callback, state) => ((Socket)state).BeginSend(
                                                              targetBuffer.Array,
                                                              targetBuffer.Offset,
                                                              targetBuffer.Count,
                                                              flags,
                                                              callback,
                                                              state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSend(asyncResult),
                buffer,
                socketFlags,
                state: socket);
        }

        public static Task<int> SendAsync(
            this Socket socket,
            IList<ArraySegment<byte>> buffers,
            SocketFlags socketFlags)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffers, flags, callback, state) => ((Socket)state).BeginSend(targetBuffers, flags, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSend(asyncResult),
                buffers,
                socketFlags,
                state: socket);
        }

        public static Task<int> SendToAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            return Task<int>.Factory.FromAsync(
                (targetBuffer, flags, endPoint, callback, state) => ((Socket)state).BeginSendTo(
                                                                        targetBuffer.Array,
                                                                        targetBuffer.Offset,
                                                                        targetBuffer.Count,
                                                                        flags,
                                                                        endPoint,
                                                                        callback,
                                                                        state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndSendTo(asyncResult),
                buffer,
                socketFlags,
                remoteEndPoint,
                state: socket);
        }
    }
}

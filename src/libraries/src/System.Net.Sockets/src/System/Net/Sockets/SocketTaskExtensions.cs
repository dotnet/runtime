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
            var tcs = new TaskCompletionSource<Socket>(socket);
            socket.BeginAccept(iar =>
            {
                var innerTcs = (TaskCompletionSource<Socket>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndAccept(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<Socket> AcceptAsync(this Socket socket, Socket acceptSocket)
        {
            const int ReceiveSize = 0;
            var tcs = new TaskCompletionSource<Socket>(socket);
            socket.BeginAccept(acceptSocket, ReceiveSize, iar =>
            {
                var innerTcs = (TaskCompletionSource<Socket>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndAccept(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task ConnectAsync(this Socket socket, EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task ConnectAsync(this Socket socket, IPAddress address, int port)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(address, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(addresses, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task ConnectAsync(this Socket socket, string host, int port)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(host, port, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginReceive(buffer.Array, buffer.Offset, buffer.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> ReceiveAsync(
            this Socket socket,
            IList<ArraySegment<byte>> buffers,
            SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginReceive(buffers, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndReceive(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<SocketReceiveFromResult> ReceiveFromAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            var tcs = new StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>(socket) { _field1 = remoteEndPoint };
            socket.BeginReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, socketFlags, ref tcs._field1, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<EndPoint, SocketReceiveFromResult>)iar.AsyncState;
                try
                {
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState).EndReceiveFrom(iar, ref innerTcs._field1);
                    innerTcs.TrySetResult(new SocketReceiveFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = innerTcs._field1
                    });
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEndPoint)
        {
            var tcs = new StateTaskCompletionSource<SocketFlags, EndPoint, SocketReceiveMessageFromResult>(socket) { _field1 = socketFlags, _field2 = remoteEndPoint };
            socket.BeginReceiveMessageFrom(buffer.Array, buffer.Offset, buffer.Count, socketFlags, ref tcs._field2, iar =>
            {
                var innerTcs = (StateTaskCompletionSource<SocketFlags, EndPoint, SocketReceiveMessageFromResult>)iar.AsyncState;
                try
                {
                    IPPacketInformation ipPacketInformation;
                    int receivedBytes = ((Socket)innerTcs.Task.AsyncState).EndReceiveMessageFrom(iar, ref innerTcs._field1, ref innerTcs._field2, out ipPacketInformation);
                    innerTcs.TrySetResult(new SocketReceiveMessageFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = innerTcs._field2,
                        SocketFlags = innerTcs._field1,
                        PacketInformation = ipPacketInformation
                    });
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginSend(buffer.Array, buffer.Offset, buffer.Count, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> SendAsync(
            this Socket socket,
            IList<ArraySegment<byte>> buffers,
            SocketFlags socketFlags)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginSend(buffers, socketFlags, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSend(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        public static Task<int> SendToAsync(
            this Socket socket,
            ArraySegment<byte> buffer,
            SocketFlags socketFlags,
            EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<int>(socket);
            socket.BeginSendTo(buffer.Array, buffer.Offset, buffer.Count, socketFlags, remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<int>)iar.AsyncState;
                try { innerTcs.TrySetResult(((Socket)innerTcs.Task.AsyncState).EndSendTo(iar)); }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }

        private class StateTaskCompletionSource<TField1, TResult> : TaskCompletionSource<TResult>
        {
            internal TField1 _field1;
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }

        private class StateTaskCompletionSource<TField1, TField2, TResult> : StateTaskCompletionSource<TField1, TResult>
        {
            internal TField2 _field2;
            public StateTaskCompletionSource(object baseState) : base(baseState) { }
        }
    }
}

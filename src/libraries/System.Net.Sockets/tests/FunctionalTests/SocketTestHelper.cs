// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    // Abstract base class for various different socket "modes" (sync, async, etc)
    // See SendReceive.cs for usage
    public abstract class SocketHelperBase
    {
        public abstract Task<Socket> AcceptAsync(Socket s);
        public abstract Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize);
        public abstract Task<Socket> AcceptAsync(Socket s, Socket acceptSocket);
        public abstract Task ConnectAsync(Socket s, EndPoint endPoint);
        public abstract Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port);
        public abstract Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<SocketReceiveFromResult> ReceiveFromAsync(
            Socket s, ArraySegment<byte> buffer, EndPoint endPoint);
        public abstract Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(
            Socket s, ArraySegment<byte> buffer, EndPoint endPoint);
        public abstract Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endpoint);
        public abstract Task SendFileAsync(Socket s, string fileName);
        public abstract Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags);
        public abstract Task DisconnectAsync(Socket s, bool reuseSocket);
        public virtual bool GuaranteedSendOrdering => true;
        public virtual bool ValidatesArrayArguments => true;
        public virtual bool UsesSync => false;
        public virtual bool UsesApm => false;
        public virtual bool UsesEap => false;
        public virtual bool ConnectAfterDisconnectResultsInInvalidOperationException => false;
        public virtual bool SupportsMultiConnect => true;
        public virtual bool SupportsAcceptIntoExistingSocket => true;
        public virtual bool SupportsAcceptReceive => false;
        public virtual bool SupportsSendFileSlicing => false;
        public virtual void Listen(Socket s, int backlog) { s.Listen(backlog); }
        public virtual void ConfigureNonBlocking(Socket s) { }
    }

    public class SocketHelperArraySync : SocketHelperBase
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => s.Accept());
        public override Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize) => throw new NotSupportedException();
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) => throw new NotSupportedException();
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => s.Connect(endPoint));
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) =>
            Task.Run(() => s.Connect(addresses, port));
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Receive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Run(() => s.Receive(bufferList, SocketFlags.None));
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() =>
            {
                int received = s.ReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, ref endPoint);
                return new SocketReceiveFromResult
                {
                    ReceivedBytes = received,
                    RemoteEndPoint = endPoint
                };
            });
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() =>
            {
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                int received = s.ReceiveMessageFrom(buffer.Array, buffer.Offset, buffer.Count, ref socketFlags, ref endPoint, out ipPacketInformation);
                return new SocketReceiveMessageFromResult
                {
                    ReceivedBytes = received,
                    SocketFlags = socketFlags,
                    RemoteEndPoint = endPoint,
                    PacketInformation = ipPacketInformation
                };
            });
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Run(() => s.Send(bufferList, SocketFlags.None));
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() => s.SendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endPoint));
        public override Task SendFileAsync(Socket s, string fileName) => Task.Run(() => s.SendFile(fileName));
        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            Task.Run(() => s.SendFile(fileName, preBuffer.Array, postBuffer.Array, flags));
        public override Task DisconnectAsync(Socket s, bool reuseSocket) =>
            Task.Run(() => s.Disconnect(reuseSocket));

        public override bool GuaranteedSendOrdering => false;
        public override bool UsesSync => true;
        public override bool ConnectAfterDisconnectResultsInInvalidOperationException => true;
        public override bool SupportsAcceptIntoExistingSocket => false;
    }

    public sealed class SocketHelperSyncForceNonBlocking : SocketHelperArraySync
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => { Socket accepted = s.Accept(); accepted.ForceNonBlocking(true); return accepted; });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => { s.ForceNonBlocking(true); s.Connect(endPoint); });
        public override void Listen(Socket s, int backlog)
        {
            s.Listen(backlog);
            s.ForceNonBlocking(true);
        }
        public override void ConfigureNonBlocking(Socket s) => s.ForceNonBlocking(true);
    }

    public sealed class SocketHelperApm : SocketHelperBase
    {
        public override bool SupportsAcceptReceive => true;

        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Factory.FromAsync(s.BeginAccept, s.EndAccept, null);
        public override async Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize)
        {
            byte[] buffer = null;

            IAsyncResult BeginAccept(AsyncCallback callback, object state) => s.BeginAccept(receiveSize, callback, state);
            Socket EndAccept(IAsyncResult iar) => s.EndAccept(out buffer, iar);

            Socket socket = await Task.Factory.FromAsync(BeginAccept, EndAccept, null);
            return (socket, buffer);
        }
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginAccept(acceptSocket, 0, callback, state),
                result => s.EndAccept(out _, out _, result),
                null);
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Factory.FromAsync(s.BeginConnect, s.EndConnect, endPoint, null);
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) =>
            Task.Factory.FromAsync(s.BeginConnect, s.EndConnect, addresses, port, null);
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Factory.FromAsync((callback, state) =>
                s.BeginReceive(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, callback, state),
                s.EndReceive, null);
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Factory.FromAsync(s.BeginReceive, s.EndReceive, bufferList, SocketFlags.None, null);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint)
        {
            var tcs = new TaskCompletionSource<SocketReceiveFromResult>();
            s.BeginReceiveFrom(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, ref endPoint, iar =>
            {
                try
                {
                    int receivedBytes = s.EndReceiveFrom(iar, ref endPoint);
                    tcs.TrySetResult(new SocketReceiveFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        RemoteEndPoint = endPoint
                    });
                }
                catch (Exception e) { tcs.TrySetException(e); }
            }, null);
            return tcs.Task;
        }
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint)
        {
            var tcs = new TaskCompletionSource<SocketReceiveMessageFromResult>();
            SocketFlags socketFlags = SocketFlags.None;
            s.BeginReceiveMessageFrom(buffer.Array, buffer.Offset, buffer.Count, socketFlags, ref endPoint, iar =>
            {
                try
                {
                    int receivedBytes = s.EndReceiveMessageFrom(iar, ref socketFlags, ref endPoint, out IPPacketInformation ipPacketInformation);
                    var result = new SocketReceiveMessageFromResult
                    {
                        ReceivedBytes = receivedBytes,
                        SocketFlags = socketFlags,
                        RemoteEndPoint = endPoint,
                        PacketInformation = ipPacketInformation
                    };
                    tcs.TrySetResult(result);
                }
                catch (Exception e) { tcs.TrySetException(e); }

            }, null);
            return tcs.Task;
        }
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Factory.FromAsync((callback, state) =>
                s.BeginSend(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, callback, state),
                s.EndSend, null);
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Factory.FromAsync(s.BeginSend, s.EndSend, bufferList, SocketFlags.None, null);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginSendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endPoint, callback, state),
                s.EndSendTo, null);
        public override Task SendFileAsync(Socket s, string fileName) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginSendFile(fileName, callback, state),
                s.EndSendFile, null);
        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginSendFile(fileName, preBuffer.Array, postBuffer.Array, flags, callback, state),
                s.EndSendFile, null);
        public override Task DisconnectAsync(Socket s, bool reuseSocket) =>
            Task.Factory.FromAsync(
                (callback, state) => s.BeginDisconnect(reuseSocket, callback, state),
                s.EndDisconnect, null);

        public override bool UsesApm => true;
    }

    // This class elides the SocketFlags argument in calls where possible.
    // SocketHelperCancellableTask does pass a SocketFlags argument where possible.
    // Together they provide coverage for overloads with and without SocketFlags.
    public class SocketHelperTask : SocketHelperBase
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            s.AcceptAsync();
        public override Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize)
            => throw new NotSupportedException();
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            s.AcceptAsync(acceptSocket);
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            s.ConnectAsync(endPoint);
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) =>
            s.ConnectAsync(addresses, port);
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync(buffer);
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.ReceiveAsync(bufferList);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.ReceiveFromAsync(buffer, endPoint);
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.ReceiveMessageFromAsync(buffer, endPoint);
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync(buffer);
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.SendAsync(bufferList);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.SendToAsync(buffer, endPoint);
        public override Task SendFileAsync(Socket s, string fileName) =>
            s.SendFileAsync(fileName).AsTask();
        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            s.SendFileAsync(fileName, preBuffer, postBuffer, flags).AsTask();
        public override Task DisconnectAsync(Socket s, bool reuseSocket) =>
            s.DisconnectAsync(reuseSocket).AsTask();
    }

    // Same as above, but call the CancellationToken overloads where possible
    public class SocketHelperCancellableTask : SocketHelperBase
    {
        // Use a cancellable CancellationToken that we never cancel so that implementations can't just elide handling the CancellationToken.
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        // This variant is typically working with Memory<T> overloads.
        public override bool ValidatesArrayArguments => false;

        public override Task<Socket> AcceptAsync(Socket s) =>
            s.AcceptAsync(_cts.Token).AsTask();
        public override Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize)
            => throw new NotSupportedException();
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            s.AcceptAsync(acceptSocket, _cts.Token).AsTask();
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            s.ConnectAsync(endPoint, _cts.Token).AsTask();
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) =>
            s.ConnectAsync(addresses, port, _cts.Token).AsTask();
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync(buffer, SocketFlags.None, _cts.Token).AsTask();
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.ReceiveAsync(bufferList, SocketFlags.None);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.ReceiveFromAsync(buffer, SocketFlags.None, endPoint, _cts.Token).AsTask();
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
           s.ReceiveMessageFromAsync(buffer, SocketFlags.None, endPoint, _cts.Token).AsTask();
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync(buffer, SocketFlags.None, _cts.Token).AsTask();
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.SendAsync(bufferList, SocketFlags.None);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.SendToAsync(buffer, SocketFlags.None, endPoint, _cts.Token).AsTask() ;
        public override Task SendFileAsync(Socket s, string fileName) =>
            s.SendFileAsync(fileName, _cts.Token).AsTask();
        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            s.SendFileAsync(fileName, preBuffer, postBuffer, flags, _cts.Token).AsTask();
        public override Task DisconnectAsync(Socket s, bool reuseSocket) =>
            s.DisconnectAsync(reuseSocket, _cts.Token).AsTask();
    }

    public sealed class SocketHelperEap : SocketHelperBase
    {
        public override bool UsesEap => true;
        public override bool ValidatesArrayArguments => false;
        public override bool SupportsAcceptReceive => PlatformDetection.IsWindows;

        public override Task<Socket> AcceptAsync(Socket s) =>
            InvokeAsync(s, e => e.AcceptSocket, e => s.AcceptAsync(e));
        public override Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize) =>
            InvokeAsync(s, e =>
            {
                byte[] buffer = new byte[receiveSize];
                Array.Copy(e.Buffer, buffer, receiveSize);
                return (e.AcceptSocket, buffer);
            }, e =>
            {
                // The buffer needs to be large enough for the two special sockaddr buffers that AcceptEx requires
                // see comments SocketAsyncEventArgs.StartOperationAccept()
                int bufferLength = receiveSize + 2 * (72 + 16); // 2 * (IPV6 size + 16)
                e.SetBuffer(new byte[bufferLength], 0, bufferLength);
                return s.AcceptAsync(e);
            });
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            InvokeAsync(s, e => e.AcceptSocket, e =>
            {
                e.AcceptSocket = acceptSocket;
                return s.AcceptAsync(e);
            });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            InvokeAsync(s, e => true, e =>
            {
                e.RemoteEndPoint = endPoint;
                return s.ConnectAsync(e);
            });
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) => throw new NotSupportedException();
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                return s.ReceiveAsync(e);
            });
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.BufferList = bufferList;
                return s.ReceiveAsync(e);
            });
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            InvokeAsync(s, e => new SocketReceiveFromResult { ReceivedBytes = e.BytesTransferred, RemoteEndPoint = e.RemoteEndPoint }, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                e.RemoteEndPoint = endPoint;
                return s.ReceiveFromAsync(e);
            });
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            InvokeAsync(s,
                e => new SocketReceiveMessageFromResult
                {
                    ReceivedBytes = e.BytesTransferred,
                    RemoteEndPoint = e.RemoteEndPoint,
                    SocketFlags = e.SocketFlags,
                    PacketInformation = e.ReceiveMessageFromPacketInfo
                },
                e =>
                {
                    e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                    e.RemoteEndPoint = endPoint;
                    return s.ReceiveMessageFromAsync(e);
                });
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                return s.SendAsync(e);
            });
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.BufferList = bufferList;
                return s.SendAsync(e);
            });
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            InvokeAsync(s, e => e.BytesTransferred, e =>
            {
                e.SetBuffer(buffer.Array, buffer.Offset, buffer.Count);
                e.RemoteEndPoint = endPoint;
                return s.SendToAsync(e);
            });
        public override Task SendFileAsync(Socket s, string fileName) => throw new NotSupportedException();
        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) => throw new NotSupportedException();
        public override Task DisconnectAsync(Socket s, bool reuseSocket) =>
            InvokeAsync(s, e => true, e =>
            {
                e.DisconnectReuseSocket = reuseSocket;
                return s.DisconnectAsync(e);
            });

        private static Task<TResult> InvokeAsync<TResult>(
            Socket s,
            Func<SocketAsyncEventArgs, TResult> getResult,
            Func<SocketAsyncEventArgs, bool> invoke)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var saea = new SocketAsyncEventArgs();
            EventHandler<SocketAsyncEventArgs> handler = (_, e) =>
            {
                if (e.SocketError == SocketError.Success)
                    tcs.SetResult(getResult(e));
                else
                    tcs.SetException(new SocketException((int)e.SocketError));
                saea.Dispose();
            };
            saea.Completed += handler;
            if (!invoke(saea))
                handler(s, saea);
            return tcs.Task;
        }

        public override bool SupportsMultiConnect => false;
    }

    public abstract class SocketTestHelperBase<T> : MemberDatas
        where T : SocketHelperBase, new()
    {
        private readonly T _socketHelper;
        public readonly ITestOutputHelper _output;

        public SocketTestHelperBase(ITestOutputHelper output)
        {
            _socketHelper = new T();
            _output = output;
        }

        //
        // Methods that delegate to SocketHelper implementation
        //

        public Task<Socket> AcceptAsync(Socket s) => _socketHelper.AcceptAsync(s);
        public Task<(Socket socket, byte[] buffer)> AcceptAsync(Socket s, int receiveSize) => _socketHelper.AcceptAsync(s, receiveSize);
        public Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) => _socketHelper.AcceptAsync(s, acceptSocket);
        public Task ConnectAsync(Socket s, EndPoint endPoint) => _socketHelper.ConnectAsync(s, endPoint);
        public Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) => _socketHelper.MultiConnectAsync(s, addresses, port);
        public Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) => _socketHelper.ReceiveAsync(s, buffer);
        public Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            _socketHelper.ReceiveFromAsync(s, buffer, endPoint);
        public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            _socketHelper.ReceiveMessageFromAsync(s, buffer, endPoint);
        public Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) => _socketHelper.ReceiveAsync(s, bufferList);
        public Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) => _socketHelper.SendAsync(s, buffer);
        public Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) => _socketHelper.SendAsync(s, bufferList);
        public Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endpoint) => _socketHelper.SendToAsync(s, buffer, endpoint);
        public Task SendFileAsync(Socket s, string fileName) => _socketHelper.SendFileAsync(s, fileName);
        public Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            _socketHelper.SendFileAsync(s, fileName, preBuffer, postBuffer, flags);
        public Task DisconnectAsync(Socket s, bool reuseSocket) => _socketHelper.DisconnectAsync(s, reuseSocket);
        public bool GuaranteedSendOrdering => _socketHelper.GuaranteedSendOrdering;
        public bool ValidatesArrayArguments => _socketHelper.ValidatesArrayArguments;
        public bool UsesSync => _socketHelper.UsesSync;
        public bool UsesApm => _socketHelper.UsesApm;
        public bool UsesEap => _socketHelper.UsesEap;
        public bool ConnectAfterDisconnectResultsInInvalidOperationException => _socketHelper.ConnectAfterDisconnectResultsInInvalidOperationException;
        public bool SupportsMultiConnect => _socketHelper.SupportsMultiConnect;
        public bool SupportsAcceptIntoExistingSocket => _socketHelper.SupportsAcceptIntoExistingSocket;
        public bool SupportsAcceptReceive => _socketHelper.SupportsAcceptReceive;
        public bool SupportsSendFileSlicing => _socketHelper.SupportsSendFileSlicing;
        public void Listen(Socket s, int backlog) => _socketHelper.Listen(s, backlog);
        public void ConfigureNonBlocking(Socket s) => _socketHelper.ConfigureNonBlocking(s);

        // A helper method to observe exceptions on sync paths of async variants.
        // In that case, exceptions should be seen without awaiting completion.
        // Synchronous variants are started on a separate thread using Task.Run(), therefore we should await the task.
        protected async Task<TException> AssertThrowsSynchronously<TException>(Func<Task> testCode)
            where TException : Exception
        {
            if (UsesSync)
            {
                return await Assert.ThrowsAsync<TException>(testCode);
            }
            else
            {
                return Assert.Throws<TException>(() => { _ = testCode(); });
            }
        }

        // When owning is false, replaces the socket argument with another Socket that
        // doesn't own the handle, and return a new owning handle.
        protected static SafeSocketHandle? ReplaceWithNonOwning(ref Socket socket, bool owning)
        {
            if (owning)
            {
                return null;
            }

            IntPtr handle = socket.SafeHandle.DangerousGetHandle();
            socket.SafeHandle.SetHandleAsInvalid();

            socket = new Socket(new SafeSocketHandle(handle, ownsHandle: false));

            return new SafeSocketHandle(handle, ownsHandle: true);
        }
    }

    // This class elides the SocketFlags argument in calls where possible.
    // SocketHelperArraySync does pass a SocketFlags argument where possible.
    // Together they provide coverage for overloads with and without SocketFlags.
    public class SocketHelperSpanSync : SocketHelperArraySync
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Receive((Span<byte>)buffer));
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send((ReadOnlySpan<byte>)buffer));
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer,
            EndPoint endPoint) =>
            Task.Run(() =>
            {
                int received = s.ReceiveFrom((Span<byte>)buffer, ref endPoint);
                return new SocketReceiveFromResult
                {
                    ReceivedBytes = received,
                    RemoteEndPoint = endPoint,
                };
            });
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() => s.SendTo((ReadOnlySpan<byte>)buffer, endPoint));
        public override Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() =>
            {
                SocketFlags socketFlags = SocketFlags.None;
                IPPacketInformation ipPacketInformation;
                int received = s.ReceiveMessageFrom((Span<byte>)buffer, ref socketFlags, ref endPoint, out ipPacketInformation);
                return new SocketReceiveMessageFromResult
                {
                    ReceivedBytes = received,
                    SocketFlags = socketFlags,
                    RemoteEndPoint = endPoint,
                    PacketInformation = ipPacketInformation
                };
            });

        public override Task SendFileAsync(Socket s, string fileName, ArraySegment<byte> preBuffer, ArraySegment<byte> postBuffer, TransmitFileOptions flags) =>
            Task.Run(() => s.SendFile(fileName, preBuffer, postBuffer, flags));
        public override bool UsesSync => true;
        public override bool SupportsSendFileSlicing => true;
    }

    public sealed class SocketHelperSpanSyncForceNonBlocking : SocketHelperSpanSync
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => { s.ForceNonBlocking(true); Socket accepted = s.Accept(); accepted.ForceNonBlocking(true); return accepted; });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => { s.ForceNonBlocking(true); s.Connect(endPoint); });
        public override void ConfigureNonBlocking(Socket s) => s.ForceNonBlocking(true);
    }

    public sealed class SocketHelperMemoryArrayTask : SocketHelperTask
    {
        public override bool ValidatesArrayArguments => false;
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync((Memory<byte>)buffer, SocketFlags.None).AsTask();
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync((ReadOnlyMemory<byte>)buffer, SocketFlags.None).AsTask();
    }

    public sealed class SocketHelperMemoryNativeTask : SocketHelperTask
    {
        public override bool ValidatesArrayArguments => false;
        public override async Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer)
        {
            using (var m = new NativeMemoryManager(buffer.Count))
            {
                int bytesReceived = await s.ReceiveAsync(m.Memory, SocketFlags.None).ConfigureAwait(false);
                m.Memory.Span.Slice(0, bytesReceived).CopyTo(buffer.AsSpan());
                return bytesReceived;
            }
        }

        public override async Task<int> SendAsync(Socket s, ArraySegment<byte> buffer)
        {
            using (var m = new NativeMemoryManager(buffer.Count))
            {
                buffer.AsSpan().CopyTo(m.Memory.Span);
                return await s.SendAsync(m.Memory, SocketFlags.None).ConfigureAwait(false);
            }
        }
    }

    //
    // MemberDatas that are generally useful
    //

    public abstract class MemberDatas
    {
        public static readonly object[][] Loopbacks = new[]
        {
            new object[] { IPAddress.Loopback },
            new object[] { IPAddress.IPv6Loopback },
        };

        public static readonly object[][] LoopbacksAndBuffers = new object[][]
        {
            new object[] { IPAddress.IPv6Loopback, true },
            new object[] { IPAddress.IPv6Loopback, false },
            new object[] { IPAddress.Loopback, true },
            new object[] { IPAddress.Loopback, false },
        };
    }

    //
    // Utility stuff
    //

    internal struct FakeArraySegment
    {
        public byte[] Array;
        public int Offset;
        public int Count;

        public ArraySegment<byte> ToActual()
        {
            ArraySegmentWrapper wrapper = default(ArraySegmentWrapper);
            wrapper.Fake = this;
            return wrapper.Actual;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ArraySegmentWrapper
    {
        [FieldOffset(0)] public ArraySegment<byte> Actual;
        [FieldOffset(0)] public FakeArraySegment Fake;
    }
}

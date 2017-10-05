// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Net.Sockets.Tests
{
    // Abstract base class for various different socket "modes" (sync, async, etc)
    // See SendReceive.cs for usage
    public abstract class SocketHelperBase
    {
        public abstract Task<Socket> AcceptAsync(Socket s);
        public abstract Task<Socket> AcceptAsync(Socket s, Socket acceptSocket);
        public abstract Task ConnectAsync(Socket s, EndPoint endPoint);
        public abstract Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port);
        public abstract Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<SocketReceiveFromResult> ReceiveFromAsync(
            Socket s, ArraySegment<byte> buffer, EndPoint endPoint);
        public abstract Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendAsync(Socket s, ArraySegment<byte> buffer);
        public abstract Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList);
        public abstract Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endpoint);
        public virtual bool GuaranteedSendOrdering => true;
        public virtual bool ValidatesArrayArguments => true;
        public virtual bool UsesSync => false;
        public virtual bool DisposeDuringOperationResultsInDisposedException => false;
        public virtual bool ConnectAfterDisconnectResultsInInvalidOperationException => false;
        public virtual bool SupportsMultiConnect => true;
        public virtual bool SupportsAcceptIntoExistingSocket => true;
    }

    public class SocketHelperArraySync : SocketHelperBase
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => s.Accept());
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
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            Task.Run(() => s.Send(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None));
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            Task.Run(() => s.Send(bufferList, SocketFlags.None));
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            Task.Run(() => s.SendTo(buffer.Array, buffer.Offset, buffer.Count, SocketFlags.None, endPoint));

        public override bool GuaranteedSendOrdering => false;
        public override bool UsesSync => true;
        public override bool ConnectAfterDisconnectResultsInInvalidOperationException => true;
        public override bool SupportsAcceptIntoExistingSocket => false;
    }

    public sealed class SocketHelperSyncForceNonBlocking : SocketHelperArraySync
    {
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Run(() => { s.ForceNonBlocking(true); Socket accepted = s.Accept(); accepted.ForceNonBlocking(true); return accepted; });
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            Task.Run(() => { s.ForceNonBlocking(true); s.Connect(endPoint); });
    }

    public sealed class SocketHelperApm : SocketHelperBase
    {
        public override bool DisposeDuringOperationResultsInDisposedException => true;
        public override Task<Socket> AcceptAsync(Socket s) =>
            Task.Factory.FromAsync(s.BeginAccept, s.EndAccept, null);
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            Task.Factory.FromAsync(s.BeginAccept, s.EndAccept, acceptSocket, 0, null);
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
    }

    public class SocketHelperTask : SocketHelperBase
    {
        public override bool DisposeDuringOperationResultsInDisposedException =>
            PlatformDetection.IsFullFramework; // due to SocketTaskExtensions.netfx implementation wrapping APM rather than EAP
        public override Task<Socket> AcceptAsync(Socket s) =>
            s.AcceptAsync();
        public override Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) =>
            s.AcceptAsync(acceptSocket);
        public override Task ConnectAsync(Socket s, EndPoint endPoint) =>
            s.ConnectAsync(endPoint);
        public override Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) =>
            s.ConnectAsync(addresses, port);
        public override Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) =>
            s.ReceiveAsync(buffer, SocketFlags.None);
        public override Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.ReceiveAsync(bufferList, SocketFlags.None);
        public override Task<SocketReceiveFromResult> ReceiveFromAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.ReceiveFromAsync(buffer, SocketFlags.None, endPoint);
        public override Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) =>
            s.SendAsync(buffer, SocketFlags.None);
        public override Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) =>
            s.SendAsync(bufferList, SocketFlags.None);
        public override Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endPoint) =>
            s.SendToAsync(buffer, SocketFlags.None, endPoint);
    }

    public sealed class SocketHelperEap : SocketHelperBase
    {
        public override bool ValidatesArrayArguments => false;

        public override Task<Socket> AcceptAsync(Socket s) =>
            InvokeAsync(s, e => e.AcceptSocket, e => s.AcceptAsync(e));
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

        public SocketTestHelperBase()
        {
            _socketHelper = new T();
        }

        //
        // Methods that delegate to SocketHelper implementation
        //

        public Task<Socket> AcceptAsync(Socket s) => _socketHelper.AcceptAsync(s);
        public Task<Socket> AcceptAsync(Socket s, Socket acceptSocket) => _socketHelper.AcceptAsync(s, acceptSocket);
        public Task ConnectAsync(Socket s, EndPoint endPoint) => _socketHelper.ConnectAsync(s, endPoint);
        public Task MultiConnectAsync(Socket s, IPAddress[] addresses, int port) => _socketHelper.MultiConnectAsync(s, addresses, port);
        public Task<int> ReceiveAsync(Socket s, ArraySegment<byte> buffer) => _socketHelper.ReceiveAsync(s, buffer);
        public Task<SocketReceiveFromResult> ReceiveFromAsync(
            Socket s, ArraySegment<byte> buffer, EndPoint endPoint) => _socketHelper.ReceiveFromAsync(s, buffer, endPoint);
        public Task<int> ReceiveAsync(Socket s, IList<ArraySegment<byte>> bufferList) => _socketHelper.ReceiveAsync(s, bufferList);
        public Task<int> SendAsync(Socket s, ArraySegment<byte> buffer) => _socketHelper.SendAsync(s, buffer);
        public Task<int> SendAsync(Socket s, IList<ArraySegment<byte>> bufferList) => _socketHelper.SendAsync(s, bufferList);
        public Task<int> SendToAsync(Socket s, ArraySegment<byte> buffer, EndPoint endpoint) => _socketHelper.SendToAsync(s, buffer, endpoint);
        public bool GuaranteedSendOrdering => _socketHelper.GuaranteedSendOrdering;
        public bool ValidatesArrayArguments => _socketHelper.ValidatesArrayArguments;
        public bool UsesSync => _socketHelper.UsesSync;
        public bool DisposeDuringOperationResultsInDisposedException => _socketHelper.DisposeDuringOperationResultsInDisposedException;
        public bool ConnectAfterDisconnectResultsInInvalidOperationException => _socketHelper.ConnectAfterDisconnectResultsInInvalidOperationException;
        public bool SupportsMultiConnect => _socketHelper.SupportsMultiConnect;
        public bool SupportsAcceptIntoExistingSocket => _socketHelper.SupportsAcceptIntoExistingSocket;
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

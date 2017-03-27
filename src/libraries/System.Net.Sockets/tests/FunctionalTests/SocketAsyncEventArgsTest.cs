// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketAsyncEventArgsTest
    {
        [Fact]
        public void Usertoken_Roundtrips()
        {
            using (var args = new SocketAsyncEventArgs())
            {
                object o = new object();
                Assert.Null(args.UserToken);
                args.UserToken = o;
                Assert.Same(o, args.UserToken);
            }
        }

        [Fact]
        public void SocketFlags_Roundtrips()
        {
            using (var args = new SocketAsyncEventArgs())
            {
                Assert.Equal(SocketFlags.None, args.SocketFlags);
                args.SocketFlags = SocketFlags.Broadcast;
                Assert.Equal(SocketFlags.Broadcast, args.SocketFlags);
            }
        }

        [Fact]
        public void SendPacketsSendSize_Roundtrips()
        {
            using (var args = new SocketAsyncEventArgs())
            {
                Assert.Equal(0, args.SendPacketsSendSize);
                args.SendPacketsSendSize = 4;
                Assert.Equal(4, args.SendPacketsSendSize);
            }
        }

        [Fact]
        public void SendPacketsFlags_Roundtrips()
        {
            using (var args = new SocketAsyncEventArgs())
            {
                Assert.Equal((TransmitFileOptions)0, args.SendPacketsFlags);
                args.SendPacketsFlags = TransmitFileOptions.UseDefaultWorkerThread;
                Assert.Equal(TransmitFileOptions.UseDefaultWorkerThread, args.SendPacketsFlags);
            }
        }

        [Fact]
        public void Dispose_MultipleCalls_Success()
        {
            using (var args = new SocketAsyncEventArgs())
            {
                args.Dispose();
            }
        }

        [Fact]
        public async Task Dispose_WhileInUse_DisposeDelayed()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listen.Listen(1);

                Task<Socket> acceptTask = listen.AcceptAsync();
                await Task.WhenAll(
                    acceptTask,
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                using (var receiveSaea = new SocketAsyncEventArgs())
                {
                    var tcs = new TaskCompletionSource<bool>();
                    receiveSaea.SetBuffer(new byte[1], 0, 1);
                    receiveSaea.Completed += delegate { tcs.SetResult(true); };

                    Assert.True(client.ReceiveAsync(receiveSaea));
                    Assert.Throws<InvalidOperationException>(() => client.ReceiveAsync(receiveSaea)); // already in progress

                    receiveSaea.Dispose();

                    server.Send(new byte[1]);
                    await tcs.Task; // completes successfully even though it was disposed

                    Assert.Throws<ObjectDisposedException>(() => client.ReceiveAsync(receiveSaea));
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ExecutionContext_FlowsIfNotSuppressed(bool suppressed)
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listen.Listen(1);

                Task<Socket> acceptTask = listen.AcceptAsync();
                await Task.WhenAll(
                    acceptTask,
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                using (var receiveSaea = new SocketAsyncEventArgs())
                {
                    if (suppressed)
                    {
                        ExecutionContext.SuppressFlow();
                    }

                    var local = new AsyncLocal<int>();
                    local.Value = 42;
                    int threadId = Environment.CurrentManagedThreadId;

                    var mres = new ManualResetEventSlim();
                    receiveSaea.SetBuffer(new byte[1], 0, 1);
                    receiveSaea.Completed += delegate
                    {
                        Assert.NotEqual(threadId, Environment.CurrentManagedThreadId);
                        Assert.Equal(suppressed ? 0 : 42, local.Value);
                        mres.Set();
                    };

                    Assert.True(client.ReceiveAsync(receiveSaea));
                    server.Send(new byte[1]);
                    mres.Wait();
                }
            }
        }

        [Fact]
        public void SetBuffer_InvalidArgs_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(new byte[1], -1, 0));
                Assert.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(new byte[1], 2, 0));
                Assert.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 0, -1));
                Assert.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 0, 2));
                Assert.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 1, 2));
            }
        }

        [Fact]
        public void SetBufferListWhenBufferSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };

                byte[] buffer = new byte[1];
                saea.SetBuffer(buffer, 0, 1);
                Assert.Throws<ArgumentException>(() => saea.BufferList = bufferList);
                Assert.Same(buffer, saea.Buffer);
                Assert.Null(saea.BufferList);

                saea.SetBuffer(null, 0, 0);
                saea.BufferList = bufferList; // works fine when Buffer has been set back to null
            }
        }

        [Fact]
        public void SetBufferWhenBufferListSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                saea.BufferList = bufferList;
                Assert.Throws<ArgumentException>(() => saea.SetBuffer(new byte[1], 0, 1));
                Assert.Same(bufferList, saea.BufferList);
                Assert.Null(saea.Buffer);

                saea.BufferList = null;
                saea.SetBuffer(new byte[1], 0, 1); // works fine when BufferList has been set back to null
            }
        }

        [Fact]
        public void SetBufferListWhenBufferListSet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Assert.Null(saea.BufferList);
                saea.BufferList = null;
                Assert.Null(saea.BufferList);

                var bufferList1 = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                saea.BufferList = bufferList1;
                Assert.Same(bufferList1, saea.BufferList);

                saea.BufferList = bufferList1;
                Assert.Same(bufferList1, saea.BufferList);

                var bufferList2 = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                saea.BufferList = bufferList2;
                Assert.Same(bufferList2, saea.BufferList);
            }
        }

        [Fact]
        public void SetBufferWhenBufferSet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                byte[] buffer1 = new byte[1];
                saea.SetBuffer(buffer1, 0, buffer1.Length);
                Assert.Same(buffer1, saea.Buffer);

                saea.SetBuffer(buffer1, 0, buffer1.Length);
                Assert.Same(buffer1, saea.Buffer);

                byte[] buffer2 = new byte[1];
                saea.SetBuffer(buffer2, 0, buffer1.Length);
                Assert.Same(buffer2, saea.Buffer);
            }
        }

        [Fact]
        public async Task Completed_RegisterThenInvoked_UnregisterThenNotInvoked()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listen.Listen(1);

                Task<Socket> acceptTask = listen.AcceptAsync();
                await Task.WhenAll(
                    acceptTask,
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                using (var receiveSaea = new SocketAsyncEventArgs())
                {
                    receiveSaea.SetBuffer(new byte[1], 0, 1);
                    TaskCompletionSource<bool> tcs1 = null, tcs2 = null;

                    EventHandler<SocketAsyncEventArgs> handler1 = (_, __) => tcs1.SetResult(true);
                    EventHandler<SocketAsyncEventArgs> handler2 = (_, __) => tcs2.SetResult(true);

                    receiveSaea.Completed += handler2;
                    receiveSaea.Completed += handler1;

                    tcs1 = new TaskCompletionSource<bool>();
                    tcs2 = new TaskCompletionSource<bool>();
                    Assert.True(client.ReceiveAsync(receiveSaea));

                    server.Send(new byte[1]);
                    await Task.WhenAll(tcs1.Task, tcs2.Task);

                    receiveSaea.Completed -= handler2;

                    tcs1 = new TaskCompletionSource<bool>();
                    tcs2 = new TaskCompletionSource<bool>();
                    Assert.True(client.ReceiveAsync(receiveSaea));

                    server.Send(new byte[1]);
                    await tcs1.Task;

                    Assert.False(tcs2.Task.IsCompleted);
                }
            }
        }

        [Fact]
        public void CancelConnectAsync_InstanceConnect_CancelsInProgressConnect()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                using (var connectSaea = new SocketAsyncEventArgs())
                {
                    var tcs = new TaskCompletionSource<SocketError>();
                    connectSaea.Completed += (s, e) => tcs.SetResult(e.SocketError);
                    connectSaea.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port);

                    Assert.True(client.ConnectAsync(connectSaea), $"ConnectAsync completed synchronously with SocketError == {connectSaea.SocketError}");
                    if (tcs.Task.IsCompleted)
                    {
                        Assert.NotEqual(SocketError.Success, tcs.Task.Result);
                    }

                    Socket.CancelConnectAsync(connectSaea);
                    Assert.False(client.Connected, "Expected Connected to be false");
                }
            }
        }

        [Fact]
        public void CancelConnectAsync_StaticConnect_CancelsInProgressConnect()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                using (var connectSaea = new SocketAsyncEventArgs())
                {
                    var tcs = new TaskCompletionSource<SocketError>();
                    connectSaea.Completed += (s, e) => tcs.SetResult(e.SocketError);
                    connectSaea.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port);

                    Assert.True(Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, connectSaea), $"ConnectAsync completed synchronously with SocketError == {connectSaea.SocketError}");
                    if (tcs.Task.IsCompleted)
                    {
                        Assert.NotEqual(SocketError.Success, tcs.Task.Result);
                    }

                    Socket.CancelConnectAsync(connectSaea);
                }
            }
        }

        [Fact]
        public async Task ReuseSocketAsyncEventArgs_SameInstance_MultipleSockets()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listen.Listen(1);

                Task<Socket> acceptTask = listen.AcceptAsync();
                await Task.WhenAll(
                    acceptTask,
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                {
                    TaskCompletionSource<bool> tcs = null;

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(new byte[1024], 0, 1024);
                    args.Completed += (_,__) => tcs.SetResult(true);

                    for (int i = 1; i <= 10; i++)
                    {
                        tcs = new TaskCompletionSource<bool>();
                        args.Buffer[0] = (byte)i;
                        args.SetBuffer(0, 1);
                        if (server.SendAsync(args))
                        {
                            await tcs.Task;
                        }

                        args.Buffer[0] = 0;
                        tcs = new TaskCompletionSource<bool>();
                        if (client.ReceiveAsync(args))
                        {
                            await tcs.Task;
                        }
                        Assert.Equal(1, args.BytesTransferred);
                        Assert.Equal(i, args.Buffer[0]);
                    }
                }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public async Task ReuseSocketAsyncEventArgs_MutateBufferList()
        {
            using (var listen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listen.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listen.Listen(1);

                Task<Socket> acceptTask = listen.AcceptAsync();
                await Task.WhenAll(
                    acceptTask,
                    client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)listen.LocalEndPoint).Port)));

                using (Socket server = await acceptTask)
                {
                    TaskCompletionSource<bool> tcs = null;

                    var sendBuffer = new byte[64];
                    var sendBufferList = new List<ArraySegment<byte>>();
                    sendBufferList.Add(new ArraySegment<byte>(sendBuffer, 0, 1));
                    var sendArgs = new SocketAsyncEventArgs();
                    sendArgs.BufferList = sendBufferList;
                    sendArgs.Completed += (_,__) => tcs.SetResult(true);

                    var recvBuffer = new byte[64];
                    var recvBufferList = new List<ArraySegment<byte>>();
                    recvBufferList.Add(new ArraySegment<byte>(recvBuffer, 0, 1));
                    var recvArgs = new SocketAsyncEventArgs();
                    recvArgs.BufferList = recvBufferList;
                    recvArgs.Completed += (_,__) => tcs.SetResult(true);

                    for (int i = 1; i <= 10; i++)
                    {
                        tcs = new TaskCompletionSource<bool>();

                        sendBuffer[0] = (byte)i;
                        if (server.SendAsync(sendArgs))
                        {
                            await tcs.Task;
                        }

                        recvBuffer[0] = 0;
                        tcs = new TaskCompletionSource<bool>();
                        if (client.ReceiveAsync(recvArgs))
                        {
                            await tcs.Task;
                        }

                        Assert.Equal(1, recvArgs.BytesTransferred);
                        Assert.Equal(i, recvBuffer[0]);

                        // Mutate the send/recv BufferLists
                        // This should not affect Send or Receive behavior, since the buffer list is cached
                        // at the time it is set.
                        sendBufferList[0] = new ArraySegment<byte>(sendBuffer, i, 1);
                        sendBufferList.Insert(0, new ArraySegment<byte>(sendBuffer, i * 2, 1));

                        recvBufferList[0] = new ArraySegment<byte>(recvBuffer, i, 1);
                        recvBufferList.Add(new ArraySegment<byte>(recvBuffer, i * 2, 1));
                    }
                }
            }
        }
    }
}

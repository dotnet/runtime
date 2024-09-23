// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net.Test.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
                    var tcs = new TaskCompletionSource();
                    receiveSaea.SetBuffer(new byte[1], 0, 1);
                    receiveSaea.Completed += delegate { tcs.SetResult(); };

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
                    using (suppressed ? ExecutionContext.SuppressFlow() : default)
                    {
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
        }

        [Fact]
        public async Task ExecutionContext_SocketAsyncEventArgs_Ctor_Default_FlowIsNotSuppressed()
        {
            await ExecutionContext_SocketAsyncEventArgs_Ctors(() => new SocketAsyncEventArgs(), false);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExecutionContext_SocketAsyncEventArgs_Ctor_UnsafeSuppressExecutionContextFlow(bool suppressed)
        {
            await ExecutionContext_SocketAsyncEventArgs_Ctors(() => new SocketAsyncEventArgs(suppressed), suppressed);
        }

        private async Task ExecutionContext_SocketAsyncEventArgs_Ctors(Func<SocketAsyncEventArgs> saeaFactory, bool suppressed)
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
                using (SocketAsyncEventArgs receiveSaea = saeaFactory())
                {
                    var local = new AsyncLocal<int>
                    {
                        Value = 42
                    };
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
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(new byte[1], -1, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(new byte[1], 2, 0));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 0, 2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(new byte[1], 1, 2));

                saea.SetBuffer(new byte[2], 0, 2);
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(-1, 2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => saea.SetBuffer(3, 2));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(0, -1));
                AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => saea.SetBuffer(0, 3));
            }
        }

        [Fact]
        public void SetBuffer_NoBuffer_ResetsCountOffset()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                saea.SetBuffer(42, 84);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);

                saea.SetBuffer(new byte[3], 1, 2);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                saea.SetBuffer(null, 1, 2);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);
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
                AssertExtensions.Throws<ArgumentException>(null, () => saea.BufferList = bufferList);
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
                AssertExtensions.Throws<ArgumentException>(null, () => saea.SetBuffer(new byte[1], 0, 1));
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

        [Theory]
        [InlineData(1, -1, 0)] // offset low
        [InlineData(1, 2, 0)] // offset high
        [InlineData(1, 0, -1)] // count low
        [InlineData(1, 1, 2)] // count high
        public void BufferList_InvalidArguments_Throws(int length, int offset, int count)
        {
            using (var e = new SocketAsyncEventArgs())
            {
                ArraySegment<byte> invalidBuffer = new FakeArraySegment { Array = new byte[length], Offset = offset, Count = count }.ToActual();
                Assert.Throws<ArgumentOutOfRangeException>(() => e.BufferList = new List<ArraySegment<byte>> { invalidBuffer });

                ArraySegment<byte> validBuffer = new ArraySegment<byte>(new byte[1]);
                Assert.Throws<ArgumentOutOfRangeException>(() => e.BufferList = new List<ArraySegment<byte>> { validBuffer, invalidBuffer });
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
                    TaskCompletionSource tcs1 = null, tcs2 = null;

                    EventHandler<SocketAsyncEventArgs> handler1 = (_, __) => tcs1.SetResult();
                    EventHandler<SocketAsyncEventArgs> handler2 = (_, __) => tcs2.SetResult();

                    receiveSaea.Completed += handler2;
                    receiveSaea.Completed += handler1;

                    tcs1 = new TaskCompletionSource();
                    tcs2 = new TaskCompletionSource();
                    Assert.True(client.ReceiveAsync(receiveSaea));

                    server.Send(new byte[1]);
                    await Task.WhenAll(tcs1.Task, tcs2.Task);

                    receiveSaea.Completed -= handler2;

                    tcs1 = new TaskCompletionSource();
                    tcs2 = new TaskCompletionSource();
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

                    bool pending = client.ConnectAsync(connectSaea);
                    if (!pending) tcs.SetResult(connectSaea.SocketError);
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

                    bool pending = Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, connectSaea);
                    if (!pending) tcs.SetResult(connectSaea.SocketError);
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
                    TaskCompletionSource tcs = null;

                    var args = new SocketAsyncEventArgs();
                    args.SetBuffer(new byte[1024], 0, 1024);
                    args.Completed += (_, __) => tcs.SetResult();

                    for (int i = 1; i <= 10; i++)
                    {
                        tcs = new TaskCompletionSource();
                        args.Buffer[0] = (byte)i;
                        args.SetBuffer(0, 1);
                        if (server.SendAsync(args))
                        {
                            await tcs.Task;
                        }

                        args.Buffer[0] = 0;
                        tcs = new TaskCompletionSource();
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

        [OuterLoop]
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
                    TaskCompletionSource tcs = null;

                    var sendBuffer = new byte[64];
                    var sendBufferList = new List<ArraySegment<byte>>();
                    sendBufferList.Add(new ArraySegment<byte>(sendBuffer, 0, 1));
                    var sendArgs = new SocketAsyncEventArgs();
                    sendArgs.BufferList = sendBufferList;
                    sendArgs.Completed += (_, __) => tcs.SetResult();

                    var recvBuffer = new byte[64];
                    var recvBufferList = new List<ArraySegment<byte>>();
                    recvBufferList.Add(new ArraySegment<byte>(recvBuffer, 0, 1));
                    var recvArgs = new SocketAsyncEventArgs();
                    recvArgs.BufferList = recvBufferList;
                    recvArgs.Completed += (_, __) => tcs.SetResult();

                    for (int i = 1; i <= 10; i++)
                    {
                        tcs = new TaskCompletionSource();

                        sendBuffer[0] = (byte)i;
                        if (server.SendAsync(sendArgs))
                        {
                            await tcs.Task;
                        }

                        recvBuffer[0] = 0;
                        tcs = new TaskCompletionSource();
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

        private static void OnAcceptCompleted(object sender, SocketAsyncEventArgs args)
        {
            EventWaitHandle handle = (EventWaitHandle)args.UserToken;
            handle.Set();
        }

        [OuterLoop]
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Unix platforms don't yet support receiving data with AcceptAsync.
        public void AcceptAsync_WithReceiveBuffer_Success()
        {
            Assert.True(Capability.IPv4Support());

            AutoResetEvent accepted = new AutoResetEvent(false);

            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                const int acceptBufferOverheadSize = 288; // see https://msdn.microsoft.com/en-us/library/system.net.sockets.socket.acceptasync(v=vs.110).aspx
                const int acceptBufferDataSize = 256;
                const int acceptBufferSize = acceptBufferOverheadSize + acceptBufferDataSize;

                byte[] sendBuffer = new byte[acceptBufferDataSize];
                Random.Shared.NextBytes(sendBuffer);

                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptCompleted;
                acceptArgs.UserToken = accepted;
                acceptArgs.SetBuffer(new byte[acceptBufferSize], 0, acceptBufferSize);

                Assert.True(server.AcceptAsync(acceptArgs));

                using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.Connect(IPAddress.Loopback, port);
                    client.Send(sendBuffer);
                    client.Shutdown(SocketShutdown.Both);
                }

                Assert.True(accepted.WaitOne(TestSettings.PassingTestTimeout), "Test completed in allotted time");

                Assert.Equal(SocketError.Success, acceptArgs.SocketError);

                Assert.Equal(acceptBufferDataSize, acceptArgs.BytesTransferred);

                AssertExtensions.SequenceEqual(sendBuffer, acceptArgs.Buffer.AsSpan(0, acceptArgs.BytesTransferred));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Unix platforms don't yet support receiving data with AcceptAsync.
        public void AcceptAsync_WithTooSmallReceiveBuffer_Failure()
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptCompleted;
                acceptArgs.UserToken = new ManualResetEvent(false);

                byte[] buffer = new byte[1];
                acceptArgs.SetBuffer(buffer, 0, buffer.Length);

                AssertExtensions.Throws<ArgumentException>("Count", () => server.AcceptAsync(acceptArgs));
            }
        }

        [OuterLoop]
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Unix platforms don't yet support receiving data with AcceptAsync.
        public void AcceptAsync_WithReceiveBuffer_Failure()
        {
            Assert.True(Capability.IPv4Support());

            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                int port = server.BindToAnonymousPort(IPAddress.Loopback);
                server.Listen(1);

                SocketAsyncEventArgs acceptArgs = new SocketAsyncEventArgs();
                acceptArgs.Completed += OnAcceptCompleted;
                acceptArgs.UserToken = new ManualResetEvent(false);

                byte[] buffer = new byte[1024];
                acceptArgs.SetBuffer(buffer, 0, buffer.Length);

                Assert.Throws<PlatformNotSupportedException>(() => server.AcceptAsync(acceptArgs));
            }
        }

        [Fact]
        public async Task SocketConnectAsync_IPAddressAny_SocketAsyncEventArgsReusableAfterFailure()
        {
            var e = new SocketAsyncEventArgs();

            foreach (DnsEndPoint dns in new[] { new DnsEndPoint("::0", 80), new DnsEndPoint("0.0.0.0", 80) })
            {
                e.RemoteEndPoint = dns;

                AssertExtensions.Throws<ArgumentException>("hostName", () => Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e));
                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    AssertExtensions.Throws<ArgumentException>("hostName", () => client.ConnectAsync(e));
                }
            }

            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);
                e.RemoteEndPoint = listener.LocalEndPoint;

                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                e.Completed += delegate { tcs.SetResult(); };

                Task<Socket> acceptTask = listener.AcceptAsync();
                if (Socket.ConnectAsync(SocketType.Stream, ProtocolType.Tcp, e))
                {
                    await new Task[] { tcs.Task, acceptTask }.WhenAllOrAnyFailed();
                }

                e.ConnectSocket.Dispose();
                (await acceptTask).Dispose();
            }
        }

        [Fact]
        public void SetBuffer_MemoryBuffer_Roundtrips()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> memory = new byte[42];
                saea.SetBuffer(memory);
                Assert.True(memory.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(memory.Length, saea.Count);
                Assert.Null(saea.Buffer);
            }
        }

        [Fact]
        public void SetBufferMemory_ThenSetBufferIntInt_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> memory = new byte[42];
                saea.SetBuffer(memory);
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(0, 42));
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(0, 0));
                Assert.Throws<InvalidOperationException>(() => saea.SetBuffer(1, 2));
                Assert.True(memory.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(memory.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferArrayIntInt_AvailableFromMemoryBuffer()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                byte[] array = new byte[42];

                saea.SetBuffer(array, 0, array.Length);
                Assert.True(MemoryMarshal.TryGetArray(saea.MemoryBuffer, out ArraySegment<byte> result));
                Assert.Same(array, result.Array);
                Assert.Same(saea.Buffer, array);
                Assert.Equal(0, result.Offset);
                Assert.Equal(array.Length, result.Count);

                saea.SetBuffer(1, 2);
                Assert.Same(saea.Buffer, array);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                Assert.True(MemoryMarshal.TryGetArray(saea.MemoryBuffer, out result));
                Assert.Same(array, result.Array);
                Assert.Equal(0, result.Offset);
                Assert.Equal(array.Length, result.Count);
            }
        }

        [Fact]
        public void SetBufferMemory_Default_ResetsCountOffset()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                saea.SetBuffer(42, 84);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);

                saea.SetBuffer(new byte[3], 1, 2);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                saea.SetBuffer(Memory<byte>.Empty);
                Assert.Null(saea.Buffer);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(0, saea.Count);
            }
        }

        [Fact]
        public void SetBufferListWhenMemoryBufferSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                Memory<byte> buffer = new byte[1];

                saea.SetBuffer(buffer);
                AssertExtensions.Throws<ArgumentException>(null, () => saea.BufferList = bufferList);
                Assert.True(buffer.Equals(saea.MemoryBuffer));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer.Length, saea.Count);
                Assert.Null(saea.BufferList);

                saea.SetBuffer(Memory<byte>.Empty);
                saea.BufferList = bufferList; // works fine when Buffer has been set back to null
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferListSet_Throws()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                var bufferList = new List<ArraySegment<byte>> { new ArraySegment<byte>(new byte[1]) };
                saea.BufferList = bufferList;

                saea.SetBuffer(Memory<byte>.Empty); // nop

                Memory<byte> buffer = new byte[2];
                AssertExtensions.Throws<ArgumentException>(null, () => saea.SetBuffer(buffer));
                Assert.Same(bufferList, saea.BufferList);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(default));

                saea.BufferList = null;
                saea.SetBuffer(buffer); // works fine when BufferList has been set back to null
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferMemorySet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                Memory<byte> buffer1 = new byte[1];
                Memory<byte> buffer2 = new byte[2];

                for (int i = 0; i < 2; i++)
                {
                    saea.SetBuffer(buffer1);
                    Assert.Null(saea.Buffer);
                    Assert.True(saea.MemoryBuffer.Equals(buffer1));
                    Assert.Equal(0, saea.Offset);
                    Assert.Equal(buffer1.Length, saea.Count);
                }

                saea.SetBuffer(buffer2);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(buffer2));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer2.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferMemoryWhenBufferSet_Succeeds()
        {
            using (var saea = new SocketAsyncEventArgs())
            {
                byte[] buffer1 = new byte[3];
                Memory<byte> buffer2 = new byte[4];

                saea.SetBuffer(buffer1, 0, buffer1.Length);
                Assert.Same(buffer1, saea.Buffer);
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer1.Length, saea.Count);

                saea.SetBuffer(1, 2);
                Assert.Same(buffer1, saea.Buffer);
                Assert.Equal(1, saea.Offset);
                Assert.Equal(2, saea.Count);

                saea.SetBuffer(buffer2);
                Assert.Null(saea.Buffer);
                Assert.True(saea.MemoryBuffer.Equals(buffer2));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(buffer2.Length, saea.Count);
            }
        }

        [Fact]
        public void SetBufferMemory_NonArray_BufferReturnsNull()
        {
            using (var m = new NativeMemoryManager(42))
            using (var saea = new SocketAsyncEventArgs())
            {
                saea.SetBuffer(m.Memory);
                Assert.True(saea.MemoryBuffer.Equals(m.Memory));
                Assert.Equal(0, saea.Offset);
                Assert.Equal(m.Memory.Length, saea.Count);
                Assert.Null(saea.Buffer);
            }
        }

        [OuterLoop("Involves GC and finalization")]
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public void Finalizer_InvokedWhenNoLongerReferenced(bool afterAsyncOperation)
        {
            var cwt = new ConditionalWeakTable<object, object>();

            for (int i = 0; i < 5; i++) // create several SAEA instances, stored into cwt
            {
                CreateSocketAsyncEventArgs();

                void CreateSocketAsyncEventArgs() // separated out so that JIT doesn't extend lifetime of SAEA instances
                {
                    var saea = new SocketAsyncEventArgs();
                    cwt.Add(saea, saea);

                    if (afterAsyncOperation)
                    {
                        using (Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                        {
                            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                            listener.Listen(1);

                            using (Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                            {
                                saea.RemoteEndPoint = listener.LocalEndPoint;
                                using (var mres = new ManualResetEventSlim())
                                {
                                    saea.Completed += (s, e) => mres.Set();
                                    if (client.ConnectAsync(saea))
                                    {
                                        mres.Wait();
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Assert.True(SpinWait.SpinUntil(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return cwt.Count() == 0; // validate that the cwt becomes empty
            }, 30_000));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task SendTo_DifferentEP_Success(bool ipv4)
        {
            IPAddress address = ipv4 ? IPAddress.Loopback : IPAddress.IPv6Loopback;
            IPEndPoint remoteEp = new IPEndPoint(address, 0);

            using Socket receiver1 = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket receiver2 = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

            receiver1.BindToAnonymousPort(address);
            receiver2.BindToAnonymousPort(address);

            byte[] sendBuffer = new byte[32];
            var receiveInternalBuffer = new byte[sendBuffer.Length];
            ArraySegment<byte> receiveBuffer = new ArraySegment<byte>(receiveInternalBuffer, 0, receiveInternalBuffer.Length);

            using SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
            ManualResetEventSlim mres = new ManualResetEventSlim(false); 

            saea.SetBuffer(sendBuffer);
            saea.RemoteEndPoint = receiver1.LocalEndPoint;
            saea.Completed += delegate { mres.Set(); };
            if (sender.SendToAsync(saea))
            {
                // did not finish synchronously.
                mres.Wait();
            }

            SocketReceiveFromResult result = await receiver1.ReceiveFromAsync(receiveBuffer, remoteEp).WaitAsync(TestSettings.PassingTestTimeout);
            Assert.Equal(sendBuffer.Length, result.ReceivedBytes);
            mres.Reset();


            saea.RemoteEndPoint = receiver2.LocalEndPoint;
            if (sender.SendToAsync(saea))
            {
                // did not finish synchronously.
                mres.Wait();
            }

            result = await receiver2.ReceiveFromAsync(receiveBuffer, remoteEp).WaitAsync(TestSettings.PassingTestTimeout);
            Assert.Equal(sendBuffer.Length, result.ReceivedBytes);
        }
    }
}

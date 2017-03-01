// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketAsyncEventArgsTest
    {
        [Fact]
        public void TestDefaultConstructor()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            Assert.Equal(0, args.SendPacketsSendSize);
            Assert.Equal((TransmitFileOptions)0, args.SendPacketsFlags);
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

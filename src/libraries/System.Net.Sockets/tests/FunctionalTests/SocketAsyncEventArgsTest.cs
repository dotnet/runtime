// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketAsyncEventArgsTest
    {
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
    }
}

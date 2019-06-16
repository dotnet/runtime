// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class DisposedSocket
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NonDisposedSocket_SafeHandlesCollected(bool clientAsync)
        {
            List<WeakReference> handles = await CreateHandlesAsync(clientAsync);
            RetryHelper.Execute(() =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Assert.Equal(0, handles.Count(h => h.IsAlive));
            });
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static async Task<List<WeakReference>> CreateHandlesAsync(bool clientAsync)
        {
            var handles = new List<WeakReference>();

            using (var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(int.MaxValue);

                for (int i = 0; i < 100; i++)
                {
                    var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // do not dispose
                    handles.Add(new WeakReference(client.SafeHandle));
                    if (clientAsync)
                    {
                        await client.ConnectAsync(listener.LocalEndPoint);
                    }
                    else
                    {
                        client.Connect(listener.LocalEndPoint);
                    }

                    using (Socket server = listener.Accept())
                    {
                        if (clientAsync)
                        {
                            Task<int> receiveTask = client.ReceiveAsync(new ArraySegment<byte>(new byte[1]), SocketFlags.None);
                            Assert.Equal(1, server.Send(new byte[1]));
                            Assert.Equal(1, await receiveTask);
                        }
                        else
                        {
                            Assert.Equal(1, server.Send(new byte[1]));
                            Assert.Equal(1, client.Receive(new byte[1]));
                        }
                    }
                }
            }

            return handles;
        }
    }
}

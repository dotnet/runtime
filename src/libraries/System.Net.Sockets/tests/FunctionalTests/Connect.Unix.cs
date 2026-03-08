// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class ConnectAsyncBlockingModeTests
    {
        private static bool IsSocketNonBlocking(Socket socket)
        {
            int rv = Interop.Sys.Fcntl.GetIsNonBlocking(socket.SafeHandle, out bool isNonBlocking);
            Assert.NotEqual(-1, rv);
            return isNonBlocking;
        }

        [Fact]
        public async Task ConnectAsync_Success_SocketIsBlockingAfterCompletion()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Assert.True(client.Blocking);
            Assert.False(IsSocketNonBlocking(client));

            await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);

            Assert.True(client.Blocking);
            Assert.False(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task ConnectAsync_UserSetNonBlocking_SocketStaysNonBlocking()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            client.Blocking = false;
            Assert.False(client.Blocking);
            Assert.True(IsSocketNonBlocking(client));

            await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);

            Assert.False(client.Blocking);
            Assert.True(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task ConnectAsync_ThenSendAsync_SocketBecomesNonBlocking()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);

            Assert.True(client.Blocking);
            Assert.False(IsSocketNonBlocking(client));

            using Socket accepted = listener.Accept();

            await client.SendAsync(new byte[] { 1, 2, 3 }, SocketFlags.None);

            Assert.True(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task ConnectAsync_ThenReceiveAsync_SocketBecomesNonBlocking()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);

            Assert.True(client.Blocking);
            Assert.False(IsSocketNonBlocking(client));

            using Socket accepted = listener.Accept();
            accepted.Send(new byte[] { 1, 2, 3 });

            byte[] buffer = new byte[10];
            await client.ReceiveAsync(buffer, SocketFlags.None);

            Assert.True(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task ConnectAsync_Failure_SocketIsRestoredToBlocking()
        {
            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            await Assert.ThrowsAsync<SocketException>(async () =>
                await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 1)));

            Assert.False(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task AcceptAsync_AcceptedSocketIsBlockingByDefault()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect((IPEndPoint)listener.LocalEndPoint!);

            using Socket accepted = await listener.AcceptAsync();

            Assert.True(accepted.Blocking);
            Assert.False(IsSocketNonBlocking(accepted));
        }

        [Fact]
        public async Task AcceptAsync_AcceptedSocketSyncReceiveWorks()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect((IPEndPoint)listener.LocalEndPoint!);

            using Socket accepted = await listener.AcceptAsync();

            client.Send(new byte[] { 1, 2, 3 });

            byte[] buffer = new byte[10];
            int received = accepted.Receive(buffer);

            Assert.Equal(3, received);
            Assert.True(accepted.Blocking);
            Assert.False(IsSocketNonBlocking(accepted));
        }

        [Fact]
        public async Task AcceptAsync_ConcurrentAccepts_DoNotCorruptListenerState()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(5);

            Task<Socket> accept1 = listener.AcceptAsync();
            Task<Socket> accept2 = listener.AcceptAsync();

            using Socket client1 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            using Socket client2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client1.Connect((IPEndPoint)listener.LocalEndPoint!);
            client2.Connect((IPEndPoint)listener.LocalEndPoint!);

            using Socket accepted1 = await accept1;
            using Socket accepted2 = await accept2;

            Assert.True(accepted1.Blocking);
            Assert.False(IsSocketNonBlocking(accepted1));
            Assert.True(accepted2.Blocking);
            Assert.False(IsSocketNonBlocking(accepted2));
        }

        [Fact]
        public async Task ConnectAsync_WithBuffer_SocketStaysNonBlocking()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            var saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = (IPEndPoint)listener.LocalEndPoint!;
            saea.SetBuffer(new byte[] { 1, 2, 3 }, 0, 3);

            var tcs = new TaskCompletionSource();
            saea.Completed += (_, _) => tcs.SetResult();

            if (!client.ConnectAsync(saea))
            {
                tcs.SetResult();
            }

            await tcs.Task;

            Assert.Equal(SocketError.Success, saea.SocketError);

            // When buffer > 0, the socket stays non-blocking because SendToAsync
            // may have been used to send the initial data after connect.
            Assert.True(IsSocketNonBlocking(client));

            saea.Dispose();
        }
    }
}

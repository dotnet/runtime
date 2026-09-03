// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class SocketBlockingModeTransitionTests
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
        public async Task ConnectAsync_WithBuffer_Failure_CallbackInvokedAndSocketIsBlocking()
        {
            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            using var saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = new IPEndPoint(IPAddress.Loopback, 1);
            saea.SetBuffer(new byte[] { 1, 2, 3 }, 0, 3);

            var tcs = new TaskCompletionSource();
            saea.Completed += (_, _) => tcs.SetResult();

            if (!client.ConnectAsync(saea))
            {
                tcs.SetResult();
            }

            await tcs.Task;

            Assert.NotEqual(SocketError.Success, saea.SocketError);
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
        public async Task ConnectAsync_WithBuffer_Succeeds()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            using var saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = (IPEndPoint)listener.LocalEndPoint!;
            saea.SetBuffer(new byte[] { 1, 2, 3 }, 0, 3);

            var tcs = new TaskCompletionSource();
            saea.Completed += (_, _) => tcs.SetResult();

            bool completedAsync = client.ConnectAsync(saea);
            if (!completedAsync)
            {
                tcs.SetResult();
            }

            await tcs.Task;

            Assert.Equal(SocketError.Success, saea.SocketError);
            Assert.True(client.Blocking);

            // Native blocking mode is only restored once the entire connect, including any
            // buffered send, has fully completed -- regardless of platform, and regardless of
            // whether that completion happened synchronously or asynchronously.
            Assert.False(IsSocketNonBlocking(client));
        }

        [Fact]
        public async Task ConnectAsync_WithLargeBuffer_PendingSendCompletesBeforeBlockingIsRestored()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(1);

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Force a small send buffer so a multi-megabyte payload can't fit in a single non-blocking
            // send(), guaranteeing (rather than merely hoping) that the buffered send started as part
            // of ConnectAsync goes through the asynchronous (IOPending) completion path.
            client.SendBufferSize = 8 * 1024;

            byte[] data = new byte[4 * 1024 * 1024];

            using var saea = new SocketAsyncEventArgs();
            saea.RemoteEndPoint = (IPEndPoint)listener.LocalEndPoint!;
            saea.SetBuffer(data, 0, data.Length);

            var tcs = new TaskCompletionSource();
            saea.Completed += (_, _) => tcs.SetResult();

            bool completedAsync = client.ConnectAsync(saea);
            if (!completedAsync)
            {
                tcs.SetResult();
            }

            using Socket accepted = listener.Accept();
            accepted.ReceiveBufferSize = 8 * 1024;

            byte[] readBuffer = new byte[8 * 1024];
            int totalRead = 0;
            while (totalRead < data.Length)
            {
                int n = accepted.Receive(readBuffer);
                Assert.NotEqual(0, n);
                totalRead += n;
            }

            // Sanity check: the small buffers configured above should have forced this send async.
            Assert.True(completedAsync);

            await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));

            Assert.Equal(SocketError.Success, saea.SocketError);
            Assert.True(client.Blocking);

            // Native blocking mode must not be restored until the pending buffered send has actually
            // completed -- restoring it prematurely (e.g. right after connect() succeeds, before the
            // follow-up send finishes) would fail intermittently depending on scheduling.
            Assert.False(IsSocketNonBlocking(client));
        }
    }
}

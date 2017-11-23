// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Sockets.Tests
{
    public partial class UnixDomainSocketTest
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // new UnixDomainSocketEndPoint should throw on Windows
        public void UnixDomainSocketEndPoint_Throws_OnWindows()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new UnixDomainSocketEndPoint("/path"));
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests ConnectAsyncUnixDomainSocketEndPoint success on Unix
        public async Task Socket_ConnectAsyncUnixDomainSocketEndPoint_Success()
        {
            string path = null;
            SocketTestServer server = null;
            UnixDomainSocketEndPoint endPoint = null;

            for (int attempt = 0; attempt < 5; attempt++)
            {
                path = GetRandomNonExistingFilePath();
                endPoint = new UnixDomainSocketEndPoint(path);
                try
                {
                    server = SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, endPoint, ProtocolType.Unspecified);
                    break;
                }
                catch (SocketException)
                {
                    //Path selection is contingent on a successful Bind().
                    //If it fails, the next iteration will try another path.
                }
            }

            try
            {
                Assert.NotNull(server);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;
                args.Completed += (s, e) => ((TaskCompletionSource<bool>)e.UserToken).SetResult(true);

                var complete = new TaskCompletionSource<bool>();
                args.UserToken = complete;

                using (Socket sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    bool willRaiseEvent = sock.ConnectAsync(args);
                    if (willRaiseEvent)
                    {
                        await complete.Task;
                    }

                    Assert.Equal(SocketError.Success, args.SocketError);
                    Assert.Null(args.ConnectByNameError);
                }
            }
            finally
            {
                server.Dispose();

                try { File.Delete(path); }
                catch { }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests ConnectAsyncUnixDomainSocketEndPoint seccess on Unix
        public async Task Socket_ConnectAsyncUnixDomainSocketEndPoint_NotServer()
        {
            string path = GetRandomNonExistingFilePath();
            var endPoint = new UnixDomainSocketEndPoint(path);
            try
            {
                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;
                args.Completed += (s, e) => ((TaskCompletionSource<bool>)e.UserToken).SetResult(true);

                var complete = new TaskCompletionSource<bool>();
                args.UserToken = complete;

                using (Socket sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    bool willRaiseEvent = sock.ConnectAsync(args);
                    if (willRaiseEvent)
                    {
                        await complete.Task;
                    }

                    Assert.Equal(SocketError.AddressNotAvailable, args.SocketError);
                }
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests SendReceive success for UnixDomainSocketEndPoint on Unix
        public void Socket_SendReceive_Success()
        {
            string path = GetRandomNonExistingFilePath();
            var endPoint = new UnixDomainSocketEndPoint(path);
            try
            {
                using (var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                using (var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    server.Bind(endPoint);
                    server.Listen(1);

                    client.Connect(endPoint);
                    using (Socket accepted = server.Accept())
                    {
                        var data = new byte[1];
                        for (int i = 0; i < 10; i++)
                        {
                            data[0] = (byte)i;

                            accepted.Send(data);
                            data[0] = 0;

                            Assert.Equal(1, client.Receive(data));
                            Assert.Equal(i, data[0]);
                        }
                    }
                }
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests SendReceiveAsync success for UnixDomainSocketEndPoint on Unix
        public async Task Socket_SendReceiveAsync_Success()
        {
            string path = GetRandomNonExistingFilePath();
            var endPoint = new UnixDomainSocketEndPoint(path);
            try
            {
                using (var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                using (var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    server.Bind(endPoint);
                    server.Listen(1);

                    await client.ConnectAsync(endPoint);
                    using (Socket accepted = await server.AcceptAsync())
                    {
                        var data = new byte[1];
                        for (int i = 0; i < 10; i++)
                        {
                            data[0] = (byte)i;

                            await accepted.SendAsync(new ArraySegment<byte>(data), SocketFlags.None);
                            data[0] = 0;

                            Assert.Equal(1, await client.ReceiveAsync(new ArraySegment<byte>(data), SocketFlags.None));
                            Assert.Equal(i, data[0]);
                        }
                    }
                }
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(5000, 1, 1)]
        [InlineData(500, 18, 21)]
        [InlineData(500, 21, 18)]
        [InlineData(5, 128000, 64000)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests SendReceiveAsync success for UnixDomainSocketEndPoint on Unix
        public async Task Socket_SendReceiveAsync_PropagateToStream_Success(int iterations, int writeBufferSize, int readBufferSize)
        {             
            var writeBuffer = new byte[writeBufferSize * iterations];
            new Random().NextBytes(writeBuffer);
            var readData = new MemoryStream();

            string path = GetRandomNonExistingFilePath();
            var endPoint = new UnixDomainSocketEndPoint(path);
            try
            {
                using (var server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                using (var client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    server.Bind(endPoint);
                    server.Listen(1);

                    Task<Socket> serverAccept = server.AcceptAsync();
                    await Task.WhenAll(serverAccept, client.ConnectAsync(endPoint));

                    Task clientReceives = Task.Run(async () =>
                    {
                        int bytesRead;
                        byte[] buffer = new byte[readBufferSize];
                        while ((bytesRead = await client.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None)) > 0)
                        {
                            readData.Write(buffer, 0, bytesRead);
                        }
                    });

                    using (Socket accepted = await serverAccept)
                    {
                        for (int iter = 0; iter < iterations; iter++)
                        {
                            Task<int> sendTask = accepted.SendAsync(new ArraySegment<byte>(writeBuffer, iter * writeBufferSize, writeBufferSize), SocketFlags.None);
                            await await Task.WhenAny(clientReceives, sendTask);
                            Assert.Equal(writeBufferSize, await sendTask);
                        }
                    }

                    await clientReceives;
                }

                Assert.Equal(writeBuffer.Length, readData.Length);
                Assert.Equal(writeBuffer, readData.ToArray());
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests ConcurrentSendReceive success for UnixDomainSocketEndPoint on Unix
        public async Task ConcurrentSendReceive(bool forceNonBlocking)
        {
            using (Socket server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            using (Socket client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                const int Iters = 25;
                byte[] sendData = new byte[Iters];
                byte[] receiveData = new byte[sendData.Length];
                new Random().NextBytes(sendData);

                string path = GetRandomNonExistingFilePath();

                server.Bind(new UnixDomainSocketEndPoint(path));
                server.Listen(1);

                Task<Socket> acceptTask = server.AcceptAsync();
                client.Connect(new UnixDomainSocketEndPoint(path));
                await acceptTask;
                Socket accepted = acceptTask.Result;

                client.ForceNonBlocking(forceNonBlocking);
                accepted.ForceNonBlocking(forceNonBlocking);

                Task[] writes = new Task[Iters];
                Task<int>[] reads = new Task<int>[Iters];
                for (int i = 0; i < Iters; i++)
                {
                    reads[i] = Task.Factory.StartNew(s => accepted.Receive(receiveData, (int)s, 1, SocketFlags.None), i,
                        CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                for (int i = 0; i < Iters; i++)
                {
                    writes[i] = Task.Factory.StartNew(s => client.Send(sendData, (int)s, 1, SocketFlags.None), i,
                        CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
                }
                await TestSettings.WhenAllOrAnyFailedWithTimeout(writes.Concat(reads).ToArray());

                Assert.Equal(sendData.OrderBy(i => i), receiveData.OrderBy(i => i));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests ConcurrentSendReceive success for UnixDomainSocketEndPoint on Unix
        public async Task ConcurrentSendReceiveAsync()
        {
            using (Socket server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            using (Socket client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                const int Iters = 2048;
                byte[] sendData = new byte[Iters];
                byte[] receiveData = new byte[sendData.Length];
                new Random().NextBytes(sendData);

                string path = GetRandomNonExistingFilePath();

                server.Bind(new UnixDomainSocketEndPoint(path));
                server.Listen(1);

                Task<Socket> acceptTask = server.AcceptAsync();
                client.Connect(new UnixDomainSocketEndPoint(path));
                await acceptTask;
                Socket accepted = acceptTask.Result;

                Task[] writes = new Task[Iters];
                Task<int>[] reads = new Task<int>[Iters];
                for (int i = 0; i < Iters; i++)
                {
                    writes[i] = client.SendAsync(new ArraySegment<byte>(sendData, i, 1), SocketFlags.None);
                }
                for (int i = 0; i < Iters; i++)
                {
                    reads[i] = accepted.ReceiveAsync(new ArraySegment<byte>(receiveData, i, 1), SocketFlags.None);
                }

                await TestSettings.WhenAllOrAnyFailedWithTimeout(writes.Concat(reads).ToArray());

                Assert.Equal(sendData, receiveData);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests new UnixDomainSocketEndPoint throws the correct exception for invalid args
        public void UnixDomainSocketEndPoint_InvalidPaths_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new UnixDomainSocketEndPoint(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixDomainSocketEndPoint(string.Empty));

            int maxNativeSize = (int)typeof(UnixDomainSocketEndPoint)
                .GetField("s_nativePathLength", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

            string invalidLengthString = new string('a', maxNativeSize + 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => new UnixDomainSocketEndPoint(invalidLengthString));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [InlineData(false)]
        [InlineData(true)]
        public void UnixDomainSocketEndPoint_RemoteEndPointEqualsBindAddress(bool abstractAddress)
        {
            string serverAddress;
            string clientAddress;
            string expectedClientAddress;
            if (abstractAddress)
            {
                // abstract socket addresses are a Linux feature.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return;
                }
                // An abstract socket address starts with a zero byte.
                serverAddress = '\0' + Guid.NewGuid().ToString();
                clientAddress = '\0' + Guid.NewGuid().ToString();
                expectedClientAddress = '@' + clientAddress.Substring(1);
            }
            else
            {
                serverAddress = GetRandomNonExistingFilePath();
                clientAddress = GetRandomNonExistingFilePath();
                expectedClientAddress = clientAddress;
            }

            try
            {
                using (Socket server = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                {
                    server.Bind(new UnixDomainSocketEndPoint(serverAddress));
                    server.Listen(1);

                    using (Socket client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
                    {
                        // Bind the client.
                        client.Bind(new UnixDomainSocketEndPoint(clientAddress));
                        client.Connect(new UnixDomainSocketEndPoint(serverAddress));
                        using (Socket acceptedClient = server.Accept())
                        {
                            // Verify the client address on the server.
                            EndPoint clientAddressOnServer = acceptedClient.RemoteEndPoint;
                            Assert.True(string.CompareOrdinal(expectedClientAddress, clientAddressOnServer.ToString()) == 0);
                        }
                    }
                }
            }
            finally
            {
                if (!abstractAddress)
                {
                    try { File.Delete(serverAddress); }
                    catch { }
                    try { File.Delete(clientAddress); }
                    catch { }
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix & ~TestPlatforms.Linux)] // Don't support abstract socket addresses.
        public void UnixDomainSocketEndPoint_UsingAbstractSocketAddressOnUnsupported_Throws()
        {
            // An abstract socket address starts with a zero byte.
            string address = '\0' + Guid.NewGuid().ToString();

            // Bind
            using (Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                Assert.ThrowsAny<SocketException>(() => socket.Bind(new UnixDomainSocketEndPoint(address)));
            }

            // Connect
            using (Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            {
                Assert.ThrowsAny<SocketException>(() => socket.Connect(new UnixDomainSocketEndPoint(address)));
            }
        }

        private static string GetRandomNonExistingFilePath()
        {
            string result;
            do
            {
                result = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            }
            while (File.Exists(result));

            return result;
        }
    }
}

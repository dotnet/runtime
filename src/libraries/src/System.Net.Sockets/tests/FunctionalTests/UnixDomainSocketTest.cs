// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class UnixDomainSocketTest
    {
        private readonly ITestOutputHelper _log;

        public UnixDomainSocketTest(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // CreateUnixDomainSocket should throw on Windows
        public void Socket_CreateUnixDomainSocket_Throws_OnWindows()
        {
            SocketException e = Assert.Throws<SocketException>(() => new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified));
            Assert.Equal(SocketError.AddressFamilyNotSupported, e.SocketErrorCode);
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
        public void ConcurrentSendReceive(bool forceNonBlocking)
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
                acceptTask.Wait();
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
                Task.WaitAll(writes);
                Task.WaitAll(reads);

                Assert.Equal(sendData.OrderBy(i => i), receiveData.OrderBy(i => i));
            }
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Tests ConcurrentSendReceive success for UnixDomainSocketEndPoint on Unix
        public void ConcurrentSendReceiveAsync()
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
                acceptTask.Wait();
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
                Task.WaitAll(writes);
                Task.WaitAll(reads);

                Assert.Equal(sendData, receiveData);
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

        private sealed class UnixDomainSocketEndPoint : EndPoint
        {
            private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

            private static readonly Encoding s_pathEncoding = Encoding.UTF8;
            private static readonly int s_nativePathOffset = 2; // = offsetof(struct sockaddr_un, sun_path). It's the same on Linux and OSX
            private static readonly int s_nativePathLength = 91; // sockaddr_un.sun_path at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html, -1 for terminator
            private static readonly int s_nativeAddressSize = s_nativePathOffset + s_nativePathLength;

            private readonly string _path;
            private readonly byte[] _encodedPath;

            public UnixDomainSocketEndPoint(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                _path = path;
                _encodedPath = s_pathEncoding.GetBytes(_path);

                if (path.Length == 0 || _encodedPath.Length > s_nativePathLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(path));
                }
            }

            internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
            {
                if (socketAddress == null)
                {
                    throw new ArgumentNullException(nameof(socketAddress));
                }

                if (socketAddress.Family != EndPointAddressFamily ||
                    socketAddress.Size > s_nativeAddressSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(socketAddress));
                }

                if (socketAddress.Size > s_nativePathOffset)
                {
                    _encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
                    for (int i = 0; i < _encodedPath.Length; i++)
                    {
                        _encodedPath[i] = socketAddress[s_nativePathOffset + i];
                    }

                    _path = s_pathEncoding.GetString(_encodedPath, 0, _encodedPath.Length);
                }
                else
                {
                    _encodedPath = Array.Empty<byte>();
                    _path = string.Empty;
                }
            }

            public override SocketAddress Serialize()
            {
                var result = new SocketAddress(AddressFamily.Unix, s_nativeAddressSize);
                Debug.Assert(_encodedPath.Length + s_nativePathOffset <= result.Size, "Expected path to fit in address");

                for (int index = 0; index < _encodedPath.Length; index++)
                {
                    result[s_nativePathOffset + index] = _encodedPath[index];
                }
                result[s_nativePathOffset + _encodedPath.Length] = 0; // path must be null-terminated

                return result;
            }

            public override EndPoint Create(SocketAddress socketAddress) => new UnixDomainSocketEndPoint(socketAddress);

            public override AddressFamily AddressFamily => EndPointAddressFamily;

            public override string ToString() => _path;
        }
    }
}

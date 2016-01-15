// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Test.Common;
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

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_CreateUnixDomainSocket_Throws_OnWindows()
        {
            // Throws SocketException with this message "An address incompatible with the requested protocol was used"
            Assert.Throws<SocketException>(() => new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified));
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
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
                    server = SocketTestServer.SocketTestServerFactory(endPoint, ProtocolType.Unspecified);
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
                    Assert.True(sock.ConnectAsync(args));

                    await complete.Task;

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

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
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

                    Assert.Equal(SocketError.SocketError, args.SocketError);
                }
            }
            finally
            {
                try { File.Delete(path); }
                catch { }
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.AnyUnix)]
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

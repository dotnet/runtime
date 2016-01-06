// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading;

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

        private void OnConnectAsyncCompleted(object sender, SocketAsyncEventArgs args)
        {
            ManualResetEvent complete = (ManualResetEvent)args.UserToken;
            complete.Set();
        }

        [Fact]
        [PlatformSpecific(PlatformID.Windows)]
        public void Socket_CreateUnixDomainSocket_Throws_OnWindows()
        {
            // Throws SocketException with this message "An address incompatible with the requested protocol was used"
            Assert.Throws<SocketException>(() => new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified));
        }

        [Fact]
        [PlatformSpecific(PlatformID.Linux | PlatformID.OSX)]
        public void Socket_ConnectAsyncUnixDomainSocketEndPoint_Success()
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
                    // Path selection is contingent on a successful Bind(). 
                    // If it fails, the next iteration will try another path.
                }
            }

            try
            {
                Assert.NotNull(server);

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.RemoteEndPoint = endPoint;
                args.Completed += OnConnectAsyncCompleted;

                ManualResetEvent complete = new ManualResetEvent(false);
                args.UserToken = complete;

                Socket sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                Assert.True(sock.ConnectAsync(args));

                complete.WaitOne();

                Assert.Equal(SocketError.Success, args.SocketError);
                Assert.Null(args.ConnectByNameError);

                complete.Dispose();
                sock.Dispose();
                server.Dispose();
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        [PlatformSpecific(PlatformID.Linux | PlatformID.OSX)]
        public void Socket_ConnectAsyncUnixDomainSocketEndPoint_NotServer()
        {
            string path = GetRandomNonExistingFilePath();
            var endPoint = new UnixDomainSocketEndPoint(path);

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            args.RemoteEndPoint = endPoint;
            args.Completed += OnConnectAsyncCompleted;

            ManualResetEvent complete = new ManualResetEvent(false);
            args.UserToken = complete;

            Socket sock = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            bool willRaiseEvent = sock.ConnectAsync(args);
            if (willRaiseEvent)
            {
                complete.WaitOne();
            }

            Assert.Equal(SocketError.SocketError, args.SocketError);

            complete.Dispose();
            sock.Dispose();
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

        private class UnixDomainSocketEndPoint : EndPoint
        {
            private static readonly Encoding PathEncoding = Encoding.UTF8;

            private const int MaxPathLength = 92;   // sockaddr_un.sun_path at http://pubs.opengroup.org/onlinepubs/9699919799/basedefs/sys_un.h.html
            private const int PathOffset = 2;       // = offsetof(struct sockaddr_un, sun_path). It's the same on Linux and OSX
            private const int MaxSocketAddressSize = PathOffset + MaxPathLength;
            private const int MinSocketAddressSize = PathOffset + 2; // +1 for one character and +1 for \0 ending
            private const AddressFamily EndPointAddressFamily = AddressFamily.Unix;

            private readonly string _path;
            private readonly byte[] _encodedPath;

            public UnixDomainSocketEndPoint(string path)
            {
                if (path == null)
                {
                    throw new ArgumentNullException("path");
                }

                if (path.Length == 0 || PathEncoding.GetByteCount(path) >= MaxPathLength)
                {
                    throw new ArgumentOutOfRangeException("path");
                }
                
                _path = path;
                _encodedPath = PathEncoding.GetBytes(_path);
            }

            internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
            {
                if (socketAddress == null)
                {
                    throw new ArgumentNullException("socketAddress");
                }

                if (socketAddress.Family != EndPointAddressFamily || socketAddress.Size < MinSocketAddressSize || socketAddress.Size > MaxSocketAddressSize)
                {
                    throw new ArgumentException("socketAddress");
                }

                _encodedPath = new byte[socketAddress.Size - PathOffset];
                for (int index = 0; index < socketAddress.Size - PathOffset; index++)
                {
                    _encodedPath[index] = socketAddress[PathOffset + index];
                }

                _path = PathEncoding.GetString(_encodedPath);
            }

            public string Path
            {
                get
                {
                    return _path;
                }
            }

            public override AddressFamily AddressFamily
            {
                get
                {
                    return EndPointAddressFamily;
                }
            }

            public override SocketAddress Serialize()
            {
                SocketAddress result = new SocketAddress(AddressFamily.Unix, MaxSocketAddressSize);

                // Ctor has already checked that PathOffset + _encodedPath.Length < MaxSocketAddressSize
                for (int index = 0; index < _encodedPath.Length; index++)
                {
                    result[PathOffset + index] = _encodedPath[index];
                }

                // The path must be ending with \0
                result[PathOffset + _encodedPath.Length] = 0;

                return result;
            }

            public override EndPoint Create(SocketAddress socketAddress)
            {
                return new UnixDomainSocketEndPoint(socketAddress);
            }

            public override string ToString()
            {
                return Path;
            }
        }
    }
}

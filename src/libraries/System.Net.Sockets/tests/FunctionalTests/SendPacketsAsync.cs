// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public partial class SendPacketsAsync
    {
        private readonly ITestOutputHelper _log;

        private IPAddress _serverAddress = IPAddress.IPv6Loopback;
        // Accessible directories for UWP app:
        // C:\Users\<UserName>\AppData\Local\Packages\<ApplicationPackageName>\
        private string TestFileName = Environment.GetEnvironmentVariable("LocalAppData") + @"\NCLTest.Socket.SendPacketsAsync.testpayload";
        private static int s_testFileSize = 1024;

        #region Additional test attributes

        public SendPacketsAsync(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();

            byte[] buffer = new byte[s_testFileSize];

            for (int i = 0; i < s_testFileSize; i++)
            {
                buffer[i] = (byte)(i % 255);
            }

            try
            {
                _log.WriteLine("Creating file {0} with size: {1}", TestFileName, s_testFileSize);
                using (FileStream fs = new FileStream(TestFileName, FileMode.CreateNew))
                {
                    fs.Write(buffer, 0, buffer.Length);
                }
            }
            catch (IOException)
            {
                // Test payload file already exists.
                _log.WriteLine("Payload file exists: {0}", TestFileName);
            }
        }

        #endregion Additional test attributes


        #region Basic Arguments

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void Disposed_Throw(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    sock.Dispose();

                    Assert.Throws<ObjectDisposedException>(() =>
                    {
                        sock.SendPacketsAsync(new SocketAsyncEventArgs());
                    });
                }
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendPacketsAsync that dereferences null SAEA argument")]
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void NullArgs_Throw(SocketImplementationType type)
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    
                    AssertExtensions.Throws<ArgumentNullException>("e", () => sock.SendPacketsAsync(null));
                }
            }
        }

        [Fact]
        public void NotConnected_Throw()
        {
            Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            // Needs to be connected before send

            Assert.Throws<NotSupportedException>(() =>
            {
                socket.SendPacketsAsync(new SocketAsyncEventArgs { SendPacketsElements = new SendPacketsElement[0] });
            });
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Bug in SendPacketsAsync that dereferences null m_SendPacketsElementsInternal array")]
        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void NullList_Throws(SocketImplementationType type)
        {
            AssertExtensions.Throws<ArgumentNullException>("e.SendPacketsElements", () => SendPackets(type, (SendPacketsElement[])null, SocketError.Success, 0));
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void NullElement_Ignored(SocketImplementationType type)
        {
            SendPackets(type, (SendPacketsElement)null, 0);
        }

        [OuterLoop] // TODO: Issue #11345
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void EmptyList_Ignored(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement[0], SocketError.Success, 0);
        }

        [OuterLoop] // TODO: Issue #11345
        [Fact]
        public void SocketAsyncEventArgs_DefaultSendSize_0()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            Assert.Equal(0, args.SendPacketsSendSize);
        }

        #endregion Basic Arguments

        #region Buffers

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void NormalBuffer_Success(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[10]), 10);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void NormalBufferRange_Success(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[10], 5, 5), 5);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void EmptyBuffer_Ignored(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[0]), 0);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void BufferZeroCount_Ignored(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[10], 4, 0), 0);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void BufferMixedBuffers_ZeroCountBufferIgnored(SocketImplementationType type)
        {
            SendPacketsElement[] elements = new SendPacketsElement[]
            {
                new SendPacketsElement(new byte[10], 4, 0), // Ignored
                new SendPacketsElement(new byte[10], 4, 4),
                new SendPacketsElement(new byte[10], 0, 4)
            };
            SendPackets(type, elements, SocketError.Success, 8);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void BufferZeroCountThenNormal_ZeroCountIgnored(SocketImplementationType type)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    using (SocketAsyncEventArgs args = new SocketAsyncEventArgs())
                    {
                        args.Completed += OnCompleted;
                        args.UserToken = completed;

                        // First do an empty send, ignored
                        args.SendPacketsElements = new SendPacketsElement[]
                        {
                            new SendPacketsElement(new byte[5], 3, 0)
                        };

                        if (sock.SendPacketsAsync(args))
                        {
                            Assert.True(completed.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(SocketError.Success, args.SocketError);
                        Assert.Equal(0, args.BytesTransferred);

                        completed.Reset();
                        // Now do a real send
                        args.SendPacketsElements = new SendPacketsElement[]
                        {
                            new SendPacketsElement(new byte[5], 1, 4)
                        };

                        if (sock.SendPacketsAsync(args))
                        {
                            Assert.True(completed.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(SocketError.Success, args.SocketError);
                        Assert.Equal(4, args.BytesTransferred);
                    }
                }
            }
        }

        #endregion Buffers

        #region TransmitFileOptions
        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SocketDisconnected_TransmitFileOptionDisconnect(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[10], 4, 4), TransmitFileOptions.Disconnect, 4);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SocketDisconnectedAndReusable_TransmitFileOptionReuseSocket(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(new byte[10], 4, 4), TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket, 4);
        }
        #endregion

        #region Files

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_EmptyFileName_Throws(SocketImplementationType type)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                SendPackets(type, new SendPacketsElement(string.Empty), 0);
            });
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [PlatformSpecific(TestPlatforms.Windows)] // whitespace-only is a valid name on Unix
        public void SendPacketsElement_BlankFileName_Throws(SocketImplementationType type)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                // Existence is validated on send
                SendPackets(type, new SendPacketsElement("   "), 0);
            });
        }

        [Theory]
        [ActiveIssue(27269)]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [PlatformSpecific(TestPlatforms.Windows)] // valid filename chars on Unix
        public void SendPacketsElement_BadCharactersFileName_Throws(SocketImplementationType type)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                // Existence is validated on send
                SendPackets(type, new SendPacketsElement("blarkd@dfa?/sqersf"), 0);
            });
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_MissingDirectoryName_Throws(SocketImplementationType type)
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                // Existence is validated on send
                SendPackets(type, new SendPacketsElement(Path.Combine("nodir", "nofile")), 0);
            });
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_MissingFile_Throws(SocketImplementationType type)
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                // Existence is validated on send
                SendPackets(type, new SendPacketsElement("DoesntExit"), 0);
            });
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_File_Success(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(TestFileName), s_testFileSize); // Whole File
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileZeroCount_Success(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(TestFileName, 0, 0), s_testFileSize);  // Whole File
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FilePart_Success(SocketImplementationType type)
        {
            SendPackets(type, new SendPacketsElement(TestFileName, 10, 20), 20);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileMultiPart_Success(SocketImplementationType type)
        {
            var elements = new[]
            {
                new SendPacketsElement(TestFileName, 10, 20),
                new SendPacketsElement(TestFileName, 30, 10),
                new SendPacketsElement(TestFileName, 0, 10),
            };
            SendPackets(type, elements, SocketError.Success, 40);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileLargeOffset_Throws(SocketImplementationType type)
        {
            // Length is validated on Send
            SendPackets(type, new SendPacketsElement(TestFileName, 11000, 1), SocketError.InvalidArgument, 0);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        public void SendPacketsElement_FileLargeCount_Throws(SocketImplementationType type)
        {
            // Length is validated on Send
            SendPackets(type, new SendPacketsElement(TestFileName, 5, 10000), SocketError.InvalidArgument, 0);
        }

        [Theory]
        [InlineData(SocketImplementationType.APM)]
        [InlineData(SocketImplementationType.Async)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The fix was made in corefx that is not in netfx. See https://github.com/dotnet/corefx/pull/34331")]
        public void SendPacketsElement_FileStreamIsReleasedOnError(SocketImplementationType type)
        {
            // this test checks that FileStreams opened by the implementation of SendPacketsAsync
            // are properly disposed of when the SendPacketsAsync operation fails asynchronously.
            // To trigger this codepath we must call SendPacketsAsync with a wrong offset (to create an error), 
            // and twice (to avoid synchronous completion).

            SendPacketsElement[] goodElements = new[] { new SendPacketsElement(TestFileName, 0, 0) };
            SendPacketsElement[] badElements = new[] { new SendPacketsElement(TestFileName, 50_000, 10) };
            EventWaitHandle completed1 = new ManualResetEvent(false);
            EventWaitHandle completed2 = new ManualResetEvent(false);

            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out int port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    bool r1, r2;
                    using (SocketAsyncEventArgs args1 = new SocketAsyncEventArgs())
                    using (SocketAsyncEventArgs args2 = new SocketAsyncEventArgs())
                    {
                        args1.Completed += OnCompleted;
                        args1.UserToken = completed1;
                        args1.SendPacketsElements = goodElements;

                        args2.Completed += OnCompleted;
                        args2.UserToken = completed2;
                        args2.SendPacketsElements = badElements;

                        r1 = sock.SendPacketsAsync(args1);
                        r2 = sock.SendPacketsAsync(args2);

                        if (r1)
                        {
                            Assert.True(completed1.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(SocketError.Success, args1.SocketError);

                        if (r2)
                        {
                            Assert.True(completed2.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(SocketError.InvalidArgument, args2.SocketError);

                        using (var fs = new FileStream(TestFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
                        {
                            // If a SendPacketsAsync call did not dispose of its FileStreams, the FileStream ctor throws.
                        }
                    }
                }
            }
        }

        #endregion Files

        #region Helpers
        private void SendPackets(SocketImplementationType type, SendPacketsElement element, TransmitFileOptions flags, int bytesExpected)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    using (SocketAsyncEventArgs args = new SocketAsyncEventArgs())
                    {
                        args.Completed += OnCompleted;
                        args.UserToken = completed;
                        args.SendPacketsElements = new[] { element };
                        args.SendPacketsFlags = flags;

                        if (sock.SendPacketsAsync(args))
                        {
                            Assert.True(completed.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(SocketError.Success, args.SocketError);
                        Assert.Equal(bytesExpected, args.BytesTransferred);
                    }

                    switch (flags)
                    {
                        case TransmitFileOptions.Disconnect:
                            // Sending data again throws with socket shut down error.
                            Assert.Throws<SocketException>(() => { sock.Send(new byte[1] { 01 }); });
                            break;
                        case TransmitFileOptions.ReuseSocket & TransmitFileOptions.Disconnect:
                            // Able to send data again with reuse socket flag set.
                            Assert.Equal(1, sock.Send(new byte[1] { 01 }));
                            break;
                    }
                }
            }
        }

        private void SendPackets(SocketImplementationType type, SendPacketsElement element, int bytesExpected, byte[] contentExpected = null)
        {
            SendPackets(type, new[] {element}, SocketError.Success, bytesExpected, contentExpected);
        }

        private void SendPackets(SocketImplementationType type, SendPacketsElement element, SocketError expectedResult, int bytesExpected)
        {
            SendPackets(type, new[] {element}, expectedResult, bytesExpected);
        }

        private void SendPackets(SocketImplementationType type, SendPacketsElement[] elements, SocketError expectedResult, int bytesExpected, byte[] contentExpected = null)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(type, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    using (SocketAsyncEventArgs args = new SocketAsyncEventArgs())
                    {
                        args.Completed += OnCompleted;
                        args.UserToken = completed;
                        args.SendPacketsElements = elements;

                        if (sock.SendPacketsAsync(args))
                        {
                            Assert.True(completed.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }
                        Assert.Equal(expectedResult, args.SocketError);
                        Assert.Equal(bytesExpected, args.BytesTransferred);
                    
                    }

                    if (contentExpected != null) {
                        // test server just echos back, so read number of expected bytes from the stream
                        var contentActual = new byte[bytesExpected];
                        int bytesReceived = 0;
                        while (bytesReceived < bytesExpected) {
                            bytesReceived += sock.Receive(contentActual, bytesReceived, bytesExpected-bytesReceived, SocketFlags.None);
                        }
                        Assert.Equal(bytesExpected, bytesReceived);
                        Assert.Equal(contentExpected, contentActual);
                    }
                }
            }
        }

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            EventWaitHandle handle = (EventWaitHandle)e.UserToken;
            handle.Set();
        }

        #endregion Helpers
    }
}

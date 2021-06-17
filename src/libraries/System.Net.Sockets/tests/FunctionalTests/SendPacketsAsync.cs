// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO;
using System.Net.Test.Common;
using System.Threading;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class SendPacketsAsync
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

        [Fact]
        public void Disposed_Throw()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out port))
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

        [Fact]
        public void NullArgs_Throw()
        {
            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out port))
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

        [Fact]
        public void NullList_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("e", () => SendPackets((SendPacketsElement[])null, SocketError.Success, 0));
        }

        [Fact]
        public void NullElement_Ignored()
        {
            SendPackets((SendPacketsElement)null, 0);
        }

        [Fact]
        public void EmptyList_Ignored()
        {
            SendPackets(new SendPacketsElement[0], SocketError.Success, 0);
        }

        [Fact]
        public void SocketAsyncEventArgs_DefaultSendSize_0()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            Assert.Equal(0, args.SendPacketsSendSize);
        }

        #endregion Basic Arguments

        #region Buffers

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void NormalBuffer_Success(BufferType bufferType)
        {
            using var e = CreateElementForBuffer(bufferType, 10);
            SendPackets(e.Element, 10);
        }

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void NormalBufferRange_Success(BufferType bufferType)
        {
            using var e = CreateElementForBuffer(bufferType, 10, 5, 5);
            SendPackets(e.Element, 5);
        }

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void EmptyBuffer_Ignored(BufferType bufferType)
        {
            using var e = CreateElementForBuffer(bufferType, 0);
            SendPackets(e.Element, 0);
        }

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void BufferZeroCount_Ignored(BufferType bufferType)
        {
            using var e = CreateElementForBuffer(bufferType, 10, 4, 0);
            SendPackets(e.Element, 0);
        }

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void BufferMixedBuffers_ZeroCountBufferIgnored(BufferType bufferType)
        {
            using var e1 = CreateElementForBuffer(bufferType, 10, 4, 0);
            using var e2 = CreateElementForBuffer(bufferType, 10, 4, 4);
            using var e3 = CreateElementForBuffer(bufferType, 10, 0, 4);

            SendPacketsElement[] elements = new SendPacketsElement[] { e1.Element, e2.Element, e3.Element };
            SendPackets(elements, SocketError.Success, 8);
        }

        [Theory]
        [InlineData(BufferType.ByteArray)]
        [InlineData(BufferType.ManagedMemory)]
        [InlineData(BufferType.NativeMemory)]
        public void BufferZeroCountThenNormal_ZeroCountIgnored(BufferType bufferType)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out port))
            {
                using (Socket sock = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp))
                {
                    sock.Connect(new IPEndPoint(_serverAddress, port));
                    using (SocketAsyncEventArgs args = new SocketAsyncEventArgs())
                    {
                        args.Completed += OnCompleted;
                        args.UserToken = completed;

                        // First do an empty send, ignored
                        using var e1 = CreateElementForBuffer(bufferType, 5, 3, 0);
                        args.SendPacketsElements = new SendPacketsElement[] { e1.Element };
                        if (sock.SendPacketsAsync(args))
                        {
                            Assert.True(completed.WaitOne(TestSettings.PassingTestTimeout), "Timed out");
                        }

                        Assert.Equal(SocketError.Success, args.SocketError);
                        Assert.Equal(0, args.BytesTransferred);

                        completed.Reset();

                        // Now do a real send
                        using var e2 = CreateElementForBuffer(bufferType, 5, 1, 4);
                        args.SendPacketsElements = new SendPacketsElement[] { e2.Element };
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

        [Fact]
        public void SocketDisconnected_TransmitFileOptionDisconnect()
        {
            SendPackets(new SendPacketsElement(new byte[10], 4, 4), TransmitFileOptions.Disconnect, 4);
        }

        [Fact]
        public void SocketDisconnectedAndReusable_TransmitFileOptionReuseSocket()
        {
            SendPackets(new SendPacketsElement(new byte[10], 4, 4), TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket, 4);
        }
        #endregion

        #region Files

        [Fact]
        public void SendPacketsElement_EmptyFileName_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                SendPackets(new SendPacketsElement(string.Empty), 0);
            });
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // whitespace-only is a valid name on Unix
        public void SendPacketsElement_BlankFileName_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                // Existence is validated on send
                SendPackets(new SendPacketsElement("   "), 0);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/25099")]
        [PlatformSpecific(TestPlatforms.Windows)] // valid filename chars on Unix
        public void SendPacketsElement_BadCharactersFileName_Throws()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () =>
            {
                // Existence is validated on send
                SendPackets(new SendPacketsElement("blarkd@dfa?/sqersf"), 0);
            });
        }

        [Fact]
        public void SendPacketsElement_MissingDirectoryName_Throws()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                // Existence is validated on send
                SendPackets(new SendPacketsElement(Path.Combine("nodir", "nofile")), 0);
            });
        }

        [Fact]
        public void SendPacketsElement_MissingFile_Throws()
        {
            Assert.Throws<FileNotFoundException>(() =>
            {
                // Existence is validated on send
                SendPackets(new SendPacketsElement("DoesntExit"), 0);
            });
        }

        [Fact]
        public void SendPacketsElement_File_Success()
        {
            SendPackets(new SendPacketsElement(TestFileName), s_testFileSize); // Whole File
        }

        [Fact]
        public void SendPacketsElement_FileZeroCount_Success()
        {
            SendPackets(new SendPacketsElement(TestFileName, 0, 0), s_testFileSize);  // Whole File
        }

        [Fact]
        public void SendPacketsElement_FilePart_Success()
        {
            SendPackets(new SendPacketsElement(TestFileName, 10, 20), 20);
        }

        [Fact]
        public void SendPacketsElement_FileMultiPart_Success()
        {
            var elements = new[]
            {
                new SendPacketsElement(TestFileName, 10, 20),
                new SendPacketsElement(TestFileName, 30, 10),
                new SendPacketsElement(TestFileName, 0, 10),
            };
            SendPackets(elements, SocketError.Success, 40);
        }

        [Fact]
        public void SendPacketsElement_FileLargeOffset_Throws()
        {
            // Length is validated on Send
            SendPackets(new SendPacketsElement(TestFileName, 11000, 1), SocketError.InvalidArgument, 0);
        }

        [Fact]
        public void SendPacketsElement_FileLargeCount_Throws()
        {
            // Length is validated on Send
            SendPackets(new SendPacketsElement(TestFileName, 5, 10000), SocketError.InvalidArgument, 0);
        }

        [Fact]
        public void SendPacketsElement_FileStreamIsReleasedOnError()
        {
            // this test checks that FileStreams opened by the implementation of SendPacketsAsync
            // are properly disposed of when the SendPacketsAsync operation fails asynchronously.
            // To trigger this codepath we must call SendPacketsAsync with a wrong offset (to create an error),
            // and twice (to avoid synchronous completion).

            SendPacketsElement[] goodElements = new[] { new SendPacketsElement(TestFileName, 0, 0) };
            SendPacketsElement[] badElements = new[] { new SendPacketsElement(TestFileName, 50_000, 10) };
            EventWaitHandle completed1 = new ManualResetEvent(false);
            EventWaitHandle completed2 = new ManualResetEvent(false);

            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out int port))
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

        [Fact]
        public void SendPacketsElement_FileZeroCount_OffsetLong_Success()
        {
            var element = new SendPacketsElement(TestFileName, 0L, 0);
            SendPackets(element, s_testFileSize, GetExpectedContent(element));  // Whole File
        }

        [Fact]
        public void SendPacketsElement_FilePart_OffsetLong_Success()
        {
            var element = new SendPacketsElement(TestFileName, 10L, 20);
            SendPackets(element, 20, GetExpectedContent(element));
        }

        [Fact]
        public void SendPacketsElement_FileMultiPart_OffsetLong_Success()
        {
            var elements = new[]
            {
                new SendPacketsElement(TestFileName, 10L, 20),
                new SendPacketsElement(TestFileName, 30L, 10),
                new SendPacketsElement(TestFileName, 0L, 10),
            };
            SendPackets(elements, SocketError.Success, 40, GetExpectedContent(elements));
        }

        [Fact]
        public void SendPacketsElement_FileLargeOffset_OffsetLong_Throws()
        {
            // Length is validated on Send
            SendPackets(new SendPacketsElement(TestFileName, (long)uint.MaxValue + 11000, 1), SocketError.InvalidArgument, 0);
        }

        [Fact]
        public void SendPacketsElement_FileLargeCount_OffsetLong_Throws()
        {
            // Length is validated on Send
            SendPackets(new SendPacketsElement(TestFileName, 5L, 10000), SocketError.InvalidArgument, 0);
        }

        #endregion Files

        #region FileStreams

        [Fact]
        public void SendPacketsElement_FileStream_Success()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(new SendPacketsElement(stream), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(new SendPacketsElement(stream), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamZeroCount_Success()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(new SendPacketsElement(stream, 0, 0), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(new SendPacketsElement(stream, 0, 0), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamSizeCount_Success()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                SendPackets(new SendPacketsElement(stream, 0, s_testFileSize), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);

                SendPackets(new SendPacketsElement(stream, 0, s_testFileSize), s_testFileSize); // Whole File
                Assert.Equal(s_testFileSize / 2, stream.Position);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamPart_Success()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize - 10, SeekOrigin.Begin);
                SendPackets(new SendPacketsElement(stream, 0, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(new SendPacketsElement(stream, 10, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(new SendPacketsElement(stream, s_testFileSize - 20, 20), 20);
                Assert.Equal(s_testFileSize - 10, stream.Position);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamMultiPart_Success()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                var elements = new[]
                {
                    new SendPacketsElement(stream, 0, 20),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(stream, 0, 10),
                    new SendPacketsElement(stream, 10, 20),
                    new SendPacketsElement(stream, 30, 10),
                };
                stream.Seek(s_testFileSize - 10, SeekOrigin.Begin);
                SendPackets(elements, SocketError.Success, 70, GetExpectedContent(elements));
                Assert.Equal(s_testFileSize - 10, stream.Position);

                SendPackets(elements, SocketError.Success, 70, GetExpectedContent(elements));
                Assert.Equal(s_testFileSize - 10, stream.Position);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamLargeOffset_Throws()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                // Length is validated on Send
                SendPackets(new SendPacketsElement(stream, (long)uint.MaxValue + 11000, 1), SocketError.InvalidArgument, 0);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamLargeCount_Throws()
        {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true))
            {
                stream.Seek(s_testFileSize / 2, SeekOrigin.Begin);
                // Length is validated on Send
                SendPackets(new SendPacketsElement(stream, 5, 10000), SocketError.InvalidArgument,
                    0);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamWithOptions_Success() {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan)) {
                var element = new SendPacketsElement(stream, 0, s_testFileSize);
                SendPackets(element, s_testFileSize, GetExpectedContent(element));
            }
        }

        #endregion FileStreams

        #region Mixed Buffer, FilePath, FileStream tests

        [Fact]
        public void SendPacketsElement_FileStreamMultiPartMixed_Success() {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(new byte[] { 5, 6, 7 }, 0, 3),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[] { 11, 12, 13 }, 0, 3)),
                    new SendPacketsElement(stream, 10L, 20),
                    new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[] { 14, 15, 16 })),
                    new SendPacketsElement(TestFileName, 30, 10),
                    new SendPacketsElement(new byte[] { 8, 9, 10 }, 0, 3),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(elements, SocketError.Success, expected.Length, expected);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamMultiPartMixed_MultipleFileStreams_Success() {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            using (var stream2 = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[] { 11, 12, 13 }, 0, 3)),
                    new SendPacketsElement(new byte[] { 5, 6, 7 }, 0, 0),
                    new SendPacketsElement(stream, s_testFileSize - 10, 10),
                    new SendPacketsElement(stream2, s_testFileSize - 100, 10),
                    new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[] { 14, 15, 16 })),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(new byte[] { 8, 9, 10 }, 0, 1),
                    new SendPacketsElement(TestFileName, 30, 10),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(elements, SocketError.Success, expected.Length, expected);
            }
        }

        [Fact]
        public void SendPacketsElement_FileStreamMultiPartMixed_MultipleWholeFiles_Success() {
            using (var stream = new FileStream(TestFileName, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous)) {
                var elements = new[]
                {
                    new SendPacketsElement(stream, 0L, 0),
                    new SendPacketsElement(TestFileName, 0L, 10),
                    new SendPacketsElement(stream, 0L, 0),
                    new SendPacketsElement(TestFileName, 0L, 10),
                };
                byte[] expected = GetExpectedContent(elements);
                SendPackets(elements, SocketError.Success, expected.Length, expected);
            }
        }

        #endregion
        
        #region Helpers

        private void SendPackets(SendPacketsElement element, TransmitFileOptions flags, int bytesExpected)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out port))
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

        private void SendPackets(SendPacketsElement element, int bytesExpected, byte[] contentExpected = null)
        {
            SendPackets(new[] { element }, SocketError.Success, bytesExpected, contentExpected);
        }

        private void SendPackets(SendPacketsElement element, SocketError expectedResult, int bytesExpected)
        {
            SendPackets(new[] { element }, expectedResult, bytesExpected);
        }

        private void SendPackets(SendPacketsElement[] elements, SocketError expectedResult, int bytesExpected, byte[] contentExpected = null)
        {
            Assert.True(Capability.IPv6Support());

            EventWaitHandle completed = new ManualResetEvent(false);

            int port;
            using (SocketTestServer.SocketTestServerFactory(SocketImplementationType.Async, _serverAddress, out port))
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
                        AssertExtensions.SequenceEqual(contentExpected, contentActual);
                    }
                }
            }
        }

        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            EventWaitHandle handle = (EventWaitHandle)e.UserToken;
            handle.Set();
        }

        /// <summary>
        /// Recreate what SendPacketsAsync should send given the <paramref name="elements"/>,
        /// directly by collating their buffers or reading from their files.
        /// </summary>
        /// <param name="elements"></param>
        /// <returns></returns>
        private byte[] GetExpectedContent(params SendPacketsElement[] elements) {

            void ReadFromFile(string filePath, long offset, long count, byte[] destination, ref long destinationOffset) {
                using (FileStream fs = new FileStream(filePath, FileMode.Open,FileAccess.Read, FileShare.Read)) {
                    // Passing a zero count to SendPacketsElement means it sends the whole file.
                    if (count == 0) {
                        count = fs.Length;
                    }
                    fs.Position = offset;
                    int actualRead = 0;
                    do {
                        actualRead += fs.Read(destination, (int) destinationOffset + actualRead, (int) count - actualRead);
                    } while (actualRead != count && fs.Position < fs.Length);
                    destinationOffset += actualRead;
                }
            }

            int FileCount(SendPacketsElement element) {
                if (element.Count != 0) return element.Count;
                if (element.FilePath != null) {
                    return (int) new FileInfo(element.FilePath).Length;
                }
                else if (element.FileStream != null) {
                    return (int) element.FileStream.Length;
                }
                throw new ArgumentException("Expected SendPacketsElement with FilePath or FileStream set.", nameof(element));
            }

            int totalCount = 0;
            foreach (var element in elements) {
                totalCount += element.Buffer != null ? element.Count : FileCount(element);
            }
            var result = new byte[totalCount];
            long resultOffset = 0L;
            foreach (var spe in elements) {
                if (spe.FilePath != null) {
                    ReadFromFile(spe.FilePath, spe.OffsetLong, spe.Count, result, ref resultOffset);
                }
                else if (spe.FileStream != null) {
                    ReadFromFile(spe.FileStream.Name, spe.OffsetLong, spe.Count, result, ref resultOffset);
                }
                else if (spe.MemoryBuffer != null && spe.Count > 0) {
                    spe.MemoryBuffer.Value.CopyTo(result.AsMemory((int)resultOffset));
                    resultOffset += spe.Count;
                }
            }

            Assert.Equal(totalCount, resultOffset);
            return result;
        }

        public enum BufferType
        {
            ByteArray,
            ManagedMemory,
            NativeMemory
        }

        private struct ElementWithMemoryManager : IDisposable
        {
            public ElementWithMemoryManager(SendPacketsElement element, MemoryManager<byte> memoryManager)
            {
                Element = element;
                MemoryManager = memoryManager;
            }

            public SendPacketsElement Element { get; init; }
            public MemoryManager<byte> MemoryManager { get; init; }

            public void Dispose() => ((IDisposable)MemoryManager)?.Dispose();

        }

        private static ElementWithMemoryManager CreateElementForNativeBuffer(int size, int offset, int count)
        {
            MemoryManager<byte> memoryManager = new NativeMemoryManager(size);
            return new ElementWithMemoryManager(new SendPacketsElement(memoryManager.Memory.Slice(offset, count)), memoryManager);
        }

        private static ElementWithMemoryManager CreateElementForBuffer(BufferType bufferType, int size) =>
            bufferType switch
            {
                BufferType.ByteArray => new ElementWithMemoryManager(new SendPacketsElement(new byte[size]), null),
                BufferType.ManagedMemory => new ElementWithMemoryManager(new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[size])), null),
                BufferType.NativeMemory => CreateElementForNativeBuffer(size, 0, size),
                _ => throw new InvalidOperationException()
            };

        private static ElementWithMemoryManager CreateElementForBuffer(BufferType bufferType, int size, int offset, int count) =>
            bufferType switch
            {
                BufferType.ByteArray => new ElementWithMemoryManager(new SendPacketsElement(new byte[size], offset, count), null),
                BufferType.ManagedMemory => new ElementWithMemoryManager(new SendPacketsElement(new ReadOnlyMemory<byte>(new byte[size], offset, count)), null),
                BufferType.NativeMemory => CreateElementForNativeBuffer(size, offset, count),
                _ => throw new InvalidOperationException()
            };

        #endregion Helpers
    }
}

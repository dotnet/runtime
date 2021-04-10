// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.IO.Tests;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace System.Text.Tests
{
    public class TranscodingStreamTests : ConnectedStreamConformanceTests
    {
        protected override bool FlushRequiredToWriteData => true;
        protected override bool FlushGuaranteesAllDataWritten => false;
        protected override bool BlocksOnZeroByteReads => true;

        protected override Task<StreamPair> CreateConnectedStreamsAsync()
        {
            (Stream stream1, Stream stream2) = ConnectedStreams.CreateBidirectional();
            return Task.FromResult<StreamPair>(
                (Encoding.CreateTranscodingStream(stream1, new IdentityEncoding(), new IdentityEncoding()),
                 Encoding.CreateTranscodingStream(stream2, new IdentityEncoding(), new IdentityEncoding())));
        }

        public static IEnumerable<object[]> ReadWriteTestBufferLengths
        {
            get
            {
                yield return new object[] { 1 };
                yield return new object[] { 4 * 1024 };
                yield return new object[] { 128 * 1024 };
                yield return new object[] { 2 * 1024 * 1024 };
            }
        }

        [Fact]
        public void AsyncMethods_ReturnCanceledTaskIfCancellationTokenTripped()
        {
            // Arrange

            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken expectedCancellationToken = cts.Token;
            cts.Cancel();

            var innerStreamMock = new Mock<Stream>(MockBehavior.Strict); // only CanRead/CanWrite should ever be invoked
            innerStreamMock.Setup(o => o.CanRead).Returns(true);
            innerStreamMock.Setup(o => o.CanWrite).Returns(true);

            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStreamMock.Object, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            // Act & assert

            RunTest(() => transcodingStream.ReadAsync(new byte[0], 0, 0, expectedCancellationToken));
            RunTest(() => transcodingStream.ReadAsync(Memory<byte>.Empty, expectedCancellationToken).AsTask());
            RunTest(() => transcodingStream.WriteAsync(new byte[0], 0, 0, expectedCancellationToken));
            RunTest(() => transcodingStream.WriteAsync(ReadOnlyMemory<byte>.Empty, expectedCancellationToken).AsTask());

            void RunTest(Func<Task> callback)
            {
                Task task = callback();
                Assert.True(task.IsCanceled);
                Assert.Equal(expectedCancellationToken, Assert.Throws<TaskCanceledException>(() => task.GetAwaiter().GetResult()).CancellationToken);
            }
        }

        [Fact]
        public void CreateTranscodingStream_InvalidArgs()
        {
            Assert.Throws<ArgumentNullException>("innerStream", () => Encoding.CreateTranscodingStream(null, Encoding.UTF8, Encoding.UTF8));
            Assert.Throws<ArgumentNullException>("innerStreamEncoding", () => Encoding.CreateTranscodingStream(Stream.Null, null, Encoding.UTF8));
            Assert.Throws<ArgumentNullException>("outerStreamEncoding", () => Encoding.CreateTranscodingStream(Stream.Null, Encoding.UTF8, null));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanRead_DelegatesToInnerStream(bool expectedCanRead)
        {
            // Arrange

            var innerStreamMock = new Mock<Stream>();
            innerStreamMock.Setup(o => o.CanRead).Returns(expectedCanRead);
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStreamMock.Object, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            // Act

            bool actualCanReadBeforeDispose = transcodingStream.CanRead;
            transcodingStream.Dispose();
            bool actualCanReadAfterDispose = transcodingStream.CanRead;

            // Assert

            Assert.Equal(expectedCanRead, actualCanReadBeforeDispose);
            Assert.False(actualCanReadAfterDispose);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanWrite_DelegatesToInnerStream(bool expectedCanWrite)
        {
            // Arrange

            var innerStreamMock = new Mock<Stream>();
            innerStreamMock.Setup(o => o.CanWrite).Returns(expectedCanWrite);
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStreamMock.Object, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            // Act

            bool actualCanWriteBeforeDispose = transcodingStream.CanWrite;
            transcodingStream.Dispose();
            bool actualCanWriteAfterDispose = transcodingStream.CanWrite;

            // Assert

            Assert.Equal(expectedCanWrite, actualCanWriteBeforeDispose);
            Assert.False(actualCanWriteAfterDispose);
        }

        [Fact]
        public void Dispose_MakesMostSubsequentOperationsThrow()
        {
            // Arrange

            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            // Act

            transcodingStream.Dispose();

            // Assert
            // For Task/ValueTask-returning methods, we want the exception to be thrown synchronously.

            Assert.False(transcodingStream.CanRead);
            Assert.False(transcodingStream.CanSeek);
            Assert.False(transcodingStream.CanWrite);

            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.BeginRead(new byte[0], 0, 0, null, null));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.BeginWrite(new byte[0], 0, 0, null, null));
            Assert.Throws<ObjectDisposedException>(() => transcodingStream.Flush());
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.FlushAsync());
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.Read(new byte[0], 0, 0));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.Read(Span<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.ReadAsync(new byte[0], 0, 0));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.ReadAsync(Memory<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => transcodingStream.Write(new byte[0], 0, 0));
            Assert.Throws<ObjectDisposedException>(() => transcodingStream.Write(ReadOnlySpan<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.WriteAsync(new byte[0], 0, 0));
            Assert.Throws<ObjectDisposedException>(() => (object)transcodingStream.WriteAsync(ReadOnlyMemory<byte>.Empty));
            Assert.Throws<ObjectDisposedException>(() => transcodingStream.WriteByte((byte)'x'));
        }

        [Fact]
        public void Dispose_WithLeaveOpenFalse_DisposesInnerStream()
        {
            // Sync

            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8, leaveOpen: false);
            transcodingStream.Dispose();
            transcodingStream.Dispose(); // calling it a second time should no-op
            Assert.Throws<ObjectDisposedException>(() => innerStream.Read(Span<byte>.Empty));

            // Async

            innerStream = new MemoryStream();
            transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8, leaveOpen: false);
            transcodingStream.DisposeAsync().GetAwaiter().GetResult();
            transcodingStream.DisposeAsync().GetAwaiter().GetResult(); // calling it a second time should no-op
            Assert.Throws<ObjectDisposedException>(() => innerStream.Read(Span<byte>.Empty));
        }

        [Fact]
        public void Dispose_WithLeaveOpenTrue_DoesNotDisposeInnerStream()
        {
            // Sync

            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);
            transcodingStream.Dispose();
            transcodingStream.Dispose(); // calling it a second time should no-op
            innerStream.Read(Span<byte>.Empty); // shouldn't throw

            // Async

            innerStream = new MemoryStream();
            transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);
            transcodingStream.DisposeAsync().GetAwaiter().GetResult();
            transcodingStream.DisposeAsync().GetAwaiter().GetResult(); // calling it a second time should no-op
            innerStream.Read(Span<byte>.Empty); // shouldn't throw
        }

        [Fact]
        public void Flush_FlushesInnerStreamButNotDecodedState()
        {
            // Arrange

            CancellationToken expectedCancellationToken = new CancellationTokenSource().Token;
            Task expectedFlushAsyncTask = Task.FromResult("just some task");

            var innerStreamMock = new Mock<MemoryStream>() { CallBase = true };
            innerStreamMock.Setup(o => o.FlushAsync(expectedCancellationToken)).Returns(expectedFlushAsyncTask);
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStreamMock.Object, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            transcodingStream.Write(new byte[] { 0x7A, 0xE0 });
            innerStreamMock.Verify(o => o.Flush(), Times.Never);
            innerStreamMock.Verify(o => o.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Act & assert - sync flush

            transcodingStream.Flush();
            innerStreamMock.Verify(o => o.Flush(), Times.Once);
            innerStreamMock.Verify(o => o.FlushAsync(It.IsAny<CancellationToken>()), Times.Never);

            // Act & assert - async flush
            // This also validates that we flowed the CancellationToken as expected

            Task actualFlushAsyncReturnedTask = transcodingStream.FlushAsync(expectedCancellationToken);
            Assert.Same(expectedFlushAsyncTask, actualFlushAsyncReturnedTask);
            innerStreamMock.Verify(o => o.Flush(), Times.Once);
            innerStreamMock.Verify(o => o.FlushAsync(expectedCancellationToken), Times.Once);

            Assert.Equal("z", Encoding.UTF8.GetString(innerStreamMock.Object.ToArray())); // [ E0 ] shouldn't have been flushed
        }

        [Fact]
        public void IdenticalInnerAndOuterEncodings_DoesNotActAsPassthrough()
        {
            // Test read
            // [ C0 ] is never a valid UTF-8 byte, should be replaced with U+FFFD

            MemoryStream innerStream = new MemoryStream(new byte[] { 0xC0 });
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8);

            Assert.Equal(0xEF, transcodingStream.ReadByte());
            Assert.Equal(0xBF, transcodingStream.ReadByte());
            Assert.Equal(0xBD, transcodingStream.ReadByte());
            Assert.Equal(-1 /* eof */, transcodingStream.ReadByte());

            // Test write

            innerStream = new MemoryStream();
            transcodingStream = Encoding.CreateTranscodingStream(innerStream, Encoding.UTF8, Encoding.UTF8);
            transcodingStream.WriteByte(0xC0);
            Assert.Equal(new byte[] { 0xEF, 0xBF, 0xBD }, innerStream.ToArray());
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestBufferLengths))]
        public void Read_ByteArray(int bufferLength)
        {
            // Tests TranscodingStream.Read(byte[], int, int)

            byte[] buffer = new byte[bufferLength + 3];

            RunReadTest((transcodingStream, sink) =>
            {
                int numBytesRead = transcodingStream.Read(buffer, 1, bufferLength);
                Assert.True(numBytesRead >= 0);
                Assert.True(numBytesRead <= bufferLength);

                sink.Write(buffer, 1, numBytesRead);
                return numBytesRead;
            });
        }

        [Fact]
        public void Read_ByteArray_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => transcodingStream.Read(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Read(new byte[5], -1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Read(new byte[5], 3, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Read(new byte[5], 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Read(new byte[5], 6, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Read(new byte[5], 6, 0));
        }

        [Fact]
        public void Read_ByteByByte()
        {
            // Tests TranscodingStream.ReadByte

            RunReadTest((transcodingStream, sink) =>
            {
                int value = transcodingStream.ReadByte();
                if (value < 0)
                {
                    return 0;
                }

                sink.WriteByte(checked((byte)value));
                return 1;
            });
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestBufferLengths))]
        public void Read_Span(int bufferLength)
        {
            // Tests TranscodingStream.Read(Span<byte>)

            byte[] buffer = new byte[bufferLength];

            RunReadTest((transcodingStream, sink) =>
            {
                int numBytesRead = transcodingStream.Read(buffer.AsSpan());
                Assert.True(numBytesRead >= 0);
                Assert.True(numBytesRead <= bufferLength);

                sink.Write(buffer.AsSpan(0..numBytesRead));
                return numBytesRead;
            });
        }

        private void RunReadTest(Func<Stream, MemoryStream, int> callback)
        {
            MemoryStream sink = new MemoryStream();

            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStream,
                innerStreamEncoding: Encoding.UTF8,
                outerStreamEncoding: CustomAsciiEncoding);

            // Test with a small string, then test with a large string

            RunOneTestIteration(128);
            RunOneTestIteration(10 * 1024 * 1024);

            Assert.Equal(-1, transcodingStream.ReadByte()); // should've reached EOF

            // Now put some invalid data into the inner stream, followed by EOF, and ensure we get U+FFFD back out.

            innerStream.SetLength(0); // reset
            innerStream.WriteByte(0xC0); // [ C0 ] is never valid in UTF-8
            innerStream.Position = 0;

            sink.SetLength(0); // reset
            int numBytesReadJustNow;
            do
            {
                numBytesReadJustNow = callback(transcodingStream, sink);
                Assert.True(numBytesReadJustNow >= 0);
            } while (numBytesReadJustNow > 0);

            Assert.Equal("[FFFD]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            Assert.Equal(-1, transcodingStream.ReadByte()); // should've reached EOF

            // Now put some incomplete data into the inner stream, followed by EOF, and ensure we get U+FFFD back out.

            innerStream.SetLength(0); // reset
            innerStream.WriteByte(0xC2); // [ C2 ] must be followed by [ 80..BF ] in UTF-8
            innerStream.Position = 0;

            sink.SetLength(0); // reset
            do
            {
                numBytesReadJustNow = callback(transcodingStream, sink);
                Assert.True(numBytesReadJustNow >= 0);
            } while (numBytesReadJustNow > 0);

            Assert.Equal("[FFFD]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            Assert.Equal(-1, transcodingStream.ReadByte()); // should've reached EOF

            void RunOneTestIteration(int stringLength)
            {
                sink.SetLength(0); // reset

                string expectedStringContents = GetVeryLongAsciiString(stringLength);
                innerStream.SetLength(0); // reset
                innerStream.Write(Encoding.UTF8.GetBytes(expectedStringContents));
                innerStream.Position = 0;

                int numBytesReadJustNow;
                do
                {
                    numBytesReadJustNow = callback(transcodingStream, sink);
                    Assert.True(numBytesReadJustNow >= 0);
                } while (numBytesReadJustNow > 0);

                Assert.Equal(expectedStringContents, ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            }
        }

        [Fact]
        public Task ReadApm()
        {
            // Tests TranscodingStream.BeginRead / EndRead

            byte[] buffer = new byte[1024 * 1024];

            return RunReadTestAsync((transcodingStream, cancellationToken, sink) =>
            {
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                object expectedState = new object();

                try
                {
                    IAsyncResult asyncResult = transcodingStream.BeginRead(buffer, 1, buffer.Length - 2, (asyncResult) =>
                    {
                        try
                        {
                            int numBytesReadJustNow = transcodingStream.EndRead(asyncResult);
                            Assert.True(numBytesReadJustNow >= 0);
                            Assert.True(numBytesReadJustNow < buffer.Length - 3);
                            sink.Write(buffer, 1, numBytesReadJustNow);
                            tcs.SetResult(numBytesReadJustNow);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    }, expectedState);
                    Assert.Same(expectedState, asyncResult.AsyncState);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }

                return new ValueTask<int>(tcs.Task);
            },
            suppressExpectedCancellationTokenAsserts: true); // APM pattern doesn't allow flowing CancellationToken
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestBufferLengths))]
        public Task ReadAsync_ByteArray(int bufferLength)
        {
            // Tests TranscodingStream.ReadAsync(byte[], int, int, CancellationToken)

            byte[] buffer = new byte[bufferLength + 3];

            return RunReadTestAsync(async (transcodingStream, cancellationToken, sink) =>
            {
                int numBytesRead = await transcodingStream.ReadAsync(buffer, 1, bufferLength, cancellationToken);
                Assert.True(numBytesRead >= 0);
                Assert.True(numBytesRead <= bufferLength);

                sink.Write(buffer, 1, numBytesRead);
                return numBytesRead;
            });
        }

        [Theory]
        [MemberData(nameof(ReadWriteTestBufferLengths))]
        public async Task ReadAsync_Memory(int bufferLength)
        {
            // Tests TranscodingStream.ReadAsync(Memory<byte>, CancellationToken)

            byte[] buffer = new byte[bufferLength];

            await RunReadTestAsync(async (transcodingStream, cancellationToken, sink) =>
            {
                int numBytesRead = await transcodingStream.ReadAsync(buffer.AsMemory(), cancellationToken);
                Assert.True(numBytesRead >= 0);
                Assert.True(numBytesRead <= bufferLength);

                sink.Write(buffer.AsSpan(0..numBytesRead));
                return numBytesRead;
            });
        }

        [Fact]
        public async Task ReadAsync_LoopsWhenPartialDataReceived()
        {
            // Validates that the TranscodingStream will loop instead of returning 0
            // if the inner stream read partial data and GetBytes cannot make forward progress.

            using AsyncComms comms = new AsyncComms();
            Stream transcodingStream = Encoding.CreateTranscodingStream(comms.ReadStream, Encoding.UTF8, Encoding.UTF8);

            // First, ensure that writing [ C0 ] (always invalid UTF-8) to the stream
            // causes the reader to return immediately with fallback behavior.

            byte[] readBuffer = new byte[1024];
            comms.WriteBytes(new byte[] { 0xC0 });

            int numBytesRead = await transcodingStream.ReadAsync(readBuffer.AsMemory());
            Assert.Equal(new byte[] { 0xEF, 0xBF, 0xBD }, readBuffer[0..numBytesRead]); // fallback substitution

            // Next, ensure that writing [ C2 ] (partial UTF-8, needs more data) to the stream
            // causes the reader to asynchronously loop, returning "not yet complete".

            readBuffer = new byte[1024];
            comms.WriteBytes(new byte[] { 0xC2 });

            ValueTask<int> task = transcodingStream.ReadAsync(readBuffer.AsMemory());
            Assert.False(task.IsCompleted);
            comms.WriteBytes(new byte[] { 0x80 }); // [ C2 80 ] is valid UTF-8

            numBytesRead = await task; // should complete successfully
            Assert.Equal(new byte[] { 0xC2, 0x80 }, readBuffer[0..numBytesRead]);

            // Finally, ensure that writing [ C2 ] (partial UTF-8, needs more data) to the stream
            // followed by EOF causes the reader to perform substitution before returning EOF.

            readBuffer = new byte[1024];
            comms.WriteBytes(new byte[] { 0xC2 });

            task = transcodingStream.ReadAsync(readBuffer.AsMemory());
            Assert.False(task.IsCompleted);
            comms.WriteEof();

            numBytesRead = await task; // should complete successfully
            Assert.Equal(new byte[] { 0xEF, 0xBF, 0xBD }, readBuffer[0..numBytesRead]); // fallback substitution

            // Next call really should return "EOF reached"

            readBuffer = new byte[1024];
            Assert.Equal(0, await transcodingStream.ReadAsync(readBuffer.AsMemory()));
        }

        [Fact]
        public void ReadAsync_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => (object)transcodingStream.ReadAsync(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.ReadAsync(new byte[5], -1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.ReadAsync(new byte[5], 3, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.ReadAsync(new byte[5], 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.ReadAsync(new byte[5], 6, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.ReadAsync(new byte[5], 6, 0));
        }

        [Fact]
        public void ReadApm_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => transcodingStream.BeginRead(null, 0, 0, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginRead(new byte[5], -1, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginRead(new byte[5], 3, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginRead(new byte[5], 5, 1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginRead(new byte[5], 6, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginRead(new byte[5], 6, 0, null, null));
        }

        private async Task RunReadTestAsync(Func<Stream, CancellationToken, MemoryStream, ValueTask<int>> callback, bool suppressExpectedCancellationTokenAsserts = false)
        {
            CancellationToken expectedCancellationToken = new CancellationTokenSource().Token;
            MemoryStream sink = new MemoryStream();
            MemoryStream innerStream = new MemoryStream();

            var delegatingInnerStreamMock = new Mock<Stream>(MockBehavior.Strict);
            delegatingInnerStreamMock.Setup(o => o.CanRead).Returns(true);

            if (suppressExpectedCancellationTokenAsserts)
            {
                delegatingInnerStreamMock.Setup(o => o.ReadAsync(It.IsAny<Memory<byte>>(), It.IsAny<CancellationToken>()))
                    .Returns<Memory<byte>, CancellationToken>(innerStream.ReadAsync);
            }
            else
            {
                delegatingInnerStreamMock.Setup(o => o.ReadAsync(It.IsAny<Memory<byte>>(), expectedCancellationToken))
                    .Returns<Memory<byte>, CancellationToken>(innerStream.ReadAsync);
            }

            Stream transcodingStream = Encoding.CreateTranscodingStream(
                innerStream: delegatingInnerStreamMock.Object,
                innerStreamEncoding: Encoding.UTF8,
                outerStreamEncoding: CustomAsciiEncoding);

            // Test with a small string, then test with a large string

            await RunOneTestIteration(128);
            await RunOneTestIteration(10 * 1024 * 1024);

            Assert.Equal(-1, await transcodingStream.ReadByteAsync(expectedCancellationToken)); // should've reached EOF

            // Now put some invalid data into the inner stream, followed by EOF, and ensure we get U+FFFD back out.

            innerStream.SetLength(0); // reset
            innerStream.WriteByte(0xC0); // [ C0 ] is never valid in UTF-8
            innerStream.Position = 0;

            sink.SetLength(0); // reset
            int numBytesReadJustNow;
            do
            {
                numBytesReadJustNow = await callback(transcodingStream, expectedCancellationToken, sink);
                Assert.True(numBytesReadJustNow >= 0);
            } while (numBytesReadJustNow > 0);

            Assert.Equal("[FFFD]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            Assert.Equal(-1, await transcodingStream.ReadByteAsync(expectedCancellationToken)); // should've reached EOF

            // Now put some incomplete data into the inner stream, followed by EOF, and ensure we get U+FFFD back out.

            innerStream.SetLength(0); // reset
            innerStream.WriteByte(0xC2); // [ C2 ] must be followed by [ 80..BF ] in UTF-8
            innerStream.Position = 0;

            sink.SetLength(0); // reset
            do
            {
                numBytesReadJustNow = await callback(transcodingStream, expectedCancellationToken, sink);
                Assert.True(numBytesReadJustNow >= 0);
            } while (numBytesReadJustNow > 0);

            Assert.Equal("[FFFD]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            Assert.Equal(-1, await transcodingStream.ReadByteAsync(expectedCancellationToken)); // should've reached EOF

            async Task RunOneTestIteration(int stringLength)
            {
                sink.SetLength(0); // reset

                string expectedStringContents = GetVeryLongAsciiString(stringLength);
                innerStream.SetLength(0); // reset
                innerStream.Write(Encoding.UTF8.GetBytes(expectedStringContents));
                innerStream.Position = 0;

                int numBytesReadJustNow;
                do
                {
                    numBytesReadJustNow = await callback(transcodingStream, expectedCancellationToken, sink);
                    Assert.True(numBytesReadJustNow >= 0);
                } while (numBytesReadJustNow > 0);

                Assert.Equal(expectedStringContents, ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
            }
        }

        [Fact]
        public void ReadTimeout_WriteTimeout_NotSupported()
        {
            // Arrange - allow inner stream to support ReadTimeout + WriteTimeout

            var innerStreamMock = new Mock<Stream>();
            innerStreamMock.SetupProperty(o => o.ReadTimeout);
            innerStreamMock.SetupProperty(o => o.WriteTimeout);
            Stream transcodingStream = Encoding.CreateTranscodingStream(Stream.Null, Encoding.UTF8, Encoding.UTF8, leaveOpen: true);

            // Act & assert - TranscodingStream shouldn't support ReadTimeout + WriteTimeout

            Assert.False(transcodingStream.CanTimeout);
            Assert.Throws<InvalidOperationException>(() => transcodingStream.ReadTimeout);
            Assert.Throws<InvalidOperationException>(() => transcodingStream.ReadTimeout = 42);
            Assert.Throws<InvalidOperationException>(() => transcodingStream.WriteTimeout);
            Assert.Throws<InvalidOperationException>(() => transcodingStream.WriteTimeout = 42);
        }

        [Fact]
        public void Seek_AlwaysThrows()
        {
            // MemoryStream is seekable, but we're not
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.False(transcodingStream.CanSeek);
            Assert.Throws<NotSupportedException>(() => transcodingStream.Length);
            Assert.Throws<NotSupportedException>(() => transcodingStream.Position);
            Assert.Throws<NotSupportedException>(() => transcodingStream.Position = 0);
            Assert.Throws<NotSupportedException>(() => transcodingStream.Seek(0, SeekOrigin.Current));
            Assert.Throws<NotSupportedException>(() => transcodingStream.SetLength(0));
        }

        [Fact]
        public void Write()
        {
            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(
                innerStream,
                innerStreamEncoding: ErrorCheckingUnicodeEncoding /* throws on error */,
                outerStreamEncoding: Encoding.UTF8 /* performs substition */,
                leaveOpen: true);

            // First, test Write(byte[], int, int)

            transcodingStream.Write(Encoding.UTF8.GetBytes("abcdefg"), 2, 3);
            Assert.Equal("cde", ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            // Then test WriteByte(byte)

            transcodingStream.WriteByte((byte)'z');
            Assert.Equal("cdez", ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            // We'll write U+00E0 (utf-8: [C3 A0]) byte-by-byte.
            // We shouldn't flush any intermediate bytes.

            transcodingStream.WriteByte((byte)0xC3);
            Assert.Equal("cdez", ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            transcodingStream.WriteByte((byte)0xA0);
            Assert.Equal("cdez\u00E0", ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            innerStream.SetLength(0); // reset inner stream

            // Then test Write(ROS<byte>), once with a short string and once with a long string

            string asciiString = GetVeryLongAsciiString(128);
            byte[] asciiBytesAsUtf8 = Encoding.UTF8.GetBytes(asciiString);
            transcodingStream.Write(asciiBytesAsUtf8.AsSpan());
            Assert.Equal(asciiString, ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            innerStream.SetLength(0); // reset inner stream

            asciiString = GetVeryLongAsciiString(16 * 1024 * 1024);
            asciiBytesAsUtf8 = Encoding.UTF8.GetBytes(asciiString);
            transcodingStream.Write(asciiBytesAsUtf8.AsSpan());
            Assert.Equal(asciiString, ErrorCheckingUnicodeEncoding.GetString(innerStream.ToArray()));

            innerStream.SetLength(0); // reset inner stream

            // Close the outer stream and ensure no leftover data was written to the inner stream

            transcodingStream.Close();
            Assert.Equal(0, innerStream.Position);
        }

        [Fact]
        public void Write_WithPartialData()
        {
            MemoryStream innerStream = new MemoryStream();
            Stream transcodingStream = Encoding.CreateTranscodingStream(
                innerStream,
                innerStreamEncoding: CustomAsciiEncoding /* performs custom substitution */,
                outerStreamEncoding: Encoding.UTF8 /* performs U+FFFD substition */,
                leaveOpen: true);

            // First, write some incomplete data

            transcodingStream.Write(new byte[] { 0x78, 0x79, 0x7A, 0xC3 }); // [C3] shouldn't be flushed yet
            Assert.Equal("xyz", ErrorCheckingAsciiEncoding.GetString(innerStream.ToArray()));

            // Flushing should have no effect

            transcodingStream.Flush();
            Assert.Equal("xyz", ErrorCheckingAsciiEncoding.GetString(innerStream.ToArray()));

            // Provide the second byte of the multi-byte sequence

            transcodingStream.WriteByte(0xA0); // [C3 A0] = U+00E0
            Assert.Equal("xyz[00E0]", ErrorCheckingAsciiEncoding.GetString(innerStream.ToArray()));

            // Provide an incomplete sequence, then close the stream.
            // Closing the stream should flush the underlying buffers and write the replacement char.

            transcodingStream.Write(new byte[] { 0xE0, 0xBF }); // first 2 bytes of incomplete 3-byte sequence
            Assert.Equal("xyz[00E0]", ErrorCheckingAsciiEncoding.GetString(innerStream.ToArray())); // wasn't flushed yet

            transcodingStream.Close();
            Assert.Equal("xyz[00E0][FFFD]", ErrorCheckingAsciiEncoding.GetString(innerStream.ToArray()));
        }

        [Fact]
        public void Write_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => transcodingStream.Write(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Write(new byte[5], -1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Write(new byte[5], 3, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Write(new byte[5], 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Write(new byte[5], 6, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.Write(new byte[5], 6, 0));
        }

        [Fact]
        public async Task WriteAsync_WithFullData()
        {
            MemoryStream sink = new MemoryStream();
            CancellationToken expectedFlushAsyncCancellationToken = new CancellationTokenSource().Token;
            CancellationToken expectedWriteAsyncCancellationToken = new CancellationTokenSource().Token;

            var innerStreamMock = new Mock<Stream>(MockBehavior.Strict);
            innerStreamMock.Setup(o => o.CanWrite).Returns(true);
            innerStreamMock.Setup(o => o.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), expectedWriteAsyncCancellationToken))
                .Returns<ReadOnlyMemory<byte>, CancellationToken>(sink.WriteAsync);
            innerStreamMock.Setup(o => o.FlushAsync(expectedFlushAsyncCancellationToken)).Returns(Task.CompletedTask);

            Stream transcodingStream = Encoding.CreateTranscodingStream(
                innerStreamMock.Object,
                innerStreamEncoding: ErrorCheckingUnicodeEncoding,
                outerStreamEncoding: Encoding.UTF8 /* performs U+FFFD substition */,
                leaveOpen: true);

            // First, test WriteAsync(byte[], int, int, CancellationToken)

            await transcodingStream.WriteAsync(Encoding.UTF8.GetBytes("abcdefg"), 2, 3, expectedWriteAsyncCancellationToken);
            Assert.Equal("cde", ErrorCheckingUnicodeEncoding.GetString(sink.ToArray()));

            // We'll write U+00E0 (utf-8: [C3 A0]) byte-by-byte.
            // We shouldn't flush any intermediate bytes.

            await transcodingStream.WriteAsync(new byte[] { 0xC3, 0xA0 }, 0, 1, expectedWriteAsyncCancellationToken);
            await transcodingStream.FlushAsync(expectedFlushAsyncCancellationToken);
            Assert.Equal("cde", ErrorCheckingUnicodeEncoding.GetString(sink.ToArray()));

            await transcodingStream.WriteAsync(new byte[] { 0xC3, 0xA0 }, 1, 1, expectedWriteAsyncCancellationToken);
            Assert.Equal("cde\u00E0", ErrorCheckingUnicodeEncoding.GetString(sink.ToArray()));

            sink.SetLength(0); // reset sink

            // Then test WriteAsync(ROM<byte>, CancellationToken), once with a short string and once with a long string

            string asciiString = GetVeryLongAsciiString(128);
            byte[] asciiBytesAsUtf8 = Encoding.UTF8.GetBytes(asciiString);
            await transcodingStream.WriteAsync(asciiBytesAsUtf8.AsMemory(), expectedWriteAsyncCancellationToken);
            Assert.Equal(asciiString, ErrorCheckingUnicodeEncoding.GetString(sink.ToArray()));

            sink.SetLength(0); // reset sink

            asciiString = GetVeryLongAsciiString(16 * 1024 * 1024);
            asciiBytesAsUtf8 = Encoding.UTF8.GetBytes(asciiString);
            await transcodingStream.WriteAsync(asciiBytesAsUtf8.AsMemory(), expectedWriteAsyncCancellationToken);
            Assert.Equal(asciiString, ErrorCheckingUnicodeEncoding.GetString(sink.ToArray()));

            sink.SetLength(0); // reset sink

            // Close the outer stream and ensure no leftover data was written to the inner stream

            ValueTask actualDisposeTask = transcodingStream.DisposeAsync();
            Assert.Equal(default(ValueTask), actualDisposeTask); // should've completed synchronously
            Assert.Equal(0, sink.Position);
        }

        [Fact]
        public async Task WriteAsync_WithPartialData()
        {
            MemoryStream sink = new MemoryStream();
            CancellationToken expectedCancellationToken = new CancellationTokenSource().Token;

            var innerStreamMock = new Mock<Stream>(MockBehavior.Strict);
            innerStreamMock.Setup(o => o.CanWrite).Returns(true);
            innerStreamMock.Setup(o => o.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), expectedCancellationToken))
                .Returns<ReadOnlyMemory<byte>, CancellationToken>(sink.WriteAsync);

            Stream transcodingStream = Encoding.CreateTranscodingStream(
                innerStreamMock.Object,
                innerStreamEncoding: CustomAsciiEncoding /* performs custom substitution */,
                outerStreamEncoding: Encoding.UTF8 /* performs U+FFFD substition */,
                leaveOpen: true);

            // First, write some incomplete data

            await transcodingStream.WriteAsync(new byte[] { 0x78, 0x79, 0x7A, 0xC3 }, expectedCancellationToken); // [C3] shouldn't be flushed yet
            Assert.Equal("xyz", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));

            // Provide the second byte of the multi-byte sequence

            await transcodingStream.WriteAsync(new byte[] { 0xA0 }, expectedCancellationToken); // [C3 A0] = U+00E0
            Assert.Equal("xyz[00E0]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));

            // Provide an incomplete sequence, then close the stream.
            // Closing the stream should flush the underlying buffers and write the replacement char.

            await transcodingStream.WriteAsync(new byte[] { 0xE0, 0xBF }, expectedCancellationToken); // first 2 bytes of incomplete 3-byte sequence
            Assert.Equal("xyz[00E0]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray())); // wasn't flushed yet

            // The call to DisposeAsync() will call innerStream.WriteAsync without a CancellationToken.

            innerStreamMock.Setup(o => o.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), CancellationToken.None))
                .Returns<ReadOnlyMemory<byte>, CancellationToken>(sink.WriteAsync);

            await transcodingStream.DisposeAsync();
            Assert.Equal("xyz[00E0][FFFD]", ErrorCheckingAsciiEncoding.GetString(sink.ToArray()));
        }

        [Fact]
        public void WriteAsync_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => (object)transcodingStream.WriteAsync(null, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.WriteAsync(new byte[5], -1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.WriteAsync(new byte[5], 3, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.WriteAsync(new byte[5], 5, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.WriteAsync(new byte[5], 6, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => (object)transcodingStream.WriteAsync(new byte[5], 6, 0));
        }

        [Fact]
        public void WriteApm()
        {
            // Arrange

            MemoryStream sink = new MemoryStream();
            object expectedState = new object();

            var innerStreamMock = new Mock<Stream>(MockBehavior.Strict);
            innerStreamMock.Setup(o => o.CanWrite).Returns(true);
            innerStreamMock.Setup(o => o.WriteAsync(It.IsAny<ReadOnlyMemory<byte>>(), CancellationToken.None))
                .Returns<ReadOnlyMemory<byte>, CancellationToken>(sink.WriteAsync);

            Stream transcodingStream = Encoding.CreateTranscodingStream(innerStreamMock.Object, Encoding.UTF8, Encoding.UTF8);

            // Act

            IAsyncResult asyncResult = transcodingStream.BeginWrite(Encoding.UTF8.GetBytes("abcdefg"), 1, 3, null, expectedState);
            transcodingStream.EndWrite(asyncResult);

            // Assert

            Assert.Equal(expectedState, asyncResult.AsyncState);
            Assert.Equal("bcd", Encoding.UTF8.GetString(sink.ToArray()));
        }

        [Fact]
        public void WriteApm_WithInvalidArgs_Throws()
        {
            Stream transcodingStream = Encoding.CreateTranscodingStream(new MemoryStream(), Encoding.UTF8, Encoding.UTF8);

            Assert.Throws<ArgumentNullException>("buffer", () => transcodingStream.BeginWrite(null, 0, 0, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginWrite(new byte[5], -1, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginWrite(new byte[5], 3, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginWrite(new byte[5], 5, 1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginWrite(new byte[5], 6, -1, null, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => transcodingStream.BeginWrite(new byte[5], 6, 0, null, null));
        }

        // returns "abc...xyzabc...xyzabc..."
        private static string GetVeryLongAsciiString(int length)
        {
            return string.Create(length, (object)null, (buffer, _) =>
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (char)('a' + (i % 26));
                }
            });
        }

        // A custom ASCIIEncoding where both encoder + decoder fallbacks have been specified
        private static readonly Encoding CustomAsciiEncoding = Encoding.GetEncoding(
            "ascii", new CustomEncoderFallback(), new DecoderReplacementFallback("\uFFFD"));

        private static readonly Encoding ErrorCheckingAsciiEncoding
            = Encoding.GetEncoding("ascii", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);

        private static readonly UnicodeEncoding ErrorCheckingUnicodeEncoding
            = new UnicodeEncoding(bigEndian: false, byteOrderMark: false, throwOnInvalidBytes: true);

        // A custom encoder fallback which substitutes unknown chars with "[xxxx]" (the code point as hex)
        private sealed class CustomEncoderFallback : EncoderFallback
        {
            public override int MaxCharCount => 8; // = "[10FFFF]".Length

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new CustomEncoderFallbackBuffer();
            }

            private sealed class CustomEncoderFallbackBuffer : EncoderFallbackBuffer
            {
                private string _remaining = string.Empty;
                private int _remainingIdx = 0;

                public override int Remaining => _remaining.Length - _remainingIdx;

                public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
                    => FallbackCommon((uint)char.ConvertToUtf32(charUnknownHigh, charUnknownLow));

                public override bool Fallback(char charUnknown, int index)
                    => FallbackCommon(charUnknown);

                private bool FallbackCommon(uint codePoint)
                {
                    Assert.True(codePoint <= 0x10FFFF);
                    _remaining = FormattableString.Invariant($"[{codePoint:X4}]");
                    _remainingIdx = 0;
                    return true;
                }

                public override char GetNextChar()
                {
                    return (_remainingIdx < _remaining.Length)
                        ? _remaining[_remainingIdx++]
                        : '\0' /* end of string reached */;
                }

                public override bool MovePrevious()
                {
                    if (_remainingIdx == 0)
                    {
                        return false;
                    }

                    _remainingIdx--;
                    return true;
                }
            }
        }

        /// <summary>A custom encoding that's used to roundtrip from bytes to bytes through a string.</summary>
        private sealed class IdentityEncoding : Encoding
        {
            public override int GetByteCount(char[] chars, int index, int count) => count;

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            {
                Span<char> span = chars.AsSpan(charIndex, charCount);
                for (int i = 0; i < span.Length; i++)
                {
                    Debug.Assert(span[i] <= 0xFF);
                    bytes[byteIndex + i] = (byte)span[i];
                }
                return charCount;
            }

            public override int GetCharCount(byte[] bytes, int index, int count) => count;

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                Span<byte> span = bytes.AsSpan(byteIndex, byteCount);
                for (int i = 0; i < span.Length; i++)
                {
                    Debug.Assert(span[i] <= 0xFF);
                    chars[charIndex + i] = (char)span[i];
                }
                return byteCount;
            }

            public override int GetMaxByteCount(int charCount) => charCount;

            public override int GetMaxCharCount(int byteCount) => byteCount;

            public override byte[] GetPreamble() => Array.Empty<byte>();
        }

        // A helper type that allows synchronously writing to a stream while asynchronously
        // reading from it.
        private sealed class AsyncComms : IDisposable
        {
            private readonly BlockingCollection<byte[]> _blockingCollection;
            private readonly PipeWriter _writer;

            public AsyncComms()
            {
                _blockingCollection = new BlockingCollection<byte[]>();
                var pipe = new Pipe();
                ReadStream = pipe.Reader.AsStream();
                _writer = pipe.Writer;
                Task.Run(_DrainWorker);
            }

            public Stream ReadStream { get; }

            public void Dispose()
            {
                _blockingCollection.Dispose();
            }

            public void WriteBytes(ReadOnlySpan<byte> bytes)
            {
                _blockingCollection.Add(bytes.ToArray());
            }

            public void WriteEof()
            {
                _blockingCollection.Add(null);
            }

            private async Task _DrainWorker()
            {
                byte[] buffer;
                while ((buffer = _blockingCollection.Take()) is not null)
                {
                    await _writer.WriteAsync(buffer);
                }
                _writer.Complete();
            }
        }
    }
}

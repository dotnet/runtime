// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Tests
{
    public class BufferedStream_StreamAsync
    {
        [Fact]
        public static void NullConstructor_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new BufferedStream(null));
        }

        [Fact]
        public static void NegativeBufferSize_Throws_ArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferedStream(new MemoryStream(), -1));
        }

        [Fact]
        public static void ZeroBufferSize_Throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BufferedStream(new MemoryStream(), 0));
        }

        [Fact]
        public static void UnderlyingStreamDisposed_Throws_ObjectDisposedException()
        {
            MemoryStream disposedStream = new MemoryStream();
            disposedStream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => new BufferedStream(disposedStream));
        }

        [Fact]
        public void UnderlyingStream()
        {
            var underlyingStream = new MemoryStream();
            var bufferedStream = new BufferedStream(underlyingStream);
            Assert.Same(underlyingStream, bufferedStream.UnderlyingStream);
        }

        [Fact]
        public void BufferSize()
        {
            var bufferedStream = new BufferedStream(new MemoryStream(), 1234);
            Assert.Equal(1234, bufferedStream.BufferSize);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [OuterLoop]
        public void WriteFromByte_InputSizeLargerThanHalfOfMaxInt_ShouldSuccess()
        {
            const int InputSize = int.MaxValue / 2 + 1;
            byte[] bytes;
            try
            {
                bytes = new byte[InputSize];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            var writableStream = new WriteOnlyStream();
            using (var bs = new BufferedStream(writableStream))
            {
                bs.Write(bytes, 0, InputSize);
                Assert.Equal(InputSize, writableStream.Position);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
        [OuterLoop]
        public void WriteFromSpan_InputSizeLargerThanHalfOfMaxInt_ShouldSuccess()
        {
            const int InputSize = int.MaxValue / 2 + 1;
            byte[] bytes;
            try
            {
                bytes = new byte[InputSize];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            var writableStream = new WriteOnlyStream();
            using (var bs = new BufferedStream(writableStream))
            {
                bs.Write(new ReadOnlySpan<byte>(bytes));
                Assert.Equal(InputSize, writableStream.Position);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ShouldNotFlushUnderlyingStreamIfReadOnly(bool underlyingCanSeek)
        {
            var underlying = new DelegateStream(
                canReadFunc: () => true,
                canWriteFunc: () => false,
                canSeekFunc: () => underlyingCanSeek,
                readFunc: (_, __, ___) => 123,
                writeFunc: (_, __, ___) =>
                {
                    throw new NotSupportedException();
                },
                seekFunc: (_, __) => 123L
            );

            var wrapper = new CallTrackingStream(underlying);

            var buffered = new BufferedStream(wrapper);
            buffered.ReadByte();

            buffered.Flush();
            Assert.Equal(0, wrapper.TimesCalled(nameof(wrapper.Flush)));

            await buffered.FlushAsync();
            Assert.Equal(0, wrapper.TimesCalled(nameof(wrapper.FlushAsync)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task ConcurrentOperationsAreSerialized()
        {
            byte[] data = Enumerable.Range(0, 1000).Select(i => unchecked((byte)i)).ToArray();
            var mcaos = new ManuallyReleaseAsyncOperationsStream();
            var stream = new BufferedStream(mcaos, 1);

            var tasks = new Task[4];
            for (int i = 0; i < 4; i++)
            {
                tasks[i] = stream.WriteAsync(data, 250 * i, 250);
            }
            Assert.All(tasks, t => Assert.Equal(TaskStatus.WaitingForActivation, t.Status));

            mcaos.Release();
            await Task.WhenAll(tasks);

            stream.Position = 0;
            for (int i = 0; i < tasks.Length; i++)
            {
                Assert.Equal(i, stream.ReadByte());
            }
        }

        [Fact]
        public void UnderlyingStreamThrowsExceptions()
        {
            var stream = new BufferedStream(new ThrowsExceptionFromAsyncOperationsStream());

            Assert.Equal(TaskStatus.Faulted, stream.ReadAsync(new byte[1], 0, 1).Status);

            Assert.Equal(TaskStatus.Faulted, stream.WriteAsync(new byte[10000], 0, 10000).Status);

            stream.WriteByte(1);
            Assert.Equal(TaskStatus.Faulted, stream.FlushAsync().Status);
        }

        [ConditionalTheory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CopyToTest_RequiresFlushingOfWrites(bool copyAsynchronously)
        {
            if (copyAsynchronously && !PlatformDetection.IsThreadingSupported)
            {
                throw new SkipTestException(nameof(PlatformDetection.IsThreadingSupported));
            }

            byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

            var manualReleaseStream = new ManuallyReleaseAsyncOperationsStream();
            var src = new BufferedStream(manualReleaseStream);
            src.Write(data, 0, data.Length);
            src.Position = 0;

            var dst = new MemoryStream();

            data[0] = 42;
            src.WriteByte(42);
            dst.WriteByte(42);

            if (copyAsynchronously)
            {
                Task copyTask = src.CopyToAsync(dst);
                manualReleaseStream.Release();
                await copyTask;
            }
            else
            {
                manualReleaseStream.Release();
                src.CopyTo(dst);
            }

            Assert.Equal(data, dst.ToArray());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task CopyToTest_ReadBeforeCopy_CopiesAllData(bool copyAsynchronously, bool wrappedStreamCanSeek)
        {
            byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

            var wrapped = new ManuallyReleaseAsyncOperationsStream();
            wrapped.Release();
            wrapped.Write(data, 0, data.Length);
            wrapped.Position = 0;
            wrapped.SetCanSeek(wrappedStreamCanSeek);
            var src = new BufferedStream(wrapped, 100);

            src.ReadByte();

            var dst = new MemoryStream();
            if (copyAsynchronously)
            {
                await src.CopyToAsync(dst);
            }
            else
            {
                src.CopyTo(dst);
            }

            var expected = new byte[data.Length - 1];
            Array.Copy(data, 1, expected, 0, expected.Length);
            Assert.Equal(expected, dst.ToArray());
        }
    }

    public class BufferedStream_TestLeaveOpen : TestLeaveOpen
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamWriterWithBufferedStream_CloseTests : CloseTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamWriterWithBufferedStream_FlushTests : FlushTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        [Fact]
        public void WriteAfterRead_NonSeekableStream_Throws()
        {
            var wrapped = new WrappedMemoryStream(canRead: true, canWrite: true, canSeek: false, data: new byte[] { 1, 2, 3, 4, 5 });
            var s = new BufferedStream(wrapped);

            s.Read(new byte[3], 0, 3);
            Assert.Throws<NotSupportedException>(() => s.Write(new byte[10], 0, 10));
        }
    }

    public class StreamWriterWithBufferedStream_WriteTests : WriteTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class StreamReaderWithBufferedStream_Tests : StreamReaderTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        protected override Stream GetSmallStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            return new BufferedStream(new MemoryStream(testData));
        }

        protected override Stream GetLargeStream()
        {
            byte[] testData = new byte[] { 72, 69, 76, 76, 79 };
            List<byte> data = new List<byte>();
            for (int i = 0; i < 1000; i++)
            {
                data.AddRange(testData);
            }

            return new BufferedStream(new MemoryStream(data.ToArray()));
        }
    }

    public class BinaryWriterWithBufferedStream_Tests : BinaryWriterTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        [Fact]
        public override void BinaryWriter_FlushTests()
        {
            // [] Check that flush updates the underlying stream
            using (Stream memstr2 = CreateStream())
            using (BinaryWriter bw2 = new BinaryWriter(memstr2))
            {
                string str = "HelloWorld";
                int expectedLength = str.Length + 1; // 1 for 7-bit encoded length
                bw2.Write(str);
                Assert.Equal(expectedLength, memstr2.Length);
                bw2.Flush();
                Assert.Equal(expectedLength, memstr2.Length);
            }

            // [] Flushing a closed writer may throw an exception depending on the underlying stream
            using (Stream memstr2 = CreateStream())
            {
                BinaryWriter bw2 = new BinaryWriter(memstr2);
                bw2.Dispose();
                Assert.Throws<ObjectDisposedException>(() => bw2.Flush());
            }
        }
    }

    public class BinaryWriterWithBufferedStream_WriteByteCharTests : BinaryWriter_WriteByteCharTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    public class BinaryWriterWithBufferedStream_WriteTests : BinaryWriter_WriteTests
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }
    }

    internal sealed class ManuallyReleaseAsyncOperationsStream : Stream
    {
        private readonly MemoryStream _stream = new MemoryStream();
        private readonly TaskCompletionSource _tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _canSeek = true;

        public override bool CanSeek => _canSeek;

        public override bool CanRead => _stream.CanRead;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position { get => _stream.Position; set => _stream.Position = value; }

        public void SetCanSeek(bool canSeek) => _canSeek = canSeek;

        public void Release() => _tcs.SetResult();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _tcs.Task;
            return await _stream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _tcs.Task;
            await _stream.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _tcs.Task;
            await _stream.FlushAsync(cancellationToken);
        }

        public override void Flush() => _stream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _stream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => _stream.Write(buffer, offset, count);
    }

    internal sealed class ThrowsExceptionFromAsyncOperationsStream : MemoryStream
    {
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Exception from ReadAsync");

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new InvalidOperationException("Exception from ReadAsync");

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Exception from ReadAsync");

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Exception from WriteAsync");

        public override Task FlushAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Exception from FlushAsync");
    }

    internal sealed class WriteOnlyStream : Stream
    {
        private long _pos;

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _pos += count;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            _pos += buffer.Length;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _pos;

        public override long Position
        {
            get => _pos;
            set => throw new NotSupportedException();
        }
    }
}

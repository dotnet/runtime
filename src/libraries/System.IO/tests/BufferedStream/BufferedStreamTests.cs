// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class BufferedStream_StreamAsync : StreamAsync
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        [Fact]
        public async Task ConcurrentOperationsAreSerialized()
        {
            byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
            var mcaos = new ManuallyReleaseAsyncOperationsStream();
            var stream = new BufferedStream(mcaos, 1);

            var tasks = new Task[4];
            for (int i = 0; i < 4; i++)
            {
                tasks[i] = stream.WriteAsync(data, 250 * i, 250);
            }
            Assert.False(tasks.All(t => t.IsCompleted));

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

        [Fact]
        public async Task CopyToAsyncTest_RequiresAsyncFlushingOfWrites()
        {
            byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)(i % 256)).ToArray();

            var manualReleaseStream = new ManuallyReleaseAsyncOperationsStream();
            var src = new BufferedStream(manualReleaseStream);
            src.Write(data, 0, data.Length);
            src.Position = 0;

            var dst = new MemoryStream();

            data[0] = 42;
            src.WriteByte(42);
            dst.WriteByte(42);

            Task copyTask = src.CopyToAsync(dst);
            manualReleaseStream.Release();
            await copyTask;

            Assert.Equal(data, dst.ToArray());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task CopyToAsyncTest_ReadBeforeCopy_CopiesAllData(bool wrappedStreamCanSeek)
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
            await src.CopyToAsync(dst);

            var expected = new byte[data.Length - 1];
            Array.Copy(data, 1, expected, 0, expected.Length);
            Assert.Equal(expected, dst.ToArray());
        }
    }

    public class BufferedStream_StreamMethods : StreamMethods
    {
        protected override Stream CreateStream()
        {
            return new BufferedStream(new MemoryStream());
        }

        protected override Stream CreateStream(int bufferSize)
        {
            return new BufferedStream(new MemoryStream(), bufferSize);
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

    internal sealed class ManuallyReleaseAsyncOperationsStream : MemoryStream
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private bool _canSeek = true;

        public override bool CanSeek => _canSeek;

        public void SetCanSeek(bool canSeek) => _canSeek = canSeek;

        public void Release() { _tcs.SetResult(true); }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _tcs.Task;
            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _tcs.Task;
            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _tcs.Task;
            await base.FlushAsync(cancellationToken);
        }
    }

    internal sealed class ThrowsExceptionFromAsyncOperationsStream : MemoryStream
    {
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Exception from ReadAsync");
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Exception from WriteAsync");
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Exception from FlushAsync");
        }
    }
}

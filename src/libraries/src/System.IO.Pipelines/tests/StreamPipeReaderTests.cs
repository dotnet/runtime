using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeReaderTests
    {
        [Fact]
        public async Task CanRead()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(stream);

            ReadResult readResult = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(buffer.ToArray()));

            reader.AdvanceTo(buffer.End);
            reader.Complete();
        }

        [Fact]
        public async Task TryReadReturnsTrueIfBufferedBytesAndNotExaminedEverything()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(stream);

            ReadResult readResult = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;
            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            reader.AdvanceTo(buffer.Start, buffer.GetPosition(5));

            Assert.True(reader.TryRead(out readResult));
            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(buffer.ToArray()));

            reader.Complete();
        }

        [Fact]
        public async Task TryReadReturnsFalseIfBufferedBytesAndEverythingExamined()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(stream);

            ReadResult readResult = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;
            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            reader.AdvanceTo(buffer.End);

            Assert.False(reader.TryRead(out readResult));
            reader.Complete();
        }

        [Fact]
        public async Task CanReadMultipleTimes()
        {
            // This needs to run inline to synchronize the reader and writer
            TaskCompletionSource<object> waitForRead = null;

            async Task DoAsyncRead(PipeReader reader, int[] bufferSizes)
            {
                var index = 0;
                while (true)
                {
                    ReadResult readResult = await reader.ReadAsync().ConfigureAwait(false);

                    if (readResult.IsCompleted)
                    {
                        break;
                    }

                    Assert.Equal(bufferSizes[index], readResult.Buffer.Length);
                    reader.AdvanceTo(readResult.Buffer.End);
                    index++;
                    waitForRead?.TrySetResult(null);
                }

                reader.Complete();
            }

            async Task DoAsyncWrites(PipeWriter writer, int[] bufferSizes)
            {
                for (int i = 0; i < bufferSizes.Length; i++)
                {
                    writer.WriteEmpty(bufferSizes[i]);
                    waitForRead = new TaskCompletionSource<object>();
                    await writer.FlushAsync().ConfigureAwait(false);
                    await waitForRead.Task;
                }

                writer.Complete();
            }

            // We're using the pipe here as a way to pump bytes into the reader asynchronously
            var pipe = new Pipe();
            var options = new StreamPipeReaderOptions(bufferSize: 4096);
            PipeReader reader = PipeReader.Create(pipe.Reader.AsStream(), options);

            var writes = new[] { 4096, 1024, 123, 4096, 100 };

            Task readingTask = DoAsyncRead(reader, writes);
            Task writingTask = DoAsyncWrites(pipe.Writer, writes);

            await readingTask;
            await writingTask;

            pipe.Reader.Complete();
        }

        [Theory]
        [MemberData(nameof(ReadSettings))]
        public async Task ReadWithDifferentSettings(int bytesInBuffer, int bufferSize, int minimumReadSize, int[] readBufferSizes)
        {
            var options = new StreamPipeReaderOptions(bufferSize: bufferSize, minimumReadSize: minimumReadSize, pool: new HeapBufferPool());
            var stream = new MemoryStream(Enumerable.Range(0, bytesInBuffer).Select(i => (byte)i).ToArray());
            PipeReader reader = PipeReader.Create(stream, options);

            for (int i = 0; i < readBufferSizes.Length; i++)
            {
                ReadResult readResult = await reader.ReadAsync();
                long length = readResult.Buffer.Length;
                Assert.Equal(readBufferSizes[i], length);
                reader.AdvanceTo(readResult.Buffer.End);
                if (length == 0)
                {
                    Assert.True(readResult.IsCompleted);
                }
            }

            reader.Complete();
        }

        [Fact]
        public async Task ReadAsyncAfterReceivingCompletedReadResultDoesNotThrow()
        {
            var stream = new ThrowAfterZeroByteReadStream();
            PipeReader reader = PipeReader.Create(stream);
            ReadResult readResult = await reader.ReadAsync();
            Assert.True(readResult.Buffer.IsEmpty);
            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);

            readResult = await reader.ReadAsync();
            Assert.True(readResult.Buffer.IsEmpty);
            Assert.True(readResult.IsCompleted);
            reader.AdvanceTo(readResult.Buffer.End);
            reader.Complete();
        }

        [Fact]
        public async Task BufferingDataPastEndOfStreamCanBeReadAgain()
        {
            var helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new ThrowAfterZeroByteReadStream(helloBytes);
            PipeReader reader = PipeReader.Create(stream);


            ReadResult readResult = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;
            reader.AdvanceTo(buffer.Start, buffer.End);

            // Make sure IsCompleted is true
            readResult = await reader.ReadAsync();
            buffer = readResult.Buffer;
            reader.AdvanceTo(buffer.Start, buffer.End);
            Assert.True(readResult.IsCompleted);

            var value = await ReadFromPipeAsString(reader);
            Assert.Equal("Hello World", value);
            reader.Complete();
        }

        [Fact]
        public async Task NextReadAfterPartiallyExaminedReturnsImmediately()
        {
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(new string('a', 10000)));
            PipeReader reader = PipeReader.Create(stream);

            ReadResult readResult = await reader.ReadAsync();
            reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.GetPosition(2048));

            ValueTask<ReadResult> task = reader.ReadAsync();

            // This should complete synchronously since
            Assert.True(task.IsCompleted);

            readResult = await task;
            reader.AdvanceTo(readResult.Buffer.End);
            reader.Complete();
        }

        [Fact]
        public async Task CompleteReaderWithoutAdvanceDoesNotThrow()
        {
            PipeReader reader = PipeReader.Create(Stream.Null);
            await reader.ReadAsync();
            reader.Complete();
        }

        [Fact]
        public async Task AdvanceAfterCompleteThrows()
        {
            PipeReader reader = PipeReader.Create(new MemoryStream(new byte[100]));
            ReadOnlySequence<byte> buffer = (await reader.ReadAsync()).Buffer;

            reader.Complete();

            Assert.Throws<InvalidOperationException>(() => reader.AdvanceTo(buffer.End));
        }

        [Fact]
        public async Task ThrowsOnReadAfterCompleteReader()
        {
            var reader = PipeReader.Create(Stream.Null);

            reader.Complete();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.ReadAsync());
        }

        [Fact]
        public void TryReadAfterCancelPendingReadReturnsTrue()
        {
            var reader = PipeReader.Create(Stream.Null);

            reader.CancelPendingRead();

            Assert.True(reader.TryRead(out ReadResult result));
            Assert.True(result.IsCanceled);
            reader.AdvanceTo(result.Buffer.End);
            reader.Complete();
        }

        [Fact]
        public async Task ReadCanBeCancelledViaProvidedCancellationToken()
        {
            var stream = new CancelledReadsStream();
            PipeReader reader = PipeReader.Create(stream);
            var cts = new CancellationTokenSource();

            ValueTask<ReadResult> task = reader.ReadAsync(cts.Token);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            stream.WaitForReadTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            reader.Complete();
        }

        [Fact]
        public async Task ReadCanBeCanceledViaCancelPendingReadWhenReadIsAsync()
        {
            var stream = new CancelledReadsStream();
            PipeReader reader = PipeReader.Create(stream);

            ValueTask<ReadResult> task = reader.ReadAsync();

            reader.CancelPendingRead();

            stream.WaitForReadTask.TrySetResult(null);

            ReadResult readResult = await task;
            Assert.True(readResult.IsCanceled);
            reader.Complete();
        }

        [Fact]
        public async Task ReadAsyncReturnsCanceledIfCanceledBeforeRead()
        {
            var stream = new MemoryStream(new byte[10000]);
            PipeReader reader = PipeReader.Create(stream);

            // Make sure state isn't used from before
            for (var i = 0; i < 3; i++)
            {
                reader.CancelPendingRead();
                ValueTask<ReadResult> readResultTask = reader.ReadAsync();
                Assert.True(readResultTask.IsCompleted);
                ReadResult readResult = readResultTask.GetAwaiter().GetResult();
                Assert.True(readResult.IsCanceled);
                readResult = await reader.ReadAsync();
                reader.AdvanceTo(readResult.Buffer.End);
            }

            reader.Complete();
        }

        [Fact]
        public async Task ReadAsyncReturnsCanceledInterleaved()
        {
            var stream = new MemoryStream(new byte[10000]);
            PipeReader reader = PipeReader.Create(stream);

            // Cancel and Read interleaved to confirm cancellations are independent
            for (var i = 0; i < 3; i++)
            {
                reader.CancelPendingRead();
                ValueTask<ReadResult> readResultTask = reader.ReadAsync();
                Assert.True(readResultTask.IsCompleted);
                ReadResult readResult = readResultTask.GetAwaiter().GetResult();
                Assert.True(readResult.IsCanceled);

                readResult = await reader.ReadAsync();
                Assert.False(readResult.IsCanceled);
            }

            reader.Complete();
        }

        [Fact]
        public async Task ConsumePartialBufferWorks()
        {
            // We're using the pipe here as a way to pump bytes into the reader asynchronously
            var pipe = new Pipe();
            PipeReader reader = PipeReader.Create(pipe.Reader.AsStream());

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            ReadResult readResult = await reader.ReadAsync();
            Assert.Equal(10, readResult.Buffer.Length);
            reader.AdvanceTo(readResult.Buffer.GetPosition(4), readResult.Buffer.End);

            pipe.Writer.WriteEmpty(2);
            await pipe.Writer.FlushAsync();

            readResult = await reader.ReadAsync();
            // 6 bytes left over plus 2 newly written bytes
            Assert.Equal(8, readResult.Buffer.Length);
            reader.AdvanceTo(readResult.Buffer.End);

            reader.Complete();

            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task ConsumingSegmentsReturnsMemoryToPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var options = new StreamPipeReaderOptions(pool: pool, bufferSize: 4096, minimumReadSize: 1024);
                // 2 full segments
                var stream = new MemoryStream(new byte[options.BufferSize * 2]);
                PipeReader reader = PipeReader.Create(stream, options);

                ReadResult readResult = await reader.ReadAsync();
                ReadOnlySequence<byte> buffer = readResult.Buffer;
                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(options.BufferSize, buffer.Length);
                reader.AdvanceTo(buffer.Start, buffer.End);

                readResult = await reader.ReadAsync();
                buffer = readResult.Buffer;
                Assert.Equal(options.BufferSize * 2, buffer.Length);
                Assert.Equal(2, pool.CurrentlyRentedBlocks);
                reader.AdvanceTo(buffer.Start, buffer.End);

                readResult = await reader.ReadAsync();
                buffer = readResult.Buffer;
                Assert.Equal(options.BufferSize * 2, buffer.Length);
                // We end up allocating a 3rd block here since we don't know ahead of time that 
                // it's the last one
                Assert.Equal(3, pool.CurrentlyRentedBlocks);

                reader.AdvanceTo(buffer.Slice(buffer.Start, 4096).End, buffer.End);

                Assert.Equal(2, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);

                readResult = await reader.ReadAsync();
                buffer = readResult.Buffer;
                Assert.Equal(options.BufferSize, buffer.Length);
                reader.AdvanceTo(buffer.Slice(buffer.Start, 4096).End, buffer.End);

                // All of the blocks get returned here since we hit the first case of emptying the entire list
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);

                reader.Complete();
            }
        }

        [Fact]
        public void CompletingTheReadingDisposesStream()
        {
            var stream = new ObserveDisposeStream();
            PipeReader reader = PipeReader.Create(stream);
            reader.Complete();

            Assert.Equal(1, stream.DisposedCount);
        }

        [Fact]
        public void CompletingAlreadyCompletePipeReaderNoopsSecondTime()
        {
            var stream = new ObserveDisposeStream();
            PipeReader reader = PipeReader.Create(stream);
            reader.Complete();
            reader.Complete();

            Assert.Equal(1, stream.DisposedCount);
        }

        [Fact]
        public async Task CompletingReturnsUnconsumedMemoryToPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var options = new StreamPipeReaderOptions(pool: pool, bufferSize: 4096, minimumReadSize: 1024);
                // 2 full segments
                var stream = new MemoryStream(new byte[options.BufferSize * 3]);
                PipeReader reader = PipeReader.Create(stream, options);

                while (true)
                {
                    ReadResult readResult = await reader.ReadAsync();
                    reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

                    if (readResult.IsCompleted)
                    {
                        break;
                    }
                }

                Assert.Equal(4, pool.CurrentlyRentedBlocks);
                reader.Complete();
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(4, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task NewSegmentsAllocatedWhenBufferReachesMinimumReadSize()
        {
            // We're using the pipe here as a way to pump bytes into the reader asynchronously
            var pipe = new Pipe();
            var options = new StreamPipeReaderOptions(pool: new HeapBufferPool(), bufferSize: 10, minimumReadSize: 5);
            PipeReader reader = PipeReader.Create(pipe.Reader.AsStream(), options);

            pipe.Writer.WriteEmpty(6);
            await pipe.Writer.FlushAsync();

            ReadResult readResult = await reader.ReadAsync();
            Assert.Equal(6, readResult.Buffer.Length);
            Assert.True(readResult.Buffer.IsSingleSegment);
            reader.AdvanceTo(readResult.Buffer.Start, readResult.Buffer.End);

            pipe.Writer.WriteEmpty(4);
            await pipe.Writer.FlushAsync();

            readResult = await reader.ReadAsync();
            Assert.Equal(10, readResult.Buffer.Length);
            Assert.False(readResult.Buffer.IsSingleSegment);
            var segments = 0;
            foreach (ReadOnlyMemory<byte> segment in readResult.Buffer)
            {
                segments++;
            }
            Assert.Equal(2, segments);
            reader.AdvanceTo(readResult.Buffer.End);

            reader.Complete();

            pipe.Writer.Complete();
        }

        [Fact]
        public void OnWriterCompletedThrowsNotSupportedException()
        {
            PipeReader reader = PipeReader.Create(Stream.Null);

            Assert.Throws<NotSupportedException>(() => reader.OnWriterCompleted((_, __) => { }, null));
            reader.Complete();
        }

        [Fact]
        public async Task InvalidCursorThrows()
        {
            var pipe = new Pipe();
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            ReadResult readResult = await pipe.Reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;

            PipeReader reader = PipeReader.Create(Stream.Null);
            Assert.Throws<InvalidOperationException>(() => reader.AdvanceTo(buffer.Start, buffer.End));

            pipe.Reader.Complete();
            pipe.Writer.Complete();

            reader.Complete();
        }
        
        [Fact]
        public void NullStreamThrows()
        {
            Assert.Throws<ArgumentNullException>(() => PipeReader.Create(null));
        }

        [Fact]
        public void InvalidBufferSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeReaderOptions(bufferSize: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeReaderOptions(bufferSize: 0));
        }

        [Fact]
        public void InvalidMinimumReadSizeThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeReaderOptions(minimumReadSize: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeReaderOptions(minimumReadSize: 0));
        }

        private static async Task<string> ReadFromPipeAsString(PipeReader reader)
        {
            ReadResult readResult = await reader.ReadAsync();
            var result = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            reader.AdvanceTo(readResult.Buffer.End);
            return result;
        }

        public static IEnumerable<object[]> ReadSettings
        {
            get
            {
                yield return CreateRead(bytesInBuffer: 1024, bufferSize: 1024, minimumReadSize: 1024, readSizes: new[] { 1024, 0 });
                yield return CreateRead(bytesInBuffer: 1023, bufferSize: 512, minimumReadSize: 512, readSizes: new[] { 512, 511, 0 });
                yield return CreateRead(bytesInBuffer: 512, bufferSize: 1000, minimumReadSize: 512, readSizes: new[] { 512, 0 });
                yield return CreateRead(bytesInBuffer: 10, bufferSize: 100, minimumReadSize: 512, readSizes: new[] { 10, 0 });
                yield return CreateRead(bytesInBuffer: 8192, bufferSize: 3000, minimumReadSize: 2048, readSizes: new[] { 3000, 3000, 2192, 0 });
                yield return CreateRead(bytesInBuffer: 4096, bufferSize: 3000, minimumReadSize: 2048, readSizes: new[] { 3000, 1096, 0 });
            }
        }

        // Helper to make the above code look nicer
        private static object[] CreateRead(int bytesInBuffer, int bufferSize, int minimumReadSize, int[] readSizes)
        {
            return new object[] { bytesInBuffer, bufferSize, minimumReadSize, readSizes };
        }

        private class ThrowAfterZeroByteReadStream : MemoryStream
        {
            public ThrowAfterZeroByteReadStream()
            {

            }

            public ThrowAfterZeroByteReadStream(byte[] buffer) : base(buffer)
            {

            }

            private bool _throwOnNextCallToRead;
            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (_throwOnNextCallToRead)
                {
                    throw new Exception();
                }
                var bytes = await base.ReadAsync(buffer, offset, count, cancellationToken);
                if (bytes == 0)
                {
                    _throwOnNextCallToRead = true;
                }
                return bytes;
            }

#if netcoreapp
            public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
            {
                if (_throwOnNextCallToRead)
                {
                    throw new Exception();
                }
                var bytes = await base.ReadAsync(destination, cancellationToken);
                if (bytes == 0)
                {
                    _throwOnNextCallToRead = true;
                }
                return bytes;
            }
#endif
        }
    }
}

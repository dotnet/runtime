// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeWriterTests
    {
        [Fact]
        public void NothingWrittenToStreamUnlessFlushed()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream);

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            writer.Complete();


        }

        [Fact]
        public void DataFlushedOnComplete()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            writer.Complete();

            Assert.Equal(0, writer.UnflushedBytes);
            Assert.Equal(bytes.Length, stream.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task DataFlushedOnCompleteAsync()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            await writer.CompleteAsync();

            Assert.Equal(bytes.Length, stream.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(stream.ToArray()));
            Assert.Equal(0, writer.UnflushedBytes);
        }

        [Fact]
        public void DataNotFlushedOnCompleteWithException()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            writer.Complete(new Exception());

            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public async Task DataNotFlushedOnCompleteAsyncWithException()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            await writer.CompleteAsync(new Exception());

            Assert.Equal(0, stream.Length);
        }

        [Fact]
        public async Task CompleteAsyncDoesNotThrowObjectDisposedException()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            await writer.FlushAsync();
            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(0, stream.Length);

            await writer.CompleteAsync();

            Assert.Equal(bytes.Length, stream.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(stream.ToArray()));
        }

        [Fact]
        public async Task DataWrittenOnFlushAsync()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream);

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);
            await writer.FlushAsync();

            Assert.Equal(bytes.Length, stream.Length);
            Assert.Equal("Hello World", Encoding.ASCII.GetString(stream.ToArray()));

            writer.Complete();

            Assert.Equal(0, writer.UnflushedBytes);
        }

        [Fact]
        public async Task FlushAsyncWithoutWritingDoesNotFlushAsync()
        {
            var stream = new FlushAsyncAwareStream();
            PipeWriter writer = PipeWriter.Create(stream);
            await writer.FlushAsync();

            Assert.False(stream.FlushAsyncCalled);
            writer.Complete();
            Assert.Equal(0, writer.UnflushedBytes);
        }

        [Fact]
        public async Task WritesUsingGetSpanWorks()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwzyz");
            var stream = new MemoryStream();
            var options = new StreamPipeWriterOptions(new HeapBufferPool(), minimumBufferSize: 1);
            PipeWriter writer = PipeWriter.Create(stream, options);

            for (int i = 0; i < bytes.Length; i++)
            {
                writer.GetSpan()[0] = bytes[i];
                writer.Advance(1);
            }

            await writer.FlushAsync();
            Assert.Equal(bytes, stream.ToArray());
            writer.Complete();
            Assert.Equal(0, writer.UnflushedBytes);
        }

        [Fact]
        public async Task WritesUsingGetMemoryWorks()
        {
            var bytes = Encoding.ASCII.GetBytes("abcdefghijklmnopqrstuvwzyz");
            var stream = new MemoryStream();
            var options = new StreamPipeWriterOptions(new HeapBufferPool(), minimumBufferSize: 1);
            PipeWriter writer = PipeWriter.Create(stream, options);

            for (int i = 0; i < bytes.Length; i++)
            {
                writer.GetMemory().Span[0] = bytes[i];
                writer.Advance(1);
            }

            await writer.FlushAsync();
            Assert.Equal(bytes, stream.ToArray());
            writer.Complete();
            Assert.Equal(0, writer.UnflushedBytes);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanDoMultipleAsyncWritesToStream()
        {
            var pipe = new Pipe();
            PipeWriter writer = PipeWriter.Create(pipe.Writer.AsStream());
            // This needs to run inline to synchronize the reader and writer
            TaskCompletionSource<object> waitForRead = null;

            async Task DoWritesAsync(PipeWriter writer, byte[][] writes)
            {
                for (int i = 0; i < writes.Length; i++)
                {
                    waitForRead = new TaskCompletionSource<object>();
                    await writer.WriteAsync(writes[i]);
                    await waitForRead.Task;
                }

                writer.Complete();
            }

            async Task DoReadsAsync(PipeReader reader, byte[][] reads)
            {
                int index = 0;
                while (true)
                {
                    ReadResult readResult = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = readResult.Buffer;
                    if (readResult.IsCompleted)
                    {
                        break;
                    }
                    Assert.Equal(reads[index], buffer.ToArray());
                    reader.AdvanceTo(buffer.End);
                    index++;
                    waitForRead.TrySetResult(null);
                }

                reader.Complete();
            }

            var data = new List<byte[]>
            {
                Encoding.ASCII.GetBytes("Hello"),
                Encoding.ASCII.GetBytes("World"),
                Encoding.ASCII.GetBytes("This"),
                Encoding.ASCII.GetBytes("Works"),
            }.
            ToArray();

            Task readsTask = DoReadsAsync(pipe.Reader, data);
            Task writesTask = DoWritesAsync(writer, data);

            await writesTask;
            await readsTask;
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanCancelFlushAsyncWithCancellationTokenStreamFlushAsyncThrows()
        {
            var stream = new CancelledWritesStream();
            stream.WaitForWriteTask.TrySetResult(null);

            var cts = new CancellationTokenSource();
            PipeWriter writer = PipeWriter.Create(stream);
            writer.WriteEmpty(10);
            ValueTask<FlushResult> task = writer.FlushAsync(cts.Token);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            stream.WaitForFlushTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanCancelFlushAsyncWithCancellationTokenWhenStreamWriteAsyncThrows()
        {
            var stream = new CancelledWritesStream();
            stream.WaitForFlushTask.TrySetResult(null);

            var cts = new CancellationTokenSource();
            PipeWriter writer = PipeWriter.Create(stream);
            writer.WriteEmpty(10);
            ValueTask<FlushResult> task = writer.FlushAsync(cts.Token);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            stream.WaitForWriteTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanCancelFlushAsyncWithCancelPendingFlushStreamFlushAsyncThrows()
        {
            var stream = new CancelledWritesStream();
            stream.WaitForWriteTask.TrySetResult(null);

            PipeWriter writer = PipeWriter.Create(stream);
            writer.WriteEmpty(10);
            ValueTask<FlushResult> task = writer.FlushAsync();

            Assert.False(task.IsCompleted);

            writer.CancelPendingFlush();

            stream.WaitForFlushTask.TrySetResult(null);

            FlushResult result = await task;
            Assert.True(result.IsCanceled);
            writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanCancelFlushAsyncWithCancelPendingFlushStreamWriteAsyncThrows()
        {
            var stream = new CancelledWritesStream();
            stream.WaitForFlushTask.TrySetResult(null);

            PipeWriter writer = PipeWriter.Create(stream);
            writer.WriteEmpty(10);
            ValueTask<FlushResult> task = writer.FlushAsync();

            Assert.False(task.IsCompleted);

            writer.CancelPendingFlush();

            stream.WaitForWriteTask.TrySetResult(null);

            FlushResult result = await task;
            Assert.True(result.IsCanceled);
            writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task StreamWriteAsyncThrowingDoesNotReturnMemoryToPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var stream = new CancelledWritesStream();
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);
                writer.WriteEmpty(10);

                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                ValueTask<FlushResult> task = writer.FlushAsync();
                stream.WaitForWriteTask.TrySetException(new Exception());

                await Assert.ThrowsAsync<Exception>(async () => await task);
                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                writer.Complete();
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task StreamFlushAsyncThrowingDoesReturnMemoryToPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var stream = new CancelledWritesStream();
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);
                writer.WriteEmpty(10);

                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                ValueTask<FlushResult> task = writer.FlushAsync();
                stream.WaitForWriteTask.TrySetResult(null);
                stream.WaitForFlushTask.TrySetException(new Exception());

                await Assert.ThrowsAsync<Exception>(async () => await task);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);

                writer.Complete();
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task WriteAsyncThrowsDuringMultiSegmentWriteCompleteReturnsAllMemory()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var stream = new ThrowAfterNWritesStream(1);
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);
                writer.WriteEmpty(pool.MaxBufferSize);
                writer.WriteEmpty(pool.MaxBufferSize);
                writer.WriteEmpty(pool.MaxBufferSize);

                Assert.Equal(3, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await writer.FlushAsync());

                Assert.Equal(2, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);

                writer.Complete();
                Assert.Equal(0, writer.UnflushedBytes);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }

        [Fact]
        public void CompletingTheReadingDisposesStream()
        {
            var stream = new ObserveDisposeStream();
            PipeWriter reader = PipeWriter.Create(stream);
            reader.Complete();

            Assert.Equal(1, stream.DisposedCount);
        }

        [Fact]
        public void CompletingAlreadyCompletePipeReaderNoopsSecondTime()
        {
            var stream = new ObserveDisposeStream();
            PipeWriter writer = PipeWriter.Create(stream);
            writer.Complete();
            writer.Complete();

            Assert.Equal(1, stream.DisposedCount);
        }

        [Fact]
        public void CompletingWithBufferedBytesStillReturnsMemoryToPool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
                var stream = new MemoryStream();
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);

                bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
                writer.Advance(bytes.Length);
                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                Assert.Equal(0, stream.Length);

                writer.Complete();
                Assert.Equal(0, writer.UnflushedBytes);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);
            }
        }

        [Fact]
        public void GetMemorySameAsTheMaxPoolSizeUsesThePool()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
                var stream = new MemoryStream();
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);
                Memory<byte> memory = writer.GetMemory(pool.MaxBufferSize);

                Assert.Equal(pool.MaxBufferSize, memory.Length);
                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                writer.Complete();
                Assert.Equal(0, writer.UnflushedBytes);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(1, pool.DisposedBlocks);
            }
        }

        [Fact]
        public void GetMemoryBiggerThanPoolSizeAllocatesArrayPoolArray()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var stream = new MemoryStream();
                var options = new StreamPipeWriterOptions(pool);
                PipeWriter writer = PipeWriter.Create(stream, options);
                Memory<byte> memory = writer.GetMemory(pool.MaxBufferSize + 1);
                Assert.True(memory.Length > pool.MaxBufferSize + 1);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);

                writer.Complete();
                Assert.Equal(0, writer.UnflushedBytes);
                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(0, pool.DisposedBlocks);
            }
        }

        [Fact]
        public void InvalidMinimumBufferSize_ThrowsArgException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeWriterOptions(minimumBufferSize: 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new StreamPipeWriterOptions(minimumBufferSize: -2));
        }

        [Fact]
        public void StreamPipeWriterOptions_Ctor_Defaults()
        {
            var options = new StreamPipeWriterOptions();
            Assert.Same(MemoryPool<byte>.Shared, options.Pool);
            Assert.Equal(4096, options.MinimumBufferSize);
            Assert.False(options.LeaveOpen);
        }

        [Fact]
        public void StreamPipeWriterOptions_Ctor_Roundtrip()
        {
            using (var pool = new TestMemoryPool())
            {
                var options = new StreamPipeWriterOptions(pool: pool, minimumBufferSize: 1234, leaveOpen: true);
                Assert.Same(pool, options.Pool);
                Assert.Equal(1234, options.MinimumBufferSize);
                Assert.True(options.LeaveOpen);
            }
        }

        [Fact]
        public void CallGetMemoryWithNegativeSizeHint_ThrowsArgException()
        {
            PipeWriter writer = PipeWriter.Create(Stream.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(-1));
            writer.Complete();
        }

        [Fact]
        public void CallGetSpanWithNegativeSizeHint_ThrowsArgException()
        {
            PipeWriter writer = PipeWriter.Create(Stream.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));
            writer.Complete();
        }

        [Fact]
        public void AdvancingOutOfRangeThrows()
        {
            PipeWriter writer = PipeWriter.Create(new MemoryStream());
            int oversize = writer.GetSpan().Length + 1;
            Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(oversize));
            writer.Complete();
        }

        [Fact]
        public void GetSpanAfterCompleteThrows()
        {
            PipeWriter writer = PipeWriter.Create(new MemoryStream());
            writer.WriteEmpty(10);
            writer.Complete();

            Assert.Throws<InvalidOperationException>(() => writer.GetSpan());
        }

        [Fact]
        public void GetMemoryAfterCompleteThrows()
        {
            PipeWriter writer = PipeWriter.Create(new MemoryStream());
            writer.WriteEmpty(10);
            writer.Complete();

            Assert.Throws<InvalidOperationException>(() => writer.GetMemory());
        }

        [Fact]
        public void OnReaderCompletedNoops()
        {
            bool fired = false;
            PipeWriter writer = PipeWriter.Create(Stream.Null);
#pragma warning disable CS0618 // Type or member is obsolete
            writer.OnReaderCompleted((_, __) => { fired = true; }, null);
#pragma warning restore CS0618 // Type or member is obsolete
            writer.Complete();
            Assert.False(fired);
        }

        [Fact]
        public void LeaveUnderlyingStreamOpen()
        {
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));

            writer.Complete();

            Assert.True(stream.CanRead);
        }

        [Fact]
        public async Task OperationCancelledExceptionNotSwallowedIfNotThrownFromSpecifiedToken()
        {
            PipeWriter writer = PipeWriter.Create(new ThrowsOperationCanceledExceptionStream());

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await writer.WriteAsync(new byte[1]));
        }

        [Fact]
        public void UnflushedBytesWorks()
        {
            byte[] bytes = Encoding.ASCII.GetBytes("Hello World");
            var stream = new MemoryStream();
            PipeWriter writer = PipeWriter.Create(stream);

            Assert.True(writer.CanGetUnflushedBytes);

            bytes.AsSpan().CopyTo(writer.GetSpan(bytes.Length));
            writer.Advance(bytes.Length);

            Assert.Equal(bytes.Length, writer.UnflushedBytes);

            writer.Complete();

            Assert.Equal(0, writer.UnflushedBytes);
        }

        private class ThrowsOperationCanceledExceptionStream : WriteOnlyStream
        {
            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new OperationCanceledException();
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                throw new OperationCanceledException();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                throw new OperationCanceledException();
            }

#if NETCOREAPP
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                throw new OperationCanceledException();
            }
#endif
        }

        private class FlushAsyncAwareStream : WriteOnlyStream
        {
            public bool FlushAsyncCalled { get; set; }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
            }

            public override Task FlushAsync(CancellationToken cancellationToken)
            {
                FlushAsyncCalled = true;
                return base.FlushAsync(cancellationToken);
            }
        }
    }
}

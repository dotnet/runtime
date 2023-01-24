// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines.Tests.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class CopyToAsyncTests
    {
        private static readonly PipeOptions s_testOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

        protected Pipe Pipe { get; set; }
        protected virtual PipeReader PipeReader => Pipe.Reader;

        public CopyToAsyncTests()
        {
            Pipe = new Pipe(s_testOptions);
        }

        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullDestination()
        {
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => PipeReader.CopyToAsync((Stream)null));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => PipeReader.CopyToAsync((PipeWriter)null));
        }

        [Fact]
        public async Task CopyToAsyncThrowsTaskCanceledExceptionForAlreadyCancelledToken()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(() => PipeReader.CopyToAsync(new MemoryStream(), new CancellationToken(true)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CopyToAsyncStreamWorks()
        {
            var messages = new List<byte[]>()
            {
                "Hello World1"u8.ToArray(),
                "Hello World2"u8.ToArray(),
                "Hello World3"u8.ToArray(),
            };

            var stream = new WriteCheckMemoryStream();

            Task task = PipeReader.CopyToAsync(stream);
            foreach (var msg in messages)
            {
                await Pipe.Writer.WriteAsync(msg);
                await stream.WaitForBytesWrittenAsync(msg.Length);
            }
            Pipe.Writer.Complete();
            await task;

            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), stream.ToArray());
        }

        [Fact]
        public async Task CopyToAsyncPipeWriterWorks()
        {
            var messages = new List<byte[]>()
            {
                "Hello World1"u8.ToArray(),
                "Hello World2"u8.ToArray(),
                "Hello World3"u8.ToArray(),
            };

            var targetPipe = new Pipe(s_testOptions);

            Task task = PipeReader.CopyToAsync(targetPipe.Writer);
            foreach (var msg in messages)
            {
                await Pipe.Writer.WriteAsync(msg);
            }
            Pipe.Writer.Complete();
            await task;

            ReadResult readResult = await targetPipe.Reader.ReadAsync();
            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), readResult.Buffer.ToArray());

            targetPipe.Reader.AdvanceTo(readResult.Buffer.End);
            targetPipe.Reader.Complete();
            targetPipe.Writer.Complete();
        }

        [Fact]
        public async Task MultiSegmentWritesWorks()
        {
            using (var pool = new TestMemoryPool())
            {
                Pipe = new Pipe(new PipeOptions(pool: pool, readerScheduler: PipeScheduler.Inline));
                Pipe.Writer.WriteEmpty(4096);
                Pipe.Writer.WriteEmpty(4096);
                Pipe.Writer.WriteEmpty(4096);
                await Pipe.Writer.FlushAsync();
                Pipe.Writer.Complete();

                var stream = new MemoryStream();
                await PipeReader.CopyToAsync(stream);
                PipeReader.Complete();

                Assert.Equal(4096 * 3, stream.Length);
            }
        }

        [Fact]
        public async Task MultiSegmentWritesUntilFailure()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                Pipe = new Pipe(new PipeOptions(pool, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
                Pipe.Writer.WriteEmpty(4096);
                Pipe.Writer.WriteEmpty(4096);
                Pipe.Writer.WriteEmpty(4096);
                await Pipe.Writer.FlushAsync();
                Pipe.Writer.Complete();

                Assert.Equal(3, pool.CurrentlyRentedBlocks);

                var stream = new ThrowAfterNWritesStream(2);
                try
                {
                    await PipeReader.CopyToAsync(stream);
                    Assert.True(false, $"CopyToAsync should have failed, wrote {stream.Writes} times.");
                }
                catch (InvalidOperationException)
                {

                }

                Assert.Equal(2, stream.Writes);

                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(2, pool.DisposedBlocks);

                ReadResult result = await PipeReader.ReadAsync();
                Assert.Equal(4096, result.Buffer.Length);
                PipeReader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task EmptyBufferNotWrittenToStream()
        {
            Pipe.Writer.Complete();

            var stream = new ThrowingStream();
            await PipeReader.CopyToAsync(stream);
            PipeReader.Complete();
        }

        [Fact]
        public async Task CancelingThePendingReadThrowsOperationCancelledException()
        {
            var stream = new MemoryStream();
            Task task = PipeReader.CopyToAsync(stream);

            PipeReader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingBetweenReadsThrowsOperationCancelledException()
        {
            var stream = new WriteCheckMemoryStream { MidWriteCancellation = new CancellationTokenSource() };
            Task task = PipeReader.CopyToAsync(stream, stream.MidWriteCancellation.Token);
            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingViaCancellationTokenThrowsOperationCancelledException()
        {
            var stream = new MemoryStream();
            var cts = new CancellationTokenSource();
            Task task = PipeReader.CopyToAsync(stream, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaCancellationTokenThrowsOperationCancelledException()
        {
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            var cts = new CancellationTokenSource();
            await Pipe.Writer.WriteAsync("Gello World"u8.ToArray());
            Task task = PipeReader.CopyToAsync(targetPipe.Writer, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaPendingFlushThrowsOperationCancelledException()
        {
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            await Pipe.Writer.WriteAsync("Gello World"u8.ToArray());
            Task task = PipeReader.CopyToAsync(targetPipe.Writer);

            targetPipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingStreamViaCancellationTokenThrowsOperationCancelledException()
        {
            var stream = new CancelledWritesStream();
            var cts = new CancellationTokenSource();
            Task task = PipeReader.CopyToAsync(stream, cts.Token);

            // Call write async inline, this will yield when it hits the tcs
            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();

            // Then cancel
            cts.Cancel();

            // Now resume the write which should result in an exception
            stream.WaitForWriteTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task ThrowingFromStreamDoesNotLeavePipeReaderInBrokenState()
        {
            var stream = new ThrowingStream();
            Task task = PipeReader.CopyToAsync(stream);

            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();
            Pipe.Writer.Complete();

            var stream2 = new MemoryStream();
            await PipeReader.CopyToAsync(stream2);
            Assert.Equal(20, stream2.Length);
            PipeReader.Complete();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        public async Task ThrowingFromStreamCallsAdvanceToWithStartOfLastReadResult(int throwAfterNWrites)
        {
            var wrappedPipeReader = new TestPipeReader(PipeReader);

            var stream = new ThrowAfterNWritesStream(throwAfterNWrites);
            Task task = wrappedPipeReader.CopyToAsync(stream);

            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();

            // Write twice for the test case where the stream throws on the second write.
            Pipe.Writer.WriteEmpty(10);
            await Pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            SequencePosition startPosition = wrappedPipeReader.LastReadResult.Buffer.Start;

            Assert.NotNull(startPosition.GetObject());
            Assert.True(startPosition.Equals(wrappedPipeReader.LastConsumed));
            Assert.True(startPosition.Equals(wrappedPipeReader.LastExamined));
        }

        [Fact]
        public async Task CopyToAsyncStreamCopiesRemainderAfterReadingSome()
        {
            byte[] buffer = "Hello World"u8.ToArray();
            await Pipe.Writer.WriteAsync(buffer);
            Pipe.Writer.Complete();

            var result = await PipeReader.ReadAsync();
            Assert.Equal(result.Buffer.ToArray(), buffer);
            // Consume Hello
            PipeReader.AdvanceTo(result.Buffer.GetPosition(5));

            var ms = new MemoryStream();
            await PipeReader.CopyToAsync(ms);

            Assert.Equal(buffer.AsMemory(5).ToArray(), ms.ToArray());
        }

        [Fact]
        public async Task CopyToAsyncPipeWriterCopiesRemainderAfterReadingSome()
        {
            byte[] buffer = "Hello World"u8.ToArray();
            await Pipe.Writer.WriteAsync(buffer);
            Pipe.Writer.Complete();

            var result = await PipeReader.ReadAsync();
            Assert.Equal(result.Buffer.ToArray(), buffer);
            // Consume Hello
            PipeReader.AdvanceTo(result.Buffer.GetPosition(5));

            var ms = new MemoryStream();
            await PipeReader.CopyToAsync(PipeWriter.Create(ms));

            Assert.Equal(buffer.AsMemory(5).ToArray(), ms.ToArray());
        }
    }
}

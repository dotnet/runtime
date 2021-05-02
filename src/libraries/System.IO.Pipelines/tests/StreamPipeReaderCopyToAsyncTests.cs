// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    public class StreamPipeReaderCopyToAsyncTests
    {
        private static readonly StreamPipeReaderOptions s_testOptions = default;
        private static readonly PipeOptions s_testPipeOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);

        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullDestination()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => pipeReader.CopyToAsync((Stream)null));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => pipeReader.CopyToAsync((PipeWriter)null));
        }

        [Fact]
        public async Task CopyToAsyncThrowsTaskCanceledExceptionForAlreadyCancelledToken()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            await Assert.ThrowsAsync<TaskCanceledException>(() => pipeReader.CopyToAsync(new MemoryStream(), new CancellationToken(true)));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CopyToAsyncStreamWorks()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new WriteCheckMemoryStream();

            Task task = pipeReader.CopyToAsync(stream);
            foreach (var msg in messages)
            {
                await pipe.Writer.WriteAsync(msg);
                await stream.WaitForBytesWrittenAsync(msg.Length);
            }
            pipe.Writer.Complete();
            await task;

            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), stream.ToArray());
        }

        [Fact]
        public async Task CopyToAsyncPipeWriterWorks()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var targetPipe = new Pipe(s_testPipeOptions);

            Task task = pipeReader.CopyToAsync(targetPipe.Writer);
            foreach (var msg in messages)
            {
                await pipe.Writer.WriteAsync(msg);
            }
            pipe.Writer.Complete();
            await task;

            ReadResult readResult = await targetPipe.Reader.ReadAsync();
            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), readResult.Buffer.ToArray());

            targetPipe.Reader.AdvanceTo(readResult.Buffer.End);
            targetPipe.Reader.Complete();
            targetPipe.Writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CopyToAsyncStreamWorksWithBufferedSegments()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);

            foreach (var msg in messages)
            {
                await pipe.Writer.WriteAsync(msg);
            }
            pipe.Writer.Complete();

            byte[] expected = messages.SelectMany(msg => msg).ToArray();

            var readResult = await pipeReader.ReadAsync();
            Assert.Equal(expected, readResult.Buffer.ToArray());

            var stream = new MemoryStream();
            await pipeReader.CopyToAsync(stream);
            Assert.Equal(expected, stream.ToArray());
        }

        [Fact]
        public async Task CopyToAsyncPipeWriterWorksWithBufferedSegments()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var targetPipe = new Pipe(s_testPipeOptions);

            foreach (var msg in messages)
            {
                await pipe.Writer.WriteAsync(msg);
            }
            pipe.Writer.Complete();

            byte[] expected = messages.SelectMany(msg => msg).ToArray();

            var readResult = await pipeReader.ReadAsync();
            Assert.Equal(expected, readResult.Buffer.ToArray());

            await pipeReader.CopyToAsync(targetPipe.Writer);

            readResult = await targetPipe.Reader.ReadAsync();
            Assert.Equal(expected, readResult.Buffer.ToArray());

            targetPipe.Reader.AdvanceTo(readResult.Buffer.End);
            targetPipe.Reader.Complete();
            targetPipe.Writer.Complete();
        }

        [Fact]
        public async Task MultiSegmentWritesWorks()
        {
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool, readerScheduler: PipeScheduler.Inline));
                var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                var stream = new MemoryStream();
                await pipeReader.CopyToAsync(stream);
                pipeReader.Complete();

                Assert.Equal(4096 * 3, stream.Length);
            }
        }

        [Fact]
        public async Task MultiSegmentWritesUntilFailure()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var pipe = new Pipe(new PipeOptions(pool, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
                var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                Assert.Equal(3, pool.CurrentlyRentedBlocks);

                var stream = new ThrowAfterNWritesStream(2);
                try
                {
                    await pipeReader.CopyToAsync(stream);
                    Assert.True(false, $"CopyToAsync should have failed, wrote {stream.Writes} times.");
                }
                catch (InvalidOperationException)
                {

                }

                Assert.Equal(2, stream.Writes);

                //Assert.Equal(1, pool.CurrentlyRentedBlocks);
                //Assert.Equal(2, pool.DisposedBlocks);

                ReadResult result = await pipeReader.ReadAsync();
                Assert.Equal(4096, result.Buffer.Length);
                pipe.Reader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task EmptyBufferNotWrittenToStream()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            pipe.Writer.Complete();

            var stream = new ThrowingStream();
            await pipeReader.CopyToAsync(stream);
            pipeReader.Complete();
        }

        [Fact]
        public async Task CancelingThePendingReadThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new MemoryStream();
            Task task = pipeReader.CopyToAsync(stream);

            pipeReader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingBetweenReadsThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new WriteCheckMemoryStream { MidWriteCancellation = new CancellationTokenSource() };
            Task task = pipeReader.CopyToAsync(stream, stream.MidWriteCancellation.Token);
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new MemoryStream();
            var cts = new CancellationTokenSource();
            Task task = pipeReader.CopyToAsync(stream, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            var cts = new CancellationTokenSource();
            await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes("Gello World"));
            Task task = pipeReader.CopyToAsync(targetPipe.Writer, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaPendingFlushThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            await pipe.Writer.WriteAsync(Encoding.ASCII.GetBytes("Gello World"));
            Task task = pipeReader.CopyToAsync(targetPipe.Writer);

            targetPipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingStreamViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new CancelledWritesStream();
            var cts = new CancellationTokenSource();
            Task task = pipeReader.CopyToAsync(stream, cts.Token);

            // Call write async inline, this will yield when it hits the tcs
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            // Then cancel
            cts.Cancel();

            // Now resume the write which should result in an exception
            stream.WaitForWriteTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task ThrowingFromStreamDoesNotLeavePipeReaderInBrokenState()
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var stream = new ThrowingStream();
            Task task = pipeReader.CopyToAsync(stream);

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            var stream2 = new MemoryStream();
            await pipeReader.CopyToAsync(stream2);
            Assert.Equal(20, stream2.Length);
            pipeReader.Complete();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        public async Task ThrowingFromStreamCallsAdvanceToWithStartOfLastReadResult(int throwAfterNWrites)
        {
            var pipe = new Pipe(s_testPipeOptions);
            var pipeReader = PipeReader.Create(pipe.Reader.AsStream(), s_testOptions);
            var wrappedPipeReader = new TestPipeReader(pipeReader);

            var stream = new ThrowAfterNWritesStream(throwAfterNWrites);
            Task task = wrappedPipeReader.CopyToAsync(stream);

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            // Write twice for the test case where the stream throws on the second write.
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            SequencePosition startPosition = wrappedPipeReader.LastReadResult.Buffer.Start;

            Assert.NotNull(startPosition.GetObject());
            Assert.True(startPosition.Equals(wrappedPipeReader.LastConsumed));
            Assert.True(startPosition.Equals(wrappedPipeReader.LastExamined));
        }
    }
}

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
    public class StreamPipeReaderCopyToAsyncTests
    {
        private static readonly StreamPipeReaderOptions s_testOptions = default;
        private static readonly PipeOptions s_testPipeOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);


        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullDestination()
        {
            var pipeReader = PipeReader.Create(new MemoryStream(), s_testOptions);
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => pipeReader.CopyToAsync((Stream)null));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("destination", () => pipeReader.CopyToAsync((PipeWriter)null));
        }

        [Fact]
        public async Task CopyToAsyncThrowsTaskCanceledExceptionForAlreadyCancelledToken()
        {
            var pipeReader = PipeReader.Create(new MemoryStream(), s_testOptions);
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

            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var stream = new WriteCheckMemoryStream();

            Task task = pipeReader.CopyToAsync(stream);
            foreach (var msg in messages)
            {
                await ms.WriteAsync(msg);
                await stream.WaitForBytesWrittenAsync(msg.Length);
            }
            ms.Dispose();
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

            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var targetPipe = new Pipe(s_testPipeOptions);

            Task task = pipeReader.CopyToAsync(targetPipe.Writer);
            foreach (var msg in messages)
            {
                await ms.WriteAsync(msg);
            }
            ms.Dispose();
            await task;

            ReadResult readResult = await targetPipe.Reader.ReadAsync();
            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), readResult.Buffer.ToArray());

            targetPipe.Reader.AdvanceTo(readResult.Buffer.End);
            targetPipe.Reader.Complete();
            targetPipe.Writer.Complete();
        }

        [Fact]
        public async Task CopyToAsyncPipeWriterResume()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var targetPipe = new Pipe(s_testPipeOptions);
            targetPipe.Reader.Complete();
            Task task = pipeReader.CopyToAsync(targetPipe.Writer);
            foreach (var msg in messages)
            {
                await ms.WriteAsync(msg);
            }
            ms.Dispose();
            await task;

            var resumePipe = new Pipe(s_testPipeOptions);
            await pipeReader.CopyToAsync(resumePipe.Writer);

            ReadResult readResult = await resumePipe.Reader.ReadAsync();
            Assert.Equal(messages.SelectMany(msg => msg).ToArray(), readResult.Buffer.ToArray());

            resumePipe.Reader.AdvanceTo(readResult.Buffer.End);
            resumePipe.Reader.Complete();
            resumePipe.Writer.Complete();
        }

        [Fact]
        public async Task MultiSegmentWritesWorks()
        {
            using (var pool = new TestMemoryPool())
            {
                MemoryStream ms = new MemoryStream();
                var pipeReader = PipeReader.Create(ms, new StreamPipeReaderOptions(pool: pool));
                byte[] buffer = new byte[4096];
                ms.Write(buffer);
                ms.Write(buffer);
                ms.Write(buffer);
                await ms.FlushAsync();
                ms.Dispose();

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
                MemoryStream ms = new MemoryStream();
                var pipeReader = PipeReader.Create(ms, new StreamPipeReaderOptions(pool: pool));
                byte[] buffer = new byte[4096];
                ms.Write(buffer);
                ms.Write(buffer);
                ms.Write(buffer);
                await ms.FlushAsync();
                ms.Dispose();

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

                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(2, pool.DisposedBlocks);

                ReadResult result = await pipeReader.ReadAsync();
                Assert.Equal(4096, result.Buffer.Length);
                pipeReader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task EmptyBufferNotWrittenToStream()
        {
            var pipeReader = PipeReader.Create(new MemoryStream(), s_testOptions);

            var stream = new ThrowingStream();
            await pipeReader.CopyToAsync(stream);
            pipeReader.Complete();
        }

        [Fact]
        public async Task CancelingThePendingReadThrowsOperationCancelledException()
        {
            var pipeReader = PipeReader.Create(new MemoryStream(), s_testOptions);
            var stream = new MemoryStream();
            Task task = pipeReader.CopyToAsync(stream);

            pipeReader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingBetweenReadsThrowsOperationCancelledException()
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var stream = new WriteCheckMemoryStream { MidWriteCancellation = new CancellationTokenSource() };
            Task task = pipeReader.CopyToAsync(stream, stream.MidWriteCancellation.Token);
            ms.Write(new byte[10]);
            await ms.FlushAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipeReader = PipeReader.Create(new MemoryStream(), s_testOptions);
            var stream = new MemoryStream();
            var cts = new CancellationTokenSource();
            Task task = pipeReader.CopyToAsync(stream, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaCancellationTokenThrowsOperationCancelledException()
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            var cts = new CancellationTokenSource();
            await ms.WriteAsync(Encoding.ASCII.GetBytes("Hello World"));
            Task task = pipeReader.CopyToAsync(targetPipe.Writer, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingPipeWriterViaPendingFlushThrowsOperationCancelledException()
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            // This should make the write call pause
            var targetPipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1, resumeWriterThreshold: 1));
            await ms.WriteAsync(Encoding.ASCII.GetBytes("Gello World"));
            Task task = pipeReader.CopyToAsync(targetPipe.Writer);

            targetPipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancelingStreamViaCancellationTokenThrowsOperationCancelledException()
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var stream = new CancelledWritesStream();
            var cts = new CancellationTokenSource();
            Task task = pipeReader.CopyToAsync(stream, cts.Token);

            // Call write async inline, this will yield when it hits the tcs
            ms.Write(new byte[10]);
            await ms.FlushAsync();

            // Then cancel
            cts.Cancel();

            // Now resume the write which should result in an exception
            stream.WaitForWriteTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task ThrowingFromStreamDoesNotLeavePipeReaderInBrokenState()
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var stream = new ThrowingStream();
            Task task = pipeReader.CopyToAsync(stream);

            ms.Write(new byte[10]);
            await ms.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            ms.Write(new byte[10]);
            await ms.FlushAsync();
            ms.Dispose();

            ReadResult result = await pipeReader.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.Equal(20, result.Buffer.Length);
            pipeReader.Complete();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(1)]
        public async Task ThrowingFromStreamCallsAdvanceToWithStartOfLastReadResult(int throwAfterNWrites)
        {
            MemoryStream ms = new MemoryStream();
            var pipeReader = PipeReader.Create(ms, s_testOptions);
            var wrappedPipeReader = new TestPipeReader(pipeReader);

            var stream = new ThrowAfterNWritesStream(throwAfterNWrites);
            Task task = wrappedPipeReader.CopyToAsync(stream);

            ms.Write(new byte[10]);
            await ms.FlushAsync();

            // Write twice for the test case where the stream throws on the second write.
            ms.Write(new byte[10]);
            await ms.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            SequencePosition startPosition = wrappedPipeReader.LastReadResult.Buffer.Start;

            Assert.NotNull(startPosition.GetObject());
            Assert.True(startPosition.Equals(wrappedPipeReader.LastConsumed));
            Assert.True(startPosition.Equals(wrappedPipeReader.LastExamined));
        }

        private class ThrowingStream : ThrowAfterNWritesStream
        {
            public ThrowingStream() : base(0)
            {
            }
        }

        private class TestPipeReader : PipeReader
        {
            private readonly PipeReader _inner;


            public TestPipeReader(PipeReader inner)
            {
                _inner = inner;
            }

            public ReadResult LastReadResult { get; private set; }
            public SequencePosition LastConsumed { get; private set; }
            public SequencePosition LastExamined { get; private set; }

            public override void AdvanceTo(SequencePosition consumed)
            {
                LastConsumed = consumed;
                LastExamined = consumed;
                _inner.AdvanceTo(consumed);
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                LastConsumed = consumed;
                LastExamined = examined;
                _inner.AdvanceTo(consumed);
            }

            public override void CancelPendingRead()
            {
                _inner.CancelPendingRead();
            }

            public override void Complete(Exception exception = null)
            {
                _inner.Complete(exception);
            }

            public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                LastReadResult = await _inner.ReadAsync(cancellationToken);
                return LastReadResult;
            }

            public override bool TryRead(out ReadResult result)
            {
                if (_inner.TryRead(out result))
                {
                    LastReadResult = result;
                    return true;
                }

                return false;
            }
        }
    }
}

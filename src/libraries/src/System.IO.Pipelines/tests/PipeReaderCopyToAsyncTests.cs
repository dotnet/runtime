// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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

        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullDestination()
        {
            var pipe = new Pipe(s_testOptions);
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => pipe.Reader.CopyToAsync(null));
            Assert.Equal("destination", ex.ParamName);
        }

        [Fact]
        public async Task CopyToAsyncThrowsTaskCanceledExceptionForAlreadyCancelledToken()
        {
            var pipe = new Pipe(s_testOptions);
            await Assert.ThrowsAsync<TaskCanceledException>(() => pipe.Reader.CopyToAsync(new MemoryStream(), new CancellationToken(true)));
        }

        [Fact]
        public async Task CopyToAsyncWorks()
        {
            var messages = new List<byte[]>()
            {
                Encoding.UTF8.GetBytes("Hello World1"),
                Encoding.UTF8.GetBytes("Hello World2"),
                Encoding.UTF8.GetBytes("Hello World3"),
            };

            var pipe = new Pipe(s_testOptions);
            var stream = new WriteCheckMemoryStream();

            Task task = pipe.Reader.CopyToAsync(stream);
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
        public async Task MultiSegmentWritesWorks()
        {
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool, readerScheduler: PipeScheduler.Inline));
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                var stream = new MemoryStream();
                await pipe.Reader.CopyToAsync(stream);
                pipe.Reader.Complete();

                Assert.Equal(4096 * 3, stream.Length);
            }
        }

        [Fact]
        public async Task MultiSegmentWritesUntilFailure()
        {
            using (var pool = new DisposeTrackingBufferPool())
            {
                var pipe = new Pipe(new PipeOptions(pool, readerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                Assert.Equal(3, pool.CurrentlyRentedBlocks);

                var stream = new ThrowAfterNWritesStream(2);
                try
                {
                    await pipe.Reader.CopyToAsync(stream);
                    Assert.True(false, $"CopyToAsync should have failed, wrote {stream.Writes} times.");
                }
                catch(InvalidOperationException)
                {
                    
                }

                Assert.Equal(2, stream.Writes);

                Assert.Equal(1, pool.CurrentlyRentedBlocks);
                Assert.Equal(2, pool.DisposedBlocks);

                ReadResult result = await pipe.Reader.ReadAsync();
                Assert.Equal(4096, result.Buffer.Length);
                pipe.Reader.Complete();

                Assert.Equal(0, pool.CurrentlyRentedBlocks);
                Assert.Equal(3, pool.DisposedBlocks);
            }
        }

        [Fact]
        public async Task EmptyBufferNotWrittenToStream()
        {
            var pipe = new Pipe(s_testOptions);
            pipe.Writer.Complete();

            var stream = new ThrowingStream();
            await pipe.Reader.CopyToAsync(stream);
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task CancelingThePendingReadThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testOptions);
            var stream = new MemoryStream();
            Task task = pipe.Reader.CopyToAsync(stream);

            pipe.Reader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingBetweenReadsThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testOptions);
            var stream = new WriteCheckMemoryStream { MidWriteCancellation = new CancellationTokenSource() };
            Task task = pipe.Reader.CopyToAsync(stream, stream.MidWriteCancellation.Token);
            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testOptions);
            var stream = new MemoryStream();
            var cts = new CancellationTokenSource();
            Task task = pipe.Reader.CopyToAsync(stream, cts.Token);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);
        }

        [Fact]
        public async Task CancelingStreamViaCancellationTokenThrowsOperationCancelledException()
        {
            var pipe = new Pipe(s_testOptions);
            var stream = new CancelledWritesStream();
            var cts = new CancellationTokenSource();
            Task task = pipe.Reader.CopyToAsync(stream, cts.Token);

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
            var pipe = new Pipe(s_testOptions);
            var stream = new ThrowingStream();
            Task task = pipe.Reader.CopyToAsync(stream);

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();

            await Assert.ThrowsAsync<InvalidOperationException>(() => task);

            pipe.Writer.WriteEmpty(10);
            await pipe.Writer.FlushAsync();
            pipe.Writer.Complete();

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.True(result.IsCompleted);
            Assert.Equal(20, result.Buffer.Length);
            pipe.Reader.Complete();
        }
        private class ThrowingStream : ThrowAfterNWritesStream
        {
            public ThrowingStream() : base(0)
            {
            }
        }
    }
}

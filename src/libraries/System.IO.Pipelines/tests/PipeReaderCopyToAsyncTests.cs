// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class CopyToAsyncTests
    {
        private static readonly PipeOptions s_testOptions = new PipeOptions(readerScheduler: PipeScheduler.Inline);

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
            var helloBytes = Encoding.UTF8.GetBytes("Hello World");

            var pipe = new Pipe(s_testOptions);
            await pipe.Writer.WriteAsync(helloBytes);
            pipe.Writer.Complete();

            var stream = new MemoryStream();
            await pipe.Reader.CopyToAsync(stream);
            pipe.Reader.Complete();

            Assert.Equal(helloBytes, stream.ToArray());
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
            using (var pool = new TestMemoryPool())
            {
                var pipe = new Pipe(new PipeOptions(pool: pool, readerScheduler: PipeScheduler.Inline));
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                pipe.Writer.WriteEmpty(4096);
                await pipe.Writer.FlushAsync();
                pipe.Writer.Complete();

                var stream = new ThrowAfterNWritesStream(2);
                await Assert.ThrowsAsync<InvalidOperationException>(() => pipe.Reader.CopyToAsync(stream));

                ReadResult result = await pipe.Reader.ReadAsync();
                Assert.Equal(4096, result.Buffer.Length);
                pipe.Reader.Complete();
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

        private class CancelledWritesStream : WriteOnlyStream
        {
            public TaskCompletionSource<object> WaitForWriteTask = new TaskCompletionSource<object>(TaskContinuationOptions.RunContinuationsAsynchronously);

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }

            public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                await WaitForWriteTask.Task;

                cancellationToken.ThrowIfCancellationRequested();
            }

#if !netstandard
            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                await WaitForWriteTask.Task;

                cancellationToken.ThrowIfCancellationRequested();
            }
#endif
        }
        private class ThrowingStream : ThrowAfterNWritesStream
        {
            public ThrowingStream() : base(0)
            {
            }
        }

        private class ThrowAfterNWritesStream : WriteOnlyStream
        {
            private readonly int _maxWrites;
            private int _writes;

            public ThrowAfterNWritesStream(int maxWrites)
            {
                _maxWrites = maxWrites;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException();
            }

            public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (_writes >= _maxWrites)
                {
                    throw new InvalidOperationException();
                }
                _writes++;
                return Task.CompletedTask;
            }

#if !netstandard
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_writes >= _maxWrites)
                {
                    throw new InvalidOperationException();
                }
                _writes++;
                return default;
            }
#endif
        }

        private abstract class WriteOnlyStream : Stream
        {
            public override bool CanRead => false;

            public override bool CanSeek => false;

            public override bool CanWrite => true;

            public override long Length => throw new NotSupportedException();

            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

            public override void Flush()
            {
                throw new InvalidOperationException();
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
        }
    }
}

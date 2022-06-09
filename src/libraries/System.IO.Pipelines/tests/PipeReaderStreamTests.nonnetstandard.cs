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
    public class PipeReaderStreamTests
    {
        public delegate Task<int> ReadAsyncDelegate(Stream stream, byte[] data);

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposingPipeReaderStreamCompletesPipeReader(bool dataInPipe)
        {
            var pipe = new Pipe();
            Stream s = pipe.Reader.AsStream();

            if (dataInPipe)
            {
                await pipe.Writer.WriteAsync(new byte[42]);
                await pipe.Writer.FlushAsync();
            }

            var readerCompletedTask = new TaskCompletionSource<bool>();
#pragma warning disable CS0618 // Type or member is obsolete
            pipe.Writer.OnReaderCompleted(delegate { readerCompletedTask.SetResult(true); }, null);
#pragma warning restore CS0618 // Type or member is obsolete

            // Call Dispose{Async} multiple times; all should succeed.
            for (int i = 0; i < 2; i++)
            {
                s.Dispose();
                await s.DisposeAsync();
            }

            // Make sure OnReaderCompleted was invoked.
            await readerCompletedTask.Task;

            // Unable to read after disposing.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await s.ReadAsync(new byte[1]));

            // Writes still work.
            await pipe.Writer.WriteAsync(new byte[1]);
        }

        [Theory]
        [MemberData(nameof(ReadCalls))]
        public async Task ReadingFromPipeReaderStreamReadsFromUnderlyingPipeReader(ReadAsyncDelegate readAsync)
        {
            byte[] helloBytes = "Hello World"u8.ToArray();
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(helloBytes);
            pipe.Writer.Complete();

            var stream = new PipeReaderStream(pipe.Reader, leaveOpen: false);

            var buffer = new byte[1024];
            int read = await readAsync(stream, buffer);

            Assert.Equal(helloBytes, buffer.AsSpan(0, read).ToArray());
            pipe.Reader.Complete();
        }

        [Theory]
        [MemberData(nameof(ReadCalls))]
        public async Task AsStreamReturnsPipeReaderStream(ReadAsyncDelegate readAsync)
        {
            byte[] helloBytes = "Hello World"u8.ToArray();
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(helloBytes);
            pipe.Writer.Complete();

            Stream stream = pipe.Reader.AsStream();

            var buffer = new byte[1024];
            int read = await readAsync(stream, buffer);

            Assert.Equal(helloBytes, buffer.AsSpan(0, read).ToArray());
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task ReadingWithSmallerBufferWorks()
        {
            byte[] helloBytes = "Hello World"u8.ToArray();
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(helloBytes);
            pipe.Writer.Complete();

            Stream stream = pipe.Reader.AsStream();

            var buffer = new byte[5];
            int read = await stream.ReadAsync(buffer);

            Assert.Equal(5, read);
            Assert.Equal(helloBytes.AsSpan(0, 5).ToArray(), buffer);

            buffer = new byte[3];
            read = await stream.ReadAsync(buffer);

            Assert.Equal(3, read);
            Assert.Equal(helloBytes.AsSpan(5, 3).ToArray(), buffer);

            // Verify that the buffer is partially consumed and we can read the rest from the PipeReader directly
            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.Equal(helloBytes.AsSpan(8).ToArray(), result.Buffer.ToArray());
            pipe.Reader.AdvanceTo(result.Buffer.End);

            pipe.Reader.Complete();
        }

        [Fact]
        public async Task EndOfPipeReaderReturnsZeroBytesFromReadAsync()
        {
            var pipe = new Pipe();
            Memory<byte> memory = pipe.Writer.GetMemory();
            pipe.Writer.Advance(5);
            pipe.Writer.Complete();

            Stream stream = pipe.Reader.AsStream();

            var buffer = new byte[5];
            var read = await stream.ReadAsync(buffer);

            Assert.Equal(5, read);

            read = await stream.ReadAsync(buffer);

            // Read again to make sure it always returns 0
            Assert.Equal(0, read);

            pipe.Reader.Complete();
        }

        [Fact]
        public async Task BuggyPipeReaderImplementationThrows()
        {
            var pipeReader = new BuggyPipeReader();

            Stream stream = pipeReader.AsStream();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await stream.ReadAsync(new byte[5]));
        }

        [Fact]
        public async Task WritingToPipeReaderStreamThrowsNotSupported()
        {
            var pipe = new Pipe();

            Stream stream = pipe.Reader.AsStream();
            Assert.False(stream.CanWrite);
            Assert.False(stream.CanSeek);
            Assert.True(stream.CanRead);
            Assert.Throws<NotSupportedException>(() => { long length = stream.Length; });
            Assert.Throws<NotSupportedException>(() => { long position = stream.Position; });
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.Write(new byte[10], 0, 10));
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[10], 0, 10));
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(new byte[10]).AsTask());

            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancellingPendingReadThrowsOperationCancelledException()
        {
            var pipe = new Pipe();

            Stream stream = pipe.Reader.AsStream();
            ValueTask<int> task = stream.ReadAsync(new byte[1024]);
            Assert.False(task.IsCompleted);

            pipe.Reader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CanReadAfterCancellingPendingRead()
        {
            var pipe = new Pipe();

            Stream stream = pipe.Reader.AsStream();
            ValueTask<int> task = stream.ReadAsync(new byte[1024]);
            Assert.False(task.IsCompleted);

            pipe.Reader.CancelPendingRead();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            pipe.Writer.Complete();

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.True(result.IsCompleted);

            pipe.Reader.Complete();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task CancellationTokenFlowsToUnderlyingPipeReader()
        {
            var pipe = new Pipe();

            Stream stream = pipe.Reader.AsStream();
            var cts = new CancellationTokenSource();
            ValueTask<int> task = stream.ReadAsync(new byte[1024], cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task DefaultPipeReaderImplementationReturnsPipeReaderStream()
        {
            var pipeReader = new TestPipeReader();
            Stream stream = pipeReader.AsStream();

            await stream.ReadAsync(new byte[10]);

            Assert.True(pipeReader.ReadCalled);
            Assert.True(pipeReader.AdvanceToCalled);
        }

        [Fact]
        public void AsStreamReturnsSameInstance()
        {
            var pipeReader = new TestPipeReader();
            Stream stream = pipeReader.AsStream();

            Assert.Same(stream, pipeReader.AsStream());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task PipeWriterStreamProducesToConsumingPipeReaderStream()
        {
            var pipe = new Pipe();

            int consumedSum = 0, producedSum = 0;
            Task consumer = Task.Run(() =>
            {
                using (Stream reader = pipe.Reader.AsStream())
                {
                    int b;
                    while ((b = reader.ReadByte()) != -1)
                    {
                        consumedSum += b;
                    }

                    Assert.Equal(-1, reader.ReadByte());
                }
            });

            var rand = new Random();
            using (Stream writer = pipe.Writer.AsStream())
            {
                for (int i = 0; i < 1000; i++)
                {
                    byte b = (byte)rand.Next(256);
                    writer.WriteByte(b);
                    producedSum += b;
                }
            }

            await consumer;
            Assert.Equal(producedSum, consumedSum);
        }

        [Fact]
        public void AsStreamDoNotCompleteReader()
        {
            var pipeReader = new NotImplementedPipeReader();

            // would throw in Complete if it was actually invoked
            pipeReader.AsStream(leaveOpen: true).Dispose();
        }

        public class BuggyPipeReader : PipeReader
        {
            public override void AdvanceTo(SequencePosition consumed)
            {

            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {

            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                // Returns a ReadResult with no buffer and with IsCompleted and IsCancelled false
                return default;
            }

            public override bool TryRead(out ReadResult result)
            {
                throw new NotImplementedException();
            }
        }

        public class NotImplementedPipeReader : PipeReader
        {
            public override void AdvanceTo(SequencePosition consumed) => throw new NotImplementedException();
            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => throw new NotImplementedException();
            public override void CancelPendingRead() => throw new NotImplementedException();
            public override void Complete(Exception exception = null) => throw new NotImplementedException();
            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public override bool TryRead(out ReadResult result) => throw new NotImplementedException();
        }

        public class TestPipeReader : PipeReader
        {
            public bool ReadCalled { get; set; }
            public bool AdvanceToCalled { get; set; }

            public override void AdvanceTo(SequencePosition consumed)
            {
                AdvanceToCalled = true;
            }

            public override void AdvanceTo(SequencePosition consumed, SequencePosition examined)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingRead()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
            {
                ReadCalled = true;
                return new ValueTask<ReadResult>(new ReadResult(default, isCanceled: false, isCompleted: true));
            }

            public override bool TryRead(out ReadResult result)
            {
                throw new NotImplementedException();
            }
        }

        public static IEnumerable<object[]> ReadCalls
        {
            get
            {
                ReadAsyncDelegate readArrayAsync = (stream, data) =>
                {
                    return stream.ReadAsync(data, 0, data.Length);
                };

                ReadAsyncDelegate readMemoryAsync = async (stream, data) =>
                {
                    return await stream.ReadAsync(data);
                };

                ReadAsyncDelegate readMemoryAsyncWithThreadHop = async (stream, data) =>
                {
                    await Task.Yield();

                    return await stream.ReadAsync(data);
                };

                ReadAsyncDelegate readArraySync = (stream, data) =>
                {
                    return Task.FromResult(stream.Read(data, 0, data.Length));
                };

                ReadAsyncDelegate readSpanSync = (stream, data) =>
                {
                    return Task.FromResult(stream.Read(data));
                };

                yield return new object[] { readArrayAsync };
                yield return new object[] { readMemoryAsync };
                if (PlatformDetection.IsThreadingSupported)
                {
                    yield return new object[] { readMemoryAsyncWithThreadHop };
                }
                yield return new object[] { readArraySync };
                yield return new object[] { readSpanSync };
            }
        }
    }
}

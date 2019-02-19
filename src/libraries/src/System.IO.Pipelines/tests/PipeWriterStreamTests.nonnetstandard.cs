using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeWriterStreamTests
    {
        public delegate Task WriteAsyncDelegate(Stream stream, byte[] data);

        [Theory]
        [MemberData(nameof(WriteCalls))]
        public async Task WritingToPipeStreamWritesToUnderlyingPipeWriter(WriteAsyncDelegate writeAsync)
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var pipe = new Pipe();
            var stream = new PipeWriterStream(pipe.Writer);

            await writeAsync(stream, helloBytes);

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.Equal(helloBytes, result.Buffer.ToArray());
            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Theory]
        [MemberData(nameof(WriteCalls))]
        public async Task AsStreamReturnsPipeWriterStream(WriteAsyncDelegate writeAsync)
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var pipe = new Pipe();
            Stream stream = pipe.Writer.AsStream();

            await writeAsync(stream, helloBytes);

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.Equal(helloBytes, result.Buffer.ToArray());
            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Fact]
        public async Task FlushAsyncFlushesBufferedData()
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var pipe = new Pipe();

            Memory<byte> memory = pipe.Writer.GetMemory();
            helloBytes.CopyTo(memory);
            pipe.Writer.Advance(helloBytes.Length);

            Stream stream = pipe.Writer.AsStream();
            await stream.FlushAsync();

            ReadResult result = await pipe.Reader.ReadAsync();
            Assert.Equal(helloBytes, result.Buffer.ToArray());
            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Fact]
        public async Task ReadingFromPipeWriterStreamThrowsNotSupported()
        {
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var pipe = new Pipe();

            Stream stream = pipe.Writer.AsStream();
            Assert.True(stream.CanWrite);
            Assert.False(stream.CanSeek);
            Assert.False(stream.CanRead);
            Assert.Throws<NotSupportedException>(() => { long length = stream.Length; });
            Assert.Throws<NotSupportedException>(() => { long position = stream.Position; });
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.Read(new byte[10], 0, 10));
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(new byte[10], 0, 10));
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.ReadAsync(new byte[10]).AsTask());
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.CopyToAsync(Stream.Null));

            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Fact]
        public async Task CancellingPendingFlushThrowsOperationCancelledException()
        {
            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 0));
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");

            Stream stream = pipe.Writer.AsStream();
            ValueTask task = stream.WriteAsync(helloBytes);
            Assert.False(task.IsCompleted);

            pipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task CancellationTokenFlowsToUnderlyingPipeWriter()
        {
            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 0));
            byte[] helloBytes = Encoding.ASCII.GetBytes("Hello World");

            Stream stream = pipe.Writer.AsStream();
            var cts = new CancellationTokenSource();
            ValueTask task = stream.WriteAsync(helloBytes, cts.Token);
            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task DefaultPipeWriterImplementationReturnsPipeWriterStream()
        {
            var pipeWriter = new TestPipeWriter();
            Stream stream = pipeWriter.AsStream();

            await stream.WriteAsync(new byte[10]);

            Assert.True(pipeWriter.WriteAsyncCalled);

            await stream.FlushAsync();

            Assert.True(pipeWriter.FlushCalled);
        }

        public class TestPipeWriter : PipeWriter
        {
            public bool FlushCalled { get; set; }
            public bool WriteAsyncCalled { get; set; }

            public override void Advance(int bytes)
            {
                throw new NotImplementedException();
            }

            public override void CancelPendingFlush()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                FlushCalled = true;
                return default;
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                throw new NotImplementedException();
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                throw new NotImplementedException();
            }

            public override void OnReaderCompleted(Action<Exception, object> callback, object state)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<FlushResult> WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                WriteAsyncCalled = true;
                return default;
            }
        }

        public static IEnumerable<object[]> WriteCalls
        {
            get
            {
                WriteAsyncDelegate writeArrayAsync = (stream, data) =>
                {
                    return stream.WriteAsync(data, 0, data.Length);
                };

                WriteAsyncDelegate writeMemoryAsync = async (stream, data) =>
                {
                    await stream.WriteAsync(data);
                };

                WriteAsyncDelegate writeArraySync = (stream, data) =>
                {
                    stream.Write(data, 0, data.Length);
                    return Task.CompletedTask;
                };

                WriteAsyncDelegate writeSpanSync = (stream, data) =>
                {
                    stream.Write(data);
                    return Task.CompletedTask;
                };

                yield return new object[] { writeArrayAsync };
                yield return new object[] { writeMemoryAsync };
                yield return new object[] { writeArraySync };
                yield return new object[] { writeSpanSync };
            }
        }
    }
}

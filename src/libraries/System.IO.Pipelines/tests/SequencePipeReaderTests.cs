// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class SequencePipeReaderTests
    {
        [Fact]
        public async Task CanRead()
        {
            var sequence = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(sequence);

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
            var sequence = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(sequence);

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
            var sequence = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes("Hello World"));
            var reader = PipeReader.Create(sequence);

            ReadResult readResult = await reader.ReadAsync();
            ReadOnlySequence<byte> buffer = readResult.Buffer;
            Assert.Equal(11, buffer.Length);
            Assert.True(buffer.IsSingleSegment);
            reader.AdvanceTo(buffer.End);

            Assert.False(reader.TryRead(out readResult));
            reader.Complete();
        }

        [Fact]
        public async Task ReadAsyncAfterReceivingCompletedReadResultDoesNotThrow()
        {
            var sequence = ReadOnlySequence<byte>.Empty;
            PipeReader reader = PipeReader.Create(sequence);
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
        public async Task DataCanBeReadMultipleTimes()
        {
            var helloBytes = Encoding.ASCII.GetBytes("Hello World");
            var sequence = new ReadOnlySequence<byte>(helloBytes);
            PipeReader reader = PipeReader.Create(sequence);


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
            var sequence = new ReadOnlySequence<byte>(Encoding.ASCII.GetBytes(new string('a', 10000)));
            PipeReader reader = PipeReader.Create(sequence);

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
            PipeReader reader = PipeReader.Create(ReadOnlySequence<byte>.Empty);
            await reader.ReadAsync();
            reader.Complete();
        }

        [Fact]
        public async Task AdvanceAfterCompleteThrows()
        {
            PipeReader reader = PipeReader.Create(new ReadOnlySequence<byte>(new byte[100]));
            ReadOnlySequence<byte> buffer = (await reader.ReadAsync()).Buffer;

            reader.Complete();

            Assert.Throws<InvalidOperationException>(() => reader.AdvanceTo(buffer.End));
        }

        [Fact]
        public async Task ThrowsOnReadAfterCompleteReader()
        {
            PipeReader reader = PipeReader.Create(ReadOnlySequence<byte>.Empty);

            reader.Complete();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await reader.ReadAsync());
        }

        [Fact]
        public void TryReadAfterCancelPendingReadReturnsTrue()
        {
            PipeReader reader = PipeReader.Create(ReadOnlySequence<byte>.Empty);

            reader.CancelPendingRead();

            Assert.True(reader.TryRead(out ReadResult result));
            Assert.True(result.IsCanceled);
            reader.AdvanceTo(result.Buffer.End);
            reader.Complete();
        }

        [Fact]
        public async Task ReadAsyncReturnsCanceledIfCanceledBeforeRead()
        {
            var sequence = new ReadOnlySequence<byte>(new byte[10000]);
            PipeReader reader = PipeReader.Create(sequence);

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
            var sequence = new ReadOnlySequence<byte>(new byte[10000]);
            PipeReader reader = PipeReader.Create(sequence);

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
        public void OnWriterCompletedNoops()
        {
            bool fired = false;
            PipeReader reader = PipeReader.Create(ReadOnlySequence<byte>.Empty);
#pragma warning disable CS0618 // Type or member is obsolete
            reader.OnWriterCompleted((_, __) => { fired = true; }, null);
#pragma warning restore CS0618 // Type or member is obsolete
            reader.Complete();
            Assert.False(fired);
        }

        private static async Task<string> ReadFromPipeAsString(PipeReader reader)
        {
            ReadResult readResult = await reader.ReadAsync();
            var result = Encoding.ASCII.GetString(readResult.Buffer.ToArray());
            reader.AdvanceTo(readResult.Buffer.End);
            return result;
        }
    }
}

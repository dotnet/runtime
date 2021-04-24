// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class FlushAsyncTests : PipeTest
    {
        [Fact]
        public void FlushAsync_ReturnsCompletedTaskWhenMaxSizeIfZero()
        {
            PipeWriter writableBuffer = Pipe.Writer.WriteEmpty(1);
            ValueTask<FlushResult> flushTask = writableBuffer.FlushAsync();
            Assert.True(flushTask.IsCompleted);

            writableBuffer = Pipe.Writer.WriteEmpty(1);
            flushTask = writableBuffer.FlushAsync();
            Assert.True(flushTask.IsCompleted);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50959", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsMonoAOT))]
        public async Task FlushAsync_ThrowsIfWriterReaderWithException()
        {
            void ThrowTestException()
            {
                try
                {
                    throw new InvalidOperationException("Reader exception");
                }
                catch (Exception e)
                {
                    Pipe.Reader.Complete(e);
                }
            }

            ThrowTestException();

            InvalidOperationException invalidOperationException =
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Writer.FlushAsync());

            Assert.Equal("Reader exception", invalidOperationException.Message);
            Assert.Contains("ThrowTestException", invalidOperationException.StackTrace);

            invalidOperationException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Writer.FlushAsync());
            Assert.Equal("Reader exception", invalidOperationException.Message);
            Assert.Contains("ThrowTestException", invalidOperationException.StackTrace);

            Assert.Single(Regex.Matches(invalidOperationException.StackTrace, "Pipe.GetFlushResult"));
        }

        [Fact]
        public async Task CallingFlushAsyncMultipleTimesAllowsFirstToComplete()
        {
            Pipe.Writer.WriteEmpty(65);
            Task<FlushResult> flushTask = Pipe.Writer.FlushAsync().AsTask();

            Pipe.Writer.WriteEmpty(1);

            _ = Pipe.Writer.FlushAsync();
            _ = Pipe.Writer.FlushAsync();
            _ = Pipe.Writer.FlushAsync();

            ReadResult result = await Pipe.Reader.ReadAsync();
            Pipe.Reader.AdvanceTo(result.Buffer.End);

            Assert.True(flushTask.IsCompleted);
        }

        [Fact]
        public async Task DoubleFlushAsyncThrows()
        {
            Pipe.Writer.WriteEmpty(65);

            ValueTask<FlushResult> flushResult1 = Pipe.Writer.FlushAsync();
            ValueTask<FlushResult> flushResult2 = Pipe.Writer.FlushAsync();

            var task1 = Assert.ThrowsAsync<InvalidOperationException>(async () => await flushResult1);
            var task2 = Assert.ThrowsAsync<InvalidOperationException>(async () => await flushResult2);

            var exception1 = await task1;
            var exception2 = await task2;

            Assert.Equal("Concurrent reads or writes are not supported.", exception1.Message);
            Assert.Equal("Concurrent reads or writes are not supported.", exception2.Message);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class FlushAsyncCompletionTests : PipeTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50927", TestPlatforms.Android)]
        public void AwaitingFlushAsyncAwaitableTwiceCompletesReaderWithException()
        {
            async Task Await(ValueTask<FlushResult> a)
            {
                await a;
            }

            PipeWriter writeBuffer = Pipe.Writer;
            writeBuffer.Write(new byte[MaximumSizeHigh]);
            ValueTask<FlushResult> awaitable = writeBuffer.FlushAsync();

            Task task1 = Await(awaitable);
            Task task2 = Await(awaitable);

            Assert.True(task1.IsCompleted);
            Assert.True(task1.IsFaulted);
            Assert.Equal("Concurrent reads or writes are not supported.", task1.Exception.InnerExceptions[0].Message);

            Assert.True(task2.IsCompleted);
            Assert.True(task2.IsFaulted);
            Assert.Equal("Concurrent reads or writes are not supported.", task2.Exception.InnerExceptions[0].Message);
        }

        [Fact]
        public async Task CompletingWithExceptionDoesNotAffectState()
        {
            Pipe.Writer.Complete();
            Pipe.Writer.Complete(new Exception());

            var result = await Pipe.Reader.ReadAsync();
            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task CompletingWithExceptionDoesNotAffectFailedState()
        {
            Pipe.Writer.Complete(new InvalidOperationException());
            Pipe.Writer.Complete(new Exception());

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Reader.ReadAsync());
        }

        [Fact]
        public async Task CompletingWithoutExceptionDoesNotAffectState()
        {
            Pipe.Writer.Complete(new InvalidOperationException());
            Pipe.Writer.Complete();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Reader.ReadAsync());
        }
    }
}

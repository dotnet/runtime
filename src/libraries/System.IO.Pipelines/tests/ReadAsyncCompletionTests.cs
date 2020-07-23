// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class ReadAsyncCompletionTests : PipeTest
    {
        [Fact]
        public void AwaitingReadAsyncAwaitableTwiceCompletesWriterWithException()
        {
            async Task Await(ValueTask<ReadResult> a)
            {
                await a;
            }

            ValueTask<ReadResult> awaitable = Pipe.Reader.ReadAsync();

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
            Pipe.Reader.Complete();
            Pipe.Reader.Complete(new Exception());

            var result = await Pipe.Writer.FlushAsync();
            Assert.True(result.IsCompleted);
        }

        [Fact]
        public async Task CompletingWithExceptionDoesNotAffectFailedState()
        {
            Pipe.Reader.Complete(new InvalidOperationException());
            Pipe.Reader.Complete(new Exception());

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Writer.FlushAsync());
        }

        [Fact]
        public async Task CompletingWithoutExceptionDoesNotAffectState()
        {
            Pipe.Reader.Complete(new InvalidOperationException());
            Pipe.Reader.Complete();

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Pipe.Writer.FlushAsync());
        }
    }
}

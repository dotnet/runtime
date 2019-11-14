// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeWriterCopyToAsyncTests
    {
        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullSource()
        {
            var pipe = new Pipe();
            MemoryStream stream = null;
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => stream.CopyToAsync(pipe.Writer));
            Assert.Equal("source", ex.ParamName);
        }

        [Fact]
        public async Task CopyToAsyncThrowsArgumentNullExceptionForNullDestination()
        {
            var stream = new MemoryStream();
            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() => stream.CopyToAsync(null));
            Assert.Equal("destination", ex.ParamName);
        }

        [Fact]
        public async Task CopyToAsyncThrowsTaskCanceledExceptionForAlreadyCancelledToken()
        {
            var pipe = new Pipe();
            await Assert.ThrowsAsync<TaskCanceledException>(() => new MemoryStream().CopyToAsync(pipe.Writer, new CancellationToken(true)));
        }

        [Fact]
        public async Task CopyToAsyncWorks()
        {
            var helloBytes = Encoding.UTF8.GetBytes("Hello World");

            var pipe = new Pipe();
            var stream = new MemoryStream(helloBytes);
            await stream.CopyToAsync(pipe.Writer);

            ReadResult result = await pipe.Reader.ReadAsync();

            Assert.Equal(helloBytes, result.Buffer.ToArray());

            pipe.Reader.AdvanceTo(result.Buffer.End);
            pipe.Reader.Complete();
            pipe.Writer.Complete();
        }

        [Fact]
        public async Task CopyToAsyncCalledMultipleTimesWorks()
        {
            var hello = "Hello World";
            var helloBytes = Encoding.UTF8.GetBytes(hello);
            var expected = Encoding.UTF8.GetBytes(hello + hello + hello);

            var pipe = new Pipe();
            await new MemoryStream(helloBytes).CopyToAsync(pipe.Writer);
            await new MemoryStream(helloBytes).CopyToAsync(pipe.Writer);
            await new MemoryStream(helloBytes).CopyToAsync(pipe.Writer);
            pipe.Writer.Complete();

            ReadResult result = await pipe.Reader.ReadAsync();

            Assert.Equal(expected, result.Buffer.ToArray());

            pipe.Reader.AdvanceTo(result.Buffer.End);
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task StreamCopyToAsyncWorks()
        {
            var helloBytes = Encoding.UTF8.GetBytes("Hello World");

            var pipe = new Pipe();
            var stream = new MemoryStream(helloBytes);
            await stream.CopyToAsync(pipe.Writer);

            ReadResult result = await pipe.Reader.ReadAsync();

            Assert.Equal(helloBytes, result.Buffer.ToArray());

            pipe.Reader.AdvanceTo(result.Buffer.End);
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task CancelingViaCancelPendingFlushThrows()
        {
            var helloBytes = Encoding.UTF8.GetBytes("Hello World");

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: helloBytes.Length - 1, resumeWriterThreshold: 0));
            var stream = new MemoryStream(helloBytes);
            Task task = stream.CopyToAsync(pipe.Writer);

            Assert.False(task.IsCompleted);

            pipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);

            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task CancelingViaCancellationTokenThrows()
        {
            var helloBytes = Encoding.UTF8.GetBytes("Hello World");

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: helloBytes.Length - 1, resumeWriterThreshold: 0));
            var stream = new MemoryStream(helloBytes);
            var cts = new CancellationTokenSource();
            Task task = stream.CopyToAsync(pipe.Writer, cts.Token);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);

            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }

        [Fact]
        public async Task CancelingStreamViaCancellationTokenThrows()
        {
            var pipe = new Pipe();
            var stream = new CancelledReadsStream();
            var cts = new CancellationTokenSource();
            Task task = stream.CopyToAsync(pipe.Writer, cts.Token);

            Assert.False(task.IsCompleted);

            cts.Cancel();

            stream.WaitForReadTask.TrySetResult(null);

            await Assert.ThrowsAsync<OperationCanceledException>(() => task);

            pipe.Writer.Complete();
            pipe.Reader.Complete();
        }
    }
}

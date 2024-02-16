// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class FlushTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void AutoFlushSetTrue()
        {
            // [] Set the autoflush to true
            var sw2 = new StreamWriter(CreateStream());
            sw2.AutoFlush = true;
            Assert.True(sw2.AutoFlush);
        }

        [Fact]
        public void AutoFlushSetFalse()
        {
            // [] Set autoflush to false
            var sw2 = new StreamWriter(CreateStream());
            sw2.AutoFlush = false;
            Assert.False(sw2.AutoFlush);
        }

        [Fact]
        public async Task FlushAsync_Cancelable()
        {
            var sw = new StreamWriter(CreateStream());

            await sw.FlushAsync();
            await sw.FlushAsync(CancellationToken.None);
            await sw.FlushAsync(new CancellationTokenSource().Token);

            var cts = new CancellationTokenSource();
            cts.Cancel();
            Task t = sw.FlushAsync(cts.Token);
            Assert.Equal(TaskStatus.Canceled, t.Status);
            Assert.Equal(cts.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t)).CancellationToken);

            cts = new CancellationTokenSource();
            sw.Write("hello");
            await sw.FlushAsync(cts.Token);

            Stream s = sw.BaseStream;
            s.Position = 0;
            Assert.Equal("hello", new StreamReader(s).ReadToEnd());
        }

        [Fact]
        public async Task FlushAsync_DerivedStreamWriter_NonCancelableFlushAsyncInvoked()
        {
            var sw = new DerivedStreamWriter(CreateStream());
            await sw.FlushAsync(new CancellationTokenSource().Token);
            Assert.True(sw.NonCancelableFlushAsyncInvoked);
        }

        [Fact]
        public async Task FlushAsync_CancelsUnderlyingStreamOperation()
        {
            var sw = new StreamWriter(new WaitUntilCanceledWriteMemoryStream());
            sw.Write("hello");
            var cts = new CancellationTokenSource();
            Task t = sw.FlushAsync(cts.Token);
            Assert.False(t.IsCompleted);
            cts.Cancel();
            Assert.Equal(cts.Token, (await Assert.ThrowsAnyAsync<OperationCanceledException>(() => t)).CancellationToken);
        }

        private sealed class DerivedStreamWriter : StreamWriter
        {
            public bool NonCancelableFlushAsyncInvoked;

            public DerivedStreamWriter(Stream stream) : base(stream) { }

            public override Task FlushAsync()
            {
                NonCancelableFlushAsyncInvoked = true;
                return Task.CompletedTask;
            }
        }

        private sealed class WaitUntilCanceledWriteMemoryStream : MemoryStream
        {
            public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
            {
                return new ValueTask(Task.Delay(-1, cancellationToken));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public partial class PipeResetTests : IDisposable
    {
        public PipeResetTests()
        {
            _pool = new TestMemoryPool();
            _pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false));
        }

        public void Dispose()
        {
            _pipe.Writer.Complete();
            _pipe.Reader.Complete();
            _pool?.Dispose();
        }

        private readonly TestMemoryPool _pool;

        private readonly Pipe _pipe;

        [Fact]
        public async Task ReadsAndWritesAfterReset()
        {
            var source = new byte[] { 1, 2, 3 };

            await _pipe.Writer.WriteAsync(source);
            ReadResult result = await _pipe.Reader.ReadAsync();

            Assert.Equal(source, result.Buffer.ToArray());
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            _pipe.Reader.Complete();
            _pipe.Writer.Complete();

            _pipe.Reset();

            await _pipe.Writer.WriteAsync(source);
            result = await _pipe.Reader.ReadAsync();

            Assert.Equal(source, result.Buffer.ToArray());
            _pipe.Reader.AdvanceTo(result.Buffer.End);
        }

        [Fact]
        public void ResetThrowsIfReaderNotCompleted()
        {
            _pipe.Writer.Complete();
            Assert.Throws<InvalidOperationException>(() => _pipe.Reset());
        }

        [Fact]
        public void ResetThrowsIfWriterNotCompleted()
        {
            _pipe.Reader.Complete();
            Assert.Throws<InvalidOperationException>(() => _pipe.Reset());
        }

        [Fact]
        public void ResetResetsReaderAwaitable()
        {
            _pipe.Reader.Complete();
            _pipe.Writer.Complete();

            _pipe.Reset();

            Assert.False(_pipe.Reader.TryRead(out ReadResult result));
        }
    }
}

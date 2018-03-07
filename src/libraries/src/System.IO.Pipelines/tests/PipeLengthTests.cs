// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeLengthTests : IDisposable
    {
        public PipeLengthTests()
        {
            _pool = new TestMemoryPool();
            _pipe = new Pipe(new PipeOptions(_pool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, pauseWriterThreshold: 0, resumeWriterThreshold: 0, useSynchronizationContext: false));
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
        public async Task ByteByByteTest()
        {
            for (var i = 1; i <= 1024 * 1024; i++)
            {
                _pipe.Writer.GetMemory(100);
                _pipe.Writer.Advance(1);
                await _pipe.Writer.FlushAsync();

                Assert.Equal(i, _pipe.Length);
            }

            await _pipe.Writer.FlushAsync();

            for (int i = 1024 * 1024 - 1; i >= 0; i--)
            {
                ReadResult result = await _pipe.Reader.ReadAsync();
                SequencePosition consumed = result.Buffer.Slice(1).Start;

                Assert.Equal(i + 1, result.Buffer.Length);

                _pipe.Reader.AdvanceTo(consumed, consumed);

                Assert.Equal(i, _pipe.Length);
            }
        }

        [Fact]
        public async Task LengthCorrectAfterAlloc0AdvanceFlush()
        {
            _pipe.Writer.GetMemory(0);
            _pipe.Writer.WriteEmpty(10);
            await _pipe.Writer.FlushAsync();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public async Task LengthCorrectAfterAllocAdvanceFlush()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            await writableBuffer.FlushAsync();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public async Task LengthDecreasedAfterReadAdvanceConsume()
        {
            _pipe.Writer.GetMemory(100);
            _pipe.Writer.Advance(10);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            SequencePosition consumed = result.Buffer.Slice(5).Start;
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.Equal(5, _pipe.Length);
        }

        [Fact]
        public async Task LengthNotChangeAfterReadAdvanceExamine()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            await writableBuffer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            Assert.Equal(10, _pipe.Length);
        }
    }
}

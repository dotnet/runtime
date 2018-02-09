// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class PipeLengthTests : IDisposable
    {
        public PipeLengthTests()
        {
            _pool = new TestMemoryPool();
            _pipe = new Pipe(new PipeOptions(_pool));
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
        public void ByteByByteTest()
        {
            for (var i = 1; i <= 1024 * 1024; i++)
            {
                _pipe.Writer.GetMemory(100);
                _pipe.Writer.Advance(1);
                _pipe.Writer.Commit();

                Assert.Equal(i, _pipe.Length);
            }

            _pipe.Writer.FlushAsync();

            for (int i = 1024 * 1024 - 1; i >= 0; i--)
            {
                ReadResult result = _pipe.Reader.ReadAsync().GetResult();
                SequencePosition consumed = result.Buffer.Slice(1).Start;

                Assert.Equal(i + 1, result.Buffer.Length);

                _pipe.Reader.AdvanceTo(consumed, consumed);

                Assert.Equal(i, _pipe.Length);
            }
        }

        [Fact]
        public void LengthCorrectAfterAlloc0AdvanceCommit()
        {
            _pipe.Writer.GetMemory(0);
            _pipe.Writer.WriteEmpty(10);
            _pipe.Writer.Commit();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public void LengthCorrectAfterAllocAdvanceCommit()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            writableBuffer.Commit();

            Assert.Equal(10, _pipe.Length);
        }

        [Fact]
        public void LengthDecreasedAfterReadAdvanceConsume()
        {
            _pipe.Writer.GetMemory(100);
            _pipe.Writer.Advance(10);
            _pipe.Writer.Commit();
            _pipe.Writer.FlushAsync();

            ReadResult result = _pipe.Reader.ReadAsync().GetResult();
            SequencePosition consumed = result.Buffer.Slice(5).Start;
            _pipe.Reader.AdvanceTo(consumed, consumed);

            Assert.Equal(5, _pipe.Length);
        }

        [Fact]
        public void LengthNotChangeAfterReadAdvanceExamine()
        {
            PipeWriter writableBuffer = _pipe.Writer.WriteEmpty(10);
            writableBuffer.Commit();
            writableBuffer.FlushAsync();

            ReadResult result = _pipe.Reader.ReadAsync().GetResult();
            _pipe.Reader.AdvanceTo(result.Buffer.Start, result.Buffer.End);

            Assert.Equal(10, _pipe.Length);
        }
    }
}

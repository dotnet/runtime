// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeReaderReadAtLeastAsyncTests : ReadAtLeastAsyncTests
    {
        private PipeReader? _pipeReader;
        protected override PipeReader PipeReader => _pipeReader ??= PipeReader.Create(Pipe.Reader.AsStream());

        protected override void SetPipeReaderOptions(MemoryPool<byte>? pool = null, int bufferSize = -1)
        {
            _pipeReader = PipeReader.Create(Pipe.Reader.AsStream(), new StreamPipeReaderOptions(pool, bufferSize));
        }

        private static Func<DisposeTrackingBufferPool> CustomPoolFunc = () => new DisposeTrackingBufferPool();
        public static TheoryData<MemoryPool<byte>?, int, bool, bool> TestData =>
            new TheoryData<MemoryPool<byte>?, int, bool, bool>
            {
                // pool, bufferSize, isSingleSegment, isFromCustomPool
                { default, 1, true, false },
                { default, StreamPipeReaderOptions.DefaultMaxBufferSize, true, false },
                { default, StreamPipeReaderOptions.DefaultMaxBufferSize + 1, false, false },

                { CustomPoolFunc(), 1, true, true },
                { CustomPoolFunc(), TestMemoryPool.DefaultMaxBufferSize, true, true },
                { CustomPoolFunc(), TestMemoryPool.DefaultMaxBufferSize + 1, true, false },
                { CustomPoolFunc(), StreamPipeReaderOptions.DefaultMaxBufferSize, true, false },
                { CustomPoolFunc(), StreamPipeReaderOptions.DefaultMaxBufferSize + 1, false, false },
            };

        [Theory]
        [MemberData(nameof(TestData))]
        public async Task ReadAtLeastAsyncSegmentSizeLessThanMaxBufferSize(DisposeTrackingBufferPool? pool, int bufferSize, bool isSingleSegment, bool isFromCustomPool)
        {
            SetPipeReaderOptions(pool);
            Pipe.Writer.WriteEmpty(bufferSize);
            var task = Pipe.Writer.FlushAsync();
            ReadResult readResult = await PipeReader.ReadAtLeastAsync(bufferSize);
            await task;

            Assert.Equal(isSingleSegment, readResult.Buffer.IsSingleSegment);
            Assert.Equal(isFromCustomPool, (pool?.CurrentlyRentedBlocks ?? 0) != 0);
            Assert.Equal(bufferSize, readResult.Buffer.Length);
        }
    }
}

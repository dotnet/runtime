// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipelines.Tests
{
    public class BufferSegmentPoolTest : IDisposable
    {
        private readonly TestMemoryPool _pool;
        private readonly Pipe _pipe;

        public BufferSegmentPoolTest()
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

        [Fact]
        public async Task BufferSegmentsAreReused()
        {
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();
            object oldSegment = result.Buffer.End.GetObject();

            // This should return the first segment
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            // One block remaining
            Assert.Equal(0, _pipe.Length);

            // This should use the segment that was returned
            _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();
            object newSegment = result.Buffer.End.GetObject();
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            Assert.Same(oldSegment, newSegment);

            Assert.Equal(0, _pipe.Length);
        }

        [Fact]
        public async Task BufferSegmentsPooledUpToThreshold()
        {
            int blockCount = PipeOptions.Default.MaxSegmentPoolSize + 1;

            // Write 256 blocks to ensure they get reused
            for (int i = 0; i < blockCount; i++)
            {
                _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            }

            await _pipe.Writer.FlushAsync();

            ReadResult result = await _pipe.Reader.ReadAsync();

            List<ReadOnlySequenceSegment<byte>> oldSegments = GetSegments(result);

            Assert.Equal(blockCount, oldSegments.Count);

            // This should return them all to the segment pool (256 blocks, the last block will be discarded)
            _pipe.Reader.AdvanceTo(result.Buffer.End);

            for (int i = 0; i < blockCount; i++)
            {
                _pipe.Writer.WriteEmpty(_pool.MaxBufferSize);
            }

            await _pipe.Writer.FlushAsync();

            result = await _pipe.Reader.ReadAsync();

            List<ReadOnlySequenceSegment<byte>> newSegments = GetSegments(result);

            Assert.Equal(blockCount, newSegments.Count);

            _pipe.Reader.AdvanceTo(result.Buffer.End);

            // Assert Pipe.MaxSegmentPoolSize pooled segments
            for (int i = 0; i < PipeOptions.Default.MaxSegmentPoolSize; i++)
            {
                Assert.Same(oldSegments[i], newSegments[PipeOptions.Default.MaxSegmentPoolSize - i - 1]);
            }

            // The last segment shouldn't exist in the new list of segments at all (it should be new)
            Assert.DoesNotContain(oldSegments[256], newSegments);
        }

        private static List<ReadOnlySequenceSegment<byte>> GetSegments(ReadResult result)
        {
            SequenceMarshal.TryGetReadOnlySequenceSegment(
                           result.Buffer,
                           out ReadOnlySequenceSegment<byte> start,
                           out int startIndex,
                           out ReadOnlySequenceSegment<byte> end,
                           out int endIndex);

            var segments = new List<ReadOnlySequenceSegment<byte>>();

            while (start != end.Next)
            {
                segments.Add(start);
                start = start.Next;
            }

            return segments;
        }
    }
}

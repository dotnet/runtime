// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using Xunit;

namespace System.Buffers.Tests
{
    public class BuffersExtensionsTests
    {
        [Fact]
        public void WritingToSingleSegmentBuffer()
        {
            IBufferWriter<byte> bufferWriter = new TestBufferWriterSingleSegment();
            bufferWriter.Write("Hello"u8);
            bufferWriter.Write(" World!"u8);
            Assert.Equal("Hello World!", bufferWriter.ToString());
        }

        [Fact]
        public void WritingToMultiSegmentBuffer()
        {
            var bufferWriter = new TestBufferWriterMultiSegment();
            bufferWriter.Write("Hello"u8);
            bufferWriter.Write(" World!"u8);
            Assert.Equal(12, bufferWriter.Committed.Count);
            Assert.Equal("Hello World!", bufferWriter.ToString());
        }

        [Fact]
        public void WritingEmptyBufferToSingleSegmentEmptyBufferWriterDoesNothing()
        {
            IBufferWriter<byte> bufferWriter = new MultiSegmentArrayBufferWriter<byte>(
                new byte[][] { Array.Empty<byte>() }
            );

            bufferWriter.Write(Array.Empty<byte>()); // This is equivalent to: Span<byte>.Empty.CopyTo(Span<byte>.Empty);
        }

        [Fact]
        public void WritingEmptyBufferToMultipleSegmentEmptyBufferWriterDoesNothing()
        {
            IBufferWriter<byte> bufferWriter = new MultiSegmentArrayBufferWriter<byte>(
                new byte[][] { Array.Empty<byte>(), Array.Empty<byte>() }
            );

            bufferWriter.Write(Array.Empty<byte>());
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(10, 9)]
        public void WritingToTooSmallSingleSegmentBufferFailsWithException(int inputSize, int destinationSize)
        {
            IBufferWriter<byte> bufferWriter = new MultiSegmentArrayBufferWriter<byte>(
                new byte[][] { new byte[destinationSize] }
            );

            Assert.Throws<ArgumentOutOfRangeException>(paramName: "writer", testCode: () => bufferWriter.Write(new byte[inputSize]));
        }

        [Theory]
        [InlineData(10, 2, 2)]
        [InlineData(10, 9, 0)]
        public void WritingToTooSmallMultiSegmentBufferFailsWithException(int inputSize, int firstSegmentSize, int secondSegmentSize)
        {
            IBufferWriter<byte> bufferWriter = new MultiSegmentArrayBufferWriter<byte>(
                new byte[][] {
                    new byte[firstSegmentSize],
                    new byte[secondSegmentSize]
                }
            );

            Assert.Throws<ArgumentOutOfRangeException>(
                paramName: "writer",
                testCode: () => bufferWriter.Write(new byte[inputSize]));
        }

        private class MultiSegmentArrayBufferWriter<T> : IBufferWriter<T>
        {
            private readonly T[][] _segments;
            private int _segmentIndex;

            public MultiSegmentArrayBufferWriter(T[][] segments) => _segments = segments;

            public void Advance(int size)
            {
                if (size != _segments[_segmentIndex].Length)
                    throw new NotSupportedException("By design");

                _segmentIndex++;
            }

            public Memory<T> GetMemory(int sizeHint = 0) => _segmentIndex < _segments.Length ? _segments[_segmentIndex] : Memory<T>.Empty;

            public Span<T> GetSpan(int sizeHint = 0) => _segmentIndex < _segments.Length ? _segments[_segmentIndex] : Span<T>.Empty;
        }

        private class TestBufferWriterSingleSegment : IBufferWriter<byte>
        {
            private byte[] _buffer = new byte[1000];
            private int _written = 0;

            public void Advance(int bytes)
            {
                _written += bytes;
            }

            public Memory<byte> GetMemory(int sizeHint = 0) => _buffer.AsMemory(_written);

            public Span<byte> GetSpan(int sizeHint) => _buffer.AsSpan(_written);

            public override string ToString()
            {
                return Encoding.UTF8.GetString(_buffer.AsSpan(0, _written).ToArray());
            }
        }

        private class TestBufferWriterMultiSegment : IBufferWriter<byte>
        {
            private byte[] _current = new byte[0];
            private List<byte[]> _committed = new List<byte[]>();

            public List<byte[]> Committed => _committed;

            public void Advance(int bytes)
            {
                if (bytes == 0)
                    return;
                _committed.Add(_current.AsSpan(0, bytes).ToArray());
                _current = new byte[0];
            }

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                if (sizeHint == 0)
                    sizeHint = _current.Length + 1;
                if (sizeHint < _current.Length)
                    throw new InvalidOperationException();
                var newBuffer = new byte[sizeHint];
                _current.CopyTo(newBuffer.AsSpan());
                _current = newBuffer;
                return _current;
            }

            public Span<byte> GetSpan(int sizeHint)
            {
                if (sizeHint == 0)
                    sizeHint = _current.Length + 1;
                if (sizeHint < _current.Length)
                    throw new InvalidOperationException();
                var newBuffer = new byte[sizeHint];
                _current.CopyTo(newBuffer.AsSpan());
                _current = newBuffer;
                return _current;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                foreach (byte[] buffer in _committed)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer));
                }
                return builder.ToString();
            }
        }

        [Fact]
        public void StringBuilderAsSequence()
        {
            var random = new Random(218904821);

            // Test on a variety of lengths, at least up to the point of 9 8K chunks = 72K because this is where
            // we start using a different technique for creating the ChunkEnumerator.   200 * 500 = 100K which hits this.
            for (int i = 0; i < 200; i++)
            {
                StringBuilder inBuilder = new StringBuilder();
                for (int j = 0; j < i; j++)
                {
                    // Make some unique strings that are at least 500 bytes long.
                    inBuilder.Append(j);
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz01_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz0123_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz012345_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz012345678_");
                    inBuilder.Append("_abcdefghijklmnopqrstuvwxyz01234567890__Abcdefghijklmnopqrstuvwxyz01234567890__ABcdefghijklmnopqrstuvwxyz01234567890_");
                }

                // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                var sequence = inBuilder.AsSequence();
                var outStr = sequence.ToString();
                Assert.Equal(outStr, inBuilder.ToString());

                // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                sequence = inBuilder.AsSequence(0, inBuilder.Length);
                outStr = sequence.ToString();
                Assert.Equal(outStr, inBuilder.ToString(0, inBuilder.Length));

                // Execute 10 tests with random startIndex and length
                for (var t = 0; t < 10; t++)
                {
                    var startIndex = random.Next(0, inBuilder.Length + 1);
                    var length = random.Next(0, inBuilder.Length - startIndex + 1);

                    // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                    sequence = inBuilder.AsSequence(startIndex, 0);
                    outStr = sequence.ToString();
                    Assert.Equal(outStr, inBuilder.ToString(startIndex, 0));

                    if (length > 0)
                    {
                        // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                        sequence = inBuilder.AsSequence(startIndex, 1);
                        outStr = sequence.ToString();
                        Assert.Equal(outStr, inBuilder.ToString(startIndex, 1));
                    }

                    // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                    sequence = inBuilder.AsSequence(0, length);
                    outStr = sequence.ToString();
                    Assert.Equal(outStr, inBuilder.ToString(0, length));

                    // The strings formed by concatenating the chunks should be the same as the value in the StringBuilder.
                    sequence = inBuilder.AsSequence(startIndex, length);
                    outStr = sequence.ToString();
                    Assert.Equal(outStr, inBuilder.ToString(startIndex, length));
                }
            }
        }

        [Fact]
        public void StringBuilderAsSequence_Invalid()
        {
            var builder = new StringBuilder("Hello");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.AsSequence(-1, 0)); // Start index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.AsSequence(0, -1)); // Length < 0

            AssertExtensions.Throws<ArgumentOutOfRangeException>("startIndex", () => builder.AsSequence(6, 0)); // Length + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.AsSequence(5, 1)); // Length + start index > builder.Length
            AssertExtensions.Throws<ArgumentOutOfRangeException>("length", () => builder.AsSequence(4, 2)); // Length + start index > builder.Length
        }
    }
}

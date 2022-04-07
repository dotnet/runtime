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
            Assert.Equal(12, bufferWriter.Comitted.Count);
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
            private List<byte[]> _commited = new List<byte[]>();

            public List<byte[]> Comitted => _commited;

            public void Advance(int bytes)
            {
                if (bytes == 0)
                    return;
                _commited.Add(_current.AsSpan(0, bytes).ToArray());
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
                foreach (byte[] buffer in _commited)
                {
                    builder.Append(Encoding.UTF8.GetString(buffer));
                }
                return builder.ToString();
            }
        }
    }
}

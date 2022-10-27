// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Formats.Tar.Tests
{
    // Stream that returns `length` amount of bytes with leading and trailing dummy data to verify it was correctly preserved
    // e.g:
    // 0x01, 0x02, 0x03, 0x04, 0x05, 0x00, 0x00, 0x00, 0x00, ...0x00, 0x01, 0x02, 0x03, 0x04, 0x05.
    // or in decimal:
    // 1, 2, 3, 4, 5, 0, 0, 0 ,0, ...0, 1, 2, 3, 4, 5.
    internal class SimulatedDataStream : Stream
    {
        private readonly long _length;
        private long _position;
        internal static ReadOnlyMemory<byte> DummyData { get; } = GetDummyData();

        private static ReadOnlyMemory<byte> GetDummyData()
        {
            byte[] data = new byte[5];
            new Random(42).NextBytes(data);
            return data;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }

                _position = value;
            }
        }

        public SimulatedDataStream(long length)
        {
            if (length < 10)
            {
                throw new ArgumentException("Length must be at least 10 to append 5 bytes of dummy data at the beginning and end.");
            }

            _length = length;
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (_position == _length || buffer.Length == 0)
            {
                return 0;
            }

            ReadOnlySpan<byte> dummyData = DummyData.Span;

            // Write leading data and return.
            if (_position < dummyData.Length - 1)
            {
                int bytesToWrite = Math.Min(dummyData.Length, buffer.Length);
                dummyData.Slice((int)_position, bytesToWrite).CopyTo(buffer);

                _position += bytesToWrite;
                return bytesToWrite;
            }

            // write middle data by just zero'ing the read buffer.
            int bytesToConsume = (int)Math.Min(_length - _position, buffer.Length);
            Span<byte> usefulBuffer = buffer.Slice(0, bytesToConsume);
            usefulBuffer.Clear();

            _position += bytesToConsume;
            long tempPos = _position;
            long dummyDataTrailingLimit = _length - dummyData.Length;

            // and write trailing data at the end.
            while (tempPos > dummyDataTrailingLimit)
            {
                int dummyDataIdx = (int)(tempPos - dummyDataTrailingLimit) - 1;
                int bufferIdx = usefulBuffer.Length - 1 - (int)(_length - tempPos);

                usefulBuffer[bufferIdx] = dummyData[dummyDataIdx];
                tempPos--;
            }

            return bytesToConsume;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

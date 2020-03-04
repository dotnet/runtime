// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal ref partial struct CborValueReader
    {
        private ReadOnlySpan<byte> _buffer;

        public CborValueReader(ReadOnlySpan<byte> buffer)
        {
            _buffer = buffer;
        }

        public CborInitialByte Peek()
        {
            if (_buffer.IsEmpty)
            {
                throw new InvalidOperationException("end of buffer");
            }

            return new CborInitialByte(_buffer[0]);
        }

        public CborInitialByte Peek(CborMajorType expectedType)
        {
            if (_buffer.IsEmpty)
            {
                throw new InvalidOperationException("end of buffer");
            }

            var result = new CborInitialByte(_buffer[0]);

            if (expectedType != result.MajorType)
            {
                throw new InvalidOperationException("Data item major type mismatch.");
            }

            return result;
        }

        public bool TryPeek(out CborInitialByte result)
        {
            if (!_buffer.IsEmpty)
            {
                result = new CborInitialByte(_buffer[0]);
                return true;
            }

            result = default;
            return false;
        }

        private void AdvanceBuffer(int length)
        {
            _buffer = _buffer.Slice(length);
        }

        private void EnsureBuffer(int length)
        {
            if (_buffer.Length < length)
            {
                throw new FormatException("Unexpected end of buffer.");
            }
        }
    }
}

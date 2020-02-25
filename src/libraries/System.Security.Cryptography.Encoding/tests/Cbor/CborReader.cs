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

        private CborDataItem ReadInitialByte()
        {
            if (_buffer.IsEmpty)
            {
                throw new FormatException("end of buffer");
            }

            return new CborDataItem(_buffer[0]);
        }

        private CborDataItem ReadInitialByte(CborMajorType expectedType)
        {
            if (_buffer.IsEmpty)
            {
                throw new FormatException("end of buffer");
            }

            CborDataItem result = new CborDataItem(_buffer[0]);

            if (expectedType != result.MajorType)
            {
                throw new ArgumentException("Major type does not match expected type");
            }

            return result;
        }

        private bool TryReadInitialByte(out CborDataItem result)
        {
            if (!_buffer.IsEmpty)
            {
                result = new CborDataItem(_buffer[0]);
                return true;
            }

            result = default;
            return false;
        }

        private void AdvanceBuffer(int length)
        {
            _buffer = _buffer.Slice(length);
        }
    }
}

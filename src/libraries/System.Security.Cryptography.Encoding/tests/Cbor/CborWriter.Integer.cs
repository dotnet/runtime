// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        // Implements major type 0 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void Write(ulong value)
        {
            WriteUnsignedInteger(CborMajorType.UnsignedInteger, value);
        }

        // Implements major type 0,1 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void Write(long value)
        {
            if (value < 0)
            {
                ulong unsignedRepresentation = (value == long.MinValue) ? (ulong)long.MaxValue : (ulong)(-value) - 1;
                WriteUnsignedInteger(CborMajorType.NegativeInteger, unsignedRepresentation);
            }
            else
            {
                WriteUnsignedInteger(CborMajorType.UnsignedInteger, (ulong)value);
            }
        }

        // Unsigned integer encoding https://tools.ietf.org/html/rfc7049#section-2.1
        private void WriteUnsignedInteger(CborMajorType type, ulong value)
        {
            if (value < 24)
            {
                EnsureWriteCapacity(1);
                _buffer[0] = new CborDataItem(type, (byte)value).InitialByte;
                _offset++;
            }
            else if (value <= byte.MaxValue)
            {
                EnsureWriteCapacity(2);
                _buffer[0] = new CborDataItem(type, 24).InitialByte;
                _buffer[1] = (byte)value;
                _offset += 2;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureWriteCapacity(3);
                _buffer[0] = new CborDataItem(type, 25).InitialByte;
                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(1), (ushort)value);
                _offset += 3;
            }
            else if (value <= uint.MaxValue)
            {
                EnsureWriteCapacity(5);
                _buffer[0] = new CborDataItem(type, 26).InitialByte;
                BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(1), (uint)value);
                _offset += 5;
            }
            else
            {
                EnsureWriteCapacity(9);
                _buffer[0] = new CborDataItem(type, 27).InitialByte;
                BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(1), value);
                _offset += 9;
            }
        }
    }
}

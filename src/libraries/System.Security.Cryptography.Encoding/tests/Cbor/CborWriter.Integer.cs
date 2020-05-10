// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 0 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void WriteUInt64(ulong value)
        {
            WriteUnsignedInteger(CborMajorType.UnsignedInteger, value);
            AdvanceDataItemCounters();
        }

        // Implements major type 0,1 encoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public void WriteInt64(long value)
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

            AdvanceDataItemCounters();
        }

        public void WriteInt32(int value) => WriteInt64(value);
        public void WriteUInt32(uint value) => WriteUInt64(value);

        // Writes a CBOR negative integer encoding according to
        // https://tools.ietf.org/html/rfc7049#section-2.1
        public void WriteCborNegativeIntegerEncoding(ulong value)
        {
            WriteUnsignedInteger(CborMajorType.NegativeInteger, value);
            AdvanceDataItemCounters();
        }

        // Unsigned integer encoding https://tools.ietf.org/html/rfc7049#section-2.1
        private void WriteUnsignedInteger(CborMajorType type, ulong value)
        {
            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(type, (CborAdditionalInfo)value));
            }
            else if (value <= byte.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(byte));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(ushort));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional16BitData));
                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_offset), (ushort)value);
                _offset += sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                EnsureWriteCapacity(1 + sizeof(uint));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional32BitData));
                BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_offset), (uint)value);
                _offset += sizeof(uint);
            }
            else
            {
                EnsureWriteCapacity(1 + sizeof(ulong));
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional64BitData));
                BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(_offset), value);
                _offset += sizeof(ulong);
            }
        }

        private int GetIntegerEncodingLength(ulong value)
        {
            if (value < (byte)CborAdditionalInfo.Additional8BitData)
            {
                return 1;
            }
            else if (value <= byte.MaxValue)
            {
                return 1 + sizeof(byte);
            }
            else if (value <= ushort.MaxValue)
            {
                return 1 + sizeof(ushort);
            }
            else if (value <= uint.MaxValue)
            {
                return 1 + sizeof(uint);
            }
            else
            {
                return 1 + sizeof(ulong);
            }
        }
    }
}

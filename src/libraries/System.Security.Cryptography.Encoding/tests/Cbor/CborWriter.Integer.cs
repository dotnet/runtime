// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
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

        public void WriteTag(CborTag tag)
        {
            WriteUnsignedInteger(CborMajorType.Tag, (ulong)tag);
            // NB tag writes do not advance data item counters
            _isTagContext = true;
        }

        // Unsigned integer encoding https://tools.ietf.org/html/rfc7049#section-2.1
        private void WriteUnsignedInteger(CborMajorType type, ulong value)
        {
            if (value < 24)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(type, (CborAdditionalInfo)value));
            }
            else if (value <= byte.MaxValue)
            {
                EnsureWriteCapacity(2);
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }
            else if (value <= ushort.MaxValue)
            {
                EnsureWriteCapacity(3);
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional16BitData));
                BinaryPrimitives.WriteUInt16BigEndian(_buffer.AsSpan(_offset), (ushort)value);
                _offset += 2;
            }
            else if (value <= uint.MaxValue)
            {
                EnsureWriteCapacity(5);
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional32BitData));
                BinaryPrimitives.WriteUInt32BigEndian(_buffer.AsSpan(_offset), (uint)value);
                _offset += 4;
            }
            else
            {
                EnsureWriteCapacity(9);
                WriteInitialByte(new CborInitialByte(type, CborAdditionalInfo.Additional64BitData));
                BinaryPrimitives.WriteUInt64BigEndian(_buffer.AsSpan(_offset), value);
                _offset += 8;
            }
        }
    }
}

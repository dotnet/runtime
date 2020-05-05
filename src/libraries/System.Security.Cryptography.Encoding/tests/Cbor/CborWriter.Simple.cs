// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements https://tools.ietf.org/html/rfc7049#section-2.3

        public void WriteSingle(float value)
        {
            EnsureWriteCapacity(5);
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional32BitData));
            BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += 4;
            AdvanceDataItemCounters();
        }

        public void WriteDouble(double value)
        {
            EnsureWriteCapacity(9);
            WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional64BitData));
            BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += 8;
            AdvanceDataItemCounters();
        }

        public void WriteBoolean(bool value)
        {
            WriteSimpleValue(value ? CborSimpleValue.True : CborSimpleValue.False);
        }

        public void WriteNull()
        {
            WriteSimpleValue(CborSimpleValue.Null);
        }

        public void WriteSimpleValue(CborSimpleValue value)
        {
            if ((byte)value < 24)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, (CborAdditionalInfo)value));
            }
            else
            {
                EnsureWriteCapacity(2);
                WriteInitialByte(new CborInitialByte(CborMajorType.Simple, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }

            AdvanceDataItemCounters();
        }
    }
}

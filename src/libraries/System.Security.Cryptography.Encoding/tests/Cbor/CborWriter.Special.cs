// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        // Implements https://tools.ietf.org/html/rfc7049#section-2.3

        public void WriteSingle(float value)
        {
            EnsureWriteCapacity(5);
            WriteInitialByte(new CborInitialByte(CborMajorType.Special, CborAdditionalInfo.Additional32BitData));
            BinaryPrimitives.WriteSingleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += 4;
            AdvanceDataItemCounters();
        }

        public void WriteDouble(double value)
        {
            EnsureWriteCapacity(9);
            WriteInitialByte(new CborInitialByte(CborMajorType.Special, CborAdditionalInfo.Additional64BitData));
            BinaryPrimitives.WriteDoubleBigEndian(_buffer.AsSpan(_offset), value);
            _offset += 8;
            AdvanceDataItemCounters();
        }

        public void WriteBoolean(bool value)
        {
            WriteSpecialValue(value ? CborSpecialValue.True : CborSpecialValue.False);
        }

        public void WriteNull()
        {
            WriteSpecialValue(CborSpecialValue.Null);
        }

        public void WriteSpecialValue(CborSpecialValue value)
        {
            if ((byte)value < 24)
            {
                EnsureWriteCapacity(1);
                WriteInitialByte(new CborInitialByte(CborMajorType.Special, (CborAdditionalInfo)value));
            }
            else
            {
                EnsureWriteCapacity(2);
                WriteInitialByte(new CborInitialByte(CborMajorType.Special, CborAdditionalInfo.Additional8BitData));
                _buffer[_offset++] = (byte)value;
            }

            AdvanceDataItemCounters();
        }
    }
}

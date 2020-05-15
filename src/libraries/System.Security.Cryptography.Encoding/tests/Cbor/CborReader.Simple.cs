// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        public float ReadSingle()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            float result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 3);
                    result = (float)ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    throw new InvalidOperationException(SR.Cbor_Reader_ReadingDoubleAsSingle);

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);

            }
        }

        public double ReadDouble()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            double result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 3);
                    result = ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureReadCapacity(buffer, 9);
                    result = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(9);
                    AdvanceDataItemCounters();
                    return result;

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);
            }
        }

        public bool ReadBoolean()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);

            bool result = header.AdditionalInfo switch
            {
                (CborAdditionalInfo)CborSimpleValue.False => false,
                (CborAdditionalInfo)CborSimpleValue.True => true,
                _ => throw new InvalidOperationException(SR.Cbor_Reader_NotABooleanEncoding),
            };

            AdvanceBuffer(1);
            AdvanceDataItemCounters();
            return result;
        }

        public void ReadNull()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);

            switch (header.AdditionalInfo)
            {
                case (CborAdditionalInfo)CborSimpleValue.Null:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return;
                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotANullEncoding);
            }
        }

        public CborSimpleValue ReadSimpleValue()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo info when (byte)info < 24:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return (CborSimpleValue)header.AdditionalInfo;
                case CborAdditionalInfo.Additional8BitData:
                    EnsureReadCapacity(2);
                    byte value = _data.Span[_offset + 1];

                    if (value < 32)
                    {
                        throw new FormatException(SR.Cbor_Reader_InvalidCbor_InvalidSimpleValueEncoding);
                    }

                    AdvanceBuffer(2);
                    AdvanceDataItemCounters();
                    return (CborSimpleValue)value;
                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotASimpleValueEncoding);
            }
        }

        // half-precision float decoder adapted from https://tools.ietf.org/html/rfc7049#appendix-D
        private static double ReadHalfBigEndian(ReadOnlySpan<byte> buffer)
        {
            int half = (buffer[0] << 8) + buffer[1];
            bool isNegative = (half >> 15) != 0;
            int exp = (half >> 10) & 0x1f;
            int mant = half & 0x3ff;
            double value;

            if (exp == 0)
            {
                value = mant * 5.9604644775390625e-08 /* precomputed 2^-24 */;
            }
            else if (exp != 31)
            {
                value = (mant + 1024) * Math.Pow(2, exp - 25);
            }
            else
            {
                value = (mant == 0) ? double.PositiveInfinity : double.NaN;
            }

            return isNegative ? -value : value;
        }
    }
}

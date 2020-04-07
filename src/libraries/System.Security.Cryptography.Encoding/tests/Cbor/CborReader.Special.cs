// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        public float ReadSingle()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Special);
            ReadOnlySpan<byte> buffer = _buffer.Span;
            float result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureBuffer(buffer, 3);
                    result = (float)ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureBuffer(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    throw new InvalidOperationException("Attempting to read double-precision floating point encoding as single-precision.");

                default:
                    throw new InvalidOperationException("CBOR data item does not encode a floating point number.");

            }
        }

        public double ReadDouble()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Special);
            ReadOnlySpan<byte> buffer = _buffer.Span;
            double result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureBuffer(buffer, 3);
                    result = ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureBuffer(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureBuffer(buffer, 9);
                    result = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(9);
                    AdvanceDataItemCounters();
                    return result;

                default:
                    throw new InvalidOperationException("CBOR data item does not encode a floating point number.");
            }
        }

        public bool ReadBoolean()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Special);

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.SpecialValueFalse:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return false;
                case CborAdditionalInfo.SpecialValueTrue:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return true;
                default:
                    throw new InvalidOperationException("CBOR data item does not encode a boolean value.");
            }
        }

        public void ReadNull()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Special);

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.SpecialValueNull:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return;
                default:
                    throw new InvalidOperationException("CBOR data item does not encode a null value.");
            }
        }

        public CborSpecialValue ReadSpecialValue()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Special);

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo info when (byte)info < 24:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return (CborSpecialValue)header.AdditionalInfo;
                case CborAdditionalInfo.Additional8BitData:
                    EnsureBuffer(2);
                    byte value = _buffer.Span[1];

                    if (value < 32)
                    {
                        throw new FormatException("Two-byte CBOR special value must be between 32 and 255.");
                    }

                    AdvanceBuffer(2);
                    AdvanceDataItemCounters();
                    return (CborSpecialValue)value;
                default:
                    throw new InvalidOperationException("CBOR data item does not encode a special value.");
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

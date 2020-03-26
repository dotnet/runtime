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
                    result = Read16BitFloatBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    DecrementRemainingItemCount();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureBuffer(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    DecrementRemainingItemCount();
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
                    result = Read16BitFloatBigEndian(buffer.Slice(1));
                    AdvanceBuffer(3);
                    DecrementRemainingItemCount();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureBuffer(buffer, 5);
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(5);
                    DecrementRemainingItemCount();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureBuffer(buffer, 9);
                    result = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(9);
                    DecrementRemainingItemCount();
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
                    DecrementRemainingItemCount();
                    return false;
                case CborAdditionalInfo.SpecialValueTrue:
                    AdvanceBuffer(1);
                    DecrementRemainingItemCount();
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
                    DecrementRemainingItemCount();
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
                    DecrementRemainingItemCount();
                    return (CborSpecialValue)header.AdditionalInfo;
                case CborAdditionalInfo.Additional8BitData:
                    EnsureBuffer(2);
                    byte value = _buffer.Span[1];

                    if (value < 32)
                    {
                        throw new FormatException("Two-byte CBOR special value must be between 32 and 255.");
                    }

                    AdvanceBuffer(2);
                    DecrementRemainingItemCount();
                    return (CborSpecialValue)value;
                default:
                    throw new InvalidOperationException("CBOR data item does not encode a special value.");
            }
        }

        private static float Read16BitFloatBigEndian(ReadOnlySpan<byte> buffer)
        {
            throw new NotImplementedException(nameof(Read16BitFloatBigEndian));
        }
    }
}

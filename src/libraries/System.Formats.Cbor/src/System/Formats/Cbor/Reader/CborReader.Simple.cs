// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>Reads the next data item as a half-precision floating point number (major type 7).</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a floating-point number encoding.
        /// -or-
        /// The encoded value is a double-precision float.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public Half ReadHalf()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            Half result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 1 + HalfHelpers.SizeOfHalf);
                    result = HalfHelpers.ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + HalfHelpers.SizeOfHalf);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                case CborAdditionalInfo.Additional64BitData:
                    throw new InvalidOperationException(SR.Cbor_Reader_ReadingAsLowerPrecision);

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);
            }
        }

        /// <summary>Reads the next data item as a single-precision floating point number (major type 7).</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a floating-point number encoding.
        /// -or-
        /// The encoded value is a double-precision float</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public float ReadSingle()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            float result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 1 + HalfHelpers.SizeOfHalf);
                    result = (float)HalfHelpers.ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + HalfHelpers.SizeOfHalf);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(buffer, 1 + sizeof(float));
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + sizeof(float));
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    throw new InvalidOperationException(SR.Cbor_Reader_ReadingAsLowerPrecision);

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);

            }
        }

        /// <summary>Reads the next data item as a double-precision floating point number (major type 7).</summary>
        /// <returns>The decoded <see cref="double" /> value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a floating-point number encoding</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public double ReadDouble()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            double result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 1 + HalfHelpers.SizeOfHalf);
                    result = (double)HalfHelpers.ReadHalfBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + HalfHelpers.SizeOfHalf);
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(buffer, 1 + sizeof(float));
                    result = BinaryPrimitives.ReadSingleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + sizeof(float));
                    AdvanceDataItemCounters();
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureReadCapacity(buffer, 1 + sizeof(double));
                    result = BinaryPrimitives.ReadDoubleBigEndian(buffer.Slice(1));
                    AdvanceBuffer(1 + sizeof(double));
                    AdvanceDataItemCounters();
                    return result;

                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotAFloatEncoding);
            }
        }

        /// <summary>Reads the next data item as a boolean value (major type 7).</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a boolean encoding</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
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

        /// <summary>Reads the next data item as a <see langword="null" /> value (major type 7).</summary>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a <see langword="null" /> value encoding.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
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

        /// <summary>Reads the next data item as a CBOR simple value (major type 7).</summary>
        /// <returns>The decoded CBOR simple value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next simple value is not a simple value encoding.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public CborSimpleValue ReadSimpleValue()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Simple);

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo info when info < CborAdditionalInfo.Additional8BitData:
                    AdvanceBuffer(1);
                    AdvanceDataItemCounters();
                    return (CborSimpleValue)header.AdditionalInfo;
                case CborAdditionalInfo.Additional8BitData:
                    EnsureReadCapacity(2);
                    byte value = _data.Span[_offset + 1];

                    if (value <= (byte)CborAdditionalInfo.IndefiniteLength &&
                        _isConformanceModeCheckEnabled &&
                        CborConformanceModeHelpers.RequireCanonicalSimpleValueEncodings(ConformanceMode))
                    {
                        throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_InvalidSimpleValueEncoding, ConformanceMode));
                    }

                    AdvanceBuffer(2);
                    AdvanceDataItemCounters();
                    return (CborSimpleValue)value;
                default:
                    throw new InvalidOperationException(SR.Cbor_Reader_NotASimpleValueEncoding);
            }
        }
    }
}

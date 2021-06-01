// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        // Implements major type 0,1 decoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>Reads the next data item as a signed integer (major types 0,1)</summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="OverflowException">The encoded integer is out of range for <see cref="int" />.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public int ReadInt32()
        {
            int value = checked((int)PeekSignedInteger(out int bytesRead));
            AdvanceBuffer(bytesRead);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>Reads the next data item as an unsigned integer (major type 0).</summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="OverflowException">The encoded integer is out of range for <see cref="uint" />.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public uint ReadUInt32()
        {
            uint value = checked((uint)PeekUnsignedInteger(out int bytesRead));
            AdvanceBuffer(bytesRead);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>Reads the next data item as a signed integer (major types 0,1)</summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="OverflowException">The encoded integer is out of range for <see cref="long" />.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public long ReadInt64()
        {
            long value = PeekSignedInteger(out int bytesRead);
            AdvanceBuffer(bytesRead);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>Reads the next data item as an unsigned integer (major type 0).</summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="OverflowException">The encoded integer is out of range for <see cref="ulong" />.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public ulong ReadUInt64()
        {
            ulong value = PeekUnsignedInteger(out int bytesRead);
            AdvanceBuffer(bytesRead);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>Reads the next data item as a CBOR negative integer representation (major type 1).</summary>
        /// <returns>An unsigned integer denoting -1 minus the integer.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="OverflowException">The encoded integer is out of range for <see cref="uint" /></exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>
        /// This method supports decoding integers between -18446744073709551616 and -1.
        /// Useful for handling values that do not fit in the <see cref="long" /> type.
        /// </remarks>
        [CLSCompliant(false)]
        public ulong ReadCborNegativeIntegerRepresentation()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.NegativeInteger);
            ulong value = DecodeUnsignedInteger(header, GetRemainingBytes(), out int bytesRead);
            AdvanceBuffer(bytesRead);
            AdvanceDataItemCounters();
            return value;
        }

        private ulong PeekUnsignedInteger(out int bytesRead)
        {
            CborInitialByte header = PeekInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    ulong value = DecodeUnsignedInteger(header, GetRemainingBytes(), out bytesRead);
                    return value;

                case CborMajorType.NegativeInteger:
                    throw new OverflowException();

                default:
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, (int)header.MajorType));
            }
        }

        private long PeekSignedInteger(out int bytesRead)
        {
            CborInitialByte header = PeekInitialByte();
            long value;

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = checked((long)DecodeUnsignedInteger(header, GetRemainingBytes(), out bytesRead));
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)DecodeUnsignedInteger(header, GetRemainingBytes(), out bytesRead));
                    return value;

                default:
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, (int)header.MajorType));
            }
        }

        // Peek definite length for given data item
        private int DecodeDefiniteLength(CborInitialByte header, ReadOnlySpan<byte> data, out int bytesRead)
        {
            ulong length = DecodeUnsignedInteger(header, data, out bytesRead);

            // conservative check: ensure the buffer has the minimum required length for declared definite length.
            if (length > (ulong)(data.Length - bytesRead))
            {
                throw new CborContentException(SR.Cbor_Reader_DefiniteLengthExceedsBufferSize);
            }

            return (int)length;
        }

        // Unsigned integer decoding https://tools.ietf.org/html/rfc7049#section-2.1
        private ulong DecodeUnsignedInteger(CborInitialByte header, ReadOnlySpan<byte> data, out int bytesRead)
        {
            ulong result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.Additional8BitData):
                    bytesRead = 1;
                    return (ulong)x;

                case CborAdditionalInfo.Additional8BitData:
                    EnsureReadCapacity(data, 1 + sizeof(byte));
                    result = data[1];

                    if (result < (int)CborAdditionalInfo.Additional8BitData)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    bytesRead = 1 + sizeof(byte);
                    return result;

                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(data, 1 + sizeof(ushort));
                    result = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(1));

                    if (result <= byte.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    bytesRead = 1 + sizeof(ushort);
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(data, 1 + sizeof(uint));
                    result = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(1));

                    if (result <= ushort.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    bytesRead = 1 + sizeof(uint);
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureReadCapacity(data, 1 + sizeof(ulong));
                    result = BinaryPrimitives.ReadUInt64BigEndian(data.Slice(1));

                    if (result <= uint.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    bytesRead = 1 + sizeof(ulong);
                    return result;

                default:
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_InvalidIntegerEncoding);
            }

            void ValidateIsNonStandardIntegerRepresentationSupported()
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresCanonicalIntegerRepresentation(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_NonCanonicalIntegerRepresentation, ConformanceMode));
                }
            }
        }
    }
}

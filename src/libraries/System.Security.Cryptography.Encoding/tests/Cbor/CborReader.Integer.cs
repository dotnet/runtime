// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Xml;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        // Implements major type 0,1 decoding per https://tools.ietf.org/html/rfc7049#section-2.1

        /// <summary>
        ///   Reads the next data item as a signed integer (major types 0,1)
        /// </summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   the next value does have the correct major type.
        /// </exception>
        /// <exception cref="OverflowException">
        ///   the encoded integer is out of range for <see cref="int"/>
        /// </exception>
        /// <exception cref="FormatException">
        ///   unexpected end of CBOR encoding data --OR--
        ///   the length encoding is not valid under the current conformance level --OR--
        ///   the data item is located in an illegal context (e.g. an indefinite-length string)
        /// </exception>
        public int ReadInt32()
        {
            int value = checked((int)PeekSignedInteger(out int additionalBytes));
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>
        ///   Reads the next data item as an usigned integer (major type 0)
        /// </summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   the next value does have the correct major type.
        /// </exception>
        /// <exception cref="OverflowException">
        ///   the encoded integer is out of range for <see cref="uint"/>
        /// </exception>
        /// <exception cref="FormatException">
        ///   unexpected end of CBOR encoding data --OR--
        ///   the length encoding is not valid under the current conformance level --OR--
        ///   the data item is located in an illegal context (e.g. an indefinite-length string)
        /// </exception>
        public uint ReadUInt32()
        {
            uint value = checked((uint)PeekUnsignedInteger(out int additionalBytes));
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>
        ///   Reads the next data item as a signed integer (major types 0,1)
        /// </summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   the next value does have the correct major type.
        /// </exception>
        /// <exception cref="OverflowException">
        ///   the encoded integer is out of range for <see cref="long"/>
        /// </exception>
        /// <exception cref="FormatException">
        ///   unexpected end of CBOR encoding data --OR--
        ///   the length encoding is not valid under the current conformance level --OR--
        ///   the data item is located in an illegal context (e.g. an indefinite-length string)
        /// </exception>
        public long ReadInt64()
        {
            long value = PeekSignedInteger(out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>
        ///   Reads the next data item as an usigned integer (major type 0)
        /// </summary>
        /// <returns>The decoded integer value.</returns>
        /// <exception cref="InvalidOperationException">
        ///   the next value does have the correct major type.
        /// </exception>
        /// <exception cref="OverflowException">
        ///   the encoded integer is out of range for <see cref="ulong"/>
        /// </exception>
        /// <exception cref="FormatException">
        ///   unexpected end of CBOR encoding data --OR--
        ///   the length encoding is not valid under the current conformance level --OR--
        ///   the data item is located in an illegal context (e.g. an indefinite-length string)
        /// </exception>
        public ulong ReadUInt64()
        {
            ulong value = PeekUnsignedInteger(out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        /// <summary>
        ///   Reads the next data item as a CBOR negative integer encoding (major type 1).
        /// </summary>
        /// <returns>
        ///   An unsigned integer denoting -1 minus the integer.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   the next value does have the correct major type.
        /// </exception>
        /// <exception cref="OverflowException">
        ///   the encoded integer is out of range for <see cref="uint"/>
        /// </exception>
        /// <exception cref="FormatException">
        ///   unexpected end of CBOR encoding data --OR--
        ///   the length encoding is not valid under the current conformance level --OR--
        ///   the data item is located in an illegal context (e.g. an indefinite-length string)
        /// </exception>
        /// <remarks>
        ///   Intended as an escape hatch in cases of valid CBOR negative integers exceeding primitive sizes.
        /// </remarks>
        public ulong ReadCborNegativeIntegerEncoding()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.NegativeInteger);
            ulong value = ReadUnsignedInteger(GetRemainingBytes(), header, out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        private ulong PeekUnsignedInteger(out int additionalBytes)
        {
            CborInitialByte header = PeekInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    ulong value = ReadUnsignedInteger(GetRemainingBytes(), header, out additionalBytes);
                    return value;

                case CborMajorType.NegativeInteger:
                    throw new OverflowException();

                default:
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, header.MajorType));
            }
        }

        private long PeekSignedInteger(out int additionalBytes)
        {
            CborInitialByte header = PeekInitialByte();
            long value;

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = checked((long)ReadUnsignedInteger(GetRemainingBytes(), header, out additionalBytes));
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)ReadUnsignedInteger(GetRemainingBytes(), header, out additionalBytes));
                    return value;

                default:
                    throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, header.MajorType));
            }
        }

        // Unsigned integer decoding https://tools.ietf.org/html/rfc7049#section-2.1
        private ulong ReadUnsignedInteger(ReadOnlySpan<byte> buffer, CborInitialByte header, out int additionalBytes)
        {
            ulong result;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.Additional8BitData):
                    additionalBytes = 0;
                    return (ulong)x;

                case CborAdditionalInfo.Additional8BitData:
                    EnsureReadCapacity(buffer, 2);
                    result = buffer[1];

                    if (result < (int)CborAdditionalInfo.Additional8BitData)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    additionalBytes = 1;
                    return result;

                case CborAdditionalInfo.Additional16BitData:
                    EnsureReadCapacity(buffer, 3);
                    result = BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1));

                    if (result <= byte.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    additionalBytes = 2;
                    return result;

                case CborAdditionalInfo.Additional32BitData:
                    EnsureReadCapacity(buffer, 5);
                    result = BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(1));

                    if (result <= ushort.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    additionalBytes = 4;
                    return result;

                case CborAdditionalInfo.Additional64BitData:
                    EnsureReadCapacity(buffer, 9);
                    result = BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(1));

                    if (result <= uint.MaxValue)
                    {
                        ValidateIsNonStandardIntegerRepresentationSupported();
                    }

                    additionalBytes = 8;
                    return result;

                default:
                    throw new FormatException(SR.Cbor_Reader_InvalidCbor_InvalidIntegerEncoding);
            }

            void ValidateIsNonStandardIntegerRepresentationSupported()
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresCanonicalIntegerRepresentation(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_NonCanonicalIntegerRepresentation, ConformanceLevel));
                }
            }
        }
    }
}

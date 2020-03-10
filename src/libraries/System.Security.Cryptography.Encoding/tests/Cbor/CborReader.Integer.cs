// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        // Implements major type 0 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public ulong ReadUInt64()
        {
            CborInitialByte header = PeekInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    ulong value = ReadUnsignedInteger(header, out int additionalBytes);
                    AdvanceBuffer(1 + additionalBytes);
                    _remainingDataItems--;
                    return value;

                case CborMajorType.NegativeInteger:
                    throw new OverflowException();

                default:
                    throw new InvalidOperationException("Data item major type mismatch.");
            }
        }

        // Implements major type 0,1 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public long ReadInt64()
        {
            long value;
            int additionalBytes;

            CborInitialByte header = PeekInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = checked((long)ReadUnsignedInteger(header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    _remainingDataItems--;
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)ReadUnsignedInteger(header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    _remainingDataItems--;
                    return value;

                default:
                    throw new InvalidOperationException("Data item major type mismatch.");
            }
        }

        // Returns the next CBOR negative integer encoding according to
        // https://tools.ietf.org/html/rfc7049#section-2.1
        public ulong ReadCborNegativeIntegerEncoding()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.NegativeInteger);
            ulong value = ReadUnsignedInteger(header, out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            _remainingDataItems--;
            return value;
        }

        // Unsigned integer decoding https://tools.ietf.org/html/rfc7049#section-2.1
        private ulong ReadUnsignedInteger(CborInitialByte header, out int additionalBytes)
        {
            ReadOnlySpan<byte> buffer = _buffer.Span;

            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.UnsignedInteger8BitEncoding):
                    additionalBytes = 0;
                    return (ulong)x;

                case CborAdditionalInfo.UnsignedInteger8BitEncoding:
                    EnsureBuffer(2);
                    additionalBytes = 1;
                    return buffer[1];

                case CborAdditionalInfo.UnsignedInteger16BitEncoding:
                    EnsureBuffer(3);
                    additionalBytes = 2;
                    return BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1));

                case CborAdditionalInfo.UnsignedInteger32BitEncoding:
                    EnsureBuffer(5);
                    additionalBytes = 4;
                    return BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(1));

                case CborAdditionalInfo.UnsignedInteger64BitEncoding:
                    EnsureBuffer(9);
                    additionalBytes = 8;
                    return BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(1));

                case CborAdditionalInfo.IndefiniteLength:
                    throw new NotImplementedException("indefinite length support");

                default:
                    throw new FormatException("initial byte contains invalid integer encoding data");
            }
        }
    }
}

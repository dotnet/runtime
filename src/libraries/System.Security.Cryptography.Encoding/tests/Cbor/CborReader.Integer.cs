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
                    ulong value = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);
                    AdvanceBuffer(1 + additionalBytes);
                    AdvanceDataItemCounters();
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
                    value = checked((long)ReadUnsignedInteger(_buffer.Span, header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    AdvanceDataItemCounters();
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)ReadUnsignedInteger(_buffer.Span, header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    AdvanceDataItemCounters();
                    return value;

                default:
                    throw new InvalidOperationException("Data item major type mismatch.");
            }
        }

        public CborTag ReadTag()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Tag);
            ulong tag = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            // NB tag reads do not advance data item counters
            _isTagContext = true;
            return (CborTag)tag;
        }

        // Returns the next CBOR negative integer encoding according to
        // https://tools.ietf.org/html/rfc7049#section-2.1
        public ulong ReadCborNegativeIntegerEncoding()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.NegativeInteger);
            ulong value = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);
            AdvanceBuffer(1 + additionalBytes);
            AdvanceDataItemCounters();
            return value;
        }

        // Unsigned integer decoding https://tools.ietf.org/html/rfc7049#section-2.1
        private static ulong ReadUnsignedInteger(ReadOnlySpan<byte> buffer, CborInitialByte header, out int additionalBytes)
        {
            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.Additional8BitData):
                    additionalBytes = 0;
                    return (ulong)x;

                case CborAdditionalInfo.Additional8BitData:
                    EnsureBuffer(buffer, 2);
                    additionalBytes = 1;
                    return buffer[1];

                case CborAdditionalInfo.Additional16BitData:
                    EnsureBuffer(buffer, 3);
                    additionalBytes = 2;
                    return BinaryPrimitives.ReadUInt16BigEndian(buffer.Slice(1));

                case CborAdditionalInfo.Additional32BitData:
                    EnsureBuffer(buffer, 5);
                    additionalBytes = 4;
                    return BinaryPrimitives.ReadUInt32BigEndian(buffer.Slice(1));

                case CborAdditionalInfo.Additional64BitData:
                    EnsureBuffer(buffer, 9);
                    additionalBytes = 8;
                    return BinaryPrimitives.ReadUInt64BigEndian(buffer.Slice(1));

                default:
                    throw new FormatException("initial byte contains invalid integer encoding data");
            }
        }
    }
}

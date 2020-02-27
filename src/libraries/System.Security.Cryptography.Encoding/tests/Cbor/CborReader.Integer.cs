// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal ref partial struct CborValueReader
    {
        // Implements major type 0 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public ulong ReadUInt64()
        {
            CborDataItem header = ReadInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    ulong value = ReadUnsignedInteger(header, out int additionalBytes);
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                default:
                    throw new InvalidOperationException("Data item type mismatch");
            }
        }

        public bool TryReadUInt64(out ulong value)
        {
            CborDataItem header = ReadInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = ReadUnsignedInteger(header, out int additionalBytes);
                    AdvanceBuffer(1 + additionalBytes);
                    return true;

                default:
                    value = 0;
                    return false;
            }
        }

        // Implements major type 0,1 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public long ReadInt64()
        {
            long value;
            int additionalBytes;

            CborDataItem header = ReadInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = checked((long)ReadUnsignedInteger(header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)ReadUnsignedInteger(header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                default:
                    throw new InvalidOperationException("Data item type mismatch");
            }
        }

        public bool TryReadInt64(out long value)
        {
            ulong result;
            int additionalBytes;

            CborDataItem header = ReadInitialByte();

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    result = ReadUnsignedInteger(header, out additionalBytes);
                    if (result > long.MaxValue)
                    {
                        value = 0;
                        return false;
                    }
                    else
                    {
                        value = (long)result;
                        AdvanceBuffer(1 + additionalBytes);
                        return true;
                    }

                case CborMajorType.NegativeInteger:
                    result = ReadUnsignedInteger(header, out additionalBytes);
                    if (result > long.MaxValue)
                    {
                        value = 0;
                        return false;
                    }
                    else
                    {
                        value = -1 - (long)result;
                        AdvanceBuffer(1 + additionalBytes);
                        return true;
                    }

                default:
                    value = 0;
                    return false;
            }
        }

        // Unsigned integer decoding https://tools.ietf.org/html/rfc7049#section-2.1
        private ulong ReadUnsignedInteger(CborDataItem header, out int additionalBytes)
        {
            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.UnsignedInteger8BitEncoding):
                    additionalBytes = 0;
                    return (ulong)x;

                case CborAdditionalInfo.UnsignedInteger8BitEncoding:
                    EnsureBuffer(1);
                    additionalBytes = 1;
                    return _buffer[1];

                case CborAdditionalInfo.UnsignedInteger16BitEncoding:
                    EnsureBuffer(2);
                    additionalBytes = 2;
                    return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(1));

                case CborAdditionalInfo.UnsignedInteger32BitEncoding:
                    EnsureBuffer(4);
                    additionalBytes = 4;
                    return BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(1));

                case CborAdditionalInfo.UnsignedInteger64BitEncoding:
                    EnsureBuffer(8);
                    additionalBytes = 8;
                    return BinaryPrimitives.ReadUInt64BigEndian(_buffer.Slice(1));

                case CborAdditionalInfo.IndefiniteLength:
                    throw new NotImplementedException("indefinite length support");

                default:
                    throw new FormatException("initial byte contains invalid integer encoding data");
            }
        }
    }
}

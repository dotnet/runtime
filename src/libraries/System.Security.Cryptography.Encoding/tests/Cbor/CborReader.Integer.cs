// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal ref partial struct CborReader
    {
        // Implements major type 0 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public ulong ReadUInt64()
        {
            ReadInitialByte(out CborDataItem header);

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    ulong value = ReadUnsignedInteger(in header, out int additionalBytes);
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                default:
                    throw new ArgumentException("Data item type mismatch");
            }
        }

        public bool TryReadUInt64(out ulong value)
        {
            ReadInitialByte(out CborDataItem header);

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = ReadUnsignedInteger(in header, out int additionalBytes);
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

            ReadInitialByte(out CborDataItem header);

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    value = checked((long)ReadUnsignedInteger(in header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                case CborMajorType.NegativeInteger:
                    value = checked(-1 - (long)ReadUnsignedInteger(in header, out additionalBytes));
                    AdvanceBuffer(1 + additionalBytes);
                    return value;

                default:
                    throw new ArgumentException("Data item type mismatch");
            }
        }

        public bool TryReadInt64(out long value)
        {
            ulong result;
            int additionalBytes;

            ReadInitialByte(out CborDataItem header);

            switch (header.MajorType)
            {
                case CborMajorType.UnsignedInteger:
                    result = ReadUnsignedInteger(in header, out additionalBytes);
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
                    result = ReadUnsignedInteger(in header, out additionalBytes);
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
        private ulong ReadUnsignedInteger(in CborDataItem header, out int additionalBytes)
        {
            switch (header.AdditionalInfo)
            {
                case CborAdditionalInfo x when (x < CborAdditionalInfo.UnsignedInteger8BitEncoding):
                    additionalBytes = 0;
                    return (ulong)x;

                case CborAdditionalInfo.UnsignedInteger8BitEncoding:
                    if (_buffer.Length > 1)
                    {
                        additionalBytes = 1;
                        return _buffer[1];
                    }

                    throw new FormatException("End of buffer");

                case CborAdditionalInfo.UnsignedInteger16BitEncoding:
                    if (_buffer.Length > 2)
                    {
                        additionalBytes = 2;
                        return BinaryPrimitives.ReadUInt16BigEndian(_buffer.Slice(1));
                    }

                    throw new FormatException("End of buffer");

                case CborAdditionalInfo.UnsignedInteger32BitEncoding:
                    if (_buffer.Length > 4)
                    {
                        additionalBytes = 4;
                        return BinaryPrimitives.ReadUInt32BigEndian(_buffer.Slice(1));
                    }

                    throw new FormatException("End of buffer");

                case CborAdditionalInfo.UnsignedInteger64BitEncoding:
                    if (_buffer.Length > 2)
                    {
                        additionalBytes = 8;
                        return BinaryPrimitives.ReadUInt64BigEndian(_buffer.Slice(1));
                    }

                    throw new FormatException("End of buffer");

                case CborAdditionalInfo.IndefiniteLength:
                    throw new NotImplementedException("indefinite length support");

                default:
                    throw new FormatException("initial byte contains invalid integer encoding data");
            }
        }
    }
}

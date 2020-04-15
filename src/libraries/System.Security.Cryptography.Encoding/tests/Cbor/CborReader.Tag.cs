// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Numerics;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        public CborTag ReadTag()
        {
            CborTag tag = PeekTagCore(out int additionalBytes);

            AdvanceBuffer(1 + additionalBytes);
            _isTagContext = true;
            return tag;
        }

        public void ReadTag(CborTag expectedTag)
        {
            CborTag tag = PeekTagCore(out int additionalBytes);

            if (expectedTag != tag)
            {
                throw new InvalidOperationException("CBOR tag mismatch.");
            }

            AdvanceBuffer(1 + additionalBytes);
            _isTagContext = true;
        }

        public CborTag PeekTag() => PeekTagCore(out int _);

        private CborTag PeekTagCore(out int additionalBytes)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Tag);
            return (CborTag)ReadUnsignedInteger(_buffer.Span, header, out additionalBytes);
        }

        // Additional tagged type support

        public DateTimeOffset ReadDateTimeOffset()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.1

            CborReaderCheckpoint checkpoint = CreateCheckpoint();

            try
            {
                switch (ReadTag())
                {
                    case CborTag.DateTimeString:
                        if (Peek() != CborReaderState.TextString)
                        {
                            throw new FormatException("String DateTime semantic tag should annotate string value.");
                        }

                        string dateString = ReadTextString();

                        if (!DateTimeOffset.TryParseExact(dateString, CborWriter.Rfc3339FormatString, null, DateTimeStyles.RoundtripKind, out DateTimeOffset result))
                        {
                            throw new FormatException("DateTime string is not valid RFC3339.");
                        }

                        return result;

                    case CborTag.DateTimeUnixSeconds:
                        switch (Peek())
                        {
                            case CborReaderState.UnsignedInteger:
                            case CborReaderState.NegativeInteger:
                                return DateTimeOffset.FromUnixTimeSeconds(ReadInt64());

                            case CborReaderState.HalfPrecisionFloat:
                            case CborReaderState.SinglePrecisionFloat:
                            case CborReaderState.DoublePrecisionFloat:
                                // we don't (but probably should) have a float overload for DateTimeOffset.FromUnixTimeSeconds
                                double seconds = ReadDouble();
                                long epochTicks = DateTimeOffset.UnixEpoch.Ticks;
                                long ticks = checked(epochTicks + (long)(seconds * TimeSpan.TicksPerSecond));
                                return new DateTimeOffset(ticks, TimeSpan.Zero);

                            default:
                                throw new FormatException("Epoch DateTime semantic tag should annotate numeric value.");
                        }

                    default:
                        throw new InvalidOperationException("CBOR tag is not a recognized DateTime value.");
                }
            }
            catch
            {
                RestoreCheckpoint(checkpoint);
                throw;
            }
        }

        public BigInteger ReadBigInteger()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.2

            CborReaderCheckpoint checkpoint = CreateCheckpoint();

            try
            {
                bool isUnsigned = ReadTag() switch
                {
                    CborTag.UnsignedBigNum => true,
                    CborTag.NegativeBigNum => false,
                    _ => throw new InvalidOperationException("CBOR tag is not a recognized Bignum value."),
                };

                if (Peek() != CborReaderState.ByteString)
                {
                    throw new FormatException("BigNum semantic tag should annotate byte string value.");
                }

                byte[] unsignedBigEndianEncoding = ReadByteString();
                BigInteger unsignedValue = new BigInteger(unsignedBigEndianEncoding, isUnsigned: true, isBigEndian: true);
                return isUnsigned ? unsignedValue : -1 - unsignedValue;
            }
            catch
            {
                RestoreCheckpoint(checkpoint);
                throw;
            }
        }

        public decimal ReadDecimal()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.3

            CborReaderCheckpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadTag(expectedTag: CborTag.DecimalFraction);

                if (Peek() != CborReaderState.StartArray || ReadStartArray() != 2)
                {
                    throw new FormatException("DecimalFraction tag should annotate a list of two numeric elements.");
                }

                decimal mantissa; // signed integral component of the decimal value
                long exponent;    // base-10 exponent

                switch (Peek())
                {
                    case CborReaderState.UnsignedInteger:
                    case CborReaderState.NegativeInteger:
                        exponent = ReadInt64();
                        break;

                    default:
                        throw new FormatException("DecimalFraction tag should annotate a list of two numeric elements.");
                }

                switch (Peek())
                {
                    case CborReaderState.UnsignedInteger:
                        mantissa = ReadUInt64();
                        break;

                    case CborReaderState.NegativeInteger:
                        mantissa = -1m - ReadCborNegativeIntegerEncoding();
                        break;

                    case CborReaderState.Tag:
                        switch(PeekTag())
                        {
                            case CborTag.UnsignedBigNum:
                            case CborTag.NegativeBigNum:
                                mantissa = (decimal)ReadBigInteger();
                                break;

                            default:
                                throw new FormatException("DecimalFraction tag should annotate a list of two numeric elements.");
                        }

                        break;

                    default:
                        throw new FormatException("DecimalFraction tag should annotate a list of two numeric elements.");
                }

                ReadEndArray();

                return DecimalHelpers.Reconstruct(mantissa, exponent);
            }
            catch
            {
                RestoreCheckpoint(checkpoint);
                throw;
            }
        }
    }
}

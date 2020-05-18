// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Numerics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        public CborTag ReadTag()
        {
            CborTag tag = PeekTagCore(out int additionalBytes);

            if (_isConformanceLevelCheckEnabled && !CborConformanceLevelHelpers.AllowsTags(ConformanceLevel))
            {
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_TagsNotSupported, ConformanceLevel));
            }

            AdvanceBuffer(1 + additionalBytes);
            _isTagContext = true;
            return tag;
        }

        public void ReadTag(CborTag expectedTag)
        {
            CborTag tag = PeekTagCore(out int additionalBytes);

            if (_isConformanceLevelCheckEnabled && !CborConformanceLevelHelpers.AllowsTags(ConformanceLevel))
            {
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_TagsNotSupported, ConformanceLevel));
            }

            if (expectedTag != tag)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_TagMismatch);
            }

            AdvanceBuffer(1 + additionalBytes);
            _isTagContext = true;
        }

        public CborTag PeekTag() => PeekTagCore(out int _);

        private CborTag PeekTagCore(out int additionalBytes)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Tag);
            return (CborTag)DecodeUnsignedInteger(header, GetRemainingBytes(), out additionalBytes);
        }

        // Additional tagged type support

        public DateTimeOffset ReadDateTimeOffset()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.1

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadTag(expectedTag: CborTag.DateTimeString);

                switch (PeekState())
                {
                    case CborReaderState.TextString:
                    case CborReaderState.StartTextString:
                        break;
                    default:
                        throw new FormatException(SR.Cbor_Reader_InvalidDateTimeEncoding);
                }

                string dateString = ReadTextString();

                // TODO determine if conformance levels should allow inexact date sting parsing
                if (!DateTimeOffset.TryParseExact(dateString, CborWriter.Rfc3339FormatString, null, DateTimeStyles.RoundtripKind, out DateTimeOffset result))
                {
                    throw new FormatException(SR.Cbor_Reader_InvalidDateTimeEncoding);
                }

                return result;
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        public DateTimeOffset ReadUnixTimeSeconds()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.1

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadTag(expectedTag: CborTag.UnixTimeSeconds);

                switch (PeekState())
                {
                    case CborReaderState.UnsignedInteger:
                    case CborReaderState.NegativeInteger:
                        return DateTimeOffset.FromUnixTimeSeconds(ReadInt64());

                    case CborReaderState.HalfPrecisionFloat:
                    case CborReaderState.SinglePrecisionFloat:
                    case CborReaderState.DoublePrecisionFloat:
                        double seconds = ReadDouble();

                        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
                        {
                            throw new FormatException(SR.Cbor_Reader_InvalidUnixTimeEncoding);
                        }

                        TimeSpan timespan = TimeSpan.FromSeconds(seconds);
                        return DateTimeOffset.UnixEpoch + timespan;

                    default:
                        throw new FormatException(SR.Cbor_Reader_InvalidUnixTimeEncoding);
                }
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        public BigInteger ReadBigInteger()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.2

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                bool isNegative = ReadTag() switch
                {
                    CborTag.UnsignedBigNum => false,
                    CborTag.NegativeBigNum => true,
                    _ => throw new InvalidOperationException(SR.Cbor_Reader_InvalidBigNumEncoding),
                };

                switch (PeekState())
                {
                    case CborReaderState.ByteString:
                    case CborReaderState.StartByteString:
                        break;
                    default:
                        throw new FormatException(SR.Cbor_Reader_InvalidBigNumEncoding);
                }

                byte[] unsignedBigEndianEncoding = ReadByteString();
                BigInteger unsignedValue = new BigInteger(unsignedBigEndianEncoding, isUnsigned: true, isBigEndian: true);
                return isNegative ? -1 - unsignedValue : unsignedValue;
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        public decimal ReadDecimal()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.3

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadTag(expectedTag: CborTag.DecimalFraction);

                if (PeekState() != CborReaderState.StartArray || ReadStartArray() != 2)
                {
                    throw new FormatException(SR.Cbor_Reader_InvalidDecimalEncoding);
                }

                decimal mantissa; // signed integral component of the decimal value
                long exponent;    // base-10 exponent

                switch (PeekState())
                {
                    case CborReaderState.UnsignedInteger:
                    case CborReaderState.NegativeInteger:
                        exponent = ReadInt64();
                        break;

                    default:
                        throw new FormatException(SR.Cbor_Reader_InvalidDecimalEncoding);
                }

                switch (PeekState())
                {
                    case CborReaderState.UnsignedInteger:
                        mantissa = ReadUInt64();
                        break;

                    case CborReaderState.NegativeInteger:
                        mantissa = -1m - ReadCborNegativeIntegerEncoding();
                        break;

                    case CborReaderState.Tag:
                        switch (PeekTag())
                        {
                            case CborTag.UnsignedBigNum:
                            case CborTag.NegativeBigNum:
                                mantissa = (decimal)ReadBigInteger();
                                break;

                            default:
                                throw new FormatException(SR.Cbor_Reader_InvalidDecimalEncoding);
                        }

                        break;

                    default:
                        throw new FormatException(SR.Cbor_Reader_InvalidDecimalEncoding);
                }

                ReadEndArray();

                return DecimalHelpers.Reconstruct(mantissa, exponent);
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }
    }
}

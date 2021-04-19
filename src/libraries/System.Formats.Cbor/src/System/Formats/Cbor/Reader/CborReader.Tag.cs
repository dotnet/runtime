// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Numerics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        /// <summary>Reads the next data item as a semantic tag (major type 6).</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public CborTag ReadTag()
        {
            CborTag tag = PeekTagCore(out int bytesRead);

            AdvanceBuffer(bytesRead);
            _isTagContext = true;
            return tag;
        }

        /// <summary>Reads the next data item as a semantic tag (major type 6), without advancing the reader.</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>Useful in scenarios where the semantic value decoder needs to be determined at run time.</remarks>
        [CLSCompliant(false)]
        public CborTag PeekTag() => PeekTagCore(out int _);

        /// <summary>Reads the next data item as a tagged date/time string, as described in RFC7049 section 2.4.1.</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next date item does not have the correct semantic tag.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// Invalid semantic date/time encoding.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public DateTimeOffset ReadDateTimeOffset()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.1

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadExpectedTag(expectedTag: CborTag.DateTimeString);

                switch (PeekState())
                {
                    case CborReaderState.TextString:
                    case CborReaderState.StartIndefiniteLengthTextString:
                        break;
                    default:
                        throw new CborContentException(SR.Cbor_Reader_InvalidDateTimeEncoding);
                }

                string dateString = ReadTextString();

                // TODO determine if conformance modes should allow inexact date sting parsing
                if (!DateTimeOffset.TryParseExact(dateString, CborWriter.Rfc3339FormatString, null, DateTimeStyles.RoundtripKind, out DateTimeOffset result))
                {
                    throw new CborContentException(SR.Cbor_Reader_InvalidDateTimeEncoding);
                }

                return result;
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        /// <summary>Reads the next data item as a tagged unix time in seconds, as described in RFC7049 section 2.4.1.</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next date item does not have the correct semantic tag.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// invalid semantic date/time encoding.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public DateTimeOffset ReadUnixTimeSeconds()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.1

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadExpectedTag(expectedTag: CborTag.UnixTimeSeconds);

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
                            throw new CborContentException(SR.Cbor_Reader_InvalidUnixTimeEncoding);
                        }

                        TimeSpan timespan = TimeSpan.FromSeconds(seconds);
                        return DateTimeOffset.UnixEpoch + timespan;

                    default:
                        throw new CborContentException(SR.Cbor_Reader_InvalidUnixTimeEncoding);
                }
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        /// <summary>Reads the next data item as a tagged bignum encoding, as described in RFC7049 section 2.4.2.</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next date item does not have the correct semantic tag.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// Invalid semantic bignum encoding.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
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
                    case CborReaderState.StartIndefiniteLengthByteString:
                        break;
                    default:
                        throw new CborContentException(SR.Cbor_Reader_InvalidBigNumEncoding);
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

        /// <summary>Reads the next data item as a tagged decimal fraction encoding, as described in RFC7049 section 2.4.3.</summary>
        /// <returns>The decoded value.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next date item does not have the correct semantic tag.</exception>
        /// <exception cref="OverflowException">Decoded decimal fraction is either too large or too small for a <see cref="decimal" /> value.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// Invalid semantic decimal fraction encoding.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public decimal ReadDecimal()
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.3

            Checkpoint checkpoint = CreateCheckpoint();

            try
            {
                ReadExpectedTag(expectedTag: CborTag.DecimalFraction);

                if (PeekState() != CborReaderState.StartArray || ReadStartArray() != 2)
                {
                    throw new CborContentException(SR.Cbor_Reader_InvalidDecimalEncoding);
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
                        throw new CborContentException(SR.Cbor_Reader_InvalidDecimalEncoding);
                }

                switch (PeekState())
                {
                    case CborReaderState.UnsignedInteger:
                        mantissa = ReadUInt64();
                        break;

                    case CborReaderState.NegativeInteger:
                        mantissa = -1m - ReadCborNegativeIntegerRepresentation();
                        break;

                    case CborReaderState.Tag:
                        switch (PeekTag())
                        {
                            case CborTag.UnsignedBigNum:
                            case CborTag.NegativeBigNum:
                                mantissa = (decimal)ReadBigInteger();
                                break;

                            default:
                                throw new CborContentException(SR.Cbor_Reader_InvalidDecimalEncoding);
                        }

                        break;

                    default:
                        throw new CborContentException(SR.Cbor_Reader_InvalidDecimalEncoding);
                }

                ReadEndArray();

                return CborWriter.DecimalHelpers.Reconstruct(mantissa, exponent);
            }
            catch
            {
                RestoreCheckpoint(in checkpoint);
                throw;
            }
        }

        private void ReadExpectedTag(CborTag expectedTag)
        {
            CborTag tag = PeekTagCore(out int bytesRead);

            if (expectedTag != tag)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_TagMismatch);
            }

            AdvanceBuffer(bytesRead);
            _isTagContext = true;
        }

        private CborTag PeekTagCore(out int bytesRead)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Tag);
            CborTag result = (CborTag)DecodeUnsignedInteger(header, GetRemainingBytes(), out bytesRead);

            if (_isConformanceModeCheckEnabled && !CborConformanceModeHelpers.AllowsTags(ConformanceMode))
            {
                throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_TagsNotSupported, ConformanceMode));
            }

            return result;
        }
    }
}

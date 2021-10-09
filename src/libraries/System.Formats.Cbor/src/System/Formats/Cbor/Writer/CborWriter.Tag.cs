// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        /// <summary>Assign a semantic tag (major type 6) to the next data item.</summary>
        /// <param name="tag">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        [CLSCompliant(false)]
        public void WriteTag(CborTag tag)
        {
            if (!CborConformanceModeHelpers.AllowsTags(ConformanceMode))
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_TagsNotSupported, ConformanceMode));
            }

            WriteUnsignedInteger(CborMajorType.Tag, (ulong)tag);
            _isTagContext = true;
        }

        /// <summary>Writes the provided value as a tagged date/time string, as described in RFC7049 section 2.4.1.</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteDateTimeOffset(DateTimeOffset value)
        {
            string dateString =
                value.Offset == TimeSpan.Zero ?
                value.UtcDateTime.ToString(Rfc3339FormatString) : // prefer 'Z' over '+00:00'
                value.ToString(Rfc3339FormatString);

            WriteTag(CborTag.DateTimeString);
            WriteTextString(dateString);
        }

        /// <summary>Writes a unix time in seconds as a tagged date/time value, as described in RFC7049 section 2.4.1.</summary>
        /// <param name="seconds">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteUnixTimeSeconds(long seconds)
        {
            WriteTag(CborTag.UnixTimeSeconds);
            WriteInt64(seconds);
        }

        /// <summary>Writes a unix time in seconds as a tagged date/time value, as described in RFC7049 section 2.4.1.</summary>
        /// <param name="seconds">The value to write.</param>
        /// <exception cref="ArgumentException">The <paramref name="seconds" /> parameter cannot be infinite or NaN</exception>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteUnixTimeSeconds(double seconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds))
            {
                throw new ArgumentException(SR.Cbor_Writer_ValueCannotBeInfiniteOrNaN, nameof(seconds));
            }

            WriteTag(CborTag.UnixTimeSeconds);
            WriteDouble(seconds);
        }

        /// <summary>Writes the provided value as a tagged bignum encoding, as described in RFC7049 section 2.4.2.</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteBigInteger(BigInteger value)
        {
            bool isUnsigned = value.Sign >= 0;
            BigInteger unsignedValue = isUnsigned ? value : -1 - value;
            byte[] unsignedBigEndianEncoding = unsignedValue.ToByteArray(isUnsigned: true, isBigEndian: true);

            WriteTag(isUnsigned ? CborTag.UnsignedBigNum : CborTag.NegativeBigNum);
            WriteByteString(unsignedBigEndianEncoding);
        }

        /// <summary>Writes the provided value value as a tagged decimal fraction encoding, as described in RFC7049 section 2.4.3</summary>
        /// <param name="value">The value to write.</param>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        public void WriteDecimal(decimal value)
        {
            DecimalHelpers.Deconstruct(value, out decimal mantissa, out byte scale);

            WriteTag(CborTag.DecimalFraction);
            WriteStartArray(2);
            WriteInt64(-(long)scale);

            if (-1m - ulong.MinValue <= mantissa && mantissa <= ulong.MaxValue)
            {
                if (mantissa >= 0m)
                {
                    WriteUInt64((ulong)mantissa);
                }
                else
                {
                    WriteCborNegativeIntegerRepresentation((ulong)(-1m - mantissa));
                }
            }
            else
            {
                // the mantissa can also be a BigNum
                WriteBigInteger((BigInteger)mantissa);
            }

            WriteEndArray();
        }

        internal const string Rfc3339FormatString = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

        internal static class DecimalHelpers
        {
            private const int SignMask = unchecked((int)0x80000000);
            private const int ScaleMask = 0x00ff0000;
            private const int ScaleShift = 16;
            private const int ExponentUpperBound = 28;

            /// deconstructs a decimal value into its signed integral component and negative base-10 exponent
            public static void Deconstruct(decimal value, out decimal mantissa, out byte scale)
            {
                Span<int> buf = stackalloc int[4];
                decimal.GetBits(value, buf);

                int flags = buf[3];
                bool isNegative = (flags & SignMask) == SignMask;
                mantissa = new decimal(lo: buf[0], mid: buf[1], hi: buf[2], isNegative: isNegative, scale: 0);
                scale = (byte)((flags & ScaleMask) >> ScaleShift);
            }

            /// reconstructs a decimal value out of a signed integral component and a negative base-10 exponent
            private static decimal ReconstructFromNegativeScale(decimal mantissa, byte scale)
            {
                Span<int> buf = stackalloc int[4];
                decimal.GetBits(mantissa, buf);

                int flags = buf[3];
                bool isNegative = (flags & SignMask) == SignMask;
                Debug.Assert((flags & ScaleMask) == 0, "mantissa argument should be integral.");
                return new decimal(lo: buf[0], mid: buf[1], hi: buf[2], isNegative: isNegative, scale: scale);
            }

            public static decimal Reconstruct(decimal mantissa, long exponent)
            {
                if (mantissa == 0)
                {
                    return mantissa;
                }
                else if (exponent > ExponentUpperBound)
                {
                    throw new OverflowException(SR.Cbor_Writer_DecimalOverflow);
                }
                else if (exponent >= 0)
                {
                    // for positive exponents attempt to compute a decimal
                    // representation, with risk of throwing OverflowException
                    for (; exponent >= 5; exponent -= 5)
                    {
                        mantissa *= 100_000m;
                    }

                    switch (exponent)
                    {
                        case 0: return mantissa;
                        case 1: return mantissa * 10m;
                        case 2: return mantissa * 100m;
                        case 3: return mantissa * 1000m;
                        case 4: return mantissa * 10000m;
                        default:
                            Debug.Fail("Unreachable code in decimal exponentiation logic");
                            throw new Exception();
                    }
                }
                else if (exponent >= -ExponentUpperBound)
                {
                    // exponent falls within range of decimal normal-form representation
                    return ReconstructFromNegativeScale(mantissa, (byte)(-exponent));
                }
                else
                {
                    throw new OverflowException(SR.Cbor_Writer_DecimalOverflow);
                }
            }
        }
    }
}

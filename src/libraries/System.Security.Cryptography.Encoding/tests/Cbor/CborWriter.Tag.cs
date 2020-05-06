// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        public void WriteTag(CborTag tag)
        {
            if (!CborConformanceLevelHelpers.AllowsTags(ConformanceLevel))
            {
                throw new InvalidOperationException("Tagged items are not permitted under the current conformance level.");
            }

            WriteUnsignedInteger(CborMajorType.Tag, (ulong)tag);
            _isTagContext = true;
        }

        // Additional tagged type support

        internal const string Rfc3339FormatString = "yyyy-MM-ddTHH:mm:ss.FFFFFFFK";

        public void WriteDateTimeOffset(DateTimeOffset value)
        {
            string dateString =
                value.Offset == TimeSpan.Zero ?
                value.UtcDateTime.ToString(Rfc3339FormatString) : // prefer 'Z' over '+00:00'
                value.ToString(Rfc3339FormatString);

            WriteTag(CborTag.DateTimeString);
            WriteTextString(dateString);
        }

        public void WriteUnixTimeSeconds(long seconds)
        {
            WriteTag(CborTag.UnixTimeSeconds);
            WriteInt64(seconds);
        }

        public void WriteUnixTimeSeconds(double seconds)
        {
            if (double.IsInfinity(seconds) || double.IsNaN(seconds))
            {
                throw new ArgumentException("Value cannot be infinite or NaN.", nameof(seconds));
            }

            WriteTag(CborTag.UnixTimeSeconds);
            WriteDouble(seconds);
        }

        public void WriteBigInteger(BigInteger value)
        {
            bool isUnsigned = value.Sign >= 0;
            BigInteger unsignedValue = isUnsigned ? value : -1 - value;
            byte[] unsignedBigEndianEncoding = unsignedValue.ToByteArray(isUnsigned: true, isBigEndian: true);

            WriteTag(isUnsigned ? CborTag.UnsignedBigNum : CborTag.NegativeBigNum);
            WriteByteString(unsignedBigEndianEncoding);
        }

        public void WriteDecimal(decimal value)
        {
            // implements https://tools.ietf.org/html/rfc7049#section-2.4.3
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
                    WriteCborNegativeIntegerEncoding((ulong)(-1m - mantissa));
                }
            }
            else
            {
                // the mantissa can also be a BigNum
                WriteBigInteger((BigInteger)mantissa);
            }

            WriteEndArray();
        }
    }

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
        public static decimal Reconstruct(decimal mantissa, byte scale)
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
                throw new OverflowException("Value was either too large or too small for a Decimal.");
            }
            else if (exponent >= 0)
            {
                // for positive exponents attempt to compute its decimal representation,
                // with risk of throwing OverflowException
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
                        throw new Exception("Unreachable code in decimal exponentiation logic");
                }
            }
            else if (exponent >= -ExponentUpperBound)
            {
                // exponent falls within range of decimal normal-form representation
                return Reconstruct(mantissa, (byte)(-exponent));
            }
            else
            {
                throw new OverflowException("Value was either too large or too small for a Decimal.");
            }
        }
    }
}

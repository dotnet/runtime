// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;

namespace System
{
    internal static partial class Number
    {
        internal const int BFloat16NumberBufferLength = 96 + 1 + 1; // 96 for the longest input + 1 for rounding (+1 for the null terminator)

        // Undetermined values
        private const int BFloat16Precision = 5;
        private const int BFloat16PrecisionCustomFormat = 5;

        public static string FormatBFloat16(BFloat16 value, string? format, NumberFormatInfo info)
        {
            var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);
            string result = FormatBFloat16(ref vlb, value, format, info) ?? vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static unsafe string? FormatBFloat16<TChar>(ref ValueListBuilder<TChar> vlb, BFloat16 value, ReadOnlySpan<char> format, NumberFormatInfo info) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (!BFloat16.IsFinite(value))
            {
                if (BFloat16.IsNaN(value))
                {
                    if (typeof(TChar) == typeof(char))
                    {
                        return info.NaNSymbol;
                    }
                    else
                    {
                        vlb.Append(info.NaNSymbolTChar<TChar>());
                        return null;
                    }
                }

                if (typeof(TChar) == typeof(char))
                {
                    return BFloat16.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
                }
                else
                {
                    vlb.Append(BFloat16.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
                    return null;
                }
            }

            char fmt = ParseFormatSpecifier(format, out int precision);
            byte* pDigits = stackalloc byte[BFloat16NumberBufferLength];

            if (fmt == '\0')
            {
                precision = BFloat16PrecisionCustomFormat;
            }

            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, BFloat16NumberBufferLength);
            number.IsNegative = BFloat16.IsNegative(value);

            // We need to track the original precision requested since some formats
            // accept values like 0 and others may require additional fixups.
            int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(fmt, ref precision, info, out bool isSignificantDigits);

            if ((value != default) && (!isSignificantDigits || !Grisu3.TryRunBFloat16(value, precision, ref number)))
            {
                Dragon4BFloat16(value, precision, isSignificantDigits, ref number);
            }

            number.CheckConsistency();

            // When the number is known to be roundtrippable (either because we requested it be, or
            // because we know we have enough digits to satisfy roundtrippability), we should validate
            // that the number actually roundtrips back to the original result.

            Debug.Assert(((precision != -1) && (precision < BFloat16Precision)) || (value._value == NumberToFloat<BFloat16>(ref number)._value));

            if (fmt != 0)
            {
                if (precision == -1)
                {
                    Debug.Assert((fmt == 'G') || (fmt == 'g') || (fmt == 'R') || (fmt == 'r'));

                    // For the roundtrip and general format specifiers, when returning the shortest roundtrippable
                    // string, we need to update the maximum number of digits to be the greater of number.DigitsCount
                    // or SinglePrecision. This ensures that we continue returning "pretty" strings for values with
                    // less digits. One example this fixes is "-60", which would otherwise be formatted as "-6E+01"
                    // since DigitsCount would be 1 and the formatter would almost immediately switch to scientific notation.

                    nMaxDigits = Math.Max(number.DigitsCount, BFloat16Precision);
                }
                NumberToString(ref vlb, ref number, fmt, nMaxDigits, info);
            }
            else
            {
                Debug.Assert(precision == BFloat16PrecisionCustomFormat);
                NumberToStringFormat(ref vlb, ref number, format, info);
            }
            return null;
        }

        public static bool TryFormatBFloat16<TChar>(BFloat16 value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);
            string? s = FormatBFloat16(ref vlb, value, format, info);

            Debug.Assert(s is null || typeof(TChar) == typeof(char));
            bool success = s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                vlb.TryCopyTo(destination, out charsWritten);

            vlb.Dispose();
            return success;
        }

        public static unsafe void Dragon4BFloat16(BFloat16 value, int cutoffNumber, bool isSignificantDigits, ref NumberBuffer number)
        {
            throw new NotImplementedException();
        }

        internal static partial class Grisu3
        {
            public static bool TryRunBFloat16(BFloat16 value, int requestedDigits, ref NumberBuffer number)
            {
                throw new NotImplementedException();
            }
        }
    }
}

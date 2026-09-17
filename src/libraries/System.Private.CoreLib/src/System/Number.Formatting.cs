// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    // The Format methods provided by the numeric classes convert
    // the numeric value to a string using the format string given by the
    // format parameter. If the format parameter is null or
    // an empty string, the number is formatted as if the string "G" (general
    // format) was specified. The info parameter specifies the
    // NumberFormatInfo instance to use when formatting the number. If the
    // info parameter is null or omitted, the numeric formatting information
    // is obtained from the current culture. The NumberFormatInfo supplies
    // such information as the characters to use for decimal and thousand
    // separators, and the spelling and placement of currency symbols in monetary
    // values.
    //
    // Format strings fall into two categories: Standard format strings and
    // user-defined format strings. A format string consisting of a single
    // alphabetic character (A-Z or a-z), optionally followed by a sequence of
    // digits (0-9), is a standard format string. All other format strings are
    // used-defined format strings.
    //
    // A standard format string takes the form Axx, where A is an
    // alphabetic character called the format specifier and xx is a
    // sequence of digits called the precision specifier. The format
    // specifier controls the type of formatting applied to the number and the
    // precision specifier controls the number of significant digits or decimal
    // places of the formatting operation. The following table describes the
    // supported standard formats.
    //
    // C c - Currency format. The number is
    // converted to a string that represents a currency amount. The conversion is
    // controlled by the currency format information of the NumberFormatInfo
    // used to format the number. The precision specifier indicates the desired
    // number of decimal places. If the precision specifier is omitted, the default
    // currency precision given by the NumberFormatInfo is used.
    //
    // D d - Decimal format. This format is
    // supported for integral types only. The number is converted to a string of
    // decimal digits, prefixed by a minus sign if the number is negative. The
    // precision specifier indicates the minimum number of digits desired in the
    // resulting string. If required, the number will be left-padded with zeros to
    // produce the number of digits given by the precision specifier.
    //
    // E e Engineering (scientific) format.
    // The number is converted to a string of the form
    // "-d.ddd...E+ddd" or "-d.ddd...e+ddd", where each
    // 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative, and one digit always precedes the decimal point. The
    // precision specifier indicates the desired number of digits after the decimal
    // point. If the precision specifier is omitted, a default of 6 digits after
    // the decimal point is used. The format specifier indicates whether to prefix
    // the exponent with an 'E' or an 'e'. The exponent is always consists of a
    // plus or minus sign and three digits.
    //
    // F f Fixed point format. The number is
    // converted to a string of the form "-ddd.ddd....", where each
    // 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative. The precision specifier indicates the desired number of
    // decimal places. If the precision specifier is omitted, the default numeric
    // precision given by the NumberFormatInfo is used.
    //
    // G g - General format. The number is
    // converted to the shortest possible decimal representation using fixed point
    // or scientific format. The precision specifier determines the number of
    // significant digits in the resulting string. If the precision specifier is
    // omitted, the number of significant digits is determined by the type of the
    // number being converted (10 for int, 19 for long, 7 for
    // float, 15 for double, 19 for Currency, and 29 for
    // Decimal). Trailing zeros after the decimal point are removed, and the
    // resulting string contains a decimal point only if required. The resulting
    // string uses fixed point format if the exponent of the number is less than
    // the number of significant digits and greater than or equal to -4. Otherwise,
    // the resulting string uses scientific format, and the case of the format
    // specifier controls whether the exponent is prefixed with an 'E' or an 'e'.
    //
    // N n Number format. The number is
    // converted to a string of the form "-d,ddd,ddd.ddd....", where
    // each 'd' indicates a digit (0-9). The string starts with a minus sign if the
    // number is negative. Thousand separators are inserted between each group of
    // three digits to the left of the decimal point. The precision specifier
    // indicates the desired number of decimal places. If the precision specifier
    // is omitted, the default numeric precision given by the
    // NumberFormatInfo is used.
    //
    // X x - Hexadecimal format. This format is
    // supported for integral types only. The number is converted to a string of
    // hexadecimal digits. The format specifier indicates whether to use upper or
    // lower case characters for the hexadecimal digits above 9 ('X' for 'ABCDEF',
    // and 'x' for 'abcdef'). The precision specifier indicates the minimum number
    // of digits desired in the resulting string. If required, the number will be
    // left-padded with zeros to produce the number of digits given by the
    // precision specifier.
    //
    // B b - Binary format. This format is
    // supported for integral types only. The number is converted to a string of
    // binary digits, '0' or '1'. The precision specifier indicates the minimum number
    // of digits desired in the resulting string. If required, the number will be
    // left-padded with zeros to produce the number of digits given by the
    // precision specifier.
    //
    // Some examples of standard format strings and their results are shown in the
    // table below. (The examples all assume a default NumberFormatInfo.)
    //
    // Value        Format  Result
    // 12345.6789   C       $12,345.68
    // -12345.6789  C       ($12,345.68)
    // 12345        D       12345
    // 12345        D8      00012345
    // 12345.6789   E       1.234568E+004
    // 12345.6789   E10     1.2345678900E+004
    // 12345.6789   e4      1.2346e+004
    // 12345.6789   F       12345.68
    // 12345.6789   F0      12346
    // 12345.6789   F6      12345.678900
    // 12345.6789   G       12345.6789
    // 12345.6789   G7      12345.68
    // 123456789    G7      1.234568E8
    // 12345.6789   N       12,345.68
    // 123456789    N4      123,456,789.0000
    // 0x2c45e      x       2c45e
    // 0x2c45e      X       2C45E
    // 0x2c45e      X8      0002C45E
    //
    // Format strings that do not start with an alphabetic character, or that start
    // with an alphabetic character followed by a non-digit, are called
    // user-defined format strings. The following table describes the formatting
    // characters that are supported in user defined format strings.
    //
    //
    // 0 - Digit placeholder. If the value being
    // formatted has a digit in the position where the '0' appears in the format
    // string, then that digit is copied to the output string. Otherwise, a '0' is
    // stored in that position in the output string. The position of the leftmost
    // '0' before the decimal point and the rightmost '0' after the decimal point
    // determines the range of digits that are always present in the output
    // string.
    //
    // # - Digit placeholder. If the value being
    // formatted has a digit in the position where the '#' appears in the format
    // string, then that digit is copied to the output string. Otherwise, nothing
    // is stored in that position in the output string.
    //
    // . - Decimal point. The first '.' character
    // in the format string determines the location of the decimal separator in the
    // formatted value; any additional '.' characters are ignored. The actual
    // character used as a the decimal separator in the output string is given by
    // the NumberFormatInfo used to format the number.
    //
    // , - Thousand separator and number scaling.
    // The ',' character serves two purposes. First, if the format string contains
    // a ',' character between two digit placeholders (0 or #) and to the left of
    // the decimal point if one is present, then the output will have thousand
    // separators inserted between each group of three digits to the left of the
    // decimal separator. The actual character used as a the decimal separator in
    // the output string is given by the NumberFormatInfo used to format the
    // number. Second, if the format string contains one or more ',' characters
    // immediately to the left of the decimal point, or after the last digit
    // placeholder if there is no decimal point, then the number will be divided by
    // 1000 times the number of ',' characters before it is formatted. For example,
    // the format string '0,,' will represent 100 million as just 100. Use of the
    // ',' character to indicate scaling does not also cause the formatted number
    // to have thousand separators. Thus, to scale a number by 1 million and insert
    // thousand separators you would use the format string '#,##0,,'.
    //
    // % - Percentage placeholder. The presence of
    // a '%' character in the format string causes the number to be multiplied by
    // 100 before it is formatted. The '%' character itself is inserted in the
    // output string where it appears in the format string.
    //
    // E+ E- e+ e-   - Scientific notation.
    // If any of the strings 'E+', 'E-', 'e+', or 'e-' are present in the format
    // string and are immediately followed by at least one '0' character, then the
    // number is formatted using scientific notation with an 'E' or 'e' inserted
    // between the number and the exponent. The number of '0' characters following
    // the scientific notation indicator determines the minimum number of digits to
    // output for the exponent. The 'E+' and 'e+' formats indicate that a sign
    // character (plus or minus) should always precede the exponent. The 'E-' and
    // 'e-' formats indicate that a sign character should only precede negative
    // exponents.
    //
    // \ - Literal character. A backslash character
    // causes the next character in the format string to be copied to the output
    // string as-is. The backslash itself isn't copied, so to place a backslash
    // character in the output string, use two backslashes (\\) in the format
    // string.
    //
    // 'ABC' "ABC" - Literal string. Characters
    // enclosed in single or double quotation marks are copied to the output string
    // as-is and do not affect formatting.
    //
    // ; - Section separator. The ';' character is
    // used to separate sections for positive, negative, and zero numbers in the
    // format string.
    //
    // Other - All other characters are copied to
    // the output string in the position they appear.
    //
    // For fixed point formats (formats not containing an 'E+', 'E-', 'e+', or
    // 'e-'), the number is rounded to as many decimal places as there are digit
    // placeholders to the right of the decimal point. If the format string does
    // not contain a decimal point, the number is rounded to the nearest
    // integer. If the number has more digits than there are digit placeholders to
    // the left of the decimal point, the extra digits are copied to the output
    // string immediately before the first digit placeholder.
    //
    // For scientific formats, the number is rounded to as many significant digits
    // as there are digit placeholders in the format string.
    //
    // To allow for different formatting of positive, negative, and zero values, a
    // user-defined format string may contain up to three sections separated by
    // semicolons. The results of having one, two, or three sections in the format
    // string are described in the table below.
    //
    // Sections:
    //
    // One - The format string applies to all values.
    //
    // Two - The first section applies to positive values
    // and zeros, and the second section applies to negative values. If the number
    // to be formatted is negative, but becomes zero after rounding according to
    // the format in the second section, then the resulting zero is formatted
    // according to the first section.
    //
    // Three - The first section applies to positive
    // values, the second section applies to negative values, and the third section
    // applies to zeros. The second section may be left empty (by having no
    // characters between the semicolons), in which case the first section applies
    // to all non-zero values. If the number to be formatted is non-zero, but
    // becomes zero after rounding according to the format in the first or second
    // section, then the resulting zero is formatted according to the third
    // section.
    //
    // For both standard and user-defined formatting operations on values of type
    // float and double, if the value being formatted is a NaN (Not
    // a Number) or a positive or negative infinity, then regardless of the format
    // string, the resulting string is given by the NaNSymbol,
    // PositiveInfinitySymbol, or NegativeInfinitySymbol property of
    // the NumberFormatInfo used to format the number.

    internal static partial class Number
    {
        internal const int DecimalPrecision = 29; // Decimal.DecCalc also uses this value

        /// <summary>The non-inclusive upper bound of <see cref="SmallNumberCache.Value"/>.</summary>
        /// <remarks>
        /// This is a semi-arbitrary bound. For mono, which is often used for more size-constrained workloads,
        /// we keep the size really small, supporting only single digit values.  For coreclr, we use a larger
        /// value, still relatively small but large enough to accommodate common sources of numbers to strings, e.g. HTTP success status codes.
        /// By being >= 255, it also accommodates all byte.ToString()s.  If no small numbers are ever formatted, we incur
        /// the ~2400 bytes on 64-bit for the array itself.  If all small numbers are formatted, we incur ~11,500 bytes
        /// on 64-bit for the array and all the strings.
        /// </remarks>
        private const int SmallNumberCacheLength =
#if MONO
            10;
#else
            300;
#endif
        private static class SmallNumberCache
        {
            /// <summary>Lazily-populated cache of strings for uint values in the range [0, <see cref="SmallNumberCacheLength"/>).</summary>
            internal static readonly string?[] Value = new string[SmallNumberCacheLength];
        }

        // Keep the pair's alignment equal to char so every char span can be safely reinterpreted.
        [StructLayout(LayoutKind.Sequential, Pack = sizeof(char))]
        private readonly struct DigitPair
        {
            public readonly uint Value;

            public DigitPair(uint value) => Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<char> GetFreshStringSpan(string result)
        {
            // The string has its definitive length and does not become observable until every character is initialized.
            return new Span<char>(ref result.GetRawStringData(), result.Length);
        }

        internal static string FormatDecimalIeee754<TDecimal, TValue>(TValue value, string? format, NumberFormatInfo info)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);
            NumberBuffer number = new NumberBuffer(NumberBufferKind.DecimalIeee754, stackalloc byte[TDecimal.BufferLength]);
            string result = FormatDecimalIeee754<TDecimal, TValue, char>(ref vlb, ref number, value, format, info) ?? vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        // The number buffer is created by the caller so that it shares a scope with the value list builder;
        // otherwise passing it on to the formatting helpers is a ref-safety error now that Number is not unsafe.
        private static string? FormatDecimalIeee754<TDecimal, TValue, TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, TValue value, ReadOnlySpan<char> format, NumberFormatInfo info)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (!TDecimal.IsFinite(value))
            {
                if (TDecimal.IsNaN(value))
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
                    return TDecimal.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
                }
                else
                {
                    vlb.Append(TDecimal.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
                    return null;
                }
            }
            char fmt = ParseFormatSpecifier(format, out int digits);

            DecimalIeee754ToNumber<TDecimal, TValue>(value, ref number);

            if (fmt != 0)
            {
                if (fmt is 'G' or 'R' or 'g' or 'r')
                {
                    if (fmt is 'R' or 'r')
                    {
                        // The roundtrip specifier ignores any precision specifier and is otherwise identical to the general specifier
                        fmt = (char)(fmt - ('R' - 'G'));
                        digits = -1;
                    }

                    FormatGeneralAndRoundTripDecimalIeee754(ref vlb, ref number, (char)(fmt - ('G' - 'E')), digits, info);
                }
                else
                {
                    NumberToString(ref vlb, ref number, fmt, digits, info);
                }
            }
            else
            {
                NumberToStringFormat(ref vlb, ref number, format, info);
            }

            return null;
        }

        internal static bool TryFormatDecimalIeee754<TDecimal, TValue, TChar>(TValue value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);
            NumberBuffer number = new NumberBuffer(NumberBufferKind.DecimalIeee754, stackalloc byte[TDecimal.BufferLength]);
            string? s = FormatDecimalIeee754<TDecimal, TValue, TChar>(ref vlb, ref number, value, format, info);

            Debug.Assert(s is null || typeof(TChar) == typeof(char));
            bool success = s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                vlb.TryCopyTo(destination, out charsWritten);

            vlb.Dispose();
            return success;
        }

        /// <summary>
        /// Formats <paramref name="number"/> using the general format, preserving the quantum exponent so that
        /// reparsing the result recovers the same member of the cohort.
        /// </summary>
        /// <remarks>
        /// Fixed-point notation can only spell a quantum exponent that is at or below zero, since a positive
        /// quantum would require trailing zeros that reparse as a larger coefficient. Scientific notation is
        /// therefore required whenever the quantum exponent is positive, and is otherwise picked using the same
        /// compactness heuristic as the binary floating-point types.
        /// </remarks>
        private static void FormatGeneralAndRoundTripDecimalIeee754<TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, char expChar, int nMaxDigits, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(number.Kind == NumberBufferKind.DecimalIeee754);

            bool rounded = (nMaxDigits > 0) && (nMaxDigits < number.DigitsCount);

            if (rounded)
            {
                RoundNumber(ref number, nMaxDigits, isCorrectlyRounded: false);
            }

            if (number.IsNegative)
            {
                vlb.Append(info.NegativeSignTChar<TChar>());
            }

            int digitCount = number.DigitsCount;
            ReadOnlySpan<byte> dig = number.Digits.Slice(0, digitCount);

            // `Scale` is the coefficient digit count plus the quantum exponent, so `Scale` exceeding the number
            // of significant digits means the quantum exponent is positive. Rounding drops trailing coefficient
            // digits without touching `Scale`, so the requested precision is what remains significant in that
            // case; the dropped digits are recovered as trailing zeros below.
            int significantDigits = rounded ? nMaxDigits : digitCount;

            // A zero coefficient has no stored digits but still participates as the single digit `0` when
            // computing the adjusted exponent.
            int adjustedExponent = (digitCount != 0) ? (number.Scale - 1) : number.Scale;

            if ((number.Scale > significantDigits) || (adjustedExponent < -4))
            {
                vlb.Append(TChar.CastFrom((digitCount != 0) ? (char)dig[0] : '0'));

                if (digitCount > 1)
                {
                    vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());

                    for (int i = 1; i < digitCount; i++)
                    {
                        vlb.Append(TChar.CastFrom((char)dig[i]));
                    }
                }

                FormatExponent(ref vlb, info, adjustedExponent, expChar, minDigits: 2, positiveSign: true);
                return;
            }

            int integerDigits = number.Scale;

            if (integerDigits > 0)
            {
                for (int i = 0; i < integerDigits; i++)
                {
                    // Rounding can leave fewer digits than the scale requires, in which case the remaining
                    // integer positions are trailing zeros of the rounded coefficient.
                    vlb.Append(TChar.CastFrom((i < digitCount) ? (char)dig[i] : '0'));
                }
            }
            else
            {
                vlb.Append(TChar.CastFrom('0'));
            }

            if (integerDigits < digitCount)
            {
                vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());

                for (int i = integerDigits; i < 0; i++)
                {
                    vlb.Append(TChar.CastFrom('0'));
                }

                for (int i = Math.Max(integerDigits, 0); i < digitCount; i++)
                {
                    vlb.Append(TChar.CastFrom((char)dig[i]));
                }
            }
        }

        public static string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, stackalloc byte[DecimalNumberBufferLength]);

            DecimalToNumber(ref value, ref number);

            var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

            if (fmt != 0)
            {
                NumberToString(ref vlb, ref number, fmt, digits, info);
            }
            else
            {
                NumberToStringFormat(ref vlb, ref number, format, info);
            }

            string result = vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        public static bool TryFormatDecimal<TChar>(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            char fmt = ParseFormatSpecifier(format, out int digits);

            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, stackalloc byte[DecimalNumberBufferLength]);

            DecimalToNumber(ref value, ref number);

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

            if (fmt != 0)
            {
                NumberToString(ref vlb, ref number, fmt, digits, info);
            }
            else
            {
                NumberToStringFormat(ref vlb, ref number, format, info);
            }

            bool success = vlb.TryCopyTo(destination, out charsWritten);
            vlb.Dispose();
            return success;
        }

        internal static void DecimalIeee754ToNumber<TDecimal, TValue>(TValue value, ref NumberBuffer number)
            where TDecimal : unmanaged, IDecimalIeee754ParseAndFormatInfo<TDecimal, TValue>
            where TValue : unmanaged, IBinaryInteger<TValue>
        {
            DecodedDecimalIeee754<TValue> unpackDecimal = Number.UnpackDecimalIeee754<TDecimal, TValue>(value);
            number.IsNegative = unpackDecimal.Signed;

            if (TValue.IsZero(unpackDecimal.Significand))
            {
                // A zero coefficient has no stored digits, so `Scale` carries the quantum exponent directly.
                // Every other format specifier calls `RoundNumber` (or resets `Scale` itself) before reading it.
                number.Scale = unpackDecimal.UnbiasedExponent;
                number.DigitsCount = 0;
                number.Digits[0] = (byte)'\0';
                number.CheckConsistency();
                return;
            }

            string significand = TDecimal.ToDecStr(unpackDecimal.Significand);

            Debug.Assert(significand.Length < TDecimal.BufferLength);

            for (int i = 0; i < significand.Length; i++)
            {
                number.Digits[i] = (byte)significand[i];
            }

            number.Scale = significand.Length + unpackDecimal.UnbiasedExponent;
            number.DigitsCount = significand.Length;
            number.Digits[significand.Length] = (byte)'\0';

            number.CheckConsistency();
        }

        internal static void DecimalToNumber(scoped ref decimal d, ref NumberBuffer number)
        {
            number.IsNegative = decimal.IsNegative(d);

            // Pre-compute the exact digit count from the 96-bit integer value so we can write
            // directly into digits[0..i) without a subsequent shift.
            UInt128 absValue = new UInt128((uint)d.High, ((ulong)(uint)d.Mid << 32) | (uint)d.Low);
            int i = absValue != UInt128.Zero ? FormattingHelpers.CountDigits(absValue) : 0;
            int scale = d.Scale; // capture before DecDivMod1E9 mutates d (it doesn't touch scale, but be explicit)

            number.DigitsCount = i;
            number.Scale = i - scale;

            Span<byte> digits = number.Digits;
            int index = i;
            while ((d.Mid | d.High) != 0)
            {
                index = UInt32ToDecChars(digits, index, decimal.DecDivMod1E9(ref d), 9);
            }
            UInt32ToDecChars(digits, index, d.Low, 0);

            digits[i] = (byte)'\0';
            number.CheckConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFloatingPointMaxDigitsAndPrecision(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
        {
            // We want to fast path the common case of no format and general format + precision.
            // These are commonly encountered and the full switch is otherwise large enough to show up in hot path profiles

            if (fmt == 0)
            {
                isSignificantDigits = true;
                return precision;
            }

            // Bitwise-or with space (' ') converts any uppercase character to
            // lowercase and keeps unsupported characters as something unsupported.
            fmt |= ' ';

            if (fmt == 'g')
            {
                // The general format uses the precision specifier to indicate the number of significant
                // digits to format. This defaults to the shortest roundtrippable string. Additionally,
                // given that we can't return zero significant digits, we treat 0 as returning the shortest
                // roundtrippable string as well.

                isSignificantDigits = true;

                if (precision == 0)
                {
                    precision = -1;
                    return 0;
                }
                return precision;
            }

            return Slow(fmt, ref precision, info, out isSignificantDigits);

            static int Slow(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
            {
                int maxDigits = precision;

                switch (fmt)
                {
                    case 'c':
                    {
                        // The currency format uses the precision specifier to indicate the number of
                        // decimal digits to format. This defaults to NumberFormatInfo.CurrencyDecimalDigits.

                        if (precision == -1)
                        {
                            precision = info.CurrencyDecimalDigits;
                        }
                        isSignificantDigits = false;

                        break;
                    }

                    case 'e':
                    {
                        // The exponential format uses the precision specifier to indicate the number of
                        // decimal digits to format. This defaults to 6. However, the exponential format
                        // also always formats a single integral digit, so we need to increase the precision
                        // specifier and treat it as the number of significant digits to account for this.

                        if (precision == -1)
                        {
                            precision = DefaultPrecisionExponentialFormat;
                        }

                        precision++;
                        isSignificantDigits = true;

                        break;
                    }

                    case 'f':
                    case 'n':
                    {
                        // The fixed-point and number formats use the precision specifier to indicate the number
                        // of decimal digits to format. This defaults to NumberFormatInfo.NumberDecimalDigits.

                        if (precision == -1)
                        {
                            precision = info.NumberDecimalDigits;
                        }
                        isSignificantDigits = false;

                        break;
                    }

                    case 'p':
                    {
                        // The percent format uses the precision specifier to indicate the number of
                        // decimal digits to format. This defaults to NumberFormatInfo.PercentDecimalDigits.
                        // However, the percent format also always multiplies the number by 100, so we need
                        // to increase the precision specifier to ensure we get the appropriate number of digits.

                        if (precision == -1)
                        {
                            precision = info.PercentDecimalDigits;
                        }

                        precision += 2;
                        isSignificantDigits = false;

                        break;
                    }

                    case 'r':
                    {
                        // The roundtrip format ignores the precision specifier and always returns the shortest
                        // roundtrippable string.

                        precision = -1;
                        isSignificantDigits = true;

                        break;
                    }

                    default:
                    {
                        ThrowHelper.ThrowFormatException_BadFormatSpecifier();
                        goto case 'r'; // unreachable
                    }
                }

                return maxDigits;
            }
        }

        private static void FormatFloatingPointAsHex<TNumber, TChar>(ref ValueListBuilder<TChar> vlb, TNumber value, char fmt, int precision, NumberFormatInfo info)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert((fmt | 0x20) == 'x');
            Debug.Assert(TNumber.IsFinite(value));

            bool isNegative = TNumber.IsNegative(value);

            if (isNegative)
            {
                vlb.Append(info.NegativeSignTChar<TChar>());
            }

            vlb.Append(TChar.CastFrom('0'));
            vlb.Append(TChar.CastFrom(fmt));

            ulong fraction = ExtractFractionAndBiasedExponent(value, out int exponent);

            if (fraction == 0)
            {
                // +/- 0
                vlb.Append(TChar.CastFrom('0'));

                if (precision > 0)
                {
                    vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());
                    vlb.AppendSpan(precision).Fill(TChar.CastFrom('0'));
                }

                // Exponent sign is always emitted ('+' or '-'), consistent with the 'E' format.
                vlb.Append(TChar.CastFrom(fmt == 'X' ? 'P' : 'p'));
                vlb.Append(TChar.CastFrom('+'));
                vlb.Append(TChar.CastFrom('0'));

                return;
            }

            // ExtractFractionAndBiasedExponent returns (note: despite the name, the exponent is unbiased):
            //   For normal:   fraction = (1 << DenormalMantissaBits) | mantissa, exponent = biasedExp - ExponentBias - DenormalMantissaBits
            //   For denormal: fraction = mantissa, exponent = MinBinaryExponent - DenormalMantissaBits
            //
            // We want the form: 1.xxxxx * 2^e
            // So we need to normalize so that the leading 1 bit is at bit DenormalMantissaBits.
            // For normal numbers, this is already the case.
            // For denormal numbers, we need to shift left until the leading 1 is there.

            int mantissaBits = TNumber.DenormalMantissaBits;

            if (fraction < (1UL << mantissaBits))
            {
                // Denormal: shift the leading 1 up to the implicit bit position
                int lz = BitOperations.LeadingZeroCount(fraction) - (63 - mantissaBits);
                fraction <<= lz;
                exponent -= lz;
            }

            // Now fraction has the leading 1 at bit [mantissaBits], and the remaining bits below.
            // The unbiased exponent for the value is: exponent + mantissaBits (since fraction is
            // really fraction * 2^exponent, and we want 1.xxx * 2^actualExponent).
            int actualExponent = exponent + mantissaBits;

            // Strip the implicit leading 1 to get the fractional bits
            ulong significandBits = fraction & ((1UL << mantissaBits) - 1);

            // Leading digit is normally '1' for non-zero (the implicit bit)
            int leadingDigit = 1;

            // Determine how many hex digits to emit for the fractional part
            int defaultHexDigits = (mantissaBits + 3) / 4;

            if (precision == 0)
            {
                // Round significandBits into the leading digit
                ulong half = (mantissaBits > 0) ? (1UL << (mantissaBits - 1)) : 0;
                if (significandBits > half || (significandBits == half && (leadingDigit & 1) != 0))
                {
                    leadingDigit++;
                    // leadingDigit can't exceed 2 since it started at 1
                }

                significandBits = 0;
            }

            vlb.Append(TChar.CastFrom((char)('0' + leadingDigit)));

            if (precision > 0)
            {
                ulong shifted;

                if (precision < defaultHexDigits)
                {
                    // Need to round
                    int bitsToKeep = precision * 4;
                    int bitsToDiscard = mantissaBits - bitsToKeep;

                    // bitsToDiscard is always in (0, mantissaBits) here because precision >= 1
                    // (we're in the precision > 0 branch) and precision < defaultHexDigits
                    // (checked above), so bitsToKeep < mantissaBits and bitsToDiscard > 0.
                    // For all IEEE types mantissaBits <= 52, so bitsToDiscard < 64.
                    Debug.Assert(bitsToDiscard > 0 && bitsToDiscard < 64);
                    if (bitsToDiscard > 0 && bitsToDiscard < 64)
                    {
                        ulong roundBit = 1UL << (bitsToDiscard - 1);
                        ulong discardedBits = significandBits & ((1UL << bitsToDiscard) - 1);
                        bool roundUp = discardedBits > roundBit || (discardedBits == roundBit && ((significandBits >> bitsToDiscard) & 1) != 0);

                        if (roundUp)
                        {
                            significandBits = (significandBits >> bitsToDiscard) + 1;

                            // Check if rounding overflowed into leading digit
                            if (significandBits >= (1UL << bitsToKeep))
                            {
                                significandBits = 0;
                                actualExponent++;
                            }
                        }
                        else
                        {
                            significandBits >>= bitsToDiscard;
                        }

                        shifted = significandBits << (64 - bitsToKeep);
                    }
                    else
                    {
                        shifted = significandBits << (64 - mantissaBits);
                    }
                }
                else
                {
                    shifted = significandBits << (64 - mantissaBits);
                }

                vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());

                // Emit real nibbles
                int realDigits = Math.Min(precision, defaultHexDigits);
                for (int i = 0; i < realDigits; i++)
                {
                    vlb.Append(TChar.CastFrom(fmt == 'X' ? HexConverter.ToCharUpper((int)(shifted >> 60)) : HexConverter.ToCharLower((int)(shifted >> 60))));
                    shifted <<= 4;
                }

                // Emit padding zeros (when precision > defaultHexDigits)
                int padCount = precision - realDigits;
                if (padCount > 0)
                {
                    vlb.AppendSpan(padCount).Fill(TChar.CastFrom('0'));
                }
            }
            else if (precision < 0)
            {
                // Default precision: emit significant hex digits, trimming trailing zeros.
                // Compute trailing zero nibbles from the nibble-aligned representation.
                if (significandBits != 0)
                {
                    // Align significand to nibble boundary (pad LSB so total bits = defaultHexDigits * 4),
                    // then count trailing zero nibbles via trailing zero bits.
                    int paddingBits = defaultHexDigits * 4 - mantissaBits;
                    ulong nibbleAligned = significandBits << paddingBits;
                    int trailingZeroBits = BitOperations.TrailingZeroCount(nibbleAligned);
                    int trimmedDigits = defaultHexDigits - (trailingZeroBits / 4);

                    if (trimmedDigits > 0)
                    {
                        vlb.Append(info.NumberDecimalSeparatorTChar<TChar>());

                        ulong shifted = significandBits << (64 - mantissaBits);
                        for (int i = 0; i < trimmedDigits; i++)
                        {
                            vlb.Append(TChar.CastFrom(fmt == 'X' ? HexConverter.ToCharUpper((int)(shifted >> 60)) : HexConverter.ToCharLower((int)(shifted >> 60))));
                            shifted <<= 4;
                        }
                    }
                }
            }

            // Emit exponent: p+NNN or p-NNN
            // The exponent sign is always ASCII '+'/'-' per IEEE 754 §5.12.3,
            // independent of NumberFormatInfo (which only governs the leading value sign).
            vlb.Append(TChar.CastFrom(fmt == 'X' ? 'P' : 'p'));

            if (actualExponent >= 0)
            {
                vlb.Append(TChar.CastFrom('+'));
            }
            else
            {
                vlb.Append(TChar.CastFrom('-'));
                actualExponent = -actualExponent;
            }

            // Write exponent digits
            Debug.Assert(actualExponent >= 0);
            int digitCount = FormattingHelpers.CountDigits((uint)actualExponent);
            Span<TChar> exponentBuffer = vlb.AppendSpan(digitCount);
            int exponentPos = UInt32ToDecChars<TChar>(exponentBuffer, digitCount, (uint)actualExponent);
            Debug.Assert(exponentPos == 0);
        }

        public static string FormatFloat<TNumber>(TNumber value, string? format, NumberFormatInfo info)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[TNumber.NumberBufferLength]);
            string result = FormatFloat(ref vlb, ref number, value, format, info) ?? vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        public static bool TryFormatFloat<TNumber, TChar>(TNumber value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[TNumber.NumberBufferLength]);
            string? s = FormatFloat(ref vlb, ref number, value, format, info);

            Debug.Assert(s is null || typeof(TChar) == typeof(char));
            bool success = s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                vlb.TryCopyTo(destination, out charsWritten);

            vlb.Dispose();
            return success;
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static string? FormatFloat<TNumber, TChar>(ref ValueListBuilder<TChar> vlb, ref NumberBuffer number, TNumber value, ReadOnlySpan<char> format, NumberFormatInfo info)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (!TNumber.IsFinite(value))
            {
                if (TNumber.IsNaN(value))
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
                    return TNumber.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
                }
                else
                {
                    vlb.Append(TNumber.IsNegative(value) ? info.NegativeInfinitySymbolTChar<TChar>() : info.PositiveInfinitySymbolTChar<TChar>());
                    return null;
                }
            }

            char fmt = ParseFormatSpecifier(format, out int precision);

            // Handle hex float formatting (X/x format specifier)
            if ((fmt | 0x20) == 'x')
            {
                FormatFloatingPointAsHex(ref vlb, value, fmt, precision, info);
                return null;
            }

            if (fmt == '\0')
            {
                precision = TNumber.MaxPrecisionCustomFormat;
            }

            number.IsNegative = TNumber.IsNegative(value);

            // We need to track the original precision requested since some formats
            // accept values like 0 and others may require additional fixups.
            int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(fmt, ref precision, info, out bool isSignificantDigits);

            if ((value != default) && (!isSignificantDigits || !Grisu3.TryRun(value, precision, ref number)))
            {
                Dragon4(value, precision, isSignificantDigits, ref number);
            }

            number.CheckConsistency();

            // When the number is known to be roundtrippable (either because we requested it be, or
            // because we know we have enough digits to satisfy roundtrippability), we should validate
            // that the number actually roundtrips back to the original result.

            Debug.Assert(((precision != -1) && (precision < TNumber.MaxRoundTripDigits)) || (TNumber.FloatToBits(value) == TNumber.FloatToBits(NumberToFloat<TNumber>(ref number))));

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

                    nMaxDigits = Math.Max(number.DigitsCount, TNumber.MaxRoundTripDigits);
                }
                NumberToString(ref vlb, ref number, fmt, nMaxDigits, info);
            }
            else
            {
                Debug.Assert(precision == TNumber.MaxPrecisionCustomFormat);
                NumberToStringFormat(ref vlb, ref number, format, info);
            }
            return null;
        }

        private static bool TryCopyTo<TChar>(string source, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(source != null);

            if (typeof(TChar) == typeof(char))
            {
                if (source.TryCopyTo(Unsafe.BitCast<Span<TChar>, Span<char>>(destination)))
                {
                    charsWritten = source.Length;
                    return true;
                }

                charsWritten = 0;
                return false;
            }

            Debug.Assert(typeof(TChar) == typeof(byte));

            return Encoding.UTF8.TryGetBytes(source, Unsafe.BitCast<Span<TChar>, Span<byte>>(destination), out charsWritten);
        }

        internal static char GetHexBase(char fmt)
        {
            // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
            // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code produces lowercase.
            return (char)(fmt - ('X' - 'A' + 10));
        }

        public static string FormatInt32(int value, int hexMask, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return value >= 0 ?
                    UInt32ToDecStr((uint)value) :
                    NegativeInt32ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
            }

            return FormatInt32Slow(value, hexMask, format, provider);

            static string FormatInt32Slow(int value, int hexMask, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;
                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        UInt32ToDecStr((uint)value, digits) :
                        NegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
                }
                else if (fmtUpper == 'X')
                {
                    return Int32ToHexStr(value & hexMask, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt32ToBinaryStr((uint)(value & hexMask), digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int32NumberBufferLength]);

                    Int32ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatInt32<TChar>(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt32ToDecStr((uint)value, destination, out charsWritten) :
                    TryNegativeInt32ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt32Slow(value, hexMask, format, provider, destination, out charsWritten);

            static bool TryFormatInt32Slow(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten) :
                        TryNegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt32ToHexStr(value & hexMask, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt32ToBinaryStr((uint)(value & hexMask), digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int32NumberBufferLength]);

                    Int32ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static string FormatUInt32(uint value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt32ToDecStr(value);
            }

            return FormatUInt32Slow(value, format, provider);

            static string FormatUInt32Slow(uint value, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;
                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return UInt32ToDecStr(value, digits);
                }
                else if (fmtUpper == 'X')
                {
                    return Int32ToHexStr((int)value, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt32ToBinaryStr(value, digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt32NumberBufferLength]);

                    UInt32ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatUInt32<TChar>(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt32ToDecStr(value, destination, out charsWritten);
            }

            return TryFormatUInt32Slow(value, format, provider, destination, out charsWritten);

            static bool TryFormatUInt32Slow(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt32ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt32ToHexStr((int)value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt32ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt32NumberBufferLength]);

                    UInt32ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static string FormatInt64(long value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return value >= 0 ?
                    UInt64ToDecStr((ulong)value) :
                    NegativeInt64ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
            }

            return FormatInt64Slow(value, format, provider);

            static string FormatInt64Slow(long value, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;
                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        UInt64ToDecStr((ulong)value, digits) :
                        NegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
                }
                else if (fmtUpper == 'X')
                {
                    return Int64ToHexStr(value, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt64ToBinaryStr((ulong)value, digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int64NumberBufferLength]);

                    Int64ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatInt64<TChar>(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt64ToDecStr((ulong)value, destination, out charsWritten) :
                    TryNegativeInt64ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt64Slow(value, format, provider, destination, out charsWritten);

            static bool TryFormatInt64Slow(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten) :
                        TryNegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt64ToHexStr(value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt64ToBinaryStr((ulong)value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int64NumberBufferLength]);

                    Int64ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static string FormatUInt64(ulong value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt64ToDecStr(value);
            }

            return FormatUInt64Slow(value, format, provider);

            static string FormatUInt64Slow(ulong value, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;
                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return UInt64ToDecStr(value, digits);
                }
                else if (fmtUpper == 'X')
                {
                    return Int64ToHexStr((long)value, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt64ToBinaryStr(value, digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt64NumberBufferLength]);

                    UInt64ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // expose to caller's likely-const format to trim away slow path
        public static bool TryFormatUInt64<TChar>(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt64ToDecStr(value, destination, out charsWritten);
            }

            return TryFormatUInt64Slow(value, format, provider, destination, out charsWritten);

            static bool TryFormatUInt64Slow(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt64ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt64ToHexStr((long)value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt64ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt64NumberBufferLength]);

                    UInt64ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static string FormatInt128(Int128 value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return Int128.IsPositive(value)
                     ? UInt128ToDecStr((UInt128)value, digits: -1)
                     : NegativeInt128ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSign);
            }

            return FormatInt128Slow(value, format, provider);

            static string FormatInt128Slow(Int128 value, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;

                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return Int128.IsPositive(value)
                        ? UInt128ToDecStr((UInt128)value, digits)
                        : NegativeInt128ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign);
                }
                else if (fmtUpper == 'X')
                {
                    return Int128ToHexStr(value, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt128ToBinaryStr(value, digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int128NumberBufferLength]);

                    Int128ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        public static bool TryFormatInt128<TChar>(Int128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return Int128.IsPositive(value)
                     ? TryUInt128ToDecStr((UInt128)value, digits: -1, destination, out charsWritten)
                     : TryNegativeInt128ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
            }

            return TryFormatInt128Slow(value, format, provider, destination, out charsWritten);

            static bool TryFormatInt128Slow(Int128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return Int128.IsPositive(value)
                        ? TryUInt128ToDecStr((UInt128)value, digits, destination, out charsWritten)
                        : TryNegativeInt128ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSignTChar<TChar>(), destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt128ToHexStr(value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt128ToBinaryStr(value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[Int128NumberBufferLength]);

                    Int128ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        public static string FormatUInt128(UInt128 value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt128ToDecStr(value, digits: -1);
            }

            return FormatUInt128Slow(value, format, provider);

            static string FormatUInt128Slow(UInt128 value, string? format, IFormatProvider? provider)
            {
                ReadOnlySpan<char> formatSpan = format;

                char fmt = ParseFormatSpecifier(formatSpan, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return UInt128ToDecStr(value, digits);
                }
                else if (fmtUpper == 'X')
                {
                    return Int128ToHexStr((Int128)value, GetHexBase(fmt), digits);
                }
                else if (fmtUpper == 'B')
                {
                    return UInt128ToBinaryStr((Int128)value, digits);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt128NumberBufferLength]);

                    UInt128ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, formatSpan, info);
                    }

                    string result = vlb.AsSpan().ToString();
                    vlb.Dispose();
                    return result;
                }
            }
        }

        public static bool TryFormatUInt128<TChar>(UInt128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt128ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            return TryFormatUInt128Slow(value, format, provider, destination, out charsWritten);

            static bool TryFormatUInt128Slow(UInt128 value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<TChar> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison

                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return TryUInt128ToDecStr(value, digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt128ToHexStr((Int128)value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else if (fmtUpper == 'B')
                {
                    return TryUInt128ToBinaryStr((Int128)value, digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[UInt128NumberBufferLength]);

                    UInt128ToNumber(value, ref number);

                    var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);

                    if (fmt != 0)
                    {
                        NumberToString(ref vlb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref vlb, ref number, format, info);
                    }

                    bool success = vlb.TryCopyTo(destination, out charsWritten);
                    vlb.Dispose();
                    return success;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Int32ToNumber(int value, ref NumberBuffer number)
        {
            if (value >= 0)
            {
                number.IsNegative = false;
            }
            else
            {
                number.IsNegative = true;
                value = -value;
            }

            // Pre-compute the exact digit count so we can write directly into digits[0..i) — no shift.
            int i = value != 0 ? FormattingHelpers.CountDigits((uint)value) : 0;
            number.DigitsCount = i;
            number.Scale = i;

            Span<byte> digits = number.Digits;
            UInt32ToDecChars(digits, i, (uint)value, 0);
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int32ToDecStr(int value) =>
            value >= 0 ?
                UInt32ToDecStr((uint)value) :
                NegativeInt32ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UInt32ToDecChars(uint value, Span<char> buffer)
        {
            Debug.Assert(!buffer.IsEmpty);

            int leadingDigits = 2 - (buffer.Length & 1);
            Span<DigitPair> pairs = MemoryMarshal.Cast<char, DigitPair>(buffer.Slice(leadingDigits));

            for (int i = pairs.Length - 1; (uint)i < (uint)pairs.Length; i--)
            {
                (value, uint remainder) = Math.DivRem(value, 100);
                pairs[i] = new DigitPair(GetTwoDigitsChars(remainder));
            }

            if (leadingDigits == 1)
            {
                Debug.Assert(value < 10);
                buffer[0] = (char)(value + '0');
            }
            else
            {
                Debug.Assert(value < 100);
                WriteTwoDigits(value, buffer.Slice(0, 2));
            }
        }

        private static string NegativeInt32ToDecStr(int value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt32ToDecChars((uint)(-value), buffer.Slice(sNegative.Length));
            CopyNegativeSign(sNegative, buffer);
            return result;
        }

        internal static bool TryNegativeInt32ToDecStr<TChar>(int value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            int pos = UInt32ToDecChars<TChar>(destination, bufferLength, (uint)(-value), digits);
            Debug.Assert(pos == sNegative.Length);
            CopyNegativeSign(sNegative, destination);
            return true;
        }

        private static string Int32ToHexStr(int value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            Int32ToHexChars(buffer, (uint)value, hexBase);
            return result;
        }

        internal static bool TryInt32ToHexStr<TChar>(int value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            Int32ToHexChars(destination.Slice(0, bufferLength), (uint)value, hexBase);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Int32ToHexChars<TChar>(Span<TChar> buffer, uint value, int hexBase) where TChar : unmanaged, IUtfChar<TChar>
        {
            for (int i = buffer.Length - 1; (uint)i < (uint)buffer.Length; i--)
            {
                byte digit = (byte)(value & 0xF);
                buffer[i] = TChar.CastFrom(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }

            Debug.Assert(value == 0);
        }

        private static string UInt32ToBinaryStr(uint value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 32 - (int)uint.LeadingZeroCount(value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt32ToBinaryChars(buffer, value);
            return result;
        }

        private static bool TryUInt32ToBinaryStr<TChar>(uint value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 32 - (int)uint.LeadingZeroCount(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            UInt32ToBinaryChars(destination.Slice(0, bufferLength), value);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UInt32ToBinaryChars<TChar>(Span<TChar> buffer, uint value) where TChar : unmanaged, IUtfChar<TChar>
        {
            for (int i = buffer.Length - 1; (uint)i < (uint)buffer.Length; i--)
            {
                buffer[i] = TChar.CastFrom('0' + (byte)(value & 0x1));
                value >>= 1;
            }

            Debug.Assert(value == 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UInt32ToNumber(uint value, ref NumberBuffer number)
        {
            number.IsNegative = false;

            int i = value != 0 ? FormattingHelpers.CountDigits(value) : 0;
            number.DigitsCount = i;
            number.Scale = i;

            Span<byte> digits = number.Digits;
            UInt32ToDecChars(digits, i, value, 0);
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }


        /// <summary>
        /// Writes a value [ 0000 .. 9999 ] to the start of a pre-sliced 4-element span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteFourDigits<TChar>(uint value, Span<TChar> destination) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(destination.Length >= 4);
            (value, uint remainder) = Math.DivRem(value, 100);
            WriteTwoDigits(value, destination.Slice(0, 2));
            WriteTwoDigits(remainder, destination.Slice(2, 2));
        }

        /// <summary>Writes exactly <c>destination.Length</c> digits for <paramref name="value"/> into <paramref name="destination"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteDigits<TChar>(uint value, Span<TChar> destination) where TChar : unmanaged, IUtfChar<TChar>
        {
            int cur = destination.Length - 1;
            while (cur > 0)
            {
                uint temp = '0' + value;
                value /= 10;
                destination[cur--] = TChar.CastFrom(temp - value * 10);
            }
            Debug.Assert(value < 10);
            destination[0] = TChar.CastFrom('0' + value);
        }

        internal static string UInt32ToDecStr(uint value)
        {
            // For small numbers, consult a lazily-populated cache.
            if (value < SmallNumberCacheLength)
            {
                return UInt32ToDecStrForKnownSmallNumber(value);
            }

            return UInt32ToDecStr_NoSmallNumberCheck(value);
        }

        internal static string UInt32ToDecStrForKnownSmallNumber(uint value)
        {
            Debug.Assert(value < SmallNumberCacheLength);
            return SmallNumberCache.Value[value] ?? CreateAndCacheString(value);

            [MethodImpl(MethodImplOptions.NoInlining)] // keep rare usage out of fast path
            static string CreateAndCacheString(uint value) =>
                SmallNumberCache.Value[value] = UInt32ToDecStr_NoSmallNumberCheck(value);
        }

        private static string UInt32ToDecStr_NoSmallNumberCheck(uint value)
        {
            int bufferLength = FormattingHelpers.CountDigits(value);
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt32ToDecChars(value, buffer);
            return result;
        }

        private static string UInt32ToDecStr(uint value, int digits)
        {
            if (digits <= 1)
                return UInt32ToDecStr(value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt32ToDecChars(value, buffer);
            return result;
        }

        internal static bool TryUInt32ToDecStr<TChar>(uint value, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int bufferLength = FormattingHelpers.CountDigits(value);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                int pos = UInt32ToDecChars<TChar>(destination, bufferLength, value);
                Debug.Assert(pos == 0);
                return true;
            }

            charsWritten = 0;
            return false;
        }

        internal static bool TryUInt32ToDecStr<TChar>(uint value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                int pos = digits > countedDigits ?
                    UInt32ToDecChars<TChar>(destination, bufferLength, value, digits) :
                    UInt32ToDecChars<TChar>(destination, bufferLength, value);
                Debug.Assert(pos == 0);
                return true;
            }

            charsWritten = 0;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Int64ToNumber(long value, ref NumberBuffer number)
        {
            if (value >= 0)
            {
                number.IsNegative = false;
            }
            else
            {
                number.IsNegative = true;
                value = -value;
            }

            int i = value != 0 ? FormattingHelpers.CountDigits((ulong)value) : 0;
            number.DigitsCount = i;
            number.Scale = i;

            Span<byte> digits = number.Digits;
            UInt64ToDecChars(digits, i, (ulong)value, 0);
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int64ToDecStr(long value)
        {
            return value >= 0 ?
                UInt64ToDecStr((ulong)value) :
                NegativeInt64ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static string NegativeInt64ToDecStr(long value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt64ToDecChars((ulong)(-value), buffer.Slice(sNegative.Length));
            CopyNegativeSign(sNegative, buffer);
            return result;
        }

        internal static bool TryNegativeInt64ToDecStr<TChar>(long value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            int pos = UInt64ToDecChars<TChar>(destination, bufferLength, (ulong)(-value), digits);
            Debug.Assert(pos == sNegative.Length);
            CopyNegativeSign(sNegative, destination);
            return true;
        }

        private static string Int64ToHexStr(long value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            Int64ToHexChars(buffer, (ulong)value, hexBase);
            return result;
        }

        internal static bool TryInt64ToHexStr<TChar>(long value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            Int64ToHexChars(destination.Slice(0, bufferLength), (ulong)value, hexBase);
            return true;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void Int64ToHexChars<TChar>(Span<TChar> buffer, ulong value, int hexBase) where TChar : unmanaged, IUtfChar<TChar>
        {
#if TARGET_32BIT
            if (buffer.Length > 8)
            {
                int upperLength = buffer.Length - 8;
                Int32ToHexChars(buffer.Slice(upperLength), (uint)value, hexBase);
                Int32ToHexChars(buffer.Slice(0, upperLength), (uint)(value >> 32), hexBase);
            }
            else
            {
                Debug.Assert((uint)(value >> 32) == 0);
                Int32ToHexChars(buffer, (uint)value, hexBase);
            }
#else
            for (int i = buffer.Length - 1; (uint)i < (uint)buffer.Length; i--)
            {
                byte digit = (byte)(value & 0xF);
                buffer[i] = TChar.CastFrom(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }

            Debug.Assert(value == 0);
#endif
        }

        private static string UInt64ToBinaryStr(ulong value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt64ToBinaryChars(buffer, value);
            return result;
        }

        private static bool TryUInt64ToBinaryStr<TChar>(ulong value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            UInt64ToBinaryChars(destination.Slice(0, bufferLength), value);
            return true;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void UInt64ToBinaryChars<TChar>(Span<TChar> buffer, ulong value) where TChar : unmanaged, IUtfChar<TChar>
        {
#if TARGET_32BIT
            if (buffer.Length > 32)
            {
                int upperLength = buffer.Length - 32;
                UInt32ToBinaryChars(buffer.Slice(upperLength), (uint)value);
                UInt32ToBinaryChars(buffer.Slice(0, upperLength), (uint)(value >> 32));
            }
            else
            {
                Debug.Assert((uint)(value >> 32) == 0);
                UInt32ToBinaryChars(buffer, (uint)value);
            }
#else
            for (int i = buffer.Length - 1; (uint)i < (uint)buffer.Length; i--)
            {
                buffer[i] = TChar.CastFrom('0' + (byte)(value & 0x1));
                value >>= 1;
            }

            Debug.Assert(value == 0);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UInt64ToNumber(ulong value, ref NumberBuffer number)
        {
            number.IsNegative = false;

            int i = value != 0 ? FormattingHelpers.CountDigits(value) : 0;
            number.DigitsCount = i;
            number.Scale = i;

            Span<byte> digits = number.Digits;
            UInt64ToDecChars(digits, i, value, 0);
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint Int64DivMod1E9(ref ulong value)
        {
            uint rem = (uint)(value % 1_000_000_000);
            value /= 1_000_000_000;
            return rem;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void UInt64ToDecChars(ulong value, Span<char> buffer)
        {
            Debug.Assert(!buffer.IsEmpty);

#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                Debug.Assert(buffer.Length > 9);
                int index = buffer.Length - 9;
                UInt32ToDecChars(Int64DivMod1E9(ref value), buffer.Slice(index));
                buffer = buffer.Slice(0, index);
            }
            UInt32ToDecChars((uint)value, buffer);
#else
            int leadingDigits = 2 - (buffer.Length & 1);
            Span<DigitPair> pairs = MemoryMarshal.Cast<char, DigitPair>(buffer.Slice(leadingDigits));

            for (int i = pairs.Length - 1; (uint)i < (uint)pairs.Length; i--)
            {
                (value, ulong remainder) = Math.DivRem(value, 100);
                pairs[i] = new DigitPair(GetTwoDigitsChars((uint)remainder));
            }

            if (leadingDigits == 1)
            {
                Debug.Assert(value < 10);
                buffer[0] = (char)(value + '0');
            }
            else
            {
                Debug.Assert(value < 100);
                WriteTwoDigits((uint)value, buffer.Slice(0, 2));
            }
#endif
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static int UInt64ToDecChars<TChar>(Span<TChar> buffer, int index, ulong value) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                index = UInt32ToDecChars(buffer, index, Int64DivMod1E9(ref value), 9);
            }
            return UInt32ToDecChars(buffer, index, (uint)value);
#else
            if (value >= 10)
            {
                while (value >= 100)
                {
                    index -= 2;
                    (value, ulong remainder) = Math.DivRem(value, 100);
                    WriteTwoDigits((uint)remainder, buffer, index);
                }
                if (value >= 10)
                {
                    index -= 2;
                    WriteTwoDigits((uint)value, buffer, index);
                    return index;
                }
            }
            buffer[--index] = TChar.CastFrom(value + '0');
            return index;
#endif
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static int UInt64ToDecChars<TChar>(Span<TChar> buffer, int index, ulong value, int digits) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

#if TARGET_32BIT
            while ((uint)(value >> 32) != 0)
            {
                index = UInt32ToDecChars(buffer, index, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            return UInt32ToDecChars(buffer, index, (uint)value, digits);
#else
            ulong remainder;
            while (value >= 100)
            {
                index -= 2;
                digits -= 2;
                (value, remainder) = Math.DivRem(value, 100);
                WriteTwoDigits((uint)remainder, buffer, index);
            }
            while (value != 0 || digits > 0)
            {
                digits--;
                (value, remainder) = Math.DivRem(value, 10);
                buffer[--index] = TChar.CastFrom(remainder + '0');
            }
            return index;
#endif
        }

        internal static string UInt64ToDecStr(ulong value)
        {
            // For small numbers, consult a lazily-populated cache.
            if (value < SmallNumberCacheLength)
            {
                return UInt32ToDecStrForKnownSmallNumber((uint)value);
            }

            int bufferLength = FormattingHelpers.CountDigits(value);
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt64ToDecChars(value, buffer);
            return result;
        }

        internal static string UInt64ToDecStr(ulong value, int digits)
        {
            if (digits <= 1)
            {
                return UInt64ToDecStr(value);
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt64ToDecChars(value, buffer);
            return result;
        }

        internal static bool TryUInt64ToDecStr<TChar>(ulong value, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            int bufferLength = FormattingHelpers.CountDigits(value);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                int pos = UInt64ToDecChars<TChar>(destination, bufferLength, value);
                Debug.Assert(pos == 0);
                return true;
            }

            charsWritten = 0;
            return false;
        }

        internal static bool TryUInt64ToDecStr<TChar>(ulong value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                int pos = digits > countedDigits ?
                    UInt64ToDecChars<TChar>(destination, bufferLength, value, digits) :
                    UInt64ToDecChars<TChar>(destination, bufferLength, value);
                Debug.Assert(pos == 0);
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static void Int128ToNumber(Int128 value, ref NumberBuffer number)
        {
            number.DigitsCount = Int128Precision;

            if (Int128.IsPositive(value))
            {
                number.IsNegative = false;
            }
            else
            {
                number.IsNegative = true;
                value = -value;
            }

            Span<byte> digits = number.Digits;
            int start = UInt128ToDecChars(digits, Int128Precision, (UInt128)value, 0);

            int i = Int128Precision - start;

            number.DigitsCount = i;
            number.Scale = i;

            if (start != 0)
            {
                digits.Slice(start, i).CopyTo(digits);
            }
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int128ToDecStr(Int128 value)
        {
            return Int128.IsPositive(value)
                 ? UInt128ToDecStr((UInt128)value, -1)
                 : NegativeInt128ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static string NegativeInt128ToDecStr(Int128 value, int digits, string sNegative)
        {
            Debug.Assert(Int128.IsNegative(value));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 absValue = (UInt128)(-value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(absValue)) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt128ToDecChars(absValue, buffer.Slice(sNegative.Length));
            CopyNegativeSign(sNegative, buffer);
            return result;
        }

        private static bool TryNegativeInt128ToDecStr<TChar>(Int128 value, int digits, ReadOnlySpan<TChar> sNegative, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(Int128.IsNegative(value));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 absValue = (UInt128)(-value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(absValue)) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            int pos = UInt128ToDecChars<TChar>(destination, bufferLength, absValue, digits);
            Debug.Assert(pos == sNegative.Length);
            CopyNegativeSign(sNegative, destination);
            return true;
        }

        private static string Int128ToHexStr(Int128 value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits(uValue));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            Int128ToHexChars(buffer, uValue, hexBase);
            return result;
        }

        private static bool TryInt128ToHexStr<TChar>(Int128 value, char hexBase, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits(uValue));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            Int128ToHexChars(destination.Slice(0, bufferLength), uValue, hexBase);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Int128ToHexChars<TChar>(Span<TChar> buffer, UInt128 value, int hexBase) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (buffer.Length > 16)
            {
                int upperLength = buffer.Length - 16;
                Int64ToHexChars(buffer.Slice(upperLength), value.Lower, hexBase);
                Int64ToHexChars(buffer.Slice(0, upperLength), value.Upper, hexBase);
            }
            else
            {
                Debug.Assert(value.Upper == 0);
                Int64ToHexChars(buffer, value.Lower, hexBase);
            }
        }

        private static string UInt128ToBinaryStr(Int128 value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt128ToBinaryChars(buffer, uValue);
            return result;
        }

        private static bool TryUInt128ToBinaryStr<TChar>(Int128 value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            UInt128ToBinaryChars(destination.Slice(0, bufferLength), uValue);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UInt128ToBinaryChars<TChar>(Span<TChar> buffer, UInt128 value) where TChar : unmanaged, IUtfChar<TChar>
        {
            if (buffer.Length > 64)
            {
                int upperLength = buffer.Length - 64;
                UInt64ToBinaryChars(buffer.Slice(upperLength), value.Lower);
                UInt64ToBinaryChars(buffer.Slice(0, upperLength), value.Upper);
            }
            else
            {
                Debug.Assert(value.Upper == 0);
                UInt64ToBinaryChars(buffer, value.Lower);
            }
        }

        internal static void UInt128ToNumber(UInt128 value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt128Precision;
            number.IsNegative = false;

            Span<byte> digits = number.Digits;
            int start = UInt128ToDecChars(digits, UInt128Precision, value, 0);

            int i = UInt128Precision - start;

            number.DigitsCount = i;
            number.Scale = i;

            if (start != 0)
            {
                digits.Slice(start, i).CopyTo(digits);
            }
            digits[i] = (byte)'\0';

            number.CheckConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Int128DivMod1E19(ref UInt128 value)
        {
            UInt128 divisor = new UInt128(0, 10_000_000_000_000_000_000);
            (value, UInt128 remainder) = UInt128.DivRem(value, divisor);
            return remainder.Lower;
        }

#if TARGET_64BIT
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static void UInt128ToDecChars(UInt128 value, Span<char> buffer)
        {
            Debug.Assert(!buffer.IsEmpty);

#if TARGET_32BIT
            while (value.Upper != 0)
#else
            while (buffer.Length > 19)
#endif
            {
                Debug.Assert(buffer.Length > 19);
                int index = buffer.Length - 19;
                UInt64ToDecChars(Int128DivMod1E19(ref value), buffer.Slice(index));
                buffer = buffer.Slice(0, index);
            }
            Debug.Assert(value.Upper == 0);
            UInt64ToDecChars(value.Lower, buffer);
        }

        internal static int UInt128ToDecChars<TChar>(Span<TChar> buffer, int index, UInt128 value) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (value.Upper != 0)
            {
                index = UInt64ToDecChars(buffer, index, Int128DivMod1E19(ref value), 19);
            }
            return UInt64ToDecChars(buffer, index, value.Lower);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int UInt128ToDecChars<TChar>(Span<TChar> buffer, int index, UInt128 value, int digits) where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            while (value.Upper != 0)
            {
                index = UInt64ToDecChars(buffer, index, Int128DivMod1E19(ref value), 19);
                digits -= 19;
            }
            return UInt64ToDecChars(buffer, index, value.Lower, digits);
        }

        internal static string UInt128ToDecStr(UInt128 value)
        {
            if (value.Upper == 0)
            {
                return UInt64ToDecStr(value.Lower);
            }

            int bufferLength = FormattingHelpers.CountDigits(value);
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt128ToDecChars(value, buffer);
            return result;
        }

        internal static string UInt128ToDecStr(UInt128 value, int digits)
        {
            if (digits <= 1)
            {
                return UInt128ToDecStr(value);
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            Span<char> buffer = GetFreshStringSpan(result);
            UInt128ToDecChars(value, buffer);
            return result;
        }

        private static bool TryUInt128ToDecStr<TChar>(UInt128 value, int digits, Span<TChar> destination, out int charsWritten) where TChar : unmanaged, IUtfChar<TChar>
        {
            int countedDigits = FormattingHelpers.CountDigits(value);
            int bufferLength = Math.Max(digits, countedDigits);
            if (bufferLength <= destination.Length)
            {
                charsWritten = bufferLength;
                int pos = digits > countedDigits ?
                    UInt128ToDecChars<TChar>(destination, bufferLength, value, digits) :
                    UInt128ToDecChars<TChar>(destination, bufferLength, value);
                Debug.Assert(pos == 0);
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static ulong ExtractFractionAndBiasedExponent<TNumber>(TNumber value, out int exponent)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            ulong bits = TNumber.FloatToBits(value);
            ulong fraction = (bits & TNumber.DenormalMantissaMask);
            exponent = ((int)(bits >> TNumber.DenormalMantissaBits) & TNumber.InfinityExponent);

            if (exponent != 0)
            {
                // For normalized value,
                // value = 1.fraction * 2^(exp - ExponentBias)
                //       = (1 + mantissa / 2^TrailingSignificandLength) * 2^(exp - ExponentBias)
                //       = (2^TrailingSignificandLength + mantissa) * 2^(exp - ExponentBias - TrailingSignificandLength)
                //
                // So f = (2^TrailingSignificandLength + mantissa), e = exp - ExponentBias - TrailingSignificandLength;

                fraction |= (1UL << TNumber.DenormalMantissaBits);
                exponent -= TNumber.ExponentBias + TNumber.DenormalMantissaBits;
            }
            else
            {
                // For denormalized value,
                // value = 0.fraction * 2^(MinBinaryExponent)
                //       = (mantissa / 2^TrailingSignificandLength) * 2^(MinBinaryExponent)
                //       = mantissa * 2^(MinBinaryExponent - TrailingSignificandLength)
                //       = mantissa * 2^(MinBinaryExponent - TrailingSignificandLength)
                // So f = mantissa, e = MinBinaryExponent - TrailingSignificandLength
                exponent = TNumber.MinBinaryExponent - TNumber.DenormalMantissaBits;
            }

            return fraction;
        }
    }
}

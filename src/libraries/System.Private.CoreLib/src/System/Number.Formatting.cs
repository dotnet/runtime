// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const int DecimalPrecision = 29; // Decimal.DecCalc also uses this value

        /// <summary>The non-inclusive upper bound of <see cref="s_smallNumberCache"/>.</summary>
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
        /// <summary>Lazily-populated cache of strings for uint values in the range [0, <see cref="SmallNumberCacheLength"/>).</summary>
        private static readonly string?[] s_smallNumberCache = new string[SmallNumberCacheLength];

        // Optimizations using "TwoDigits" inspired by:
        // https://engineering.fb.com/2013/03/15/developer-tools/three-optimization-tips-for-c/
#if MONO
        // Workaround for a performance regression on Mono: https://github.com/dotnet/runtime/issues/111932
        private static readonly byte[] TwoDigitsCharsAsBytes =
            MemoryMarshal.AsBytes<char>("00010203040506070809" +
                                        "10111213141516171819" +
                                        "20212223242526272829" +
                                        "30313233343536373839" +
                                        "40414243444546474849" +
                                        "50515253545556575859" +
                                        "60616263646566676869" +
                                        "70717273747576777879" +
                                        "80818283848586878889" +
                                        "90919293949596979899").ToArray();
        private static readonly byte[] TwoDigitsBytes =
                                       ("00010203040506070809"u8 +
                                        "10111213141516171819"u8 +
                                        "20212223242526272829"u8 +
                                        "30313233343536373839"u8 +
                                        "40414243444546474849"u8 +
                                        "50515253545556575859"u8 +
                                        "60616263646566676869"u8 +
                                        "70717273747576777879"u8 +
                                        "80818283848586878889"u8 +
                                        "90919293949596979899"u8).ToArray();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetTwoDigitsBytesRef(bool useChars) =>
            ref MemoryMarshal.GetArrayDataReference(useChars ? TwoDigitsCharsAsBytes : TwoDigitsBytes);
#else
        private static ReadOnlySpan<byte> TwoDigitsCharsAsBytes =>
            MemoryMarshal.AsBytes<char>("00010203040506070809" +
                                        "10111213141516171819" +
                                        "20212223242526272829" +
                                        "30313233343536373839" +
                                        "40414243444546474849" +
                                        "50515253545556575859" +
                                        "60616263646566676869" +
                                        "70717273747576777879" +
                                        "80818283848586878889" +
                                        "90919293949596979899");
        private static ReadOnlySpan<byte> TwoDigitsBytes =>
                                        "00010203040506070809"u8 +
                                        "10111213141516171819"u8 +
                                        "20212223242526272829"u8 +
                                        "30313233343536373839"u8 +
                                        "40414243444546474849"u8 +
                                        "50515253545556575859"u8 +
                                        "60616263646566676869"u8 +
                                        "70717273747576777879"u8 +
                                        "80818283848586878889"u8 +
                                        "90919293949596979899"u8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte GetTwoDigitsBytesRef(bool useChars) =>
            ref MemoryMarshal.GetReference(useChars ? TwoDigitsCharsAsBytes : TwoDigitsBytes);
#endif


        public static unsafe string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            byte* pDigits = stackalloc byte[DecimalNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, pDigits, DecimalNumberBufferLength);

            DecimalToNumber(ref value, ref number);

            char* stackPtr = stackalloc char[CharStackBufferSize];
            var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

        public static unsafe void DecimalToNumber(scoped ref decimal d, ref NumberBuffer number)
        {
            byte* buffer = number.DigitsPtr;
            number.DigitsCount = DecimalPrecision;
            number.IsNegative = decimal.IsNegative(d);

            byte* p = buffer + DecimalPrecision;
            while ((d.Mid | d.High) != 0)
            {
                p = NumberFormat<byte>.UInt32ToDecChars(p, decimal.DecDivMod1E9(ref d), 9);
            }
            p = NumberFormat<byte>.UInt32ToDecChars(p, d.Low, 0);

            int i = (int)((buffer + DecimalPrecision) - p);

            number.DigitsCount = i;
            number.Scale = i - d.Scale;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        private static int GetFloatingPointMaxDigitsAndPrecision(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
        {
            if (fmt == 0)
            {
                isSignificantDigits = true;
                return precision;
            }

            int maxDigits = precision;

            switch (fmt | 0x20)
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

                case 'g':
                    {
                        // The general format uses the precision specifier to indicate the number of significant
                        // digits to format. This defaults to the shortest roundtrippable string. Additionally,
                        // given that we can't return zero significant digits, we treat 0 as returning the shortest
                        // roundtrippable string as well.

                        if (precision == 0)
                        {
                            precision = -1;
                        }
                        isSignificantDigits = true;

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

        public static string FormatFloat<TNumber>(TNumber value, string? format, NumberFormatInfo info)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
        {
            var vlb = new ValueListBuilder<char>(stackalloc char[CharStackBufferSize]);
            string result = FormatFloat(ref vlb, value, format, info) ?? vlb.AsSpan().ToString();
            vlb.Dispose();
            return result;
        }

        public static bool TryFormatFloat<TNumber, TChar>(TNumber value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<TChar> destination, out int charsWritten)
            where TNumber : unmanaged, IBinaryFloatParseAndFormatInfo<TNumber>
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));

            var vlb = new ValueListBuilder<TChar>(stackalloc TChar[CharStackBufferSize]);
            string? s = FormatFloat(ref vlb, value, format, info);

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
        private static unsafe string? FormatFloat<TNumber, TChar>(ref ValueListBuilder<TChar> vlb, TNumber value, ReadOnlySpan<char> format, NumberFormatInfo info)
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
            byte* pDigits = stackalloc byte[TNumber.NumberBufferLength];

            if (fmt == '\0')
            {
                precision = TNumber.MaxPrecisionCustomFormat;
            }

            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, TNumber.NumberBufferLength);
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
            else
            {
                return Encoding.UTF8.TryGetBytes(source, Unsafe.BitCast<Span<TChar>, Span<byte>>(destination), out charsWritten);
            }
        }

        public static char GetHexBase(char fmt)
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

            static unsafe string FormatInt32Slow(int value, int hexMask, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[Int32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int32NumberBufferLength);

                    Int32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

        public static string FormatUInt32(uint value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt32ToDecStr(value);
            }

            return FormatUInt32Slow(value, format, provider);

            static unsafe string FormatUInt32Slow(uint value, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[UInt32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt32NumberBufferLength);

                    UInt32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

            static unsafe string FormatInt64Slow(long value, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[Int64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int64NumberBufferLength);

                    Int64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

        public static string FormatUInt64(ulong value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt64ToDecStr(value);
            }

            return FormatUInt64Slow(value, format, provider);

            static unsafe string FormatUInt64Slow(ulong value, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[UInt64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt64NumberBufferLength);

                    UInt64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

            static unsafe string FormatInt128Slow(Int128 value, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[Int128NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int128NumberBufferLength);

                    Int128ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

        public static string FormatUInt128(UInt128 value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt128ToDecStr(value, digits: -1);
            }

            return FormatUInt128Slow(value, format, provider);

            static unsafe string FormatUInt128Slow(UInt128 value, string? format, IFormatProvider? provider)
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

                    byte* pDigits = stackalloc byte[UInt128NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt128NumberBufferLength);

                    UInt128ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    var vlb = new ValueListBuilder<char>(new Span<char>(stackPtr, CharStackBufferSize));

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Int32ToNumber(int value, ref NumberBuffer number)
        {
            number.DigitsCount = Int32Precision;

            if (value >= 0)
            {
                number.IsNegative = false;
            }
            else
            {
                number.IsNegative = true;
                value = -value;
            }

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt32ToDecChars(buffer + Int32Precision, (uint)value, 0);

            int i = (int)(buffer + Int32Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int32ToDecStr(int value) =>
            value >= 0 ?
                UInt32ToDecStr((uint)value) :
                NegativeInt32ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);

        private static unsafe string NegativeInt32ToDecStr(int value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string Int32ToHexStr(int value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt32ToBinaryStr(uint value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 32 - (int)uint.LeadingZeroCount(value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt32ToBinaryChars(buffer + bufferLength, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void UInt32ToNumber(uint value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt32Precision;
            number.IsNegative = false;

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt32ToDecChars(buffer + UInt32Precision, value, 0);

            int i = (int)(buffer + UInt32Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        public static string UInt32ToDecStr(uint value)
        {
            // For small numbers, consult a lazily-populated cache.
            if (value < SmallNumberCacheLength)
            {
                return UInt32ToDecStrForKnownSmallNumber(value);
            }

            return UInt32ToDecStr_NoSmallNumberCheck(value);
        }

        private static string UInt32ToDecStrForKnownSmallNumber(uint value)
        {
            Debug.Assert(value < SmallNumberCacheLength);
            return s_smallNumberCache[value] ?? CreateAndCacheString(value);

            [MethodImpl(MethodImplOptions.NoInlining)] // keep rare usage out of fast path
            static string CreateAndCacheString(uint value) =>
                s_smallNumberCache[value] = UInt32ToDecStr_NoSmallNumberCheck(value);
        }

        private static unsafe string UInt32ToDecStr_NoSmallNumberCheck(uint value)
        {
            int bufferLength = FormattingHelpers.CountDigits(value);

            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt32ToDecChars(p, value);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt32ToDecStr(uint value, int digits)
        {
            if (digits <= 1)
                return UInt32ToDecStr(value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt32ToDecChars(p, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Int64ToNumber(long value, ref NumberBuffer number)
        {
            number.DigitsCount = Int64Precision;

            if (value >= 0)
            {
                number.IsNegative = false;
            }
            else
            {
                number.IsNegative = true;
                value = -value;
            }

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt64ToDecChars(buffer + Int64Precision, (ulong)value, 0);

            int i = (int)(buffer + Int64Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int64ToDecStr(long value)
        {
            return value >= 0 ?
                UInt64ToDecStr((ulong)value) :
                NegativeInt64ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static unsafe string NegativeInt64ToDecStr(long value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-value))) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt64ToDecChars(buffer + bufferLength, (ulong)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string Int64ToHexStr(long value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.Int64ToHexChars(buffer + bufferLength, (ulong)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt64ToBinaryStr(ulong value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            int bufferLength = Math.Max(digits, 64 - (int)ulong.LeadingZeroCount(value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt64ToBinaryChars(buffer + bufferLength, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void UInt64ToNumber(ulong value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt64Precision;
            number.IsNegative = false;

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt64ToDecChars(buffer + UInt64Precision, value, 0);

            int i = (int)(buffer + UInt64Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Int64DivMod1E9(ref ulong value)
        {
            uint rem = (uint)(value % 1_000_000_000);
            value /= 1_000_000_000;
            return rem;
        }

        public static unsafe string UInt64ToDecStr(ulong value)
        {
            // For small numbers, consult a lazily-populated cache.
            if (value < SmallNumberCacheLength)
            {
                return UInt32ToDecStrForKnownSmallNumber((uint)value);
            }

            int bufferLength = FormattingHelpers.CountDigits(value);

            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt64ToDecChars(p, value);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt64ToDecStr(ulong value, int digits)
        {
            if (digits <= 1)
            {
                return UInt64ToDecStr(value);
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt64ToDecChars(p, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        public static unsafe void Int128ToNumber(Int128 value, ref NumberBuffer number)
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

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt128ToDecChars(buffer + Int128Precision, (UInt128)value, 0);

            int i = (int)(buffer + Int128Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        public static string Int128ToDecStr(Int128 value)
        {
            return Int128.IsPositive(value)
                 ? UInt128ToDecStr((UInt128)value, -1)
                 : NegativeInt128ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static unsafe string NegativeInt128ToDecStr(Int128 value, int digits, string sNegative)
        {
            Debug.Assert(Int128.IsNegative(value));

            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 absValue = (UInt128)(-value);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(absValue)) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt128ToDecChars(buffer + bufferLength, absValue, digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string Int128ToHexStr(Int128 value, char hexBase, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits(uValue));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.Int128ToHexChars(buffer + bufferLength, uValue, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt128ToBinaryStr(Int128 value, int digits)
        {
            if (digits < 1)
            {
                digits = 1;
            }

            UInt128 uValue = (UInt128)value;

            int bufferLength = Math.Max(digits, 128 - (int)UInt128.LeadingZeroCount((UInt128)value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = NumberFormat<char>.UInt128ToBinaryChars(buffer + bufferLength, uValue, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        public static unsafe void UInt128ToNumber(UInt128 value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt128Precision;
            number.IsNegative = false;

            byte* buffer = number.DigitsPtr;
            byte* p = NumberFormat<byte>.UInt128ToDecChars(buffer + UInt128Precision, value, 0);

            int i = (int)(buffer + UInt128Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.DigitsPtr;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)'\0';

            number.CheckConsistency();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong Int128DivMod1E19(ref UInt128 value)
        {
            UInt128 divisor = new UInt128(0, 10_000_000_000_000_000_000);
            (value, UInt128 remainder) = UInt128.DivRem(value, divisor);
            return remainder.Lower;
        }

        public static unsafe string UInt128ToDecStr(UInt128 value)
        {
            if (value.Upper == 0)
            {
                return UInt64ToDecStr(value.Lower);
            }

            int bufferLength = FormattingHelpers.CountDigits(value);

            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt128ToDecChars(p, value);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe string UInt128ToDecStr(UInt128 value, int digits)
        {
            if (digits <= 1)
            {
                return UInt128ToDecStr(value);
            }

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                p = NumberFormat<char>.UInt128ToDecChars(p, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
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

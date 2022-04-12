// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics;
using System.Globalization;
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

        // SinglePrecision and DoublePrecision represent the maximum number of digits required
        // to guarantee that any given Single or Double can roundtrip. Some numbers may require
        // less, but none will require more.
        private const int HalfPrecision = 5;
        private const int SinglePrecision = 9;
        private const int DoublePrecision = 17;

        // SinglePrecisionCustomFormat and DoublePrecisionCustomFormat are used to ensure that
        // custom format strings return the same string as in previous releases when the format
        // would return x digits or less (where x is the value of the corresponding constant).
        // In order to support more digits, we would need to update ParseFormatSpecifier to pre-parse
        // the format and determine exactly how many digits are being requested and whether they
        // represent "significant digits" or "digits after the decimal point".
        private const int HalfPrecisionCustomFormat = 5;
        private const int SinglePrecisionCustomFormat = 7;
        private const int DoublePrecisionCustomFormat = 15;

        private const int DefaultPrecisionExponentialFormat = 6;

        private const int MaxUInt32DecDigits = 10;
        private const int CharStackBufferSize = 32;
        private const string PosNumberFormat = "#";

        private static readonly string[] s_singleDigitStringCache = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private static readonly string[] s_posCurrencyFormats =
        {
            "$#", "#$", "$ #", "# $"
        };

        private static readonly string[] s_negCurrencyFormats =
        {
            "($#)", "-$#", "$-#", "$#-",
            "(#$)", "-#$", "#-$", "#$-",
            "-# $", "-$ #", "# $-", "$ #-",
            "$ -#", "#- $", "($ #)", "(# $)",
            "$- #"
        };

        private static readonly string[] s_posPercentFormats =
        {
            "# %", "#%", "%#", "% #"
        };

        private static readonly string[] s_negPercentFormats =
        {
            "-# %", "-#%", "-%#",
            "%-#", "%#-",
            "#-%", "#%-",
            "-% #", "# %-", "% #-",
            "% -#", "#- %"
        };

        private static readonly string[] s_negNumberFormats =
        {
            "(#)", "-#", "- #", "#-", "# -",
        };

        public static unsafe string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            byte* pDigits = stackalloc byte[DecimalNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, pDigits, DecimalNumberBufferLength);

            DecimalToNumber(ref value, ref number);

            char* stackPtr = stackalloc char[CharStackBufferSize];
            ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }

            return sb.ToString();
        }

        public static unsafe bool TryFormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            byte* pDigits = stackalloc byte[DecimalNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, pDigits, DecimalNumberBufferLength);

            DecimalToNumber(ref value, ref number);

            char* stackPtr = stackalloc char[CharStackBufferSize];
            ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }

            return sb.TryCopyTo(destination, out charsWritten);
        }

        internal static unsafe void DecimalToNumber(ref decimal d, ref NumberBuffer number)
        {
            byte* buffer = number.GetDigitsPointer();
            number.DigitsCount = DecimalPrecision;
            number.IsNegative = decimal.IsNegative(d);

            byte* p = buffer + DecimalPrecision;
            while ((d.Mid | d.High) != 0)
            {
                p = UInt32ToDecChars(p, decimal.DecDivMod1E9(ref d), 9);
            }
            p = UInt32ToDecChars(p, d.Low, 0);

            int i = (int)((buffer + DecimalPrecision) - p);

            number.DigitsCount = i;
            number.Scale = i - d.Scale;

            byte* dst = number.GetDigitsPointer();
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = (byte)('\0');

            number.CheckConsistency();
        }

        public static string FormatDouble(double value, string? format, NumberFormatInfo info)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            return FormatDouble(ref sb, value, format, info) ?? sb.ToString();
        }

        public static bool TryFormatDouble(double value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            string? s = FormatDouble(ref sb, value, format, info);
            return s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                sb.TryCopyTo(destination, out charsWritten);
        }

        private static int GetFloatingPointMaxDigitsAndPrecision(char fmt, ref int precision, NumberFormatInfo info, out bool isSignificantDigits)
        {
            if (fmt == 0)
            {
                isSignificantDigits = true;
                return precision;
            }

            int maxDigits = precision;

            switch (fmt)
            {
                case 'C':
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

                case 'E':
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

                case 'F':
                case 'f':
                case 'N':
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

                case 'G':
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

                case 'P':
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

                case 'R':
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
                    throw new FormatException(SR.Argument_BadFormatSpecifier);
                }
            }

            return maxDigits;
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static unsafe string? FormatDouble(ref ValueStringBuilder sb, double value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            if (!double.IsFinite(value))
            {
                if (double.IsNaN(value))
                {
                    return info.NaNSymbol;
                }

                return double.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
            }

            char fmt = ParseFormatSpecifier(format, out int precision);
            byte* pDigits = stackalloc byte[DoubleNumberBufferLength];

            if (fmt == '\0')
            {
                // For back-compat we currently specially treat the precision for custom
                // format specifiers. The constant has more details as to why.
                precision = DoublePrecisionCustomFormat;
            }

            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, DoubleNumberBufferLength);
            number.IsNegative = double.IsNegative(value);

            // We need to track the original precision requested since some formats
            // accept values like 0 and others may require additional fixups.
            int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(fmt, ref precision, info, out bool isSignificantDigits);

            if ((value != 0.0) && (!isSignificantDigits || !Grisu3.TryRunDouble(value, precision, ref number)))
            {
                Dragon4Double(value, precision, isSignificantDigits, ref number);
            }

            number.CheckConsistency();

            // When the number is known to be roundtrippable (either because we requested it be, or
            // because we know we have enough digits to satisfy roundtrippability), we should validate
            // that the number actually roundtrips back to the original result.

            Debug.Assert(((precision != -1) && (precision < DoublePrecision)) || (BitConverter.DoubleToInt64Bits(value) == BitConverter.DoubleToInt64Bits(NumberToDouble(ref number))));

            if (fmt != 0)
            {
                if (precision == -1)
                {
                    Debug.Assert((fmt == 'G') || (fmt == 'g') || (fmt == 'R') || (fmt == 'r'));

                    // For the roundtrip and general format specifiers, when returning the shortest roundtrippable
                    // string, we need to update the maximum number of digits to be the greater of number.DigitsCount
                    // or DoublePrecision. This ensures that we continue returning "pretty" strings for values with
                    // less digits. One example this fixes is "-60", which would otherwise be formatted as "-6E+01"
                    // since DigitsCount would be 1 and the formatter would almost immediately switch to scientific notation.

                    nMaxDigits = Math.Max(number.DigitsCount, DoublePrecision);
                }
                NumberToString(ref sb, ref number, fmt, nMaxDigits, info);
            }
            else
            {
                Debug.Assert(precision == DoublePrecisionCustomFormat);
                NumberToStringFormat(ref sb, ref number, format, info);
            }
            return null;
        }

        public static string FormatSingle(float value, string? format, NumberFormatInfo info)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            return FormatSingle(ref sb, value, format, info) ?? sb.ToString();
        }

        public static bool TryFormatSingle(float value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            string? s = FormatSingle(ref sb, value, format, info);
            return s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                sb.TryCopyTo(destination, out charsWritten);
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static unsafe string? FormatSingle(ref ValueStringBuilder sb, float value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            if (!float.IsFinite(value))
            {
                if (float.IsNaN(value))
                {
                    return info.NaNSymbol;
                }

                return float.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
            }

            char fmt = ParseFormatSpecifier(format, out int precision);
            byte* pDigits = stackalloc byte[SingleNumberBufferLength];

            if (fmt == '\0')
            {
                // For back-compat we currently specially treat the precision for custom
                // format specifiers. The constant has more details as to why.
                precision = SinglePrecisionCustomFormat;
            }

            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, SingleNumberBufferLength);
            number.IsNegative = float.IsNegative(value);

            // We need to track the original precision requested since some formats
            // accept values like 0 and others may require additional fixups.
            int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(fmt, ref precision, info, out bool isSignificantDigits);

            if ((value != default) && (!isSignificantDigits || !Grisu3.TryRunSingle(value, precision, ref number)))
            {
                Dragon4Single(value, precision, isSignificantDigits, ref number);
            }

            number.CheckConsistency();

            // When the number is known to be roundtrippable (either because we requested it be, or
            // because we know we have enough digits to satisfy roundtrippability), we should validate
            // that the number actually roundtrips back to the original result.

            Debug.Assert(((precision != -1) && (precision < SinglePrecision)) || (BitConverter.SingleToInt32Bits(value) == BitConverter.SingleToInt32Bits(NumberToSingle(ref number))));

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

                    nMaxDigits = Math.Max(number.DigitsCount, SinglePrecision);
                }
                NumberToString(ref sb, ref number, fmt, nMaxDigits, info);
            }
            else
            {
                Debug.Assert(precision == SinglePrecisionCustomFormat);
                NumberToStringFormat(ref sb, ref number, format, info);
            }
            return null;
        }

        public static string FormatHalf(Half value, string? format, NumberFormatInfo info)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            return FormatHalf(ref sb, value, format, info) ?? sb.ToString();
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static unsafe string? FormatHalf(ref ValueStringBuilder sb, Half value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            if (!Half.IsFinite(value))
            {
                if (Half.IsNaN(value))
                {
                    return info.NaNSymbol;
                }

                return Half.IsNegative(value) ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
            }

            char fmt = ParseFormatSpecifier(format, out int precision);
            byte* pDigits = stackalloc byte[HalfNumberBufferLength];

            if (fmt == '\0')
            {
                precision = HalfPrecisionCustomFormat;
            }

            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, HalfNumberBufferLength);
            number.IsNegative = Half.IsNegative(value);

            // We need to track the original precision requested since some formats
            // accept values like 0 and others may require additional fixups.
            int nMaxDigits = GetFloatingPointMaxDigitsAndPrecision(fmt, ref precision, info, out bool isSignificantDigits);

            if ((value != default) && (!isSignificantDigits || !Grisu3.TryRunHalf(value, precision, ref number)))
            {
                Dragon4Half(value, precision, isSignificantDigits, ref number);
            }

            number.CheckConsistency();

            // When the number is known to be roundtrippable (either because we requested it be, or
            // because we know we have enough digits to satisfy roundtrippability), we should validate
            // that the number actually roundtrips back to the original result.

            Debug.Assert(((precision != -1) && (precision < HalfPrecision)) || (BitConverter.HalfToInt16Bits(value) == BitConverter.HalfToInt16Bits(NumberToHalf(ref number))));

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

                    nMaxDigits = Math.Max(number.DigitsCount, HalfPrecision);
                }
                NumberToString(ref sb, ref number, fmt, nMaxDigits, info);
            }
            else
            {
                Debug.Assert(precision == HalfPrecisionCustomFormat);
                NumberToStringFormat(ref sb, ref number, format, info);
            }
            return null;
        }

        public static bool TryFormatHalf(Half value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            var sb = new ValueStringBuilder(stackalloc char[CharStackBufferSize]);
            string? s = FormatHalf(ref sb, value, format, info);
            return s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                sb.TryCopyTo(destination, out charsWritten);
        }


        private static bool TryCopyTo(string source, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(source != null);

            if (source.TryCopyTo(destination))
            {
                charsWritten = source.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        private static char GetHexBase(char fmt)
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Int32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int32NumberBufferLength);

                    Int32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, formatSpan, info);
                    }
                    return sb.ToString();
                }
            }
        }

        public static bool TryFormatInt32(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt32ToDecStr((uint)value, digits: -1, destination, out charsWritten) :
                    TryNegativeInt32ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
            }

            return TryFormatInt32Slow(value, hexMask, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatInt32Slow(int value, int hexMask, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten) :
                        TryNegativeInt32ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt32ToHexStr(value & hexMask, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Int32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int32NumberBufferLength);

                    Int32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, format, info);
                    }
                    return sb.TryCopyTo(destination, out charsWritten);
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[UInt32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt32NumberBufferLength);

                    UInt32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, formatSpan, info);
                    }
                    return sb.ToString();
                }
            }
        }

        public static bool TryFormatUInt32(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt32ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            return TryFormatUInt32Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatUInt32Slow(uint value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[UInt32NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt32NumberBufferLength);

                    UInt32ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, format, info);
                    }
                    return sb.TryCopyTo(destination, out charsWritten);
                }
            }
        }

        public static string FormatInt64(long value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return value >= 0 ?
                    UInt64ToDecStr((ulong)value, digits: -1) :
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Int64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int64NumberBufferLength);

                    Int64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, formatSpan, info);
                    }
                    return sb.ToString();
                }
            }
        }

        public static bool TryFormatInt64(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return value >= 0 ?
                    TryUInt64ToDecStr((ulong)value, digits: -1, destination, out charsWritten) :
                    TryNegativeInt64ToDecStr(value, digits: -1, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
            }

            return TryFormatInt64Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatInt64Slow(long value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
            {
                char fmt = ParseFormatSpecifier(format, out int digits);
                char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
                if (fmtUpper == 'G' ? digits < 1 : fmtUpper == 'D')
                {
                    return value >= 0 ?
                        TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten) :
                        TryNegativeInt64ToDecStr(value, digits, NumberFormatInfo.GetInstance(provider).NegativeSign, destination, out charsWritten);
                }
                else if (fmtUpper == 'X')
                {
                    return TryInt64ToHexStr(value, GetHexBase(fmt), digits, destination, out charsWritten);
                }
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[Int64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int64NumberBufferLength);

                    Int64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, format, info);
                    }
                    return sb.TryCopyTo(destination, out charsWritten);
                }
            }
        }

        public static string FormatUInt64(ulong value, string? format, IFormatProvider? provider)
        {
            // Fast path for default format
            if (string.IsNullOrEmpty(format))
            {
                return UInt64ToDecStr(value, digits: -1);
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[UInt64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt64NumberBufferLength);

                    UInt64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, formatSpan, info);
                    }
                    return sb.ToString();
                }
            }
        }

        public static bool TryFormatUInt64(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt64ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            return TryFormatUInt64Slow(value, format, provider, destination, out charsWritten);

            static unsafe bool TryFormatUInt64Slow(ulong value, ReadOnlySpan<char> format, IFormatProvider? provider, Span<char> destination, out int charsWritten)
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
                else
                {
                    NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

                    byte* pDigits = stackalloc byte[UInt64NumberBufferLength];
                    NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt64NumberBufferLength);

                    UInt64ToNumber(value, ref number);

                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    ValueStringBuilder sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));

                    if (fmt != 0)
                    {
                        NumberToString(ref sb, ref number, fmt, digits, info);
                    }
                    else
                    {
                        NumberToStringFormat(ref sb, ref number, format, info);
                    }
                    return sb.TryCopyTo(destination, out charsWritten);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called from only one location
        private static unsafe void Int32ToNumber(int value, ref NumberBuffer number)
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

            byte* buffer = number.GetDigitsPointer();
            byte* p = UInt32ToDecChars(buffer + Int32Precision, (uint)value, 0);

            int i = (int)(buffer + Int32Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.GetDigitsPointer();
            while (--i >= 0)
                *dst++ = *p++;
            *dst = (byte)('\0');

            number.CheckConsistency();
        }

        public static string Int32ToDecStr(int value)
        {
            return value >= 0 ?
                UInt32ToDecStr((uint)value) :
                NegativeInt32ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static unsafe string NegativeInt32ToDecStr(int value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryNegativeInt32ToDecStr(int value, int digits, string sNegative, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((uint)(-value))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        private static unsafe string Int32ToHexStr(int value, char hexBase, int digits)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryInt32ToHexStr(int value, char hexBase, int digits, Span<char> destination, out int charsWritten)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((uint)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        private static unsafe char* Int32ToHexChars(char* buffer, uint value, int hexBase, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                byte digit = (byte)(value & 0xF);
                *(--buffer) = (char)(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }
            return buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called from only one location
        private static unsafe void UInt32ToNumber(uint value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt32Precision;
            number.IsNegative = false;

            byte* buffer = number.GetDigitsPointer();
            byte* p = UInt32ToDecChars(buffer + UInt32Precision, value, 0);

            int i = (int)(buffer + UInt32Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.GetDigitsPointer();
            while (--i >= 0)
                *dst++ = *p++;
            *dst = (byte)('\0');

            number.CheckConsistency();
        }

        internal static unsafe byte* UInt32ToDecChars(byte* bufferEnd, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                uint remainder;
                (value, remainder) = Math.DivRem(value, 10);
                *(--bufferEnd) = (byte)(remainder + '0');
            }
            return bufferEnd;
        }

        internal static unsafe char* UInt32ToDecChars(char* bufferEnd, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                uint remainder;
                (value, remainder) = Math.DivRem(value, 10);
                *(--bufferEnd) = (char)(remainder + '0');
            }
            return bufferEnd;
        }

        internal static unsafe string UInt32ToDecStr(uint value)
        {
            // Intrinsified in mono interpreter
            int bufferLength = FormattingHelpers.CountDigits(value);

            // For single-digit values that are very common, especially 0 and 1, just return cached strings.
            if (bufferLength == 1)
            {
                return s_singleDigitStringCache[value];
            }

            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                do
                {
                    uint remainder;
                    (value, remainder) = Math.DivRem(value, 10);
                    *(--p) = (char)(remainder + '0');
                }
                while (value != 0);
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
                p = UInt32ToDecChars(p, value, digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryUInt32ToDecStr(uint value, int digits, Span<char> destination, out int charsWritten)
        {
            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = buffer + bufferLength;
                if (digits <= 1)
                {
                    do
                    {
                        uint remainder;
                        (value, remainder) = Math.DivRem(value, 10);
                        *(--p) = (char)(remainder + '0');
                    }
                    while (value != 0);
                }
                else
                {
                    p = UInt32ToDecChars(p, value, digits);
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        private static unsafe void Int64ToNumber(long input, ref NumberBuffer number)
        {
            ulong value = (ulong)input;
            number.IsNegative = input < 0;
            number.DigitsCount = Int64Precision;
            if (number.IsNegative)
            {
                value = (ulong)(-input);
            }

            byte* buffer = number.GetDigitsPointer();
            byte* p = buffer + Int64Precision;
            while (High32(value) != 0)
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
            p = UInt32ToDecChars(p, Low32(value), 0);

            int i = (int)(buffer + Int64Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.GetDigitsPointer();
            while (--i >= 0)
                *dst++ = *p++;
            *dst = (byte)('\0');

            number.CheckConsistency();
        }

        public static string Int64ToDecStr(long value)
        {
            return value >= 0 ?
                UInt64ToDecStr((ulong)value, -1) :
                NegativeInt64ToDecStr(value, -1, NumberFormatInfo.CurrentInfo.NegativeSign);
        }

        private static unsafe string NegativeInt64ToDecStr(long input, int digits, string sNegative)
        {
            Debug.Assert(input < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            ulong value = (ulong)(-input);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value)) + sNegative.Length;
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                while (High32(value) != 0)
                {
                    p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                    digits -= 9;
                }
                p = UInt32ToDecChars(p, Low32(value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryNegativeInt64ToDecStr(long input, int digits, string sNegative, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(input < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            ulong value = (ulong)(-input);

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits((ulong)(-input))) + sNegative.Length;
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = buffer + bufferLength;
                while (High32(value) != 0)
                {
                    p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                    digits -= 9;
                }
                p = UInt32ToDecChars(p, Low32(value), digits);
                Debug.Assert(p == buffer + sNegative.Length);

                for (int i = sNegative.Length - 1; i >= 0; i--)
                {
                    *(--p) = sNegative[i];
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        private static unsafe string Int64ToHexStr(long value, char hexBase, int digits)
        {
            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                if (High32((ulong)value) != 0)
                {
                    p = Int32ToHexChars(p, Low32((ulong)value), hexBase, 8);
                    p = Int32ToHexChars(p, High32((ulong)value), hexBase, digits - 8);
                }
                else
                {
                    p = Int32ToHexChars(p, Low32((ulong)value), hexBase, Math.Max(digits, 1));
                }
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryInt64ToHexStr(long value, char hexBase, int digits, Span<char> destination, out int charsWritten)
        {
            int bufferLength = Math.Max(digits, FormattingHelpers.CountHexDigits((ulong)value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = buffer + bufferLength;
                if (High32((ulong)value) != 0)
                {
                    p = Int32ToHexChars(p, Low32((ulong)value), hexBase, 8);
                    p = Int32ToHexChars(p, High32((ulong)value), hexBase, digits - 8);
                }
                else
                {
                    p = Int32ToHexChars(p, Low32((ulong)value), hexBase, Math.Max(digits, 1));
                }
                Debug.Assert(p == buffer);
            }
            return true;
        }

        private static unsafe void UInt64ToNumber(ulong value, ref NumberBuffer number)
        {
            number.DigitsCount = UInt64Precision;
            number.IsNegative = false;

            byte* buffer = number.GetDigitsPointer();
            byte* p = buffer + UInt64Precision;

            while (High32(value) != 0)
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
            p = UInt32ToDecChars(p, Low32(value), 0);

            int i = (int)(buffer + UInt64Precision - p);

            number.DigitsCount = i;
            number.Scale = i;

            byte* dst = number.GetDigitsPointer();
            while (--i >= 0)
                *dst++ = *p++;
            *dst = (byte)('\0');

            number.CheckConsistency();
        }

        internal static unsafe string UInt64ToDecStr(ulong value, int digits)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));

            // For single-digit values that are very common, especially 0 and 1, just return cached strings.
            if (bufferLength == 1)
            {
                return s_singleDigitStringCache[value];
            }

            string result = string.FastAllocateString(bufferLength);
            fixed (char* buffer = result)
            {
                char* p = buffer + bufferLength;
                while (High32(value) != 0)
                {
                    p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                    digits -= 9;
                }
                p = UInt32ToDecChars(p, Low32(value), digits);
                Debug.Assert(p == buffer);
            }
            return result;
        }

        private static unsafe bool TryUInt64ToDecStr(ulong value, int digits, Span<char> destination, out int charsWritten)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, FormattingHelpers.CountDigits(value));
            if (bufferLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = bufferLength;
            fixed (char* buffer = &MemoryMarshal.GetReference(destination))
            {
                char* p = buffer + bufferLength;
                while (High32(value) != 0)
                {
                    p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                    digits -= 9;
                }
                p = UInt32ToDecChars(p, Low32(value), digits);
                Debug.Assert(p == buffer);
            }
            return true;
        }

        internal static unsafe char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
        {
            char c = default;
            if (format.Length > 0)
            {
                // If the format begins with a symbol, see if it's a standard format
                // with or without a specified number of digits.
                c = format[0];
                if ((uint)(c - 'A') <= 'Z' - 'A' ||
                    (uint)(c - 'a') <= 'z' - 'a')
                {
                    // Fast path for sole symbol, e.g. "D"
                    if (format.Length == 1)
                    {
                        digits = -1;
                        return c;
                    }

                    if (format.Length == 2)
                    {
                        // Fast path for symbol and single digit, e.g. "X4"
                        int d = format[1] - '0';
                        if ((uint)d < 10)
                        {
                            digits = d;
                            return c;
                        }
                    }
                    else if (format.Length == 3)
                    {
                        // Fast path for symbol and double digit, e.g. "F12"
                        int d1 = format[1] - '0', d2 = format[2] - '0';
                        if ((uint)d1 < 10 && (uint)d2 < 10)
                        {
                            digits = d1 * 10 + d2;
                            return c;
                        }
                    }

                    // Fallback for symbol and any length digits.  The digits value must be >= 0 && <= 99,
                    // but it can begin with any number of 0s, and thus we may need to check more than two
                    // digits.  Further, for compat, we need to stop when we hit a null char.
                    int n = 0;
                    int i = 1;
                    while (i < format.Length && (((uint)format[i] - '0') < 10))
                    {
                        int temp = ((n * 10) + format[i++] - '0');
                        if (temp < n)
                        {
                            throw new FormatException(SR.Argument_BadFormatSpecifier);
                        }
                        n = temp;
                    }

                    // If we're at the end of the digits rather than having stopped because we hit something
                    // other than a digit or overflowed, return the standard format info.
                    if (i == format.Length || format[i] == '\0')
                    {
                        digits = n;
                        return c;
                    }
                }
            }

            // Default empty format to be "G"; custom format is signified with '\0'.
            digits = -1;
            return format.Length == 0 || c == '\0' ? // For compat, treat '\0' as the end of the specifier, even if the specifier extends beyond it.
                'G' :
                '\0';
        }

        internal static unsafe void NumberToString(ref ValueStringBuilder sb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info)
        {
            number.CheckConsistency();
            bool isCorrectlyRounded = (number.Kind == NumberBufferKind.FloatingPoint);

            switch (format)
            {
                case 'C':
                case 'c':
                {
                    if (nMaxDigits < 0)
                        nMaxDigits = info.CurrencyDecimalDigits;

                    RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded); // Don't change this line to use digPos since digCount could have its sign changed.

                    FormatCurrency(ref sb, ref number, nMaxDigits, info);

                    break;
                }

                case 'F':
                case 'f':
                {
                    if (nMaxDigits < 0)
                        nMaxDigits = info.NumberDecimalDigits;

                    RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                    if (number.IsNegative)
                        sb.Append(info.NegativeSign);

                    FormatFixed(ref sb, ref number, nMaxDigits, null, info.NumberDecimalSeparator, null);

                    break;
                }

                case 'N':
                case 'n':
                {
                    if (nMaxDigits < 0)
                        nMaxDigits = info.NumberDecimalDigits; // Since we are using digits in our calculation

                    RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                    FormatNumber(ref sb, ref number, nMaxDigits, info);

                    break;
                }

                case 'E':
                case 'e':
                {
                    if (nMaxDigits < 0)
                        nMaxDigits = DefaultPrecisionExponentialFormat;
                    nMaxDigits++;

                    RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);

                    if (number.IsNegative)
                        sb.Append(info.NegativeSign);

                    FormatScientific(ref sb, ref number, nMaxDigits, info, format);

                    break;
                }

                case 'G':
                case 'g':
                {
                    bool noRounding = false;
                    if (nMaxDigits < 1)
                    {
                        if ((number.Kind == NumberBufferKind.Decimal) && (nMaxDigits == -1))
                        {
                            noRounding = true;  // Turn off rounding for ECMA compliance to output trailing 0's after decimal as significant

                            if (number.Digits[0] == 0)
                            {
                                // -0 should be formatted as 0 for decimal. This is normally handled by RoundNumber (which we are skipping)
                                goto SkipSign;
                            }

                            goto SkipRounding;
                        }
                        else
                        {
                            // This ensures that the PAL code pads out to the correct place even when we use the default precision
                            nMaxDigits = number.DigitsCount;
                        }
                    }

                    RoundNumber(ref number, nMaxDigits, isCorrectlyRounded);

                SkipRounding:
                    if (number.IsNegative)
                        sb.Append(info.NegativeSign);

                SkipSign:
                    FormatGeneral(ref sb, ref number, nMaxDigits, info, (char)(format - ('G' - 'E')), noRounding);

                    break;
                }

                case 'P':
                case 'p':
                {
                    if (nMaxDigits < 0)
                        nMaxDigits = info.PercentDecimalDigits;
                    number.Scale += 2;

                    RoundNumber(ref number, number.Scale + nMaxDigits, isCorrectlyRounded);

                    FormatPercent(ref sb, ref number, nMaxDigits, info);

                    break;
                }

                case 'R':
                case 'r':
                {
                    if (number.Kind != NumberBufferKind.FloatingPoint)
                    {
                        goto default;
                    }

                    format = (char)(format - ('R' - 'G'));
                    Debug.Assert((format == 'G') || (format == 'g'));
                    goto case 'G';
                }

                default:
                    throw new FormatException(SR.Argument_BadFormatSpecifier);
            }
        }

        internal static unsafe void NumberToStringFormat(ref ValueStringBuilder sb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            number.CheckConsistency();

            int digitCount;
            int decimalPos;
            int firstDigit;
            int lastDigit;
            int digPos;
            bool scientific;
            int thousandPos;
            int thousandCount = 0;
            bool thousandSeps;
            int scaleAdjust;
            int adjust;

            int section;
            int src;
            byte* dig = number.GetDigitsPointer();
            char ch;

            section = FindSection(format, dig[0] == 0 ? 2 : number.IsNegative ? 1 : 0);

            while (true)
            {
                digitCount = 0;
                decimalPos = -1;
                firstDigit = 0x7FFFFFFF;
                lastDigit = 0;
                scientific = false;
                thousandPos = -1;
                thousandSeps = false;
                scaleAdjust = 0;
                src = section;

                fixed (char* pFormat = &MemoryMarshal.GetReference(format))
                {
                    while (src < format.Length && (ch = pFormat[src++]) != 0 && ch != ';')
                    {
                        switch (ch)
                        {
                            case '#':
                                digitCount++;
                                break;
                            case '0':
                                if (firstDigit == 0x7FFFFFFF)
                                    firstDigit = digitCount;
                                digitCount++;
                                lastDigit = digitCount;
                                break;
                            case '.':
                                if (decimalPos < 0)
                                    decimalPos = digitCount;
                                break;
                            case ',':
                                if (digitCount > 0 && decimalPos < 0)
                                {
                                    if (thousandPos >= 0)
                                    {
                                        if (thousandPos == digitCount)
                                        {
                                            thousandCount++;
                                            break;
                                        }
                                        thousandSeps = true;
                                    }
                                    thousandPos = digitCount;
                                    thousandCount = 1;
                                }
                                break;
                            case '%':
                                scaleAdjust += 2;
                                break;
                            case '\x2030':
                                scaleAdjust += 3;
                                break;
                            case '\'':
                            case '"':
                                while (src < format.Length && pFormat[src] != 0 && pFormat[src++] != ch)
                                    ;
                                break;
                            case '\\':
                                if (src < format.Length && pFormat[src] != 0)
                                    src++;
                                break;
                            case 'E':
                            case 'e':
                                if ((src < format.Length && pFormat[src] == '0') ||
                                    (src + 1 < format.Length && (pFormat[src] == '+' || pFormat[src] == '-') && pFormat[src + 1] == '0'))
                                {
                                    while (++src < format.Length && pFormat[src] == '0')
                                        ;
                                    scientific = true;
                                }
                                break;
                        }
                    }
                }

                if (decimalPos < 0)
                    decimalPos = digitCount;

                if (thousandPos >= 0)
                {
                    if (thousandPos == decimalPos)
                        scaleAdjust -= thousandCount * 3;
                    else
                        thousandSeps = true;
                }

                if (dig[0] != 0)
                {
                    number.Scale += scaleAdjust;
                    int pos = scientific ? digitCount : number.Scale + digitCount - decimalPos;
                    RoundNumber(ref number, pos, isCorrectlyRounded: false);
                    if (dig[0] == 0)
                    {
                        src = FindSection(format, 2);
                        if (src != section)
                        {
                            section = src;
                            continue;
                        }
                    }
                }
                else
                {
                    if (number.Kind != NumberBufferKind.FloatingPoint)
                    {
                        // The integer types don't have a concept of -0 and decimal always format -0 as 0
                        number.IsNegative = false;
                    }
                    number.Scale = 0;      // Decimals with scale ('0.00') should be rounded.
                }

                break;
            }

            firstDigit = firstDigit < decimalPos ? decimalPos - firstDigit : 0;
            lastDigit = lastDigit > decimalPos ? decimalPos - lastDigit : 0;
            if (scientific)
            {
                digPos = decimalPos;
                adjust = 0;
            }
            else
            {
                digPos = number.Scale > decimalPos ? number.Scale : decimalPos;
                adjust = number.Scale - decimalPos;
            }
            src = section;

            // Adjust can be negative, so we make this an int instead of an unsigned int.
            // Adjust represents the number of characters over the formatting e.g. format string is "0000" and you are trying to
            // format 100000 (6 digits). Means adjust will be 2. On the other hand if you are trying to format 10 adjust will be
            // -2 and we'll need to fixup these digits with 0 padding if we have 0 formatting as in this example.
            Span<int> thousandsSepPos = stackalloc int[4];
            int thousandsSepCtr = -1;

            if (thousandSeps)
            {
                // We need to precompute this outside the number formatting loop
                if (info.NumberGroupSeparator.Length > 0)
                {
                    // We need this array to figure out where to insert the thousands separator. We would have to traverse the string
                    // backwards. PIC formatting always traverses forwards. These indices are precomputed to tell us where to insert
                    // the thousands separator so we can get away with traversing forwards. Note we only have to compute up to digPos.
                    // The max is not bound since you can have formatting strings of the form "000,000..", and this
                    // should handle that case too.

                    int[] groupDigits = info._numberGroupSizes;

                    int groupSizeIndex = 0;     // Index into the groupDigits array.
                    int groupTotalSizeCount = 0;
                    int groupSizeLen = groupDigits.Length;    // The length of groupDigits array.
                    if (groupSizeLen != 0)
                        groupTotalSizeCount = groupDigits[groupSizeIndex];   // The current running total of group size.
                    int groupSize = groupTotalSizeCount;

                    int totalDigits = digPos + ((adjust < 0) ? adjust : 0); // Actual number of digits in o/p
                    int numDigits = (firstDigit > totalDigits) ? firstDigit : totalDigits;
                    while (numDigits > groupTotalSizeCount)
                    {
                        if (groupSize == 0)
                            break;
                        ++thousandsSepCtr;
                        if (thousandsSepCtr >= thousandsSepPos.Length)
                        {
                            var newThousandsSepPos = new int[thousandsSepPos.Length * 2];
                            thousandsSepPos.CopyTo(newThousandsSepPos);
                            thousandsSepPos = newThousandsSepPos;
                        }

                        thousandsSepPos[thousandsSepCtr] = groupTotalSizeCount;
                        if (groupSizeIndex < groupSizeLen - 1)
                        {
                            groupSizeIndex++;
                            groupSize = groupDigits[groupSizeIndex];
                        }
                        groupTotalSizeCount += groupSize;
                    }
                }
            }

            if (number.IsNegative && (section == 0) && (number.Scale != 0))
                sb.Append(info.NegativeSign);

            bool decimalWritten = false;

            fixed (char* pFormat = &MemoryMarshal.GetReference(format))
            {
                byte* cur = dig;

                while (src < format.Length && (ch = pFormat[src++]) != 0 && ch != ';')
                {
                    if (adjust > 0)
                    {
                        switch (ch)
                        {
                            case '#':
                            case '0':
                            case '.':
                                while (adjust > 0)
                                {
                                    // digPos will be one greater than thousandsSepPos[thousandsSepCtr] since we are at
                                    // the character after which the groupSeparator needs to be appended.
                                    sb.Append(*cur != 0 ? (char)(*cur++) : '0');
                                    if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                    {
                                        if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                        {
                                            sb.Append(info.NumberGroupSeparator);
                                            thousandsSepCtr--;
                                        }
                                    }
                                    digPos--;
                                    adjust--;
                                }
                                break;
                        }
                    }

                    switch (ch)
                    {
                        case '#':
                        case '0':
                        {
                            if (adjust < 0)
                            {
                                adjust++;
                                ch = digPos <= firstDigit ? '0' : '\0';
                            }
                            else
                            {
                                ch = *cur != 0 ? (char)(*cur++) : digPos > lastDigit ? '0' : '\0';
                            }
                            if (ch != 0)
                            {
                                sb.Append(ch);
                                if (thousandSeps && digPos > 1 && thousandsSepCtr >= 0)
                                {
                                    if (digPos == thousandsSepPos[thousandsSepCtr] + 1)
                                    {
                                        sb.Append(info.NumberGroupSeparator);
                                        thousandsSepCtr--;
                                    }
                                }
                            }

                            digPos--;
                            break;
                        }
                        case '.':
                        {
                            if (digPos != 0 || decimalWritten)
                            {
                                // For compatibility, don't echo repeated decimals
                                break;
                            }
                            // If the format has trailing zeros or the format has a decimal and digits remain
                            if (lastDigit < 0 || (decimalPos < digitCount && *cur != 0))
                            {
                                sb.Append(info.NumberDecimalSeparator);
                                decimalWritten = true;
                            }
                            break;
                        }
                        case '\x2030':
                            sb.Append(info.PerMilleSymbol);
                            break;
                        case '%':
                            sb.Append(info.PercentSymbol);
                            break;
                        case ',':
                            break;
                        case '\'':
                        case '"':
                            while (src < format.Length && pFormat[src] != 0 && pFormat[src] != ch)
                                sb.Append(pFormat[src++]);
                            if (src < format.Length && pFormat[src] != 0)
                                src++;
                            break;
                        case '\\':
                            if (src < format.Length && pFormat[src] != 0)
                                sb.Append(pFormat[src++]);
                            break;
                        case 'E':
                        case 'e':
                        {
                            bool positiveSign = false;
                            int i = 0;
                            if (scientific)
                            {
                                if (src < format.Length && pFormat[src] == '0')
                                {
                                    // Handles E0, which should format the same as E-0
                                    i++;
                                }
                                else if (src + 1 < format.Length && pFormat[src] == '+' && pFormat[src + 1] == '0')
                                {
                                    // Handles E+0
                                    positiveSign = true;
                                }
                                else if (src + 1 < format.Length && pFormat[src] == '-' && pFormat[src + 1] == '0')
                                {
                                    // Handles E-0
                                    // Do nothing, this is just a place holder s.t. we don't break out of the loop.
                                }
                                else
                                {
                                    sb.Append(ch);
                                    break;
                                }

                                while (++src < format.Length && pFormat[src] == '0')
                                    i++;
                                if (i > 10)
                                    i = 10;

                                int exp = dig[0] == 0 ? 0 : number.Scale - decimalPos;
                                FormatExponent(ref sb, info, exp, ch, i, positiveSign);
                                scientific = false;
                            }
                            else
                            {
                                sb.Append(ch); // Copy E or e to output
                                if (src < format.Length)
                                {
                                    if (pFormat[src] == '+' || pFormat[src] == '-')
                                        sb.Append(pFormat[src++]);
                                    while (src < format.Length && pFormat[src] == '0')
                                        sb.Append(pFormat[src++]);
                                }
                            }
                            break;
                        }
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            if (number.IsNegative && (section == 0) && (number.Scale == 0) && (sb.Length > 0))
                sb.Insert(0, info.NegativeSign);
        }

        private static void FormatCurrency(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.IsNegative ?
                s_negCurrencyFormats[info.CurrencyNegativePattern] :
                s_posCurrencyFormats[info.CurrencyPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMaxDigits, info._currencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
                        break;
                    case '-':
                        sb.Append(info.NegativeSign);
                        break;
                    case '$':
                        sb.Append(info.CurrencySymbol);
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
        }

        private static unsafe void FormatFixed(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, int[]? groupDigits, string? sDecimal, string? sGroup)
        {
            int digPos = number.Scale;
            byte* dig = number.GetDigitsPointer();

            if (digPos > 0)
            {
                if (groupDigits != null)
                {
                    Debug.Assert(sGroup != null, "Must be null when groupDigits != null");
                    int groupSizeIndex = 0;                             // Index into the groupDigits array.
                    int bufferSize = digPos;                            // The length of the result buffer string.
                    int groupSize = 0;                                  // The current group size.

                    // Find out the size of the string buffer for the result.
                    if (groupDigits.Length != 0) // You can pass in 0 length arrays
                    {
                        int groupSizeCount = groupDigits[groupSizeIndex];   // The current total of group size.

                        while (digPos > groupSizeCount)
                        {
                            groupSize = groupDigits[groupSizeIndex];
                            if (groupSize == 0)
                                break;

                            bufferSize += sGroup.Length;
                            if (groupSizeIndex < groupDigits.Length - 1)
                                groupSizeIndex++;

                            groupSizeCount += groupDigits[groupSizeIndex];
                            if (groupSizeCount < 0 || bufferSize < 0)
                                throw new ArgumentOutOfRangeException(); // If we overflow
                        }

                        groupSize = groupSizeCount == 0 ? 0 : groupDigits[0]; // If you passed in an array with one entry as 0, groupSizeCount == 0
                    }

                    groupSizeIndex = 0;
                    int digitCount = 0;
                    int digLength = number.DigitsCount;
                    int digStart = (digPos < digLength) ? digPos : digLength;
                    fixed (char* spanPtr = &MemoryMarshal.GetReference(sb.AppendSpan(bufferSize)))
                    {
                        char* p = spanPtr + bufferSize - 1;
                        for (int i = digPos - 1; i >= 0; i--)
                        {
                            *(p--) = (i < digStart) ? (char)(dig[i]) : '0';

                            if (groupSize > 0)
                            {
                                digitCount++;
                                if ((digitCount == groupSize) && (i != 0))
                                {
                                    for (int j = sGroup.Length - 1; j >= 0; j--)
                                        *(p--) = sGroup[j];

                                    if (groupSizeIndex < groupDigits.Length - 1)
                                    {
                                        groupSizeIndex++;
                                        groupSize = groupDigits[groupSizeIndex];
                                    }
                                    digitCount = 0;
                                }
                            }
                        }

                        Debug.Assert(p >= spanPtr - 1, "Underflow");
                        dig += digStart;
                    }
                }
                else
                {
                    do
                    {
                        sb.Append(*dig != 0 ? (char)(*dig++) : '0');
                    }
                    while (--digPos > 0);
                }
            }
            else
            {
                sb.Append('0');
            }

            if (nMaxDigits > 0)
            {
                Debug.Assert(sDecimal != null);
                sb.Append(sDecimal);
                if ((digPos < 0) && (nMaxDigits > 0))
                {
                    int zeroes = Math.Min(-digPos, nMaxDigits);
                    sb.Append('0', zeroes);
                    digPos += zeroes;
                    nMaxDigits -= zeroes;
                }

                while (nMaxDigits > 0)
                {
                    sb.Append((*dig != 0) ? (char)(*dig++) : '0');
                    nMaxDigits--;
                }
            }
        }

        private static void FormatNumber(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.IsNegative ?
                s_negNumberFormats[info.NumberNegativePattern] :
                PosNumberFormat;

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMaxDigits, info._numberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
                        break;
                    case '-':
                        sb.Append(info.NegativeSign);
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
        }

        private static unsafe void FormatScientific(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar)
        {
            byte* dig = number.GetDigitsPointer();

            sb.Append((*dig != 0) ? (char)(*dig++) : '0');

            if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
                sb.Append(info.NumberDecimalSeparator);

            while (--nMaxDigits > 0)
                sb.Append((*dig != 0) ? (char)(*dig++) : '0');

            int e = number.Digits[0] == 0 ? 0 : number.Scale - 1;
            FormatExponent(ref sb, info, e, expChar, 3, true);
        }

        private static unsafe void FormatExponent(ref ValueStringBuilder sb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign)
        {
            sb.Append(expChar);

            if (value < 0)
            {
                sb.Append(info.NegativeSign);
                value = -value;
            }
            else
            {
                if (positiveSign)
                    sb.Append(info.PositiveSign);
            }

            char* digits = stackalloc char[MaxUInt32DecDigits];
            char* p = UInt32ToDecChars(digits + MaxUInt32DecDigits, (uint)value, minDigits);
            sb.Append(p, (int)(digits + MaxUInt32DecDigits - p));
        }

        private static unsafe void FormatGeneral(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
        {
            int digPos = number.Scale;
            bool scientific = false;

            if (!bSuppressScientific)
            {
                // Don't switch to scientific notation
                if (digPos > nMaxDigits || digPos < -3)
                {
                    digPos = 1;
                    scientific = true;
                }
            }

            byte* dig = number.GetDigitsPointer();

            if (digPos > 0)
            {
                do
                {
                    sb.Append((*dig != 0) ? (char)(*dig++) : '0');
                } while (--digPos > 0);
            }
            else
            {
                sb.Append('0');
            }

            if (*dig != 0 || digPos < 0)
            {
                sb.Append(info.NumberDecimalSeparator);

                while (digPos < 0)
                {
                    sb.Append('0');
                    digPos++;
                }

                while (*dig != 0)
                    sb.Append((char)(*dig++));
            }

            if (scientific)
                FormatExponent(ref sb, info, number.Scale - 1, expChar, 2, true);
        }

        private static void FormatPercent(ref ValueStringBuilder sb, ref NumberBuffer number, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.IsNegative ?
                s_negPercentFormats[info.PercentNegativePattern] :
                s_posPercentFormats[info.PercentPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMaxDigits, info._percentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
                        break;
                    case '-':
                        sb.Append(info.NegativeSign);
                        break;
                    case '%':
                        sb.Append(info.PercentSymbol);
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }
        }

        internal static unsafe void RoundNumber(ref NumberBuffer number, int pos, bool isCorrectlyRounded)
        {
            byte* dig = number.GetDigitsPointer();

            int i = 0;
            while (i < pos && dig[i] != '\0')
                i++;

            if ((i == pos) && ShouldRoundUp(dig, i, number.Kind, isCorrectlyRounded))
            {
                while (i > 0 && dig[i - 1] == '9')
                    i--;

                if (i > 0)
                {
                    dig[i - 1]++;
                }
                else
                {
                    number.Scale++;
                    dig[0] = (byte)('1');
                    i = 1;
                }
            }
            else
            {
                while (i > 0 && dig[i - 1] == '0')
                    i--;
            }

            if (i == 0)
            {
                if (number.Kind != NumberBufferKind.FloatingPoint)
                {
                    // The integer types don't have a concept of -0 and decimal always format -0 as 0
                    number.IsNegative = false;
                }
                number.Scale = 0;      // Decimals with scale ('0.00') should be rounded.
            }

            dig[i] = (byte)('\0');
            number.DigitsCount = i;
            number.CheckConsistency();

            static bool ShouldRoundUp(byte* dig, int i, NumberBufferKind numberKind, bool isCorrectlyRounded)
            {
                // We only want to round up if the digit is greater than or equal to 5 and we are
                // not rounding a floating-point number. If we are rounding a floating-point number
                // we have one of two cases.
                //
                // In the case of a standard numeric-format specifier, the exact and correctly rounded
                // string will have been produced. In this scenario, pos will have pointed to the
                // terminating null for the buffer and so this will return false.
                //
                // However, in the case of a custom numeric-format specifier, we currently fall back
                // to generating Single/DoublePrecisionCustomFormat digits and then rely on this
                // function to round correctly instead. This can unfortunately lead to double-rounding
                // bugs but is the best we have right now due to back-compat concerns.

                byte digit = dig[i];

                if ((digit == '\0') || isCorrectlyRounded)
                {
                    // Fast path for the common case with no rounding
                    return false;
                }

                // Values greater than or equal to 5 should round up, otherwise we round down. The IEEE
                // 754 spec actually dictates that ties (exactly 5) should round to the nearest even number
                // but that can have undesired behavior for custom numeric format strings. This probably
                // needs further thought for .NET 5 so that we can be spec compliant and so that users
                // can get the desired rounding behavior for their needs.

                return digit >= '5';
            }
        }

        private static unsafe int FindSection(ReadOnlySpan<char> format, int section)
        {
            int src;
            char ch;

            if (section == 0)
                return 0;

            fixed (char* pFormat = &MemoryMarshal.GetReference(format))
            {
                src = 0;
                while (true)
                {
                    if (src >= format.Length)
                    {
                        return 0;
                    }

                    switch (ch = pFormat[src++])
                    {
                        case '\'':
                        case '"':
                            while (src < format.Length && pFormat[src] != 0 && pFormat[src++] != ch) ;
                            break;
                        case '\\':
                            if (src < format.Length && pFormat[src] != 0)
                                src++;
                            break;
                        case ';':
                            if (--section != 0)
                                break;
                            if (src < format.Length && pFormat[src] != 0 && pFormat[src] != ';')
                                return src;
                            goto case '\0';
                        case '\0':
                            return 0;
                    }
                }
            }
        }

        private static uint Low32(ulong value) => (uint)value;

        private static uint High32(ulong value) => (uint)((value & 0xFFFFFFFF00000000) >> 32);

        private static uint Int64DivMod1E9(ref ulong value)
        {
            uint rem = (uint)(value % 1000000000);
            value /= 1000000000;
            return rem;
        }

        private static ulong ExtractFractionAndBiasedExponent(double value, out int exponent)
        {
            ulong bits = BitConverter.DoubleToUInt64Bits(value);
            ulong fraction = (bits & 0xFFFFFFFFFFFFF);
            exponent = ((int)(bits >> 52) & 0x7FF);

            if (exponent != 0)
            {
                // For normalized value, according to https://en.wikipedia.org/wiki/Double-precision_floating-point_format
                // value = 1.fraction * 2^(exp - 1023)
                //       = (1 + mantissa / 2^52) * 2^(exp - 1023)
                //       = (2^52 + mantissa) * 2^(exp - 1023 - 52)
                //
                // So f = (2^52 + mantissa), e = exp - 1075;

                fraction |= (1UL << 52);
                exponent -= 1075;
            }
            else
            {
                // For denormalized value, according to https://en.wikipedia.org/wiki/Double-precision_floating-point_format
                // value = 0.fraction * 2^(1 - 1023)
                //       = (mantissa / 2^52) * 2^(-1022)
                //       = mantissa * 2^(-1022 - 52)
                //       = mantissa * 2^(-1074)
                // So f = mantissa, e = -1074
                exponent = -1074;
            }

            return fraction;
        }

        private static ushort ExtractFractionAndBiasedExponent(Half value, out int exponent)
        {
            ushort bits = BitConverter.HalfToUInt16Bits(value);
            ushort fraction = (ushort)(bits & 0x3FF);
            exponent = ((int)(bits >> 10) & 0x1F);

            if (exponent != 0)
            {
                // For normalized value, according to https://en.wikipedia.org/wiki/Half-precision_floating-point_format
                // value = 1.fraction * 2^(exp - 15)
                //       = (1 + mantissa / 2^10) * 2^(exp - 15)
                //       = (2^10 + mantissa) * 2^(exp - 15 - 10)
                //
                // So f = (2^10 + mantissa), e = exp - 25;

                fraction |= (ushort)(1U << 10);
                exponent -= 25;
            }
            else
            {
                // For denormalized value, according to https://en.wikipedia.org/wiki/Half-precision_floating-point_format
                // value = 0.fraction * 2^(1 - 15)
                //       = (mantissa / 2^10) * 2^(-14)
                //       = mantissa * 2^(-14 - 10)
                //       = mantissa * 2^(-24)
                // So f = mantissa, e = -24
                exponent = -24;
            }

            return fraction;
        }

        private static uint ExtractFractionAndBiasedExponent(float value, out int exponent)
        {
            uint bits = BitConverter.SingleToUInt32Bits(value);
            uint fraction = (bits & 0x7FFFFF);
            exponent = ((int)(bits >> 23) & 0xFF);

            if (exponent != 0)
            {
                // For normalized value, according to https://en.wikipedia.org/wiki/Single-precision_floating-point_format
                // value = 1.fraction * 2^(exp - 127)
                //       = (1 + mantissa / 2^23) * 2^(exp - 127)
                //       = (2^23 + mantissa) * 2^(exp - 127 - 23)
                //
                // So f = (2^23 + mantissa), e = exp - 150;

                fraction |= (1U << 23);
                exponent -= 150;
            }
            else
            {
                // For denormalized value, according to https://en.wikipedia.org/wiki/Single-precision_floating-point_format
                // value = 0.fraction * 2^(1 - 127)
                //       = (mantissa / 2^23) * 2^(-126)
                //       = mantissa * 2^(-126 - 23)
                //       = mantissa * 2^(-149)
                // So f = mantissa, e = -149
                exponent = -149;
            }

            return fraction;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private const int FloatPrecision = 7;
        private const int DoublePrecision = 15;
        private const int ScaleNAN = unchecked((int)0x80000000);
        private const int ScaleINF = 0x7FFFFFFF;
        private const int MaxUInt32HexDigits = 8;
        private const int MaxUInt32DecDigits = 10;
        private const int MaxUInt64DecDigits = 20;
        private const int CharStackBufferSize = 32;
        private const string PosNumberFormat = "#";

        private static readonly string[] s_posCurrencyFormats =
        {
            "$#", "#$", "$ #", "# $"
        };

        private static readonly string[] s_negCurrencyFormats =
        {
            "($#)", "-$#", "$-#", "$#-",
            "(#$)", "-#$", "#-$", "#$-",
            "-# $", "-$ #", "# $-", "$ #-",
            "$ -#", "#- $", "($ #)", "(# $)"
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

        public static string FormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            NumberBuffer number = default;
            DecimalToNumber(value, ref number);

            ValueStringBuilder sb;
            unsafe
            {
                char* stackPtr = stackalloc char[CharStackBufferSize];
                sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
            }

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info, isDecimal:true);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }

            return sb.ToString();
        }

        public static bool TryFormatDecimal(decimal value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);

            NumberBuffer number = default;
            DecimalToNumber(value, ref number);

            ValueStringBuilder sb;
            unsafe
            {
                char* stackPtr = stackalloc char[CharStackBufferSize];
                sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
            }

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info, isDecimal: true);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }

            return sb.TryCopyTo(destination, out charsWritten);
        }

        private static unsafe void DecimalToNumber(decimal value, ref NumberBuffer number)
        {
            decimal d = value;

            char* buffer = number.digits;
            number.precision = DecimalPrecision;
            number.sign = d.IsNegative;

            char* p = buffer + DecimalPrecision;
            while ((d.Mid | d.High) != 0)
            {
                p = UInt32ToDecChars(p, decimal.DecDivMod1E9(ref d), 9);
            }
            p = UInt32ToDecChars(p, d.Low, 0);

            int i = (int)(buffer + DecimalPrecision - p);
            number.scale = i - d.Scale;

            char* dst = number.digits;
            while (--i >= 0)
            {
                *dst++ = *p++;
            }
            *dst = '\0';
        }

        public static string FormatDouble(double value, string format, NumberFormatInfo info)
        {
            Span<char> stackBuffer = stackalloc char[CharStackBufferSize];
            var sb = new ValueStringBuilder(stackBuffer);
            return FormatDouble(ref sb, value, format, info) ?? sb.ToString();
        }

        public static bool TryFormatDouble(double value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            Span<char> stackBuffer = stackalloc char[CharStackBufferSize];
            var sb = new ValueStringBuilder(stackBuffer);
            string s = FormatDouble(ref sb, value, format, info);
            return s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                sb.TryCopyTo(destination, out charsWritten);
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static string FormatDouble(ref ValueStringBuilder sb, double value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);
            int precision = DoublePrecision;
            NumberBuffer number = default;

            switch (fmt)
            {
                case 'R':
                case 'r':
                    {
                        // In order to give numbers that are both friendly to display and round-trippable, we parse the
                        // number using 15 digits and then determine if it round trips to the same value. If it does, we
                        // convert that NUMBER to a string, otherwise we reparse using 17 digits and display that.
                        DoubleToNumber(value, DoublePrecision, ref number);
                        if (number.scale == ScaleNAN)
                        {
                            return info.NaNSymbol;
                        }
                        else if (number.scale == ScaleINF)
                        {
                            return number.sign ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
                        }

                        if (NumberToDouble(ref number) == value)
                        {
                            NumberToString(ref sb, ref number, 'G', DoublePrecision, info, isDecimal: false);
                        }
                        else
                        {
                            DoubleToNumber(value, 17, ref number);
                            NumberToString(ref sb, ref number, 'G', 17, info, isDecimal: false);
                        }

                        return null;
                    }

                case 'E':
                case 'e':
                    // Round values less than E14 to 15 digits
                    if (digits > 14)
                    {
                        precision = 17;
                    }
                    break;

                case 'G':
                case 'g':
                    // Round values less than G15 to 15 digits. G16 and G17 will not be touched.
                    if (digits > 15)
                    {
                        precision = 17;
                    }
                    break;
            }

            DoubleToNumber(value, precision, ref number);
            if (number.scale == ScaleNAN)
            {
                return info.NaNSymbol;
            }
            else if (number.scale == ScaleINF)
            {
                return number.sign ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
            }

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info, isDecimal: false);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }

            return null;
        }

        public static string FormatSingle(float value, string format, NumberFormatInfo info)
        {
            Span<char> stackBuffer = stackalloc char[CharStackBufferSize];
            var sb = new ValueStringBuilder(stackBuffer);
            return FormatSingle(ref sb, value, format, info) ?? sb.ToString();
        }

        public static bool TryFormatSingle(float value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            Span<char> stackBuffer = stackalloc char[CharStackBufferSize];
            var sb = new ValueStringBuilder(stackBuffer);
            string s = FormatSingle(ref sb, value, format, info);
            return s != null ?
                TryCopyTo(s, destination, out charsWritten) :
                sb.TryCopyTo(destination, out charsWritten);
        }

        /// <summary>Formats the specified value according to the specified format and info.</summary>
        /// <returns>
        /// Non-null if an existing string can be returned, in which case the builder will be unmodified.
        /// Null if no existing string was returned, in which case the formatted output is in the builder.
        /// </returns>
        private static string FormatSingle(ref ValueStringBuilder sb, float value, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
            char fmt = ParseFormatSpecifier(format, out int digits);
            int precision = FloatPrecision;
            NumberBuffer number = default;

            switch (fmt)
            {
                case 'R':
                case 'r':
                    {
                        // In order to give numbers that are both friendly to display and round-trippable, we parse the
                        // number using 7 digits and then determine if it round trips to the same value. If it does, we
                        // convert that NUMBER to a string, otherwise we reparse using 9 digits and display that.
                        DoubleToNumber(value, FloatPrecision, ref number);
                        if (number.scale == ScaleNAN)
                        {
                            return info.NaNSymbol;
                        }
                        else if (number.scale == ScaleINF)
                        {
                            return number.sign ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
                        }

                        if ((float)NumberToDouble(ref number) == value)
                        {
                            NumberToString(ref sb, ref number, 'G', FloatPrecision, info, isDecimal: false);
                        }
                        else
                        {
                            DoubleToNumber(value, 9, ref number);
                            NumberToString(ref sb, ref number, 'G', 9, info, isDecimal: false);
                        }
                        return null;
                    }

                case 'E':
                case 'e':
                    // Round values less than E14 to 15 digits.
                    if (digits > 6)
                    {
                        precision = 9;
                    }
                    break;

                case 'G':
                case 'g':
                    // Round values less than G15 to 15 digits. G16 and G17 will not be touched.
                    if (digits > 7)
                    {
                        precision = 9;
                    }
                    break;
            }

            DoubleToNumber(value, precision, ref number);
            if (number.scale == ScaleNAN)
            {
                return info.NaNSymbol;
            }
            else if (number.scale == ScaleINF)
            {
                return number.sign ? info.NegativeInfinitySymbol : info.PositiveInfinitySymbol;
            }

            if (fmt != 0)
            {
                NumberToString(ref sb, ref number, fmt, digits, info, false);
            }
            else
            {
                NumberToStringFormat(ref sb, ref number, format, info);
            }
            return null;
        }

        private static bool TryCopyTo(string source, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(source != null);

            if (source.AsReadOnlySpan().TryCopyTo(destination))
            {
                charsWritten = source.Length;
                return true;
            }

            charsWritten = 0;
            return false;
        }

        public static string FormatInt32(int value, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            // Fast path for default format with a non-negative value
            if (value >= 0 && format.Length == 0)
            {
                return UInt32ToDecStr((uint)value, digits: -1);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return value >= 0 ?
                    UInt32ToDecStr((uint)value, digits) :
                    NegativeInt32ToDecStr(value, digits, info.NegativeSign);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code produces lowercase.
                return Int32ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits);
            }
            else
            {
                NumberBuffer number = default;
                Int32ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.ToString();
            }
        }

        public static bool TryFormatInt32(int value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format with a non-negative value
            if (value >= 0 && format.Length == 0)
            {
                return TryUInt32ToDecStr((uint)value, digits: -1, destination, out charsWritten);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return value >= 0 ?
                    TryUInt32ToDecStr((uint)value, digits, destination, out charsWritten) :
                    TryNegativeInt32ToDecStr(value, digits, info.NegativeSign, destination, out charsWritten);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code produces lowercase.
                return TryInt32ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits, destination, out charsWritten);
            }
            else
            {
                NumberBuffer number = default;
                Int32ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.TryCopyTo(destination, out charsWritten);
            }
        }

        public static string FormatUInt32(uint value, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return UInt32ToDecStr(value, digits: -1);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return UInt32ToDecStr(value, digits);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code produces lowercase.
                return Int32ToHexStr((int)value, (char)(fmt - ('X' - 'A' + 10)), digits);
            }
            else
            {
                NumberBuffer number = default;
                UInt32ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.ToString();
            }
        }

        public static bool TryFormatUInt32(uint value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt32ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return TryUInt32ToDecStr(value, digits, destination, out charsWritten);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code produces lowercase.
                return TryInt32ToHexStr((int)value, (char)(fmt - ('X' - 'A' + 10)), digits, destination, out charsWritten);
            }
            else
            {
                NumberBuffer number = default;
                UInt32ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.TryCopyTo(destination, out charsWritten);
            }
        }

        public static string FormatInt64(long value, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            // Fast path for default format with a non-negative value
            if (value >= 0 && format.Length == 0)
            {
                return UInt64ToDecStr((ulong)value, digits: -1);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return value >= 0 ?
                    UInt64ToDecStr((ulong)value, digits) :
                    NegativeInt64ToDecStr(value, digits, info.NegativeSign);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                // produces lowercase.
                return Int64ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits);
            }
            else
            {
                NumberBuffer number = default;
                Int64ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.ToString();
            }
        }

        public static bool TryFormatInt64(long value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format with a non-negative value
            if (value >= 0 && format.Length == 0)
            {
                return TryUInt64ToDecStr((ulong)value, digits: -1, destination, out charsWritten);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return value >= 0 ?
                    TryUInt64ToDecStr((ulong)value, digits, destination, out charsWritten) :
                    TryNegativeInt64ToDecStr(value, digits, info.NegativeSign, destination, out charsWritten);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                // produces lowercase.
                return TryInt64ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits, destination, out charsWritten);
            }
            else
            {
                NumberBuffer number = default;
                Int64ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.TryCopyTo(destination, out charsWritten);
            }
        }

        public static string FormatUInt64(ulong value, ReadOnlySpan<char> format, IFormatProvider provider)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return UInt64ToDecStr(value, digits: -1);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return UInt64ToDecStr(value, digits);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                // produces lowercase.
                return Int64ToHexStr((long)value, (char)(fmt - ('X' - 'A' + 10)), digits);
            }
            else
            {
                NumberBuffer number = default;
                UInt64ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.ToString();
            }
        }

        public static bool TryFormatUInt64(ulong value, ReadOnlySpan<char> format, IFormatProvider provider, Span<char> destination, out int charsWritten)
        {
            // Fast path for default format
            if (format.Length == 0)
            {
                return TryUInt64ToDecStr(value, digits: -1, destination, out charsWritten);
            }

            char fmt = ParseFormatSpecifier(format, out int digits);
            NumberFormatInfo info = NumberFormatInfo.GetInstance(provider);

            char fmtUpper = (char)(fmt & 0xFFDF); // ensure fmt is upper-cased for purposes of comparison
            if ((fmtUpper == 'G' && digits < 1) || fmtUpper == 'D')
            {
                return TryUInt64ToDecStr(value, digits, destination, out charsWritten);
            }
            else if (fmtUpper == 'X')
            {
                // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                // produces lowercase.
                return TryInt64ToHexStr((long)value, (char)(fmt - ('X' - 'A' + 10)), digits, destination, out charsWritten);
            }
            else
            {
                NumberBuffer number = default;
                UInt64ToNumber(value, ref number);
                ValueStringBuilder sb;
                unsafe
                {
                    char* stackPtr = stackalloc char[CharStackBufferSize];
                    sb = new ValueStringBuilder(new Span<char>(stackPtr, CharStackBufferSize));
                }
                if (fmt != 0)
                {
                    NumberToString(ref sb, ref number, fmt, digits, info, false);
                }
                else
                {
                    NumberToStringFormat(ref sb, ref number, format, info);
                }
                return sb.TryCopyTo(destination, out charsWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // called from only one location
        private static unsafe void Int32ToNumber(int value, ref NumberBuffer number)
        {
            number.precision = Int32Precision;

            if (value >= 0)
            {
                number.sign = false;
            }
            else
            {
                number.sign = true;
                value = -value;
            }

            char* buffer = number.digits;
            char* p = UInt32ToDecChars(buffer + Int32Precision, (uint)value, 0);
            int i = (int)(buffer + Int32Precision - p);

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = *p++;
            *dst = '\0';
        }

        private static unsafe string NegativeInt32ToDecStr(int value, int digits, string sNegative)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, MaxUInt32DecDigits) + sNegative.Length;
            int index = bufferLength;

            char* buffer = stackalloc char[bufferLength];
            char* p = UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
            for (int i = sNegative.Length - 1; i >= 0; i--)
            {
                *(--p) = sNegative[i];
            }

            Debug.Assert(buffer + bufferLength - p >= 0 && buffer <= p);
            return new string(p, 0, (int)(buffer + bufferLength - p));
        }

        private static unsafe bool TryNegativeInt32ToDecStr(int value, int digits, string sNegative, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(value < 0);

            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, MaxUInt32DecDigits) + sNegative.Length;
            int index = bufferLength;

            char* buffer = stackalloc char[bufferLength];
            char* p = UInt32ToDecChars(buffer + bufferLength, (uint)(-value), digits);
            for (int i = sNegative.Length - 1; i >= 0; i--)
            {
                *(--p) = sNegative[i];
            }

            Debug.Assert(buffer + bufferLength - p >= 0 && buffer <= p);
            return TryCopyTo(p, (int)(buffer + bufferLength - p), destination, out charsWritten);
        }

        private static unsafe string Int32ToHexStr(int value, char hexBase, int digits)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, MaxUInt32HexDigits);
            char* buffer = stackalloc char[bufferLength];

            char* p = Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
            return new string(p, 0, (int)(buffer + bufferLength - p));
        }

        private static unsafe bool TryInt32ToHexStr(int value, char hexBase, int digits, Span<char> destination, out int charsWritten)
        {
            if (digits < 1)
                digits = 1;

            int bufferLength = Math.Max(digits, MaxUInt32HexDigits);
            char* buffer = stackalloc char[bufferLength];

            char* p = Int32ToHexChars(buffer + bufferLength, (uint)value, hexBase, digits);
            return TryCopyTo(p, (int)(buffer + bufferLength - p), destination, out charsWritten);
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
            number.precision = UInt32Precision;
            number.sign = false;

            char* buffer = number.digits;
            char* p = UInt32ToDecChars(buffer + UInt32Precision, value, 0);
            int i = (int)(buffer + UInt32Precision - p);
            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = *p++;
            *dst = '\0';
        }

        internal static unsafe char* UInt32ToDecChars(char* bufferEnd, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                // TODO https://github.com/dotnet/coreclr/issues/3439
                uint newValue = value / 10;
                *(--bufferEnd) = (char)(value - (newValue * 10) + '0');
                value = newValue;
            }
            return bufferEnd;
        }

        private static unsafe string UInt32ToDecStr(uint value, int digits)
        {
            if (digits <= 1)
            {
                char* buffer = stackalloc char[MaxUInt32DecDigits];

                char* start = buffer + MaxUInt32DecDigits;
                char* p = start;
                do
                {
                    // TODO https://github.com/dotnet/coreclr/issues/3439
                    uint div = value / 10;
                    *(--p) = (char)('0' + value - (div * 10));
                    value = div;
                }
                while (value != 0);

                return new string(p, 0, (int)(start - p));
            }
            else
            {
                int bufferSize = Math.Max(digits, MaxUInt32DecDigits);
                char* buffer = stackalloc char[bufferSize];
                char* p = UInt32ToDecChars(buffer + bufferSize, value, digits);
                return new string(p, 0, (int)(buffer + bufferSize - p));
            }
        }

        private static unsafe bool TryUInt32ToDecStr(uint value, int digits, Span<char> destination, out int charsWritten)
        {
            if (digits <= 1)
            {
                char* buffer = stackalloc char[MaxUInt32DecDigits];
                char* start = buffer + MaxUInt32DecDigits;
                char* p = start;
                do
                {
                    // TODO https://github.com/dotnet/coreclr/issues/3439
                    uint div = value / 10;
                    *(--p) = (char)('0' + value - (div * 10));
                    value = div;
                }
                while (value != 0);
                return TryCopyTo(p, (int)(start - p), destination, out charsWritten);
            }
            else
            {
                int bufferSize = Math.Max(digits, MaxUInt32DecDigits);
                char* buffer = stackalloc char[bufferSize];
                char* p = UInt32ToDecChars(buffer + bufferSize, value, digits);
                return TryCopyTo(p, (int)(buffer + bufferSize - p), destination, out charsWritten);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe bool TryCopyTo(char* src, int length, Span<char> destination, out int charsWritten)
        {
            if (length <= destination.Length)
            {
                bool copied = new ReadOnlySpan<char>(src, length).TryCopyTo(destination);
                Debug.Assert(copied);
                charsWritten = length;
                return true;
            }
            else
            {
                charsWritten = 0;
                return false;
            }
        }

        private static unsafe void Int64ToNumber(long input, ref NumberBuffer number)
        {
            ulong value = (ulong)input;
            number.sign = input < 0;
            number.precision = Int64Precision;
            if (number.sign)
            {
                value = (ulong)(-input);
            }

            char* buffer = number.digits;
            char* p = buffer + Int64Precision;
            while (High32(value) != 0)
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
            p = UInt32ToDecChars(p, Low32(value), 0);
            int i = (int)(buffer + Int64Precision - p);

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = *p++;
            *dst = '\0';
        }

        private static unsafe string NegativeInt64ToDecStr(long input, int digits, string sNegative)
        {
            Debug.Assert(input < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            ulong value = (ulong)(-input);

            int bufferLength = Math.Max(digits, MaxUInt64DecDigits) + sNegative.Length;
            int index = bufferLength;

            char* buffer = stackalloc char[bufferLength];
            char* p = buffer + bufferLength;
            while (High32(value) != 0)
            {
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            p = UInt32ToDecChars(p, Low32(value), digits);

            for (int i = sNegative.Length - 1; i >= 0; i--)
            {
                *(--p) = sNegative[i];
            }

            return new string(p, 0, (int)(buffer + bufferLength - p));
        }

        private static unsafe bool TryNegativeInt64ToDecStr(long input, int digits, string sNegative, Span<char> destination, out int charsWritten)
        {
            Debug.Assert(input < 0);

            if (digits < 1)
            {
                digits = 1;
            }

            ulong value = (ulong)(-input);

            int bufferLength = Math.Max(digits, MaxUInt64DecDigits) + sNegative.Length;
            int index = bufferLength;

            char* buffer = stackalloc char[bufferLength];
            char* p = buffer + bufferLength;
            while (High32(value) != 0)
            {
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            p = UInt32ToDecChars(p, Low32(value), digits);

            for (int i = sNegative.Length - 1; i >= 0; i--)
            {
                *(--p) = sNegative[i];
            }

            return TryCopyTo(p, (int)(buffer + bufferLength - p), destination, out charsWritten);
        }

        private static unsafe string Int64ToHexStr(long value, char hexBase, int digits)
        {
            int bufferLength = Math.Max(digits, MaxUInt32HexDigits * 2);
            char* buffer = stackalloc char[bufferLength];
            int index = bufferLength;

            char* p;
            if (High32((ulong)value) != 0)
            {
                p = Int32ToHexChars(buffer + index, Low32((ulong)value), hexBase, 8);
                p = Int32ToHexChars(p, High32((ulong)value), hexBase, digits - 8);
            }
            else
            {
                p = Int32ToHexChars(buffer + index, Low32((ulong)value), hexBase, Math.Max(digits, 1));
            }

            return new string(p, 0, (int)(buffer + bufferLength - p));
        }

        private static unsafe bool TryInt64ToHexStr(long value, char hexBase, int digits, Span<char> destination, out int charsWritten)
        {
            int bufferLength = Math.Max(digits, MaxUInt32HexDigits * 2);
            char* buffer = stackalloc char[bufferLength];
            int index = bufferLength;

            char* p;
            if (High32((ulong)value) != 0)
            {
                p = Int32ToHexChars(buffer + index, Low32((ulong)value), hexBase, 8);
                p = Int32ToHexChars(p, High32((ulong)value), hexBase, digits - 8);
            }
            else
            {
                p = Int32ToHexChars(buffer + index, Low32((ulong)value), hexBase, Math.Max(digits, 1));
            }

            return TryCopyTo(p, (int)(buffer + bufferLength - p), destination, out charsWritten);
        }

        private static unsafe void UInt64ToNumber(ulong value, ref NumberBuffer number)
        {
            number.precision = UInt64Precision;
            number.sign = false;

            char* buffer = number.digits;
            char* p = buffer + UInt64Precision;

            while (High32(value) != 0)
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
            p = UInt32ToDecChars(p, Low32(value), 0);
            int i = (int)(buffer + UInt64Precision - p);

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = *p++;
            *dst = '\0';
        }

        private static unsafe string UInt64ToDecStr(ulong value, int digits)
        {
            if (digits < 1)
                digits = 1;

            int bufferSize = Math.Max(digits, MaxUInt64DecDigits);
            char* buffer = stackalloc char[bufferSize];
            char* p = buffer + bufferSize;
            while (High32(value) != 0)
            {
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            p = UInt32ToDecChars(p, Low32(value), digits);

            return new string(p, 0, (int)(buffer + bufferSize - p));
        }

        private static unsafe bool TryUInt64ToDecStr(ulong value, int digits, Span<char> destination, out int charsWritten)
        {
            if (digits < 1)
                digits = 1;

            int bufferSize = Math.Max(digits, MaxUInt64DecDigits);
            char* buffer = stackalloc char[bufferSize];
            char* p = buffer + bufferSize;
            while (High32(value) != 0)
            {
                p = UInt32ToDecChars(p, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            p = UInt32ToDecChars(p, Low32(value), digits);

            return TryCopyTo(p, (int)(buffer + bufferSize - p), destination, out charsWritten);
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
                    while (i < format.Length && (((uint)format[i] - '0') < 10) && n < 10)
                    {
                        n = (n * 10) + format[i++] - '0';
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

        internal static unsafe void NumberToString(ref ValueStringBuilder sb, ref NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info, bool isDecimal)
        {
            int nMinDigits = -1;

            switch (format)
            {
                case 'C':
                case 'c':
                    {
                        nMinDigits = nMaxDigits >= 0 ? nMaxDigits : info.CurrencyDecimalDigits;
                        if (nMaxDigits < 0)
                            nMaxDigits = info.CurrencyDecimalDigits;

                        RoundNumber(ref number, number.scale + nMaxDigits); // Don't change this line to use digPos since digCount could have its sign changed.

                        FormatCurrency(ref sb, ref number, nMinDigits, nMaxDigits, info);

                        break;
                    }

                case 'F':
                case 'f':
                    {
                        if (nMaxDigits < 0)
                            nMaxDigits = nMinDigits = info.NumberDecimalDigits;
                        else
                            nMinDigits = nMaxDigits;

                        RoundNumber(ref number, number.scale + nMaxDigits);

                        if (number.sign)
                            sb.Append(info.NegativeSign);

                        FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, null, info.NumberDecimalSeparator, null);

                        break;
                    }

                case 'N':
                case 'n':
                    {
                        if (nMaxDigits < 0)
                            nMaxDigits = nMinDigits = info.NumberDecimalDigits; // Since we are using digits in our calculation
                        else
                            nMinDigits = nMaxDigits;

                        RoundNumber(ref number, number.scale + nMaxDigits);

                        FormatNumber(ref sb, ref number, nMinDigits, nMaxDigits, info);

                        break;
                    }

                case 'E':
                case 'e':
                    {
                        if (nMaxDigits < 0)
                            nMaxDigits = nMinDigits = 6;
                        else
                            nMinDigits = nMaxDigits;
                        nMaxDigits++;

                        RoundNumber(ref number, nMaxDigits);

                        if (number.sign)
                            sb.Append(info.NegativeSign);

                        FormatScientific(ref sb, ref number, nMinDigits, nMaxDigits, info, format);

                        break;
                    }

                case 'G':
                case 'g':
                    {
                        bool enableRounding = true;
                        if (nMaxDigits < 1)
                        {
                            if (isDecimal && (nMaxDigits == -1))
                            {
                                // Default to 29 digits precision only for G formatting without a precision specifier
                                // This ensures that the PAL code pads out to the correct place even when we use the default precision
                                nMaxDigits = nMinDigits = DecimalPrecision;
                                enableRounding = false;  // Turn off rounding for ECMA compliance to output trailing 0's after decimal as significant
                            }
                            else
                            {
                                // This ensures that the PAL code pads out to the correct place even when we use the default precision
                                nMaxDigits = nMinDigits = number.precision;
                            }
                        }
                        else
                            nMinDigits = nMaxDigits;

                        if (enableRounding) // Don't round for G formatting without precision
                            RoundNumber(ref number, nMaxDigits); // This also fixes up the minus zero case
                        else
                        {
                            if (isDecimal && (number.digits[0] == 0))
                            {
                                // Minus zero should be formatted as 0
                                number.sign = false;
                            }
                        }

                        if (number.sign)
                            sb.Append(info.NegativeSign);

                        FormatGeneral(ref sb, ref number, nMinDigits, nMaxDigits, info, (char)(format - ('G' - 'E')), !enableRounding);

                        break;
                    }

                case 'P':
                case 'p':
                    {
                        if (nMaxDigits < 0)
                            nMaxDigits = nMinDigits = info.PercentDecimalDigits;
                        else
                            nMinDigits = nMaxDigits;
                        number.scale += 2;

                        RoundNumber(ref number, number.scale + nMaxDigits);

                        FormatPercent(ref sb, ref number, nMinDigits, nMaxDigits, info);

                        break;
                    }

                default:
                    throw new FormatException(SR.Argument_BadFormatSpecifier);
            }
        }

        internal static unsafe void NumberToStringFormat(ref ValueStringBuilder sb, ref NumberBuffer number, ReadOnlySpan<char> format, NumberFormatInfo info)
        {
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
            char* dig = number.digits;
            char ch;

            section = FindSection(format, dig[0] == 0 ? 2 : number.sign ? 1 : 0);

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
                                    while (++src < format.Length && pFormat[src] == '0');
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
                    number.scale += scaleAdjust;
                    int pos = scientific ? digitCount : number.scale + digitCount - decimalPos;
                    RoundNumber(ref number, pos);
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
                    number.sign = false;   // We need to format -0 without the sign set.
                    number.scale = 0;      // Decimals with scale ('0.00') should be rounded.
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
                digPos = number.scale > decimalPos ? number.scale : decimalPos;
                adjust = number.scale - decimalPos;
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

                    int[] groupDigits = info.numberGroupSizes;

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
                            bool copied = thousandsSepPos.TryCopyTo(newThousandsSepPos);
                            Debug.Assert(copied, "Expect copy to succeed, as the new array is larger than the original");
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
            
            if (number.sign && section == 0)
                sb.Append(info.NegativeSign);

            bool decimalWritten = false;

            fixed (char* pFormat = &MemoryMarshal.GetReference(format))
            {
                char* cur = dig;

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
                                    sb.Append(*cur != 0 ? *cur++ : '0');
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
                                    ch = *cur != 0 ? *cur++ : digPos > lastDigit ? '0' : '\0';
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
                                    else if (src+1 < format.Length && pFormat[src] == '+' && pFormat[src + 1] == '0')
                                    {
                                        // Handles E+0
                                        positiveSign = true;
                                    }
                                    else if (src+1 < format.Length && pFormat[src] == '-' && pFormat[src + 1] == '0')
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

                                    int exp = dig[0] == 0 ? 0 : number.scale - decimalPos;
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
        }

        private static void FormatCurrency(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negCurrencyFormats[info.CurrencyNegativePattern] :
                s_posCurrencyFormats[info.CurrencyPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.currencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
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

        private static unsafe void FormatFixed(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, int[] groupDigits, string sDecimal, string sGroup)
        {
            int digPos = number.scale;
            char* dig = number.digits;

            if (digPos > 0)
            {
                if (groupDigits != null)
                {
                    int groupSizeIndex = 0;                             // Index into the groupDigits array.
                    int groupSizeCount = groupDigits[groupSizeIndex];   // The current total of group size.
                    int bufferSize = digPos;                            // The length of the result buffer string.
                    int groupSize = 0;                                  // The current group size.

                    // Find out the size of the string buffer for the result.
                    if (groupDigits.Length != 0) // You can pass in 0 length arrays
                    {
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
                    int digLength = string.wcslen(dig);
                    int digStart = (digPos < digLength) ? digPos : digLength;
                    fixed (char* spanPtr = &MemoryMarshal.GetReference(sb.AppendSpan(bufferSize)))
                    {
                        char* p = spanPtr + bufferSize - 1;
                        for (int i = digPos - 1; i >= 0; i--)
                        {
                            *(p--) = (i < digStart) ? dig[i] : '0';

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
                        sb.Append(*dig != 0 ? *dig++ : '0');
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
                    sb.Append((*dig != 0) ? *dig++ : '0');
                    nMaxDigits--;
                }
            }
        }

        private static void FormatNumber(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negNumberFormats[info.NumberNegativePattern] :
                PosNumberFormat;

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.numberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
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

        private static unsafe void FormatScientific(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar)
        {
            char* dig = number.digits;

            sb.Append((*dig != 0) ? *dig++ : '0');

            if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
                sb.Append(info.NumberDecimalSeparator);

            while (--nMaxDigits > 0)
                sb.Append((*dig != 0) ? *dig++ : '0');

            int e = number.digits[0] == 0 ? 0 : number.scale - 1;
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
            int i = (int)(digits + MaxUInt32DecDigits - p);
            sb.Append(p, (int)(digits + MaxUInt32DecDigits - p));
        }

        private static unsafe void FormatGeneral(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
        {
            int digPos = number.scale;
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

            char* dig = number.digits;

            if (digPos > 0)
            {
                do
                {
                    sb.Append((*dig != 0) ? *dig++ : '0');
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
                    sb.Append(*dig++);
            }

            if (scientific)
                FormatExponent(ref sb, info, number.scale - 1, expChar, 2, true);
        }

        private static void FormatPercent(ref ValueStringBuilder sb, ref NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negPercentFormats[info.PercentNegativePattern] :
                s_posPercentFormats[info.PercentPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(ref sb, ref number, nMinDigits, nMaxDigits, info, info.percentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
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

        private static unsafe void RoundNumber(ref NumberBuffer number, int pos)
        {
            char* dig = number.digits;

            int i = 0;
            while (i < pos && dig[i] != 0)
                i++;

            if (i == pos && dig[i] >= '5')
            {
                while (i > 0 && dig[i - 1] == '9')
                    i--;

                if (i > 0)
                {
                    dig[i - 1]++;
                }
                else
                {
                    number.scale++;
                    dig[0] = '1';
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
                number.scale = 0;
                number.sign = false;
            }
            dig[i] = '\0';
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
                for (;;)
                {
                    if (src >= format.Length)
                    {
                        return 0;
                    }

                    switch (ch = pFormat[src++])
                    {
                        case '\'':
                        case '"':
                            while (src < format.Length && pFormat[src] != 0 && pFormat[src++] != ch)
                                ;
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
    }
}

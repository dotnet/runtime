// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The BigNumber class implements methods for formatting and parsing
// big numeric values. To format and parse numeric values, applications should
// use the Format and Parse methods provided by the numeric
// classes (BigInteger). Those
// Format and Parse methods share a common implementation
// provided by this class, and are thus documented in detail here.
//
// Formatting
//
// The Format methods provided by the numeric classes are all of the
// form
//
//  public static String Format(XXX value, String format);
//  public static String Format(XXX value, String format, NumberFormatInfo info);
//
// where XXX is the name of the particular numeric class. The methods convert
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
// specifier controls whether the exponent is prefixed with an 'E' or an
// 'e'.
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
//
// Parsing
//
// The Parse methods provided by the numeric classes are all of the form
//
//  public static XXX Parse(String s);
//  public static XXX Parse(String s, int style);
//  public static XXX Parse(String s, int style, NumberFormatInfo info);
//
// where XXX is the name of the particular numeric class. The methods convert a
// string to a numeric value. The optional style parameter specifies the
// permitted style of the numeric string. It must be a combination of bit flags
// from the NumberStyles enumeration. The optional info parameter
// specifies the NumberFormatInfo instance to use when parsing the
// string. If the info parameter is null or omitted, the numeric
// formatting information is obtained from the current culture.
//
// Numeric strings produced by the Format methods using the Currency,
// Decimal, Engineering, Fixed point, General, or Number standard formats
// (the C, D, E, F, G, and N format specifiers) are guaranteed to be parseable
// by the Parse methods if the NumberStyles.Any style is
// specified. Note, however, that the Parse methods do not accept
// NaNs or Infinities.
//

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace System.Numerics
{
    internal static class BigNumber
    {
        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                           | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                           | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                           | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                           | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);

        private struct BigNumberBuffer
        {
            public StringBuilder digits;
            public int precision;
            public int scale;
            public bool sign;  // negative sign exists

            public static BigNumberBuffer Create()
            {
                BigNumberBuffer number = default;
                number.digits = new StringBuilder();
                return number;
            }
        }


        internal static bool TryValidateParseStyleInteger(NumberStyles style, [NotNullWhen(false)] out ArgumentException? e)
        {
            // Check for undefined flags
            if ((style & InvalidNumberStyles) != 0)
            {
                e = new ArgumentException(SR.Format(SR.Argument_InvalidNumberStyles, nameof(style)));
                return false;
            }
            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            { // Check for hex number
                if ((style & ~NumberStyles.HexNumber) != 0)
                {
                    e = new ArgumentException(SR.Argument_InvalidHexStyle);
                    return false;
                }
            }
            e = null;
            return true;
        }

        internal static bool TryParseBigInteger(string? value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
        {
            if (value == null)
            {
                result = default(BigInteger);
                return false;
            }

            return TryParseBigInteger(value.AsSpan(), style, info, out result);
        }

        internal static bool TryParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info, out BigInteger result)
        {
            unsafe
            {
                result = BigInteger.Zero;
                ArgumentException? e;
                if (!TryValidateParseStyleInteger(style, out e))
                    throw e; // TryParse still throws ArgumentException on invalid NumberStyles

                BigNumberBuffer bignumber = BigNumberBuffer.Create();
                if (!FormatProvider.TryStringToBigInteger(value, style, info, bignumber.digits, out bignumber.precision, out bignumber.scale, out bignumber.sign))
                    return false;

                if ((style & NumberStyles.AllowHexSpecifier) != 0)
                {
                    if (!HexNumberToBigInteger(ref bignumber, ref result))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!NumberToBigInteger(ref bignumber, ref result))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        internal static BigInteger ParseBigInteger(string value, NumberStyles style, NumberFormatInfo info)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return ParseBigInteger(value.AsSpan(), style, info);
        }

        internal static BigInteger ParseBigInteger(ReadOnlySpan<char> value, NumberStyles style, NumberFormatInfo info)
        {
            ArgumentException? e;
            if (!TryValidateParseStyleInteger(style, out e))
                throw e;

            BigInteger result = BigInteger.Zero;
            if (!TryParseBigInteger(value, style, info, out result))
            {
                throw new FormatException(SR.Overflow_ParseBigInteger);
            }
            return result;
        }

        private static unsafe bool HexNumberToBigInteger(ref BigNumberBuffer number, ref BigInteger value)
        {
            if (number.digits == null || number.digits.Length == 0)
                return false;

            int len = number.digits.Length - 1; // Ignore trailing '\0'
            byte[] bits = new byte[(len / 2) + (len % 2)];

            bool shift = false;
            bool isNegative = false;
            int bitIndex = 0;

            // Parse the string into a little-endian two's complement byte array
            // string value     : O F E B 7 \0
            // string index (i) : 0 1 2 3 4 5 <--
            // byte[] (bitIndex): 2 1 1 0 0 <--
            //
            for (int i = len - 1; i > -1; i--)
            {
                char c = number.digits[i];
                int b = HexConverter.FromChar(c);
                Debug.Assert(b != 0xFF);
                if (i == 0 && (b & 0x08) == 0x08)
                    isNegative = true;

                if (shift)
                {
                    bits[bitIndex] = (byte)(bits[bitIndex] | (b << 4));
                    bitIndex++;
                }
                else
                {
                    bits[bitIndex] = (byte)(isNegative ? (b | 0xF0) : (b));
                }
                shift = !shift;
            }

            value = new BigInteger(bits);
            return true;
        }

        private static unsafe bool NumberToBigInteger(ref BigNumberBuffer number, ref BigInteger value)
        {
            int i = number.scale;
            int cur = 0;

            BigInteger ten = 10;
            value = 0;
            while (--i >= 0)
            {
                value *= ten;
                if (number.digits[cur] != '\0')
                {
                    value += number.digits[cur++] - '0';
                }
            }
            while (number.digits[cur] != '\0')
            {
                if (number.digits[cur++] != '0') return false; // Disallow non-zero trailing decimal places
            }
            if (number.sign)
            {
                value = -value;
            }
            return true;
        }

        // This function is consistent with VM\COMNumber.cpp!COMNumber::ParseFormatSpecifier
        internal static char ParseFormatSpecifier(ReadOnlySpan<char> format, out int digits)
        {
            digits = -1;
            if (format.Length == 0)
            {
                return 'R';
            }

            int i = 0;
            char ch = format[i];
            if (ch >= 'A' && ch <= 'Z' || ch >= 'a' && ch <= 'z')
            {
                i++;
                int n = -1;

                if (i < format.Length && format[i] >= '0' && format[i] <= '9')
                {
                    n = format[i++] - '0';
                    while (i < format.Length && format[i] >= '0' && format[i] <= '9')
                    {
                        n = n * 10 + (format[i++] - '0');
                        if (n >= 10)
                            break;
                    }
                }
                if (i >= format.Length || format[i] == '\0')
                {
                    digits = n;
                    return ch;
                }
            }
            return (char)0; // Custom format
        }

        private static string? FormatBigIntegerToHex(bool targetSpan, BigInteger value, char format, int digits, NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(format == 'x' || format == 'X');

            // Get the bytes that make up the BigInteger.
            byte[]? arrayToReturnToPool = null;
            Span<byte> bits = stackalloc byte[64]; // arbitrary threshold
            if (!value.TryWriteOrCountBytes(bits, out int bytesWrittenOrNeeded))
            {
                bits = arrayToReturnToPool = ArrayPool<byte>.Shared.Rent(bytesWrittenOrNeeded);
                bool success = value.TryWriteBytes(bits, out bytesWrittenOrNeeded);
                Debug.Assert(success);
            }
            bits = bits.Slice(0, bytesWrittenOrNeeded);

            var sb = new ValueStringBuilder(stackalloc char[128]); // each byte is typically two chars

            int cur = bits.Length - 1;
            if (cur > -1)
            {
                // [FF..F8] drop the high F as the two's complement negative number remains clear
                // [F7..08] retain the high bits as the two's complement number is wrong without it
                // [07..00] drop the high 0 as the two's complement positive number remains clear
                bool clearHighF = false;
                byte head = bits[cur];

                if (head > 0xF7)
                {
                    head -= 0xF0;
                    clearHighF = true;
                }

                if (head < 0x08 || clearHighF)
                {
                    // {0xF8-0xFF} print as {8-F}
                    // {0x00-0x07} print as {0-7}
                    sb.Append(head < 10 ?
                        (char)(head + '0') :
                        format == 'X' ? (char)((head & 0xF) - 10 + 'A') : (char)((head & 0xF) - 10 + 'a'));
                    cur--;
                }
            }

            if (cur > -1)
            {
                Span<char> chars = sb.AppendSpan((cur + 1) * 2);
                int charsPos = 0;
                string hexValues = format == 'x' ? "0123456789abcdef" : "0123456789ABCDEF";
                while (cur > -1)
                {
                    byte b = bits[cur--];
                    chars[charsPos++] = hexValues[b >> 4];
                    chars[charsPos++] = hexValues[b & 0xF];
                }
            }

            if (digits > sb.Length)
            {
                // Insert leading zeros, e.g. user specified "X5" so we create "0ABCD" instead of "ABCD"
                sb.Insert(
                    0,
                    value._sign >= 0 ? '0' : (format == 'x') ? 'f' : 'F',
                    digits - sb.Length);
            }

            if (arrayToReturnToPool != null)
            {
                ArrayPool<byte>.Shared.Return(arrayToReturnToPool);
            }

            if (targetSpan)
            {
                spanSuccess = sb.TryCopyTo(destination, out charsWritten);
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return sb.ToString();
            }
        }

        internal static string FormatBigInteger(BigInteger value, string? format, NumberFormatInfo info)
        {
            return FormatBigInteger(targetSpan: false, value, format, format, info, default, out _, out _)!;
        }

        internal static bool TryFormatBigInteger(BigInteger value, ReadOnlySpan<char> format, NumberFormatInfo info, Span<char> destination, out int charsWritten)
        {
            FormatBigInteger(targetSpan: true, value, null, format, info, destination, out charsWritten, out bool spanSuccess);
            return spanSuccess;
        }

        private static string? FormatBigInteger(
            bool targetSpan, BigInteger value,
            string? formatString, ReadOnlySpan<char> formatSpan,
            NumberFormatInfo info, Span<char> destination, out int charsWritten, out bool spanSuccess)
        {
            Debug.Assert(formatString == null || formatString.Length == formatSpan.Length);

            int digits = 0;
            char fmt = ParseFormatSpecifier(formatSpan, out digits);
            if (fmt == 'x' || fmt == 'X')
            {
                return FormatBigIntegerToHex(targetSpan, value, fmt, digits, info, destination, out charsWritten, out spanSuccess);
            }


            if (value._bits == null)
            {
                if (fmt == 'g' || fmt == 'G' || fmt == 'r' || fmt == 'R')
                {
                    formatSpan = formatString = digits > 0 ? string.Format("D{0}", digits) : "D";
                }

                if (targetSpan)
                {
                    spanSuccess = value._sign.TryFormat(destination, out charsWritten, formatSpan, info);
                    return null;
                }
                else
                {
                    charsWritten = 0;
                    spanSuccess = false;
                    return value._sign.ToString(formatString, info);
                }
            }

            // First convert to base 10^9.
            const uint kuBase = 1000000000; // 10^9
            const int kcchBase = 9;

            int cuSrc = value._bits.Length;
            int cuMax;
            try
            {
                cuMax = checked(cuSrc * 10 / 9 + 2);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }
            uint[] rguDst = new uint[cuMax];
            int cuDst = 0;

            for (int iuSrc = cuSrc; --iuSrc >= 0;)
            {
                uint uCarry = value._bits[iuSrc];
                for (int iuDst = 0; iuDst < cuDst; iuDst++)
                {
                    Debug.Assert(rguDst[iuDst] < kuBase);
                    ulong uuRes = NumericsHelpers.MakeUlong(rguDst[iuDst], uCarry);
                    rguDst[iuDst] = (uint)(uuRes % kuBase);
                    uCarry = (uint)(uuRes / kuBase);
                }
                if (uCarry != 0)
                {
                    rguDst[cuDst++] = uCarry % kuBase;
                    uCarry /= kuBase;
                    if (uCarry != 0)
                        rguDst[cuDst++] = uCarry;
                }
            }

            int cchMax;
            try
            {
                // Each uint contributes at most 9 digits to the decimal representation.
                cchMax = checked(cuDst * kcchBase);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }

            bool decimalFmt = (fmt == 'g' || fmt == 'G' || fmt == 'd' || fmt == 'D' || fmt == 'r' || fmt == 'R');
            if (decimalFmt)
            {
                if (digits > 0 && digits > cchMax)
                    cchMax = digits;
                if (value._sign < 0)
                {
                    try
                    {
                        // Leave an extra slot for a minus sign.
                        cchMax = checked(cchMax + info.NegativeSign.Length);
                    }
                    catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }
                }
            }

            int rgchBufSize;

            try
            {
                // We'll pass the rgch buffer to native code, which is going to treat it like a string of digits, so it needs
                // to be null terminated.  Let's ensure that we can allocate a buffer of that size.
                rgchBufSize = checked(cchMax + 1);
            }
            catch (OverflowException e) { throw new FormatException(SR.Format_TooLarge, e); }

            char[] rgch = new char[rgchBufSize];

            int ichDst = cchMax;

            for (int iuDst = 0; iuDst < cuDst - 1; iuDst++)
            {
                uint uDig = rguDst[iuDst];
                Debug.Assert(uDig < kuBase);
                for (int cch = kcchBase; --cch >= 0;)
                {
                    rgch[--ichDst] = (char)('0' + uDig % 10);
                    uDig /= 10;
                }
            }
            for (uint uDig = rguDst[cuDst - 1]; uDig != 0;)
            {
                rgch[--ichDst] = (char)('0' + uDig % 10);
                uDig /= 10;
            }

            if (!decimalFmt)
            {
                // sign = true for negative and false for 0 and positive values
                bool sign = (value._sign < 0);
                // The cut-off point to switch (G)eneral from (F)ixed-point to (E)xponential form
                int precision = 29;
                int scale = cchMax - ichDst;

                var sb = new ValueStringBuilder(stackalloc char[128]); // arbitrary stack cut-off
                FormatProvider.FormatBigInteger(ref sb, precision, scale, sign, formatSpan, info, rgch, ichDst);

                if (targetSpan)
                {
                    spanSuccess = sb.TryCopyTo(destination, out charsWritten);
                    return null;
                }
                else
                {
                    charsWritten = 0;
                    spanSuccess = false;
                    return sb.ToString();
                }
            }

            // Format Round-trip decimal
            // This format is supported for integral types only. The number is converted to a string of
            // decimal digits (0-9), prefixed by a minus sign if the number is negative. The precision
            // specifier indicates the minimum number of digits desired in the resulting string. If required,
            // the number is padded with zeros to its left to produce the number of digits given by the
            // precision specifier.
            int numDigitsPrinted = cchMax - ichDst;
            while (digits > 0 && digits > numDigitsPrinted)
            {
                // pad leading zeros
                rgch[--ichDst] = '0';
                digits--;
            }
            if (value._sign < 0)
            {
                string negativeSign = info.NegativeSign;
                for (int i = negativeSign.Length - 1; i > -1; i--)
                    rgch[--ichDst] = negativeSign[i];
            }

            int resultLength = cchMax - ichDst;
            if (!targetSpan)
            {
                charsWritten = 0;
                spanSuccess = false;
                return new string(rgch, ichDst, cchMax - ichDst);
            }
            else if (new ReadOnlySpan<char>(rgch, ichDst, cchMax - ichDst).TryCopyTo(destination))
            {
                charsWritten = resultLength;
                spanSuccess = true;
                return null;
            }
            else
            {
                charsWritten = 0;
                spanSuccess = false;
                return null;
            }
        }
    }
}

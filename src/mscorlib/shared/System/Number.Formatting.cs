// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace System
{
    internal static partial class Number
    {
        internal const int DecimalPrecision = 29; // Decimal.DecCalc also uses this value
        private const int MinStringBufferSize = 105;
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

        public static string FormatInt32(int value, string format, NumberFormatInfo info)
        {
            int digits;
            char fmt = ParseFormatSpecifier(format, out digits);

            // ANDing fmt with FFDF has the effect of uppercasing the character because we've removed the bit
            // that marks lower-case.
            switch (fmt)
            {
                case 'G':
                case 'g':
                    if (digits > 0)
                    {
                        NumberBuffer number = new NumberBuffer();
                        Int32ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
                    // fall through
                    goto case 'D';

                case 'D':
                case 'd':
                    return Int32ToDecStr(value, digits, info.NegativeSign);

                case 'X':
                case 'x':
                    // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                    // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                    // produces lowercase.
                    return Int32ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits);

                default:
                    {
                        NumberBuffer number = new NumberBuffer();
                        Int32ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
            }
        }

        public static string FormatUInt32(uint value, string format, NumberFormatInfo info)
        {
            int digits;
            char fmt = ParseFormatSpecifier(format, out digits);

            // ANDing fmt with FFDF has the effect of uppercasing the character because we've removed the bit
            // that marks lower-case.
            switch (fmt)
            {
                case 'G':
                case 'g':
                    if (digits > 0)
                    {
                        NumberBuffer number = new NumberBuffer();
                        UInt32ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
                    // fall through
                    goto case 'D';

                case 'D':
                case 'd':
                    return UInt32ToDecStr(value, digits);

                case 'X':
                case 'x':
                    // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                    // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                    // produces lowercase.
                    return Int32ToHexStr((int)value, (char)(fmt - ('X' - 'A' + 10)), digits);

                default:
                    {
                        NumberBuffer number = new NumberBuffer();
                        UInt32ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
            }
        }

        public static string FormatInt64(long value, string format, NumberFormatInfo info)
        {
            int digits;
            char fmt = ParseFormatSpecifier(format, out digits);

            // ANDing fmt with FFDF has the effect of uppercasing the character because we've removed the bit
            // that marks lower-case.
            switch (fmt)
            {
                case 'G':
                case 'g':
                    if (digits > 0)
                    {
                        NumberBuffer number = new NumberBuffer();
                        Int64ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
                    // fall through
                    goto case 'D';

                case 'D':
                case 'd':
                    return Int64ToDecStr(value, digits, info.NegativeSign);

                case 'X':
                case 'x':
                    // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                    // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                    // produces lowercase.
                    return Int64ToHexStr(value, (char)(fmt - ('X' - 'A' + 10)), digits);

                default:
                    {
                        NumberBuffer number = new NumberBuffer();
                        Int64ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
            }
        }

        public static string FormatUInt64(ulong value, string format, NumberFormatInfo info)
        {
            int digits;
            char fmt = ParseFormatSpecifier(format, out digits);

            // ANDing fmt with FFDF has the effect of uppercasing the character because we've removed the bit
            // that marks lower-case.
            switch (fmt)
            {
                case 'G':
                case 'g':
                    if (digits > 0)
                    {
                        NumberBuffer number = new NumberBuffer();
                        UInt64ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
                    // fall through
                    goto case 'D';

                case 'D':
                case 'd':
                    return UInt64ToDecStr(value, digits);

                case 'X':
                case 'x':
                    // The fmt-(X-A+10) hack has the effect of dictating whether we produce uppercase or lowercase
                    // hex numbers for a-f. 'X' as the fmt code produces uppercase. 'x' as the format code
                    // produces lowercase.
                    return Int64ToHexStr((long)value, (char)(fmt - ('X' - 'A' + 10)), digits);

                default:
                    {
                        NumberBuffer number = new NumberBuffer();
                        UInt64ToNumber(value, ref number);
                        if (fmt != 0)
                            return NumberToString(number, fmt, digits, info, false);
                        return NumberToStringFormat(number, format, info);
                    }
            }
        }

        internal static unsafe void Int32ToDecChars(char[] buffer, ref int index, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                buffer[--index] = (char)(value % 10 + '0');
                value /= 10;
            }
        }

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
            int index = Int32Precision;
            Int32ToDecChars(buffer, ref index, (uint)value, 0);
            int i = Int32Precision - index;

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = buffer[index++];
            *dst = '\0';
        }

        private static unsafe string Int32ToDecStr(int value, int digits, string sNegative)
        {
            if (digits < 1)
                digits = 1;

            int maxDigitsLength = (digits > 15) ? digits : 15; // Since an int32 can have maximum of 10 chars as a string
            int bufferLength = (maxDigitsLength > 100) ? maxDigitsLength : 100;
            int negLength = 0;
            string src = null;

            if (value < 0)
            {
                src = sNegative;
                negLength = sNegative.Length;
                if (negLength > bufferLength - maxDigitsLength)
                    bufferLength = negLength + maxDigitsLength;
            }

            char* buffer = stackalloc char[bufferLength];

            int index = bufferLength;
            Int32ToDecChars(buffer, ref index, (uint)(value >= 0 ? value : -value), digits);

            if (value < 0)
            {
                for (int i = negLength - 1; i >= 0; i--)
                    buffer[--index] = src[i];
            }

            return new string(buffer, index, bufferLength - index);
        }

        private static string Int32ToHexStr(int value, char hexBase, int digits)
        {
            if (digits < 1)
                digits = 1;
            char[] buffer = new char[100];
            int index = 100;
            Int32ToHexChars(buffer, ref index, (uint)value, hexBase, digits);
            return new string(buffer, index, 100 - index);
        }

        private static void Int32ToHexChars(char[] buffer, ref int index, uint value, int hexBase, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                byte digit = (byte)(value & 0xF);
                buffer[--index] = (char)(digit + (digit < 10 ? (byte)'0' : hexBase));
                value >>= 4;
            }
        }

        private static unsafe void UInt32ToNumber(uint value, ref NumberBuffer number)
        {
            number.precision = UInt32Precision;
            number.sign = false;

            char* buffer = number.digits;
            int index = UInt32Precision;
            Int32ToDecChars(buffer, ref index, value, 0);
            int i = UInt32Precision - index;

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = buffer[index++];
            *dst = '\0';
        }

        private static unsafe string UInt32ToDecStr(uint value, int digits)
        {
            if (digits < 1)
                digits = 1;

            char* buffer = stackalloc char[100];
            int index = 100;
            Int32ToDecChars(buffer, ref index, value, digits);

            return new string(buffer, index, 100 - index);
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
            int index = Int64Precision;
            while (High32(value) != 0)
                Int32ToDecChars(buffer, ref index, Int64DivMod1E9(ref value), 9);
            Int32ToDecChars(buffer, ref index, Low32(value), 0);
            int i = Int64Precision - index;

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = buffer[index++];
            *dst = '\0';
        }

        private static unsafe string Int64ToDecStr(long input, int digits, string sNegative)
        {
            if (digits < 1)
                digits = 1;

            ulong value = (ulong)input;
            int sign = (int)High32(value);

            // digits as specified in the format string can be at most 99.
            int maxDigitsLength = (digits > 20) ? digits : 20;
            int bufferLength = (maxDigitsLength > 100) ? maxDigitsLength : 100;

            if (sign < 0)
            {
                value = (ulong)(-input);
                int negLength = sNegative.Length;
                if (negLength > bufferLength - maxDigitsLength)
                    bufferLength = negLength + maxDigitsLength;
            }

            char* buffer = stackalloc char[bufferLength];
            int index = bufferLength;
            while (High32(value) != 0)
            {
                Int32ToDecChars(buffer, ref index, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            Int32ToDecChars(buffer, ref index, Low32(value), digits);

            if (sign < 0)
            {
                for (int i = sNegative.Length - 1; i >= 0; i--)
                    buffer[--index] = sNegative[i];
            }

            return new string(buffer, index, bufferLength - index);
        }

        private static string Int64ToHexStr(long value, char hexBase, int digits)
        {
            char[] buffer = new char[100];
            int index = 100;

            if (High32((ulong)value) != 0)
            {
                Int32ToHexChars(buffer, ref index, Low32((ulong)value), hexBase, 8);
                Int32ToHexChars(buffer, ref index, High32((ulong)value), hexBase, digits - 8);
            }
            else
            {
                if (digits < 1)
                    digits = 1;
                Int32ToHexChars(buffer, ref index, Low32((ulong)value), hexBase, digits);
            }

            return new string(buffer, index, 100 - index);
        }

        private static unsafe void UInt64ToNumber(ulong value, ref NumberBuffer number)
        {
            number.precision = UInt64Precision;
            number.sign = false;

            char* buffer = number.digits;
            int index = UInt64Precision;

            while (High32(value) != 0)
                Int32ToDecChars(buffer, ref index, Int64DivMod1E9(ref value), 9);
            Int32ToDecChars(buffer, ref index, Low32(value), 0);
            int i = UInt64Precision - index;

            number.scale = i;

            char* dst = number.digits;
            while (--i >= 0)
                *dst++ = buffer[index++];
            *dst = '\0';
        }

        private static unsafe string UInt64ToDecStr(ulong value, int digits)
        {
            if (digits < 1)
                digits = 1;

            char* buffer = stackalloc char[100];
            int index = 100;
            while (High32(value) != 0)
            {
                Int32ToDecChars(buffer, ref index, Int64DivMod1E9(ref value), 9);
                digits -= 9;
            }
            Int32ToDecChars(buffer, ref index, Low32(value), digits);

            return new string(buffer, index, 100 - index);
        }

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> str, NumberStyles options, ref NumberBuffer number, StringBuilder sb, NumberFormatInfo numfmt, bool parseDecimal)
        {
            Debug.Assert(numfmt != null);

            fixed (char* stringPointer = &str.DangerousGetPinnableReference())
            {
                char* p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, sb, numfmt, parseDecimal)
                    || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                {
                    return false;
                }
            }

            return true;
        }

        internal static unsafe void Int32ToDecChars(char* buffer, ref int index, uint value, int digits)
        {
            while (--digits >= 0 || value != 0)
            {
                buffer[--index] = (char)(value % 10 + '0');
                value /= 10;
            }
        }

        internal static unsafe char ParseFormatSpecifier(string format, out int digits)
        {
            if (format != null)
            {
                fixed (char* pFormat = format)
                {
                    int i = 0;
                    char ch = pFormat[i];
                    if (ch != 0)
                    {
                        if (((ch >= 'A') && (ch <= 'Z')) || ((ch >= 'a') && (ch <= 'z')))
                        {
                            i++;
                            int n = -1;
                            if ((pFormat[i] >= '0') && (pFormat[i] <= '9'))
                            {
                                n = pFormat[i++] - '0';
                                while ((pFormat[i] >= '0') && (pFormat[i] <= '9'))
                                {
                                    n = (n * 10) + pFormat[i++] - '0';
                                    if (n >= 10)
                                        break;
                                }
                            }
                            if (pFormat[i] == 0)
                            {
                                digits = n;
                                return ch;
                            }
                        }

                        digits = -1;
                        return '\0';
                    }
                }
            }

            digits = -1;
            return 'G';
        }

        internal static unsafe string NumberToString(NumberBuffer number, char format, int nMaxDigits, NumberFormatInfo info, bool isDecimal)
        {
            int nMinDigits = -1;

            StringBuilder sb = new StringBuilder(MinStringBufferSize);

            switch (format)
            {
                case 'C':
                case 'c':
                    {
                        nMinDigits = nMaxDigits >= 0 ? nMaxDigits : info.CurrencyDecimalDigits;
                        if (nMaxDigits < 0)
                            nMaxDigits = info.CurrencyDecimalDigits;

                        RoundNumber(ref number, number.scale + nMaxDigits); // Don't change this line to use digPos since digCount could have its sign changed.

                        FormatCurrency(sb, number, nMinDigits, nMaxDigits, info);

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

                        FormatFixed(sb, number, nMinDigits, nMaxDigits, info, null, info.NumberDecimalSeparator, null);

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

                        FormatNumber(sb, number, nMinDigits, nMaxDigits, info);

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

                        FormatScientific(sb, number, nMinDigits, nMaxDigits, info, format);

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

                        FormatGeneral(sb, number, nMinDigits, nMaxDigits, info, (char)(format - ('G' - 'E')), !enableRounding);

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

                        FormatPercent(sb, number, nMinDigits, nMaxDigits, info);

                        break;
                    }

                default:
                    throw new FormatException(SR.Argument_BadFormatSpecifier);
            }

            return sb.ToString();
        }

        internal static unsafe string NumberToStringFormat(NumberBuffer number, string format, NumberFormatInfo info)
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

                fixed (char* pFormat = format)
                {
                    while ((ch = pFormat[src++]) != 0 && ch != ';')
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
                                while (pFormat[src] != 0 && pFormat[src++] != ch)
                                    ;
                                break;
                            case '\\':
                                if (pFormat[src] != 0)
                                    src++;
                                break;
                            case 'E':
                            case 'e':
                                if (pFormat[src] == '0' || ((pFormat[src] == '+' || pFormat[src] == '-') && pFormat[src + 1] == '0'))
                                {
                                    while (pFormat[++src] == '0')
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
            int[] thousandsSepPos = new int[4];
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

                    int[] groupDigits = info.NumberGroupSizes;

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
                            Array.Resize(ref thousandsSepPos, thousandsSepPos.Length * 2);

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

            StringBuilder sb = new StringBuilder(MinStringBufferSize);

            if (number.sign && section == 0)
                sb.Append(info.NegativeSign);

            bool decimalWritten = false;

            fixed (char* pFormat = format)
            {
                char* cur = dig;

                while ((ch = pFormat[src++]) != 0 && ch != ';')
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
                            while (pFormat[src] != 0 && pFormat[src] != ch)
                                sb.Append(pFormat[src++]);
                            if (pFormat[src] != 0)
                                src++;
                            break;
                        case '\\':
                            if (pFormat[src] != 0)
                                sb.Append(pFormat[src++]);
                            break;
                        case 'E':
                        case 'e':
                            {
                                bool positiveSign = false;
                                int i = 0;
                                if (scientific)
                                {
                                    if (pFormat[src] == '0')
                                    {
                                        // Handles E0, which should format the same as E-0
                                        i++;
                                    }
                                    else if (pFormat[src] == '+' && pFormat[src + 1] == '0')
                                    {
                                        // Handles E+0
                                        positiveSign = true;
                                    }
                                    else if (pFormat[src] == '-' && pFormat[src + 1] == '0')
                                    {
                                        // Handles E-0
                                        // Do nothing, this is just a place holder s.t. we don't break out of the loop.
                                    }
                                    else
                                    {
                                        sb.Append(ch);
                                        break;
                                    }

                                    while (pFormat[++src] == '0')
                                        i++;
                                    if (i > 10)
                                        i = 10;

                                    int exp = dig[0] == 0 ? 0 : number.scale - decimalPos;
                                    FormatExponent(sb, info, exp, ch, i, positiveSign);
                                    scientific = false;
                                }
                                else
                                {
                                    sb.Append(ch); // Copy E or e to output
                                    if (pFormat[src] == '+' || pFormat[src] == '-')
                                        sb.Append(pFormat[src++]);
                                    while (pFormat[src] == '0')
                                        sb.Append(pFormat[src++]);
                                }
                                break;
                            }
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString();
        }

        private static void FormatCurrency(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negCurrencyFormats[info.CurrencyNegativePattern] :
                s_posCurrencyFormats[info.CurrencyPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.CurrencyGroupSizes, info.CurrencyDecimalSeparator, info.CurrencyGroupSeparator);
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

        private static unsafe int wcslen(char* s)
        {
            int result = 0;
            while (*s++ != '\0')
                result++;
            return result;
        }

        private static unsafe void FormatFixed(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, int[] groupDigits, string sDecimal, string sGroup)
        {
            int digPos = number.scale;
            char* dig = number.digits;
            int digLength = wcslen(dig);

            if (digPos > 0)
            {
                if (groupDigits != null)
                {
                    int groupSizeIndex = 0;                             // Index into the groupDigits array.
                    int groupSizeCount = groupDigits[groupSizeIndex];   // The current total of group size.
                    int groupSizeLen = groupDigits.Length;              // The length of groupDigits array.
                    int bufferSize = digPos;                            // The length of the result buffer string.
                    int groupSeparatorLen = sGroup.Length;              // The length of the group separator string.
                    int groupSize = 0;                                  // The current group size.

                    // Find out the size of the string buffer for the result.
                    if (groupSizeLen != 0) // You can pass in 0 length arrays
                    {
                        while (digPos > groupSizeCount)
                        {
                            groupSize = groupDigits[groupSizeIndex];
                            if (groupSize == 0)
                                break;

                            bufferSize += groupSeparatorLen;
                            if (groupSizeIndex < groupSizeLen - 1)
                                groupSizeIndex++;

                            groupSizeCount += groupDigits[groupSizeIndex];
                            if (groupSizeCount < 0 || bufferSize < 0)
                                throw new ArgumentOutOfRangeException(); // If we overflow
                        }
                        if (groupSizeCount == 0) // If you passed in an array with one entry as 0, groupSizeCount == 0
                            groupSize = 0;
                        else
                            groupSize = groupDigits[0];
                    }

                    char* tmpBuffer = stackalloc char[bufferSize];
                    groupSizeIndex = 0;
                    int digitCount = 0;
                    int digStart;
                    digStart = (digPos < digLength) ? digPos : digLength;
                    char* p = tmpBuffer + bufferSize - 1;
                    for (int i = digPos - 1; i >= 0; i--)
                    {
                        *(p--) = (i < digStart) ? dig[i] : '0';

                        if (groupSize > 0)
                        {
                            digitCount++;
                            if ((digitCount == groupSize) && (i != 0))
                            {
                                for (int j = groupSeparatorLen - 1; j >= 0; j--)
                                    *(p--) = sGroup[j];

                                if (groupSizeIndex < groupSizeLen - 1)
                                {
                                    groupSizeIndex++;
                                    groupSize = groupDigits[groupSizeIndex];
                                }
                                digitCount = 0;
                            }
                        }
                    }

                    sb.Append(tmpBuffer, bufferSize);
                    dig += digStart;
                }
                else
                {
                    int digits = Math.Min(digLength, digPos);
                    sb.Append(dig, digits);
                    dig += digits;
                    if (digPos > digLength)
                        sb.Append('0', digPos - digLength);
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

        private static void FormatNumber(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negNumberFormats[info.NumberNegativePattern] :
                PosNumberFormat;

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.NumberGroupSizes, info.NumberDecimalSeparator, info.NumberGroupSeparator);
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

        private static unsafe void FormatScientific(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar)
        {
            char* dig = number.digits;

            sb.Append((*dig != 0) ? *dig++ : '0');

            if (nMaxDigits != 1) // For E0 we would like to suppress the decimal point
                sb.Append(info.NumberDecimalSeparator);

            while (--nMaxDigits > 0)
                sb.Append((*dig != 0) ? *dig++ : '0');

            int e = number.digits[0] == 0 ? 0 : number.scale - 1;
            FormatExponent(sb, info, e, expChar, 3, true);
        }

        private static unsafe void FormatExponent(StringBuilder sb, NumberFormatInfo info, int value, char expChar, int minDigits, bool positiveSign)
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

            char* digits = stackalloc char[11];
            int index = 10;
            Int32ToDecChars(digits, ref index, (uint)value, minDigits);
            int i = 10 - index;
            while (--i >= 0)
                sb.Append(digits[index++]);
        }

        private static unsafe void FormatGeneral(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info, char expChar, bool bSuppressScientific)
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
                FormatExponent(sb, info, number.scale - 1, expChar, 2, true);
        }

        private static void FormatPercent(StringBuilder sb, NumberBuffer number, int nMinDigits, int nMaxDigits, NumberFormatInfo info)
        {
            string fmt = number.sign ?
                s_negPercentFormats[info.PercentNegativePattern] :
                s_posPercentFormats[info.PercentPositivePattern];

            foreach (char ch in fmt)
            {
                switch (ch)
                {
                    case '#':
                        FormatFixed(sb, number, nMinDigits, nMaxDigits, info, info.PercentGroupSizes, info.PercentDecimalSeparator, info.PercentGroupSeparator);
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

        private static unsafe int FindSection(string format, int section)
        {
            int src;
            char ch;

            if (section == 0)
                return 0;

            fixed (char* pFormat = format)
            {
                src = 0;
                for (;;)
                {
                    switch (ch = pFormat[src++])
                    {
                        case '\'':
                        case '"':
                            while (pFormat[src] != 0 && pFormat[src++] != ch)
                                ;
                            break;
                        case '\\':
                            if (pFormat[src] != 0)
                                src++;
                            break;
                        case ';':
                            if (--section != 0)
                                break;
                            if (pFormat[src] != 0 && pFormat[src] != ';')
                                return src;
                            goto case '\0';
                        case '\0':
                            return 0;
                    }
                }
            }
        }

        private static uint Low32(ulong value) => (uint)value;

        private static uint High32(ulong value) => (uint)(((ulong)value & 0xFFFFFFFF00000000) >> 32);

        private static uint Int64DivMod1E9(ref ulong value)
        {
            uint rem = (uint)(value % 1000000000);
            value /= 1000000000;
            return rem;
        }
    }
}

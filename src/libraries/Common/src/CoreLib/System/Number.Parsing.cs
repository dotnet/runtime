// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System
{
    // The Parse methods provided by the numeric classes convert a
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

    internal partial class Number
    {
        private const int Int32Precision = 10;
        private const int UInt32Precision = Int32Precision;
        private const int Int64Precision = 19;
        private const int UInt64Precision = 20;

        private static bool HexNumberToInt32(ref NumberBuffer number, ref int value)
        {
            uint passedValue = 0;
            bool returnValue = HexNumberToUInt32(ref number, ref passedValue);
            value = (int)passedValue;
            return returnValue;
        }

        private static bool HexNumberToInt64(ref NumberBuffer number, ref long value)
        {
            ulong passedValue = 0;
            bool returnValue = HexNumberToUInt64(ref number, ref passedValue);
            value = (long)passedValue;
            return returnValue;
        }

        private static unsafe bool HexNumberToUInt32(ref NumberBuffer number, ref uint value)
        {
            int i = number.scale;
            if (i > UInt32Precision || i < number.precision)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);

            uint n = 0;
            while (--i >= 0)
            {
                if (n > ((uint)0xFFFFFFFF / 16))
                {
                    return false;
                }
                n *= 16;
                if (*p != '\0')
                {
                    uint newN = n;
                    if (*p != '\0')
                    {
                        if (*p >= '0' && *p <= '9')
                        {
                            newN += (uint)(*p - '0');
                        }
                        else
                        {
                            if (*p >= 'A' && *p <= 'F')
                            {
                                newN += (uint)((*p - 'A') + 10);
                            }
                            else
                            {
                                Debug.Assert(*p >= 'a' && *p <= 'f');
                                newN += (uint)((*p - 'a') + 10);
                            }
                        }
                        p++;
                    }

                    // Detect an overflow here...
                    if (newN < n)
                    {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        private static unsafe bool HexNumberToUInt64(ref NumberBuffer number, ref ulong value)
        {
            int i = number.scale;
            if (i > UInt64Precision || i < number.precision)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);

            ulong n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFFFFFFFFFF / 16))
                {
                    return false;
                }
                n *= 16;
                if (*p != '\0')
                {
                    ulong newN = n;
                    if (*p != '\0')
                    {
                        if (*p >= '0' && *p <= '9')
                        {
                            newN += (ulong)(*p - '0');
                        }
                        else
                        {
                            if (*p >= 'A' && *p <= 'F')
                            {
                                newN += (ulong)((*p - 'A') + 10);
                            }
                            else
                            {
                                Debug.Assert(*p >= 'a' && *p <= 'f');
                                newN += (ulong)((*p - 'a') + 10);
                            }
                        }
                        p++;
                    }

                    // Detect an overflow here...
                    if (newN < n)
                    {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        private static unsafe bool NumberToInt32(ref NumberBuffer number, ref int value)
        {
            int i = number.scale;
            if (i > Int32Precision || i < number.precision)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);
            int n = 0;
            while (--i >= 0)
            {
                if ((uint)n > (0x7FFFFFFF / 10))
                {
                    return false;
                }
                n *= 10;
                if (*p != '\0')
                {
                    n += (int)(*p++ - '0');
                }
            }
            if (number.sign)
            {
                n = -n;
                if (n > 0)
                {
                    return false;
                }
            }
            else
            {
                if (n < 0)
                {
                    return false;
                }
            }
            value = n;
            return true;
        }

        private static unsafe bool NumberToInt64(ref NumberBuffer number, ref long value)
        {
            int i = number.scale;
            if (i > Int64Precision || i < number.precision)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);
            long n = 0;
            while (--i >= 0)
            {
                if ((ulong)n > (0x7FFFFFFFFFFFFFFF / 10))
                {
                    return false;
                }
                n *= 10;
                if (*p != '\0')
                {
                    n += (int)(*p++ - '0');
                }
            }
            if (number.sign)
            {
                n = -n;
                if (n > 0)
                {
                    return false;
                }
            }
            else
            {
                if (n < 0)
                {
                    return false;
                }
            }
            value = n;
            return true;
        }

        private static unsafe bool NumberToUInt32(ref NumberBuffer number, ref uint value)
        {
            int i = number.scale;
            if (i > UInt32Precision || i < number.precision || number.sign)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);
            uint n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFF / 10))
                {
                    return false;
                }
                n *= 10;
                if (*p != '\0')
                {
                    uint newN = n + (uint)(*p++ - '0');
                    // Detect an overflow here...
                    if (newN < n)
                    {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        private static unsafe bool NumberToUInt64(ref NumberBuffer number, ref ulong value)
        {
            int i = number.scale;
            if (i > UInt64Precision || i < number.precision || number.sign)
            {
                return false;
            }
            char* p = number.digits;
            Debug.Assert(p != null);
            ulong n = 0;
            while (--i >= 0)
            {
                if (n > (0xFFFFFFFFFFFFFFFF / 10))
                {
                    return false;
                }
                n *= 10;
                if (*p != '\0')
                {
                    ulong newN = n + (ulong)(*p++ - '0');
                    // Detect an overflow here...
                    if (newN < n)
                    {
                        return false;
                    }
                    n = newN;
                }
            }
            value = n;
            return true;
        }

        internal unsafe static int ParseInt32(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
        {
            NumberBuffer number = default;
            int i = 0;

            StringToNumber(s, style, ref number, info, false);

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt32(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_Int32);
                }
            }
            else
            {
                if (!NumberToInt32(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_Int32);
                }
            }
            return i;
        }

        internal unsafe static long ParseInt64(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            long i = 0;

            StringToNumber(value, options, ref number, numfmt, false);

            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt64(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_Int64);
                }
            }
            else
            {
                if (!NumberToInt64(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_Int64);
                }
            }
            return i;
        }

        internal unsafe static uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            uint i = 0;

            StringToNumber(value, options, ref number, numfmt, false);

            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt32(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_UInt32);
                }
            }
            else
            {
                if (!NumberToUInt32(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_UInt32);
                }
            }

            return i;
        }

        internal unsafe static ulong ParseUInt64(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            ulong i = 0;

            StringToNumber(value, options, ref number, numfmt, false);
            if ((options & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt64(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_UInt64);
                }
            }
            else
            {
                if (!NumberToUInt64(ref number, ref i))
                {
                    throw new OverflowException(SR.Overflow_UInt64);
                }
            }
            return i;
        }

        private unsafe static bool ParseNumber(ref char* str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo numfmt, bool parseDecimal)
        {
            const int StateSign = 0x0001;
            const int StateParens = 0x0002;
            const int StateDigits = 0x0004;
            const int StateNonZero = 0x0008;
            const int StateDecimal = 0x0010;
            const int StateCurrency = 0x0020;

            number.scale = 0;
            number.sign = false;
            string decSep;                  // decimal separator from NumberFormatInfo.
            string groupSep;                // group separator from NumberFormatInfo.
            string currSymbol = null;       // currency symbol from NumberFormatInfo.

            bool parsingCurrency = false;
            if ((options & NumberStyles.AllowCurrencySymbol) != 0)
            {
                currSymbol = numfmt.CurrencySymbol;

                // The idea here is to match the currency separators and on failure match the number separators to keep the perf of VB's IsNumeric fast.
                // The values of decSep are setup to use the correct relevant separator (currency in the if part and decimal in the else part).
                decSep = numfmt.CurrencyDecimalSeparator;
                groupSep = numfmt.CurrencyGroupSeparator;
                parsingCurrency = true;
            }
            else
            {
                decSep = numfmt.NumberDecimalSeparator;
                groupSep = numfmt.NumberGroupSeparator;
            }

            int state = 0;
            char* p = str;
            char ch = *p;
            char* next;

            while (true)
            {
                // Eat whitespace unless we've found a sign which isn't followed by a currency symbol.
                // "-Kr 1231.47" is legal but "- 1231.47" is not.
                if (!IsWhite(ch) || (options & NumberStyles.AllowLeadingWhite) == 0 || ((state & StateSign) != 0 && ((state & StateCurrency) == 0 && numfmt.NumberNegativePattern != 2)))
                {
                    if ((((options & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0) && ((next = MatchChars(p, numfmt.PositiveSign)) != null || ((next = MatchChars(p, numfmt.NegativeSign)) != null && (number.sign = true))))
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (ch == '(' && ((options & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.sign = true;
                    }
                    else if (currSymbol != null && (next = MatchChars(p, currSymbol)) != null)
                    {
                        state |= StateCurrency;
                        currSymbol = null;
                        // We already found the currency symbol. There should not be more currency symbols. Set
                        // currSymbol to NULL so that we won't search it again in the later code path.
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }
                }
                ch = *++p;
            }
            int digCount = 0;
            int digEnd = 0;
            while (true)
            {
                if ((ch >= '0' && ch <= '9') || (((options & NumberStyles.AllowHexSpecifier) != 0) && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))))
                {
                    state |= StateDigits;

                    if (ch != '0' || (state & StateNonZero) != 0)
                    {
                        if (digCount < NumberMaxDigits)
                        {
                            number.digits[digCount++] = ch;
                            if (ch != '0' || parseDecimal)
                            {
                                digEnd = digCount;
                            }
                        }
                        if ((state & StateDecimal) == 0)
                        {
                            number.scale++;
                        }
                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0)
                    {
                        number.scale--;
                    }
                }
                else if (((options & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, decSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, numfmt.NumberDecimalSeparator)) != null))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((options & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, groupSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, numfmt.NumberGroupSeparator)) != null))
                {
                    p = next - 1;
                }
                else
                {
                    break;
                }
                ch = *++p;
            }

            bool negExp = false;
            number.precision = digEnd;
            number.digits[digEnd] = '\0';
            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((options & NumberStyles.AllowExponent) != 0))
                {
                    char* temp = p;
                    ch = *++p;
                    if ((next = MatchChars(p, numfmt.positiveSign)) != null)
                    {
                        ch = *(p = next);
                    }
                    else if ((next = MatchChars(p, numfmt.negativeSign)) != null)
                    {
                        ch = *(p = next);
                        negExp = true;
                    }
                    if (ch >= '0' && ch <= '9')
                    {
                        int exp = 0;
                        do
                        {
                            exp = exp * 10 + (ch - '0');
                            ch = *++p;
                            if (exp > 1000)
                            {
                                exp = 9999;
                                while (ch >= '0' && ch <= '9')
                                {
                                    ch = *++p;
                                }
                            }
                        } while (ch >= '0' && ch <= '9');
                        if (negExp)
                        {
                            exp = -exp;
                        }
                        number.scale += exp;
                    }
                    else
                    {
                        p = temp;
                        ch = *p;
                    }
                }
                while (true)
                {
                    if (!IsWhite(ch) || (options & NumberStyles.AllowTrailingWhite) == 0)
                    {
                        if (((options & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0)) && ((next = MatchChars(p, numfmt.PositiveSign)) != null || (((next = MatchChars(p, numfmt.NegativeSign)) != null) && (number.sign = true))))
                        {
                            state |= StateSign;
                            p = next - 1;
                        }
                        else if (ch == ')' && ((state & StateParens) != 0))
                        {
                            state &= ~StateParens;
                        }
                        else if (currSymbol != null && (next = MatchChars(p, currSymbol)) != null)
                        {
                            currSymbol = null;
                            p = next - 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    ch = *++p;
                }
                if ((state & StateParens) == 0)
                {
                    if ((state & StateNonZero) == 0)
                    {
                        if (!parseDecimal)
                        {
                            number.scale = 0;
                        }
                        if ((state & StateDecimal) == 0)
                        {
                            number.sign = false;
                        }
                    }
                    str = p;
                    return true;
                }
            }
            str = p;
            return false;
        }

        internal unsafe static bool TryParseInt32(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out int result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt32(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToInt32(ref number, ref result))
                {
                    return false;
                }
            }
            return true;
        }

        internal unsafe static bool TryParseInt64(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out long result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToInt64(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToInt64(ref number, ref result))
                {
                    return false;
                }
            }
            return true;
        }

        internal unsafe static bool TryParseUInt32(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out uint result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt32(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToUInt32(ref number, ref result))
                {
                    return false;
                }
            }
            return true;
        }

        internal unsafe static bool TryParseUInt64(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out ulong result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(s, style, ref number, info, false))
            {
                return false;
            }

            if ((style & NumberStyles.AllowHexSpecifier) != 0)
            {
                if (!HexNumberToUInt64(ref number, ref result))
                {
                    return false;
                }
            }
            else
            {
                if (!NumberToUInt64(ref number, ref result))
                {
                    return false;
                }
            }
            return true;
        }

        internal unsafe static decimal ParseDecimal(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            decimal result = 0;

            StringToNumber(value, options, ref number, numfmt, true);

            if (!NumberBufferToDecimal(ref number, ref result))
            {
                throw new OverflowException(SR.Overflow_Decimal);
            }
            return result;
        }

        internal unsafe static double ParseDouble(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            double d = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                //If we failed TryStringToNumber, it may be from one of our special strings.
                //Check the three with which we're concerned and rethrow if it's not one of
                //those strings.
                ReadOnlySpan<char> sTrim = value.Trim();
                if (StringSpanHelpers.Equals(sTrim, numfmt.PositiveInfinitySymbol))
                {
                    return double.PositiveInfinity;
                }
                if (StringSpanHelpers.Equals(sTrim, numfmt.NegativeInfinitySymbol))
                {
                    return double.NegativeInfinity;
                }
                if (StringSpanHelpers.Equals(sTrim, numfmt.NaNSymbol))
                {
                    return double.NaN;
                }
                throw new FormatException(SR.Format_InvalidString);
            }

            if (!NumberBufferToDouble(ref number, ref d))
            {
                throw new OverflowException(SR.Overflow_Double);
            }

            return d;
        }

        internal unsafe static float ParseSingle(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt)
        {
            NumberBuffer number = default;
            double d = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                //If we failed TryStringToNumber, it may be from one of our special strings.
                //Check the three with which we're concerned and rethrow if it's not one of
                //those strings.
                ReadOnlySpan<char> sTrim = value.Trim();
                if (StringSpanHelpers.Equals(sTrim, numfmt.PositiveInfinitySymbol))
                {
                    return float.PositiveInfinity;
                }
                if (StringSpanHelpers.Equals(sTrim, numfmt.NegativeInfinitySymbol))
                {
                    return float.NegativeInfinity;
                }
                if (StringSpanHelpers.Equals(sTrim, numfmt.NaNSymbol))
                {
                    return float.NaN;
                }
                throw new FormatException(SR.Format_InvalidString);
            }

            if (!NumberBufferToDouble(ref number, ref d))
            {
                throw new OverflowException(SR.Overflow_Single);
            }
            float castSingle = (float)d;
            if (float.IsInfinity(castSingle))
            {
                throw new OverflowException(SR.Overflow_Single);
            }
            return castSingle;
        }

        internal unsafe static bool TryParseDecimal(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt, out decimal result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, true))
            {
                return false;
            }

            if (!NumberBufferToDecimal(ref number, ref result))
            {
                return false;
            }
            return true;
        }

        internal unsafe static bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt, out double result)
        {
            NumberBuffer number = default;
            result = 0;


            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                return false;
            }
            if (!NumberBufferToDouble(ref number, ref result))
            {
                return false;
            }
            return true;
        }

        internal unsafe static bool TryParseSingle(ReadOnlySpan<char> value, NumberStyles options, NumberFormatInfo numfmt, out float result)
        {
            NumberBuffer number = default;
            result = 0;
            double d = 0;

            if (!TryStringToNumber(value, options, ref number, numfmt, false))
            {
                return false;
            }
            if (!NumberBufferToDouble(ref number, ref d))
            {
                return false;
            }
            float castSingle = (float)d;
            if (float.IsInfinity(castSingle))
            {
                return false;
            }

            result = castSingle;
            return true;
        }

        private static unsafe void StringToNumber(ReadOnlySpan<char> str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo info, bool parseDecimal)
        {
            Debug.Assert(info != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(str))
            {
                char* p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, info, parseDecimal)
                    || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                {
                    throw new FormatException(SR.Format_InvalidString);
                }
            }
        }

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> str, NumberStyles options, ref NumberBuffer number, NumberFormatInfo numfmt, bool parseDecimal)
        {
            Debug.Assert(numfmt != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(str))
            {
                char* p = stringPointer;
                if (!ParseNumber(ref p, options, ref number, numfmt, parseDecimal)
                    || (p - stringPointer < str.Length && !TrailingZeros(str, (int)(p - stringPointer))))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TrailingZeros(ReadOnlySpan<char> s, int index)
        {
            // For compatibility, we need to allow trailing zeros at the end of a number string
            for (int i = index; i < s.Length; i++)
            {
                if (s[i] != '\0')
                {
                    return false;
                }
            }

            return true;
        }

        private unsafe static char* MatchChars(char* p, string str)
        {
            fixed (char* stringPointer = str)
            {
                return MatchChars(p, stringPointer);
            }
        }

        private unsafe static char* MatchChars(char* p, char* str)
        {
            Debug.Assert(p != null && str != null);

            if (*str == '\0')
            {
                return null;
            }
            
            // We only hurt the failure case
            // This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 as a
            // space character we use 0x20 space character instead to mean the same.
            while (*p == *str || (*str == '\u00a0' && *p == '\u0020'))
            {
                p++;
                str++;
                if (*str == '\0') return p;
            }

            return null;
        }

        private static bool IsWhite(char ch) => ch == 0x20 || (ch >= 0x09 && ch <= 0x0D);

        private static bool NumberBufferToDouble(ref NumberBuffer number, ref double value)
        {
            double d = NumberToDouble(ref number);
            uint e = DoubleHelper.Exponent(d);
            ulong m = DoubleHelper.Mantissa(d);

            if (e == 0x7FF)
            {
                return false;
            }

            if (e == 0 && m == 0)
            {
                d = 0;
            }

            value = d;
            return true;
        }

        private static class DoubleHelper
        {
            public static unsafe uint Exponent(double d) =>
                (*((uint*)&d + 1) >> 20) & 0x000007ff;

            public static unsafe ulong Mantissa(double d) =>
                *((ulong*)&d) & 0x000fffffffffffff;

            public static unsafe bool Sign(double d) =>
                (*((uint*)&d + 1) >> 31) != 0;
        }
    }
}

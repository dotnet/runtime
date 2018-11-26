// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

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

        /// <summary>256-element map from an ASCII char to its hex value, e.g. arr['b'] == 11. 0xFF means it's not a hex digit.</summary>
        internal static readonly int[] s_charToHexLookup =
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 15
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 31
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 47
            0x0,  0x1,  0x2,  0x3,  0x4,  0x5,  0x6,  0x7,  0x8,  0x9,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 63
            0xFF, 0xA,  0xB,  0xC,  0xD,  0xE,  0xF,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 79
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 95
            0xFF, 0xa,  0xb,  0xc,  0xd,  0xe,  0xf,  0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 111
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 127
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 143
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 159
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 175
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 191
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 207
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 223
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, // 239
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF  // 255
        };

        private static unsafe bool TryNumberToInt32(ref NumberBuffer number, ref int value)
        {
            number.CheckConsistency();

            int i = number.Scale;
            if (i > Int32Precision || i < number.DigitsCount)
            {
                return false;
            }
            byte* p = number.GetDigitsPointer();
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
                    n += (*p++ - '0');
                }
            }
            if (number.IsNegative)
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

        private static unsafe bool TryNumberToInt64(ref NumberBuffer number, ref long value)
        {
            number.CheckConsistency();

            int i = number.Scale;
            if (i > Int64Precision || i < number.DigitsCount)
            {
                return false;
            }
            byte* p = number.GetDigitsPointer();
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
                    n += (*p++ - '0');
                }
            }
            if (number.IsNegative)
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

        private static unsafe bool TryNumberToUInt32(ref NumberBuffer number, ref uint value)
        {
            number.CheckConsistency();

            int i = number.Scale;
            if (i > UInt32Precision || i < number.DigitsCount || number.IsNegative)
            {
                return false;
            }
            byte* p = number.GetDigitsPointer();
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

        private static unsafe bool TryNumberToUInt64(ref NumberBuffer number, ref ulong value)
        {
            number.CheckConsistency();

            int i = number.Scale;
            if (i > UInt64Precision || i < number.DigitsCount || number.IsNegative)
            {
                return false;
            }
            byte* p = number.GetDigitsPointer();
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

        internal static int ParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseInt32(value, styles, info, out int result, out bool failureIsOverflow))
            {
                ThrowOverflowOrFormatException(failureIsOverflow, nameof(SR.Overflow_Int32));
            }

            return result;
        }

        internal static long ParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseInt64(value, styles, info, out long result, out bool failureIsOverflow))
            {
                ThrowOverflowOrFormatException(failureIsOverflow, nameof(SR.Overflow_Int64));
            }

            return result;
        }

        internal static uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseUInt32(value, styles, info, out uint result, out bool failureIsOverflow))
            {
                ThrowOverflowOrFormatException(failureIsOverflow, nameof(SR.Overflow_UInt32));
            }

            return result;
        }

        internal static ulong ParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseUInt64(value, styles, info, out ulong result, out bool failureIsOverflow))
            {
                ThrowOverflowOrFormatException(failureIsOverflow, nameof(SR.Overflow_UInt64));
            }

            return result;
        }

        private static unsafe bool TryParseNumber(ref char* str, char* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
        {
            Debug.Assert(str != null);
            Debug.Assert(strEnd != null);
            Debug.Assert(str <= strEnd);
            Debug.Assert((styles & NumberStyles.AllowHexSpecifier) == 0);

            const int StateSign = 0x0001;
            const int StateParens = 0x0002;
            const int StateDigits = 0x0004;
            const int StateNonZero = 0x0008;
            const int StateDecimal = 0x0010;
            const int StateCurrency = 0x0020;

            Debug.Assert(number.DigitsCount == 0);
            Debug.Assert(number.Scale == 0);
            Debug.Assert(number.IsNegative == false);
            Debug.Assert(number.HasNonZeroTail == false);

            number.CheckConsistency();

            string decSep;                  // decimal separator from NumberFormatInfo.
            string groupSep;                // group separator from NumberFormatInfo.
            string currSymbol = null;       // currency symbol from NumberFormatInfo.

            bool parsingCurrency = false;
            if ((styles & NumberStyles.AllowCurrencySymbol) != 0)
            {
                currSymbol = info.CurrencySymbol;

                // The idea here is to match the currency separators and on failure match the number separators to keep the perf of VB's IsNumeric fast.
                // The values of decSep are setup to use the correct relevant separator (currency in the if part and decimal in the else part).
                decSep = info.CurrencyDecimalSeparator;
                groupSep = info.CurrencyGroupSeparator;
                parsingCurrency = true;
            }
            else
            {
                decSep = info.NumberDecimalSeparator;
                groupSep = info.NumberGroupSeparator;
            }

            int state = 0;
            char* p = str;
            char ch = p < strEnd ? *p : '\0';
            char* next;

            while (true)
            {
                // Eat whitespace unless we've found a sign which isn't followed by a currency symbol.
                // "-Kr 1231.47" is legal but "- 1231.47" is not.
                if (!IsWhite(ch) || (styles & NumberStyles.AllowLeadingWhite) == 0 || ((state & StateSign) != 0 && ((state & StateCurrency) == 0 && info.NumberNegativePattern != 2)))
                {
                    if ((((styles & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || ((next = MatchChars(p, strEnd, info.NegativeSign)) != null && (number.IsNegative = true))))
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (ch == '(' && ((styles & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.IsNegative = true;
                    }
                    else if (currSymbol != null && (next = MatchChars(p, strEnd, currSymbol)) != null)
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
                ch = ++p < strEnd ? *p : '\0';
            }

            int digCount = 0;
            int digEnd = 0;
            int maxDigCount = number.Digits.Length - 1;

            while (true)
            {
                if (IsDigit(ch))
                {
                    state |= StateDigits;

                    if (ch != '0' || (state & StateNonZero) != 0)
                    {
                        if (digCount < maxDigCount)
                        {
                            number.Digits[digCount++] = (byte)(ch);
                            if ((ch != '0') || (number.Kind != NumberBufferKind.Integer))
                            {
                                digEnd = digCount;
                            }
                        }
                        else if (ch != '0')
                        {
                            // For decimal and binary floating-point numbers, we only
                            // need to store digits up to maxDigCount. However, we still
                            // need to keep track of whether any additional digits past
                            // maxDigCount were non-zero, as that can impact rounding
                            // for an input that falls evenly between two representable
                            // results.

                            number.HasNonZeroTail = true;
                        }

                        if ((state & StateDecimal) == 0)
                        {
                            number.Scale++;
                        }
                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0)
                    {
                        number.Scale--;
                    }
                }
                else if (((styles & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, decSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, strEnd, info.NumberDecimalSeparator)) != null))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((styles & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, groupSep)) != null || ((parsingCurrency) && (state & StateCurrency) == 0) && (next = MatchChars(p, strEnd, info.NumberGroupSeparator)) != null))
                {
                    p = next - 1;
                }
                else
                {
                    break;
                }
                ch = ++p < strEnd ? *p : '\0';
            }

            bool negExp = false;
            number.DigitsCount = digEnd;
            number.Digits[digEnd] = (byte)('\0');
            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((styles & NumberStyles.AllowExponent) != 0))
                {
                    char* temp = p;
                    ch = ++p < strEnd ? *p : '\0';
                    if ((next = MatchChars(p, strEnd, info.positiveSign)) != null)
                    {
                        ch = (p = next) < strEnd ? *p : '\0';
                    }
                    else if ((next = MatchChars(p, strEnd, info.negativeSign)) != null)
                    {
                        ch = (p = next) < strEnd ? *p : '\0';
                        negExp = true;
                    }
                    if (IsDigit(ch))
                    {
                        int exp = 0;
                        do
                        {
                            exp = exp * 10 + (ch - '0');
                            ch = ++p < strEnd ? *p : '\0';
                            if (exp > 1000)
                            {
                                exp = 9999;
                                while (IsDigit(ch))
                                {
                                    ch = ++p < strEnd ? *p : '\0';
                                }
                            }
                        } while (IsDigit(ch));
                        if (negExp)
                        {
                            exp = -exp;
                        }
                        number.Scale += exp;
                    }
                    else
                    {
                        p = temp;
                        ch = p < strEnd ? *p : '\0';
                    }
                }
                while (true)
                {
                    if (!IsWhite(ch) || (styles & NumberStyles.AllowTrailingWhite) == 0)
                    {
                        if (((styles & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0)) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || (((next = MatchChars(p, strEnd, info.NegativeSign)) != null) && (number.IsNegative = true))))
                        {
                            state |= StateSign;
                            p = next - 1;
                        }
                        else if (ch == ')' && ((state & StateParens) != 0))
                        {
                            state &= ~StateParens;
                        }
                        else if (currSymbol != null && (next = MatchChars(p, strEnd, currSymbol)) != null)
                        {
                            currSymbol = null;
                            p = next - 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    ch = ++p < strEnd ? *p : '\0';
                }
                if ((state & StateParens) == 0)
                {
                    if ((state & StateNonZero) == 0)
                    {
                        if (number.Kind != NumberBufferKind.Decimal)
                        {
                            number.Scale = 0;
                        }
                        if ((number.Kind == NumberBufferKind.Integer) && (state & StateDecimal) == 0)
                        {
                            number.IsNegative = false;
                        }
                    }
                    str = p;
                    return true;
                }
            }
            str = p;
            return false;
        }

        internal static unsafe bool TryParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result, out bool failureIsOverflow)
        {
            result = 0;
            failureIsOverflow = false;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                return TryParseInt32IntegerStyle(value, styles, info, out result, ref failureIsOverflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseUInt32HexNumberStyle(value, styles, out Unsafe.As<int, uint>(ref result), ref failureIsOverflow);
            }

            byte* pDigits = stackalloc byte[Int32NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int32NumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return false;
            }

            if (!TryNumberToInt32(ref number, ref result))
            {
                failureIsOverflow = true;
                return false;
            }

            return true;
        }

        /// <summary>Parses int limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseInt32IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            int sign = 1;
            int index = 0;
            int num = value[0];

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            // Parse leading sign.
            if ((styles & NumberStyles.AllowLeadingSign) != 0)
            {
                string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;

                if (positiveSign == "+" && negativeSign == "-")
                {
                    if (num == '-')
                    {
                        sign = -1;
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;
                    if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        sign = -1;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
            }

            int answer = 0;

            if (IsDigit(num))
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                }

                // Parse most digits, up to the potential for overflow, which can't happen until after 9 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 8; i++) // next 8 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 10th digit.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num))
                    goto HasTrailingChars;
                index++;
                if (answer > int.MaxValue / 10)
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((uint)answer > (uint)int.MaxValue + (-1 * sign + 1) / 2)
                {
                    overflow = true;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
            result = answer * sign;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses long inputs limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseInt64IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            int sign = 1;
            int index = 0;
            int num = value[0];

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            // Parse leading sign.
            if ((styles & NumberStyles.AllowLeadingSign) != 0)
            {
                string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;

                if (positiveSign == "+" && negativeSign == "-")
                {
                    if (num == '-')
                    {
                        sign = -1;
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;
                    if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        sign = -1;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
            }

            long answer = 0;

            if (IsDigit(num))
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                }

                // Parse most digits, up to the potential for overflow, which can't happen until after 18 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 17; i++) // next 17 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 19th digit.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num))
                    goto HasTrailingChars;
                index++;
                if (answer > long.MaxValue / 10)
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((ulong)answer > (ulong)long.MaxValue + (ulong)((-1 * sign + 1) / 2)) // + sign => 0, - sign => 1
                {
                    overflow = true;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
            result = answer * sign;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        internal static unsafe bool TryParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result, out bool failureIsOverflow)
        {
            result = 0;
            failureIsOverflow = false;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                return TryParseInt64IntegerStyle(value, styles, info, out result, ref failureIsOverflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseUInt64HexNumberStyle(value, styles, out Unsafe.As<long, ulong>(ref result), ref failureIsOverflow);
            }

            byte* pDigits = stackalloc byte[Int64NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, Int64NumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return false;
            }

            if (!TryNumberToInt64(ref number, ref result))
            {
                failureIsOverflow = true;
                return false;
            }

            return true;
        }

        internal static unsafe bool TryParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result, out bool failureIsOverflow)
        {
            result = 0;
            failureIsOverflow = false;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                return TryParseUInt32IntegerStyle(value, styles, info, out result, ref failureIsOverflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseUInt32HexNumberStyle(value, styles, out result, ref failureIsOverflow);
            }

            byte* pDigits = stackalloc byte[UInt32NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt32NumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return false;
            }

            if (!TryNumberToUInt32(ref number, ref result))
            {
                failureIsOverflow = true;
                return false;
            }

            return true;
        }

        /// <summary>Parses uint limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseUInt32IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            bool hasNegativeSign = false;
            int index = 0;
            int num = value[0];

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            // Parse leading sign.
            if ((styles & NumberStyles.AllowLeadingSign) != 0)
            {
                string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;

                if (positiveSign == "+" && negativeSign == "-")
                {
                    if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '-')
                    {
                        hasNegativeSign = true;
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;
                    if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        hasNegativeSign = true;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
            }

            int answer = 0;

            if (IsDigit(num))
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                }

                // Parse most digits, up to the potential for overflow, which can't happen until after 9 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 8; i++) // next 8 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 10th digit.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num))
                    goto HasTrailingChars;
                index++;
                if ((uint)answer > uint.MaxValue / 10 || ((uint)answer == uint.MaxValue / 10 && num > '5'))
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow || (hasNegativeSign && answer != 0))
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
            result = (uint)answer;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses uint limited to styles that make up NumberStyles.HexNumber.</summary>
        private static bool TryParseUInt32HexNumberStyle(
            ReadOnlySpan<char> value, NumberStyles styles, out uint result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.HexNumber) == 0, "Only handles subsets of HexNumber format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            int index = 0;
            int num = value[0];
            int numValue = 0;

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            int answer = 0;
            int[] charToHexLookup = s_charToHexLookup;

            if ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEnd;
                        num = value[index];
                    } while (num == '0');
                    if ((uint)num >= (uint)charToHexLookup.Length || charToHexLookup[num] == 0xFF)
                        goto HasTrailingChars;
                }

                // Parse up through 8 digits, as no overflow is possible
                answer = charToHexLookup[num]; // first digit
                index++;
                for (int i = 0; i < 7; i++) // next 7 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEnd;
                    num = value[index];
                    if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF)
                        goto HasTrailingChars;
                    index++;
                    answer = 16 * answer + numValue;
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEnd;
                num = value[index];
                if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF)
                    goto HasTrailingChars;
                index++;
                overflow = true;
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                num = value[index];
                while ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
        DoneAtEnd:
            result = (uint)answer;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        internal static unsafe bool TryParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result, out bool failureIsOverflow)
        {
            result = 0;
            failureIsOverflow = false;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                return TryParseUInt64IntegerStyle(value, styles, info, out result, ref failureIsOverflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseUInt64HexNumberStyle(value, styles, out result, ref failureIsOverflow);
            }

            byte* pDigits = stackalloc byte[UInt64NumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, pDigits, UInt64NumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return false;
            }

            if (!TryNumberToUInt64(ref number, ref result))
            {
                failureIsOverflow = true;
                return false;
            }

            return true;
        }

        /// <summary>Parses ulong limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseUInt64IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            bool hasNegativeSign = false;
            int index = 0;
            int num = value[0];

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            // Parse leading sign.
            if ((styles & NumberStyles.AllowLeadingSign) != 0)
            {
                string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;

                if (positiveSign == "+" && negativeSign == "-")
                {
                    if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '-')
                    {
                        hasNegativeSign = true;
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;
                    if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        hasNegativeSign = true;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
            }

            long answer = 0;

            if (IsDigit(num))
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                }

                // Parse most digits, up to the potential for overflow, which can't happen until after 19 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 18; i++) // next 18 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 20th digit.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num))
                    goto HasTrailingChars;
                index++;
                if ((ulong)answer > ulong.MaxValue / 10 || ((ulong)answer == ulong.MaxValue / 10 && num > '5'))
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow || (hasNegativeSign && answer != 0))
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
            result = (ulong)answer;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses ulong limited to styles that make up NumberStyles.HexNumber.</summary>
        private static bool TryParseUInt64HexNumberStyle(
            ReadOnlySpan<char> value, NumberStyles styles, out ulong result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.HexNumber) == 0, "Only handles subsets of HexNumber format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1)
                goto FalseExit;

            bool overflow = false;
            int index = 0;
            int num = value[0];
            int numValue = 0;

            // Skip past any whitespace at the beginning.  
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                while (IsWhite(num));
            }

            long answer = 0;
            int[] charToHexLookup = s_charToHexLookup;

            if ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
            {
                // Skip past leading zeros.
                if (num == '0')
                {
                    do
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length)
                            goto DoneAtEnd;
                        num = value[index];
                    } while (num == '0');
                    if ((uint)num >= (uint)charToHexLookup.Length || charToHexLookup[num] == 0xFF)
                        goto HasTrailingChars;
                }

                // Parse up through 16 digits, as no overflow is possible
                answer = charToHexLookup[num]; // first digit
                index++;
                for (int i = 0; i < 15; i++) // next 15 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEnd;
                    num = value[index];
                    if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF)
                        goto HasTrailingChars;
                    index++;
                    answer = 16 * answer + numValue;
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEnd;
                num = value[index];
                if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF)
                    goto HasTrailingChars;
                index++;
                overflow = true;
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                num = value[index];
                while ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                }
                goto HasTrailingChars;
            }

        FalseExit: // parsing failed
            result = 0;
            return false;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                failureIsOverflow = true;
                goto FalseExit;
            }
        DoneAtEnd:
            result = (ulong)answer;
            return true;

        HasTrailingChars: // we've successfully parsed, but there are still remaining characters in the span
            // Skip past trailing whitespace, then past trailing zeros, and if anything else remains, fail.
            if (IsWhite(num))
            {
                if ((styles & NumberStyles.AllowTrailingWhite) == 0)
                    goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index]))
                        break;
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
                goto FalseExit;

            goto DoneAtEndButPotentialOverflow;
        }

        internal static decimal ParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseDecimal(value, styles, info, out decimal result, out bool failureIsOverflow))
            {
                ThrowOverflowOrFormatException(failureIsOverflow, nameof(SR.Overflow_Decimal));
            }

            return result;
        }

        internal static unsafe bool TryNumberToDecimal(ref NumberBuffer number, ref decimal value)
        {
            number.CheckConsistency();

            byte* p = number.GetDigitsPointer();
            int e = number.Scale;
            bool sign = number.IsNegative;
            uint c = *p;
            if (c == 0)
            {
                // To avoid risking an app-compat issue with pre 4.5 (where some app was illegally using Reflection to examine the internal scale bits), we'll only force
                // the scale to 0 if the scale was previously positive (previously, such cases were unparsable to a bug.)
                value = new decimal(0, 0, 0, sign, (byte)Math.Clamp(-e, 0, 28));
                return true;
            }

            if (e > DecimalPrecision)
                return false;

            ulong low64 = 0;
            while (e > -28)
            {
                e--;
                low64 *= 10;
                low64 += c - '0';
                c = *++p;
                if (low64 >= ulong.MaxValue / 10)
                    break;
                if (c == 0)
                {
                    while (e > 0)
                    {
                        e--;
                        low64 *= 10;
                        if (low64 >= ulong.MaxValue / 10)
                            break;
                    }
                    break;
                }
            }

            uint high = 0;
            while ((e > 0 || (c != 0 && e > -28)) &&
              (high < uint.MaxValue / 10 || (high == uint.MaxValue / 10 && (low64 < 0x99999999_99999999 || (low64 == 0x99999999_99999999 && c <= '5')))))
            {
                // multiply by 10
                ulong tmpLow = (uint)low64 * 10UL;
                ulong tmp64 = (uint)(low64 >> 32) * 10UL + (tmpLow >> 32);
                low64 = (uint)tmpLow + (tmp64 << 32);
                high = (uint)(tmp64 >> 32) + high * 10;

                if (c != 0)
                {
                    c -= '0';
                    low64 += c;
                    if (low64 < c)
                        high++;
                    c = *++p;
                }
                e--;
            }

            if (c >= '5')
            {
                if ((c == '5') && ((low64 & 1) == 0))
                {
                    c = *++p;

                    // At this point we should either be at the end of the buffer, or just
                    // have a single rounding digit left, and the next should be the end
                    Debug.Assert((c == 0) || (p[1] == 0));

                    if (((c == 0) || c == '0') && !number.HasNonZeroTail)
                    {
                        // When the next digit is 5, the number is even, and all following digits are zero
                        // we don't need to round.
                        goto NoRounding;
                    }
                }

                if (++low64 == 0 && ++high == 0)
                {
                    low64 = 0x99999999_9999999A;
                    high = uint.MaxValue / 10;
                    e++;
                }
            }
        NoRounding:

            if (e > 0)
                return false;

            if (e <= -DecimalPrecision)
            {
                // Parsing a large scale zero can give you more precision than fits in the decimal.
                // This should only happen for actual zeros or very small numbers that round to zero.
                value = new decimal(0, 0, 0, sign, DecimalPrecision - 1);
            }
            else
            {
                value = new decimal((int)low64, (int)(low64 >> 32), (int)high, sign, (byte)-e);
            }
            return true;
        }

        internal static double ParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseDouble(value, styles, info, out double result))
            {
                ThrowOverflowOrFormatException(overflow: false, overflowResourceKey: null);
            }

            return result;
        }

        internal static float ParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseSingle(value, styles, info, out float result))
            {
                ThrowOverflowOrFormatException(overflow: false, overflowResourceKey: null);
            }

            return result;
        }

        internal static unsafe bool TryParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out decimal result, out bool failureIsOverflow)
        {
            byte* pDigits = stackalloc byte[DecimalNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, pDigits, DecimalNumberBufferLength);

            result = 0;
            failureIsOverflow = false;

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return false;
            }

            if (!TryNumberToDecimal(ref number, ref result))
            {
                failureIsOverflow = true;
                return false;
            }

            return true;
        }

        internal static unsafe bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out double result)
        {
            byte* pDigits = stackalloc byte[DoubleNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, DoubleNumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                ReadOnlySpan<char> valueTrim = value.Trim();

                // This code would be simpler if we only had the concept of `InfinitySymbol`, but
                // we don't so we'll check the existing cases first and then handle `PositiveSign` +
                // `PositiveInfinitySymbol` and `PositiveSign/NegativeSign` + `NaNSymbol` last.

                if (valueTrim.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
                {
                    result = double.PositiveInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
                {
                    result = double.NegativeInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = double.NaN;
                }
                else if (valueTrim.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
                {
                    valueTrim = valueTrim.Slice(info.PositiveSign.Length);

                    if (valueTrim.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
                    {
                        result = double.PositiveInfinity;
                    }
                    else if (valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                    {
                        result = double.NaN;
                    }
                    else
                    {
                        result = 0;
                        return false;
                    }
                }
                else if (valueTrim.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                        valueTrim.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = double.NaN;
                }
                else
                {
                    result = 0;
                    return false; // We really failed
                }
            }
            else
            {
                result = NumberToDouble(ref number);
            }

            return true;
        }

        internal static unsafe bool TryParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out float result)
        {
            byte* pDigits = stackalloc byte[SingleNumberBufferLength];
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, pDigits, SingleNumberBufferLength);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                ReadOnlySpan<char> valueTrim = value.Trim();

                // This code would be simpler if we only had the concept of `InfinitySymbol`, but
                // we don't so we'll check the existing cases first and then handle `PositiveSign` +
                // `PositiveInfinitySymbol` and `PositiveSign/NegativeSign` + `NaNSymbol` last.
                //
                // Additionally, since some cultures ("wo") actually define `PositiveInfinitySymbol`
                // to include `PositiveSign`, we need to check whether `PositiveInfinitySymbol` fits
                // that case so that we don't start parsing things like `++infini`.

                if (valueTrim.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
                {
                    result = float.PositiveInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
                {
                    result = float.NegativeInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = float.NaN;
                }
                else if (valueTrim.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
                {
                    valueTrim = valueTrim.Slice(info.PositiveSign.Length);

                    if (!info.PositiveInfinitySymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
                    {
                        result = float.PositiveInfinity;
                    }
                    else if (!info.NaNSymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                    {
                        result = float.NaN;
                    }
                    else
                    {
                        result = 0;
                        return false;
                    }
                }
                else if (valueTrim.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         valueTrim.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = float.NaN;
                }
                else
                {
                    result = 0;
                    return false; // We really failed
                }
            }
            else
            {
                result = NumberToSingle(ref number);
            }

            return true;
        }

        private static unsafe void StringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
        {
            if (!TryStringToNumber(value, styles, ref number, info))
            {
                ThrowOverflowOrFormatException(overflow: false, overflowResourceKey: null);
            }
        }

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
        {
            Debug.Assert(info != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(value))
            {
                char* p = stringPointer;
                if (!TryParseNumber(ref p, p + value.Length, styles, ref number, info)
                    || (p - stringPointer < value.Length && !TrailingZeros(value, (int)(p - stringPointer))))
                {
                    number.CheckConsistency();
                    return false;
                }
            }

            number.CheckConsistency();
            return true;
        }

        private static bool TrailingZeros(ReadOnlySpan<char> value, int index)
        {
            // For compatibility, we need to allow trailing zeros at the end of a number string
            for (int i = index; i < value.Length; i++)
            {
                if (value[i] != '\0')
                {
                    return false;
                }
            }

            return true;
        }

        private static unsafe char* MatchChars(char* p, char* pEnd, string value)
        {
            Debug.Assert(p != null && pEnd != null && p <= pEnd && value != null);
            fixed (char* stringPointer = value)
            {
                char* str = stringPointer;
                if (*str != '\0')
                {
                    // We only hurt the failure case
                    // This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 as a
                    // space character we use 0x20 space character instead to mean the same.
                    while (true)
                    {
                        char cp = p < pEnd ? *p : '\0';
                        if (cp != *str && !(*str == '\u00a0' && cp == '\u0020'))
                        {
                            break;
                        }
                        p++;
                        str++;
                        if (*str == '\0')
                            return p;
                    }
                }
            }

            return null;
        }

        private static bool IsWhite(int ch) => ch == 0x20 || ((uint)(ch - 0x09) <= (0x0D - 0x09));

        private static bool IsDigit(int ch) => ((uint)ch - '0') <= 9;

        private static void ThrowOverflowOrFormatException(bool overflow, string overflowResourceKey)
        {
            throw overflow ?
               new OverflowException(SR.GetResourceString(overflowResourceKey)) :
               (Exception)new FormatException(SR.Format_InvalidString);
        }

        internal static double NumberToDouble(ref NumberBuffer number)
        {
            number.CheckConsistency();

            ulong bits = NumberToFloatingPointBits(ref number, in FloatingPointInfo.Double);
            double result = BitConverter.Int64BitsToDouble((long)(bits));
            return number.IsNegative ? -result : result;
        }

        internal static float NumberToSingle(ref NumberBuffer number)
        {
            number.CheckConsistency();

            uint bits = (uint)(NumberToFloatingPointBits(ref number, in FloatingPointInfo.Single));
            float result = BitConverter.Int32BitsToSingle((int)(bits));
            return number.IsNegative ? -result : result;
        }
    }
}

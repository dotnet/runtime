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
        private static readonly int[] s_charToHexLookup =
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
                    n += (*p++ - '0');
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
                    n += (*p++ - '0');
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

        internal static int ParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                if (!TryParseInt32IntegerStyle(value, styles, info, out int intResult, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_Int32));
                }
                return intResult;
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                if (!TryParseUInt32HexNumberStyle(value, styles, info, out uint hexResult, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_Int32));
                }
                return (int)hexResult;
            }

            NumberBuffer number = default;
            int result = 0;
            StringToNumber(value, styles, ref number, info, false);
            if (!NumberToInt32(ref number, ref result))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Int32));
            }
            return result;
        }

        internal static long ParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                if (!TryParseInt64IntegerStyle(value, styles, info, out long intResult, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_Int64));
                }
                return intResult;
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                if (!TryParseUInt64HexNumberStyle(value, styles, info, out ulong hexResult, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_Int64));
                }
                return (long)hexResult;
            }

            NumberBuffer number = default;
            long result = 0;
            StringToNumber(value, styles, ref number, info, false);
            if (!NumberToInt64(ref number, ref result))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Int64));
            }
            return result;
        }

        internal static uint ParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            uint result = 0;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                if (!TryParseUInt32IntegerStyle(value, styles, info, out result, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_UInt32));
                }
                return result;
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                if (!TryParseUInt32HexNumberStyle(value, styles, info, out result, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_UInt32));
                }
                return result;
            }

            NumberBuffer number = default;
            StringToNumber(value, styles, ref number, info, false);
            if (!NumberToUInt32(ref number, ref result))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_UInt32));
            }
            return result;
        }

        internal static ulong ParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            ulong result = 0;

            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                if (!TryParseUInt64IntegerStyle(value, styles, info, out result, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_UInt64));
                }
                return result;
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                if (!TryParseUInt64HexNumberStyle(value, styles, info, out result, ref overflow))
                {
                    ThrowOverflowOrFormatException(overflow, nameof(SR.Overflow_UInt64));
                }
                return result;
            }

            NumberBuffer number = default;
            StringToNumber(value, styles, ref number, info, false);
            if (!NumberToUInt64(ref number, ref result))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_UInt64));
            }
            return result;
        }

        private static unsafe bool ParseNumber(ref char* str, char* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info, bool parseDecimal)
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

            number.scale = 0;
            number.sign = false;
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
                    if ((((styles & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || ((next = MatchChars(p, strEnd, info.NegativeSign)) != null && (number.sign = true))))
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (ch == '(' && ((styles & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.sign = true;
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
            while (true)
            {
                if (IsDigit(ch))
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
            number.precision = digEnd;
            number.digits[digEnd] = '\0';
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
                        number.scale += exp;
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
                        if (((styles & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0)) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || (((next = MatchChars(p, strEnd, info.NegativeSign)) != null) && (number.sign = true))))
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

        internal static bool TryParseInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                return TryParseInt32IntegerStyle(value, styles, info, out result, ref overflow);
            }

            result = 0;

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                return TryParseUInt32HexNumberStyle(value, styles, info, out Unsafe.As<int, uint>(ref result), ref overflow);
            }

            NumberBuffer number = default;
            return
                TryStringToNumber(value, styles, ref number, info, false) &&
                NumberToInt32(ref number, ref result);
        }

        /// <summary>Parses int limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseInt32IntegerStyle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out int result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        sign = -1;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num)) goto HasTrailingChars;
                }
 
                // Parse most digits, up to the potential for overflow, which can't happen until after 9 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 8; i++) // next 8 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num)) goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 10th digit.
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num)) goto HasTrailingChars;
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
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses long inputs limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseInt64IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '+')
                    {
                        index++;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        sign = -1;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num)) goto HasTrailingChars;
                }
 
                // Parse most digits, up to the potential for overflow, which can't happen until after 18 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 17; i++) // next 17 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num)) goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 19th digit.
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num)) goto HasTrailingChars;
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
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }

        internal static bool TryParseInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out long result)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                return TryParseInt64IntegerStyle(value, styles, info, out result, ref overflow);
            }

            result = 0;

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                return TryParseUInt64HexNumberStyle(value, styles, info, out Unsafe.As<long, ulong>(ref result), ref overflow);
            }

            NumberBuffer number = default;
            return
                TryStringToNumber(value, styles, ref number, info, false) &&
                NumberToInt64(ref number, ref result);
        }

        internal static bool TryParseUInt32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                return TryParseUInt32IntegerStyle(value, styles, info, out result, ref overflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                return TryParseUInt32HexNumberStyle(value, styles, info, out result, ref overflow);
            }

            NumberBuffer number = default;
            result = 0;
            return
                TryStringToNumber(value, styles, ref number, info, false) &&
                NumberToUInt32(ref number, ref result);
        }

        /// <summary>Parses uint limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseUInt32IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '-')
                    {
                        hasNegativeSign = true;
                        index++;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        hasNegativeSign = true;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num)) goto HasTrailingChars;
                }
 
                // Parse most digits, up to the potential for overflow, which can't happen until after 9 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 8; i++) // next 8 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num)) goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 10th digit.
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num)) goto HasTrailingChars;
                index++;
                if ((uint)answer > uint.MaxValue / 10 || ((uint)answer == uint.MaxValue / 10 && num > '5'))
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses uint limited to styles that make up NumberStyles.HexNumber.</summary>
        private static bool TryParseUInt32HexNumberStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out uint result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.HexNumber) == 0, "Only handles subsets of HexNumber format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                        num = value[index];
                    } while (num == '0');
                    if ((uint)num >= (uint)charToHexLookup.Length || charToHexLookup[num] == 0xFF) goto HasTrailingChars;
                }
 
                // Parse up through 8 digits, as no overflow is possible
                answer = charToHexLookup[num]; // first digit
                index++;
                for (int i = 0; i < 7; i++) // next 7 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                    num = value[index];
                    if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF) goto HasTrailingChars;
                    index++;
                    answer = 16 * answer + numValue;
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                num = value[index];
                if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF) goto HasTrailingChars;
                index++;
                overflow = true;
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                num = value[index];
                while ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
                {
                    index++;
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }
        
        internal static bool TryParseUInt64(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result)
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                bool overflow = false;
                return TryParseUInt64IntegerStyle(value, styles, info, out result, ref overflow);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                bool overflow = false;
                return TryParseUInt64HexNumberStyle(value, styles, info, out result, ref overflow);
            }

            NumberBuffer number = default;
            result = 0;
            return
                TryStringToNumber(value, styles, ref number, info, false) &&
                NumberToUInt64(ref number, ref result);
        }

        /// <summary>Parses ulong limited to styles that make up NumberStyles.Integer.</summary>
        private static bool TryParseUInt64IntegerStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (num == '-')
                    {
                        hasNegativeSign = true;
                        index++;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        hasNegativeSign = true;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                        num = value[index];
                    } while (num == '0');
                    if (!IsDigit(num)) goto HasTrailingChars;
                }
 
                // Parse most digits, up to the potential for overflow, which can't happen until after 19 digits.
                answer = num - '0'; // first digit
                index++;
                for (int i = 0; i < 18; i++) // next 18 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                    num = value[index];
                    if (!IsDigit(num)) goto HasTrailingChars;
                    index++;
                    answer = 10 * answer + num - '0';
                }

                // Potential overflow now processing the 20th digit.
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
                num = value[index];
                if (!IsDigit(num)) goto HasTrailingChars;
                index++;
                if ((ulong)answer > ulong.MaxValue / 10 || ((ulong)answer == ulong.MaxValue / 10 && num > '5'))
                {
                    overflow = true;
                }
                answer = answer * 10 + num - '0';
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = value[index];
                while (IsDigit(num))
                {
                    overflow = true;
                    index++;
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses ulong limited to styles that make up NumberStyles.HexNumber.</summary>
        private static bool TryParseUInt64HexNumberStyle(
            ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out ulong result, ref bool failureIsOverflow)
        {
            Debug.Assert((styles & ~NumberStyles.HexNumber) == 0, "Only handles subsets of HexNumber format");
            Debug.Assert(!failureIsOverflow, $"failureIsOverflow should have been initialized to false");

            if ((uint)value.Length < 1) goto FalseExit;

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
                    if ((uint)index >= (uint)value.Length) goto FalseExit;
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
                        if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                        num = value[index];
                    } while (num == '0');
                    if ((uint)num >= (uint)charToHexLookup.Length || charToHexLookup[num] == 0xFF) goto HasTrailingChars;
                }
 
                // Parse up through 16 digits, as no overflow is possible
                answer = charToHexLookup[num]; // first digit
                index++;
                for (int i = 0; i < 15; i++) // next 15 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                    num = value[index];
                    if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF) goto HasTrailingChars;
                    index++;
                    answer = 16 * answer + numValue;
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length) goto DoneAtEnd;
                num = value[index];
                if ((uint)num >= (uint)charToHexLookup.Length || (numValue = charToHexLookup[num]) == 0xFF) goto HasTrailingChars;
                index++;
                overflow = true;
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                num = value[index];
                while ((uint)num < (uint)charToHexLookup.Length && charToHexLookup[num] != 0xFF)
                {
                    index++;
                    if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
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
                if ((styles & NumberStyles.AllowTrailingWhite) == 0) goto FalseExit;
                for (index++; index < value.Length; index++)
                {
                    if (!IsWhite(value[index])) break;
                }
                if ((uint)index >= (uint)value.Length) goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index)) goto FalseExit;
            
            goto DoneAtEndButPotentialOverflow;
        }

        internal static decimal ParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            NumberBuffer number = default;
            decimal result = 0;

            StringToNumber(value, styles, ref number, info, true);

            if (!NumberBufferToDecimal(ref number, ref result))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Decimal));
            }
            return result;
        }

        private static unsafe bool NumberBufferToDecimal(ref NumberBuffer number, ref decimal value)
        {
            char* p = number.digits;
            int e = number.scale;
            bool sign = number.sign;
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
                // If the next digit is 5, round up if the number is odd or any following digit is non-zero
                if (c == '5' && (low64 & 1) == 0)
                {
                    c = *++p;
                    int count = 20; // Look at the next 20 digits to check to round
                    while (c == '0' && count != 0)
                    {
                        c = *++p;
                        count--;
                    }
                    if (c == 0 || count == 0)
                        goto NoRounding;// Do nothing
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
            NumberBuffer number = default;
            double d = 0;

            if (!TryStringToNumber(value, styles, ref number, info, false))
            {
                //If we failed TryStringToNumber, it may be from one of our special strings.
                //Check the three with which we're concerned and rethrow if it's not one of
                //those strings.
                ReadOnlySpan<char> sTrim = value.Trim();
                if (sTrim.EqualsOrdinal(info.PositiveInfinitySymbol))
                {
                    return double.PositiveInfinity;
                }
                if (sTrim.EqualsOrdinal(info.NegativeInfinitySymbol))
                {
                    return double.NegativeInfinity;
                }
                if (sTrim.EqualsOrdinal(info.NaNSymbol))
                {
                    return double.NaN;
                }
                ThrowOverflowOrFormatException(overflow: false, null);
            }

            if (!NumberBufferToDouble(ref number, ref d))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Double));
            }

            return d;
        }

        internal static float ParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            NumberBuffer number = default;
            double d = 0;

            if (!TryStringToNumber(value, styles, ref number, info, false))
            {
                //If we failed TryStringToNumber, it may be from one of our special strings.
                //Check the three with which we're concerned and rethrow if it's not one of
                //those strings.
                ReadOnlySpan<char> sTrim = value.Trim();
                if (sTrim.EqualsOrdinal(info.PositiveInfinitySymbol))
                {
                    return float.PositiveInfinity;
                }
                if (sTrim.EqualsOrdinal(info.NegativeInfinitySymbol))
                {
                    return float.NegativeInfinity;
                }
                if (sTrim.EqualsOrdinal(info.NaNSymbol))
                {
                    return float.NaN;
                }
                ThrowOverflowOrFormatException(overflow: false, null);
            }

            if (!NumberBufferToDouble(ref number, ref d))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Single));
            }
            float castSingle = (float)d;
            if (float.IsInfinity(castSingle))
            {
                ThrowOverflowOrFormatException(overflow: true, nameof(SR.Overflow_Single));
            }
            return castSingle;
        }

        internal static bool TryParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out decimal result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(value, styles, ref number, info, true))
            {
                return false;
            }

            if (!NumberBufferToDecimal(ref number, ref result))
            {
                return false;
            }
            return true;
        }

        internal static bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out double result)
        {
            NumberBuffer number = default;
            result = 0;

            if (!TryStringToNumber(value, styles, ref number, info, false))
            {
                return false;
            }
            if (!NumberBufferToDouble(ref number, ref result))
            {
                return false;
            }
            return true;
        }

        internal static bool TryParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out float result)
        {
            NumberBuffer number = default;
            result = 0;
            double d = 0;

            if (!TryStringToNumber(value, styles, ref number, info, false))
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

        private static unsafe void StringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info, bool parseDecimal)
        {
            Debug.Assert(info != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(value))
            {
                char* p = stringPointer;
                if (!ParseNumber(ref p, p + value.Length, styles, ref number, info, parseDecimal)
                    || (p - stringPointer < value.Length && !TrailingZeros(value, (int)(p - stringPointer))))
                {
                    ThrowOverflowOrFormatException(overflow: false, null);
                }
            }
        }

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info, bool parseDecimal)
        {
            Debug.Assert(info != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(value))
            {
                char* p = stringPointer;
                if (!ParseNumber(ref p, p + value.Length, styles, ref number, info, parseDecimal)
                    || (p - stringPointer < value.Length && !TrailingZeros(value, (int)(p - stringPointer))))
                {
                    return false;
                }
            }

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
                        if (*str == '\0') return p;
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

        private static bool NumberBufferToDouble(ref NumberBuffer number, ref double value)
        {
            double d = NumberToDouble(ref number);
            if (!double.IsFinite(d))
            {
                value = default;
                return false;
            }

            if (d == 0.0)
            {
                // normalize -0.0 to 0.0
                d = 0.0;
            }

            value = d;
            return true;
        }
    }
}

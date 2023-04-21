// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
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

    internal interface IBinaryIntegerParseAndFormatInfo<TSelf> : IBinaryInteger<TSelf>, IMinMaxValue<TSelf>
        where TSelf : unmanaged, IBinaryIntegerParseAndFormatInfo<TSelf>
    {
        static abstract bool IsSigned { get; }

        static abstract int MaxDigitCount { get; }

        static abstract int MaxHexDigitCount { get; }

        static abstract TSelf MaxValueDiv10 { get; }

        static abstract string OverflowMessage { get; }

        static abstract bool IsGreaterThanAsUnsigned(TSelf left, TSelf right);

        static abstract TSelf MultiplyBy10(TSelf value);

        static abstract TSelf MultiplyBy16(TSelf value);
    }

    internal static partial class Number
    {
        private const int Int32Precision = 10;
        private const int UInt32Precision = Int32Precision;
        private const int Int64Precision = 19;
        private const int UInt64Precision = 20;
        private const int Int128Precision = 39;
        private const int UInt128Precision = 39;

        private const int DoubleMaxExponent = 309;
        private const int DoubleMinExponent = -324;

        private const int FloatingPointMaxExponent = DoubleMaxExponent;
        private const int FloatingPointMinExponent = DoubleMinExponent;

        private const int SingleMaxExponent = 39;
        private const int SingleMinExponent = -45;

        private const int HalfMaxExponent = 5;
        private const int HalfMinExponent = -8;

        private static unsafe bool TryNumberBufferToBinaryInteger<TInteger>(ref NumberBuffer number, ref TInteger value)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            number.CheckConsistency();

            int i = number.Scale;

            if ((i > TInteger.MaxDigitCount) || (i < number.DigitsCount) || (!TInteger.IsSigned && number.IsNegative))
            {
                return false;
            }

            byte* p = number.GetDigitsPointer();

            Debug.Assert(p != null);
            TInteger n = TInteger.Zero;

            while (--i >= 0)
            {
                if (TInteger.IsGreaterThanAsUnsigned(n, TInteger.MaxValueDiv10))
                {
                    return false;
                }

                n = TInteger.MultiplyBy10(n);

                if (*p != '\0')
                {
                    TInteger newN = n + TInteger.CreateTruncating(*p++ - '0');

                    if (!TInteger.IsSigned && (newN < n))
                    {
                        return false;
                    }

                    n = newN;
                }
            }

            if (TInteger.IsSigned)
            {
                if (number.IsNegative)
                {
                    n = -n;

                    if (n > TInteger.Zero)
                    {
                        return false;
                    }
                }
                else if (n < TInteger.Zero)
                {
                    return false;
                }
            }

            value = n;
            return true;
        }

        internal static TInteger ParseBinaryInteger<TInteger>(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            ParsingStatus status = TryParseBinaryInteger(value, styles, info, out TInteger result);

            if (status != ParsingStatus.OK)
            {
                ThrowOverflowOrFormatException<TInteger>(status, value);
            }
            return result;
        }

        private static unsafe bool TryParseNumber(scoped ref char* str, char* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
        {
            Debug.Assert(str != null);
            Debug.Assert(strEnd != null);
            Debug.Assert(str <= strEnd);
            Debug.Assert((styles & (NumberStyles.AllowHexSpecifier | NumberStyles.AllowBinarySpecifier)) == 0);

            const int StateSign = 0x0001;
            const int StateParens = 0x0002;
            const int StateDigits = 0x0004;
            const int StateNonZero = 0x0008;
            const int StateDecimal = 0x0010;
            const int StateCurrency = 0x0020;

            Debug.Assert(number.DigitsCount == 0);
            Debug.Assert(number.Scale == 0);
            Debug.Assert(!number.IsNegative);
            Debug.Assert(!number.HasNonZeroTail);

            number.CheckConsistency();

            string decSep;                  // decimal separator from NumberFormatInfo.
            string groupSep;                // group separator from NumberFormatInfo.
            string? currSymbol = null;       // currency symbol from NumberFormatInfo.

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
                    if ((((styles & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || ((next = MatchNegativeSignChars(p, strEnd, info)) != null && (number.IsNegative = true))))
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
            int numberOfTrailingZeros = 0;

            while (true)
            {
                if (IsDigit(ch))
                {
                    state |= StateDigits;

                    if (ch != '0' || (state & StateNonZero) != 0)
                    {
                        if (digCount < maxDigCount)
                        {
                            number.Digits[digCount] = (byte)(ch);
                            if ((ch != '0') || (number.Kind != NumberBufferKind.Integer))
                            {
                                digEnd = digCount + 1;
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

                        if (digCount < maxDigCount)
                        {
                            // Handle a case like "53.0". We need to ignore trailing zeros in the fractional part for floating point numbers, so we keep a count of the number of trailing zeros and update digCount later
                            if (ch == '0')
                            {
                                numberOfTrailingZeros++;
                            }
                            else
                            {
                                numberOfTrailingZeros = 0;
                            }
                        }
                        digCount++;
                        state |= StateNonZero;
                    }
                    else if ((state & StateDecimal) != 0)
                    {
                        number.Scale--;
                    }
                }
                else if (((styles & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, decSep)) != null || (parsingCurrency && (state & StateCurrency) == 0) && (next = MatchChars(p, strEnd, info.NumberDecimalSeparator)) != null))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((styles & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, groupSep)) != null || (parsingCurrency && (state & StateCurrency) == 0) && (next = MatchChars(p, strEnd, info.NumberGroupSeparator)) != null))
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
                    if ((next = MatchChars(p, strEnd, info._positiveSign)) != null)
                    {
                        ch = (p = next) < strEnd ? *p : '\0';
                    }
                    else if ((next = MatchNegativeSignChars(p, strEnd, info)) != null)
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

                if (number.Kind == NumberBufferKind.FloatingPoint && !number.HasNonZeroTail)
                {
                    // Adjust the number buffer for trailing zeros
                    int numberOfFractionalDigits = digEnd - number.Scale;
                    if (numberOfFractionalDigits > 0)
                    {
                        numberOfTrailingZeros = Math.Min(numberOfTrailingZeros, numberOfFractionalDigits);
                        Debug.Assert(numberOfTrailingZeros >= 0);
                        number.DigitsCount = digEnd - numberOfTrailingZeros;
                        number.Digits[number.DigitsCount] = (byte)('\0');
                    }
                }

                while (true)
                {
                    if (!IsWhite(ch) || (styles & NumberStyles.AllowTrailingWhite) == 0)
                    {
                        if ((styles & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0) && ((next = MatchChars(p, strEnd, info.PositiveSign)) != null || (((next = MatchNegativeSignChars(p, strEnd, info)) != null) && (number.IsNegative = true))))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ParsingStatus TryParseBinaryInteger<TInteger>(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            if ((styles & ~NumberStyles.Integer) == 0)
            {
                // Optimized path for the common case of anything that's allowed for integer style.
                return TryParseBinaryIntegerStyle(value, styles, info, out result);
            }

            if ((styles & NumberStyles.AllowHexSpecifier) != 0)
            {
                return TryParseBinaryIntegerHexNumberStyle(value, styles, out result);
            }

            if ((styles & NumberStyles.AllowBinarySpecifier) != 0)
            {
                return TryParseBinaryIntegerHexOrBinaryNumberStyle<TInteger, BinaryParser<TInteger>>(value, styles, out result);
            }

            return TryParseBinaryIntegerNumber(value, styles, info, out result);
        }

        private static unsafe ParsingStatus TryParseBinaryIntegerNumber<TInteger>(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            result = TInteger.Zero;
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Integer, stackalloc byte[TInteger.MaxDigitCount + 1]);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return ParsingStatus.Failed;
            }

            if (!TryNumberBufferToBinaryInteger(ref number, ref result))
            {
                return ParsingStatus.Overflow;
            }

            return ParsingStatus.OK;
        }

        /// <summary>Parses int limited to styles that make up NumberStyles.Integer.</summary>
        internal static ParsingStatus TryParseBinaryIntegerStyle<TInteger>(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");

            if (value.IsEmpty)
                goto FalseExit;

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
            bool isNegative = false;
            if ((styles & NumberStyles.AllowLeadingSign) != 0)
            {
                if (info.HasInvariantNumberSigns)
                {
                    if (num == '-')
                    {
                        isNegative = true;
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
                else if (info.AllowHyphenDuringParsing && num == '-')
                {
                    isNegative = true;
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto FalseExit;
                    num = value[index];
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;
                    string positiveSign = info.PositiveSign, negativeSign = info.NegativeSign;
                    if (!string.IsNullOrEmpty(positiveSign) && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                    else if (!string.IsNullOrEmpty(negativeSign) && value.StartsWith(negativeSign))
                    {
                        isNegative = true;
                        index += negativeSign.Length;
                        if ((uint)index >= (uint)value.Length)
                            goto FalseExit;
                        num = value[index];
                    }
                }
            }

            bool overflow = !TInteger.IsSigned && isNegative;
            TInteger answer = TInteger.Zero;

            if (IsDigit(num))
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

                    if (!IsDigit(num))
                    {
                        if (!TInteger.IsSigned)
                        {
                            overflow = false;
                        }
                        goto HasTrailingChars;
                    }
                }

                // Parse most digits, up to the potential for overflow, which can't happen until after MaxDigitCount - 1 digits.
                answer = TInteger.CreateTruncating(num - '0'); // first digit
                index++;
                for (int i = 0; i < TInteger.MaxDigitCount - 2; i++) // next MaxDigitCount - 2 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                    {
                        if (!TInteger.IsSigned)
                            goto DoneAtEndButPotentialOverflow;
                        else
                            goto DoneAtEnd;
                    }
                    num = value[index];
                    if (!IsDigit(num))
                        goto HasTrailingChars;
                    index++;
                    answer = TInteger.MultiplyBy10(answer);
                    answer += TInteger.CreateTruncating(num - '0');
                }

                if ((uint)index >= (uint)value.Length)
                {
                    if (!TInteger.IsSigned)
                        goto DoneAtEndButPotentialOverflow;
                    else
                        goto DoneAtEnd;
                }
                num = value[index];
                if (!IsDigit(num))
                    goto HasTrailingChars;
                index++;
                // Potential overflow now processing the MaxDigitCount digit.
                if (!TInteger.IsSigned)
                {
                    overflow |= (answer > TInteger.MaxValueDiv10) || (answer == TInteger.MaxValueDiv10) && (num > '5');
                }
                else
                {
                    overflow = answer > TInteger.MaxValueDiv10;
                }
                answer = TInteger.MultiplyBy10(answer);
                answer += TInteger.CreateTruncating(num - '0');
                if (TInteger.IsSigned)
                {
                    overflow |= TInteger.IsGreaterThanAsUnsigned(answer, TInteger.MaxValue + (isNegative ? TInteger.One : TInteger.Zero));
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
                        goto OverflowExit;
                    num = value[index];
                }
                goto HasTrailingChars;
            }
            goto FalseExit;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                goto OverflowExit;
            }
        DoneAtEnd:
            if (!TInteger.IsSigned)
            {
                result = answer;
            }
            else
            {
                result = isNegative ? -answer : answer;
            }
            ParsingStatus status = ParsingStatus.OK;
        Exit:
            return status;

        FalseExit: // parsing failed
            result = TInteger.Zero;
            status = ParsingStatus.Failed;
            goto Exit;
        OverflowExit:
            result = TInteger.Zero;
            status = ParsingStatus.Overflow;
            goto Exit;

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

        /// <summary>Parses <typeparamref name="TInteger"/> limited to styles that make up NumberStyles.HexNumber.</summary>
        internal static ParsingStatus TryParseBinaryIntegerHexNumberStyle<TInteger>(ReadOnlySpan<char> value, NumberStyles styles, out TInteger result)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger> =>
            TryParseBinaryIntegerHexOrBinaryNumberStyle<TInteger, HexParser<TInteger>>(value, styles, out result);

        private interface IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            static abstract NumberStyles AllowedStyles { get; }
            static abstract bool IsValidChar(int ch);
            static abstract uint FromChar(int ch);
            static abstract uint MaxDigitValue { get; }
            static abstract int MaxDigitCount { get; }
            static abstract TInteger ShiftLeftForNextDigit(TInteger value);
        }

        private readonly struct HexParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            public static NumberStyles AllowedStyles => NumberStyles.HexNumber;
            public static bool IsValidChar(int ch) => HexConverter.IsHexChar(ch);
            public static uint FromChar(int ch) => (uint)HexConverter.FromChar(ch);
            public static uint MaxDigitValue => 0xF;
            public static int MaxDigitCount => TInteger.MaxHexDigitCount;
            public static TInteger ShiftLeftForNextDigit(TInteger value) => TInteger.MultiplyBy16(value);
        }

        private readonly struct BinaryParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            public static NumberStyles AllowedStyles => NumberStyles.BinaryNumber;
            public static bool IsValidChar(int ch) => (uint)(ch - '0') <= 1;
            public static uint FromChar(int ch) => (uint)(ch - '0');
            public static uint MaxDigitValue => 1;
            public static unsafe int MaxDigitCount => sizeof(TInteger) * 8;
            public static TInteger ShiftLeftForNextDigit(TInteger value) => value << 1;
        }

        private static ParsingStatus TryParseBinaryIntegerHexOrBinaryNumberStyle<TInteger, TParser>(ReadOnlySpan<char> value, NumberStyles styles, out TInteger result)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
            where TParser : struct, IHexOrBinaryParser<TInteger>
        {
            Debug.Assert((styles & ~TParser.AllowedStyles) == 0, $"Only handles subsets of {TParser.AllowedStyles} format");

            if (value.IsEmpty)
                goto FalseExit;

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

            bool overflow = false;
            TInteger answer = TInteger.Zero;

            if (TParser.IsValidChar(num))
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
                    if (!TParser.IsValidChar(num))
                        goto HasTrailingChars;
                }

                // Parse up through MaxDigitCount digits, as no overflow is possible
                answer = TInteger.CreateTruncating(TParser.FromChar(num)); // first digit
                index++;
                for (int i = 0; i < TParser.MaxDigitCount - 1; i++) // next MaxDigitCount - 1 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                        goto DoneAtEnd;
                    num = value[index];

                    uint numValue = TParser.FromChar(num);
                    if (numValue > TParser.MaxDigitValue)
                        goto HasTrailingChars;
                    index++;
                    answer = TParser.ShiftLeftForNextDigit(answer);
                    answer += TInteger.CreateTruncating(numValue);
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEnd;
                num = value[index];
                if (!TParser.IsValidChar(num))
                    goto HasTrailingChars;

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                do
                {
                    index++;
                    if ((uint)index >= (uint)value.Length)
                        goto OverflowExit;
                    num = value[index];
                } while (TParser.IsValidChar(num));
                overflow = true;
                goto HasTrailingChars;
            }
            goto FalseExit;

        DoneAtEndButPotentialOverflow:
            if (overflow)
            {
                goto OverflowExit;
            }
        DoneAtEnd:
            result = answer;
            ParsingStatus status = ParsingStatus.OK;
        Exit:
            return status;

        FalseExit: // parsing failed
            result = TInteger.Zero;
            status = ParsingStatus.Failed;
            goto Exit;
        OverflowExit:
            result = TInteger.Zero;
            status = ParsingStatus.Overflow;
            goto Exit;

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
            ParsingStatus status = TryParseDecimal(value, styles, info, out decimal result);
            if (status != ParsingStatus.OK)
            {
                ThrowOverflowOrFormatException(status, value, TypeCode.Decimal);
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

                    bool hasZeroTail = !number.HasNonZeroTail;

                    // We might still have some additional digits, in which case they need
                    // to be considered as part of hasZeroTail. Some examples of this are:
                    //  * 3.0500000000000000000001e-27
                    //  * 3.05000000000000000000001e-27
                    // In these cases, we will have processed 3 and 0, and ended on 5. The
                    // buffer, however, will still contain a number of trailing zeros and
                    // a trailing non-zero number.

                    while ((c != 0) && hasZeroTail)
                    {
                        hasZeroTail &= (c == '0');
                        c = *++p;
                    }

                    // We should either be at the end of the stream or have a non-zero tail
                    Debug.Assert((c == 0) || !hasZeroTail);

                    if (hasZeroTail)
                    {
                        // When the next digit is 5, the number is even, and all following
                        // digits are zero we don't need to round.
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
                ThrowOverflowOrFormatException(ParsingStatus.Failed, value);
            }

            return result;
        }

        internal static float ParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseSingle(value, styles, info, out float result))
            {
                ThrowOverflowOrFormatException(ParsingStatus.Failed, value);
            }

            return result;
        }

        internal static Half ParseHalf(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseHalf(value, styles, info, out Half result))
            {
                ThrowOverflowOrFormatException(ParsingStatus.Failed, value);
            }

            return result;
        }

        internal static unsafe ParsingStatus TryParseDecimal(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out decimal result)
        {
            NumberBuffer number = new NumberBuffer(NumberBufferKind.Decimal, stackalloc byte[DecimalNumberBufferLength]);

            result = 0;

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                return ParsingStatus.Failed;
            }

            if (!TryNumberToDecimal(ref number, ref result))
            {
                return ParsingStatus.Overflow;
            }

            return ParsingStatus.OK;
        }

        internal static bool SpanStartsWith(ReadOnlySpan<char> span, char c) => !span.IsEmpty && span[0] == c;

        internal static unsafe bool TryParseDouble(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out double result)
        {
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[DoubleNumberBufferLength]);

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
                else if ((valueTrim.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) && valueTrim.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol)) ||
                        (info.AllowHyphenDuringParsing && SpanStartsWith(valueTrim, '-') && valueTrim.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol)))
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

        internal static unsafe bool TryParseHalf(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out Half result)
        {
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[HalfNumberBufferLength]);

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
                    result = Half.PositiveInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NegativeInfinitySymbol))
                {
                    result = Half.NegativeInfinity;
                }
                else if (valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = Half.NaN;
                }
                else if (valueTrim.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
                {
                    valueTrim = valueTrim.Slice(info.PositiveSign.Length);

                    if (!info.PositiveInfinitySymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.EqualsOrdinalIgnoreCase(info.PositiveInfinitySymbol))
                    {
                        result = Half.PositiveInfinity;
                    }
                    else if (!info.NaNSymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.EqualsOrdinalIgnoreCase(info.NaNSymbol))
                    {
                        result = Half.NaN;
                    }
                    else
                    {
                        result = Half.Zero;
                        return false;
                    }
                }
                else if (valueTrim.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         valueTrim.Slice(info.NegativeSign.Length).EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = Half.NaN;
                }
                else if (info.AllowHyphenDuringParsing && SpanStartsWith(valueTrim, '-') && !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith('-') && valueTrim.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol))
                {
                    result = Half.NaN;
                }
                else
                {
                    result = Half.Zero;
                    return false; // We really failed
                }
            }
            else
            {
                result = NumberToHalf(ref number);
            }

            return true;
        }

        internal static unsafe bool TryParseSingle(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out float result)
        {
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[SingleNumberBufferLength]);

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
                else if (info.AllowHyphenDuringParsing && SpanStartsWith(valueTrim, '-') && !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith('-') && valueTrim.Slice(1).EqualsOrdinalIgnoreCase(info.NaNSymbol))
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

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
        {
            Debug.Assert(info != null);
            fixed (char* stringPointer = &MemoryMarshal.GetReference(value))
            {
                char* p = stringPointer;
                if (!TryParseNumber(ref p, p + value.Length, styles, ref number, info)
                    || ((int)(p - stringPointer) < value.Length && !TrailingZeros(value, (int)(p - stringPointer))))
                {
                    number.CheckConsistency();
                    return false;
                }
            }

            number.CheckConsistency();
            return true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // rare slow path that shouldn't impact perf of the main use case
        private static bool TrailingZeros(ReadOnlySpan<char> value, int index) =>
            // For compatibility, we need to allow trailing zeros at the end of a number string
            value.Slice(index).IndexOfAnyExcept('\0') < 0;

        private static bool IsSpaceReplacingChar(char c) => c == '\u00a0' || c == '\u202f';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe char* MatchNegativeSignChars(char* p, char* pEnd, NumberFormatInfo info)
        {
            char* ret = MatchChars(p, pEnd, info.NegativeSign);
            if (ret == null && info.AllowHyphenDuringParsing && p < pEnd && *p == '-')
            {
                ret = p + 1;
            }

            return ret;
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
                    // This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 or 0x202F as a
                    // space character we use 0x20 space character instead to mean the same.
                    while (true)
                    {
                        char cp = p < pEnd ? *p : '\0';
                        if (cp != *str && !(IsSpaceReplacingChar(*str) && cp == '\u0020'))
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

        private static bool IsWhite(int ch) => ch == 0x20 || (uint)(ch - 0x09) <= (0x0D - 0x09);

        private static bool IsDigit(int ch) => ((uint)ch - '0') <= 9;

        internal enum ParsingStatus
        {
            OK,
            Failed,
            Overflow
        }

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException(ParsingStatus status, ReadOnlySpan<char> value, TypeCode type = 0) => throw GetException(status, value, type);

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException<TInteger>(ParsingStatus status, ReadOnlySpan<char> value)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            throw GetException<TInteger>(status, value);
        }

        [DoesNotReturn]
        internal static void ThrowOverflowException(TypeCode type) => throw GetOverflowException(type);

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatExceptionInt128(ParsingStatus status) => throw GetExceptionInt128(status);

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatExceptionUInt128(ParsingStatus status) => throw GetExceptionUInt128(status);

        private static Exception GetException(ParsingStatus status, ReadOnlySpan<char> value, TypeCode type)
        {
            if (status == ParsingStatus.Failed)
                return new FormatException(SR.Format(SR.Format_InvalidStringWithValue, value.ToString()));

            return GetOverflowException(type);
        }

        private static Exception GetException<TInteger>(ParsingStatus status, ReadOnlySpan<char> value)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            if (status == ParsingStatus.Failed)
                return new FormatException(SR.Format(SR.Format_InvalidStringWithValue, value.ToString()));

            return new OverflowException(TInteger.OverflowMessage);
        }

        private static OverflowException GetOverflowException(TypeCode type)
        {
            string s;
            switch (type)
            {
                case TypeCode.SByte:
                    s = SR.Overflow_SByte;
                    break;
                case TypeCode.Byte:
                    s = SR.Overflow_Byte;
                    break;
                case TypeCode.Int16:
                    s = SR.Overflow_Int16;
                    break;
                case TypeCode.UInt16:
                    s = SR.Overflow_UInt16;
                    break;
                case TypeCode.Int32:
                    s = SR.Overflow_Int32;
                    break;
                case TypeCode.UInt32:
                    s = SR.Overflow_UInt32;
                    break;
                case TypeCode.Int64:
                    s = SR.Overflow_Int64;
                    break;
                case TypeCode.UInt64:
                    s = SR.Overflow_UInt64;
                    break;
                default:
                    Debug.Assert(type == TypeCode.Decimal);
                    s = SR.Overflow_Decimal;
                    break;
            }
            return new OverflowException(s);
        }

        private static Exception GetExceptionInt128(ParsingStatus status) =>
            status == ParsingStatus.Failed ?
                new FormatException(SR.Format_InvalidString) :
                new OverflowException(SR.Overflow_Int128);

        private static Exception GetExceptionUInt128(ParsingStatus status) =>
            status == ParsingStatus.Failed ?
                new FormatException(SR.Format_InvalidString) :
                new OverflowException(SR.Overflow_UInt128);

        internal static double NumberToDouble(ref NumberBuffer number)
        {
            number.CheckConsistency();
            double result;

            if ((number.DigitsCount == 0) || (number.Scale < DoubleMinExponent))
            {
                result = 0;
            }
            else if (number.Scale > DoubleMaxExponent)
            {
                result = double.PositiveInfinity;
            }
            else
            {
                ulong bits = NumberToDoubleFloatingPointBits(ref number, in FloatingPointInfo.Double);
                result = BitConverter.UInt64BitsToDouble(bits);
            }

            return number.IsNegative ? -result : result;
        }

        internal static Half NumberToHalf(ref NumberBuffer number)
        {
            number.CheckConsistency();
            Half result;

            if ((number.DigitsCount == 0) || (number.Scale < HalfMinExponent))
            {
                result = default;
            }
            else if (number.Scale > HalfMaxExponent)
            {
                result = Half.PositiveInfinity;
            }
            else
            {
                ushort bits = NumberToHalfFloatingPointBits(ref number, in FloatingPointInfo.Half);
                result = new Half(bits);
            }

            return number.IsNegative ? Half.Negate(result) : result;
        }

        internal static float NumberToSingle(ref NumberBuffer number)
        {
            number.CheckConsistency();
            float result;

            if ((number.DigitsCount == 0) || (number.Scale < SingleMinExponent))
            {
                result = 0;
            }
            else if (number.Scale > SingleMaxExponent)
            {
                result = float.PositiveInfinity;
            }
            else
            {
                uint bits = NumberToSingleFloatingPointBits(ref number, in FloatingPointInfo.Single);
                result = BitConverter.UInt32BitsToSingle(bits);
            }

            return number.IsNegative ? -result : result;
        }
    }
}

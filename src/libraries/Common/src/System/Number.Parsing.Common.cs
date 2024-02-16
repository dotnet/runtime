// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class Number
    {
        private static unsafe bool TryParseNumber<TChar>(scoped ref TChar* str, TChar* strEnd, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
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

            ReadOnlySpan<TChar> decSep;                                 // decimal separator from NumberFormatInfo.
            ReadOnlySpan<TChar> groupSep;                               // group separator from NumberFormatInfo.
            ReadOnlySpan<TChar> currSymbol = ReadOnlySpan<TChar>.Empty; // currency symbol from NumberFormatInfo.

            bool parsingCurrency = false;
            if ((styles & NumberStyles.AllowCurrencySymbol) != 0)
            {
                currSymbol = info.CurrencySymbolTChar<TChar>();

                // The idea here is to match the currency separators and on failure match the number separators to keep the perf of VB's IsNumeric fast.
                // The values of decSep are setup to use the correct relevant separator (currency in the if part and decimal in the else part).
                decSep = info.CurrencyDecimalSeparatorTChar<TChar>();
                groupSep = info.CurrencyGroupSeparatorTChar<TChar>();
                parsingCurrency = true;
            }
            else
            {
                decSep = info.NumberDecimalSeparatorTChar<TChar>();
                groupSep = info.NumberGroupSeparatorTChar<TChar>();
            }

            int state = 0;
            TChar* p = str;
            uint ch = (p < strEnd) ? TChar.CastToUInt32(*p) : '\0';
            TChar* next;

            while (true)
            {
                // Eat whitespace unless we've found a sign which isn't followed by a currency symbol.
                // "-Kr 1231.47" is legal but "- 1231.47" is not.
                if (!IsWhite(ch) || (styles & NumberStyles.AllowLeadingWhite) == 0 || ((state & StateSign) != 0 && (state & StateCurrency) == 0 && info.NumberNegativePattern != 2))
                {
                    if (((styles & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0 && ((next = MatchChars(p, strEnd, info.PositiveSignTChar<TChar>())) != null || ((next = MatchNegativeSignChars(p, strEnd, info)) != null && (number.IsNegative = true))))
                    {
                        state |= StateSign;
                        p = next - 1;
                    }
                    else if (ch == '(' && ((styles & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.IsNegative = true;
                    }
                    else if (!currSymbol.IsEmpty && (next = MatchChars(p, strEnd, currSymbol)) != null)
                    {
                        state |= StateCurrency;
                        currSymbol = ReadOnlySpan<TChar>.Empty;
                        // We already found the currency symbol. There should not be more currency symbols. Set
                        // currSymbol to NULL so that we won't search it again in the later code path.
                        p = next - 1;
                    }
                    else
                    {
                        break;
                    }
                }
                ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
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
                            number.Digits[digCount] = (byte)ch;
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
                else if (((styles & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, decSep)) != null || (parsingCurrency && (state & StateCurrency) == 0 && (next = MatchChars(p, strEnd, info.NumberDecimalSeparatorTChar<TChar>())) != null)))
                {
                    state |= StateDecimal;
                    p = next - 1;
                }
                else if (((styles & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((next = MatchChars(p, strEnd, groupSep)) != null || (parsingCurrency && (state & StateCurrency) == 0 && (next = MatchChars(p, strEnd, info.NumberGroupSeparatorTChar<TChar>())) != null)))
                {
                    p = next - 1;
                }
                else
                {
                    break;
                }
                ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
            }

            bool negExp = false;
            number.DigitsCount = digEnd;
            number.Digits[digEnd] = (byte)'\0';
            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((styles & NumberStyles.AllowExponent) != 0))
                {
                    TChar* temp = p;
                    ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
                    if ((next = MatchChars(p, strEnd, info.PositiveSignTChar<TChar>())) != null)
                    {
                        ch = (p = next) < strEnd ? TChar.CastToUInt32(*p) : '\0';
                    }
                    else if ((next = MatchNegativeSignChars(p, strEnd, info)) != null)
                    {
                        ch = (p = next) < strEnd ? TChar.CastToUInt32(*p) : '\0';
                        negExp = true;
                    }
                    if (IsDigit(ch))
                    {
                        int exp = 0;
                        do
                        {
                            // Check if we are about to overflow past our limit of 9 digits
                            if (exp >= 100_000_000)
                            {
                                // Set exp to Int.MaxValue to signify the requested exponent is too large. This will lead to an OverflowException later.
                                exp = int.MaxValue;
                                number.Scale = 0;

                                // Finish parsing the number, a FormatException could still occur later on.
                                while (IsDigit(ch))
                                {
                                    ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
                                }
                                break;
                            }

                            exp = (exp * 10) + (int)(ch - '0');
                            ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
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
                        ch = p < strEnd ? TChar.CastToUInt32(*p) : '\0';
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
                        number.Digits[number.DigitsCount] = (byte)'\0';
                    }
                }

                while (true)
                {
                    if (!IsWhite(ch) || (styles & NumberStyles.AllowTrailingWhite) == 0)
                    {
                        if ((styles & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0) && ((next = MatchChars(p, strEnd, info.PositiveSignTChar<TChar>())) != null || (((next = MatchNegativeSignChars(p, strEnd, info)) != null) && (number.IsNegative = true))))
                        {
                            state |= StateSign;
                            p = next - 1;
                        }
                        else if (ch == ')' && ((state & StateParens) != 0))
                        {
                            state &= ~StateParens;
                        }
                        else if (!currSymbol.IsEmpty && (next = MatchChars(p, strEnd, currSymbol)) != null)
                        {
                            currSymbol = ReadOnlySpan<TChar>.Empty;
                            p = next - 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    ch = ++p < strEnd ? TChar.CastToUInt32(*p) : '\0';
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

        internal static unsafe bool TryStringToNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(info != null);

            fixed (TChar* stringPointer = &MemoryMarshal.GetReference(value))
            {
                TChar* p = stringPointer;

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
        private static bool TrailingZeros<TChar>(ReadOnlySpan<TChar> value, int index)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            // For compatibility, we need to allow trailing zeros at the end of a number string
            return !value.Slice(index).ContainsAnyExcept(TChar.CastFrom('\0'));
        }

        private static bool IsWhite(uint ch) => (ch == 0x20) || ((ch - 0x09) <= (0x0D - 0x09));

        private static bool IsDigit(uint ch) => (ch - '0') <= 9;

        internal enum ParsingStatus
        {
            OK,
            Failed,
            Overflow
        }

        private static bool IsSpaceReplacingChar(uint c) => (c == '\u00a0') || (c == '\u202f');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe TChar* MatchNegativeSignChars<TChar>(TChar* p, TChar* pEnd, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            TChar* ret = MatchChars(p, pEnd, info.NegativeSignTChar<TChar>());

            if ((ret is null) && info.AllowHyphenDuringParsing() && (p < pEnd) && (TChar.CastToUInt32(*p) == '-'))
            {
                ret = p + 1;
            }

            return ret;
        }

        private static unsafe TChar* MatchChars<TChar>(TChar* p, TChar* pEnd, ReadOnlySpan<TChar> value)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert((p != null) && (pEnd != null) && (p <= pEnd) && (value != null));

            fixed (TChar* stringPointer = &MemoryMarshal.GetReference(value))
            {
                TChar* str = stringPointer;

                if (TChar.CastToUInt32(*str) != '\0')
                {
                    // We only hurt the failure case
                    // This fix is for French or Kazakh cultures. Since a user cannot type 0xA0 or 0x202F as a
                    // space character we use 0x20 space character instead to mean the same.
                    while (true)
                    {
                        uint cp = (p < pEnd) ? TChar.CastToUInt32(*p) : '\0';
                        uint val = TChar.CastToUInt32(*str);

                        if ((cp != val) && !(IsSpaceReplacingChar(val) && (cp == '\u0020')))
                        {
                            break;
                        }

                        p++;
                        str++;

                        if (TChar.CastToUInt32(*str) == '\0')
                        {
                            return p;
                        }
                    }
                }
            }

            return null;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System
{
    internal static partial class Number
    {
        // Internal-only style bit used by the INumberBase.TryParsePartial implementations to signal that parsing
        // should stop at the first otherwise-invalid character rather than failing. This is deliberately not a
        // public NumberStyles value; it is layered on top of the user-provided style after validation. It uses the
        // highest bit (0x8000_0000) so public flags can keep growing upward without stomping it; see NumberStyles.
        internal const NumberStyles AllowTrailingInvalidCharacters = unchecked((NumberStyles)0x80000000);

        private static bool TryParseNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info, out int elementsConsumed)
            where TChar : unmanaged, IUtfChar<TChar>
        {
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
            int index = 0;
            uint ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';

            while (true)
            {
                // Eat whitespace unless we've found a sign which isn't followed by a currency symbol.
                // "-Kr 1231.47" is legal but "- 1231.47" is not.
                if (!IsWhite(ch) || (styles & NumberStyles.AllowLeadingWhite) == 0 || ((state & StateSign) != 0 && (state & StateCurrency) == 0 && info.NumberNegativePattern != 2))
                {
                    int nextIndex;

                    if (((styles & NumberStyles.AllowLeadingSign) != 0) && (state & StateSign) == 0 && (((nextIndex = MatchChars(value, index, info.PositiveSignTChar<TChar>())) >= 0) || (((nextIndex = MatchNegativeSignChars(value, index, info)) >= 0) && (number.IsNegative = true))))
                    {
                        state |= StateSign;
                        index = nextIndex;
                    }
                    else if (ch == '(' && ((styles & NumberStyles.AllowParentheses) != 0) && ((state & StateSign) == 0))
                    {
                        state |= StateSign | StateParens;
                        number.IsNegative = true;
                        index++;
                    }
                    else if (!currSymbol.IsEmpty && (nextIndex = MatchChars(value, index, currSymbol)) >= 0)
                    {
                        state |= StateCurrency;
                        currSymbol = ReadOnlySpan<TChar>.Empty;
                        // We already found the currency symbol. There should not be more currency symbols. Set
                        // currSymbol to NULL so that we won't search it again in the later code path.
                        index = nextIndex;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    index++;
                }

                ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
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
                else
                {
                    int nextIndex;

                    if (((styles & NumberStyles.AllowDecimalPoint) != 0) && ((state & StateDecimal) == 0) && ((nextIndex = MatchChars(value, index, decSep)) >= 0 || (parsingCurrency && (state & StateCurrency) == 0 && (nextIndex = MatchChars(value, index, info.NumberDecimalSeparatorTChar<TChar>())) >= 0)))
                    {
                        state |= StateDecimal;
                        index = nextIndex;
                    }
                    else if (((styles & NumberStyles.AllowThousands) != 0) && ((state & StateDigits) != 0) && ((state & StateDecimal) == 0) && ((nextIndex = MatchChars(value, index, groupSep)) >= 0 || (parsingCurrency && (state & StateCurrency) == 0 && (nextIndex = MatchChars(value, index, info.NumberGroupSeparatorTChar<TChar>())) >= 0)))
                    {
                        index = nextIndex;
                    }
                    else
                    {
                        break;
                    }
                }
                if (IsDigit(ch))
                {
                    index++;
                }

                ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
            }

            bool negExp = false;
            number.DigitsCount = digEnd;
            number.Digits[digEnd] = (byte)'\0';
            if ((state & StateDigits) != 0)
            {
                if ((ch == 'E' || ch == 'e') && ((styles & NumberStyles.AllowExponent) != 0))
                {
                    int exponentIndex = index;
                    index++;
                    ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';

                    int nextIndex = MatchChars(value, index, info.PositiveSignTChar<TChar>());
                    if (nextIndex >= 0)
                    {
                        index = nextIndex;
                        ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
                    }
                    else if ((nextIndex = MatchNegativeSignChars(value, index, info)) >= 0)
                    {
                        index = nextIndex;
                        ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
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
                                    index++;
                                    ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
                                }
                                break;
                            }

                            exp = (exp * 10) + (int)(ch - '0');
                            index++;
                            ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
                        } while (IsDigit(ch));
                        if (negExp)
                        {
                            exp = -exp;
                        }
                        number.Scale += exp;
                    }
                    else
                    {
                        index = exponentIndex;
                        ch = TChar.CastToUInt32(value[index]);
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
                        int nextIndex;

                        if ((styles & NumberStyles.AllowTrailingSign) != 0 && ((state & StateSign) == 0) && (((nextIndex = MatchChars(value, index, info.PositiveSignTChar<TChar>())) >= 0) || ((((nextIndex = MatchNegativeSignChars(value, index, info)) >= 0)) && (number.IsNegative = true))))
                        {
                            state |= StateSign;
                            index = nextIndex;
                        }
                        else if (ch == ')' && ((state & StateParens) != 0))
                        {
                            state &= ~StateParens;
                            index++;
                        }
                        else if (!currSymbol.IsEmpty && (nextIndex = MatchChars(value, index, currSymbol)) >= 0)
                        {
                            currSymbol = ReadOnlySpan<TChar>.Empty;
                            index = nextIndex;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        index++;
                    }

                    ch = index < value.Length ? TChar.CastToUInt32(value[index]) : '\0';
                }
                if ((state & StateParens) == 0)
                {
                    if ((state & StateNonZero) == 0)
                    {
                        if (number.Kind is not (NumberBufferKind.Decimal or NumberBufferKind.DecimalIeee754))
                        {
                            number.Scale = 0;
                        }
                        if ((number.Kind == NumberBufferKind.Integer) && (state & StateDecimal) == 0)
                        {
                            number.IsNegative = false;
                        }
                    }

                    // For compatibility we still need to process any trailing
                    // nulls that exist and report them as having been consumed.

                    index = ConsumeTrailingNulls(value, index);

                    if ((index == value.Length) || ((styles & AllowTrailingInvalidCharacters) != 0))
                    {
                        elementsConsumed = index;
                        return true;
                    }
                }
            }

            elementsConsumed = 0;
            return false;
        }

        internal static bool TryStringToNumber<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, ref NumberBuffer number, NumberFormatInfo info, out int elementsConsumed)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            Debug.Assert(info != null);

            bool succeeded = TryParseNumber(value, styles, ref number, info, out elementsConsumed);
            number.CheckConsistency();
            return succeeded;
        }

        private static int ConsumeTrailingNulls<TChar>(ReadOnlySpan<TChar> value, int index)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            // For compatibility, we need to allow trailing nulls at the end of a number string
            var remainder = value.Slice(index);

            var nullsToConsume = remainder.IndexOfAnyExcept(TChar.CastFrom('\0'));
            return index + ((nullsToConsume >= 0) ? nullsToConsume : remainder.Length);
        }

        private static bool IsWhite(uint ch) => (ch == 0x20) || ((ch - 0x09) <= (0x0D - 0x09));

        private static bool IsDigit(uint ch) => (ch - '0') <= 9;

        internal enum ParsingStatus
        {
            OK,
            Failed,
            Overflow
        }

        private static bool IsSpaceReplacingChar(uint c) => c is '\u00a0' or '\u202f';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint NormalizeSpaceReplacingChar(uint c) => IsSpaceReplacingChar(c) ? '\u0020' : c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int MatchNegativeSignChars<TChar>(ReadOnlySpan<TChar> value, int index, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            int nextIndex = MatchChars(value, index, info.NegativeSignTChar<TChar>());

            if ((nextIndex < 0) && info.AllowHyphenDuringParsing() && ((uint)index < (uint)value.Length) && (TChar.CastToUInt32(value[index]) == '-'))
            {
                nextIndex = index + 1;
            }

            return nextIndex;
        }

        private static int MatchChars<TChar>(ReadOnlySpan<TChar> source, int index, ReadOnlySpan<TChar> value)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            // An empty pattern never matches, and one longer than the remaining input cannot match, so
            // the loop only has to bound itself by the pattern.
            if (value.IsEmpty || (value.Length > (source.Length - index)))
            {
                return -1;
            }

            for (int i = 0; i < value.Length; i++)
            {
                uint cp = TChar.CastToUInt32(source[index + i]);
                uint val = TChar.CastToUInt32(value[i]);

                // We only hurt the failure case
                // This fix is for cultures that use NBSP (U+00A0) or narrow NBSP (U+202F) as group/decimal separators
                // (e.g., French, Kazakh, Ukrainian). Since a user cannot easily type these characters,
                // we accept regular space (U+0020) as equivalent.
                // For UTF-16, we also handle the reverse case where the input has NBSP and the format string has space.
                if (cp != val && (TChar.IsUtf8 || NormalizeSpaceReplacingChar(cp) != NormalizeSpaceReplacingChar(val)))
                {
                    return -1;
                }
            }

            return index + value.Length;
        }
    }
}

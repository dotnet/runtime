// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The IeeeDecimalNumber class implements methods for formatting and parsing IEEE decimal types.
// If these types lived in the System namespace, these methods would live in Number.Parsing.cs and Number.Formatting.cs,
// just like Single, Double, and Half.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static partial class IeeeDecimalNumber
    {
        // IeeeDecimalNumberBuffer

        internal const int Decimal32BufferLength = 112 + 1 + 1; // TODO: X for the longest input + 1 for rounding (+1 for the null terminator). I just picked 112 cause that's what Single does for now.

        internal unsafe ref struct IeeeDecimalNumberBuffer
        {
            public int DigitsCount;
            public int Scale;
            public bool IsNegative;
            public bool HasNonZeroTail;
            public Span<byte> Digits;

            public IeeeDecimalNumberBuffer(byte* digits, int digitsLength) : this(new Span<byte>(digits, digitsLength))
            {
                Debug.Assert(digits != null);
            }

            /// <summary>Initializes the NumberBuffer.</summary>
            /// <param name="digits">The digits scratch space. The referenced memory must not be movable, e.g. stack memory, pinned array, etc.</param>
            public IeeeDecimalNumberBuffer(Span<byte> digits)
            {
                Debug.Assert(!digits.IsEmpty);

                DigitsCount = 0;
                Scale = 0;
                IsNegative = false;
                HasNonZeroTail = false;
                Digits = digits;
#if DEBUG
                Digits.Fill(0xCC);
#endif
                Digits[0] = (byte)'\0';
                CheckConsistency();
            }

            [Conditional("DEBUG")]
            public void CheckConsistency()
            {
#if DEBUG
                Debug.Assert(Digits[0] != '0', "Leading zeros should never be stored in an IeeeDecimalNumber");

                int numDigits;
                for (numDigits = 0; numDigits < Digits.Length; numDigits++)
                {
                    byte digit = Digits[numDigits];

                    if (digit == 0)
                    {
                        break;
                    }

                    Debug.Assert(char.IsAsciiDigit((char)digit), $"Unexpected character found in IeeeDecimalNumber: {digit}");
                }

                Debug.Assert(numDigits == DigitsCount, "Null terminator found in unexpected location in IeeeDecimalNumber");
                Debug.Assert(numDigits < Digits.Length, "Null terminator not found in IeeeDecimalNumber");
#endif // DEBUG
            }

            public byte* GetDigitsPointer()
            {
                // This is safe to do since we are a ref struct
                return (byte*)(Unsafe.AsPointer(ref Digits[0]));
            }

            //
            // Code coverage note: This only exists so that IeeeDecimalNumber displays nicely in the VS watch window. So yes, I know it works.
            //
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append('[');
                sb.Append('"');

                for (int i = 0; i < Digits.Length; i++)
                {
                    byte digit = Digits[i];

                    if (digit == 0)
                    {
                        break;
                    }

                    sb.Append((char)(digit));
                }

                sb.Append('"');
                sb.Append(", Length = ").Append(DigitsCount);
                sb.Append(", Scale = ").Append(Scale);
                sb.Append(", IsNegative = ").Append(IsNegative);
                sb.Append(", HasNonZeroTail = ").Append(HasNonZeroTail);
                sb.Append(']');

                return sb.ToString();
            }
        }

        // IeeeDecimalNumber Parsing

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
        // by the Parse methods if the NumberStyles. Any style is
        // specified. Note, however, that the Parse methods do not accept
        // NaNs or Infinities.

        // Max and Min Exponent assuming the value is in the form 0.Mantissa x 10^Exponent
        private const int Decimal32MaxExponent = Decimal32.MaxQExponent + Decimal32.Precision; // TODO check this
        private const int Decimal32MinExponent = Decimal32.MinQExponent + Decimal32.Precision; // TODO check this, possibly wrong

        [DoesNotReturn]
        internal static void ThrowFormatException(ReadOnlySpan<char> value) => throw new FormatException(/*SR.Format(SR.Format_InvalidStringWithValue,*/ value.ToString()/*)*/); // TODO get this to work

        internal static Decimal32 ParseDecimal32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseDecimal32(value, styles, info, out Decimal32 result))
            {
                ThrowFormatException(value);
            }

            return result;
        }

        internal static unsafe bool TryParseDecimal32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out Decimal32 result)
        {
            IeeeDecimalNumberBuffer number = new IeeeDecimalNumberBuffer(stackalloc byte[Decimal32BufferLength]);

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

                if (valueTrim.Equals(info.PositiveInfinitySymbol, StringComparison.OrdinalIgnoreCase))
                {
                    result = Decimal32.PositiveInfinity;
                }
                else if (valueTrim.Equals(info.NegativeInfinitySymbol, StringComparison.OrdinalIgnoreCase))
                {
                    result = Decimal32.NegativeInfinity;
                }
                else if (valueTrim.Equals(info.NaNSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    result = Decimal32.NaN;
                }
                else if (valueTrim.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase))
                {
                    valueTrim = valueTrim.Slice(info.PositiveSign.Length);

                    if (!info.PositiveInfinitySymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.Equals(info.PositiveInfinitySymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        result = Decimal32.PositiveInfinity;
                    }
                    else if (!info.NaNSymbol.StartsWith(info.PositiveSign, StringComparison.OrdinalIgnoreCase) && valueTrim.Equals(info.NaNSymbol, StringComparison.OrdinalIgnoreCase))
                    {
                        result = Decimal32.NaN;
                    }
                    else
                    {
                        result = Decimal32.Zero;
                        return false;
                    }
                }
                else if (valueTrim.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         valueTrim.Slice(info.NegativeSign.Length).Equals(info.NaNSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    result = Decimal32.NaN;
                }
                else if (info.AllowHyphenDuringParsing && SpanStartsWith(valueTrim, '-') && !info.NaNSymbol.StartsWith(info.NegativeSign, StringComparison.OrdinalIgnoreCase) &&
                         !info.NaNSymbol.StartsWith('-') && valueTrim.Slice(1).Equals(info.NaNSymbol, StringComparison.OrdinalIgnoreCase))
                {
                    result = Decimal32.NaN;
                }
                else
                {
                    result = Decimal32.Zero;
                    return false; // We really failed
                }
            }
            else
            {
                result = NumberToDecimal32(ref number);
            }

            return true;
        }

        internal static bool SpanStartsWith(ReadOnlySpan<char> span, char c) => !span.IsEmpty && span[0] == c;

        internal static unsafe bool TryStringToNumber(ReadOnlySpan<char> value, NumberStyles styles, ref IeeeDecimalNumberBuffer number, NumberFormatInfo info)
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

        // Note: this is copy pasted and adjusted from Number.Parsing.cs in System.Private.CoreLib
        private static unsafe bool TryParseNumber(scoped ref char* str, char* strEnd, NumberStyles styles, ref IeeeDecimalNumberBuffer number, NumberFormatInfo info)
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
            // int numberOfTrailingZeros = 0; TODO delete this comment

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
                            // TODO delete this comment
                            //if ((ch != '0') || (number.Kind != NumberBufferKind.Integer)) // number.Kind will always not be Integer
                            //{
                                digEnd = digCount + 1;
                            //}
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
                            // // TODO delete this comment: I removed this case because we care about trailing zeros for IEEE decimals
                            /* if (ch == '0')
                            {
                                numberOfTrailingZeros++;
                            }
                            else
                            {
                                numberOfTrailingZeros = 0;
                            }*/
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
                    if ((next = MatchChars(p, strEnd, info.PositiveSign)) != null)
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


                // TODO delete this comment: I removed this case because we care about trailing zeros for IEEE decimals
                /*
                if (number.Kind == NumberBufferKind.FloatingPoint &&
                !number.HasNonZeroTail)
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
                */

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
                        // Both of the below should always be false. I believe the first case is handling a specific edge case for System.Decimal that does not apply to the IEEE Decimals. TODO confirm this.
/*                        if (number.Kind != NumberBufferKind.Decimal)
                        {
                            number.Scale = 0;
                        }
                        if ((number.Kind == NumberBufferKind.Integer) && (state & StateDecimal) == 0)
                        {
                            number.IsNegative = false;
                        }*/
                    }
                    str = p;
                    return true;
                }
            }
            str = p;
            return false;
        }

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

        private static bool IsSpaceReplacingChar(char c) => c == '\u00a0' || c == '\u202f';

        private static bool IsWhite(int ch) => ch == 0x20 || (uint)(ch - 0x09) <= (0x0D - 0x09);

        private static bool IsDigit(int ch) => ((uint)ch - '0') <= 9;

        internal static unsafe Decimal32 NumberToDecimal32(ref IeeeDecimalNumberBuffer number)
        {
            number.CheckConsistency();

            if ((number.DigitsCount == 0) || (number.Scale < Decimal32MinExponent)) // TODO double check this
            {
                // TODO are we sure this is the "right" zero to return in all these cases
                return number.IsNegative ? Decimal32.NegativeZero : Decimal32.Zero;
            }
            else if (number.Scale > Decimal32MaxExponent) // TODO double check this
            {
                return number.IsNegative ? Decimal32.NegativeInfinity : Decimal32.PositiveInfinity;
            }
            else
            {
                // The input value is of the form 0.Mantissa x 10^Exponent, where 'Mantissa' are
                // the decimal digits of the mantissa and 'Exponent' is the decimal exponent.
                // We want to extract q (the exponent) and c (the significand) such that
                // value = c * 10 ^ q
                // Which means
                // c = first N digits of Mantissa, where N is min(Decimal32.Precision, number.DigitsCount)
                // q = Exponent - N

                byte* mantissa = number.GetDigitsPointer();

                int q = number.Scale;
                byte* mantissaPointer = number.GetDigitsPointer();
                uint c = 0;

                int i;
                for (i = 0; i < number.DigitsCount; i++)
                {
                    if (i >= Decimal32.Precision)
                    {
                        // We have more digits than the precision allows
                        break;
                    }

                    q--;
                    c *= 10;
                    c += (uint)(mantissa[i] - '0');
                }

                if (i < number.DigitsCount)
                {
                    // We have more digits than the precision allows, we might need to round up
                    // roundUp = (next digit > 5)
                    //        || ((next digit == 5) && (trailing digits || current digit is odd)
                    bool roundUp = false;

                    if (mantissa[i] > '5')
                    {
                        roundUp = true;
                    }
                    else if (mantissa[i] == '5')
                    {

                        if ((c & 1) == 1)
                        {
                            // current digit is odd, round to even regardless of whether or not we have trailing digits
                            roundUp = true;

                        }
                        else
                        {
                            // Current digit is even, but there might be trailing digits that cause us to round up anyway
                            // We might still have some additional digits, in which case they need
                            // to be considered as part of hasZeroTail. Some examples of this are:
                            //  * 3.0500000000000000000001e-27
                            //  * 3.05000000000000000000001e-27
                            // In these cases, we will have processed 3 and 0, and ended on 5. The
                            // buffer, however, will still contain a number of trailing zeros and
                            // a trailing non-zero number.

                            bool hasZeroTail = !number.HasNonZeroTail;
                            i++;
                            while ((mantissa[i] != 0) && hasZeroTail)
                            {
                                hasZeroTail &= (mantissa[i] == '0');
                                i++;
                            }

                            // We should either be at the end of the stream or have a non-zero tail
                            Debug.Assert((mantissa[i] == 0) || !hasZeroTail);

                            if (!hasZeroTail)
                            {
                                // If the next digit is 5 with a non-zero tail we must round up
                                roundUp = true;
                            }
                        }

                    }

                    if (roundUp)
                    {
                        if (++c > Decimal32.MaxSignificand)
                        {
                            // We have rounded up to Infinity, return early
                            return number.IsNegative ? Decimal32.NegativeInfinity : Decimal32.PositiveInfinity;
                        }
                    }
                }
                Debug.Assert(q >= Decimal32.MinQExponent && q <= Decimal32.MaxQExponent);
                return new Decimal32(number.IsNegative, (sbyte)q, c);
            }
        }

        internal enum ParsingStatus // No ParsingStatus.Overflow because these types can represent infinity
        {
            OK,
            Failed
        }

        private const NumberStyles InvalidNumberStyles = ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite
                                                   | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingSign
                                                   | NumberStyles.AllowParentheses | NumberStyles.AllowDecimalPoint
                                                   | NumberStyles.AllowThousands | NumberStyles.AllowExponent
                                                   | NumberStyles.AllowCurrencySymbol | NumberStyles.AllowHexSpecifier);
        internal static void ValidateParseStyleFloatingPoint(NumberStyles style)
        {
            // Check for undefined flags or hex number
            if ((style & (InvalidNumberStyles | NumberStyles.AllowHexSpecifier)) != 0)
            {
                ThrowInvalid(style);

                static void ThrowInvalid(NumberStyles value)
                {
                    if ((value & InvalidNumberStyles) != 0)
                    {
                        throw new ArgumentException(SR.Argument_InvalidNumberStyles, nameof(style));
                    }

                    throw new ArgumentException(SR.Arg_HexStyleNotSupported);
                }
            }
        }

        // IeeeDecimalNumber Formatting TODO
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

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
    // (the C, D, E, F, G, and N format specifiers) are guaranteed to be parsable
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

    internal interface IBinaryFloatParseAndFormatInfo<TSelf> : IBinaryFloatingPointIeee754<TSelf>, IMinMaxValue<TSelf>
        where TSelf : unmanaged, IBinaryFloatParseAndFormatInfo<TSelf>
    {
        static abstract int NumberBufferLength { get; }

        static abstract ulong ZeroBits { get; }
        static abstract ulong InfinityBits { get; }

        static abstract ulong NormalMantissaMask { get; }
        static abstract ulong DenormalMantissaMask { get; }

        static abstract int MinBinaryExponent { get; }
        static abstract int MaxBinaryExponent { get; }

        static abstract int MinDecimalExponent { get; }
        static abstract int MaxDecimalExponent { get; }

        static abstract int ExponentBias { get; }
        static abstract ushort ExponentBits { get; }

        static abstract int OverflowDecimalExponent { get; }
        static abstract int InfinityExponent { get; }

        static abstract ushort NormalMantissaBits { get; }
        static abstract ushort DenormalMantissaBits { get; }

        static abstract int MinFastFloatDecimalExponent { get; }
        static abstract int MaxFastFloatDecimalExponent { get; }

        static abstract int MinExponentRoundToEven { get; }
        static abstract int MaxExponentRoundToEven { get; }

        static abstract int MaxExponentFastPath { get; }
        static abstract ulong MaxMantissaFastPath { get; }

        static abstract TSelf BitsToFloat(ulong bits);

        static abstract ulong FloatToBits(TSelf value);
    }

    internal static partial class Number
    {
        private const int Int32Precision = 10;
        private const int UInt32Precision = Int32Precision;
        private const int Int64Precision = 19;
        private const int UInt64Precision = 20;
        private const int Int128Precision = 39;
        private const int UInt128Precision = 39;

        private const int FloatingPointMaxExponent = 309;
        private const int FloatingPointMinExponent = -324;

        private const int FloatingPointMaxDenormalMantissaBits = 52;

        private static unsafe bool TryNumberBufferToBinaryInteger<TInteger>(ref NumberBuffer number, ref TInteger value)
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            number.CheckConsistency();

            int i = number.Scale;

            if ((i > TInteger.MaxDigitCount) || (i < number.DigitsCount) || (!TInteger.IsSigned && number.IsNegative))
            {
                return false;
            }

            byte* p = number.DigitsPtr;

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

        internal static TInteger ParseBinaryInteger<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            ParsingStatus status = TryParseBinaryInteger(value, styles, info, out TInteger result);

            if (status != ParsingStatus.OK)
            {
                ThrowOverflowOrFormatException<TChar, TInteger>(status, value);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ParsingStatus TryParseBinaryInteger<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
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
                return TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, BinaryParser<TInteger>>(value, styles, out result);
            }

            return TryParseBinaryIntegerNumber(value, styles, info, out result);
        }

        private static ParsingStatus TryParseBinaryIntegerNumber<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
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
        internal static ParsingStatus TryParseBinaryIntegerStyle<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            Debug.Assert((styles & ~NumberStyles.Integer) == 0, "Only handles subsets of Integer format");

            if (value.IsEmpty)
            {
                goto FalseExit;
            }

            int index = 0;
            uint num = TChar.CastToUInt32(value[0]);

            // Skip past any whitespace at the beginning.
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;

                    if ((uint)index >= (uint)value.Length)
                    {
                        goto FalseExit;
                    }
                    num = TChar.CastToUInt32(value[index]);
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
                        {
                            goto FalseExit;
                        }
                        num = TChar.CastToUInt32(value[index]);
                    }
                    else if (num == '+')
                    {
                        index++;

                        if ((uint)index >= (uint)value.Length)
                        {
                            goto FalseExit;
                        }
                        num = TChar.CastToUInt32(value[index]);
                    }
                }
                else if (info.AllowHyphenDuringParsing() && num == '-')
                {
                    isNegative = true;
                    index++;

                    if ((uint)index >= (uint)value.Length)
                    {
                        goto FalseExit;
                    }
                    num = TChar.CastToUInt32(value[index]);
                }
                else
                {
                    value = value.Slice(index);
                    index = 0;

                    ReadOnlySpan<TChar> positiveSign = info.PositiveSignTChar<TChar>();
                    ReadOnlySpan<TChar> negativeSign = info.NegativeSignTChar<TChar>();

                    if (!positiveSign.IsEmpty && value.StartsWith(positiveSign))
                    {
                        index += positiveSign.Length;

                        if ((uint)index >= (uint)value.Length)
                        {
                            goto FalseExit;
                        }
                        num = TChar.CastToUInt32(value[index]);
                    }
                    else if (!negativeSign.IsEmpty && value.StartsWith(negativeSign))
                    {
                        isNegative = true;
                        index += negativeSign.Length;

                        if ((uint)index >= (uint)value.Length)
                        {
                            goto FalseExit;
                        }
                        num = TChar.CastToUInt32(value[index]);
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
                        {
                            goto DoneAtEnd;
                        }
                        num = TChar.CastToUInt32(value[index]);
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
                        {
                            goto DoneAtEndButPotentialOverflow;
                        }
                        else
                        {
                            goto DoneAtEnd;
                        }
                    }

                    num = TChar.CastToUInt32(value[index]);

                    if (!IsDigit(num))
                    {
                        goto HasTrailingChars;
                    }
                    index++;

                    answer = TInteger.MultiplyBy10(answer);
                    answer += TInteger.CreateTruncating(num - '0');
                }

                if ((uint)index >= (uint)value.Length)
                {
                    if (!TInteger.IsSigned)
                    {
                        goto DoneAtEndButPotentialOverflow;
                    }
                    else
                    {
                        goto DoneAtEnd;
                    }
                }

                num = TChar.CastToUInt32(value[index]);

                if (!IsDigit(num))
                {
                    goto HasTrailingChars;
                }
                index++;

                // Potential overflow now processing the MaxDigitCount digit.
                if (!TInteger.IsSigned)
                {
                    overflow |= (answer > TInteger.MaxValueDiv10) || ((answer == TInteger.MaxValueDiv10) && (num > '5'));
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
                {
                    goto DoneAtEndButPotentialOverflow;
                }

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility.
                num = TChar.CastToUInt32(value[index]);

                while (IsDigit(num))
                {
                    overflow = true;
                    index++;

                    if ((uint)index >= (uint)value.Length)
                    {
                        goto OverflowExit;
                    }
                    num = TChar.CastToUInt32(value[index]);
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
                {
                    goto FalseExit;
                }

                for (index++; index < value.Length; index++)
                {
                    uint ch = TChar.CastToUInt32(value[index]);

                    if (!IsWhite(ch))
                    {
                        break;
                    }
                }
                if ((uint)index >= (uint)value.Length)
                    goto DoneAtEndButPotentialOverflow;
            }

            if (!TrailingZeros(value, index))
            {
                goto FalseExit;
            }
            goto DoneAtEndButPotentialOverflow;
        }

        /// <summary>Parses <typeparamref name="TInteger"/> limited to styles that make up NumberStyles.HexNumber.</summary>
        internal static ParsingStatus TryParseBinaryIntegerHexNumberStyle<TChar, TInteger>(ReadOnlySpan<TChar> value, NumberStyles styles, out TInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            return TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, HexParser<TInteger>>(value, styles, out result);
        }

        private interface IHexOrBinaryParser<TInteger>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            static abstract NumberStyles AllowedStyles { get; }
            static abstract bool IsValidChar(uint ch);
            static abstract uint FromChar(uint ch);
            static abstract uint MaxDigitValue { get; }
            static abstract int MaxDigitCount { get; }
            static abstract TInteger ShiftLeftForNextDigit(TInteger value);
        }

        private readonly struct HexParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            public static NumberStyles AllowedStyles => NumberStyles.HexNumber;
            public static bool IsValidChar(uint ch) => HexConverter.IsHexChar((int)ch);
            public static uint FromChar(uint ch) => (uint)HexConverter.FromChar((int)ch);
            public static uint MaxDigitValue => 0xF;
            public static int MaxDigitCount => TInteger.MaxHexDigitCount;
            public static TInteger ShiftLeftForNextDigit(TInteger value) => TInteger.MultiplyBy16(value);
        }

        private readonly struct BinaryParser<TInteger> : IHexOrBinaryParser<TInteger> where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            public static NumberStyles AllowedStyles => NumberStyles.BinaryNumber;
            public static bool IsValidChar(uint ch) => (ch - '0') <= 1;
            public static uint FromChar(uint ch) => ch - '0';
            public static uint MaxDigitValue => 1;
            public static unsafe int MaxDigitCount => sizeof(TInteger) * 8;
            public static TInteger ShiftLeftForNextDigit(TInteger value) => value << 1;
        }

        private static ParsingStatus TryParseBinaryIntegerHexOrBinaryNumberStyle<TChar, TInteger, TParser>(ReadOnlySpan<TChar> value, NumberStyles styles, out TInteger result)
            where TChar : unmanaged, IUtfChar<TChar>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
            where TParser : struct, IHexOrBinaryParser<TInteger>
        {
            Debug.Assert((styles & ~TParser.AllowedStyles) == 0, $"Only handles subsets of {TParser.AllowedStyles} format");

            if (value.IsEmpty)
            {
                goto FalseExit;
            }

            int index = 0;
            uint num = TChar.CastToUInt32(value[0]);

            // Skip past any whitespace at the beginning.
            if ((styles & NumberStyles.AllowLeadingWhite) != 0 && IsWhite(num))
            {
                do
                {
                    index++;

                    if ((uint)index >= (uint)value.Length)
                    {
                        goto FalseExit;
                    }
                    num = TChar.CastToUInt32(value[index]);
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
                        {
                            goto DoneAtEnd;
                        }
                        num = TChar.CastToUInt32(value[index]);
                    } while (num == '0');

                    if (!TParser.IsValidChar(num))
                    {
                        goto HasTrailingChars;
                    }
                }

                // Parse up through MaxDigitCount digits, as no overflow is possible
                answer = TInteger.CreateTruncating(TParser.FromChar(num)); // first digit
                index++;

                for (int i = 0; i < TParser.MaxDigitCount - 1; i++) // next MaxDigitCount - 1 digits can't overflow
                {
                    if ((uint)index >= (uint)value.Length)
                    {
                        goto DoneAtEnd;
                    }
                    num = TChar.CastToUInt32(value[index]);

                    uint numValue = TParser.FromChar(num);

                    if (numValue > TParser.MaxDigitValue)
                    {
                        goto HasTrailingChars;
                    }
                    index++;

                    answer = TParser.ShiftLeftForNextDigit(answer);
                    answer += TInteger.CreateTruncating(numValue);
                }

                // If there's another digit, it's an overflow.
                if ((uint)index >= (uint)value.Length)
                {
                    goto DoneAtEnd;
                }

                num = TChar.CastToUInt32(value[index]);

                if (!TParser.IsValidChar(num))
                {
                    goto HasTrailingChars;
                }

                // At this point, we're either overflowing or hitting a formatting error.
                // Format errors take precedence for compatibility. Read through any remaining digits.
                do
                {
                    index++;

                    if ((uint)index >= (uint)value.Length)
                    {
                        goto OverflowExit;
                    }
                    num = TChar.CastToUInt32(value[index]);
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
                {
                    goto FalseExit;
                }

                for (index++; index < value.Length; index++)
                {
                    uint ch = TChar.CastToUInt32(value[index]);

                    if (!IsWhite(ch))
                    {
                        break;
                    }
                }

                if ((uint)index >= (uint)value.Length)
                {
                    goto DoneAtEndButPotentialOverflow;
                }
            }

            if (!TrailingZeros(value, index))
            {
                goto FalseExit;
            }
            goto DoneAtEndButPotentialOverflow;
        }

        internal static decimal ParseDecimal<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            ParsingStatus status = TryParseDecimal(value, styles, info, out decimal result);
            if (status != ParsingStatus.OK)
            {
                if (status == ParsingStatus.Failed)
                {
                    ThrowFormatException(value);
                }
                ThrowOverflowException(SR.Overflow_Decimal);
            }

            return result;
        }

        internal static unsafe bool TryNumberToDecimal(ref NumberBuffer number, ref decimal value)
        {
            number.CheckConsistency();

            byte* p = number.DigitsPtr;
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
                ulong tmp64 = ((uint)(low64 >> 32) * 10UL) + (tmpLow >> 32);
                low64 = (uint)tmpLow + (tmp64 << 32);
                high = (uint)(tmp64 >> 32) + (high * 10);

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
                        hasZeroTail &= c == '0';
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

        internal static TFloat ParseFloat<TChar, TFloat>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info)
            where TChar : unmanaged, IUtfChar<TChar>
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            if (!TryParseFloat(value, styles, info, out TFloat result))
            {
                ThrowFormatException(value);
            }
            return result;
        }

        internal static ParsingStatus TryParseDecimal<TChar>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out decimal result)
            where TChar : unmanaged, IUtfChar<TChar>
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

        internal static bool SpanStartsWith<TChar>(ReadOnlySpan<TChar> span, TChar c)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            return !span.IsEmpty && (span[0] == c);
        }

        internal static bool SpanStartsWith<TChar>(ReadOnlySpan<TChar> span, ReadOnlySpan<TChar> value, StringComparison comparisonType)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (typeof(TChar) == typeof(char))
            {
                ReadOnlySpan<char> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<char> typedValue = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(value)), value.Length);
                return typedSpan.StartsWith(typedValue, comparisonType);
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));

                ReadOnlySpan<byte> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<byte> typedValue = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
                return typedSpan.StartsWithUtf8(typedValue, comparisonType);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlySpan<TChar> SpanTrim<TChar>(ReadOnlySpan<TChar> span)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (typeof(TChar) == typeof(char))
            {
                ReadOnlySpan<char> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<char> result = typedSpan.Trim();
                return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<char, TChar>(ref MemoryMarshal.GetReference(result)), result.Length);
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));

                ReadOnlySpan<byte> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<byte> result = typedSpan.TrimUtf8();
                return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, TChar>(ref MemoryMarshal.GetReference(result)), result.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool SpanEqualsOrdinalIgnoreCase<TChar>(ReadOnlySpan<TChar> span, ReadOnlySpan<TChar> value)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            if (typeof(TChar) == typeof(char))
            {
                ReadOnlySpan<char> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<char> typedValue = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, char>(ref MemoryMarshal.GetReference(value)), value.Length);
                return typedSpan.EqualsOrdinalIgnoreCase(typedValue);
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(byte));

                ReadOnlySpan<byte> typedSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(span)), span.Length);
                ReadOnlySpan<byte> typedValue = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(value)), value.Length);
                return typedSpan.EqualsOrdinalIgnoreCaseUtf8(typedValue);
            }
        }

        internal static bool TryParseFloat<TChar, TFloat>(ReadOnlySpan<TChar> value, NumberStyles styles, NumberFormatInfo info, out TFloat result)
            where TChar : unmanaged, IUtfChar<TChar>
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            NumberBuffer number = new NumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[TFloat.NumberBufferLength]);

            if (!TryStringToNumber(value, styles, ref number, info))
            {
                ReadOnlySpan<TChar> valueTrim = SpanTrim(value);

                // This code would be simpler if we only had the concept of `InfinitySymbol`, but
                // we don't so we'll check the existing cases first and then handle `PositiveSign` +
                // `PositiveInfinitySymbol` and `PositiveSign/NegativeSign` + `NaNSymbol` last.

                ReadOnlySpan<TChar> positiveInfinitySymbol = info.PositiveInfinitySymbolTChar<TChar>();

                if (SpanEqualsOrdinalIgnoreCase(valueTrim, positiveInfinitySymbol))
                {
                    result = TFloat.PositiveInfinity;
                    return true;
                }

                if (SpanEqualsOrdinalIgnoreCase(valueTrim, info.NegativeInfinitySymbolTChar<TChar>()))
                {
                    result = TFloat.NegativeInfinity;
                    return true;
                }

                ReadOnlySpan<TChar> nanSymbol = info.NaNSymbolTChar<TChar>();

                if (SpanEqualsOrdinalIgnoreCase(valueTrim, nanSymbol))
                {
                    result = TFloat.NaN;
                    return true;
                }

                var positiveSign = info.PositiveSignTChar<TChar>();

                if (SpanStartsWith(valueTrim, positiveSign, StringComparison.OrdinalIgnoreCase))
                {
                    valueTrim = valueTrim.Slice(positiveSign.Length);

                    if (SpanEqualsOrdinalIgnoreCase(valueTrim, positiveInfinitySymbol))
                    {
                        result = TFloat.PositiveInfinity;
                        return true;
                    }
                    else if (SpanEqualsOrdinalIgnoreCase(valueTrim, nanSymbol))
                    {
                        result = TFloat.NaN;
                        return true;
                    }

                    result = TFloat.Zero;
                    return false;
                }

                ReadOnlySpan<TChar> negativeSign = info.NegativeSignTChar<TChar>();

                if (SpanStartsWith(valueTrim, negativeSign, StringComparison.OrdinalIgnoreCase))
                {
                    if (SpanEqualsOrdinalIgnoreCase(valueTrim.Slice(negativeSign.Length), nanSymbol))
                    {
                        result = TFloat.NaN;
                        return true;
                    }

                    if (info.AllowHyphenDuringParsing() && SpanStartsWith(valueTrim, TChar.CastFrom('-')) && SpanEqualsOrdinalIgnoreCase(valueTrim.Slice(1), nanSymbol))
                    {
                        result = TFloat.NaN;
                        return true;
                    }
                }

                result = TFloat.Zero;
                return false; // We really failed
            }

            result = NumberToFloat<TFloat>(ref number);
            return true;
        }

        [DoesNotReturn]
        internal static void ThrowOverflowOrFormatException<TChar, TInteger>(ParsingStatus status, ReadOnlySpan<TChar> value)
            where TChar : unmanaged, IUtfChar<TChar>
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            if (status == ParsingStatus.Failed)
            {
                ThrowFormatException(value);
            }
            ThrowOverflowException<TInteger>();
        }

        [DoesNotReturn]
        internal static void ThrowFormatException<TChar>(ReadOnlySpan<TChar> value)
            where TChar : unmanaged, IUtfChar<TChar>
        {
            string errorMessage;

            if (typeof(TChar) == typeof(byte))
            {
                // Decode the UTF8 value into a string we can include in the error message. We're here
                // because we failed to parse, which also means the bytes might not be valid UTF8,
                // so fallback to a message that doesn't include the value if the bytes are invalid.
                // It's possible after we check the bytes for validity that they could be concurrently
                // mutated, but if that's happening, all bets are off, anyway, and it simply impacts
                // which exception is thrown.
                ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<TChar, byte>(value);
                errorMessage = Utf8.IsValid(bytes) ?
                    SR.Format(SR.Format_InvalidStringWithValue, Encoding.UTF8.GetString(bytes)) :
                    SR.Format_InvalidString;
            }
            else
            {
                errorMessage = SR.Format(SR.Format_InvalidStringWithValue, value.ToString());
            }

            throw new FormatException(errorMessage);
        }

        [DoesNotReturn]
        internal static void ThrowOverflowException<TInteger>()
            where TInteger : unmanaged, IBinaryIntegerParseAndFormatInfo<TInteger>
        {
            throw new OverflowException(TInteger.OverflowMessage);
        }

        [DoesNotReturn]
        internal static void ThrowOverflowException(string message)
        {
            throw new OverflowException(message);
        }

        internal static TFloat NumberToFloat<TFloat>(ref NumberBuffer number)
            where TFloat : unmanaged, IBinaryFloatParseAndFormatInfo<TFloat>
        {
            number.CheckConsistency();
            TFloat result;

            if ((number.DigitsCount == 0) || (number.Scale < TFloat.MinDecimalExponent))
            {
                result = TFloat.Zero;
            }
            else if (number.Scale > TFloat.MaxDecimalExponent)
            {
                result = TFloat.PositiveInfinity;
            }
            else
            {
                ulong bits = NumberToFloatingPointBits<TFloat>(ref number);
                result = TFloat.BitsToFloat(bits);
            }

            return number.IsNegative ? -result : result;
        }
    }
}

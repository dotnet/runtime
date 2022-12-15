// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// The IeeeDecimalNumber class implements methods for formatting and parsing IEEE decimal types.
// If these types lived in the System namespace, these methods would live in Number.Parsing.cs and Number.Formatting.cs,
// just like Single, Double, and Half.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    internal static class IeeeDecimalNumber
    {

        // IeeeDecimalNumberBuffer

        internal const int Decimal32BufferLength = 0 + 1 + 1; // TODO: X for the longest input + 1 for rounding (+1 for the null terminator)

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

#pragma warning disable CA1822
            [Conditional("DEBUG")]
            public void CheckConsistency() // TODO do we want this?
            {
#if DEBUG
                Debug.Assert(Digits[0] != '0', "Leading zeros should never be stored in a IeeeDecimalNumber");

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
#pragma warning restore CA1822

#pragma warning disable CA1822 // TODO I don't know why this is required, it isn't for Number.NumberBuffer.cs
            public byte* GetDigitsPointer() // TODO this is complaining that the method could be static, but it is wrong
            {
                // This is safe to do since we are a ref struct
                return (byte*)(Unsafe.AsPointer(ref Digits[0]));
            }
#pragma warning restore CA1822

            //
            // Code coverage note: This only exists so that Number displays nicely in the VS watch window. So yes, I know it works.
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

        // IeeeDecimalNumber Parsing TODO potentially rewrite this

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

        [DoesNotReturn]
        internal static void ThrowFormatException(ParsingStatus status, ReadOnlySpan<char> value, TypeCode type = 0) => throw new FormatException(SR.Format(SR.Format_InvalidStringWithValue, value.ToString()));

        internal static Half ParseDecimal32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info)
        {
            if (!TryParseDecimal32(value, styles, info, out Decimal32 result))
            {
                ThrowFormatException(ParsingStatus.Failed, value);
            }

            return result;
        }

        internal static unsafe bool TryParseDecimal32(ReadOnlySpan<char> value, NumberStyles styles, NumberFormatInfo info, out Decimal32 result)
        {
            IeeeDecimalNumberBuffer number = new IeeeDecimalNumberBuffer(NumberBufferKind.FloatingPoint, stackalloc byte[HalfNumberBufferLength]);

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

        internal static Decimal32 NumberToDecimal32(ref NumberBuffer number)
        {
            number.CheckConsistency();
            Decimal32 result;

            if ((number.DigitsCount == 0) || (number.Scale < Decimal32MinExponent))
            {
                result = default;
            }
            else if (number.Scale > Decimal32MaxExponent)
            {
                result = Decimal32.PositiveInfinity;
            }
            else
            {
                ushort bits = NumberToDecimal32FloatingPointBits(ref number, in FloatingPointInfo.Decimal32);
                result = new Decimal32(bits);
            }

            return number.IsNegative ? Decimal32.Negate(result) : result;
        }

        internal enum ParsingStatus // No Overflow because these types can represent Infinity
        {
            OK,
            Failed
        }

        // IeeeDecimalNumber Formatting
    }
}

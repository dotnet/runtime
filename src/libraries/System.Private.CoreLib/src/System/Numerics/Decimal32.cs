// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Decimal32
        : IComparable,
          IComparable<Decimal32>,
          IEquatable<Decimal32>,
          ISpanParsable<Decimal32>,
          IMinMaxValue<Decimal32>,
          IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>
    {
        internal readonly uint _value;

        internal Decimal32(uint value)
        {
            _value = value;
        }

        private const int MaxExponent = 96;
        private const int MinExponent = -95;
        private const int Precision = 7;
        private const int ExponentBias = 101;
        private const uint PositiveInfinityValue = 0x7800_0000;
        private const uint NegativeInfinityValue = 0xF800_0000;
        // Canonical ±0 use the IEEE 754 preferred representation for integer values,
        // which stores zero with the biased exponent rather than the minimum exponent.
        private const uint ZeroValue = 0x3280_0000;
        private const uint NegativeZeroValue = 0xB280_0000;
        // One (+1 * 10^0) shares the biased exponent of canonical zero with a coefficient of one.
        private const uint OneValue = ZeroValue | 0x1;
        private const uint NegativeOneValue = NegativeZeroValue | 0x1;
        // Mathematical constants correctly rounded to the format's precision (7 significant digits).
        private const uint EValue = 0x2FA9_7A4A;   // +2.718282
        private const uint PiValue = 0x2FAF_EFD9;  // +3.141593
        private const uint TauValue = 0x2FDF_DFB1; // +6.283185
        private const uint QuietNaNValue = 0xFC00_0000;
        private const uint G0G1Mask = 0x6000_0000;
        private const uint SignMask = 0x8000_0000;
        private const uint MostSignificantBitOfSignificandMask = 0x0080_0000;
        private const uint NaNMask = 0x7C00_0000;
        private const uint InfinityMask = 0x7800_0000;
        private const uint MaxSignificand = 9_999_999;
        private const uint MaxInternalValue = 0x77F8_967F; // +9.999_999 * 10^96; aka +9_999_999 * 10^90
        private const uint MinInternalValue = 0xF7F8_967F; // -9.999_999 * 10^96; aka -9_999_999 * 10^90

        public static Decimal32 PositiveInfinity => new Decimal32(PositiveInfinityValue);
        public static Decimal32 NegativeInfinity => new Decimal32(NegativeInfinityValue);
        public static Decimal32 NaN => new Decimal32(QuietNaNValue);
        public static Decimal32 NegativeZero => new Decimal32(NegativeZeroValue);
        public static Decimal32 Zero => new Decimal32(ZeroValue);
        public static Decimal32 MinValue => new Decimal32(MinInternalValue);
        public static Decimal32 MaxValue => new Decimal32(MaxInternalValue);

        public static Decimal32 Epsilon => new Decimal32(0x0000_0001); // Smallest positive subnormal value, aka 1 * 10^-101

        private static ReadOnlySpan<uint> UInt32Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
            ];

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<char, Decimal32, uint>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal32"/> value representing the input string. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal32 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal32 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal32 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s, style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input string if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value == null)
            {
                return 1;
            }

            if (value is not Decimal32 i)
            {
                throw new ArgumentException(SR.Arg_MustBeDecimal32);
            }

            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, i._value);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal32 other)
        {
            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, other._value);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal32 other)
        {
            return Number.CompareDecimalIeee754<Decimal32, uint>(_value, other._value) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal32 other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return Number.GetDecimalIeee754HashCode<Decimal32, uint>(_value);
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(_value, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(_value, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(_value, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal32, uint>(_value, format, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute the unary plus.</param>
        /// <returns><paramref name="value" /> unchanged.</returns>
        public static Decimal32 operator +(Decimal32 value) => value;

        /// <summary>Computes the unary negation of a value.</summary>
        /// <param name="value">The value for which to compute the unary negation.</param>
        /// <returns>The unary negation of <paramref name="value" />.</returns>
        public static Decimal32 operator -(Decimal32 value) => new Decimal32(value._value ^ SignMask);

        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" /> by one.</returns>
        public static Decimal32 operator ++(Decimal32 value)
        {
            return new Decimal32(Number.AddDecimalIeee754<Decimal32, uint>(value._value, OneValue));
        }

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" /> by one.</returns>
        public static Decimal32 operator --(Decimal32 value)
        {
            return new Decimal32(Number.SubtractDecimalIeee754<Decimal32, uint>(value._value, OneValue));
        }

        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal32 operator +(Decimal32 left, Decimal32 right)
        {
            return new Decimal32(Number.AddDecimalIeee754<Decimal32, uint>(left._value, right._value));
        }

        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        public static Decimal32 operator -(Decimal32 left, Decimal32 right)
        {
            return new Decimal32(Number.SubtractDecimalIeee754<Decimal32, uint>(left._value, right._value));
        }

        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal32 operator *(Decimal32 left, Decimal32 right)
        {
            return new Decimal32(Number.MultiplyDecimalIeee754<Decimal32, uint>(left._value, right._value));
        }

        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal32 operator /(Decimal32 left, Decimal32 right)
        {
            return new Decimal32(Number.DivideDecimalIeee754<Decimal32, uint>(left._value, right._value));
        }

        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Decimal32 left, Decimal32 right)
        {
            return Number.EqualsDecimalIeee754<Decimal32, uint>(left._value, right._value);
        }

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Decimal32 left, Decimal32 right)
        {
            return !(left == right);
        }

        /// <summary>Compares two values to determine which is less.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <(Decimal32 left, Decimal32 right)
        {
            return Number.LessThanDecimalIeee754<Decimal32, uint>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is greater.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >(Decimal32 left, Decimal32 right)
        {
            return Number.GreaterThanDecimalIeee754<Decimal32, uint>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is less or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <=(Decimal32 left, Decimal32 right)
        {
            return Number.LessThanOrEqualDecimalIeee754<Decimal32, uint>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is greater or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >=(Decimal32 left, Decimal32 right)
        {
            return Number.GreaterThanOrEqualDecimalIeee754<Decimal32, uint>(left._value, right._value);
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s, style, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int bytesConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<byte, Decimal32, uint>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result, out bytesConsumed) == Number.ParsingStatus.OK;
        }

        /// <summary>Gets the value <c>1</c>.</summary>
        public static Decimal32 One => new Decimal32(OneValue);

        /// <summary>Gets the value <c>-1</c>.</summary>
        public static Decimal32 NegativeOne => new Decimal32(NegativeOneValue);

        /// <summary>Gets the mathematical constant <c>e</c>.</summary>
        public static Decimal32 E => new Decimal32(EValue);

        /// <summary>Gets the mathematical constant <c>pi</c>.</summary>
        public static Decimal32 Pi => new Decimal32(PiValue);

        /// <summary>Gets the mathematical constant <c>tau</c>.</summary>
        public static Decimal32 Tau => new Decimal32(TauValue);

        /// <summary>Computes the absolute of a value.</summary>
        /// <param name="value">The value for which to get its absolute.</param>
        /// <returns>The absolute of <paramref name="value" />.</returns>
        public static Decimal32 Abs(Decimal32 value) => new Decimal32(Number.AbsDecimalIeee754<Decimal32, uint>(value._value));

        /// <summary>Determines if a value is finite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is finite; otherwise, <c>false</c>.</returns>
        public static bool IsFinite(Decimal32 value) => (value._value & InfinityMask) != InfinityMask;

        /// <summary>Determines if a value is infinite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is infinite; otherwise, <c>false</c>.</returns>
        public static bool IsInfinity(Decimal32 value) => (value._value & NaNMask) == InfinityMask;

        /// <summary>Determines if a value is NaN.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is NaN; otherwise, <c>false</c>.</returns>
        public static bool IsNaN(Decimal32 value) => (value._value & NaNMask) == NaNMask;

        /// <summary>Determines if a value is negative.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative; otherwise, <c>false</c>.</returns>
        public static bool IsNegative(Decimal32 value) => (value._value & SignMask) != 0;

        /// <summary>Determines if a value is negative infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative infinity; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeInfinity(Decimal32 value) => (value._value & (SignMask | NaNMask)) == NegativeInfinityValue;

        /// <summary>Determines if a value is positive.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive; otherwise, <c>false</c>.</returns>
        public static bool IsPositive(Decimal32 value) => (value._value & SignMask) == 0;

        /// <summary>Determines if a value is positive infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive infinity; otherwise, <c>false</c>.</returns>
        public static bool IsPositiveInfinity(Decimal32 value) => (value._value & (SignMask | NaNMask)) == PositiveInfinityValue;

        /// <summary>Determines if a value represents a real number.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a real number; otherwise, <c>false</c>.</returns>
        public static bool IsRealNumber(Decimal32 value) => !IsNaN(value);

        /// <summary>Determines if a value represents an integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an integer; otherwise, <c>false</c>.</returns>
        public static bool IsInteger(Decimal32 value) => Number.IsIntegerDecimalIeee754<Decimal32, uint>(value._value);

        /// <summary>Determines if a value represents an even integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an even integer; otherwise, <c>false</c>.</returns>
        public static bool IsEvenInteger(Decimal32 value) => Number.IsEvenIntegerDecimalIeee754<Decimal32, uint>(value._value);

        /// <summary>Determines if a value represents an odd integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an odd integer; otherwise, <c>false</c>.</returns>
        public static bool IsOddInteger(Decimal32 value) => Number.IsOddIntegerDecimalIeee754<Decimal32, uint>(value._value);

        /// <summary>Determines if a value is normal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is normal; otherwise, <c>false</c>.</returns>
        public static bool IsNormal(Decimal32 value) => Number.IsNormalDecimalIeee754<Decimal32, uint>(value._value);

        /// <summary>Determines if a value is subnormal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is subnormal; otherwise, <c>false</c>.</returns>
        public static bool IsSubnormal(Decimal32 value) => Number.IsSubnormalDecimalIeee754<Decimal32, uint>(value._value);

        /// <summary>Compares two values to compute which has the greater magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MaxMagnitude(Decimal32 x, Decimal32 y) => new Decimal32(Number.MaxMagnitudeDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which has the greater magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MaxMagnitudeNumber(Decimal32 x, Decimal32 y) => new Decimal32(Number.MaxMagnitudeNumberDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which has the lesser magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MinMagnitude(Decimal32 x, Decimal32 y) => new Decimal32(Number.MinMagnitudeDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which has the lesser magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MinMagnitudeNumber(Decimal32 x, Decimal32 y) => new Decimal32(Number.MinMagnitudeNumberDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Computes an estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="left">The value to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The value to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The value to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>An estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</returns>
        public static Decimal32 MultiplyAddEstimate(Decimal32 left, Decimal32 right, Decimal32 addend) => (left * right) + addend;

        /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        public static Decimal32 Clamp(Decimal32 value, Decimal32 min, Decimal32 max)
        {
            if (min > max)
            {
                Math.ThrowMinMaxException(min, max);
            }
            return Min(Max(value, min), max);
        }

        /// <summary>Clamps a value to an inclusive minimum and maximum value using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        public static Decimal32 ClampNative(Decimal32 value, Decimal32 min, Decimal32 max)
        {
            if (min > max)
            {
                Math.ThrowMinMaxException(min, max);
            }
            return MinNative(MaxNative(value, min), max);
        }

        /// <summary>Copies the sign of a value to the sign of another value.</summary>
        /// <param name="value">The value whose magnitude is used in the result.</param>
        /// <param name="sign">The value whose sign is used in the result.</param>
        /// <returns>A value with the magnitude of <paramref name="value" /> and the sign of <paramref name="sign" />.</returns>
        public static Decimal32 CopySign(Decimal32 value, Decimal32 sign) => new Decimal32(Number.CopySignDecimalIeee754<Decimal32, uint>(value._value, sign._value));

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 Max(Decimal32 x, Decimal32 y) => new Decimal32(Number.MaxDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which is greater using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MaxNative(Decimal32 x, Decimal32 y) => new Decimal32(Number.MaxNativeDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which is greater and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MaxNumber(Decimal32 x, Decimal32 y) => new Decimal32(Number.MaxNumberDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 Min(Decimal32 x, Decimal32 y) => new Decimal32(Number.MinDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MinNative(Decimal32 x, Decimal32 y) => new Decimal32(Number.MinNativeDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal32 MinNumber(Decimal32 x, Decimal32 y) => new Decimal32(Number.MinNumberDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Computes the sign of a value.</summary>
        /// <param name="value">The value whose sign is to be computed.</param>
        /// <returns>A positive one if <paramref name="value" /> is positive, a negative one if <paramref name="value" /> is negative, and zero if <paramref name="value" /> is zero.</returns>
        /// <exception cref="ArithmeticException"><paramref name="value" /> is <c>NaN</c>.</exception>
        public static int Sign(Decimal32 value) => Number.SignDecimalIeee754<Decimal32, uint>(value._value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.BufferLength => Number.Decimal32NumberBufferLength;

        static string IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ToDecStr(uint significand)
        {
            return Number.UInt32ToDecStr(significand);
        }

        static unsafe uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            return Number.DigitsToUInt32(number.DigitsPtr, digits);
        }

        static Decimal32 IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Construct(uint value) => new Decimal32(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ConvertToExponent(uint value) => (int)value;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Power10(int exponent) => UInt32Powers10[exponent];

        static (uint Quotient, uint Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.DivRemPow10(uint value, int exponent)
        {
            uint power = UInt32Powers10[exponent];
            return Math.DivRem(value, power);
        }
        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.CountDigits(uint significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MinExponent => MinExponent;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.PositiveInfinity => PositiveInfinityValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NegativeInfinity => NegativeInfinityValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.Zero => ZeroValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NaN => QuietNaNValue;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NaNMask => NaNMask;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.SignMask => SignMask;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G0G1Mask => G0G1Mask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.NumberBitsSignificand => 20;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G0ToGwPlus1ExponentMask => 0x7F80_0000;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.G2ToGwPlus3ExponentMask => 0x1FE0_0000;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.GwPlus2ToGwPlus4SignificandMask => 0x007F_FFFF;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.GwPlus4SignificandMask => 0x001F_FFFF;

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxSignificand => MaxSignificand;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsNaN(uint decimalBits) => IsNaN(new Decimal32(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsNegative(uint decimalBits) => IsNegative(new Decimal32(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsFinite(uint decimalBits) => IsFinite(new Decimal32(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsInfinity(uint decimalBits) => IsInfinity(new Decimal32(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsPositiveInfinity(uint decimalBits) => IsPositiveInfinity(new Decimal32(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.IsNegativeInfinity(uint decimalBits) => IsNegativeInfinity(new Decimal32(decimalBits));

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.EncodeExponentToG0ThroughGwPlus1(uint biasedExponent)
        {
            return biasedExponent << 23;
        }

        static uint IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.EncodeExponentToG2ThroughGwPlus3(uint biasedExponent)
        {
            return biasedExponent << 21;
        }
    }
}

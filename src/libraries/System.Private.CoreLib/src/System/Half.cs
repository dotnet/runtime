// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.

    /// <summary>
    /// An IEEE 754 compliant float16 type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Half
        : IComparable,
          ISpanFormattable,
          IComparable<Half>,
          IEquatable<Half>,
          IBinaryFloatingPointIeee754<Half>,
          IMinMaxValue<Half>
    {
        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        // Constants for manipulating the private bit-representation

        internal const ushort SignMask = 0x8000;
        internal const int SignShift = 15;
        internal const byte ShiftedSignMask = SignMask >> SignShift;

        internal const ushort BiasedExponentMask = 0x7C00;
        internal const int BiasedExponentShift = 10;
        internal const byte ShiftedBiasedExponentMask = BiasedExponentMask >> BiasedExponentShift;

        internal const ushort TrailingSignificandMask = 0x03FF;

        internal const byte MinSign = 0;
        internal const byte MaxSign = 1;

        internal const byte MinBiasedExponent = 0x00;
        internal const byte MaxBiasedExponent = 0x1F;

        internal const byte ExponentBias = 15;

        internal const sbyte MinExponent = -14;
        internal const sbyte MaxExponent = +15;

        internal const ushort MinTrailingSignificand = 0x0000;
        internal const ushort MaxTrailingSignificand = 0x03FF;

        // Constants representing the private bit-representation for various default values

        private const ushort PositiveZeroBits = 0x0000;
        private const ushort NegativeZeroBits = 0x8000;

        private const ushort EpsilonBits = 0x0001;

        private const ushort PositiveInfinityBits = 0x7C00;
        private const ushort NegativeInfinityBits = 0xFC00;

        private const ushort PositiveQNaNBits = 0x7E00;
        private const ushort NegativeQNaNBits = 0xFE00;

        private const ushort MinValueBits = 0xFBFF;
        private const ushort MaxValueBits = 0x7BFF;

        private const ushort PositiveOneBits = 0x3C00;
        private const ushort NegativeOneBits = 0xBC00;

        private const ushort EBits = 0x4170;
        private const ushort PiBits = 0x4248;
        private const ushort TauBits = 0x4648;

        // Well-defined and commonly used values

        public static Half Epsilon => new Half(EpsilonBits);                        //  5.9604645E-08

        public static Half PositiveInfinity => new Half(PositiveInfinityBits);      //  1.0 / 0.0;

        public static Half NegativeInfinity => new Half(NegativeInfinityBits);      // -1.0 / 0.0

        public static Half NaN => new Half(NegativeQNaNBits);                       //  0.0 / 0.0

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static Half MinValue => new Half(MinValueBits);                      // -65504

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static Half MaxValue => new Half(MaxValueBits);                      //  65504

        internal readonly ushort _value;

        internal Half(ushort value)
        {
            _value = value;
        }

        private Half(bool sign, ushort exp, ushort sig) => _value = (ushort)(((sign ? 1 : 0) << SignShift) + (exp << BiasedExponentShift) + sig);

        internal byte BiasedExponent
        {
            get
            {
                ushort bits = _value;
                return ExtractBiasedExponentFromBits(bits);
            }
        }

        internal sbyte Exponent
        {
            get
            {
                return (sbyte)(BiasedExponent - ExponentBias);
            }
        }

        internal ushort Significand
        {
            get
            {
                return (ushort)(TrailingSignificand | ((BiasedExponent != 0) ? (1U << BiasedExponentShift) : 0U));
            }
        }

        internal ushort TrailingSignificand
        {
            get
            {
                ushort bits = _value;
                return ExtractTrailingSignificandFromBits(bits);
            }
        }

        internal static byte ExtractBiasedExponentFromBits(ushort bits)
        {
            return (byte)((bits >> BiasedExponentShift) & ShiftedBiasedExponentMask);
        }

        internal static ushort ExtractTrailingSignificandFromBits(ushort bits)
        {
            return (ushort)(bits & TrailingSignificandMask);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative && !AreZero(left, right);
            }

            return (left._value != right._value) && ((left._value < right._value) ^ leftIsNegative);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(Half left, Half right)
        {
            return right < left;
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative || AreZero(left, right);
            }

            return (left._value == right._value) || ((left._value < right._value) ^ leftIsNegative);
        }

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther, TResult}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(Half left, Half right)
        {
            return right <= left;
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is not equal to anything, including itself.
                return false;
            }

            // IEEE defines that positive and negative zero are equivalent.
            return (left._value == right._value) || AreZero(left, right);
        }

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther, TResult}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(Half left, Half right)
        {
            return !(left == right);
        }

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        public static bool IsFinite(Half value)
        {
            return StripSign(value) < PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        public static bool IsInfinity(Half value)
        {
            return StripSign(value) == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        public static bool IsNaN(Half value)
        {
            return StripSign(value) > PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        public static bool IsNegative(Half value)
        {
            return (short)(value._value) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        public static bool IsNegativeInfinity(Half value)
        {
            return value._value == NegativeInfinityBits;
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsNormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & BiasedExponentMask) != 0);    // is not subnormal (has a non-zero exponent)
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        public static bool IsPositiveInfinity(Half value)
        {
            return value._value == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsSubnormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & BiasedExponentMask) == 0);    // is subnormal (has a zero exponent)
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, DefaultParseStyle, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, NumberStyles style)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, style, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, IFormatProvider? provider)
        {
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, DefaultParseStyle, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(string s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            if (s == null) ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Half"/> value representing the input string. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. </returns>
        public static Half Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultParseStyle, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to parses a <see cref="Half"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Half result)
        {
            if (s == null)
            {
                result = default;
                return false;
            }
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <summary>
        /// Tries to parses a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Half result)
        {
            return TryParse(s, DefaultParseStyle, provider: null, out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="Half"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Half result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);

            if (s == null)
            {
                result = default;
                return false;
            }

            return TryParse(s.AsSpan(), style, provider, out result);
        }

        /// <summary>
        /// Tries to parse a <see cref="Half"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Half"/> value representing the input string if the parse was successful. If the input exceeds Half's range, a <see cref="Half.PositiveInfinity"/> or <see cref="Half.NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Half"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Half result)
        {
            NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
            return Number.TryParseHalf(s, style, NumberFormatInfo.GetInstance(provider), out result);
        }

        private static bool AreZero(Half left, Half right)
        {
            // IEEE defines that positive and negative zero are equal, this gives us a quick equality check
            // for two values by or'ing the private bits together and stripping the sign. They are both zero,
            // and therefore equivalent, if the resulting value is still zero.
            return (ushort)((left._value | right._value) & ~SignMask) == 0;
        }

        private static bool IsNaNOrZero(Half value)
        {
            return ((value._value - 1) & ~SignMask) >= PositiveInfinityBits;
        }

        private static uint StripSign(Half value)
        {
            return (ushort)(value._value & ~SignMask);
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="obj"/>, zero if this is equal to <paramref name="obj"/>, or a value greater than zero if this is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not of type <see cref="Half"/>.</exception>
        public int CompareTo(object? obj)
        {
            if (!(obj is Half))
            {
                return (obj is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeHalf);
            }
            return CompareTo((Half)(obj));
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="other"/>, zero if this is equal to <paramref name="other"/>, or a value greater than zero if this is greater than <paramref name="other"/>.</returns>
        public int CompareTo(Half other)
        {
            if (this < other)
            {
                return -1;
            }

            if (this > other)
            {
                return 1;
            }

            if (this == other)
            {
                return 0;
            }

            if (IsNaN(this))
            {
                return IsNaN(other) ? 0 : -1;
            }

            Debug.Assert(IsNaN(other));
            return 1;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Half other) && Equals(other);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="other"/> value.
        /// </summary>
        public bool Equals(Half other)
        {
            return _value == other._value
                || AreZero(this, other)
                || (IsNaN(this) && IsNaN(other));
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            if (IsNaNOrZero(this))
            {
                // All NaNs should have the same hash code, as should both Zeros.
                return _value & PositiveInfinityBits;
            }
            return _value;
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatHalf(this, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatHalf(this, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatHalf(this, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatHalf(this, format, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Tries to format the value of the current Half instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">When this method returns, this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination"/>.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination"/>.</param>
        /// <returns></returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        //
        // Explicit Convert To Half
        //

        /// <summary>Explicitly converts a <see cref="char" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(char value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(decimal value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(double value)
        {
            const int DoubleMaxExponent = 0x7FF;

            ulong doubleInt = BitConverter.DoubleToUInt64Bits(value);
            bool sign = (doubleInt & double.SignMask) >> double.SignShift != 0;
            int exp = (int)((doubleInt & double.BiasedExponentMask) >> double.BiasedExponentShift);
            ulong sig = doubleInt & double.TrailingSignificandMask;

            if (exp == DoubleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, sig << 12); // Shift the significand bits to the left end
                }
                return sign ? NegativeInfinity : PositiveInfinity;
            }

            uint sigHalf = (uint)ShiftRightJam(sig, 38);
            if ((exp | (int)sigHalf) == 0)
            {
                return new Half(sign, 0, 0);
            }
            return new Half(RoundPackToHalf(sign, (short)(exp - 0x3F1), (ushort)(sigHalf | 0x4000)));
        }

        /// <summary>Explicitly converts a <see cref="short" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(short value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="int" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(int value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="long" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(long value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="System.IntPtr" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(nint value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static explicit operator Half(float value)
        {
            const int SingleMaxExponent = 0xFF;

            uint floatInt = BitConverter.SingleToUInt32Bits(value);
            bool sign = (floatInt & float.SignMask) >> float.SignShift != 0;
            int exp = (int)(floatInt & float.BiasedExponentMask) >> float.BiasedExponentShift;
            uint sig = floatInt & float.TrailingSignificandMask;

            if (exp == SingleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, (ulong)sig << 41); // Shift the significand bits to the left end
                }
                return sign ? NegativeInfinity : PositiveInfinity;
            }

            uint sigHalf = sig >> 9 | ((sig & 0x1FFU) != 0 ? 1U : 0U); // RightShiftJam

            if ((exp | (int)sigHalf) == 0)
            {
                return new Half(sign, 0, 0);
            }

            return new Half(RoundPackToHalf(sign, (short)(exp - 0x71), (ushort)(sigHalf | 0x4000)));
        }

        /// <summary>Explicitly converts a <see cref="ushort" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Half(ushort value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="uint" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Half(uint value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="ulong" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Half(ulong value) => (Half)(float)value;

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Half(nuint value) => (Half)(float)value;

        //
        // Explicit Convert From Half
        //

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        public static explicit operator byte(Half value) => (byte)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        public static explicit operator checked byte(Half value) => checked((byte)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        public static explicit operator char(Half value) => (char)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        public static explicit operator checked char(Half value) => checked((char)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        public static explicit operator decimal(Half value) => (decimal)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        public static explicit operator short(Half value) => (short)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        public static explicit operator checked short(Half value) => checked((short)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        public static explicit operator int(Half value) => (int)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        public static explicit operator checked int(Half value) => checked((int)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        public static explicit operator long(Half value) => (long)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        public static explicit operator checked long(Half value) => checked((long)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="Int128"/>.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        public static explicit operator Int128(Half value) => (Int128)(double)(value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="Int128"/>, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit signed integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        public static explicit operator checked Int128(Half value) => checked((Int128)(double)(value));

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        public static explicit operator nint(Half value) => (nint)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="IntPtr" />.</exception>
        public static explicit operator checked nint(Half value) => checked((nint)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(Half value) => (sbyte)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(Half value) => checked((sbyte)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(Half value) => (ushort)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(Half value) => checked((ushort)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(Half value) => (uint)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(Half value) => checked((uint)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(Half value) => (ulong)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(Half value) => checked((ulong)(float)value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="UInt128"/>.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(Half value) => (UInt128)(double)(value);

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="UInt128"/>, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a 128-bit unsigned integer.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(Half value) => checked((UInt128)(double)(value));

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(Half value) => (nuint)(float)value;

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UIntPtr" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(Half value) => checked((nuint)(float)value);

        //
        // Implicit Convert To Half
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        public static implicit operator Half(byte value) => (Half)(float)value;

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable half-precision floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable half-precision floating-point value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Half(sbyte value) => (Half)(float)value;

        //
        // Implicit Convert From Half (actually explicit due to back-compat)
        //

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
        public static explicit operator double(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.BiasedExponent;
            uint sig = value.TrailingSignificand;

            if (exp == MaxBiasedExponent)
            {
                if (sig != 0)
                {
                    return CreateDoubleNaN(sign, (ulong)sig << 54);
                }
                return sign ? double.NegativeInfinity : double.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return BitConverter.UInt64BitsToDouble(sign ? double.SignMask : 0); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateDouble(sign, (ushort)(exp + 0x3F0), (ulong)sig << 42);
        }

        /// <summary>Explicitly converts a half-precision floating-point value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        public static explicit operator float(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.BiasedExponent;
            uint sig = value.TrailingSignificand;

            if (exp == MaxBiasedExponent)
            {
                if (sig != 0)
                {
                    return CreateSingleNaN(sign, (ulong)sig << 54);
                }
                return sign ? float.NegativeInfinity : float.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return BitConverter.UInt32BitsToSingle(sign ? float.SignMask : 0); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateSingle(sign, (byte)(exp + 0x70), sig << 13);
        }

        // IEEE 754 specifies NaNs to be propagated
        internal static Half Negate(Half value)
        {
            return IsNaN(value) ? value : new Half((ushort)(value._value ^ SignMask));
        }

        private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
        {
            int shiftDist = BitOperations.LeadingZeroCount(sig) - 16 - 5;
            return (1 - shiftDist, sig << shiftDist);
        }

        #region Utilities

        // Significand bits should be shifted towards to the left end before calling these methods
        // Creates Quiet NaN if significand == 0
        private static Half CreateHalfNaN(bool sign, ulong significand)
        {
            const uint NaNBits = BiasedExponentMask | 0x200; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << SignShift;
            uint sigInt = (uint)(significand >> 54);

            return BitConverter.UInt16BitsToHalf((ushort)(signInt | NaNBits | sigInt));
        }

        private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
        {
            const int RoundIncrement = 0x8; // Depends on rounding mode but it's always towards closest / ties to even
            int roundBits = sig & 0xF;

            if ((uint)exp >= 0x1D)
            {
                if (exp < 0)
                {
                    sig = (ushort)ShiftRightJam(sig, -exp);
                    exp = 0;
                    roundBits = sig & 0xF;
                }
                else if (exp > 0x1D || sig + RoundIncrement >= 0x8000) // Overflow
                {
                    return sign ? NegativeInfinityBits : PositiveInfinityBits;
                }
            }

            sig = (ushort)((sig + RoundIncrement) >> 4);
            sig &= (ushort)~(((roundBits ^ 8) != 0 ? 0 : 1) & 1);

            if (sig == 0)
            {
                exp = 0;
            }

            return new Half(sign, (ushort)exp, sig)._value;
        }

        // If any bits are lost by shifting, "jam" them into the LSB.
        // if dist > bit count, Will be 1 or 0 depending on i
        // (unlike bitwise operators that masks the lower 5 bits)
        private static uint ShiftRightJam(uint i, int dist) => dist < 31 ? (i >> dist) | (i << (-dist & 31) != 0 ? 1U : 0U) : (i != 0 ? 1U : 0U);

        private static ulong ShiftRightJam(ulong l, int dist) => dist < 63 ? (l >> dist) | (l << (-dist & 63) != 0 ? 1UL : 0UL) : (l != 0 ? 1UL : 0UL);

        private static float CreateSingleNaN(bool sign, ulong significand)
        {
            const uint NaNBits = float.BiasedExponentMask | 0x400000; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << float.SignShift;
            uint sigInt = (uint)(significand >> 41);

            return BitConverter.UInt32BitsToSingle(signInt | NaNBits | sigInt);
        }

        private static double CreateDoubleNaN(bool sign, ulong significand)
        {
            const ulong NaNBits = double.BiasedExponentMask | 0x80000_00000000; // Most significant significand bit

            ulong signInt = (sign ? 1UL : 0UL) << double.SignShift;
            ulong sigInt = significand >> 12;

            return BitConverter.UInt64BitsToDouble(signInt | NaNBits | sigInt);
        }

        private static float CreateSingle(bool sign, byte exp, uint sig) => BitConverter.UInt32BitsToSingle(((sign ? 1U : 0U) << float.SignShift) + ((uint)exp << float.BiasedExponentShift) + sig);

        private static double CreateDouble(bool sign, ushort exp, ulong sig) => BitConverter.UInt64BitsToDouble(((sign ? 1UL : 0UL) << double.SignShift) + ((ulong)exp << double.BiasedExponentShift) + sig);

        #endregion

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static Half operator +(Half left, Half right) => (Half)((float)left + (float)right);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Half IAdditiveIdentity<Half, Half>.AdditiveIdentity => new Half(PositiveZeroBits);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static Half IBinaryNumber<Half>.AllBitsSet => BitConverter.UInt16BitsToHalf(0xFFFF);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(Half value)
        {
            ushort bits = BitConverter.HalfToUInt16Bits(value);

            byte biasedExponent = ExtractBiasedExponentFromBits(bits);
            ushort trailingSignificand = ExtractTrailingSignificandFromBits(bits);

            return (value > Zero)
                && (biasedExponent != MinBiasedExponent) && (biasedExponent != MaxBiasedExponent)
                && (trailingSignificand == MinTrailingSignificand);
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static Half Log2(Half value) => (Half)MathF.Log2((float)value);

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator &(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) & BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator |(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) | BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator ^(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) ^ BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static Half IBitwiseOperators<Half, Half, Half>.operator ~(Half value)
        {
            ushort bits = (ushort)(~BitConverter.HalfToUInt16Bits(value));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static Half operator --(Half value)
        {
            var tmp = (float)value;
            --tmp;
            return (Half)tmp;
        }

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static Half operator /(Half left, Half right) => (Half)((float)left / (float)right);

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static Half Exp(Half x) => (Half)MathF.Exp((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static Half ExpM1(Half x) => (Half)float.ExpM1((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static Half Exp2(Half x) => (Half)float.Exp2((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static Half Exp2M1(Half x) => (Half)float.Exp2M1((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static Half Exp10(Half x) => (Half)float.Exp10((float)x);

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static Half Exp10M1(Half x) => (Half)float.Exp10M1((float)x);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Half Ceiling(Half x) => (Half)MathF.Ceiling((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Half Floor(Half x) => (Half)MathF.Floor((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Half Round(Half x) => (Half)MathF.Round((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Half Round(Half x, int digits) => (Half)MathF.Round((float)x, digits);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Half Round(Half x, MidpointRounding mode) => (Half)MathF.Round((float)x, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Half Round(Half x, int digits, MidpointRounding mode) => (Half)MathF.Round((float)x, digits, mode);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Half Truncate(Half x) => (Half)MathF.Truncate((float)x);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Half>.GetExponentByteCount() => sizeof(sbyte);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<Half>.GetExponentShortestBitLength()
        {
            sbyte exponent = Exponent;

            if (exponent >= 0)
            {
                return (sizeof(sbyte) * 8) - sbyte.LeadingZeroCount(exponent);
            }
            else
            {
                return (sizeof(sbyte) * 8) + 1 - sbyte.LeadingZeroCount((sbyte)(~exponent));
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Half>.GetSignificandByteCount() => sizeof(ushort);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<Half>.GetSignificandBitLength() => 11;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(sbyte))
            {
                sbyte exponent = Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(sbyte);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort significand = Significand;

                if (BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ushort);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Half>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(ushort))
            {
                ushort significand = Significand;

                if (!BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(ushort);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        public static Half E => new Half(EBits);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        public static Half Pi => new Half(PiBits);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        public static Half Tau => new Half(TauBits);

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static Half NegativeZero => new Half(NegativeZeroBits);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static Half Atan2(Half y, Half x) => (Half)MathF.Atan2((float)y, (float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static Half Atan2Pi(Half y, Half x) => (Half)float.Atan2Pi((float)y, (float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Half BitDecrement(Half x) => (Half)MathF.BitDecrement((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Half BitIncrement(Half x) => (Half)MathF.BitIncrement((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Half FusedMultiplyAdd(Half left, Half right, Half addend) => (Half)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Half Ieee754Remainder(Half left, Half right) => (Half)MathF.IEEERemainder((float)left, (float)right);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Half x) => MathF.ILogB((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static Half ReciprocalEstimate(Half x) => (Half)MathF.ReciprocalEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static Half ReciprocalSqrtEstimate(Half x) => (Half)MathF.ReciprocalSqrtEstimate((float)x);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Half ScaleB(Half x, int n) => (Half)MathF.ScaleB((float)x, n);

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static Half Compound(Half x, Half n) => (Half)MathF.Compound((float)x, (float)n);

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Half Acosh(Half x) => (Half)MathF.Acosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Half Asinh(Half x) => (Half)MathF.Asinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Half Atanh(Half x) => (Half)MathF.Atanh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Half Cosh(Half x) => (Half)MathF.Cosh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Half Sinh(Half x) => (Half)MathF.Sinh((float)x);

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Half Tanh(Half x) => (Half)MathF.Tanh((float)x);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static Half operator ++(Half value)
        {
            var tmp = (float)value;
            ++tmp;
            return (Half)tmp;
        }

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Half Log(Half x) => (Half)MathF.Log((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Half Log(Half x, Half newBase) => (Half)MathF.Log((float)x, (float)newBase);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Half Log10(Half x) => (Half)MathF.Log10((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static Half LogP1(Half x) => (Half)float.LogP1((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static Half Log2P1(Half x) => (Half)float.Log2P1((float)x);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static Half Log10P1(Half x) => (Half)float.Log10P1((float)x);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static Half operator %(Half left, Half right) => (Half)((float)left % (float)right);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        public static Half MultiplicativeIdentity => new Half(PositiveOneBits);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static Half operator *(Half left, Half right) => (Half)((float)left * (float)right);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static Half Clamp(Half value, Half min, Half max) => (Half)Math.Clamp((float)value, (float)min, (float)max);

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static Half CopySign(Half value, Half sign) => (Half)MathF.CopySign((float)value, (float)sign);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static Half Max(Half x, Half y) => (Half)MathF.Max((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static Half MaxNumber(Half x, Half y)
        {
            // This matches the IEEE 754:2019 `maximumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return y < x ? x : y;
                }

                return x;
            }

            return IsNegative(y) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static Half Min(Half x, Half y) => (Half)MathF.Min((float)x, (float)y);

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static Half MinNumber(Half x, Half y)
        {
            // This matches the IEEE 754:2019 `minimumNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the larger of the inputs. It
            // treats +0 as larger than -0 as per the specification.

            if (x != y)
            {
                if (!IsNaN(y))
                {
                    return x < y ? x : y;
                }

                return x;
            }

            return IsNegative(x) ? x : y;
        }

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(Half value) => MathF.Sign((float)value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        public static Half One => new Half(PositiveOneBits);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Half>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        public static Half Zero => new Half(PositiveZeroBits);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static Half Abs(Half value) => (Half)MathF.Abs((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Half result;

            if (typeof(TOther) == typeof(Half))
            {
                result = (Half)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Half result;

            if (typeof(TOther) == typeof(Half))
            {
                result = (Half)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Half result;

            if (typeof(TOther) == typeof(Half))
            {
                result = (Half)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Half>.IsCanonical(Half value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Half>.IsComplexNumber(Half value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(Half value) => float.IsEvenInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Half>.IsImaginaryNumber(Half value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(Half value) => float.IsInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(Half value) => float.IsOddInteger((float)value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(Half value) => (short)(value._value) >= 0;

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(Half value)
        {
            // A NaN will never equal itself so this is an
            // easy and efficient way to check for a real number.

#pragma warning disable CS1718
            return value == value;
#pragma warning restore CS1718
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Half>.IsZero(Half value) => (value == Zero);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static Half MaxMagnitude(Half x, Half y) => (Half)MathF.MaxMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static Half MaxMagnitudeNumber(Half x, Half y)
        {
            // This matches the IEEE 754:2019 `maximumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            Half ax = Abs(x);
            Half ay = Abs(y);

            if ((ax > ay) || IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? y : x;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static Half MinMagnitude(Half x, Half y) => (Half)MathF.MinMagnitude((float)x, (float)y);

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static Half MinMagnitudeNumber(Half x, Half y)
        {
            // This matches the IEEE 754:2019 `minimumMagnitudeNumber` function
            //
            // It does not propagate NaN inputs back to the caller and
            // otherwise returns the input with a larger magnitude.
            // It treats +0 as larger than -0 as per the specification.

            Half ax = Abs(x);
            Half ay = Abs(y);

            if ((ax < ay) || IsNaN(ay))
            {
                return x;
            }

            if (ax == ay)
            {
                return IsNegative(x) ? x : y;
            }

            return y;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertFromChecked<TOther>(TOther value, out Half result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertFromSaturating<TOther>(TOther value, out Half result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertFromTruncating<TOther>(TOther value, out Half result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out Half result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `Half` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = (Half)actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertToChecked<TOther>(Half value, [NotNullWhen(true)] out TOther result)
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `Half` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types.

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default!;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertToSaturating<TOther>(Half value, [NotNullWhen(true)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Half>.TryConvertToTruncating<TOther>(Half value, [NotNullWhen(true)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        private static bool TryConvertTo<TOther>(Half value, [NotNullWhen(true)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // In order to reduce overall code duplication and improve the inlinabilty of these
            // methods for the corelib types we have `ConvertFrom` handle the same sign and
            // `ConvertTo` handle the opposite sign. However, since there is an uneven split
            // between signed and unsigned types, the one that handles unsigned will also
            // handle `Decimal`.
            //
            // That is, `ConvertFrom` for `Half` will handle the other signed types and
            // `ConvertTo` will handle the unsigned types

            if (typeof(TOther) == typeof(byte))
            {
                var actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
                                   (value <= byte.MinValue) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value == PositiveInfinity) ? char.MaxValue :
                                    (value <= Zero) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value == PositiveInfinity) ? decimal.MaxValue :
                                       (value == NegativeInfinity) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value == PositiveInfinity) ? ushort.MaxValue :
                                      (value <= Zero) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value == PositiveInfinity) ? uint.MaxValue :
                                    (value <= Zero) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value == PositiveInfinity) ? ulong.MaxValue :
                                     (value <= Zero) ? ulong.MinValue :
                                     IsNaN(value) ? 0 : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value == PositiveInfinity) ? UInt128.MaxValue :
                                       (value <= Zero) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = (value == PositiveInfinity) ? nuint.MaxValue :
                                     (value <= Zero) ? nuint.MinValue : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default!;
                return false;
            }
        }

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Half result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Half Pow(Half x, Half y) => (Half)MathF.Pow((float)x, (float)y);

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Half Cbrt(Half x) => (Half)MathF.Cbrt((float)x);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static Half Hypot(Half x, Half y) => (Half)float.Hypot((float)x, (float)y);

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static Half RootN(Half x, int n) => (Half)float.RootN((float)x, n);

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Half Sqrt(Half x) => (Half)MathF.Sqrt((float)x);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        public static Half NegativeOne => new Half(NegativeOneBits);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Half Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, DefaultParseStyle, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Half result) => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static Half operator -(Half left, Half right) => (Half)((float)left - (float)right);

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Half Acos(Half x) => (Half)MathF.Acos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static Half AcosPi(Half x) => (Half)float.AcosPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Half Asin(Half x) => (Half)MathF.Asin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static Half AsinPi(Half x) => (Half)float.AsinPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Half Atan(Half x) => (Half)MathF.Atan((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static Half AtanPi(Half x) => (Half)float.AtanPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Half Cos(Half x) => (Half)MathF.Cos((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static Half CosPi(Half x) => (Half)float.CosPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Half Sin(Half x) => (Half)MathF.Sin((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Half Sin, Half Cos) SinCos(Half x)
        {
            var (sin, cos) = MathF.SinCos((float)x);
            return ((Half)sin, (Half)cos);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (Half SinPi, Half CosPi) SinCosPi(Half x)
        {
            var (sinPi, cosPi) = float.SinCosPi((float)x);
            return ((Half)sinPi, (Half)cosPi);
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static Half SinPi(Half x) => (Half)float.SinPi((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Half Tan(Half x) => (Half)MathF.Tan((float)x);

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static Half TanPi(Half x) => (Half)float.TanPi((float)x);

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static Half operator -(Half value) => (Half)(-(float)value);

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static Half operator +(Half value) => value;
    }
}

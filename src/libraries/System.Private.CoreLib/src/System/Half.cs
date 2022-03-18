// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

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
          IBinaryFloatingPoint<Half>,
          IMinMaxValue<Half>
    {
        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        // Constants for manipulating the private bit-representation

        private const ushort SignMask = 0x8000;
        private const ushort SignShift = 15;

        private const ushort ExponentMask = 0x7C00;
        private const ushort ExponentShift = 10;
        private const ushort ShiftedExponentMask = ExponentMask >> ExponentShift;

        private const ushort SignificandMask = 0x03FF;
        private const ushort SignificandShift = 0;

        private const ushort MinSign = 0;
        private const ushort MaxSign = 1;

        private const ushort MinExponent = 0x00;
        private const ushort MaxExponent = 0x1F;

        private const ushort MinSignificand = 0x0000;
        private const ushort MaxSignificand = 0x03FF;

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

        // Well-defined and commonly used values

        public static Half Epsilon => new Half(EpsilonBits);                        //  5.9604645E-08

        public static Half PositiveInfinity => new Half(PositiveInfinityBits);      //  1.0 / 0.0;

        public static Half NegativeInfinity => new Half(NegativeInfinityBits);      // -1.0 / 0.0

        public static Half NaN => new Half(NegativeQNaNBits);                       //  0.0 / 0.0

        public static Half MinValue => new Half(MinValueBits);                      // -65504

        public static Half MaxValue => new Half(MaxValueBits);                      //  65504

        // We use these explicit definitions to avoid the confusion between 0.0 and -0.0.
        private static readonly Half PositiveZero = new Half(PositiveZeroBits);            //  0.0
        private static readonly Half NegativeZero = new Half(NegativeZeroBits);            // -0.0

        private readonly ushort _value;

        internal Half(ushort value)
        {
            _value = value;
        }

        private Half(bool sign, ushort exp, ushort sig)
            => _value = (ushort)(((sign ? 1 : 0) << SignShift) + (exp << ExponentShift) + sig);

        private sbyte Exponent
        {
            get
            {
                return (sbyte)((_value & ExponentMask) >> ExponentShift);
            }
        }

        private ushort Significand
        {
            get
            {
                return (ushort)((_value & SignificandMask) >> SignificandShift);
            }
        }

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

        public static bool operator >(Half left, Half right)
        {
            return right < left;
        }

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

        public static bool operator >=(Half left, Half right)
        {
            return right <= left;
        }

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
                && ((absValue & ExponentMask) != 0);    // is not subnormal (has a non-zero exponent)
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
                && ((absValue & ExponentMask) == 0);    // is subnormal (has a zero exponent)
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
        public string ToString(string? format)
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
        public string ToString(string? format, IFormatProvider? provider)
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
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        // -----------------------Start of to-half conversions-------------------------

        public static explicit operator Half(float value)
        {
            const int SingleMaxExponent = 0xFF;

            uint floatInt = BitConverter.SingleToUInt32Bits(value);
            bool sign = (floatInt & float.SignMask) >> float.SignShift != 0;
            int exp = (int)(floatInt & float.ExponentMask) >> float.ExponentShift;
            uint sig = floatInt & float.SignificandMask;

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

        public static explicit operator Half(double value)
        {
            const int DoubleMaxExponent = 0x7FF;

            ulong doubleInt = BitConverter.DoubleToUInt64Bits(value);
            bool sign = (doubleInt & double.SignMask) >> double.SignShift != 0;
            int exp = (int)((doubleInt & double.ExponentMask) >> double.ExponentShift);
            ulong sig = doubleInt & double.SignificandMask;

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

        // -----------------------Start of from-half conversions-------------------------
        public static explicit operator float(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.Exponent;
            uint sig = value.Significand;

            if (exp == MaxExponent)
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

        public static explicit operator double(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.Exponent;
            uint sig = value.Significand;

            if (exp == MaxExponent)
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
            const uint NaNBits = ExponentMask | 0x200; // Most significant significand bit

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
        private static uint ShiftRightJam(uint i, int dist)
            => dist < 31 ? (i >> dist) | (i << (-dist & 31) != 0 ? 1U : 0U) : (i != 0 ? 1U : 0U);

        private static ulong ShiftRightJam(ulong l, int dist)
            => dist < 63 ? (l >> dist) | (l << (-dist & 63) != 0 ? 1UL : 0UL) : (l != 0 ? 1UL : 0UL);

        private static float CreateSingleNaN(bool sign, ulong significand)
        {
            const uint NaNBits = float.ExponentMask | 0x400000; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << float.SignShift;
            uint sigInt = (uint)(significand >> 41);

            return BitConverter.UInt32BitsToSingle(signInt | NaNBits | sigInt);
        }

        private static double CreateDoubleNaN(bool sign, ulong significand)
        {
            const ulong NaNBits = double.ExponentMask | 0x80000_00000000; // Most significant significand bit

            ulong signInt = (sign ? 1UL : 0UL) << double.SignShift;
            ulong sigInt = significand >> 12;

            return BitConverter.UInt64BitsToDouble(signInt | NaNBits | sigInt);
        }

        private static float CreateSingle(bool sign, byte exp, uint sig)
            => BitConverter.UInt32BitsToSingle(((sign ? 1U : 0U) << float.SignShift) + ((uint)exp << float.ExponentShift) + sig);

        private static double CreateDouble(bool sign, ushort exp, ulong sig)
            => BitConverter.UInt64BitsToDouble(((sign ? 1UL : 0UL) << double.SignShift) + ((ulong)exp << double.ExponentShift) + sig);

        #endregion

        //
        // IAdditionOperators
        //

        static Half IAdditionOperators<Half, Half, Half>.operator +(Half left, Half right)
            => (Half)((float)left + (float)right);

        // static checked Half IAdditionOperators<Half, Half, Half>.operator +(Half left, Half right)
        //     => checked((Half)((float)left + (float)right));

        //
        // IAdditiveIdentity
        //

        static Half IAdditiveIdentity<Half, Half>.AdditiveIdentity => PositiveZero;

        //
        // IBinaryNumber
        //

        static bool IBinaryNumber<Half>.IsPow2(Half value)
        {
            uint bits = BitConverter.HalfToUInt16Bits(value);

            uint exponent = (bits >> ExponentShift) & ShiftedExponentMask;
            uint significand = bits & SignificandMask;

            return (value > PositiveZero)
                && (exponent != MinExponent) && (exponent != MaxExponent)
                && (significand == MinSignificand);
        }

        static Half IBinaryNumber<Half>.Log2(Half value)
            => (Half)MathF.Log2((float)value);

        //
        // IBitwiseOperators
        //

        static Half IBitwiseOperators<Half, Half, Half>.operator &(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) & BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        static Half IBitwiseOperators<Half, Half, Half>.operator |(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) | BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        static Half IBitwiseOperators<Half, Half, Half>.operator ^(Half left, Half right)
        {
            ushort bits = (ushort)(BitConverter.HalfToUInt16Bits(left) ^ BitConverter.HalfToUInt16Bits(right));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        static Half IBitwiseOperators<Half, Half, Half>.operator ~(Half value)
        {
            ushort bits = (ushort)(~BitConverter.HalfToUInt16Bits(value));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        //
        // IComparisonOperators
        //

        static bool IComparisonOperators<Half, Half>.operator <(Half left, Half right)
            => left < right;

        static bool IComparisonOperators<Half, Half>.operator <=(Half left, Half right)
            => left <= right;

        static bool IComparisonOperators<Half, Half>.operator >(Half left, Half right)
            => left > right;

        static bool IComparisonOperators<Half, Half>.operator >=(Half left, Half right)
            => left >= right;

        //
        // IDecrementOperators
        //

        static Half IDecrementOperators<Half>.operator --(Half value)
        {
            var tmp = (float)value;
            --tmp;
            return (Half)tmp;
        }

        // static checked Half IDecrementOperators<Half>.operator --(Half value)
        // {
        //     var tmp = (float)value;
        //     --tmp;
        //     return (Half)tmp;
        // }

        //
        // IEqualityOperators
        //

        static bool IEqualityOperators<Half, Half>.operator ==(Half left, Half right)
            => left == right;

        static bool IEqualityOperators<Half, Half>.operator !=(Half left, Half right)
            => left != right;

        //
        // IDivisionOperators
        //

        static Half IDivisionOperators<Half, Half, Half>.operator /(Half left, Half right)
            => (Half)((float)left / (float)right);

        // static checked Half IDivisionOperators<Half, Half, Half>.operator /(Half left, Half right)
        //     => checked((Half)((float)left / (float)right));

        //
        // IFloatingPoint
        //

        static Half IFloatingPoint<Half>.E => (Half)MathF.E;

        static Half IFloatingPoint<Half>.Epsilon => Epsilon;

        static Half IFloatingPoint<Half>.NaN => NaN;

        static Half IFloatingPoint<Half>.NegativeInfinity => NegativeInfinity;

        static Half IFloatingPoint<Half>.NegativeZero => NegativeZero;

        static Half IFloatingPoint<Half>.Pi => (Half)MathF.PI;

        static Half IFloatingPoint<Half>.PositiveInfinity => PositiveInfinity;

        static Half IFloatingPoint<Half>.Tau => (Half)MathF.Tau;

        static Half IFloatingPoint<Half>.Acos(Half x)
            => (Half)MathF.Acos((float)x);

        static Half IFloatingPoint<Half>.Acosh(Half x)
            => (Half)MathF.Acosh((float)x);

        static Half IFloatingPoint<Half>.Asin(Half x)
            => (Half)MathF.Asin((float)x);

        static Half IFloatingPoint<Half>.Asinh(Half x)
            => (Half)MathF.Asinh((float)x);

        static Half IFloatingPoint<Half>.Atan(Half x)
            => (Half)MathF.Atan((float)x);

        static Half IFloatingPoint<Half>.Atan2(Half y, Half x)
            => (Half)MathF.Atan2((float)y, (float)x);

        static Half IFloatingPoint<Half>.Atanh(Half x)
            => (Half)MathF.Atanh((float)x);

        static Half IFloatingPoint<Half>.BitIncrement(Half x)
        {
            ushort bits = BitConverter.HalfToUInt16Bits(x);

            if ((bits & ExponentMask) >= ExponentMask)
            {
                // NaN returns NaN
                // -Infinity returns float.MinValue
                // +Infinity returns +Infinity
                return (bits == (ExponentMask | SignMask)) ? MinValue : x;
            }

            if (bits == NegativeZeroBits)
            {
                // -0.0 returns float.Epsilon
                return Epsilon;
            }

            // Negative values need to be decremented
            // Positive values need to be incremented

            bits += unchecked((ushort)((bits < 0) ? -1 : +1));
            return BitConverter.UInt16BitsToHalf(bits);
        }

        static Half IFloatingPoint<Half>.BitDecrement(Half x)
        {
            ushort bits = BitConverter.HalfToUInt16Bits(x);

            if ((bits & ExponentMask) >= ExponentMask)
            {
                // NaN returns NaN
                // -Infinity returns -Infinity
                // +Infinity returns float.MaxValue
                return (bits == ExponentMask) ? MaxValue : x;
            }

            if (bits == PositiveZeroBits)
            {
                // +0.0 returns -float.Epsilon
                return new Half(EpsilonBits | SignMask);
            }

            // Negative values need to be incremented
            // Positive values need to be decremented

            bits += (ushort)((bits < 0) ? +1 : -1);
            return BitConverter.UInt16BitsToHalf(bits);
        }

        static Half IFloatingPoint<Half>.Cbrt(Half x)
            => (Half)MathF.Cbrt((float)x);

        static Half IFloatingPoint<Half>.Ceiling(Half x)
            => (Half)MathF.Ceiling((float)x);

        static Half IFloatingPoint<Half>.CopySign(Half x, Half y)
            => (Half)MathF.CopySign((float)x, (float)y);

        static Half IFloatingPoint<Half>.Cos(Half x)
            => (Half)MathF.Cos((float)x);

        static Half IFloatingPoint<Half>.Cosh(Half x)
            => (Half)MathF.Cosh((float)x);

        static Half IFloatingPoint<Half>.Exp(Half x)
            => (Half)MathF.Exp((float)x);

        static Half IFloatingPoint<Half>.Floor(Half x)
            => (Half)MathF.Floor((float)x);

        static Half IFloatingPoint<Half>.FusedMultiplyAdd(Half left, Half right, Half addend)
            => (Half)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);

        static Half IFloatingPoint<Half>.IEEERemainder(Half left, Half right)
            => (Half)MathF.IEEERemainder((float)left, (float)right);

        static TInteger IFloatingPoint<Half>.ILogB<TInteger>(Half x)
            => TInteger.Create(MathF.ILogB((float)x));

        static Half IFloatingPoint<Half>.Log(Half x)
            => (Half)MathF.Log((float)x);

        static Half IFloatingPoint<Half>.Log(Half x, Half newBase)
            => (Half)MathF.Log((float)x, (float)newBase);

        static Half IFloatingPoint<Half>.Log2(Half x)
            => (Half)MathF.Log2((float)x);

        static Half IFloatingPoint<Half>.Log10(Half x)
            => (Half)MathF.Log10((float)x);

        static Half IFloatingPoint<Half>.MaxMagnitude(Half x, Half y)
            => (Half)MathF.MaxMagnitude((float)x, (float)y);

        static Half IFloatingPoint<Half>.MinMagnitude(Half x, Half y)
            => (Half)MathF.MinMagnitude((float)x, (float)y);

        static Half IFloatingPoint<Half>.Pow(Half x, Half y)
            => (Half)MathF.Pow((float)x, (float)y);

        static Half IFloatingPoint<Half>.Round(Half x)
            => (Half)MathF.Round((float)x);

        static Half IFloatingPoint<Half>.Round<TInteger>(Half x, TInteger digits)
            => (Half)MathF.Round((float)x, int.Create(digits));

        static Half IFloatingPoint<Half>.Round(Half x, MidpointRounding mode)
            => (Half)MathF.Round((float)x, mode);

        static Half IFloatingPoint<Half>.Round<TInteger>(Half x, TInteger digits, MidpointRounding mode)
            => (Half)MathF.Round((float)x, int.Create(digits), mode);

        static Half IFloatingPoint<Half>.ScaleB<TInteger>(Half x, TInteger n)
            => (Half)MathF.ScaleB((float)x, int.Create(n));

        static Half IFloatingPoint<Half>.Sin(Half x)
            => (Half)MathF.Sin((float)x);

        static Half IFloatingPoint<Half>.Sinh(Half x)
            => (Half)MathF.Sinh((float)x);

        static Half IFloatingPoint<Half>.Sqrt(Half x)
            => (Half)MathF.Sqrt((float)x);

        static Half IFloatingPoint<Half>.Tan(Half x)
            => (Half)MathF.Tan((float)x);

        static Half IFloatingPoint<Half>.Tanh(Half x)
            => (Half)MathF.Tanh((float)x);

        static Half IFloatingPoint<Half>.Truncate(Half x)
            => (Half)MathF.Truncate((float)x);

        static bool IFloatingPoint<Half>.IsFinite(Half x) => IsFinite(x);

        static bool IFloatingPoint<Half>.IsInfinity(Half x) => IsInfinity(x);

        static bool IFloatingPoint<Half>.IsNaN(Half x) => IsNaN(x);

        static bool IFloatingPoint<Half>.IsNegative(Half x) => IsNegative(x);

        static bool IFloatingPoint<Half>.IsNegativeInfinity(Half x) => IsNegativeInfinity(x);

        static bool IFloatingPoint<Half>.IsNormal(Half x) => IsNormal(x);

        static bool IFloatingPoint<Half>.IsPositiveInfinity(Half x) => IsPositiveInfinity(x);

        static bool IFloatingPoint<Half>.IsSubnormal(Half x) => IsSubnormal(x);


        // static Half IFloatingPoint<Half>.AcosPi(Half x)
        //     => (Half)MathF.AcosPi((float)x);
        //
        // static Half IFloatingPoint<Half>.AsinPi(Half x)
        //     => (Half)MathF.AsinPi((float)x);
        //
        // static Half IFloatingPoint<Half>.AtanPi(Half x)
        //     => (Half)MathF.AtanPi((float)x);
        //
        // static Half IFloatingPoint<Half>.Atan2Pi(Half y, Half x)
        //     => (Half)MathF.Atan2Pi((float)y, (float)x);
        //
        // static Half IFloatingPoint<Half>.Compound(Half x, Half n)
        //     => (Half)MathF.Compound((float)x, (float)n);
        //
        // static Half IFloatingPoint<Half>.CosPi(Half x)
        //     => (Half)MathF.CosPi((float)x);
        //
        // static Half IFloatingPoint<Half>.ExpM1(Half x)
        //     => (Half)MathF.ExpM1((float)x);
        //
        // static Half IFloatingPoint<Half>.Exp2(Half x)
        //     => (Half)MathF.Exp2((float)x);
        //
        // static Half IFloatingPoint<Half>.Exp2M1(Half x)
        //     => (Half)MathF.Exp2M1((float)x);
        //
        // static Half IFloatingPoint<Half>.Exp10(Half x)
        //     => (Half)MathF.Exp10((float)x);
        //
        // static Half IFloatingPoint<Half>.Exp10M1(Half x)
        //     => (Half)MathF.Exp10M1((float)x);
        //
        // static Half IFloatingPoint<Half>.Hypot(Half x, Half y)
        //     => (Half)MathF.Hypot((float)x, (float)y);
        //
        // static Half IFloatingPoint<Half>.LogP1(Half x)
        //     => (Half)MathF.LogP1((float)x);
        //
        // static Half IFloatingPoint<Half>.Log2P1(Half x)
        //     => (Half)MathF.Log2P1((float)x);
        //
        // static Half IFloatingPoint<Half>.Log10P1(Half x)
        //     => (Half)MathF.Log10P1((float)x);
        //
        // static Half IFloatingPoint<Half>.MaxMagnitudeNumber(Half x, Half y)
        //     => (Half)MathF.MaxMagnitudeNumber((float)x, (float)y);
        //
        // static Half IFloatingPoint<Half>.MaxNumber(Half x, Half y)
        //     => (Half)MathF.MaxNumber((float)x, (float)y);
        //
        // static Half IFloatingPoint<Half>.MinMagnitudeNumber(Half x, Half y)
        //     => (Half)MathF.MinMagnitudeNumber((float)x, (float)y);
        //
        // static Half IFloatingPoint<Half>.MinNumber(Half x, Half y)
        //     => (Half)MathF.MinNumber((float)x, (float)y);
        //
        // static Half IFloatingPoint<Half>.Root(Half x, Half n)
        //     => (Half)MathF.Root((float)x, (float)n);
        //
        // static Half IFloatingPoint<Half>.SinPi(Half x)
        //     => (Half)MathF.SinPi((float)x, (float)y);
        //
        // static Half TanPi(Half x)
        //     => (Half)MathF.TanPi((float)x, (float)y);

        //
        // IIncrementOperators
        //

        static Half IIncrementOperators<Half>.operator ++(Half value)
        {
            var tmp = (float)value;
            ++tmp;
            return (Half)tmp;
        }

        // static checked Half IIncrementOperators<Half>.operator ++(Half value)
        // {
        //     var tmp = (float)value;
        //     ++tmp;
        //     return (Half)tmp;
        // }

        //
        // IMinMaxValue
        //

        static Half IMinMaxValue<Half>.MinValue => MinValue;

        static Half IMinMaxValue<Half>.MaxValue => MaxValue;

        //
        // IModulusOperators
        //

        static Half IModulusOperators<Half, Half, Half>.operator %(Half left, Half right)
            => (Half)((float)left % (float)right);

        // static checked Half IModulusOperators<Half, Half, Half>.operator %(Half left, Half right)
        //     => checked((Half)((float)left % (float)right));

        //
        // IMultiplicativeIdentity
        //

        static Half IMultiplicativeIdentity<Half, Half>.MultiplicativeIdentity => (Half)1.0f;

        //
        // IMultiplyOperators
        //

        static Half IMultiplyOperators<Half, Half, Half>.operator *(Half left, Half right)
            => (Half)((float)left * (float)right);

        // static checked Half IMultiplyOperators<Half, Half, Half>.operator *(Half left, Half right)
        //     => checked((Half)((float)left * (float)right));

        //
        // INumber
        //

        static Half INumber<Half>.One => (Half)1.0f;

        static Half INumber<Half>.Zero => PositiveZero;

        static Half INumber<Half>.Abs(Half value)
            => (Half)MathF.Abs((float)value);

        static Half INumber<Half>.Clamp(Half value, Half min, Half max)
            => (Half)Math.Clamp((float)value, (float)min, (float)max);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Half INumber<Half>.Create<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Half INumber<Half>.CreateSaturating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Half INumber<Half>.CreateTruncating<TOther>(TOther value)
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (Half)(byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (Half)(char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (Half)(float)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (Half)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (Half)(short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (Half)(int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (Half)(long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (Half)(long)(nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (Half)(sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (Half)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (Half)(ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (Half)(uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (Half)(ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (Half)(ulong)(nuint)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        static (Half Quotient, Half Remainder) INumber<Half>.DivRem(Half left, Half right)
            => ((Half, Half))((float)left / (float)right, (float)left % (float)right);

        static Half INumber<Half>.Max(Half x, Half y)
            => (Half)MathF.Max((float)x, (float)y);

        static Half INumber<Half>.Min(Half x, Half y)
            => (Half)MathF.Min((float)x, (float)y);

        static Half INumber<Half>.Parse(string s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static Half INumber<Half>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider)
            => Parse(s, style, provider);

        static Half INumber<Half>.Sign(Half value)
            => (Half)MathF.Sign((float)value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumber<Half>.TryCreate<TOther>(TOther value, out Half result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (Half)(byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (Half)(char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (Half)(float)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (Half)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (Half)(short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (Half)(int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (Half)(long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (Half)(long)(nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (Half)(sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (Half)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (Half)(ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (Half)(uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (Half)(ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (Half)(ulong)(nuint)(object)value;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        static bool INumber<Half>.TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Half result)
            => TryParse(s, style, provider, out result);

        static bool INumber<Half>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Half result)
            => TryParse(s, style, provider, out result);

        //
        // IParseable
        //

        static Half IParseable<Half>.Parse(string s, IFormatProvider? provider)
            => Parse(s, provider);

        static bool IParseable<Half>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Half result)
            => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // ISignedNumber
        //

        static Half ISignedNumber<Half>.NegativeOne => (Half)(-1.0f);

        //
        // ISpanParseable
        //

        static Half ISpanParseable<Half>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
            => Parse(s, DefaultParseStyle, provider);

        static bool ISpanParseable<Half>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Half result)
            => TryParse(s, DefaultParseStyle, provider, out result);

        //
        // ISubtractionOperators
        //

        static Half ISubtractionOperators<Half, Half, Half>.operator -(Half left, Half right)
            => (Half)((float)left - (float)right);

        // static checked Half ISubtractionOperators<Half, Half, Half>.operator -(Half left, Half right)
        //     => checked((Half)((float)left - (float)right));

        //
        // IUnaryNegationOperators
        //

        static Half IUnaryNegationOperators<Half, Half>.operator -(Half value)
            => (Half)(-(float)value);

        // static checked Half IUnaryNegationOperators<Half, Half>.operator -(Half value)
        //     => checked((Half)(-(float)value));

        //
        // IUnaryNegationOperators
        //

        static Half IUnaryPlusOperators<Half, Half>.operator +(Half value)
            => value;

        // static checked Half IUnaryPlusOperators<Half, Half>.operator +(Half value)
        //     => checked(value);
    }
}

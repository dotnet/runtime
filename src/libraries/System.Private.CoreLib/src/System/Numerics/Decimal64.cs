// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System.Numerics
{
    /// <summary>
    /// Represents a decimal floating-point number that uses the IEEE 754 <c>decimal64</c> interchange format, providing 16 decimal digits of precision.
    /// </summary>
    /// <remarks>The IEEE 754 standard defines two interchange encodings for decimal floating-point: binary integer decimal (BID) and densely packed decimal (DPD). Which encoding is used is determined by the underlying ABI for the platform and defaults to BID where the ABI does not otherwise specify.</remarks>
    public readonly struct Decimal64
        : IComparable,
          IComparable<Decimal64>,
          IEquatable<Decimal64>,
          IDecimalFloatingPointIeee754<Decimal64>,
          ISpanFormattable,
          ISpanParsable<Decimal64>,
          IMinMaxValue<Decimal64>,
          IUtf8SpanFormattable,
          IUtf8SpanParsable<Decimal64>,
          IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>
    {
        internal readonly ulong _value;

        private const int MaxExponent = 384;
        private const int MinExponent = -383;
        private const int Precision = 16;
        private const int ExponentBias = 398;
        private const ulong PositiveInfinityValue = 0x7800_0000_0000_0000;
        private const ulong NegativeInfinityValue = 0xF800_0000_0000_0000;
        // Canonical ±0 use the IEEE 754 preferred representation for integer values,
        // which stores zero with the biased exponent rather than the minimum exponent.
        private const ulong ZeroValue = 0x31C0_0000_0000_0000;
        private const ulong NegativeZeroValue = 0xB1C0_0000_0000_0000;
        // One (+1 * 10^0) shares the biased exponent of canonical zero with a coefficient of one.
        private const ulong OneValue = ZeroValue | 0x1;
        private const ulong NegativeOneValue = NegativeZeroValue | 0x1;
        // Mathematical constants correctly rounded to the format's precision (16 significant digits).
        private const ulong EValue = 0x2FE9_A843_4EC8_E225;   // +2.718281828459045
        private const ulong PiValue = 0x2FEB_2943_0A25_6D21;  // +3.141592653589793
        private const ulong TauValue = 0x2FF6_5286_144A_DA42; // +6.283185307179586
        private const ulong QuietNaNValue = 0xFC00_0000_0000_0000;
        private const ulong G0G1Mask = 0x6000_0000_0000_0000;
        private const ulong SignMask = 0x8000_0000_0000_0000;
        private const ulong MostSignificantBitOfSignificandMask = 0x0020_0000_0000_0000;
        private const ulong NaNMask = 0x7C00_0000_0000_0000;
        private const ulong InfinityMask = 0x7800_0000_0000_0000;
        private const ulong MaxSignificand = 9_999_999_999_999_999;
        private const ulong MaxInternalValue = 0x77FB_86F2_6FC0_FFFF; // 9.999_999_999_999_999 * 10^384; aka 9_999_999_999_999_999 * 10^369
        private const ulong MinInternalValue = 0xF7FB_86F2_6FC0_FFFF; // -9.999_999_999_999_999 * 10^384; aka -9_999_999_999_999_999 * 10^369

        /// <summary>Gets a value that represents positive <c>infinity</c>.</summary>
        public static Decimal64 PositiveInfinity => new Decimal64(PositiveInfinityValue);

        /// <summary>Gets a value that represents negative <c>infinity</c>.</summary>
        public static Decimal64 NegativeInfinity => new Decimal64(NegativeInfinityValue);

        /// <summary>Gets a value that represents <c>NaN</c>.</summary>
        public static Decimal64 NaN => new Decimal64(QuietNaNValue);

        /// <summary>Gets a value that represents negative <c>zero</c>.</summary>
        public static Decimal64 NegativeZero => new Decimal64(NegativeZeroValue);

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        public static Decimal64 Zero => new Decimal64(ZeroValue);

        /// <summary>Gets the minimum value of the current type.</summary>
        public static Decimal64 MinValue => new Decimal64(MinInternalValue);

        /// <summary>Gets the maximum value of the current type.</summary>
        public static Decimal64 MaxValue => new Decimal64(MaxInternalValue);

        /// <summary>Gets the smallest value such that can be added to <c>0</c> that does not result in <c>0</c>.</summary>
        public static Decimal64 Epsilon => new Decimal64(0x0000_0000_0000_0001); // Smallest positive subnormal value, aka 1 * 10^-398

        private static ReadOnlySpan<ulong> UInt64Powers10 =>
            [
                1,
                10,
                100,
                1000,
                10000,
                100000,
                1000000,
                10000000,
                100000000,
                1000000000,
                10000000000,
                100000000000,
                1000000000000,
                10000000000000,
                100000000000000,
                1000000000000000,
            ];

        internal Decimal64(ulong value)
        {
            _value = value;
        }

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal64 Parse(string s) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal64 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal64 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal64 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal64 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<char, Decimal64, ulong>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal64"/> value representing the input string. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal64 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal64 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal64 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s, style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input string if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value is not Decimal64 other)
            {
                return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeDecimal64);
            }
            return CompareTo(other);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, ulong>(_value, other._value);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal64 other)
        {
            return Number.CompareDecimalIeee754<Decimal64, ulong>(_value, other._value) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Decimal64 other && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return Number.GetDecimalIeee754HashCode<Decimal64, ulong>(_value);
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(_value, null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(_value, format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(_value, null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal64, ulong>(_value, format, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal64, ulong, char>(_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal64, ulong, byte>(_value, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute the unary plus.</param>
        /// <returns><paramref name="value" /> unchanged.</returns>
        public static Decimal64 operator +(Decimal64 value) => value;

        /// <summary>Computes the unary negation of a value.</summary>
        /// <param name="value">The value for which to compute the unary negation.</param>
        /// <returns>The unary negation of <paramref name="value" />.</returns>
        public static Decimal64 operator -(Decimal64 value) => new Decimal64(value._value ^ SignMask);

        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" /> by one.</returns>
        public static Decimal64 operator ++(Decimal64 value)
        {
            return new Decimal64(Number.AddDecimalIeee754<Decimal64, ulong>(value._value, OneValue));
        }

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" /> by one.</returns>
        public static Decimal64 operator --(Decimal64 value)
        {
            return new Decimal64(Number.SubtractDecimalIeee754<Decimal64, ulong>(value._value, OneValue));
        }

        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal64 operator +(Decimal64 left, Decimal64 right)
        {
            return new Decimal64(Number.AddDecimalIeee754<Decimal64, ulong>(left._value, right._value));
        }

        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        public static Decimal64 operator -(Decimal64 left, Decimal64 right)
        {
            return new Decimal64(Number.SubtractDecimalIeee754<Decimal64, ulong>(left._value, right._value));
        }

        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal64 operator *(Decimal64 left, Decimal64 right)
        {
            return new Decimal64(Number.MultiplyDecimalIeee754<Decimal64, ulong>(left._value, right._value));
        }

        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal64 operator /(Decimal64 left, Decimal64 right)
        {
            return new Decimal64(Number.DivideDecimalIeee754<Decimal64, ulong>(left._value, right._value));
        }

        /// <summary>Divides two values together to compute their remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal64 operator %(Decimal64 left, Decimal64 right)
        {
            return new Decimal64(Number.RemainderDecimalIeee754<Decimal64, ulong>(left._value, right._value));
        }

        //
        // Explicit conversions to Decimal64
        //

        /// <summary>Explicitly converts a <see cref="System.IntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(nint value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, nint>(value));

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal64(nuint value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, nuint>(value));

        /// <summary>Explicitly converts a <see cref="long" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(long value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, long>(value));

        /// <summary>Explicitly converts a <see cref="ulong" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal64(ulong value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, ulong>(value));

        /// <summary>Explicitly converts a <see cref="System.Int128" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(Int128 value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, Int128>(value));

        /// <summary>Explicitly converts a <see cref="System.UInt128" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal64(UInt128 value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, UInt128>(value));

        /// <summary>Explicitly converts a <see cref="System.Half" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(Half value) => new Decimal64(Number.ConvertFloatToDecimalIeee754<Half, Decimal64, ulong>(value));

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(float value) => new Decimal64(Number.ConvertFloatToDecimalIeee754<float, Decimal64, ulong>(value));

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(double value) => new Decimal64(Number.ConvertFloatToDecimalIeee754<double, Decimal64, ulong>(value));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(Decimal128 value) => new Decimal64(Number.ConvertDecimalIeee754<Decimal128, UInt128, Decimal64, ulong>(new UInt128(value._upper, value._lower)));

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static explicit operator Decimal64(decimal value) => new Decimal64(Number.ConvertDecimalToDecimalIeee754<Decimal64, ulong>(value));

        //
        // Explicit conversions from Decimal64
        //

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        public static explicit operator byte(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, byte>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        public static explicit operator checked byte(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, byte>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, sbyte>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, sbyte>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        public static explicit operator char(Decimal64 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ushort>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        public static explicit operator checked char(Decimal64 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ushort>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        public static explicit operator short(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, short>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        public static explicit operator checked short(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, short>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ushort>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ushort>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        public static explicit operator int(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, int>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        public static explicit operator checked int(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, int>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, uint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, uint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        public static explicit operator nint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, nint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.IntPtr" />.</exception>
        public static explicit operator checked nint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, nint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, nuint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UIntPtr" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, nuint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        public static explicit operator long(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, long>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        public static explicit operator checked long(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, long>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ulong>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, ulong>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        public static explicit operator Int128(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, Int128>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.Int128" />.</exception>
        public static explicit operator checked Int128(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, Int128>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, UInt128>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(Decimal64 value) => Number.ConvertDecimalIeee754ToInteger<Decimal64, ulong, UInt128>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Half" /> value.</returns>
        public static explicit operator Half(Decimal64 value) => Number.ConvertDecimalIeee754ToFloat<Decimal64, ulong, Half>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        public static explicit operator float(Decimal64 value) => Number.ConvertDecimalIeee754ToFloat<Decimal64, ulong, float>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
        public static explicit operator double(Decimal64 value) => Number.ConvertDecimalIeee754ToFloat<Decimal64, ulong, double>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="decimal" />.</exception>
        public static explicit operator decimal(Decimal64 value) => Number.ConvertDecimalIeee754ToDecimal<Decimal64, ulong>(value._value);

        //
        // Implicit conversions to Decimal64
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static implicit operator Decimal64(byte value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, byte>(value));

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal64(sbyte value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, sbyte>(value));

        /// <summary>Implicitly converts a <see cref="char" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static implicit operator Decimal64(char value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, ushort>(value));

        /// <summary>Implicitly converts a <see cref="short" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static implicit operator Decimal64(short value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, short>(value));

        /// <summary>Implicitly converts a <see cref="ushort" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal64(ushort value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, ushort>(value));

        /// <summary>Implicitly converts a <see cref="int" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static implicit operator Decimal64(int value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, int>(value));

        /// <summary>Implicitly converts a <see cref="uint" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal64(uint value) => new Decimal64(Number.ConvertIntegerToDecimalIeee754<Decimal64, ulong, uint>(value));

        //
        // Implicit conversions from Decimal64
        //

        /// <summary>Implicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(Decimal64 value) => new Decimal128(Number.ConvertDecimalIeee754<Decimal64, ulong, Decimal128, UInt128>(value._value));

        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Decimal64 left, Decimal64 right)
        {
            return Number.EqualsDecimalIeee754<Decimal64, ulong>(left._value, right._value);
        }

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Decimal64 left, Decimal64 right)
        {
            return !(left == right);
        }

        /// <summary>Compares two values to determine which is less.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <(Decimal64 left, Decimal64 right)
        {
            return Number.LessThanDecimalIeee754<Decimal64, ulong>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is greater.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >(Decimal64 left, Decimal64 right)
        {
            return Number.GreaterThanDecimalIeee754<Decimal64, ulong>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is less or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <=(Decimal64 left, Decimal64 right)
        {
            return Number.LessThanOrEqualDecimalIeee754<Decimal64, ulong>(left._value, right._value);
        }

        /// <summary>Compares two values to determine which is greater or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >=(Decimal64 left, Decimal64 right)
        {
            return Number.GreaterThanOrEqualDecimalIeee754<Decimal64, ulong>(left._value, right._value);
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Decimal64 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s.AsSpan(), style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Decimal64 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal64, ulong>(s, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Decimal64 result, out int bytesConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<byte, Decimal64, ulong>(utf8Text, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out bytesConsumed) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal64"/> from a <see cref="ReadOnlySpan{Byte}"/> containing UTF-8 text in the default parse style.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal64"/> value representing the input if the parse was successful. If the input exceeds Decimal64's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal64"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Decimal64 result) => TryParse(utf8Text, provider: null, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static Decimal64 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<byte, Decimal64, ulong>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static Decimal64 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal64 result) => Number.TryParseDecimalIeee754<byte, Decimal64, ulong>(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;

        /// <summary>Gets the value <c>1</c>.</summary>
        public static Decimal64 One => new Decimal64(OneValue);

        /// <summary>Gets the value <c>-1</c>.</summary>
        public static Decimal64 NegativeOne => new Decimal64(NegativeOneValue);

        /// <summary>Gets the mathematical constant <c>e</c>.</summary>
        public static Decimal64 E => new Decimal64(EValue);

        /// <summary>Gets the mathematical constant <c>pi</c>.</summary>
        public static Decimal64 Pi => new Decimal64(PiValue);

        /// <summary>Gets the mathematical constant <c>tau</c>.</summary>
        public static Decimal64 Tau => new Decimal64(TauValue);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Decimal64 Ceiling(Decimal64 x) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, 0, MidpointRounding.ToPositiveInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToInteger{TInteger}(TSelf)" />
        public static TInteger ConvertToInteger<TInteger>(Decimal64 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToIntegerNative{TInteger}(TSelf)" />
        public static TInteger ConvertToIntegerNative<TInteger>(Decimal64 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Decimal64 Floor(Decimal64 x) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, 0, MidpointRounding.ToNegativeInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Decimal64 Round(Decimal64 x) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, 0, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Decimal64 Round(Decimal64 x, int digits) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, digits, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Decimal64 Round(Decimal64 x, MidpointRounding mode) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, 0, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Decimal64 Round(Decimal64 x, int digits, MidpointRounding mode) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, digits, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Decimal64 Truncate(Decimal64 x) => new Decimal64(Number.RoundDecimalIeee754<Decimal64, ulong>(x._value, 0, MidpointRounding.ToZero));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Decimal64>.GetExponentByteCount() => sizeof(int);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<Decimal64>.GetExponentShortestBitLength()
        {
            int exponent = Number.UnpackDecimalIeee754<Decimal64, ulong>(_value).UnbiasedExponent;

            if (exponent >= 0)
            {
                return (sizeof(int) * 8) - int.LeadingZeroCount(exponent);
            }
            else
            {
                return (sizeof(int) * 8) + 1 - int.LeadingZeroCount(~exponent);
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<Decimal64>.GetSignificandBitLength() => 54;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Decimal64>.GetSignificandByteCount() => sizeof(ulong);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal64>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32BigEndian(destination, Number.UnpackDecimalIeee754<Decimal64, ulong>(_value).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal64>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal64, ulong>(_value).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal64>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt64BigEndian(destination, Number.UnpackDecimalIeee754<Decimal64, ulong>(_value).Significand))
            {
                bytesWritten = sizeof(ulong);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal64>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt64LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal64, ulong>(_value).Significand))
            {
                bytesWritten = sizeof(ulong);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Decimal64 Acos(Decimal64 x) => new Decimal64(Number.AcosDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static Decimal64 AcosPi(Decimal64 x) => new Decimal64(Number.AcosPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Decimal64 Acosh(Decimal64 x) => new Decimal64(Number.AcoshDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Decimal64 Asin(Decimal64 x) => new Decimal64(Number.AsinDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static Decimal64 AsinPi(Decimal64 x) => new Decimal64(Number.AsinPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Decimal64 Asinh(Decimal64 x) => new Decimal64(Number.AsinhDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Decimal64 Atan(Decimal64 x) => new Decimal64(Number.AtanDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static Decimal64 Atan2(Decimal64 y, Decimal64 x) => new Decimal64(Number.Atan2DecimalIeee754<Decimal64, ulong>(y._value, x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static Decimal64 Atan2Pi(Decimal64 y, Decimal64 x) => new Decimal64(Number.Atan2PiDecimalIeee754<Decimal64, ulong>(y._value, x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static Decimal64 AtanPi(Decimal64 x) => new Decimal64(Number.AtanPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Decimal64 Atanh(Decimal64 x) => new Decimal64(Number.AtanhDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Decimal64 BitDecrement(Decimal64 x) => new Decimal64(Number.BitDecrementDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Decimal64 BitIncrement(Decimal64 x) => new Decimal64(Number.BitIncrementDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Decimal64 Cbrt(Decimal64 x) => new Decimal64(Number.CbrtDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Decimal64 Cos(Decimal64 x) => new Decimal64(Number.CosDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static Decimal64 CosPi(Decimal64 x) => new Decimal64(Number.CosPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Decimal64 Cosh(Decimal64 x) => new Decimal64(Number.CoshDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp(TSelf)" />
        public static Decimal64 Exp(Decimal64 x) => new Decimal64(Number.ExpDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static Decimal64 Exp10(Decimal64 x) => new Decimal64(Number.Exp10DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static Decimal64 Exp10M1(Decimal64 x) => new Decimal64(Number.Exp10M1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static Decimal64 Exp2(Decimal64 x) => new Decimal64(Number.Exp2DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static Decimal64 Exp2M1(Decimal64 x) => new Decimal64(Number.Exp2M1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static Decimal64 ExpM1(Decimal64 x) => new Decimal64(Number.ExpM1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Decimal64 FusedMultiplyAdd(Decimal64 left, Decimal64 right, Decimal64 addend) => new Decimal64(Number.FusedMultiplyAddDecimalIeee754<Decimal64, ulong>(left._value, right._value, addend._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static Decimal64 Hypot(Decimal64 x, Decimal64 y) => new Decimal64(Number.HypotDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Decimal64 Ieee754Remainder(Decimal64 left, Decimal64 right) => new Decimal64(Number.Ieee754RemainderDecimalIeee754<Decimal64, ulong>(left._value, right._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Decimal64 x) => Number.ILogBDecimalIeee754<Decimal64, ulong>(x._value);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Decimal64 Log(Decimal64 x) => new Decimal64(Number.LogDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Decimal64 Log(Decimal64 x, Decimal64 newBase) => new Decimal64(Number.LogDecimalIeee754<Decimal64, ulong>(x._value, newBase._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Decimal64 Log10(Decimal64 x) => new Decimal64(Number.Log10DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static Decimal64 Log10P1(Decimal64 x) => new Decimal64(Number.Log10P1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2(TSelf)" />
        public static Decimal64 Log2(Decimal64 x) => new Decimal64(Number.Log2DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static Decimal64 Log2P1(Decimal64 x) => new Decimal64(Number.Log2P1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static Decimal64 LogP1(Decimal64 x) => new Decimal64(Number.LogP1DecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Decimal64 Pow(Decimal64 x, Decimal64 y) => new Decimal64(Number.PowDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static Decimal64 RootN(Decimal64 x, int n) => new Decimal64(Number.RootNDecimalIeee754<Decimal64, ulong>(x._value, n));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Decimal64 ScaleB(Decimal64 x, int n) => new Decimal64(Number.ScaleBDecimalIeee754<Decimal64, ulong>(x._value, n));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Decimal64 Sin(Decimal64 x) => new Decimal64(Number.SinDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Decimal64 Sin, Decimal64 Cos) SinCos(Decimal64 x)
        {
            (ulong sin, ulong cos) = Number.SinCosDecimalIeee754<Decimal64, ulong>(x._value);
            return (new Decimal64(sin), new Decimal64(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (Decimal64 SinPi, Decimal64 CosPi) SinCosPi(Decimal64 x)
        {
            (ulong sin, ulong cos) = Number.SinCosPiDecimalIeee754<Decimal64, ulong>(x._value);
            return (new Decimal64(sin), new Decimal64(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static Decimal64 SinPi(Decimal64 x) => new Decimal64(Number.SinPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Decimal64 Sinh(Decimal64 x) => new Decimal64(Number.SinhDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Decimal64 Sqrt(Decimal64 x) => new Decimal64(Number.SqrtDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Decimal64 Tan(Decimal64 x) => new Decimal64(Number.TanDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static Decimal64 TanPi(Decimal64 x) => new Decimal64(Number.TanPiDecimalIeee754<Decimal64, ulong>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Decimal64 Tanh(Decimal64 x) => new Decimal64(Number.TanhDecimalIeee754<Decimal64, ulong>(x._value));

        /// <summary>Adjusts a value to the quantum (exponent) of another value, rounding to nearest with ties to even.</summary>
        /// <param name="x">The value whose quantum is adjusted.</param>
        /// <param name="y">The value that provides the target quantum.</param>
        /// <returns><paramref name="x" /> expressed with the quantum of <paramref name="y" />, or NaN when the value cannot be represented at that quantum.</returns>
        public static Decimal64 Quantize(Decimal64 x, Decimal64 y) => new Decimal64(Number.QuantizeDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Computes the quantum of a value: one unit in the last place sharing its exponent.</summary>
        /// <param name="x">The value whose quantum is returned.</param>
        /// <returns>The quantum of <paramref name="x" />.</returns>
        public static Decimal64 Quantum(Decimal64 x) => new Decimal64(Number.QuantumDecimalIeee754<Decimal64, ulong>(x._value));

        /// <summary>Determines whether two values have the same quantum (exponent).</summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="x" /> and <paramref name="y" /> have the same quantum; otherwise, <c>false</c>.</returns>
        public static bool SameQuantum(Decimal64 x, Decimal64 y) => Number.SameQuantumDecimalIeee754<Decimal64, ulong>(x._value, y._value);

        /// <summary>Computes the absolute of a value.</summary>
        /// <param name="value">The value for which to get its absolute.</param>
        /// <returns>The absolute of <paramref name="value" />.</returns>
        public static Decimal64 Abs(Decimal64 value) => new Decimal64(Number.AbsDecimalIeee754<Decimal64, ulong>(value._value));

        /// <summary>Determines if a value is finite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is finite; otherwise, <c>false</c>.</returns>
        public static bool IsFinite(Decimal64 value) => (value._value & InfinityMask) != InfinityMask;

        /// <summary>Determines if a value is infinite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is infinite; otherwise, <c>false</c>.</returns>
        public static bool IsInfinity(Decimal64 value) => (value._value & NaNMask) == InfinityMask;

        /// <summary>Determines if a value is NaN.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is NaN; otherwise, <c>false</c>.</returns>
        public static bool IsNaN(Decimal64 value) => (value._value & NaNMask) == NaNMask;

        /// <summary>Determines if a value is negative.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative; otherwise, <c>false</c>.</returns>
        public static bool IsNegative(Decimal64 value) => (value._value & SignMask) != 0;

        /// <summary>Determines if a value is negative infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative infinity; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeInfinity(Decimal64 value) => (value._value & (SignMask | NaNMask)) == NegativeInfinityValue;

        /// <summary>Determines if a value is positive.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive; otherwise, <c>false</c>.</returns>
        public static bool IsPositive(Decimal64 value) => (value._value & SignMask) == 0;

        /// <summary>Determines if a value is positive infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive infinity; otherwise, <c>false</c>.</returns>
        public static bool IsPositiveInfinity(Decimal64 value) => (value._value & (SignMask | NaNMask)) == PositiveInfinityValue;

        /// <summary>Determines if a value represents a real number.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a real number; otherwise, <c>false</c>.</returns>
        public static bool IsRealNumber(Decimal64 value) => !IsNaN(value);

        /// <summary>Determines if a value represents an integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an integer; otherwise, <c>false</c>.</returns>
        public static bool IsInteger(Decimal64 value) => Number.IsIntegerDecimalIeee754<Decimal64, ulong>(value._value);

        /// <summary>Determines if a value represents an even integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an even integer; otherwise, <c>false</c>.</returns>
        public static bool IsEvenInteger(Decimal64 value) => Number.IsEvenIntegerDecimalIeee754<Decimal64, ulong>(value._value);

        /// <summary>Determines if a value represents an odd integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an odd integer; otherwise, <c>false</c>.</returns>
        public static bool IsOddInteger(Decimal64 value) => Number.IsOddIntegerDecimalIeee754<Decimal64, ulong>(value._value);

        /// <summary>Determines if a value is normal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is normal; otherwise, <c>false</c>.</returns>
        public static bool IsNormal(Decimal64 value) => Number.IsNormalDecimalIeee754<Decimal64, ulong>(value._value);

        /// <summary>Determines if a value is subnormal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is subnormal; otherwise, <c>false</c>.</returns>
        public static bool IsSubnormal(Decimal64 value) => Number.IsSubnormalDecimalIeee754<Decimal64, ulong>(value._value);

        /// <summary>Compares two values to compute which has the greater magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MaxMagnitude(Decimal64 x, Decimal64 y) => new Decimal64(Number.MaxMagnitudeDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which has the greater magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MaxMagnitudeNumber(Decimal64 x, Decimal64 y) => new Decimal64(Number.MaxMagnitudeNumberDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which has the lesser magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MinMagnitude(Decimal64 x, Decimal64 y) => new Decimal64(Number.MinMagnitudeDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which has the lesser magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MinMagnitudeNumber(Decimal64 x, Decimal64 y) => new Decimal64(Number.MinMagnitudeNumberDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Computes an estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="left">The value to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The value to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The value to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>An estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</returns>
        public static Decimal64 MultiplyAddEstimate(Decimal64 left, Decimal64 right, Decimal64 addend) => (left * right) + addend;

        /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        public static Decimal64 Clamp(Decimal64 value, Decimal64 min, Decimal64 max)
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
        public static Decimal64 ClampNative(Decimal64 value, Decimal64 min, Decimal64 max)
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
        public static Decimal64 CopySign(Decimal64 value, Decimal64 sign) => new Decimal64(Number.CopySignDecimalIeee754<Decimal64, ulong>(value._value, sign._value));

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 Max(Decimal64 x, Decimal64 y) => new Decimal64(Number.MaxDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which is greater using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MaxNative(Decimal64 x, Decimal64 y) => new Decimal64(Number.MaxNativeDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which is greater and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MaxNumber(Decimal64 x, Decimal64 y) => new Decimal64(Number.MaxNumberDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 Min(Decimal64 x, Decimal64 y) => new Decimal64(Number.MinDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MinNative(Decimal64 x, Decimal64 y) => new Decimal64(Number.MinNativeDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Compares two values to compute which is lesser and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal64 MinNumber(Decimal64 x, Decimal64 y) => new Decimal64(Number.MinNumberDecimalIeee754<Decimal64, ulong>(x._value, y._value));

        /// <summary>Computes the sign of a value.</summary>
        /// <param name="value">The value whose sign is to be computed.</param>
        /// <returns>A positive one if <paramref name="value" /> is positive, a negative one if <paramref name="value" /> is negative, and zero if <paramref name="value" /> is zero.</returns>
        /// <exception cref="ArithmeticException"><paramref name="value" /> is <c>NaN</c>.</exception>
        public static int Sign(Decimal64 value) => Number.SignDecimalIeee754<Decimal64, ulong>(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Decimal64>.Radix => 10;

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Decimal64>.IsCanonical(Decimal64 value) => Number.IsCanonicalDecimalIeee754<Decimal64, ulong>(value._value, nanReservedMask: 0x01FC_0000_0000_0000, nanPayloadMask: 0x0003_FFFF_FFFF_FFFF, maxNaNPayload: 999_999_999_999_999);

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Decimal64>.IsComplexNumber(Decimal64 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Decimal64>.IsImaginaryNumber(Decimal64 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Decimal64>.IsZero(Decimal64 value) => Number.IsZeroDecimalIeee754<Decimal64, ulong>(value._value);

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Decimal64 IAdditiveIdentity<Decimal64, Decimal64>.AdditiveIdentity => Zero;

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Decimal64 IMultiplicativeIdentity<Decimal64, Decimal64>.MultiplicativeIdentity => One;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal64 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal64 result;

            if (typeof(TOther) == typeof(Decimal64))
            {
                result = (Decimal64)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal64 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal64 result;

            if (typeof(TOther) == typeof(Decimal64))
            {
                result = (Decimal64)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal64 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal64 result;

            if (typeof(TOther) == typeof(Decimal64))
            {
                result = (Decimal64)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal64>.TryConvertFromChecked<TOther>(TOther value, out Decimal64 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal64>.TryConvertFromSaturating<TOther>(TOther value, out Decimal64 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal64>.TryConvertFromTruncating<TOther>(TOther value, out Decimal64 result) => TryConvertFrom(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFrom<TOther>(TOther value, out Decimal64 result)
            where TOther : INumberBase<TOther>
        {
            // Decimal64 must handle every source type itself because the built-in numeric types
            // predate the IEEE 754 decimal types and therefore never convert to them. Widening from
            // an integer or floating-point value never throws; out-of-range inputs become infinity.

            if (typeof(TOther) == typeof(byte))
            {
                result = (byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (Decimal64)(long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (Decimal64)(ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                result = (Decimal64)(Int128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                result = (Decimal64)(UInt128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (Decimal64)(nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (Decimal64)(nuint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                result = (Decimal64)(Half)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (Decimal64)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (Decimal64)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (Decimal64)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal128))
            {
                result = (Decimal64)(Decimal128)(object)value;
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
        static bool INumberBase<Decimal64>.TryConvertToChecked<TOther>(Decimal64 value, [MaybeNullWhen(false)] out TOther result)
        {
            // Conversions to an integer target throw on overflow, NaN, or infinity. Conversions to a
            // floating-point or wider decimal target never throw; conversions to `System.Decimal`
            // throw when the value cannot be represented, matching the checked contract.

            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                result = (TOther)(object)(Half)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (TOther)(object)(float)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (TOther)(object)(double)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (TOther)(object)(decimal)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal32))
            {
                result = (TOther)(object)(Decimal32)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal128))
            {
                result = (TOther)(object)(Decimal128)value;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal64>.TryConvertToSaturating<TOther>(Decimal64 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal64>.TryConvertToTruncating<TOther>(Decimal64 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertTo<TOther>(Decimal64 value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            // Conversions to an integer target saturate (NaN becomes zero, out-of-range clamps to the
            // target's minimum or maximum). Truncating and saturating share this path because the
            // integer conversion operators already saturate. Conversions to `System.Decimal` clamp to
            // its range because that operator would otherwise throw.

            if (typeof(TOther) == typeof(byte))
            {
                result = (TOther)(object)(byte)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (TOther)(object)(sbyte)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (TOther)(object)(char)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (TOther)(object)(short)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (TOther)(object)(ushort)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (TOther)(object)(int)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (TOther)(object)(uint)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (TOther)(object)(long)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (TOther)(object)(ulong)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                result = (TOther)(object)(Int128)value;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                result = (TOther)(object)(UInt128)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (TOther)(object)(nint)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (TOther)(object)(nuint)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                result = (TOther)(object)(Half)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (TOther)(object)(float)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (TOther)(object)(double)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                // `decimal.MaxValue`/`decimal.MinValue` are not exactly representable as `Decimal64` and
                // round outward, so a near-boundary value can compare as in-range yet still overflow the
                // `(decimal)value` cast. Range-check against the exact endpoints in `Decimal128` space,
                // which represents them without loss.
                Decimal128 wide = (Decimal128)value;
                decimal actualResult = (wide > (Decimal128)decimal.MaxValue) ? decimal.MaxValue :
                                       (wide < (Decimal128)decimal.MinValue) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal32))
            {
                result = (TOther)(object)(Decimal32)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal128))
            {
                result = (TOther)(object)(Decimal128)value;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }


        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.BufferLength => Number.Decimal64NumberBufferLength;

        static string IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ToDecStr(ulong significand)
        {
            return Number.UInt64ToDecStr(significand);
        }

        static unsafe ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            return Number.DigitsToUInt64(number.DigitsPtr, digits);
        }

        static Decimal64 IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Construct(ulong value) => new Decimal64(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ConvertToExponent(ulong value) => (int)value;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Power10(int exponent) => UInt64Powers10[exponent];

        static (ulong Quotient, ulong Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.DivRemPow10(ulong value, int exponent)
        {
            ulong power = UInt64Powers10[exponent];
            return Math.DivRem(value, power);
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.CountDigits(ulong significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MinExponent => MinExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxAdjustedExponent => MaxExponent - Precision + 1;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MinAdjustedExponent => MinExponent - Precision + 1;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.PositiveInfinity => PositiveInfinityValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NegativeInfinity => NegativeInfinityValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.Zero => ZeroValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NaN => QuietNaNValue;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MostSignificantBitOfSignificandMask => MostSignificantBitOfSignificandMask;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NaNMask => NaNMask;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.SignMask => SignMask;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G0G1Mask => G0G1Mask;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.NumberBitsSignificand => 50;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G0ToGwPlus1ExponentMask => 0x7FE0_0000_0000_0000;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.G2ToGwPlus3ExponentMask => 0x1FF8_0000_0000_0000;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.GwPlus2ToGwPlus4SignificandMask => 0x001F_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.GwPlus4SignificandMask => 0x0007_FFFF_FFFF_FFFF;

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.MaxSignificand => MaxSignificand;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsNaN(ulong decimalBits) => IsNaN(new Decimal64(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsNegative(ulong decimalBits) => IsNegative(new Decimal64(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsFinite(ulong decimalBits) => IsFinite(new Decimal64(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsInfinity(ulong decimalBits) => IsInfinity(new Decimal64(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsPositiveInfinity(ulong decimalBits) => IsPositiveInfinity(new Decimal64(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.IsNegativeInfinity(ulong decimalBits) => IsNegativeInfinity(new Decimal64(decimalBits));

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.EncodeExponentToG0ThroughGwPlus1(uint biasedExponent)
        {
            return ((ulong)biasedExponent) << 53;
        }

        static ulong IDecimalIeee754ParseAndFormatInfo<Decimal64, ulong>.EncodeExponentToG2ThroughGwPlus3(uint biasedExponent)
        {
            return ((ulong)biasedExponent) << 51;
        }
    }
}

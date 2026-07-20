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
    /// Represents a decimal floating-point number that uses the IEEE 754 <c>decimal128</c> interchange format, providing 34 decimal digits of precision.
    /// </summary>
    /// <remarks>The IEEE 754 standard defines two interchange encodings for decimal floating-point: binary integer decimal (BID) and densely packed decimal (DPD). Which encoding is used is determined by the underlying ABI for the platform and defaults to BID where the ABI does not otherwise specify.</remarks>
    public readonly struct Decimal128
        : IComparable,
          IComparable<Decimal128>,
          IEquatable<Decimal128>,
          IDecimalFloatingPointIeee754<Decimal128>,
          ISpanFormattable,
          ISpanParsable<Decimal128>,
          IMinMaxValue<Decimal128>,
          IUtf8SpanFormattable,
          IUtf8SpanParsable<Decimal128>,
          IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>
    {
#if BIGENDIAN
        internal readonly ulong _upper;
        internal readonly ulong _lower;
#else
        internal readonly ulong _lower;
        internal readonly ulong _upper;
#endif

        private const int MaxExponent = 6144;
        private const int MinExponent = -6143;
        private const int Precision = 34;
        private const int ExponentBias = 6176;
        private static UInt128 PositiveInfinityValue => new UInt128(upper: 0x7800_0000_0000_0000, lower: 0);
        private static UInt128 NegativeInfinityValue => new UInt128(upper: 0xF800_0000_0000_0000, lower: 0);
        // Canonical ±0 use the IEEE 754 preferred representation for integer values,
        // which stores zero with the biased exponent rather than the minimum exponent.
        private static UInt128 ZeroValue => new UInt128(0x3040_0000_0000_0000, 0);
        private static UInt128 NegativeZeroValue => new UInt128(0xB040_0000_0000_0000, 0);
        // One (+1 * 10^0) shares the biased exponent of canonical zero with a coefficient of one.
        private static UInt128 OneValue => new UInt128(0x3040_0000_0000_0000, 1);
        private static UInt128 NegativeOneValue => new UInt128(0xB040_0000_0000_0000, 1);
        // Mathematical constants correctly rounded to the format's precision (34 significant digits).
        private static UInt128 EValue => new UInt128(0x2FFE_8605_8A4B_F4DE, 0x4E90_6ACC_B26A_BB56);   // +2.718281828459045235360287471352662
        private static UInt128 PiValue => new UInt128(0x2FFE_9AE4_7957_96A7, 0xBABE_5564_E6F3_9F8F);  // +3.141592653589793238462643383279503
        private static UInt128 TauValue => new UInt128(0x2FFF_35C8_F2AF_2D4F, 0x757C_AAC9_CDE7_3F1E); // +6.283185307179586476925286766559006
        private static UInt128 QuietNaNValue => new UInt128(0xFC00_0000_0000_0000, 0);

        private const ulong SignMaskUpper = 0x8000_0000_0000_0000;
        private const ulong NaNMaskUpper = 0x7C00_0000_0000_0000;
        private const ulong InfinityMaskUpper = 0x7800_0000_0000_0000;

        /// <summary>Gets a value that represents positive <c>infinity</c>.</summary>
        public static Decimal128 PositiveInfinity => new Decimal128(PositiveInfinityValue);

        /// <summary>Gets a value that represents negative <c>infinity</c>.</summary>
        public static Decimal128 NegativeInfinity => new Decimal128(NegativeInfinityValue);

        /// <summary>Gets a value that represents <c>NaN</c>.</summary>
        public static Decimal128 NaN => new Decimal128(QuietNaNValue);

        /// <summary>Gets a value that represents negative <c>zero</c>.</summary>
        public static Decimal128 NegativeZero => new Decimal128(NegativeZeroValue);

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        public static Decimal128 Zero => new Decimal128(ZeroValue);

        /// <summary>Gets the minimum value of the current type.</summary>
        public static Decimal128 MinValue => new Decimal128(upper: 0xDFFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);

        /// <summary>Gets the maximum value of the current type.</summary>
        public static Decimal128 MaxValue => new Decimal128(upper: 0x5FFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);

        /// <summary>Gets the smallest value such that can be added to <c>0</c> that does not result in <c>0</c>.</summary>
        public static Decimal128 Epsilon => new Decimal128(upper: 0x0000_0000_0000_0000, lower: 0x0000_0000_0000_0001); // Smallest positive subnormal value, aka 1 * 10^-6176

        internal Decimal128(UInt128 value)
        {
            _upper = value.Upper;
            _lower = value.Lower;
        }

        internal Decimal128(ulong upper, ulong lower)
        {
            _upper = upper;
            _lower = lower;
        }

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal128 Parse(string s) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> in the given <see cref="NumberStyles"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal128 Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <inheritdoc cref="ISpanParsable{T}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static Decimal128 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal128 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal128 Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<char, Decimal128, UInt128>(s, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Parses a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider.</param>
        /// <returns>The equivalent <see cref="Decimal128"/> value representing the input string. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. </returns>
        public static Decimal128 Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (s is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
            }
            return Parse(s.AsSpan(), style, provider);
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out Decimal128 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> in the default parse style.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out Decimal128 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider: null, out result);

        /// <inheritdoc cref="ISpanParsable{T}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out T)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Char}"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s, style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="string"/> with the given <see cref="NumberStyles"/> and <see cref="IFormatProvider"/>.
        /// </summary>
        /// <param name="s">The input to be parsed.</param>
        /// <param name="style">The <see cref="NumberStyles"/> used to parse the input.</param>
        /// <param name="provider">A format provider. </param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input string if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="IComparable.CompareTo(object?)" />
        public int CompareTo(object? value)
        {
            if (value is not Decimal128 other)
            {
                return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeDecimal128);
            }
            return CompareTo(other);
        }

        /// <inheritdoc cref="IComparable{T}.CompareTo(T)" />
        public int CompareTo(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, UInt128>(current, another);
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(Decimal128 other)
        {
            var current = new UInt128(_upper, _lower);
            var another = new UInt128(other._upper, other._lower);
            return Number.CompareDecimalIeee754<Decimal128, UInt128>(current, another) == 0;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Decimal128 other) && Equals(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            return Number.GetDecimalIeee754HashCode<Decimal128, UInt128>(new UInt128(_upper, _lower));
        }

        /// <summary>
        /// Returns a string representation of the current value.
        /// </summary>
        public override string ToString()
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower), null, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower), format, NumberFormatInfo.CurrentInfo);
        }

        /// <summary>
        /// Returns a string representation of the current value with the specified <paramref name="provider"/>.
        /// </summary>
        public string ToString(IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower), null, NumberFormatInfo.GetInstance(provider));
        }

        /// <summary>
        /// Returns a string representation of the current value using the specified <paramref name="format"/> and <paramref name="provider"/>.
        /// </summary>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider)
        {
            return Number.FormatDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower), format, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal128, UInt128, char>(new UInt128(_upper, _lower), format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal128, UInt128, byte>(new UInt128(_upper, _lower), format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
        }

        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute the unary plus.</param>
        /// <returns><paramref name="value" /> unchanged.</returns>
        public static Decimal128 operator +(Decimal128 value) => value;

        /// <summary>Computes the unary negation of a value.</summary>
        /// <param name="value">The value for which to compute the unary negation.</param>
        /// <returns>The unary negation of <paramref name="value" />.</returns>
        public static Decimal128 operator -(Decimal128 value) => new Decimal128(value._upper ^ SignMaskUpper, value._lower);

        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" /> by one.</returns>
        public static Decimal128 operator ++(Decimal128 value)
        {
            UInt128 result = Number.AddDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower), OneValue);
            return new Decimal128(result);
        }

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" /> by one.</returns>
        public static Decimal128 operator --(Decimal128 value)
        {
            UInt128 result = Number.SubtractDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower), OneValue);
            return new Decimal128(result);
        }

        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal128 operator +(Decimal128 left, Decimal128 right)
        {
            UInt128 result = Number.AddDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
            return new Decimal128(result);
        }

        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        public static Decimal128 operator -(Decimal128 left, Decimal128 right)
        {
            UInt128 result = Number.SubtractDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
            return new Decimal128(result);
        }

        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        public static Decimal128 operator *(Decimal128 left, Decimal128 right)
        {
            UInt128 result = Number.MultiplyDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
            return new Decimal128(result);
        }

        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal128 operator /(Decimal128 left, Decimal128 right)
        {
            UInt128 result = Number.DivideDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
            return new Decimal128(result);
        }

        /// <summary>Divides two values together to compute their remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal128 operator %(Decimal128 left, Decimal128 right)
        {
            UInt128 result = Number.RemainderDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
            return new Decimal128(result);
        }

        //
        // Explicit conversions to Decimal128
        //

        /// <summary>Explicitly converts a <see cref="System.Int128" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static explicit operator Decimal128(Int128 value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, Int128>(value));

        /// <summary>Explicitly converts a <see cref="System.UInt128" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal128(UInt128 value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, UInt128>(value));

        /// <summary>Explicitly converts a <see cref="System.Half" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static explicit operator Decimal128(Half value) => new Decimal128(Number.ConvertFloatToDecimalIeee754<Half, Decimal128, UInt128>(value));

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static explicit operator Decimal128(float value) => new Decimal128(Number.ConvertFloatToDecimalIeee754<float, Decimal128, UInt128>(value));

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static explicit operator Decimal128(double value) => new Decimal128(Number.ConvertFloatToDecimalIeee754<double, Decimal128, UInt128>(value));

        //
        // Explicit conversions from Decimal128
        //

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        public static explicit operator byte(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, byte>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        public static explicit operator checked byte(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, byte>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, sbyte>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, sbyte>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        public static explicit operator char(Decimal128 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ushort>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        public static explicit operator checked char(Decimal128 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ushort>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        public static explicit operator short(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, short>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        public static explicit operator checked short(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, short>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ushort>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ushort>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        public static explicit operator int(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, int>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        public static explicit operator checked int(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, int>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, uint>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, uint>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        public static explicit operator nint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, nint>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.IntPtr" />.</exception>
        public static explicit operator checked nint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, nint>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, nuint>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UIntPtr" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, nuint>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        public static explicit operator long(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, long>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        public static explicit operator checked long(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, long>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ulong>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, ulong>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        public static explicit operator Int128(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, Int128>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.Int128" />.</exception>
        public static explicit operator checked Int128(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, Int128>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, UInt128>(new UInt128(value._upper, value._lower), isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(Decimal128 value) => Number.ConvertDecimalIeee754ToInteger<Decimal128, UInt128, UInt128>(new UInt128(value._upper, value._lower), isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Half" /> value.</returns>
        public static explicit operator Half(Decimal128 value) => Number.ConvertDecimalIeee754ToFloat<Decimal128, UInt128, Half>(new UInt128(value._upper, value._lower));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        public static explicit operator float(Decimal128 value) => Number.ConvertDecimalIeee754ToFloat<Decimal128, UInt128, float>(new UInt128(value._upper, value._lower));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
        public static explicit operator double(Decimal128 value) => Number.ConvertDecimalIeee754ToFloat<Decimal128, UInt128, double>(new UInt128(value._upper, value._lower));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="decimal" />.</exception>
        public static explicit operator decimal(Decimal128 value) => Number.ConvertDecimalIeee754ToDecimal<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        //
        // Implicit conversions to Decimal128
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(byte value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, byte>(value));

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal128(sbyte value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, sbyte>(value));

        /// <summary>Implicitly converts a <see cref="char" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(char value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, ushort>(value));

        /// <summary>Implicitly converts a <see cref="short" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(short value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, short>(value));

        /// <summary>Implicitly converts a <see cref="ushort" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal128(ushort value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, ushort>(value));

        /// <summary>Implicitly converts a <see cref="int" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(int value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, int>(value));

        /// <summary>Implicitly converts a <see cref="uint" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal128(uint value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, uint>(value));

        /// <summary>Implicitly converts a <see cref="System.IntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(nint value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, nint>(value));

        /// <summary>Implicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal128(nuint value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, nuint>(value));

        /// <summary>Implicitly converts a <see cref="long" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(long value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, long>(value));

        /// <summary>Implicitly converts a <see cref="ulong" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal128(ulong value) => new Decimal128(Number.ConvertIntegerToDecimalIeee754<Decimal128, UInt128, ulong>(value));

        /// <summary>Implicitly converts a <see cref="decimal" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(decimal value) => new Decimal128(Number.ConvertDecimalToDecimalIeee754<Decimal128, UInt128>(value));

        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Decimal128 left, Decimal128 right)
        {
            return Number.EqualsDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
        }

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Decimal128 left, Decimal128 right)
        {
            return !(left == right);
        }

        /// <summary>Compares two values to determine which is less.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <(Decimal128 left, Decimal128 right)
        {
            return Number.LessThanDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
        }

        /// <summary>Compares two values to determine which is greater.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >(Decimal128 left, Decimal128 right)
        {
            return Number.GreaterThanDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
        }

        /// <summary>Compares two values to determine which is less or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator <=(Decimal128 left, Decimal128 right)
        {
            return Number.LessThanOrEqualDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
        }

        /// <summary>Compares two values to determine which is greater or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        public static bool operator >=(Decimal128 left, Decimal128 right)
        {
            return Number.GreaterThanOrEqualDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower));
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s.AsSpan(), style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int bytesConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<byte, Decimal128, UInt128>(utf8Text, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out bytesConsumed) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal128"/> from a <see cref="ReadOnlySpan{Byte}"/> containing UTF-8 text in the default parse style.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal128"/> value representing the input if the parse was successful. If the input exceeds Decimal128's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal128"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Decimal128 result) => TryParse(utf8Text, provider: null, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static Decimal128 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<byte, Decimal128, UInt128>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static Decimal128 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal128 result) => Number.TryParseDecimalIeee754<byte, Decimal128, UInt128>(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;

        /// <summary>Gets the value <c>1</c>.</summary>
        public static Decimal128 One => new Decimal128(OneValue);

        /// <summary>Gets the value <c>-1</c>.</summary>
        public static Decimal128 NegativeOne => new Decimal128(NegativeOneValue);

        /// <summary>Gets the mathematical constant <c>e</c>.</summary>
        public static Decimal128 E => new Decimal128(EValue);

        /// <summary>Gets the mathematical constant <c>pi</c>.</summary>
        public static Decimal128 Pi => new Decimal128(PiValue);

        /// <summary>Gets the mathematical constant <c>tau</c>.</summary>
        public static Decimal128 Tau => new Decimal128(TauValue);

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Decimal128 Ceiling(Decimal128 x) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), 0, MidpointRounding.ToPositiveInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToInteger{TInteger}(TSelf)" />
        public static TInteger ConvertToInteger<TInteger>(Decimal128 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToIntegerNative{TInteger}(TSelf)" />
        public static TInteger ConvertToIntegerNative<TInteger>(Decimal128 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Decimal128 Floor(Decimal128 x) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), 0, MidpointRounding.ToNegativeInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Decimal128 Round(Decimal128 x) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), 0, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Decimal128 Round(Decimal128 x, int digits) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), digits, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Decimal128 Round(Decimal128 x, MidpointRounding mode) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), 0, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Decimal128 Round(Decimal128 x, int digits, MidpointRounding mode) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), digits, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Decimal128 Truncate(Decimal128 x) => new Decimal128(Number.RoundDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), 0, MidpointRounding.ToZero));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Decimal128>.GetExponentByteCount() => sizeof(int);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<Decimal128>.GetExponentShortestBitLength()
        {
            int exponent = Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower)).UnbiasedExponent;

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
        int IFloatingPoint<Decimal128>.GetSignificandBitLength() => 113;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Decimal128>.GetSignificandByteCount() => Unsafe.SizeOf<UInt128>();

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal128>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32BigEndian(destination, Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower)).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal128>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower)).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal128>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt128BigEndian(destination, Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower)).Significand))
            {
                bytesWritten = Unsafe.SizeOf<UInt128>();
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal128>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt128LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal128, UInt128>(new UInt128(_upper, _lower)).Significand))
            {
                bytesWritten = Unsafe.SizeOf<UInt128>();
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Decimal128 Acos(Decimal128 x) => new Decimal128(Number.AcosDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static Decimal128 AcosPi(Decimal128 x) => new Decimal128(Number.AcosPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Decimal128 Acosh(Decimal128 x) => new Decimal128(Number.AcoshDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Decimal128 Asin(Decimal128 x) => new Decimal128(Number.AsinDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static Decimal128 AsinPi(Decimal128 x) => new Decimal128(Number.AsinPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Decimal128 Asinh(Decimal128 x) => new Decimal128(Number.AsinhDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Decimal128 Atan(Decimal128 x) => new Decimal128(Number.AtanDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static Decimal128 Atan2(Decimal128 y, Decimal128 x) => new Decimal128(Number.Atan2DecimalIeee754<Decimal128, UInt128>(new UInt128(y._upper, y._lower), new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static Decimal128 Atan2Pi(Decimal128 y, Decimal128 x) => new Decimal128(Number.Atan2PiDecimalIeee754<Decimal128, UInt128>(new UInt128(y._upper, y._lower), new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static Decimal128 AtanPi(Decimal128 x) => new Decimal128(Number.AtanPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Decimal128 Atanh(Decimal128 x) => new Decimal128(Number.AtanhDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Decimal128 BitDecrement(Decimal128 x) => new Decimal128(Number.BitDecrementDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Decimal128 BitIncrement(Decimal128 x) => new Decimal128(Number.BitIncrementDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Decimal128 Cbrt(Decimal128 x) => new Decimal128(Number.CbrtDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Decimal128 Cos(Decimal128 x) => new Decimal128(Number.CosDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static Decimal128 CosPi(Decimal128 x) => new Decimal128(Number.CosPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Decimal128 Cosh(Decimal128 x) => new Decimal128(Number.CoshDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp(TSelf)" />
        public static Decimal128 Exp(Decimal128 x) => new Decimal128(Number.ExpDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static Decimal128 Exp10(Decimal128 x) => new Decimal128(Number.Exp10DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static Decimal128 Exp10M1(Decimal128 x) => new Decimal128(Number.Exp10M1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static Decimal128 Exp2(Decimal128 x) => new Decimal128(Number.Exp2DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static Decimal128 Exp2M1(Decimal128 x) => new Decimal128(Number.Exp2M1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static Decimal128 ExpM1(Decimal128 x) => new Decimal128(Number.ExpM1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Decimal128 FusedMultiplyAdd(Decimal128 left, Decimal128 right, Decimal128 addend) => new Decimal128(Number.FusedMultiplyAddDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower), new UInt128(addend._upper, addend._lower)));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static Decimal128 Hypot(Decimal128 x, Decimal128 y) => new Decimal128(Number.HypotDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Decimal128 Ieee754Remainder(Decimal128 left, Decimal128 right) => new Decimal128(Number.Ieee754RemainderDecimalIeee754<Decimal128, UInt128>(new UInt128(left._upper, left._lower), new UInt128(right._upper, right._lower)));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Decimal128 x) => Number.ILogBDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Decimal128 Log(Decimal128 x) => new Decimal128(Number.LogDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Decimal128 Log(Decimal128 x, Decimal128 newBase) => new Decimal128(Number.LogDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(newBase._upper, newBase._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Decimal128 Log10(Decimal128 x) => new Decimal128(Number.Log10DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static Decimal128 Log10P1(Decimal128 x) => new Decimal128(Number.Log10P1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2(TSelf)" />
        public static Decimal128 Log2(Decimal128 x) => new Decimal128(Number.Log2DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static Decimal128 Log2P1(Decimal128 x) => new Decimal128(Number.Log2P1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static Decimal128 LogP1(Decimal128 x) => new Decimal128(Number.LogP1DecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Decimal128 Pow(Decimal128 x, Decimal128 y) => new Decimal128(Number.PowDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static Decimal128 RootN(Decimal128 x, int n) => new Decimal128(Number.RootNDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), n));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Decimal128 ScaleB(Decimal128 x, int n) => new Decimal128(Number.ScaleBDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), n));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Decimal128 Sin(Decimal128 x) => new Decimal128(Number.SinDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Decimal128 Sin, Decimal128 Cos) SinCos(Decimal128 x)
        {
            (UInt128 sin, UInt128 cos) = Number.SinCosDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower));
            return (new Decimal128(sin), new Decimal128(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (Decimal128 SinPi, Decimal128 CosPi) SinCosPi(Decimal128 x)
        {
            (UInt128 sin, UInt128 cos) = Number.SinCosPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower));
            return (new Decimal128(sin), new Decimal128(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static Decimal128 SinPi(Decimal128 x) => new Decimal128(Number.SinPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Decimal128 Sinh(Decimal128 x) => new Decimal128(Number.SinhDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Decimal128 Sqrt(Decimal128 x) => new Decimal128(Number.SqrtDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Decimal128 Tan(Decimal128 x) => new Decimal128(Number.TanDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static Decimal128 TanPi(Decimal128 x) => new Decimal128(Number.TanPiDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Decimal128 Tanh(Decimal128 x) => new Decimal128(Number.TanhDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <summary>Adjusts a value to the quantum (exponent) of another value, rounding to nearest with ties to even.</summary>
        /// <param name="x">The value whose quantum is adjusted.</param>
        /// <param name="y">The value that provides the target quantum.</param>
        /// <returns><paramref name="x" /> expressed with the quantum of <paramref name="y" />, or NaN when the value cannot be represented at that quantum.</returns>
        public static Decimal128 Quantize(Decimal128 x, Decimal128 y) => new Decimal128(Number.QuantizeDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Computes the quantum of a value: one unit in the last place sharing its exponent.</summary>
        /// <param name="x">The value whose quantum is returned.</param>
        /// <returns>The quantum of <paramref name="x" />.</returns>
        public static Decimal128 Quantum(Decimal128 x) => new Decimal128(Number.QuantumDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower)));

        /// <summary>Determines whether two values have the same quantum (exponent).</summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="x" /> and <paramref name="y" /> have the same quantum; otherwise, <c>false</c>.</returns>
        public static bool SameQuantum(Decimal128 x, Decimal128 y) => Number.SameQuantumDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower));

        /// <summary>Computes the absolute of a value.</summary>
        /// <param name="value">The value for which to get its absolute.</param>
        /// <returns>The absolute of <paramref name="value" />.</returns>
        public static Decimal128 Abs(Decimal128 value) => new Decimal128(Number.AbsDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower)));

        /// <summary>Determines if a value is finite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is finite; otherwise, <c>false</c>.</returns>
        public static bool IsFinite(Decimal128 value) => (value._upper & InfinityMaskUpper) != InfinityMaskUpper;

        /// <summary>Determines if a value is infinite.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is infinite; otherwise, <c>false</c>.</returns>
        public static bool IsInfinity(Decimal128 value) => (value._upper & NaNMaskUpper) == InfinityMaskUpper;

        /// <summary>Determines if a value is NaN.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is NaN; otherwise, <c>false</c>.</returns>
        public static bool IsNaN(Decimal128 value) => (value._upper & NaNMaskUpper) == NaNMaskUpper;

        /// <summary>Determines if a value is negative.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative; otherwise, <c>false</c>.</returns>
        public static bool IsNegative(Decimal128 value) => (value._upper & SignMaskUpper) != 0;

        /// <summary>Determines if a value is negative infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is negative infinity; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeInfinity(Decimal128 value) => (value._upper & (SignMaskUpper | NaNMaskUpper)) == NegativeInfinityValue.Upper;

        /// <summary>Determines if a value is positive.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive; otherwise, <c>false</c>.</returns>
        public static bool IsPositive(Decimal128 value) => (value._upper & SignMaskUpper) == 0;

        /// <summary>Determines if a value is positive infinity.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is positive infinity; otherwise, <c>false</c>.</returns>
        public static bool IsPositiveInfinity(Decimal128 value) => (value._upper & (SignMaskUpper | NaNMaskUpper)) == PositiveInfinityValue.Upper;

        /// <summary>Determines if a value represents a real number.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is a real number; otherwise, <c>false</c>.</returns>
        public static bool IsRealNumber(Decimal128 value) => !IsNaN(value);

        /// <summary>Determines if a value represents an integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an integer; otherwise, <c>false</c>.</returns>
        public static bool IsInteger(Decimal128 value) => Number.IsIntegerDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <summary>Determines if a value represents an even integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an even integer; otherwise, <c>false</c>.</returns>
        public static bool IsEvenInteger(Decimal128 value) => Number.IsEvenIntegerDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <summary>Determines if a value represents an odd integral value.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is an odd integer; otherwise, <c>false</c>.</returns>
        public static bool IsOddInteger(Decimal128 value) => Number.IsOddIntegerDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <summary>Determines if a value is normal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is normal; otherwise, <c>false</c>.</returns>
        public static bool IsNormal(Decimal128 value) => Number.IsNormalDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <summary>Determines if a value is subnormal.</summary>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if <paramref name="value" /> is subnormal; otherwise, <c>false</c>.</returns>
        public static bool IsSubnormal(Decimal128 value) => Number.IsSubnormalDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <summary>Compares two values to compute which has the greater magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MaxMagnitude(Decimal128 x, Decimal128 y) => new Decimal128(Number.MaxMagnitudeDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which has the greater magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a greater magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MaxMagnitudeNumber(Decimal128 x, Decimal128 y) => new Decimal128(Number.MaxMagnitudeNumberDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which has the lesser magnitude.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MinMagnitude(Decimal128 x, Decimal128 y) => new Decimal128(Number.MinMagnitudeDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which has the lesser magnitude and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it has a lesser magnitude than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MinMagnitudeNumber(Decimal128 x, Decimal128 y) => new Decimal128(Number.MinMagnitudeNumberDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Computes an estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</summary>
        /// <param name="left">The value to be multiplied with <paramref name="right" />.</param>
        /// <param name="right">The value to be multiplied with <paramref name="left" />.</param>
        /// <param name="addend">The value to be added to the result of <paramref name="left" /> multiplied by <paramref name="right" />.</param>
        /// <returns>An estimate of <c>(<paramref name="left" /> * <paramref name="right" />) + <paramref name="addend" /></c>.</returns>
        public static Decimal128 MultiplyAddEstimate(Decimal128 left, Decimal128 right, Decimal128 addend) => (left * right) + addend;

        /// <summary>Clamps a value to an inclusive minimum and maximum value.</summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The inclusive minimum to which <paramref name="value" /> should clamp.</param>
        /// <param name="max">The inclusive maximum to which <paramref name="value" /> should clamp.</param>
        /// <returns>The result of clamping <paramref name="value" /> to the inclusive range of <paramref name="min" /> and <paramref name="max" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="min" /> is greater than <paramref name="max" />.</exception>
        public static Decimal128 Clamp(Decimal128 value, Decimal128 min, Decimal128 max)
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
        public static Decimal128 ClampNative(Decimal128 value, Decimal128 min, Decimal128 max)
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
        public static Decimal128 CopySign(Decimal128 value, Decimal128 sign) => new Decimal128(Number.CopySignDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower), new UInt128(sign._upper, sign._lower)));

        /// <summary>Compares two values to compute which is greater.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 Max(Decimal128 x, Decimal128 y) => new Decimal128(Number.MaxDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which is greater using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MaxNative(Decimal128 x, Decimal128 y) => new Decimal128(Number.MaxNativeDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which is greater and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is greater than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MaxNumber(Decimal128 x, Decimal128 y) => new Decimal128(Number.MaxNumberDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which is lesser.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 Min(Decimal128 x, Decimal128 y) => new Decimal128(Number.MinDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which is lesser using platform-specific behavior for <c>NaN</c> and <c>NegativeZero</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MinNative(Decimal128 x, Decimal128 y) => new Decimal128(Number.MinNativeDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Compares two values to compute which is lesser and returning the other value if an input is <c>NaN</c>.</summary>
        /// <param name="x">The value to compare with <paramref name="y" />.</param>
        /// <param name="y">The value to compare with <paramref name="x" />.</param>
        /// <returns><paramref name="x" /> if it is less than <paramref name="y" />; otherwise, <paramref name="y" />.</returns>
        public static Decimal128 MinNumber(Decimal128 x, Decimal128 y) => new Decimal128(Number.MinNumberDecimalIeee754<Decimal128, UInt128>(new UInt128(x._upper, x._lower), new UInt128(y._upper, y._lower)));

        /// <summary>Computes the sign of a value.</summary>
        /// <param name="value">The value whose sign is to be computed.</param>
        /// <returns>A positive one if <paramref name="value" /> is positive, a negative one if <paramref name="value" /> is negative, and zero if <paramref name="value" /> is zero.</returns>
        /// <exception cref="ArithmeticException"><paramref name="value" /> is <c>NaN</c>.</exception>
        public static int Sign(Decimal128 value) => Number.SignDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Decimal128>.Radix => 10;

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Decimal128>.IsCanonical(Decimal128 value) => Number.IsCanonicalDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower), nanReservedMask: new UInt128(0x01FF_C000_0000_0000, 0x0000_0000_0000_0000), nanPayloadMask: new UInt128(0x0000_3FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF), maxNaNPayload: new UInt128(0x0000_314D_C644_8D93, 0x38C1_5B09_FFFF_FFFF));

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Decimal128>.IsComplexNumber(Decimal128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Decimal128>.IsImaginaryNumber(Decimal128 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Decimal128>.IsZero(Decimal128 value) => Number.IsZeroDecimalIeee754<Decimal128, UInt128>(new UInt128(value._upper, value._lower));

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Decimal128 IAdditiveIdentity<Decimal128, Decimal128>.AdditiveIdentity => Zero;

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Decimal128 IMultiplicativeIdentity<Decimal128, Decimal128>.MultiplicativeIdentity => One;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal128 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal128 result;

            if (typeof(TOther) == typeof(Decimal128))
            {
                result = (Decimal128)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal128 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal128 result;

            if (typeof(TOther) == typeof(Decimal128))
            {
                result = (Decimal128)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal128 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal128 result;

            if (typeof(TOther) == typeof(Decimal128))
            {
                result = (Decimal128)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal128>.TryConvertFromChecked<TOther>(TOther value, out Decimal128 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal128>.TryConvertFromSaturating<TOther>(TOther value, out Decimal128 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal128>.TryConvertFromTruncating<TOther>(TOther value, out Decimal128 result) => TryConvertFrom(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFrom<TOther>(TOther value, out Decimal128 result)
            where TOther : INumberBase<TOther>
        {
            // Decimal128 must handle every source type itself because the built-in numeric types
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
                result = (long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                result = (Decimal128)(Int128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                result = (Decimal128)(UInt128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (nuint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                result = (Decimal128)(Half)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (Decimal128)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (Decimal128)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (Decimal64)(object)value;
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
        static bool INumberBase<Decimal128>.TryConvertToChecked<TOther>(Decimal128 value, [MaybeNullWhen(false)] out TOther result)
        {
            // Conversions to an integer target throw on overflow, NaN, or infinity. Conversions to a
            // floating-point or narrower decimal target never throw; conversions to `System.Decimal`
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
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (TOther)(object)(Decimal64)value;
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
        static bool INumberBase<Decimal128>.TryConvertToSaturating<TOther>(Decimal128 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal128>.TryConvertToTruncating<TOther>(Decimal128 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertTo<TOther>(Decimal128 value, [MaybeNullWhen(false)] out TOther result)
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
                decimal actualResult = (value > (Decimal128)decimal.MaxValue) ? decimal.MaxValue :
                                       (value < (Decimal128)decimal.MinValue) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal32))
            {
                result = (TOther)(object)(Decimal32)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (TOther)(object)(Decimal64)value;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }


        private static readonly UInt128[] UInt128Powers10 =
            [
                new UInt128(0, 1),
                new UInt128(0, 10),
                new UInt128(0, 100),
                new UInt128(0, 1000),
                new UInt128(0, 10000),
                new UInt128(0, 100000),
                new UInt128(0, 1000000),
                new UInt128(0, 10000000),
                new UInt128(0, 100000000),
                new UInt128(0, 1000000000),
                new UInt128(0, 10000000000),
                new UInt128(0, 100000000000),
                new UInt128(0, 1000000000000),
                new UInt128(0, 10000000000000),
                new UInt128(0, 100000000000000),
                new UInt128(0, 1000000000000000),
                new UInt128(0, 10000000000000000),
                new UInt128(0, 100000000000000000),
                new UInt128(0, 1000000000000000000),
                new UInt128(0, 10000000000000000000),
                new UInt128(5, 7766279631452241920),
                new UInt128(54, 3875820019684212736),
                new UInt128(542, 1864712049423024128),
                new UInt128(5421, 200376420520689664),
                new UInt128(54210, 2003764205206896640),
                new UInt128(542101, 1590897978359414784),
                new UInt128(5421010, 15908979783594147840),
                new UInt128(54210108, 11515845246265065472),
                new UInt128(542101086, 4477988020393345024),
                new UInt128(5421010862, 7886392056514347008),
                new UInt128(54210108624, 5076944270305263616),
                new UInt128(542101086242, 13875954555633532928),
                new UInt128(5421010862427, 9632337040368467968),
                new UInt128(54210108624275, 4089650035136921600),
                new UInt128(542101086242752, 4003012203950112768),
            ];

        static string IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ToDecStr(UInt128 significand)
        {
            return Number.UInt128ToDecStr(significand);
        }

        static unsafe UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberToSignificand(ref Number.NumberBuffer number, int digits)
        {
            if (digits <= 19)
            {
                return Number.DigitsToUInt64(number.DigitsPtr, digits);
            }
            else
            {
                Number.AccumulateDecimalDigitsIntoBigInteger(ref number, 0, (uint)digits, out Number.BigInteger result);
                return result.ToUInt128();
            }
        }

        static Decimal128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Construct(UInt128 value) => new Decimal128(value);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ConvertToExponent(UInt128 value) => (int)value;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Power10(int exponent) => UInt128Powers10[exponent];

        static (UInt128 Quotient, UInt128 Remainder) IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.DivRemPow10(UInt128 value, int exponent)
        {
            UInt128 power = UInt128Powers10[exponent];
            return UInt128.DivRem(value, power);
        }

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.CountDigits(UInt128 significand) => FormattingHelpers.CountDigits(significand);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Precision => Precision;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.BufferLength => Number.Decimal128NumberBufferLength;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxExponent => MaxExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MinExponent => MinExponent;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxAdjustedExponent => MaxExponent - Precision + 1;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MinAdjustedExponent => MinExponent - Precision + 1;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.PositiveInfinity => PositiveInfinityValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NegativeInfinity => NegativeInfinityValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.Zero => ZeroValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NaN => QuietNaNValue;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MostSignificantBitOfSignificandMask => new UInt128(0x0002_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NaNMask => new UInt128(NaNMaskUpper, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.SignMask => new UInt128(SignMaskUpper, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G0G1Mask => new UInt128(0x6000_0000_0000_0000, 0);

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.ExponentBias => ExponentBias;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.NumberBitsSignificand => 110;

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G0ToGwPlus1ExponentMask => new UInt128(0x7FFE_0000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.G2ToGwPlus3ExponentMask => new UInt128(0x1FFF_8000_0000_0000, 0);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.GwPlus2ToGwPlus4SignificandMask => new UInt128(0x0001_FFFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.GwPlus4SignificandMask => new UInt128(0x0000_7FFF_FFFF_FFFF, 0xFFFF_FFFF_FFFF_FFFF);

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.MaxSignificand => new UInt128(upper: 0x0001_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF); // 9_999_999_999_999_999_999_999_999_999_999_999;

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsNaN(UInt128 decimalBits) => IsNaN(new Decimal128(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsNegative(UInt128 decimalBits) => IsNegative(new Decimal128(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsFinite(UInt128 decimalBits) => IsFinite(new Decimal128(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsInfinity(UInt128 decimalBits) => IsInfinity(new Decimal128(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsPositiveInfinity(UInt128 decimalBits) => IsPositiveInfinity(new Decimal128(decimalBits));

        static bool IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.IsNegativeInfinity(UInt128 decimalBits) => IsNegativeInfinity(new Decimal128(decimalBits));

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.EncodeExponentToG0ThroughGwPlus1(uint biasedExponent)
        {
            return ((UInt128)biasedExponent) << 113;
        }

        static UInt128 IDecimalIeee754ParseAndFormatInfo<Decimal128, UInt128>.EncodeExponentToG2ThroughGwPlus3(uint biasedExponent)
        {
            return ((UInt128)biasedExponent) << 111;
        }
    }
}

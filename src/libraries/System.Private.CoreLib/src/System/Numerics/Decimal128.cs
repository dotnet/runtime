// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Numerics
{
    public readonly struct Decimal128
        : IComparable,
          IComparable<Decimal128>,
          IEquatable<Decimal128>,
          ISpanParsable<Decimal128>,
          IMinMaxValue<Decimal128>,
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
        private static UInt128 NegativeInfinityValue => new UInt128(upper: 0xf800_0000_0000_0000, lower: 0);
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

        public static Decimal128 PositiveInfinity => new Decimal128(PositiveInfinityValue);
        public static Decimal128 NegativeInfinity => new Decimal128(NegativeInfinityValue);
        public static Decimal128 NaN => new Decimal128(QuietNaNValue);
        public static Decimal128 NegativeZero => new Decimal128(NegativeZeroValue);
        public static Decimal128 Zero => new Decimal128(ZeroValue);
        public static Decimal128 MinValue => new Decimal128(upper: 0xDFFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);
        public static Decimal128 MaxValue => new Decimal128(upper: 0x5FFF_ED09_BEAD_87C0, lower: 0x378D_8E63_FFFF_FFFF);

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

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s.AsSpan(), style, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal128, UInt128>(s, style, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Decimal128 result, out int bytesConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<byte, Decimal128, UInt128>(utf8Text, style, NumberFormatInfo.GetInstance(provider), out result, out bytesConsumed) == Number.ParsingStatus.OK;
        }

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

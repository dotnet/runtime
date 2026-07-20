// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Numerics
{
    /// <summary>
    /// Represents a decimal floating-point number that uses the IEEE 754 <c>decimal32</c> interchange format, providing 7 decimal digits of precision.
    /// </summary>
    /// <remarks>The IEEE 754 standard defines two interchange encodings for decimal floating-point: binary integer decimal (BID) and densely packed decimal (DPD). Which encoding is used is determined by the underlying ABI for the platform and defaults to BID where the ABI does not otherwise specify.</remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Decimal32
        : IComparable,
          IComparable<Decimal32>,
          IEquatable<Decimal32>,
          IDecimalFloatingPointIeee754<Decimal32>,
          ISpanFormattable,
          ISpanParsable<Decimal32>,
          IMinMaxValue<Decimal32>,
          IUtf8SpanFormattable,
          IUtf8SpanParsable<Decimal32>,
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

        /// <summary>Gets a value that represents positive <c>infinity</c>.</summary>
        public static Decimal32 PositiveInfinity => new Decimal32(PositiveInfinityValue);

        /// <summary>Gets a value that represents negative <c>infinity</c>.</summary>
        public static Decimal32 NegativeInfinity => new Decimal32(NegativeInfinityValue);

        /// <summary>Gets a value that represents <c>NaN</c>.</summary>
        public static Decimal32 NaN => new Decimal32(QuietNaNValue);

        /// <summary>Gets a value that represents negative <c>zero</c>.</summary>
        public static Decimal32 NegativeZero => new Decimal32(NegativeZeroValue);

        /// <summary>Gets the value <c>0</c> for the type.</summary>
        public static Decimal32 Zero => new Decimal32(ZeroValue);

        /// <summary>Gets the minimum value of the current type.</summary>
        public static Decimal32 MinValue => new Decimal32(MinInternalValue);

        /// <summary>Gets the maximum value of the current type.</summary>
        public static Decimal32 MaxValue => new Decimal32(MaxInternalValue);

        /// <summary>Gets the smallest value such that can be added to <c>0</c> that does not result in <c>0</c>.</summary>
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

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal32, uint, char>(_value, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
        }

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        {
            return Number.TryFormatDecimalIeee754<Decimal32, uint, byte>(_value, format, NumberFormatInfo.GetInstance(provider), utf8Destination, out bytesWritten);
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

        /// <summary>Divides two values together to compute their remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        public static Decimal32 operator %(Decimal32 left, Decimal32 right)
        {
            return new Decimal32(Number.RemainderDecimalIeee754<Decimal32, uint>(left._value, right._value));
        }

        //
        // Explicit conversions to Decimal32
        //

        /// <summary>Explicitly converts a <see cref="int" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(int value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, int>(value));

        /// <summary>Explicitly converts a <see cref="uint" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal32(uint value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, uint>(value));

        /// <summary>Explicitly converts a <see cref="System.IntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(nint value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, nint>(value));

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal32(nuint value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, nuint>(value));

        /// <summary>Explicitly converts a <see cref="long" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(long value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, long>(value));

        /// <summary>Explicitly converts a <see cref="ulong" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal32(ulong value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, ulong>(value));

        /// <summary>Explicitly converts a <see cref="System.Int128" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(Int128 value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, Int128>(value));

        /// <summary>Explicitly converts a <see cref="System.UInt128" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator Decimal32(UInt128 value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, UInt128>(value));

        /// <summary>Explicitly converts a <see cref="System.Half" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(Half value) => new Decimal32(Number.ConvertFloatToDecimalIeee754<Half, Decimal32, uint>(value));

        /// <summary>Explicitly converts a <see cref="float" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(float value) => new Decimal32(Number.ConvertFloatToDecimalIeee754<float, Decimal32, uint>(value));

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(double value) => new Decimal32(Number.ConvertFloatToDecimalIeee754<double, Decimal32, uint>(value));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal64" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(Decimal64 value) => new Decimal32(Number.ConvertDecimalIeee754<Decimal64, ulong, Decimal32, uint>(value._value));

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal128" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(Decimal128 value) => new Decimal32(Number.ConvertDecimalIeee754<Decimal128, UInt128, Decimal32, uint>(new UInt128(value._upper, value._lower)));

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static explicit operator Decimal32(decimal value) => new Decimal32(Number.ConvertDecimalToDecimalIeee754<Decimal32, uint>(value));

        //
        // Explicit conversions from Decimal32
        //

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        public static explicit operator byte(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, byte>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        public static explicit operator checked byte(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, byte>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, sbyte>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, sbyte>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        public static explicit operator char(Decimal32 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ushort>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        public static explicit operator checked char(Decimal32 value) => (char)Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ushort>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        public static explicit operator short(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, short>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        public static explicit operator checked short(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, short>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ushort>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ushort>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        public static explicit operator int(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, int>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        public static explicit operator checked int(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, int>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, uint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, uint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        public static explicit operator nint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, nint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.IntPtr" />.</exception>
        public static explicit operator checked nint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, nint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, nuint>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UIntPtr" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, nuint>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        public static explicit operator long(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, long>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        public static explicit operator checked long(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, long>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ulong>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, ulong>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        public static explicit operator Int128(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, Int128>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.Int128" />.</exception>
        public static explicit operator checked Int128(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, Int128>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator UInt128(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, UInt128>(value._value, isChecked: false);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt128" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="System.UInt128" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(Decimal32 value) => Number.ConvertDecimalIeee754ToInteger<Decimal32, uint, UInt128>(value._value, isChecked: true);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Half" /> value.</returns>
        public static explicit operator Half(Decimal32 value) => Number.ConvertDecimalIeee754ToFloat<Decimal32, uint, Half>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        public static explicit operator float(Decimal32 value) => Number.ConvertDecimalIeee754ToFloat<Decimal32, uint, float>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
        public static explicit operator double(Decimal32 value) => Number.ConvertDecimalIeee754ToFloat<Decimal32, uint, double>(value._value);

        /// <summary>Explicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        /// <exception cref="System.OverflowException"><paramref name="value" /> is not representable by <see cref="decimal" />.</exception>
        public static explicit operator decimal(Decimal32 value) => Number.ConvertDecimalIeee754ToDecimal<Decimal32, uint>(value._value);

        //
        // Implicit conversions to Decimal32
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static implicit operator Decimal32(byte value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, byte>(value));

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal32(sbyte value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, sbyte>(value));

        /// <summary>Implicitly converts a <see cref="char" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static implicit operator Decimal32(char value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, ushort>(value));

        /// <summary>Implicitly converts a <see cref="short" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        public static implicit operator Decimal32(short value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, short>(value));

        /// <summary>Implicitly converts a <see cref="ushort" /> value to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal32" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator Decimal32(ushort value) => new Decimal32(Number.ConvertIntegerToDecimalIeee754<Decimal32, uint, ushort>(value));

        //
        // Implicit conversions from Decimal32
        //

        /// <summary>Implicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal64" /> value.</returns>
        public static implicit operator Decimal64(Decimal32 value) => new Decimal64(Number.ConvertDecimalIeee754<Decimal32, uint, Decimal64, ulong>(value._value));

        /// <summary>Implicitly converts a <see cref="System.Numerics.Decimal32" /> value to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Numerics.Decimal128" /> value.</returns>
        public static implicit operator Decimal128(Decimal32 value) => new Decimal128(Number.ConvertDecimalIeee754<Decimal32, uint, Decimal128, UInt128>(value._value));

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

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(string, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s.AsSpan(), style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int charsConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<char, Decimal32, uint>(s, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out charsConsumed) == Number.ParsingStatus.OK;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParsePartial(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf, out int)" />
        public static bool TryParsePartial(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out Decimal32 result, out int bytesConsumed)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.TryParseDecimalIeee754<byte, Decimal32, uint>(utf8Text, style | Number.AllowTrailingInvalidCharacters, NumberFormatInfo.GetInstance(provider), out result, out bytesConsumed) == Number.ParsingStatus.OK;
        }

        /// <summary>
        /// Tries to parse a <see cref="Decimal32"/> from a <see cref="ReadOnlySpan{Byte}"/> containing UTF-8 text in the default parse style.
        /// </summary>
        /// <param name="utf8Text">The UTF-8 input to be parsed.</param>
        /// <param name="result">The equivalent <see cref="Decimal32"/> value representing the input if the parse was successful. If the input exceeds Decimal32's range, a <see cref="PositiveInfinity"/> or <see cref="NegativeInfinity"/> is returned. If the parse was unsuccessful, a default <see cref="Decimal32"/> value is returned.</param>
        /// <returns><see langword="true" /> if the parse was successful, <see langword="false" /> otherwise.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out Decimal32 result) => TryParse(utf8Text, provider: null, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            NumberFormatInfo.ValidateParseStyleDecimal(style);
            return Number.ParseDecimalIeee754<byte, Decimal32, uint>(utf8Text, style, NumberFormatInfo.GetInstance(provider));
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static Decimal32 Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out Decimal32 result) => Number.TryParseDecimalIeee754<byte, Decimal32, uint>(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider), out result, out _) == Number.ParsingStatus.OK;

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

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static Decimal32 Ceiling(Decimal32 x) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, 0, MidpointRounding.ToPositiveInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToInteger{TInteger}(TSelf)" />
        public static TInteger ConvertToInteger<TInteger>(Decimal32 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.ConvertToIntegerNative{TInteger}(TSelf)" />
        public static TInteger ConvertToIntegerNative<TInteger>(Decimal32 value)
            where TInteger : IBinaryInteger<TInteger> => TInteger.CreateSaturating(value);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static Decimal32 Floor(Decimal32 x) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, 0, MidpointRounding.ToNegativeInfinity));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static Decimal32 Round(Decimal32 x) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, 0, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static Decimal32 Round(Decimal32 x, int digits) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, digits, MidpointRounding.ToEven));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static Decimal32 Round(Decimal32 x, MidpointRounding mode) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, 0, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static Decimal32 Round(Decimal32 x, int digits, MidpointRounding mode) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, digits, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static Decimal32 Truncate(Decimal32 x) => new Decimal32(Number.RoundDecimalIeee754<Decimal32, uint>(x._value, 0, MidpointRounding.ToZero));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<Decimal32>.GetExponentByteCount() => sizeof(int);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<Decimal32>.GetExponentShortestBitLength()
        {
            int exponent = Number.UnpackDecimalIeee754<Decimal32, uint>(_value).UnbiasedExponent;

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
        int IFloatingPoint<Decimal32>.GetSignificandBitLength() => 24;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<Decimal32>.GetSignificandByteCount() => sizeof(uint);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32BigEndian(destination, Number.UnpackDecimalIeee754<Decimal32, uint>(_value).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteInt32LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal32, uint>(_value).UnbiasedExponent))
            {
                bytesWritten = sizeof(int);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt32BigEndian(destination, Number.UnpackDecimalIeee754<Decimal32, uint>(_value).Significand))
            {
                bytesWritten = sizeof(uint);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<Decimal32>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (BinaryPrimitives.TryWriteUInt32LittleEndian(destination, Number.UnpackDecimalIeee754<Decimal32, uint>(_value).Significand))
            {
                bytesWritten = sizeof(uint);
                return true;
            }

            bytesWritten = 0;
            return false;
        }

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static Decimal32 Acos(Decimal32 x) => new Decimal32(Number.AcosDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static Decimal32 AcosPi(Decimal32 x) => new Decimal32(Number.AcosPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static Decimal32 Acosh(Decimal32 x) => new Decimal32(Number.AcoshDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static Decimal32 Asin(Decimal32 x) => new Decimal32(Number.AsinDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static Decimal32 AsinPi(Decimal32 x) => new Decimal32(Number.AsinPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static Decimal32 Asinh(Decimal32 x) => new Decimal32(Number.AsinhDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static Decimal32 Atan(Decimal32 x) => new Decimal32(Number.AtanDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static Decimal32 Atan2(Decimal32 y, Decimal32 x) => new Decimal32(Number.Atan2DecimalIeee754<Decimal32, uint>(y._value, x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static Decimal32 Atan2Pi(Decimal32 y, Decimal32 x) => new Decimal32(Number.Atan2PiDecimalIeee754<Decimal32, uint>(y._value, x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static Decimal32 AtanPi(Decimal32 x) => new Decimal32(Number.AtanPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static Decimal32 Atanh(Decimal32 x) => new Decimal32(Number.AtanhDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static Decimal32 BitDecrement(Decimal32 x) => new Decimal32(Number.BitDecrementDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static Decimal32 BitIncrement(Decimal32 x) => new Decimal32(Number.BitIncrementDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static Decimal32 Cbrt(Decimal32 x) => new Decimal32(Number.CbrtDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static Decimal32 Cos(Decimal32 x) => new Decimal32(Number.CosDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static Decimal32 CosPi(Decimal32 x) => new Decimal32(Number.CosPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static Decimal32 Cosh(Decimal32 x) => new Decimal32(Number.CoshDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp(TSelf)" />
        public static Decimal32 Exp(Decimal32 x) => new Decimal32(Number.ExpDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static Decimal32 Exp10(Decimal32 x) => new Decimal32(Number.Exp10DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static Decimal32 Exp10M1(Decimal32 x) => new Decimal32(Number.Exp10M1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static Decimal32 Exp2(Decimal32 x) => new Decimal32(Number.Exp2DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static Decimal32 Exp2M1(Decimal32 x) => new Decimal32(Number.Exp2M1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static Decimal32 ExpM1(Decimal32 x) => new Decimal32(Number.ExpM1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static Decimal32 FusedMultiplyAdd(Decimal32 left, Decimal32 right, Decimal32 addend) => new Decimal32(Number.FusedMultiplyAddDecimalIeee754<Decimal32, uint>(left._value, right._value, addend._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static Decimal32 Hypot(Decimal32 x, Decimal32 y) => new Decimal32(Number.HypotDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static Decimal32 Ieee754Remainder(Decimal32 left, Decimal32 right) => new Decimal32(Number.Ieee754RemainderDecimalIeee754<Decimal32, uint>(left._value, right._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(Decimal32 x) => Number.ILogBDecimalIeee754<Decimal32, uint>(x._value);

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static Decimal32 Log(Decimal32 x) => new Decimal32(Number.LogDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static Decimal32 Log(Decimal32 x, Decimal32 newBase) => new Decimal32(Number.LogDecimalIeee754<Decimal32, uint>(x._value, newBase._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static Decimal32 Log10(Decimal32 x) => new Decimal32(Number.Log10DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static Decimal32 Log10P1(Decimal32 x) => new Decimal32(Number.Log10P1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2(TSelf)" />
        public static Decimal32 Log2(Decimal32 x) => new Decimal32(Number.Log2DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static Decimal32 Log2P1(Decimal32 x) => new Decimal32(Number.Log2P1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static Decimal32 LogP1(Decimal32 x) => new Decimal32(Number.LogP1DecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static Decimal32 Pow(Decimal32 x, Decimal32 y) => new Decimal32(Number.PowDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static Decimal32 RootN(Decimal32 x, int n) => new Decimal32(Number.RootNDecimalIeee754<Decimal32, uint>(x._value, n));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static Decimal32 ScaleB(Decimal32 x, int n) => new Decimal32(Number.ScaleBDecimalIeee754<Decimal32, uint>(x._value, n));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static Decimal32 Sin(Decimal32 x) => new Decimal32(Number.SinDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (Decimal32 Sin, Decimal32 Cos) SinCos(Decimal32 x)
        {
            (uint sin, uint cos) = Number.SinCosDecimalIeee754<Decimal32, uint>(x._value);
            return (new Decimal32(sin), new Decimal32(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCosPi(TSelf)" />
        public static (Decimal32 SinPi, Decimal32 CosPi) SinCosPi(Decimal32 x)
        {
            (uint sin, uint cos) = Number.SinCosPiDecimalIeee754<Decimal32, uint>(x._value);
            return (new Decimal32(sin), new Decimal32(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static Decimal32 SinPi(Decimal32 x) => new Decimal32(Number.SinPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static Decimal32 Sinh(Decimal32 x) => new Decimal32(Number.SinhDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static Decimal32 Sqrt(Decimal32 x) => new Decimal32(Number.SqrtDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static Decimal32 Tan(Decimal32 x) => new Decimal32(Number.TanDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static Decimal32 TanPi(Decimal32 x) => new Decimal32(Number.TanPiDecimalIeee754<Decimal32, uint>(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static Decimal32 Tanh(Decimal32 x) => new Decimal32(Number.TanhDecimalIeee754<Decimal32, uint>(x._value));

        /// <summary>Adjusts a value to the quantum (exponent) of another value, rounding to nearest with ties to even.</summary>
        /// <param name="x">The value whose quantum is adjusted.</param>
        /// <param name="y">The value that provides the target quantum.</param>
        /// <returns><paramref name="x" /> expressed with the quantum of <paramref name="y" />, or NaN when the value cannot be represented at that quantum.</returns>
        public static Decimal32 Quantize(Decimal32 x, Decimal32 y) => new Decimal32(Number.QuantizeDecimalIeee754<Decimal32, uint>(x._value, y._value));

        /// <summary>Computes the quantum of a value: one unit in the last place sharing its exponent.</summary>
        /// <param name="x">The value whose quantum is returned.</param>
        /// <returns>The quantum of <paramref name="x" />.</returns>
        public static Decimal32 Quantum(Decimal32 x) => new Decimal32(Number.QuantumDecimalIeee754<Decimal32, uint>(x._value));

        /// <summary>Determines whether two values have the same quantum (exponent).</summary>
        /// <param name="x">The first value to compare.</param>
        /// <param name="y">The second value to compare.</param>
        /// <returns><c>true</c> if <paramref name="x" /> and <paramref name="y" /> have the same quantum; otherwise, <c>false</c>.</returns>
        public static bool SameQuantum(Decimal32 x, Decimal32 y) => Number.SameQuantumDecimalIeee754<Decimal32, uint>(x._value, y._value);

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

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<Decimal32>.Radix => 10;

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<Decimal32>.IsCanonical(Decimal32 value) => Number.IsCanonicalDecimalIeee754<Decimal32, uint>(value._value, nanReservedMask: 0x01F0_0000, nanPayloadMask: 0x000F_FFFF, maxNaNPayload: 999_999);

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<Decimal32>.IsComplexNumber(Decimal32 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<Decimal32>.IsImaginaryNumber(Decimal32 value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<Decimal32>.IsZero(Decimal32 value) => Number.IsZeroDecimalIeee754<Decimal32, uint>(value._value);

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static Decimal32 IAdditiveIdentity<Decimal32, Decimal32>.AdditiveIdentity => Zero;

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static Decimal32 IMultiplicativeIdentity<Decimal32, Decimal32>.MultiplicativeIdentity => One;

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Decimal32 CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            Decimal32 result;

            if (typeof(TOther) == typeof(Decimal32))
            {
                result = (Decimal32)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromChecked<TOther>(TOther value, out Decimal32 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromSaturating<TOther>(TOther value, out Decimal32 result) => TryConvertFrom(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertFromTruncating<TOther>(TOther value, out Decimal32 result) => TryConvertFrom(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertFrom<TOther>(TOther value, out Decimal32 result)
            where TOther : INumberBase<TOther>
        {
            // Decimal32 must handle every source type itself because the built-in numeric types
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
                result = (Decimal32)(int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (Decimal32)(uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (Decimal32)(long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (Decimal32)(ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                result = (Decimal32)(Int128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                result = (Decimal32)(UInt128)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (Decimal32)(nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (Decimal32)(nuint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                result = (Decimal32)(Half)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (Decimal32)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (Decimal32)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (Decimal32)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (Decimal32)(Decimal64)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(Decimal128))
            {
                result = (Decimal32)(Decimal128)(object)value;
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
        static bool INumberBase<Decimal32>.TryConvertToChecked<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result)
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
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (TOther)(object)(Decimal64)value;
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
        static bool INumberBase<Decimal32>.TryConvertToSaturating<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<Decimal32>.TryConvertToTruncating<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result) => TryConvertTo(value, out result);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryConvertTo<TOther>(Decimal32 value, [MaybeNullWhen(false)] out TOther result)
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
                // `decimal.MaxValue`/`decimal.MinValue` are not exactly representable as `Decimal32` and
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
            else if (typeof(TOther) == typeof(Decimal64))
            {
                result = (TOther)(object)(Decimal64)value;
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

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MaxAdjustedExponent => MaxExponent - Precision + 1;

        static int IDecimalIeee754ParseAndFormatInfo<Decimal32, uint>.MinAdjustedExponent => MinExponent - Precision + 1;

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

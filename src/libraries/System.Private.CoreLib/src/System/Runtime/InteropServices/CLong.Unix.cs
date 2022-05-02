// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_WINDOWS
#error "Unsupported target"
#else
// We don't specify a using directive since IntPtr doesn't expose any operators yet, but nint does
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// <see cref="CLong"/> is an immutable value type that represents the <c>long</c> type in C and C++.
    /// It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent
    /// in managed code unmanaged APIs that use the <c>long</c> type.
    /// This type has 32-bits of storage on all Windows platforms and 32-bit Unix-based platforms.
    /// It has 64-bits of storage on 64-bit Unix platforms.
    /// </summary>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly struct CLong
        : IComparable,
          ISpanFormattable,
          IComparable<CLong>,
          IEquatable<CLong>,
          IBinaryInteger<CLong>,
          IMinMaxValue<CLong>,
          ISignedNumber<CLong>
    {
        private readonly nint _value;

        /// <summary>
        /// Constructs an instance from a 32-bit integer.
        /// </summary>
        /// <param name="value">The integer vaule.</param>
        public CLong(int value)
        {
            _value = (nint)value;
        }

        /// <summary>
        /// Constructs an instance from a native sized integer.
        /// </summary>
        /// <param name="value">The integer vaule.</param>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying storage type.</exception>
        public CLong(nint value)
        {
            _value = checked((nint)value);
        }

        /// <summary>
        /// The underlying integer value of this instance.
        /// </summary>
        public nint Value => _value;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="o">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="o"/> is an instance of <see cref="CLong"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? o) => o is CLong other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="CLong"/> value.
        /// </summary>
        /// <param name="other">A <see cref="CLong"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(CLong other) => _value == other._value;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>Converts the string representation of a number to its integral number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <returns>An integral number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static CLong Parse(string s) => Parse(s, NumberStyles.Integer);

        /// <summary>Converts the string representation of a number in a specified style to its integral number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <returns>A integral number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static CLong Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance, consisting of a negative sign if the value is negative, and a sequence of digits ranging from 0 to 9 with no leading zeroes.</returns>
        public override string ToString() => _value.ToString();

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified format.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format" />.</returns>
        /// <exception cref="FormatException"><paramref name="format" /> is invalid.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => _value.ToString(format);

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.</summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="provider" />.</returns>
        public string ToString(IFormatProvider? provider) => _value.ToString(provider);

        /// <summary>Tries to convert the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <c>null</c>, <see cref="string.Empty" />, or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out CULong result)
        {
            Unsafe.SkipInit(out result);
            return nint.TryParse(s, out Unsafe.As<CULong, nint>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="string.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out CULong result)
        {
            Unsafe.SkipInit(out result);
            return nint.TryParse(s, out Unsafe.As<CULong, nint>(ref result));
        }

        //
        // Explicit Convert To CLong
        //

        /// <summary>Explicitly converts a <see cref="decimal" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CLong(decimal value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CLong(double value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked CLong(double value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="long" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CLong(long value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="long" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked CLong(long value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CLong(nint value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked CLong(nint value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="float" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CLong(float value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="float" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked CLong(float value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="uint" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CLong(uint value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="uint" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CLong(uint value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="ulong" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CLong(ulong value) => new CLong((nint)value);

        /// <summary>Explicitly converts a <see cref="ulong" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CLong(ulong value) => new CLong(checked((nint)value));

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CLong(nuint value)
        {
            return new CLong((nint)value);
        }

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CLong(nuint value)
        {
            return new CLong(checked((nint)value));
        }

        //
        // Explicit Convert From CLong
        //

        /// <summary>Explicitly converts a c-sized long value to a <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" /> value.</returns>
        public static explicit operator byte(CLong value) => (byte)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked byte(CLong value) => checked((byte)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" /> value.</returns>
        public static explicit operator char(CLong value) => (char)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked char(CLong value) => checked((char)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" /> value.</returns>
        public static explicit operator short(CLong value) => (short)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked short(CLong value) => checked((short)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" /> value.</returns>
        public static explicit operator int(CLong value) => (int)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        public static explicit operator checked int(CLong value) => checked((int)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(CLong value) => (sbyte)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(CLong value) => checked((sbyte)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(CLong value) => (ushort)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(CLong value) => checked((ushort)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(CLong value) => (uint)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(CLong value) => checked((uint)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ulong(CLong value) => (ulong)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ulong(CLong value) => checked((ulong)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator nuint(CLong value) => (nuint)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CLong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked nuint(CLong value) => checked((nuint)(value._value));

        //
        // Implicit Convert To CLong
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CLong(byte value) => new CLong((nint)value);

        /// <summary>Implicitly converts a <see cref="char" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CLong(char value) => new CLong((nint)value);

        /// <summary>Implicitly converts a <see cref="short" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CLong(short value) => new CLong((nint)value);

        /// <summary>Implicitly converts a <see cref="int" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CLong(int value) => new CLong((nint)value);

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static implicit operator CLong(sbyte value) => new CLong((nint)value);

        /// <summary>Implicitly converts a <see cref="ushort" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static implicit operator CLong(ushort value) => new CLong((nint)value);

        //
        // Implicit Convert From CLong
        //

        /// <summary>Implicitly converts a c-sized long value to a <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="decimal" /> value.</returns>
        public static implicit operator decimal(CLong value) => (decimal)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="double" /> value.</returns>
        public static implicit operator double(CLong value) => (double)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" /> value.</returns>
        public static implicit operator long(CLong value) => (long)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="System.IntPtr" /> value.</returns>
        public static implicit operator nint(CLong value) => (nint)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="float" /> value.</returns>
        public static implicit operator float(CLong value) => (float)(value._value);

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static CLong operator +(CLong left, CLong right) => new CLong(left._value + right._value);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static CLong operator checked +(CLong left, CLong right) => new CLong(checked(left._value + right._value));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static CLong IAdditiveIdentity<CLong, CLong>.AdditiveIdentity => new CLong((nint)0);

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (CLong Quotient, CLong Remainder) DivRem(CLong left, CLong right)
        {
            (nint quotient, nint remainder) = nint.DivRem(left._value, right._value);
            return (new CLong(quotient), new CLong(remainder));
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static CLong LeadingZeroCount(CLong value) => new CLong(nint.LeadingZeroCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static CLong PopCount(CLong value) => new CLong(nint.PopCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static CLong RotateLeft(CLong value, int rotateAmount) => new CLong(nint.RotateLeft(value._value, rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static CLong RotateRight(CLong value, int rotateAmount) => new CLong(nint.RotateRight(value._value, rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static CLong TrailingZeroCount(CLong value) => new CLong(nint.TrailingZeroCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        unsafe long IBinaryInteger<CLong>.GetShortestBitLength()
        {
            nint value = _value;

            if (value >= 0)
            {
                return (sizeof(nint) * 8) - nint.LeadingZeroCount(value);
            }
            else
            {
                return (sizeof(nint) * 8) + 1 - nint.LeadingZeroCount(~value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        unsafe int IBinaryInteger<CLong>.GetByteCount() => sizeof(nint);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        unsafe bool IBinaryInteger<CLong>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(CLong))
            {
                nint value = BitConverter.IsLittleEndian ? _value : BinaryPrimitives.ReverseEndianness(_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(CLong);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(CLong value) => nint.IsPow2(value._value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static CLong Log2(CLong value) => new CLong(nint.Log2(value._value));

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        public static CLong operator &(CLong left, CLong right) => new CLong(left._value & right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        public static CLong operator |(CLong left, CLong right) => new CLong(left._value | right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        public static CLong operator ^(CLong left, CLong right) => new CLong(left._value ^ right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        public static CLong operator ~(CLong value) => new CLong(~value._value);

        //
        // IComparable
        //

        public int CompareTo(object? value)
        {
            if (value is CLong other)
            {
                return CompareTo(other);
            }
            return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeCLong);
        }

        public int CompareTo(CLong value) => _value.CompareTo(value._value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(CLong left, CLong right) => left._value < right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(CLong left, CLong right) => left._value <= right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(CLong left, CLong right) => left._value > right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(CLong left, CLong right) => left._value >= right._value;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static CLong operator --(CLong value)
        {
            nint tmp = value._value;
            --tmp;
            return new CLong(tmp);
        }

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static CLong operator checked --(CLong value)
        {
            nint tmp = value._value;

            checked
            {
                --tmp;
            }
            return new CLong(tmp);
        }

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static CLong operator /(CLong left, CLong right) => new CLong(left._value / right._value);

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static CLong IDivisionOperators<CLong, CLong, CLong>.operator checked /(CLong left, CLong right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(CLong left, CLong right) => left._value == right._value;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(CLong left, CLong right) => left._value != right._value;

        //
        // IFormattable
        //

        /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
        public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static CLong operator ++(CLong value)
        {
            nint tmp = value._value;
            ++tmp;
            return new CLong(tmp);
        }

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        public static CLong operator checked ++(CLong value)
        {
            nint tmp = value._value;

            checked
            {
                ++tmp;
            }
            return new CLong(tmp);
        }

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static CLong MinValue => new CLong(nint.MinValue);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static CLong MaxValue => new CLong(nint.MaxValue);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static CLong operator %(CLong left, CLong right) => new CLong(left._value % right._value);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static CLong IMultiplicativeIdentity<CLong, CLong>.MultiplicativeIdentity => new CLong((nint)1);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static CLong operator *(CLong left, CLong right) => new CLong(left._value * right._value);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        public static CLong operator checked *(CLong left, CLong right) => new CLong(checked(left._value * right._value));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static CLong Abs(CLong value) => new CLong(nint.Abs(value._value));

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static CLong Clamp(CLong value, CLong min, CLong max) => new CLong(nint.Clamp(value._value, min._value, max._value));

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static CLong CopySign(CLong value, CLong sign) => new CLong(nint.CopySign(value._value, sign._value));

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CLong CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther> => new CLong(IntPtr.CreateChecked(value));

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CLong CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther> => new CLong(IntPtr.CreateSaturating(value));

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CLong CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther> => new CLong(IntPtr.CreateTruncating(value));

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(CLong value) => nint.IsNegative(value._value);

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static CLong Max(CLong x, CLong y) => new CLong(nint.Max(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static CLong MaxMagnitude(CLong x, CLong y) => new CLong(nint.MaxMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static CLong Min(CLong x, CLong y) => new CLong(nint.Min(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static CLong MinMagnitude(CLong x, CLong y) => new CLong(nint.MinMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Parse(string, NumberStyles, IFormatProvider?)" />
        public static CLong Parse(string s, NumberStyles style, IFormatProvider? provider) => new CLong(nint.Parse(s, style, provider));

        /// <inheritdoc cref="INumber{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static CLong Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => new CLong(nint.Parse(s, style, provider));

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(CLong value) => nint.Sign(value._value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out CLong result)
            where TOther : INumber<TOther>
        {
            Unsafe.SkipInit(out result);
            return IntPtr.TryCreate(value, out Unsafe.As<CLong, nint>(ref result));
        }

        /// <inheritdoc cref="INumber{TSelf}.TryParse(string?, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out CLong result)
        {
            Unsafe.SkipInit(out result);
            return nint.TryParse(s, style, provider, out Unsafe.As<CLong, nint>(ref result));
        }


        /// <inheritdoc cref="INumber{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out CLong result)
        {
            Unsafe.SkipInit(out result);
            return nint.TryParse(s, style, provider, out Unsafe.As<CLong, nint>(ref result));
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static CLong INumberBase<CLong>.One => new CLong((nint)1);

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static CLong INumberBase<CLong>.Zero => new CLong((nint)0);

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
        public static CLong Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CLong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        public static CLong operator <<(CLong value, int shiftAmount) => new CLong(value._value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        public static CLong operator >>(CLong value, int shiftAmount) => new CLong(value._value >> shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        public static CLong operator >>>(CLong value, int shiftAmount) => new CLong(value._value >>> shiftAmount);

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static CLong ISignedNumber<CLong>.NegativeOne => new CLong((nint)(-1));

        //
        // ISpanFormattable
        //

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _value.TryFormat(destination, out charsWritten, format, provider);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static CLong Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CLong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static CLong operator -(CLong left, CLong right) => new CLong(left._value - right._value);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        public static CLong operator checked -(CLong left, CLong right) => new CLong(checked(left._value - right._value));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        public static CLong operator -(CLong value) => new CLong(-value._value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        public static CLong operator checked -(CLong value) => new CLong(checked(-value._value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static CLong operator +(CLong value) => value;
    }
}

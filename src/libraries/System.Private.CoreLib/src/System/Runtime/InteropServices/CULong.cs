// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_WINDOWS
using NativeType = System.UInt32;
#else
using NativeType = System.UIntPtr;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// <see cref="CULong"/> is an immutable value type that represents the <c>unsigned long</c> type in C and C++.
    /// It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent
    /// in managed code unmanaged APIs that use the <c>unsigned long</c> type.
    /// This type has 32-bits of storage on all Windows platforms and 32-bit Unix-based platforms.
    /// It has 64-bits of storage on 64-bit Unix platforms.
    /// </summary>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly struct CULong
        : IComparable,
          ISpanFormattable,
          IComparable<CULong>,
          IEquatable<CULong>,
          IBinaryInteger<CULong>,
          IMinMaxValue<CULong>,
          IUnsignedNumber<CULong>
    {
        private readonly NativeType _value;

        /// <summary>
        /// Constructs an instance from a 32-bit unsigned integer.
        /// </summary>
        /// <param name="value">The integer vaule.</param>
        public CULong(uint value)
        {
            _value = (NativeType)value;
        }

        /// <summary>
        /// Constructs an instance from a native sized unsigned integer.
        /// </summary>
        /// <param name="value">The integer vaule.</param>
        /// <exception cref="OverflowException"><paramref name="value"/> is outside the range of the underlying storage type.</exception>
        public CULong(nuint value)
        {
            _value = checked((NativeType)value);
        }

        /// <summary>
        /// The underlying integer value of this instance.
        /// </summary>
        public nuint Value => _value;

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="o">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="o"/> is an instance of <see cref="CULong"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? o) => o is CULong other && Equals(other);

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified <see cref="CLong"/> value.
        /// </summary>
        /// <param name="other">A <see cref="CULong"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(CULong other) => _value == other._value;

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
        public static CULong Parse(string s) => Parse(s, NumberStyles.Integer);

        /// <summary>Converts the string representation of a number in a specified style to its integral number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <returns>A integral number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException"><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static CULong Parse(string s, NumberStyles style) => Parse(s, style, provider: null);

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance, consisting of a sequence of digits ranging from 0 to 9 with no leading zeroes.</returns>
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
            return NativeType.TryParse(s, out Unsafe.As<CULong, NativeType>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="string.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out CULong result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, out Unsafe.As<CULong, NativeType>(ref result));
        }

        //
        // Explicit Convert To CULong
        //

        /// <summary>Explicitly converts a <see cref="decimal" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(decimal value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(double value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(double value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="short" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(short value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="short" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(short value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="int" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(int value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="int" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(int value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="long" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(long value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="long" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(long value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(nint value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="IntPtr" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(nint value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="sbyte" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CULong(sbyte value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="sbyte" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CULong(sbyte value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="float" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static explicit operator CULong(float value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="float" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked CULong(float value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="ulong" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CULong(ulong value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="ulong" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CULong(ulong value) => new CULong(checked((NativeType) value));

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static explicit operator CULong(nuint value) => new CULong((NativeType)value);

        /// <summary>Explicitly converts a <see cref="System.UIntPtr" /> value to a c-sized long value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked CULong(nuint value) => new CULong(checked((NativeType) value));

        //
        // Explicit Convert From CULong
        //

        /// <summary>Explicitly converts a c-sized long value to a <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" /> value.</returns>
        public static explicit operator byte(CULong value) => (byte)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked byte(CULong value) => checked((byte)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" /> value.</returns>
        public static explicit operator char(CULong value) => (char)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked char(CULong value) => checked((char)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" /> value.</returns>
        public static explicit operator short(CULong value) => (short)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked short(CULong value) => checked((short)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" /> value.</returns>
        public static explicit operator int(CULong value) => (int)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked int(CULong value) => checked((int)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" /> value.</returns>
        public static explicit operator long(CULong value) => (long)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="long" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked long(CULong value) => checked((long)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="System.IntPtr" /> value.</returns>
        public static explicit operator nint(CULong value) => (nint)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="System.IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="System.IntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        public static explicit operator checked nint(CULong value) => checked((nint)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator sbyte(CULong value) => (sbyte)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(CULong value) => checked((sbyte)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator ushort(CULong value) => (ushort)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked ushort(CULong value) => checked((ushort)(value._value));

        /// <summary>Explicitly converts a c-sized long value to a <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" /> value.</returns>
        [CLSCompliant(false)]
        public static explicit operator uint(CULong value) => (uint)(value._value);

        /// <summary>Explicitly converts a c-sized long value to a <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="CULong" />.</exception>
        [CLSCompliant(false)]
        public static explicit operator checked uint(CULong value) => checked((uint)(value._value));

        //
        // Implicit Convert To CULong
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CULong(byte value) => new CULong((NativeType)value);

        /// <summary>Implicitly converts a <see cref="char" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        public static implicit operator CULong(char value) => new CULong((NativeType)value);

        /// <summary>Implicitly converts a <see cref="ushort" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static implicit operator CULong(ushort value) => new CULong((NativeType)value);

        /// <summary>Implicitly converts a <see cref="uint" /> value to a c-sized long value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a c-sized long value.</returns>
        [CLSCompliant(false)]
        public static implicit operator CULong(uint value) => new CULong((NativeType)value);

        //
        // Implicit Convert From CULong
        //

        /// <summary>Implicitly converts a c-sized long value to a <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="decimal" /> value.</returns>
        public static implicit operator decimal(CULong value) => (decimal)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="double" /> value.</returns>
        public static implicit operator double(CULong value) => (double)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="float" /> value.</returns>
        public static implicit operator float(CULong value) => (float)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="ulong" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator ulong(CULong value) => (ulong)(value._value);

        /// <summary>Implicitly converts a c-sized long value to a <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to a <see cref="UIntPtr" /> value.</returns>
        [CLSCompliant(false)]
        public static implicit operator nuint(CULong value) => (nuint)(value._value);

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static CULong operator +(CULong left, CULong right) => new CULong(left._value + right._value);

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        public static CULong operator checked +(CULong left, CULong right) => new CULong(checked(left._value + right._value));

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static CULong IAdditiveIdentity<CULong, CULong>.AdditiveIdentity => new CULong(NativeType.AdditiveIdentity);

        //
        // IBinaryInteger
        //

        /// <inheritdoc cref="IBinaryInteger{TSelf}.DivRem(TSelf, TSelf)" />
        public static (CULong Quotient, CULong Remainder) DivRem(CULong left, CULong right)
        {
            (NativeType quotient, NativeType remainder) = NativeType.DivRem(left._value, right._value);
            return (new CULong(quotient), new CULong(remainder));
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.LeadingZeroCount(TSelf)" />
        public static CULong LeadingZeroCount(CULong value) => new CULong(NativeType.LeadingZeroCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.PopCount(TSelf)" />
        public static CULong PopCount(CULong value) => new CULong(NativeType.PopCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateLeft(TSelf, int)" />
        public static CULong RotateLeft(CULong value, int rotateAmount) => new CULong(NativeType.RotateLeft(value._value, rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.RotateRight(TSelf, int)" />
        public static CULong RotateRight(CULong value, int rotateAmount) => new CULong(NativeType.RotateRight(value._value, rotateAmount));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TrailingZeroCount(TSelf)" />
        public static CULong TrailingZeroCount(CULong value) => new CULong(NativeType.TrailingZeroCount(value._value));

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetShortestBitLength()" />
        long IBinaryInteger<CULong>.GetShortestBitLength()
        {
            NativeType value = _value;

            if (value >= 0)
            {
                return (sizeof(NativeType) * 8) - NativeType.LeadingZeroCount(value);
            }
            else
            {
                return (sizeof(NativeType) * 8) + 1 - NativeType.LeadingZeroCount(~value);
            }
        }

        /// <inheritdoc cref="IBinaryInteger{TSelf}.GetByteCount()" />
        int IBinaryInteger<CULong>.GetByteCount() => sizeof(NativeType);

        /// <inheritdoc cref="IBinaryInteger{TSelf}.TryWriteLittleEndian(Span{byte}, out int)" />
        unsafe bool IBinaryInteger<CULong>.TryWriteLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(CULong))
            {
                NativeType value = BitConverter.IsLittleEndian ? _value : BinaryPrimitives.ReverseEndianness(_value);
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);

                bytesWritten = sizeof(CULong);
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
        public static bool IsPow2(CULong value) => NativeType.IsPow2(value._value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static CULong Log2(CULong value) => new CULong(NativeType.Log2(value._value));

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        public static CULong operator &(CULong left, CULong right) => new CULong(left._value & right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        public static CULong operator |(CULong left, CULong right) => new CULong(left._value | right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        public static CULong operator ^(CULong left, CULong right) => new CULong(left._value ^ right._value);

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        public static CULong operator ~(CULong value) => new CULong(~value._value);

        //
        // IComparable
        //

        public int CompareTo(object? value)
        {
            if (value is CULong other)
            {
                return CompareTo(other);
            }
            return (value is null) ? 1 : throw new ArgumentException(SR.Arg_MustBeCULong);
        }

        public int CompareTo(CULong value) => _value.CompareTo(value._value);

        //
        // IComparisonOperators
        //

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThan(TSelf, TOther)" />
        public static bool operator <(CULong left, CULong right) => left._value < right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_LessThanOrEqual(TSelf, TOther)" />
        public static bool operator <=(CULong left, CULong right) => left._value <= right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThan(TSelf, TOther)" />
        public static bool operator >(CULong left, CULong right) => left._value > right._value;

        /// <inheritdoc cref="IComparisonOperators{TSelf, TOther}.op_GreaterThanOrEqual(TSelf, TOther)" />
        public static bool operator >=(CULong left, CULong right) => left._value >= right._value;

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static CULong operator --(CULong value)
        {
            NativeType tmp = value._value;
            --tmp;
            return new CULong(tmp);
        }

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        public static CULong operator checked --(CULong value)
        {
            NativeType tmp = value._value;

            checked
            {
                --tmp;
            }
            return new CULong(tmp);
        }

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_Division(TSelf, TOther)" />
        public static CULong operator /(CULong left, CULong right) => new CULong(left._value / right._value);

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static CULong IDivisionOperators<CULong, CULong, CULong>.operator checked /(CULong left, CULong right) => left / right;

        //
        // IEqualityOperators
        //

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Equality(TSelf, TOther)" />
        public static bool operator ==(CULong left, CULong right) => left._value == right._value;

        /// <inheritdoc cref="IEqualityOperators{TSelf, TOther}.op_Inequality(TSelf, TOther)" />
        public static bool operator !=(CULong left, CULong right) => left._value != right._value;

        //
        // IFormattable
        //

        /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
        public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_Increment(TSelf)" />
        public static CULong operator ++(CULong value)
        {
            NativeType tmp = value._value;
            ++tmp;
            return new CULong(tmp);
        }

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        public static CULong operator checked ++(CULong value)
        {
            NativeType tmp = value._value;

            checked
            {
                ++tmp;
            }
            return new CULong(tmp);
        }

        //
        // IMinMaxValue
        //

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MinValue" />
        public static CULong MinValue => new CULong(NativeType.MinValue);

        /// <inheritdoc cref="IMinMaxValue{TSelf}.MaxValue" />
        public static CULong MaxValue => new CULong(NativeType.MaxValue);

        //
        // IModulusOperators
        //

        /// <inheritdoc cref="IModulusOperators{TSelf, TOther, TResult}.op_Modulus(TSelf, TOther)" />
        public static CULong operator %(CULong left, CULong right) => new CULong(left._value % right._value);

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static CULong IMultiplicativeIdentity<CULong, CULong>.MultiplicativeIdentity => new CULong(NativeType.MultiplicativeIdentity);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_Multiply(TSelf, TOther)" />
        public static CULong operator *(CULong left, CULong right) => new CULong(left._value * right._value);

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        public static CULong operator checked *(CULong left, CULong right) => new CULong(checked(left._value * right._value));

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static CULong Abs(CULong value) => value;

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static CULong Clamp(CULong value, CULong min, CULong max) => new CULong(NativeType.Clamp(value._value, min._value, max._value));

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static CULong CopySign(CULong value, CULong sign) => value;

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CULong CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther> => new CULong(NativeType.CreateChecked(value));

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CULong CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther> => new CULong(NativeType.CreateSaturating(value));

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CULong CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther> => new CULong(NativeType.CreateTruncating(value));

        /// <inheritdoc cref="INumber{TSelf}.IsNegative(TSelf)" />
        public static bool IsNegative(CULong value) => false;

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static CULong Max(CULong x, CULong y) => new CULong(NativeType.Max(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static CULong MaxMagnitude(CULong x, CULong y) => Max(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static CULong Min(CULong x, CULong y) => new CULong(NativeType.Min(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static CULong MinMagnitude(CULong x, CULong y) => Min(x, y);

        /// <inheritdoc cref="INumber{TSelf}.Parse(string, NumberStyles, IFormatProvider?)" />
        public static CULong Parse(string s, NumberStyles style, IFormatProvider? provider) => new CULong(NativeType.Parse(s, style, provider));

        /// <inheritdoc cref="INumber{TSelf}.Parse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?)" />
        public static CULong Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => new CULong(NativeType.Parse(s, style, provider));

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(CULong value) => NativeType.Sign(value._value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out CULong result)
            where TOther : INumber<TOther>
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryCreate(value, out Unsafe.As<CULong, NativeType>(ref result));
        }

        /// <inheritdoc cref="INumber{TSelf}.TryParse(string?, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out CULong result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, style, provider, out Unsafe.As<CULong, NativeType>(ref result));
        }


        /// <inheritdoc cref="INumber{TSelf}.TryParse(ReadOnlySpan{char}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out CULong result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, style, provider, out Unsafe.As<CULong, NativeType>(ref result));
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static CULong INumberBase<CULong>.One => new CULong(NativeType.One);

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static CULong INumberBase<CULong>.Zero => new CULong(NativeType.Zero);

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" />
        public static CULong Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out CULong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // IShiftOperators
        //

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_LeftShift(TSelf, int)" />
        public static CULong operator <<(CULong value, int shiftAmount) => new CULong(value._value << shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_RightShift(TSelf, int)" />
        public static CULong operator >>(CULong value, int shiftAmount) => new CULong(value._value >> shiftAmount);

        /// <inheritdoc cref="IShiftOperators{TSelf, TResult}.op_UnsignedRightShift(TSelf, int)" />
        public static CULong operator >>>(CULong value, int shiftAmount) => new CULong(value._value >>> shiftAmount);

        //
        // ISpanFormattable
        //

        /// <inheritdoc cref="ISpanFormattable.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" />
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _value.TryFormat(destination, out charsWritten, format, provider);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static CULong Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out CULong result) => TryParse(s, NumberStyles.Integer, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_Subtraction(TSelf, TOther)" />
        public static CULong operator -(CULong left, CULong right) => new CULong(left._value - right._value);

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        public static CULong operator checked -(CULong left, CULong right) => new CULong(checked(left._value - right._value));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_UnaryNegation(TSelf)" />
        static CULong IUnaryNegationOperators<CULong, CULong>.operator -(CULong value) => new CULong(0 - value._value);

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static CULong IUnaryNegationOperators<CULong, CULong>.operator checked -(CULong value) => new CULong(checked(0 - value._value));

        //
        // IUnaryPlusOperators
        //

        /// <inheritdoc cref="IUnaryPlusOperators{TSelf, TResult}.op_UnaryPlus(TSelf)" />
        public static CULong operator +(CULong value) => value;
    }
}

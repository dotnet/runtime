// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_32BIT
using NativeType = System.Single;
#else
using NativeType = System.Double;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>Defines an immutable value type that represents a floating type that has the same size as the native integer size.</summary>
    /// <remarks>It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent in managed code unmanaged APIs that use a type alias for C or C++'s <c>float</c> on 32-bit platforms or <c>double</c> on 64-bit platforms, such as the CGFloat type in libraries provided by Apple.</remarks>
    [Intrinsic]
    public readonly struct NFloat
        : IComparable,
          IComparable<NFloat>,
          IEquatable<NFloat>,
          ISpanFormattable
    {
        private const NumberStyles DefaultNumberStyles = NumberStyles.Float | NumberStyles.AllowThousands;

        private readonly NativeType _value;

        /// <summary>Constructs an instance from a 32-bit floating point value.</summary>
        /// <param name="value">The floating-point value.</param>
        [NonVersionable]
        public NFloat(float value)
        {
            _value = value;
        }

        /// <summary>Constructs an instance from a 64-bit floating point value.</summary>
        /// <param name="value">The floating-point value.</param>
        [NonVersionable]
        public NFloat(double value)
        {
            _value = (NativeType)value;
        }

        /// <summary>Represents the smallest positive NFloat value that is greater than zero.</summary>
        public static NFloat Epsilon
        {
            [NonVersionable]
            get => new NFloat(NativeType.Epsilon);
        }

        /// <summary>Represents the largest finite value of a NFloat.</summary>
        public static NFloat MaxValue
        {
            [NonVersionable]
            get => new NFloat(NativeType.MaxValue);
        }

        /// <summary>Represents the smallest finite value of a NFloat.</summary>
        public static NFloat MinValue
        {
            [NonVersionable]
            get => new NFloat(NativeType.MinValue);
        }

        /// <summary>Represents a value that is not a number (NaN).</summary>
        public static NFloat NaN
        {
            [NonVersionable]
            get => new NFloat(NativeType.NaN);
        }

        /// <summary>Represents negative infinity.</summary>
        public static NFloat NegativeInfinity
        {
            [NonVersionable]
            get => new NFloat(NativeType.NegativeInfinity);
        }

        /// <summary>Represents positive infinity.</summary>
        public static NFloat PositiveInfinity
        {
            [NonVersionable]
            get => new NFloat(NativeType.PositiveInfinity);
        }

        /// <summary>Gets the size, in bytes, of an NFloat.</summary>
        public static int Size
        {
            [NonVersionable]
            get => sizeof(NativeType);
        }

        /// <summary>The underlying floating-point value of this instance.</summary>
        public double Value
        {
            [NonVersionable]
            get => _value;
        }

        //
        // Unary Arithmetic
        //

        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute its unary plus.</param>
        /// <returns>The unary plus of <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator +(NFloat value) => value;

        /// <summary>Computes the unary negation of a value.</summary>
        /// <param name="value">The value for which to compute its unary negation.</param>
        /// <returns>The unary negation of <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator -(NFloat value) => new NFloat(-value._value);

        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator ++(NFloat value) => new NFloat(value._value + 1);

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator --(NFloat value) => new NFloat(value._value - 1);

        //
        // Binary Arithmetic
        //

        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator +(NFloat left, NFloat right) => new NFloat(left._value + right._value);

        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        [NonVersionable]
        public static NFloat operator -(NFloat left, NFloat right) => new NFloat(left._value - right._value);

        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator *(NFloat left, NFloat right) => new NFloat(left._value * right._value);

        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator /(NFloat left, NFloat right) => new NFloat(left._value / right._value);

        /// <summary>Divides two values together to compute their remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator %(NFloat left, NFloat right) => new NFloat(left._value % right._value);

        //
        // Comparisons
        //

        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator ==(NFloat left, NFloat right) => left._value == right._value;

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator !=(NFloat left, NFloat right) => left._value != right._value;

        /// <summary>Compares two values to determine which is less.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator <(NFloat left, NFloat right) => left._value < right._value;

        /// <summary>Compares two values to determine which is less or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator <=(NFloat left, NFloat right) => left._value <= right._value;

        /// <summary>Compares two values to determine which is greater.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator >(NFloat left, NFloat right) => left._value > right._value;

        /// <summary>Compares two values to determine which is greater or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator >=(NFloat left, NFloat right) => left._value >= right._value;

        //
        // Explicit Convert To NFloat
        //

        /// <summary>Explicitly converts a <see cref="System.Decimal" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(decimal value) => new NFloat((NativeType)value);

        /// <summary>Explicitly converts a <see cref="System.Double" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(double value) => new NFloat((NativeType)value);

        //
        // Explicit Convert From NFloat
        //

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Byte" /> value.</returns>
        [NonVersionable]
        public static explicit operator byte(NFloat value) => (byte)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Char" /> value.</returns>
        [NonVersionable]
        public static explicit operator char(NFloat value) => (char)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Decimal" /> value.</returns>
        [NonVersionable]
        public static explicit operator decimal(NFloat value) => (decimal)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Int16" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int16" /> value.</returns>
        [NonVersionable]
        public static explicit operator short(NFloat value) => (short)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Int32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int32" /> value.</returns>
        [NonVersionable]
        public static explicit operator int(NFloat value) => (int)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Int64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Int64" /> value.</returns>
        [NonVersionable]
        public static explicit operator long(NFloat value) => (long)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        [NonVersionable]
        public static explicit operator nint(NFloat value) => (nint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.SByte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.SByte" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator sbyte(NFloat value) => (sbyte)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Single" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Single" /> value.</returns>
        [NonVersionable]
        public static explicit operator float(NFloat value) => (float)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.UInt16" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt16" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ushort(NFloat value) => (ushort)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.UInt32" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt32" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator uint(NFloat value) => (uint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.UInt64" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UInt64" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ulong(NFloat value) => (ulong)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.UIntPtr" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator nuint(NFloat value) => (nuint)(value._value);

        //
        // Implicit Convert To NFloat
        //

        /// <summary>Implicitly converts a <see cref="System.Byte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(byte value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.Char" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(char value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.Int16" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(short value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.Int32" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(int value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.Int64" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(long value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.IntPtr" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(nint value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.SByte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(sbyte value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.Single" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(float value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.UInt16" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(ushort value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.UInt32" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(uint value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.UInt64" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(ulong value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(nuint value) => new NFloat((NativeType)value);

        //
        // Implicit Convert From NFloat
        //

        /// <summary>Implicitly converts a native-sized floating-point value to its nearest representable <see cref="System.Double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.Double" /> value.</returns>
        public static implicit operator double(NFloat value) => (double)(value._value);

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is finite (zero, subnormal or normal); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsFinite(NFloat value) => NativeType.IsFinite(value._value);

        /// <summary>Determines whether the specified value is infinite (positive or negative infinity).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is infinite (positive or negative infinity); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsInfinity(NFloat value) => NativeType.IsInfinity(value._value);

        /// <summary>Determines whether the specified value is NaN (not a number).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is NaN (not a number); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNaN(NFloat value) => NativeType.IsNaN(value._value);

        /// <summary>Determines whether the specified value is negative.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is negative; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNegative(NFloat value) => NativeType.IsNegative(value._value);

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is negative infinity; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNegativeInfinity(NFloat value) => NativeType.IsNegativeInfinity(value._value);

        /// <summary>Determines whether the specified value is normal.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is normal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNormal(NFloat value) => NativeType.IsNormal(value._value);

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is positive infinity; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsPositiveInfinity(NFloat value) => NativeType.IsPositiveInfinity(value._value);

        /// <summary>Determines whether the specified value is subnormal.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is subnormal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsSubnormal(NFloat value) => NativeType.IsSubnormal(value._value);

        /// <summary>Converts the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s)
        {
            var result = NativeType.Parse(s);
            return new NFloat(result);
        }

        /// <summary>Converts the string representation of a number in a specified style to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, NumberStyles style)
        {
            var result = NativeType.Parse(s, style);
            return new NFloat(result);
        }

        /// <summary>Converts the string representation of a number in a specified culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, IFormatProvider? provider)
        {
            var result = NativeType.Parse(s, provider);
            return new NFloat(result);
        }

        /// <summary>Converts the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            var result = NativeType.Parse(s, style, provider);
            return new NFloat(result);
        }

        /// <summary>Converts a character span that contains the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultNumberStyles, IFormatProvider? provider = null)
        {
            var result = NativeType.Parse(s, style, provider);
            return new NFloat(result);
        }

        /// <summary>Tries to convert the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <c>null</c>, <see cref="string.Empty" />, or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, out Unsafe.As<NFloat, NativeType>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="string.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, out Unsafe.As<NFloat, NativeType>(ref result));
        }

        /// <summary>Tries to convert the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <c>null</c>, <see cref="string.Empty" />, or is not in a format compliant with <paramref name="style" />, or if <paramref name="style" /> is not a valid combination of <see cref="NumberStyles" /> enumeration constants. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> value.</para>
        /// </exception>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, style, provider, out Unsafe.As<NFloat, NativeType>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="string.Empty" /> or is not in a format compliant with <paramref name="style" />, or if <paramref name="style" /> is not a valid combination of <see cref="NumberStyles" /> enumeration constants. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> value.</para>
        /// </exception>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeType.TryParse(s, style, provider, out Unsafe.As<NFloat, NativeType>(ref result));
        }

        /// <summary>Compares this instance to a specified object and returns an integer that indicates whether the value of this instance is less than, equal to, or greater than the value of the specified object.</summary>
        /// <param name="obj">An object to compare, or <c>null</c>.</param>
        /// <returns>
        ///     <para>A signed number indicating the relative values of this instance and <paramref name="obj" />.</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <paramref name="obj" />, or this instance is not a number and <paramref name="obj" /> is a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <paramref name="obj" />, or both this instance and <paramref name="obj" /> are not a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance is greater than <paramref name="obj" />, or this instance is a number and <paramref name="obj" /> is not a number or <paramref name="obj" /> is <c>null</c>.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <exception cref="System.ArgumentException"><paramref name="obj" /> is not a <see cref="NFloat" />.</exception>
        public int CompareTo(object? obj)
        {
            if (obj is NFloat other)
            {
                if (_value < other._value) return -1;
                if (_value > other._value) return 1;
                if (_value == other._value) return 0;

                // At least one of the values is NaN.
                if (NativeType.IsNaN(_value))
                {
                    return NativeType.IsNaN(other._value) ? 0 : -1;
                }
                else
                {
                    return 1;
                }
            }
            else if (obj is null)
            {
                return 1;
            }

            throw new ArgumentException(SR.Arg_MustBeNFloat);
        }

        /// <summary>Compares this instance to a specified floating-point number and returns an integer that indicates whether the value of this instance is less than, equal to, or greater than the value of the specified floating-point number.</summary>
        /// <param name="other">A floating-point number to compare.</param>
        /// <returns>
        ///     <para>A signed number indicating the relative values of this instance and <paramref name="other" />.</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <paramref name="other" />, or this instance is not a number and <paramref name="other" /> is a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <paramref name="other" />, or both this instance and <paramref name="other" /> are not a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance is greater than <paramref name="other" />, or this instance is a number and <paramref name="other" /> is not a number.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int CompareTo(NFloat other) => _value.CompareTo(other._value);

        /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an instance of <see cref="NFloat"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is NFloat other) && Equals(other);

        /// <summary>Returns a value indicating whether this instance is equal to a specified <see cref="NFloat" /> value.</summary>
        /// <param name="other">An <see cref="NFloat"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(NFloat other) => _value.Equals(other._value);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => _value.GetHashCode();

        /// <summary>Converts the numeric value of this instance to its equivalent string representation.</summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString() => _value.ToString();

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified format.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format" />.</returns>
        /// <exception cref="FormatException"><paramref name="format" /> is invalid.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => _value.ToString(format);

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.</summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="provider" />.</returns>
        public string ToString(IFormatProvider? provider)=> _value.ToString(provider);

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified format and culture-specific format information.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format" /> and <paramref name="provider" />.</returns>
        /// <exception cref="FormatException"><paramref name="format" /> is invalid.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => _value.ToString(format, provider);

        /// <summary>Tries to format the value of the current instance into the provided span of characters.</summary>
        /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination" />.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination" />.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the formatting was successful; otherwise, <c>false</c>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) => _value.TryFormat(destination, out charsWritten, format, provider);
    }
}

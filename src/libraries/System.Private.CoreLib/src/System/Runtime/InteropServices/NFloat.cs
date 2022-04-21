// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
#if TARGET_32BIT
using NativeExponentType = System.SByte;
using NativeSignificandType = System.UInt32;
using NativeType = System.Single;
#else
using NativeExponentType = System.Int16;
using NativeSignificandType = System.UInt64;
using NativeType = System.Double;
#endif

namespace System.Runtime.InteropServices
{
    /// <summary>Defines an immutable value type that represents a floating type that has the same size as the native integer size.</summary>
    /// <remarks>It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent in managed code unmanaged APIs that use a type alias for C or C++'s <c>float</c> on 32-bit platforms or <c>double</c> on 64-bit platforms, such as the CGFloat type in libraries provided by Apple.</remarks>
    [Intrinsic]
    public readonly struct NFloat
        : IBinaryFloatingPointIeee754<NFloat>,
          IMinMaxValue<NFloat>
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
        public static NFloat operator ++(NFloat value)
        {
            NativeType tmp = value._value;
            ++tmp;
            return new NFloat(tmp);
        }

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator --(NFloat value)
        {
            NativeType tmp = value._value;
            --tmp;
            return new NFloat(tmp);
        }

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

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(decimal value) => new NFloat((NativeType)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(double value) => new NFloat((NativeType)value);

        //
        // Explicit Convert From NFloat
        //

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        [NonVersionable]
        public static explicit operator byte(NFloat value) => (byte)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        [NonVersionable]
        public static explicit operator char(NFloat value) => (char)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        [NonVersionable]
        public static explicit operator decimal(NFloat value) => (decimal)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        [NonVersionable]
        public static explicit operator short(NFloat value) => (short)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        [NonVersionable]
        public static explicit operator int(NFloat value) => (int)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        [NonVersionable]
        public static explicit operator long(NFloat value) => (long)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="System.IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="System.IntPtr" /> value.</returns>
        [NonVersionable]
        public static explicit operator nint(NFloat value) => (nint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator sbyte(NFloat value) => (sbyte)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        [NonVersionable]
        public static explicit operator float(NFloat value) => (float)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ushort(NFloat value) => (ushort)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator uint(NFloat value) => (uint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
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

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(byte value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="char" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(char value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="short" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(short value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="int" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(int value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="long" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(long value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="System.IntPtr" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(nint value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(sbyte value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="float" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(float value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="ushort" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(ushort value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="uint" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(uint value) => new NFloat((NativeType)value);

        /// <summary>Implicitly converts a <see cref="ulong" /> value to its nearest representable native-sized floating-point value.</summary>
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

        /// <summary>Implicitly converts a native-sized floating-point value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
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

        //
        // IAdditionOperators
        //

        /// <inheritdoc cref="IAdditionOperators{TSelf, TOther, TResult}.op_Addition(TSelf, TOther)" />
        static NFloat IAdditionOperators<NFloat, NFloat, NFloat>.operator checked +(NFloat left, NFloat right) => left + right;

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static NFloat IAdditiveIdentity<NFloat, NFloat>.AdditiveIdentity => new NFloat(NativeType.AdditiveIdentity);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(NFloat value) => NativeType.IsPow2(value._value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static NFloat Log2(NFloat value) => new NFloat(NativeType.Log2(value._value));

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator &(NFloat left, NFloat right)
        {
#if TARGET_32BIT
            uint bits = BitConverter.SingleToUInt32Bits(left._value) & BitConverter.SingleToUInt32Bits(right._value);
            NativeType result = BitConverter.UInt32BitsToSingle(bits);
            return new NFloat(result);
#else
            ulong bits = BitConverter.DoubleToUInt64Bits(left._value) & BitConverter.DoubleToUInt64Bits(right._value);
            NativeType result = BitConverter.UInt64BitsToDouble(bits);
            return new NFloat(result);
#endif
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator |(NFloat left, NFloat right)
        {
#if TARGET_32BIT
            uint bits = BitConverter.SingleToUInt32Bits(left._value) | BitConverter.SingleToUInt32Bits(right._value);
            NativeType result = BitConverter.UInt32BitsToSingle(bits);
            return new NFloat(result);
#else
            ulong bits = BitConverter.DoubleToUInt64Bits(left._value) | BitConverter.DoubleToUInt64Bits(right._value);
            NativeType result = BitConverter.UInt64BitsToDouble(bits);
            return new NFloat(result);
#endif
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ^(NFloat left, NFloat right)
        {
#if TARGET_32BIT
            uint bits = BitConverter.SingleToUInt32Bits(left._value) ^ BitConverter.SingleToUInt32Bits(right._value);
            NativeType result = BitConverter.UInt32BitsToSingle(bits);
            return new NFloat(result);
#else
            ulong bits = BitConverter.DoubleToUInt64Bits(left._value) ^ BitConverter.DoubleToUInt64Bits(right._value);
            NativeType result = BitConverter.UInt64BitsToDouble(bits);
            return new NFloat(result);
#endif
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ~(NFloat value)
        {
#if TARGET_32BIT
            uint bits = ~BitConverter.SingleToUInt32Bits(value._value);
            NativeType result = BitConverter.UInt32BitsToSingle(bits);
            return new NFloat(result);
#else
            ulong bits = ~BitConverter.DoubleToUInt64Bits(value._value);
            NativeType result = BitConverter.UInt64BitsToDouble(bits);
            return new NFloat(result);
#endif
        }

        //
        // IDecrementOperators
        //

        /// <inheritdoc cref="IDecrementOperators{TSelf}.op_Decrement(TSelf)" />
        static NFloat IDecrementOperators<NFloat>.operator checked --(NFloat value) => --value;

        //
        // IDivisionOperators
        //

        /// <inheritdoc cref="IDivisionOperators{TSelf, TOther, TResult}.op_CheckedDivision(TSelf, TOther)" />
        static NFloat IDivisionOperators<NFloat, NFloat, NFloat>.operator checked /(NFloat left, NFloat right) => left / right;

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static NFloat Exp(NFloat x) => new NFloat(NativeType.Exp(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static NFloat ExpM1(NFloat x) => new NFloat(NativeType.ExpM1(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static NFloat Exp2(NFloat x) => new NFloat(NativeType.Exp2(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static NFloat Exp2M1(NFloat x) => new NFloat(NativeType.Exp2M1(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static NFloat Exp10(NFloat x) => new NFloat(NativeType.Exp10(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static NFloat Exp10M1(NFloat x) => new NFloat(NativeType.Exp10M1(x._value));

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static NFloat Ceiling(NFloat x) => new NFloat(NativeType.Ceiling(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static NFloat Floor(NFloat x) => new NFloat(NativeType.Floor(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static NFloat Round(NFloat x) => new NFloat(NativeType.Round(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static NFloat Round(NFloat x, int digits) => new NFloat(NativeType.Round(x._value, digits));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static NFloat Round(NFloat x, MidpointRounding mode) => new NFloat(NativeType.Round(x._value, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static NFloat Round(NFloat x, int digits, MidpointRounding mode) => new NFloat(NativeType.Round(x._value, digits, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static NFloat Truncate(NFloat x) => new NFloat(NativeType.Truncate(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        long IFloatingPoint<NFloat>.GetExponentShortestBitLength()
        {
            NativeExponentType exponent = _value.Exponent;

            if (exponent >= 0)
            {
                return (sizeof(NativeExponentType) * 8) - NativeExponentType.LeadingZeroCount(exponent);
            }
            else
            {
                return (sizeof(NativeExponentType) * 8) + 1 - NativeExponentType.LeadingZeroCount((NativeExponentType)(~exponent));
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<NFloat>.GetExponentByteCount() => sizeof(NativeExponentType);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeExponentType))
            {
                NativeExponentType exponent = _value.Exponent;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                bytesWritten = sizeof(NativeExponentType);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        long IFloatingPoint<NFloat>.GetSignificandBitLength()
        {
#if TARGET_32BIT
            return 24;
#else
            return 53;
#endif
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<NFloat>.GetSignificandByteCount() => sizeof(NativeSignificandType);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeSignificandType))
            {
                NativeSignificandType significand = _value.Significand;
                Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                bytesWritten = sizeof(NativeSignificandType);
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.E" />
        public static NFloat E => new NFloat(NativeType.E);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static NFloat NegativeZero => new NFloat(NativeType.NegativeZero);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Pi" />
        public static NFloat Pi => new NFloat(NativeType.Pi);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Tau" />
        public static NFloat Tau => new NFloat(NativeType.Tau);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static NFloat BitDecrement(NFloat x) => new NFloat(NativeType.BitDecrement(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static NFloat BitIncrement(NFloat x) => new NFloat(NativeType.BitIncrement(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static NFloat FusedMultiplyAdd(NFloat left, NFloat right, NFloat addend) => new NFloat(NativeType.FusedMultiplyAdd(left._value, right._value, addend._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static NFloat Ieee754Remainder(NFloat left, NFloat right) => new NFloat(NativeType.Ieee754Remainder(left._value, right._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(NFloat x) => NativeType.ILogB(x._value);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MaxMagnitudeNumber(NFloat x, NFloat y) => new NFloat(NativeType.MaxMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static NFloat MaxNumber(NFloat x, NFloat y) => new NFloat(NativeType.MaxNumber(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MinMagnitudeNumber(NFloat x, NFloat y) => new NFloat(NativeType.MinMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.MinNumber(TSelf, TSelf)" />
        public static NFloat MinNumber(NFloat x, NFloat y) => new NFloat(NativeType.MinNumber(x._value, y._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static NFloat ReciprocalEstimate(NFloat x) => new NFloat(NativeType.ReciprocalEstimate(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static NFloat ReciprocalSqrtEstimate(NFloat x) => new NFloat(NativeType.ReciprocalSqrtEstimate(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static NFloat ScaleB(NFloat x, int n) => new NFloat(NativeType.ScaleB(x._value, n));

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static NFloat Compound(NFloat x, NFloat n) => new NFloat(NativeType.Compound(x._value, n._value));

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static NFloat Acosh(NFloat x) => new NFloat(NativeType.Acosh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static NFloat Asinh(NFloat x) => new NFloat(NativeType.Asinh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static NFloat Atanh(NFloat x) => new NFloat(NativeType.Atanh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static NFloat Cosh(NFloat x) => new NFloat(NativeType.Cosh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static NFloat Sinh(NFloat x) => new NFloat(NativeType.Sinh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static NFloat Tanh(NFloat x) => new NFloat(NativeType.Tanh(x._value));

        //
        // IIncrementOperators
        //

        /// <inheritdoc cref="IIncrementOperators{TSelf}.op_CheckedIncrement(TSelf)" />
        static NFloat IIncrementOperators<NFloat>.operator checked ++(NFloat value) => ++value;

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static NFloat Log(NFloat x) => new NFloat(NativeType.Log(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static NFloat Log(NFloat x, NFloat newBase) => new NFloat(NativeType.Log(x._value, newBase._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static NFloat LogP1(NFloat x) => new NFloat(NativeType.LogP1(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static NFloat Log2P1(NFloat x) => new NFloat(NativeType.Log2P1(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static NFloat Log10(NFloat x) => new NFloat(NativeType.Log10(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static NFloat Log10P1(NFloat x) => new NFloat(NativeType.Log10P1(x._value));

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static NFloat IMultiplicativeIdentity<NFloat, NFloat>.MultiplicativeIdentity => new NFloat(NativeType.MultiplicativeIdentity);

        //
        // IMultiplyOperators
        //

        /// <inheritdoc cref="IMultiplyOperators{TSelf, TOther, TResult}.op_CheckedMultiply(TSelf, TOther)" />
        static NFloat IMultiplyOperators<NFloat, NFloat, NFloat>.operator checked *(NFloat left, NFloat right) => left * right;

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Abs(TSelf)" />
        public static NFloat Abs(NFloat value) => new NFloat(NativeType.Abs(value._value));

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static NFloat Clamp(NFloat value, NFloat min, NFloat max) => new NFloat(NativeType.Clamp(value._value, min._value, max._value));

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static NFloat CopySign(NFloat x, NFloat y) => new NFloat(NativeType.CopySign(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateChecked<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                return (byte)(object)value;
            }
            else if (typeof(TOther) == typeof(char))
            {
                return (char)(object)value;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                return (NFloat)(decimal)(object)value;
            }
            else if (typeof(TOther) == typeof(double))
            {
                return (NFloat)(double)(object)value;
            }
            else if (typeof(TOther) == typeof(short))
            {
                return (short)(object)value;
            }
            else if (typeof(TOther) == typeof(int))
            {
                return (int)(object)value;
            }
            else if (typeof(TOther) == typeof(long))
            {
                return (long)(object)value;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                return (nint)(object)value;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                return (sbyte)(object)value;
            }
            else if (typeof(TOther) == typeof(float))
            {
                return (NFloat)(float)(object)value;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                return (ushort)(object)value;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                return (uint)(object)value;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                return (ulong)(object)value;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                return (nuint)(object)value;
            }
            else if (typeof(TOther) == typeof(NFloat))
            {
                return (NFloat)(object)value;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                return default;
            }
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateSaturating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            return CreateChecked(value);
        }

        /// <inheritdoc cref="INumber{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateTruncating<TOther>(TOther value)
            where TOther : INumber<TOther>
        {
            return CreateChecked(value);
        }

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static NFloat Max(NFloat x, NFloat y) => new NFloat(NativeType.Max(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static NFloat MaxMagnitude(NFloat x, NFloat y) => new NFloat(NativeType.MaxMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static NFloat Min(NFloat x, NFloat y) => new NFloat(NativeType.Min(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static NFloat MinMagnitude(NFloat x, NFloat y) => new NFloat(NativeType.MinMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(NFloat value) => NativeType.Sign(value._value);

        /// <inheritdoc cref="INumber{TSelf}.TryCreate{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryCreate<TOther>(TOther value, out NFloat result)
            where TOther : INumber<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                result = (byte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                result = (char)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                result = (NFloat)(decimal)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                result = (NFloat)(double)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                result = (short)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                result = (int)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                result = (long)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                result = (nint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                result = (sbyte)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                result = (NFloat)(float)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                result = (ushort)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                result = (uint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                result = (ulong)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                result = (nuint)(object)value;
                return true;
            }
            else if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
                return true;
            }
            else
            {
                ThrowHelper.ThrowNotSupportedException();
                result = default;
                return false;
            }
        }

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static NFloat INumberBase<NFloat>.One => new NFloat(NativeType.One);

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static NFloat INumberBase<NFloat>.Zero => new NFloat(NativeType.Zero);

        //
        // IParsable
        //

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NFloat result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static NFloat Pow(NFloat x, NFloat y) => new NFloat(NativeType.Pow(x._value, y._value));

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static NFloat Cbrt(NFloat x) => new NFloat(NativeType.Cbrt(x._value));

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        // public static NFloat Hypot(NFloat x, NFloat y) => new NFloat(NativeType.Hypot(x._value, y._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static NFloat Sqrt(NFloat x) => new NFloat(NativeType.Sqrt(x._value));

        // /// <inheritdoc cref="IRootFunctions{TSelf}.Root(TSelf, TSelf)" />
        // public static NFloat Root(NFloat x, NFloat n) => new NFloat(NativeType.Root(x._value, n._value));

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static NFloat ISignedNumber<NFloat>.NegativeOne => new NFloat(NativeType.NegativeOne);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static NFloat Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out NFloat result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ISubtractionOperators
        //

        /// <inheritdoc cref="ISubtractionOperators{TSelf, TOther, TResult}.op_CheckedSubtraction(TSelf, TOther)" />
        static NFloat ISubtractionOperators<NFloat, NFloat, NFloat>.operator checked -(NFloat left, NFloat right) => left - right;

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static NFloat Acos(NFloat x) => new NFloat(NativeType.Acos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static NFloat Asin(NFloat x) => new NFloat(NativeType.Asin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static NFloat Atan(NFloat x) => new NFloat(NativeType.Atan(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2(TSelf, TSelf)" />
        public static NFloat Atan2(NFloat y, NFloat x) => new NFloat(NativeType.Atan2(y._value, x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static NFloat Cos(NFloat x) => new NFloat(NativeType.Cos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static NFloat Sin(NFloat x) => new NFloat(NativeType.Sin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (NFloat Sin, NFloat Cos) SinCos(NFloat x)
        {
            var (sin, cos) = MathF.SinCos((float)x);
            return (new NFloat(sin), new NFloat(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static NFloat Tan(NFloat x) => new NFloat(NativeType.Tan(x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        // public static NFloat AcosPi(NFloat x) => new NFloat(NativeType.AcosPi(x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        // public static NFloat AsinPi(NFloat x) => new NFloat(NativeType.AsinPi(x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        // public static NFloat AtanPi(NFloat x) => new NFloat(NativeType.AtanPi(x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan2Pi(TSelf)" />
        // public static NFloat Atan2Pi(NFloat y, NFloat x) => new NFloat(NativeType.Atan2Pi(y._value, x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        // public static NFloat CosPi(NFloat x) => new NFloat(NativeType.CosPi(x._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        // public static NFloat SinPi(NFloat x) => new NFloat(NativeType.SinPi(x._value, y._value));

        // /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        // public static NFloat TanPi(NFloat x) => new NFloat(NativeType.TanPi(x._value, y._value));

        //
        // IUnaryNegationOperators
        //

        /// <inheritdoc cref="IUnaryNegationOperators{TSelf, TResult}.op_CheckedUnaryNegation(TSelf)" />
        static NFloat IUnaryNegationOperators<NFloat, NFloat>.operator checked -(NFloat value) => -value;
    }
}

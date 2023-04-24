// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
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
    [NonVersionable] // This only applies to field layout
    public readonly struct NFloat
        : IBinaryFloatingPointIeee754<NFloat>,
          IMinMaxValue<NFloat>,
          IUtf8SpanFormattable
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
        /// <returns>The product of <paramref name="left" /> multiplied-by <paramref name="right" />.</returns>
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

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        [NonVersionable]
        public static explicit operator checked byte(NFloat value) => checked((byte)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        [NonVersionable]
        public static explicit operator char(NFloat value) => (char)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        [NonVersionable]
        public static explicit operator checked char(NFloat value) => checked((char)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        [NonVersionable]
        public static explicit operator decimal(NFloat value) => (decimal)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Half" /> value.</returns>
        [NonVersionable]
        public static explicit operator Half(NFloat value) => (Half)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        [NonVersionable]
        public static explicit operator short(NFloat value) => (short)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        [NonVersionable]
        public static explicit operator checked short(NFloat value) => checked((short)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        [NonVersionable]
        public static explicit operator int(NFloat value) => (int)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        [NonVersionable]
        public static explicit operator checked int(NFloat value) => checked((int)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        [NonVersionable]
        public static explicit operator long(NFloat value) => (long)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        [NonVersionable]
        public static explicit operator checked long(NFloat value) => checked((long)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Int128" /> value.</returns>
        [NonVersionable]
        public static explicit operator Int128(NFloat value) => (Int128)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Int128" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [NonVersionable]
        public static explicit operator checked Int128(NFloat value) => checked((Int128)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        [NonVersionable]
        public static explicit operator nint(NFloat value) => (nint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="IntPtr" />.</exception>
        [NonVersionable]
        public static explicit operator checked nint(NFloat value) => checked((nint)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator sbyte(NFloat value) => (sbyte)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(NFloat value) => checked((sbyte)(value._value));

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

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked ushort(NFloat value) => checked((ushort)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator uint(NFloat value) => (uint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked uint(NFloat value) => checked((uint)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ulong(NFloat value) => (ulong)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked ulong(NFloat value) => checked((ulong)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UInt128" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator UInt128(NFloat value) => (UInt128)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UInt128" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(NFloat value) => checked((UInt128)(value._value));

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator nuint(NFloat value) => (nuint)(value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UIntPtr" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked nuint(NFloat value) => checked((nuint)(value._value));

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

        /// <summary>Implicitly converts a <see cref="Half" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(Half value) => (NFloat)(float)value;

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

        /// <summary>Explicitly converts a <see cref="Int128" /> to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(Int128 value)
        {
            if (Int128.IsNegative(value))
            {
                value = -value;
                return -(NFloat)(UInt128)(value);
            }
            return (NFloat)(UInt128)(value);
        }

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

        /// <summary>Explicitly converts <see cref="UInt128"/> to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator NFloat(UInt128 value) => (NFloat)(double)(value);

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
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
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
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
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
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
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
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
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
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
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

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) => _value.TryFormat(utf8Destination, out bytesWritten, format, provider);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static NFloat IAdditiveIdentity<NFloat, NFloat>.AdditiveIdentity => new NFloat(NativeType.AdditiveIdentity);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static NFloat IBinaryNumber<NFloat>.AllBitsSet
        {
#if TARGET_64BIT
            [NonVersionable]
            get => (NFloat)BitConverter.UInt64BitsToDouble(0xFFFF_FFFF_FFFF_FFFF);
#else
            [NonVersionable]
            get => BitConverter.UInt32BitsToSingle(0xFFFF_FFFF);
#endif
        }

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<NFloat>.GetExponentByteCount() => sizeof(NativeExponentType);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<NFloat>.GetExponentShortestBitLength()
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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<NFloat>.GetSignificandByteCount() => sizeof(NativeSignificandType);

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<NFloat>.GetSignificandBitLength()
        {
#if TARGET_32BIT
            return 24;
#else
            return 53;
#endif
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeExponentType))
            {
                NativeExponentType exponent = _value.Exponent;

                if (BitConverter.IsLittleEndian)
                {
                    exponent = BinaryPrimitives.ReverseEndianness(exponent);
                }

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeExponentType))
            {
                NativeExponentType exponent = _value.Exponent;

                if (!BitConverter.IsLittleEndian)
                {
                    exponent = BinaryPrimitives.ReverseEndianness(exponent);
                }

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeSignificandType))
            {
                NativeSignificandType significand = _value.Significand;

                if (BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

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

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= sizeof(NativeSignificandType))
            {
                NativeSignificandType significand = _value.Significand;

                if (!BitConverter.IsLittleEndian)
                {
                    significand = BinaryPrimitives.ReverseEndianness(significand);
                }

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
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        public static NFloat E => new NFloat(NativeType.E);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        public static NFloat Pi => new NFloat(NativeType.Pi);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        public static NFloat Tau => new NFloat(NativeType.Tau);

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static NFloat NegativeZero => new NFloat(NativeType.NegativeZero);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static NFloat Atan2(NFloat y, NFloat x) => new NFloat(NativeType.Atan2(y._value, x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static NFloat Atan2Pi(NFloat y, NFloat x) => new NFloat(NativeType.Atan2Pi(y._value, x._value));

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

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static NFloat Lerp(NFloat value1, NFloat value2, NFloat amount) => new NFloat(NativeType.Lerp(value1._value, value2._value, amount._value));

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
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static NFloat Clamp(NFloat value, NFloat min, NFloat max) => new NFloat(NativeType.Clamp(value._value, min._value, max._value));

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static NFloat CopySign(NFloat value, NFloat sign) => new NFloat(NativeType.CopySign(value._value, sign._value));

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static NFloat Max(NFloat x, NFloat y) => new NFloat(NativeType.Max(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static NFloat MaxNumber(NFloat x, NFloat y) => new NFloat(NativeType.MaxNumber(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static NFloat Min(NFloat x, NFloat y) => new NFloat(NativeType.Min(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static NFloat MinNumber(NFloat x, NFloat y) => new NFloat(NativeType.MinNumber(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(NFloat value) => NativeType.Sign(value._value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static NFloat INumberBase<NFloat>.One => new NFloat(NativeType.One);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<NFloat>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static NFloat INumberBase<NFloat>.Zero => new NFloat(NativeType.Zero);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static NFloat Abs(NFloat value) => new NFloat(NativeType.Abs(value._value));

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<NFloat>.IsCanonical(NFloat value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<NFloat>.IsComplexNumber(NFloat value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(NFloat value) => NativeType.IsEvenInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<NFloat>.IsImaginaryNumber(NFloat value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(NFloat value) => NativeType.IsInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(NFloat value) => NativeType.IsOddInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(NFloat value) => NativeType.IsPositive(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(NFloat value) => NativeType.IsRealNumber(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<NFloat>.IsZero(NFloat value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static NFloat MaxMagnitude(NFloat x, NFloat y) => new NFloat(NativeType.MaxMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MaxMagnitudeNumber(NFloat x, NFloat y) => new NFloat(NativeType.MaxMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static NFloat MinMagnitude(NFloat x, NFloat y) => new NFloat(NativeType.MinMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MinMagnitudeNumber(NFloat x, NFloat y) => new NFloat(NativeType.MinMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromChecked<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromSaturating<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromTruncating<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom<TOther>(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out NFloat result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
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
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
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
        static bool INumberBase<NFloat>.TryConvertToChecked<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
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
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
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
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertToSaturating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertToTruncating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo<TOther>(value, out result);
        }

        private static bool TryConvertTo<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
                                    (value <= byte.MinValue) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value >= char.MaxValue) ? char.MaxValue :
                                    (value <= char.MinValue) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= +79228162514264337593543950336.0f) ? decimal.MaxValue :
                                       (value <= -79228162514264337593543950336.0f) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (value >= short.MaxValue) ? short.MaxValue :
                                     (value <= short.MinValue) ? short.MinValue : (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = (value >= int.MaxValue) ? int.MaxValue :
                                   (value <= int.MinValue) ? int.MinValue : (int)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value >= long.MaxValue) ? long.MaxValue :
                                    (value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value >= +170141183460469231731687303715884105727.0) ? Int128.MaxValue :
                                      (value <= -170141183460469231731687303715884105728.0) ? Int128.MinValue : (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value >= nint.MaxValue) ? nint.MaxValue :
                                    (value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value <= ushort.MinValue) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    (value <= uint.MinValue) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value <= ulong.MinValue) ? ulong.MinValue :
                                     IsNaN(value) ? 0 : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value >= 340282366920938463463374607431768211455.0) ? UInt128.MaxValue :
                                       (value <= 0.0) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
#if TARGET_32BIT
                nuint actualResult = (value >= uint.MaxValue) ? unchecked((nuint)uint.MaxValue) :
                                     (value <= uint.MinValue) ? unchecked((nuint)uint.MinValue) : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
#else
                nuint actualResult = (value >= ulong.MaxValue) ? unchecked((nuint)ulong.MaxValue) :
                                     (value <= ulong.MinValue) ? unchecked((nuint)ulong.MinValue) : (nuint)value;
                result = (TOther)(object)actualResult;
                return true;
#endif
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
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

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static NFloat Hypot(NFloat x, NFloat y) => new NFloat(NativeType.Hypot(x._value, y._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static NFloat RootN(NFloat x, int n) => new NFloat(NativeType.RootN(x._value, n));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static NFloat Sqrt(NFloat x) => new NFloat(NativeType.Sqrt(x._value));

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
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static NFloat Acos(NFloat x) => new NFloat(NativeType.Acos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static NFloat AcosPi(NFloat x) => new NFloat(NativeType.AcosPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static NFloat Asin(NFloat x) => new NFloat(NativeType.Asin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static NFloat AsinPi(NFloat x) => new NFloat(NativeType.AsinPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static NFloat Atan(NFloat x) => new NFloat(NativeType.Atan(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static NFloat AtanPi(NFloat x) => new NFloat(NativeType.AtanPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static NFloat Cos(NFloat x) => new NFloat(NativeType.Cos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static NFloat CosPi(NFloat x) => new NFloat(NativeType.CosPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static NFloat Sin(NFloat x) => new NFloat(NativeType.Sin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (NFloat Sin, NFloat Cos) SinCos(NFloat x)
        {
            var (sin, cos) = NativeType.SinCos(x._value);
            return (new NFloat(sin), new NFloat(cos));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (NFloat SinPi, NFloat CosPi) SinCosPi(NFloat x)
        {
            var (sinPi, cosPi) = NativeType.SinCosPi(x._value);
            return (new NFloat(sinPi), new NFloat(cosPi));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static NFloat SinPi(NFloat x) => new NFloat(NativeType.SinPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static NFloat Tan(NFloat x) => new NFloat(NativeType.Tan(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static NFloat TanPi(NFloat x) => new NFloat(NativeType.TanPi(x._value));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profitability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    /// <summary>Represents a 512-bit vector of a specified numeric type that is suitable for low-level optimization of parallel algorithms.</summary>
    /// <typeparam name="T">The type of the elements in the vector.</typeparam>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector512DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector512.Size)]
    public readonly unsafe struct Vector512<T> : ISimdVector<Vector512<T>, T>
    {
        internal readonly Vector256<T> _lower;
        internal readonly Vector256<T> _upper;

        /// <summary>Gets a new <see cref="Vector512{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector512<T> AllBitsSet
        {
            [Intrinsic]
            get => Vector512.Create(Scalar<T>.AllBitsSet);
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector512{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
                return Vector512.Size / sizeof(T);
            }
        }

        /// <summary>Gets a new <see cref="Vector512{T}" /> with the elements set to their index.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector512<T> Indices
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
                Unsafe.SkipInit(out Vector512<T> result);

                for (int i = 0; i < Count; i++)
                {
                    result.SetElementUnsafe(i, Scalar<T>.Convert(i));
                }

                return result;
            }
        }

        /// <summary>Gets <c>true</c> if <typeparamref name="T" /> is supported; otherwise, <c>false</c>.</summary>
        /// <returns><c>true</c> if <typeparamref name="T" /> is supported; otherwise, <c>false</c>.</returns>
        public static bool IsSupported
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (typeof(T) == typeof(byte))
                    || (typeof(T) == typeof(double))
                    || (typeof(T) == typeof(short))
                    || (typeof(T) == typeof(int))
                    || (typeof(T) == typeof(long))
                    || (typeof(T) == typeof(nint))
                    || (typeof(T) == typeof(sbyte))
                    || (typeof(T) == typeof(float))
                    || (typeof(T) == typeof(ushort))
                    || (typeof(T) == typeof(uint))
                    || (typeof(T) == typeof(ulong))
                    || (typeof(T) == typeof(nuint));
            }
        }

        /// <summary>Gets a new <see cref="Vector512{T}" /> with all elements initialized to one.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector512<T> One
        {
            [Intrinsic]
            get => Vector512.Create(Scalar<T>.One);
        }

        /// <summary>Gets a new <see cref="Vector512{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector512<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
                return default;
            }
        }

        internal string DisplayString => IsSupported ? ToString() : SR.NotSupported_Type;

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public T this[int index] => this.GetElement(index);

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator +(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower + right._lower,
                left._upper + right._upper
            );
        }

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator &(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower & right._lower,
                left._upper & right._upper
            );
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator |(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower | right._lower,
                left._upper | right._upper
            );
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator /(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower / right._lower,
                left._upper / right._upper
            );
        }

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator /(Vector512<T> left, T right)
        {
            return Vector512.Create(
                left._lower / right,
                left._upper / right
            );
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector512<T> left, Vector512<T> right)
        {
            return (left._lower == right._lower)
                && (left._upper == right._upper);
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator ^(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower ^ right._lower,
                left._upper ^ right._upper
            );
        }

        /// <summary>Compares two vectors to determine if any elements are not equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was not equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static bool operator !=(Vector512<T> left, Vector512<T> right) => !(left == right);

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator <<(Vector512<T> value, int shiftCount)
        {
            return Vector512.Create(
                value._lower << shiftCount,
                value._upper << shiftCount
            );
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator *(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower * right._lower,
                left._upper * right._upper
            );
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator *(Vector512<T> left, T right)
        {
            return Vector512.Create(
                left._lower * right,
                left._upper * right
            );
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector512<T> operator *(T left, Vector512<T> right) => right * left;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator ~(Vector512<T> vector)
        {
            return Vector512.Create(
                ~vector._lower,
                ~vector._upper
            );
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator >>(Vector512<T> value, int shiftCount)
        {
            return Vector512.Create(
                value._lower >> shiftCount,
                value._upper >> shiftCount
            );
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator -(Vector512<T> left, Vector512<T> right)
        {
            return Vector512.Create(
                left._lower - right._lower,
                left._upper - right._upper
            );
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector512<T> operator -(Vector512<T> vector) => Zero - vector;

        /// <summary>Returns a given vector unchanged.</summary>
        /// <param name="value">The vector.</param>
        /// <returns><paramref name="value" /></returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector512<T> operator +(Vector512<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();
            return value;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<T> operator >>>(Vector512<T> value, int shiftCount)
        {
            return Vector512.Create(
                value._lower >>> shiftCount,
                value._upper >>> shiftCount
            );
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector512{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector512<T> other) && Equals(other);

        /// <summary>Determines whether the specified <see cref="Vector512{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector512{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector512<T> other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector512.IsHardwareAccelerated)
            {
                if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                {
                    Vector512<T> result = Vector512.Equals(this, other) | ~(Vector512.Equals(this, this) | Vector512.Equals(other, other));
                    return result.AsInt32() == Vector512<int>.AllBitsSet;
                }
                else
                {
                    return this == other;
                }
            }
            else
            {
                return _lower.Equals(other._lower)
                    && _upper.Equals(other._upper);
            }
        }

        /// <summary>Gets the hash code for the instance.</summary>
        /// <returns>The hash code for the instance.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public override int GetHashCode()
        {
            HashCode hashCode = default;

            for (int i = 0; i < Count; i++)
            {
                T value = this.GetElementUnsafe(i);
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>Converts the current instance to an equivalent string representation.</summary>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public override string ToString() => ToString("G", CultureInfo.InvariantCulture);

        private string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<T>();

            var sb = new ValueStringBuilder(stackalloc char[64]);
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            sb.Append('<');
            sb.Append(((IFormattable)this.GetElementUnsafe(0)).ToString(format, formatProvider));

            for (int i = 1; i < Count; i++)
            {
                sb.Append(separator);
                sb.Append(' ');
                sb.Append(((IFormattable)this.GetElementUnsafe(i)).ToString(format, formatProvider));
            }
            sb.Append('>');

            return sb.ToString();
        }

        //
        // ISimdVector
        //

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Alignment" />
        static int ISimdVector<Vector512<T>, T>.Alignment => Vector512.Alignment;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.IsHardwareAccelerated" />
        static bool ISimdVector<Vector512<T>, T>.IsHardwareAccelerated => Vector512.IsHardwareAccelerated;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Abs(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Abs(Vector512<T> vector) => Vector512.Abs(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Add(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Add(Vector512<T> left, Vector512<T> right) => left + right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.AndNot(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.AndNot(Vector512<T> left, Vector512<T> right) => Vector512.AndNot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseAnd(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.BitwiseAnd(Vector512<T> left, Vector512<T> right) => left & right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseOr(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.BitwiseOr(Vector512<T> left, Vector512<T> right) => left | right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Ceiling(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Ceiling(Vector512<T> vector) => Vector512.Ceiling(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Clamp(Vector512<T> value, Vector512<T> min, Vector512<T> max) => Vector512.Clamp(value, min, max);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.ClampNative(Vector512<T> value, Vector512<T> min, Vector512<T> max) => Vector512.ClampNative(value, min, max);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ConditionalSelect(TSelf, TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.ConditionalSelect(Vector512<T> condition, Vector512<T> left, Vector512<T> right) => Vector512.ConditionalSelect(condition, left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.CopySign(Vector512<T> value, Vector512<T> sign) => Vector512.CopySign(value, sign);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[])" />
        static void ISimdVector<Vector512<T>, T>.CopyTo(Vector512<T> vector, T[] destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[], int)" />
        static void ISimdVector<Vector512<T>, T>.CopyTo(Vector512<T> vector, T[] destination, int startIndex) => vector.CopyTo(destination, startIndex);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, Span{T})" />
        static void ISimdVector<Vector512<T>, T>.CopyTo(Vector512<T> vector, Span<T> destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Create(T value) => Vector512.Create(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[])" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Create(T[] values) => Vector512.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[], int)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Create(T[] values, int index) => Vector512.Create(values, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(ReadOnlySpan{T})" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Create(ReadOnlySpan<T> values) => Vector512.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalar(T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.CreateScalar(T value) => Vector512.CreateScalar(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalarUnsafe(T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.CreateScalarUnsafe(T value) => Vector512.CreateScalarUnsafe(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Divide(Vector512<T> left, Vector512<T> right) => left / right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Divide(Vector512<T> left, T right) => left / right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Dot(TSelf, TSelf)" />
        static T ISimdVector<Vector512<T>, T>.Dot(Vector512<T> left, Vector512<T> right) => Vector512.Dot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Equals(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Equals(Vector512<T> left, Vector512<T> right) => Vector512.Equals(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.EqualsAll(Vector512<T> left, Vector512<T> right) => left == right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.EqualsAny(Vector512<T> left, Vector512<T> right) => Vector512.EqualsAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Floor(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Floor(Vector512<T> vector) => Vector512.Floor(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GetElement(TSelf, int)" />
        static T ISimdVector<Vector512<T>, T>.GetElement(Vector512<T> vector, int index) => vector.GetElement(index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThan(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.GreaterThan(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.GreaterThanAll(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.GreaterThanAny(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqual(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.GreaterThanOrEqual(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.GreaterThanOrEqualAll(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.GreaterThanOrEqualAny(Vector512<T> left, Vector512<T> right) => Vector512.GreaterThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThan(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LessThan(Vector512<T> left, Vector512<T> right) => Vector512.LessThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.LessThanAll(Vector512<T> left, Vector512<T> right) => Vector512.LessThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.LessThanAny(Vector512<T> left, Vector512<T> right) => Vector512.LessThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqual(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LessThanOrEqual(Vector512<T> left, Vector512<T> right) => Vector512.LessThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.LessThanOrEqualAll(Vector512<T> left, Vector512<T> right) => Vector512.LessThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector512<T>, T>.LessThanOrEqualAny(Vector512<T> left, Vector512<T> right) => Vector512.LessThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Load(T*)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Load(T* source) => Vector512.Load(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAligned(T*)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LoadAligned(T* source) => Vector512.LoadAligned(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAlignedNonTemporal(T*)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LoadAlignedNonTemporal(T* source) => Vector512.LoadAlignedNonTemporal(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LoadUnsafe(ref readonly T source) => Vector512.LoadUnsafe(in source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T, nuint)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.LoadUnsafe(ref readonly T source, nuint elementOffset) => Vector512.LoadUnsafe(in source, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Max(Vector512<T> left, Vector512<T> right) => Vector512.Max(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MaxMagnitude(Vector512<T> left, Vector512<T> right) => Vector512.MaxMagnitude(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MaxMagnitudeNumber(Vector512<T> left, Vector512<T> right) => Vector512.MaxMagnitudeNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MaxNative(Vector512<T> left, Vector512<T> right) => Vector512.MaxNative(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MaxNumber(Vector512<T> left, Vector512<T> right) => Vector512.MaxNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Min(Vector512<T> left, Vector512<T> right) => Vector512.Min(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MinMagnitude(Vector512<T> left, Vector512<T> right) => Vector512.MinMagnitude(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MinMagnitudeNumber(Vector512<T> left, Vector512<T> right) => Vector512.MinMagnitudeNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MinNative(Vector512<T> left, Vector512<T> right) => Vector512.MinNative(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MinNumber(Vector512<T> left, Vector512<T> right) => Vector512.MinNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Multiply(Vector512<T> left, Vector512<T> right) => left * right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Multiply(Vector512<T> left, T right) => left * right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MultiplyAddEstimate(TSelf, TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.MultiplyAddEstimate(Vector512<T> left, Vector512<T> right, Vector512<T> addend) => Vector512.MultiplyAddEstimate(left, right, addend);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Negate(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Negate(Vector512<T> vector) => -vector;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.OnesComplement(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.OnesComplement(Vector512<T> vector) => ~vector;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Round(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Round(Vector512<T> vector) => Vector512.Round(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftLeft(TSelf, int)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.ShiftLeft(Vector512<T> vector, int shiftCount) => vector << shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightArithmetic(TSelf, int)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.ShiftRightArithmetic(Vector512<T> vector, int shiftCount) => vector >> shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightLogical(TSelf, int)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.ShiftRightLogical(Vector512<T> vector, int shiftCount) => vector >>> shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sqrt(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Sqrt(Vector512<T> vector) => Vector512.Sqrt(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Store(TSelf, T*)" />
        static void ISimdVector<Vector512<T>, T>.Store(Vector512<T> source, T* destination) => source.Store(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAligned(TSelf, T*)" />
        static void ISimdVector<Vector512<T>, T>.StoreAligned(Vector512<T> source, T* destination) => source.StoreAligned(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAlignedNonTemporal(TSelf, T*)" />
        static void ISimdVector<Vector512<T>, T>.StoreAlignedNonTemporal(Vector512<T> source, T* destination) => source.StoreAlignedNonTemporal(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T)" />
        static void ISimdVector<Vector512<T>, T>.StoreUnsafe(Vector512<T> vector, ref T destination) => vector.StoreUnsafe(ref destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T, nuint)" />
        static void ISimdVector<Vector512<T>, T>.StoreUnsafe(Vector512<T> vector, ref T destination, nuint elementOffset) => vector.StoreUnsafe(ref destination, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Subtract(TSelf, TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Subtract(Vector512<T> left, Vector512<T> right) => left - right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sum(TSelf)" />
        static T ISimdVector<Vector512<T>, T>.Sum(Vector512<T> vector) => Vector512.Sum(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ToScalar(TSelf)" />
        static T ISimdVector<Vector512<T>, T>.ToScalar(Vector512<T> vector) => vector.ToScalar();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Truncate(TSelf)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Truncate(Vector512<T> vector) => Vector512.Truncate(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.TryCopyTo(TSelf, Span{T})" />
        static bool ISimdVector<Vector512<T>, T>.TryCopyTo(Vector512<T> vector, Span<T> destination) => vector.TryCopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.WithElement(TSelf, int, T)" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.WithElement(Vector512<T> vector, int index, T value) => vector.WithElement(index, value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Xor" />
        static Vector512<T> ISimdVector<Vector512<T>, T>.Xor(Vector512<T> left, Vector512<T> right) => left ^ right;

        //
        // New Surface Area
        //

        static bool ISimdVector<Vector512<T>, T>.AnyWhereAllBitsSet(Vector512<T> vector) => Vector512.EqualsAny(vector, AllBitsSet);

        static bool ISimdVector<Vector512<T>, T>.Any(Vector512<T> vector, T value) => Vector512.EqualsAny(vector, Vector512.Create(value));

        static int ISimdVector<Vector512<T>, T>.IndexOfLastMatch(Vector512<T> vector)
        {
            ulong mask = vector.ExtractMostSignificantBits();
            return 63 - BitOperations.LeadingZeroCount(mask); // 63 = 64 (bits in Int64) - 1 (indexing from zero)
        }

        static Vector512<T> ISimdVector<Vector512<T>, T>.IsNaN(Vector512<T> vector) => Vector512.IsNaN(vector);

        static Vector512<T> ISimdVector<Vector512<T>, T>.IsNegative(Vector512<T> vector) => Vector512.IsNegative(vector);

        static Vector512<T> ISimdVector<Vector512<T>, T>.IsPositive(Vector512<T> vector) => Vector512.IsPositive(vector);

        static Vector512<T> ISimdVector<Vector512<T>, T>.IsPositiveInfinity(Vector512<T> vector) => Vector512.IsPositiveInfinity(vector);

        static Vector512<T> ISimdVector<Vector512<T>, T>.IsZero(Vector512<T> vector) => Vector512.IsZero(vector);
    }
}

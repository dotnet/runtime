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
    // will generate code small enough to make the AggressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    /// <summary>Represents a 256-bit vector of a specified numeric type that is suitable for low-level optimization of parallel algorithms.</summary>
    /// <typeparam name="T">The type of the elements in the vector.</typeparam>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector256DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector256.Size)]
    public readonly unsafe struct Vector256<T> : ISimdVector<Vector256<T>, T>
    {
        internal readonly Vector128<T> _lower;
        internal readonly Vector128<T> _upper;

        /// <summary>Gets a new <see cref="Vector256{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> AllBitsSet
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector128<T> vector = Vector128<T>.AllBitsSet;
                return Vector256.Create(vector, vector);
            }
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector256{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Vector128<T>.Count * 2;
            }
        }

        /// <summary>Gets a new <see cref="Vector256{T}" /> with the elements set to their index.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> Indices
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
                Unsafe.SkipInit(out Vector256<T> result);

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

        /// <summary>Gets a new <see cref="Vector256{T}" /> with all elements initialized to one.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> One
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector128<T> vector = Vector128<T>.One;
                return Vector256.Create(vector, vector);
            }
        }

        /// <summary>Gets a new <see cref="Vector256{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> Zero
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
                return default;
            }
        }

        internal string DisplayString
        {
            get
            {
                return IsSupported ? ToString() : SR.NotSupported_Type;
            }
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return this.GetElement(index);
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator +(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator &(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator |(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator /(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator /(Vector256<T> left, T right)
        {
            return Vector256.Create(
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
        public static bool operator ==(Vector256<T> left, Vector256<T> right)
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
        public static Vector256<T> operator ^(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector256<T> left, Vector256<T> right)
        {
            return (left._lower != right._lower)
                || (left._upper != right._upper);
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator <<(Vector256<T> value, int shiftCount)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator *(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator *(Vector256<T> left, T right)
        {
            return Vector256.Create(
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator *(T left, Vector256<T> right) => right * left;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator ~(Vector256<T> vector)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator >>(Vector256<T> value, int shiftCount)
        {
            return Vector256.Create(
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
        public static Vector256<T> operator -(Vector256<T> left, Vector256<T> right)
        {
            return Vector256.Create(
                left._lower - right._lower,
                left._upper - right._upper
            );
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator -(Vector256<T> vector)
        {
            return Vector256.Create(
                -vector._lower,
                -vector._upper
            );
        }

        /// <summary>Returns a given vector unchanged.</summary>
        /// <param name="value">The vector.</param>
        /// <returns><paramref name="value" /></returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator +(Vector256<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();
            return value;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> operator >>>(Vector256<T> value, int shiftCount)
        {
            return Vector256.Create(
                value._lower >>> shiftCount,
                value._upper >>> shiftCount
            );
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector256{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector256<T> other) && Equals(other);

        /// <summary>Determines whether the specified <see cref="Vector256{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector256{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector256<T> other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector256.IsHardwareAccelerated)
            {
                if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                {
                    Vector256<T> result = Vector256.Equals(this, other) | ~(Vector256.Equals(this, this) | Vector256.Equals(other, other));
                    return result.AsInt32() == Vector256<int>.AllBitsSet;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => ToString("G", CultureInfo.InvariantCulture);

        private string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector256BaseType<T>();

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
        static int ISimdVector<Vector256<T>, T>.Alignment => Vector256.Alignment;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.IsHardwareAccelerated" />
        static bool ISimdVector<Vector256<T>, T>.IsHardwareAccelerated => Vector256.IsHardwareAccelerated;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Abs(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Abs(Vector256<T> vector) => Vector256.Abs(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Add(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Add(Vector256<T> left, Vector256<T> right) => Vector256.Add(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.AndNot(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.AndNot(Vector256<T> left, Vector256<T> right) => Vector256.AndNot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseAnd(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.BitwiseAnd(Vector256<T> left, Vector256<T> right) => Vector256.BitwiseAnd(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseOr(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.BitwiseOr(Vector256<T> left, Vector256<T> right) => Vector256.BitwiseOr(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Ceiling(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Ceiling(Vector256<T> vector) => Vector256.Ceiling(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ConditionalSelect(TSelf, TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.ConditionalSelect(Vector256<T> condition, Vector256<T> left, Vector256<T> right) => Vector256.ConditionalSelect(condition, left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[])" />
        static void ISimdVector<Vector256<T>, T>.CopyTo(Vector256<T> vector, T[] destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[], int)" />
        static void ISimdVector<Vector256<T>, T>.CopyTo(Vector256<T> vector, T[] destination, int startIndex) => vector.CopyTo(destination, startIndex);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, Span{T})" />
        static void ISimdVector<Vector256<T>, T>.CopyTo(Vector256<T> vector, Span<T> destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Create(T value) => Vector256.Create(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[])" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Create(T[] values) => Vector256.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[], int)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Create(T[] values, int index) => Vector256.Create(values, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(ReadOnlySpan{T})" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Create(ReadOnlySpan<T> values) => Vector256.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalar(T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.CreateScalar(T value) => Vector256.CreateScalar(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalarUnsafe(T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.CreateScalarUnsafe(T value) => Vector256.CreateScalarUnsafe(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Divide(Vector256<T> left, Vector256<T> right) => Vector256.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Divide(Vector256<T> left, T right) => Vector256.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Dot(TSelf, TSelf)" />
        static T ISimdVector<Vector256<T>, T>.Dot(Vector256<T> left, Vector256<T> right) => Vector256.Dot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Equals(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Equals(Vector256<T> left, Vector256<T> right) => Vector256.Equals(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.EqualsAll(Vector256<T> left, Vector256<T> right) => Vector256.EqualsAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.EqualsAny(Vector256<T> left, Vector256<T> right) => Vector256.EqualsAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Floor(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Floor(Vector256<T> vector) => Vector256.Floor(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GetElement(TSelf, int)" />
        static T ISimdVector<Vector256<T>, T>.GetElement(Vector256<T> vector, int index) => Vector256.GetElement(vector, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThan(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.GreaterThan(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.GreaterThanAll(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.GreaterThanAny(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqual(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.GreaterThanOrEqual(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.GreaterThanOrEqualAll(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.GreaterThanOrEqualAny(Vector256<T> left, Vector256<T> right) => Vector256.GreaterThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThan(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LessThan(Vector256<T> left, Vector256<T> right) => Vector256.LessThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.LessThanAll(Vector256<T> left, Vector256<T> right) => Vector256.LessThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.LessThanAny(Vector256<T> left, Vector256<T> right) => Vector256.LessThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqual(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LessThanOrEqual(Vector256<T> left, Vector256<T> right) => Vector256.LessThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.LessThanOrEqualAll(Vector256<T> left, Vector256<T> right) => Vector256.LessThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector256<T>, T>.LessThanOrEqualAny(Vector256<T> left, Vector256<T> right) => Vector256.LessThanOrEqualAny(left, right);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Load(T*)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Load(T* source) => Vector256.Load(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAligned(T*)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LoadAligned(T* source) => Vector256.LoadAligned(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAlignedNonTemporal(T*)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LoadAlignedNonTemporal(T* source) => Vector256.LoadAlignedNonTemporal(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LoadUnsafe(ref readonly T source) => Vector256.LoadUnsafe(in source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T, nuint)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.LoadUnsafe(ref readonly T source, nuint elementOffset) => Vector256.LoadUnsafe(in source, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Max(Vector256<T> left, Vector256<T> right) => Vector256.Max(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Min(Vector256<T> left, Vector256<T> right) => Vector256.Min(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Multiply(Vector256<T> left, Vector256<T> right) => Vector256.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Multiply(Vector256<T> left, T right) => Vector256.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Negate(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Negate(Vector256<T> vector) => Vector256.Negate(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.OnesComplement(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.OnesComplement(Vector256<T> vector) => Vector256.OnesComplement(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftLeft(TSelf, int)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.ShiftLeft(Vector256<T> vector, int shiftCount) => Vector256.ShiftLeft(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightArithmetic(TSelf, int)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.ShiftRightArithmetic(Vector256<T> vector, int shiftCount) => Vector256.ShiftRightArithmetic(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightLogical(TSelf, int)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.ShiftRightLogical(Vector256<T> vector, int shiftCount) => Vector256.ShiftRightLogical(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sqrt(TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Sqrt(Vector256<T> vector) => Vector256.Sqrt(vector);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Store(TSelf, T*)" />
        static void ISimdVector<Vector256<T>, T>.Store(Vector256<T> source, T* destination) => Vector256.Store(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAligned(TSelf, T*)" />
        static void ISimdVector<Vector256<T>, T>.StoreAligned(Vector256<T> source, T* destination) => Vector256.StoreAligned(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAlignedNonTemporal(TSelf, T*)" />
        static void ISimdVector<Vector256<T>, T>.StoreAlignedNonTemporal(Vector256<T> source, T* destination) => Vector256.StoreAlignedNonTemporal(source, destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T)" />
        static void ISimdVector<Vector256<T>, T>.StoreUnsafe(Vector256<T> vector, ref T destination) => Vector256.StoreUnsafe(vector, ref destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T, nuint)" />
        static void ISimdVector<Vector256<T>, T>.StoreUnsafe(Vector256<T> vector, ref T destination, nuint elementOffset) => Vector256.StoreUnsafe(vector, ref destination, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Subtract(TSelf, TSelf)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Subtract(Vector256<T> left, Vector256<T> right) => Vector256.Subtract(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sum(TSelf)" />
        static T ISimdVector<Vector256<T>, T>.Sum(Vector256<T> vector) => Vector256.Sum(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ToScalar(TSelf)" />
        static T ISimdVector<Vector256<T>, T>.ToScalar(Vector256<T> vector) => Vector256.ToScalar(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.TryCopyTo(TSelf, Span{T})" />
        static bool ISimdVector<Vector256<T>, T>.TryCopyTo(Vector256<T> vector, Span<T> destination) => Vector256.TryCopyTo(vector, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.WithElement(TSelf, int, T)" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.WithElement(Vector256<T> vector, int index, T value) => Vector256.WithElement(vector, index, value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Xor" />
        static Vector256<T> ISimdVector<Vector256<T>, T>.Xor(Vector256<T> left, Vector256<T> right) => Vector256.Xor(left, right);

        //
        // New Surface Area
        //

        static int ISimdVector<Vector256<T>, T>.IndexOfLastMatch(Vector256<T> vector)
        {
            uint mask = vector.ExtractMostSignificantBits();
            return 31 - BitOperations.LeadingZeroCount(mask); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
        }
    }
}

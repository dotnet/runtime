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


    /// <summary>Represents a 128-bit vector of a specified numeric type that is suitable for low-level optimization of parallel algorithms.</summary>
    /// <typeparam name="T">The type of the elements in the vector.</typeparam>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector128DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector128.Size)]
    public readonly unsafe struct Vector128<T> : ISimdVector<Vector128<T>, T>
    {
        internal readonly Vector64<T> _lower;
        internal readonly Vector64<T> _upper;

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> AllBitsSet
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector64<T> vector = Vector64<T>.AllBitsSet;
                return Vector128.Create(vector, vector);
            }
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector128{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Vector64<T>.Count * 2;
            }
        }

        /// <summary>Gets a new <see cref="Vector128{T}" /> with the elements set to their index.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> Indices
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
                Unsafe.SkipInit(out Vector128<T> result);

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

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all elements initialized to one.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> One
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector64<T> vector = Vector64<T>.One;
                return Vector128.Create(vector, vector);
            }
        }

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> Zero
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
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
        public static Vector128<T> operator +(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator &(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator |(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator /(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator /(Vector128<T> left, T right)
        {
            return Vector128.Create(
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
        public static bool operator ==(Vector128<T> left, Vector128<T> right)
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
        public static Vector128<T> operator ^(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static bool operator !=(Vector128<T> left, Vector128<T> right)
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
        public static Vector128<T> operator <<(Vector128<T> value, int shiftCount)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator *(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator *(Vector128<T> left, T right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator *(T left, Vector128<T> right) => right * left;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator ~(Vector128<T> vector)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator >>(Vector128<T> value, int shiftCount)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator -(Vector128<T> left, Vector128<T> right)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator -(Vector128<T> vector)
        {
            return Vector128.Create(
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
        public static Vector128<T> operator +(Vector128<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            return value;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator >>>(Vector128<T> value, int shiftCount)
        {
            return Vector128.Create(
                value._lower >>> shiftCount,
                value._upper >>> shiftCount
            );
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector128{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector128<T> other) && Equals(other);

        // Account for floating-point equality around NaN
        // This is in a separate method so it can be optimized by the mono interpreter/jiterpreter
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool EqualsFloatingPoint (Vector128<T> lhs, Vector128<T> rhs)
        {
            Vector128<T> result = Vector128.Equals(lhs, rhs) | ~(Vector128.Equals(lhs, lhs) | Vector128.Equals(rhs, rhs));
            return result.AsInt32() == Vector128<int>.AllBitsSet;
        }

        /// <summary>Determines whether the specified <see cref="Vector128{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector128{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector128<T> other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector128.IsHardwareAccelerated)
            {
                if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                {
                    return EqualsFloatingPoint(this, other);
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
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

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
        static int ISimdVector<Vector128<T>, T>.Alignment => Vector128.Alignment;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.IsHardwareAccelerated" />
        static bool ISimdVector<Vector128<T>, T>.IsHardwareAccelerated => Vector128.IsHardwareAccelerated;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Abs(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Abs(Vector128<T> vector) => Vector128.Abs(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Add(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Add(Vector128<T> left, Vector128<T> right) => Vector128.Add(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.AndNot(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.AndNot(Vector128<T> left, Vector128<T> right) => Vector128.AndNot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseAnd(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.BitwiseAnd(Vector128<T> left, Vector128<T> right) => Vector128.BitwiseAnd(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseOr(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.BitwiseOr(Vector128<T> left, Vector128<T> right) => Vector128.BitwiseOr(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Ceiling(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Ceiling(Vector128<T> vector) => Vector128.Ceiling(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ConditionalSelect(TSelf, TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.ConditionalSelect(Vector128<T> condition, Vector128<T> left, Vector128<T> right) => Vector128.ConditionalSelect(condition, left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[])" />
        static void ISimdVector<Vector128<T>, T>.CopyTo(Vector128<T> vector, T[] destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[], int)" />
        static void ISimdVector<Vector128<T>, T>.CopyTo(Vector128<T> vector, T[] destination, int startIndex) => vector.CopyTo(destination, startIndex);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, Span{T})" />
        static void ISimdVector<Vector128<T>, T>.CopyTo(Vector128<T> vector, Span<T> destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Create(T value) => Vector128.Create(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[])" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Create(T[] values) => Vector128.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[], int)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Create(T[] values, int index) => Vector128.Create(values, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(ReadOnlySpan{T})" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Create(ReadOnlySpan<T> values) => Vector128.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalar(T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.CreateScalar(T value) => Vector128.CreateScalar(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalarUnsafe(T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.CreateScalarUnsafe(T value) => Vector128.CreateScalarUnsafe(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Divide(Vector128<T> left, Vector128<T> right) => Vector128.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Divide(Vector128<T> left, T right) => Vector128.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Dot(TSelf, TSelf)" />
        static T ISimdVector<Vector128<T>, T>.Dot(Vector128<T> left, Vector128<T> right) => Vector128.Dot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Equals(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Equals(Vector128<T> left, Vector128<T> right) => Vector128.Equals(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.EqualsAll(Vector128<T> left, Vector128<T> right) => Vector128.EqualsAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.EqualsAny(Vector128<T> left, Vector128<T> right) => Vector128.EqualsAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Floor(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Floor(Vector128<T> vector) => Vector128.Floor(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GetElement(TSelf, int)" />
        static T ISimdVector<Vector128<T>, T>.GetElement(Vector128<T> vector, int index) => Vector128.GetElement(vector, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThan(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.GreaterThan(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.GreaterThanAll(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.GreaterThanAny(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqual(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.GreaterThanOrEqual(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.GreaterThanOrEqualAll(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.GreaterThanOrEqualAny(Vector128<T> left, Vector128<T> right) => Vector128.GreaterThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThan(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LessThan(Vector128<T> left, Vector128<T> right) => Vector128.LessThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.LessThanAll(Vector128<T> left, Vector128<T> right) => Vector128.LessThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.LessThanAny(Vector128<T> left, Vector128<T> right) => Vector128.LessThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqual(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LessThanOrEqual(Vector128<T> left, Vector128<T> right) => Vector128.LessThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.LessThanOrEqualAll(Vector128<T> left, Vector128<T> right) => Vector128.LessThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector128<T>, T>.LessThanOrEqualAny(Vector128<T> left, Vector128<T> right) => Vector128.LessThanOrEqualAny(left, right);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Load(T*)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Load(T* source) => Vector128.Load(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAligned(T*)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LoadAligned(T* source) => Vector128.LoadAligned(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAlignedNonTemporal(T*)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LoadAlignedNonTemporal(T* source) => Vector128.LoadAlignedNonTemporal(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LoadUnsafe(ref readonly T source) => Vector128.LoadUnsafe(in source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T, nuint)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.LoadUnsafe(ref readonly T source, nuint elementOffset) => Vector128.LoadUnsafe(in source, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Max(Vector128<T> left, Vector128<T> right) => Vector128.Max(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Min(Vector128<T> left, Vector128<T> right) => Vector128.Min(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Multiply(Vector128<T> left, Vector128<T> right) => Vector128.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Multiply(Vector128<T> left, T right) => Vector128.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Negate(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Negate(Vector128<T> vector) => Vector128.Negate(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.OnesComplement(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.OnesComplement(Vector128<T> vector) => Vector128.OnesComplement(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftLeft(TSelf, int)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.ShiftLeft(Vector128<T> vector, int shiftCount) => Vector128.ShiftLeft(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightArithmetic(TSelf, int)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.ShiftRightArithmetic(Vector128<T> vector, int shiftCount) => Vector128.ShiftRightArithmetic(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightLogical(TSelf, int)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.ShiftRightLogical(Vector128<T> vector, int shiftCount) => Vector128.ShiftRightLogical(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sqrt(TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Sqrt(Vector128<T> vector) => Vector128.Sqrt(vector);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Store(TSelf, T*)" />
        static void ISimdVector<Vector128<T>, T>.Store(Vector128<T> source, T* destination) => Vector128.Store(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAligned(TSelf, T*)" />
        static void ISimdVector<Vector128<T>, T>.StoreAligned(Vector128<T> source, T* destination) => Vector128.StoreAligned(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAlignedNonTemporal(TSelf, T*)" />
        static void ISimdVector<Vector128<T>, T>.StoreAlignedNonTemporal(Vector128<T> source, T* destination) => Vector128.StoreAlignedNonTemporal(source, destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T)" />
        static void ISimdVector<Vector128<T>, T>.StoreUnsafe(Vector128<T> vector, ref T destination) => Vector128.StoreUnsafe(vector, ref destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T, nuint)" />
        static void ISimdVector<Vector128<T>, T>.StoreUnsafe(Vector128<T> vector, ref T destination, nuint elementOffset) => Vector128.StoreUnsafe(vector, ref destination, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Subtract(TSelf, TSelf)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Subtract(Vector128<T> left, Vector128<T> right) => Vector128.Subtract(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sum(TSelf)" />
        static T ISimdVector<Vector128<T>, T>.Sum(Vector128<T> vector) => Vector128.Sum(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ToScalar(TSelf)" />
        static T ISimdVector<Vector128<T>, T>.ToScalar(Vector128<T> vector) => Vector128.ToScalar(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.TryCopyTo(TSelf, Span{T})" />
        static bool ISimdVector<Vector128<T>, T>.TryCopyTo(Vector128<T> vector, Span<T> destination) => Vector128.TryCopyTo(vector, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.WithElement(TSelf, int, T)" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.WithElement(Vector128<T> vector, int index, T value) => Vector128.WithElement(vector, index, value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Xor" />
        static Vector128<T> ISimdVector<Vector128<T>, T>.Xor(Vector128<T> left, Vector128<T> right) => Vector128.Xor(left, right);

        //
        // New Surface Area
        //

        static int ISimdVector<Vector128<T>, T>.IndexOfLastMatch(Vector128<T> vector)
        {
            uint mask = vector.ExtractMostSignificantBits();
            return 31 - BitOperations.LeadingZeroCount(mask); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
        }
    }
}

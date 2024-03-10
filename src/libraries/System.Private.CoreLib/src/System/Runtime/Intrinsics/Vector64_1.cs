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

    /// <summary>Represents a 64-bit vector of a specified numeric type that is suitable for low-level optimization of parallel algorithms.</summary>
    /// <typeparam name="T">The type of the elements in the vector.</typeparam>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector64DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector64.Size)]
    public readonly unsafe struct Vector64<T> : ISimdVector<Vector64<T>, T>
    {
        // This field allows the debug view to work https://github.com/dotnet/runtime/issues/9495)
        internal readonly ulong _00;

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> AllBitsSet
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

                Unsafe.SkipInit(out Vector64<T> result);
                Unsafe.AsRef(in result._00) = ulong.MaxValue;

                return result;
            }
        }

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector64{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static unsafe int Count
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
                return Vector64.Size / sizeof(T);
            }
        }
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <summary>Gets a new <see cref="Vector64{T}" /> with the elements set to their index.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> Indices
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
                Unsafe.SkipInit(out Vector64<T> result);

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

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all elements initialized to one.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> One
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                T scalar = Scalar<T>.One;
                return Vector64.Create(scalar);
            }
        }

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> Zero
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
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
        public static Vector64<T> operator +(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Add(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator &(Vector64<T> left, Vector64<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = left._00 & right._00;

            return result;
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator |(Vector64<T> left, Vector64<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = left._00 | right._00;

            return result;
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator /(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Divide(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator /(Vector64<T> left, T right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Divide(left.GetElementUnsafe(index), right);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Count; index++)
            {
                if (!Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator ^(Vector64<T> left, Vector64<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = left._00 ^ right._00;

            return result;
        }

        /// <summary>Compares two vectors to determine if any elements are not equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was not equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector64<T> left, Vector64<T> right)
        {
            for (int index = 0; index < Count; index++)
            {
                if (!Scalar<T>.Equals(left.GetElementUnsafe(index), right.GetElementUnsafe(index)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator <<(Vector64<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T element = Scalar<T>.ShiftLeft(value.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator *(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Multiply(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The scalar to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator *(Vector64<T> left, T right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Multiply(left.GetElementUnsafe(index), right);
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="left">The scalar to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator *(T left, Vector64<T> right) => right * left;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator ~(Vector64<T> vector)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

            Unsafe.SkipInit(out Vector64<T> result);
            Unsafe.AsRef(in result._00) = ~vector._00;

            return result;
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator >>(Vector64<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T element = Scalar<T>.ShiftRightArithmetic(value.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator -(Vector64<T> left, Vector64<T> right)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Subtract(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="vector">The vector to negate.</param>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator -(Vector64<T> vector) => Zero - vector;

        /// <summary>Returns a given vector unchanged.</summary>
        /// <param name="value">The vector.</param>
        /// <returns><paramref name="value" /></returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator +(Vector64<T> value)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();
            return value;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<T> operator >>>(Vector64<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector64<T> result);

            for (int index = 0; index < Count; index++)
            {
                T element = Scalar<T>.ShiftRightLogical(value.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector64{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector64<T> other) && Equals(other);

        /// <summary>Determines whether the specified <see cref="Vector64{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector64{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector64<T> other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector64.IsHardwareAccelerated)
            {
                if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                {
                    Vector64<T> result = Vector64.Equals(this, other) | ~(Vector64.Equals(this, this) | Vector64.Equals(other, other));
                    return result.AsInt32() == Vector64<int>.AllBitsSet;
                }
                else
                {
                    return this == other;
                }
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Vector64<T> self, Vector64<T> other)
            {
                for (int index = 0; index < Count; index++)
                {
                    if (!Scalar<T>.ObjectEquals(self.GetElementUnsafe(index), other.GetElementUnsafe(index)))
                    {
                        return false;
                    }
                }
                return true;
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
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector64BaseType<T>();

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
        static int ISimdVector<Vector64<T>, T>.Alignment => Vector64.Alignment;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.IsHardwareAccelerated" />
        static bool ISimdVector<Vector64<T>, T>.IsHardwareAccelerated => Vector64.IsHardwareAccelerated;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Abs(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Abs(Vector64<T> vector) => Vector64.Abs(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Add(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Add(Vector64<T> left, Vector64<T> right) => Vector64.Add(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.AndNot(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.AndNot(Vector64<T> left, Vector64<T> right) => Vector64.AndNot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseAnd(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.BitwiseAnd(Vector64<T> left, Vector64<T> right) => Vector64.BitwiseAnd(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseOr(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.BitwiseOr(Vector64<T> left, Vector64<T> right) => Vector64.BitwiseOr(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Ceiling(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Ceiling(Vector64<T> vector) => Vector64.Ceiling(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ConditionalSelect(TSelf, TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.ConditionalSelect(Vector64<T> condition, Vector64<T> left, Vector64<T> right) => Vector64.ConditionalSelect(condition, left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[])" />
        static void ISimdVector<Vector64<T>, T>.CopyTo(Vector64<T> vector, T[] destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[], int)" />
        static void ISimdVector<Vector64<T>, T>.CopyTo(Vector64<T> vector, T[] destination, int startIndex) => vector.CopyTo(destination, startIndex);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, Span{T})" />
        static void ISimdVector<Vector64<T>, T>.CopyTo(Vector64<T> vector, Span<T> destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Create(T value) => Vector64.Create(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[])" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Create(T[] values) => Vector64.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[], int)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Create(T[] values, int index) => Vector64.Create(values, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(ReadOnlySpan{T})" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Create(ReadOnlySpan<T> values) => Vector64.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalar(T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.CreateScalar(T value) => Vector64.CreateScalar(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalarUnsafe(T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.CreateScalarUnsafe(T value) => Vector64.CreateScalarUnsafe(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Divide(Vector64<T> left, Vector64<T> right) => Vector64.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Divide(Vector64<T> left, T right) => Vector64.Divide(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Dot(TSelf, TSelf)" />
        static T ISimdVector<Vector64<T>, T>.Dot(Vector64<T> left, Vector64<T> right) => Vector64.Dot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Equals(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Equals(Vector64<T> left, Vector64<T> right) => Vector64.Equals(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.EqualsAll(Vector64<T> left, Vector64<T> right) => Vector64.EqualsAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.EqualsAny(Vector64<T> left, Vector64<T> right) => Vector64.EqualsAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Floor(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Floor(Vector64<T> vector) => Vector64.Floor(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GetElement(TSelf, int)" />
        static T ISimdVector<Vector64<T>, T>.GetElement(Vector64<T> vector, int index) => Vector64.GetElement(vector, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThan(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.GreaterThan(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.GreaterThanAll(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.GreaterThanAny(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqual(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.GreaterThanOrEqual(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.GreaterThanOrEqualAll(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.GreaterThanOrEqualAny(Vector64<T> left, Vector64<T> right) => Vector64.GreaterThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThan(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LessThan(Vector64<T> left, Vector64<T> right) => Vector64.LessThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.LessThanAll(Vector64<T> left, Vector64<T> right) => Vector64.LessThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.LessThanAny(Vector64<T> left, Vector64<T> right) => Vector64.LessThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqual(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LessThanOrEqual(Vector64<T> left, Vector64<T> right) => Vector64.LessThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.LessThanOrEqualAll(Vector64<T> left, Vector64<T> right) => Vector64.LessThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector64<T>, T>.LessThanOrEqualAny(Vector64<T> left, Vector64<T> right) => Vector64.LessThanOrEqualAny(left, right);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Load(T*)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Load(T* source) => Vector64.Load(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAligned(T*)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LoadAligned(T* source) => Vector64.LoadAligned(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAlignedNonTemporal(T*)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LoadAlignedNonTemporal(T* source) => Vector64.LoadAlignedNonTemporal(source);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LoadUnsafe(ref readonly T source) => Vector64.LoadUnsafe(in source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T, nuint)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.LoadUnsafe(ref readonly T source, nuint elementOffset) => Vector64.LoadUnsafe(in source, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Max(Vector64<T> left, Vector64<T> right) => Vector64.Max(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Min(Vector64<T> left, Vector64<T> right) => Vector64.Min(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Multiply(Vector64<T> left, Vector64<T> right) => Vector64.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Multiply(Vector64<T> left, T right) => Vector64.Multiply(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Negate(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Negate(Vector64<T> vector) => Vector64.Negate(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.OnesComplement(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.OnesComplement(Vector64<T> vector) => Vector64.OnesComplement(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftLeft(TSelf, int)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.ShiftLeft(Vector64<T> vector, int shiftCount) => Vector64.ShiftLeft(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightArithmetic(TSelf, int)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.ShiftRightArithmetic(Vector64<T> vector, int shiftCount) => Vector64.ShiftRightArithmetic(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightLogical(TSelf, int)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.ShiftRightLogical(Vector64<T> vector, int shiftCount) => Vector64.ShiftRightLogical(vector, shiftCount);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sqrt(TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Sqrt(Vector64<T> vector) => Vector64.Sqrt(vector);

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')
        /// <inheritdoc cref="ISimdVector{TSelf, T}.Store(TSelf, T*)" />
        static void ISimdVector<Vector64<T>, T>.Store(Vector64<T> source, T* destination) => Vector64.Store(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAligned(TSelf, T*)" />
        static void ISimdVector<Vector64<T>, T>.StoreAligned(Vector64<T> source, T* destination) => Vector64.StoreAligned(source, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAlignedNonTemporal(TSelf, T*)" />
        static void ISimdVector<Vector64<T>, T>.StoreAlignedNonTemporal(Vector64<T> source, T* destination) => Vector64.StoreAlignedNonTemporal(source, destination);
#pragma warning restore CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T)" />
        static void ISimdVector<Vector64<T>, T>.StoreUnsafe(Vector64<T> vector, ref T destination) => Vector64.StoreUnsafe(vector, ref destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T, nuint)" />
        static void ISimdVector<Vector64<T>, T>.StoreUnsafe(Vector64<T> vector, ref T destination, nuint elementOffset) => Vector64.StoreUnsafe(vector, ref destination, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Subtract(TSelf, TSelf)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Subtract(Vector64<T> left, Vector64<T> right) => Vector64.Subtract(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sum(TSelf)" />
        static T ISimdVector<Vector64<T>, T>.Sum(Vector64<T> vector) => Vector64.Sum(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ToScalar(TSelf)" />
        static T ISimdVector<Vector64<T>, T>.ToScalar(Vector64<T> vector) => Vector64.ToScalar(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.TryCopyTo(TSelf, Span{T})" />
        static bool ISimdVector<Vector64<T>, T>.TryCopyTo(Vector64<T> vector, Span<T> destination) => Vector64.TryCopyTo(vector, destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.WithElement(TSelf, int, T)" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.WithElement(Vector64<T> vector, int index, T value) => Vector64.WithElement(vector, index, value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Xor" />
        static Vector64<T> ISimdVector<Vector64<T>, T>.Xor(Vector64<T> left, Vector64<T> right) => Vector64.Xor(left, right);

        //
        // New Surface Area
        //

        static int ISimdVector<Vector64<T>, T>.IndexOfLastMatch(Vector64<T> vector)
        {
            uint mask = vector.ExtractMostSignificantBits();
            return 31 - BitOperations.LeadingZeroCount(mask); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
        }
    }
}

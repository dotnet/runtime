// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector128DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector128.Size)]
    public readonly struct Vector128<T> : IEquatable<Vector128<T>>
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/runtime/issues/9495)
        private readonly ulong _00;
        private readonly ulong _01;

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> AllBitsSet
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
                return Vector128.Create(0xFFFFFFFF).As<uint, T>();
            }
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector128{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
                return Vector128.Size / Unsafe.SizeOf<T>();
            }
        }

        internal static bool IsTypeSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (typeof(T) == typeof(byte)) ||
                   (typeof(T) == typeof(double)) ||
                   (typeof(T) == typeof(short)) ||
                   (typeof(T) == typeof(int)) ||
                   (typeof(T) == typeof(long)) ||
                   (typeof(T) == typeof(nint)) ||
                   (typeof(T) == typeof(nuint)) ||
                   (typeof(T) == typeof(sbyte)) ||
                   (typeof(T) == typeof(float)) ||
                   (typeof(T) == typeof(ushort)) ||
                   (typeof(T) == typeof(uint)) ||
                   (typeof(T) == typeof(ulong));
        }

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
                return default;
            }
        }

        internal unsafe string DisplayString
        {
            get
            {
                if (IsTypeSupported)
                {
                    return ToString();
                }
                else
                {
                    return SR.NotSupported_Type;
                }
            }
        }

        public T this[int index] => this.GetElement(index);

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<T> operator +(Vector128<T> left, Vector128<T> right)
        {
            Unsafe.SkipInit(out Vector128<T> result);

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
        [Intrinsic]
        public static unsafe Vector128<T> operator &(Vector128<T> left, Vector128<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            Unsafe.SkipInit(out Vector128<T> result);

            Unsafe.AsRef(in result._00) = left._00 & right._00;
            Unsafe.AsRef(in result._01) = left._01 & right._01;

            return result;
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        public static unsafe Vector128<T> operator |(Vector128<T> left, Vector128<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            Unsafe.SkipInit(out Vector128<T> result);

            Unsafe.AsRef(in result._00) = left._00 | right._00;
            Unsafe.AsRef(in result._01) = left._01 | right._01;

            return result;
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<T> operator /(Vector128<T> left, Vector128<T> right)
        {
            Unsafe.SkipInit(out Vector128<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Divide(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        public static bool operator ==(Vector128<T> left, Vector128<T> right)
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
        [Intrinsic]
        public static unsafe Vector128<T> operator ^(Vector128<T> left, Vector128<T> right)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();
            Unsafe.SkipInit(out Vector128<T> result);

            Unsafe.AsRef(in result._00) = left._00 ^ right._00;
            Unsafe.AsRef(in result._01) = left._01 ^ right._01;

            return result;
        }

        /// <summary>Compares two vectors to determine if any elements are not equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was not equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector128<T> left, Vector128<T> right)
            => !(left == right);

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<T> operator *(Vector128<T> left, Vector128<T> right)
        {
            Unsafe.SkipInit(out Vector128<T> result);

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
        [Intrinsic]
        public static Vector128<T> operator *(Vector128<T> left, T right)
        {
            Unsafe.SkipInit(out Vector128<T> result);

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
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator *(T left, Vector128<T> right)
            => right * left;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="vector">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="vector" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator ~(Vector128<T> vector) => AllBitsSet ^ vector;

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector128<T> operator -(Vector128<T> left, Vector128<T> right)
        {
            Unsafe.SkipInit(out Vector128<T> result);

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
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator -(Vector128<T> vector) => Zero - vector;

        /// <summary>Returns a given vector unchanged.</summary>
        /// <param name="value">The vector.</param>
        /// <returns><paramref name="value" /></returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> operator +(Vector128<T> value) => value;

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector128{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is Vector128<T> other) && Equals(other);

        /// <summary>Determines whether the specified <see cref="Vector128{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector128{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector128<T> other)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            if (Sse.IsSupported && (typeof(T) == typeof(float)))
            {
                Vector128<float> result = Sse.CompareEqual(this.AsSingle(), other.AsSingle());
                return Sse.MoveMask(result) == 0b1111; // We have one bit per element
            }

            if (Sse2.IsSupported)
            {
                if (typeof(T) == typeof(double))
                {
                    Vector128<double> result = Sse2.CompareEqual(this.AsDouble(), other.AsDouble());
                    return Sse2.MoveMask(result) == 0b11; // We have one bit per element
                }
                else if (Sse41.IsSupported)
                {
                    // xor + testz is slightly better for integer types
                    Vector128<byte> xored = Sse2.Xor(this.AsByte(), other.AsByte());
                    return Sse41.TestZ(xored, xored);
                }
                else
                {
                    // Unlike float/double, there are no special values to consider
                    // for integral types and we can just do a comparison that all
                    // bytes are exactly the same.

                    Debug.Assert((typeof(T) != typeof(float)) && (typeof(T) != typeof(double)));
                    Vector128<byte> result = Sse2.CompareEqual(this.AsByte(), other.AsByte());
                    return Sse2.MoveMask(result) == 0b1111_1111_1111_1111; // We have one bit per element
                }
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Vector128<T> vector, Vector128<T> other)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!((IEquatable<T>)(vector.GetElement(i))).Equals(other.GetElement(i)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Gets the hash code for the instance.</summary>
        /// <returns>The hash code for the instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override int GetHashCode()
        {
            HashCode hashCode = default;

            for (int i = 0; i < Count; i++)
            {
                T value = this.GetElement(i);
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>Converts the current instance to an equivalent string representation.</summary>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override string ToString()
            => ToString("G", CultureInfo.InvariantCulture);

        private string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector128BaseType<T>();

            var sb = new ValueStringBuilder(stackalloc char[64]);
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;

            sb.Append('<');
            sb.Append(((IFormattable)this.GetElement(0)).ToString(format, formatProvider));

            for (int i = 1; i < Count; i++)
            {
                sb.Append(separator);
                sb.Append(' ');
                sb.Append(((IFormattable)this.GetElement(i)).ToString(format, formatProvider));
            }
            sb.Append('>');

            return sb.ToString();
        }
    }
}

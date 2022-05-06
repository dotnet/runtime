// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;

namespace System.Numerics
{
    /* Note: The following patterns are used throughout the code here and are described here
    *
    * PATTERN:
    *    if (typeof(T) == typeof(int)) { ... }
    *    else if (typeof(T) == typeof(float)) { ... }
    * EXPLANATION:
    *    At runtime, each instantiation of Vector<T> will be type-specific, and each of these typeof blocks will be eliminated,
    *    as typeof(T) is a (JIT) compile-time constant for each instantiation. This design was chosen to eliminate any overhead from
    *    delegates and other patterns.
    *
    * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

    /// <summary>
    /// A structure that represents a single Vector. The count of this Vector is fixed but CPU register dependent.
    /// This struct only supports numerical types. This type is intended to be used as a building block for vectorizing
    /// large algorithms. This type is immutable, individual elements cannot be modified.
    /// </summary>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(VectorDebugView<>))]
    public readonly struct Vector<T> : IEquatable<Vector<T>>, IFormattable
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        internal readonly ulong _00;
        internal readonly ulong _01;

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public unsafe Vector(T value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out this);

            for (int index = 0; index < Count; index++)
            {
                this.SetElementUnsafe(index, value);
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        [Intrinsic]
        public unsafe Vector(T[] values) : this(values, 0)
        {
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector128{T}.Count" />.</exception>
        [Intrinsic]
        public unsafe Vector(T[] values, int index)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((index < 0) || ((values.Length - index) < Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given readonly span.</summary>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<T> values)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values.Length < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given readonly span.</summary>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <c>sizeof(<see cref="Vector{T}" />)</c> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<byte> values)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values.Length < Vector<byte>.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(values));
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given span.</summary>
        /// <param name="values">The span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Span<T> values) : this((ReadOnlySpan<T>)values)
        {
        }

        /// <summary>Gets a new <see cref="Vector{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        internal static Vector<T> AllBitsSet
        {
            [Intrinsic]
            get => new Vector<T>(Scalar<T>.AllBitsSet);
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return Unsafe.SizeOf<Vector<T>>() / Unsafe.SizeOf<T>();
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

        /// <summary>Gets a new <see cref="Vector{T}" /> with all elements initialized to one.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector<T> One
        {
            [Intrinsic]
            get => new Vector<T>(Scalar<T>.One);
        }

        /// <summary>Gets a new <see cref="Vector{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
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

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public unsafe T this[int index]
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

                if ((uint)index >= (uint)Count)
                {
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
                }

                return this.GetElementUnsafe(index);
            }
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator +(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        public static unsafe Vector<T> operator &(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<T> result);

            Unsafe.AsRef(in result._00) = left._00 & right._00;
            Unsafe.AsRef(in result._01) = left._01 & right._01;

            return result;
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator |(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<T> result);

            Unsafe.AsRef(in result._00) = left._00 | right._00;
            Unsafe.AsRef(in result._01) = left._01 | right._01;

            return result;
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
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
        public static unsafe Vector<T> operator ^(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<T> result);

            Unsafe.AsRef(in result._00) = left._00 ^ right._00;
            Unsafe.AsRef(in result._01) = left._01 ^ right._01;

            return result;
        }

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Byte}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<byte>(Vector<T> value)
            => value.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<double>(Vector<T> value)
            => value.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int16}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<short>(Vector<T> value)
            => value.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int32}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<int>(Vector<T> value)
            => value.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int64}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<long>(Vector<T> value)
            => value.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{IntPtr}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<nint>(Vector<T> value)
            => value.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UIntPtr}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<nuint>(Vector<T> value)
            => value.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{SByte}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<sbyte>(Vector<T> value)
            => value.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<float>(Vector<T> value)
            => value.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt16}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<ushort>(Vector<T> value)
            => value.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt32}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<uint>(Vector<T> value)
            => value.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt64}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<ulong>(Vector<T> value)
            => value.As<T, ulong>();

        /// <summary>Compares two vectors to determine if any elements are not equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was not equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector<T> left, Vector<T> right)
            => !(left == right);

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator *(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Multiply(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="value">The vector to multiply with <paramref name="factor" />.</param>
        /// <param name="factor">The scalar to multiply with <paramref name="value" />.</param>
        /// <returns>The product of <paramref name="value" /> and <paramref name="factor" />.</returns>
        [Intrinsic]
        public static Vector<T> operator *(Vector<T> value, T factor)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Count; index++)
            {
                T element = Scalar<T>.Multiply(value.GetElementUnsafe(index), factor);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="factor">The scalar to multiply with <paramref name="value" />.</param>
        /// <param name="value">The vector to multiply with <paramref name="factor" />.</param>
        /// <returns>The product of <paramref name="factor" /> and <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(T factor, Vector<T> value)
            => value * factor;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ~(Vector<T> value) => AllBitsSet ^ value;

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Count; index++)
            {
                T value = Scalar<T>.Subtract(left.GetElementUnsafe(index), right.GetElementUnsafe(index));
                result.SetElementUnsafe(index, value);
            }

            return result;
        }

        /// <summary>Computes the unary negation of a vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>A vector whose elements are the unary negation of the corresponding elements in <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> value) => Zero - value;

        /// <summary>Copies a <see cref="Vector{T}" /> to a given array.</summary>
        /// <param name="destination">The array to which the current instance is copied.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [Intrinsic]
        public void CopyTo(T[] destination) => CopyTo(destination, 0);

        /// <summary>Copies a <see cref="Vector{T}" /> to a given array starting at the specified index.</summary>
        /// <param name="destination">The array to which the current instance is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which current instance will be copied to.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        [Intrinsic]
        public unsafe void CopyTo(T[] destination, int startIndex)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (destination is null)
            {
                ThrowHelper.ThrowNullReferenceException();
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref destination[startIndex]), this);
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</exception>
        public void CopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Vector<byte>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(destination), this);
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        public void CopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>Returns a boolean indicating whether the given Object is equal to this vector instance.</summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this vector; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals([NotNullWhen(true)] object? obj)
            => (obj is Vector<T> other) && Equals(other);

        /// <summary>Returns a boolean indicating whether the given vector is equal to this vector instance.</summary>
        /// <param name="other">The vector to compare this instance to.</param>
        /// <returns>True if the other vector is equal to this instance; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector<T> other)
        {
            // This function needs to account for floating-point equality around NaN
            // and so must behave equivalently to the underlying float/double.Equals

            if (Vector.IsHardwareAccelerated)
            {
                if ((typeof(T) == typeof(double)) || (typeof(T) == typeof(float)))
                {
                    Vector<T> result = Vector.Equals(this, other) | ~(Vector.Equals(this, this) | Vector.Equals(other, other));
                    return result.As<T, int>() == Vector<int>.AllBitsSet;
                }
                else
                {
                    return this == other;
                }
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Vector<T> self, Vector<T> other)
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

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            HashCode hashCode = default;

            for (int index = 0; index < Count; index++)
            {
                T value = this.GetElementUnsafe(index);
                hashCode.Add(value);
            }

            return hashCode.ToHashCode();
        }

        /// <summary>Returns a String representing this vector.</summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
            => ToString("G", CultureInfo.CurrentCulture);

        /// <summary>Returns a String representing this vector, using the specified format string to format individual elements.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format)
            => ToString(format, CultureInfo.CurrentCulture);

        /// <summary>Returns a String representing this vector, using the specified format string to format individual elements and the given IFormatProvider.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? formatProvider)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

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

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <returns><c>true</c> if the current instance was succesfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</returns>
        public bool TryCopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Vector<byte>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(destination), this);
            return true;
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <returns><c>true</c> if the current instance was succesfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</returns>
        public bool TryCopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
            return true;
        }
    }
}

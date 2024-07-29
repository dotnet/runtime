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

    /// <summary>Represents a single vector of a specified numeric type that is suitable for low-level optimization of parallel algorithms.</summary>
    /// <typeparam name="T">The type of the elements in the vector. <typeparamref name="T" /> can be any primitive numeric type.</typeparam>
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(VectorDebugView<>))]
    public readonly unsafe struct Vector<T> : ISimdVector<Vector<T>, T>, IFormattable
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        internal readonly ulong _00;
        internal readonly ulong _01;

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public Vector(T value)
        {
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
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(T[] values)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (values.Length < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[0]));
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(T[] values, int index)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

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
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

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
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons
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
        public Vector(Span<T> values) : this((ReadOnlySpan<T>)values)
        {
        }

        /// <summary>Gets a new <see cref="Vector{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector<T> AllBitsSet
        {
            [Intrinsic]
            get => Vector.Create(Scalar<T>.AllBitsSet);
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return sizeof(Vector<T>) / sizeof(T);
            }
        }

        /// <summary>Gets a new <see cref="Vector{T}" /> with the elements set to their index.</summary>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        public static Vector<T> Indices
        {
            [Intrinsic]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                Unsafe.SkipInit(out Vector<T> result);

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
            get => Vector.Create(Scalar<T>.One);
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

        internal string DisplayString => IsSupported ? ToString() : SR.NotSupported_Type;

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public T this[int index]
        {
            [Intrinsic]
            get => this.GetElement(index);
        }

        /// <summary>Adds two vectors to compute their sum.</summary>
        /// <param name="left">The vector to add with <paramref name="right" />.</param>
        /// <param name="right">The vector to add with <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator +(Vector<T> left, Vector<T> right)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator &(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<ulong> result);

            Vector<ulong> vleft = left.As<T, ulong>();
            Vector<ulong> vright = right.As<T, ulong>();

            for (int index = 0; index < Vector<ulong>.Count; index++)
            {
                ulong value = vleft.GetElementUnsafe(index) & vright.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result.As<ulong, T>();
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator |(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<ulong> result);

            Vector<ulong> vleft = left.As<T, ulong>();
            Vector<ulong> vright = right.As<T, ulong>();

            for (int index = 0; index < Vector<ulong>.Count; index++)
            {
                ulong value = vleft.GetElementUnsafe(index) | vright.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result.As<ulong, T>();
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        public static Vector<T> operator /(Vector<T> left, T right)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ^(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<ulong> result);

            Vector<ulong> vleft = left.As<T, ulong>();
            Vector<ulong> vright = right.As<T, ulong>();

            for (int index = 0; index < Vector<ulong>.Count; index++)
            {
                ulong value = vleft.GetElementUnsafe(index) ^ vright.GetElementUnsafe(index);
                result.SetElementUnsafe(index, value);
            }

            return result.As<ulong, T>();
        }

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Byte}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<byte>(Vector<T> value) => value.As<T, byte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Double}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<double>(Vector<T> value) => value.As<T, double>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int16}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<short>(Vector<T> value) => value.As<T, short>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int32}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<int>(Vector<T> value) => value.As<T, int>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Int64}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<long>(Vector<T> value) => value.As<T, long>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{IntPtr}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{IntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<nint>(Vector<T> value) => value.As<T, nint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UIntPtr}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UIntPtr}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static explicit operator Vector<nuint>(Vector<T> value) => value.As<T, nuint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{SByte}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static explicit operator Vector<sbyte>(Vector<T> value) => value.As<T, sbyte>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{Single}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static explicit operator Vector<float>(Vector<T> value) => value.As<T, float>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt16}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static explicit operator Vector<ushort>(Vector<T> value) => value.As<T, ushort>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt32}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static explicit operator Vector<uint>(Vector<T> value) => value.As<T, uint>();

        /// <summary>Reinterprets a <see cref="Vector{T}" /> as a new <see cref="Vector{UInt64}" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="value" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static explicit operator Vector<ulong>(Vector<T> value) => value.As<T, ulong>();

        /// <summary>Compares two vectors to determine if any elements are not equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was not equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        public static bool operator !=(Vector<T> left, Vector<T> right) => !(left == right);

        /// <summary>Shifts each element of a vector left by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted left by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator <<(Vector<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(Vector<T> left, Vector<T> right)
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
        public static Vector<T> operator *(Vector<T> value, T factor) => value * Vector.Create(factor);

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="factor">The scalar to multiply with <paramref name="value" />.</param>
        /// <param name="value">The vector to multiply with <paramref name="factor" />.</param>
        /// <returns>The product of <paramref name="factor" /> and <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> operator *(T factor, Vector<T> value) => value * factor;

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ~(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out Vector<ulong> result);

            Vector<ulong> vector = value.As<T, ulong>();

            for (int index = 0; index < Vector<ulong>.Count; index++)
            {
                ulong element = ~vector.GetElementUnsafe(index);
                result.SetElementUnsafe(index, element);
            }

            return result.As<ulong, T>();
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator >>(Vector<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector<T> result);

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
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> left, Vector<T> right)
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
        [Intrinsic]
        public static Vector<T> operator -(Vector<T> value) => Zero - value;

        /// <summary>Returns a given vector unchanged.</summary>
        /// <param name="value">The vector.</param>
        /// <returns><paramref name="value" /></returns>
        /// <exception cref="NotSupportedException">The type of the vector (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector<T> operator +(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return value;
        }

        /// <summary>Shifts (unsigned) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator >>>(Vector<T> value, int shiftCount)
        {
            Unsafe.SkipInit(out Vector<T> result);

            for (int index = 0; index < Count; index++)
            {
                T element = Scalar<T>.ShiftRightLogical(value.GetElementUnsafe(index), shiftCount);
                result.SetElementUnsafe(index, element);
            }

            return result;
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given array.</summary>
        /// <param name="destination">The array to which the current instance is copied.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] destination)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (destination.Length < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[0]), this);
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given array starting at the specified index.</summary>
        /// <param name="destination">The array to which the current instance is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which current instance will be copied to.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] destination, int startIndex)
        {
            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)startIndex >= (uint)destination.Length)
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), this);
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (destination.Length < Vector<byte>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination)
        {
            if (destination.Length < Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>Returns a boolean indicating whether the given Object is equal to this vector instance.</summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this vector; False otherwise.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector<T> other) && Equals(other);

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
        public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <summary>Returns a String representing this vector, using the specified format string to format individual elements.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => ToString(format, CultureInfo.CurrentCulture);

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
        /// <returns><c>true</c> if the current instance was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (destination.Length < Vector<byte>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
            return true;
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <returns><c>true</c> if the current instance was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination)
        {
            if (destination.Length < Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
            return true;
        }

        //
        // ISimdVector
        //

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Alignment" />
        static int ISimdVector<Vector<T>, T>.Alignment => Vector.Alignment;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.IsHardwareAccelerated" />
        static bool ISimdVector<Vector<T>, T>.IsHardwareAccelerated => Vector.IsHardwareAccelerated;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Abs(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Abs(Vector<T> vector) => Vector.Abs(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Add(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Add(Vector<T> left, Vector<T> right) => left + right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.AndNot(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.AndNot(Vector<T> left, Vector<T> right) => Vector.AndNot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseAnd(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.BitwiseAnd(Vector<T> left, Vector<T> right) => left & right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.BitwiseOr(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.BitwiseOr(Vector<T> left, Vector<T> right) => left | right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Ceiling(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Ceiling(Vector<T> vector) => Vector.Ceiling(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Clamp(TSelf, TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Clamp(Vector<T> value, Vector<T> min, Vector<T> max) => Vector.Clamp(value, min, max);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ClampNative(TSelf, TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.ClampNative(Vector<T> value, Vector<T> min, Vector<T> max) => Vector.ClampNative(value, min, max);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ConditionalSelect(TSelf, TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.ConditionalSelect(Vector<T> condition, Vector<T> left, Vector<T> right) => Vector.ConditionalSelect(condition, left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopySign(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.CopySign(Vector<T> value, Vector<T> sign) => Vector.CopySign(value, sign);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[])" />
        static void ISimdVector<Vector<T>, T>.CopyTo(Vector<T> vector, T[] destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, T[], int)" />
        static void ISimdVector<Vector<T>, T>.CopyTo(Vector<T> vector, T[] destination, int startIndex) => vector.CopyTo(destination, startIndex);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CopyTo(TSelf, Span{T})" />
        static void ISimdVector<Vector<T>, T>.CopyTo(Vector<T> vector, Span<T> destination) => vector.CopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Create(T value) => Vector.Create(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[])" />
        static Vector<T> ISimdVector<Vector<T>, T>.Create(T[] values) => new Vector<T>(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(T[], int)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Create(T[] values, int index) => new Vector<T>(values, index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Create(ReadOnlySpan{T})" />
        static Vector<T> ISimdVector<Vector<T>, T>.Create(ReadOnlySpan<T> values) => Vector.Create(values);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalar(T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.CreateScalar(T value) => Vector.CreateScalar(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.CreateScalarUnsafe(T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.CreateScalarUnsafe(T value) => Vector.CreateScalarUnsafe(value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Divide(Vector<T> left, Vector<T> right) => left / right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Divide(TSelf, T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Divide(Vector<T> left, T right) => left / right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Dot(TSelf, TSelf)" />
        static T ISimdVector<Vector<T>, T>.Dot(Vector<T> left, Vector<T> right) => Vector.Dot(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Equals(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Equals(Vector<T> left, Vector<T> right) => Vector.Equals(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.EqualsAll(Vector<T> left, Vector<T> right) => left == right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.EqualsAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.EqualsAny(Vector<T> left, Vector<T> right) => Vector.EqualsAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Floor(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Floor(Vector<T> vector) => Vector.Floor(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GetElement(TSelf, int)" />
        static T ISimdVector<Vector<T>, T>.GetElement(Vector<T> vector, int index) => vector.GetElement(index);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThan(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.GreaterThan(Vector<T> left, Vector<T> right) => Vector.GreaterThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.GreaterThanAll(Vector<T> left, Vector<T> right) => Vector.GreaterThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.GreaterThanAny(Vector<T> left, Vector<T> right) => Vector.GreaterThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqual(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.GreaterThanOrEqual(Vector<T> left, Vector<T> right) => Vector.GreaterThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.GreaterThanOrEqualAll(Vector<T> left, Vector<T> right) => Vector.GreaterThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.GreaterThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.GreaterThanOrEqualAny(Vector<T> left, Vector<T> right) => Vector.GreaterThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThan(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LessThan(Vector<T> left, Vector<T> right) => Vector.LessThan(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.LessThanAll(Vector<T> left, Vector<T> right) => Vector.LessThanAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.LessThanAny(Vector<T> left, Vector<T> right) => Vector.LessThanAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqual(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LessThanOrEqual(Vector<T> left, Vector<T> right) => Vector.LessThanOrEqual(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAll(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.LessThanOrEqualAll(Vector<T> left, Vector<T> right) => Vector.LessThanOrEqualAll(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LessThanOrEqualAny(TSelf, TSelf)" />
        static bool ISimdVector<Vector<T>, T>.LessThanOrEqualAny(Vector<T> left, Vector<T> right) => Vector.LessThanOrEqualAny(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Load(T*)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Load(T* source) => Vector.Load(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAligned(T*)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LoadAligned(T* source) => Vector.LoadAligned(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadAlignedNonTemporal(T*)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LoadAlignedNonTemporal(T* source) => Vector.LoadAlignedNonTemporal(source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LoadUnsafe(ref readonly T source) => Vector.LoadUnsafe(in source);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.LoadUnsafe(ref readonly T, nuint)" />
        static Vector<T> ISimdVector<Vector<T>, T>.LoadUnsafe(ref readonly T source, nuint elementOffset) => Vector.LoadUnsafe(in source, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Max(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Max(Vector<T> left, Vector<T> right) => Vector.Max(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitude(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MaxMagnitude(Vector<T> left, Vector<T> right) => Vector.MaxMagnitude(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxMagnitudeNumber(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MaxMagnitudeNumber(Vector<T> left, Vector<T> right) => Vector.MaxMagnitudeNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNative(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MaxNative(Vector<T> left, Vector<T> right) => Vector.MaxNative(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MaxNumber(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MaxNumber(Vector<T> left, Vector<T> right) => Vector.MaxNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Min(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Min(Vector<T> left, Vector<T> right) => Vector.Min(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitude(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MinMagnitude(Vector<T> left, Vector<T> right) => Vector.MinMagnitude(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinMagnitudeNumber(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MinMagnitudeNumber(Vector<T> left, Vector<T> right) => Vector.MinMagnitudeNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNative(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MinNative(Vector<T> left, Vector<T> right) => Vector.MinNative(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MinNumber(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MinNumber(Vector<T> left, Vector<T> right) => Vector.MinNumber(left, right);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Multiply(Vector<T> left, Vector<T> right) => left * right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Multiply(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Multiply(Vector<T> left, T right) => left * right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.MultiplyAddEstimate(TSelf, TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.MultiplyAddEstimate(Vector<T> left, Vector<T> right, Vector<T> addend) => Vector.MultiplyAddEstimate(left, right, addend);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Negate(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Negate(Vector<T> vector) => -vector;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.OnesComplement(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.OnesComplement(Vector<T> vector) => ~vector;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Round(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Round(Vector<T> vector) => Vector.Round(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftLeft(TSelf, int)" />
        static Vector<T> ISimdVector<Vector<T>, T>.ShiftLeft(Vector<T> vector, int shiftCount) => vector << shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightArithmetic(TSelf, int)" />
        static Vector<T> ISimdVector<Vector<T>, T>.ShiftRightArithmetic(Vector<T> vector, int shiftCount) => vector >> shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ShiftRightLogical(TSelf, int)" />
        static Vector<T> ISimdVector<Vector<T>, T>.ShiftRightLogical(Vector<T> vector, int shiftCount) => vector >>> shiftCount;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sqrt(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Sqrt(Vector<T> vector) => Vector.SquareRoot(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Store(TSelf, T*)" />
        static void ISimdVector<Vector<T>, T>.Store(Vector<T> source, T* destination) => source.Store(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAligned(TSelf, T*)" />
        static void ISimdVector<Vector<T>, T>.StoreAligned(Vector<T> source, T* destination) => source.StoreAligned(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreAlignedNonTemporal(TSelf, T*)" />
        static void ISimdVector<Vector<T>, T>.StoreAlignedNonTemporal(Vector<T> source, T* destination) => source.StoreAlignedNonTemporal(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T)" />
        static void ISimdVector<Vector<T>, T>.StoreUnsafe(Vector<T> vector, ref T destination) => vector.StoreUnsafe(ref destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.StoreUnsafe(TSelf, ref T, nuint)" />
        static void ISimdVector<Vector<T>, T>.StoreUnsafe(Vector<T> vector, ref T destination, nuint elementOffset) => vector.StoreUnsafe(ref destination, elementOffset);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Subtract(TSelf, TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Subtract(Vector<T> left, Vector<T> right) => left - right;

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Sum(TSelf)" />
        static T ISimdVector<Vector<T>, T>.Sum(Vector<T> vector) => Vector.Sum(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.ToScalar(TSelf)" />
        static T ISimdVector<Vector<T>, T>.ToScalar(Vector<T> vector) => vector.ToScalar();

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Truncate(TSelf)" />
        static Vector<T> ISimdVector<Vector<T>, T>.Truncate(Vector<T> vector) => Vector.Truncate(vector);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.TryCopyTo(TSelf, Span{T})" />
        static bool ISimdVector<Vector<T>, T>.TryCopyTo(Vector<T> vector, Span<T> destination) => vector.TryCopyTo(destination);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.WithElement(TSelf, int, T)" />
        static Vector<T> ISimdVector<Vector<T>, T>.WithElement(Vector<T> vector, int index, T value) => vector.WithElement(index, value);

        /// <inheritdoc cref="ISimdVector{TSelf, T}.Xor" />
        static Vector<T> ISimdVector<Vector<T>, T>.Xor(Vector<T> left, Vector<T> right) => left ^ right;

        //
        // New Surface Area
        //

        static bool ISimdVector<Vector<T>, T>.AnyWhereAllBitsSet(Vector<T> vector) => Vector.EqualsAny(vector, AllBitsSet);

        static bool ISimdVector<Vector<T>, T>.Any(Vector<T> vector, T value) => Vector.EqualsAny(vector, Vector.Create(value));

        static int ISimdVector<Vector<T>, T>.IndexOfLastMatch(Vector<T> vector)
        {
            if (sizeof(Vector<T>) == 64)
            {
                ulong mask = vector.AsVector512().ExtractMostSignificantBits();
                return 63 - BitOperations.LeadingZeroCount(mask); // 63 = 64 (bits in Int64) - 1 (indexing from zero)
            }
            else if (sizeof(Vector<T>) == 32)
            {
                uint mask = vector.AsVector256().ExtractMostSignificantBits();
                return 31 - BitOperations.LeadingZeroCount(mask); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                uint mask = vector.AsVector128().ExtractMostSignificantBits();
                return 31 - BitOperations.LeadingZeroCount(mask); // 31 = 32 (bits in Int32) - 1 (indexing from zero)
            }
        }

        static Vector<T> ISimdVector<Vector<T>, T>.IsNaN(Vector<T> vector) => Vector.IsNaN(vector);

        static Vector<T> ISimdVector<Vector<T>, T>.IsNegative(Vector<T> vector) => Vector.IsNegative(vector);

        static Vector<T> ISimdVector<Vector<T>, T>.IsPositive(Vector<T> vector) => Vector.IsPositive(vector);

        static Vector<T> ISimdVector<Vector<T>, T>.IsPositiveInfinity(Vector<T> vector) => Vector.IsPositiveInfinity(vector);

        static Vector<T> ISimdVector<Vector<T>, T>.IsZero(Vector<T> vector) => Vector.IsZero(vector);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type ('T')

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
    public readonly unsafe struct Vector<T> : IEquatable<Vector<T>>, IFormattable
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        internal readonly ulong _00;
        internal readonly ulong _01;

        /// <summary>Creates a new <see cref="Vector{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(T value)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this = Vector512.Create(value).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this = Vector256.Create(value).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this = Vector128.Create(value).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector128{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(T[] values)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this = Vector512.Create(values).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this = Vector256.Create(values).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this = Vector128.Create(values).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Vector128{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(T[] values, int index)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this = Vector512.Create(values, index).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this = Vector256.Create(values, index).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this = Vector128.Create(values, index).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given readonly span.</summary>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <see cref="Vector{T}.Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<T> values)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this = Vector512.Create(values).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this = Vector256.Create(values).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this = Vector128.Create(values).AsVector();
            }
        }

        /// <summary>Creates a new <see cref="Vector{T}" /> from a given readonly span.</summary>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new <see cref="Vector{T}" /> with its elements set to the first <c>sizeof(<see cref="Vector{T}" />)</c> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<byte> values)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this = Vector512.Create(values).AsVector().As<byte, T>();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this = Vector256.Create(values).AsVector().As<byte, T>();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this = Vector128.Create(values).AsVector().As<byte, T>();
            }
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
            get => new Vector<T>(Scalar<T>.AllBitsSet);
        }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                if (sizeof(Vector<T>) == 64)
                {
                    return Vector512<T>.Count;
                }
                else if (sizeof(Vector<T>) == 32)
                {
                    return Vector256<T>.Count;
                }
                else
                {
                    Debug.Assert(sizeof(Vector<T>) == 16);
                    return Vector128<T>.Count;
                }
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
                if (sizeof(Vector<T>) == 64)
                {
                    return Vector512<T>.Indices.AsVector();
                }
                else if (sizeof(Vector<T>) == 32)
                {
                    return Vector256<T>.Indices.AsVector();
                }
                else
                {
                    Debug.Assert(sizeof(Vector<T>) == 16);
                    return Vector128<T>.Indices.AsVector();
                }
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
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() + right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() + right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() + right.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the bitwise-and of two vectors.</summary>
        /// <param name="left">The vector to bitwise-and with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-and with <paramref name="left" />.</param>
        /// <returns>The bitwise-and of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator &(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() & right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() & right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() & right.AsVector128()).AsVector();
            }
        }

        /// <summary>Computes the bitwise-or of two vectors.</summary>
        /// <param name="left">The vector to bitwise-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to bitwise-or with <paramref name="left" />.</param>
        /// <returns>The bitwise-or of <paramref name="left" /> and <paramref name="right"/>.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator |(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() | right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() | right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() | right.AsVector128()).AsVector();
            }
        }

        /// <summary>Divides two vectors to compute their quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The vector that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() / right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() / right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() / right.AsVector128()).AsVector();
            }
        }

        /// <summary>Divides a vector by a scalar to compute the per-element quotient.</summary>
        /// <param name="left">The vector that will be divided by <paramref name="right" />.</param>
        /// <param name="right">The scalar that will divide <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided by <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator /(Vector<T> left, T right) => left / new Vector<T>(right);

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return left.AsVector512() == right.AsVector512();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return left.AsVector256() == right.AsVector256();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return left.AsVector128() == right.AsVector128();
            }
        }

        /// <summary>Computes the exclusive-or of two vectors.</summary>
        /// <param name="left">The vector to exclusive-or with <paramref name="right" />.</param>
        /// <param name="right">The vector to exclusive-or with <paramref name="left" />.</param>
        /// <returns>The exclusive-or of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ^(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() ^ right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() ^ right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() ^ right.AsVector128()).AsVector();
            }
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
            if (sizeof(Vector<T>) == 64)
            {
                return (value.AsVector512() << shiftCount).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (value.AsVector256() << shiftCount).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (value.AsVector128() << shiftCount).AsVector();
            }
        }

        /// <summary>Multiplies two vectors to compute their element-wise product.</summary>
        /// <param name="left">The vector to multiply with <paramref name="right" />.</param>
        /// <param name="right">The vector to multiply with <paramref name="left" />.</param>
        /// <returns>The element-wise product of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() * right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() * right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() * right.AsVector128()).AsVector();
            }
        }

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="value">The vector to multiply with <paramref name="factor" />.</param>
        /// <param name="factor">The scalar to multiply with <paramref name="value" />.</param>
        /// <returns>The product of <paramref name="value" /> and <paramref name="factor" />.</returns>
        [Intrinsic]
        public static Vector<T> operator *(Vector<T> value, T factor) => value * new Vector<T>(factor);

        /// <summary>Multiplies a vector by a scalar to compute their product.</summary>
        /// <param name="factor">The scalar to multiply with <paramref name="value" />.</param>
        /// <param name="value">The vector to multiply with <paramref name="factor" />.</param>
        /// <returns>The product of <paramref name="factor" /> and <paramref name="value" />.</returns>
        [Intrinsic]
        public static Vector<T> operator *(T factor, Vector<T> value) => value * new Vector<T>(factor);

        /// <summary>Computes the ones-complement of a vector.</summary>
        /// <param name="value">The vector whose ones-complement is to be computed.</param>
        /// <returns>A vector whose elements are the ones-complement of the corresponding elements in <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ~(Vector<T> value)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (~(value.AsVector512())).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (~(value.AsVector256())).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (~(value.AsVector128())).AsVector();
            }
        }

        /// <summary>Shifts (signed) each element of a vector right by the specified amount.</summary>
        /// <param name="value">The vector whose elements are to be shifted.</param>
        /// <param name="shiftCount">The number of bits by which to shift each element.</param>
        /// <returns>A vector whose elements where shifted right by <paramref name="shiftCount" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator >>(Vector<T> value, int shiftCount)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (value.AsVector512() >> shiftCount).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (value.AsVector256() >> shiftCount).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (value.AsVector128() >> shiftCount).AsVector();
            }
        }

        /// <summary>Subtracts two vectors to compute their difference.</summary>
        /// <param name="left">The vector from which <paramref name="right" /> will be subtracted.</param>
        /// <param name="right">The vector to subtract from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="left" /> and <paramref name="right" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return (left.AsVector512() - right.AsVector512()).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (left.AsVector256() - right.AsVector256()).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (left.AsVector128() - right.AsVector128()).AsVector();
            }
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
            if (sizeof(Vector<T>) == 64)
            {
                return (value.AsVector512() >>> shiftCount).AsVector();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return (value.AsVector256() >>> shiftCount).AsVector();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return (value.AsVector128() >>> shiftCount).AsVector();
            }
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given array.</summary>
        /// <param name="destination">The array to which the current instance is copied.</param>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(T[] destination)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this.AsVector512().CopyTo(destination);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this.AsVector256().CopyTo(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this.AsVector128().CopyTo(destination);
            }
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
            if (sizeof(Vector<T>) == 64)
            {
                this.AsVector512().CopyTo(destination, startIndex);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this.AsVector256().CopyTo(destination, startIndex);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this.AsVector128().CopyTo(destination, startIndex);
            }
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<byte> destination)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this.AsVector512().As<T, byte>().CopyTo(destination);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this.AsVector256().As<T, byte>().CopyTo(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this.AsVector128().As<T, byte>().CopyTo(destination);
            }
        }

        /// <summary>Copies a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(Span<T> destination)
        {
            if (sizeof(Vector<T>) == 64)
            {
                this.AsVector512().CopyTo(destination);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                this.AsVector256().CopyTo(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                this.AsVector128().CopyTo(destination);
            }
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
            if (sizeof(Vector<T>) == 64)
            {
                return this.AsVector512().Equals(other.AsVector512());
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return this.AsVector256().Equals(other.AsVector256());
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return this.AsVector128().Equals(other.AsVector128());
            }
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            if (sizeof(Vector<T>) == 64)
            {
                return this.AsVector512().GetHashCode();
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return this.AsVector256().GetHashCode();
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return this.AsVector128().GetHashCode();
            }
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
            if (sizeof(Vector<T>) == 64)
            {
                return this.AsVector512().ToString(format, formatProvider);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return this.AsVector256().ToString(format, formatProvider);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return this.AsVector128().ToString(format, formatProvider);
            }
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <returns><c>true</c> if the current instance was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <c>sizeof(<see cref="Vector{T}" />)</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<byte> destination)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return this.AsVector512().As<T, byte>().TryCopyTo(destination);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return this.AsVector256().As<T, byte>().TryCopyTo(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return this.AsVector128().As<T, byte>().TryCopyTo(destination);
            }
        }

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="destination">The span to which the current instance is copied.</param>
        /// <returns><c>true</c> if the current instance was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Vector{T}.Count" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryCopyTo(Span<T> destination)
        {
            if (sizeof(Vector<T>) == 64)
            {
                return this.AsVector512().TryCopyTo(destination);
            }
            else if (sizeof(Vector<T>) == 32)
            {
                return this.AsVector256().TryCopyTo(destination);
            }
            else
            {
                Debug.Assert(sizeof(Vector<T>) == 16);
                return this.AsVector128().TryCopyTo(destination);
            }
        }
    }
}

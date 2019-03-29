// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    public static class Vector64
    {
        internal const int Size = 8;

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{U}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <typeparam name="U">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector64<U> As<T, U>(this Vector64<T> vector)
            where T : struct
            where U : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            ThrowHelper.ThrowForUnsupportedVectorBaseType<U>();
            return Unsafe.As<Vector64<T>, Vector64<U>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<byte> AsByte<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, byte>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<double> AsDouble<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, double>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<short> AsInt16<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, short>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<int> AsInt32<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, int>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<long> AsInt64<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, long>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<sbyte> AsSByte<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, sbyte>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector64<float> AsSingle<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, float>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<ushort> AsUInt16<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, ushort>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, uint>();
        }

        /// <summary>Reinterprets a <see cref="Vector64{T}" /> as a new <see cref="Vector64{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector64{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector)
            where T : struct
        {
            return vector.As<T, ulong>();
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<byte> Create(byte value)
        {
            var pResult = stackalloc byte[8]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<double> Create(double value)
        {
            return Unsafe.As<double, Vector64<double>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<short> Create(short value)
        {
            var pResult = stackalloc short[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<int> Create(int value)
        {
            var pResult = stackalloc int[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<long> Create(long value)
        {
            return Unsafe.As<long, Vector64<long>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> Create(sbyte value)
        {
            var pResult = stackalloc sbyte[8]
            {
                value,
                value,
                value,
                value,
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        public static unsafe Vector64<float> Create(float value)
        {
            var pResult = stackalloc float[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> Create(ushort value)
        {
            var pResult = stackalloc ushort[4]
            {
                value,
                value,
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint value)
        {
            var pResult = stackalloc uint[2]
            {
                value,
                value,
            };

            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ulong> Create(ulong value)
        {
            return Unsafe.As<ulong, Vector64<ulong>>(ref value);
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
        {
            var pResult = stackalloc byte[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };

            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<short> Create(short e0, short e1, short e2, short e3)
        {
            var pResult = stackalloc short[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<int> Create(int e0, int e1)
        {
            var pResult = stackalloc int[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
        {
            var pResult = stackalloc sbyte[8]
            {
                e0,
                e1,
                e2,
                e3,
                e4,
                e5,
                e6,
                e7,
            };

            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> with each element initialized to corresponding specified value.</returns>
        public static unsafe Vector64<float> Create(float e0, float e1)
        {
            var pResult = stackalloc float[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
        {
            var pResult = stackalloc ushort[4]
            {
                e0,
                e1,
                e2,
                e3,
            };

            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> Create(uint e0, uint e1)
        {
            var pResult = stackalloc uint[2]
            {
                e0,
                e1,
            };

            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<byte> CreateScalar(byte value)
        {
            var result = Vector64<byte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<byte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<short> CreateScalar(short value)
        {
            var result = Vector64<short>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<short>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<int> CreateScalar(int value)
        {
            var result = Vector64<int>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<int>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> CreateScalar(sbyte value)
        {
            var result = Vector64<sbyte>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<sbyte>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector64<float> CreateScalar(float value)
        {
            var result = Vector64<float>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<float>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> CreateScalar(ushort value)
        {
            var result = Vector64<ushort>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<ushort>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements initialized to zero.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> CreateScalar(uint value)
        {
            var result = Vector64<uint>.Zero;
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<uint>, byte>(ref result), value);
            return result;
        }

        /// <summary>Creates a new <see cref="Vector64{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Byte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc short[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Int32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc int[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{SByte}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc sbyte[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{Single}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        public static unsafe Vector64<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc float[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt16}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ushort[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector64{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector64{UInt32}" /> instance with the first element initialized to <paramref name="value"/> and the remaining elements left uninitialized.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector64<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc uint[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector64<uint>>(pResult);
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public static T GetElement<T>(this Vector64<T> vector, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            ref T e0 = ref Unsafe.As<Vector64<T>, T>(ref vector);
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector64{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector64{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        public static Vector64<T> WithElement<T>(this Vector64<T> vector, int index, T value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector64<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector64<T> result = vector;
            ref T e0 = ref Unsafe.As<Vector64<T>, T>(ref result);
            Unsafe.Add(ref e0, index) = value;
            return result;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static T ToScalar<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            return Unsafe.As<Vector64<T>, T>(ref vector);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> ToVector128<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            Vector128<T> result = Vector128<T>.Zero;
            Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = vector;
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of the given vector and the upper 64-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the value of <paramref name="vector" /> and the upper 64-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static unsafe Vector128<T> ToVector128Unsafe<T>(this Vector64<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[Vector128.Size];
            Unsafe.AsRef<Vector64<T>>(pResult) = vector;
            return Unsafe.AsRef<Vector128<T>>(pResult);
        }
    }
}

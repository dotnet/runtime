// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

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

    // Many of the instance methods were moved to be extension methods as it results
    // in overall better codegen. This is because instance methods require the C# compiler
    // to generate extra locals as the `this` parameter has to be passed by reference.
    // Having them be extension methods means that the `this` parameter can be passed by
    // value instead, thus reducing the number of locals and helping prevent us from hitting
    // the internal inlining limits of the JIT.

    public static class Vector256
    {
        internal const int Size = 32;

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{U}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <typeparam name="U">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<U> As<T, U>(this Vector256<T> vector)
            where T : struct
            where U : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            ThrowHelper.ThrowForUnsupportedVectorBaseType<U>();
            return Unsafe.As<Vector256<T>, Vector256<U>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<byte> AsByte<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, byte>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<double> AsDouble<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, double>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<short> AsInt16<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, short>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<int> AsInt32<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, int>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<long> AsInt64<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, long>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector256<sbyte> AsSByte<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, sbyte>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<float> AsSingle<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, float>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector256<ushort> AsUInt16<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, ushort>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector256<uint> AsUInt32<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, uint>();
        }

        /// <summary>Reinterprets a <see cref="Vector256{T}" /> as a new <see cref="Vector256{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector256{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector256<ulong> AsUInt64<T>(this Vector256<T> vector)
            where T : struct
        {
            return vector.As<T, ulong>();
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<byte> Create(byte value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<byte> result = Vector128.CreateScalarUnsafe(value);           // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<byte> result = Vector128.Create(value);                       // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<byte> SoftwareFallback(byte value)
            {
                var pResult = stackalloc byte[32]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<double> Create(double value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<double> result = Vector128.CreateScalarUnsafe(value);         // < v, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<double> result = Vector128.Create(value);                     // < v, v, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<double> SoftwareFallback(double value)
            {
                var pResult = stackalloc double[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<double>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<short> Create(short value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<short> result = Vector128.CreateScalarUnsafe(value);          // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<short> result = Vector128.Create(value);                      // < v, v, v, v, v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<short> SoftwareFallback(short value)
            {
                var pResult = stackalloc short[16]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<int> Create(int value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<int> result = Vector128.CreateScalarUnsafe(value);            // < v, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<int> result = Vector128.Create(value);                        // < v, v, v, v, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<int> SoftwareFallback(int value)
            {
                var pResult = stackalloc int[8]
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

                return Unsafe.AsRef<Vector256<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<long> Create(long value)
        {
            if (Sse2.X64.IsSupported)
            {
                if (Avx2.IsSupported)
                {
                    Vector128<long> result = Vector128.CreateScalarUnsafe(value);           // < v, ?, ?, ? >
                    return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v >
                }
                else if (Avx.IsSupported)
                {
                    Vector128<long> result = Vector128.Create(value);                       // < v, v, ?, ? >
                    return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v >
                }
            }

            return SoftwareFallback(value);

            static Vector256<long> SoftwareFallback(long value)
            {
                var pResult = stackalloc long[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<long>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<sbyte> Create(sbyte value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<sbyte> result = Vector128.CreateScalarUnsafe(value);          // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<sbyte> result = Vector128.Create(value);                      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<sbyte> SoftwareFallback(sbyte value)
            {
                var pResult = stackalloc sbyte[32]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<float> Create(float value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<float> result = Vector128.CreateScalarUnsafe(value);          // < v, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<float> result = Vector128.Create(value);                      // < v, v, v, v, ?, ?, ?, ? >   
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<float> SoftwareFallback(float value)
            {
                var pResult = stackalloc float[8]
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

                return Unsafe.AsRef<Vector256<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ushort> Create(ushort value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<ushort> result = Vector128.CreateScalarUnsafe(value);         // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<ushort> result = Vector128.Create(value);                     // < v, v, v, v, v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<ushort> SoftwareFallback(ushort value)
            {
                var pResult = stackalloc ushort[16]
                {
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<uint> Create(uint value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<uint> result = Vector128.CreateScalarUnsafe(value);           // < v, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v, v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<uint> result = Vector128.Create(value);                       // < v, v, v, v, ?, ?, ?, ? >
                return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector256<uint> SoftwareFallback(uint value)
            {
                var pResult = stackalloc uint[8]
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

                return Unsafe.AsRef<Vector256<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ulong> Create(ulong value)
        {
            if (Sse2.X64.IsSupported)
            {
                if (Avx2.IsSupported)
                {
                    Vector128<ulong> result = Vector128.CreateScalarUnsafe(value);          // < v, ?, ?, ? >
                    return Avx2.BroadcastScalarToVector256(result);                         // < v, v, v, v >
                }
                else if (Avx.IsSupported)
                {
                    Vector128<ulong> result = Vector128.Create(value);                      // < v, v, ?, ? >
                    return Avx.InsertVector128(result.ToVector256Unsafe(), result, 1);      // < v, v, v, v >
                }
            }

            return SoftwareFallback(value);

            static Vector256<ulong> SoftwareFallback(ulong value)
            {
                var pResult = stackalloc ulong[4]
            {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector256<ulong>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
        {
            if (Avx.IsSupported)
            {
                Vector128<byte> lo128 = Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
                Vector128<byte> hi128 = Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);

            static Vector256<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
            {
                var pResult = stackalloc byte[32]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                    e4,
                    e5,
                    e6,
                    e7,
                    e8,
                    e9,
                    e10,
                    e11,
                    e12,
                    e13,
                    e14,
                    e15,
                    e16,
                    e17,
                    e18,
                    e19,
                    e20,
                    e21,
                    e22,
                    e23,
                    e24,
                    e25,
                    e26,
                    e27,
                    e28,
                    e29,
                    e30,
                    e31,
                };

                return Unsafe.AsRef<Vector256<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<double> Create(double e0, double e1, double e2, double e3)
        {
            if (Avx.IsSupported)
            {
                Vector128<double> lo128 = Vector128.Create(e0, e1);
                Vector128<double> hi128 = Vector128.Create(e2, e3);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector256<double> SoftwareFallback(double e0, double e1, double e2, double e3)
            {
                var pResult = stackalloc double[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector256<double>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
        {
            if (Avx.IsSupported)
            {
                Vector128<short> lo128 = Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7);
                Vector128<short> hi128 = Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);

            static Vector256<short> SoftwareFallback(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
            {
                var pResult = stackalloc short[16]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                    e4,
                    e5,
                    e6,
                    e7,
                    e8,
                    e9,
                    e10,
                    e11,
                    e12,
                    e13,
                    e14,
                    e15,
                };

                return Unsafe.AsRef<Vector256<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
        {
            if (Avx.IsSupported)
            {
                Vector128<int> lo128 = Vector128.Create(e0, e1, e2, e3);
                Vector128<int> hi128 = Vector128.Create(e4, e5, e6, e7);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector256<int> SoftwareFallback(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
            {
                var pResult = stackalloc int[8]
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

                return Unsafe.AsRef<Vector256<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<long> Create(long e0, long e1, long e2, long e3)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                Vector128<long> lo128 = Vector128.Create(e0, e1);
                Vector128<long> hi128 = Vector128.Create(e2, e3);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector256<long> SoftwareFallback(long e0, long e1, long e2, long e3)
            {
                var pResult = stackalloc long[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector256<long>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <param name="e16">The value that element 16 will be initialized to.</param>
        /// <param name="e17">The value that element 17 will be initialized to.</param>
        /// <param name="e18">The value that element 18 will be initialized to.</param>
        /// <param name="e19">The value that element 19 will be initialized to.</param>
        /// <param name="e20">The value that element 20 will be initialized to.</param>
        /// <param name="e21">The value that element 21 will be initialized to.</param>
        /// <param name="e22">The value that element 22 will be initialized to.</param>
        /// <param name="e23">The value that element 23 will be initialized to.</param>
        /// <param name="e24">The value that element 24 will be initialized to.</param>
        /// <param name="e25">The value that element 25 will be initialized to.</param>
        /// <param name="e26">The value that element 26 will be initialized to.</param>
        /// <param name="e27">The value that element 27 will be initialized to.</param>
        /// <param name="e28">The value that element 28 will be initialized to.</param>
        /// <param name="e29">The value that element 29 will be initialized to.</param>
        /// <param name="e30">The value that element 30 will be initialized to.</param>
        /// <param name="e31">The value that element 31 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
        {
            if (Avx.IsSupported)
            {
                Vector128<sbyte> lo128 = Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
                Vector128<sbyte> hi128 = Vector128.Create(e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);

            static Vector256<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
            {
                var pResult = stackalloc sbyte[32]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                    e4,
                    e5,
                    e6,
                    e7,
                    e8,
                    e9,
                    e10,
                    e11,
                    e12,
                    e13,
                    e14,
                    e15,
                    e16,
                    e17,
                    e18,
                    e19,
                    e20,
                    e21,
                    e22,
                    e23,
                    e24,
                    e25,
                    e26,
                    e27,
                    e28,
                    e29,
                    e30,
                    e31,
                };

                return Unsafe.AsRef<Vector256<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
        {
            if (Avx.IsSupported)
            {
                Vector128<float> lo128 = Vector128.Create(e0, e1, e2, e3);
                Vector128<float> hi128 = Vector128.Create(e4, e5, e6, e7);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector256<float> SoftwareFallback(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
            {
                var pResult = stackalloc float[8]
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

                return Unsafe.AsRef<Vector256<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <param name="e8">The value that element 8 will be initialized to.</param>
        /// <param name="e9">The value that element 9 will be initialized to.</param>
        /// <param name="e10">The value that element 10 will be initialized to.</param>
        /// <param name="e11">The value that element 11 will be initialized to.</param>
        /// <param name="e12">The value that element 12 will be initialized to.</param>
        /// <param name="e13">The value that element 13 will be initialized to.</param>
        /// <param name="e14">The value that element 14 will be initialized to.</param>
        /// <param name="e15">The value that element 15 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
        {
            if (Avx.IsSupported)
            {
                Vector128<ushort> lo128 = Vector128.Create(e0, e1, e2, e3, e4, e5, e6, e7);
                Vector128<ushort> hi128 = Vector128.Create(e8, e9, e10, e11, e12, e13, e14, e15);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);

            static Vector256<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
            {
                var pResult = stackalloc ushort[16]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                    e4,
                    e5,
                    e6,
                    e7,
                    e8,
                    e9,
                    e10,
                    e11,
                    e12,
                    e13,
                    e14,
                    e15,
                };

                return Unsafe.AsRef<Vector256<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
        {
            if (Avx.IsSupported)
            {
                Vector128<uint> lo128 = Vector128.Create(e0, e1, e2, e3);
                Vector128<uint> hi128 = Vector128.Create(e4, e5, e6, e7);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector256<uint> SoftwareFallback(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
            {
                var pResult = stackalloc uint[8]
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

                return Unsafe.AsRef<Vector256<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                Vector128<ulong> lo128 = Vector128.Create(e0, e1);
                Vector128<ulong> hi128 = Vector128.Create(e2, e3);
                return Create(lo128, hi128);
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector256<ulong> SoftwareFallback(ulong e0, ulong e1, ulong e2, ulong e3)
            {
                var pResult = stackalloc ulong[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector256<ulong>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance from two <see cref="Vector128{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<byte> Create(Vector128<byte> lower, Vector128<byte> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<byte> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<byte> SoftwareFallback(Vector128<byte> lower, Vector128<byte> upper)
            {
                Vector256<byte> result256 = Vector256<byte>.Zero;

                ref Vector128<byte> result128 = ref Unsafe.As<Vector256<byte>, Vector128<byte>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance from two <see cref="Vector128{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<double> Create(Vector128<double> lower, Vector128<double> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<double> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<double> SoftwareFallback(Vector128<double> lower, Vector128<double> upper)
            {
                Vector256<double> result256 = Vector256<double>.Zero;

                ref Vector128<double> result128 = ref Unsafe.As<Vector256<double>, Vector128<double>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance from two <see cref="Vector128{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<short> Create(Vector128<short> lower, Vector128<short> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<short> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<short> SoftwareFallback(Vector128<short> lower, Vector128<short> upper)
            {
                Vector256<short> result256 = Vector256<short>.Zero;

                ref Vector128<short> result128 = ref Unsafe.As<Vector256<short>, Vector128<short>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance from two <see cref="Vector128{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<int> Create(Vector128<int> lower, Vector128<int> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<int> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<int> SoftwareFallback(Vector128<int> lower, Vector128<int> upper)
            {
                Vector256<int> result256 = Vector256<int>.Zero;

                ref Vector128<int> result128 = ref Unsafe.As<Vector256<int>, Vector128<int>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance from two <see cref="Vector128{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<long> Create(Vector128<long> lower, Vector128<long> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<long> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<long> SoftwareFallback(Vector128<long> lower, Vector128<long> upper)
            {
                Vector256<long> result256 = Vector256<long>.Zero;

                ref Vector128<long> result128 = ref Unsafe.As<Vector256<long>, Vector128<long>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance from two <see cref="Vector128{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<sbyte> Create(Vector128<sbyte> lower, Vector128<sbyte> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<sbyte> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<sbyte> SoftwareFallback(Vector128<sbyte> lower, Vector128<sbyte> upper)
            {
                Vector256<sbyte> result256 = Vector256<sbyte>.Zero;

                ref Vector128<sbyte> result128 = ref Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance from two <see cref="Vector128{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<float> Create(Vector128<float> lower, Vector128<float> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<float> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<float> SoftwareFallback(Vector128<float> lower, Vector128<float> upper)
            {
                Vector256<float> result256 = Vector256<float>.Zero;

                ref Vector128<float> result128 = ref Unsafe.As<Vector256<float>, Vector128<float>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance from two <see cref="Vector128{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ushort> Create(Vector128<ushort> lower, Vector128<ushort> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<ushort> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<ushort> SoftwareFallback(Vector128<ushort> lower, Vector128<ushort> upper)
            {
                Vector256<ushort> result256 = Vector256<ushort>.Zero;

                ref Vector128<ushort> result128 = ref Unsafe.As<Vector256<ushort>, Vector128<ushort>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance from two <see cref="Vector128{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<uint> Create(Vector128<uint> lower, Vector128<uint> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<uint> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<uint> SoftwareFallback(Vector128<uint> lower, Vector128<uint> upper)
            {
                Vector256<uint> result256 = Vector256<uint>.Zero;

                ref Vector128<uint> result128 = ref Unsafe.As<Vector256<uint>, Vector128<uint>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance from two <see cref="Vector128{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 128-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 128-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ulong> Create(Vector128<ulong> lower, Vector128<ulong> upper)
        {
            if (Avx.IsSupported)
            {
                Vector256<ulong> result = lower.ToVector256Unsafe();
                return result.WithUpper(upper);
            }

            return SoftwareFallback(lower, upper);

            static Vector256<ulong> SoftwareFallback(Vector128<ulong> lower, Vector128<ulong> upper)
            {
                Vector256<ulong> result256 = Vector256<ulong>.Zero;

                ref Vector128<ulong> result128 = ref Unsafe.As<Vector256<ulong>, Vector128<ulong>>(ref result256);
                result128 = lower;
                Unsafe.Add(ref result128, 1) = upper;

                return result256;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<byte> CreateScalar(byte value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<byte> SoftwareFallback(byte value)
            {
                var result = Vector256<byte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<byte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<double> CreateScalar(double value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<double> SoftwareFallback(double value)
            {
                var result = Vector256<double>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<double>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<short> CreateScalar(short value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<short> SoftwareFallback(short value)
            {
                var result = Vector256<short>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<short>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<int> CreateScalar(int value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<int> SoftwareFallback(int value)
            {
                var result = Vector256<int>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<int>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<long> CreateScalar(long value)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<long> SoftwareFallback(long value)
            {
                var result = Vector256<long>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<long>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<sbyte> CreateScalar(sbyte value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<sbyte> SoftwareFallback(sbyte value)
            {
                var result = Vector256<sbyte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<sbyte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector256<float> CreateScalar(float value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<float> SoftwareFallback(float value)
            {
                var result = Vector256<float>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<float>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ushort> CreateScalar(ushort value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<ushort> SoftwareFallback(ushort value)
            {
                var result = Vector256<ushort>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<ushort>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<uint> CreateScalar(uint value)
        {
            if (Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<uint> SoftwareFallback(uint value)
            {
                var result = Vector256<uint>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<uint>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector256<ulong> CreateScalar(ulong value)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                return Vector128.CreateScalar(value).ToVector256();
            }

            return SoftwareFallback(value);

            static Vector256<ulong> SoftwareFallback(ulong value)
            {
                var result = Vector256<ulong>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<ulong>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector256{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[32];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<double> CreateScalarUnsafe(double value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc double[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc short[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc int[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<long> CreateScalarUnsafe(long value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc long[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector256<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc sbyte[32];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector256<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc float[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector256<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ushort[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector256<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc uint[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector256{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector256{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector256<ulong> CreateScalarUnsafe(ulong value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ulong[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector256<ulong>>(pResult);
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static T GetElement<T>(this Vector256<T> vector, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector256<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            ref T e0 = ref Unsafe.As<Vector256<T>, T>(ref vector);
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector256{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Vector256<T> WithElement<T>(this Vector256<T> vector, int index, T value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector256<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector256<T> result = vector;
            ref T e0 = ref Unsafe.As<Vector256<T>, T>(ref result);
            Unsafe.Add(ref e0, index) = value;
            return result;
        }

        /// <summary>Gets the value of the lower 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 128-bits from.</param>
        /// <returns>The value of the lower 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<T> GetLower<T>(this Vector256<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            return Unsafe.As<Vector256<T>, Vector128<T>>(ref vector);
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the lower 128-bits set to the specified value and the upper 128-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 128-bits from.</param>
        /// <param name="value">The value of the lower 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the specified value and the upper 128-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> WithLower<T>(this Vector256<T> vector, Vector128<T> value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.InsertVector128(vector.AsByte(), value.AsByte(), 0).As<byte, T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.InsertVector128(vector.AsSingle(), value.AsSingle(), 0).As<float, T>();
            }

            return SoftwareFallback(vector, value);

            static Vector256<T> SoftwareFallback(Vector256<T> vector, Vector128<T> value)
            {
                Vector256<T> result = vector;
                Unsafe.As<Vector256<T>, Vector128<T>>(ref result) = value;
                return result;
            }
        }

        /// <summary>Gets the value of the upper 128-bits as a new <see cref="Vector128{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 128-bits from.</param>
        /// <returns>The value of the upper 128-bits as a new <see cref="Vector128{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<T> GetUpper<T>(this Vector256<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.ExtractVector128(vector.AsByte(), 1).As<byte, T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.ExtractVector128(vector.AsSingle(), 1).As<float, T>();
            }

            return SoftwareFallback(vector);

            static Vector128<T> SoftwareFallback(Vector256<T> vector)
            {
                ref Vector128<T> lower = ref Unsafe.As<Vector256<T>, Vector128<T>>(ref vector);
                return Unsafe.Add(ref lower, 1);
            }
        }

        /// <summary>Creates a new <see cref="Vector256{T}" /> with the upper 128-bits set to the specified value and the upper 128-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 128-bits from.</param>
        /// <param name="value">The value of the upper 128-bits as a <see cref="Vector128{T}" />.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the upper 128-bits set to the specified value and the upper 128-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector256<T> WithUpper<T>(this Vector256<T> vector, Vector128<T> value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if (Avx2.IsSupported && ((typeof(T) != typeof(float)) && (typeof(T) != typeof(double))))
            {
                // All integral types generate the same instruction, so just pick one rather than handling each T separately
                return Avx2.InsertVector128(vector.AsByte(), value.AsByte(), 1).As<byte, T>();
            }

            if (Avx.IsSupported)
            {
                // All floating-point types generate the same instruction, so just pick one rather than handling each T separately
                // We also just fallback to this for integral types if AVX2 isn't supported, since that is still faster than software
                return Avx.InsertVector128(vector.AsSingle(), value.AsSingle(), 1).As<float, T>();
            }

            return SoftwareFallback(vector, value);

            static Vector256<T> SoftwareFallback(Vector256<T> vector, Vector128<T> value)
            {
                Vector256<T> result = vector;
                ref Vector128<T> lower = ref Unsafe.As<Vector256<T>, Vector128<T>>(ref result);
                Unsafe.Add(ref lower, 1) = value;
                return result;
            }
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static T ToScalar<T>(this Vector256<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            return Unsafe.As<Vector256<T>, T>(ref vector);
        }
    }
}

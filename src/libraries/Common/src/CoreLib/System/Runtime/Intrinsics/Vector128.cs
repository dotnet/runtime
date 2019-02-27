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

    public static class Vector128
    {
        internal const int Size = 16;

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{U}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <typeparam name="U">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) or the type of the target (<typeparamref name="U" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<U> As<T, U>(this Vector128<T> vector)
            where T : struct
            where U : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            ThrowHelper.ThrowForUnsupportedVectorBaseType<U>();
            return Unsafe.As<Vector128<T>, Vector128<U>>(ref vector);
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Byte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Byte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<byte> AsByte<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, byte>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Double}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Double}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<double> AsDouble<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, double>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<short> AsInt16<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, short>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<int> AsInt32<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, int>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Int64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Int64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<long> AsInt64<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, long>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{SByte}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{SByte}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, sbyte>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{Single}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{Single}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector128<float> AsSingle<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, float>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt16}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt16}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, ushort>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt32}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt32}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, uint>();
        }

        /// <summary>Reinterprets a <see cref="Vector128{T}" /> as a new <see cref="Vector128{UInt64}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector128{UInt64}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        [CLSCompliant(false)]
        public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector)
            where T : struct
        {
            return vector.As<T, ulong>();
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<byte> Create(byte value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<byte> result = CreateScalarUnsafe(value);                         // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Ssse3.IsSupported)
            {
                Vector128<byte> result = CreateScalarUnsafe(value);                         // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Ssse3.Shuffle(result, Vector128<byte>.Zero);                         // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                // We first unpack as bytes to duplicate value into the lower 2 bytes, then we treat it as a ushort and unpack again to duplicate those
                // bits into the lower 2 words, we can finally treat it as a uint and shuffle the lower dword to duplicate value across the entire result

                Vector128<byte> result = CreateScalarUnsafe(value);                         // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result, result);                                    // < v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result.AsUInt16(), result.AsUInt16()).AsByte();     // < v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Sse2.Shuffle(result.AsUInt32(), 0x00).AsByte();                      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<byte> SoftwareFallback(byte value)
            {
                var pResult = stackalloc byte[16]
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

                return Unsafe.AsRef<Vector128<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> Create(double value)
        {
            if (Sse3.IsSupported)
            {
                Vector128<double> result = CreateScalarUnsafe(value);                       // < v, ? >
                return Sse3.MoveAndDuplicate(result);                                       // < v, v >
            }

            if (Sse2.IsSupported)
            {
                // Treating the value as a set of singles and emitting MoveLowToHigh is more efficient than dealing with the elements directly as double
                // However, we still need to check if Sse2 is supported since CreateScalarUnsafe needs it to for movsd, when value is not already in register

                Vector128<double> result = CreateScalarUnsafe(value);                       // < v, ? >
                return Sse.MoveLowToHigh(result.AsSingle(), result.AsSingle()).AsDouble();  // < v, v >
            }

            return SoftwareFallback(value);

            static Vector128<double> SoftwareFallback(double value)
            {
                var pResult = stackalloc double[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<double>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<short> Create(short value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<short> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v, v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                // We first unpack as ushort to duplicate value into the lower 2 words, then we can treat it as a uint and shuffle the lower dword to
                // duplicate value across the entire result

                Vector128<short> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result, result);                                    // < v, v, ?, ?, ?, ?, ?, ? >
                return Sse2.Shuffle(result.AsInt32(), 0x00).AsInt16();                      // < v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<short> SoftwareFallback(short value)
            {
                var pResult = stackalloc short[8]
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

                return Unsafe.AsRef<Vector128<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<int> Create(int value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<int> result = CreateScalarUnsafe(value);                          // < v, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                Vector128<int> result = CreateScalarUnsafe(value);                          // < v, ?, ?, ? >
                return Sse2.Shuffle(result, 0x00);                                          // < v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<int> SoftwareFallback(int value)
            {
                var pResult = stackalloc int[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<long> Create(long value)
        {
            if (Sse2.X64.IsSupported)
            {
                if (Avx2.IsSupported)
                {
                    Vector128<long> result = CreateScalarUnsafe(value);                     // < v, ? >
                    return Avx2.BroadcastScalarToVector128(result);                         // < v, v >
                }
                else
                {
                    Vector128<long> result = CreateScalarUnsafe(value);                     // < v, ? >
                    return Sse2.UnpackLow(result, result);                                  // < v, v >
                }
            }

            return SoftwareFallback(value);

            static Vector128<long> SoftwareFallback(long value)
            {
                var pResult = stackalloc long[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<long>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<sbyte> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Ssse3.IsSupported)
            {
                Vector128<sbyte> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Ssse3.Shuffle(result, Vector128<sbyte>.Zero);                        // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                // We first unpack as bytes to duplicate value into the lower 2 bytes, then we treat it as a ushort and unpack again to duplicate those
                // bits into the lower 2 words, we can finally treat it as a uint and shuffle the lower dword to duplicate value across the entire result

                Vector128<sbyte> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result, result);                                    // < v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result.AsInt16(), result.AsInt16()).AsSByte();      // < v, v, v, v, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ? >
                return Sse2.Shuffle(result.AsInt32(), 0x00).AsSByte();                      // < v, v, v, v, v, v, v, v, v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<sbyte> SoftwareFallback(sbyte value)
            {
                var pResult = stackalloc sbyte[16]
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

                return Unsafe.AsRef<Vector128<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> Create(float value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<float> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v >
            }

            if (Avx.IsSupported)
            {
                Vector128<float> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ? >
                return Avx.Permute(result, 0x00);                                           // < v, v, v, v >
            }

            if (Sse.IsSupported)
            {
                Vector128<float> result = CreateScalarUnsafe(value);                        // < v, ?, ?, ? >
                return Sse.Shuffle(result, result, 0x00);                                   // < v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<float> SoftwareFallback(float value)
            {
                var pResult = stackalloc float[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<ushort> result = CreateScalarUnsafe(value);                       // < v, ?, ?, ?, ?, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v, v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                // We first unpack as ushort to duplicate value into the lower 2 words, then we can treat it as a uint and shuffle the lower dword to
                // duplicate value across the entire result

                Vector128<ushort> result = CreateScalarUnsafe(value);                       // < v, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.UnpackLow(result, result);                                    // < v, v, ?, ?, ?, ?, ?, ? >
                return Sse2.Shuffle(result.AsUInt32(), 0x00).AsUInt16();                    // < v, v, v, v, v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<ushort> SoftwareFallback(ushort value)
            {
                var pResult = stackalloc ushort[8]
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

                return Unsafe.AsRef<Vector128<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint value)
        {
            if (Avx2.IsSupported)
            {
                Vector128<uint> result = CreateScalarUnsafe(value);                         // < v, ?, ?, ? >
                return Avx2.BroadcastScalarToVector128(result);                             // < v, v, v, v >
            }

            if (Sse2.IsSupported)
            {
                Vector128<uint> result = CreateScalarUnsafe(value);                         // < v, ?, ?, ? >
                return Sse2.Shuffle(result, 0x00);                                          // < v, v, v, v >
            }

            return SoftwareFallback(value);

            static Vector128<uint> SoftwareFallback(uint value)
            {
                var pResult = stackalloc uint[4]
                {
                    value,
                    value,
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong value)
        {
            if (Sse2.X64.IsSupported)
            {
                if (Avx2.IsSupported)
                {
                    Vector128<ulong> result = CreateScalarUnsafe(value);                    // < v, ? >
                    return Avx2.BroadcastScalarToVector128(result);                         // < v, v >
                }
                else
                {
                    Vector128<ulong> result = CreateScalarUnsafe(value);                    // < v, ? >
                    return Sse2.UnpackLow(result, result);                                  // < v, v >
                }
            }

            return SoftwareFallback(value);

            static Vector128<ulong> SoftwareFallback(ulong value)
            {
                var pResult = stackalloc ulong[2]
                {
                    value,
                    value,
                };

                return Unsafe.AsRef<Vector128<ulong>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector128{Byte}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
        {
            if (Sse41.IsSupported)
            {
                Vector128<byte> result = CreateScalarUnsafe(e0);                                    // <  0, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e1, 1);                                               // <  0,  1, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e2, 2);                                               // <  0,  1,  2, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e3, 3);                                               // <  0,  1,  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e4, 4);                                               // <  0,  1,  2,  3,  4, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e5, 5);                                               // <  0,  1,  2,  3,  4,  5, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e6, 6);                                               // <  0,  1,  2,  3,  4,  5,  6, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e7, 7);                                               // <  0,  1,  2,  3,  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e8, 8);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e9, 9);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e10, 10);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e11, 11);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e12, 12);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, ??, ??, ?? >
                result = Sse41.Insert(result, e13, 13);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, ??, ?? >
                result = Sse41.Insert(result, e14, 14);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, ?? >
                return Sse41.Insert(result, e15, 15);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 >
            }

            if (Sse2.IsSupported)
            {
                // We deal with the elements in order, unpacking the ordered pairs of bytes into vectors. We then treat those vectors as ushort and
                // unpack them again, then again treating those results as uint, and a final time treating them as ulong. This efficiently gets all
                // bytes ordered into the result.

                Vector128<ushort> lo16, hi16;
                Vector128<uint> lo32, hi32;
                Vector128<ulong> lo64, hi64;

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1)).AsUInt16();   // <  0,  1, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e2), CreateScalarUnsafe(e3)).AsUInt16();   // <  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                lo32 = Sse2.UnpackLow(lo16, hi16).AsUInt32();                                       // <  0,  1,  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e4), CreateScalarUnsafe(e5)).AsUInt16();   // <  4,  5, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e6), CreateScalarUnsafe(e7)).AsUInt16();   // <  6,  7, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi32 = Sse2.UnpackLow(lo16, hi16).AsUInt32();                                       // <  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo64 = Sse2.UnpackLow(lo32, hi32).AsUInt64();                                       // <  0,  1,  2,  3,  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e8), CreateScalarUnsafe(e9)).AsUInt16();   // <  8,  9, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e10), CreateScalarUnsafe(e11)).AsUInt16(); // < 10, 11, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                lo32 = Sse2.UnpackLow(lo16, hi16).AsUInt32();                                       // <  8,  9, 10, 11, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e12), CreateScalarUnsafe(e13)).AsUInt16(); // < 12, 13, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e14), CreateScalarUnsafe(e15)).AsUInt16(); // < 14, 15, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi32 = Sse2.UnpackLow(lo16, hi16).AsUInt32();                                       // < 12, 13, 14, 15, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                hi64 = Sse2.UnpackLow(lo32, hi32).AsUInt64();                                       // <  8,  9, 10, 11, 12, 13, 14, 15, ??, ??, ??, ??, ??, ??, ??, ?? >

                return Sse2.UnpackLow(lo64, hi64).AsByte();                                         // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 >
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);

            static Vector128<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
            {
                var pResult = stackalloc byte[16]
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

                return Unsafe.AsRef<Vector128<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> Create(double e0, double e1)
        {
            if (Sse2.IsSupported)
            {
                // Treating the value as a set of singles and emitting MoveLowToHigh is more efficient than dealing with the elements directly as double
                // However, we still need to check if Sse2 is supported since CreateScalarUnsafe needs it to for movsd, when value is not already in register

                return Sse.MoveLowToHigh(CreateScalarUnsafe(e0).AsSingle(), CreateScalarUnsafe(e1).AsSingle()).AsDouble();
            }

            return SoftwareFallback(e0, e1);

            static Vector128<double> SoftwareFallback(double e0, double e1)
            {
                var pResult = stackalloc double[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector128<double>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
        {
            if (Sse2.IsSupported)
            {
                Vector128<short> result = CreateScalarUnsafe(e0);                                   // < 0, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e1, 1);                                                // < 0, 1, ?, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e2, 2);                                                // < 0, 1, 2, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e3, 3);                                                // < 0, 1, 2, 3, ?, ?, ?, ? >
                result = Sse2.Insert(result, e4, 4);                                                // < 0, 1, 2, 3, 4, ?, ?, ? >
                result = Sse2.Insert(result, e5, 5);                                                // < 0, 1, 2, 3, 4, 5, ?, ? >
                result = Sse2.Insert(result, e6, 6);                                                // < 0, 1, 2, 3, 4, 5, 6, ? >
                return Sse2.Insert(result, e7, 7);                                                  // < 0, 1, 2, 3, 4, 5, 6, 7 >
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector128<short> SoftwareFallback(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
            {
                var pResult = stackalloc short[8]
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

                return Unsafe.AsRef<Vector128<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<int> Create(int e0, int e1, int e2, int e3)
        {
            if (Sse41.IsSupported)
            {
                Vector128<int> result = CreateScalarUnsafe(e0);                                     // < 0, ?, ?, ? >
                result = Sse41.Insert(result, e1, 1);                                               // < 0, 1, ?, ? >
                result = Sse41.Insert(result, e2, 2);                                               // < 0, 1, 2, ? >
                return Sse41.Insert(result, e3, 3);                                                 // < 0, 1, 2, 3 >
            }

            if (Sse2.IsSupported)
            {
                // We deal with the elements in order, unpacking the ordered pairs of int into vectors. We then treat those vectors as ulong and
                // unpack them again. This efficiently gets all ints ordered into the result.

                Vector128<long> lo64, hi64;
                lo64 = Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1)).AsInt64();    // < 0, 1, ?, ? >
                hi64 = Sse2.UnpackLow(CreateScalarUnsafe(e2), CreateScalarUnsafe(e3)).AsInt64();    // < 2, 3, ?, ? >
                return Sse2.UnpackLow(lo64, hi64).AsInt32();                                        // < 0, 1, 2, 3 >
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector128<int> SoftwareFallback(int e0, int e1, int e2, int e3)
            {
                var pResult = stackalloc int[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector128<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<long> Create(long e0, long e1)
        {
            if (Sse41.X64.IsSupported)
            {
                Vector128<long> result = CreateScalarUnsafe(e0);                                    // < 0, ? >
                return Sse41.X64.Insert(result, e1, 1);                                             // < 0, 1 >
            }

            if (Sse2.X64.IsSupported)
            {
                return Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1));              // < 0, 1 >
            }

            return SoftwareFallback(e0, e1);

            static Vector128<long> SoftwareFallback(long e0, long e1)
            {
                var pResult = stackalloc long[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector128<long>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with each element initialized to the corresponding specified value.</summary>
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
        /// <returns>A new <see cref="Vector128{SByte}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
        {
            if (Sse41.IsSupported)
            {
                Vector128<sbyte> result = CreateScalarUnsafe(e0);                                   // <  0, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e1, 1);                                               // <  0,  1, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e2, 2);                                               // <  0,  1,  2, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e3, 3);                                               // <  0,  1,  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e4, 4);                                               // <  0,  1,  2,  3,  4, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e5, 5);                                               // <  0,  1,  2,  3,  4,  5, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e6, 6);                                               // <  0,  1,  2,  3,  4,  5,  6, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e7, 7);                                               // <  0,  1,  2,  3,  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e8, 8);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8, ??, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e9, 9);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, ??, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e10, 10);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, ??, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e11, 11);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, ??, ??, ??, ?? >
                result = Sse41.Insert(result, e12, 12);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, ??, ??, ?? >
                result = Sse41.Insert(result, e13, 13);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, ??, ?? >
                result = Sse41.Insert(result, e14, 14);                                             // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, ?? >
                return Sse41.Insert(result, e15, 15);                                               // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 >
            }

            if (Sse2.IsSupported)
            {
                // We deal with the elements in order, unpacking the ordered pairs of bytes into vectors. We then treat those vectors as ushort and
                // unpack them again, then again treating those results as uint, and a final time treating them as ulong. This efficiently gets all
                // bytes ordered into the result.

                Vector128<short> lo16, hi16;
                Vector128<int> lo32, hi32;
                Vector128<long> lo64, hi64;

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1)).AsInt16();    // <  0,  1, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e2), CreateScalarUnsafe(e3)).AsInt16();    // <  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                lo32 = Sse2.UnpackLow(lo16, hi16).AsInt32();                                        // <  0,  1,  2,  3, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e4), CreateScalarUnsafe(e5)).AsInt16();    // <  4,  5, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e6), CreateScalarUnsafe(e7)).AsInt16();    // <  6,  7, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi32 = Sse2.UnpackLow(lo16, hi16).AsInt32();                                        // <  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo64 = Sse2.UnpackLow(lo32, hi32).AsInt64();                                        // <  0,  1,  2,  3,  4,  5,  6,  7, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e8), CreateScalarUnsafe(e9)).AsInt16();    // <  8,  9, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e10), CreateScalarUnsafe(e11)).AsInt16();  // < 10, 11, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                lo32 = Sse2.UnpackLow(lo16, hi16).AsInt32();                                        // <  8,  9, 10, 11, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                lo16 = Sse2.UnpackLow(CreateScalarUnsafe(e12), CreateScalarUnsafe(e13)).AsInt16();  // < 12, 13, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi16 = Sse2.UnpackLow(CreateScalarUnsafe(e14), CreateScalarUnsafe(e15)).AsInt16();  // < 14, 15, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >
                hi32 = Sse2.UnpackLow(lo16, hi16).AsInt32();                                        // < 12, 13, 14, 15, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ??, ?? >

                hi64 = Sse2.UnpackLow(lo32, hi32).AsInt64();                                        // <  8,  9, 10, 11, 12, 13, 14, 15, ??, ??, ??, ??, ??, ??, ??, ?? >

                return Sse2.UnpackLow(lo64, hi64).AsSByte();                                        // <  0,  1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12, 13, 14, 15 >
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);

            static Vector128<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
            {
                var pResult = stackalloc sbyte[16]
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

                return Unsafe.AsRef<Vector128<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> Create(float e0, float e1, float e2, float e3)
        {
            if (Sse41.IsSupported)
            {
                Vector128<float> result = CreateScalarUnsafe(e0);                                   // < 0, ?, ?, ? >
                result = Sse41.Insert(result, CreateScalarUnsafe(e1), 0x10);                        // < 0, 1, ?, ? >
                result = Sse41.Insert(result, CreateScalarUnsafe(e2), 0x20);                        // < 0, 1, 2, ? >
                return Sse41.Insert(result, CreateScalarUnsafe(e3), 0x30);                          // < 0, 1, 2, 3 >
            }

            if (Sse.IsSupported)
            {
                Vector128<float> lo64, hi64;
                lo64 = Sse.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1));               // < 0, 1, ?, ? >
                hi64 = Sse.UnpackLow(CreateScalarUnsafe(e2), CreateScalarUnsafe(e3));               // < 2, 3, ?, ? >
                return Sse.MoveLowToHigh(lo64, hi64);                                               // < 0, 1, 2, 3 >
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector128<float> SoftwareFallback(float e0, float e1, float e2, float e3)
            {
                var pResult = stackalloc float[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector128<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <param name="e4">The value that element 4 will be initialized to.</param>
        /// <param name="e5">The value that element 5 will be initialized to.</param>
        /// <param name="e6">The value that element 6 will be initialized to.</param>
        /// <param name="e7">The value that element 7 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
        {
            if (Sse2.IsSupported)
            {
                Vector128<ushort> result = CreateScalarUnsafe(e0);                                  // < 0, ?, ?, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e1, 1);                                                // < 0, 1, ?, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e2, 2);                                                // < 0, 1, 2, ?, ?, ?, ?, ? >
                result = Sse2.Insert(result, e3, 3);                                                // < 0, 1, 2, 3, ?, ?, ?, ? >
                result = Sse2.Insert(result, e4, 4);                                                // < 0, 1, 2, 3, 4, ?, ?, ? >
                result = Sse2.Insert(result, e5, 5);                                                // < 0, 1, 2, 3, 4, 5, ?, ? >
                result = Sse2.Insert(result, e6, 6);                                                // < 0, 1, 2, 3, 4, 5, 6, ? >
                return Sse2.Insert(result, e7, 7);                                                  // < 0, 1, 2, 3, 4, 5, 6, 7 >
            }

            return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);

            static Vector128<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
            {
                var pResult = stackalloc ushort[8]
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

                return Unsafe.AsRef<Vector128<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <param name="e2">The value that element 2 will be initialized to.</param>
        /// <param name="e3">The value that element 3 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(uint e0, uint e1, uint e2, uint e3)
        {
            if (Sse41.IsSupported)
            {
                Vector128<uint> result = CreateScalarUnsafe(e0);                                    // < 0, ?, ?, ? >
                result = Sse41.Insert(result, e1, 1);                                               // < 0, 1, ?, ? >
                result = Sse41.Insert(result, e2, 2);                                               // < 0, 1, 2, ? >
                return Sse41.Insert(result, e3, 3);                                                 // < 0, 1, 2, 3 >
            }

            if (Sse2.IsSupported)
            {
                // We deal with the elements in order, unpacking the ordered pairs of int into vectors. We then treat those vectors as ulong and
                // unpack them again. This efficiently gets all ints ordered into the result.

                Vector128<ulong> lo64, hi64;
                lo64 = Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1)).AsUInt64();   // < 0, 1, ?, ? >
                hi64 = Sse2.UnpackLow(CreateScalarUnsafe(e2), CreateScalarUnsafe(e3)).AsUInt64();   // < 2, 3, ?, ? >
                return Sse2.UnpackLow(lo64, hi64).AsUInt32();                                       // < 0, 1, 2, 3 >
            }

            return SoftwareFallback(e0, e1, e2, e3);

            static Vector128<uint> SoftwareFallback(uint e0, uint e1, uint e2, uint e3)
            {
                var pResult = stackalloc uint[4]
                {
                    e0,
                    e1,
                    e2,
                    e3,
                };

                return Unsafe.AsRef<Vector128<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with each element initialized to the corresponding specified value.</summary>
        /// <param name="e0">The value that element 0 will be initialized to.</param>
        /// <param name="e1">The value that element 1 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> with each element initialized to corresponding specified value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(ulong e0, ulong e1)
        {
            if (Sse41.X64.IsSupported)
            {
                Vector128<ulong> result = CreateScalarUnsafe(e0);                                   // < 0, ? >
                return Sse41.X64.Insert(result, e1, 1);                                             // < 0, 1 >
            }

            if (Sse2.X64.IsSupported)
            {
                return Sse2.UnpackLow(CreateScalarUnsafe(e0), CreateScalarUnsafe(e1));              // < 0, 1 >
            }

            return SoftwareFallback(e0, e1);

            static Vector128<ulong> SoftwareFallback(ulong e0, ulong e1)
            {
                var pResult = stackalloc ulong[2]
                {
                    e0,
                    e1,
                };

                return Unsafe.AsRef<Vector128<ulong>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance from two <see cref="Vector64{Byte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
        {
            Vector128<byte> result128 = Vector128<byte>.Zero;

            ref Vector64<byte> result64 = ref Unsafe.As<Vector128<byte>, Vector64<byte>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance from two <see cref="Vector64{Double}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<double> Create(Vector64<double> lower, Vector64<double> upper)
        {
            Vector128<double> result128 = Vector128<double>.Zero;

            ref Vector64<double> result64 = ref Unsafe.As<Vector128<double>, Vector64<double>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance from two <see cref="Vector64{Int16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<short> Create(Vector64<short> lower, Vector64<short> upper)
        {
            Vector128<short> result128 = Vector128<short>.Zero;

            ref Vector64<short> result64 = ref Unsafe.As<Vector128<short>, Vector64<short>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance from two <see cref="Vector64{Int32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<int> Create(Vector64<int> lower, Vector64<int> upper)
        {
            Vector128<int> result128 = Vector128<int>.Zero;

            ref Vector64<int> result64 = ref Unsafe.As<Vector128<int>, Vector64<int>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance from two <see cref="Vector64{Int64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<long> Create(Vector64<long> lower, Vector64<long> upper)
        {
            Vector128<long> result128 = Vector128<long>.Zero;

            ref Vector64<long> result64 = ref Unsafe.As<Vector128<long>, Vector64<long>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance from two <see cref="Vector64{SByte}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> Create(Vector64<sbyte> lower, Vector64<sbyte> upper)
        {
            Vector128<sbyte> result128 = Vector128<sbyte>.Zero;

            ref Vector64<sbyte> result64 = ref Unsafe.As<Vector128<sbyte>, Vector64<sbyte>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance from two <see cref="Vector64{Single}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        public static unsafe Vector128<float> Create(Vector64<float> lower, Vector64<float> upper)
        {
            Vector128<float> result128 = Vector128<float>.Zero;

            ref Vector64<float> result64 = ref Unsafe.As<Vector128<float>, Vector64<float>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance from two <see cref="Vector64{UInt16}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> Create(Vector64<ushort> lower, Vector64<ushort> upper)
        {
            Vector128<ushort> result128 = Vector128<ushort>.Zero;

            ref Vector64<ushort> result64 = ref Unsafe.As<Vector128<ushort>, Vector64<ushort>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance from two <see cref="Vector64{UInt32}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> Create(Vector64<uint> lower, Vector64<uint> upper)
        {
            Vector128<uint> result128 = Vector128<uint>.Zero;

            ref Vector64<uint> result64 = ref Unsafe.As<Vector128<uint>, Vector64<uint>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance from two <see cref="Vector64{UInt64}" /> instances.</summary>
        /// <param name="lower">The value that the lower 64-bits will be initialized to.</param>
        /// <param name="upper">The value that the upper 64-bits will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> initialized from <paramref name="lower" /> and <paramref name="upper" />.</returns>
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> Create(Vector64<ulong> lower, Vector64<ulong> upper)
        {
            Vector128<ulong> result128 = Vector128<ulong>.Zero;

            ref Vector64<ulong> result64 = ref Unsafe.As<Vector128<ulong>, Vector64<ulong>>(ref result128);
            result64 = lower;
            Unsafe.Add(ref result64, 1) = upper;

            return result128;
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<byte> CreateScalar(byte value)
        {
            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we call
                // the UInt32 overload to ensure zero extension. We can then just treat the result as byte and return.
                return Sse2.ConvertScalarToVector128UInt32(value).AsByte();
            }

            return SoftwareFallback(value);

            static Vector128<byte> SoftwareFallback(byte value)
            {
                var result = Vector128<byte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<byte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<double> CreateScalar(double value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.MoveScalar(Vector128<double>.Zero, CreateScalarUnsafe(value));
            }

            return SoftwareFallback(value);

            static Vector128<double> SoftwareFallback(double value)
            {
                var result = Vector128<double>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<short> CreateScalar(short value)
        {
            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we cast
                // to ushort and call the UInt32 overload to ensure zero extension. We can then just treat the result as short and return.
                return Sse2.ConvertScalarToVector128UInt32((ushort)(value)).AsInt16();
            }

            return SoftwareFallback(value);

            static Vector128<short> SoftwareFallback(short value)
            {
                var result = Vector128<short>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<short>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<int> CreateScalar(int value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ConvertScalarToVector128Int32(value);
            }

            return SoftwareFallback(value);

            static Vector128<int> SoftwareFallback(int value)
            {
                var result = Vector128<int>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        public static unsafe Vector128<long> CreateScalar(long value)
        {
            if (Sse2.X64.IsSupported)
            {
                return Sse2.X64.ConvertScalarToVector128Int64(value);
            }

            return SoftwareFallback(value);

            static Vector128<long> SoftwareFallback(long value)
            {
                var result = Vector128<long>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<long>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalar(sbyte value)
        {
            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we cast
                // to byte and call the UInt32 overload to ensure zero extension. We can then just treat the result as sbyte and return.
                return Sse2.ConvertScalarToVector128UInt32((byte)(value)).AsSByte();
            }

            return SoftwareFallback(value);

            static Vector128<sbyte> SoftwareFallback(sbyte value)
            {
                var result = Vector128<sbyte>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<sbyte>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector128<float> CreateScalar(float value)
        {
            if (Sse.IsSupported)
            {
                return Sse.MoveScalar(Vector128<float>.Zero, CreateScalarUnsafe(value));
            }

            return SoftwareFallback(value);

            static Vector128<float> SoftwareFallback(float value)
            {
                var result = Vector128<float>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalar(ushort value)
        {
            if (Sse2.IsSupported)
            {
                // ConvertScalarToVector128 only deals with 32/64-bit inputs and we need to ensure all upper-bits are zeroed, so we call
                // the UInt32 overload to ensure zero extension. We can then just treat the result as ushort and return.
                return Sse2.ConvertScalarToVector128UInt32(value).AsUInt16();
            }

            return SoftwareFallback(value);

            static Vector128<ushort> SoftwareFallback(ushort value)
            {
                var result = Vector128<ushort>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalar(uint value)
        {
            if (Sse2.IsSupported)
            {
                return Sse2.ConvertScalarToVector128UInt32(value);
            }

            return SoftwareFallback(value);

            static Vector128<uint> SoftwareFallback(uint value)
            {
                var result = Vector128<uint>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalar(ulong value)
        {
            if (Sse2.X64.IsSupported)
            {
                return Sse2.X64.ConvertScalarToVector128UInt64(value);
            }

            return SoftwareFallback(value);

            static Vector128<ulong> SoftwareFallback(ulong value)
            {
                var result = Vector128<ulong>.Zero;
                Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ulong>, byte>(ref result), value);
                return result;
            }
        }

        /// <summary>Creates a new <see cref="Vector128{Byte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Byte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<byte> CreateScalarUnsafe(byte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<byte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Double}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Double}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<double> CreateScalarUnsafe(double value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc double[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<double>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<short> CreateScalarUnsafe(short value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc short[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<short>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<int> CreateScalarUnsafe(int value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc int[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<int>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Int64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Int64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<long> CreateScalarUnsafe(long value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc long[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<long>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{SByte}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{SByte}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<sbyte> CreateScalarUnsafe(sbyte value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc sbyte[16];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<sbyte>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{Single}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{Single}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        public static unsafe Vector128<float> CreateScalarUnsafe(float value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc float[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<float>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt16}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt16}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ushort> CreateScalarUnsafe(ushort value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ushort[8];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ushort>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt32}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt32}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<uint> CreateScalarUnsafe(uint value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc uint[4];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<uint>>(pResult);
        }

        /// <summary>Creates a new <see cref="Vector128{UInt64}" /> instance with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new <see cref="Vector128{UInt64}" /> instance with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector128<ulong> CreateScalarUnsafe(ulong value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc ulong[2];
            pResult[0] = value;
            return Unsafe.AsRef<Vector128<ulong>>(pResult);
        }

        /// <summary>Gets the element at the specified index.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static T GetElement<T>(this Vector128<T> vector, int index)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector128<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            ref T e0 = ref Unsafe.As<Vector128<T>, T>(ref vector);
            return Unsafe.Add(ref e0, index);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector128{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        [Intrinsic]
        public static Vector128<T> WithElement<T>(this Vector128<T> vector, int index, T value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if ((uint)(index) >= (uint)(Vector128<T>.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
            }

            Vector128<T> result = vector;
            ref T e0 = ref Unsafe.As<Vector128<T>, T>(ref result);
            Unsafe.Add(ref e0, index) = value;
            return result;
        }

        /// <summary>Gets the value of the lower 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 64-bits from.</param>
        /// <returns>The value of the lower 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> GetLower<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            return Unsafe.As<Vector128<T>, Vector64<T>>(ref vector);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the lower 64-bits set to the specified value and the upper 64-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 64-bits from.</param>
        /// <param name="value">The value of the lower 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the lower 64-bits set to the specified value and the upper 64-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> WithLower<T>(this Vector128<T> vector, Vector64<T> value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            Vector128<T> result = vector;
            Unsafe.As<Vector128<T>, Vector64<T>>(ref result) = value;
            return result;
        }

        /// <summary>Gets the value of the upper 64-bits as a new <see cref="Vector64{T}" />.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the upper 64-bits from.</param>
        /// <returns>The value of the upper 64-bits as a new <see cref="Vector64{T}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> GetUpper<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref vector);
            return Unsafe.Add(ref lower, 1);
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the upper 64-bits set to the specified value and the upper 64-bits set to the same value as that in the given vector.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the lower 64-bits from.</param>
        /// <param name="value">The value of the upper 64-bits as a <see cref="Vector64{T}" />.</param>
        /// <returns>A new <see cref="Vector128{T}" /> with the upper 64-bits set to the specified value and the upper 64-bits set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> WithUpper<T>(this Vector128<T> vector, Vector64<T> value)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            Vector128<T> result = vector;
            ref Vector64<T> lower = ref Unsafe.As<Vector128<T>, Vector64<T>>(ref result);
            Unsafe.Add(ref lower, 1) = value;
            return result;
        }

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static T ToScalar<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
            return Unsafe.As<Vector128<T>, T>(ref vector);
        }

        /// <summary>Converts the given vector to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the given vector and the upper 128-bits initialized to zero.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of <paramref name="vector" /> and the upper 128-bits initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static Vector256<T> ToVector256<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            Vector256<T> result = Vector256<T>.Zero;
            Unsafe.As<Vector256<T>, Vector128<T>>(ref result) = vector;
            return result;
        }

        /// <summary>Converts the given vector to a new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of the given vector and the upper 128-bits left uninitialized.</summary>
        /// <typeparam name="T">The type of the input vector.</typeparam>
        /// <param name="vector">The vector to extend.</param>
        /// <returns>A new <see cref="Vector256{T}" /> with the lower 128-bits set to the value of <paramref name="vector" /> and the upper 128-bits left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="T" />) is not supported.</exception>
        [Intrinsic]
        public static unsafe Vector256<T> ToVector256Unsafe<T>(this Vector128<T> vector)
            where T : struct
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            var pResult = stackalloc byte[Vector256.Size];
            Unsafe.AsRef<Vector128<T>>(pResult) = vector;
            return Unsafe.AsRef<Vector256<T>>(pResult);
        }
    }
}

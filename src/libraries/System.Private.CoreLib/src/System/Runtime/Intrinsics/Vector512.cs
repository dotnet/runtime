// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    public static class Vector512
    {
        internal const int Size = 64;

        /// <summary>Gets a value that indicates whether 512-bit vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if 512-bit vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        /// <remarks>512-bit vector operations are subject to hardware acceleration on systems that support Single Instruction, Multiple Data (SIMD) instructions for 512-bit vectors and the RyuJIT just-in-time compiler is used to compile managed code.</remarks>
        public static bool IsHardwareAccelerated
        {
            [Intrinsic]
            get => false;
        }

        /// <summary>Creates a new <see cref="Vector512{T}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <typeparam name="T">The type of the elements in the vector.</typeparam>
        /// <returns>A new <see cref="Vector512{T}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Vector512<T> Create<T>(T value)
            where T : struct
        {
            if (typeof(T) == typeof(byte))
            {
                return Create((byte)(object)value).As<byte, T>();
            }
            else if (typeof(T) == typeof(double))
            {
                return Create((double)(object)value).As<double, T>();
            }
            else if (typeof(T) == typeof(short))
            {
                return Create((short)(object)value).As<short, T>();
            }
            else if (typeof(T) == typeof(int))
            {
                return Create((int)(object)value).As<int, T>();
            }
            else if (typeof(T) == typeof(long))
            {
                return Create((long)(object)value).As<long, T>();
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return Create((sbyte)(object)value).As<sbyte, T>();
            }
            else if (typeof(T) == typeof(float))
            {
                return Create((float)(object)value).As<float, T>();
            }
            else if (typeof(T) == typeof(ushort))
            {
                return Create((ushort)(object)value).As<ushort, T>();
            }
            else if (typeof(T) == typeof(uint))
            {
                return Create((uint)(object)value).As<uint, T>();
            }
            else if (typeof(T) == typeof(ulong))
            {
                return Create((ulong)(object)value).As<ulong, T>();
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Byte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi8</remarks>
        /// <returns>A new <see cref="Vector512{Byte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<byte> Create(byte value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<byte> SoftwareFallback(byte value)
            {
                byte* pResult = stackalloc byte[64]
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

                return Unsafe.AsRef<Vector512<byte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Double}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512d _mm512_set1_pd</remarks>
        /// <returns>A new <see cref="Vector512{Double}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<double> Create(double value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<double> SoftwareFallback(double value)
            {
                double* pResult = stackalloc double[8]
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

                return Unsafe.AsRef<Vector512<double>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Int16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi16</remarks>
        /// <returns>A new <see cref="Vector512{Int16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<short> Create(short value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<short> SoftwareFallback(short value)
            {
                short* pResult = stackalloc short[32]
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

                return Unsafe.AsRef<Vector512<short>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Int32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi32</remarks>
        /// <returns>A new <see cref="Vector512{Int32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<int> Create(int value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<int> SoftwareFallback(int value)
            {
                int* pResult = stackalloc int[16]
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

                return Unsafe.AsRef<Vector512<int>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Int64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi64x</remarks>
        /// <returns>A new <see cref="Vector512{Int64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<long> Create(long value)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<long> SoftwareFallback(long value)
            {
                long* pResult = stackalloc long[8]
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

                return Unsafe.AsRef<Vector512<long>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{SByte}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi8</remarks>
        /// <returns>A new <see cref="Vector512{SByte}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector512<sbyte> Create(sbyte value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<sbyte> SoftwareFallback(sbyte value)
            {
                sbyte* pResult = stackalloc sbyte[64]
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

                return Unsafe.AsRef<Vector512<sbyte>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{Single}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512 _mm512_set1_ps</remarks>
        /// <returns>A new <see cref="Vector512{Single}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        public static unsafe Vector512<float> Create(float value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<float> SoftwareFallback(float value)
            {
                float* pResult = stackalloc float[16]
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

                return Unsafe.AsRef<Vector512<float>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{UInt16}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi16</remarks>
        /// <returns>A new <see cref="Vector512{UInt16}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector512<ushort> Create(ushort value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<ushort> SoftwareFallback(ushort value)
            {
                ushort* pResult = stackalloc ushort[32]
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

                return Unsafe.AsRef<Vector512<ushort>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{UInt32}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi32</remarks>
        /// <returns>A new <see cref="Vector512{UInt32}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector512<uint> Create(uint value)
        {
            if (Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<uint> SoftwareFallback(uint value)
            {
                uint* pResult = stackalloc uint[16]
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

                return Unsafe.AsRef<Vector512<uint>>(pResult);
            }
        }

        /// <summary>Creates a new <see cref="Vector512{UInt64}" /> instance with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <remarks>On x86, this method corresponds to __m512i _mm512_set1_epi64x</remarks>
        /// <returns>A new <see cref="Vector512{UInt64}" /> with all elements initialized to <paramref name="value" />.</returns>
        [Intrinsic]
        [CLSCompliant(false)]
        public static unsafe Vector512<ulong> Create(ulong value)
        {
            if (Sse2.X64.IsSupported && Avx.IsSupported)
            {
                return Create(value);
            }

            return SoftwareFallback(value);

            static Vector512<ulong> SoftwareFallback(ulong value)
            {
                ulong* pResult = stackalloc ulong[8]
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

                return Unsafe.AsRef<Vector512<ulong>>(pResult);
            }
        }

        /// <summary>Reinterprets a <see cref="Vector512{T}" /> as a new <see cref="Vector512{U}" />.</summary>
        /// <typeparam name="TFrom">The type of the input vector.</typeparam>
        /// <typeparam name="TTo">The type of the vector <paramref name="vector" /> should be reinterpreted as.</typeparam>
        /// <param name="vector">The vector to reinterpret.</param>
        /// <returns><paramref name="vector" /> reinterpreted as a new <see cref="Vector512{U}" />.</returns>
        /// <exception cref="NotSupportedException">The type of <paramref name="vector" /> (<typeparamref name="TFrom" />) or the type of the target (<typeparamref name="TTo" />) is not supported.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector512<TTo> As<TFrom, TTo>(this Vector512<TFrom> vector)
            where TFrom : struct
            where TTo : struct
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TFrom>();
            ThrowHelper.ThrowForUnsupportedIntrinsicsVector512BaseType<TTo>();

            return Unsafe.As<Vector512<TFrom>, Vector512<TTo>>(ref vector);
        }
    }
}

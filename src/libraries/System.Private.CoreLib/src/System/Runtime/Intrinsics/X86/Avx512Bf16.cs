// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512-BF16 hardware instructions via intrinsics.</summary>
    /// <remarks>
    /// BF16 operands are exposed as <see cref="Vector128{T}"/> / <see cref="Vector256{T}"/> / <see cref="Vector512{T}"/>
    /// of <see cref="ushort"/> (raw bf16 bit pattern). Strongly-typed <c>BFloat16</c> overloads are intended as a
    /// follow-up once the <c>System.Numerics.BFloat16</c> primitive is wired through the JIT/VM SIMD pipeline.
    /// </remarks>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512Bf16 : Avx512F
    {
        internal Avx512Bf16() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 AVX512-BF16 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        ///   <para>__m512 _mm512_dpbf16_ps (__m512 src, __m512bh a, __m512bh b)</para>
        ///   <para>  VDPBF16PS zmm1, zmm2, zmm3/m512</para>
        /// </summary>
        public static Vector512<float> MultiplyWideningAndAdd(Vector512<float> addend, Vector512<ushort> left, Vector512<ushort> right) => MultiplyWideningAndAdd(addend, left, right);

        /// <summary>
        ///   <para>__m512bh _mm512_cvtne2ps_pbh (__m512 a, __m512 b)</para>
        ///   <para>  VCVTNE2PS2BF16 zmm1, zmm2, zmm3/m512</para>
        ///   <para>Round-to-nearest-even conversion of two packed FP32 vectors into a single packed BF16 vector (returned as <see cref="Vector512{T}"/> of <see cref="ushort"/>).</para>
        /// </summary>
        public static Vector512<ushort> ConvertToBFloat16(Vector512<float> lower, Vector512<float> upper) => ConvertToBFloat16(lower, upper);

        /// <summary>
        ///   <para>__m256bh _mm512_cvtneps_pbh (__m512 a)</para>
        ///   <para>  VCVTNEPS2BF16 ymm1, zmm2/m512</para>
        ///   <para>Round-to-nearest-even conversion of a packed FP32 vector into a half-width packed BF16 vector.</para>
        /// </summary>
        public static Vector256<ushort> ConvertToBFloat16(Vector512<float> value) => ConvertToBFloat16(value);

        /// <summary>Provides access to the x86 AVX512-BF16+VL hardware instructions via intrinsics.</summary>
        [Intrinsic]
        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>__m128 _mm_dpbf16_ps (__m128 src, __m128bh a, __m128bh b) — VDPBF16PS xmm</summary>
            public static Vector128<float> MultiplyWideningAndAdd(Vector128<float> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningAndAdd(addend, left, right);

            /// <summary>__m256 _mm256_dpbf16_ps (__m256 src, __m256bh a, __m256bh b) — VDPBF16PS ymm</summary>
            public static Vector256<float> MultiplyWideningAndAdd(Vector256<float> addend, Vector256<ushort> left, Vector256<ushort> right) => MultiplyWideningAndAdd(addend, left, right);

            /// <summary>__m128bh _mm_cvtne2ps_pbh (__m128 a, __m128 b) — VCVTNE2PS2BF16 xmm</summary>
            public static Vector128<ushort> ConvertToBFloat16(Vector128<float> lower, Vector128<float> upper) => ConvertToBFloat16(lower, upper);

            /// <summary>__m256bh _mm256_cvtne2ps_pbh (__m256 a, __m256 b) — VCVTNE2PS2BF16 ymm</summary>
            public static Vector256<ushort> ConvertToBFloat16(Vector256<float> lower, Vector256<float> upper) => ConvertToBFloat16(lower, upper);

            /// <summary>__m128bh _mm_cvtneps_pbh (__m128 a) — VCVTNEPS2BF16 xmm</summary>
            public static Vector128<ushort> ConvertToBFloat16(Vector128<float> value) => ConvertToBFloat16(value);

            /// <summary>__m128bh _mm256_cvtneps_pbh (__m256 a) — VCVTNEPS2BF16 xmm from ymm</summary>
            public static Vector128<ushort> ConvertToBFloat16(Vector256<float> value) => ConvertToBFloat16(value);
        }
    }
}

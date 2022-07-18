// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSE hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Sse : X86Base
    {
        internal Sse() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __int64 _mm_cvtss_si64 (__m128 a)
            ///   CVTSS2SI r64, xmm/m32
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __m128 _mm_cvtsi64_ss (__m128 a, __int64 b)
            ///   CVTSI2SS xmm, reg/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvttss_si64 (__m128 a)
            ///   CVTTSS2SI r64, xmm/m32
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        }

        /// <summary>
        /// __m128 _mm_add_ps (__m128 a,  __m128 b)
        ///   ADDPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_add_ss (__m128 a,  __m128 b)
        ///   ADDSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_and_ps (__m128 a, __m128 b)
        ///   ANDPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_andnot_ps (__m128 a, __m128 b)
        ///   ANDNPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpeq_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(0)
        /// </summary>
        public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comieq_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomieq_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpeq_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(0)
        /// </summary>
        public static Vector128<float> CompareScalarEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpgt_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(1) with swapped operands
        /// </summary>
        public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comigt_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomigt_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpgt_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(1) with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpge_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(2) with swapped operands
        /// </summary>
        public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comige_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomige_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpge_ss (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m32, imm8(2) with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmplt_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(1)
        /// </summary>
        public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comilt_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomilt_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmplt_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(1)
        /// </summary>
        public static Vector128<float> CompareScalarLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmple_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(2)
        /// </summary>
        public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comile_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomile_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmple_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(2)
        /// </summary>
        public static Vector128<float> CompareScalarLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpneq_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(4)
        /// </summary>
        public static Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comineq_ss (__m128 a, __m128 b)
        ///   COMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarOrderedNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomineq_ss (__m128 a, __m128 b)
        ///   UCOMISS xmm, xmm/m32
        /// </summary>
        public static bool CompareScalarUnorderedNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpneq_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(4)
        /// </summary>
        public static Vector128<float> CompareScalarNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpngt_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(5) with swapped operands
        /// </summary>
        public static Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpngt_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(5) with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnge_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(6) with swapped operands
        /// </summary>
        public static Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnge_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(6) with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnlt_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(5)
        /// </summary>
        public static Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnlt_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(5)
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnle_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(6)
        /// </summary>
        public static Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpnle_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(6)
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpord_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(7)
        /// </summary>
        public static Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpord_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(7)
        /// </summary>
        public static Vector128<float> CompareScalarOrdered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpunord_ps (__m128 a,  __m128 b)
        ///   CMPPS xmm, xmm/m128, imm8(3)
        /// </summary>
        public static Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpunord_ss (__m128 a,  __m128 b)
        ///   CMPSS xmm, xmm/m32, imm8(3)
        /// </summary>
        public static Vector128<float> CompareScalarUnordered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvtss_si32 (__m128 a)
        ///   CVTSS2SI r32, xmm/m32
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtsi32_ss (__m128 a, int b)
        ///   CVTSI2SS xmm, reg/m32
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvttss_si32 (__m128 a)
        ///   CVTTSS2SI r32, xmm/m32
        /// </summary>
        public static int ConvertToInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_div_ps (__m128 a,  __m128 b)
        ///   DIVPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_div_ss (__m128 a,  __m128 b)
        ///   DIVSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_loadu_ps (float const* mem_address)
        ///   MOVUPS xmm, m128
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_load_ss (float const* mem_address)
        ///   MOVSS xmm, m32
        /// </summary>
        public static unsafe Vector128<float> LoadScalarVector128(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_load_ps (float const* mem_address)
        ///   MOVAPS xmm, m128
        /// </summary>
        public static unsafe Vector128<float> LoadAlignedVector128(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_loadh_pi (__m128 a, __m64 const* mem_addr)
        ///   MOVHPS xmm, m64
        /// </summary>
        public static unsafe Vector128<float> LoadHigh(Vector128<float> lower, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_loadl_pi (__m128 a, __m64 const* mem_addr)
        ///   MOVLPS xmm, m64
        /// </summary>
        public static unsafe Vector128<float> LoadLow(Vector128<float> upper, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_max_ps (__m128 a,  __m128 b)
        ///   MAXPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_max_ss (__m128 a,  __m128 b)
        ///   MAXSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> MaxScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_min_ps (__m128 a,  __m128 b)
        ///   MINPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_min_ss (__m128 a,  __m128 b)
        ///   MINSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> MinScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_move_ss (__m128 a, __m128 b)
        ///   MOVSS xmm, xmm
        /// </summary>
        public static Vector128<float> MoveScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_movehl_ps (__m128 a,  __m128 b)
        ///   MOVHLPS xmm, xmm
        /// </summary>
        public static Vector128<float> MoveHighToLow(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_movelh_ps (__m128 a,  __m128 b)
        ///   MOVLHPS xmm, xmm
        /// </summary>
        public static Vector128<float> MoveLowToHigh(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_movemask_ps (__m128 a)
        ///   MOVMSKPS reg, xmm
        /// </summary>
        public static int MoveMask(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_mul_ps (__m128 a, __m128 b)
        ///   MULPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_mul_ss (__m128 a, __m128 b)
        ///   MULPS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_prefetch(char* p, int i)
        ///   PREFETCHT0 m8
        /// </summary>
        public static unsafe void Prefetch0(void* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_prefetch(char* p, int i)
        ///   PREFETCHT1 m8
        /// </summary>
        public static unsafe void Prefetch1(void* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_prefetch(char* p, int i)
        ///   PREFETCHT2 m8
        /// </summary>
        public static unsafe void Prefetch2(void* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_prefetch(char* p, int i)
        ///   PREFETCHNTA m8
        /// </summary>
        public static unsafe void PrefetchNonTemporal(void* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_or_ps (__m128 a,  __m128 b)
        ///   ORPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp_ps (__m128 a)
        ///   RCPPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Reciprocal(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp_ss (__m128 a)
        ///   RCPSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp_ss (__m128 a, __m128 b)
        ///   RCPSS xmm, xmm/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt_ps (__m128 a)
        ///   RSQRTPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> ReciprocalSqrt(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt_ss (__m128 a)
        ///   RSQRTSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt_ss (__m128 a, __m128 b)
        ///   RSQRTSS xmm, xmm/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_shuffle_ps (__m128 a,  __m128 b, unsigned int control)
        ///   SHUFPS xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<float> Shuffle(Vector128<float> left, Vector128<float> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_ps (__m128 a)
        ///   SQRTPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Sqrt(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_ss (__m128 a)
        ///   SQRTSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_ss (__m128 a, __m128 b)
        ///   SQRTSS xmm, xmm/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_store_ps (float* mem_addr, __m128 a)
        ///   MOVAPS m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_stream_ps (float* mem_addr, __m128 a)
        ///   MOVNTPS m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storeu_ps (float* mem_addr, __m128 a)
        ///   MOVUPS m128, xmm
        /// </summary>
        public static unsafe void Store(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_sfence(void)
        ///   SFENCE
        /// </summary>
        public static void StoreFence() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_store_ss (float* mem_addr, __m128 a)
        ///   MOVSS m32, xmm
        /// </summary>
        public static unsafe void StoreScalar(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storeh_pi (__m64* mem_addr, __m128 a)
        ///   MOVHPS m64, xmm
        /// </summary>
        public static unsafe void StoreHigh(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storel_pi (__m64* mem_addr, __m128 a)
        ///   MOVLPS m64, xmm
        /// </summary>
        public static unsafe void StoreLow(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sub_ps (__m128d a, __m128d b)
        ///   SUBPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sub_ss (__m128 a, __m128 b)
        ///   SUBSS xmm, xmm/m32
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_unpackhi_ps (__m128 a,  __m128 b)
        ///   UNPCKHPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> UnpackHigh(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_unpacklo_ps (__m128 a,  __m128 b)
        ///   UNPCKLPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> UnpackLow(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_xor_ps (__m128 a,  __m128 b)
        ///   XORPS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
    }
}

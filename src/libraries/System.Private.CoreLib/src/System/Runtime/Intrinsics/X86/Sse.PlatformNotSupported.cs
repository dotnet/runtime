// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
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
            /// __m128 _mm_cvtsi64_ss (__m128 a, __int64 b)
            ///    CVTSI2SS xmm1,       r/m64
            ///   VCVTSI2SS xmm1, xmm2, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvtss_si64 (__m128 a)
            ///    CVTSS2SI r64, xmm1/m32
            ///   VCVTSS2SI r64, xmm1/m32
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __int64 _mm_cvttss_si64 (__m128 a)
            ///    CVTTSS2SI r64, xmm1/m32
            ///   VCVTTSS2SI r64, xmm1/m32
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// __m128 _mm_add_ps (__m128 a,  __m128 b)
        ///    ADDPS xmm1,               xmm2/m128
        ///   VADDPS xmm1,         xmm2, xmm3/m128
        ///   VADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_add_ss (__m128 a,  __m128 b)
        ///    ADDSS xmm1,               xmm2/m32
        ///   VADDSS xmm1,         xmm2, xmm3/m32
        ///   VADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_and_ps (__m128 a, __m128 b)
        ///    ANDPS xmm1,               xmm2/m128
        ///   VANDPS xmm1,         xmm2, xmm3/m128
        ///   VANDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_andnot_ps (__m128 a, __m128 b)
        ///    ANDNPS xmm1,               xmm2/m128
        ///   VANDNPS xmm1,         xmm2, xmm3/m128
        ///   VANDNPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpeq_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(0)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(0)
        /// </summary>
        public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpgt_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(1)   ; with swapped operands
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(1)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpge_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(2)   ; with swapped operands
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(2)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmplt_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(1)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(1)
        /// </summary>
        public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmple_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(2)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(2)
        /// </summary>
        public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpneq_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(4)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(4)
        /// </summary>
        public static Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpngt_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(5)   ; with swapped operands
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(5)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnge_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(6)   ; with swapped operands
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(6)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnlt_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(5)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(5)
        /// </summary>
        public static Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnle_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(6)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(6)
        /// </summary>
        public static Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpord_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(7)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(7)
        /// </summary>
        public static Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpeq_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(0)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(0)
        /// </summary>
        public static Vector128<float> CompareScalarEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpgt_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(1)   ; with swapped operands
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(1)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpge_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(2)   ; with swapped operands
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(2)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmplt_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(1)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(1)
        /// </summary>
        public static Vector128<float> CompareScalarLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmple_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(2)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(2)
        /// </summary>
        public static Vector128<float> CompareScalarLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpneq_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(4)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(4)
        /// </summary>
        public static Vector128<float> CompareScalarNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpngt_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(5)   ; with swapped operands
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(5)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnge_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(6)   ; with swapped operands
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(6)   ; with swapped operands
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnlt_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(5)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(5)
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cmpnle_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(6)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(6)
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpord_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(7)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(7)
        /// </summary>
        public static Vector128<float> CompareScalarOrdered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comieq_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; ZF=1 &amp;&amp; PF=0
        ///   VCOMISS xmm1, xmm2/m32        ; ZF=1 &amp;&amp; PF=0
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; ZF=1 &amp;&amp; PF=0
        /// </summary>
        public static bool CompareScalarOrderedEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comigt_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; ZF=0 &amp;&amp; CF=0
        ///   VCOMISS xmm1, xmm2/m32        ; ZF=0 &amp;&amp; CF=0
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; ZF=0 &amp;&amp; CF=0
        /// </summary>
        public static bool CompareScalarOrderedGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comige_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; CF=0
        ///   VCOMISS xmm1, xmm2/m32        ; CF=0
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; CF=0
        /// </summary>
        public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comilt_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; CF=1
        ///   VCOMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; CF=1
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; PF=0 &amp;&amp; CF=1
        /// </summary>
        public static bool CompareScalarOrderedLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comile_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        ///   VCOMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        /// </summary>
        public static bool CompareScalarOrderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_comineq_ss (__m128 a, __m128 b)
        ///    COMISS xmm1, xmm2/m32        ; ZF=0 || PF=1
        ///   VCOMISS xmm1, xmm2/m32        ; ZF=0 || PF=1
        ///   VCOMISS xmm1, xmm2/m32{sae}   ; ZF=0 || PF=1
        /// </summary>
        public static bool CompareScalarOrderedNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpunord_ss (__m128 a,  __m128 b)
        ///    CMPSS xmm1,       xmm2/m32, imm8(3)
        ///   VCMPSS xmm1, xmm2, xmm3/m32, imm8(3)
        /// </summary>
        public static Vector128<float> CompareScalarUnordered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomieq_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; ZF=1 &amp;&amp; PF=0
        ///   VUCOMISS xmm1, xmm2/m32       ; ZF=1 &amp;&amp; PF=0
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=1 &amp;&amp; PF=0
        /// </summary>
        public static bool CompareScalarUnorderedEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomigt_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; ZF=0 &amp;&amp; CF=0
        ///   VUCOMISS xmm1, xmm2/m32       ; ZF=0 &amp;&amp; CF=0
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=0 &amp;&amp; CF=0
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomige_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; CF=0
        ///   VUCOMISS xmm1, xmm2/m32       ; CF=0
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; CF=0
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomilt_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; CF=1
        ///   VUCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; CF=1
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; PF=0 &amp;&amp; CF=1
        /// </summary>
        public static bool CompareScalarUnorderedLessThan(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomile_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        ///   VUCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; PF=0 &amp;&amp; (ZF=1 || CF=1)
        /// </summary>
        public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_ucomineq_ss (__m128 a, __m128 b)
        ///    UCOMISS xmm1, xmm2/m32       ; ZF=0 || PF=1
        ///   VUCOMISS xmm1, xmm2/m32       ; ZF=0 || PF=1
        ///   VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=0 || PF=1
        /// </summary>
        public static bool CompareScalarUnorderedNotEqual(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cmpunord_ps (__m128 a,  __m128 b)
        ///    CMPPS xmm1,       xmm2/m128, imm8(3)
        ///   VCMPPS xmm1, xmm2, xmm3/m128, imm8(3)
        /// </summary>
        public static Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_cvtsi32_ss (__m128 a, int b)
        ///    CVTSI2SS xmm1,       r/m32
        ///   VCVTSI2SS xmm1, xmm2, r/m32
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvtss_si32 (__m128 a)
        ///    CVTSS2SI r32, xmm1/m32
        ///   VCVTSS2SI r32, xmm1/m32
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_cvttss_si32 (__m128 a)
        ///    CVTTSS2SI r32, xmm1/m32
        ///   VCVTTSS2SI r32, xmm1/m32
        /// </summary>
        public static int ConvertToInt32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_div_ps (__m128 a,  __m128 b)
        ///    DIVPS xmm,                xmm2/m128
        ///   VDIVPS xmm1,         xmm2, xmm3/m128
        ///   VDIVPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_div_ss (__m128 a,  __m128 b)
        ///    DIVSS xmm1,       xmm2/m32
        ///   VDIVSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_load_ps (float const* mem_address)
        ///    MOVAPS xmm1,         m128
        ///   VMOVAPS xmm1,         m128
        ///   VMOVAPS xmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector128<float> LoadAlignedVector128(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_loadh_pi (__m128 a, __m64 const* mem_addr)
        ///    MOVHPS xmm1,       m64
        ///   VMOVHPS xmm1, xmm2, m64
        /// </summary>
        public static unsafe Vector128<float> LoadHigh(Vector128<float> lower, float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_loadl_pi (__m128 a, __m64 const* mem_addr)
        ///    MOVLPS xmm1,       m64
        ///   VMOVLPS xmm1, xmm2, m64
        /// </summary>
        public static unsafe Vector128<float> LoadLow(Vector128<float> upper, float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_load_ss (float const* mem_address)
        ///    MOVSS xmm1,      m32
        ///   VMOVSS xmm1,      m32
        ///   VMOVSS xmm1 {k1}, m32
        /// </summary>
        public static unsafe Vector128<float> LoadScalarVector128(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_loadu_ps (float const* mem_address)
        ///    MOVUPS xmm1,         m128
        ///   VMOVUPS xmm1,         m128
        ///   VMOVUPS xmm1 {k1}{z}, m128
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_max_ps (__m128 a,  __m128 b)
        ///    MAXPS xmm1,               xmm2/m128
        ///   VMAXPS xmm1,         xmm2, xmm3/m128
        ///   VMAXPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_max_ss (__m128 a,  __m128 b)
        ///    MAXSS xmm1,       xmm2/m32
        ///   VMAXSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> MaxScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_min_ps (__m128 a,  __m128 b)
        ///    MINPS xmm1,               xmm2/m128
        ///   VMINPS xmm1,         xmm2, xmm3/m128
        ///   VMINPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_min_ss (__m128 a,  __m128 b)
        ///    MINSS xmm1,       xmm2/m32
        ///   VMINSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> MinScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_movehl_ps (__m128 a,  __m128 b)
        ///    MOVHLPS xmm1,       xmm2
        ///   VMOVHLPS xmm1, xmm2, xmm3
        /// </summary>
        public static Vector128<float> MoveHighToLow(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_movelh_ps (__m128 a,  __m128 b)
        ///    MOVLHPS xmm1,       xmm2
        ///   VMOVLHPS xmm1, xmm2, xmm3
        /// </summary>
        public static Vector128<float> MoveLowToHigh(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_movemask_ps (__m128 a)
        ///    MOVMSKPS r32, xmm1
        ///   VMOVMSKPS r32, xmm1
        /// </summary>
        public static int MoveMask(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_move_ss (__m128 a, __m128 b)
        ///    MOVSS xmm1,         xmm2
        ///   VMOVSS xmm1,         xmm2, xmm3
        ///   VMOVSS xmm1 {k1}{z}, xmm2, xmm3
        /// </summary>
        public static Vector128<float> MoveScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_mul_ps (__m128 a, __m128 b)
        ///    MULPS xmm1,               xmm2/m128
        ///   VMULPS xmm1,         xmm2, xmm3/m128
        ///   VMULPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_mul_ss (__m128 a, __m128 b)
        ///    MULSS xmm1,       xmm2/m32
        ///   VMULSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_or_ps (__m128 a,  __m128 b)
        ///    ORPS xmm1,               xmm2/m128
        ///   VORPS xmm1,         xmm2, xmm3/m128
        ///   VORPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

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
        /// __m128 _mm_rcp_ps (__m128 a)
        ///    RCPPS xmm1, xmm2/m128
        ///   VRCPPS xmm1, xmm2/m128
        /// </summary>
        public static Vector128<float> Reciprocal(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rcp_ss (__m128 a)
        ///    RCPSS xmm1,       xmm2/m32
        ///   VRCPSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_rcp_ss (__m128 a, __m128 b)
        ///    RCPSS xmm1,       xmm2/m32
        ///   VRCPSS xmm1, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt_ps (__m128 a)
        ///    RSQRTPS xmm1, xmm2/m128
        ///   VRSQRTPS xmm1, xmm2/m128
        /// </summary>
        public static Vector128<float> ReciprocalSqrt(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_rsqrt_ss (__m128 a)
        ///    RSQRTSS xmm1,       xmm2/m32
        ///   VRSQRTSS xmm1, xmm2, xmm3/m32
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_rsqrt_ss (__m128 a, __m128 b)
        ///    RSQRTSS xmm1,       xmm2/m32
        ///   VRSQRTSS xmm1, xmm2, xmm3/m32
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_shuffle_ps (__m128 a,  __m128 b, unsigned int control)
        ///    SHUFPS xmm1,               xmm2/m128,         imm8
        ///   VSHUFPS xmm1,         xmm2, xmm3/m128,         imm8
        ///   VSHUFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8
        /// </summary>
        public static Vector128<float> Shuffle(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_ps (__m128 a)
        ///    SQRTPS xmm1,         xmm2/m128
        ///   VSQRTPS xmm1,         xmm2/m128
        ///   VSQRTPS xmm1 {k1}{z}, xmm2/m128/m32bcst
        /// </summary>
        public static Vector128<float> Sqrt(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sqrt_ss (__m128 a)
        ///    SQRTSS xmm1,               xmm2/m32
        ///   VSQRTSS xmm1,         xmm2, xmm3/m32
        ///   VSQRTSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_sqrt_ss (__m128 a, __m128 b)
        ///    SQRTSS xmm1,               xmm2/m32
        ///   VSQRTSS xmm1,         xmm2, xmm3/m32
        ///   VSQRTSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storeu_ps (float* mem_addr, __m128 a)
        ///    MOVUPS m128,         xmm1
        ///   VMOVUPS m128,         xmm1
        ///   VMOVUPS m128 {k1}{z}, xmm1
        /// </summary>
        public static unsafe void Store(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_ps (float* mem_addr, __m128 a)
        ///    MOVAPS m128,         xmm1
        ///   VMOVAPS m128,         xmm1
        ///   VMOVAPS m128 {k1}{z}, xmm1
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_ps (float* mem_addr, __m128 a)
        ///    MOVNTPS m128, xmm1
        ///   VMOVNTPS m128, xmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_sfence(void)
        ///   SFENCE
        /// </summary>
        public static void StoreFence() { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeh_pi (__m64* mem_addr, __m128 a)
        ///    MOVHPS m64, xmm1
        ///   VMOVHPS m64, xmm1
        /// </summary>
        public static unsafe void StoreHigh(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storel_pi (__m64* mem_addr, __m128 a)
        ///    MOVLPS m64, xmm1
        ///   VMOVLPS m64, xmm1
        /// </summary>
        public static unsafe void StoreLow(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_ss (float* mem_addr, __m128 a)
        ///    MOVSS m32,      xmm1
        ///   VMOVSS m32,      xmm1
        ///   VMOVSS m32 {k1}, xmm1
        /// </summary>
        public static unsafe void StoreScalar(float* address, Vector128<float> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sub_ps (__m128d a, __m128d b)
        ///    SUBPS xmm1,               xmm2/m128
        ///   VSUBPS xmm1,         xmm2, xmm3/m128
        ///   VSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_sub_ss (__m128 a, __m128 b)
        ///    SUBSS xmm1,               xmm2/m32
        ///   VSUBSS xmm1,         xmm2, xmm3/m32
        ///   VSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_unpackhi_ps (__m128 a,  __m128 b)
        ///    UNPCKHPS xmm1,               xmm2/m128
        ///   VUNPCKHPS xmm1,         xmm2, xmm3/m128
        ///   VUNPCKHPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> UnpackHigh(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_unpacklo_ps (__m128 a,  __m128 b)
        ///    UNPCKLPS xmm1,               xmm2/m128
        ///   VUNPCKLPS xmm1,         xmm2, xmm3/m128
        ///   VUNPCKLPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> UnpackLow(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128 _mm_xor_ps (__m128 a,  __m128 b)
        ///    XORPS xmm1,               xmm2/m128
        ///   VXORPS xmm1,         xmm2, xmm3/m128
        ///   VXORPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SSE hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse : X86Base
    {
        internal Sse() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 SSE hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : X86Base.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m128 _mm_cvtsi64_ss (__m128 a, __int64 b)</para>
            ///   <para>   CVTSI2SS xmm1,       r/m64</para>
            ///   <para>  VCVTSI2SS xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, long value) => ConvertScalarToVector128Single(upper, value);

            /// <summary>
            ///   <para>__int64 _mm_cvtss_si64 (__m128 a)</para>
            ///   <para>   CVTSS2SI r64, xmm1/m32</para>
            ///   <para>  VCVTSS2SI r64, xmm1/m32</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<float> value) => ConvertToInt64(value);
            /// <summary>
            ///   <para>__int64 _mm_cvttss_si64 (__m128 a)</para>
            ///   <para>   CVTTSS2SI r64, xmm1/m32</para>
            ///   <para>  VCVTTSS2SI r64, xmm1/m32</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64WithTruncation(Vector128<float> value) => ConvertToInt64WithTruncation(value);
        }

        /// <summary>
        ///   <para>__m128 _mm_add_ps (__m128 a,  __m128 b)</para>
        ///   <para>   ADDPS xmm1,               xmm2/m128</para>
        ///   <para>  VADDPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VADDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) => Add(left, right);

        /// <summary>
        ///   <para>__m128 _mm_add_ss (__m128 a,  __m128 b)</para>
        ///   <para>   ADDSS xmm1,               xmm2/m32</para>
        ///   <para>  VADDSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VADDSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> AddScalar(Vector128<float> left, Vector128<float> right) => AddScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_and_ps (__m128 a, __m128 b)</para>
        ///   <para>   ANDPS xmm1,               xmm2/m128</para>
        ///   <para>  VANDPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VANDPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        ///   <para>__m128 _mm_andnot_ps (__m128 a, __m128 b)</para>
        ///   <para>   ANDNPS xmm1,               xmm2/m128</para>
        ///   <para>  VANDNPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VANDNPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) => AndNot(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cmpeq_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(0)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpgt_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(1)   ; with swapped operands</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(1)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpge_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(2)   ; with swapped operands</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(2)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmplt_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(1)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(1)</para>
        /// </summary>
        public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmple_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(2)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(2)</para>
        /// </summary>
        public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpneq_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(4)</para>
        /// </summary>
        public static Vector128<float> CompareNotEqual(Vector128<float> left, Vector128<float> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpngt_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(5)   ; with swapped operands</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(5)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareNotGreaterThan(Vector128<float> left, Vector128<float> right) => CompareNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnge_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(6)   ; with swapped operands</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(6)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnlt_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(5)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(5)</para>
        /// </summary>
        public static Vector128<float> CompareNotLessThan(Vector128<float> left, Vector128<float> right) => CompareNotLessThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnle_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(6)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(6)</para>
        /// </summary>
        public static Vector128<float> CompareNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareNotLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpord_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(7)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(7)</para>
        /// </summary>
        public static Vector128<float> CompareOrdered(Vector128<float> left, Vector128<float> right) => CompareOrdered(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cmpeq_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(0)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(0)</para>
        /// </summary>
        public static Vector128<float> CompareScalarEqual(Vector128<float> left, Vector128<float> right) => CompareScalarEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpgt_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(1)   ; with swapped operands</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(1)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThan(Vector128<float> left, Vector128<float> right) => CompareScalarGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpge_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(2)   ; with swapped operands</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(2)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareScalarGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmplt_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(1)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(1)</para>
        /// </summary>
        public static Vector128<float> CompareScalarLessThan(Vector128<float> left, Vector128<float> right) => CompareScalarLessThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmple_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(2)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(2)</para>
        /// </summary>
        public static Vector128<float> CompareScalarLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpneq_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(4)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(4)</para>
        /// </summary>
        public static Vector128<float> CompareScalarNotEqual(Vector128<float> left, Vector128<float> right) => CompareScalarNotEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpngt_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(5)   ; with swapped operands</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(5)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThan(Vector128<float> left, Vector128<float> right) => CompareScalarNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnge_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(6)   ; with swapped operands</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(6)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<float> CompareScalarNotGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnlt_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(5)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(5)</para>
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThan(Vector128<float> left, Vector128<float> right) => CompareScalarNotLessThan(left, right);
        /// <summary>
        ///   <para>__m128 _mm_cmpnle_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(6)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(6)</para>
        /// </summary>
        public static Vector128<float> CompareScalarNotLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarNotLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cmpord_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(7)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(7)</para>
        /// </summary>
        public static Vector128<float> CompareScalarOrdered(Vector128<float> left, Vector128<float> right) => CompareScalarOrdered(left, right);
        /// <summary>
        ///   <para>int _mm_comieq_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; ZF=1 &amp;&amp; PF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedEqual(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comigt_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedGreaterThan(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedGreaterThan(left, right);
        /// <summary>
        ///   <para>int _mm_comige_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; CF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; CF=0</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; CF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comilt_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; PF=0 &amp;&amp; CF=1</para>
        /// </summary>
        public static bool CompareScalarOrderedLessThan(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedLessThan(left, right);
        /// <summary>
        ///   <para>int _mm_comile_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        /// </summary>
        public static bool CompareScalarOrderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comineq_ss (__m128 a, __m128 b)</para>
        ///   <para>   COMISS xmm1, xmm2/m32        ; ZF=0 || PF=1</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32        ; ZF=0 || PF=1</para>
        ///   <para>  VCOMISS xmm1, xmm2/m32{sae}   ; ZF=0 || PF=1</para>
        /// </summary>
        public static bool CompareScalarOrderedNotEqual(Vector128<float> left, Vector128<float> right) => CompareScalarOrderedNotEqual(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cmpunord_ss (__m128 a,  __m128 b)</para>
        ///   <para>   CMPSS xmm1,       xmm2/m32, imm8(3)</para>
        ///   <para>  VCMPSS xmm1, xmm2, xmm3/m32, imm8(3)</para>
        /// </summary>
        public static Vector128<float> CompareScalarUnordered(Vector128<float> left, Vector128<float> right) => CompareScalarUnordered(left, right);
        /// <summary>
        ///   <para>int _mm_ucomieq_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=1 &amp;&amp; PF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedEqual(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomigt_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThan(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedGreaterThan(left, right);
        /// <summary>
        ///   <para>int _mm_ucomige_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; CF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; CF=0</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; CF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomilt_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; PF=0 &amp;&amp; CF=1</para>
        /// </summary>
        public static bool CompareScalarUnorderedLessThan(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedLessThan(left, right);
        /// <summary>
        ///   <para>int _mm_ucomile_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        /// </summary>
        public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomineq_ss (__m128 a, __m128 b)</para>
        ///   <para>   UCOMISS xmm1, xmm2/m32       ; ZF=0 || PF=1</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32       ; ZF=0 || PF=1</para>
        ///   <para>  VUCOMISS xmm1, xmm2/m32{sae}  ; ZF=0 || PF=1</para>
        /// </summary>
        public static bool CompareScalarUnorderedNotEqual(Vector128<float> left, Vector128<float> right) => CompareScalarUnorderedNotEqual(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cmpunord_ps (__m128 a,  __m128 b)</para>
        ///   <para>   CMPPS xmm1,       xmm2/m128, imm8(3)</para>
        ///   <para>  VCMPPS xmm1, xmm2, xmm3/m128, imm8(3)</para>
        /// </summary>
        public static Vector128<float> CompareUnordered(Vector128<float> left, Vector128<float> right) => CompareUnordered(left, right);

        /// <summary>
        ///   <para>__m128 _mm_cvtsi32_ss (__m128 a, int b)</para>
        ///   <para>   CVTSI2SS xmm1,       r/m32</para>
        ///   <para>  VCVTSI2SS xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, int value) => ConvertScalarToVector128Single(upper, value);

        /// <summary>
        ///   <para>int _mm_cvtss_si32 (__m128 a)</para>
        ///   <para>   CVTSS2SI r32, xmm1/m32</para>
        ///   <para>  VCVTSS2SI r32, xmm1/m32</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<float> value) => ConvertToInt32(value);
        /// <summary>
        ///   <para>int _mm_cvttss_si32 (__m128 a)</para>
        ///   <para>   CVTTSS2SI r32, xmm1/m32</para>
        ///   <para>  VCVTTSS2SI r32, xmm1/m32</para>
        /// </summary>
        public static int ConvertToInt32WithTruncation(Vector128<float> value) => ConvertToInt32WithTruncation(value);

        /// <summary>
        ///   <para>__m128 _mm_div_ps (__m128 a,  __m128 b)</para>
        ///   <para>   DIVPS xmm,                xmm2/m128</para>
        ///   <para>  VDIVPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VDIVPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) => Divide(left, right);

        /// <summary>
        ///   <para>__m128 _mm_div_ss (__m128 a,  __m128 b)</para>
        ///   <para>   DIVSs xmm1,       xmm2/m32</para>
        ///   <para>  VDIVSs xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> DivideScalar(Vector128<float> left, Vector128<float> right) => DivideScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_load_ps (float const* mem_address)</para>
        ///   <para>   MOVAPS xmm1,         m128</para>
        ///   <para>  VMOVAPS xmm1,         m128</para>
        ///   <para>  VMOVAPS xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<float> LoadAlignedVector128(float* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128 _mm_loadh_pi (__m128 a, __m64 const* mem_addr)</para>
        ///   <para>   MOVHPS xmm1,       m64</para>
        ///   <para>  VMOVHPS xmm1, xmm2, m64</para>
        /// </summary>
        public static unsafe Vector128<float> LoadHigh(Vector128<float> lower, float* address) => LoadHigh(lower, address);
        /// <summary>
        ///   <para>__m128 _mm_loadl_pi (__m128 a, __m64 const* mem_addr)</para>
        ///   <para>   MOVLPS xmm1,       m64</para>
        ///   <para>  VMOVLPS xmm1, xmm2, m64</para>
        /// </summary>
        public static unsafe Vector128<float> LoadLow(Vector128<float> upper, float* address) => LoadLow(upper, address);
        /// <summary>
        ///   <para>__m128 _mm_load_ss (float const* mem_address)</para>
        ///   <para>   MOVSS xmm1,      m32</para>
        ///   <para>  VMOVSS xmm1,      m32</para>
        ///   <para>  VMOVSS xmm1 {k1}, m32</para>
        /// </summary>
        public static unsafe Vector128<float> LoadScalarVector128(float* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>__m128 _mm_loadu_ps (float const* mem_address)</para>
        ///   <para>   MOVUPS xmm1,         m128</para>
        ///   <para>  VMOVUPS xmm1,         m128</para>
        ///   <para>  VMOVUPS xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) => LoadVector128(address);

        /// <summary>
        ///   <para>__m128 _mm_max_ps (__m128 a,  __m128 b)</para>
        ///   <para>   MAXPS xmm1,               xmm2/m128</para>
        ///   <para>  VMAXPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMAXPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) => Max(left, right);

        /// <summary>
        ///   <para>__m128 _mm_max_ss (__m128 a,  __m128 b)</para>
        ///   <para>   MAXSS xmm1,       xmm2/m32</para>
        ///   <para>  VMAXSS xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> MaxScalar(Vector128<float> left, Vector128<float> right) => MaxScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_min_ps (__m128 a,  __m128 b)</para>
        ///   <para>   MINPS xmm1,               xmm2/m128</para>
        ///   <para>  VMINPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMINPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) => Min(left, right);

        /// <summary>
        ///   <para>__m128 _mm_min_ss (__m128 a,  __m128 b)</para>
        ///   <para>   MINSS xmm1,       xmm2/m32</para>
        ///   <para>  VMINSS xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> MinScalar(Vector128<float> left, Vector128<float> right) => MinScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_movehl_ps (__m128 a,  __m128 b)</para>
        ///   <para>   MOVHLPS xmm1,       xmm2</para>
        ///   <para>  VMOVHLPS xmm1, xmm2, xmm3</para>
        /// </summary>
        public static Vector128<float> MoveHighToLow(Vector128<float> left, Vector128<float> right) => MoveHighToLow(left, right);
        /// <summary>
        ///   <para>__m128 _mm_movelh_ps (__m128 a,  __m128 b)</para>
        ///   <para>   MOVLHPS xmm1,       xmm2</para>
        ///   <para>  VMOVLHPS xmm1, xmm2, xmm3</para>
        /// </summary>
        public static Vector128<float> MoveLowToHigh(Vector128<float> left, Vector128<float> right) => MoveLowToHigh(left, right);
        /// <summary>
        ///   <para>int _mm_movemask_ps (__m128 a)</para>
        ///   <para>   MOVMSKPS r32, xmm1</para>
        ///   <para>  VMOVMSKPS r32, xmm1</para>
        /// </summary>
        public static int MoveMask(Vector128<float> value) => MoveMask(value);
        /// <summary>
        ///   <para>__m128 _mm_move_ss (__m128 a, __m128 b)</para>
        ///   <para>   MOVSS xmm1,         xmm2</para>
        ///   <para>  VMOVSS xmm1,         xmm2, xmm3</para>
        ///   <para>  VMOVSS xmm1 {k1}{z}, xmm2, xmm3</para>
        /// </summary>
        public static Vector128<float> MoveScalar(Vector128<float> upper, Vector128<float> value) => MoveScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_mul_ps (__m128 a, __m128 b)</para>
        ///   <para>   MULPS xmm1,               xmm2/m128</para>
        ///   <para>  VMULPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMULPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) => Multiply(left, right);

        /// <summary>
        ///   <para>__m128 _mm_mul_ss (__m128 a, __m128 b)</para>
        ///   <para>   MULSS xmm1,       xmm2/m32</para>
        ///   <para>  VMULSS xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> MultiplyScalar(Vector128<float> left, Vector128<float> right) => MultiplyScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_or_ps (__m128 a,  __m128 b)</para>
        ///   <para>   ORPS xmm1,               xmm2/m128</para>
        ///   <para>  VORPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VORPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        ///   <para>void _mm_prefetch(char* p, int i)</para>
        ///   <para>  PREFETCHT0 m8</para>
        /// </summary>
        public static unsafe void Prefetch0(void* address) => Prefetch0(address);
        /// <summary>
        ///   <para>void _mm_prefetch(char* p, int i)</para>
        ///   <para>  PREFETCHT1 m8</para>
        /// </summary>
        public static unsafe void Prefetch1(void* address) => Prefetch1(address);
        /// <summary>
        ///   <para>void _mm_prefetch(char* p, int i)</para>
        ///   <para>  PREFETCHT2 m8</para>
        /// </summary>
        public static unsafe void Prefetch2(void* address) => Prefetch2(address);
        /// <summary>
        ///   <para>void _mm_prefetch(char* p, int i)</para>
        ///   <para>  PREFETCHNTA m8</para>
        /// </summary>
        public static unsafe void PrefetchNonTemporal(void* address) => PrefetchNonTemporal(address);

        /// <summary>
        ///   <para>__m128 _mm_rcp_ps (__m128 a)</para>
        ///   <para>   RCPPS xmm1, xmm2/m128</para>
        ///   <para>  VRCPPS xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<float> Reciprocal(Vector128<float> value) => Reciprocal(value);

        /// <summary>
        ///   <para>__m128 _mm_rcp_ss (__m128 a)</para>
        ///   <para>   RCPSS xmm1,       xmm2/m32</para>
        ///   <para>  VRCPSS xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> value) => ReciprocalScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_rcp_ss (__m128 a, __m128 b)</para>
        ///   <para>   RCPSS xmm1,       xmm2/m32</para>
        ///   <para>  VRCPSS xmm1, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> ReciprocalScalar(Vector128<float> upper, Vector128<float> value) => ReciprocalScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_rsqrt_ps (__m128 a)</para>
        ///   <para>   RSQRTPS xmm1, xmm2/m128</para>
        ///   <para>  VRSQRTPS xmm1, xmm2/m128</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrt(Vector128<float> value) => ReciprocalSqrt(value);

        /// <summary>
        ///   <para>__m128 _mm_rsqrt_ss (__m128 a)</para>
        ///   <para>   RSQRTSS xmm1,       xmm2/m32</para>
        ///   <para>  VRSQRTSS xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> value) => ReciprocalSqrtScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_rsqrt_ss (__m128 a, __m128 b)</para>
        ///   <para>   RSQRTSS xmm1,       xmm2/m32</para>
        ///   <para>  VRSQRTSS xmm1, xmm2, xmm3/m32</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> ReciprocalSqrtScalar(Vector128<float> upper, Vector128<float> value) => ReciprocalSqrtScalar(upper, value);

        /// <summary>
        ///   <para>__m128 _mm_shuffle_ps (__m128 a,  __m128 b, unsigned int control)</para>
        ///   <para>   SHUFPS xmm1,               xmm2/m128,         imm8</para>
        ///   <para>  VSHUFPS xmm1,         xmm2, xmm3/m128,         imm8</para>
        ///   <para>  VSHUFPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<float> Shuffle(Vector128<float> left, Vector128<float> right, [ConstantExpected] byte control) => Shuffle(left, right, control);

        /// <summary>
        ///   <para>__m128 _mm_sqrt_ps (__m128 a)</para>
        ///   <para>   SQRTPS xmm1,         xmm2/m128</para>
        ///   <para>  VSQRTPS xmm1,         xmm2/m128</para>
        ///   <para>  VSQRTPS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Sqrt(Vector128<float> value) => Sqrt(value);

        /// <summary>
        ///   <para>__m128 _mm_sqrt_ss (__m128 a)</para>
        ///   <para>   SQRTSS xmm1,               xmm2/m32</para>
        ///   <para>  VSQRTSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VSQRTSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> value) => SqrtScalar(value);
        /// <summary>
        ///   <para>__m128 _mm_sqrt_ss (__m128 a, __m128 b)</para>
        ///   <para>   SQRTSS xmm1,               xmm2/m32</para>
        ///   <para>  VSQRTSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VSQRTSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.</para>
        /// </summary>
        public static Vector128<float> SqrtScalar(Vector128<float> upper, Vector128<float> value) => SqrtScalar(upper, value);

        /// <summary>
        ///   <para>void _mm_storeu_ps (float* mem_addr, __m128 a)</para>
        ///   <para>   MOVAPS m128,         xmm1</para>
        ///   <para>  VMOVAPS m128,         xmm1</para>
        ///   <para>  VMOVAPS m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(float* address, Vector128<float> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_store_ps (float* mem_addr, __m128 a)</para>
        ///   <para>   MOVAPS m128,         xmm1</para>
        ///   <para>  VMOVAPS m128,         xmm1</para>
        ///   <para>  VMOVAPS m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector128<float> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_stream_ps (float* mem_addr, __m128 a)</para>
        ///   <para>   MOVNTPS m128, xmm1</para>
        ///   <para>  VMOVNTPS m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector128<float> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_sfence(void)</para>
        ///   <para>  SFENCE</para>
        /// </summary>
        public static void StoreFence() => StoreFence();
        /// <summary>
        ///   <para>void _mm_storeh_pi (__m64* mem_addr, __m128 a)</para>
        ///   <para>   MOVHPS m64, xmm1</para>
        ///   <para>  VMOVHPS m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreHigh(float* address, Vector128<float> source) => StoreHigh(address, source);
        /// <summary>
        ///   <para>void _mm_storel_pi (__m64* mem_addr, __m128 a)</para>
        ///   <para>   MOVLPS m64, xmm1</para>
        ///   <para>  VMOVLPS m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreLow(float* address, Vector128<float> source) => StoreLow(address, source);
        /// <summary>
        ///   <para>void _mm_store_ss (float* mem_addr, __m128 a)</para>
        ///   <para>   MOVSS m32,      xmm1</para>
        ///   <para>  VMOVSS m32,      xmm1</para>
        ///   <para>  VMOVSS m32 {k1}, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(float* address, Vector128<float> source) => StoreScalar(address, source);

        /// <summary>
        ///   <para>__m128d _mm_sub_ps (__m128d a, __m128d b)</para>
        ///   <para>   SUBPS xmm1,               xmm2/m128</para>
        ///   <para>  VSUBPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VSUBPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) => Subtract(left, right);

        /// <summary>
        ///   <para>__m128 _mm_sub_ss (__m128 a, __m128 b)</para>
        ///   <para>   SUBSS xmm1,               xmm2/m32</para>
        ///   <para>  VSUBSS xmm1,         xmm2, xmm3/m32</para>
        ///   <para>  VSUBSS xmm1 {k1}{z}, xmm2, xmm3/m32{er}</para>
        /// </summary>
        public static Vector128<float> SubtractScalar(Vector128<float> left, Vector128<float> right) => SubtractScalar(left, right);

        /// <summary>
        ///   <para>__m128 _mm_unpackhi_ps (__m128 a,  __m128 b)</para>
        ///   <para>   UNPCKHPS xmm1,               xmm2/m128</para>
        ///   <para>  VUNPCKHPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VUNPCKHPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> UnpackHigh(Vector128<float> left, Vector128<float> right) => UnpackHigh(left, right);

        /// <summary>
        ///   <para>__m128 _mm_unpacklo_ps (__m128 a,  __m128 b)</para>
        ///   <para>   UNPCKLPS xmm1,               xmm2/m128</para>
        ///   <para>  VUNPCKLPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VUNPCKLPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> UnpackLow(Vector128<float> left, Vector128<float> right) => UnpackLow(left, right);

        /// <summary>
        ///   <para>__m128 _mm_xor_ps (__m128 a,  __m128 b)</para>
        ///   <para>   XORPS xmm1,               xmm2/m128</para>
        ///   <para>  VXORPS xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VXORPS xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);
    }
}

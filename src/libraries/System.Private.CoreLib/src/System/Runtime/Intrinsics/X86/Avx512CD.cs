// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512CD hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512CD : Avx512F
    {
        internal Avx512CD() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 AVX512CD+VL hardware instructions via intrinsics.</summary>
        [Intrinsic]
        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m128i _mm_conflict_epi32 (__m128i a)</para>
            ///   <para>  VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<int> DetectConflicts(Vector128<int> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m128i _mm_conflict_epi32 (__m128i a)</para>
            ///   <para>  VPCONFLICTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> DetectConflicts(Vector128<uint> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m128i _mm_conflict_epi64 (__m128i a)</para>
            ///   <para>  VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> DetectConflicts(Vector128<long> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m128i _mm_conflict_epi64 (__m128i a)</para>
            ///   <para>  VPCONFLICTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> DetectConflicts(Vector128<ulong> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m256i _mm256_conflict_epi32 (__m256i a)</para>
            ///   <para>  VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<int> DetectConflicts(Vector256<int> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m256i _mm256_conflict_epi32 (__m256i a)</para>
            ///   <para>  VPCONFLICTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> DetectConflicts(Vector256<uint> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m256i _mm256_conflict_epi64 (__m256i a)</para>
            ///   <para>  VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> DetectConflicts(Vector256<long> value) => DetectConflicts(value);
            /// <summary>
            ///   <para>__m256i _mm256_conflict_epi64 (__m256i a)</para>
            ///   <para>  VPCONFLICTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> DetectConflicts(Vector256<ulong> value) => DetectConflicts(value);

            /// <summary>
            ///   <para>__m128i _mm_lzcnt_epi32 (__m128i a)</para>
            ///   <para>  VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<int> LeadingZeroCount(Vector128<int> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m128i _mm_lzcnt_epi32 (__m128i a)</para>
            ///   <para>  VPLZCNTD xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
            /// </summary>
            public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m128i _mm_lzcnt_epi64 (__m128i a)</para>
            ///   <para>  VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<long> LeadingZeroCount(Vector128<long> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m128i _mm_lzcnt_epi64 (__m128i a)</para>
            ///   <para>  VPLZCNTQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
            /// </summary>
            public static Vector128<ulong> LeadingZeroCount(Vector128<ulong> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m256i _mm256_lzcnt_epi32 (__m256i a)</para>
            ///   <para>  VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<int> LeadingZeroCount(Vector256<int> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m256i _mm256_lzcnt_epi32 (__m256i a)</para>
            ///   <para>  VPLZCNTD ymm1 {k1}{z}, ymm2/m256/m32bcst</para>
            /// </summary>
            public static Vector256<uint> LeadingZeroCount(Vector256<uint> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m256i _mm256_lzcnt_epi64 (__m256i a)</para>
            ///   <para>  VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<long> LeadingZeroCount(Vector256<long> value) => LeadingZeroCount(value);
            /// <summary>
            ///   <para>__m256i _mm256_lzcnt_epi64 (__m256i a)</para>
            ///   <para>  VPLZCNTQ ymm1 {k1}{z}, ymm2/m256/m64bcst</para>
            /// </summary>
            public static Vector256<ulong> LeadingZeroCount(Vector256<ulong> value) => LeadingZeroCount(value);
        }

        /// <summary>Provides access to the x86 AVX512CD hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        ///   <para>__m512i _mm512_conflict_epi32 (__m512i a)</para>
        ///   <para>  VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> DetectConflicts(Vector512<int> value) => DetectConflicts(value);
        /// <summary>
        ///   <para>__m512i _mm512_conflict_epi32 (__m512i a)</para>
        ///   <para>  VPCONFLICTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> DetectConflicts(Vector512<uint> value) => DetectConflicts(value);
        /// <summary>
        ///   <para>__m512i _mm512_conflict_epi64 (__m512i a)</para>
        ///   <para>  VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> DetectConflicts(Vector512<long> value) => DetectConflicts(value);
        /// <summary>
        ///   <para>__m512i _mm512_conflict_epi64 (__m512i a)</para>
        ///   <para>  VPCONFLICTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> DetectConflicts(Vector512<ulong> value) => DetectConflicts(value);

        /// <summary>
        ///   <para>__m512i _mm512_lzcnt_epi32 (__m512i a)</para>
        ///   <para>  VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<int> LeadingZeroCount(Vector512<int> value) => LeadingZeroCount(value);
        /// <summary>
        ///   <para>__m512i _mm512_lzcnt_epi32 (__m512i a)</para>
        ///   <para>  VPLZCNTD zmm1 {k1}{z}, zmm2/m512/m32bcst</para>
        /// </summary>
        public static Vector512<uint> LeadingZeroCount(Vector512<uint> value) => LeadingZeroCount(value);
        /// <summary>
        ///   <para>__m512i _mm512_lzcnt_epi64 (__m512i a)</para>
        ///   <para>  VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<long> LeadingZeroCount(Vector512<long> value) => LeadingZeroCount(value);
        /// <summary>
        ///   <para>__m512i _mm512_lzcnt_epi64 (__m512i a)</para>
        ///   <para>  VPLZCNTQ zmm1 {k1}{z}, zmm2/m512/m64bcst</para>
        /// </summary>
        public static Vector512<ulong> LeadingZeroCount(Vector512<ulong> value) => LeadingZeroCount(value);
    }
}

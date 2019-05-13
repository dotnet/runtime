// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSE2 hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Sse2 : Sse
    {
        internal Sse2() { }

        public new static bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Sse.X64
        {
            internal X64() { }

            public new static bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __int64 _mm_cvtsd_si64 (__m128d a)
            ///   CVTSD2SI r64, xmm/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<double> value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// __int64 _mm_cvtsi128_si64 (__m128i a)
            ///   MOVQ reg/m64, xmm
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64(Vector128<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvtsi128_si64 (__m128i a)
            ///   MOVQ reg/m64, xmm
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128d _mm_cvtsi64_sd (__m128d a, __int64 b)
            ///   CVTSI2SD xmm, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cvtsi64_si128 (__int64 a)
            ///   MOVQ xmm, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static Vector128<long> ConvertScalarToVector128Int64(long value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __m128i _mm_cvtsi64_si128 (__int64 a)
            ///   MOVQ xmm, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static Vector128<ulong> ConvertScalarToVector128UInt64(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// __int64 _mm_cvttsd_si64 (__m128d a)
            ///   CVTTSD2SI reg, xmm/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static long ConvertToInt64WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// void _mm_stream_si64(__int64 *p, __int64 a)
            ///   MOVNTI m64, r64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static unsafe void StoreNonTemporal(long* address, long value) { throw new PlatformNotSupportedException(); }
            /// <summary>
            /// void _mm_stream_si64(__int64 *p, __int64 a)
            ///   MOVNTI m64, r64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static unsafe void StoreNonTemporal(ulong* address, ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// __m128i _mm_add_epi8 (__m128i a,  __m128i b)
        ///   PADDB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi8 (__m128i a,  __m128i b)
        ///   PADDB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi16 (__m128i a,  __m128i b)
        ///   PADDW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi16 (__m128i a,  __m128i b)
        ///   PADDW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi32 (__m128i a,  __m128i b)
        ///   PADDD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi32 (__m128i a,  __m128i b)
        ///   PADDD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi64 (__m128i a,  __m128i b)
        ///   PADDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_add_epi64 (__m128i a,  __m128i b)
        ///   PADDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_add_pd (__m128d a,  __m128d b)
        ///   ADDPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_add_sd (__m128d a,  __m128d b)
        ///   ADDSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_adds_epi8 (__m128i a,  __m128i b)
        ///   PADDSB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_adds_epu8 (__m128i a,  __m128i b)
        ///   PADDUSB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_adds_epi16 (__m128i a,  __m128i b)
        ///   PADDSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_adds_epu16 (__m128i a,  __m128i b)
        ///   PADDUSW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_and_si128 (__m128i a,  __m128i b)
        ///   PAND xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_and_pd (__m128d a, __m128d b)
        ///   ANDPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_andnot_si128 (__m128i a,  __m128i b)
        ///   PANDN xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_andnot_pd (__m128d a, __m128d b)
        ///   ADDNPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_avg_epu8 (__m128i a,  __m128i b)
        ///   PAVGB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Average(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_avg_epu16 (__m128i a,  __m128i b)
        ///   PAVGW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Average(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpeq_epi8 (__m128i a,  __m128i b)
        ///   PCMPEQB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi8 (__m128i a,  __m128i b)
        ///   PCMPEQB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi16 (__m128i a,  __m128i b)
        ///   PCMPEQW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi16 (__m128i a,  __m128i b)
        ///   PCMPEQW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi32 (__m128i a,  __m128i b)
        ///   PCMPEQD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpeq_epi32 (__m128i a,  __m128i b)
        ///   PCMPEQD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cmpeq_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(0)
        /// </summary>
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comieq_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomieq_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpeq_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(0)
        /// </summary>
        public static Vector128<double> CompareScalarEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmpgt_epi8 (__m128i a,  __m128i b)
        ///   PCMPGTB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpgt_epi16 (__m128i a,  __m128i b)
        ///   PCMPGTW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmpgt_epi32 (__m128i a,  __m128i b)
        ///   PCMPGTD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cmpgt_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(6)
        /// </summary>
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comigt_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomigt_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpgt_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(6)
        /// </summary>
        public static Vector128<double> CompareScalarGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpge_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(5)
        /// </summary>
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comige_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomige_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpge_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(5)
        /// </summary>
        public static Vector128<double> CompareScalarGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cmplt_epi8 (__m128i a,  __m128i b)
        ///   PCMPGTB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmplt_epi16 (__m128i a,  __m128i b)
        ///   PCMPGTW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cmplt_epi32 (__m128i a,  __m128i b)
        ///   PCMPGTD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cmplt_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(1)
        /// </summary>
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comilt_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomilt_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmplt_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(1)
        /// </summary>
        public static Vector128<double> CompareScalarLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmple_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(2)
        /// </summary>
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comile_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomile_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmple_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(2)
        /// </summary>
        public static Vector128<double> CompareScalarLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpneq_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(4)
        /// </summary>
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_comineq_sd (__m128d a, __m128d b)
        ///   COMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarOrderedNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_ucomineq_sd (__m128d a, __m128d b)
        ///   UCOMISD xmm, xmm/m64
        /// </summary>
        public static bool CompareScalarUnorderedNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpneq_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(4)
        /// </summary>
        public static Vector128<double> CompareScalarNotEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpngt_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(2)
        /// </summary>
        public static Vector128<double> CompareNotGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpngt_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(2)
        /// </summary>
        public static Vector128<double> CompareScalarNotGreaterThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnge_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(1)
        /// </summary>
        public static Vector128<double> CompareNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnge_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(1)
        /// </summary>
        public static Vector128<double> CompareScalarNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnlt_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(5)
        /// </summary>
        public static Vector128<double> CompareNotLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnlt_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(5)
        /// </summary>
        public static Vector128<double> CompareScalarNotLessThan(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnle_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(6)
        /// </summary>
        public static Vector128<double> CompareNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpnle_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(6)
        /// </summary>
        public static Vector128<double> CompareScalarNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpord_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(7)
        /// </summary>
        public static Vector128<double> CompareOrdered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpord_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(7)
        /// </summary>
        public static Vector128<double> CompareScalarOrdered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpunord_pd (__m128d a,  __m128d b)
        ///   CMPPD xmm, xmm/m128, imm8(3)
        /// </summary>
        public static Vector128<double> CompareUnordered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cmpunord_sd (__m128d a,  __m128d b)
        ///   CMPSD xmm, xmm/m64, imm8(3)
        /// </summary>
        public static Vector128<double> CompareScalarUnordered(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvtps_epi32 (__m128 a)
        ///   CVTPS2DQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtpd_epi32 (__m128d a)
        ///   CVTPD2DQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cvtepi32_ps (__m128i a)
        ///   CVTDQ2PS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cvtpd_ps (__m128d a)
        ///   CVTPD2PS xmm, xmm/m128
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cvtepi32_pd (__m128i a)
        ///   CVTDQ2PD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cvtps_pd (__m128 a)
        ///   CVTPS2PD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvtsd_si32 (__m128d a)
        ///   CVTSD2SI r32, xmm/m64
        /// </summary>
        public static int ConvertToInt32(Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_cvtsi128_si32 (__m128i a)
        ///   MOVD reg/m32, xmm
        /// </summary>
        public static int ConvertToInt32(Vector128<int> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_cvtsi128_si32 (__m128i a)
        ///   MOVD reg/m32, xmm
        /// </summary>
        public static uint ConvertToUInt32(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_cvtsi32_sd (__m128d a, int b)
        ///   CVTSI2SD xmm, reg/m32
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, int value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_cvtss_sd (__m128d a, __m128 b)
        ///   CVTSS2SD xmm, xmm/m32
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtsi32_si128 (int a)
        ///   MOVD xmm, reg/m32
        /// </summary>
        public static Vector128<int> ConvertScalarToVector128Int32(int value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128 _mm_cvtsd_ss (__m128 a, __m128d b)
        ///   CVTSD2SS xmm, xmm/m64
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvtsi32_si128 (int a)
        ///   MOVD xmm, reg/m32
        /// </summary>
        public static Vector128<uint> ConvertScalarToVector128UInt32(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_cvttps_epi32 (__m128 a)
        ///   CVTTPS2DQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<float> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_cvttpd_epi32 (__m128d a)
        ///   CVTTPD2DQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_cvttsd_si32 (__m128d a)
        ///   CVTTSD2SI reg, xmm/m64
        /// </summary>
        public static int ConvertToInt32WithTruncation(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_div_pd (__m128d a,  __m128d b)
        ///   DIVPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_div_sd (__m128d a,  __m128d b)
        ///   DIVSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_extract_epi16 (__m128i a,  int immediate)
        ///   PEXTRW reg, xmm, imm8
        /// </summary>
        public static ushort Extract(Vector128<ushort> value, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_insert_epi16 (__m128i a,  int i, int immediate)
        ///   PINSRW xmm, reg/m16, imm8
        /// </summary>
        public static Vector128<short> Insert(Vector128<short> value, short data, byte index) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_insert_epi16 (__m128i a,  int i, int immediate)
        ///   PINSRW xmm, reg/m16, imm8
        /// </summary>
        public static Vector128<ushort> Insert(Vector128<ushort> value, ushort data, byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadu_si128 (__m128i const* mem_address)
        ///   MOVDQU xmm, m128
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_loadu_pd (double const* mem_address)
        ///   MOVUPD xmm, m128
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_load_sd (double const* mem_address)
        ///   MOVSD xmm, m64
        /// </summary>
        public static unsafe Vector128<double> LoadScalarVector128(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<sbyte> LoadAlignedVector128(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<byte> LoadAlignedVector128(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<short> LoadAlignedVector128(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<ushort> LoadAlignedVector128(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<int> LoadAlignedVector128(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<uint> LoadAlignedVector128(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<long> LoadAlignedVector128(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_load_si128 (__m128i const* mem_address)
        ///   MOVDQA xmm, m128
        /// </summary>
        public static unsafe Vector128<ulong> LoadAlignedVector128(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_load_pd (double const* mem_address)
        ///   MOVAPD xmm, m128
        /// </summary>
        public static unsafe Vector128<double> LoadAlignedVector128(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_lfence(void)
        ///   LFENCE
        /// </summary>
        public static void LoadFence() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_loadh_pd (__m128d a, double const* mem_addr)
        ///   MOVHPD xmm, m64
        /// </summary>
        public static unsafe Vector128<double> LoadHigh(Vector128<double> lower, double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_loadl_pd (__m128d a, double const* mem_addr)
        ///   MOVLPD xmm, m64
        /// </summary>
        public static unsafe Vector128<double> LoadLow(Vector128<double> upper, double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_loadl_epi32 (__m128i const* mem_addr)
        ///   MOVD xmm, reg/m64
        /// The above native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector128<int> LoadScalarVector128(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadl_epi32 (__m128i const* mem_addr)
        ///   MOVD xmm, reg/m64
        /// The above native signature does not exist. We provide this additional overload for completeness.
        /// </summary>
        public static unsafe Vector128<uint> LoadScalarVector128(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadl_epi64 (__m128i const* mem_addr)
        ///   MOVQ xmm, reg/m64
        /// </summary>
        public static unsafe Vector128<long> LoadScalarVector128(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_loadl_epi64 (__m128i const* mem_addr)
        ///   MOVQ xmm, reg/m64
        /// </summary>
        public static unsafe Vector128<ulong> LoadScalarVector128(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_maskmoveu_si128 (__m128i a,  __m128i mask, char* mem_address)
        ///   MASKMOVDQU xmm, xmm
        /// </summary>
        public static unsafe void MaskMove(Vector128<sbyte> source, Vector128<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_maskmoveu_si128 (__m128i a,  __m128i mask, char* mem_address)
        ///   MASKMOVDQU xmm, xmm
        /// </summary>
        public static unsafe void MaskMove(Vector128<byte> source, Vector128<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_max_epu8 (__m128i a,  __m128i b)
        ///   PMAXUB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_max_epi16 (__m128i a,  __m128i b)
        ///   PMAXSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Max(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_max_pd (__m128d a,  __m128d b)
        ///   MAXPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_max_sd (__m128d a,  __m128d b)
        ///   MAXSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> MaxScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_mfence(void)
        ///   MFENCE
        /// </summary>
        public static void MemoryFence() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_min_epu8 (__m128i a,  __m128i b)
        ///   PMINUB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_min_epi16 (__m128i a,  __m128i b)
        ///   PMINSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Min(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_min_pd (__m128d a,  __m128d b)
        ///   MINPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_min_sd (__m128d a,  __m128d b)
        ///   MINSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> MinScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_move_sd (__m128d a, __m128d b)
        ///   MOVSD xmm, xmm
        /// </summary>
        public static Vector128<double> MoveScalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int _mm_movemask_epi8 (__m128i a)
        ///   PMOVMSKB reg, xmm
        /// </summary>
        public static int MoveMask(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_movemask_epi8 (__m128i a)
        ///   PMOVMSKB reg, xmm
        /// </summary>
        public static int MoveMask(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// int _mm_movemask_pd (__m128d a)
        ///   MOVMSKPD reg, xmm
        /// </summary>
        public static int MoveMask(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_move_epi64 (__m128i a)
        ///   MOVQ xmm, xmm
        /// </summary>
        public static Vector128<long> MoveScalar(Vector128<long> value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_move_epi64 (__m128i a)
        ///   MOVQ xmm, xmm
        /// </summary>
        public static Vector128<ulong> MoveScalar(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mul_epu32 (__m128i a,  __m128i b)
        ///   PMULUDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> Multiply(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_mul_pd (__m128d a,  __m128d b)
        ///   MULPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_mul_sd (__m128d a,  __m128d b)
        ///   MULSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mulhi_epi16 (__m128i a,  __m128i b)
        ///   PMULHW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> MultiplyHigh(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mulhi_epu16 (__m128i a,  __m128i b)
        ///   PMULHUW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> MultiplyHigh(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_madd_epi16 (__m128i a,  __m128i b)
        ///   PMADDWD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> MultiplyAddAdjacent(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_mullo_epi16 (__m128i a,  __m128i b)
        ///   PMULLW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> MultiplyLow(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_mullo_epi16 (__m128i a,  __m128i b)
        ///   PMULLW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> MultiplyLow(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_or_si128 (__m128i a,  __m128i b)
        ///   POR xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_or_pd (__m128d a,  __m128d b)
        ///   ORPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_packs_epi16 (__m128i a,  __m128i b)
        ///   PACKSSWB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> PackSignedSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_packs_epi32 (__m128i a,  __m128i b)
        ///   PACKSSDW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> PackSignedSaturate(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_packus_epi16 (__m128i a,  __m128i b)
        ///   PACKUSWB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> PackUnsignedSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sad_epu8 (__m128i a,  __m128i b)
        ///   PSADBW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> SumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_shuffle_epi32 (__m128i a,  int immediate)
        ///   PSHUFD xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<int> Shuffle(Vector128<int> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_shuffle_epi32 (__m128i a,  int immediate)
        ///   PSHUFD xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<uint> Shuffle(Vector128<uint> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_shuffle_pd (__m128d a,  __m128d b, int immediate)
        ///   SHUFPD xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<double> Shuffle(Vector128<double> left, Vector128<double> right, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_shufflehi_epi16 (__m128i a,  int immediate)
        ///   PSHUFHW xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<short> ShuffleHigh(Vector128<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_shufflehi_epi16 (__m128i a,  int control)
        ///   PSHUFHW xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> ShuffleHigh(Vector128<ushort> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_shufflelo_epi16 (__m128i a,  int control)
        ///   PSHUFLW xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<short> ShuffleLow(Vector128<short> value, byte control) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_shufflelo_epi16 (__m128i a,  int control)
        ///   PSHUFLW xmm, xmm/m128, imm8
        /// </summary>
        public static Vector128<ushort> ShuffleLow(Vector128<ushort> value, byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sll_epi16 (__m128i a, __m128i count)
        ///   PSLLW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sll_epi16 (__m128i a,  __m128i count)
        ///   PSLLW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sll_epi32 (__m128i a, __m128i count)
        ///   PSLLD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sll_epi32 (__m128i a, __m128i count)
        ///   PSLLD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sll_epi64 (__m128i a, __m128i count)
        ///   PSLLQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sll_epi64 (__m128i a, __m128i count)
        ///   PSLLQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_slli_epi16 (__m128i a,  int immediate)
        ///   PSLLW xmm, imm8
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_slli_epi16 (__m128i a,  int immediate)
        ///   PSLLW xmm, imm8
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_slli_epi32 (__m128i a,  int immediate)
        ///   PSLLD xmm, imm8
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_slli_epi32 (__m128i a,  int immediate)
        ///   PSLLD xmm, imm8
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_slli_epi64 (__m128i a,  int immediate)
        ///   PSLLQ xmm, imm8
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_slli_epi64 (__m128i a,  int immediate)
        ///   PSLLQ xmm, imm8
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical128BitLane(Vector128<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical128BitLane(Vector128<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<short> ShiftLeftLogical128BitLane(Vector128<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical128BitLane(Vector128<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<int> ShiftLeftLogical128BitLane(Vector128<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical128BitLane(Vector128<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<long> ShiftLeftLogical128BitLane(Vector128<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bslli_si128 (__m128i a, int imm8)
        ///   PSLLDQ xmm, imm8
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical128BitLane(Vector128<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sra_epi16 (__m128i a, __m128i count)
        ///   PSRAW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sra_epi32 (__m128i a, __m128i count)
        ///   PSRAD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srai_epi16 (__m128i a,  int immediate)
        ///   PSRAW xmm, imm8
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srai_epi32 (__m128i a,  int immediate)
        ///   PSRAD xmm, imm8
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srl_epi16 (__m128i a, __m128i count)
        ///   PSRLW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, Vector128<short> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srl_epi16 (__m128i a, __m128i count)
        ///   PSRLW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, Vector128<ushort> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srl_epi32 (__m128i a, __m128i count)
        ///   PSRLD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, Vector128<int> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srl_epi32 (__m128i a, __m128i count)
        ///   PSRLD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, Vector128<uint> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srl_epi64 (__m128i a, __m128i count)
        ///   PSRLQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, Vector128<long> count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srl_epi64 (__m128i a, __m128i count)
        ///   PSRLQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, Vector128<ulong> count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_srli_epi16 (__m128i a,  int immediate)
        ///   PSRLW xmm, imm8
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srli_epi16 (__m128i a,  int immediate)
        ///   PSRLW xmm, imm8
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srli_epi32 (__m128i a,  int immediate)
        ///   PSRLD xmm, imm8
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srli_epi32 (__m128i a,  int immediate)
        ///   PSRLD xmm, imm8
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srli_epi64 (__m128i a,  int immediate)
        ///   PSRLQ xmm, imm8
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, byte count) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_srli_epi64 (__m128i a,  int immediate)
        ///   PSRLQ xmm, imm8
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, byte count) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical128BitLane(Vector128<sbyte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<byte> ShiftRightLogical128BitLane(Vector128<byte> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<short> ShiftRightLogical128BitLane(Vector128<short> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical128BitLane(Vector128<ushort> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<int> ShiftRightLogical128BitLane(Vector128<int> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<uint> ShiftRightLogical128BitLane(Vector128<uint> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<long> ShiftRightLogical128BitLane(Vector128<long> value, byte numBytes) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_bsrli_si128 (__m128i a, int imm8)
        ///   PSRLDQ xmm, imm8
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical128BitLane(Vector128<ulong> value, byte numBytes) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sqrt_pd (__m128d a)
        ///   SQRTPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Sqrt(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sqrt_sd (__m128d a)
        ///   SQRTSD xmm, xmm/64
        /// The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sqrt_sd (__m128d a, __m128d b)
        ///   SQRTSD xmm, xmm/64
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_store_sd (double* mem_addr, __m128d a)
        ///   MOVSD m64, xmm
        /// </summary>
        public static unsafe void StoreScalar(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storel_epi64 (__m128i* mem_addr, __m128i a)
        ///   MOVQ m64, xmm
        /// </summary>
        public static unsafe void StoreScalar(long* address, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storel_epi64 (__m128i* mem_addr, __m128i a)
        ///   MOVQ m64, xmm
        /// </summary>
        public static unsafe void StoreScalar(ulong* address, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector128<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQA m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_store_pd (double* mem_addr, __m128d a)
        ///   MOVAPD m128, xmm
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector128<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVNTDQ m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_pd (double* mem_addr, __m128d a)
        ///   MOVNTPD m128, xmm
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector128<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(byte* address, Vector128<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(short* address, Vector128<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(ushort* address, Vector128<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(int* address, Vector128<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(uint* address, Vector128<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(long* address, Vector128<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)
        ///   MOVDQU m128, xmm
        /// </summary>
        public static unsafe void Store(ulong* address, Vector128<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_storeu_pd (double* mem_addr, __m128d a)
        ///   MOVUPD m128, xmm
        /// </summary>
        public static unsafe void Store(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storeh_pd (double* mem_addr, __m128d a)
        ///   MOVHPD m64, xmm
        /// </summary>
        public static unsafe void StoreHigh(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_storel_pd (double* mem_addr, __m128d a)
        ///   MOVLPD m64, xmm
        /// </summary>
        public static unsafe void StoreLow(double* address, Vector128<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm_stream_si32(int *p, int a)
        ///   MOVNTI m32, r32
        /// </summary>
        public static unsafe void StoreNonTemporal(int* address, int value) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm_stream_si32(int *p, int a)
        ///   MOVNTI m32, r32
        /// </summary>
        public static unsafe void StoreNonTemporal(uint* address, uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_sub_epi8 (__m128i a,  __m128i b)
        ///   PSUBB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi8 (__m128i a,  __m128i b)
        ///   PSUBB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi16 (__m128i a,  __m128i b)
        ///   PSUBW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi16 (__m128i a,  __m128i b)
        ///   PSUBW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi32 (__m128i a,  __m128i b)
        ///   PSUBD xmm, xmm/m128
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi32 (__m128i a,  __m128i b)
        ///   PSUBD xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi64 (__m128i a,  __m128i b)
        ///   PSUBQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_sub_epi64 (__m128i a,  __m128i b)
        ///   PSUBQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_sub_pd (__m128d a, __m128d b)
        ///   SUBPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128d _mm_sub_sd (__m128d a, __m128d b)
        ///   SUBSD xmm, xmm/m64
        /// </summary>
        public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_subs_epi8 (__m128i a,  __m128i b)
        ///   PSUBSB xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_subs_epi16 (__m128i a,  __m128i b)
        ///   PSUBSW xmm, xmm/m128
        /// </summary>
        public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_subs_epu8 (__m128i a,  __m128i b)
        ///   PSUBUSB xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_subs_epu16 (__m128i a,  __m128i b)
        ///   PSUBUSW xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_unpackhi_epi8 (__m128i a,  __m128i b)
        ///   PUNPCKHBW xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> UnpackHigh(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi8 (__m128i a,  __m128i b)
        ///   PUNPCKHBW xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> UnpackHigh(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi16 (__m128i a,  __m128i b)
        ///   PUNPCKHWD xmm, xmm/m128
        /// </summary>
        public static Vector128<short> UnpackHigh(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi16 (__m128i a,  __m128i b)
        ///   PUNPCKHWD xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> UnpackHigh(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi32 (__m128i a,  __m128i b)
        ///   PUNPCKHDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> UnpackHigh(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi32 (__m128i a,  __m128i b)
        ///   PUNPCKHDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> UnpackHigh(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi64 (__m128i a,  __m128i b)
        ///   PUNPCKHQDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> UnpackHigh(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpackhi_epi64 (__m128i a,  __m128i b)
        ///   PUNPCKHQDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> UnpackHigh(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_unpackhi_pd (__m128d a,  __m128d b)
        ///   UNPCKHPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> UnpackHigh(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_unpacklo_epi8 (__m128i a,  __m128i b)
        ///   PUNPCKLBW xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> UnpackLow(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi8 (__m128i a,  __m128i b)
        ///   PUNPCKLBW xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> UnpackLow(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi16 (__m128i a,  __m128i b)
        ///   PUNPCKLWD xmm, xmm/m128
        /// </summary>
        public static Vector128<short> UnpackLow(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi16 (__m128i a,  __m128i b)
        ///   PUNPCKLWD xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> UnpackLow(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi32 (__m128i a,  __m128i b)
        ///   PUNPCKLDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<int> UnpackLow(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi32 (__m128i a,  __m128i b)
        ///   PUNPCKLDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> UnpackLow(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi64 (__m128i a,  __m128i b)
        ///   PUNPCKLQDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<long> UnpackLow(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_unpacklo_epi64 (__m128i a,  __m128i b)
        ///   PUNPCKLQDQ xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> UnpackLow(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_unpacklo_pd (__m128d a,  __m128d b)
        ///   UNPCKLPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> UnpackLow(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128i _mm_xor_si128 (__m128i a,  __m128i b)
        ///   PXOR xmm, xmm/m128
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m128d _mm_xor_pd (__m128d a,  __m128d b)
        ///   XORPD xmm, xmm/m128
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
    }
}

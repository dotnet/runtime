// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 SSE2 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse2 : Sse
    {
        internal Sse2() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 SSE2 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>__m128d _mm_cvtsi64_sd (__m128d a, __int64 b)</para>
            ///   <para>   CVTSI2SD xmm1,       r/m64</para>
            ///   <para>  VCVTSI2SD xmm1, xmm2, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, long value) => ConvertScalarToVector128Double(upper, value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsi64_si128 (__int64 a)</para>
            ///   <para>   MOVQ xmm1, r/m64</para>
            ///   <para>  VMOVQ xmm1, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<long> ConvertScalarToVector128Int64(long value) => ConvertScalarToVector128Int64(value);
            /// <summary>
            ///   <para>__m128i _mm_cvtsi64_si128 (__int64 a)</para>
            ///   <para>   MOVQ xmm1, r/m64</para>
            ///   <para>  VMOVQ xmm1, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static Vector128<ulong> ConvertScalarToVector128UInt64(ulong value) => ConvertScalarToVector128UInt64(value);

            /// <summary>
            ///   <para>__int64 _mm_cvtsi128_si64 (__m128i a)</para>
            ///   <para>   MOVQ r/m64, xmm1</para>
            ///   <para>  VMOVQ r/m64, xmm1</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<long> value) => ConvertToInt64(value);
            /// <summary>
            ///   <para>__int64 _mm_cvtsd_si64 (__m128d a)</para>
            ///   <para>   CVTSD2SI r64, xmm1/m64</para>
            ///   <para>  VCVTSD2SI r64, xmm1/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64(Vector128<double> value) => ConvertToInt64(value);
            /// <summary>
            ///   <para>__int64 _mm_cvttsd_si64 (__m128d a)</para>
            ///   <para>   CVTTSD2SI r64, xmm1/m64</para>
            ///   <para>  VCVTTSD2SI r64, xmm1/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static long ConvertToInt64WithTruncation(Vector128<double> value) => ConvertToInt64WithTruncation(value);
            /// <summary>
            ///   <para>__int64 _mm_cvtsi128_si64 (__m128i a)</para>
            ///   <para>   MOVQ r/m64, xmm1</para>
            ///   <para>  VMOVQ r/m64, xmm1</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong ConvertToUInt64(Vector128<ulong> value) => ConvertToUInt64(value);

            /// <summary>
            ///   <para>void _mm_stream_si64(__int64 *p, __int64 a)</para>
            ///   <para>  MOVNTI m64, r64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static unsafe void StoreNonTemporal(long* address, long value) => StoreNonTemporal(address, value);
            /// <summary>
            ///   <para>void _mm_stream_si64(__int64 *p, __int64 a)</para>
            ///   <para>  MOVNTI m64, r64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static unsafe void StoreNonTemporal(ulong* address, ulong value) => StoreNonTemporal(address, value);
        }

        /// <summary>
        ///   <para>__m128i _mm_add_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDB xmm1,               xmm2/m128</para>
        ///   <para>  VPADDB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDB xmm1,               xmm2/m128</para>
        ///   <para>  VPADDB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDW xmm1,               xmm2/m128</para>
        ///   <para>  VPADDW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDW xmm1,               xmm2/m128</para>
        ///   <para>  VPADDW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDD xmm1,               xmm2/m128</para>
        ///   <para>  VPADDD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDD xmm1,               xmm2/m128</para>
        ///   <para>  VPADDD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPADDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128i _mm_add_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPADDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) => Add(left, right);
        /// <summary>
        ///   <para>__m128d _mm_add_pd (__m128d a,  __m128d b)</para>
        ///   <para>   ADDPD xmm1,               xmm2/m128</para>
        ///   <para>  VADDPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VADDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

        /// <summary>
        ///   <para>__m128d _mm_add_sd (__m128d a,  __m128d b)</para>
        ///   <para>   ADDSD xmm1,               xmm2/m64</para>
        ///   <para>  VADDSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VADDSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> AddScalar(Vector128<double> left, Vector128<double> right) => AddScalar(left, right);

        /// <summary>
        ///   <para>__m128i _mm_adds_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDSB xmm1,               xmm2/m128</para>
        ///   <para>  VPADDSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_adds_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDUSB xmm1,               xmm2/m128</para>
        ///   <para>  VPADDUSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDUSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_adds_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDSW xmm1,               xmm2/m128</para>
        ///   <para>  VPADDSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right) => AddSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_adds_epu16 (__m128i a,  __m128i b)</para>
        ///   <para>   PADDUSW xmm1,               xmm2/m128</para>
        ///   <para>  VPADDUSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPADDUSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND xmm1,       xmm2/m128</para>
        ///   <para>  VPAND xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND xmm1,       xmm2/m128</para>
        ///   <para>  VPAND xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND xmm1,       xmm2/m128</para>
        ///   <para>  VPAND xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND xmm1,       xmm2/m128</para>
        ///   <para>  VPAND xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND  xmm1,               xmm2/m128</para>
        ///   <para>  VPAND  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND  xmm1,               xmm2/m128</para>
        ///   <para>  VPAND  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND  xmm1,               xmm2/m128</para>
        ///   <para>  VPAND  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);
        /// <summary>
        ///   <para>__m128i _mm_and_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PAND  xmm1,               xmm2/m128</para>
        ///   <para>  VPAND  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);
        /// <summary>
        ///   <para>__m128d _mm_and_pd (__m128d a, __m128d b)</para>
        ///   <para>   ANDPD xmm1,               xmm2/m128</para>
        ///   <para>  VANDPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VANDPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN xmm1,       xmm2/m128</para>
        ///   <para>  VPANDN xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN xmm1,       xmm2/m128</para>
        ///   <para>  VPANDN xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN xmm1,       xmm2/m128</para>
        ///   <para>  VPANDN xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN xmm1,       xmm2/m128</para>
        ///   <para>  VPANDN xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN  xmm1,               xmm2/m128</para>
        ///   <para>  VPANDN  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN  xmm1,               xmm2/m128</para>
        ///   <para>  VPANDN  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDND xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN  xmm1,               xmm2/m128</para>
        ///   <para>  VPANDN  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128i _mm_andnot_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PANDN  xmm1,               xmm2/m128</para>
        ///   <para>  VPANDN  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPANDNQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) => AndNot(left, right);
        /// <summary>
        ///   <para>__m128d _mm_andnot_pd (__m128d a, __m128d b)</para>
        ///   <para>   ANDNPD xmm1,               xmm2/m128</para>
        ///   <para>  VANDNPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VANDNPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);

        /// <summary>
        ///   <para>__m128i _mm_avg_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PAVGB xmm1,               xmm2/m128</para>
        ///   <para>  VPAVGB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPAVGB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Average(Vector128<byte> left, Vector128<byte> right) => Average(left, right);
        /// <summary>
        ///   <para>__m128i _mm_avg_epu16 (__m128i a,  __m128i b)</para>
        ///   <para>   PAVGW xmm1,               xmm2/m128</para>
        ///   <para>  VPAVGW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPAVGW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Average(Vector128<ushort> left, Vector128<ushort> right) => Average(left, right);

        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQB xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQB xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQB xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQB xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQW xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQW xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQD xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQD xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpeq_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPEQD xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPEQD xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) => CompareEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpeq_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(0)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(0)</para>
        /// </summary>
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>__m128i _mm_cmpgt_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTB xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPGTB xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpgt_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTW xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPGTW xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmpgt_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTD xmm1,       xmm2/m128</para>
        ///   <para>  VPCMPGTD xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) => CompareGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpgt_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(1)   ; with swapped operands</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(1)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmpge_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(2)   ; with swapped operands</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(2)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>__m128i _mm_cmplt_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTB xmm1,       xmm2/m128    ; with swapped operands</para>
        ///   <para>  VPCMPGTB xmm1, xmm2, xmm3/m128    ; with swapped operands</para>
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmplt_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTW xmm1,       xmm2/m128    ; with swapped operands</para>
        ///   <para>  VPCMPGTW xmm1, xmm2, xmm3/m128    ; with swapped operands</para>
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m128i _mm_cmplt_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PCMPGTD xmm1,       xmm2/m128    ; with swapped operands</para>
        ///   <para>  VPCMPGTD xmm1, xmm2, xmm3/m128    ; with swapped operands</para>
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) => CompareLessThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmplt_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(1)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(1)</para>
        /// </summary>
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmple_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(2)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(2)</para>
        /// </summary>
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpneq_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(4)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(4)</para>
        /// </summary>
        public static Vector128<double> CompareNotEqual(Vector128<double> left, Vector128<double> right) => CompareNotEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpngt_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(5)   ; with swapped operands</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(5)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareNotGreaterThan(Vector128<double> left, Vector128<double> right) => CompareNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnge_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(6)   ; with swapped operands</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(6)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnlt_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(5)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(5)</para>
        /// </summary>
        public static Vector128<double> CompareNotLessThan(Vector128<double> left, Vector128<double> right) => CompareNotLessThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnle_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(6)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(6)</para>
        /// </summary>
        public static Vector128<double> CompareNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareNotLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpord_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(7)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(7)</para>
        /// </summary>
        public static Vector128<double> CompareOrdered(Vector128<double> left, Vector128<double> right) => CompareOrdered(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmpeq_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(0)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(0)</para>
        /// </summary>
        public static Vector128<double> CompareScalarEqual(Vector128<double> left, Vector128<double> right) => CompareScalarEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpgt_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(1)   ; with swapped operands</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(1)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareScalarGreaterThan(Vector128<double> left, Vector128<double> right) => CompareScalarGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpge_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(2)   ; with swapped operands</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(2)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareScalarGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmplt_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(1)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(1)</para>
        /// </summary>
        public static Vector128<double> CompareScalarLessThan(Vector128<double> left, Vector128<double> right) => CompareScalarLessThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmple_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(2)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(2)</para>
        /// </summary>
        public static Vector128<double> CompareScalarLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpneq_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(4)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(4)</para>
        /// </summary>
        public static Vector128<double> CompareScalarNotEqual(Vector128<double> left, Vector128<double> right) => CompareScalarNotEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpngt_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(5)   ; with swapped operands</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(5)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareScalarNotGreaterThan(Vector128<double> left, Vector128<double> right) => CompareScalarNotGreaterThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnge_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(6)   ; with swapped operands</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(6)   ; with swapped operands</para>
        /// </summary>
        public static Vector128<double> CompareScalarNotGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarNotGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnlt_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(5)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(5)</para>
        /// </summary>
        public static Vector128<double> CompareScalarNotLessThan(Vector128<double> left, Vector128<double> right) => CompareScalarNotLessThan(left, right);
        /// <summary>
        ///   <para>__m128d _mm_cmpnle_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(6)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(6)</para>
        /// </summary>
        public static Vector128<double> CompareScalarNotLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarNotLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmpord_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(7)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(7)</para>
        /// </summary>
        public static Vector128<double> CompareScalarOrdered(Vector128<double> left, Vector128<double> right) => CompareScalarOrdered(left, right);
        /// <summary>
        ///   <para>int _mm_comieq_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; ZF=1 &amp;&amp; PF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedEqual(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comigt_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedGreaterThan(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedGreaterThan(left, right);
        /// <summary>
        ///   <para>int _mm_comige_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; CF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; CF=0</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; CF=0</para>
        /// </summary>
        public static bool CompareScalarOrderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comilt_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; PF=0 &amp;&amp; CF=1</para>
        /// </summary>
        public static bool CompareScalarOrderedLessThan(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedLessThan(left, right);
        /// <summary>
        ///   <para>int _mm_comile_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        /// </summary>
        public static bool CompareScalarOrderedLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_comineq_sd (__m128d a, __m128d b)</para>
        ///   <para>   COMISD xmm1, xmm2/m64        ; ZF=0 || PF=1</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64        ; ZF=0 || PF=1</para>
        ///   <para>  VCOMISD xmm1, xmm2/m64{sae}   ; ZF=0 || PF=1</para>
        /// </summary>
        public static bool CompareScalarOrderedNotEqual(Vector128<double> left, Vector128<double> right) => CompareScalarOrderedNotEqual(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmpunord_sd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPDS xmm1,       xmm2/m64, imm8(3)</para>
        ///   <para>  VCMPDS xmm1, xmm2, xmm3/m64, imm8(3)</para>
        /// </summary>
        public static Vector128<double> CompareScalarUnordered(Vector128<double> left, Vector128<double> right) => CompareScalarUnordered(left, right);
        /// <summary>
        ///   <para>int _mm_ucomieq_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; ZF=1 &amp;&amp; PF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; ZF=1 &amp;&amp; PF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedEqual(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomigt_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; ZF=0 &amp;&amp; CF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; ZF=0 &amp;&amp; CF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThan(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedGreaterThan(left, right);
        /// <summary>
        ///   <para>int _mm_ucomige_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; CF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; CF=0</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; CF=0</para>
        /// </summary>
        public static bool CompareScalarUnorderedGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedGreaterThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomilt_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; PF=0 &amp;&amp; CF=1</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; PF=0 &amp;&amp; CF=1</para>
        /// </summary>
        public static bool CompareScalarUnorderedLessThan(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedLessThan(left, right);
        /// <summary>
        ///   <para>int _mm_ucomile_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; PF=0 &amp;&amp; (ZF=1 || CF=1)</para>
        /// </summary>
        public static bool CompareScalarUnorderedLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedLessThanOrEqual(left, right);
        /// <summary>
        ///   <para>int _mm_ucomineq_sd (__m128d a, __m128d b)</para>
        ///   <para>   UCOMISD xmm1, xmm2/m64       ; ZF=0 || PF=1</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64       ; ZF=0 || PF=1</para>
        ///   <para>  VUCOMISD xmm1, xmm2/m64{sae}  ; ZF=0 || PF=1</para>
        /// </summary>
        public static bool CompareScalarUnorderedNotEqual(Vector128<double> left, Vector128<double> right) => CompareScalarUnorderedNotEqual(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cmpunord_pd (__m128d a,  __m128d b)</para>
        ///   <para>   CMPPD xmm1,       xmm2/m128, imm8(3)</para>
        ///   <para>  VCMPPD xmm1, xmm2, xmm3/m128, imm8(3)</para>
        /// </summary>
        public static Vector128<double> CompareUnordered(Vector128<double> left, Vector128<double> right) => CompareUnordered(left, right);

        /// <summary>
        ///   <para>__m128d _mm_cvtsi32_sd (__m128d a, int b)</para>
        ///   <para>   CVTSI2SD xmm1,       r/m32</para>
        ///   <para>  VCVTSI2SD xmm1, xmm2, r/m32</para>
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, int value) => ConvertScalarToVector128Double(upper, value);
        /// <summary>
        ///   <para>__m128d _mm_cvtss_sd (__m128d a, __m128 b)</para>
        ///   <para>   CVTSS2SD xmm1,       xmm2/m32</para>
        ///   <para>  VCVTSS2SD xmm1, xmm2, xmm3/m32</para>
        /// </summary>
        public static Vector128<double> ConvertScalarToVector128Double(Vector128<double> upper, Vector128<float> value) => ConvertScalarToVector128Double(upper, value);
        /// <summary>
        ///   <para>__m128i _mm_cvtsi32_si128 (int a)</para>
        ///   <para>   MOVD xmm1, r/m32</para>
        ///   <para>  VMOVD xmm1, r/m32</para>
        /// </summary>
        public static Vector128<int> ConvertScalarToVector128Int32(int value) => ConvertScalarToVector128Int32(value);
        /// <summary>
        ///   <para>__m128 _mm_cvtsd_ss (__m128 a, __m128d b)</para>
        ///   <para>   CVTSD2SS xmm1,       xmm2/m64</para>
        ///   <para>  VCVTSD2SS xmm1, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<float> ConvertScalarToVector128Single(Vector128<float> upper, Vector128<double> value) => ConvertScalarToVector128Single(upper, value);
        /// <summary>
        ///   <para>__m128i _mm_cvtsi32_si128 (int a)</para>
        ///   <para>   MOVD xmm1, r/m32</para>
        ///   <para>  VMOVD xmm1, r/m32</para>
        /// </summary>
        public static Vector128<uint> ConvertScalarToVector128UInt32(uint value) => ConvertScalarToVector128UInt32(value);

        /// <summary>
        ///   <para>int _mm_cvtsi128_si32 (__m128i a)</para>
        ///   <para>   MOVD r/m32, xmm1</para>
        ///   <para>  VMOVD r/m32, xmm1</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<int> value) => ConvertToInt32(value);
        /// <summary>
        ///   <para>int _mm_cvtsd_si32 (__m128d a)</para>
        ///   <para>   CVTSD2SI r32, xmm1/m64</para>
        ///   <para>  VCVTSD2SI r32, xmm1/m64</para>
        /// </summary>
        public static int ConvertToInt32(Vector128<double> value) => ConvertToInt32(value);
        /// <summary>
        ///   <para>int _mm_cvttsd_si32 (__m128d a)</para>
        ///   <para>   CVTTSD2SI r32, xmm1/m64</para>
        ///   <para>  VCVTTSD2SI r32, xmm1/m64</para>
        /// </summary>
        public static int ConvertToInt32WithTruncation(Vector128<double> value) => ConvertToInt32WithTruncation(value);
        /// <summary>
        ///   <para>int _mm_cvtsi128_si32 (__m128i a)</para>
        ///   <para>   MOVD r/m32, xmm1</para>
        ///   <para>  VMOVD r/m32, xmm1</para>
        /// </summary>
        public static uint ConvertToUInt32(Vector128<uint> value) => ConvertToUInt32(value);

        /// <summary>
        ///   <para>__m128d _mm_cvtepi32_pd (__m128i a)</para>
        ///   <para>   CVTDQ2PD xmm1,         xmm2/m64</para>
        ///   <para>  VCVTDQ2PD xmm1,         xmm2/m64</para>
        ///   <para>  VCVTDQ2PD xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<int> value) => ConvertToVector128Double(value);
        /// <summary>
        ///   <para>__m128d _mm_cvtps_pd (__m128 a)</para>
        ///   <para>   CVTPS2PD xmm1,         xmm2/m64</para>
        ///   <para>  VCVTPS2PD xmm1,         xmm2/m64</para>
        ///   <para>  VCVTPS2PD xmm1 {k1}{z}, xmm2/m64/m32bcst</para>
        /// </summary>
        public static Vector128<double> ConvertToVector128Double(Vector128<float> value) => ConvertToVector128Double(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtps_epi32 (__m128 a)</para>
        ///   <para>   CVTPS2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPS2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPS2DQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<float> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvtpd_epi32 (__m128d a)</para>
        ///   <para>   CVTPD2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPD2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPD2DQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32(Vector128<double> value) => ConvertToVector128Int32(value);
        /// <summary>
        ///   <para>__m128i _mm_cvttps_epi32 (__m128 a)</para>
        ///   <para>   CVTTPS2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTTPS2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTTPS2DQ xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<float> value) => ConvertToVector128Int32WithTruncation(value);
        /// <summary>
        ///   <para>__m128i _mm_cvttpd_epi32 (__m128d a)</para>
        ///   <para>   CVTTPD2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTTPD2DQ xmm1,         xmm2/m128</para>
        ///   <para>  VCVTTPD2DQ xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<int> ConvertToVector128Int32WithTruncation(Vector128<double> value) => ConvertToVector128Int32WithTruncation(value);
        /// <summary>
        ///   <para>__m128 _mm_cvtepi32_ps (__m128i a)</para>
        ///   <para>   CVTDQ2PS xmm1,         xmm2/m128</para>
        ///   <para>  VCVTDQ2PS xmm1,         xmm2/m128</para>
        ///   <para>  VCVTDQ2PS xmm1 {k1}{z}, xmm2/m128/m32bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<int> value) => ConvertToVector128Single(value);
        /// <summary>
        ///   <para>__m128 _mm_cvtpd_ps (__m128d a)</para>
        ///   <para>   CVTPD2PS xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPD2PS xmm1,         xmm2/m128</para>
        ///   <para>  VCVTPD2PS xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<float> ConvertToVector128Single(Vector128<double> value) => ConvertToVector128Single(value);

        /// <summary>
        ///   <para>__m128d _mm_div_pd (__m128d a,  __m128d b)</para>
        ///   <para>   DIVPD xmm1,               xmm2/m128</para>
        ///   <para>  VDIVPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VDIVPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

        /// <summary>
        ///   <para>__m128d _mm_div_sd (__m128d a,  __m128d b)</para>
        ///   <para>   DIVSD xmm1,       xmm2/m64</para>
        ///   <para>  VDIVSD xmm1, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> DivideScalar(Vector128<double> left, Vector128<double> right) => DivideScalar(left, right);

        /// <summary>
        ///   <para>int _mm_extract_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PEXTRW r/m16, xmm1, imm8</para>
        ///   <para>  VPEXTRW r/m16, xmm1, imm8</para>
        /// </summary>
        public static ushort Extract(Vector128<ushort> value, [ConstantExpected] byte index) => Extract(value, index);

        /// <summary>
        ///   <para>__m128i _mm_insert_epi16 (__m128i a,  int i, int immediate)</para>
        ///   <para>   PINSRW xmm1,       r/m16, imm8</para>
        ///   <para>  VPINSRW xmm1, xmm2, r/m16, imm8</para>
        /// </summary>
        public static Vector128<short> Insert(Vector128<short> value, short data, [ConstantExpected] byte index) => Insert(value, data, index);
        /// <summary>
        ///   <para>__m128i _mm_insert_epi16 (__m128i a,  int i, int immediate)</para>
        ///   <para>   PINSRW xmm1,       r/m16, imm8</para>
        ///   <para>  VPINSRW xmm1, xmm2, r/m16, imm8</para>
        /// </summary>
        public static Vector128<ushort> Insert(Vector128<ushort> value, ushort data, [ConstantExpected] byte index) => Insert(value, data, index);

        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<sbyte> LoadAlignedVector128(sbyte* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<byte> LoadAlignedVector128(byte* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<short> LoadAlignedVector128(short* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<ushort> LoadAlignedVector128(ushort* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<int> LoadAlignedVector128(int* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<uint> LoadAlignedVector128(uint* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<long> LoadAlignedVector128(long* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_load_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA   xmm1,         m128</para>
        ///   <para>  VMOVDQA64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<ulong> LoadAlignedVector128(ulong* address) => LoadAlignedVector128(address);
        /// <summary>
        ///   <para>__m128d _mm_load_pd (double const* mem_address)</para>
        ///   <para>   MOVAPD xmm1,         m128</para>
        ///   <para>  VMOVAPD xmm1,         m128</para>
        ///   <para>  VMOVAPD xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<double> LoadAlignedVector128(double* address) => LoadAlignedVector128(address);

        /// <summary>
        ///   <para>void _mm_lfence(void)</para>
        ///   <para>  LFENCE</para>
        /// </summary>
        public static void LoadFence() => LoadFence();
        /// <summary>
        ///   <para>__m128d _mm_loadh_pd (__m128d a, double const* mem_addr)</para>
        ///   <para>   MOVHPD xmm1,       m64</para>
        ///   <para>  VMOVHPD xmm1, xmm2, m64</para>
        /// </summary>
        public static unsafe Vector128<double> LoadHigh(Vector128<double> lower, double* address) => LoadHigh(lower, address);
        /// <summary>
        ///   <para>__m128d _mm_loadl_pd (__m128d a, double const* mem_addr)</para>
        ///   <para>   MOVLPD xmm1,       m64</para>
        ///   <para>  VMOVLPD xmm1, xmm2, m64</para>
        /// </summary>
        public static unsafe Vector128<double> LoadLow(Vector128<double> upper, double* address) => LoadLow(upper, address);

        /// <summary>
        ///   <para>__m128i _mm_loadu_si32 (void const* mem_addr)</para>
        ///   <para>   MOVD xmm1, m32</para>
        ///   <para>  VMOVD xmm1, m32</para>
        /// </summary>
        public static unsafe Vector128<int> LoadScalarVector128(int* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si32 (void const* mem_addr)</para>
        ///   <para>   MOVD xmm1, m32</para>
        ///   <para>  VMOVD xmm1, m32</para>
        /// </summary>
        public static unsafe Vector128<uint> LoadScalarVector128(uint* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadl_epi64 (__m128i const* mem_addr)</para>
        ///   <para>   MOVQ xmm1, m64</para>
        ///   <para>  VMOVQ xmm1, m64</para>
        /// </summary>
        public static unsafe Vector128<long> LoadScalarVector128(long* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadl_epi64 (__m128i const* mem_addr)</para>
        ///   <para>   MOVQ xmm1, m64</para>
        ///   <para>  VMOVQ xmm1, m64</para>
        /// </summary>
        public static unsafe Vector128<ulong> LoadScalarVector128(ulong* address) => LoadScalarVector128(address);
        /// <summary>
        ///   <para>__m128d _mm_load_sd (double const* mem_address)</para>
        ///   <para>   MOVSD xmm1,      m64</para>
        ///   <para>  VMOVSD xmm1,      m64</para>
        ///   <para>  VMOVSD xmm1 {k1}, m64</para>
        /// </summary>
        public static unsafe Vector128<double> LoadScalarVector128(double* address) => LoadScalarVector128(address);

        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU  xmm1,         m128</para>
        ///   <para>  VMOVDQU  xmm1,         m128</para>
        ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU  xmm1,         m128</para>
        ///   <para>  VMOVDQU  xmm1,         m128</para>
        ///   <para>  VMOVDQU8 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU16 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU16 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU32 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128i _mm_loadu_si128 (__m128i const* mem_address)</para>
        ///   <para>   MOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU   xmm1,         m128</para>
        ///   <para>  VMOVDQU64 xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) => LoadVector128(address);
        /// <summary>
        ///   <para>__m128d _mm_loadu_pd (double const* mem_address)</para>
        ///   <para>   MOVUPD xmm1,         m128</para>
        ///   <para>  VMOVUPD xmm1,         m128</para>
        ///   <para>  VMOVUPD xmm1 {k1}{z}, m128</para>
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) => LoadVector128(address);

        /// <summary>
        ///   <para>void _mm_maskmoveu_si128 (__m128i a,  __m128i mask, char* mem_address)</para>
        ///   <para>   MASKMOVDQU xmm1, xmm2    ; Address: EDI/RDI</para>
        ///   <para>  VMASKMOVDQU xmm1, xmm2    ; Address: EDI/RDI</para>
        /// </summary>
        public static unsafe void MaskMove(Vector128<sbyte> source, Vector128<sbyte> mask, sbyte* address) => MaskMove(source, mask, address);
        /// <summary>
        ///   <para>void _mm_maskmoveu_si128 (__m128i a,  __m128i mask, char* mem_address)</para>
        ///   <para>   MASKMOVDQU xmm1, xmm2    ; Address: EDI/RDI</para>
        ///   <para>  VMASKMOVDQU xmm1, xmm2    ; Address: EDI/RDI</para>
        /// </summary>
        public static unsafe void MaskMove(Vector128<byte> source, Vector128<byte> mask, byte* address) => MaskMove(source, mask, address);

        /// <summary>
        ///   <para>__m128i _mm_max_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PMAXUB xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXUB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXUB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right) => Max(left, right);
        /// <summary>
        ///   <para>__m128i _mm_max_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMAXSW xmm1,               xmm2/m128</para>
        ///   <para>  VPMAXSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMAXSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Max(Vector128<short> left, Vector128<short> right) => Max(left, right);
        /// <summary>
        ///   <para>__m128d _mm_max_pd (__m128d a,  __m128d b)</para>
        ///   <para>   MAXPD xmm1,               xmm2/m128</para>
        ///   <para>  VMAXPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMAXPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        /// <summary>
        ///   <para>__m128d _mm_max_sd (__m128d a,  __m128d b)</para>
        ///   <para>   MAXSD xmm1,       xmm2/m64</para>
        ///   <para>  VMAXSD xmm1, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> MaxScalar(Vector128<double> left, Vector128<double> right) => MaxScalar(left, right);

        /// <summary>
        ///   <para>void _mm_mfence(void)</para>
        ///   <para>  MFENCE</para>
        /// </summary>
        public static void MemoryFence() => MemoryFence();

        /// <summary>
        ///   <para>__m128i _mm_min_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PMINUB xmm1,               xmm2/m128</para>
        ///   <para>  VPMINUB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINUB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right) => Min(left, right);
        /// <summary>
        ///   <para>__m128i _mm_min_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMINSW xmm1,               xmm2/m128</para>
        ///   <para>  VPMINSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMINSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Min(Vector128<short> left, Vector128<short> right) => Min(left, right);
        /// <summary>
        ///   <para>__m128d _mm_min_pd (__m128d a,  __m128d b)</para>
        ///   <para>   MINPD xmm1,               xmm2/m128</para>
        ///   <para>  VMINPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMINPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

        /// <summary>
        ///   <para>__m128d _mm_min_sd (__m128d a,  __m128d b)</para>
        ///   <para>   MINSD xmm1,       xmm2/m64</para>
        ///   <para>  VMINSD xmm1, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> MinScalar(Vector128<double> left, Vector128<double> right) => MinScalar(left, right);

        /// <summary>
        ///   <para>int _mm_movemask_epi8 (__m128i a)</para>
        ///   <para>   PMOVMSKB r32, xmm1</para>
        ///   <para>  VPMOVMSKB r32, xmm1</para>
        /// </summary>
        public static int MoveMask(Vector128<sbyte> value) => MoveMask(value);
        /// <summary>
        ///   <para>int _mm_movemask_epi8 (__m128i a)</para>
        ///   <para>   PMOVMSKB r32, xmm1</para>
        ///   <para>  VPMOVMSKB r32, xmm1</para>
        /// </summary>
        public static int MoveMask(Vector128<byte> value) => MoveMask(value);
        /// <summary>
        ///   <para>int _mm_movemask_pd (__m128d a)</para>
        ///   <para>   MOVMSKPD r32, xmm1</para>
        ///   <para>  VMOVMSKPD r32, xmm1</para>
        /// </summary>
        public static int MoveMask(Vector128<double> value) => MoveMask(value);

        /// <summary>
        ///   <para>__m128i _mm_move_epi64 (__m128i a)</para>
        ///   <para>   MOVQ xmm1, xmm2</para>
        ///   <para>  VMOVQ xmm1, xmm2</para>
        /// </summary>
        public static Vector128<long> MoveScalar(Vector128<long> value) => MoveScalar(value);
        /// <summary>
        ///   <para>__m128i _mm_move_epi64 (__m128i a)</para>
        ///   <para>   MOVQ xmm1, xmm2</para>
        ///   <para>  VMOVQ xmm1, xmm2</para>
        /// </summary>
        public static Vector128<ulong> MoveScalar(Vector128<ulong> value) => MoveScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_move_sd (__m128d a, __m128d b)</para>
        ///   <para>   MOVSD xmm1,               xmm2</para>
        ///   <para>  VMOVSD xmm1,         xmm2, xmm3</para>
        ///   <para>  VMOVSD xmm1 {k1}{z}, xmm2, xmm3</para>
        /// </summary>
        public static Vector128<double> MoveScalar(Vector128<double> upper, Vector128<double> value) => MoveScalar(upper, value);

        /// <summary>
        ///   <para>__m128i _mm_mul_epu32 (__m128i a,  __m128i b)</para>
        ///   <para>   PMULUDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPMULUDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULUDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Multiply(Vector128<uint> left, Vector128<uint> right) => Multiply(left, right);
        /// <summary>
        ///   <para>__m128d _mm_mul_pd (__m128d a,  __m128d b)</para>
        ///   <para>   MULPD xmm1,               xmm2/m128</para>
        ///   <para>  VMULPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VMULPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

        /// <summary>
        ///   <para>__m128i _mm_madd_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMADDWD xmm1,               xmm2/m128</para>
        ///   <para>  VPMADDWD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMADDWD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> MultiplyAddAdjacent(Vector128<short> left, Vector128<short> right) => MultiplyAddAdjacent(left, right);

        /// <summary>
        ///   <para>__m128i _mm_mulhi_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMULHW xmm1,               xmm2/m128</para>
        ///   <para>  VPMULHW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULHW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> MultiplyHigh(Vector128<short> left, Vector128<short> right) => MultiplyHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_mulhi_epu16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMULHUW xmm1,               xmm2/m128</para>
        ///   <para>  VPMULHUW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULHUW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> MultiplyHigh(Vector128<ushort> left, Vector128<ushort> right) => MultiplyHigh(left, right);

        /// <summary>
        ///   <para>__m128i _mm_mullo_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMULLW xmm1,               xmm2/m128</para>
        ///   <para>  VPMULLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> MultiplyLow(Vector128<short> left, Vector128<short> right) => MultiplyLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_mullo_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PMULLW xmm1,               xmm2/m128</para>
        ///   <para>  VPMULLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPMULLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> MultiplyLow(Vector128<ushort> left, Vector128<ushort> right) => MultiplyLow(left, right);

        /// <summary>
        ///   <para>__m128d _mm_mul_sd (__m128d a,  __m128d b)</para>
        ///   <para>   MULSD xmm1,       xmm2/m64</para>
        ///   <para>  VMULSD xmm1, xmm2, xmm3/m64</para>
        /// </summary>
        public static Vector128<double> MultiplyScalar(Vector128<double> left, Vector128<double> right) => MultiplyScalar(left, right);

        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR xmm1,       xmm2/m128</para>
        ///   <para>  VPOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR xmm1,       xmm2/m128</para>
        ///   <para>  VPOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR xmm1,       xmm2/m128</para>
        ///   <para>  VPOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR xmm1,       xmm2/m128</para>
        ///   <para>  VPOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR  xmm1,               xmm2/m128</para>
        ///   <para>  VPOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPORD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR  xmm1,               xmm2/m128</para>
        ///   <para>  VPOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPORD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR  xmm1,               xmm2/m128</para>
        ///   <para>  VPOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPORQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128i _mm_or_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   POR  xmm1,               xmm2/m128</para>
        ///   <para>  VPOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPORQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);
        /// <summary>
        ///   <para>__m128d _mm_or_pd (__m128d a,  __m128d b)</para>
        ///   <para>   ORPD xmm1,               xmm2/m128</para>
        ///   <para>  VORPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VORPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        ///   <para>__m128i _mm_packs_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PACKSSWB xmm1,               xmm2/m128</para>
        ///   <para>  VPACKSSWB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPACKSSWB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> PackSignedSaturate(Vector128<short> left, Vector128<short> right) => PackSignedSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_packs_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PACKSSDW xmm1,               xmm2/m128</para>
        ///   <para>  VPACKSSDW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPACKSSDW xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<short> PackSignedSaturate(Vector128<int> left, Vector128<int> right) => PackSignedSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_packus_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PACKUSWB xmm1,               xmm2/m128</para>
        ///   <para>  VPACKUSWB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPACKUSWB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> PackUnsignedSaturate(Vector128<short> left, Vector128<short> right) => PackUnsignedSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_sll_epi16 (__m128i a, __m128i count)</para>
        ///   <para>   PSLLW xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, Vector128<short> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sll_epi16 (__m128i a,  __m128i count)</para>
        ///   <para>   PSLLW xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, Vector128<ushort> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sll_epi32 (__m128i a, __m128i count)</para>
        ///   <para>   PSLLD xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, Vector128<int> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sll_epi32 (__m128i a, __m128i count)</para>
        ///   <para>   PSLLD xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, Vector128<uint> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sll_epi64 (__m128i a, __m128i count)</para>
        ///   <para>   PSLLQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, Vector128<long> count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sll_epi64 (__m128i a, __m128i count)</para>
        ///   <para>   PSLLQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSLLQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSLLQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, Vector128<ulong> count) => ShiftLeftLogical(value, count);

        /// <summary>
        ///   <para>__m128i _mm_slli_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLW xmm1,               imm8</para>
        ///   <para>  VPSLLW xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLW xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_slli_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLW xmm1,               imm8</para>
        ///   <para>  VPSLLW xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLW xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_slli_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLD xmm1,               imm8</para>
        ///   <para>  VPSLLD xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLD xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_slli_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLD xmm1,               imm8</para>
        ///   <para>  VPSLLD xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLD xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_slli_epi64 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLQ xmm1,               imm8</para>
        ///   <para>  VPSLLQ xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLQ xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_slli_epi64 (__m128i a,  int immediate)</para>
        ///   <para>   PSLLQ xmm1,               imm8</para>
        ///   <para>  VPSLLQ xmm1,         xmm2, imm8</para>
        ///   <para>  VPSLLQ xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, [ConstantExpected] byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical128BitLane(Vector128<sbyte> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical128BitLane(Vector128<byte> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<short> ShiftLeftLogical128BitLane(Vector128<short> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical128BitLane(Vector128<ushort> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<int> ShiftLeftLogical128BitLane(Vector128<int> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical128BitLane(Vector128<uint> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<long> ShiftLeftLogical128BitLane(Vector128<long> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bslli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSLLDQ xmm1,            imm8</para>
        ///   <para>  VPSLLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSLLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical128BitLane(Vector128<ulong> value, [ConstantExpected] byte numBytes) => ShiftLeftLogical128BitLane(value, numBytes);

        /// <summary>
        ///   <para>__m128i _mm_sra_epi16 (__m128i a, __m128i count)</para>
        ///   <para>   PSRAW xmm1,               xmm2/m128</para>
        ///   <para>  VPSRAW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRAW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, Vector128<short> count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>__m128i _mm_sra_epi32 (__m128i a, __m128i count)</para>
        ///   <para>   PSRAD xmm1,               xmm2/m128</para>
        ///   <para>  VPSRAD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRAD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, Vector128<int> count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   <para>__m128i _mm_srai_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSRAW xmm1,               imm8</para>
        ///   <para>  VPSRAW xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRAW xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srai_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSRAD xmm1,               imm8</para>
        ///   <para>  VPSRAD xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRAD xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, [ConstantExpected] byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        ///   <para>__m128i _mm_srl_epi16 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLW xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, Vector128<short> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srl_epi16 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLW xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, Vector128<ushort> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srl_epi32 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLD xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, Vector128<int> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srl_epi32 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLD xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, Vector128<uint> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srl_epi64 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, Vector128<long> count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srl_epi64 (__m128i a, __m128i count)</para>
        ///   <para>   PSRLQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSRLQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSRLQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, Vector128<ulong> count) => ShiftRightLogical(value, count);

        /// <summary>
        ///   <para>__m128i _mm_srli_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLW xmm1,               imm8</para>
        ///   <para>  VPSRLW xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLW xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srli_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLW xmm1,               imm8</para>
        ///   <para>  VPSRLW xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLW xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srli_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLD xmm1,               imm8</para>
        ///   <para>  VPSRLD xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLD xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srli_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLD xmm1,               imm8</para>
        ///   <para>  VPSRLD xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLD xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srli_epi64 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLQ xmm1,               imm8</para>
        ///   <para>  VPSRLQ xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLQ xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);
        /// <summary>
        ///   <para>__m128i _mm_srli_epi64 (__m128i a,  int immediate)</para>
        ///   <para>   PSRLQ xmm1,               imm8</para>
        ///   <para>  VPSRLQ xmm1,         xmm2, imm8</para>
        ///   <para>  VPSRLQ xmm1 {k1}{z}, xmm2, imm8</para>
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, [ConstantExpected] byte count) => ShiftRightLogical(value, count);

        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical128BitLane(Vector128<sbyte> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<byte> ShiftRightLogical128BitLane(Vector128<byte> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<short> ShiftRightLogical128BitLane(Vector128<short> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical128BitLane(Vector128<ushort> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<int> ShiftRightLogical128BitLane(Vector128<int> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<uint> ShiftRightLogical128BitLane(Vector128<uint> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<long> ShiftRightLogical128BitLane(Vector128<long> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);
        /// <summary>
        ///   <para>__m128i _mm_bsrli_si128 (__m128i a, int imm8)</para>
        ///   <para>   PSRLDQ xmm1,            imm8</para>
        ///   <para>  VPSRLDQ xmm1, xmm2/m128, imm8</para>
        ///   <para>This intrinsic generates PSRLDQ that operates over bytes rather than elements of the vectors.</para>
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical128BitLane(Vector128<ulong> value, [ConstantExpected] byte numBytes) => ShiftRightLogical128BitLane(value, numBytes);

        /// <summary>
        ///   <para>__m128i _mm_shuffle_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSHUFD xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPSHUFD xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPSHUFD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<int> Shuffle(Vector128<int> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        ///   <para>__m128i _mm_shuffle_epi32 (__m128i a,  int immediate)</para>
        ///   <para>   PSHUFD xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPSHUFD xmm1,         xmm2/m128,         imm8</para>
        ///   <para>  VPSHUFD xmm1 {k1}{z}, xmm2/m128/m32bcst, imm8</para>
        /// </summary>
        public static Vector128<uint> Shuffle(Vector128<uint> value, [ConstantExpected] byte control) => Shuffle(value, control);
        /// <summary>
        ///   <para>__m128d _mm_shuffle_pd (__m128d a,  __m128d b, int immediate)</para>
        ///   <para>   SHUFPD xmm1,               xmm2/m128,         imm8</para>
        ///   <para>  VSHUFPD xmm1,         xmm2, xmm3/m128,         imm8</para>
        ///   <para>  VSHUFPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst, imm8</para>
        /// </summary>
        public static Vector128<double> Shuffle(Vector128<double> left, Vector128<double> right, [ConstantExpected] byte control) => Shuffle(left, right, control);

        /// <summary>
        ///   <para>__m128i _mm_shufflehi_epi16 (__m128i a,  int immediate)</para>
        ///   <para>   PSHUFHW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFHW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFHW xmm1 {k1}{z}, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<short> ShuffleHigh(Vector128<short> value, [ConstantExpected] byte control) => ShuffleHigh(value, control);
        /// <summary>
        ///   <para>__m128i _mm_shufflehi_epi16 (__m128i a,  int control)</para>
        ///   <para>   PSHUFHW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFHW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFHW xmm1 {k1}{z}, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<ushort> ShuffleHigh(Vector128<ushort> value, [ConstantExpected] byte control) => ShuffleHigh(value, control);

        /// <summary>
        ///   <para>__m128i _mm_shufflelo_epi16 (__m128i a,  int control)</para>
        ///   <para>   PSHUFLW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFLW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFLW xmm1 {k1}{z}, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<short> ShuffleLow(Vector128<short> value, [ConstantExpected] byte control) => ShuffleLow(value, control);
        /// <summary>
        ///   <para>__m128i _mm_shufflelo_epi16 (__m128i a,  int control)</para>
        ///   <para>   PSHUFLW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFLW xmm1,         xmm2/m128, imm8</para>
        ///   <para>  VPSHUFLW xmm1 {k1}{z}, xmm2/m128, imm8</para>
        /// </summary>
        public static Vector128<ushort> ShuffleLow(Vector128<ushort> value, [ConstantExpected] byte control) => ShuffleLow(value, control);

        /// <summary>
        ///   <para>__m128d _mm_sqrt_pd (__m128d a)</para>
        ///   <para>   SQRTPD xmm1,         xmm2/m128</para>
        ///   <para>  VSQRTPD xmm1,         xmm2/m128</para>
        ///   <para>  VSQRTPD xmm1 {k1}{z}, xmm2/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

        /// <summary>
        ///   <para>__m128d _mm_sqrt_sd (__m128d a)</para>
        ///   <para>   SQRTSD xmm1,               xmm2/m64</para>
        ///   <para>  VSQRTSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VSQRTSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        ///   <para>The above native signature does not exist. We provide this additional overload for the recommended use case of this intrinsic.</para>
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> value) => SqrtScalar(value);
        /// <summary>
        ///   <para>__m128d _mm_sqrt_sd (__m128d a, __m128d b)</para>
        ///   <para>   SQRTSD xmm1,               xmm2/m64</para>
        ///   <para>  VSQRTSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VSQRTSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> SqrtScalar(Vector128<double> upper, Vector128<double> value) => SqrtScalar(upper, value);

        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU  m128,         xmm1</para>
        ///   <para>  VMOVDQU  m128,         xmm1</para>
        ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector128<sbyte> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU  m128,         xmm1</para>
        ///   <para>  VMOVDQU  m128,         xmm1</para>
        ///   <para>  VMOVDQU8 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(byte* address, Vector128<byte> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(short* address, Vector128<short> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU16 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(ushort* address, Vector128<ushort> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(int* address, Vector128<int> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(uint* address, Vector128<uint> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(long* address, Vector128<long> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU   m128,         xmm1</para>
        ///   <para>  VMOVDQU64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(ulong* address, Vector128<ulong> source) => Store(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_pd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVUPD m128,         xmm1</para>
        ///   <para>  VMOVUPD m128,         xmm1</para>
        ///   <para>  VMOVUPD m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void Store(double* address, Vector128<double> source) => Store(address, source);

        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector128<sbyte> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector128<byte> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector128<short> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector128<ushort> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector128<int> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA32 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector128<uint> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector128<long> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA   m128,         xmm1</para>
        ///   <para>  VMOVDQA64 m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector128<ulong> source) => StoreAligned(address, source);
        /// <summary>
        ///   <para>void _mm_store_pd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVAPD m128,         xmm1</para>
        ///   <para>  VMOVAPD m128,         xmm1</para>
        ///   <para>  VMOVAPD m128 {k1}{z}, xmm1</para>
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector128<double> source) => StoreAligned(address, source);

        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector128<sbyte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector128<byte> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector128<short> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector128<ushort> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector128<int> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector128<uint> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector128<long> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_si128 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVNTDQ m128, xmm1</para>
        ///   <para>  VMOVNTDQ m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector128<ulong> source) => StoreAlignedNonTemporal(address, source);
        /// <summary>
        ///   <para>void _mm_stream_pd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVNTPD m128, xmm1</para>
        ///   <para>  VMOVNTPD m128, xmm1</para>
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector128<double> source) => StoreAlignedNonTemporal(address, source);

        /// <summary>
        ///   <para>void _mm_storeh_pd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVHPD m64, xmm1</para>
        ///   <para>  VMOVHPD m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreHigh(double* address, Vector128<double> source) => StoreHigh(address, source);
        /// <summary>
        ///   <para>void _mm_storel_pd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVLPD m64, xmm1</para>
        ///   <para>  VMOVLPD m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreLow(double* address, Vector128<double> source) => StoreLow(address, source);

        /// <summary>
        ///   <para>void _mm_stream_si32(int *p, int a)</para>
        ///   <para>  MOVNTI m32, r32</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(int* address, int value) => StoreNonTemporal(address, value);
        /// <summary>
        ///   <para>void _mm_stream_si32(int *p, int a)</para>
        ///   <para>  MOVNTI m32, r32</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(uint* address, uint value) => StoreNonTemporal(address, value);

        /// <summary>
        ///   <para>void _mm_storeu_si32 (void* mem_addr, __m128i a)</para>
        ///   <para>   MOVD m32, xmm1</para>
        ///   <para>  VMOVD m32, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(int* address, Vector128<int> source) => StoreScalar(address, source);
        /// <summary>
        ///   <para>void _mm_storeu_si32 (void* mem_addr, __m128i a)</para>
        ///   <para>   MOVD m32, xmm1</para>
        ///   <para>  VMOVD m32, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(uint* address, Vector128<uint> source) => StoreScalar(address, source);
        /// <summary>
        ///   <para>void _mm_storel_epi64 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVQ m64, xmm1</para>
        ///   <para>  VMOVQ m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(long* address, Vector128<long> source) => StoreScalar(address, source);
        /// <summary>
        ///   <para>void _mm_storel_epi64 (__m128i* mem_addr, __m128i a)</para>
        ///   <para>   MOVQ m64, xmm1</para>
        ///   <para>  VMOVQ m64, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(ulong* address, Vector128<ulong> source) => StoreScalar(address, source);
        /// <summary>
        ///   <para>void _mm_store_sd (double* mem_addr, __m128d a)</para>
        ///   <para>   MOVSD m64,      xmm1</para>
        ///   <para>  VMOVSD m64,      xmm1</para>
        ///   <para>  VMOVSD m64 {k1}, xmm1</para>
        /// </summary>
        public static unsafe void StoreScalar(double* address, Vector128<double> source) => StoreScalar(address, source);

        /// <summary>
        ///   <para>__m128i _mm_sub_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBB xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBB xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBW xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBW xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBD xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBD xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128i _mm_sub_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBQ xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBQ xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);
        /// <summary>
        ///   <para>__m128d _mm_sub_pd (__m128d a, __m128d b)</para>
        ///   <para>   SUBPD xmm1,               xmm2/m128</para>
        ///   <para>  VSUBPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VSUBPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

        /// <summary>
        ///   <para>__m128d _mm_sub_sd (__m128d a, __m128d b)</para>
        ///   <para>   SUBSD xmm1,               xmm2/m64</para>
        ///   <para>  VSUBSD xmm1,         xmm2, xmm3/m64</para>
        ///   <para>  VSUBSD xmm1 {k1}{z}, xmm2, xmm3/m64{er}</para>
        /// </summary>
        public static Vector128<double> SubtractScalar(Vector128<double> left, Vector128<double> right) => SubtractScalar(left, right);

        /// <summary>
        ///   <para>__m128i _mm_subs_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBSB xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_subs_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBSW xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_subs_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBUSB xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBUSB xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBUSB xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right) => SubtractSaturate(left, right);
        /// <summary>
        ///   <para>__m128i _mm_subs_epu16 (__m128i a,  __m128i b)</para>
        ///   <para>   PSUBUSW xmm1,               xmm2/m128</para>
        ///   <para>  VPSUBUSW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSUBUSW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>__m128i _mm_sad_epu8 (__m128i a,  __m128i b)</para>
        ///   <para>   PSADBW xmm1,               xmm2/m128</para>
        ///   <para>  VPSADBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPSADBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> SumAbsoluteDifferences(Vector128<byte> left, Vector128<byte> right) => SumAbsoluteDifferences(left, right);

        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHBW xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> UnpackHigh(Vector128<byte> left, Vector128<byte> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHBW xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> UnpackHigh(Vector128<sbyte> left, Vector128<sbyte> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHWD xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHWD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHWD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> UnpackHigh(Vector128<short> left, Vector128<short> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHWD xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHWD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHWD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> UnpackHigh(Vector128<ushort> left, Vector128<ushort> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> UnpackHigh(Vector128<int> left, Vector128<int> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> UnpackHigh(Vector128<uint> left, Vector128<uint> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHQDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHQDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHQDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> UnpackHigh(Vector128<long> left, Vector128<long> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpackhi_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKHQDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKHQDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKHQDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> UnpackHigh(Vector128<ulong> left, Vector128<ulong> right) => UnpackHigh(left, right);
        /// <summary>
        ///   <para>__m128d _mm_unpackhi_pd (__m128d a,  __m128d b)</para>
        ///   <para>   UNPCKHPD xmm1,               xmm2/m128</para>
        ///   <para>  VUNPCKHPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VUNPCKHPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> UnpackHigh(Vector128<double> left, Vector128<double> right) => UnpackHigh(left, right);

        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLBW xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> UnpackLow(Vector128<byte> left, Vector128<byte> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi8 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLBW xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLBW xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLBW xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> UnpackLow(Vector128<sbyte> left, Vector128<sbyte> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLWD xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLWD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLWD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> UnpackLow(Vector128<short> left, Vector128<short> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi16 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLWD xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLWD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLWD xmm1 {k1}{z}, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> UnpackLow(Vector128<ushort> left, Vector128<ushort> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> UnpackLow(Vector128<int> left, Vector128<int> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi32 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> UnpackLow(Vector128<uint> left, Vector128<uint> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLQDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLQDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLQDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> UnpackLow(Vector128<long> left, Vector128<long> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128i _mm_unpacklo_epi64 (__m128i a,  __m128i b)</para>
        ///   <para>   PUNPCKLQDQ xmm1,               xmm2/m128</para>
        ///   <para>  VPUNPCKLQDQ xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPUNPCKLQDQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> UnpackLow(Vector128<ulong> left, Vector128<ulong> right) => UnpackLow(left, right);
        /// <summary>
        ///   <para>__m128d _mm_unpacklo_pd (__m128d a,  __m128d b)</para>
        ///   <para>   UNPCKLPD xmm1,               xmm2/m128</para>
        ///   <para>  VUNPCKLPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VUNPCKLPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> UnpackLow(Vector128<double> left, Vector128<double> right) => UnpackLow(left, right);

        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR xmm1,       xmm2/m128</para>
        ///   <para>  VPXOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR xmm1,       xmm2/m128</para>
        ///   <para>  VPXOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR xmm1,       xmm2/m128</para>
        ///   <para>  VPXOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR xmm1,       xmm2/m128</para>
        ///   <para>  VPXOR xmm1, xmm2, xmm3/m128</para>
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR  xmm1,               xmm2/m128</para>
        ///   <para>  VPXOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPXORD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR  xmm1,               xmm2/m128</para>
        ///   <para>  VPXOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPXORD xmm1 {k1}{z}, xmm2, xmm3/m128/m32bcst</para>
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR  xmm1,               xmm2/m128</para>
        ///   <para>  VPXOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPXORQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128i _mm_xor_si128 (__m128i a,  __m128i b)</para>
        ///   <para>   PXOR  xmm1,               xmm2/m128</para>
        ///   <para>  VPXOR  xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VPXORQ xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);
        /// <summary>
        ///   <para>__m128d _mm_xor_pd (__m128d a,  __m128d b)</para>
        ///   <para>   XORPD xmm1,               xmm2/m128</para>
        ///   <para>  VXORPD xmm1,         xmm2, xmm3/m128</para>
        ///   <para>  VXORPD xmm1 {k1}{z}, xmm2, xmm3/m128/m64bcst</para>
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel SSE3 hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sse3 : Sse2
    {
        internal Sse3() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class X64 : Sse2.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m128 _mm_addsub_ps (__m128 a, __m128 b)
        ///    ADDSUBPS xmm1,       xmm2/m128
        ///   VADDSUBPS xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<float> AddSubtract(Vector128<float> left, Vector128<float> right) => AddSubtract(left, right);
        /// <summary>
        /// __m128d _mm_addsub_pd (__m128d a, __m128d b)
        ///    ADDSUBPD xmm1,       xmm2/m128
        ///   VADDSUBPD xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<double> AddSubtract(Vector128<double> left, Vector128<double> right) => AddSubtract(left, right);

        /// <summary>
        /// __m128 _mm_hadd_ps (__m128 a, __m128 b)
        ///    HADDPS xmm1,       xmm2/m128
        ///   VHADDPS xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<float> HorizontalAdd(Vector128<float> left, Vector128<float> right) => HorizontalAdd(left, right);
        /// <summary>
        /// __m128d _mm_hadd_pd (__m128d a, __m128d b)
        ///    HADDPD xmm1,       xmm2/m128
        ///   VHADDPD xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<double> HorizontalAdd(Vector128<double> left, Vector128<double> right) => HorizontalAdd(left, right);

        /// <summary>
        /// __m128 _mm_hsub_ps (__m128 a, __m128 b)
        ///    HSUBPS xmm1,       xmm2/m128
        ///   VHSUBPS xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<float> HorizontalSubtract(Vector128<float> left, Vector128<float> right) => HorizontalSubtract(left, right);
        /// <summary>
        /// __m128d _mm_hsub_pd (__m128d a, __m128d b)
        ///    HSUBPD xmm1,       xmm2/m128
        ///   VHSUBPD xmm1, xmm2, xmm3/m128
        /// </summary>
        public static Vector128<double> HorizontalSubtract(Vector128<double> left, Vector128<double> right) => HorizontalSubtract(left, right);

        /// <summary>
        /// __m128d _mm_loaddup_pd (double const* mem_addr)
        ///    MOVDDUP xmm1,         m64
        ///   VMOVDDUP xmm1,         m64
        ///   VMOVDDUP xmm1 {k1}{z}, m64
        /// </summary>
        public static unsafe Vector128<double> LoadAndDuplicateToVector128(double* address) => LoadAndDuplicateToVector128(address);

        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<sbyte> LoadDquVector128(sbyte* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<byte> LoadDquVector128(byte* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<short> LoadDquVector128(short* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<ushort> LoadDquVector128(ushort* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<int> LoadDquVector128(int* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<uint> LoadDquVector128(uint* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<long> LoadDquVector128(long* address) => LoadDquVector128(address);
        /// <summary>
        /// __m128i _mm_lddqu_si128 (__m128i const* mem_addr)
        ///    LDDQU xmm1, m128
        ///   VLDDQU xmm1, m128
        /// </summary>
        public static unsafe Vector128<ulong> LoadDquVector128(ulong* address) => LoadDquVector128(address);

        /// <summary>
        /// __m128d _mm_movedup_pd (__m128d a)
        ///    MOVDDUP xmm1,         xmm2/m64
        ///   VMOVDDUP xmm1,         xmm2/m64
        ///   VMOVDDUP xmm1 {k1}{z}, xmm2/m64
        /// </summary>
        public static Vector128<double> MoveAndDuplicate(Vector128<double> source) => MoveAndDuplicate(source);
        /// <summary>
        /// __m128 _mm_movehdup_ps (__m128 a)
        ///    MOVSHDUP xmm1,         xmm2/m128
        ///   VMOVSHDUP xmm1,         xmm2/m128
        ///   VMOVSHDUP xmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector128<float> MoveHighAndDuplicate(Vector128<float> source) => MoveHighAndDuplicate(source);
        /// <summary>
        /// __m128 _mm_moveldup_ps (__m128 a)
        ///    MOVSLDUP xmm1,         xmm2/m128
        ///   VMOVSLDUP xmm1,         xmm2/m128
        ///   VMOVSLDUP xmm1 {k1}{z}, xmm2/m128
        /// </summary>
        public static Vector128<float> MoveLowAndDuplicate(Vector128<float> source) => MoveLowAndDuplicate(source);
    }
}

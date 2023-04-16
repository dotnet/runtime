// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512BW hardware instructions via intrinsics</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Avx512BW : Avx512F
    {
        internal Avx512BW() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        [Intrinsic]
        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// __m512i _mm512_loadu_epi8 (__m512i const * mem_addr)
        ///   VMOVDQU8 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<sbyte> LoadVector512(sbyte* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi8 (__m512i const * mem_addr)
        ///   VMOVDQU8 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<byte> LoadVector512(byte* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi16 (__m512i const * mem_addr)
        ///   VMOVDQU16 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<short> LoadVector512(short* address) => LoadVector512(address);
        /// <summary>
        /// __m512i _mm512_loadu_epi16 (__m512i const * mem_addr)
        ///   VMOVDQU16 zmm1 {k1}{z}, m512
        /// </summary>
        public static new unsafe Vector512<ushort> LoadVector512(ushort* address) => LoadVector512(address);

        /// <summary>
        /// void _mm512_storeu_epi8 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU8 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(sbyte* address, Vector512<sbyte> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi8 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU8 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(byte* address, Vector512<byte> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi16 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU16 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(short* address, Vector512<short> source) => Store(address, source);
        /// <summary>
        /// void _mm512_storeu_epi16 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU16 m512 {k1}{z}, zmm1
        /// </summary>
        public static new unsafe void Store(ushort* address, Vector512<ushort> source) => Store(address, source);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 AVX512F hardware instructions via intrinsics</summary>
    [CLSCompliant(false)]
    public abstract class Avx512F : Avx2
    {
        internal Avx512F() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public abstract class VL
        {
            internal VL() { }

            public static bool IsSupported { [Intrinsic] get { return false; } }
        }

        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> And(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> And(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> And(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_si512 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> And(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> And(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi32 (__m512i a, __m512i b)
        ///   VPANDD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> And(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> And(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_and_epi64 (__m512i a, __m512i b)
        ///   VPANDQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> And(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> AndNot(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> AndNot(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> AndNot(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_si512 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> AndNot(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> AndNot(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi32 (__m512i a, __m512i b)
        ///   VPANDND zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> AndNot(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> AndNot(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_andnot_epi64 (__m512i a, __m512i b)
        ///   VPANDNQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> AndNot(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadAlignedVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadAlignedVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadAlignedVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_si512 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadAlignedVector512(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadAlignedVector512(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi32 (__m512i const * mem_addr)
        ///   VMOVDQA32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadAlignedVector512(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadAlignedVector512(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_load_epi64 (__m512i const * mem_addr)
        ///   VMOVDQA64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadAlignedVector512(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_load_ps (float const * mem_addr)
        ///   VMOVAPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadAlignedVector512(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_load_pd (double const * mem_addr)
        ///   VMOVAPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadAlignedVector512(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<sbyte> LoadVector512(sbyte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<byte> LoadVector512(byte* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<short> LoadVector512(short* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_si512 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ushort> LoadVector512(ushort* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<int> LoadVector512(int* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi32 (__m512i const * mem_addr)
        ///   VMOVDQU32 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<uint> LoadVector512(uint* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<long> LoadVector512(long* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_loadu_epi64 (__m512i const * mem_addr)
        ///   VMOVDQU64 zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<ulong> LoadVector512(ulong* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512 _mm512_loadu_ps (float const * mem_addr)
        ///   VMOVUPS zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<float> LoadVector512(float* address) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512d _mm512_loadu_pd (double const * mem_addr)
        ///   VMOVUPD zmm1 {k1}{z}, m512
        /// </summary>
        public static unsafe Vector512<double> LoadVector512(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Or(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Or(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Or(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_si512 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Or(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Or(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi32 (__m512i a, __m512i b)
        ///   VPORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Or(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Or(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_or_epi64 (__m512i a, __m512i b)
        ///   VPORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Or(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQU64 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_ps (float * mem_addr, __m512 a)
        ///   VMOVUPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_storeu_pd (double * mem_addr, __m512d a)
        ///   VMOVUPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void Store(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi32 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_epi64 (__m512i * mem_addr, __m512i a)
        ///   VMOVDQA32 m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_ps (float * mem_addr, __m512 a)
        ///   VMOVAPS m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_store_pd (double * mem_addr, __m512d a)
        ///   VMOVAPD m512 {k1}{z}, zmm1
        /// </summary>
        public static unsafe void StoreAligned(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(sbyte* address, Vector512<sbyte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(byte* address, Vector512<byte> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(short* address, Vector512<short> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ushort* address, Vector512<ushort> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(int* address, Vector512<int> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(uint* address, Vector512<uint> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(long* address, Vector512<long> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_si512 (__m512i * mem_addr, __m512i a)
        ///   VMOVNTDQ m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(ulong* address, Vector512<ulong> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_ps (float * mem_addr, __m512 a)
        ///   VMOVNTPS m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(float* address, Vector512<float> source) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// void _mm512_stream_pd (double * mem_addr, __m512d a)
        ///   VMOVNTPD m512, zmm1
        /// </summary>
        public static unsafe void StoreAlignedNonTemporal(double* address, Vector512<double> source) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<byte> Xor(Vector512<byte> left, Vector512<byte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<sbyte> Xor(Vector512<sbyte> left, Vector512<sbyte> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<short> Xor(Vector512<short> left, Vector512<short> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_si512 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<ushort> Xor(Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<int> Xor(Vector512<int> left, Vector512<int> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi32 (__m512i a, __m512i b)
        ///   VPXORD zmm1 {k1}{z}, zmm2, zmm3/m512/m32bcst
        /// </summary>
        public static Vector512<uint> Xor(Vector512<uint> left, Vector512<uint> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<long> Xor(Vector512<long> left, Vector512<long> right) { throw new PlatformNotSupportedException(); }
        /// <summary>
        /// __m512i _mm512_xor_epi64 (__m512i a, __m512i b)
        ///   VPXORQ zmm1 {k1}{z}, zmm2, zmm3/m512/m64bcst
        /// </summary>
        public static Vector512<ulong> Xor(Vector512<ulong> left, Vector512<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}

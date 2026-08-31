// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 AVXVNNI hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class AvxVnniInt16 : Avx2
    {
        internal AvxVnniInt16() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 AVX-VNNI-INT8 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        // VPDPWSUD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUSD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<ushort> left, Vector128<short> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUUD xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAdd(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWSUD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUSD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<ushort> left, Vector256<short> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUUD ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAdd(Vector256<uint> addend, Vector256<ushort> left, Vector256<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWSUDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<short> left, Vector128<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUSDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<ushort> left, Vector128<short> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUUDS xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAddSaturate(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWSUDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<short> left, Vector256<ushort> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUSDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<ushort> left, Vector256<short> right)  { throw new PlatformNotSupportedException(); }

        // VPDPWUUDS ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAddSaturate(Vector256<uint> addend, Vector256<ushort> left, Vector256<ushort> right)  { throw new PlatformNotSupportedException(); }

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions for AVX-VNNI-INT16 via intrinsics.</summary>
        [Intrinsic]
        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get { return false; } }

            // VPDPWSUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<short> left, Vector512<ushort> right)  { throw new PlatformNotSupportedException(); }

            // VPDPWUSD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<ushort> left, Vector512<short> right)  { throw new PlatformNotSupportedException(); }

            // VPDPWUUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAdd(Vector512<uint> addend, Vector512<ushort> left, Vector512<ushort> right)  { throw new PlatformNotSupportedException(); }

            // VPDPWSUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<short> left, Vector512<ushort> right)  { throw new PlatformNotSupportedException(); }

            // VPDPWUSDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<ushort> left, Vector512<short> right)  { throw new PlatformNotSupportedException(); }

            // VPDPWUUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAddSaturate(Vector512<uint> addend, Vector512<ushort> left, Vector512<ushort> right)  { throw new PlatformNotSupportedException(); }
        }
    }
}

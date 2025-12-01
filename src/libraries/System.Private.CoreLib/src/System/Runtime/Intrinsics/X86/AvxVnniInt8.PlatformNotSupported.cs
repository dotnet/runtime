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
    public abstract class AvxVnniInt8 : Avx2
    {
        internal AvxVnniInt8() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        // VPDPBSSD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSUD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<sbyte> left, Vector128<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBUUD xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAdd(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSSD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<sbyte> left, Vector256<sbyte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSUD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<sbyte> left, Vector256<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBUUD ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAdd(Vector256<uint> addend, Vector256<byte> left, Vector256<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSSDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSUDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<sbyte> left, Vector128<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBUUDS xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAddSaturate(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSSDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<sbyte> left, Vector256<sbyte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBSUDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<sbyte> left, Vector256<byte> right)  { throw new PlatformNotSupportedException(); }

        // VPDPBUUDS ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAddSaturate(Vector256<uint> addend, Vector256<byte> left, Vector256<byte> right)  { throw new PlatformNotSupportedException(); }

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions for AVX-VNNI-INT8 via intrinsics.</summary>
        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get { return false; } }

            // VPDPBSSD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<sbyte> left, Vector512<sbyte> right)  { throw new PlatformNotSupportedException(); }

            // VPDPBSUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<sbyte> left, Vector512<byte> right)  { throw new PlatformNotSupportedException(); }

            // VPDPBUUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAdd(Vector512<uint> addend, Vector512<byte> left, Vector512<byte> right)  { throw new PlatformNotSupportedException(); }

            // VPDPBSSDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<sbyte> left, Vector512<sbyte> right)  { throw new PlatformNotSupportedException(); }

            // VPDPBSUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<sbyte> left, Vector512<byte> right)  { throw new PlatformNotSupportedException(); }

            // VPDPBUUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAddSaturate(Vector512<uint> addend, Vector512<byte> left, Vector512<byte> right)  { throw new PlatformNotSupportedException(); }
        }
    }
}

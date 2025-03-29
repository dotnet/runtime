// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 AVXVNNI hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class AvxVnniInt8 : Avx2
    {
        internal AvxVnniInt8() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 AVX-VNNI-INT8 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Avx2.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }

        // VPDPBSSD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBSUD xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAdd(Vector128<int> addend, Vector128<sbyte> left, Vector128<byte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBUUD xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAdd(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBSSD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBSUD ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAdd(Vector256<int> addend, Vector256<sbyte> left, Vector256<byte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBUUD ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAdd(Vector256<uint> addend, Vector256<byte> left, Vector256<byte> right) => MultiplyWideningAndAdd(addend, left, right);

        // VPDPBSSDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        // VPDPBSUDS xmm1, xmm2, xmm3/m128
        public static Vector128<int> MultiplyWideningAndAddSaturate(Vector128<int> addend, Vector128<sbyte> left, Vector128<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        // VPDPBUUDS xmm1, xmm2, xmm3/m128
        public static Vector128<uint> MultiplyWideningAndAddSaturate(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        // VPDPBSSDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        // VPDPBSUDS ymm1, ymm2, ymm3/m256
        public static Vector256<int> MultiplyWideningAndAddSaturate(Vector256<int> addend, Vector256<sbyte> left, Vector256<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        // VPDPBUUDS ymm1, ymm2, ymm3/m256
        public static Vector256<uint> MultiplyWideningAndAddSaturate(Vector256<uint> addend, Vector256<byte> left, Vector256<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

        /// <summary>Provides access to the x86 AVX10.2/512 hardware instructions for AVX-VNNI-INT8 via intrinsics.</summary>
        [Intrinsic]
        public abstract class V512
        {
            internal V512() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            // VPDPBSSD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<sbyte> left, Vector512<sbyte> right) => MultiplyWideningAndAdd(addend, left, right);

            // VPDPBSUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAdd(Vector512<int> addend, Vector512<sbyte> left, Vector512<byte> right) => MultiplyWideningAndAdd(addend, left, right);

            // VPDPBUUD zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAdd(Vector512<uint> addend, Vector512<byte> left, Vector512<byte> right) => MultiplyWideningAndAdd(addend, left, right);

            // VPDPBSSDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<sbyte> left, Vector512<sbyte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

            // VPDPBSUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<int> MultiplyWideningAndAddSaturate(Vector512<int> addend, Vector512<sbyte> left, Vector512<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);

            // VPDPBUUDS zmm1{k1}{z}, zmm2, zmm3/m512/m32bcst
            public static Vector512<uint> MultiplyWideningAndAddSaturate(Vector512<uint> addend, Vector512<byte> left, Vector512<byte> right) => MultiplyWideningAndAddSaturate(addend, left, right);
        }
    }
}

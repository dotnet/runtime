// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 AVX512-BF16 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Avx512Bf16 : Avx512F
    {
        internal Avx512Bf16() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Avx512F.X64
        {
            internal X64() { }
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        public static Vector512<float> MultiplyWideningAndAdd(Vector512<float> addend, Vector512<ushort> left, Vector512<ushort> right) { throw new PlatformNotSupportedException(); }
        public static Vector512<ushort> ConvertToBFloat16(Vector512<float> lower, Vector512<float> upper) { throw new PlatformNotSupportedException(); }
        public static Vector256<ushort> ConvertToBFloat16(Vector512<float> value) { throw new PlatformNotSupportedException(); }

        public new abstract class VL : Avx512F.VL
        {
            internal VL() { }
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            public static Vector128<float> MultiplyWideningAndAdd(Vector128<float> addend, Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }
            public static Vector256<float> MultiplyWideningAndAdd(Vector256<float> addend, Vector256<ushort> left, Vector256<ushort> right) { throw new PlatformNotSupportedException(); }
            public static Vector128<ushort> ConvertToBFloat16(Vector128<float> lower, Vector128<float> upper) { throw new PlatformNotSupportedException(); }
            public static Vector256<ushort> ConvertToBFloat16(Vector256<float> lower, Vector256<float> upper) { throw new PlatformNotSupportedException(); }
            public static Vector128<ushort> ConvertToBFloat16(Vector128<float> value) { throw new PlatformNotSupportedException(); }
            public static Vector128<ushort> ConvertToBFloat16(Vector256<float> value) { throw new PlatformNotSupportedException(); }
        }
    }
}

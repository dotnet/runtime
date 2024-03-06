// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sm4 : AdvSimd
    {
        internal Sm4() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  Sm4EncryptionAndDecryption : SM4 Encode takes input data as a 128-bit vector from the first source SIMD&FP register, and four iterations of the round key held as the elements of the 128-bit vector in the second source SIMD&FP register. It encrypts the data by four rounds, in accordance with the SM4 standard, returning the 128-bit result to the destination SIMD&FP register.

        /// <summary>
        /// uint32x4_t vsm4eq_u32(uint32x4_t a, uint32x4_t b)
        /// </summary>
        public static unsafe Vector128<uint> Sm4EncryptionAndDecryption(Vector128<uint> a, Vector128<uint> b) { throw new PlatformNotSupportedException(); }


        ///  Sm4KeyUpdates : SM4 Key takes an input as a 128-bit vector from the first source SIMD&FP register and a 128-bit constant from the second SIMD&FP register. It derives four iterations of the output key, in accordance with the SM4 standard, returning the 128-bit result to the destination SIMD&FP register.

        /// <summary>
        /// uint32x4_t vsm4ekeyq_u32(uint32x4_t a, uint32x4_t b)
        /// </summary>
        public static unsafe Vector128<uint> Sm4KeyUpdates(Vector128<uint> a, Vector128<uint> b) { throw new PlatformNotSupportedException(); }

    }
}


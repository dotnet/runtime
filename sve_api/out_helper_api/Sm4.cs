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
        ///   SM4E Vd.4S,Vn.4S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GK_2A  SM4E <Zdn>.S,
        ///        theEmitter->emitIns_R_R(INS_sve_sm4e, EA_SCALABLE, REG_V3, REG_V5, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector128<uint> Sm4EncryptionAndDecryption(Vector128<uint> a, Vector128<uint> b) => Sm4EncryptionAndDecryption(a, b);


        ///  Sm4KeyUpdates : SM4 Key takes an input as a 128-bit vector from the first source SIMD&FP register and a 128-bit constant from the second SIMD&FP register. It derives four iterations of the output key, in accordance with the SM4 standard, returning the 128-bit result to the destination SIMD&FP register.

        /// <summary>
        /// uint32x4_t vsm4ekeyq_u32(uint32x4_t a, uint32x4_t b)
        ///   SM4EKEY Vd.4S,Vn.4S,Vm.4S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GJ_3A  SM4EKEY <Zd>.S, <Zn>.S, <Zm>.S
        ///        theEmitter->emitIns_R_R_R(INS_sve_sm4ekey, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector128<uint> Sm4KeyUpdates(Vector128<uint> a, Vector128<uint> b) => Sm4KeyUpdates(a, b);

    }
}


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
    public abstract class SveSm4 : AdvSimd
    {
        internal SveSm4() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  Sm4EncryptionAndDecryption : SM4 encryption and decryption

        /// <summary>
        /// svuint32_t svsm4e[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4E Ztied1.S, Ztied1.S, Zop2.S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GK_2A   SM4E <Zdn>.S,
        ///    theEmitter->emitIns_R_R(INS_sve_sm4e, EA_SCALABLE, REG_V3, REG_V5, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<uint> Sm4EncryptionAndDecryption(Vector<uint> left, Vector<uint> right) => Sm4EncryptionAndDecryption(left, right);


        ///  Sm4KeyUpdates : SM4 key updates

        /// <summary>
        /// svuint32_t svsm4ekey[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4EKEY Zresult.S, Zop1.S, Zop2.S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GJ_3A   SM4EKEY <Zd>.S, <Zn>.S, <Zm>.S
        ///    theEmitter->emitIns_R_R_R(INS_sve_sm4ekey, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<uint> Sm4KeyUpdates(Vector<uint> left, Vector<uint> right) => Sm4KeyUpdates(left, right);

    }
}


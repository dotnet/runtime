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
    public abstract class SveAes : AdvSimd
    {
        internal SveAes() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  AesInverseMixColumns : AES inverse mix columns

        /// <summary>
        /// svuint8_t svaesimc[_u8](svuint8_t op)
        ///   AESIMC Ztied.B, Ztied.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_GL_1A   AESIMC <Zdn>.B, <Zdn>.B
        ///    theEmitter->emitIns_R(INS_sve_aesimc, EA_SCALABLE, REG_V0);
        /// </summary>
        public static unsafe Vector<byte> AesInverseMixColumns(Vector<byte> value) => AesInverseMixColumns(value);


        ///  AesMixColumns : AES mix columns

        /// <summary>
        /// svuint8_t svaesmc[_u8](svuint8_t op)
        ///   AESMC Ztied.B, Ztied.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_GL_1A   AESMC <Zdn>.B, <Zdn>.B
        ///    theEmitter->emitIns_R(INS_sve_aesmc, EA_SCALABLE, REG_V5);
        /// </summary>
        public static unsafe Vector<byte> AesMixColumns(Vector<byte> value) => AesMixColumns(value);


        ///  AesSingleRoundDecryption : AES single round decryption

        /// <summary>
        /// svuint8_t svaesd[_u8](svuint8_t op1, svuint8_t op2)
        ///   AESD Ztied1.B, Ztied1.B, Zop2.B
        ///   AESD Ztied2.B, Ztied2.B, Zop1.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_GK_2A   AESD <Zdn>.B,
        ///    theEmitter->emitIns_R_R(INS_sve_aesd, EA_SCALABLE, REG_V0, REG_V0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<byte> AesSingleRoundDecryption(Vector<byte> left, Vector<byte> right) => AesSingleRoundDecryption(left, right);


        ///  AesSingleRoundEncryption : AES single round encryption

        /// <summary>
        /// svuint8_t svaese[_u8](svuint8_t op1, svuint8_t op2)
        ///   AESE Ztied1.B, Ztied1.B, Zop2.B
        ///   AESE Ztied2.B, Ztied2.B, Zop1.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_GK_2A   AESE <Zdn>.B,
        ///    theEmitter->emitIns_R_R(INS_sve_aese, EA_SCALABLE, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<byte> AesSingleRoundEncryption(Vector<byte> left, Vector<byte> right) => AesSingleRoundEncryption(left, right);


        ///  PolynomialMultiplyWideningLower : Polynomial multiply long (bottom)

        /// <summary>
        /// svuint64_t svpmullb_pair[_u64](svuint64_t op1, svuint64_t op2)
        ///   PMULLB Zresult.Q, Zop1.D, Zop2.D
        ///
        /// codegenarm64test:
        ///    IF_SVE_FN_3A   PMULLB <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        ///    theEmitter->emitIns_R_R_R(INS_sve_pmullb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_FN_3B   PMULLB <Zd>.Q, <Zn>.D, <Zm>.D
        ///    theEmitter->emitIns_R_R_R(INS_sve_pmullb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q);
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, Vector<ulong> right) => PolynomialMultiplyWideningLower(left, right);


        ///  PolynomialMultiplyWideningUpper : Polynomial multiply long (top)

        /// <summary>
        /// svuint64_t svpmullt_pair[_u64](svuint64_t op1, svuint64_t op2)
        ///   PMULLT Zresult.Q, Zop1.D, Zop2.D
        ///
        /// codegenarm64test:
        ///    IF_SVE_FN_3A   PMULLT <Zd>.<T>, <Zn>.<Tb>, <Zm>.<Tb>
        ///    theEmitter->emitIns_R_R_R(INS_sve_pmullt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_FN_3B   PMULLT <Zd>.Q, <Zn>.D, <Zm>.D
        ///    theEmitter->emitIns_R_R_R(INS_sve_pmullt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q);
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, Vector<ulong> right) => PolynomialMultiplyWideningUpper(left, right);

    }
}


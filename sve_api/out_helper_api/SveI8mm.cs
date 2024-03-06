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
    public abstract class SveI8mm : AdvSimd
    {
        internal SveI8mm() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  DotProductSignedUnsigned : Dot product (signed × unsigned)

        /// <summary>
        /// svint32_t svsudot[_s32](svint32_t op1, svint8_t op2, svuint8_t op3)
        ///   USDOT Ztied1.S, Zop3.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop3.B, Zop2.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_EZ_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V21, REG_V22, REG_V2, 2, INS_OPTS_SCALABLE_B);
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V23, REG_V24, REG_V3, 3, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_EI_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_usdot, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3) => DotProductSignedUnsigned(op1, op2, op3);

        /// <summary>
        /// svint32_t svsudot_lane[_s32](svint32_t op1, svint8_t op2, svuint8_t op3, uint64_t imm_index)
        ///   SUDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; SUDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_EZ_3A  SUDOT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_sudot, EA_SCALABLE, REG_V17, REG_V18, REG_V0, 0, INS_OPTS_SCALABLE_B);
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_sudot, EA_SCALABLE, REG_V19, REG_V20, REG_V1, 1, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> DotProductSignedUnsigned(Vector<int> op1, Vector<sbyte> op2, Vector<byte> op3, ulong imm_index) => DotProductSignedUnsigned(op1, op2, op3, imm_index);


        ///  DotProductUnsignedSigned : Dot product (unsigned × signed)

        /// <summary>
        /// svint32_t svusdot[_s32](svint32_t op1, svuint8_t op2, svint8_t op3)
        ///   USDOT Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_EZ_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V21, REG_V22, REG_V2, 2, INS_OPTS_SCALABLE_B);
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V23, REG_V24, REG_V3, 3, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_EI_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_usdot, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3) => DotProductUnsignedSigned(op1, op2, op3);

        /// <summary>
        /// svint32_t svusdot_lane[_s32](svint32_t op1, svuint8_t op2, svint8_t op3, uint64_t imm_index)
        ///   USDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; USDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_EZ_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B[<imm>]
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V21, REG_V22, REG_V2, 2, INS_OPTS_SCALABLE_B);
        ///        theEmitter->emitIns_R_R_R_I(INS_sve_usdot, EA_SCALABLE, REG_V23, REG_V24, REG_V3, 3, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_EI_3A  USDOT <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_usdot, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<int> DotProductUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3, ulong imm_index) => DotProductUnsignedSigned(op1, op2, op3, imm_index);


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svint32_t svmmla[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        ///   SMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; SMMLA Zresult.S, Zop2.B, Zop3.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_FO_3A  SMMLA <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_smmla, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<int> MatrixMultiplyAccumulate(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3) => MatrixMultiplyAccumulate(op1, op2, op3);

        /// <summary>
        /// svuint32_t svmmla[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        ///   UMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; UMMLA Zresult.S, Zop2.B, Zop3.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_FO_3A  UMMLA <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_ummla, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<uint> MatrixMultiplyAccumulate(Vector<uint> op1, Vector<byte> op2, Vector<byte> op3) => MatrixMultiplyAccumulate(op1, op2, op3);


        ///  MatrixMultiplyAccumulateUnsignedSigned : Matrix multiply-accumulate (unsigned × signed)

        /// <summary>
        /// svint32_t svusmmla[_s32](svint32_t op1, svuint8_t op2, svint8_t op3)
        ///   USMMLA Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; USMMLA Zresult.S, Zop2.B, Zop3.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_FO_3A  USMMLA <Zda>.S, <Zn>.B, <Zm>.B
        ///        theEmitter->emitIns_R_R_R(INS_sve_usmmla, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<int> MatrixMultiplyAccumulateUnsignedSigned(Vector<int> op1, Vector<byte> op2, Vector<sbyte> op3) => MatrixMultiplyAccumulateUnsignedSigned(op1, op2, op3);

    }
}


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
    public abstract class SveBf16 : AdvSimd
    {
        internal SveBf16() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  Bfloat16DotProduct : BFloat16 dot product

        /// <summary>
        /// svfloat32_t svbfdot[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFDOT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GY_3B   BFDOT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfdot, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GY_3B   BFDOT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfdot, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HA_3A   BFDOT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfdot, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right) => Bfloat16DotProduct(addend, left, right);


        ///  Bfloat16MatrixMultiplyAccumulate : BFloat16 matrix multiply-accumulate

        /// <summary>
        /// svfloat32_t svbfmmla[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMMLA Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMMLA Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HD_3A   BFMMLA <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfmmla, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16MatrixMultiplyAccumulate(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MatrixMultiplyAccumulate(op1, op2, op3);


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionLower : BFloat16 multiply-add long to single-precision (bottom)

        /// <summary>
        /// svfloat32_t svbfmlalb[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMLALB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   BFMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V0, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   BFMLALB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MultiplyAddWideningToSinglePrecisionLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svbfmlalb_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   BFMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V0, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   BFMLALB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) => Bfloat16MultiplyAddWideningToSinglePrecisionLower(op1, op2, op3, imm_index);


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionUpper : BFloat16 multiply-add long to single-precision (top)

        /// <summary>
        /// svfloat32_t svbfmlalt[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMLALT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   BFMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfmlalt, EA_SCALABLE, REG_V2, REG_V3, REG_V1, 1, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   BFMLALT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfmlalt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MultiplyAddWideningToSinglePrecisionUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svbfmlalt_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   BFMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfmlalt, EA_SCALABLE, REG_V2, REG_V3, REG_V1, 1, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   BFMLALT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfmlalt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) => Bfloat16MultiplyAddWideningToSinglePrecisionUpper(op1, op2, op3, imm_index);


        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svuzp1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => ConcatenateEvenInt128FromTwoInputs(left, right);


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svuzp2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => ConcatenateOddInt128FromTwoInputs(left, right);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTA <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CN_3A   CLASTA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_2BYTE, REG_V12, REG_P1, REG_V15, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R0, REG_P0, REG_V0, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R1, REG_P2, REG_V3, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// bfloat16_t svclasta[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.H
        ///   CLASTA Htied, Pg, Htied, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTA <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CN_3A   CLASTA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_2BYTE, REG_V12, REG_P1, REG_V15, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R0, REG_P0, REG_V0, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R1, REG_P2, REG_V3, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTA <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CN_3A   CLASTA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_2BYTE, REG_V12, REG_P1, REG_V15, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R0, REG_P0, REG_V0, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CO_3A   CLASTA <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clasta, EA_4BYTE, REG_R1, REG_P2, REG_V3, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> defaultScalar, Vector<bfloat16> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTB <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_SCALABLE, REG_V30, REG_P6, REG_V30, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_V13, REG_P2, REG_V16, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_V14, REG_P0, REG_V17, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_R23, REG_P5, REG_V12, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_R3, REG_P6, REG_V9, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// bfloat16_t svclastb[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.H
        ///   CLASTB Htied, Pg, Htied, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTB <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_SCALABLE, REG_V30, REG_P6, REG_V30, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_V13, REG_P2, REG_V16, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_V14, REG_P0, REG_V17, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_R23, REG_P5, REG_V12, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_R3, REG_P6, REG_V9, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CM_3A   CLASTB <Zdn>.<T>, <Pg>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_SCALABLE, REG_V30, REG_P6, REG_V30, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_V13, REG_P2, REG_V16, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CN_3A   CLASTB <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_V14, REG_P0, REG_V17, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_4BYTE, REG_R23, REG_P5, REG_V12, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_CO_3A   CLASTB <R><dn>, <Pg>, <R><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_clastb, EA_8BYTE, REG_R3, REG_P6, REG_V9, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svbfloat16_t svsel[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CZ_4A   CMPNE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_sel, EA_SCALABLE, REG_P4, REG_P6, REG_P13, REG_P10, INS_OPTS_SCALABLE_B); /* SEL <Pd>.B, <Pg>, <Pn>.B, <Pm>.B */
        ///    IF_SVE_CW_4A   SEL <Zd>.<T>, <Pv>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_sel, EA_SCALABLE, REG_V29, REG_P15, REG_V28, REG_V4, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CW_4A   SEL <Zd>.<T>, <Pv>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_sel, EA_SCALABLE, REG_V5, REG_P13, REG_V27, REG_V5, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalSelect(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToBFloat16 : Floating-point convert

        /// <summary>
        /// svbfloat16_t svcvt_bf16[_f32]_m(svbfloat16_t inactive, svbool_t pg, svfloat32_t op)
        ///   BFCVT Ztied.H, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; BFCVT Zresult.H, Pg/M, Zop.S
        /// svbfloat16_t svcvt_bf16[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   BFCVT Ztied.H, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; BFCVT Zresult.H, Pg/M, Zop.S
        /// svbfloat16_t svcvt_bf16[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; BFCVT Zresult.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///   sve_bfcvt - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<bfloat16> ConvertToBFloat16(Vector<float> value) => ConvertToBFloat16(value);



        ///  CreateFalseMaskBFloat16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_DJ_1A   PFALSE <Pd>.B
        ///    theEmitter->emitIns_R(INS_sve_pfalse, EA_SCALABLE, REG_P13);
        /// </summary>
        public static unsafe Vector<bfloat16> CreateFalseMaskBFloat16() => CreateFalseMaskBFloat16();


        ///  CreateTrueMaskBFloat16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        ///
        /// codegenarm64test:
        ///    IF_SVE_DE_1A   PTRUE <Pd>.<T>{, <pattern>}
        ///    theEmitter->emitIns_R_PATTERN(INS_sve_ptrue, EA_SCALABLE, REG_P0, INS_OPTS_SCALABLE_B, SVE_PATTERN_POW2);
        ///    IF_SVE_DE_1A   PTRUE <Pd>.<T>{, <pattern>}
        ///    theEmitter->emitIns_R_PATTERN(INS_sve_ptrue, EA_SCALABLE, REG_P7, INS_OPTS_SCALABLE_H, SVE_PATTERN_MUL3);
        ///    IF_SVE_DZ_1A   PTRUE <PNd>.<T>
        ///    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P8, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_DZ_1A   PTRUE <PNd>.<T>
        ///    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P9, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_DZ_1A   PTRUE <PNd>.<T>
        ///    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P10, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_DZ_1A   PTRUE <PNd>.<T>
        ///    theEmitter->emitIns_R(INS_sve_ptrue, EA_SCALABLE, REG_P11, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> CreateTrueMaskBFloat16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskBFloat16(pattern);


        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        ///   WHILERW Presult.H, Xop1, Xop2
        ///
        /// codegenarm64test:
        ///    IF_SVE_DU_3A   WHILERW <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P0, REG_R0, REG_R1, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_DU_3A   WHILERW <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P1, REG_R2, REG_R3, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_DU_3A   WHILERW <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P2, REG_R4, REG_R5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_DU_3A   WHILERW <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilerw, EA_8BYTE, REG_P3, REG_R6, REG_R7, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileReadAfterWriteMask(bfloat16* left, bfloat16* right) => CreateWhileReadAfterWriteMask(left, right);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        ///   WHILEWR Presult.H, Xop1, Xop2
        ///
        /// codegenarm64test:
        ///    IF_SVE_DU_3A   WHILEWR <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P4, REG_R8, REG_R9, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_DU_3A   WHILEWR <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P5, REG_R10, REG_R11, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_DU_3A   WHILEWR <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P6, REG_R12, REG_R13, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_DU_3A   WHILEWR <Pd>.<T>, <Xn>, <Xm>
        ///    theEmitter->emitIns_R_R_R(INS_sve_whilewr, EA_8BYTE, REG_P7, REG_R14, REG_R15, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileWriteAfterReadMask(bfloat16* left, bfloat16* right) => CreateWhileWriteAfterReadMask(left, right);


        ///  DotProductBySelectedScalar : BFloat16 dot product

        /// <summary>
        /// svfloat32_t svbfdot_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFDOT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GY_3B   BFDOT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfdot, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GY_3B   BFDOT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_bfdot, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HA_3A   BFDOT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfdot, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> DotProductBySelectedScalar(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);


        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svbfloat16_t svcvtnt_bf16[_f32]_m(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   BFCVTNT Ztied.H, Pg/M, Zop.S
        /// svbfloat16_t svcvtnt_bf16[_f32]_x(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   BFCVTNT Ztied.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GQ_3A   BFCVTNT <Zd>.H, <Pg>/M, <Zn>.S
        ///    theEmitter->emitIns_R_R_R(INS_sve_bfcvtnt, EA_SCALABLE, REG_V3, REG_P0, REG_V4);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<bfloat16> DownConvertNarrowingUpper(Vector<float> value) => DownConvertNarrowingUpper(value);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svbfloat16_t svdup_lane[_bf16](svbfloat16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// svbfloat16_t svdupq_lane[_bf16](svbfloat16_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        ///
        /// codegenarm64test:
        ///    IF_SVE_EB_1A   DUP <Zd>.<T>, #<imm>{, <shift>}
        ///    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V0, -128, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_EB_1A   DUP <Zd>.<T>, #<imm>{, <shift>}
        ///    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V1, 0, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_SHIFT);
        ///    IF_SVE_EB_1A   DUP <Zd>.<T>, #<imm>{, <shift>}
        ///    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V2, 5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_EB_1A   DUP <Zd>.<T>, #<imm>{, <shift>}
        ///    theEmitter->emitIns_R_I(INS_sve_dup, EA_SCALABLE, REG_V3, 127, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_CB_2A   DUP <Zd>.<T>, <R><n|SP>
        ///    theEmitter->emitIns_R_R(INS_sve_dup, EA_4BYTE, REG_V0, REG_R1, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CB_2A   DUP <Zd>.<T>, <R><n|SP>
        ///    theEmitter->emitIns_R_R(INS_sve_dup, EA_4BYTE, REG_V2, REG_R3, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_CB_2A   DUP <Zd>.<T>, <R><n|SP>
        ///    theEmitter->emitIns_R_R(INS_sve_dup, EA_4BYTE, REG_V1, REG_R5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_CB_2A   DUP <Zd>.<T>, <R><n|SP>
        ///    theEmitter->emitIns_R_R(INS_sve_dup, EA_8BYTE, REG_V4, REG_SP, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        /// </summary>
        public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CR_3A   LASTA <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_1BYTE, REG_V6, REG_P1, REG_V27, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CR_3A   LASTA <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_2BYTE, REG_V5, REG_P2, REG_V26, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CS_3A   LASTA <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R1, REG_P5, REG_V23, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CS_3A   LASTA <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R0, REG_P6, REG_V22, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe bfloat16 ExtractAfterLastScalar(Vector<bfloat16> value) => ExtractAfterLastScalar(value);


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CR_3A   LASTA <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_1BYTE, REG_V6, REG_P1, REG_V27, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CR_3A   LASTA <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_2BYTE, REG_V5, REG_P2, REG_V26, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CS_3A   LASTA <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R1, REG_P5, REG_V23, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CS_3A   LASTA <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lasta, EA_4BYTE, REG_R0, REG_P6, REG_V22, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractAfterLastVector(Vector<bfloat16> value) => ExtractAfterLastVector(value);


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CR_3A   LASTB <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_V4, REG_P3, REG_V25, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CR_3A   LASTB <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_V3, REG_P4, REG_V24, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CS_3A   LASTB <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_R30, REG_P7, REG_V21, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_CS_3A   LASTB <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_R29, REG_P0, REG_V20, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe bfloat16 ExtractLastScalar(Vector<bfloat16> value) => ExtractLastScalar(value);


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CR_3A   LASTB <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_V4, REG_P3, REG_V25, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CR_3A   LASTB <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_V3, REG_P4, REG_V24, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_SIMD_SCALAR);
        ///    IF_SVE_CS_3A   LASTB <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_4BYTE, REG_R30, REG_P7, REG_V21, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_CS_3A   LASTB <R><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_lastb, EA_8BYTE, REG_R29, REG_P0, REG_V20, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractLastVector(Vector<bfloat16> value) => ExtractLastVector(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svbfloat16_t svext[_bf16](svbfloat16_t op1, svbfloat16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        ///
        /// codegenarm64test:
        ///   sve_ext - not implemented in coreclr
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractVector(Vector<bfloat16> upper, Vector<bfloat16> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_DK_3A   CNTP <Xd>, <Pg>, <Pn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_cntp, EA_8BYTE, REG_R29, REG_P0, REG_P15, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R0, REG_P0, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_VL_2X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R1, REG_P1, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_VL_4X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R2, REG_P2, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_VL_2X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R3, REG_P3, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_VL_4X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R4, REG_P4, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_VL_2X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R5, REG_P5, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_VL_4X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R6, REG_P6, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_VL_2X);
        ///    IF_SVE_DL_2A   CNTP <Xd>, <PNn>.<T>, <vl>
        ///    theEmitter->emitIns_R_R(INS_sve_cntp, EA_8BYTE, REG_R7, REG_P7, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_VL_4X);
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<bfloat16> mask, Vector<bfloat16> from) => GetActiveElementCount(mask, from);


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svbfloat16_t svinsr[_n_bf16](svbfloat16_t op1, bfloat16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        ///
        /// codegenarm64test:
        ///   sve_insr - not implemented in coreclr
        /// </summary>
        public static unsafe Vector<bfloat16> InsertIntoShiftedVector(Vector<bfloat16> left, bfloat16 right) => InsertIntoShiftedVector(left, right);


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svtrn1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveEvenInt128FromTwoInputs(left, right);


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svtrn2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveOddInt128FromTwoInputs(left, right);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svbfloat16_t svld1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IJ_3A_G   LD1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IJ_3A_G   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IJ_3A_G   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R6, 1, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HW_4A   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P3, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P3, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_A   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R0, REG_V1, INS_OPTS_SCALABLE_D_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_A   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V2, REG_P1, REG_R0, REG_V1, INS_OPTS_SCALABLE_D_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_B   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P5, REG_R4, REG_V3, INS_OPTS_SCALABLE_D_UXTW);
        ///    IF_SVE_HW_4A_B   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P5, REG_R4, REG_V3, INS_OPTS_SCALABLE_D_SXTW);
        ///    IF_SVE_HW_4A_C   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_V3, INS_OPTS_SCALABLE_S_UXTW);
        ///    IF_SVE_HW_4A_C   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_V3, INS_OPTS_SCALABLE_S_SXTW);
        ///    IF_SVE_HW_4B   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V5, REG_P4, REG_R3, REG_V2, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_HW_4B_D   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V4, REG_P2, REG_R1, REG_V3, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IK_4A_I   LD1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V4, REG_P2, REG_R3, REG_R1, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_IK_4A_I   LD1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V4, REG_P2, REG_R3, REG_R1, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_IK_4A_I   LD1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1h, EA_SCALABLE, REG_V4, REG_P2, REG_R3, REG_R1, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_HX_3A_E   LD1H {<Zt>.S }, <Pg>/Z, [<Zn>.S{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V1, REG_P0, REG_V2, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HX_3A_E   LD1H {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1h, EA_SCALABLE, REG_V1, REG_P0, REG_V2, 0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, bfloat16* address) => LoadVector(mask, address);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svbfloat16_t svld1rq[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A   LD1RQH {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1rqh, EA_SCALABLE, REG_V4, REG_P5, REG_R6, 112, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IP_4A   LD1RQH {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1rqh, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_R4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector128AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address) => LoadVector128AndReplicateToVector(mask, address);


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svbfloat16_t svld1ro[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IO_3A   LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld1roh, EA_SCALABLE, REG_V8, REG_P3, REG_R1, -256, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IP_4A   LD1ROH {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld1roh, EA_SCALABLE, REG_V4, REG_P3, REG_R2, REG_R1, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector256AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address) => LoadVector256AndReplicateToVector(mask, address);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svbfloat16_t svldff1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]
        ///
        /// codegenarm64test:
        ///    IF_SVE_HW_4A   LDFF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V2, REG_P1, REG_R3, REG_V4, INS_OPTS_SCALABLE_S_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A   LDFF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V2, REG_P1, REG_R3, REG_V4, INS_OPTS_SCALABLE_S_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_A   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P5, REG_R1, REG_V2, INS_OPTS_SCALABLE_D_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_A   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P5, REG_R1, REG_V2, INS_OPTS_SCALABLE_D_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_HW_4A_B   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V1, REG_P3, REG_R4, REG_V5, INS_OPTS_SCALABLE_D_UXTW);
        ///    IF_SVE_HW_4A_B   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V1, REG_P3, REG_R4, REG_V5, INS_OPTS_SCALABLE_D_SXTW);
        ///    IF_SVE_HW_4A_C   LDFF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P2, REG_R1, REG_V3, INS_OPTS_SCALABLE_S_UXTW);
        ///    IF_SVE_HW_4A_C   LDFF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P2, REG_R1, REG_V3, INS_OPTS_SCALABLE_S_SXTW);
        ///    IF_SVE_HW_4B   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V0, REG_P2, REG_R6, REG_V1, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_HW_4B_D   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>, <Zm>.D]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V2, REG_P3, REG_R1, REG_V5, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IG_4A_G   LDFF1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V3, REG_P1, REG_R4, REG_R0, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_IG_4A_G   LDFF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V3, REG_P1, REG_R4, REG_R0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_IG_4A_G   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V3, REG_P1, REG_R4, REG_R0, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_IG_4A_G   LDFF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, <Xm>, LSL #1}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldff1h, EA_SCALABLE, REG_V3, REG_P1, REG_R4, REG_ZR, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_HX_3A_E   LDFF1H {<Zt>.S }, <Pg>/Z, [<Zn>.S{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P7, REG_V3, 6, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HX_3A_E   LDFF1H {<Zt>.D }, <Pg>/Z, [<Zn>.D{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldff1h, EA_SCALABLE, REG_V4, REG_P7, REG_V3, 6, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, bfloat16* address) => LoadVectorFirstFaulting(mask, address);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svbfloat16_t svldnf1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IL_3A_B   LDNF1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IL_3A_B   LDNF1H {<Zt>.S }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IL_3A_B   LDNF1H {<Zt>.D }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldnf1h, EA_SCALABLE, REG_V1, REG_P3, REG_R2, 5, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonFaulting(bfloat16* address) => LoadVectorNonFaulting(address);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svbfloat16_t svldnt1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IM_3A   LDNT1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ldnt1h, EA_SCALABLE, REG_V6, REG_P7, REG_R8, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IF_4A   LDNT1H {<Zt>.S }, <Pg>/Z, [<Zn>.S{, <Xm>}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldnt1h, EA_SCALABLE, REG_V0, REG_P1, REG_V2, REG_R3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IF_4A_A   LDNT1H {<Zt>.D }, <Pg>/Z, [<Zn>.D{, <Xm>}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldnt1h, EA_SCALABLE, REG_V1, REG_P4, REG_V3, REG_R2, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_IN_4A   LDNT1H {<Zt>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ldnt1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, REG_R5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, bfloat16* address) => LoadVectorNonTemporal(mask, address);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svbfloat16x2_t svld2[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD2H {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld2h, EA_SCALABLE, REG_V6, REG_P5, REG_R4, 8, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD2H {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld2h, EA_SCALABLE, REG_V8, REG_P5, REG_R9, REG_R10, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, bfloat16* address) => LoadVectorx2(mask, address);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svbfloat16x3_t svld3[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>{,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld3h, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 21, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld3h, EA_SCALABLE, REG_V30, REG_P2, REG_R9, REG_R4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, bfloat16* address) => LoadVectorx3(mask, address);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svbfloat16x4_t svld4[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld4h, EA_SCALABLE, REG_V5, REG_P4, REG_R3, -32, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld4h, EA_SCALABLE, REG_V13, REG_P6, REG_R5, REG_R4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, bfloat16* address) => LoadVectorx4(mask, address);


        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint16_t svcnt[_bf16]_m(svuint16_t inactive, svbool_t pg, svbfloat16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_bf16]_x(svbool_t pg, svbfloat16_t op)
        ///   CNT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_bf16]_z(svbool_t pg, svbfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_AP_3A   CNT <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_cnt, EA_SCALABLE, REG_V28, REG_P3, REG_V3, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<bfloat16> value) => PopCount(value);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svbfloat16_t svrev[_bf16](svbfloat16_t op)
        ///   REV Zresult.H, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CJ_2A   REV <Pd>.<T>, <Pn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_P1, REG_P2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_CJ_2A   REV <Pd>.<T>, <Pn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_P4, REG_P5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_CJ_2A   REV <Pd>.<T>, <Pn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_P3, REG_P7, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_CJ_2A   REV <Pd>.<T>, <Pn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_P0, REG_P6, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_CG_2A   REV <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_V2, REG_V3, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CG_2A   REV <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_V2, REG_V4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CG_2A   REV <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_V7, REG_V1, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CG_2A   REV <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_rev, EA_SCALABLE, REG_V2, REG_V5, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        /// </summary>
        public static unsafe Vector<bfloat16> ReverseElement(Vector<bfloat16> value) => ReverseElement(value);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svbfloat16_t svsplice[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_CV_3A   SPLICE <Zd>.<T>, <Pv>, {<Zn1>.<T>, <Zn2>.<T>}
        ///    theEmitter->emitIns_R_R_R(INS_sve_splice, EA_SCALABLE, REG_V0, REG_P0, REG_V30, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_CV_3A   SPLICE <Zd>.<T>, <Pv>, {<Zn1>.<T>, <Zn2>.<T>}
        ///    theEmitter->emitIns_R_R_R(INS_sve_splice, EA_SCALABLE, REG_V3, REG_P7, REG_V27, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_CV_3B   SPLICE <Zdn>.<T>, <Pv>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_splice, EA_SCALABLE, REG_V1, REG_P1, REG_V29, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_CV_3B   SPLICE <Zdn>.<T>, <Pv>, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_splice, EA_SCALABLE, REG_V2, REG_P6, REG_V28, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<bfloat16> Splice(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right) => Splice(mask, left, right);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        ///   ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JD_4A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P6, REG_R1, REG_R2, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_JD_4A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_R4, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_JD_4A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P2, REG_R4, REG_R0, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_JJ_4A   ST1H {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P1, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_JJ_4A   ST1H {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V3, REG_P1, REG_R5, REG_V4, INS_OPTS_SCALABLE_S_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_JJ_4A_B   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V2, REG_P3, REG_R1, REG_V4, INS_OPTS_SCALABLE_D_UXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_JJ_4A_B   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod> #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V2, REG_P3, REG_R1, REG_V4, INS_OPTS_SCALABLE_D_SXTW, INS_SCALABLE_OPTS_MOD_N);
        ///    IF_SVE_JJ_4A_C   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P5, REG_R1, REG_V3, INS_OPTS_SCALABLE_D_UXTW);
        ///    IF_SVE_JJ_4A_C   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P5, REG_R1, REG_V3, INS_OPTS_SCALABLE_D_SXTW);
        ///    IF_SVE_JJ_4A_D   ST1H {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V7, REG_P5, REG_R4, REG_V1, INS_OPTS_SCALABLE_S_UXTW);
        ///    IF_SVE_JJ_4A_D   ST1H {<Zt>.S }, <Pg>, [<Xn|SP>, <Zm>.S, <mod>]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V7, REG_P5, REG_R4, REG_V1, INS_OPTS_SCALABLE_S_SXTW);
        ///    IF_SVE_JN_3A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, 3, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JN_3A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_JN_3A   ST1H {<Zt>.<T>}, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V0, REG_P3, REG_R4, -2, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_JJ_4B   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, REG_V4, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_LSL_N);
        ///    IF_SVE_JJ_4B_E   ST1H {<Zt>.D }, <Pg>, [<Xn|SP>, <Zm>.D]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st1h, EA_SCALABLE, REG_V1, REG_P4, REG_R3, REG_V2, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_JI_3A_A   ST1H {<Zt>.S }, <Pg>, [<Zn>.S{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P3, REG_V2, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_JI_3A_A   ST1H {<Zt>.S }, <Pg>, [<Zn>.S{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P3, REG_V2, 62, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_JI_3A_A   ST1H {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P3, REG_V2, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_JI_3A_A   ST1H {<Zt>.D }, <Pg>, [<Zn>.D{, #<imm>}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st1h, EA_SCALABLE, REG_V5, REG_P3, REG_V2, 62, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST2H {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st2h, EA_SCALABLE, REG_V6, REG_P7, REG_R8, -16, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST2H {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st2h, EA_SCALABLE, REG_V2, REG_P3, REG_R5, REG_R6, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>{, #<imm>,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st3h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, -24, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>, <Xm>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st3h, EA_SCALABLE, REG_V1, REG_P0, REG_R3, REG_R8, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>{,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st4h, EA_SCALABLE, REG_V3, REG_P5, REG_R2, -32, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st4h, EA_SCALABLE, REG_V1, REG_P0, REG_R9, REG_R8, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3, Vector<bfloat16> Value4) data) => Store(mask, address, Value1,);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        ///   STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JM_3A   STNT1H {<Zt>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_stnt1h, EA_SCALABLE, REG_V9, REG_P1, REG_R0, -5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IZ_4A   STNT1H {<Zt>.S }, <Pg>, [<Zn>.S{, <Xm>}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_stnt1h, EA_SCALABLE, REG_V2, REG_P7, REG_V6, REG_R5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_IZ_4A_A   STNT1H {<Zt>.D }, <Pg>, [<Zn>.D{, <Xm>}]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_stnt1h, EA_SCALABLE, REG_V5, REG_P3, REG_V1, REG_R2, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_JB_4A   STNT1H {<Zt>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_stnt1h, EA_SCALABLE, REG_V0, REG_P1, REG_R2, REG_R3, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data) => StoreNonTemporal(mask, address, data);


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   TRN1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   TRN1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   TRN1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn1, EA_SCALABLE, REG_P1, REG_P3, REG_P4, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeEven(Vector<bfloat16> left, Vector<bfloat16> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   TRN2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   TRN2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   TRN2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_trn2, EA_SCALABLE, REG_P5, REG_P2, REG_P7, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeOdd(Vector<bfloat16> left, Vector<bfloat16> right) => TransposeOdd(left, right);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP1 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   UZP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   UZP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   UZP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_S);
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipEven(Vector<bfloat16> left, Vector<bfloat16> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP2 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   UZP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   UZP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   UZP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_uzp2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipOdd(Vector<bfloat16> left, Vector<bfloat16> right) => UnzipOdd(left, right);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svbfloat16_t svtbl[_bf16](svbfloat16_t data, svuint16_t indices)
        ///   TBL Zresult.H, Zdata.H, Zindices.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup(Vector<bfloat16> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svbfloat16_t svtbl2[_bf16](svbfloat16x2_t data, svuint16_t indices)
        ///   TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_BZ_3A   TBL <Zd>.<T>, {<Zn>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        ///    IF_SVE_BZ_3A_A   TBL <Zd>.<T>, {<Zn1>.<T>, <Zn2>.<T>}, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbl, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_WITH_VECTOR_PAIR);
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup((Vector<bfloat16> data1, Vector<bfloat16> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svbfloat16_t svtbx[_bf16](svbfloat16_t fallback, svbfloat16_t data, svuint16_t indices)
        ///   TBX Ztied.H, Zdata.H, Zindices.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BZ_3A   TBX <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbx, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_BZ_3A   TBX <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbx, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookupExtension(Vector<bfloat16> fallback, Vector<bfloat16> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP2 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V30, REG_V31, REG_V0, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   ZIP2 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V1, REG_V2, REG_V3, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   ZIP2 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   ZIP2 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip2, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<bfloat16> ZipHigh(Vector<bfloat16> left, Vector<bfloat16> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP1 Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BR_3A   ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V24, REG_V25, REG_V26, INS_OPTS_SCALABLE_B, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3A   ZIP1 <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V27, REG_V28, REG_V29, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_BR_3B   ZIP1 <Zd>.Q, <Zn>.Q, <Zm>.Q
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_Q, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_CI_3A   ZIP1 <Pd>.<T>, <Pn>.<T>, <Pm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_zip1, EA_SCALABLE, REG_P0, REG_P0, REG_P0, INS_OPTS_SCALABLE_B);
        /// </summary>
        public static unsafe Vector<bfloat16> ZipLow(Vector<bfloat16> left, Vector<bfloat16> right) => ZipLow(left, right);

    }
}


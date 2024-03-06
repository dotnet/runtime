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
    public abstract class SveFp16 : AdvSimd
    {
        internal SveFp16() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  Abs : Absolute value

        /// <summary>
        /// svfloat16_t svabs[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FABS Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FABS Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svabs[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FABS Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FABS Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svabs[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FABS Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_AP_3A   FABS <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fabs, EA_SCALABLE, REG_V27, REG_P4, REG_V4, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Abs(Vector<half> value) => Abs(value);


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGT Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FACGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_facgt, EA_SCALABLE, REG_P15, REG_P1, REG_V20, REG_V21, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThan(left, right);


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGE Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FACGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_facge, EA_SCALABLE, REG_P0, REG_P0, REG_V10, REG_V31, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThanOrEqual(left, right);


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGT Presult.H, Pg/Z, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FACGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_facgt, EA_SCALABLE, REG_P15, REG_P1, REG_V20, REG_V21, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, Vector<half> right) => AbsoluteCompareLessThan(left, right);


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGE Presult.H, Pg/Z, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FACGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_facge, EA_SCALABLE, REG_P0, REG_P0, REG_V10, REG_V31, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, Vector<half> right) => AbsoluteCompareLessThanOrEqual(left, right);


        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svfloat16_t svabd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svabd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svabd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FABD Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FABD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fabd, EA_SCALABLE, REG_V24, REG_P3, REG_V11, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, Vector<half> right) => AbsoluteDifference(left, right);


        ///  Add : Add

        /// <summary>
        /// svfloat16_t svadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   FADD Zresult.H, Zop1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FADD Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fadd, EA_SCALABLE, REG_V25, REG_P2, REG_V10, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HK_3A   FADD <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fadd, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_HM_2A   FADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fadd, EA_SCALABLE, REG_V0, REG_P0, 0.5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HM_2A   FADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fadd, EA_SCALABLE, REG_V0, REG_P1, 1.0, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Add(Vector<half> left, Vector<half> right) => Add(left, right);


        ///  AddAcross : Add reduction

        /// <summary>
        /// float16_t svaddv[_f16](svbool_t pg, svfloat16_t op)
        ///   FADDV Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HE_3A   FADDV <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_faddv, EA_2BYTE, REG_V21, REG_P7, REG_V7, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AddAcross(Vector<half> value) => AddAcross(value);


        ///  AddPairwise : Add pairwise

        /// <summary>
        /// svfloat16_t svaddp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svaddp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GR_3A   FADDP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_faddp, EA_SCALABLE, REG_V16, REG_P3, REG_V19, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AddPairwise(Vector<half> left, Vector<half> right) => AddPairwise(left, right);


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svfloat16_t svcadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation
        /// svfloat16_t svcadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation
        /// svfloat16_t svcadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FCADD Zresult.H, Pg/M, Zresult.H, Zop2.H, #imm_rotation
        ///
        /// codegenarm64test:
        ///    IF_SVE_GP_3A   FCADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fcadd, EA_SCALABLE, REG_V0, REG_P1, REG_V2, 90, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GP_3A   FCADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fcadd, EA_SCALABLE, REG_V0, REG_P1, REG_V2, 270, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GP_3A   FCADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fcadd, EA_SCALABLE, REG_V0, REG_P1, REG_V2, 270, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GP_3A   FCADD <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fcadd, EA_SCALABLE, REG_V0, REG_P1, REG_V2, 270, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AddRotateComplex(Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float16_t svadda[_f16](svbool_t pg, float16_t initial, svfloat16_t op)
        ///   FADDA Htied, Pg, Htied, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HJ_3A   FADDA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_2BYTE, REG_V21, REG_P6, REG_V14, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HJ_3A   FADDA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_4BYTE, REG_V22, REG_P5, REG_V13, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HJ_3A   FADDA <V><dn>, <Pg>, <V><dn>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fadda, EA_8BYTE, REG_V23, REG_P4, REG_V12, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> AddSequentialAcross(Vector<half> initial, Vector<half> value) => AddSequentialAcross(initial, value);


        ///  CompareEqual : Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMEQ Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMEQ <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmeq, EA_SCALABLE, REG_P2, REG_P4, REG_V28, REG_V8, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HI_3A   FCMEQ <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmeq, EA_SCALABLE, REG_P2, REG_P3, REG_V4, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareEqual(Vector<half> left, Vector<half> right) => CompareEqual(left, right);


        ///  CompareGreaterThan : Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGT Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmgt, EA_SCALABLE, REG_P3, REG_P6, REG_V18, REG_V28, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HI_3A   FCMGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmgt, EA_SCALABLE, REG_P11, REG_P5, REG_V2, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, Vector<half> right) => CompareGreaterThan(left, right);


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGE Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmge, EA_SCALABLE, REG_P13, REG_P5, REG_V8, REG_V18, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HI_3A   FCMGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmge, EA_SCALABLE, REG_P1, REG_P2, REG_V3, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => CompareGreaterThanOrEqual(left, right);


        ///  CompareLessThan : Compare less than

        /// <summary>
        /// svbool_t svcmplt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGT Presult.H, Pg/Z, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmgt, EA_SCALABLE, REG_P3, REG_P6, REG_V18, REG_V28, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HI_3A   FCMGT <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmgt, EA_SCALABLE, REG_P11, REG_P5, REG_V2, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareLessThan(Vector<half> left, Vector<half> right) => CompareLessThan(left, right);


        ///  CompareLessThanOrEqual : Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGE Presult.H, Pg/Z, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmge, EA_SCALABLE, REG_P13, REG_P5, REG_V8, REG_V18, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HI_3A   FCMGE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmge, EA_SCALABLE, REG_P1, REG_P2, REG_V3, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, Vector<half> right) => CompareLessThanOrEqual(left, right);


        ///  CompareNotEqualTo : Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMNE Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMNE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmne, EA_SCALABLE, REG_P11, REG_P1, REG_V21, REG_V10, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HI_3A   FCMNE <Pd>.<T>, <Pg>/Z, <Zn>.<T>, #0.0
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcmne, EA_SCALABLE, REG_P1, REG_P0, REG_V5, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, Vector<half> right) => CompareNotEqualTo(left, right);


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMUO Presult.H, Pg/Z, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HT_4A   FCMUO <Pd>.<T>, <Pg>/Z, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fcmuo, EA_SCALABLE, REG_P5, REG_P2, REG_V31, REG_V20, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> CompareUnordered(Vector<half> left, Vector<half> right) => CompareUnordered(left, right);


        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svfloat16_t svuzp1q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> ConcatenateEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateEvenInt128FromTwoInputs(left, right);


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svfloat16_t svuzp2q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> ConcatenateOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateOddInt128FromTwoInputs(left, right);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
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
        public static unsafe Vector<half> ConditionalExtractAfterLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// float16_t svclasta[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data)
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
        public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
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
        public static unsafe Vector<half> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, Vector<half> defaultScalar, Vector<half> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
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
        public static unsafe Vector<half> ConditionalExtractLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// float16_t svclastb[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data)
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
        public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
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
        public static unsafe Vector<half> ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, Vector<half> fallback, Vector<half> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svfloat16_t svsel[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> ConditionalSelect(Vector<half> mask, Vector<half> left, Vector<half> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToDouble : Floating-point convert

        /// <summary>
        /// svfloat64_t svcvt_f64[_f16]_m(svfloat64_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVT Ztied.D, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.D, Pg/M, Zop.H
        /// svfloat64_t svcvt_f64[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVT Ztied.D, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.D, Pg/M, Zop.H
        /// svfloat64_t svcvt_f64[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.D, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvt - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<half> value) => ConvertToDouble(value);


        ///  ConvertToHalf : Floating-point convert

        /// <summary>
        /// svfloat16_t svcvt_f16[_s16]_m(svfloat16_t inactive, svbool_t pg, svint16_t op)
        ///   SCVTF Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svcvt_f16[_s16]_x(svbool_t pg, svint16_t op)
        ///   SCVTF Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svcvt_f16[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; SCVTF Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_scvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<short> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_s32]_m(svfloat16_t inactive, svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.H, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_s32]_x(svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.H, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; SCVTF Zresult.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///   sve_scvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<int> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_s64]_m(svfloat16_t inactive, svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.H, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_s64]_x(svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.H, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.H, Pg/M, Zop.D
        ///
        /// codegenarm64test:
        ///   sve_scvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<long> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u16]_m(svfloat16_t inactive, svbool_t pg, svuint16_t op)
        ///   UCVTF Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svcvt_f16[_u16]_x(svbool_t pg, svuint16_t op)
        ///   UCVTF Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svcvt_f16[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; UCVTF Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_ucvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<ushort> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u32]_m(svfloat16_t inactive, svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.H, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_u32]_x(svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.H, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; UCVTF Zresult.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///   sve_ucvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<uint> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u64]_m(svfloat16_t inactive, svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.H, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.H, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.H, Pg/M, Zop.D
        ///
        /// codegenarm64test:
        ///   sve_ucvtf - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<ulong> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_f32]_m(svfloat16_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVT Ztied.H, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVT Ztied.H, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.H, Pg/M, Zop.S
        /// svfloat16_t svcvt_f16[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVT Zresult.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///   sve_fcvt - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<float> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_f64]_m(svfloat16_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVT Ztied.H, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVT Ztied.H, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.H, Pg/M, Zop.D
        /// svfloat16_t svcvt_f16[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.H, Pg/M, Zop.D
        ///
        /// codegenarm64test:
        ///   sve_fcvt - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<double> value) => ConvertToHalf(value);


        ///  ConvertToInt16 : Floating-point convert

        /// <summary>
        /// svint16_t svcvt_s16[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.H, Pg/M, Zop.H
        /// svint16_t svcvt_s16[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.H, Pg/M, Zop.H
        /// svint16_t svcvt_s16[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FCVTZS Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzs - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<short> ConvertToInt16(Vector<half> value) => ConvertToInt16(value);


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f16]_m(svint32_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.S, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.H
        /// svint32_t svcvt_s32[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.S, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.H
        /// svint32_t svcvt_s32[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZS Zresult.S, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzs - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<half> value) => ConvertToInt32(value);


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f16]_m(svint64_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.D, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.H
        /// svint64_t svcvt_s64[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZS Ztied.D, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.H
        /// svint64_t svcvt_s64[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzs - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<half> value) => ConvertToInt64(value);


        ///  ConvertToSingle : Floating-point convert

        /// <summary>
        /// svfloat32_t svcvt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVT Ztied.S, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.S, Pg/M, Zop.H
        /// svfloat32_t svcvt_f32[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVT Ztied.S, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.S, Pg/M, Zop.H
        /// svfloat32_t svcvt_f32[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVT Zresult.S, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvt - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<half> value) => ConvertToSingle(value);


        ///  ConvertToUInt16 : Floating-point convert

        /// <summary>
        /// svuint16_t svcvt_u16[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcvt_u16[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcvt_u16[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FCVTZU Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzu - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<ushort> ConvertToUInt16(Vector<half> value) => ConvertToUInt16(value);


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f16]_m(svuint32_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.S, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.H
        /// svuint32_t svcvt_u32[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.S, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.H
        /// svuint32_t svcvt_u32[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZU Zresult.S, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzu - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<half> value) => ConvertToUInt32(value);


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f16]_m(svuint64_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.D, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.H
        /// svuint64_t svcvt_u64[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTZU Ztied.D, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.H
        /// svuint64_t svcvt_u64[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///   sve_fcvtzu - not implemented in coreclr
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<half> value) => ConvertToUInt64(value);



        ///  CreateFalseMaskHalf : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        ///
        /// codegenarm64test:
        ///    IF_SVE_DJ_1A   PFALSE <Pd>.B
        ///    theEmitter->emitIns_R(INS_sve_pfalse, EA_SCALABLE, REG_P13);
        /// </summary>
        public static unsafe Vector<half> CreateFalseMaskHalf() => CreateFalseMaskHalf();


        ///  CreateTrueMaskHalf : Set predicate elements to true

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
        public static unsafe Vector<half> CreateTrueMaskHalf([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskHalf(pattern);


        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_f16](const float16_t *op1, const float16_t *op2)
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
        public static unsafe Vector<half> CreateWhileReadAfterWriteMask(half* left, half* right) => CreateWhileReadAfterWriteMask(left, right);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_f16](const float16_t *op1, const float16_t *op2)
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
        public static unsafe Vector<half> CreateWhileWriteAfterReadMask(half* left, half* right) => CreateWhileWriteAfterReadMask(left, right);


        ///  Divide : Divide

        /// <summary>
        /// svfloat16_t svdiv[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FDIV Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svdiv[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FDIV Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FDIVR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svdiv[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FDIV Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FDIVR Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FDIV <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fdiv, EA_SCALABLE, REG_V28, REG_P0, REG_V7, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Divide(Vector<half> left, Vector<half> right) => Divide(left, right);



        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svfloat16_t svcvtnt_f16[_f32]_m(svfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   FCVTNT Ztied.H, Pg/M, Zop.S
        /// svfloat16_t svcvtnt_f16[_f32]_x(svfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   FCVTNT Ztied.H, Pg/M, Zop.S
        ///
        /// codegenarm64test:
        ///    IF_SVE_GQ_3A   FCVTNT <Zd>.H, <Pg>/M, <Zn>.S
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcvtnt, EA_SCALABLE, REG_V18, REG_P3, REG_V9, INS_OPTS_S_TO_H);
        ///    IF_SVE_GQ_3A   FCVTNT <Zd>.S, <Pg>/M, <Zn>.D
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcvtnt, EA_SCALABLE, REG_V12, REG_P3, REG_V5, INS_OPTS_D_TO_S);
        ///    IF_SVE_HG_2A   FCVTNT <Zd>.B, {<Zn1>.S-<Zn2>.S }
        ///    theEmitter->emitIns_R_R(INS_sve_fcvtnt, EA_SCALABLE, REG_V14, REG_V15);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> DownConvertNarrowingUpper(Vector<float> value) => DownConvertNarrowingUpper(value);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svfloat16_t svdup_lane[_f16](svfloat16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// svfloat16_t svdupq_lane[_f16](svfloat16_t data, uint64_t index)
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
        public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op)
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
        public static unsafe half ExtractAfterLastScalar(Vector<half> value) => ExtractAfterLastScalar(value);


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op)
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
        public static unsafe Vector<half> ExtractAfterLastVector(Vector<half> value) => ExtractAfterLastVector(value);


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op)
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
        public static unsafe half ExtractLastScalar(Vector<half> value) => ExtractLastScalar(value);


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op)
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
        public static unsafe Vector<half> ExtractLastVector(Vector<half> value) => ExtractLastVector(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svfloat16_t svext[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        ///
        /// codegenarm64test:
        ///   sve_ext - not implemented in coreclr
        /// </summary>
        public static unsafe Vector<half> ExtractVector(Vector<half> upper, Vector<half> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat16_t svexpa[_f16](svuint16_t op)
        ///   FEXPA Zresult.H, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BJ_2A   FEXPA <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_fexpa, EA_SCALABLE, REG_V0, REG_V1, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_BJ_2A   FEXPA <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_fexpa, EA_SCALABLE, REG_V3, REG_V0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_BJ_2A   FEXPA <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_fexpa, EA_SCALABLE, REG_V1, REG_V0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> FloatingPointExponentialAccelerator(Vector<ushort> value) => FloatingPointExponentialAccelerator(value);


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat16_t svmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   FMAD Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   FMAD Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAD Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; FMAD Zresult.H, Pg/M, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fmla, EA_SCALABLE, REG_V0, REG_P0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GU_3A   FMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V0, REG_V2, REG_V1, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V4, REG_V6, REG_V3, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3B   FMLA <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V1, REG_V0, REG_V0, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GU_3B   FMLA <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V3, REG_V2, REG_V5, 1, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right) => FusedMultiplyAdd(addend, left, right);


        ///  FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svfloat16_t svmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.H, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.H, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fmla, EA_SCALABLE, REG_V0, REG_P0, REG_V1, REG_V2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GU_3A   FMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V0, REG_V2, REG_V1, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V4, REG_V6, REG_V3, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3B   FMLA <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V1, REG_V0, REG_V0, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GU_3B   FMLA <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmla, EA_SCALABLE, REG_V3, REG_V2, REG_V5, 1, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAddBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        ///  FusedMultiplyAddNegated : Negated multiply-add, addend first

        /// <summary>
        /// svfloat16_t svnmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FNMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svnmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FNMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   FNMAD Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   FNMAD Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svnmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FNMLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FNMAD Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; FNMAD Zresult.H, Pg/M, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FNMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fnmla, EA_SCALABLE, REG_V6, REG_P4, REG_V7, REG_V8, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAddNegated(Vector<half> addend, Vector<half> left, Vector<half> right) => FusedMultiplyAddNegated(addend, left, right);


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   FMSB Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   FMSB Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMSB Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; FMSB Zresult.H, Pg/M, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FMLS <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fmls, EA_SCALABLE, REG_V3, REG_P2, REG_V4, REG_V5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLS <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLS <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3B   FMLS <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V5, REG_V4, REG_V10, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GU_3B   FMLS <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V7, REG_V6, REG_V15, 1, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right) => FusedMultiplySubtract(minuend, left, right);


        ///  FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svmls_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.H, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.H, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FMLS <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fmls, EA_SCALABLE, REG_V3, REG_P2, REG_V4, REG_V5, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLS <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3A   FMLS <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GU_3B   FMLS <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V5, REG_V4, REG_V10, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GU_3B   FMLS <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmls, EA_SCALABLE, REG_V7, REG_V6, REG_V15, 1, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtractBySelectedScalar(Vector<half> minuend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        ///  FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svnmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FNMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svnmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FNMLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   FNMSB Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   FNMSB Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svfloat16_t svnmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FNMLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FNMSB Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; FNMSB Zresult.H, Pg/M, Zop2.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HU_4A   FNMLS <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_fnmls, EA_SCALABLE, REG_V9, REG_P6, REG_V10, REG_V11, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtractNegated(Vector<half> minuend, Vector<half> left, Vector<half> right) => FusedMultiplySubtractNegated(minuend, left, right);


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
        public static unsafe ulong GetActiveElementCount(Vector<half> mask, Vector<half> from) => GetActiveElementCount(mask, from);


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svfloat16_t svinsr[_n_f16](svfloat16_t op1, float16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        ///
        /// codegenarm64test:
        ///   sve_insr - not implemented in coreclr
        /// </summary>
        public static unsafe Vector<half> InsertIntoShiftedVector(Vector<half> left, half right) => InsertIntoShiftedVector(left, right);


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svfloat16_t svtrn1q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> InterleaveEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveEvenInt128FromTwoInputs(left, right);


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svfloat16_t svzip2q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svfloat16_t svzip1q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svfloat16_t svtrn2q[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> InterleaveOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveOddInt128FromTwoInputs(left, right);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svfloat16_t svld1[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVector(Vector<half> mask, half* address) => LoadVector(mask, address);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svfloat16_t svld1rq[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVector128AndReplicateToVector(Vector<half> mask, half* address) => LoadVector128AndReplicateToVector(mask, address);


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svfloat16_t svld1ro[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVector256AndReplicateToVector(Vector<half> mask, half* address) => LoadVector256AndReplicateToVector(mask, address);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svfloat16_t svldff1[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, half* address) => LoadVectorFirstFaulting(mask, address);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svfloat16_t svldnf1[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVectorNonFaulting(half* address) => LoadVectorNonFaulting(address);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svfloat16_t svldnt1[_f16](svbool_t pg, const float16_t *base)
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
        public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, half* address) => LoadVectorNonTemporal(mask, address);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svfloat16x2_t svld2[_f16](svbool_t pg, const float16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD2H {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>{, #<imm>, MUL
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld2h, EA_SCALABLE, REG_V6, REG_P5, REG_R4, 8, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD2H {<Zt1>.H, <Zt2>.H }, <Pg>/Z, [<Xn|SP>, <Xm>, LSL
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld2h, EA_SCALABLE, REG_V8, REG_P5, REG_R9, REG_R10, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, half* address) => LoadVectorx2(mask, address);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svfloat16x3_t svld3[_f16](svbool_t pg, const float16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>{,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld3h, EA_SCALABLE, REG_V0, REG_P0, REG_R0, 21, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>/Z, [<Xn|SP>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld3h, EA_SCALABLE, REG_V30, REG_P2, REG_R9, REG_R4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, half* address) => LoadVectorx3(mask, address);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svfloat16x4_t svld4[_f16](svbool_t pg, const float16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_IS_3A   LD4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_ld4h, EA_SCALABLE, REG_V5, REG_P4, REG_R3, -32, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_IT_4A   LD4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>/Z,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_ld4h, EA_SCALABLE, REG_V13, REG_P6, REG_R5, REG_R4, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, half* address) => LoadVectorx4(mask, address);


        ///  Log2 : Base 2 logarithm as integer

        /// <summary>
        /// svint16_t svlogb[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FLOGB Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FLOGB Zresult.H, Pg/M, Zop.H
        /// svint16_t svlogb[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FLOGB Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FLOGB Zresult.H, Pg/M, Zop.H
        /// svint16_t svlogb[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FLOGB Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HP_3A   FLOGB <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_flogb, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HP_3A   FLOGB <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_flogb, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HP_3A   FLOGB <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_flogb, EA_SCALABLE, REG_V31, REG_P7, REG_V31, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<short> Log2(Vector<half> value) => Log2(value);


        ///  Max : Maximum

        /// <summary>
        /// svfloat16_t svmax[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmax[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmax[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAX Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMAX <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmax, EA_SCALABLE, REG_V30, REG_P2, REG_V5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HM_2A   FMAX <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmax, EA_SCALABLE, REG_V1, REG_P0, 0.0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HM_2A   FMAX <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmax, EA_SCALABLE, REG_V1, REG_P0, 1.0, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Max(Vector<half> left, Vector<half> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// float16_t svmaxv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMAXV Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HE_3A   FMAXV <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmaxv, EA_4BYTE, REG_V23, REG_P5, REG_V5, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MaxAcross(Vector<half> value) => MaxAcross(value);


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat16_t svmaxnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmaxnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMAXNM Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmaxnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMAXNM Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMAXNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmaxnm, EA_SCALABLE, REG_V31, REG_P3, REG_V4, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HM_2A   FMAXNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmaxnm, EA_SCALABLE, REG_V3, REG_P4, 0.0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMAXNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmaxnm, EA_SCALABLE, REG_V3, REG_P4, 1.0, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MaxNumber(Vector<half> left, Vector<half> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float16_t svmaxnmv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMAXNMV Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HE_3A   FMAXNMV <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmaxnmv, EA_2BYTE, REG_V22, REG_P6, REG_V6, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MaxNumberAcross(Vector<half> value) => MaxNumberAcross(value);


        ///  MaxNumberPairwise : Maximum number pairwise

        /// <summary>
        /// svfloat16_t svmaxnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmaxnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GR_3A   FMAXNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmaxnmp, EA_SCALABLE, REG_V17, REG_P4, REG_V18, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MaxNumberPairwise(Vector<half> left, Vector<half> right) => MaxNumberPairwise(left, right);


        ///  MaxPairwise : Maximum pairwise

        /// <summary>
        /// svfloat16_t svmaxp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmaxp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXP Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GR_3A   FMAXP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmaxp, EA_SCALABLE, REG_V18, REG_P5, REG_V17, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MaxPairwise(Vector<half> left, Vector<half> right) => MaxPairwise(left, right);


        ///  Min : Minimum

        /// <summary>
        /// svfloat16_t svmin[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmin[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmin[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMIN Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMIN <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmin, EA_SCALABLE, REG_V0, REG_P4, REG_V3, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMIN <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmin, EA_SCALABLE, REG_V6, REG_P5, 0.0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HM_2A   FMIN <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmin, EA_SCALABLE, REG_V6, REG_P5, 1.0, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Min(Vector<half> left, Vector<half> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// float16_t svminv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMINV Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HE_3A   FMINV <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fminv, EA_4BYTE, REG_V25, REG_P3, REG_V3, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MinAcross(Vector<half> value) => MinAcross(value);


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat16_t svminnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svminnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNM Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMINNM Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svminnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMINNM Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMINNM Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMINNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fminnm, EA_SCALABLE, REG_V1, REG_P5, REG_V2, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HM_2A   FMINNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fminnm, EA_SCALABLE, REG_V2, REG_P4, 0.0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HM_2A   FMINNM <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fminnm, EA_SCALABLE, REG_V2, REG_P4, 1.0, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MinNumber(Vector<half> left, Vector<half> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float16_t svminnmv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMINNMV Hresult, Pg, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HE_3A   FMINNMV <V><d>, <Pg>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fminnmv, EA_8BYTE, REG_V24, REG_P4, REG_V4, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MinNumberAcross(Vector<half> value) => MinNumberAcross(value);


        ///  MinNumberPairwise : Minimum number pairwise

        /// <summary>
        /// svfloat16_t svminnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svminnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GR_3A   FMINNMP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fminnmp, EA_SCALABLE, REG_V19, REG_P6, REG_V16, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MinNumberPairwise(Vector<half> left, Vector<half> right) => MinNumberPairwise(left, right);


        ///  MinPairwise : Minimum pairwise

        /// <summary>
        /// svfloat16_t svminp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svminp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINP Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GR_3A   FMINP <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fminp, EA_SCALABLE, REG_V20, REG_P7, REG_V15, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MinPairwise(Vector<half> left, Vector<half> right) => MinPairwise(left, right);


        ///  Multiply : Multiply

        /// <summary>
        /// svfloat16_t svmul[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmul[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   FMUL Zresult.H, Zop1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmul[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMUL Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmul, EA_SCALABLE, REG_V2, REG_P6, REG_V1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HK_3A   FMUL <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmul, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V0, REG_V2, REG_V1, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V4, REG_V6, REG_V3, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V1, REG_V0, REG_V0, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V3, REG_V2, REG_V5, 1, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_V4, REG_V10, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V7, REG_V6, REG_V15, 1, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_P1, 0.5, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_P1, 2.0, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right) => Multiply(left, right);





        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svfloat16_t svcmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation
        /// svfloat16_t svcmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation
        /// svfloat16_t svcmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FCMLA Zresult.H, Pg/M, Zop2.H, Zop3.H, #imm_rotation
        ///
        /// codegenarm64test:
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V0, REG_V1, REG_V0, 0, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_V3, REG_V5, 1, 90, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V4, REG_V5, REG_V10, 0, 180, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V6, REG_V7, REG_V15, 1, 270, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P1, REG_V3, REG_V4, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V0, REG_P2, REG_V1, REG_V5, 90, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P3, REG_V0, REG_V6, 180, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P3, REG_V0, REG_V6, 270, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);


        ///  MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

        /// <summary>
        /// svfloat16_t svcmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        ///   FCMLA Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation
        ///
        /// codegenarm64test:
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V0, REG_V1, REG_V0, 0, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_V3, REG_V5, 1, 90, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V4, REG_V5, REG_V10, 0, 180, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GV_3A   FCMLA <Zda>.S, <Zn>.S, <Zm>.S[<imm>], <const>
        ///    theEmitter->emitIns_R_R_R_I_I(INS_sve_fcmla, EA_SCALABLE, REG_V6, REG_V7, REG_V15, 1, 270, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P1, REG_V3, REG_V4, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V0, REG_P2, REG_V1, REG_V5, 90, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P3, REG_V0, REG_V6, 180, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GT_4A   FCMLA <Zda>.<T>, <Pg>/M, <Zn>.<T>, <Zm>.<T>, <const>
        ///    theEmitter->emitIns_R_R_R_R_I(INS_sve_fcmla, EA_SCALABLE, REG_V2, REG_P3, REG_V0, REG_V6, 270, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplexBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);


        ///  MultiplyAddWideningLower : Multiply-add long (bottom)

        /// <summary>
        /// svfloat32_t svmlalb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLALB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GN_3A   FMLALB <Zda>.H, <Zn>.B, <Zm>.B
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_GZ_3A   FMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlalb, EA_SCALABLE, REG_V8, REG_V9, REG_V4, 4, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLALB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalb, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GN_3A   FMLALB <Zda>.H, <Zn>.B, <Zm>.B
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalb, EA_SCALABLE, REG_V0, REG_V1, REG_V2, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_GZ_3A   FMLALB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlalb, EA_SCALABLE, REG_V8, REG_V9, REG_V4, 4, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLALB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalb, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);


        ///  MultiplyAddWideningUpper : Multiply-add long (top)

        /// <summary>
        /// svfloat32_t svmlalt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLALT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GN_3A   FMLALT <Zda>.H, <Zn>.B, <Zm>.B
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_GZ_3A   FMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlalt, EA_SCALABLE, REG_V10, REG_V11, REG_V5, 5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLALT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalt, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GN_3A   FMLALT <Zda>.H, <Zn>.B, <Zm>.B
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalt, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_B);
        ///    IF_SVE_GZ_3A   FMLALT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlalt, EA_SCALABLE, REG_V10, REG_V11, REG_V5, 5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLALT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlalt, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);


        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svfloat16_t svmul_lane[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm_index)
        ///   FMUL Zresult.H, Zop1.H, Zop2.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmul, EA_SCALABLE, REG_V2, REG_P6, REG_V1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HK_3A   FMUL <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmul, EA_SCALABLE, REG_V3, REG_V4, REG_V5, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V0, REG_V2, REG_V1, 0, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V4, REG_V6, REG_V3, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V8, REG_V10, REG_V5, 2, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3A   FMUL <Zda>.S, <Zn>.S, <Zm>.S[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V12, REG_V14, REG_V7, 3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V1, REG_V0, REG_V0, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V3, REG_V2, REG_V5, 1, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_V4, REG_V10, 0, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_GX_3B   FMUL <Zda>.D, <Zn>.D, <Zm>.D[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmul, EA_SCALABLE, REG_V7, REG_V6, REG_V15, 1, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_P1, 0.5, INS_OPTS_SCALABLE_D);
        ///    IF_SVE_HM_2A   FMUL <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fmul, EA_SCALABLE, REG_V5, REG_P1, 2.0, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> MultiplyBySelectedScalar(Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat16_t svmulx[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMULX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmulx[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMULX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FMULX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmulx[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FMULX Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FMULX Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FMULX <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmulx, EA_SCALABLE, REG_V3, REG_P7, REG_V0, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> MultiplyExtended(Vector<half> left, Vector<half> right) => MultiplyExtended(left, right);





        ///  MultiplySubtractWideningLower : Multiply-subtract long (bottom)

        /// <summary>
        /// svfloat32_t svmlslb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLSLB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   FMLSLB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlslb, EA_SCALABLE, REG_V12, REG_V13, REG_V6, 6, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLSLB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlslb, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   FMLSLB <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlslb, EA_SCALABLE, REG_V12, REG_V13, REG_V6, 6, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLSLB <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlslb, EA_SCALABLE, REG_V18, REG_V19, REG_V20, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);


        ///  MultiplySubtractWideningUpper : Multiply-subtract long (top)

        /// <summary>
        /// svfloat32_t svmlslt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLSLT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   FMLSLT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlslt, EA_SCALABLE, REG_V14, REG_V15, REG_V7, 7, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLSLT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlslt, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]
        ///
        /// codegenarm64test:
        ///    IF_SVE_GZ_3A   FMLSLT <Zda>.S, <Zn>.H, <Zm>.H[<imm>]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_fmlslt, EA_SCALABLE, REG_V14, REG_V15, REG_V7, 7, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HB_3A   FMLSLT <Zda>.S, <Zn>.H, <Zm>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fmlslt, EA_SCALABLE, REG_V21, REG_V22, REG_V23, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);


        ///  Negate : Negate

        /// <summary>
        /// svfloat16_t svneg[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FNEG Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FNEG Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svneg[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FNEG Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FNEG Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svneg[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FNEG Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_AP_3A   FNEG <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fneg, EA_SCALABLE, REG_V26, REG_P5, REG_V5, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Negate(Vector<half> value) => Negate(value);




        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint16_t svcnt[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   CNT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_AP_3A   CNT <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_cnt, EA_SCALABLE, REG_V28, REG_P3, REG_V3, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<half> value) => PopCount(value);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat16_t svrecpe[_f16](svfloat16_t op)
        ///   FRECPE Zresult.H, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HF_2A   FRECPE <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_frecpe, EA_SCALABLE, REG_V0, REG_V2, INS_OPTS_SCALABLE_H);
        /// </summary>
        public static unsafe Vector<half> ReciprocalEstimate(Vector<half> value) => ReciprocalEstimate(value);


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat16_t svrecpx[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRECPX Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRECPX Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrecpx[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRECPX Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRECPX Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrecpx[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRECPX Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HR_3A   FRECPX <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frecpx, EA_SCALABLE, REG_V5, REG_P5, REG_V5, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> ReciprocalExponent(Vector<half> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat16_t svrsqrte[_f16](svfloat16_t op)
        ///   FRSQRTE Zresult.H, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HF_2A   FRSQRTE <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_frsqrte, EA_SCALABLE, REG_V5, REG_V3, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HF_2A   FRSQRTE <Zd>.<T>, <Zn>.<T>
        ///    theEmitter->emitIns_R_R(INS_sve_frsqrte, EA_SCALABLE, REG_V9, REG_V5, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtEstimate(Vector<half> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat16_t svrsqrts[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   FRSQRTS Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HK_3A   FRSQRTS <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frsqrts, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_UNPREDICATED);
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtStep(Vector<half> left, Vector<half> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat16_t svrecps[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   FRECPS Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HK_3A   FRECPS <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frecps, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        /// </summary>
        public static unsafe Vector<half> ReciprocalStep(Vector<half> left, Vector<half> right) => ReciprocalStep(left, right);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svfloat16_t svrev[_f16](svfloat16_t op)
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
        public static unsafe Vector<half> ReverseElement(Vector<half> value) => ReverseElement(value);


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat16_t svrinta[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRINTA Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRINTA Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrinta[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRINTA Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRINTA Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrinta[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTA Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HQ_3A   FRINTA <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frinta, EA_SCALABLE, REG_V26, REG_P7, REG_V2, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> RoundAwayFromZero(Vector<half> value) => RoundAwayFromZero(value);


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat16_t svrintn[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRINTN Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRINTN Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintn[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRINTN Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRINTN Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintn[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTN Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HQ_3A   FRINTN <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frintn, EA_SCALABLE, REG_V29, REG_P4, REG_V10, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> RoundToNearest(Vector<half> value) => RoundToNearest(value);


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat16_t svrintm[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRINTM Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRINTM Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintm[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRINTM Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRINTM Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintm[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTM Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HQ_3A   FRINTM <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frintm, EA_SCALABLE, REG_V28, REG_P5, REG_V0, INS_OPTS_SCALABLE_D);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> RoundToNegativeInfinity(Vector<half> value) => RoundToNegativeInfinity(value);


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat16_t svrintp[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRINTP Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRINTP Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintp[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRINTP Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRINTP Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintp[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTP Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HQ_3A   FRINTP <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frintp, EA_SCALABLE, REG_V30, REG_P3, REG_V11, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> RoundToPositiveInfinity(Vector<half> value) => RoundToPositiveInfinity(value);


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat16_t svrintz[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FRINTZ Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FRINTZ Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintz[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FRINTZ Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FRINTZ Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svrintz[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FRINTZ Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HQ_3A   FRINTZ <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_frintz, EA_SCALABLE, REG_V0, REG_P0, REG_V13, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> RoundToZero(Vector<half> value) => RoundToZero(value);




        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat16_t svscale[_f16]_m(svbool_t pg, svfloat16_t op1, svint16_t op2)
        ///   FSCALE Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svscale[_f16]_x(svbool_t pg, svfloat16_t op1, svint16_t op2)
        ///   FSCALE Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svscale[_f16]_z(svbool_t pg, svfloat16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FSCALE Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FSCALE <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fscale, EA_SCALABLE, REG_V4, REG_P6, REG_V31, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Scale(Vector<half> left, Vector<short> right) => Scale(left, right);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svfloat16_t svsplice[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> Splice(Vector<half> mask, Vector<half> left, Vector<half> right) => Splice(mask, left, right);


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat16_t svsqrt[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        ///   FSQRT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; FSQRT Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svsqrt[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FSQRT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; FSQRT Zresult.H, Pg/M, Zop.H
        /// svfloat16_t svsqrt[_f16]_z(svbool_t pg, svfloat16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; FSQRT Zresult.H, Pg/M, Zop.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HR_3A   FSQRT <Zd>.<T>, <Pg>/M, <Zn>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fsqrt, EA_SCALABLE, REG_V6, REG_P6, REG_V6, INS_OPTS_SCALABLE_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Sqrt(Vector<half> value) => Sqrt(value);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
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
        public static unsafe void Store(Vector<half> mask, half* address, Vector<half> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_f16](svbool_t pg, float16_t *base, svfloat16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST2H {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>{, #<imm>, MUL VL}]
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st2h, EA_SCALABLE, REG_V6, REG_P7, REG_R8, -16, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST2H {<Zt1>.H, <Zt2>.H }, <Pg>, [<Xn|SP>, <Xm>, LSL #1]
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st2h, EA_SCALABLE, REG_V2, REG_P3, REG_R5, REG_R6, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_f16](svbool_t pg, float16_t *base, svfloat16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>{, #<imm>,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st3h, EA_SCALABLE, REG_V1, REG_P2, REG_R3, -24, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST3H {<Zt1>.H, <Zt2>.H, <Zt3>.H }, <Pg>, [<Xn|SP>, <Xm>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st3h, EA_SCALABLE, REG_V1, REG_P0, REG_R3, REG_R8, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_f16](svbool_t pg, float16_t *base, svfloat16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        ///
        /// codegenarm64test:
        ///    IF_SVE_JO_3A   ST4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>, [<Xn|SP>{,
        ///    theEmitter->emitIns_R_R_R_I(INS_sve_st4h, EA_SCALABLE, REG_V3, REG_P5, REG_R2, -32, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_JC_4A   ST4H {<Zt1>.H, <Zt2>.H, <Zt3>.H, <Zt4>.H }, <Pg>,
        ///    theEmitter->emitIns_R_R_R_R(INS_sve_st4h, EA_SCALABLE, REG_V1, REG_P0, REG_R9, REG_R8, INS_OPTS_SCALABLE_H, INS_SCALABLE_OPTS_LSL_N);
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3, Vector<half> Value4) data) => Store(mask, address, Value1,);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
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
        public static unsafe void StoreNonTemporal(Vector<half> mask, half* address, Vector<half> data) => StoreNonTemporal(mask, address, data);


        ///  Subtract : Subtract

        /// <summary>
        /// svfloat16_t svsub[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svsub[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FSUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FSUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   FSUB Zresult.H, Zop1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svsub[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FSUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FSUBR Zresult.H, Pg/M, Zresult.H, Zop1.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HL_3A   FSUB <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fsub, EA_SCALABLE, REG_V5, REG_P5, REG_V30, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HK_3A   FSUB <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_fsub, EA_SCALABLE, REG_V12, REG_V13, REG_V14, INS_OPTS_SCALABLE_S, INS_SCALABLE_OPTS_UNPREDICATED);
        ///    IF_SVE_HM_2A   FSUB <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fsub, EA_SCALABLE, REG_V7, REG_P2, 0.5, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HM_2A   FSUB <Zdn>.<T>, <Pg>/M, <Zdn>.<T>, <const>
        ///    theEmitter->emitIns_R_R_F(INS_sve_fsub, EA_SCALABLE, REG_V7, REG_P2, 1.0, INS_OPTS_SCALABLE_H);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<half> Subtract(Vector<half> left, Vector<half> right) => Subtract(left, right);



        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn1[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> TransposeEven(Vector<half> left, Vector<half> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn2[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> TransposeOdd(Vector<half> left, Vector<half> right) => TransposeOdd(left, right);


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat16_t svtmad[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        ///   FTMAD Ztied1.H, Ztied1.H, Zop2.H, #imm3
        ///   MOVPRFX Zresult, Zop1; FTMAD Zresult.H, Zresult.H, Zop2.H, #imm3
        ///
        /// codegenarm64test:
        ///    IF_SVE_HN_2A   FTMAD <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<imm>
        ///    theEmitter->emitIns_R_R_I(INS_sve_ftmad, EA_SCALABLE, REG_V0, REG_V2, 0, INS_OPTS_SCALABLE_H);
        ///    IF_SVE_HN_2A   FTMAD <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<imm>
        ///    theEmitter->emitIns_R_R_I(INS_sve_ftmad, EA_SCALABLE, REG_V3, REG_V5, 1, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_HN_2A   FTMAD <Zdn>.<T>, <Zdn>.<T>, <Zm>.<T>, #<imm>
        ///    theEmitter->emitIns_R_R_I(INS_sve_ftmad, EA_SCALABLE, REG_V4, REG_V2, 7, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> TrigonometricMultiplyAddCoefficient(Vector<half> left, Vector<half> right, [ConstantExpected] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat16_t svtssel[_f16](svfloat16_t op1, svuint16_t op2)
        ///   FTSSEL Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BK_3A   FTSSEL <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_ftssel, EA_SCALABLE, REG_V17, REG_V16, REG_V15, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> TrigonometricSelectCoefficient(Vector<half> value, Vector<ushort> selector) => TrigonometricSelectCoefficient(value, selector);


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat16_t svtsmul[_f16](svfloat16_t op1, svuint16_t op2)
        ///   FTSMUL Zresult.H, Zop1.H, Zop2.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_HK_3A   FTSMUL <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_ftsmul, EA_SCALABLE, REG_V15, REG_V16, REG_V17, INS_OPTS_SCALABLE_D, INS_SCALABLE_OPTS_UNPREDICATED);
        /// </summary>
        public static unsafe Vector<half> TrigonometricStartingValue(Vector<half> value, Vector<ushort> sign) => TrigonometricStartingValue(value, sign);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp1[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> UnzipEven(Vector<half> left, Vector<half> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp2[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> UnzipOdd(Vector<half> left, Vector<half> right) => UnzipOdd(left, right);


        ///  UpConvertWideningUpper : Up convert long (top)

        /// <summary>
        /// svfloat32_t svcvtlt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTLT Ztied.S, Pg/M, Zop.H
        /// svfloat32_t svcvtlt_f32[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTLT Ztied.S, Pg/M, Ztied.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_GQ_3A   FCVTLT <Zd>.D, <Pg>/M, <Zn>.S
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcvtlt, EA_SCALABLE, REG_V0, REG_P7, REG_V1, INS_OPTS_S_TO_D);
        ///    IF_SVE_GQ_3A   FCVTLT <Zd>.S, <Pg>/M, <Zn>.H
        ///    theEmitter->emitIns_R_R_R(INS_sve_fcvtlt, EA_SCALABLE, REG_V14, REG_P7, REG_V20, INS_OPTS_H_TO_S);
        ///
        /// Embedded arg1 mask predicate
        /// </summary>
        public static unsafe Vector<float> UpConvertWideningUpper(Vector<half> value) => UpConvertWideningUpper(value);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svfloat16_t svtbl[_f16](svfloat16_t data, svuint16_t indices)
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
        public static unsafe Vector<half> VectorTableLookup(Vector<half> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat16_t svtbl2[_f16](svfloat16x2_t data, svuint16_t indices)
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
        public static unsafe Vector<half> VectorTableLookup((Vector<half> data1, Vector<half> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svfloat16_t svtbx[_f16](svfloat16_t fallback, svfloat16_t data, svuint16_t indices)
        ///   TBX Ztied.H, Zdata.H, Zindices.H
        ///
        /// codegenarm64test:
        ///    IF_SVE_BZ_3A   TBX <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbx, EA_SCALABLE, REG_V6, REG_V7, REG_V8, INS_OPTS_SCALABLE_S);
        ///    IF_SVE_BZ_3A   TBX <Zd>.<T>, <Zn>.<T>, <Zm>.<T>
        ///    theEmitter->emitIns_R_R_R(INS_sve_tbx, EA_SCALABLE, REG_V9, REG_V10, REG_V11, INS_OPTS_SCALABLE_D);
        /// </summary>
        public static unsafe Vector<half> VectorTableLookupExtension(Vector<half> fallback, Vector<half> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svfloat16_t svzip2[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> ZipHigh(Vector<half> left, Vector<half> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svfloat16_t svzip1[_f16](svfloat16_t op1, svfloat16_t op2)
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
        public static unsafe Vector<half> ZipLow(Vector<half> left, Vector<half> right) => ZipLow(left, right);

    }
}


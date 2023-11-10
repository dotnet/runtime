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
        /// </summary>
        public static unsafe Vector<half> Abs(Vector<half> value) => Abs(value);


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGT Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThan(left, right);


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThanOrEqual(left, right);


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGT Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, Vector<half> right) => AbsoluteCompareLessThan(left, right);


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FACGE Presult.H, Pg/Z, Zop2.H, Zop1.H
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
        /// </summary>
        public static unsafe Vector<half> Add(Vector<half> left, Vector<half> right) => Add(left, right);


        ///  AddAcross : Add reduction

        /// <summary>
        /// float16_t svaddv[_f16](svbool_t pg, svfloat16_t op)
        ///   FADDV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half AddAcross(Vector<half> value) => AddAcross(value);


        ///  AddPairwise : Add pairwise

        /// <summary>
        /// svfloat16_t svaddp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svaddp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FADDP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FADDP Zresult.H, Pg/M, Zresult.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<half> AddRotateComplex(Vector<half> op1, Vector<half> op2, ulong imm_rotation) => AddRotateComplex(op1, op2, imm_rotation);


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float16_t svadda[_f16](svbool_t pg, float16_t initial, svfloat16_t op)
        ///   FADDA Htied, Pg, Htied, Zop.H
        /// </summary>
        public static unsafe half AddSequentialAcross(half initial, Vector<half> op) => AddSequentialAcross(initial, op);


        ///  CompareEqual : Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMEQ Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> CompareEqual(Vector<half> left, Vector<half> right) => CompareEqual(left, right);


        ///  CompareGreaterThan : Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGT Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, Vector<half> right) => CompareGreaterThan(left, right);


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => CompareGreaterThanOrEqual(left, right);


        ///  CompareLessThan : Compare less than

        /// <summary>
        /// svbool_t svcmplt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGT Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<half> CompareLessThan(Vector<half> left, Vector<half> right) => CompareLessThan(left, right);


        ///  CompareLessThanOrEqual : Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMGE Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, Vector<half> right) => CompareLessThanOrEqual(left, right);


        ///  CompareNotEqualTo : Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMNE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, Vector<half> right) => CompareNotEqualTo(left, right);


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FCMUO Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> CompareUnordered(Vector<half> left, Vector<half> right) => CompareUnordered(left, right);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractAfterLastActiveElement(Vector<half> mask, Vector<half> fallback, Vector<half> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractLastActiveElement(Vector<half> mask, Vector<half> fallback, Vector<half> data) => ConditionalExtractLastActiveElement(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svfloat16_t svsel[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<half> value) => ConvertToUInt64(value);



        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_f16](const float16_t *op1, const float16_t *op2)
        ///   WHILERW Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<half> CreateWhileReadAfterWriteMask(const half left, const half right) => CreateWhileReadAfterWriteMask(half, half);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_f16](const float16_t *op1, const float16_t *op2)
        ///   WHILEWR Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<half> CreateWhileWriteAfterReadMask(const half left, const half right) => CreateWhileWriteAfterReadMask(half, half);


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
        /// </summary>
        public static unsafe Vector<half> Divide(Vector<half> left, Vector<half> right) => Divide(left, right);



        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svfloat16_t svcvtnt_f16[_f32]_m(svfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   FCVTNT Ztied.H, Pg/M, Zop.S
        /// svfloat16_t svcvtnt_f16[_f32]_x(svfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   FCVTNT Ztied.H, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<half> DownConvertNarrowingUpper(Vector<float> value) => DownConvertNarrowingUpper(value);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svfloat16_t svdup[_n]_f16(float16_t op)
        ///   DUP Zresult.H, #op
        ///   FDUP Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svfloat16_t svdup[_n]_f16_m(svfloat16_t inactive, svbool_t pg, float16_t op)
        ///   CPY Ztied.H, Pg/M, #bitcast<int16_t>(op)
        ///   FCPY Ztied.H, Pg/M, #op
        ///   CPY Ztied.H, Pg/M, Wop
        ///   CPY Ztied.H, Pg/M, Hop
        /// svfloat16_t svdup[_n]_f16_x(svbool_t pg, float16_t op)
        ///   CPY Zresult.H, Pg/Z, #bitcast<int16_t>(op)
        ///   DUP Zresult.H, #op
        ///   FCPY Zresult.H, Pg/M, #op
        ///   FDUP Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svfloat16_t svdup[_n]_f16_z(svbool_t pg, float16_t op)
        ///   CPY Zresult.H, Pg/Z, #bitcast<int16_t>(op)
        ///   DUP Zresult.H, #0; FCPY Zresult.H, Pg/M, #op
        ///   DUP Zresult.H, #0; CPY Zresult.H, Pg/M, Wop
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CPY Zresult.H, Pg/M, Hop
        /// </summary>
        public static unsafe Vector<half> DuplicateSelectedScalarToVector(half value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svfloat16_t svdup_lane[_f16](svfloat16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// </summary>
        public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, ushort index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat16_t svdupq_lane[_f16](svfloat16_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, ulong index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLast : Extract element after last

        /// <summary>
        /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half ExtractAfterLast(Vector<half> value) => ExtractAfterLast(value);


        ///  ExtractLast : Extract last element

        /// <summary>
        /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half ExtractLast(Vector<half> value) => ExtractLast(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svfloat16_t svext[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<half> ExtractVector(Vector<half> upper, Vector<half> lower, ulong index) => ExtractVector(upper, lower, index);


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat16_t svexpa[_f16](svuint16_t op)
        ///   FEXPA Zresult.H, Zop.H
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
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// svfloat16_t svmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.H, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right, ulong imm_index) => FusedMultiplyAdd(addend, left, right, imm_index);


        ///  FusedMultiplyAddNegate : Negated multiply-add, addend first

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
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAddNegate(Vector<half> op1, Vector<half> op2, Vector<half> op3) => FusedMultiplyAddNegate(op1, op2, op3);


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
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// svfloat16_t svmls_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.H, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.H, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right, ulong imm_index) => FusedMultiplySubtract(minuend, left, right, imm_index);


        ///  FusedMultiplySubtractNegate : Negated multiply-subtract, minuend first

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
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtractNegate(Vector<half> op1, Vector<half> op2, Vector<half> op3) => FusedMultiplySubtractNegate(op1, op2, op3);


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svfloat16_t svinsr[_n_f16](svfloat16_t op1, float16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        /// </summary>
        public static unsafe Vector<half> InsertIntoShiftedVector(Vector<half> left, half right) => InsertIntoShiftedVector(left, right);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svfloat16_t svld1[_f16](svbool_t pg, const float16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<half> LoadVector(Vector<half> mask, const half *base) => LoadVector(mask, half);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svfloat16_t svld1rq[_f16](svbool_t pg, const float16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<half> LoadVector128AndReplicateToVector(Vector<half> mask, const half *base) => LoadVector128AndReplicateToVector(mask, half);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svfloat16_t svldff1[_f16](svbool_t pg, const float16_t *base)
        ///   LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, const half *base) => LoadVectorFirstFaulting(mask, half);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svfloat16_t svldnf1[_f16](svbool_t pg, const float16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<half> LoadVectorNonFaulting(Vector<half> mask, const half *base) => LoadVectorNonFaulting(mask, half);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svfloat16_t svldnt1[_f16](svbool_t pg, const float16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, const half *base) => LoadVectorNonTemporal(mask, half);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svfloat16x2_t svld2[_f16](svbool_t pg, const float16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, const half *base) => LoadVectorx2(mask, half);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svfloat16x3_t svld3[_f16](svbool_t pg, const float16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, const half *base) => LoadVectorx3(mask, half);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svfloat16x4_t svld4[_f16](svbool_t pg, const float16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, const half *base) => LoadVectorx4(mask, half);


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
        /// </summary>
        public static unsafe Vector<half> Max(Vector<half> left, Vector<half> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// float16_t svmaxv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMAXV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half MaxAcross(Vector<half> value) => MaxAcross(value);


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
        /// </summary>
        public static unsafe Vector<half> MaxNumber(Vector<half> left, Vector<half> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float16_t svmaxnmv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMAXNMV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half MaxNumberAcross(Vector<half> value) => MaxNumberAcross(value);


        ///  MaxNumberPairwise : Maximum number pairwise

        /// <summary>
        /// svfloat16_t svmaxnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svmaxnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMAXNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMAXNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<half> Min(Vector<half> left, Vector<half> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// float16_t svminv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMINV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half MinAcross(Vector<half> value) => MinAcross(value);


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
        /// </summary>
        public static unsafe Vector<half> MinNumber(Vector<half> left, Vector<half> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float16_t svminnmv[_f16](svbool_t pg, svfloat16_t op)
        ///   FMINNMV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe half MinNumberAcross(Vector<half> value) => MinNumberAcross(value);


        ///  MinNumberPairwise : Minimum number pairwise

        /// <summary>
        /// svfloat16_t svminnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svminnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FMINNMP Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FMINNMP Zresult.H, Pg/M, Zresult.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right) => Multiply(left, right);

        /// <summary>
        /// svfloat16_t svmul_lane[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm_index)
        ///   FMUL Zresult.H, Zop1.H, Zop2.H[imm_index]
        /// </summary>
        public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right, ulong index) => Multiply(left, right, index);





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
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> op1, Vector<half> op2, Vector<half> op3, ulong imm_rotation) => MultiplyAddRotateComplex(op1, op2, op3, imm_rotation);

        /// <summary>
        /// svfloat16_t svcmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        ///   FCMLA Ztied1.H, Zop2.H, Zop3.H[imm_index], #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.H, Zop2.H, Zop3.H[imm_index], #imm_rotation
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> op1, Vector<half> op2, Vector<half> op3, ulong imm_index, ulong imm_rotation) => MultiplyAddRotateComplex(op1, op2, op3, imm_index, imm_rotation);


        ///  MultiplyAddWideningLower : Multiply-add long (bottom)

        /// <summary>
        /// svfloat32_t svmlalb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLALB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);


        ///  MultiplyAddWideningUpper : Multiply-add long (top)

        /// <summary>
        /// svfloat32_t svmlalt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLALT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);


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
        /// </summary>
        public static unsafe Vector<half> MultiplyExtended(Vector<half> left, Vector<half> right) => MultiplyExtended(left, right);





        ///  MultiplySubtractWideningLower : Multiply-subtract long (bottom)

        /// <summary>
        /// svfloat32_t svmlslb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLSLB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLSLB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLSLB Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);


        ///  MultiplySubtractWideningUpper : Multiply-subtract long (top)

        /// <summary>
        /// svfloat32_t svmlslt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        ///   FMLSLT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        ///   FMLSLT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLSLT Zresult.S, Zop2.H, Zop3.H[imm_index]
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
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<half> value) => PopCount(value);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat16_t svrecpe[_f16](svfloat16_t op)
        ///   FRECPE Zresult.H, Zop.H
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
        /// </summary>
        public static unsafe Vector<half> ReciprocalExponent(Vector<half> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat16_t svrsqrte[_f16](svfloat16_t op)
        ///   FRSQRTE Zresult.H, Zop.H
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtEstimate(Vector<half> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat16_t svrsqrts[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   FRSQRTS Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtStep(Vector<half> left, Vector<half> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat16_t svrecps[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   FRECPS Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> ReciprocalStep(Vector<half> left, Vector<half> right) => ReciprocalStep(left, right);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svfloat16_t svrev[_f16](svfloat16_t op)
        ///   REV Zresult.H, Zop.H
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
        /// </summary>
        public static unsafe Vector<half> Scale(Vector<half> left, Vector<short> right) => Scale(left, right);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svfloat16_t svsplice[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<half> Sqrt(Vector<half> value) => Sqrt(value);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
        ///   ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half *base, Vector<half> data) => Store(mask, *base, data);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
        ///   STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<half> mask, half *base, Vector<half> data) => StoreNonTemporal(mask, *base, data);


        ///  Storex2 : Store two vectors into two-element tuples

        /// <summary>
        /// void svst2[_f16](svbool_t pg, float16_t *base, svfloat16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<half> mask, half *base, (Vector<half> data1, Vector<half> data2)) => Storex2(mask, *base, data1,);


        ///  Storex3 : Store three vectors into three-element tuples

        /// <summary>
        /// void svst3[_f16](svbool_t pg, float16_t *base, svfloat16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<half> mask, half *base, (Vector<half> data1, Vector<half> data2, Vector<half> data3)) => Storex3(mask, *base, data1,);


        ///  Storex4 : Store four vectors into four-element tuples

        /// <summary>
        /// void svst4[_f16](svbool_t pg, float16_t *base, svfloat16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<half> mask, half *base, (Vector<half> data1, Vector<half> data2, Vector<half> data3, Vector<half> data4)) => Storex4(mask, *base, data1,);


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
        /// </summary>
        public static unsafe Vector<half> Subtract(Vector<half> left, Vector<half> right) => Subtract(left, right);


        ///  SubtractReversed : Subtract reversed

        /// <summary>
        /// svfloat16_t svsubr[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svsubr[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   FSUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   FSUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   FSUB Zresult.H, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svfloat16_t svsubr[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; FSUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; FSUB Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<half> SubtractReversed(Vector<half> left, Vector<half> right) => SubtractReversed(left, right);


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn1[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> TransposeEven(Vector<half> left, Vector<half> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn2[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> TransposeOdd(Vector<half> left, Vector<half> right) => TransposeOdd(left, right);


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat16_t svtmad[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        ///   FTMAD Ztied1.H, Ztied1.H, Zop2.H, #imm3
        ///   MOVPRFX Zresult, Zop1; FTMAD Zresult.H, Zresult.H, Zop2.H, #imm3
        /// </summary>
        public static unsafe Vector<half> TrigonometricMultiplyAddCoefficient(Vector<half> op1, Vector<half> op2, ulong imm3) => TrigonometricMultiplyAddCoefficient(op1, op2, imm3);


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat16_t svtssel[_f16](svfloat16_t op1, svuint16_t op2)
        ///   FTSSEL Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> TrigonometricSelectCoefficient(Vector<half> left, Vector<ushort> right) => TrigonometricSelectCoefficient(left, right);


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat16_t svtsmul[_f16](svfloat16_t op1, svuint16_t op2)
        ///   FTSMUL Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> TrigonometricStartingValue(Vector<half> left, Vector<ushort> right) => TrigonometricStartingValue(left, right);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp1[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   UZP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> UnzipEven(Vector<half> left, Vector<half> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp2[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   UZP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> UnzipOdd(Vector<half> left, Vector<half> right) => UnzipOdd(left, right);


        ///  UpConvertWideningUpper : Up convert long (top)

        /// <summary>
        /// svfloat32_t svcvtlt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op)
        ///   FCVTLT Ztied.S, Pg/M, Zop.H
        /// svfloat32_t svcvtlt_f32[_f16]_x(svbool_t pg, svfloat16_t op)
        ///   FCVTLT Ztied.S, Pg/M, Ztied.H
        /// </summary>
        public static unsafe Vector<float> UpConvertWideningUpper(Vector<half> value) => UpConvertWideningUpper(value);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svfloat16_t svtbl[_f16](svfloat16_t data, svuint16_t indices)
        ///   TBL Zresult.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<half> VectorTableLookup(Vector<half> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat16_t svtbl2[_f16](svfloat16x2_t data, svuint16_t indices)
        ///   TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H
        /// </summary>
        public static unsafe Vector<half> VectorTableLookup((Vector<half> data1, Vector<half> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svfloat16_t svtbx[_f16](svfloat16_t fallback, svfloat16_t data, svuint16_t indices)
        ///   TBX Ztied.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<half> VectorTableLookupExtension(Vector<half> fallback, Vector<half> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svfloat16_t svzip2[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   ZIP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> ZipHigh(Vector<half> left, Vector<half> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svfloat16_t svzip1[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   ZIP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<half> ZipLow(Vector<half> left, Vector<half> right) => ZipLow(left, right);

    }
}


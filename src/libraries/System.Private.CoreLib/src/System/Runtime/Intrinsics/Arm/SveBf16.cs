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


        ///  Bfloat16DotProduct : BFloat16 dot product

        /// <summary>
        /// svfloat32_t svbfdot[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFDOT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16DotProduct(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svbfdot_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFDOT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFDOT Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) => Bfloat16DotProduct(op1, op2, op3, imm_index);


        ///  Bfloat16MatrixMultiplyAccumulate : BFloat16 matrix multiply-accumulate

        /// <summary>
        /// svfloat32_t svbfmmla[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMMLA Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMMLA Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> Bfloat16MatrixMultiplyAccumulate(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MatrixMultiplyAccumulate(op1, op2, op3);


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionLower : BFloat16 multiply-add long to single-precision (bottom)

        /// <summary>
        /// svfloat32_t svbfmlalb[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMLALB Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MultiplyAddWideningToSinglePrecisionLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svbfmlalb_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFMLALB Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFMLALB Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) => Bfloat16MultiplyAddWideningToSinglePrecisionLower(op1, op2, op3, imm_index);


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionUpper : BFloat16 multiply-add long to single-precision (top)

        /// <summary>
        /// svfloat32_t svbfmlalt[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        ///   BFMLALT Ztied1.S, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) => Bfloat16MultiplyAddWideningToSinglePrecisionUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svbfmlalt_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        ///   BFMLALT Ztied1.S, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; BFMLALT Zresult.S, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) => Bfloat16MultiplyAddWideningToSinglePrecisionUpper(op1, op2, op3, imm_index);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data) => ConditionalExtractLastActiveElement(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svbfloat16_t svsel[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
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
        /// </summary>
        public static unsafe Vector<bfloat16> ConvertToBFloat16(Vector<float> value) => ConvertToBFloat16(value);



        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        ///   WHILERW Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileReadAfterWriteMask(const bfloat16 left, const bfloat16 right) => CreateWhileReadAfterWriteMask(bfloat16, bfloat16);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        ///   WHILEWR Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileWriteAfterReadMask(const bfloat16 left, const bfloat16 right) => CreateWhileWriteAfterReadMask(bfloat16, bfloat16);


        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svbfloat16_t svcvtnt_bf16[_f32]_m(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   BFCVTNT Ztied.H, Pg/M, Zop.S
        /// svbfloat16_t svcvtnt_bf16[_f32]_x(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        ///   BFCVTNT Ztied.H, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<bfloat16> DownConvertNarrowingUpper(Vector<float> value) => DownConvertNarrowingUpper(value);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svbfloat16_t svdup[_n]_bf16(bfloat16_t op)
        ///   DUP Zresult.H, #op
        ///   FDUP Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svbfloat16_t svdup[_n]_bf16_m(svbfloat16_t inactive, svbool_t pg, bfloat16_t op)
        ///   CPY Ztied.H, Pg/M, #bitcast<int16_t>(op)
        ///   FCPY Ztied.H, Pg/M, #op
        ///   CPY Ztied.H, Pg/M, Wop
        ///   CPY Ztied.H, Pg/M, Hop
        /// svbfloat16_t svdup[_n]_bf16_x(svbool_t pg, bfloat16_t op)
        ///   CPY Zresult.H, Pg/Z, #bitcast<int16_t>(op)
        ///   DUP Zresult.H, #op
        ///   FCPY Zresult.H, Pg/M, #op
        ///   FDUP Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svbfloat16_t svdup[_n]_bf16_z(svbool_t pg, bfloat16_t op)
        ///   CPY Zresult.H, Pg/Z, #bitcast<int16_t>(op)
        ///   DUP Zresult.H, #0; FCPY Zresult.H, Pg/M, #op
        ///   DUP Zresult.H, #0; CPY Zresult.H, Pg/M, Wop
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CPY Zresult.H, Pg/M, Hop
        /// </summary>
        public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(bfloat16 value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svbfloat16_t svdup_lane[_bf16](svbfloat16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// </summary>
        public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, ushort index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svbfloat16_t svdupq_lane[_bf16](svbfloat16_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, ulong index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLast : Extract element after last

        /// <summary>
        /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe bfloat16 ExtractAfterLast(Vector<bfloat16> value) => ExtractAfterLast(value);


        ///  ExtractLast : Extract last element

        /// <summary>
        /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe bfloat16 ExtractLast(Vector<bfloat16> value) => ExtractLast(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svbfloat16_t svext[_bf16](svbfloat16_t op1, svbfloat16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractVector(Vector<bfloat16> upper, Vector<bfloat16> lower, ulong index) => ExtractVector(upper, lower, index);


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svbfloat16_t svinsr[_n_bf16](svbfloat16_t op1, bfloat16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        /// </summary>
        public static unsafe Vector<bfloat16> InsertIntoShiftedVector(Vector<bfloat16> left, bfloat16 right) => InsertIntoShiftedVector(left, right);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svbfloat16_t svld1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, const bfloat16 *base) => LoadVector(mask, bfloat16);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svbfloat16_t svld1rq[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector128AndReplicateToVector(Vector<bfloat16> mask, const bfloat16 *base) => LoadVector128AndReplicateToVector(mask, bfloat16);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svbfloat16_t svldff1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorFirstFaulting(mask, bfloat16);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svbfloat16_t svldnf1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonFaulting(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorNonFaulting(mask, bfloat16);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svbfloat16_t svldnt1[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorNonTemporal(mask, bfloat16);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svbfloat16x2_t svld2[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorx2(mask, bfloat16);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svbfloat16x3_t svld3[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorx3(mask, bfloat16);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svbfloat16x4_t svld4[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, const bfloat16 *base) => LoadVectorx4(mask, bfloat16);


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
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<bfloat16> value) => PopCount(value);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svbfloat16_t svrev[_bf16](svbfloat16_t op)
        ///   REV Zresult.H, Zop.H
        /// </summary>
        public static unsafe Vector<bfloat16> ReverseElement(Vector<bfloat16> value) => ReverseElement(value);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svbfloat16_t svsplice[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> Splice(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right) => Splice(mask, left, right);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        ///   ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16 *base, Vector<bfloat16> data) => Store(mask, *base, data);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        ///   STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16 *base, Vector<bfloat16> data) => StoreNonTemporal(mask, *base, data);


        ///  Storex2 : Store two vectors into two-element tuples

        /// <summary>
        /// void svst2[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<bfloat16> mask, bfloat16 *base, (Vector<bfloat16> data1, Vector<bfloat16> data2)) => Storex2(mask, *base, data1,);


        ///  Storex3 : Store three vectors into three-element tuples

        /// <summary>
        /// void svst3[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<bfloat16> mask, bfloat16 *base, (Vector<bfloat16> data1, Vector<bfloat16> data2, Vector<bfloat16> data3)) => Storex3(mask, *base, data1,);


        ///  Storex4 : Store four vectors into four-element tuples

        /// <summary>
        /// void svst4[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<bfloat16> mask, bfloat16 *base, (Vector<bfloat16> data1, Vector<bfloat16> data2, Vector<bfloat16> data3, Vector<bfloat16> data4)) => Storex4(mask, *base, data1,);


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeEven(Vector<bfloat16> left, Vector<bfloat16> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeOdd(Vector<bfloat16> left, Vector<bfloat16> right) => TransposeOdd(left, right);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipEven(Vector<bfloat16> left, Vector<bfloat16> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipOdd(Vector<bfloat16> left, Vector<bfloat16> right) => UnzipOdd(left, right);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svbfloat16_t svtbl[_bf16](svbfloat16_t data, svuint16_t indices)
        ///   TBL Zresult.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup(Vector<bfloat16> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svbfloat16_t svtbl2[_bf16](svbfloat16x2_t data, svuint16_t indices)
        ///   TBL Zresult.H, {Zdata0.H, Zdata1.H}, Zindices.H
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup((Vector<bfloat16> data1, Vector<bfloat16> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svbfloat16_t svtbx[_bf16](svbfloat16_t fallback, svbfloat16_t data, svuint16_t indices)
        ///   TBX Ztied.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookupExtension(Vector<bfloat16> fallback, Vector<bfloat16> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> ZipHigh(Vector<bfloat16> left, Vector<bfloat16> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<bfloat16> ZipLow(Vector<bfloat16> left, Vector<bfloat16> right) => ZipLow(left, right);

    }
}


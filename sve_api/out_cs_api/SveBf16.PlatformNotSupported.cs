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
        /// </summary>
        public static unsafe Vector<float> Bfloat16DotProduct(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  Bfloat16MatrixMultiplyAccumulate : BFloat16 matrix multiply-accumulate

        /// <summary>
        /// svfloat32_t svbfmmla[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> Bfloat16MatrixMultiplyAccumulate(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) { throw new PlatformNotSupportedException(); }


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionLower : BFloat16 multiply-add long to single-precision (bottom)

        /// <summary>
        /// svfloat32_t svbfmlalb[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svbfmlalb_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionLower(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) { throw new PlatformNotSupportedException(); }


        ///  Bfloat16MultiplyAddWideningToSinglePrecisionUpper : BFloat16 multiply-add long to single-precision (top)

        /// <summary>
        /// svfloat32_t svbfmlalt[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svbfmlalt_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> Bfloat16MultiplyAddWideningToSinglePrecisionUpper(Vector<float> op1, Vector<bfloat16> op2, Vector<bfloat16> op3, ulong imm_index) { throw new PlatformNotSupportedException(); }


        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svuzp1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svuzp2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// bfloat16_t svclasta[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe bfloat16 ConditionalExtractAfterLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svbfloat16_t svclasta[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> defaultScalar, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElement(Vector<bfloat16> mask, Vector<bfloat16> defaultValue, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// bfloat16_t svclastb[_n_bf16](svbool_t pg, bfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe bfloat16 ConditionalExtractLastActiveElement(Vector<bfloat16> mask, bfloat16 defaultValues, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svbfloat16_t svclastb[_bf16](svbool_t pg, svbfloat16_t fallback, svbfloat16_t data)
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalExtractLastActiveElementAndReplicate(Vector<bfloat16> mask, Vector<bfloat16> fallback, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svbfloat16_t svsel[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> ConditionalSelect(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  ConvertToBFloat16 : Floating-point convert

        /// <summary>
        /// svbfloat16_t svcvt_bf16[_f32]_m(svbfloat16_t inactive, svbool_t pg, svfloat32_t op)
        /// svbfloat16_t svcvt_bf16[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svbfloat16_t svcvt_bf16[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<bfloat16> ConvertToBFloat16(Vector<float> value) { throw new PlatformNotSupportedException(); }



        ///  CreateFalseMaskBFloat16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<bfloat16> CreateFalseMaskBFloat16() { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskBFloat16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<bfloat16> CreateTrueMaskBFloat16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileReadAfterWriteMask(bfloat16* left, bfloat16* right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_bf16](const bfloat16_t *op1, const bfloat16_t *op2)
        /// </summary>
        public static unsafe Vector<bfloat16> CreateWhileWriteAfterReadMask(bfloat16* left, bfloat16* right) { throw new PlatformNotSupportedException(); }


        ///  DotProductBySelectedScalar : BFloat16 dot product

        /// <summary>
        /// svfloat32_t svbfdot_lane[_f32](svfloat32_t op1, svbfloat16_t op2, svbfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> DotProductBySelectedScalar(Vector<float> addend, Vector<bfloat16> left, Vector<bfloat16> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }


        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svbfloat16_t svcvtnt_bf16[_f32]_m(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        /// svbfloat16_t svcvtnt_bf16[_f32]_x(svbfloat16_t even, svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<bfloat16> DownConvertNarrowingUpper(Vector<float> value) { throw new PlatformNotSupportedException(); }


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svbfloat16_t svdup_lane[_bf16](svbfloat16_t data, uint16_t index)
        /// svbfloat16_t svdupq_lane[_bf16](svbfloat16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<bfloat16> DuplicateSelectedScalarToVector(Vector<bfloat16> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op)
        /// </summary>
        public static unsafe bfloat16 ExtractAfterLastScalar(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// bfloat16_t svlasta[_bf16](svbool_t pg, svbfloat16_t op)
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractAfterLastVector(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op)
        /// </summary>
        public static unsafe bfloat16 ExtractLastScalar(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// bfloat16_t svlastb[_bf16](svbool_t pg, svbfloat16_t op)
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractLastVector(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svbfloat16_t svext[_bf16](svbfloat16_t op1, svbfloat16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<bfloat16> ExtractVector(Vector<bfloat16> upper, Vector<bfloat16> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<bfloat16> mask, Vector<bfloat16> from) { throw new PlatformNotSupportedException(); }


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svbfloat16_t svinsr[_n_bf16](svbfloat16_t op1, bfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> InsertIntoShiftedVector(Vector<bfloat16> left, bfloat16 right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svtrn1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svbfloat16_t svtrn2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  LoadVector : Unextended load

        /// <summary>
        /// svbfloat16_t svld1[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svbfloat16_t svld1rq[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector128AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svbfloat16_t svld1ro[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector256AndReplicateToVector(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svbfloat16_t svldff1[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorFirstFaulting(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svbfloat16_t svldnf1[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonFaulting(bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svbfloat16_t svldnt1[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVectorNonTemporal(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svbfloat16x2_t svld2[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>) LoadVectorx2(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svbfloat16x3_t svld3[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx3(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svbfloat16x4_t svld4[_bf16](svbool_t pg, const bfloat16_t *base)
        /// </summary>
        public static unsafe (Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>, Vector<bfloat16>) LoadVectorx4(Vector<bfloat16> mask, bfloat16* address) { throw new PlatformNotSupportedException(); }


        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint16_t svcnt[_bf16]_m(svuint16_t inactive, svbool_t pg, svbfloat16_t op)
        /// svuint16_t svcnt[_bf16]_x(svbool_t pg, svbfloat16_t op)
        /// svuint16_t svcnt[_bf16]_z(svbool_t pg, svbfloat16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svbfloat16_t svrev[_bf16](svbfloat16_t op)
        /// </summary>
        public static unsafe Vector<bfloat16> ReverseElement(Vector<bfloat16> value) { throw new PlatformNotSupportedException(); }


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svbfloat16_t svsplice[_bf16](svbool_t pg, svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> Splice(Vector<bfloat16> mask, Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<bfloat16> mask, bfloat16* address, (Vector<bfloat16> Value1, Vector<bfloat16> Value2, Vector<bfloat16> Value3, Vector<bfloat16> Value4) data) { throw new PlatformNotSupportedException(); }


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_bf16](svbool_t pg, bfloat16_t *base, svbfloat16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<bfloat16> mask, bfloat16* address, Vector<bfloat16> data) { throw new PlatformNotSupportedException(); }


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeEven(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svtrn2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> TransposeOdd(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipEven(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svbfloat16_t svuzp2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> UnzipOdd(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svbfloat16_t svtbl[_bf16](svbfloat16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup(Vector<bfloat16> data, Vector<ushort> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbfloat16_t svtbl2[_bf16](svbfloat16x2_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookup((Vector<bfloat16> data1, Vector<bfloat16> data2), Vector<ushort> indices) { throw new PlatformNotSupportedException(); }


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svbfloat16_t svtbx[_bf16](svbfloat16_t fallback, svbfloat16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<bfloat16> VectorTableLookupExtension(Vector<bfloat16> fallback, Vector<bfloat16> data, Vector<ushort> indices) { throw new PlatformNotSupportedException(); }


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip2[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> ZipHigh(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svbfloat16_t svzip1[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        /// </summary>
        public static unsafe Vector<bfloat16> ZipLow(Vector<bfloat16> left, Vector<bfloat16> right) { throw new PlatformNotSupportedException(); }

    }
}


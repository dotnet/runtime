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
        /// svfloat16_t svabs[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svabs[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> Abs(Vector<half> value) => Abs(value);


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThan(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThan(left, right);


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => AbsoluteCompareGreaterThanOrEqual(left, right);


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareLessThan(Vector<half> left, Vector<half> right) => AbsoluteCompareLessThan(left, right);


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AbsoluteCompareLessThanOrEqual(Vector<half> left, Vector<half> right) => AbsoluteCompareLessThanOrEqual(left, right);


        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svfloat16_t svabd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svabd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svabd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AbsoluteDifference(Vector<half> left, Vector<half> right) => AbsoluteDifference(left, right);


        ///  Add : Add

        /// <summary>
        /// svfloat16_t svadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Add(Vector<half> left, Vector<half> right) => Add(left, right);


        ///  AddAcross : Add reduction

        /// <summary>
        /// float16_t svaddv[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> AddAcross(Vector<half> value) => AddAcross(value);


        ///  AddPairwise : Add pairwise

        /// <summary>
        /// svfloat16_t svaddp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svaddp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> AddPairwise(Vector<half> left, Vector<half> right) => AddPairwise(left, right);


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svfloat16_t svcadd[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        /// svfloat16_t svcadd[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        /// svfloat16_t svcadd[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<half> AddRotateComplex(Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float16_t svadda[_f16](svbool_t pg, float16_t initial, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> AddSequentialAcross(Vector<half> initial, Vector<half> value) => AddSequentialAcross(initial, value);


        ///  CompareEqual : Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareEqual(Vector<half> left, Vector<half> right) => CompareEqual(left, right);


        ///  CompareGreaterThan : Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThan(Vector<half> left, Vector<half> right) => CompareGreaterThan(left, right);


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareGreaterThanOrEqual(Vector<half> left, Vector<half> right) => CompareGreaterThanOrEqual(left, right);


        ///  CompareLessThan : Compare less than

        /// <summary>
        /// svbool_t svcmplt[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareLessThan(Vector<half> left, Vector<half> right) => CompareLessThan(left, right);


        ///  CompareLessThanOrEqual : Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareLessThanOrEqual(Vector<half> left, Vector<half> right) => CompareLessThanOrEqual(left, right);


        ///  CompareNotEqualTo : Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareNotEqualTo(Vector<half> left, Vector<half> right) => CompareNotEqualTo(left, right);


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> CompareUnordered(Vector<half> left, Vector<half> right) => CompareUnordered(left, right);


        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svfloat16_t svuzp1q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ConcatenateEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateEvenInt128FromTwoInputs(left, right);


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svfloat16_t svuzp2q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ConcatenateOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateOddInt128FromTwoInputs(left, right);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractAfterLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// float16_t svclasta[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe half ConditionalExtractAfterLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svfloat16_t svclasta[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<half> mask, Vector<half> defaultScalar, Vector<half> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractLastActiveElement(Vector<half> mask, Vector<half> defaultValue, Vector<half> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// float16_t svclastb[_n_f16](svbool_t pg, float16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe half ConditionalExtractLastActiveElement(Vector<half> mask, half defaultValues, Vector<half> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svfloat16_t svclastb[_f16](svbool_t pg, svfloat16_t fallback, svfloat16_t data)
        /// </summary>
        public static unsafe Vector<half> ConditionalExtractLastActiveElementAndReplicate(Vector<half> mask, Vector<half> fallback, Vector<half> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svfloat16_t svsel[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ConditionalSelect(Vector<half> mask, Vector<half> left, Vector<half> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToDouble : Floating-point convert

        /// <summary>
        /// svfloat64_t svcvt_f64[_f16]_m(svfloat64_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat64_t svcvt_f64[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat64_t svcvt_f64[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<half> value) => ConvertToDouble(value);


        ///  ConvertToHalf : Floating-point convert

        /// <summary>
        /// svfloat16_t svcvt_f16[_s16]_m(svfloat16_t inactive, svbool_t pg, svint16_t op)
        /// svfloat16_t svcvt_f16[_s16]_x(svbool_t pg, svint16_t op)
        /// svfloat16_t svcvt_f16[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<short> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_s32]_m(svfloat16_t inactive, svbool_t pg, svint32_t op)
        /// svfloat16_t svcvt_f16[_s32]_x(svbool_t pg, svint32_t op)
        /// svfloat16_t svcvt_f16[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<int> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_s64]_m(svfloat16_t inactive, svbool_t pg, svint64_t op)
        /// svfloat16_t svcvt_f16[_s64]_x(svbool_t pg, svint64_t op)
        /// svfloat16_t svcvt_f16[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<long> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u16]_m(svfloat16_t inactive, svbool_t pg, svuint16_t op)
        /// svfloat16_t svcvt_f16[_u16]_x(svbool_t pg, svuint16_t op)
        /// svfloat16_t svcvt_f16[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<ushort> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u32]_m(svfloat16_t inactive, svbool_t pg, svuint32_t op)
        /// svfloat16_t svcvt_f16[_u32]_x(svbool_t pg, svuint32_t op)
        /// svfloat16_t svcvt_f16[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<uint> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_u64]_m(svfloat16_t inactive, svbool_t pg, svuint64_t op)
        /// svfloat16_t svcvt_f16[_u64]_x(svbool_t pg, svuint64_t op)
        /// svfloat16_t svcvt_f16[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<ulong> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_f32]_m(svfloat16_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat16_t svcvt_f16[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat16_t svcvt_f16[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<float> value) => ConvertToHalf(value);

        /// <summary>
        /// svfloat16_t svcvt_f16[_f64]_m(svfloat16_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat16_t svcvt_f16[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat16_t svcvt_f16[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<half> ConvertToHalf(Vector<double> value) => ConvertToHalf(value);


        ///  ConvertToInt16 : Floating-point convert

        /// <summary>
        /// svint16_t svcvt_s16[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op)
        /// svint16_t svcvt_s16[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svint16_t svcvt_s16[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<short> ConvertToInt16(Vector<half> value) => ConvertToInt16(value);


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f16]_m(svint32_t inactive, svbool_t pg, svfloat16_t op)
        /// svint32_t svcvt_s32[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svint32_t svcvt_s32[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<half> value) => ConvertToInt32(value);


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f16]_m(svint64_t inactive, svbool_t pg, svfloat16_t op)
        /// svint64_t svcvt_s64[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svint64_t svcvt_s64[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<half> value) => ConvertToInt64(value);


        ///  ConvertToSingle : Floating-point convert

        /// <summary>
        /// svfloat32_t svcvt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat32_t svcvt_f32[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat32_t svcvt_f32[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<half> value) => ConvertToSingle(value);


        ///  ConvertToUInt16 : Floating-point convert

        /// <summary>
        /// svuint16_t svcvt_u16[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op)
        /// svuint16_t svcvt_u16[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svuint16_t svcvt_u16[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ConvertToUInt16(Vector<half> value) => ConvertToUInt16(value);


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f16]_m(svuint32_t inactive, svbool_t pg, svfloat16_t op)
        /// svuint32_t svcvt_u32[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svuint32_t svcvt_u32[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<half> value) => ConvertToUInt32(value);


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f16]_m(svuint64_t inactive, svbool_t pg, svfloat16_t op)
        /// svuint64_t svcvt_u64[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svuint64_t svcvt_u64[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<half> value) => ConvertToUInt64(value);



        ///  CreateFalseMaskHalf : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<half> CreateFalseMaskHalf() => CreateFalseMaskHalf();


        ///  CreateTrueMaskHalf : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<half> CreateTrueMaskHalf([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskHalf(pattern);


        ///  CreateWhileReadAfterWriteMask : While free of read-after-write conflicts

        /// <summary>
        /// svbool_t svwhilerw[_f16](const float16_t *op1, const float16_t *op2)
        /// </summary>
        public static unsafe Vector<half> CreateWhileReadAfterWriteMask(half* left, half* right) => CreateWhileReadAfterWriteMask(left, right);


        ///  CreateWhileWriteAfterReadMask : While free of write-after-read conflicts

        /// <summary>
        /// svbool_t svwhilewr[_f16](const float16_t *op1, const float16_t *op2)
        /// </summary>
        public static unsafe Vector<half> CreateWhileWriteAfterReadMask(half* left, half* right) => CreateWhileWriteAfterReadMask(left, right);


        ///  Divide : Divide

        /// <summary>
        /// svfloat16_t svdiv[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svdiv[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svdiv[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Divide(Vector<half> left, Vector<half> right) => Divide(left, right);



        ///  DownConvertNarrowingUpper : Down convert and narrow (top)

        /// <summary>
        /// svfloat16_t svcvtnt_f16[_f32]_m(svfloat16_t even, svbool_t pg, svfloat32_t op)
        /// svfloat16_t svcvtnt_f16[_f32]_x(svfloat16_t even, svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<half> DownConvertNarrowingUpper(Vector<float> value) => DownConvertNarrowingUpper(value);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svfloat16_t svdup_lane[_f16](svfloat16_t data, uint16_t index)
        /// svfloat16_t svdupq_lane[_f16](svfloat16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<half> DuplicateSelectedScalarToVector(Vector<half> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe half ExtractAfterLastScalar(Vector<half> value) => ExtractAfterLastScalar(value);


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// float16_t svlasta[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ExtractAfterLastVector(Vector<half> value) => ExtractAfterLastVector(value);


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe half ExtractLastScalar(Vector<half> value) => ExtractLastScalar(value);


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// float16_t svlastb[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ExtractLastVector(Vector<half> value) => ExtractLastVector(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svfloat16_t svext[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<half> ExtractVector(Vector<half> upper, Vector<half> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat16_t svexpa[_f16](svuint16_t op)
        /// </summary>
        public static unsafe Vector<half> FloatingPointExponentialAccelerator(Vector<ushort> value) => FloatingPointExponentialAccelerator(value);


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat16_t svmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAdd(Vector<half> addend, Vector<half> left, Vector<half> right) => FusedMultiplyAdd(addend, left, right);


        ///  FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svfloat16_t svmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAddBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        ///  FusedMultiplyAddNegated : Negated multiply-add, addend first

        /// <summary>
        /// svfloat16_t svnmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svnmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svnmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplyAddNegated(Vector<half> addend, Vector<half> left, Vector<half> right) => FusedMultiplyAddNegated(addend, left, right);


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtract(Vector<half> minuend, Vector<half> left, Vector<half> right) => FusedMultiplySubtract(minuend, left, right);


        ///  FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svmls_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtractBySelectedScalar(Vector<half> minuend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        ///  FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat16_t svnmls[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svnmls[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// svfloat16_t svnmls[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<half> FusedMultiplySubtractNegated(Vector<half> minuend, Vector<half> left, Vector<half> right) => FusedMultiplySubtractNegated(minuend, left, right);


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<half> mask, Vector<half> from) => GetActiveElementCount(mask, from);


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svfloat16_t svinsr[_n_f16](svfloat16_t op1, float16_t op2)
        /// </summary>
        public static unsafe Vector<half> InsertIntoShiftedVector(Vector<half> left, half right) => InsertIntoShiftedVector(left, right);


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svfloat16_t svtrn1q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> InterleaveEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveEvenInt128FromTwoInputs(left, right);


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svfloat16_t svzip2q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svfloat16_t svzip1q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svfloat16_t svtrn2q[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> InterleaveOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveOddInt128FromTwoInputs(left, right);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svfloat16_t svld1[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVector(Vector<half> mask, half* address) => LoadVector(mask, address);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svfloat16_t svld1rq[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVector128AndReplicateToVector(Vector<half> mask, half* address) => LoadVector128AndReplicateToVector(mask, address);


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svfloat16_t svld1ro[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVector256AndReplicateToVector(Vector<half> mask, half* address) => LoadVector256AndReplicateToVector(mask, address);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svfloat16_t svldff1[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVectorFirstFaulting(Vector<half> mask, half* address) => LoadVectorFirstFaulting(mask, address);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svfloat16_t svldnf1[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVectorNonFaulting(half* address) => LoadVectorNonFaulting(address);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svfloat16_t svldnt1[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe Vector<half> LoadVectorNonTemporal(Vector<half> mask, half* address) => LoadVectorNonTemporal(mask, address);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svfloat16x2_t svld2[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>) LoadVectorx2(Vector<half> mask, half* address) => LoadVectorx2(mask, address);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svfloat16x3_t svld3[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>) LoadVectorx3(Vector<half> mask, half* address) => LoadVectorx3(mask, address);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svfloat16x4_t svld4[_f16](svbool_t pg, const float16_t *base)
        /// </summary>
        public static unsafe (Vector<half>, Vector<half>, Vector<half>, Vector<half>) LoadVectorx4(Vector<half> mask, half* address) => LoadVectorx4(mask, address);


        ///  Log2 : Base 2 logarithm as integer

        /// <summary>
        /// svint16_t svlogb[_f16]_m(svint16_t inactive, svbool_t pg, svfloat16_t op)
        /// svint16_t svlogb[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svint16_t svlogb[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<short> Log2(Vector<half> value) => Log2(value);


        ///  Max : Maximum

        /// <summary>
        /// svfloat16_t svmax[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmax[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmax[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Max(Vector<half> left, Vector<half> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// float16_t svmaxv[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> MaxAcross(Vector<half> value) => MaxAcross(value);


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat16_t svmaxnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmaxnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmaxnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MaxNumber(Vector<half> left, Vector<half> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float16_t svmaxnmv[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> MaxNumberAcross(Vector<half> value) => MaxNumberAcross(value);


        ///  MaxNumberPairwise : Maximum number pairwise

        /// <summary>
        /// svfloat16_t svmaxnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmaxnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MaxNumberPairwise(Vector<half> left, Vector<half> right) => MaxNumberPairwise(left, right);


        ///  MaxPairwise : Maximum pairwise

        /// <summary>
        /// svfloat16_t svmaxp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmaxp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MaxPairwise(Vector<half> left, Vector<half> right) => MaxPairwise(left, right);


        ///  Min : Minimum

        /// <summary>
        /// svfloat16_t svmin[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmin[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmin[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Min(Vector<half> left, Vector<half> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// float16_t svminv[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> MinAcross(Vector<half> value) => MinAcross(value);


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat16_t svminnm[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svminnm[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svminnm[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MinNumber(Vector<half> left, Vector<half> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float16_t svminnmv[_f16](svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> MinNumberAcross(Vector<half> value) => MinNumberAcross(value);


        ///  MinNumberPairwise : Minimum number pairwise

        /// <summary>
        /// svfloat16_t svminnmp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svminnmp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MinNumberPairwise(Vector<half> left, Vector<half> right) => MinNumberPairwise(left, right);


        ///  MinPairwise : Minimum pairwise

        /// <summary>
        /// svfloat16_t svminp[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svminp[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MinPairwise(Vector<half> left, Vector<half> right) => MinPairwise(left, right);


        ///  Multiply : Multiply

        /// <summary>
        /// svfloat16_t svmul[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmul[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmul[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Multiply(Vector<half> left, Vector<half> right) => Multiply(left, right);





        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svfloat16_t svcmla[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        /// svfloat16_t svcmla[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        /// svfloat16_t svcmla[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplex(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);


        ///  MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

        /// <summary>
        /// svfloat16_t svcmla_lane[_f16](svfloat16_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<half> MultiplyAddRotateComplexBySelectedScalar(Vector<half> addend, Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);


        ///  MultiplyAddWideningLower : Multiply-add long (bottom)

        /// <summary>
        /// svfloat32_t svmlalb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningLower(op1, op2, op3, imm_index);


        ///  MultiplyAddWideningUpper : Multiply-add long (top)

        /// <summary>
        /// svfloat32_t svmlalt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplyAddWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlalt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplyAddWideningUpper(op1, op2, op3, imm_index);


        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svfloat16_t svmul_lane[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<half> MultiplyBySelectedScalar(Vector<half> left, Vector<half> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat16_t svmulx[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmulx[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svmulx[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> MultiplyExtended(Vector<half> left, Vector<half> right) => MultiplyExtended(left, right);





        ///  MultiplySubtractWideningLower : Multiply-subtract long (bottom)

        /// <summary>
        /// svfloat32_t svmlslb[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningLower(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslb_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningLower(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplySubtractWideningLower(op1, op2, op3, imm_index);


        ///  MultiplySubtractWideningUpper : Multiply-subtract long (top)

        /// <summary>
        /// svfloat32_t svmlslt[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3)
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3) => MultiplySubtractWideningUpper(op1, op2, op3);

        /// <summary>
        /// svfloat32_t svmlslt_lane[_f32](svfloat32_t op1, svfloat16_t op2, svfloat16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplySubtractWideningUpper(Vector<float> op1, Vector<half> op2, Vector<half> op3, ulong imm_index) => MultiplySubtractWideningUpper(op1, op2, op3, imm_index);


        ///  Negate : Negate

        /// <summary>
        /// svfloat16_t svneg[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svneg[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svneg[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> Negate(Vector<half> value) => Negate(value);




        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint16_t svcnt[_f16]_m(svuint16_t inactive, svbool_t pg, svfloat16_t op)
        /// svuint16_t svcnt[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svuint16_t svcnt[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<half> value) => PopCount(value);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat16_t svrecpe[_f16](svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ReciprocalEstimate(Vector<half> value) => ReciprocalEstimate(value);


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat16_t svrecpx[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrecpx[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrecpx[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ReciprocalExponent(Vector<half> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat16_t svrsqrte[_f16](svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtEstimate(Vector<half> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat16_t svrsqrts[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ReciprocalSqrtStep(Vector<half> left, Vector<half> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat16_t svrecps[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ReciprocalStep(Vector<half> left, Vector<half> right) => ReciprocalStep(left, right);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svfloat16_t svrev[_f16](svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> ReverseElement(Vector<half> value) => ReverseElement(value);


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat16_t svrinta[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrinta[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrinta[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> RoundAwayFromZero(Vector<half> value) => RoundAwayFromZero(value);


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat16_t svrintn[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintn[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintn[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> RoundToNearest(Vector<half> value) => RoundToNearest(value);


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat16_t svrintm[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintm[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintm[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> RoundToNegativeInfinity(Vector<half> value) => RoundToNegativeInfinity(value);


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat16_t svrintp[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintp[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintp[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> RoundToPositiveInfinity(Vector<half> value) => RoundToPositiveInfinity(value);


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat16_t svrintz[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintz[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svrintz[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> RoundToZero(Vector<half> value) => RoundToZero(value);




        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat16_t svscale[_f16]_m(svbool_t pg, svfloat16_t op1, svint16_t op2)
        /// svfloat16_t svscale[_f16]_x(svbool_t pg, svfloat16_t op1, svint16_t op2)
        /// svfloat16_t svscale[_f16]_z(svbool_t pg, svfloat16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<half> Scale(Vector<half> left, Vector<short> right) => Scale(left, right);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svfloat16_t svsplice[_f16](svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Splice(Vector<half> mask, Vector<half> left, Vector<half> right) => Splice(mask, left, right);


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat16_t svsqrt[_f16]_m(svfloat16_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat16_t svsqrt[_f16]_x(svbool_t pg, svfloat16_t op)
        /// svfloat16_t svsqrt[_f16]_z(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<half> Sqrt(Vector<half> value) => Sqrt(value);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, Vector<half> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_f16](svbool_t pg, float16_t *base, svfloat16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_f16](svbool_t pg, float16_t *base, svfloat16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_f16](svbool_t pg, float16_t *base, svfloat16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<half> mask, half* address, (Vector<half> Value1, Vector<half> Value2, Vector<half> Value3, Vector<half> Value4) data) => Store(mask, address, Value1,);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_f16](svbool_t pg, float16_t *base, svfloat16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<half> mask, half* address, Vector<half> data) => StoreNonTemporal(mask, address, data);


        ///  Subtract : Subtract

        /// <summary>
        /// svfloat16_t svsub[_f16]_m(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svsub[_f16]_x(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// svfloat16_t svsub[_f16]_z(svbool_t pg, svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> Subtract(Vector<half> left, Vector<half> right) => Subtract(left, right);



        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn1[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> TransposeEven(Vector<half> left, Vector<half> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svfloat16_t svtrn2[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> TransposeOdd(Vector<half> left, Vector<half> right) => TransposeOdd(left, right);


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat16_t svtmad[_f16](svfloat16_t op1, svfloat16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<half> TrigonometricMultiplyAddCoefficient(Vector<half> left, Vector<half> right, [ConstantExpected] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat16_t svtssel[_f16](svfloat16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<half> TrigonometricSelectCoefficient(Vector<half> value, Vector<ushort> selector) => TrigonometricSelectCoefficient(value, selector);


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat16_t svtsmul[_f16](svfloat16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<half> TrigonometricStartingValue(Vector<half> value, Vector<ushort> sign) => TrigonometricStartingValue(value, sign);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp1[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> UnzipEven(Vector<half> left, Vector<half> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svfloat16_t svuzp2[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> UnzipOdd(Vector<half> left, Vector<half> right) => UnzipOdd(left, right);


        ///  UpConvertWideningUpper : Up convert long (top)

        /// <summary>
        /// svfloat32_t svcvtlt_f32[_f16]_m(svfloat32_t inactive, svbool_t pg, svfloat16_t op)
        /// svfloat32_t svcvtlt_f32[_f16]_x(svbool_t pg, svfloat16_t op)
        /// </summary>
        public static unsafe Vector<float> UpConvertWideningUpper(Vector<half> value) => UpConvertWideningUpper(value);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svfloat16_t svtbl[_f16](svfloat16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<half> VectorTableLookup(Vector<half> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat16_t svtbl2[_f16](svfloat16x2_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<half> VectorTableLookup((Vector<half> data1, Vector<half> data2), Vector<ushort> indices) => VectorTableLookup(data1,, indices);


        ///  VectorTableLookupExtension : Table lookup in single-vector table (merging)

        /// <summary>
        /// svfloat16_t svtbx[_f16](svfloat16_t fallback, svfloat16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<half> VectorTableLookupExtension(Vector<half> fallback, Vector<half> data, Vector<ushort> indices) => VectorTableLookupExtension(fallback, data, indices);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svfloat16_t svzip2[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ZipHigh(Vector<half> left, Vector<half> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svfloat16_t svzip1[_f16](svfloat16_t op1, svfloat16_t op2)
        /// </summary>
        public static unsafe Vector<half> ZipLow(Vector<half> left, Vector<half> right) => ZipLow(left, right);

    }
}


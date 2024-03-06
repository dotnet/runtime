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
    public abstract class Sve : AdvSimd
    {
        internal Sve() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  Abs : Absolute value

        /// <summary>
        /// svint8_t svabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svabs[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svabs[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> Abs(Vector<sbyte> value) => Abs(value);

        /// <summary>
        /// svint16_t svabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svabs[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svabs[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> Abs(Vector<short> value) => Abs(value);

        /// <summary>
        /// svint32_t svabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svabs[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svabs[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Abs(Vector<int> value) => Abs(value);

        /// <summary>
        /// svint64_t svabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svabs[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svabs[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Abs(Vector<long> value) => Abs(value);

        /// <summary>
        /// svfloat32_t svabs[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svabs[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svabs[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Abs(Vector<float> value) => Abs(value);

        /// <summary>
        /// svfloat64_t svabs[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svabs[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svabs[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Abs(Vector<double> value) => Abs(value);


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svacgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThan(left, right);


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svacge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThanOrEqual(left, right);


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        /// svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThan(left, right);


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThanOrEqual(left, right);


        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svabd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right) => AbsoluteDifference(left, right);


        ///  Add : Add

        /// <summary>
        /// svint8_t svadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Add(Vector<sbyte> left, Vector<sbyte> right) => Add(left, right);

        /// <summary>
        /// svint16_t svadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Add(Vector<short> left, Vector<short> right) => Add(left, right);

        /// <summary>
        /// svint32_t svadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Add(Vector<int> left, Vector<int> right) => Add(left, right);

        /// <summary>
        /// svint64_t svadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Add(Vector<long> left, Vector<long> right) => Add(left, right);

        /// <summary>
        /// svuint8_t svadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Add(Vector<byte> left, Vector<byte> right) => Add(left, right);

        /// <summary>
        /// svuint16_t svadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Add(Vector<ushort> left, Vector<ushort> right) => Add(left, right);

        /// <summary>
        /// svuint32_t svadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Add(Vector<uint> left, Vector<uint> right) => Add(left, right);

        /// <summary>
        /// svuint64_t svadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Add(Vector<ulong> left, Vector<ulong> right) => Add(left, right);

        /// <summary>
        /// svfloat32_t svadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Add(Vector<float> left, Vector<float> right) => Add(left, right);

        /// <summary>
        /// svfloat64_t svadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Add(Vector<double> left, Vector<double> right) => Add(left, right);


        ///  AddAcross : Add reduction

        /// <summary>
        /// int64_t svaddv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<sbyte> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<short> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<int> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<long> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<byte> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ushort> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<uint> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ulong> value) => AddAcross(value);

        /// <summary>
        /// float32_t svaddv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> AddAcross(Vector<float> value) => AddAcross(value);

        /// <summary>
        /// float64_t svaddv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> AddAcross(Vector<double> value) => AddAcross(value);


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svfloat32_t svcadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// svfloat32_t svcadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// svfloat32_t svcadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> AddRotateComplex(Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        /// svfloat64_t svcadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// svfloat64_t svcadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// svfloat64_t svcadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<double> AddRotateComplex(Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation) => AddRotateComplex(left, right, rotation);


        ///  AddSaturate : Saturating add

        /// <summary>
        /// svint8_t svqadd[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// svint16_t svqadd[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right) => AddSaturate(left, right);

        /// <summary>
        /// svint32_t svqadd[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right) => AddSaturate(left, right);

        /// <summary>
        /// svint64_t svqadd[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint16_t svqadd[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint32_t svqadd[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint64_t svqadd[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) => AddSaturate(left, right);


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float32_t svadda[_f32](svbool_t pg, float32_t initial, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> AddSequentialAcross(Vector<float> initial, Vector<float> value) => AddSequentialAcross(initial, value);

        /// <summary>
        /// float64_t svadda[_f64](svbool_t pg, float64_t initial, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> AddSequentialAcross(Vector<double> initial, Vector<double> value) => AddSequentialAcross(initial, value);


        ///  And : Bitwise AND

        /// <summary>
        /// svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right) => And(left, right);

        /// <summary>
        /// svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> And(Vector<short> left, Vector<short> right) => And(left, right);

        /// <summary>
        /// svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> And(Vector<int> left, Vector<int> right) => And(left, right);

        /// <summary>
        /// svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> And(Vector<long> left, Vector<long> right) => And(left, right);

        /// <summary>
        /// svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> And(Vector<byte> left, Vector<byte> right) => And(left, right);

        /// <summary>
        /// svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> And(Vector<ushort> left, Vector<ushort> right) => And(left, right);

        /// <summary>
        /// svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> And(Vector<uint> left, Vector<uint> right) => And(left, right);

        /// <summary>
        /// svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> And(Vector<ulong> left, Vector<ulong> right) => And(left, right);


        ///  AndAcross : Bitwise AND reduction to scalar

        /// <summary>
        /// int8_t svandv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> AndAcross(Vector<sbyte> value) => AndAcross(value);

        /// <summary>
        /// int16_t svandv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> AndAcross(Vector<short> value) => AndAcross(value);

        /// <summary>
        /// int32_t svandv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> AndAcross(Vector<int> value) => AndAcross(value);

        /// <summary>
        /// int64_t svandv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> AndAcross(Vector<long> value) => AndAcross(value);

        /// <summary>
        /// uint8_t svandv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> AndAcross(Vector<byte> value) => AndAcross(value);

        /// <summary>
        /// uint16_t svandv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> AndAcross(Vector<ushort> value) => AndAcross(value);

        /// <summary>
        /// uint32_t svandv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> AndAcross(Vector<uint> value) => AndAcross(value);

        /// <summary>
        /// uint64_t svandv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> AndAcross(Vector<ulong> value) => AndAcross(value);


        ///  AndNot : Bitwise NAND

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AndNot(Vector<sbyte> left, Vector<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> AndNot(Vector<short> left, Vector<short> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> AndNot(Vector<int> left, Vector<int> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> AndNot(Vector<long> left, Vector<long> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> AndNot(Vector<byte> left, Vector<byte> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AndNot(Vector<ushort> left, Vector<ushort> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> AndNot(Vector<uint> left, Vector<uint> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AndNot(Vector<ulong> left, Vector<ulong> right) => AndNot(left, right);


        ///  BitwiseClear : Bitwise clear

        /// <summary>
        /// svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> BitwiseClear(Vector<short> left, Vector<short> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> BitwiseClear(Vector<int> left, Vector<int> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> BitwiseClear(Vector<long> left, Vector<long> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right) => BitwiseClear(left, right);


        ///  BooleanNot : Logically invert boolean condition

        /// <summary>
        /// svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> BooleanNot(Vector<sbyte> value) => BooleanNot(value);

        /// <summary>
        /// svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> BooleanNot(Vector<short> value) => BooleanNot(value);

        /// <summary>
        /// svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> BooleanNot(Vector<int> value) => BooleanNot(value);

        /// <summary>
        /// svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> BooleanNot(Vector<long> value) => BooleanNot(value);

        /// <summary>
        /// svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> BooleanNot(Vector<byte> value) => BooleanNot(value);

        /// <summary>
        /// svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> BooleanNot(Vector<ushort> value) => BooleanNot(value);

        /// <summary>
        /// svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> BooleanNot(Vector<uint> value) => BooleanNot(value);

        /// <summary>
        /// svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> BooleanNot(Vector<ulong> value) => BooleanNot(value);



        ///  Compact : Shuffle active elements of vector to the right and fill with zero

        /// <summary>
        /// svint32_t svcompact[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Compact(Vector<int> mask, Vector<int> value) => Compact(mask, value);

        /// <summary>
        /// svint64_t svcompact[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Compact(Vector<long> mask, Vector<long> value) => Compact(mask, value);

        /// <summary>
        /// svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> Compact(Vector<uint> mask, Vector<uint> value) => Compact(mask, value);

        /// <summary>
        /// svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value) => Compact(mask, value);

        /// <summary>
        /// svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Compact(Vector<float> mask, Vector<float> value) => Compact(mask, value);

        /// <summary>
        /// svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Compact(Vector<double> mask, Vector<double> value) => Compact(mask, value);


        ///  CompareEqual : Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<short> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<int> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareEqual(Vector<long> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareEqual(Vector<ushort> left, Vector<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareEqual(Vector<uint> left, Vector<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareEqual(Vector<ulong> left, Vector<ulong> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareEqual(Vector<float> left, Vector<float> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareEqual(Vector<double> left, Vector<double> right) => CompareEqual(left, right);


        ///  CompareGreaterThan : Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThan(Vector<long> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThan(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right) => CompareGreaterThan(left, right);


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThanOrEqual(Vector<long> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => CompareGreaterThanOrEqual(left, right);


        ///  CompareLessThan : Compare less than

        /// <summary>
        /// svbool_t svcmplt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareLessThan(Vector<long> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThan(Vector<ulong> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareLessThan(Vector<float> left, Vector<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareLessThan(Vector<double> left, Vector<double> right) => CompareLessThan(left, right);


        ///  CompareLessThanOrEqual : Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareLessThanOrEqual(Vector<long> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right) => CompareLessThanOrEqual(left, right);


        ///  CompareNotEqualTo : Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<sbyte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<short> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<int> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareNotEqualTo(Vector<long> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareNotEqualTo(Vector<ushort> left, Vector<ushort> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareNotEqualTo(Vector<uint> left, Vector<uint> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareNotEqualTo(Vector<ulong> left, Vector<ulong> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right) => CompareNotEqualTo(left, right);


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareUnordered(Vector<float> left, Vector<float> right) => CompareUnordered(left, right);

        /// <summary>
        /// svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareUnordered(Vector<double> left, Vector<double> right) => CompareUnordered(left, right);


        ///  Compute16BitAddresses : Compute vector addresses for 16-bit data

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute16BitAddresses(bases, indices);


        ///  Compute32BitAddresses : Compute vector addresses for 32-bit data

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute32BitAddresses(bases, indices);


        ///  Compute64BitAddresses : Compute vector addresses for 64-bit data

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute64BitAddresses(bases, indices);


        ///  Compute8BitAddresses : Compute vector addresses for 8-bit data

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute8BitAddresses(bases, indices);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// int8_t svclasta[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// int16_t svclasta[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// int32_t svclasta[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// int64_t svclasta[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// uint8_t svclasta[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// uint16_t svclasta[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// uint32_t svclasta[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// uint64_t svclasta[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// float32_t svclasta[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// float64_t svclasta[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// int8_t svclastb[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// int16_t svclastb[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// int32_t svclastb[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// int64_t svclastb[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// uint8_t svclastb[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// uint16_t svclastb[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// uint32_t svclastb[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// uint64_t svclastb[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// float32_t svclastb[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// float64_t svclastb[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, Vector<short> fallback, Vector<short> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, Vector<int> fallback, Vector<int> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, Vector<long> fallback, Vector<long> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, Vector<float> fallback, Vector<float> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, Vector<double> fallback, Vector<double> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToDouble : Floating-point convert

        /// <summary>
        /// svfloat64_t svcvt_f64[_s32]_m(svfloat64_t inactive, svbool_t pg, svint32_t op)
        /// svfloat64_t svcvt_f64[_s32]_x(svbool_t pg, svint32_t op)
        /// svfloat64_t svcvt_f64[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<int> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_s64]_m(svfloat64_t inactive, svbool_t pg, svint64_t op)
        /// svfloat64_t svcvt_f64[_s64]_x(svbool_t pg, svint64_t op)
        /// svfloat64_t svcvt_f64[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<long> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_u32]_m(svfloat64_t inactive, svbool_t pg, svuint32_t op)
        /// svfloat64_t svcvt_f64[_u32]_x(svbool_t pg, svuint32_t op)
        /// svfloat64_t svcvt_f64[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<uint> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_u64]_m(svfloat64_t inactive, svbool_t pg, svuint64_t op)
        /// svfloat64_t svcvt_f64[_u64]_x(svbool_t pg, svuint64_t op)
        /// svfloat64_t svcvt_f64[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat64_t svcvt_f64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat64_t svcvt_f64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<float> value) => ConvertToDouble(value);


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value) => ConvertToInt32(value);

        /// <summary>
        /// svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op)
        /// svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<double> value) => ConvertToInt32(value);


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op)
        /// svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<float> value) => ConvertToInt64(value);

        /// <summary>
        /// svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value) => ConvertToInt64(value);


        ///  ConvertToSingle : Floating-point convert

        /// <summary>
        /// svfloat32_t svcvt_f32[_s32]_m(svfloat32_t inactive, svbool_t pg, svint32_t op)
        /// svfloat32_t svcvt_f32[_s32]_x(svbool_t pg, svint32_t op)
        /// svfloat32_t svcvt_f32[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<int> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_s64]_m(svfloat32_t inactive, svbool_t pg, svint64_t op)
        /// svfloat32_t svcvt_f32[_s64]_x(svbool_t pg, svint64_t op)
        /// svfloat32_t svcvt_f32[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<long> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_u32]_m(svfloat32_t inactive, svbool_t pg, svuint32_t op)
        /// svfloat32_t svcvt_f32[_u32]_x(svbool_t pg, svuint32_t op)
        /// svfloat32_t svcvt_f32[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<uint> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_u64]_m(svfloat32_t inactive, svbool_t pg, svuint64_t op)
        /// svfloat32_t svcvt_f32[_u64]_x(svbool_t pg, svuint64_t op)
        /// svfloat32_t svcvt_f32[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<ulong> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvt_f32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvt_f32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<double> value) => ConvertToSingle(value);


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value) => ConvertToUInt32(value);

        /// <summary>
        /// svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<double> value) => ConvertToUInt32(value);


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<float> value) => ConvertToUInt64(value);

        /// <summary>
        /// svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value) => ConvertToUInt64(value);


        ///  Count16BitElements : Count the number of 16-bit elements in a vector

        /// <summary>
        /// uint64_t svcnth_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count16BitElements(pattern);


        ///  Count32BitElements : Count the number of 32-bit elements in a vector

        /// <summary>
        /// uint64_t svcntw_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count32BitElements(pattern);


        ///  Count64BitElements : Count the number of 64-bit elements in a vector

        /// <summary>
        /// uint64_t svcntd_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count64BitElements(pattern);


        ///  Count8BitElements : Count the number of 8-bit elements in a vector

        /// <summary>
        /// uint64_t svcntb_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count8BitElements(pattern);



        ///  CreateBreakAfterMask : Break after first true condition

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakAfterMask(totalMask, fromMask);


        ///  CreateBreakAfterPropagateMask : Break after first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterPropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterPropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterPropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterPropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterPropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterPropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakAfterPropagateMask(mask, left, right);


        ///  CreateBreakBeforeMask : Break before first true condition

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforeMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforeMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforeMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforeMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforeMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforeMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforeMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);


        ///  CreateBreakBeforePropagateMask : Break before first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforePropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforePropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforePropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforePropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforePropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforePropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakBeforePropagateMask(mask, left, right);


        ///  CreateBreakPropagateMask : Propagate break to next partition

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakPropagateMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakPropagateMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakPropagateMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakPropagateMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakPropagateMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakPropagateMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakPropagateMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakPropagateMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);


        ///  CreateFalseMaskByte : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<byte> CreateFalseMaskByte() => CreateFalseMaskByte();


        ///  CreateFalseMaskDouble : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<double> CreateFalseMaskDouble() => CreateFalseMaskDouble();


        ///  CreateFalseMaskInt16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<short> CreateFalseMaskInt16() => CreateFalseMaskInt16();


        ///  CreateFalseMaskInt32 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<int> CreateFalseMaskInt32() => CreateFalseMaskInt32();


        ///  CreateFalseMaskInt64 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<long> CreateFalseMaskInt64() => CreateFalseMaskInt64();


        ///  CreateFalseMaskSByte : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<sbyte> CreateFalseMaskSByte() => CreateFalseMaskSByte();


        ///  CreateFalseMaskSingle : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<float> CreateFalseMaskSingle() => CreateFalseMaskSingle();


        ///  CreateFalseMaskUInt16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<ushort> CreateFalseMaskUInt16() => CreateFalseMaskUInt16();


        ///  CreateFalseMaskUInt32 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<uint> CreateFalseMaskUInt32() => CreateFalseMaskUInt32();


        ///  CreateFalseMaskUInt64 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<ulong> CreateFalseMaskUInt64() => CreateFalseMaskUInt64();


        ///  CreateMaskForFirstActiveElement : Set the first active predicate element to true

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateMaskForFirstActiveElement(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateMaskForFirstActiveElement(Vector<short> totalMask, Vector<short> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateMaskForFirstActiveElement(Vector<int> totalMask, Vector<int> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateMaskForFirstActiveElement(Vector<long> totalMask, Vector<long> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForFirstActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForFirstActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForFirstActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForFirstActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);


        ///  CreateMaskForNextActiveElement : Find next active predicate

        /// <summary>
        /// svbool_t svpnext_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForNextActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpnext_b16(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForNextActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpnext_b32(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForNextActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        /// svbool_t svpnext_b64(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForNextActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);



        ///  CreateTrueMaskByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskByte(pattern);


        ///  CreateTrueMaskDouble : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskDouble(pattern);


        ///  CreateTrueMaskInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt16(pattern);


        ///  CreateTrueMaskInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt32(pattern);


        ///  CreateTrueMaskInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt64(pattern);


        ///  CreateTrueMaskSByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSByte(pattern);


        ///  CreateTrueMaskSingle : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSingle(pattern);


        ///  CreateTrueMaskUInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b16(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt16(pattern);


        ///  CreateTrueMaskUInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b32(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt32(pattern);


        ///  CreateTrueMaskUInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b64(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt64(pattern);


        ///  CreateWhileLessThanMask16Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right) => CreateWhileLessThanMask16Bit(left, right);


        ///  CreateWhileLessThanMask32Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(int left, int right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(long left, long right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right) => CreateWhileLessThanMask32Bit(left, right);


        ///  CreateWhileLessThanMask64Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right) => CreateWhileLessThanMask64Bit(left, right);


        ///  CreateWhileLessThanMask8Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(int left, int right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(long left, long right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right) => CreateWhileLessThanMask8Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask16Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask16Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask32Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask32Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask64Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask64Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask8Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask8Bit(left, right);


        ///  Divide : Divide

        /// <summary>
        /// svint32_t svdiv[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svdiv[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svdiv[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Divide(Vector<int> left, Vector<int> right) => Divide(left, right);

        /// <summary>
        /// svint64_t svdiv[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svdiv[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svdiv[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Divide(Vector<long> left, Vector<long> right) => Divide(left, right);

        /// <summary>
        /// svuint32_t svdiv[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svdiv[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svdiv[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Divide(Vector<uint> left, Vector<uint> right) => Divide(left, right);

        /// <summary>
        /// svuint64_t svdiv[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svdiv[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svdiv[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Divide(Vector<ulong> left, Vector<ulong> right) => Divide(left, right);

        /// <summary>
        /// svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Divide(Vector<float> left, Vector<float> right) => Divide(left, right);

        /// <summary>
        /// svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Divide(Vector<double> left, Vector<double> right) => Divide(left, right);



        ///  DotProduct : Dot product

        /// <summary>
        /// svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<long> DotProduct(Vector<long> addend, Vector<short> left, Vector<short> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, Vector<byte> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right) => DotProduct(addend, left, right);


        ///  DotProductBySelectedScalar : Dot product

        /// <summary>
        /// svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> DotProductBySelectedScalar(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> DotProductBySelectedScalar(Vector<long> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> DotProductBySelectedScalar(Vector<uint> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> DotProductBySelectedScalar(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svint8_t svdup_lane[_s8](svint8_t data, uint8_t index)
        /// svint8_t svdupq_lane[_s8](svint8_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint16_t svdup_lane[_s16](svint16_t data, uint16_t index)
        /// svint16_t svdupq_lane[_s16](svint16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint32_t svdup_lane[_s32](svint32_t data, uint32_t index)
        /// svint32_t svdupq_lane[_s32](svint32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint64_t svdup_lane[_s64](svint64_t data, uint64_t index)
        /// svint64_t svdupq_lane[_s64](svint64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index)
        /// svuint8_t svdupq_lane[_u8](svuint8_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index)
        /// svuint16_t svdupq_lane[_u16](svuint16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index)
        /// svuint32_t svdupq_lane[_u32](svuint32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index)
        /// svuint64_t svdupq_lane[_u64](svuint64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index)
        /// svfloat32_t svdupq_lane[_f32](svfloat32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index)
        /// svfloat64_t svdupq_lane[_f64](svfloat64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, [ConstantExpected] byte index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// int8_t svlasta[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe sbyte ExtractAfterLastScalar(Vector<sbyte> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// int16_t svlasta[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe short ExtractAfterLastScalar(Vector<short> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// int32_t svlasta[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe int ExtractAfterLastScalar(Vector<int> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// int64_t svlasta[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe long ExtractAfterLastScalar(Vector<long> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe byte ExtractAfterLastScalar(Vector<byte> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe ushort ExtractAfterLastScalar(Vector<ushort> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe uint ExtractAfterLastScalar(Vector<uint> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe ulong ExtractAfterLastScalar(Vector<ulong> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe float ExtractAfterLastScalar(Vector<float> value) => ExtractAfterLastScalar(value);

        /// <summary>
        /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe double ExtractAfterLastScalar(Vector<double> value) => ExtractAfterLastScalar(value);


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// int8_t svlasta[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractAfterLastVector(Vector<sbyte> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// int16_t svlasta[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ExtractAfterLastVector(Vector<short> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// int32_t svlasta[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ExtractAfterLastVector(Vector<int> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// int64_t svlasta[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ExtractAfterLastVector(Vector<long> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ExtractAfterLastVector(Vector<byte> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ExtractAfterLastVector(Vector<ushort> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ExtractAfterLastVector(Vector<uint> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ExtractAfterLastVector(Vector<ulong> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ExtractAfterLastVector(Vector<float> value) => ExtractAfterLastVector(value);

        /// <summary>
        /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ExtractAfterLastVector(Vector<double> value) => ExtractAfterLastVector(value);


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// int8_t svlastb[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe sbyte ExtractLastScalar(Vector<sbyte> value) => ExtractLastScalar(value);

        /// <summary>
        /// int16_t svlastb[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe short ExtractLastScalar(Vector<short> value) => ExtractLastScalar(value);

        /// <summary>
        /// int32_t svlastb[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe int ExtractLastScalar(Vector<int> value) => ExtractLastScalar(value);

        /// <summary>
        /// int64_t svlastb[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe long ExtractLastScalar(Vector<long> value) => ExtractLastScalar(value);

        /// <summary>
        /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe byte ExtractLastScalar(Vector<byte> value) => ExtractLastScalar(value);

        /// <summary>
        /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe ushort ExtractLastScalar(Vector<ushort> value) => ExtractLastScalar(value);

        /// <summary>
        /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe uint ExtractLastScalar(Vector<uint> value) => ExtractLastScalar(value);

        /// <summary>
        /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe ulong ExtractLastScalar(Vector<ulong> value) => ExtractLastScalar(value);

        /// <summary>
        /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe float ExtractLastScalar(Vector<float> value) => ExtractLastScalar(value);

        /// <summary>
        /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe double ExtractLastScalar(Vector<double> value) => ExtractLastScalar(value);


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// int8_t svlastb[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractLastVector(Vector<sbyte> value) => ExtractLastVector(value);

        /// <summary>
        /// int16_t svlastb[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ExtractLastVector(Vector<short> value) => ExtractLastVector(value);

        /// <summary>
        /// int32_t svlastb[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ExtractLastVector(Vector<int> value) => ExtractLastVector(value);

        /// <summary>
        /// int64_t svlastb[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ExtractLastVector(Vector<long> value) => ExtractLastVector(value);

        /// <summary>
        /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ExtractLastVector(Vector<byte> value) => ExtractLastVector(value);

        /// <summary>
        /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ExtractLastVector(Vector<ushort> value) => ExtractLastVector(value);

        /// <summary>
        /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ExtractLastVector(Vector<uint> value) => ExtractLastVector(value);

        /// <summary>
        /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ExtractLastVector(Vector<ulong> value) => ExtractLastVector(value);

        /// <summary>
        /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ExtractLastVector(Vector<float> value) => ExtractLastVector(value);

        /// <summary>
        /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ExtractLastVector(Vector<double> value) => ExtractLastVector(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat32_t svexpa[_f32](svuint32_t op)
        /// </summary>
        public static unsafe Vector<float> FloatingPointExponentialAccelerator(Vector<uint> value) => FloatingPointExponentialAccelerator(value);

        /// <summary>
        /// svfloat64_t svexpa[_f64](svuint64_t op)
        /// </summary>
        public static unsafe Vector<double> FloatingPointExponentialAccelerator(Vector<ulong> value) => FloatingPointExponentialAccelerator(value);


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAdd(addend, left, right);


        ///  FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddBySelectedScalar(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        ///  FusedMultiplyAddNegated : Negated multiply-add, addend first

        /// <summary>
        /// svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddNegated(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAddNegated(addend, left, right);

        /// <summary>
        /// svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddNegated(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAddNegated(addend, left, right);


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtract(minuend, left, right);


        ///  FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractBySelectedScalar(Vector<float> minuend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractBySelectedScalar(Vector<double> minuend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        ///  FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractNegated(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtractNegated(minuend, left, right);

        /// <summary>
        /// svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractNegated(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtractNegated(minuend, left, right);


        ///  GatherPrefetch16Bit : Prefetch halfwords

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);


        ///  GatherPrefetch32Bit : Prefetch words

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);


        ///  GatherPrefetch64Bit : Prefetch doublewords

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);


        ///  GatherPrefetch8Bit : Prefetch bytes

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);


        ///  GatherVector : Unextended load

        /// <summary>
        /// svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<int> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<int> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVector(mask, address, indices);


        ///  GatherVectorByteZeroExtend : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);


        ///  GatherVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint32_t svldff1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint32_t svldff1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint32_t svldff1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint32_t svldff1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svint32_t svldff1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint32_t svldff1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svfloat32_t svldff1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        /// svfloat64_t svldff1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);


        ///  GatherVectorInt16SignExtend : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);


        ///  GatherVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint32_t svldff1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);


        ///  GatherVectorInt16WithByteOffsetsSignExtend : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);


        ///  GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorInt32SignExtend : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);


        ///  GatherVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);


        ///  GatherVectorInt32WithByteOffsetsSignExtend : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);


        ///  GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorSByteSignExtend : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);


        ///  GatherVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<int> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint32_t svldff1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint32_t svldff1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<int> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint32_t svldff1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorUInt16WithByteOffsetsZeroExtend : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);


        ///  GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorUInt16ZeroExtend : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);


        ///  GatherVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint32_t svldff1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint32_t svldff1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);


        ///  GatherVectorUInt32WithByteOffsetsZeroExtend : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);


        ///  GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);


        ///  GatherVectorUInt32ZeroExtend : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);


        ///  GatherVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);


        ///  GatherVectorWithByteOffsetFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint32_t svldff1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint32_t svldff1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svint64_t svldff1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);


        ///  GatherVectorWithByteOffsets : Unextended load

        /// <summary>
        /// svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<sbyte> mask, Vector<sbyte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<short> mask, Vector<short> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<int> mask, Vector<int> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<long> mask, Vector<long> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b16(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ushort> mask, Vector<ushort> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b32(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<uint> mask, Vector<uint> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b64(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ulong> mask, Vector<ulong> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<float> mask, Vector<float> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<double> mask, Vector<double> from) => GetActiveElementCount(mask, from);


        ///  GetFfr : Read FFR, returning predicate of succesfully loaded elements

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<sbyte> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<byte> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> GetFfr() => GetFfr();

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> GetFfr() => GetFfr();


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint16_t svinsr[_n_s16](svint16_t op1, int16_t op2)
        /// </summary>
        public static unsafe Vector<short> InsertIntoShiftedVector(Vector<short> left, short right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint32_t svinsr[_n_s32](svint32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<int> InsertIntoShiftedVector(Vector<int> left, int right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint64_t svinsr[_n_s64](svint64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<long> InsertIntoShiftedVector(Vector<long> left, long right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint16_t svinsr[_n_u16](svuint16_t op1, uint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InsertIntoShiftedVector(Vector<ushort> left, ushort right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint32_t svinsr[_n_u32](svuint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InsertIntoShiftedVector(Vector<uint> left, uint right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint64_t svinsr[_n_u64](svuint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InsertIntoShiftedVector(Vector<ulong> left, ulong right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2)
        /// </summary>
        public static unsafe Vector<float> InsertIntoShiftedVector(Vector<float> left, float right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2)
        /// </summary>
        public static unsafe Vector<double> InsertIntoShiftedVector(Vector<double> left, double right) => InsertIntoShiftedVector(left, right);


        ///  LeadingSignCount : Count leading sign bits

        /// <summary>
        /// svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingSignCount(Vector<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingSignCount(Vector<short> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingSignCount(Vector<int> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingSignCount(Vector<long> value) => LeadingSignCount(value);


        ///  LeadingZeroCount : Count leading zero bits

        /// <summary>
        /// svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<ulong> value) => LeadingZeroCount(value);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address) => LoadVector(mask, address);

        /// <summary>
        /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address) => LoadVector(mask, address);

        /// <summary>
        /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address) => LoadVector(mask, address);

        /// <summary>
        /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address) => LoadVector(mask, address);

        /// <summary>
        /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address) => LoadVector(mask, address);

        /// <summary>
        /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address) => LoadVector(mask, address);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, sbyte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, short* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, int* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, long* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, byte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, ushort* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, uint* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, ulong* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, float* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, double* address) => LoadVector128AndReplicateToVector(mask, address);


        ///  LoadVectorByteNonFaultingZeroExtendToInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt16(address);


        ///  LoadVectorByteNonFaultingZeroExtendToInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt32(address);


        ///  LoadVectorByteNonFaultingZeroExtendToInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt64(address);


        ///  LoadVectorByteNonFaultingZeroExtendToUInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt16(address);


        ///  LoadVectorByteNonFaultingZeroExtendToUInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt32(address);


        ///  LoadVectorByteNonFaultingZeroExtendToUInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt64(address);


        ///  LoadVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint32_t svldff1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint64_t svldff1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint16_t svldff1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldff1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);


        ///  LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address) => LoadVectorByteZeroExtendToInt16(mask, address);


        ///  LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address) => LoadVectorByteZeroExtendToInt32(mask, address);


        ///  LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address) => LoadVectorByteZeroExtendToInt64(mask, address);


        ///  LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address) => LoadVectorByteZeroExtendToUInt16(mask, address);


        ///  LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address) => LoadVectorByteZeroExtendToUInt32(mask, address);


        ///  LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address) => LoadVectorByteZeroExtendToUInt64(mask, address);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint8_t svldff1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, sbyte* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svint16_t svldff1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, short* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svint32_t svldff1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, int* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svint64_t svldff1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, long* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svuint8_t svldff1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, byte* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svuint16_t svldff1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, ushort* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldff1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, uint* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, ulong* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svfloat32_t svldff1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, float* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        /// svfloat64_t svldff1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, double* address) => LoadVectorFirstFaulting(mask, address);


        ///  LoadVectorInt16NonFaultingSignExtendToInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToInt32(address);


        ///  LoadVectorInt16NonFaultingSignExtendToInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToInt64(address);


        ///  LoadVectorInt16NonFaultingSignExtendToUInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt32(address);


        ///  LoadVectorInt16NonFaultingSignExtendToUInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt64(address);


        ///  LoadVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint64_t svldff1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldff1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);


        ///  LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address) => LoadVectorInt16SignExtendToInt32(mask, address);


        ///  LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address) => LoadVectorInt16SignExtendToInt64(mask, address);


        ///  LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address) => LoadVectorInt16SignExtendToUInt32(mask, address);


        ///  LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address) => LoadVectorInt16SignExtendToUInt64(mask, address);


        ///  LoadVectorInt32NonFaultingSignExtendToInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToInt64(address);


        ///  LoadVectorInt32NonFaultingSignExtendToUInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToUInt64(address);


        ///  LoadVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address) => LoadVectorInt32SignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address) => LoadVectorInt32SignExtendFirstFaulting(mask, address);


        ///  LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address) => LoadVectorInt32SignExtendToInt64(mask, address);


        ///  LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address) => LoadVectorInt32SignExtendToUInt64(mask, address);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(short* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(int* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(long* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(float* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(double* address) => LoadVectorNonFaulting(address);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address) => LoadVectorNonTemporal(mask, address);


        ///  LoadVectorSByteNonFaultingSignExtendToInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt16(address);


        ///  LoadVectorSByteNonFaultingSignExtendToInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt32(address);


        ///  LoadVectorSByteNonFaultingSignExtendToInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt64(address);


        ///  LoadVectorSByteNonFaultingSignExtendToUInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt16(address);


        ///  LoadVectorSByteNonFaultingSignExtendToUInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt32(address);


        ///  LoadVectorSByteNonFaultingSignExtendToUInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt64(address);


        ///  LoadVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendFirstFaulting(Vector<short> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint32_t svldff1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint64_t svldff1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint16_t svldff1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendFirstFaulting(Vector<ushort> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldff1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);


        ///  LoadVectorSByteSignExtendToInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address) => LoadVectorSByteSignExtendToInt16(mask, address);


        ///  LoadVectorSByteSignExtendToInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address) => LoadVectorSByteSignExtendToInt32(mask, address);


        ///  LoadVectorSByteSignExtendToInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address) => LoadVectorSByteSignExtendToInt64(mask, address);


        ///  LoadVectorSByteSignExtendToUInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt16(mask, address);


        ///  LoadVectorSByteSignExtendToUInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt32(mask, address);


        ///  LoadVectorSByteSignExtendToUInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt64(mask, address);


        ///  LoadVectorUInt16NonFaultingZeroExtendToInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt32(address);


        ///  LoadVectorUInt16NonFaultingZeroExtendToInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt64(address);


        ///  LoadVectorUInt16NonFaultingZeroExtendToUInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt32(address);


        ///  LoadVectorUInt16NonFaultingZeroExtendToUInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt64(address);


        ///  LoadVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svint64_t svldff1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldff1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);


        ///  LoadVectorUInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt32(mask, address);


        ///  LoadVectorUInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt64(mask, address);


        ///  LoadVectorUInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt32(mask, address);


        ///  LoadVectorUInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt64(mask, address);


        ///  LoadVectorUInt32NonFaultingZeroExtendToInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToInt64(address);


        ///  LoadVectorUInt32NonFaultingZeroExtendToUInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToUInt64(address);


        ///  LoadVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address) => LoadVectorUInt32ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldff1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address) => LoadVectorUInt32ZeroExtendFirstFaulting(mask, address);


        ///  LoadVectorUInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address) => LoadVectorUInt32ZeroExtendToInt64(mask, address);


        ///  LoadVectorUInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address) => LoadVectorUInt32ZeroExtendToUInt64(mask, address);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>) LoadVectorx2(Vector<sbyte> mask, sbyte* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>) LoadVectorx2(Vector<short> mask, short* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>) LoadVectorx2(Vector<int> mask, int* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>) LoadVectorx2(Vector<long> mask, long* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>) LoadVectorx2(Vector<byte> mask, byte* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>) LoadVectorx2(Vector<ushort> mask, ushort* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>) LoadVectorx2(Vector<uint> mask, uint* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>) LoadVectorx2(Vector<ulong> mask, ulong* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>) LoadVectorx2(Vector<float> mask, float* address) => LoadVectorx2(mask, address);

        /// <summary>
        /// svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>) LoadVectorx2(Vector<double> mask, double* address) => LoadVectorx2(mask, address);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx3(Vector<sbyte> mask, sbyte* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>) LoadVectorx3(Vector<short> mask, short* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>) LoadVectorx3(Vector<int> mask, int* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>) LoadVectorx3(Vector<long> mask, long* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx3(Vector<byte> mask, byte* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx3(Vector<ushort> mask, ushort* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx3(Vector<uint> mask, uint* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx3(Vector<ulong> mask, ulong* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>) LoadVectorx3(Vector<float> mask, float* address) => LoadVectorx3(mask, address);

        /// <summary>
        /// svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>) LoadVectorx3(Vector<double> mask, double* address) => LoadVectorx3(mask, address);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx4(Vector<sbyte> mask, sbyte* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) LoadVectorx4(Vector<short> mask, short* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) LoadVectorx4(Vector<int> mask, int* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) LoadVectorx4(Vector<long> mask, long* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx4(Vector<byte> mask, byte* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx4(Vector<ushort> mask, ushort* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx4(Vector<uint> mask, uint* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx4(Vector<ulong> mask, ulong* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) LoadVectorx4(Vector<float> mask, float* address) => LoadVectorx4(mask, address);

        /// <summary>
        /// svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) LoadVectorx4(Vector<double> mask, double* address) => LoadVectorx4(mask, address);


        ///  Max : Maximum

        /// <summary>
        /// svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right) => Max(left, right);

        /// <summary>
        /// svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Max(Vector<short> left, Vector<short> right) => Max(left, right);

        /// <summary>
        /// svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Max(Vector<int> left, Vector<int> right) => Max(left, right);

        /// <summary>
        /// svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Max(Vector<long> left, Vector<long> right) => Max(left, right);

        /// <summary>
        /// svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Max(Vector<byte> left, Vector<byte> right) => Max(left, right);

        /// <summary>
        /// svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right) => Max(left, right);

        /// <summary>
        /// svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Max(Vector<uint> left, Vector<uint> right) => Max(left, right);

        /// <summary>
        /// svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right) => Max(left, right);

        /// <summary>
        /// svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Max(Vector<float> left, Vector<float> right) => Max(left, right);

        /// <summary>
        /// svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Max(Vector<double> left, Vector<double> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// int8_t svmaxv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> MaxAcross(Vector<sbyte> value) => MaxAcross(value);

        /// <summary>
        /// int16_t svmaxv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> MaxAcross(Vector<short> value) => MaxAcross(value);

        /// <summary>
        /// int32_t svmaxv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> MaxAcross(Vector<int> value) => MaxAcross(value);

        /// <summary>
        /// int64_t svmaxv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> MaxAcross(Vector<long> value) => MaxAcross(value);

        /// <summary>
        /// uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> MaxAcross(Vector<byte> value) => MaxAcross(value);

        /// <summary>
        /// uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> MaxAcross(Vector<ushort> value) => MaxAcross(value);

        /// <summary>
        /// uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> MaxAcross(Vector<uint> value) => MaxAcross(value);

        /// <summary>
        /// uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> MaxAcross(Vector<ulong> value) => MaxAcross(value);

        /// <summary>
        /// float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MaxAcross(Vector<float> value) => MaxAcross(value);

        /// <summary>
        /// float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MaxAcross(Vector<double> value) => MaxAcross(value);


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MaxNumber(Vector<float> left, Vector<float> right) => MaxNumber(left, right);

        /// <summary>
        /// svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MaxNumber(Vector<double> left, Vector<double> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MaxNumberAcross(Vector<float> value) => MaxNumberAcross(value);

        /// <summary>
        /// float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MaxNumberAcross(Vector<double> value) => MaxNumberAcross(value);


        ///  Min : Minimum

        /// <summary>
        /// svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right) => Min(left, right);

        /// <summary>
        /// svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Min(Vector<short> left, Vector<short> right) => Min(left, right);

        /// <summary>
        /// svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Min(Vector<int> left, Vector<int> right) => Min(left, right);

        /// <summary>
        /// svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Min(Vector<long> left, Vector<long> right) => Min(left, right);

        /// <summary>
        /// svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Min(Vector<byte> left, Vector<byte> right) => Min(left, right);

        /// <summary>
        /// svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right) => Min(left, right);

        /// <summary>
        /// svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Min(Vector<uint> left, Vector<uint> right) => Min(left, right);

        /// <summary>
        /// svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right) => Min(left, right);

        /// <summary>
        /// svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Min(Vector<float> left, Vector<float> right) => Min(left, right);

        /// <summary>
        /// svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Min(Vector<double> left, Vector<double> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// int8_t svminv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> MinAcross(Vector<sbyte> value) => MinAcross(value);

        /// <summary>
        /// int16_t svminv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> MinAcross(Vector<short> value) => MinAcross(value);

        /// <summary>
        /// int32_t svminv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> MinAcross(Vector<int> value) => MinAcross(value);

        /// <summary>
        /// int64_t svminv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> MinAcross(Vector<long> value) => MinAcross(value);

        /// <summary>
        /// uint8_t svminv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> MinAcross(Vector<byte> value) => MinAcross(value);

        /// <summary>
        /// uint16_t svminv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> MinAcross(Vector<ushort> value) => MinAcross(value);

        /// <summary>
        /// uint32_t svminv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> MinAcross(Vector<uint> value) => MinAcross(value);

        /// <summary>
        /// uint64_t svminv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> MinAcross(Vector<ulong> value) => MinAcross(value);

        /// <summary>
        /// float32_t svminv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MinAcross(Vector<float> value) => MinAcross(value);

        /// <summary>
        /// float64_t svminv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MinAcross(Vector<double> value) => MinAcross(value);


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MinNumber(Vector<float> left, Vector<float> right) => MinNumber(left, right);

        /// <summary>
        /// svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MinNumber(Vector<double> left, Vector<double> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MinNumberAcross(Vector<float> value) => MinNumberAcross(value);

        /// <summary>
        /// float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MinNumberAcross(Vector<double> value) => MinNumberAcross(value);



        ///  Multiply : Multiply

        /// <summary>
        /// svint8_t svmul[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmul[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmul[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Multiply(Vector<sbyte> left, Vector<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// svint16_t svmul[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmul[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmul[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Multiply(Vector<short> left, Vector<short> right) => Multiply(left, right);

        /// <summary>
        /// svint32_t svmul[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmul[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmul[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Multiply(Vector<int> left, Vector<int> right) => Multiply(left, right);

        /// <summary>
        /// svint64_t svmul[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmul[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmul[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Multiply(Vector<long> left, Vector<long> right) => Multiply(left, right);

        /// <summary>
        /// svuint8_t svmul[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmul[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Multiply(Vector<byte> left, Vector<byte> right) => Multiply(left, right);

        /// <summary>
        /// svuint16_t svmul[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmul[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmul[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Multiply(Vector<ushort> left, Vector<ushort> right) => Multiply(left, right);

        /// <summary>
        /// svuint32_t svmul[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmul[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmul[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Multiply(Vector<uint> left, Vector<uint> right) => Multiply(left, right);

        /// <summary>
        /// svuint64_t svmul[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmul[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmul[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Multiply(Vector<ulong> left, Vector<ulong> right) => Multiply(left, right);

        /// <summary>
        /// svfloat32_t svmul[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmul[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmul[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Multiply(Vector<float> left, Vector<float> right) => Multiply(left, right);

        /// <summary>
        /// svfloat64_t svmul[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmul[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Multiply(Vector<double> left, Vector<double> right) => Multiply(left, right);


        ///  MultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => MultiplyAdd(addend, left, right);




        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svfloat32_t svcmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// svfloat32_t svcmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// svfloat32_t svcmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplex(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        /// svfloat64_t svcmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// svfloat64_t svcmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// svfloat64_t svcmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<double> MultiplyAddRotateComplex(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);


        ///  MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

        /// <summary>
        /// svfloat32_t svcmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);


        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplyBySelectedScalar(Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> MultiplyBySelectedScalar(Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right) => MultiplyExtended(left, right);

        /// <summary>
        /// svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right) => MultiplyExtended(left, right);



        ///  MultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right) => MultiplySubtract(minuend, left, right);




        ///  Negate : Negate

        /// <summary>
        /// svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> Negate(Vector<sbyte> value) => Negate(value);

        /// <summary>
        /// svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> Negate(Vector<short> value) => Negate(value);

        /// <summary>
        /// svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Negate(Vector<int> value) => Negate(value);

        /// <summary>
        /// svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Negate(Vector<long> value) => Negate(value);

        /// <summary>
        /// svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Negate(Vector<float> value) => Negate(value);

        /// <summary>
        /// svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Negate(Vector<double> value) => Negate(value);




        ///  Not : Bitwise invert

        /// <summary>
        /// svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> Not(Vector<sbyte> value) => Not(value);

        /// <summary>
        /// svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> Not(Vector<short> value) => Not(value);

        /// <summary>
        /// svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> Not(Vector<int> value) => Not(value);

        /// <summary>
        /// svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> Not(Vector<long> value) => Not(value);

        /// <summary>
        /// svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> Not(Vector<byte> value) => Not(value);

        /// <summary>
        /// svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> Not(Vector<ushort> value) => Not(value);

        /// <summary>
        /// svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> Not(Vector<uint> value) => Not(value);

        /// <summary>
        /// svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> Not(Vector<ulong> value) => Not(value);


        ///  Or : Bitwise inclusive OR

        /// <summary>
        /// svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right) => Or(left, right);

        /// <summary>
        /// svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> Or(Vector<short> left, Vector<short> right) => Or(left, right);

        /// <summary>
        /// svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> Or(Vector<int> left, Vector<int> right) => Or(left, right);

        /// <summary>
        /// svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> Or(Vector<long> left, Vector<long> right) => Or(left, right);

        /// <summary>
        /// svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> Or(Vector<byte> left, Vector<byte> right) => Or(left, right);

        /// <summary>
        /// svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right) => Or(left, right);

        /// <summary>
        /// svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> Or(Vector<uint> left, Vector<uint> right) => Or(left, right);

        /// <summary>
        /// svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right) => Or(left, right);


        ///  OrAcross : Bitwise inclusive OR reduction to scalar

        /// <summary>
        /// int8_t svorv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> OrAcross(Vector<sbyte> value) => OrAcross(value);

        /// <summary>
        /// int16_t svorv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> OrAcross(Vector<short> value) => OrAcross(value);

        /// <summary>
        /// int32_t svorv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> OrAcross(Vector<int> value) => OrAcross(value);

        /// <summary>
        /// int64_t svorv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> OrAcross(Vector<long> value) => OrAcross(value);

        /// <summary>
        /// uint8_t svorv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> OrAcross(Vector<byte> value) => OrAcross(value);

        /// <summary>
        /// uint16_t svorv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> OrAcross(Vector<ushort> value) => OrAcross(value);

        /// <summary>
        /// uint32_t svorv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> OrAcross(Vector<uint> value) => OrAcross(value);

        /// <summary>
        /// uint64_t svorv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> OrAcross(Vector<ulong> value) => OrAcross(value);


        ///  OrNot : Bitwise NOR

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> OrNot(Vector<sbyte> left, Vector<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> OrNot(Vector<short> left, Vector<short> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> OrNot(Vector<int> left, Vector<int> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> OrNot(Vector<long> left, Vector<long> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> OrNot(Vector<byte> left, Vector<byte> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> OrNot(Vector<ushort> left, Vector<ushort> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> OrNot(Vector<uint> left, Vector<uint> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> OrNot(Vector<ulong> left, Vector<ulong> right) => OrNot(left, right);


        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<sbyte> value) => PopCount(value);

        /// <summary>
        /// svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<byte> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<short> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<ushort> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<int> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<float> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<uint> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<long> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<double> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<ulong> value) => PopCount(value);


        ///  PrefetchBytes : Prefetch bytes

        /// <summary>
        /// void svprfb(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchBytes(mask, address, prefetchType);


        ///  PrefetchInt16 : Prefetch halfwords

        /// <summary>
        /// void svprfh(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt16(mask, address, prefetchType);


        ///  PrefetchInt32 : Prefetch words

        /// <summary>
        /// void svprfw(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt32(mask, address, prefetchType);


        ///  PrefetchInt64 : Prefetch doublewords

        /// <summary>
        /// void svprfd(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt64(mask, address, prefetchType);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat32_t svrecpe[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalEstimate(Vector<float> value) => ReciprocalEstimate(value);

        /// <summary>
        /// svfloat64_t svrecpe[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalEstimate(Vector<double> value) => ReciprocalEstimate(value);


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalExponent(Vector<float> value) => ReciprocalExponent(value);

        /// <summary>
        /// svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalExponent(Vector<double> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat32_t svrsqrte[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtEstimate(Vector<float> value) => ReciprocalSqrtEstimate(value);

        /// <summary>
        /// svfloat64_t svrsqrte[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtEstimate(Vector<double> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right) => ReciprocalSqrtStep(left, right);

        /// <summary>
        /// svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right) => ReciprocalStep(left, right);

        /// <summary>
        /// svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right) => ReciprocalStep(left, right);


        ///  ReverseBits : Reverse bits

        /// <summary>
        /// svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svrbit[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svrbit[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ReverseBits(Vector<sbyte> value) => ReverseBits(value);

        /// <summary>
        /// svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svrbit[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svrbit[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseBits(Vector<short> value) => ReverseBits(value);

        /// <summary>
        /// svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrbit[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrbit[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseBits(Vector<int> value) => ReverseBits(value);

        /// <summary>
        /// svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrbit[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrbit[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseBits(Vector<long> value) => ReverseBits(value);

        /// <summary>
        /// svuint8_t svrbit[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svrbit[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ReverseBits(Vector<byte> value) => ReverseBits(value);

        /// <summary>
        /// svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svrbit[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svrbit[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseBits(Vector<ushort> value) => ReverseBits(value);

        /// <summary>
        /// svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrbit[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrbit[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseBits(Vector<uint> value) => ReverseBits(value);

        /// <summary>
        /// svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrbit[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrbit[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseBits(Vector<ulong> value) => ReverseBits(value);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svint8_t svrev[_s8](svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ReverseElement(Vector<sbyte> value) => ReverseElement(value);

        /// <summary>
        /// svint16_t svrev[_s16](svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseElement(Vector<short> value) => ReverseElement(value);

        /// <summary>
        /// svint32_t svrev[_s32](svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement(Vector<int> value) => ReverseElement(value);

        /// <summary>
        /// svint64_t svrev[_s64](svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement(Vector<long> value) => ReverseElement(value);

        /// <summary>
        /// svuint8_t svrev[_u8](svuint8_t op)
        /// svbool_t svrev_b8(svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> ReverseElement(Vector<byte> value) => ReverseElement(value);

        /// <summary>
        /// svuint16_t svrev[_u16](svuint16_t op)
        /// svbool_t svrev_b16(svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement(Vector<ushort> value) => ReverseElement(value);

        /// <summary>
        /// svuint32_t svrev[_u32](svuint32_t op)
        /// svbool_t svrev_b32(svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement(Vector<uint> value) => ReverseElement(value);

        /// <summary>
        /// svuint64_t svrev[_u64](svuint64_t op)
        /// svbool_t svrev_b64(svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement(Vector<ulong> value) => ReverseElement(value);

        /// <summary>
        /// svfloat32_t svrev[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReverseElement(Vector<float> value) => ReverseElement(value);

        /// <summary>
        /// svfloat64_t svrev[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReverseElement(Vector<double> value) => ReverseElement(value);


        ///  ReverseElement16 : Reverse halfwords within elements

        /// <summary>
        /// svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrevh[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrevh[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement16(Vector<int> value) => ReverseElement16(value);

        /// <summary>
        /// svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevh[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevh[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement16(Vector<long> value) => ReverseElement16(value);

        /// <summary>
        /// svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrevh[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrevh[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement16(Vector<uint> value) => ReverseElement16(value);

        /// <summary>
        /// svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevh[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevh[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement16(Vector<ulong> value) => ReverseElement16(value);


        ///  ReverseElement32 : Reverse words within elements

        /// <summary>
        /// svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevw[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevw[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement32(Vector<long> value) => ReverseElement32(value);

        /// <summary>
        /// svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevw[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevw[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement32(Vector<ulong> value) => ReverseElement32(value);


        ///  ReverseElement8 : Reverse bytes within elements

        /// <summary>
        /// svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svrevb[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svrevb[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseElement8(Vector<short> value) => ReverseElement8(value);

        /// <summary>
        /// svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrevb[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrevb[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement8(Vector<int> value) => ReverseElement8(value);

        /// <summary>
        /// svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevb[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevb[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement8(Vector<long> value) => ReverseElement8(value);

        /// <summary>
        /// svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svrevb[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svrevb[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement8(Vector<ushort> value) => ReverseElement8(value);

        /// <summary>
        /// svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrevb[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrevb[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement8(Vector<uint> value) => ReverseElement8(value);

        /// <summary>
        /// svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevb[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevb[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement8(Vector<ulong> value) => ReverseElement8(value);


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundAwayFromZero(Vector<float> value) => RoundAwayFromZero(value);

        /// <summary>
        /// svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundAwayFromZero(Vector<double> value) => RoundAwayFromZero(value);


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNearest(Vector<float> value) => RoundToNearest(value);

        /// <summary>
        /// svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNearest(Vector<double> value) => RoundToNearest(value);


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNegativeInfinity(Vector<float> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNegativeInfinity(Vector<double> value) => RoundToNegativeInfinity(value);


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToPositiveInfinity(Vector<float> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToPositiveInfinity(Vector<double> value) => RoundToPositiveInfinity(value);


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToZero(Vector<float> value) => RoundToZero(value);

        /// <summary>
        /// svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToZero(Vector<double> value) => RoundToZero(value);




        ///  SaturatingDecrementBy16BitElementCount : Saturating decrement by number of halfword elements

        /// <summary>
        /// int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);


        ///  SaturatingDecrementBy32BitElementCount : Saturating decrement by number of word elements

        /// <summary>
        /// int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);


        ///  SaturatingDecrementBy64BitElementCount : Saturating decrement by number of doubleword elements

        /// <summary>
        /// int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);


        ///  SaturatingDecrementBy8BitElementCount : Saturating decrement by number of byte elements

        /// <summary>
        /// int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);


        ///  SaturatingDecrementByActiveElementCount : Saturating decrement by active element count

        /// <summary>
        /// svint16_t svqdecp[_s16](svint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svint32_t svqdecp[_s32](svint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svint64_t svqdecp[_s64](svint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);


        ///  SaturatingIncrementBy16BitElementCount : Saturating increment by number of halfword elements

        /// <summary>
        /// int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);


        ///  SaturatingIncrementBy32BitElementCount : Saturating increment by number of word elements

        /// <summary>
        /// int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);


        ///  SaturatingIncrementBy64BitElementCount : Saturating increment by number of doubleword elements

        /// <summary>
        /// int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);


        ///  SaturatingIncrementBy8BitElementCount : Saturating increment by number of byte elements

        /// <summary>
        /// int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);


        ///  SaturatingIncrementByActiveElementCount : Saturating increment by active element count

        /// <summary>
        /// svint16_t svqincp[_s16](svint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svint32_t svqincp[_s32](svint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svint64_t svqincp[_s64](svint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);


        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<float> Scale(Vector<float> left, Vector<int> right) => Scale(left, right);

        /// <summary>
        /// svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<double> Scale(Vector<double> left, Vector<long> right) => Scale(left, right);


        ///  Scatter : Non-truncating store

        /// <summary>
        /// void svst1_scatter_[s32]offset[_s32](svbool_t pg, int32_t *base, svint32_t offsets, svint32_t data)
        /// void svst1_scatter_[s32]index[_s32](svbool_t pg, int32_t *base, svint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<int> indicies, Vector<int> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data)
        /// void svst1_scatter_[u32]index[_s32](svbool_t pg, int32_t *base, svuint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<uint> indicies, Vector<int> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data)
        /// void svst1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<long> indicies, Vector<long> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data)
        /// void svst1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<ulong> indicies, Vector<long> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter_[s32]offset[_u32](svbool_t pg, uint32_t *base, svint32_t offsets, svuint32_t data)
        /// void svst1_scatter_[s32]index[_u32](svbool_t pg, uint32_t *base, svint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<int> indicies, Vector<uint> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data)
        /// void svst1_scatter_[u32]index[_u32](svbool_t pg, uint32_t *base, svuint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<uint> indicies, Vector<uint> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data)
        /// void svst1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<long> indicies, Vector<ulong> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data)
        /// void svst1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<ulong> indicies, Vector<ulong> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter_[s32]offset[_f32](svbool_t pg, float32_t *base, svint32_t offsets, svfloat32_t data)
        /// void svst1_scatter_[s32]index[_f32](svbool_t pg, float32_t *base, svint32_t indices, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<int> indicies, Vector<float> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, Vector<uint> addresses, Vector<float> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data)
        /// void svst1_scatter_[u32]index[_f32](svbool_t pg, float32_t *base, svuint32_t indices, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<uint> indicies, Vector<float> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data)
        /// void svst1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<long> indicies, Vector<double> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        /// void svst1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, Vector<ulong> addresses, Vector<double> data) => Scatter(mask, addresses, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data)
        /// void svst1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<ulong> indicies, Vector<double> data) => Scatter(mask, address, indicies, data);


        ///  Scatter16BitNarrowing : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter16BitNarrowing(mask, addresses, data);


        ///  Scatter16BitWithByteOffsetsNarrowing : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_s32](svbool_t pg, int16_t *base, svint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> offsets, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s32]index[_s32](svbool_t pg, int16_t *base, svint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> indices, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[u32]index[_s32](svbool_t pg, int16_t *base, svuint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> indices, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_u32](svbool_t pg, uint16_t *base, svint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> offsets, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s32]index[_u32](svbool_t pg, uint16_t *base, svint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> indices, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[u32]index[_u32](svbool_t pg, uint16_t *base, svuint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> indices, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, indices, data);


        ///  Scatter32BitNarrowing : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter32BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter32BitNarrowing(mask, addresses, data);


        ///  Scatter32BitWithByteOffsetsNarrowing : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);

        /// <summary>
        /// void svst1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, indices, data);


        ///  Scatter8BitNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        /// void svst1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter8BitNarrowing(mask, addresses, data);


        ///  Scatter8BitWithByteOffsetsNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_s32](svbool_t pg, int8_t *base, svint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<int> offsets, Vector<int> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_u32](svbool_t pg, uint8_t *base, svint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<int> offsets, Vector<uint> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);


        ///  SetFfr : Write to the first-fault register

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<sbyte> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<short> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<int> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<long> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<byte> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<ushort> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<uint> value) => SetFfr(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<ulong> value) => SetFfr(value);


        ///  ShiftLeftLogical : Logical shift left

        /// <summary>
        /// svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsl_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right) => ShiftLeftLogical(left, right);


        ///  ShiftRightArithmetic : Arithmetic shift right

        /// <summary>
        /// svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);


        ///  ShiftRightArithmeticForDivide : Arithmetic shift right for divide by immediate

        /// <summary>
        /// svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticForDivide(Vector<sbyte> value, [ConstantExpected] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticForDivide(Vector<short> value, [ConstantExpected] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticForDivide(Vector<int> value, [ConstantExpected] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticForDivide(Vector<long> value, [ConstantExpected] byte control) => ShiftRightArithmeticForDivide(value, control);


        ///  ShiftRightLogical : Logical shift right

        /// <summary>
        /// svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogical(Vector<ulong> left, Vector<ulong> right) => ShiftRightLogical(left, right);


        ///  SignExtend16 : Sign-extend the low 16 bits

        /// <summary>
        /// svint32_t svexth[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svexth[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svexth[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtend16(Vector<int> value) => SignExtend16(value);

        /// <summary>
        /// svint64_t svexth[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svexth[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svexth[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend16(Vector<long> value) => SignExtend16(value);


        ///  SignExtend32 : Sign-extend the low 32 bits

        /// <summary>
        /// svint64_t svextw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svextw[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svextw[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend32(Vector<long> value) => SignExtend32(value);


        ///  SignExtend8 : Sign-extend the low 8 bits

        /// <summary>
        /// svint16_t svextb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svextb[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svextb[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtend8(Vector<short> value) => SignExtend8(value);

        /// <summary>
        /// svint32_t svextb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svextb[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svextb[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtend8(Vector<int> value) => SignExtend8(value);

        /// <summary>
        /// svint64_t svextb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svextb[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svextb[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend8(Vector<long> value) => SignExtend8(value);


        ///  SignExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svint16_t svunpklo[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningLower(Vector<sbyte> value) => SignExtendWideningLower(value);

        /// <summary>
        /// svint32_t svunpklo[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningLower(Vector<short> value) => SignExtendWideningLower(value);

        /// <summary>
        /// svint64_t svunpklo[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningLower(Vector<int> value) => SignExtendWideningLower(value);


        ///  SignExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svint16_t svunpkhi[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningUpper(Vector<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// svint32_t svunpkhi[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningUpper(Vector<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// svint64_t svunpkhi[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningUpper(Vector<int> value) => SignExtendWideningUpper(value);


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => Splice(mask, left, right);

        /// <summary>
        /// svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right) => Splice(mask, left, right);

        /// <summary>
        /// svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right) => Splice(mask, left, right);

        /// <summary>
        /// svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right) => Splice(mask, left, right);


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Sqrt(Vector<float> value) => Sqrt(value);

        /// <summary>
        /// svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Sqrt(Vector<double> value) => Sqrt(value);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3, Vector<sbyte> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, Vector<short> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3, Vector<short> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, Vector<int> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3, Vector<int> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, Vector<long> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3, Vector<long> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, Vector<byte> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3, Vector<byte> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, Vector<ushort> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3, Vector<ushort> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, Vector<uint> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3, Vector<uint> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, Vector<ulong> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3, Vector<ulong> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, Vector<float> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3, Vector<float> Value4) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, Vector<double> data) => Store(mask, address, data);

        /// <summary>
        /// void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3) data) => Store(mask, address, Value1,);

        /// <summary>
        /// void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3, Vector<double> Value4) data) => Store(mask, address, Value1,);


        ///  StoreNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* address, Vector<short> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, short* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, short* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* address, Vector<ushort> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, byte* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<short> mask, short* address, Vector<short> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<int> mask, int* address, Vector<int> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<long> mask, long* address, Vector<long> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* address, Vector<byte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* address, Vector<ushort> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* address, Vector<uint> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<float> mask, float* address, Vector<float> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<double> mask, double* address, Vector<double> data) => StoreNonTemporal(mask, address, data);


        ///  Subtract : Subtract

        /// <summary>
        /// svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Subtract(Vector<short> left, Vector<short> right) => Subtract(left, right);

        /// <summary>
        /// svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Subtract(Vector<int> left, Vector<int> right) => Subtract(left, right);

        /// <summary>
        /// svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Subtract(Vector<long> left, Vector<long> right) => Subtract(left, right);

        /// <summary>
        /// svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Subtract(Vector<byte> left, Vector<byte> right) => Subtract(left, right);

        /// <summary>
        /// svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right) => Subtract(left, right);

        /// <summary>
        /// svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Subtract(Vector<uint> left, Vector<uint> right) => Subtract(left, right);

        /// <summary>
        /// svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right) => Subtract(left, right);

        /// <summary>
        /// svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Subtract(Vector<float> left, Vector<float> right) => Subtract(left, right);

        /// <summary>
        /// svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Subtract(Vector<double> left, Vector<double> right) => Subtract(left, right);



        ///  SubtractSaturate : Saturating subtract

        /// <summary>
        /// svint8_t svqsub[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint16_t svqsub[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint32_t svqsub[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint64_t svqsub[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint16_t svqsub[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint32_t svqsub[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint64_t svqsub[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) => SubtractSaturate(left, right);


        ///  TestAnyTrue : Test whether any active element is true

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<short> leftMask, Vector<short> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<int> leftMask, Vector<int> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<long> leftMask, Vector<long> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<byte> leftMask, Vector<byte> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<uint> leftMask, Vector<uint> rightMask) => TestAnyTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) => TestAnyTrue(leftMask, rightMask);


        ///  TestFirstTrue : Test whether the first active element is true

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<short> leftMask, Vector<short> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<int> leftMask, Vector<int> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<long> leftMask, Vector<long> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<byte> leftMask, Vector<byte> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<uint> leftMask, Vector<uint> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) => TestFirstTrue(leftMask, rightMask);


        ///  TestLastTrue : Test whether the last active element is true

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<short> leftMask, Vector<short> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<int> leftMask, Vector<int> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<long> leftMask, Vector<long> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<byte> leftMask, Vector<byte> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<uint> leftMask, Vector<uint> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) => TestLastTrue(leftMask, rightMask);


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right) => TransposeEven(left, right);

        /// <summary>
        /// svint16_t svtrn1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> TransposeEven(Vector<short> left, Vector<short> right) => TransposeEven(left, right);

        /// <summary>
        /// svint32_t svtrn1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> TransposeEven(Vector<int> left, Vector<int> right) => TransposeEven(left, right);

        /// <summary>
        /// svint64_t svtrn1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> TransposeEven(Vector<long> left, Vector<long> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svtrn1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svtrn1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svtrn1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svtrn1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> TransposeEven(Vector<float> left, Vector<float> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> TransposeEven(Vector<double> left, Vector<double> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint16_t svtrn2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> TransposeOdd(Vector<short> left, Vector<short> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint32_t svtrn2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> TransposeOdd(Vector<int> left, Vector<int> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint64_t svtrn2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> TransposeOdd(Vector<long> left, Vector<long> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svtrn2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svtrn2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svtrn2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svtrn2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> TransposeOdd(Vector<float> left, Vector<float> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> TransposeOdd(Vector<double> left, Vector<double> right) => TransposeOdd(left, right);


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat32_t svtmad[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<float> TrigonometricMultiplyAddCoefficient(Vector<float> left, Vector<float> right, [ConstantExpected] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);

        /// <summary>
        /// svfloat64_t svtmad[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<double> TrigonometricMultiplyAddCoefficient(Vector<double> left, Vector<double> right, [ConstantExpected] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat32_t svtssel[_f32](svfloat32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<float> TrigonometricSelectCoefficient(Vector<float> value, Vector<uint> selector) => TrigonometricSelectCoefficient(value, selector);

        /// <summary>
        /// svfloat64_t svtssel[_f64](svfloat64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<double> TrigonometricSelectCoefficient(Vector<double> value, Vector<ulong> selector) => TrigonometricSelectCoefficient(value, selector);


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat32_t svtsmul[_f32](svfloat32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<float> TrigonometricStartingValue(Vector<float> value, Vector<uint> sign) => TrigonometricStartingValue(value, sign);

        /// <summary>
        /// svfloat64_t svtsmul[_f64](svfloat64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<double> TrigonometricStartingValue(Vector<double> value, Vector<ulong> sign) => TrigonometricStartingValue(value, sign);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right) => UnzipEven(left, right);

        /// <summary>
        /// svint16_t svuzp1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> UnzipEven(Vector<short> left, Vector<short> right) => UnzipEven(left, right);

        /// <summary>
        /// svint32_t svuzp1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> UnzipEven(Vector<int> left, Vector<int> right) => UnzipEven(left, right);

        /// <summary>
        /// svint64_t svuzp1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> UnzipEven(Vector<long> left, Vector<long> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svuzp1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint16_t svuzp1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svuzp1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> UnzipEven(Vector<ushort> left, Vector<ushort> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint32_t svuzp1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svuzp1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> UnzipEven(Vector<uint> left, Vector<uint> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint64_t svuzp1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svuzp1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> UnzipEven(Vector<ulong> left, Vector<ulong> right) => UnzipEven(left, right);

        /// <summary>
        /// svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> UnzipEven(Vector<float> left, Vector<float> right) => UnzipEven(left, right);

        /// <summary>
        /// svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> UnzipEven(Vector<double> left, Vector<double> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint16_t svuzp2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> UnzipOdd(Vector<short> left, Vector<short> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint32_t svuzp2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> UnzipOdd(Vector<int> left, Vector<int> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint64_t svuzp2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> UnzipOdd(Vector<long> left, Vector<long> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svuzp2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint16_t svuzp2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svuzp2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> UnzipOdd(Vector<ushort> left, Vector<ushort> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint32_t svuzp2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svuzp2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> UnzipOdd(Vector<uint> left, Vector<uint> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint64_t svuzp2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svuzp2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> UnzipOdd(Vector<ulong> left, Vector<ulong> right) => UnzipOdd(left, right);

        /// <summary>
        /// svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> UnzipOdd(Vector<float> left, Vector<float> right) => UnzipOdd(left, right);

        /// <summary>
        /// svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> UnzipOdd(Vector<double> left, Vector<double> right) => UnzipOdd(left, right);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svint8_t svtbl[_s8](svint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint16_t svtbl[_s16](svint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint32_t svtbl[_s32](svint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint64_t svtbl[_s64](svint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices) => VectorTableLookup(data, indices);


        ///  Xor : Bitwise exclusive OR

        /// <summary>
        /// svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right) => Xor(left, right);

        /// <summary>
        /// svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> Xor(Vector<short> left, Vector<short> right) => Xor(left, right);

        /// <summary>
        /// svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> Xor(Vector<int> left, Vector<int> right) => Xor(left, right);

        /// <summary>
        /// svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> Xor(Vector<long> left, Vector<long> right) => Xor(left, right);

        /// <summary>
        /// svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> Xor(Vector<byte> left, Vector<byte> right) => Xor(left, right);

        /// <summary>
        /// svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right) => Xor(left, right);

        /// <summary>
        /// svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> Xor(Vector<uint> left, Vector<uint> right) => Xor(left, right);

        /// <summary>
        /// svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right) => Xor(left, right);


        ///  XorAcross : Bitwise exclusive OR reduction to scalar

        /// <summary>
        /// int8_t sveorv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> XorAcross(Vector<sbyte> value) => XorAcross(value);

        /// <summary>
        /// int16_t sveorv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> XorAcross(Vector<short> value) => XorAcross(value);

        /// <summary>
        /// int32_t sveorv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> XorAcross(Vector<int> value) => XorAcross(value);

        /// <summary>
        /// int64_t sveorv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> XorAcross(Vector<long> value) => XorAcross(value);

        /// <summary>
        /// uint8_t sveorv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> XorAcross(Vector<byte> value) => XorAcross(value);

        /// <summary>
        /// uint16_t sveorv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> XorAcross(Vector<ushort> value) => XorAcross(value);

        /// <summary>
        /// uint32_t sveorv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> XorAcross(Vector<uint> value) => XorAcross(value);

        /// <summary>
        /// uint64_t sveorv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> XorAcross(Vector<ulong> value) => XorAcross(value);


        ///  ZeroExtend16 : Zero-extend the low 16 bits

        /// <summary>
        /// svuint32_t svexth[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svexth[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svexth[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend16(Vector<uint> value) => ZeroExtend16(value);

        /// <summary>
        /// svuint64_t svexth[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svexth[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svexth[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend16(Vector<ulong> value) => ZeroExtend16(value);


        ///  ZeroExtend32 : Zero-extend the low 32 bits

        /// <summary>
        /// svuint64_t svextw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svextw[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svextw[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value) => ZeroExtend32(value);


        ///  ZeroExtend8 : Zero-extend the low 8 bits

        /// <summary>
        /// svuint16_t svextb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svextb[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svextb[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtend8(Vector<ushort> value) => ZeroExtend8(value);

        /// <summary>
        /// svuint32_t svextb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svextb[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svextb[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend8(Vector<uint> value) => ZeroExtend8(value);

        /// <summary>
        /// svuint64_t svextb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svextb[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svextb[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend8(Vector<ulong> value) => ZeroExtend8(value);


        ///  ZeroExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svuint16_t svunpklo[_u16](svuint8_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningLower(Vector<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint32_t svunpklo[_u32](svuint16_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningLower(Vector<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint64_t svunpklo[_u64](svuint32_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningLower(Vector<uint> value) => ZeroExtendWideningLower(value);


        ///  ZeroExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svuint16_t svunpkhi[_u16](svuint8_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningUpper(Vector<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// svuint32_t svunpkhi[_u32](svuint16_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningUpper(Vector<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// svuint64_t svunpkhi[_u64](svuint32_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value) => ZeroExtendWideningUpper(value);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right) => ZipHigh(left, right);

        /// <summary>
        /// svint16_t svzip2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ZipHigh(Vector<short> left, Vector<short> right) => ZipHigh(left, right);

        /// <summary>
        /// svint32_t svzip2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ZipHigh(Vector<int> left, Vector<int> right) => ZipHigh(left, right);

        /// <summary>
        /// svint64_t svzip2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ZipHigh(Vector<long> left, Vector<long> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svzip2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint16_t svzip2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svzip2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ZipHigh(Vector<ushort> left, Vector<ushort> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint32_t svzip2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svzip2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ZipHigh(Vector<uint> left, Vector<uint> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint64_t svzip2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svzip2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ZipHigh(Vector<ulong> left, Vector<ulong> right) => ZipHigh(left, right);

        /// <summary>
        /// svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ZipHigh(Vector<float> left, Vector<float> right) => ZipHigh(left, right);

        /// <summary>
        /// svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ZipHigh(Vector<double> left, Vector<double> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right) => ZipLow(left, right);

        /// <summary>
        /// svint16_t svzip1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ZipLow(Vector<short> left, Vector<short> right) => ZipLow(left, right);

        /// <summary>
        /// svint32_t svzip1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ZipLow(Vector<int> left, Vector<int> right) => ZipLow(left, right);

        /// <summary>
        /// svint64_t svzip1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ZipLow(Vector<long> left, Vector<long> right) => ZipLow(left, right);

        /// <summary>
        /// svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svzip1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right) => ZipLow(left, right);

        /// <summary>
        /// svuint16_t svzip1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svzip1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ZipLow(Vector<ushort> left, Vector<ushort> right) => ZipLow(left, right);

        /// <summary>
        /// svuint32_t svzip1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svzip1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ZipLow(Vector<uint> left, Vector<uint> right) => ZipLow(left, right);

        /// <summary>
        /// svuint64_t svzip1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svzip1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ZipLow(Vector<ulong> left, Vector<ulong> right) => ZipLow(left, right);

        /// <summary>
        /// svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ZipLow(Vector<float> left, Vector<float> right) => ZipLow(left, right);

        /// <summary>
        /// svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ZipLow(Vector<double> left, Vector<double> right) => ZipLow(left, right);

    }
}


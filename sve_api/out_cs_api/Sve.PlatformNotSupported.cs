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
        public static unsafe Vector<sbyte> Abs(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svabs[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svabs[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> Abs(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svabs[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svabs[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Abs(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svabs[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svabs[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Abs(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svabs[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svabs[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svabs[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Abs(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svabs[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svabs[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svabs[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Abs(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svacgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svacge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svabd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  Add : Add

        /// <summary>
        /// svint8_t svadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Add(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Add(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Add(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Add(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Add(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Add(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Add(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Add(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Add(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Add(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  AddAcross : Add reduction

        /// <summary>
        /// int64_t svaddv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svaddv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svaddv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svaddv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svaddv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svaddv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svaddv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svaddv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svaddv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> AddAcross(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svaddv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> AddAcross(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svfloat32_t svcadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// svfloat32_t svcadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// svfloat32_t svcadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> AddRotateComplex(Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// svfloat64_t svcadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// svfloat64_t svcadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<double> AddRotateComplex(Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation) { throw new PlatformNotSupportedException(); }


        ///  AddSaturate : Saturating add

        /// <summary>
        /// svint8_t svqadd[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svqadd[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqadd[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqadd[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqadd[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqadd[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqadd[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float32_t svadda[_f32](svbool_t pg, float32_t initial, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> AddSequentialAcross(Vector<float> initial, Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svadda[_f64](svbool_t pg, float64_t initial, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> AddSequentialAcross(Vector<double> initial, Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  And : Bitwise AND

        /// <summary>
        /// svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> And(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> And(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> And(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> And(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> And(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> And(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> And(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  AndAcross : Bitwise AND reduction to scalar

        /// <summary>
        /// int8_t svandv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> AndAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svandv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> AndAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svandv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> AndAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svandv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> AndAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svandv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> AndAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svandv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> AndAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svandv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> AndAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svandv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> AndAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  AndNot : Bitwise NAND

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> AndNot(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> AndNot(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> AndNot(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> AndNot(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> AndNot(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> AndNot(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> AndNot(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> AndNot(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  BitwiseClear : Bitwise clear

        /// <summary>
        /// svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> BitwiseClear(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> BitwiseClear(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> BitwiseClear(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  BooleanNot : Logically invert boolean condition

        /// <summary>
        /// svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> BooleanNot(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> BooleanNot(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> BooleanNot(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> BooleanNot(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> BooleanNot(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> BooleanNot(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> BooleanNot(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> BooleanNot(Vector<ulong> value) { throw new PlatformNotSupportedException(); }



        ///  Compact : Shuffle active elements of vector to the right and fill with zero

        /// <summary>
        /// svint32_t svcompact[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Compact(Vector<int> mask, Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svcompact[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Compact(Vector<long> mask, Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> Compact(Vector<uint> mask, Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Compact(Vector<float> mask, Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Compact(Vector<double> mask, Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  CompareEqual : Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareEqual(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareEqual(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareEqual(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareEqual(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareEqual(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareEqual(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareGreaterThan : Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThan(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThan(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThanOrEqual(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareLessThan : Compare less than

        /// <summary>
        /// svbool_t svcmplt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareLessThan(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThan(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareLessThan(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareLessThan(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareLessThanOrEqual : Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareLessThanOrEqual(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareNotEqualTo : Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> CompareNotEqualTo(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CompareNotEqualTo(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CompareNotEqualTo(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CompareNotEqualTo(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> CompareUnordered(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> CompareUnordered(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  Compute16BitAddresses : Compute vector addresses for 16-bit data

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  Compute32BitAddresses : Compute vector addresses for 32-bit data

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  Compute64BitAddresses : Compute vector addresses for 64-bit data

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  Compute8BitAddresses : Compute vector addresses for 8-bit data

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// int8_t svclasta[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// int16_t svclasta[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// int32_t svclasta[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// int64_t svclasta[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// uint8_t svclasta[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// uint16_t svclasta[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// uint32_t svclasta[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// uint64_t svclasta[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// float32_t svclasta[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// float64_t svclasta[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractAfterLastActiveElementAndReplicate : Conditionally extract element after last

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// int8_t svclastb[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// int16_t svclastb[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// int32_t svclastb[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// int64_t svclastb[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// uint8_t svclastb[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// uint16_t svclastb[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// uint32_t svclastb[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// uint64_t svclastb[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// float32_t svclastb[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// float64_t svclastb[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalExtractLastActiveElementAndReplicate : Conditionally extract last element

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, Vector<short> fallback, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, Vector<int> fallback, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, Vector<long> fallback, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, Vector<float> fallback, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, Vector<double> fallback, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  ConvertToDouble : Floating-point convert

        /// <summary>
        /// svfloat64_t svcvt_f64[_s32]_m(svfloat64_t inactive, svbool_t pg, svint32_t op)
        /// svfloat64_t svcvt_f64[_s32]_x(svbool_t pg, svint32_t op)
        /// svfloat64_t svcvt_f64[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcvt_f64[_s64]_m(svfloat64_t inactive, svbool_t pg, svint64_t op)
        /// svfloat64_t svcvt_f64[_s64]_x(svbool_t pg, svint64_t op)
        /// svfloat64_t svcvt_f64[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcvt_f64[_u32]_m(svfloat64_t inactive, svbool_t pg, svuint32_t op)
        /// svfloat64_t svcvt_f64[_u32]_x(svbool_t pg, svuint32_t op)
        /// svfloat64_t svcvt_f64[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcvt_f64[_u64]_m(svfloat64_t inactive, svbool_t pg, svuint64_t op)
        /// svfloat64_t svcvt_f64[_u64]_x(svbool_t pg, svuint64_t op)
        /// svfloat64_t svcvt_f64[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcvt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat64_t svcvt_f64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat64_t svcvt_f64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<float> value) { throw new PlatformNotSupportedException(); }


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op)
        /// svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op)
        /// svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ConvertToSingle : Floating-point convert

        /// <summary>
        /// svfloat32_t svcvt_f32[_s32]_m(svfloat32_t inactive, svbool_t pg, svint32_t op)
        /// svfloat32_t svcvt_f32[_s32]_x(svbool_t pg, svint32_t op)
        /// svfloat32_t svcvt_f32[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svcvt_f32[_s64]_m(svfloat32_t inactive, svbool_t pg, svint64_t op)
        /// svfloat32_t svcvt_f32[_s64]_x(svbool_t pg, svint64_t op)
        /// svfloat32_t svcvt_f32[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svcvt_f32[_u32]_m(svfloat32_t inactive, svbool_t pg, svuint32_t op)
        /// svfloat32_t svcvt_f32[_u32]_x(svbool_t pg, svuint32_t op)
        /// svfloat32_t svcvt_f32[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svcvt_f32[_u64]_m(svfloat32_t inactive, svbool_t pg, svuint64_t op)
        /// svfloat32_t svcvt_f32[_u64]_x(svbool_t pg, svuint64_t op)
        /// svfloat32_t svcvt_f32[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svcvt_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvt_f32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat32_t svcvt_f32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  Count16BitElements : Count the number of 16-bit elements in a vector

        /// <summary>
        /// uint64_t svcnth_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  Count32BitElements : Count the number of 32-bit elements in a vector

        /// <summary>
        /// uint64_t svcntw_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  Count64BitElements : Count the number of 64-bit elements in a vector

        /// <summary>
        /// uint64_t svcntd_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  Count8BitElements : Count the number of 8-bit elements in a vector

        /// <summary>
        /// uint64_t svcntb_pat(enum svpattern pattern)
        /// </summary>
        public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }



        ///  CreateBreakAfterMask : Break after first true condition

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterMask(Vector<short> totalMask, Vector<short> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterMask(Vector<int> totalMask, Vector<int> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterMask(Vector<long> totalMask, Vector<long> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterMask(Vector<byte> totalMask, Vector<byte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterMask(Vector<ushort> totalMask, Vector<ushort> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterMask(Vector<uint> totalMask, Vector<uint> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterMask(Vector<ulong> totalMask, Vector<ulong> fromMask) { throw new PlatformNotSupportedException(); }


        ///  CreateBreakAfterPropagateMask : Break after first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterPropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterPropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterPropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterPropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterPropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterPropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  CreateBreakBeforeMask : Break before first true condition

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforeMask(Vector<short> totalMask, Vector<short> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforeMask(Vector<int> totalMask, Vector<int> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforeMask(Vector<long> totalMask, Vector<long> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforeMask(Vector<byte> totalMask, Vector<byte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforeMask(Vector<ushort> totalMask, Vector<ushort> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforeMask(Vector<uint> totalMask, Vector<uint> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforeMask(Vector<ulong> totalMask, Vector<ulong> fromMask) { throw new PlatformNotSupportedException(); }


        ///  CreateBreakBeforePropagateMask : Break before first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforePropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforePropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforePropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforePropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforePropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforePropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  CreateBreakPropagateMask : Propagate break to next partition

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakPropagateMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> CreateBreakPropagateMask(Vector<short> totalMask, Vector<short> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> CreateBreakPropagateMask(Vector<int> totalMask, Vector<int> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> CreateBreakPropagateMask(Vector<long> totalMask, Vector<long> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateBreakPropagateMask(Vector<byte> totalMask, Vector<byte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakPropagateMask(Vector<ushort> totalMask, Vector<ushort> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateBreakPropagateMask(Vector<uint> totalMask, Vector<uint> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakPropagateMask(Vector<ulong> totalMask, Vector<ulong> fromMask) { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskByte : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<byte> CreateFalseMaskByte() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskDouble : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<double> CreateFalseMaskDouble() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskInt16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<short> CreateFalseMaskInt16() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskInt32 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<int> CreateFalseMaskInt32() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskInt64 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<long> CreateFalseMaskInt64() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskSByte : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<sbyte> CreateFalseMaskSByte() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskSingle : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<float> CreateFalseMaskSingle() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskUInt16 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<ushort> CreateFalseMaskUInt16() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskUInt32 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<uint> CreateFalseMaskUInt32() { throw new PlatformNotSupportedException(); }


        ///  CreateFalseMaskUInt64 : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        /// </summary>
        public static unsafe Vector<ulong> CreateFalseMaskUInt64() { throw new PlatformNotSupportedException(); }


        ///  CreateMaskForFirstActiveElement : Set the first active predicate element to true

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> CreateMaskForFirstActiveElement(Vector<sbyte> totalMask, Vector<sbyte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> CreateMaskForFirstActiveElement(Vector<short> totalMask, Vector<short> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> CreateMaskForFirstActiveElement(Vector<int> totalMask, Vector<int> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> CreateMaskForFirstActiveElement(Vector<long> totalMask, Vector<long> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForFirstActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForFirstActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForFirstActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForFirstActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) { throw new PlatformNotSupportedException(); }


        ///  CreateMaskForNextActiveElement : Find next active predicate

        /// <summary>
        /// svbool_t svpnext_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForNextActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpnext_b16(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForNextActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpnext_b32(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForNextActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svpnext_b64(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForNextActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) { throw new PlatformNotSupportedException(); }



        ///  CreateTrueMaskByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskDouble : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskSByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskSingle : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b16(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b32(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b64(enum svpattern pattern)
        /// </summary>
        public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanMask16Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanMask32Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanMask64Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanMask8Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanOrEqualMask16Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanOrEqualMask32Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanOrEqualMask64Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  CreateWhileLessThanOrEqualMask8Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right) { throw new PlatformNotSupportedException(); }


        ///  Divide : Divide

        /// <summary>
        /// svint32_t svdiv[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svdiv[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svdiv[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Divide(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svdiv[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svdiv[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svdiv[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Divide(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svdiv[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svdiv[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svdiv[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Divide(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svdiv[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svdiv[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svdiv[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Divide(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Divide(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Divide(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }



        ///  DotProduct : Dot product

        /// <summary>
        /// svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<long> DotProduct(Vector<long> addend, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }


        ///  DotProductBySelectedScalar : Dot product

        /// <summary>
        /// svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<int> DotProductBySelectedScalar(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<long> DotProductBySelectedScalar(Vector<long> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<uint> DotProductBySelectedScalar(Vector<uint> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<ulong> DotProductBySelectedScalar(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svint8_t svdup_lane[_s8](svint8_t data, uint8_t index)
        /// svint8_t svdupq_lane[_s8](svint8_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svdup_lane[_s16](svint16_t data, uint16_t index)
        /// svint16_t svdupq_lane[_s16](svint16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svdup_lane[_s32](svint32_t data, uint32_t index)
        /// svint32_t svdupq_lane[_s32](svint32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svdup_lane[_s64](svint64_t data, uint64_t index)
        /// svint64_t svdupq_lane[_s64](svint64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index)
        /// svuint8_t svdupq_lane[_u8](svuint8_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index)
        /// svuint16_t svdupq_lane[_u16](svuint16_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index)
        /// svuint32_t svdupq_lane[_u32](svuint32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index)
        /// svuint64_t svdupq_lane[_u64](svuint64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index)
        /// svfloat32_t svdupq_lane[_f32](svfloat32_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index)
        /// svfloat64_t svdupq_lane[_f64](svfloat64_t data, uint64_t index)
        /// </summary>
        public static unsafe Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }


        ///  ExtractAfterLastScalar : Extract element after last

        /// <summary>
        /// int8_t svlasta[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe sbyte ExtractAfterLastScalar(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svlasta[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe short ExtractAfterLastScalar(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svlasta[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe int ExtractAfterLastScalar(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svlasta[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe long ExtractAfterLastScalar(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe byte ExtractAfterLastScalar(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe ushort ExtractAfterLastScalar(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe uint ExtractAfterLastScalar(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe ulong ExtractAfterLastScalar(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe float ExtractAfterLastScalar(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe double ExtractAfterLastScalar(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractAfterLastVector : Extract element after last

        /// <summary>
        /// int8_t svlasta[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractAfterLastVector(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svlasta[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ExtractAfterLastVector(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svlasta[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ExtractAfterLastVector(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svlasta[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ExtractAfterLastVector(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ExtractAfterLastVector(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ExtractAfterLastVector(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ExtractAfterLastVector(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ExtractAfterLastVector(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ExtractAfterLastVector(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ExtractAfterLastVector(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractLastScalar : Extract last element

        /// <summary>
        /// int8_t svlastb[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe sbyte ExtractLastScalar(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svlastb[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe short ExtractLastScalar(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svlastb[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe int ExtractLastScalar(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svlastb[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe long ExtractLastScalar(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe byte ExtractLastScalar(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe ushort ExtractLastScalar(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe uint ExtractLastScalar(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe ulong ExtractLastScalar(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe float ExtractLastScalar(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe double ExtractLastScalar(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractLastVector : Extract last element

        /// <summary>
        /// int8_t svlastb[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractLastVector(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svlastb[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ExtractLastVector(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svlastb[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ExtractLastVector(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svlastb[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ExtractLastVector(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ExtractLastVector(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ExtractLastVector(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ExtractLastVector(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ExtractLastVector(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ExtractLastVector(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ExtractLastVector(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, [ConstantExpected] byte index) { throw new PlatformNotSupportedException(); }


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat32_t svexpa[_f32](svuint32_t op)
        /// </summary>
        public static unsafe Vector<float> FloatingPointExponentialAccelerator(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svexpa[_f64](svuint64_t op)
        /// </summary>
        public static unsafe Vector<double> FloatingPointExponentialAccelerator(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddBySelectedScalar(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplyAddNegated : Negated multiply-add, addend first

        /// <summary>
        /// svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddNegated(Vector<float> addend, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddNegated(Vector<double> addend, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractBySelectedScalar(Vector<float> minuend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractBySelectedScalar(Vector<double> minuend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }


        ///  FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractNegated(Vector<float> minuend, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractNegated(Vector<double> minuend, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  GatherPrefetch16Bit : Prefetch halfwords

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  GatherPrefetch32Bit : Prefetch words

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  GatherPrefetch64Bit : Prefetch doublewords

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  GatherPrefetch8Bit : Prefetch bytes

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  GatherVector : Unextended load

        /// <summary>
        /// svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorByteZeroExtend : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint32_t svldff1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt16SignExtend : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt16WithByteOffsetsSignExtend : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt32SignExtend : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtend(Vector<int> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtend(Vector<uint> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32SignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32SignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt32WithByteOffsetsSignExtend : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtend(Vector<int> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtend(Vector<uint> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorSByteSignExtend : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt16WithByteOffsetsZeroExtend : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt16ZeroExtend : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt32WithByteOffsetsZeroExtend : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt32ZeroExtend : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorWithByteOffsetFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint32_t svldff1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GatherVectorWithByteOffsets : Unextended load

        /// <summary>
        /// svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<int> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<uint> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<long> offsets) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<ulong> offsets) { throw new PlatformNotSupportedException(); }


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<sbyte> mask, Vector<sbyte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<short> mask, Vector<short> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<int> mask, Vector<int> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<long> mask, Vector<long> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b16(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ushort> mask, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b32(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<uint> mask, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b64(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ulong> mask, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<float> mask, Vector<float> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<double> mask, Vector<double> from) { throw new PlatformNotSupportedException(); }


        ///  GetFfr : Read FFR, returning predicate of succesfully loaded elements

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<sbyte> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<byte> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> GetFfr() { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svrdffr()
        /// svbool_t svrdffr_z(svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> GetFfr() { throw new PlatformNotSupportedException(); }


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svinsr[_n_s16](svint16_t op1, int16_t op2)
        /// </summary>
        public static unsafe Vector<short> InsertIntoShiftedVector(Vector<short> left, short right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svinsr[_n_s32](svint32_t op1, int32_t op2)
        /// </summary>
        public static unsafe Vector<int> InsertIntoShiftedVector(Vector<int> left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svinsr[_n_s64](svint64_t op1, int64_t op2)
        /// </summary>
        public static unsafe Vector<long> InsertIntoShiftedVector(Vector<long> left, long right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svinsr[_n_u16](svuint16_t op1, uint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InsertIntoShiftedVector(Vector<ushort> left, ushort right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svinsr[_n_u32](svuint32_t op1, uint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InsertIntoShiftedVector(Vector<uint> left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svinsr[_n_u64](svuint64_t op1, uint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InsertIntoShiftedVector(Vector<ulong> left, ulong right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2)
        /// </summary>
        public static unsafe Vector<float> InsertIntoShiftedVector(Vector<float> left, float right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2)
        /// </summary>
        public static unsafe Vector<double> InsertIntoShiftedVector(Vector<double> left, double right) { throw new PlatformNotSupportedException(); }


        ///  LeadingSignCount : Count leading sign bits

        /// <summary>
        /// svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingSignCount(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingSignCount(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingSignCount(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingSignCount(Vector<long> value) { throw new PlatformNotSupportedException(); }


        ///  LeadingZeroCount : Count leading zero bits

        /// <summary>
        /// svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  LoadVector : Unextended load

        /// <summary>
        /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToUInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToUInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteNonFaultingZeroExtendToUInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svldff1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint8_t svldff1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svldff1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svldff1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svldff1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldff1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldff1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16NonFaultingSignExtendToInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16NonFaultingSignExtendToInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16NonFaultingSignExtendToUInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16NonFaultingSignExtendToUInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32NonFaultingSignExtendToInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32NonFaultingSignExtendToUInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToUInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToUInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteNonFaultingSignExtendToUInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendFirstFaulting(Vector<short> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svldff1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svldff1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendFirstFaulting(Vector<ushort> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16NonFaultingZeroExtendToInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16NonFaultingZeroExtendToInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16NonFaultingZeroExtendToUInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16NonFaultingZeroExtendToUInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svldff1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svldff1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32NonFaultingZeroExtendToInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32NonFaultingZeroExtendToUInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svldff1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>) LoadVectorx2(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>) LoadVectorx2(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>) LoadVectorx2(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>) LoadVectorx2(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>) LoadVectorx2(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>) LoadVectorx2(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>) LoadVectorx2(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>) LoadVectorx2(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>) LoadVectorx2(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>) LoadVectorx2(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx3(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>) LoadVectorx3(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>) LoadVectorx3(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>) LoadVectorx3(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx3(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx3(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx3(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx3(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>) LoadVectorx3(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>) LoadVectorx3(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx4(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) LoadVectorx4(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) LoadVectorx4(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) LoadVectorx4(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx4(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx4(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx4(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx4(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) LoadVectorx4(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) LoadVectorx4(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  Max : Maximum

        /// <summary>
        /// svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Max(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Max(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Max(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Max(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Max(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Max(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Max(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// int8_t svmaxv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> MaxAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svmaxv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> MaxAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svmaxv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> MaxAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svmaxv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> MaxAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> MaxAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> MaxAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> MaxAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> MaxAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MaxAcross(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MaxAcross(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MaxNumber(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MaxNumber(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MaxNumberAcross(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MaxNumberAcross(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  Min : Minimum

        /// <summary>
        /// svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Min(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Min(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Min(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Min(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Min(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Min(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Min(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// int8_t svminv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> MinAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svminv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> MinAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svminv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> MinAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svminv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> MinAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svminv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> MinAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svminv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> MinAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svminv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> MinAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svminv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> MinAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32_t svminv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MinAcross(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svminv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MinAcross(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MinNumber(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MinNumber(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> MinNumberAcross(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> MinNumberAcross(Vector<double> value) { throw new PlatformNotSupportedException(); }



        ///  Multiply : Multiply

        /// <summary>
        /// svint8_t svmul[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmul[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmul[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Multiply(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svmul[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmul[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmul[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Multiply(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svmul[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmul[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmul[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Multiply(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svmul[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmul[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmul[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Multiply(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svmul[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmul[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Multiply(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svmul[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmul[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmul[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Multiply(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmul[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmul[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmul[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Multiply(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svmul[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmul[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmul[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Multiply(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svmul[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmul[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmul[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Multiply(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmul[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmul[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Multiply(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  MultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }




        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svfloat32_t svcmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// svfloat32_t svcmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// svfloat32_t svcmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplex(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rotation) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svcmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// svfloat64_t svcmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// svfloat64_t svcmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<double> MultiplyAddRotateComplex(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rotation) { throw new PlatformNotSupportedException(); }


        ///  MultiplyAddRotateComplexBySelectedScalar : Complex multiply-add with rotate

        /// <summary>
        /// svfloat32_t svcmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex, [ConstantExpected] byte rotation) { throw new PlatformNotSupportedException(); }


        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<float> MultiplyBySelectedScalar(Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index)
        /// </summary>
        public static unsafe Vector<double> MultiplyBySelectedScalar(Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) { throw new PlatformNotSupportedException(); }


        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }



        ///  MultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// </summary>
        public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// </summary>
        public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// </summary>
        public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// </summary>
        public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// </summary>
        public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }




        ///  Negate : Negate

        /// <summary>
        /// svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> Negate(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> Negate(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> Negate(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> Negate(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Negate(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Negate(Vector<double> value) { throw new PlatformNotSupportedException(); }




        ///  Not : Bitwise invert

        /// <summary>
        /// svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<sbyte> Not(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<short> Not(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<int> Not(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<long> Not(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> Not(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> Not(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> Not(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> Not(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  Or : Bitwise inclusive OR

        /// <summary>
        /// svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> Or(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> Or(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> Or(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> Or(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> Or(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  OrAcross : Bitwise inclusive OR reduction to scalar

        /// <summary>
        /// int8_t svorv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> OrAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t svorv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> OrAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svorv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> OrAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svorv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> OrAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t svorv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> OrAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t svorv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> OrAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svorv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> OrAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svorv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> OrAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  OrNot : Bitwise NOR

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> OrNot(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> OrNot(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> OrNot(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> OrNot(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> OrNot(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> OrNot(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> OrNot(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> OrNot(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  PrefetchBytes : Prefetch bytes

        /// <summary>
        /// void svprfb(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  PrefetchInt16 : Prefetch halfwords

        /// <summary>
        /// void svprfh(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  PrefetchInt32 : Prefetch words

        /// <summary>
        /// void svprfw(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  PrefetchInt64 : Prefetch doublewords

        /// <summary>
        /// void svprfd(svbool_t pg, const void *base, enum svprfop op)
        /// </summary>
        public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) { throw new PlatformNotSupportedException(); }


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat32_t svrecpe[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalEstimate(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrecpe[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalEstimate(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalExponent(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalExponent(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat32_t svrsqrte[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtEstimate(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrsqrte[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtEstimate(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  ReverseBits : Reverse bits

        /// <summary>
        /// svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svrbit[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svrbit[_s8]_z(svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ReverseBits(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svrbit[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svrbit[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseBits(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrbit[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrbit[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseBits(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrbit[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrbit[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseBits(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svrbit[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svrbit[_u8]_z(svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> ReverseBits(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svrbit[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svrbit[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseBits(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrbit[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrbit[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseBits(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrbit[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrbit[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseBits(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svint8_t svrev[_s8](svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> ReverseElement(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svrev[_s16](svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseElement(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svrev[_s32](svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svrev[_s64](svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svrev[_u8](svuint8_t op)
        /// svbool_t svrev_b8(svbool_t op)
        /// </summary>
        public static unsafe Vector<byte> ReverseElement(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svrev[_u16](svuint16_t op)
        /// svbool_t svrev_b16(svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svrev[_u32](svuint32_t op)
        /// svbool_t svrev_b32(svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrev[_u64](svuint64_t op)
        /// svbool_t svrev_b64(svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement(Vector<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svrev[_f32](svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReverseElement(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrev[_f64](svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReverseElement(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  ReverseElement16 : Reverse halfwords within elements

        /// <summary>
        /// svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrevh[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrevh[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement16(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevh[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevh[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement16(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrevh[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrevh[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement16(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevh[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevh[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement16(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ReverseElement32 : Reverse words within elements

        /// <summary>
        /// svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevw[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevw[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement32(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevw[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevw[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement32(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ReverseElement8 : Reverse bytes within elements

        /// <summary>
        /// svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svrevb[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svrevb[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> ReverseElement8(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svrevb[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svrevb[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> ReverseElement8(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svrevb[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svrevb[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> ReverseElement8(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svrevb[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svrevb[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement8(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svrevb[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svrevb[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ReverseElement8(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svrevb[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svrevb[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement8(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundAwayFromZero(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundAwayFromZero(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNearest(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNearest(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNegativeInfinity(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNegativeInfinity(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToPositiveInfinity(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToPositiveInfinity(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToZero(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToZero(Vector<double> value) { throw new PlatformNotSupportedException(); }




        ///  SaturatingDecrementBy16BitElementCount : Saturating decrement by number of halfword elements

        /// <summary>
        /// int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingDecrementBy32BitElementCount : Saturating decrement by number of word elements

        /// <summary>
        /// int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingDecrementBy64BitElementCount : Saturating decrement by number of doubleword elements

        /// <summary>
        /// int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingDecrementBy8BitElementCount : Saturating decrement by number of byte elements

        /// <summary>
        /// int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingDecrementByActiveElementCount : Saturating decrement by active element count

        /// <summary>
        /// svint16_t svqdecp[_s16](svint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> value, Vector<short> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqdecp[_s32](svint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> value, Vector<int> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqdecp[_s64](svint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> value, Vector<long> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }


        ///  SaturatingIncrementBy16BitElementCount : Saturating increment by number of halfword elements

        /// <summary>
        /// int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingIncrementBy32BitElementCount : Saturating increment by number of word elements

        /// <summary>
        /// int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingIncrementBy64BitElementCount : Saturating increment by number of doubleword elements

        /// <summary>
        /// int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingIncrementBy8BitElementCount : Saturating increment by number of byte elements

        /// <summary>
        /// int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  SaturatingIncrementByActiveElementCount : Saturating increment by active element count

        /// <summary>
        /// svint16_t svqincp[_s16](svint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> value, Vector<short> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqincp[_s32](svint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> value, Vector<int> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqincp[_s64](svint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> value, Vector<long> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<byte> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> value, Vector<uint> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg)
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg)
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg)
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg)
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) { throw new PlatformNotSupportedException(); }


        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<float> Scale(Vector<float> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<double> Scale(Vector<double> left, Vector<long> right) { throw new PlatformNotSupportedException(); }


        ///  Scatter : Non-truncating store

        /// <summary>
        /// void svst1_scatter_[s32]offset[_s32](svbool_t pg, int32_t *base, svint32_t offsets, svint32_t data)
        /// void svst1_scatter_[s32]index[_s32](svbool_t pg, int32_t *base, svint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<int> indicies, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, Vector<uint> addresses, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data)
        /// void svst1_scatter_[u32]index[_s32](svbool_t pg, int32_t *base, svuint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<uint> indicies, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data)
        /// void svst1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<long> indicies, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data)
        /// void svst1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<ulong> indicies, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[s32]offset[_u32](svbool_t pg, uint32_t *base, svint32_t offsets, svuint32_t data)
        /// void svst1_scatter_[s32]index[_u32](svbool_t pg, uint32_t *base, svint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<int> indicies, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data)
        /// void svst1_scatter_[u32]index[_u32](svbool_t pg, uint32_t *base, svuint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<uint> indicies, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data)
        /// void svst1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<long> indicies, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data)
        /// void svst1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<ulong> indicies, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[s32]offset[_f32](svbool_t pg, float32_t *base, svint32_t offsets, svfloat32_t data)
        /// void svst1_scatter_[s32]index[_f32](svbool_t pg, float32_t *base, svint32_t indices, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<int> indicies, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, Vector<uint> addresses, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data)
        /// void svst1_scatter_[u32]index[_f32](svbool_t pg, float32_t *base, svuint32_t indices, svfloat32_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<uint> indicies, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data)
        /// void svst1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<long> indicies, Vector<double> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, Vector<ulong> addresses, Vector<double> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data)
        /// void svst1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data)
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<ulong> indicies, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter16BitNarrowing : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter16BitWithByteOffsetsNarrowing : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_s32](svbool_t pg, int16_t *base, svint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> offsets, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s32]index[_s32](svbool_t pg, int16_t *base, svint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> indices, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u32]index[_s32](svbool_t pg, int16_t *base, svuint32_t indices, svint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> indices, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> indices, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> indices, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_u32](svbool_t pg, uint16_t *base, svint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> offsets, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s32]index[_u32](svbool_t pg, uint16_t *base, svint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> indices, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u32]index[_u32](svbool_t pg, uint16_t *base, svuint32_t indices, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> indices, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> indices, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> indices, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter32BitNarrowing : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter32BitWithByteOffsetsNarrowing : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> indices, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> indices, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> indices, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> indices, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter8BitNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  Scatter8BitWithByteOffsetsNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_s32](svbool_t pg, int8_t *base, svint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<int> offsets, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_u32](svbool_t pg, uint8_t *base, svint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<int> offsets, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data)
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  SetFfr : Write to the first-fault register

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svwrffr(svbool_t op)
        /// </summary>
        public static unsafe void SetFfr(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ShiftLeftLogical : Logical shift left

        /// <summary>
        /// svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsl_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  ShiftRightArithmetic : Arithmetic shift right

        /// <summary>
        /// svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  ShiftRightArithmeticForDivide : Arithmetic shift right for divide by immediate

        /// <summary>
        /// svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticForDivide(Vector<sbyte> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticForDivide(Vector<short> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticForDivide(Vector<int> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticForDivide(Vector<long> value, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }


        ///  ShiftRightLogical : Logical shift right

        /// <summary>
        /// svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogical(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  SignExtend16 : Sign-extend the low 16 bits

        /// <summary>
        /// svint32_t svexth[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svexth[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svexth[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtend16(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svexth[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svexth[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svexth[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend16(Vector<long> value) { throw new PlatformNotSupportedException(); }


        ///  SignExtend32 : Sign-extend the low 32 bits

        /// <summary>
        /// svint64_t svextw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svextw[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svextw[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend32(Vector<long> value) { throw new PlatformNotSupportedException(); }


        ///  SignExtend8 : Sign-extend the low 8 bits

        /// <summary>
        /// svint16_t svextb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svextb[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svextb[_s16]_z(svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtend8(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svextb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svextb[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svextb[_s32]_z(svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtend8(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svextb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svextb[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svextb[_s64]_z(svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtend8(Vector<long> value) { throw new PlatformNotSupportedException(); }


        ///  SignExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svint16_t svunpklo[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningLower(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svunpklo[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningLower(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svunpklo[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningLower(Vector<int> value) { throw new PlatformNotSupportedException(); }


        ///  SignExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svint16_t svunpkhi[_s16](svint8_t op)
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningUpper(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svunpkhi[_s32](svint16_t op)
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningUpper(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svunpkhi[_s64](svint32_t op)
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningUpper(Vector<int> value) { throw new PlatformNotSupportedException(); }


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Sqrt(Vector<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Sqrt(Vector<double> value) { throw new PlatformNotSupportedException(); }


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3, Vector<sbyte> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3, Vector<short> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3, Vector<int> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3, Vector<long> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3, Vector<byte> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3, Vector<ushort> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3, Vector<uint> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3, Vector<ulong> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3, Vector<float> Value4) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, Vector<double> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3) data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data)
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3, Vector<double> Value4) data) { throw new PlatformNotSupportedException(); }


        ///  StoreNarrowing : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* address, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* address, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, short* address, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* address, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, short* address, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* address, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, byte* address, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* address, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data) { throw new PlatformNotSupportedException(); }


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<short> mask, short* address, Vector<short> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<int> mask, int* address, Vector<int> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<long> mask, long* address, Vector<long> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* address, Vector<byte> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* address, Vector<ushort> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* address, Vector<uint> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<float> mask, float* address, Vector<float> data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<double> mask, double* address, Vector<double> data) { throw new PlatformNotSupportedException(); }


        ///  Subtract : Subtract

        /// <summary>
        /// svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> Subtract(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> Subtract(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> Subtract(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> Subtract(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> Subtract(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> Subtract(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> Subtract(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }



        ///  SubtractSaturate : Saturating subtract

        /// <summary>
        /// svint8_t svqsub[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svqsub[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svqsub[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svqsub[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svqsub[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svqsub[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svqsub[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  TestAnyTrue : Test whether any active element is true

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<short> leftMask, Vector<short> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<int> leftMask, Vector<int> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<long> leftMask, Vector<long> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<byte> leftMask, Vector<byte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<uint> leftMask, Vector<uint> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) { throw new PlatformNotSupportedException(); }


        ///  TestFirstTrue : Test whether the first active element is true

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<short> leftMask, Vector<short> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<int> leftMask, Vector<int> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<long> leftMask, Vector<long> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<byte> leftMask, Vector<byte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<uint> leftMask, Vector<uint> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) { throw new PlatformNotSupportedException(); }


        ///  TestLastTrue : Test whether the last active element is true

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<short> leftMask, Vector<short> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<int> leftMask, Vector<int> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<long> leftMask, Vector<long> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<byte> leftMask, Vector<byte> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<uint> leftMask, Vector<uint> rightMask) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) { throw new PlatformNotSupportedException(); }


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svtrn1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> TransposeEven(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svtrn1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> TransposeEven(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svtrn1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> TransposeEven(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svtrn1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svtrn1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svtrn1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svtrn1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> TransposeEven(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> TransposeEven(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svtrn2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> TransposeOdd(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svtrn2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> TransposeOdd(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svtrn2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> TransposeOdd(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svtrn2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svtrn2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svtrn2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svtrn2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> TransposeOdd(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> TransposeOdd(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat32_t svtmad[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<float> TrigonometricMultiplyAddCoefficient(Vector<float> left, Vector<float> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtmad[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        /// </summary>
        public static unsafe Vector<double> TrigonometricMultiplyAddCoefficient(Vector<double> left, Vector<double> right, [ConstantExpected] byte control) { throw new PlatformNotSupportedException(); }


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat32_t svtssel[_f32](svfloat32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<float> TrigonometricSelectCoefficient(Vector<float> value, Vector<uint> selector) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtssel[_f64](svfloat64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<double> TrigonometricSelectCoefficient(Vector<double> value, Vector<ulong> selector) { throw new PlatformNotSupportedException(); }


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat32_t svtsmul[_f32](svfloat32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<float> TrigonometricStartingValue(Vector<float> value, Vector<uint> sign) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtsmul[_f64](svfloat64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<double> TrigonometricStartingValue(Vector<double> value, Vector<ulong> sign) { throw new PlatformNotSupportedException(); }


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svuzp1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> UnzipEven(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svuzp1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> UnzipEven(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svuzp1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> UnzipEven(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svuzp1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svuzp1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svuzp1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> UnzipEven(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svuzp1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svuzp1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> UnzipEven(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svuzp1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svuzp1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> UnzipEven(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> UnzipEven(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> UnzipEven(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svuzp2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> UnzipOdd(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svuzp2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> UnzipOdd(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svuzp2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> UnzipOdd(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svuzp2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svuzp2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svuzp2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> UnzipOdd(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svuzp2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svuzp2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> UnzipOdd(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svuzp2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svuzp2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> UnzipOdd(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> UnzipOdd(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> UnzipOdd(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svint8_t svtbl[_s8](svint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svtbl[_s16](svint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svtbl[_s32](svint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svtbl[_s64](svint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices)
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices)
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices)
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices)
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices) { throw new PlatformNotSupportedException(); }


        ///  Xor : Bitwise exclusive OR

        /// <summary>
        /// svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<short> Xor(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<int> Xor(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<long> Xor(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> Xor(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> Xor(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  XorAcross : Bitwise exclusive OR reduction to scalar

        /// <summary>
        /// int8_t sveorv[_s8](svbool_t pg, svint8_t op)
        /// </summary>
        public static unsafe Vector<sbyte> XorAcross(Vector<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16_t sveorv[_s16](svbool_t pg, svint16_t op)
        /// </summary>
        public static unsafe Vector<short> XorAcross(Vector<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32_t sveorv[_s32](svbool_t pg, svint32_t op)
        /// </summary>
        public static unsafe Vector<int> XorAcross(Vector<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64_t sveorv[_s64](svbool_t pg, svint64_t op)
        /// </summary>
        public static unsafe Vector<long> XorAcross(Vector<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8_t sveorv[_u8](svbool_t pg, svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> XorAcross(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16_t sveorv[_u16](svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> XorAcross(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t sveorv[_u32](svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> XorAcross(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64_t sveorv[_u64](svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> XorAcross(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ZeroExtend16 : Zero-extend the low 16 bits

        /// <summary>
        /// svuint32_t svexth[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svexth[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svexth[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend16(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svexth[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svexth[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svexth[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend16(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ZeroExtend32 : Zero-extend the low 32 bits

        /// <summary>
        /// svuint64_t svextw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svextw[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svextw[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ZeroExtend8 : Zero-extend the low 8 bits

        /// <summary>
        /// svuint16_t svextb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svextb[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svextb[_u16]_z(svbool_t pg, svuint16_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtend8(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svextb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svextb[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svextb[_u32]_z(svbool_t pg, svuint32_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend8(Vector<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svextb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svextb[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svextb[_u64]_z(svbool_t pg, svuint64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend8(Vector<ulong> value) { throw new PlatformNotSupportedException(); }


        ///  ZeroExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svuint16_t svunpklo[_u16](svuint8_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningLower(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svunpklo[_u32](svuint16_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningLower(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svunpklo[_u64](svuint32_t op)
        /// svbool_t svunpklo[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningLower(Vector<uint> value) { throw new PlatformNotSupportedException(); }


        ///  ZeroExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svuint16_t svunpkhi[_u16](svuint8_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningUpper(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svunpkhi[_u32](svuint16_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningUpper(Vector<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svunpkhi[_u64](svuint32_t op)
        /// svbool_t svunpkhi[_b](svbool_t op)
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value) { throw new PlatformNotSupportedException(); }


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svzip2[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ZipHigh(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svzip2[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ZipHigh(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svzip2[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ZipHigh(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svzip2_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svzip2[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svzip2_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ZipHigh(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svzip2[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svzip2_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ZipHigh(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svzip2[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svzip2_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ZipHigh(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ZipHigh(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ZipHigh(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svzip1[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ZipLow(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svzip1[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ZipLow(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svzip1[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ZipLow(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2)
        /// svbool_t svzip1_b8(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svzip1[_u16](svuint16_t op1, svuint16_t op2)
        /// svbool_t svzip1_b16(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ZipLow(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svzip1[_u32](svuint32_t op1, svuint32_t op2)
        /// svbool_t svzip1_b32(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<uint> ZipLow(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svzip1[_u64](svuint64_t op1, svuint64_t op2)
        /// svbool_t svzip1_b64(svbool_t op1, svbool_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ZipLow(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ZipLow(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ZipLow(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }

    }
}


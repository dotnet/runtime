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
    [System.Runtime.Versioning.RequiresPreviewFeaturesAttribute("Sve is in preview. Debugger scenario is not supported.")]
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
        ///   ABS Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; ABS Zresult.B, Pg/M, Zop.B
        /// svint8_t svabs[_s8]_x(svbool_t pg, svint8_t op)
        ///   ABS Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; ABS Zresult.B, Pg/M, Zop.B
        /// svint8_t svabs[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; ABS Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> Abs(Vector<sbyte> value) => Abs(value);

        /// <summary>
        /// svint16_t svabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   ABS Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; ABS Zresult.H, Pg/M, Zop.H
        /// svint16_t svabs[_s16]_x(svbool_t pg, svint16_t op)
        ///   ABS Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; ABS Zresult.H, Pg/M, Zop.H
        /// svint16_t svabs[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; ABS Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> Abs(Vector<short> value) => Abs(value);

        /// <summary>
        /// svint32_t svabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   ABS Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; ABS Zresult.S, Pg/M, Zop.S
        /// svint32_t svabs[_s32]_x(svbool_t pg, svint32_t op)
        ///   ABS Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; ABS Zresult.S, Pg/M, Zop.S
        /// svint32_t svabs[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; ABS Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> Abs(Vector<int> value) => Abs(value);

        /// <summary>
        /// svint64_t svabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   ABS Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; ABS Zresult.D, Pg/M, Zop.D
        /// svint64_t svabs[_s64]_x(svbool_t pg, svint64_t op)
        ///   ABS Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; ABS Zresult.D, Pg/M, Zop.D
        /// svint64_t svabs[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; ABS Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> Abs(Vector<long> value) => Abs(value);

        /// <summary>
        /// svfloat32_t svabs[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FABS Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FABS Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svabs[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FABS Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FABS Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svabs[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FABS Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> Abs(Vector<float> value) => Abs(value);

        /// <summary>
        /// svfloat64_t svabs[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FABS Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FABS Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svabs[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FABS Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FABS Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svabs[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FABS Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> Abs(Vector<double> value) => Abs(value);


        ///  Absolute compare greater than

        /// <summary>
        /// svbool_t svacgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACGT Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svacgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACGT Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThan(left, right);

        ///  Absolute compare greater than or equal to

        /// <summary>
        /// svbool_t svacge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACGE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svacge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACGE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        ///  Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACLT Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        /// svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACLT Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThan(left, right);

        ///  Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACLE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACLE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThanOrEqual(left, right);

        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right) => AbsoluteDifference(left, right);

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
        /// float64_t svaddv[_f64](svbool_t pg, svfloat64_t op)
        ///   FADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> AddAcross(Vector<double> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s16](svbool_t pg, svint16_t op)
        ///   SADDV Dresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<short> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s32](svbool_t pg, svint32_t op)
        ///   SADDV Dresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<int> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s8](svbool_t pg, svint8_t op)
        ///   SADDV Dresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<sbyte> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s64](svbool_t pg, svint64_t op)
        ///   UADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> AddAcross(Vector<long> value) => AddAcross(value);

        /// <summary>
        /// float32_t svaddv[_f32](svbool_t pg, svfloat32_t op)
        ///   FADDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> AddAcross(Vector<float> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u8](svbool_t pg, svuint8_t op)
        ///   UADDV Dresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<byte> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u16](svbool_t pg, svuint16_t op)
        ///   UADDV Dresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ushort> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u32](svbool_t pg, svuint32_t op)
        ///   UADDV Dresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<uint> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u64](svbool_t pg, svuint64_t op)
        ///   UADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> AddAcross(Vector<ulong> value) => AddAcross(value);

        ///  AddSaturate : Saturating add

        /// <summary>
        /// svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2)
        ///   UQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// svint16_t svqadd[_s16](svint16_t op1, svint16_t op2)
        ///   SQADD Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> AddSaturate(Vector<short> left, Vector<short> right) => AddSaturate(left, right);

        /// <summary>
        /// svint32_t svqadd[_s32](svint32_t op1, svint32_t op2)
        ///   SQADD Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> AddSaturate(Vector<int> left, Vector<int> right) => AddSaturate(left, right);

        /// <summary>
        /// svint64_t svqadd[_s64](svint64_t op1, svint64_t op2)
        ///   SQADD Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> AddSaturate(Vector<long> left, Vector<long> right) => AddSaturate(left, right);

        /// <summary>
        /// svint8_t svqadd[_s8](svint8_t op1, svint8_t op2)
        ///   SQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint16_t svqadd[_u16](svuint16_t op1, svuint16_t op2)
        ///   UQADD Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint32_t svqadd[_u32](svuint32_t op1, svuint32_t op2)
        ///   UQADD Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// svuint64_t svqadd[_u64](svuint64_t op1, svuint64_t op2)
        ///   UQADD Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) => AddSaturate(left, right);

        ///  And : Bitwise AND

        /// <summary>
        /// svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> And(Vector<byte> left, Vector<byte> right) => And(left, right);

        /// <summary>
        /// svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> And(Vector<short> left, Vector<short> right) => And(left, right);

        /// <summary>
        /// svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> And(Vector<int> left, Vector<int> right) => And(left, right);

        /// <summary>
        /// svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> And(Vector<long> left, Vector<long> right) => And(left, right);

        /// <summary>
        /// svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right) => And(left, right);

        /// <summary>
        /// svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> And(Vector<ushort> left, Vector<ushort> right) => And(left, right);

        /// <summary>
        /// svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> And(Vector<uint> left, Vector<uint> right) => And(left, right);

        /// <summary>
        /// svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> And(Vector<ulong> left, Vector<ulong> right) => And(left, right);


        ///  AndAcross : Bitwise AND reduction to scalar

        /// <summary>
        /// uint8_t svandv[_u8](svbool_t pg, svuint8_t op)
        ///   ANDV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<byte> AndAcross(Vector<byte> value) => AndAcross(value);

        /// <summary>
        /// int16_t svandv[_s16](svbool_t pg, svint16_t op)
        ///   ANDV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<short> AndAcross(Vector<short> value) => AndAcross(value);

        /// <summary>
        /// int32_t svandv[_s32](svbool_t pg, svint32_t op)
        ///   ANDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> AndAcross(Vector<int> value) => AndAcross(value);

        /// <summary>
        /// int64_t svandv[_s64](svbool_t pg, svint64_t op)
        ///   ANDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> AndAcross(Vector<long> value) => AndAcross(value);

        /// <summary>
        /// int8_t svandv[_s8](svbool_t pg, svint8_t op)
        ///   ANDV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> AndAcross(Vector<sbyte> value) => AndAcross(value);

        /// <summary>
        /// uint16_t svandv[_u16](svbool_t pg, svuint16_t op)
        ///   ANDV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> AndAcross(Vector<ushort> value) => AndAcross(value);

        /// <summary>
        /// uint32_t svandv[_u32](svbool_t pg, svuint32_t op)
        ///   ANDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> AndAcross(Vector<uint> value) => AndAcross(value);

        /// <summary>
        /// uint64_t svandv[_u64](svbool_t pg, svuint64_t op)
        ///   ANDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> AndAcross(Vector<ulong> value) => AndAcross(value);


        ///  BitwiseClear : Bitwise clear

        /// <summary>
        /// svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> BitwiseClear(Vector<short> left, Vector<short> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> BitwiseClear(Vector<int> left, Vector<int> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> BitwiseClear(Vector<long> left, Vector<long> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right) => BitwiseClear(left, right);


        ///  BooleanNot : Logically invert boolean condition

        /// <summary>
        /// svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op)
        ///   CNOT Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> BooleanNot(Vector<byte> value) => BooleanNot(value);

        /// <summary>
        /// svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op)
        ///   CNOT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> BooleanNot(Vector<short> value) => BooleanNot(value);

        /// <summary>
        /// svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op)
        ///   CNOT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> BooleanNot(Vector<int> value) => BooleanNot(value);

        /// <summary>
        /// svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op)
        ///   CNOT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> BooleanNot(Vector<long> value) => BooleanNot(value);

        /// <summary>
        /// svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op)
        ///   CNOT Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> BooleanNot(Vector<sbyte> value) => BooleanNot(value);

        /// <summary>
        /// svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op)
        ///   CNOT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> BooleanNot(Vector<ushort> value) => BooleanNot(value);

        /// <summary>
        /// svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op)
        ///   CNOT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> BooleanNot(Vector<uint> value) => BooleanNot(value);

        /// <summary>
        /// svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op)
        ///   CNOT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> BooleanNot(Vector<ulong> value) => BooleanNot(value);

        ///  Shuffle active elements of vector to the right and fill with zero

        /// <summary>
        /// svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op)
        ///   COMPACT Zresult.D, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> Compact(Vector<double> mask, Vector<double> value) => Compact(mask, value);

        /// <summary>
        /// svint32_t svcompact[_s32](svbool_t pg, svint32_t op)
        ///   COMPACT Zresult.S, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> Compact(Vector<int> mask, Vector<int> value) => Compact(mask, value);

        /// <summary>
        /// svint64_t svcompact[_s64](svbool_t pg, svint64_t op)
        ///   COMPACT Zresult.D, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> Compact(Vector<long> mask, Vector<long> value) => Compact(mask, value);

        /// <summary>
        /// svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op)
        ///   COMPACT Zresult.S, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> Compact(Vector<float> mask, Vector<float> value) => Compact(mask, value);

        /// <summary>
        /// svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op)
        ///   COMPACT Zresult.S, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> Compact(Vector<uint> mask, Vector<uint> value) => Compact(mask, value);

        /// <summary>
        /// svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op)
        ///   COMPACT Zresult.D, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value) => Compact(mask, value);


        ///  Compare equal to

        /// <summary>
        /// svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMEQ Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareEqual(Vector<double> left, Vector<double> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<short> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareEqual(Vector<short> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<int> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareEqual(Vector<int> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> CompareEqual(Vector<long> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMEQ Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareEqual(Vector<float> left, Vector<float> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> CompareEqual(Vector<ushort> left, Vector<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> CompareEqual(Vector<uint> left, Vector<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> CompareEqual(Vector<ulong> left, Vector<ulong> right) => CompareEqual(left, right);

        ///  Compare greater than

        /// <summary>
        /// svbool_t svcmpgt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGT Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThan(Vector<short> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThan(Vector<int> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPGT Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThan(Vector<long> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGT Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPHI Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThan(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThan(left, right);


        ///  Compare greater than or equal to

        /// <summary>
        /// svbool_t svcmpge[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPGE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> CompareGreaterThanOrEqual(Vector<long> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPHS Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        ///  Compare less than

        /// <summary>
        /// svbool_t svcmplt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPHI Presult.B, Pg/Z, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   CMPLO Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> CompareLessThan(Vector<byte> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGT Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> CompareLessThan(Vector<double> left, Vector<double> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPGT Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPLT Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareLessThan(Vector<short> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPGT Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPLT Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareLessThan(Vector<int> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPGT Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> CompareLessThan(Vector<long> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPGT Presult.B, Pg/Z, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPLT Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGT Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> CompareLessThan(Vector<float> left, Vector<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPHI Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   CMPLO Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPHI Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   CMPLO Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> CompareLessThan(Vector<uint> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPHI Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThan(Vector<ulong> left, Vector<ulong> right) => CompareLessThan(left, right);


        ///  Compare less than or equal to

        /// <summary>
        /// svbool_t svcmple[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPHS Presult.B, Pg/Z, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   CMPLS Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGE Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPGE Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPLE Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPGE Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPLE Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPGE Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> CompareLessThanOrEqual(Vector<long> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPGE Presult.B, Pg/Z, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPLE Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGE Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPHS Presult.H, Pg/Z, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   CMPLS Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPHS Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   CMPLS Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPHS Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        ///  Compare not equal to

        /// <summary>
        /// svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMNE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<short> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)
        ///   CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> CompareNotEqualTo(Vector<short> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<int> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)
        ///   CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> CompareNotEqualTo(Vector<int> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> CompareNotEqualTo(Vector<long> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<sbyte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)
        ///   CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMNE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> CompareNotEqualTo(Vector<ushort> left, Vector<ushort> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> CompareNotEqualTo(Vector<uint> left, Vector<uint> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> CompareNotEqualTo(Vector<ulong> left, Vector<ulong> right) => CompareNotEqualTo(left, right);


        ///  Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMUO Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareUnordered(Vector<double> left, Vector<double> right) => CompareUnordered(left, right);

        /// <summary>
        /// svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMUO Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareUnordered(Vector<float> left, Vector<float> right) => CompareUnordered(left, right);

        ///  Compute vector addresses for 16-bit data

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute16BitAddresses(bases, indices);


        ///  Compute vector addresses for 32-bit data

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute32BitAddresses(bases, indices);


        ///  Compute vector addresses for 64-bit data

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]
        /// </summary>
        public static unsafe Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute64BitAddresses(bases, indices);


        ///  Compute vector addresses for 8-bit data

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets)
        ///   ADR Zresult.S, [Zbases.S, Zoffsets.S]
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets)
        ///   ADR Zresult.S, [Zbases.S, Zoffsets.S]
        /// </summary>
        public static unsafe Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets)
        ///   ADR Zresult.D, [Zbases.D, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets)
        ///   ADR Zresult.D, [Zbases.D, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute8BitAddresses(bases, indices);

        ///  Conditionally extract element after last

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTA Btied, Pg, Btied, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint8_t svclasta[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.B
        /// </summary>
        public static unsafe byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTA Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// float64_t svclasta[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        ///   CLASTA Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTA Htied, Pg, Htied, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int16_t svclasta[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.H
        /// </summary>
        public static unsafe short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTA Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int32_t svclasta[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.S
        /// </summary>
        public static unsafe int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTA Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int64_t svclasta[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        ///   CLASTA Xtied, Pg, Xtied, Zdata.D
        /// </summary>
        public static unsafe long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTA Btied, Pg, Btied, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int8_t svclasta[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.B
        /// </summary>
        public static unsafe sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTA Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// float32_t svclasta[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        ///   CLASTA Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTA Htied, Pg, Htied, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint16_t svclasta[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.H
        /// </summary>
        public static unsafe ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTA Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint32_t svclasta[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        ///   CLASTA Wtied, Pg, Wtied, Zdata.S
        /// </summary>
        public static unsafe uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTA Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint64_t svclasta[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        ///   CLASTA Xtied, Pg, Xtied, Zdata.D
        /// </summary>
        public static unsafe ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValues, data);


        ///  Conditionally extract element after last

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTA Ztied.B, Pg, Ztied.B, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTA Ztied.B, Pg, Ztied.B, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultScalar, data);


        ///  Conditionally extract last element

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTB Btied, Pg, Btied, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> defaultValue, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint8_t svclastb[_n_u8](svbool_t pg, uint8_t fallback, svuint8_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.B
        /// </summary>
        public static unsafe byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValues, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTB Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> defaultValue, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// float64_t svclastb[_n_f64](svbool_t pg, float64_t fallback, svfloat64_t data)
        ///   CLASTB Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValues, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTB Htied, Pg, Htied, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> defaultValue, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int16_t svclastb[_n_s16](svbool_t pg, int16_t fallback, svint16_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.H
        /// </summary>
        public static unsafe short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValues, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTB Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> defaultValue, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int32_t svclastb[_n_s32](svbool_t pg, int32_t fallback, svint32_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.S
        /// </summary>
        public static unsafe int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValues, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTB Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> defaultValue, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int64_t svclastb[_n_s64](svbool_t pg, int64_t fallback, svint64_t data)
        ///   CLASTB Xtied, Pg, Xtied, Zdata.D
        /// </summary>
        public static unsafe long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValues, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTB Btied, Pg, Btied, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultValue, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// int8_t svclastb[_n_s8](svbool_t pg, int8_t fallback, svint8_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.B
        /// </summary>
        public static unsafe sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValues, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTB Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> defaultValue, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// float32_t svclastb[_n_f32](svbool_t pg, float32_t fallback, svfloat32_t data)
        ///   CLASTB Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValues, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTB Htied, Pg, Htied, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultValue, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint16_t svclastb[_n_u16](svbool_t pg, uint16_t fallback, svuint16_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.H
        /// </summary>
        public static unsafe ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValues, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTB Stied, Pg, Stied, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> defaultValue, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint32_t svclastb[_n_u32](svbool_t pg, uint32_t fallback, svuint32_t data)
        ///   CLASTB Wtied, Pg, Wtied, Zdata.S
        /// </summary>
        public static unsafe uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValues, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTB Dtied, Pg, Dtied, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultValue, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        /// uint64_t svclastb[_n_u64](svbool_t pg, uint64_t fallback, svuint64_t data)
        ///   CLASTB Xtied, Pg, Xtied, Zdata.D
        /// </summary>
        public static unsafe ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValues, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultValues, data);


        ///  Conditionally extract last element

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTB Ztied.B, Pg, Ztied.B, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, Vector<double> fallback, Vector<double> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, Vector<short> fallback, Vector<short> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, Vector<int> fallback, Vector<int> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, Vector<long> fallback, Vector<long> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTB Ztied.B, Pg, Ztied.B, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, Vector<float> fallback, Vector<float> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data) => ConditionalExtractLastActiveElementAndReplicate(mask, fallback, data);


        ///  Compare equal to
        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SEL Zresult.B, Pg, Zop1.B, Zop2.B
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SEL Zresult.B, Pg, Zop1.B, Zop2.B
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        ///
        /// </summary>
        public static unsafe Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        ///
        /// </summary>
        public static unsafe Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        ///
        /// </summary>
        public static unsafe Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.S, Pg/M, Zop.D
        /// svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.S, Pg/M, Ztied.D
        /// svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<double> value) => ConvertToInt32(value);

        /// <summary>
        /// svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.S, Pg/M, Zop.S
        /// svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.S, Pg/M, Ztied.S
        /// svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value) => ConvertToInt32(value);


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.D, Pg/M, Zop.D
        /// svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.D, Pg/M, Ztied.D
        /// svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value) => ConvertToInt64(value);

        /// <summary>
        /// svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.D, Pg/M, Zop.S
        /// svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.D, Pg/M, Ztied.S
        /// svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<float> value) => ConvertToInt64(value);


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.S, Pg/M, Zop.D
        /// svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.S, Pg/M, Ztied.D
        /// svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<double> value) => ConvertToUInt32(value);

        /// <summary>
        /// svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.S, Pg/M, Zop.S
        /// svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.S, Pg/M, Ztied.S
        /// svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value) => ConvertToUInt32(value);


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.D, Pg/M, Zop.D
        /// svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.D, Pg/M, Ztied.D
        /// svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value) => ConvertToUInt64(value);

        /// <summary>
        /// svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.D, Pg/M, Zop.S
        /// svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.D, Pg/M, Ztied.S
        /// svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<float> value) => ConvertToUInt64(value);


        ///  Count16BitElements : Count the number of 16-bit elements in a vector

        /// <summary>
        /// uint64_t svcnth_pat(enum svpattern pattern)
        ///   CNTH Xresult, pattern
        /// </summary>
        public static unsafe ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count16BitElements(pattern);


        ///  Count32BitElements : Count the number of 32-bit elements in a vector

        /// <summary>
        /// uint64_t svcntw_pat(enum svpattern pattern)
        ///   CNTW Xresult, pattern
        /// </summary>
        public static unsafe ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count32BitElements(pattern);


        ///  Count64BitElements : Count the number of 64-bit elements in a vector

        /// <summary>
        /// uint64_t svcntd_pat(enum svpattern pattern)
        ///   CNTD Xresult, pattern
        /// </summary>
        public static unsafe ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count64BitElements(pattern);


        ///  Count8BitElements : Count the number of 8-bit elements in a vector

        /// <summary>
        /// uint64_t svcntb_pat(enum svpattern pattern)
        ///   CNTB Xresult, pattern
        /// </summary>
        public static unsafe ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count8BitElements(pattern);


        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterMask(Vector<byte> mask, Vector<byte> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterMask(Vector<short> mask, Vector<short> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterMask(Vector<int> mask, Vector<int> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterMask(Vector<long> mask, Vector<long> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> mask, Vector<sbyte> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterMask(Vector<ushort> mask, Vector<ushort> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterMask(Vector<uint> mask, Vector<uint> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterMask(Vector<ulong> mask, Vector<ulong> srcMask) => CreateBreakAfterMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterPropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterPropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterPropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterPropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterPropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterPropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforeMask(Vector<byte> mask, Vector<byte> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforeMask(Vector<short> mask, Vector<short> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforeMask(Vector<int> mask, Vector<int> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforeMask(Vector<long> mask, Vector<long> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> mask, Vector<sbyte> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforeMask(Vector<ushort> mask, Vector<ushort> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforeMask(Vector<uint> mask, Vector<uint> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforeMask(Vector<ulong> mask, Vector<ulong> srcMask) => CreateBreakBeforeMask(mask, srcMask);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforePropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforePropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforePropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforePropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforePropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforePropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakBeforePropagateMask(mask, left, right);


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<byte> CreateFalseMaskByte() => CreateFalseMaskByte();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<double> CreateFalseMaskDouble() => CreateFalseMaskDouble();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<short> CreateFalseMaskInt16() => CreateFalseMaskInt16();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<int> CreateFalseMaskInt32() => CreateFalseMaskInt32();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<long> CreateFalseMaskInt64() => CreateFalseMaskInt64();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateFalseMaskSByte() => CreateFalseMaskSByte();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<float> CreateFalseMaskSingle() => CreateFalseMaskSingle();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<ushort> CreateFalseMaskUInt16() => CreateFalseMaskUInt16();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<uint> CreateFalseMaskUInt32() => CreateFalseMaskUInt32();


        /// Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<ulong> CreateFalseMaskUInt64() => CreateFalseMaskUInt64();


        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForFirstActiveElement(Vector<byte> mask, Vector<byte> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<short> CreateMaskForFirstActiveElement(Vector<short> mask, Vector<short> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<int> CreateMaskForFirstActiveElement(Vector<int> mask, Vector<int> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<long> CreateMaskForFirstActiveElement(Vector<long> mask, Vector<long> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateMaskForFirstActiveElement(Vector<sbyte> mask, Vector<sbyte> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForFirstActiveElement(Vector<ushort> mask, Vector<ushort> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForFirstActiveElement(Vector<uint> mask, Vector<uint> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForFirstActiveElement(Vector<ulong> mask, Vector<ulong> srcMask) => CreateMaskForFirstActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpnext_b8(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<byte> CreateMaskForNextActiveElement(Vector<byte> mask, Vector<byte> srcMask) => CreateMaskForNextActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpnext_b16(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.H, Pg, Ptied.H
        /// </summary>
        public static unsafe Vector<ushort> CreateMaskForNextActiveElement(Vector<ushort> mask, Vector<ushort> srcMask) => CreateMaskForNextActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpnext_b32(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.S, Pg, Ptied.S
        /// </summary>
        public static unsafe Vector<uint> CreateMaskForNextActiveElement(Vector<uint> mask, Vector<uint> srcMask) => CreateMaskForNextActiveElement(mask, srcMask);

        /// <summary>
        /// svbool_t svpnext_b64(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.D, Pg, Ptied.D
        /// </summary>
        public static unsafe Vector<ulong> CreateMaskForNextActiveElement(Vector<ulong> mask, Vector<ulong> srcMask) => CreateMaskForNextActiveElement(mask, srcMask);


        ///  CreateTrueMaskByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskByte(pattern);


        ///  CreateTrueMaskDouble : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskDouble(pattern);


        ///  CreateTrueMaskInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt16(pattern);


        ///  CreateTrueMaskInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt32(pattern);


        ///  CreateTrueMaskInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt64(pattern);


        ///  CreateTrueMaskSByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSByte(pattern);


        ///  CreateTrueMaskSingle : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSingle(pattern);


        ///  CreateTrueMaskUInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b16(enum svpattern pattern)
        ///   PTRUE Presult.H, pattern
        /// </summary>
        public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt16(pattern);


        ///  CreateTrueMaskUInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b32(enum svpattern pattern)
        ///   PTRUE Presult.S, pattern
        /// </summary>
        public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt32(pattern);


        ///  CreateTrueMaskUInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b64(enum svpattern pattern)
        ///   PTRUE Presult.D, pattern
        /// </summary>
        public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt64(pattern);


        ///  CreateWhileLessThanMask16Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right) => CreateWhileLessThanMask16Bit(left, right);


        ///  CreateWhileLessThanMask32Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(int left, int right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(long left, long right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right) => CreateWhileLessThanMask32Bit(left, right);


        ///  CreateWhileLessThanMask64Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right) => CreateWhileLessThanMask64Bit(left, right);


        ///  CreateWhileLessThanMask8Bit : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(int left, int right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(long left, long right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right) => CreateWhileLessThanMask8Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask16Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask16Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask32Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask32Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask64Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask64Bit(left, right);


        ///  CreateWhileLessThanOrEqualMask8Bit : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask8Bit(left, right);


        ///  Divide : Divide

        /// <summary>
        /// svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> Divide(Vector<float> left, Vector<float> right) => Divide(left, right);

        /// <summary>
        /// svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> Divide(Vector<double> left, Vector<double> right) => Divide(left, right);

        ///  DotProduct : Dot product

        /// <summary>
        /// svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        ///   SDOT Ztied1.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3)
        ///   SDOT Ztied1.D, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<long> DotProduct(Vector<long> addend, Vector<short> left, Vector<short> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        ///   UDOT Ztied1.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, Vector<byte> right) => DotProduct(addend, left, right);

        /// <summary>
        /// svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3)
        ///   UDOT Ztied1.D, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right) => DotProduct(addend, left, right);


        ///  DotProductBySelectedScalar : Dot product

        /// <summary>
        /// svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index)
        ///   SDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<int> DotProductBySelectedScalar(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<long> DotProductBySelectedScalar(Vector<long> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index)
        ///   UDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<uint> DotProductBySelectedScalar(Vector<uint> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<ulong> DotProductBySelectedScalar(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);


        ///  Broadcast a scalar value

        /// <summary>
        /// svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index)
        ///   DUP Zresult.B, Zdata.B[index]
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, [ConstantExpected(Min = 0, Max = (byte)(63))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        /// </summary>
        public static unsafe Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint16_t svdup_lane[_s16](svint16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, [ConstantExpected(Min = 0, Max = (byte)(31))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint32_t svdup_lane[_s32](svint32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint64_t svdup_lane[_s64](svint64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        /// </summary>
        public static unsafe Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint8_t svdup_lane[_s8](svint8_t data, uint8_t index)
        ///   DUP Zresult.B, Zdata.B[index]
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, [ConstantExpected(Min = 0, Max = (byte)(63))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, [ConstantExpected(Min = 0, Max = (byte)(31))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        /// </summary>
        public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);


        /// <summary>
        /// svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static unsafe Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3
        /// </summary>
        public static unsafe Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAdd(addend, left, right);


        ///  FusedMultiplyAddBySelectedScalar : Multiply-add, addend first

        /// <summary>
        /// svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddBySelectedScalar(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        /// svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        ///  FusedMultiplyAddNegated : Negated multiply-add, addend first

        /// <summary>
        /// svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddNegated(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAddNegated(addend, left, right);

        /// <summary>
        /// svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddNegated(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAddNegated(addend, left, right);


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtract(minuend, left, right);


        ///  FusedMultiplySubtractBySelectedScalar : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractBySelectedScalar(Vector<double> minuend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        /// svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractBySelectedScalar(Vector<float> minuend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        ///  FusedMultiplySubtractNegated : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractNegated(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtractNegated(minuend, left, right);

        /// <summary>
        /// svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        /// svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractNegated(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtractNegated(minuend, left, right);


        ///  Prefetch halfwords

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);


        ///  Prefetch words

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);


        ///  Prefetch doublewords

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);


        ///  Prefetch bytes

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);


        ///  Unextended load

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVector(mask, address, indices);




        ///  Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        ///  Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);


        ///  Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);


        ///  Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);


        ///  Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);


        ///  Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);


        ///  Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);


        ///  Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);


        ///  Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);


        ///  Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        // <summary>
        // svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        //   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        // <summary>
        // svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        //   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);


        ///  Unextended load

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);


        ///  Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<double> mask, Vector<double> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<short> mask, Vector<short> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<int> mask, Vector<int> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<long> mask, Vector<long> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<sbyte> mask, Vector<sbyte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<float> mask, Vector<float> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b16(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.H
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ushort> mask, Vector<ushort> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b32(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.S
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<uint> mask, Vector<uint> from) => GetActiveElementCount(mask, from);

        /// <summary>
        /// uint64_t svcntp_b64(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.D
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<ulong> mask, Vector<ulong> from) => GetActiveElementCount(mask, from);


        ///  Insert scalar into shifted vector

        /// <summary>
        /// svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2)
        ///   INSR Ztied1.B, Wop2
        ///   INSR Ztied1.B, Bop2
        /// </summary>
        public static unsafe Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2)
        ///   INSR Ztied1.D, Xop2
        ///   INSR Ztied1.D, Dop2
        /// </summary>
        public static unsafe Vector<double> InsertIntoShiftedVector(Vector<double> left, double right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint16_t svinsr[_n_s16](svint16_t op1, int16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        /// </summary>
        public static unsafe Vector<short> InsertIntoShiftedVector(Vector<short> left, short right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint32_t svinsr[_n_s32](svint32_t op1, int32_t op2)
        ///   INSR Ztied1.S, Wop2
        ///   INSR Ztied1.S, Sop2
        /// </summary>
        public static unsafe Vector<int> InsertIntoShiftedVector(Vector<int> left, int right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint64_t svinsr[_n_s64](svint64_t op1, int64_t op2)
        ///   INSR Ztied1.D, Xop2
        ///   INSR Ztied1.D, Dop2
        /// </summary>
        public static unsafe Vector<long> InsertIntoShiftedVector(Vector<long> left, long right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2)
        ///   INSR Ztied1.B, Wop2
        ///   INSR Ztied1.B, Bop2
        /// </summary>
        public static unsafe Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2)
        ///   INSR Ztied1.S, Wop2
        ///   INSR Ztied1.S, Sop2
        /// </summary>
        public static unsafe Vector<float> InsertIntoShiftedVector(Vector<float> left, float right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint16_t svinsr[_n_u16](svuint16_t op1, uint16_t op2)
        ///   INSR Ztied1.H, Wop2
        ///   INSR Ztied1.H, Hop2
        /// </summary>
        public static unsafe Vector<ushort> InsertIntoShiftedVector(Vector<ushort> left, ushort right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint32_t svinsr[_n_u32](svuint32_t op1, uint32_t op2)
        ///   INSR Ztied1.S, Wop2
        ///   INSR Ztied1.S, Sop2
        /// </summary>
        public static unsafe Vector<uint> InsertIntoShiftedVector(Vector<uint> left, uint right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svuint64_t svinsr[_n_u64](svuint64_t op1, uint64_t op2)
        ///   INSR Ztied1.D, Xop2
        ///   INSR Ztied1.D, Dop2
        /// </summary>
        public static unsafe Vector<ulong> InsertIntoShiftedVector(Vector<ulong> left, ulong right) => InsertIntoShiftedVector(left, right);


        ///  LeadingSignCount : Count leading sign bits

        /// <summary>
        /// svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op)
        ///   CLS Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingSignCount(Vector<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op)
        ///   CLS Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingSignCount(Vector<short> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op)
        ///   CLS Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingSignCount(Vector<int> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op)
        ///   CLS Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingSignCount(Vector<long> value) => LeadingSignCount(value);


        ///  LeadingZeroCount : Count leading zero bits

        /// <summary>
        /// svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op)
        ///   CLZ Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op)
        ///   CLZ Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op)
        ///   CLZ Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op)
        ///   CLZ Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op)
        ///   CLZ Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op)
        ///   CLZ Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op)
        ///   CLZ Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op)
        ///   CLZ Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<ulong> value) => LeadingZeroCount(value);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address) => LoadVector(mask, address);

        /// <summary>
        /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address) => LoadVector(mask, address);

        /// <summary>
        /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address) => LoadVector(mask, address);

        /// <summary>
        /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address) => LoadVector(mask, address);

        /// <summary>
        /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address) => LoadVector(mask, address);

        /// <summary>
        /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address) => LoadVector(mask, address);

        /// <summary>
        /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address) => LoadVector(mask, address);


        /// <summary>
        /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(double* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(short* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(int* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(long* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(float* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address) => LoadVectorNonFaulting(address);

        /// <summary>
        /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address) => LoadVectorNonFaulting(address);


        /// <summary>
        /// svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base)
        ///   LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        /// svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address) => LoadVectorNonTemporal(mask, address);


        /// <summary>
        /// svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1RQB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, byte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, double* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, short* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, int* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, long* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base)
        ///   LD1RQB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, sbyte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, float* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, ushort* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, uint* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        /// svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, ulong* address) => LoadVector128AndReplicateToVector(mask, address);

        ///  LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address) => LoadVectorByteZeroExtendToInt16(mask, address);


        ///  LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address) => LoadVectorByteZeroExtendToInt32(mask, address);


        ///  LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address) => LoadVectorByteZeroExtendToInt64(mask, address);


        ///  LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address) => LoadVectorByteZeroExtendToUInt16(mask, address);


        ///  LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address) => LoadVectorByteZeroExtendToUInt32(mask, address);


        ///  LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address) => LoadVectorByteZeroExtendToUInt64(mask, address);


        ///  LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address) => LoadVectorInt16SignExtendToInt32(mask, address);


        ///  LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address) => LoadVectorInt16SignExtendToInt64(mask, address);


        ///  LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address) => LoadVectorInt16SignExtendToUInt32(mask, address);


        ///  LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address) => LoadVectorInt16SignExtendToUInt64(mask, address);


        ///  LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address) => LoadVectorInt32SignExtendToInt64(mask, address);


        ///  LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address) => LoadVectorInt32SignExtendToUInt64(mask, address);


        ///  LoadVectorSByteSignExtendToInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address) => LoadVectorSByteSignExtendToInt16(mask, address);


        ///  LoadVectorSByteSignExtendToInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address) => LoadVectorSByteSignExtendToInt32(mask, address);


        ///  LoadVectorSByteSignExtendToInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address) => LoadVectorSByteSignExtendToInt64(mask, address);


        ///  LoadVectorSByteSignExtendToUInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt16(mask, address);


        ///  LoadVectorSByteSignExtendToUInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt32(mask, address);


        ///  LoadVectorSByteSignExtendToUInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt64(mask, address);


        ///  LoadVectorUInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt32(mask, address);


        ///  LoadVectorUInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt64(mask, address);


        ///  LoadVectorUInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt32(mask, address);


        ///  LoadVectorUInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt64(mask, address);


        ///  LoadVectorUInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address) => LoadVectorUInt32ZeroExtendToInt64(mask, address);


        ///  LoadVectorUInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address) => LoadVectorUInt32ZeroExtendToUInt64(mask, address);

        /// <summary>
        /// svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt16(address);

        /// <summary>
        /// svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt32(address);

        /// <summary>
        /// svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt64(address);

        /// <summary>
        /// svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt16(address);

        /// <summary>
        /// svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt32(address);

        /// <summary>
        /// svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt64(address);

        /// <summary>
        /// svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt32(address);

        /// <summary>
        /// svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt64(address);

        /// <summary>
        /// svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt32(address);

        /// <summary>
        /// svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt64(address);

        /// <summary>
        /// svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToInt64(address);

        /// <summary>
        /// svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToUInt64(address);

        /// <summary>
        /// svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base)
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>) Load2xVectorAndUnzip(Vector<byte> mask, byte* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>) Load2xVectorAndUnzip(Vector<double> mask, double* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>) Load2xVectorAndUnzip(Vector<short> mask, short* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>) Load2xVectorAndUnzip(Vector<int> mask, int* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>) Load2xVectorAndUnzip(Vector<long> mask, long* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base)
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>) Load2xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>) Load2xVectorAndUnzip(Vector<float> mask, float* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>) Load2xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>) Load2xVectorAndUnzip(Vector<uint> mask, uint* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>) Load2xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base)
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) Load3xVectorAndUnzip(Vector<byte> mask, byte* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>) Load3xVectorAndUnzip(Vector<double> mask, double* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>) Load3xVectorAndUnzip(Vector<short> mask, short* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>) Load3xVectorAndUnzip(Vector<int> mask, int* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>) Load3xVectorAndUnzip(Vector<long> mask, long* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base)
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) Load3xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>) Load3xVectorAndUnzip(Vector<float> mask, float* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) Load3xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) Load3xVectorAndUnzip(Vector<uint> mask, uint* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) Load3xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base)
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) Load4xVectorAndUnzip(Vector<byte> mask, byte* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) Load4xVectorAndUnzip(Vector<double> mask, double* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) Load4xVectorAndUnzip(Vector<short> mask, short* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) Load4xVectorAndUnzip(Vector<int> mask, int* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) Load4xVectorAndUnzip(Vector<long> mask, long* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base)
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) Load4xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) Load4xVectorAndUnzip(Vector<float> mask, float* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) Load4xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) Load4xVectorAndUnzip(Vector<uint> mask, uint* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        /// svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) Load4xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load4xVectorAndUnzip(mask, address);

        ///  Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToInt32(address);


        ///  Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToInt64(address);


        ///  Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt32(address);


        ///  Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt64(address);

        ///  Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base)
        ///   LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToInt64(address);

        ///  Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base)
        ///   LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToUInt64(address);

        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt16(address);


        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt32(address);

        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt64(address);


        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt16(address);


        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt32(address);


        ///  Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt64(address);

        ///  Max : Maximum

        /// <summary>
        /// svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Max(Vector<byte> left, Vector<byte> right) => Max(left, right);

        /// <summary>
        /// svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Max(Vector<double> left, Vector<double> right) => Max(left, right);

        /// <summary>
        /// svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Max(Vector<short> left, Vector<short> right) => Max(left, right);

        /// <summary>
        /// svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Max(Vector<int> left, Vector<int> right) => Max(left, right);

        /// <summary>
        /// svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Max(Vector<long> left, Vector<long> right) => Max(left, right);

        /// <summary>
        /// svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right) => Max(left, right);

        /// <summary>
        /// svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Max(Vector<float> left, Vector<float> right) => Max(left, right);

        /// <summary>
        /// svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right) => Max(left, right);

        /// <summary>
        /// svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Max(Vector<uint> left, Vector<uint> right) => Max(left, right);

        /// <summary>
        /// svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op)
        ///   UMAXV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<byte> MaxAcross(Vector<byte> value) => MaxAcross(value);

        /// <summary>
        /// float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> MaxAcross(Vector<double> value) => MaxAcross(value);

        /// <summary>
        /// int16_t svmaxv[_s16](svbool_t pg, svint16_t op)
        ///   SMAXV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<short> MaxAcross(Vector<short> value) => MaxAcross(value);

        /// <summary>
        /// int32_t svmaxv[_s32](svbool_t pg, svint32_t op)
        ///   SMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> MaxAcross(Vector<int> value) => MaxAcross(value);

        /// <summary>
        /// int64_t svmaxv[_s64](svbool_t pg, svint64_t op)
        ///   SMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> MaxAcross(Vector<long> value) => MaxAcross(value);

        /// <summary>
        /// int8_t svmaxv[_s8](svbool_t pg, svint8_t op)
        ///   SMAXV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> MaxAcross(Vector<sbyte> value) => MaxAcross(value);

        /// <summary>
        /// float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> MaxAcross(Vector<float> value) => MaxAcross(value);

        /// <summary>
        /// uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op)
        ///   UMAXV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> MaxAcross(Vector<ushort> value) => MaxAcross(value);

        /// <summary>
        /// uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op)
        ///   UMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> MaxAcross(Vector<uint> value) => MaxAcross(value);

        /// <summary>
        /// uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op)
        ///   UMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> MaxAcross(Vector<ulong> value) => MaxAcross(value);


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAXNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> MaxNumber(Vector<double> left, Vector<double> right) => MaxNumber(left, right);

        /// <summary>
        /// svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAXNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> MaxNumber(Vector<float> left, Vector<float> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMAXNMV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> MaxNumberAcross(Vector<double> value) => MaxNumberAcross(value);

        /// <summary>
        /// float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMAXNMV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> MaxNumberAcross(Vector<float> value) => MaxNumberAcross(value);


        ///  Min : Minimum

        /// <summary>
        /// svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Min(Vector<byte> left, Vector<byte> right) => Min(left, right);

        /// <summary>
        /// svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Min(Vector<double> left, Vector<double> right) => Min(left, right);

        /// <summary>
        /// svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Min(Vector<short> left, Vector<short> right) => Min(left, right);

        /// <summary>
        /// svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Min(Vector<int> left, Vector<int> right) => Min(left, right);

        /// <summary>
        /// svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Min(Vector<long> left, Vector<long> right) => Min(left, right);

        /// <summary>
        /// svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right) => Min(left, right);

        /// <summary>
        /// svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Min(Vector<float> left, Vector<float> right) => Min(left, right);

        /// <summary>
        /// svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right) => Min(left, right);

        /// <summary>
        /// svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Min(Vector<uint> left, Vector<uint> right) => Min(left, right);

        /// <summary>
        /// svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// uint8_t svminv[_u8](svbool_t pg, svuint8_t op)
        ///   UMINV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<byte> MinAcross(Vector<byte> value) => MinAcross(value);

        /// <summary>
        /// float64_t svminv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> MinAcross(Vector<double> value) => MinAcross(value);

        /// <summary>
        /// int16_t svminv[_s16](svbool_t pg, svint16_t op)
        ///   SMINV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<short> MinAcross(Vector<short> value) => MinAcross(value);

        /// <summary>
        /// int32_t svminv[_s32](svbool_t pg, svint32_t op)
        ///   SMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> MinAcross(Vector<int> value) => MinAcross(value);

        /// <summary>
        /// int64_t svminv[_s64](svbool_t pg, svint64_t op)
        ///   SMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> MinAcross(Vector<long> value) => MinAcross(value);

        /// <summary>
        /// int8_t svminv[_s8](svbool_t pg, svint8_t op)
        ///   SMINV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> MinAcross(Vector<sbyte> value) => MinAcross(value);

        /// <summary>
        /// float32_t svminv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> MinAcross(Vector<float> value) => MinAcross(value);

        /// <summary>
        /// uint16_t svminv[_u16](svbool_t pg, svuint16_t op)
        ///   UMINV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> MinAcross(Vector<ushort> value) => MinAcross(value);

        /// <summary>
        /// uint32_t svminv[_u32](svbool_t pg, svuint32_t op)
        ///   UMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> MinAcross(Vector<uint> value) => MinAcross(value);

        /// <summary>
        /// uint64_t svminv[_u64](svbool_t pg, svuint64_t op)
        ///   UMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> MinAcross(Vector<ulong> value) => MinAcross(value);


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMINNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> MinNumber(Vector<double> left, Vector<double> right) => MinNumber(left, right);

        /// <summary>
        /// svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMINNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> MinNumber(Vector<float> left, Vector<float> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMINNMV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> MinNumberAcross(Vector<double> value) => MinNumberAcross(value);

        /// <summary>
        /// float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMINNMV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> MinNumberAcross(Vector<float> value) => MinNumberAcross(value);


        /// <summary>
        /// svint8_t svmul[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmul[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmul[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MUL Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Multiply(Vector<sbyte> left, Vector<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// svint16_t svmul[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmul[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmul[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MUL Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Multiply(Vector<short> left, Vector<short> right) => Multiply(left, right);

        /// <summary>
        /// svint32_t svmul[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmul[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmul[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MUL Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Multiply(Vector<int> left, Vector<int> right) => Multiply(left, right);

        /// <summary>
        /// svint64_t svmul[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmul[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmul[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MUL Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Multiply(Vector<long> left, Vector<long> right) => Multiply(left, right);

        /// <summary>
        /// svuint8_t svmul[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmul[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MUL Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MUL Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Multiply(Vector<byte> left, Vector<byte> right) => Multiply(left, right);

        /// <summary>
        /// svuint16_t svmul[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmul[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmul[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MUL Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MUL Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Multiply(Vector<ushort> left, Vector<ushort> right) => Multiply(left, right);

        /// <summary>
        /// svuint32_t svmul[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmul[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmul[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MUL Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Multiply(Vector<uint> left, Vector<uint> right) => Multiply(left, right);

        /// <summary>
        /// svuint64_t svmul[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmul[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmul[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MUL Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Multiply(Vector<ulong> left, Vector<ulong> right) => Multiply(left, right);

        /// <summary>
        /// svfloat32_t svmul[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmul[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   FMUL Zresult.S, Zop1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmul[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMUL Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMUL Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Multiply(Vector<float> left, Vector<float> right) => Multiply(left, right);

        /// <summary>
        /// svfloat64_t svmul[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmul[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   FMUL Zresult.D, Zop1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMUL Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMUL Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Multiply(Vector<double> left, Vector<double> right) => Multiply(left, right);

        ///  MultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => MultiplyAdd(addend, left, right);

        ///  MultiplyBySelectedScalar : Multiply

        /// <summary>
        /// svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index)
        ///   FMUL Zresult.D, Zop1.D, Zop2.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> MultiplyBySelectedScalar(Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        /// svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index)
        ///   FMUL Zresult.S, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> MultiplyBySelectedScalar(Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        /// svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right) => MultiplyExtended(left, right);

        /// <summary>
        /// svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        /// svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right) => MultiplyExtended(left, right);

        ///  MultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        /// svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        /// svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        /// svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        /// svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        /// svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        /// svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        /// svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        /// svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right) => MultiplySubtract(minuend, left, right);

        ///  Negate : Negate

        /// <summary>
        /// svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   FNEG Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> Negate(Vector<double> value) => Negate(value);

        /// <summary>
        /// svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op)
        /// svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op)
        ///   NEG Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> Negate(Vector<short> value) => Negate(value);

        /// <summary>
        /// svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op)
        /// svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op)
        ///   NEG Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> Negate(Vector<int> value) => Negate(value);

        /// <summary>
        /// svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op)
        /// svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op)
        ///   NEG Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> Negate(Vector<long> value) => Negate(value);

        /// <summary>
        /// svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        /// svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op)
        ///   NEG Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> Negate(Vector<sbyte> value) => Negate(value);

        /// <summary>
        /// svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   FNEG Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> Negate(Vector<float> value) => Negate(value);

        ///  Bitwise invert

        /// <summary>
        /// svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   NOT Ztied.B, Pg/M, Zop.B
        /// svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op)
        ///   NOT Ztied.B, Pg/M, Ztied.B
        /// svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<byte> Not(Vector<byte> value) => Not(value);

        /// <summary>
        /// svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   NOT Ztied.H, Pg/M, Zop.H
        /// svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op)
        ///   NOT Ztied.H, Pg/M, Ztied.H
        /// svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<short> Not(Vector<short> value) => Not(value);

        /// <summary>
        /// svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   NOT Ztied.S, Pg/M, Zop.S
        /// svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op)
        ///   NOT Ztied.S, Pg/M, Ztied.S
        /// svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<int> Not(Vector<int> value) => Not(value);

        /// <summary>
        /// svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   NOT Ztied.D, Pg/M, Zop.D
        /// svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op)
        ///   NOT Ztied.D, Pg/M, Ztied.D
        /// svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<long> Not(Vector<long> value) => Not(value);

        /// <summary>
        /// svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   NOT Ztied.B, Pg/M, Zop.B
        /// svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op)
        ///   NOT Ztied.B, Pg/M, Ztied.B
        /// svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<sbyte> Not(Vector<sbyte> value) => Not(value);

        /// <summary>
        /// svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   NOT Ztied.H, Pg/M, Zop.H
        /// svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op)
        ///   NOT Ztied.H, Pg/M, Ztied.H
        /// svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<ushort> Not(Vector<ushort> value) => Not(value);

        /// <summary>
        /// svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   NOT Ztied.S, Pg/M, Zop.S
        /// svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op)
        ///   NOT Ztied.S, Pg/M, Ztied.S
        /// svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<uint> Not(Vector<uint> value) => Not(value);

        /// <summary>
        /// svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   NOT Ztied.D, Pg/M, Zop.D
        /// svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op)
        ///   NOT Ztied.D, Pg/M, Ztied.D
        /// svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op)
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<ulong> Not(Vector<ulong> value) => Not(value);

        ///  Or : Bitwise inclusive OR

        /// <summary>
        /// svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> Or(Vector<byte> left, Vector<byte> right) => Or(left, right);

        /// <summary>
        /// svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> Or(Vector<short> left, Vector<short> right) => Or(left, right);

        /// <summary>
        /// svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> Or(Vector<int> left, Vector<int> right) => Or(left, right);

        /// <summary>
        /// svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> Or(Vector<long> left, Vector<long> right) => Or(left, right);

        /// <summary>
        /// svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right) => Or(left, right);

        /// <summary>
        /// svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right) => Or(left, right);

        /// <summary>
        /// svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> Or(Vector<uint> left, Vector<uint> right) => Or(left, right);

        /// <summary>
        /// svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right) => Or(left, right);


        ///  OrAcross : Bitwise inclusive OR reduction to scalar

        /// <summary>
        /// uint8_t svorv[_u8](svbool_t pg, svuint8_t op)
        ///   ORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<byte> OrAcross(Vector<byte> value) => OrAcross(value);

        /// <summary>
        /// int16_t svorv[_s16](svbool_t pg, svint16_t op)
        ///   ORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<short> OrAcross(Vector<short> value) => OrAcross(value);

        /// <summary>
        /// int32_t svorv[_s32](svbool_t pg, svint32_t op)
        ///   ORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> OrAcross(Vector<int> value) => OrAcross(value);

        /// <summary>
        /// int64_t svorv[_s64](svbool_t pg, svint64_t op)
        ///   ORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> OrAcross(Vector<long> value) => OrAcross(value);

        /// <summary>
        /// int8_t svorv[_s8](svbool_t pg, svint8_t op)
        ///   ORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> OrAcross(Vector<sbyte> value) => OrAcross(value);

        /// <summary>
        /// uint16_t svorv[_u16](svbool_t pg, svuint16_t op)
        ///   ORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> OrAcross(Vector<ushort> value) => OrAcross(value);

        /// <summary>
        /// uint32_t svorv[_u32](svbool_t pg, svuint32_t op)
        ///   ORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> OrAcross(Vector<uint> value) => OrAcross(value);

        /// <summary>
        /// uint64_t svorv[_u64](svbool_t pg, svuint64_t op)
        ///   ORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> OrAcross(Vector<ulong> value) => OrAcross(value);


        ///  Count nonzero bits

        /// <summary>
        /// svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op)
        /// svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op)
        ///   CNT Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<sbyte> value) => PopCount(value);

        /// <summary>
        /// svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op)
        /// svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op)
        ///   CNT Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<byte> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op)
        /// svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<short> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op)
        /// svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<ushort> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op)
        /// svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<int> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op)
        /// svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<float> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op)
        /// svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<uint> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op)
        /// svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<double> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op)
        /// svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<long> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op)
        /// svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<ulong> value) => PopCount(value);

        /// <summary>
        /// void svprfb(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchBytes(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchBytes(mask, address, prefetchType);

        /// <summary>
        /// void svprfh(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt16(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt16(mask, address, prefetchType);

        /// <summary>
        /// void svprfw(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt32(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt32(mask, address, prefetchType);

        /// <summary>
        /// void svprfd(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt64(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => PrefetchInt64(mask, address, prefetchType);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat64_t svrecpe[_f64](svfloat64_t op)
        ///   FRECPE Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalEstimate(Vector<double> value) => ReciprocalEstimate(value);

        /// <summary>
        /// svfloat32_t svrecpe[_f32](svfloat32_t op)
        ///   FRECPE Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalEstimate(Vector<float> value) => ReciprocalEstimate(value);


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRECPX Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRECPX Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> ReciprocalExponent(Vector<double> value) => ReciprocalExponent(value);

        /// <summary>
        /// svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRECPX Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRECPX Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> ReciprocalExponent(Vector<float> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat64_t svrsqrte[_f64](svfloat64_t op)
        ///   FRSQRTE Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtEstimate(Vector<double> value) => ReciprocalSqrtEstimate(value);

        /// <summary>
        /// svfloat32_t svrsqrte[_f32](svfloat32_t op)
        ///   FRSQRTE Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtEstimate(Vector<float> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   FRSQRTS Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right) => ReciprocalSqrtStep(left, right);

        /// <summary>
        /// svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   FRSQRTS Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   FRECPS Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right) => ReciprocalStep(left, right);

        /// <summary>
        /// svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   FRECPS Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right) => ReciprocalStep(left, right);
        ///  Reverse bits

        /// <summary>
        /// svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op)
        ///   RBIT Ztied.B, Pg/M, Ztied.B
        /// </summary>
        public static unsafe Vector<byte> ReverseBits(Vector<byte> value) => ReverseBits(value);

        /// <summary>
        /// svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   RBIT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> ReverseBits(Vector<short> value) => ReverseBits(value);

        /// <summary>
        /// svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   RBIT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseBits(Vector<int> value) => ReverseBits(value);

        /// <summary>
        /// svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   RBIT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseBits(Vector<long> value) => ReverseBits(value);

        /// <summary>
        /// svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   RBIT Ztied.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> ReverseBits(Vector<sbyte> value) => ReverseBits(value);

        /// <summary>
        /// svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   RBIT Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseBits(Vector<ushort> value) => ReverseBits(value);

        /// <summary>
        /// svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   RBIT Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseBits(Vector<uint> value) => ReverseBits(value);

        /// <summary>
        /// svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   RBIT Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseBits(Vector<ulong> value) => ReverseBits(value);


        ///  Reverse all elements

        /// <summary>
        /// svuint8_t svrev[_u8](svuint8_t op)
        ///   REV Zresult.B, Zop.B
        /// </summary>
        public static unsafe Vector<byte> ReverseElement(Vector<byte> value) => ReverseElement(value);

        /// <summary>
        /// svfloat64_t svrev[_f64](svfloat64_t op)
        ///   REV Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReverseElement(Vector<double> value) => ReverseElement(value);

        /// <summary>
        /// svint16_t svrev[_s16](svint16_t op)
        ///   REV Zresult.H, Zop.H
        /// </summary>
        public static unsafe Vector<short> ReverseElement(Vector<short> value) => ReverseElement(value);

        /// <summary>
        /// svint32_t svrev[_s32](svint32_t op)
        ///   REV Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseElement(Vector<int> value) => ReverseElement(value);

        /// <summary>
        /// svint64_t svrev[_s64](svint64_t op)
        ///   REV Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseElement(Vector<long> value) => ReverseElement(value);

        /// <summary>
        /// svint8_t svrev[_s8](svint8_t op)
        ///   REV Zresult.B, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> ReverseElement(Vector<sbyte> value) => ReverseElement(value);

        /// <summary>
        /// svfloat32_t svrev[_f32](svfloat32_t op)
        ///   REV Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReverseElement(Vector<float> value) => ReverseElement(value);

        /// <summary>
        /// svuint16_t svrev[_u16](svuint16_t op)
        ///   REV Zresult.H, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement(Vector<ushort> value) => ReverseElement(value);

        /// <summary>
        /// svuint32_t svrev[_u32](svuint32_t op)
        ///   REV Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseElement(Vector<uint> value) => ReverseElement(value);

        /// <summary>
        /// svuint64_t svrev[_u64](svuint64_t op)
        ///   REV Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement(Vector<ulong> value) => ReverseElement(value);


        ///  Reverse halfwords within elements

        /// <summary>
        /// svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   REVH Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseElement16(Vector<int> value) => ReverseElement16(value);

        /// <summary>
        /// svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVH Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseElement16(Vector<long> value) => ReverseElement16(value);

        /// <summary>
        /// svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   REVH Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseElement16(Vector<uint> value) => ReverseElement16(value);

        /// <summary>
        /// svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVH Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement16(Vector<ulong> value) => ReverseElement16(value);


        ///  Reverse words within elements

        /// <summary>
        /// svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVW Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseElement32(Vector<long> value) => ReverseElement32(value);

        /// <summary>
        /// svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVW Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement32(Vector<ulong> value) => ReverseElement32(value);


        ///  Reverse bytes within elements

        /// <summary>
        /// svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   REVB Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> ReverseElement8(Vector<short> value) => ReverseElement8(value);

        /// <summary>
        /// svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   REVB Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseElement8(Vector<int> value) => ReverseElement8(value);

        /// <summary>
        /// svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVB Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseElement8(Vector<long> value) => ReverseElement8(value);

        /// <summary>
        /// svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   REVB Ztied.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement8(Vector<ushort> value) => ReverseElement8(value);

        /// <summary>
        /// svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   REVB Ztied.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseElement8(Vector<uint> value) => ReverseElement8(value);

        /// <summary>
        /// svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVB Ztied.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement8(Vector<ulong> value) => ReverseElement8(value);


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTA Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTA Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundAwayFromZero(Vector<double> value) => RoundAwayFromZero(value);

        /// <summary>
        /// svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTA Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTA Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundAwayFromZero(Vector<float> value) => RoundAwayFromZero(value);


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTN Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTN Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNearest(Vector<double> value) => RoundToNearest(value);

        /// <summary>
        /// svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTN Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTN Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNearest(Vector<float> value) => RoundToNearest(value);


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTM Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTM Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToNegativeInfinity(Vector<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTM Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTM Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToNegativeInfinity(Vector<float> value) => RoundToNegativeInfinity(value);


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTP Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTP Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToPositiveInfinity(Vector<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTP Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTP Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToPositiveInfinity(Vector<float> value) => RoundToPositiveInfinity(value);


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTZ Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTZ Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> RoundToZero(Vector<double> value) => RoundToZero(value);

        /// <summary>
        /// svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTZ Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTZ Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> RoundToZero(Vector<float> value) => RoundToZero(value);


        ///  Saturating decrement by number of halfword elements

        /// <summary>
        /// int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Ztied.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Ztied.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);


        ///  Saturating decrement by number of word elements

        /// <summary>
        /// int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Ztied.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Ztied.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);


        ///  Saturating decrement by number of doubleword elements

        /// <summary>
        /// int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Ztied.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Ztied.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);


        ///  Saturating decrement by number of byte elements

        /// <summary>
        /// int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECB Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECB Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);


        ///  Saturating decrement by active element count

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.B, Wtied
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(int value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.B
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svint16_t svqdecp[_s16](svint16_t op, svbool_t pg)
        ///   SQDECP Ztied.H, Pg
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svint32_t svqdecp[_s32](svint32_t op, svbool_t pg)
        ///   SQDECP Ztied.S, Pg
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svint64_t svqdecp[_s64](svint64_t op, svbool_t pg)
        ///   SQDECP Ztied.D, Pg
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.H, Wtied
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.H
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg)
        ///   UQDECP Ztied.H, Pg
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.S, Wtied
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(int value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.S
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg)
        ///   UQDECP Ztied.S, Pg
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.D, Wtied
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.D
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg)
        ///   UQDECP Ztied.D, Pg
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);


        ///  Saturating increment by number of halfword elements

        /// <summary>
        /// int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Ztied.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Ztied.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);


        ///  Saturating increment by number of word elements

        /// <summary>
        /// int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Ztied.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Ztied.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);


        ///  Saturating increment by number of doubleword elements

        /// <summary>
        /// int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Ztied.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        /// svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Ztied.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);


        ///  Saturating increment by number of byte elements

        /// <summary>
        /// int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCB Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCB Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        /// uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);


        ///  Saturating increment by active element count

        /// <summary>
        /// int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.B, Wtied
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(int value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.B
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svint16_t svqincp[_s16](svint16_t op, svbool_t pg)
        ///   SQINCP Ztied.H, Pg
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svint32_t svqincp[_s32](svint32_t op, svbool_t pg)
        ///   SQINCP Ztied.S, Pg
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svint64_t svqincp[_s64](svint64_t op, svbool_t pg)
        ///   SQINCP Ztied.D, Pg
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.H, Wtied
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.H
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg)
        ///   UQINCP Ztied.H, Pg
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.S, Wtied
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(int value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.S
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg)
        ///   UQINCP Ztied.S, Pg
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.D, Wtied
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.D
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        /// svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg)
        ///   UQINCP Ztied.D, Pg
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);


        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2)
        ///   FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2)
        ///   FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<double> Scale(Vector<double> left, Vector<long> right) => Scale(left, right);

        /// <summary>
        /// svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2)
        ///   FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2)
        ///   FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<float> Scale(Vector<float> left, Vector<int> right) => Scale(left, right);


        ///  Logical shift left

        /// <summary>
        /// svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        /// svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSL Zresult.B, Zop1.B, Zop2.D
        /// svuint8_t svlsl_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        /// svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSL Zresult.H, Zop1.H, Zop2.D
        /// svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        /// svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSL Zresult.S, Zop1.S, Zop2.D
        /// svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        /// svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSL Zresult.B, Zop1.B, Zop2.D
        /// svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        /// svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSL Zresult.H, Zop1.H, Zop2.D
        /// svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        /// svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSL Zresult.S, Zop1.S, Zop2.D
        /// svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right) => ShiftLeftLogical(left, right);


        ///  Arithmetic shift right

        /// <summary>
        /// svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        /// svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   ASR Zresult.H, Zop1.H, Zop2.D
        /// svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        /// svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   ASR Zresult.S, Zop1.S, Zop2.D
        /// svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        /// svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   ASR Zresult.B, Zop1.B, Zop2.D
        /// svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);


        ///  Arithmetic shift right for divide by immediate

        /// <summary>
        /// svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2
        /// svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2
        /// svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticForDivide(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2
        /// svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2
        /// svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticForDivide(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(32))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2
        /// svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2
        /// svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticForDivide(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(64))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        /// svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2
        /// svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2
        /// svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticForDivide(Vector<sbyte> value, [ConstantExpected(Min = 1, Max = (byte)(8))] byte control) => ShiftRightArithmeticForDivide(value, control);


        ///  Logical shift right

        /// <summary>
        /// svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        /// svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        /// svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSR Zresult.B, Zop1.B, Zop2.D
        /// svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        /// svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        /// svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSR Zresult.H, Zop1.H, Zop2.D
        /// svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        /// svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        /// svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSR Zresult.S, Zop1.S, Zop2.D
        /// svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        /// svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ShiftRightLogical(Vector<ulong> left, Vector<ulong> right) => ShiftRightLogical(left, right);


        ///  SignExtend16 : Sign-extend the low 16 bits

        /// <summary>
        /// svint32_t svexth[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   SXTH Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; SXTH Zresult.S, Pg/M, Zop.S
        /// svint32_t svexth[_s32]_x(svbool_t pg, svint32_t op)
        ///   SXTH Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; SXTH Zresult.S, Pg/M, Zop.S
        /// svint32_t svexth[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; SXTH Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> SignExtend16(Vector<int> value) => SignExtend16(value);

        /// <summary>
        /// svint64_t svexth[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   SXTH Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SXTH Zresult.D, Pg/M, Zop.D
        /// svint64_t svexth[_s64]_x(svbool_t pg, svint64_t op)
        ///   SXTH Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SXTH Zresult.D, Pg/M, Zop.D
        /// svint64_t svexth[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTH Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> SignExtend16(Vector<long> value) => SignExtend16(value);

        ///  SignExtend32 : Sign-extend the low 32 bits

        /// <summary>
        /// svint64_t svextw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   SXTW Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SXTW Zresult.D, Pg/M, Zop.D
        /// svint64_t svextw[_s64]_x(svbool_t pg, svint64_t op)
        ///   SXTW Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SXTW Zresult.D, Pg/M, Zop.D
        /// svint64_t svextw[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTW Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> SignExtend32(Vector<long> value) => SignExtend32(value);


        ///  SignExtend8 : Sign-extend the low 8 bits

        /// <summary>
        /// svint16_t svextb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   SXTB Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; SXTB Zresult.H, Pg/M, Zop.H
        /// svint16_t svextb[_s16]_x(svbool_t pg, svint16_t op)
        ///   SXTB Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; SXTB Zresult.H, Pg/M, Zop.H
        /// svint16_t svextb[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; SXTB Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> SignExtend8(Vector<short> value) => SignExtend8(value);

        /// <summary>
        /// svint32_t svextb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   SXTB Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; SXTB Zresult.S, Pg/M, Zop.S
        /// svint32_t svextb[_s32]_x(svbool_t pg, svint32_t op)
        ///   SXTB Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; SXTB Zresult.S, Pg/M, Zop.S
        /// svint32_t svextb[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; SXTB Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> SignExtend8(Vector<int> value) => SignExtend8(value);

        /// <summary>
        /// svint64_t svextb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   SXTB Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SXTB Zresult.D, Pg/M, Zop.D
        /// svint64_t svextb[_s64]_x(svbool_t pg, svint64_t op)
        ///   SXTB Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SXTB Zresult.D, Pg/M, Zop.D
        /// svint64_t svextb[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SXTB Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> SignExtend8(Vector<long> value) => SignExtend8(value);


        ///  SignExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svint16_t svunpklo[_s16](svint8_t op)
        ///   SUNPKLO Zresult.H, Zop.B
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningLower(Vector<sbyte> value) => SignExtendWideningLower(value);

        /// <summary>
        /// svint32_t svunpklo[_s32](svint16_t op)
        ///   SUNPKLO Zresult.S, Zop.H
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningLower(Vector<short> value) => SignExtendWideningLower(value);

        /// <summary>
        /// svint64_t svunpklo[_s64](svint32_t op)
        ///   SUNPKLO Zresult.D, Zop.S
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningLower(Vector<int> value) => SignExtendWideningLower(value);


        ///  SignExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svint16_t svunpkhi[_s16](svint8_t op)
        ///   SUNPKHI Zresult.H, Zop.B
        /// </summary>
        public static unsafe Vector<short> SignExtendWideningUpper(Vector<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// svint32_t svunpkhi[_s32](svint16_t op)
        ///   SUNPKHI Zresult.S, Zop.H
        /// </summary>
        public static unsafe Vector<int> SignExtendWideningUpper(Vector<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// svint64_t svunpkhi[_s64](svint32_t op)
        ///   SUNPKHI Zresult.D, Zop.S
        /// </summary>
        public static unsafe Vector<long> SignExtendWideningUpper(Vector<int> value) => SignExtendWideningUpper(value);


        ///  Splice two vectors under predicate control

        /// <summary>
        /// svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right) => Splice(mask, left, right);

        /// <summary>
        /// svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right) => Splice(mask, left, right);

        /// <summary>
        /// svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right) => Splice(mask, left, right);

        /// <summary>
        /// svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right) => Splice(mask, left, right);

        /// <summary>
        /// svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => Splice(mask, left, right);


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FSQRT Ztied.D, Pg/M, Zop.D
        /// svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FSQRT Ztied.D, Pg/M, Ztied.D
        /// svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op)
        /// </summary>
        public static unsafe Vector<double> Sqrt(Vector<double> value) => Sqrt(value);

        /// <summary>
        /// svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FSQRT Ztied.S, Pg/M, Zop.S
        /// svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FSQRT Ztied.S, Pg/M, Ztied.S
        /// svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op)
        /// </summary>
        public static unsafe Vector<float> Sqrt(Vector<float> value) => Sqrt(value);


        ///  Non-truncating store

        /// <summary>
        /// void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        ///   ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, Vector<byte> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data)
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data)
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data)
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3, Vector<byte> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, Vector<double> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3, Vector<double> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, Vector<short> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3, Vector<short> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, Vector<int> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3, Vector<int> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, Vector<long> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3, Vector<long> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        ///   ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data)
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data)
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data)
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3, Vector<sbyte> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, Vector<float> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3, Vector<float> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, Vector<ushort> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3, Vector<ushort> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, Vector<uint> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3, Vector<uint> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, Vector<ulong> data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        /// void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3, Vector<ulong> Value4) data) => StoreAndZip(mask, address, data);
        ///  Truncate to 8 bits and store


        /// <summary>
        /// void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data)
        ///   ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* address, Vector<short> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, short* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, short* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data)
        ///   ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* address, Vector<ushort> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, byte* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        /// void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        ///   STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* address, Vector<byte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<double> mask, double* address, Vector<double> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<short> mask, short* address, Vector<short> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<int> mask, int* address, Vector<int> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<long> mask, long* address, Vector<long> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        ///   STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<float> mask, float* address, Vector<float> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* address, Vector<ushort> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* address, Vector<uint> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        /// void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> data) => StoreNonTemporal(mask, address, data);


        ///  Subtract : Subtract

        /// <summary>
        /// svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUB Zresult.B, Zop1.B, Zop2.B
        /// svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUB Zresult.H, Zop1.H, Zop2.H
        /// svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> Subtract(Vector<short> left, Vector<short> right) => Subtract(left, right);

        /// <summary>
        /// svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUB Zresult.S, Zop1.S, Zop2.S
        /// svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> Subtract(Vector<int> left, Vector<int> right) => Subtract(left, right);

        /// <summary>
        /// svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUB Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> Subtract(Vector<long> left, Vector<long> right) => Subtract(left, right);

        /// <summary>
        /// svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUB Zresult.B, Zop1.B, Zop2.B
        /// svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> Subtract(Vector<byte> left, Vector<byte> right) => Subtract(left, right);

        /// <summary>
        /// svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUB Zresult.H, Zop1.H, Zop2.H
        /// svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right) => Subtract(left, right);

        /// <summary>
        /// svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUB Zresult.S, Zop1.S, Zop2.S
        /// svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Subtract(Vector<uint> left, Vector<uint> right) => Subtract(left, right);

        /// <summary>
        /// svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUB Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right) => Subtract(left, right);

        /// <summary>
        /// svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FSUB Zresult.S, Zop1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> Subtract(Vector<float> left, Vector<float> right) => Subtract(left, right);

        /// <summary>
        /// svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FSUB Zresult.D, Zop1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> Subtract(Vector<double> left, Vector<double> right) => Subtract(left, right);

        ///  SubtractSaturate : Saturating subtract

        /// <summary>
        /// svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2)
        ///   UQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint16_t svqsub[_s16](svint16_t op1, svint16_t op2)
        ///   SQSUB Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint32_t svqsub[_s32](svint32_t op1, svint32_t op2)
        ///   SQSUB Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint64_t svqsub[_s64](svint64_t op1, svint64_t op2)
        ///   SQSUB Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svint8_t svqsub[_s8](svint8_t op1, svint8_t op2)
        ///   SQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint16_t svqsub[_u16](svuint16_t op1, svuint16_t op2)
        ///   UQSUB Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint32_t svqsub[_u32](svuint32_t op1, svuint32_t op2)
        ///   UQSUB Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// svuint64_t svqsub[_u64](svuint64_t op1, svuint64_t op2)
        ///   UQSUB Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) => SubtractSaturate(left, right);


        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<byte> mask, Vector<byte> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<short> mask, Vector<short> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<int> mask, Vector<int> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<long> mask, Vector<long> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<sbyte> mask, Vector<sbyte> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ushort> mask, Vector<ushort> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<uint> mask, Vector<uint> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestAnyTrue(Vector<ulong> mask, Vector<ulong> srcMask) => TestAnyTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<byte> mask, Vector<byte> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<short> mask, Vector<short> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<int> mask, Vector<int> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<long> mask, Vector<long> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<sbyte> mask, Vector<sbyte> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ushort> mask, Vector<ushort> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<uint> mask, Vector<uint> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestFirstTrue(Vector<ulong> mask, Vector<ulong> srcMask) => TestFirstTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<byte> mask, Vector<byte> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<short> mask, Vector<short> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<int> mask, Vector<int> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<long> mask, Vector<long> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<sbyte> mask, Vector<sbyte> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ushort> mask, Vector<ushort> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<uint> mask, Vector<uint> srcMask) => TestLastTrue(mask, srcMask);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool TestLastTrue(Vector<ulong> mask, Vector<ulong> srcMask) => TestLastTrue(mask, srcMask);


        ///  Interleave even elements from two inputs

        /// <summary>
        /// svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TransposeEven(Vector<double> left, Vector<double> right) => TransposeEven(left, right);

        /// <summary>
        /// svint16_t svtrn1[_s16](svint16_t op1, svint16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> TransposeEven(Vector<short> left, Vector<short> right) => TransposeEven(left, right);

        /// <summary>
        /// svint32_t svtrn1[_s32](svint32_t op1, svint32_t op2)
        ///   TRN1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> TransposeEven(Vector<int> left, Vector<int> right) => TransposeEven(left, right);

        /// <summary>
        /// svint64_t svtrn1[_s64](svint64_t op1, svint64_t op2)
        ///   TRN1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> TransposeEven(Vector<long> left, Vector<long> right) => TransposeEven(left, right);

        /// <summary>
        /// svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2)
        ///   TRN1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TransposeEven(Vector<float> left, Vector<float> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right) => TransposeEven(left, right);


        ///  Interleave odd elements from two inputs

        /// <summary>
        /// svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TransposeOdd(Vector<double> left, Vector<double> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint16_t svtrn2[_s16](svint16_t op1, svint16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> TransposeOdd(Vector<short> left, Vector<short> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint32_t svtrn2[_s32](svint32_t op1, svint32_t op2)
        ///   TRN2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> TransposeOdd(Vector<int> left, Vector<int> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint64_t svtrn2[_s64](svint64_t op1, svint64_t op2)
        ///   TRN2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> TransposeOdd(Vector<long> left, Vector<long> right) => TransposeOdd(left, right);

        /// <summary>
        /// svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2)
        ///   TRN2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TransposeOdd(Vector<float> left, Vector<float> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right) => TransposeOdd(left, right);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP1 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svuzp1_b8(svbool_t op1, svbool_t op2)
        ///   UZP1 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right) => UnzipEven(left, right);

        /// <summary>
        /// svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> UnzipEven(Vector<double> left, Vector<double> right) => UnzipEven(left, right);

        /// <summary>
        /// svint16_t svuzp1[_s16](svint16_t op1, svint16_t op2)
        ///   UZP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> UnzipEven(Vector<short> left, Vector<short> right) => UnzipEven(left, right);

        /// <summary>
        /// svint32_t svuzp1[_s32](svint32_t op1, svint32_t op2)
        ///   UZP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> UnzipEven(Vector<int> left, Vector<int> right) => UnzipEven(left, right);

        /// <summary>
        /// svint64_t svuzp1[_s64](svint64_t op1, svint64_t op2)
        ///   UZP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> UnzipEven(Vector<long> left, Vector<long> right) => UnzipEven(left, right);

        /// <summary>
        /// svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2)
        ///   UZP1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right) => UnzipEven(left, right);

        /// <summary>
        /// svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> UnzipEven(Vector<float> left, Vector<float> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint16_t svuzp1[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP1 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svuzp1_b16(svbool_t op1, svbool_t op2)
        ///   UZP1 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> UnzipEven(Vector<ushort> left, Vector<ushort> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint32_t svuzp1[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP1 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svuzp1_b32(svbool_t op1, svbool_t op2)
        ///   UZP1 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> UnzipEven(Vector<uint> left, Vector<uint> right) => UnzipEven(left, right);

        /// <summary>
        /// svuint64_t svuzp1[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP1 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svuzp1_b64(svbool_t op1, svbool_t op2)
        ///   UZP1 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> UnzipEven(Vector<ulong> left, Vector<ulong> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP2 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svuzp2_b8(svbool_t op1, svbool_t op2)
        ///   UZP2 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right) => UnzipOdd(left, right);

        /// <summary>
        /// svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> UnzipOdd(Vector<double> left, Vector<double> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint16_t svuzp2[_s16](svint16_t op1, svint16_t op2)
        ///   UZP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> UnzipOdd(Vector<short> left, Vector<short> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint32_t svuzp2[_s32](svint32_t op1, svint32_t op2)
        ///   UZP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> UnzipOdd(Vector<int> left, Vector<int> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint64_t svuzp2[_s64](svint64_t op1, svint64_t op2)
        ///   UZP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> UnzipOdd(Vector<long> left, Vector<long> right) => UnzipOdd(left, right);

        /// <summary>
        /// svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2)
        ///   UZP2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right) => UnzipOdd(left, right);

        /// <summary>
        /// svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> UnzipOdd(Vector<float> left, Vector<float> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint16_t svuzp2[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP2 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svuzp2_b16(svbool_t op1, svbool_t op2)
        ///   UZP2 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> UnzipOdd(Vector<ushort> left, Vector<ushort> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint32_t svuzp2[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP2 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svuzp2_b32(svbool_t op1, svbool_t op2)
        ///   UZP2 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> UnzipOdd(Vector<uint> left, Vector<uint> right) => UnzipOdd(left, right);

        /// <summary>
        /// svuint64_t svuzp2[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP2 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svuzp2_b64(svbool_t op1, svbool_t op2)
        ///   UZP2 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> UnzipOdd(Vector<ulong> left, Vector<ulong> right) => UnzipOdd(left, right);


        ///  Table lookup in single-vector table

        /// <summary>
        /// svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices)
        ///   TBL Zresult.B, {Zdata.B}, Zindices.B
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices)
        ///   TBL Zresult.D, {Zdata.D}, Zindices.D
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint16_t svtbl[_s16](svint16_t data, svuint16_t indices)
        ///   TBL Zresult.H, {Zdata.H}, Zindices.H
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint32_t svtbl[_s32](svint32_t data, svuint32_t indices)
        ///   TBL Zresult.S, {Zdata.S}, Zindices.S
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint64_t svtbl[_s64](svint64_t data, svuint64_t indices)
        ///   TBL Zresult.D, {Zdata.D}, Zindices.D
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint8_t svtbl[_s8](svint8_t data, svuint8_t indices)
        ///   TBL Zresult.B, {Zdata.B}, Zindices.B
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices)
        ///   TBL Zresult.S, {Zdata.S}, Zindices.S
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices)
        ///   TBL Zresult.H, {Zdata.H}, Zindices.H
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices)
        ///   TBL Zresult.S, {Zdata.S}, Zindices.S
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices)
        ///   TBL Zresult.D, {Zdata.D}, Zindices.D
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices) => VectorTableLookup(data, indices);


        ///  Xor : Bitwise exclusive OR

        /// <summary>
        /// svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        /// svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> Xor(Vector<byte> left, Vector<byte> right) => Xor(left, right);

        /// <summary>
        /// svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        /// svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> Xor(Vector<short> left, Vector<short> right) => Xor(left, right);

        /// <summary>
        /// svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        /// svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> Xor(Vector<int> left, Vector<int> right) => Xor(left, right);

        /// <summary>
        /// svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        /// svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> Xor(Vector<long> left, Vector<long> right) => Xor(left, right);

        /// <summary>
        /// svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        /// svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right) => Xor(left, right);

        /// <summary>
        /// svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        /// svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right) => Xor(left, right);

        /// <summary>
        /// svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        /// svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> Xor(Vector<uint> left, Vector<uint> right) => Xor(left, right);

        /// <summary>
        /// svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        /// svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right) => Xor(left, right);


        ///  XorAcross : Bitwise exclusive OR reduction to scalar

        /// <summary>
        /// uint8_t sveorv[_u8](svbool_t pg, svuint8_t op)
        ///   EORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<byte> XorAcross(Vector<byte> value) => XorAcross(value);

        /// <summary>
        /// int16_t sveorv[_s16](svbool_t pg, svint16_t op)
        ///   EORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<short> XorAcross(Vector<short> value) => XorAcross(value);

        /// <summary>
        /// int32_t sveorv[_s32](svbool_t pg, svint32_t op)
        ///   EORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<int> XorAcross(Vector<int> value) => XorAcross(value);

        /// <summary>
        /// int64_t sveorv[_s64](svbool_t pg, svint64_t op)
        ///   EORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<long> XorAcross(Vector<long> value) => XorAcross(value);

        /// <summary>
        /// int8_t sveorv[_s8](svbool_t pg, svint8_t op)
        ///   EORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> XorAcross(Vector<sbyte> value) => XorAcross(value);

        /// <summary>
        /// uint16_t sveorv[_u16](svbool_t pg, svuint16_t op)
        ///   EORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> XorAcross(Vector<ushort> value) => XorAcross(value);

        /// <summary>
        /// uint32_t sveorv[_u32](svbool_t pg, svuint32_t op)
        ///   EORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> XorAcross(Vector<uint> value) => XorAcross(value);

        /// <summary>
        /// uint64_t sveorv[_u64](svbool_t pg, svuint64_t op)
        ///   EORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> XorAcross(Vector<ulong> value) => XorAcross(value);


        ///  ZeroExtend16 : Zero-extend the low 16 bits

        /// <summary>
        /// svuint32_t svexth[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   UXTH Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; UXTH Zresult.S, Pg/M, Zop.S
        /// svuint32_t svexth[_u32]_x(svbool_t pg, svuint32_t op)
        ///   UXTH Ztied.S, Pg/M, Ztied.S
        ///   AND Ztied.S, Ztied.S, #65535
        /// svuint32_t svexth[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; UXTH Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend16(Vector<uint> value) => ZeroExtend16(value);

        /// <summary>
        /// svuint64_t svexth[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   UXTH Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UXTH Zresult.D, Pg/M, Zop.D
        /// svuint64_t svexth[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UXTH Ztied.D, Pg/M, Ztied.D
        ///   AND Ztied.D, Ztied.D, #65535
        /// svuint64_t svexth[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTH Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend16(Vector<ulong> value) => ZeroExtend16(value);


        ///  ZeroExtend32 : Zero-extend the low 32 bits

        /// <summary>
        /// svuint64_t svextw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   UXTW Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UXTW Zresult.D, Pg/M, Zop.D
        /// svuint64_t svextw[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UXTW Ztied.D, Pg/M, Ztied.D
        ///   AND Ztied.D, Ztied.D, #4294967295
        /// svuint64_t svextw[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTW Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend32(Vector<ulong> value) => ZeroExtend32(value);


        ///  ZeroExtend8 : Zero-extend the low 8 bits

        /// <summary>
        /// svuint16_t svextb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   UXTB Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; UXTB Zresult.H, Pg/M, Zop.H
        /// svuint16_t svextb[_u16]_x(svbool_t pg, svuint16_t op)
        ///   UXTB Ztied.H, Pg/M, Ztied.H
        ///   AND Ztied.H, Ztied.H, #255
        /// svuint16_t svextb[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; UXTB Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtend8(Vector<ushort> value) => ZeroExtend8(value);

        /// <summary>
        /// svuint32_t svextb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   UXTB Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; UXTB Zresult.S, Pg/M, Zop.S
        /// svuint32_t svextb[_u32]_x(svbool_t pg, svuint32_t op)
        ///   UXTB Ztied.S, Pg/M, Ztied.S
        ///   AND Ztied.S, Ztied.S, #255
        /// svuint32_t svextb[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; UXTB Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ZeroExtend8(Vector<uint> value) => ZeroExtend8(value);

        /// <summary>
        /// svuint64_t svextb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   UXTB Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UXTB Zresult.D, Pg/M, Zop.D
        /// svuint64_t svextb[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UXTB Ztied.D, Pg/M, Ztied.D
        ///   AND Ztied.D, Ztied.D, #255
        /// svuint64_t svextb[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UXTB Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtend8(Vector<ulong> value) => ZeroExtend8(value);

        ///  ZeroExtendWideningLower : Unpack and extend low half

        /// <summary>
        /// svuint16_t svunpklo[_u16](svuint8_t op)
        ///   UUNPKLO Zresult.H, Zop.B
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningLower(Vector<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint32_t svunpklo[_u32](svuint16_t op)
        ///   UUNPKLO Zresult.S, Zop.H
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningLower(Vector<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint64_t svunpklo[_u64](svuint32_t op)
        ///   UUNPKLO Zresult.D, Zop.S
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningLower(Vector<uint> value) => ZeroExtendWideningLower(value);


        ///  ZeroExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svuint16_t svunpkhi[_u16](svuint8_t op)
        ///   UUNPKHI Zresult.H, Zop.B
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningUpper(Vector<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// svuint32_t svunpkhi[_u32](svuint16_t op)
        ///   UUNPKHI Zresult.S, Zop.H
        /// svbool_t svunpkhi[_b](svbool_t op)
        ///   PUNPKHI Presult.H, Pop.B
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningUpper(Vector<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// svuint64_t svunpkhi[_u64](svuint32_t op)
        ///   UUNPKHI Zresult.D, Zop.S
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value) => ZeroExtendWideningUpper(value);

        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right) => ZipHigh(left, right);

        /// <summary>
        /// svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ZipHigh(Vector<double> left, Vector<double> right) => ZipHigh(left, right);

        /// <summary>
        /// svint16_t svzip2[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP2 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> ZipHigh(Vector<short> left, Vector<short> right) => ZipHigh(left, right);

        /// <summary>
        /// svint32_t svzip2[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> ZipHigh(Vector<int> left, Vector<int> right) => ZipHigh(left, right);

        /// <summary>
        /// svint64_t svzip2[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> ZipHigh(Vector<long> left, Vector<long> right) => ZipHigh(left, right);

        /// <summary>
        /// svint8_t svzip2[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right) => ZipHigh(left, right);

        /// <summary>
        /// svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ZipHigh(Vector<float> left, Vector<float> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint16_t svzip2[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP2 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svzip2_b16(svbool_t op1, svbool_t op2)
        ///   ZIP2 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> ZipHigh(Vector<ushort> left, Vector<ushort> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint32_t svzip2[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP2 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svzip2_b32(svbool_t op1, svbool_t op2)
        ///   ZIP2 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> ZipHigh(Vector<uint> left, Vector<uint> right) => ZipHigh(left, right);

        /// <summary>
        /// svuint64_t svzip2[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP2 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svzip2_b64(svbool_t op1, svbool_t op2)
        ///   ZIP2 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> ZipHigh(Vector<ulong> left, Vector<ulong> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP1 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svzip1_b8(svbool_t op1, svbool_t op2)
        ///   ZIP1 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right) => ZipLow(left, right);

        /// <summary>
        /// svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ZipLow(Vector<double> left, Vector<double> right) => ZipLow(left, right);

        /// <summary>
        /// svint16_t svzip1[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP1 Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> ZipLow(Vector<short> left, Vector<short> right) => ZipLow(left, right);

        /// <summary>
        /// svint32_t svzip1[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> ZipLow(Vector<int> left, Vector<int> right) => ZipLow(left, right);

        /// <summary>
        /// svint64_t svzip1[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> ZipLow(Vector<long> left, Vector<long> right) => ZipLow(left, right);

        /// <summary>
        /// svint8_t svzip1[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right) => ZipLow(left, right);

        /// <summary>
        /// svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ZipLow(Vector<float> left, Vector<float> right) => ZipLow(left, right);

        /// <summary>
        /// svuint16_t svzip1[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP1 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svzip1_b16(svbool_t op1, svbool_t op2)
        ///   ZIP1 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> ZipLow(Vector<ushort> left, Vector<ushort> right) => ZipLow(left, right);

        /// <summary>
        /// svuint32_t svzip1[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP1 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svzip1_b32(svbool_t op1, svbool_t op2)
        ///   ZIP1 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> ZipLow(Vector<uint> left, Vector<uint> right) => ZipLow(left, right);

        /// <summary>
        /// svuint64_t svzip1[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP1 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svzip1_b64(svbool_t op1, svbool_t op2)
        ///   ZIP1 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> ZipLow(Vector<ulong> left, Vector<ulong> right) => ZipLow(left, right);

    }
}

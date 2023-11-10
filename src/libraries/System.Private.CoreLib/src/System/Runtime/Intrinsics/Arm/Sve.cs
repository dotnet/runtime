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


        ///  AbsoluteCompareGreaterThan : Absolute compare greater than

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


        ///  AbsoluteCompareGreaterThanOrEqual : Absolute compare greater than or equal to

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


        ///  AbsoluteCompareLessThan : Absolute compare less than

        /// <summary>
        /// svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACGT Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        /// svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACGT Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThan(left, right);


        ///  AbsoluteCompareLessThanOrEqual : Absolute compare less than or equal to

        /// <summary>
        /// svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FACGE Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FACGE Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThanOrEqual(left, right);


        ///  AbsoluteDifference : Absolute difference

        /// <summary>
        /// svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SABD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SABD Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SABD Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SABD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SABD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UABD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svabd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; UABD Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; UABD Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UABD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; UABD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; UABD Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; UABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; UABD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; UABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; UABD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FABD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FABD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FABD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FABD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FABD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FABD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right) => AbsoluteDifference(left, right);


        ///  Add : Add

        /// <summary>
        /// svint8_t svadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   ADD Zresult.B, Zop1.B, Zop2.B
        /// svint8_t svadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; ADD Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Add(Vector<sbyte> left, Vector<sbyte> right) => Add(left, right);

        /// <summary>
        /// svint16_t svadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   ADD Zresult.H, Zop1.H, Zop2.H
        /// svint16_t svadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; ADD Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Add(Vector<short> left, Vector<short> right) => Add(left, right);

        /// <summary>
        /// svint32_t svadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   ADD Zresult.S, Zop1.S, Zop2.S
        /// svint32_t svadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; ADD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Add(Vector<int> left, Vector<int> right) => Add(left, right);

        /// <summary>
        /// svint64_t svadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   ADD Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; ADD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Add(Vector<long> left, Vector<long> right) => Add(left, right);

        /// <summary>
        /// svuint8_t svadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ADD Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ADD Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   ADD Zresult.B, Zop1.B, Zop2.B
        /// svuint8_t svadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ADD Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; ADD Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Add(Vector<byte> left, Vector<byte> right) => Add(left, right);

        /// <summary>
        /// svuint16_t svadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ADD Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ADD Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   ADD Zresult.H, Zop1.H, Zop2.H
        /// svuint16_t svadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ADD Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; ADD Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Add(Vector<ushort> left, Vector<ushort> right) => Add(left, right);

        /// <summary>
        /// svuint32_t svadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   ADD Zresult.S, Zop1.S, Zop2.S
        /// svuint32_t svadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; ADD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Add(Vector<uint> left, Vector<uint> right) => Add(left, right);

        /// <summary>
        /// svuint64_t svadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   ADD Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; ADD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Add(Vector<ulong> left, Vector<ulong> right) => Add(left, right);

        /// <summary>
        /// svfloat32_t svadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FADD Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   FADD Zresult.S, Zop1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FADD Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FADD Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Add(Vector<float> left, Vector<float> right) => Add(left, right);

        /// <summary>
        /// svfloat64_t svadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FADD Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   FADD Zresult.D, Zop1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FADD Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FADD Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Add(Vector<double> left, Vector<double> right) => Add(left, right);


        ///  AddAcross : Add reduction

        /// <summary>
        /// int64_t svaddv[_s8](svbool_t pg, svint8_t op)
        ///   SADDV Dresult, Pg, Zop.B
        /// </summary>
        public static unsafe long AddAcross(Vector<sbyte> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s16](svbool_t pg, svint16_t op)
        ///   SADDV Dresult, Pg, Zop.H
        /// </summary>
        public static unsafe long AddAcross(Vector<short> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s32](svbool_t pg, svint32_t op)
        ///   SADDV Dresult, Pg, Zop.S
        /// </summary>
        public static unsafe long AddAcross(Vector<int> value) => AddAcross(value);

        /// <summary>
        /// int64_t svaddv[_s64](svbool_t pg, svint64_t op)
        ///   UADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long AddAcross(Vector<long> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u8](svbool_t pg, svuint8_t op)
        ///   UADDV Dresult, Pg, Zop.B
        /// </summary>
        public static unsafe ulong AddAcross(Vector<byte> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u16](svbool_t pg, svuint16_t op)
        ///   UADDV Dresult, Pg, Zop.H
        /// </summary>
        public static unsafe ulong AddAcross(Vector<ushort> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u32](svbool_t pg, svuint32_t op)
        ///   UADDV Dresult, Pg, Zop.S
        /// </summary>
        public static unsafe ulong AddAcross(Vector<uint> value) => AddAcross(value);

        /// <summary>
        /// uint64_t svaddv[_u64](svbool_t pg, svuint64_t op)
        ///   UADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong AddAcross(Vector<ulong> value) => AddAcross(value);

        /// <summary>
        /// float32_t svaddv[_f32](svbool_t pg, svfloat32_t op)
        ///   FADDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float AddAcross(Vector<float> value) => AddAcross(value);

        /// <summary>
        /// float64_t svaddv[_f64](svbool_t pg, svfloat64_t op)
        ///   FADDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double AddAcross(Vector<double> value) => AddAcross(value);


        ///  AddRotateComplex : Complex add with rotate

        /// <summary>
        /// svfloat32_t svcadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation
        /// svfloat32_t svcadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation
        /// svfloat32_t svcadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FCADD Zresult.S, Pg/M, Zresult.S, Zop2.S, #imm_rotation
        /// </summary>
        public static unsafe Vector<float> AddRotateComplex(Vector<float> op1, Vector<float> op2, ulong imm_rotation) => AddRotateComplex(op1, op2, imm_rotation);

        /// <summary>
        /// svfloat64_t svcadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation
        /// svfloat64_t svcadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        ///   FCADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation
        /// svfloat64_t svcadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FCADD Zresult.D, Pg/M, Zresult.D, Zop2.D, #imm_rotation
        /// </summary>
        public static unsafe Vector<double> AddRotateComplex(Vector<double> op1, Vector<double> op2, ulong imm_rotation) => AddRotateComplex(op1, op2, imm_rotation);


        ///  AddSaturate : Saturating add

        /// <summary>
        /// svint8_t svqadd[_s8](svint8_t op1, svint8_t op2)
        ///   SQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

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
        /// svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2)
        ///   UQADD Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

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


        ///  AddSequentialAcross : Add reduction (strictly-ordered)

        /// <summary>
        /// float32_t svadda[_f32](svbool_t pg, float32_t initial, svfloat32_t op)
        ///   FADDA Stied, Pg, Stied, Zop.S
        /// </summary>
        public static unsafe float AddSequentialAcross(float initial, Vector<float> op) => AddSequentialAcross(initial, op);

        /// <summary>
        /// float64_t svadda[_f64](svbool_t pg, float64_t initial, svfloat64_t op)
        ///   FADDA Dtied, Pg, Dtied, Zop.D
        /// </summary>
        public static unsafe double AddSequentialAcross(double initial, Vector<double> op) => AddSequentialAcross(initial, op);


        ///  And : Bitwise AND

        /// <summary>
        /// svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; AND Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   AND Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; AND Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; AND Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right) => And(left, right);

        /// <summary>
        /// svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; AND Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   AND Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; AND Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; AND Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> And(Vector<short> left, Vector<short> right) => And(left, right);

        /// <summary>
        /// svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; AND Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   AND Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; AND Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; AND Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> And(Vector<int> left, Vector<int> right) => And(left, right);

        /// <summary>
        /// svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; AND Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   AND Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; AND Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; AND Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> And(Vector<long> left, Vector<long> right) => And(left, right);

        /// <summary>
        /// svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; AND Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   AND Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   AND Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; AND Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; AND Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> And(Vector<byte> left, Vector<byte> right) => And(left, right);

        /// <summary>
        /// svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; AND Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   AND Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   AND Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; AND Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; AND Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> And(Vector<ushort> left, Vector<ushort> right) => And(left, right);

        /// <summary>
        /// svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; AND Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   AND Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   AND Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; AND Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; AND Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> And(Vector<uint> left, Vector<uint> right) => And(left, right);

        /// <summary>
        /// svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; AND Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   AND Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   AND Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   AND Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; AND Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; AND Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t svand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   AND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> And(Vector<ulong> left, Vector<ulong> right) => And(left, right);


        ///  AndAcross : Bitwise AND reduction to scalar

        /// <summary>
        /// int8_t svandv[_s8](svbool_t pg, svint8_t op)
        ///   ANDV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte AndAcross(Vector<sbyte> value) => AndAcross(value);

        /// <summary>
        /// int16_t svandv[_s16](svbool_t pg, svint16_t op)
        ///   ANDV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short AndAcross(Vector<short> value) => AndAcross(value);

        /// <summary>
        /// int32_t svandv[_s32](svbool_t pg, svint32_t op)
        ///   ANDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int AndAcross(Vector<int> value) => AndAcross(value);

        /// <summary>
        /// int64_t svandv[_s64](svbool_t pg, svint64_t op)
        ///   ANDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long AndAcross(Vector<long> value) => AndAcross(value);

        /// <summary>
        /// uint8_t svandv[_u8](svbool_t pg, svuint8_t op)
        ///   ANDV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte AndAcross(Vector<byte> value) => AndAcross(value);

        /// <summary>
        /// uint16_t svandv[_u16](svbool_t pg, svuint16_t op)
        ///   ANDV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort AndAcross(Vector<ushort> value) => AndAcross(value);

        /// <summary>
        /// uint32_t svandv[_u32](svbool_t pg, svuint32_t op)
        ///   ANDV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint AndAcross(Vector<uint> value) => AndAcross(value);

        /// <summary>
        /// uint64_t svandv[_u64](svbool_t pg, svuint64_t op)
        ///   ANDV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong AndAcross(Vector<ulong> value) => AndAcross(value);


        ///  AndNot : Bitwise NAND

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> AndNot(Vector<sbyte> left, Vector<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> AndNot(Vector<short> left, Vector<short> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> AndNot(Vector<int> left, Vector<int> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> AndNot(Vector<long> left, Vector<long> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> AndNot(Vector<byte> left, Vector<byte> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> AndNot(Vector<ushort> left, Vector<ushort> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> AndNot(Vector<uint> left, Vector<uint> right) => AndNot(left, right);

        /// <summary>
        /// svbool_t svnand[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NAND Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> AndNot(Vector<ulong> left, Vector<ulong> right) => AndNot(left, right);


        ///  BitwiseClear : Bitwise clear

        /// <summary>
        /// svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> BitwiseClear(Vector<short> left, Vector<short> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> BitwiseClear(Vector<int> left, Vector<int> right) => BitwiseClear(left, right);

        /// <summary>
        /// svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> BitwiseClear(Vector<long> left, Vector<long> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; BIC Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; BIC Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; BIC Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right) => BitwiseClear(left, right);

        /// <summary>
        /// svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   BIC Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; BIC Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svbool_t svbic[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BIC Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right) => BitwiseClear(left, right);


        ///  Cnot : Logically invert boolean condition

        /// <summary>
        /// svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   CNOT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.B, Pg/M, Zop.B
        /// svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op)
        ///   CNOT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.B, Pg/M, Zop.B
        /// svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CNOT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> Cnot(Vector<sbyte> value) => Cnot(value);

        /// <summary>
        /// svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   CNOT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.H, Pg/M, Zop.H
        /// svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op)
        ///   CNOT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.H, Pg/M, Zop.H
        /// svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNOT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> Cnot(Vector<short> value) => Cnot(value);

        /// <summary>
        /// svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   CNOT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.S, Pg/M, Zop.S
        /// svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op)
        ///   CNOT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.S, Pg/M, Zop.S
        /// svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CNOT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> Cnot(Vector<int> value) => Cnot(value);

        /// <summary>
        /// svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   CNOT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.D, Pg/M, Zop.D
        /// svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op)
        ///   CNOT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.D, Pg/M, Zop.D
        /// svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CNOT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> Cnot(Vector<long> value) => Cnot(value);

        /// <summary>
        /// svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   CNOT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op)
        ///   CNOT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CNOT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> Cnot(Vector<byte> value) => Cnot(value);

        /// <summary>
        /// svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   CNOT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op)
        ///   CNOT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNOT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> Cnot(Vector<ushort> value) => Cnot(value);

        /// <summary>
        /// svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   CNOT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op)
        ///   CNOT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CNOT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> Cnot(Vector<uint> value) => Cnot(value);

        /// <summary>
        /// svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   CNOT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CNOT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op)
        ///   CNOT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CNOT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CNOT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> Cnot(Vector<ulong> value) => Cnot(value);


        ///  Compact : Shuffle active elements of vector to the right and fill with zero

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
        /// svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op)
        ///   COMPACT Zresult.S, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<uint> Compact(Vector<uint> mask, Vector<uint> value) => Compact(mask, value);

        /// <summary>
        /// svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op)
        ///   COMPACT Zresult.D, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value) => Compact(mask, value);

        /// <summary>
        /// svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op)
        ///   COMPACT Zresult.S, Pg, Zop.S
        /// </summary>
        public static unsafe Vector<float> Compact(Vector<float> mask, Vector<float> value) => Compact(mask, value);

        /// <summary>
        /// svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op)
        ///   COMPACT Zresult.D, Pg, Zop.D
        /// </summary>
        public static unsafe Vector<double> Compact(Vector<double> mask, Vector<double> value) => Compact(mask, value);


        ///  CompareEqual : Compare equal to

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
        /// svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right) => CompareEqual(left, right);

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

        /// <summary>
        /// svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMEQ Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareEqual(Vector<float> left, Vector<float> right) => CompareEqual(left, right);

        /// <summary>
        /// svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMEQ Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareEqual(Vector<double> left, Vector<double> right) => CompareEqual(left, right);


        ///  CompareGreaterThan : Compare greater than

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

        /// <summary>
        /// svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGT Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGT Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right) => CompareGreaterThan(left, right);


        ///  CompareGreaterThanOrEqual : Compare greater than or equal to

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

        /// <summary>
        /// svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => CompareGreaterThanOrEqual(left, right);


        ///  CompareLessThan : Compare less than

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

        /// <summary>
        /// svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGT Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> CompareLessThan(Vector<float> left, Vector<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGT Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> CompareLessThan(Vector<double> left, Vector<double> right) => CompareLessThan(left, right);


        ///  CompareLessThanOrEqual : Compare less than or equal to

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

        /// <summary>
        /// svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMGE Presult.S, Pg/Z, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMGE Presult.D, Pg/Z, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right) => CompareLessThanOrEqual(left, right);


        ///  CompareNotEqualTo : Compare not equal to

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
        /// svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right) => CompareNotEqualTo(left, right);

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

        /// <summary>
        /// svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMNE Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right) => CompareNotEqualTo(left, right);

        /// <summary>
        /// svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMNE Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right) => CompareNotEqualTo(left, right);


        ///  CompareUnordered : Compare unordered with

        /// <summary>
        /// svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FCMUO Presult.S, Pg/Z, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> CompareUnordered(Vector<float> left, Vector<float> right) => CompareUnordered(left, right);

        /// <summary>
        /// svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FCMUO Presult.D, Pg/Z, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> CompareUnordered(Vector<double> left, Vector<double> right) => CompareUnordered(left, right);


        ///  ComputeByteAddresses : Compute vector addresses for 8-bit data

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets)
        ///   ADR Zresult.S, [Zbases.S, Zoffsets.S]
        /// </summary>
        public static unsafe Vector<uint> ComputeByteAddresses(Vector<uint> bases, Vector<int> offsets) => ComputeByteAddresses(bases, offsets);

        /// <summary>
        /// svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets)
        ///   ADR Zresult.S, [Zbases.S, Zoffsets.S]
        /// </summary>
        public static unsafe Vector<uint> ComputeByteAddresses(Vector<uint> bases, Vector<uint> offsets) => ComputeByteAddresses(bases, offsets);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets)
        ///   ADR Zresult.D, [Zbases.D, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> ComputeByteAddresses(Vector<ulong> bases, Vector<long> offsets) => ComputeByteAddresses(bases, offsets);

        /// <summary>
        /// svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets)
        ///   ADR Zresult.D, [Zbases.D, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> ComputeByteAddresses(Vector<ulong> bases, Vector<ulong> offsets) => ComputeByteAddresses(bases, offsets);


        ///  ComputeInt16Addresses : Compute vector addresses for 16-bit data

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt16Addresses(Vector<uint> bases, Vector<int> indices) => ComputeInt16Addresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt16Addresses(Vector<uint> bases, Vector<uint> indices) => ComputeInt16Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt16Addresses(Vector<ulong> bases, Vector<long> indices) => ComputeInt16Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt16Addresses(Vector<ulong> bases, Vector<ulong> indices) => ComputeInt16Addresses(bases, indices);


        ///  ComputeInt32Addresses : Compute vector addresses for 32-bit data

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt32Addresses(Vector<uint> bases, Vector<int> indices) => ComputeInt32Addresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt32Addresses(Vector<uint> bases, Vector<uint> indices) => ComputeInt32Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt32Addresses(Vector<ulong> bases, Vector<long> indices) => ComputeInt32Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt32Addresses(Vector<ulong> bases, Vector<ulong> indices) => ComputeInt32Addresses(bases, indices);


        ///  ComputeInt64Addresses : Compute vector addresses for 64-bit data

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt64Addresses(Vector<uint> bases, Vector<int> indices) => ComputeInt64Addresses(bases, indices);

        /// <summary>
        /// svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)
        ///   ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]
        /// </summary>
        public static unsafe Vector<uint> ComputeInt64Addresses(Vector<uint> bases, Vector<uint> indices) => ComputeInt64Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt64Addresses(Vector<ulong> bases, Vector<long> indices) => ComputeInt64Addresses(bases, indices);

        /// <summary>
        /// svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)
        ///   ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> ComputeInt64Addresses(Vector<ulong> bases, Vector<ulong> indices) => ComputeInt64Addresses(bases, indices);


        ///  ConditionalExtractAfterLastActiveElement : Conditionally extract element after last

        /// <summary>
        /// svint8_t svclasta[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTA Ztied.B, Pg, Ztied.B, Zdata.B
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint16_t svclasta[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> fallback, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint32_t svclasta[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> fallback, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint64_t svclasta[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> fallback, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint8_t svclasta[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTA Ztied.B, Pg, Ztied.B, Zdata.B
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.B, Pg, Zresult.B, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint16_t svclasta[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTA Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint32_t svclasta[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint64_t svclasta[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTA Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> fallback, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTA Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTA Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> fallback, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, fallback, data);


        ///  ConditionalExtractLastActiveElement : Conditionally extract last element

        /// <summary>
        /// svint8_t svclastb[_s8](svbool_t pg, svint8_t fallback, svint8_t data)
        ///   CLASTB Ztied.B, Pg, Ztied.B, Zdata.B
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> fallback, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint16_t svclastb[_s16](svbool_t pg, svint16_t fallback, svint16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> fallback, Vector<short> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint32_t svclastb[_s32](svbool_t pg, svint32_t fallback, svint32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> fallback, Vector<int> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svint64_t svclastb[_s64](svbool_t pg, svint64_t fallback, svint64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> fallback, Vector<long> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint8_t svclastb[_u8](svbool_t pg, svuint8_t fallback, svuint8_t data)
        ///   CLASTB Ztied.B, Pg, Ztied.B, Zdata.B
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.B, Pg, Zresult.B, Zdata.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> fallback, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint16_t svclastb[_u16](svbool_t pg, svuint16_t fallback, svuint16_t data)
        ///   CLASTB Ztied.H, Pg, Ztied.H, Zdata.H
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.H, Pg, Zresult.H, Zdata.H
        /// </summary>
        public static unsafe Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> fallback, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint32_t svclastb[_u32](svbool_t pg, svuint32_t fallback, svuint32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> fallback, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svuint64_t svclastb[_u64](svbool_t pg, svuint64_t fallback, svuint64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> fallback, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t fallback, svfloat32_t data)
        ///   CLASTB Ztied.S, Pg, Ztied.S, Zdata.S
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.S, Pg, Zresult.S, Zdata.S
        /// </summary>
        public static unsafe Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> fallback, Vector<float> data) => ConditionalExtractLastActiveElement(mask, fallback, data);

        /// <summary>
        /// svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t fallback, svfloat64_t data)
        ///   CLASTB Ztied.D, Pg, Ztied.D, Zdata.D
        ///   MOVPRFX Zresult, Zfallback; CLASTB Zresult.D, Pg, Zresult.D, Zdata.D
        /// </summary>
        public static unsafe Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> fallback, Vector<double> data) => ConditionalExtractLastActiveElement(mask, fallback, data);


        ///  ConditionalSelect : Conditionally select elements

        /// <summary>
        /// svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SEL Zresult.B, Pg, Zop1.B, Zop2.B
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SEL Zresult.B, Pg, Zop1.B, Zop2.B
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SEL Zresult.H, Pg, Zop1.H, Zop2.H
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        /// svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)
        ///   SEL Presult.B, Pg, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   SEL Zresult.S, Pg, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        /// svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   SEL Zresult.D, Pg, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right) => ConditionalSelect(mask, left, right);


        ///  ConvertToDouble : Floating-point convert

        /// <summary>
        /// svfloat64_t svcvt_f64[_s32]_m(svfloat64_t inactive, svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.D, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_s32]_x(svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.D, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.D, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<int> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_s64]_m(svfloat64_t inactive, svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svcvt_f64[_s64]_x(svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svcvt_f64[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<long> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_u32]_m(svfloat64_t inactive, svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.D, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_u32]_x(svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.D, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.D, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<uint> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_u64]_m(svfloat64_t inactive, svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svcvt_f64[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svcvt_f64[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<ulong> value) => ConvertToDouble(value);

        /// <summary>
        /// svfloat64_t svcvt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVT Ztied.D, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVT Ztied.D, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.D, Pg/M, Zop.S
        /// svfloat64_t svcvt_f64[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.D, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<double> ConvertToDouble(Vector<float> value) => ConvertToDouble(value);


        ///  ConvertToInt32 : Floating-point convert

        /// <summary>
        /// svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.S
        /// svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.S
        /// svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZS Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<float> value) => ConvertToInt32(value);

        /// <summary>
        /// svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.S, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.S, Pg/M, Zop.D
        /// svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.S, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.S, Pg/M, Zop.D
        /// svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.S, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<int> ConvertToInt32(Vector<double> value) => ConvertToInt32(value);


        ///  ConvertToInt64 : Floating-point convert

        /// <summary>
        /// svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.D, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.S
        /// svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZS Ztied.D, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.S
        /// svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<float> value) => ConvertToInt64(value);

        /// <summary>
        /// svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVTZS Zresult.D, Pg/M, Zop.D
        /// svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZS Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVTZS Zresult.D, Pg/M, Zop.D
        /// svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZS Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ConvertToInt64(Vector<double> value) => ConvertToInt64(value);


        ///  ConvertToSingle : Floating-point convert

        /// <summary>
        /// svfloat32_t svcvt_f32[_s32]_m(svfloat32_t inactive, svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svcvt_f32[_s32]_x(svbool_t pg, svint32_t op)
        ///   SCVTF Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svcvt_f32[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; SCVTF Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<int> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_s64]_m(svfloat32_t inactive, svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.S, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; SCVTF Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_s64]_x(svbool_t pg, svint64_t op)
        ///   SCVTF Ztied.S, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; SCVTF Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; SCVTF Zresult.S, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<long> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_u32]_m(svfloat32_t inactive, svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svcvt_f32[_u32]_x(svbool_t pg, svuint32_t op)
        ///   UCVTF Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svcvt_f32[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; UCVTF Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<uint> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_u64]_m(svfloat32_t inactive, svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.S, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; UCVTF Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_u64]_x(svbool_t pg, svuint64_t op)
        ///   UCVTF Ztied.S, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; UCVTF Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; UCVTF Zresult.S, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<ulong> value) => ConvertToSingle(value);

        /// <summary>
        /// svfloat32_t svcvt_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVT Ztied.S, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVT Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVT Ztied.S, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVT Zresult.S, Pg/M, Zop.D
        /// svfloat32_t svcvt_f32[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVT Zresult.S, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<float> ConvertToSingle(Vector<double> value) => ConvertToSingle(value);


        ///  ConvertToUInt32 : Floating-point convert

        /// <summary>
        /// svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FCVTZU Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<float> value) => ConvertToUInt32(value);

        /// <summary>
        /// svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.S, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.S, Pg/M, Zop.D
        /// svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.S, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.S, Pg/M, Zop.D
        /// svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.S, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<uint> ConvertToUInt32(Vector<double> value) => ConvertToUInt32(value);


        ///  ConvertToUInt64 : Floating-point convert

        /// <summary>
        /// svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.D, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.S
        /// svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FCVTZU Ztied.D, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.S
        /// svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<float> value) => ConvertToUInt64(value);

        /// <summary>
        /// svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FCVTZU Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FCVTZU Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FCVTZU Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FCVTZU Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ConvertToUInt64(Vector<double> value) => ConvertToUInt64(value);


        ///  Count16BitElements : Count the number of 16-bit elements in a vector

        /// <summary>
        /// uint64_t svcnth()
        ///   CNTH Xresult, ALL
        /// </summary>
        public static unsafe ulong Count16BitElements() => Count16BitElements();

        /// <summary>
        /// uint64_t svcnth_pat(enum svpattern pattern)
        ///   CNTH Xresult, pattern
        /// </summary>
        public static unsafe ulong Count16BitElements(enum SveMaskPattern pattern) => Count16BitElements(SveMaskPattern);


        ///  Count32BitElements : Count the number of 32-bit elements in a vector

        /// <summary>
        /// uint64_t svcntw()
        ///   CNTW Xresult, ALL
        /// </summary>
        public static unsafe ulong Count32BitElements() => Count32BitElements();

        /// <summary>
        /// uint64_t svcntw_pat(enum svpattern pattern)
        ///   CNTW Xresult, pattern
        /// </summary>
        public static unsafe ulong Count32BitElements(enum SveMaskPattern pattern) => Count32BitElements(SveMaskPattern);


        ///  Count64BitElements : Count the number of 64-bit elements in a vector

        /// <summary>
        /// uint64_t svcntd()
        ///   CNTD Xresult, ALL
        /// </summary>
        public static unsafe ulong Count64BitElements() => Count64BitElements();

        /// <summary>
        /// uint64_t svcntd_pat(enum svpattern pattern)
        ///   CNTD Xresult, pattern
        /// </summary>
        public static unsafe ulong Count64BitElements(enum SveMaskPattern pattern) => Count64BitElements(SveMaskPattern);


        ///  Count8BitElements : Count the number of 8-bit elements in a vector

        /// <summary>
        /// uint64_t svcntb()
        ///   CNTB Xresult, ALL
        /// </summary>
        public static unsafe ulong Count8BitElements() => Count8BitElements();

        /// <summary>
        /// uint64_t svcntb_pat(enum svpattern pattern)
        ///   CNTB Xresult, pattern
        /// </summary>
        public static unsafe ulong Count8BitElements(enum SveMaskPattern pattern) => Count8BitElements(SveMaskPattern);



        ///  CreateBreakAfterMask : Break after first true condition

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> mask, Vector<sbyte> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakAfterMask(Vector<short> mask, Vector<short> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakAfterMask(Vector<int> mask, Vector<int> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakAfterMask(Vector<long> mask, Vector<long> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakAfterMask(Vector<byte> mask, Vector<byte> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakAfterMask(Vector<ushort> mask, Vector<ushort> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakAfterMask(Vector<uint> mask, Vector<uint> from) => CreateBreakAfterMask(mask, from);

        /// <summary>
        /// svbool_t svbrka[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKA Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKA Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakAfterMask(Vector<ulong> mask, Vector<ulong> from) => CreateBreakAfterMask(mask, from);


        ///  CreateBreakAfterPropagateMask : Break after first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakAfterPropagateMask(mask, left, right);

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
        public static unsafe Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakAfterPropagateMask(mask, left, right);

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


        ///  CreateBreakBeforeMask : Break before first true condition

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> mask, Vector<sbyte> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<short> CreateBreakBeforeMask(Vector<short> mask, Vector<short> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<int> CreateBreakBeforeMask(Vector<int> mask, Vector<int> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<long> CreateBreakBeforeMask(Vector<long> mask, Vector<long> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<byte> CreateBreakBeforeMask(Vector<byte> mask, Vector<byte> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ushort> CreateBreakBeforeMask(Vector<ushort> mask, Vector<ushort> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<uint> CreateBreakBeforeMask(Vector<uint> mask, Vector<uint> from) => CreateBreakBeforeMask(mask, from);

        /// <summary>
        /// svbool_t svbrkb[_b]_m(svbool_t inactive, svbool_t pg, svbool_t op)
        ///   BRKB Ptied.B, Pg/M, Pop.B
        /// svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)
        ///   BRKB Presult.B, Pg/Z, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> CreateBreakBeforeMask(Vector<ulong> mask, Vector<ulong> from) => CreateBreakBeforeMask(mask, from);


        ///  CreateBreakBeforePropagateMask : Break before first true condition, propagating from previous partition

        /// <summary>
        /// svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakBeforePropagateMask(mask, left, right);

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
        public static unsafe Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakBeforePropagateMask(mask, left, right);

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


        ///  CreateSeries : Create linear series

        /// <summary>
        /// svint8_t svindex_s8(int8_t base, int8_t step)
        ///   INDEX Zresult.B, #base, #step
        ///   INDEX Zresult.B, #base, Wstep
        ///   INDEX Zresult.B, Wbase, #step
        ///   INDEX Zresult.B, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<sbyte> CreateSeries(sbyte base, sbyte step) => CreateSeries(base, step);

        /// <summary>
        /// svint16_t svindex_s16(int16_t base, int16_t step)
        ///   INDEX Zresult.H, #base, #step
        ///   INDEX Zresult.H, #base, Wstep
        ///   INDEX Zresult.H, Wbase, #step
        ///   INDEX Zresult.H, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<short> CreateSeries(short base, short step) => CreateSeries(base, step);

        /// <summary>
        /// svint32_t svindex_s32(int32_t base, int32_t step)
        ///   INDEX Zresult.S, #base, #step
        ///   INDEX Zresult.S, #base, Wstep
        ///   INDEX Zresult.S, Wbase, #step
        ///   INDEX Zresult.S, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<int> CreateSeries(int base, int step) => CreateSeries(base, step);

        /// <summary>
        /// svint64_t svindex_s64(int64_t base, int64_t step)
        ///   INDEX Zresult.D, #base, #step
        ///   INDEX Zresult.D, #base, Xstep
        ///   INDEX Zresult.D, Xbase, #step
        ///   INDEX Zresult.D, Xbase, Xstep
        /// </summary>
        public static unsafe Vector<long> CreateSeries(long base, long step) => CreateSeries(base, step);

        /// <summary>
        /// svuint8_t svindex_u8(uint8_t base, uint8_t step)
        ///   INDEX Zresult.B, #base, #step
        ///   INDEX Zresult.B, #base, Wstep
        ///   INDEX Zresult.B, Wbase, #step
        ///   INDEX Zresult.B, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<byte> CreateSeries(byte base, byte step) => CreateSeries(base, step);

        /// <summary>
        /// svuint16_t svindex_u16(uint16_t base, uint16_t step)
        ///   INDEX Zresult.H, #base, #step
        ///   INDEX Zresult.H, #base, Wstep
        ///   INDEX Zresult.H, Wbase, #step
        ///   INDEX Zresult.H, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<ushort> CreateSeries(ushort base, ushort step) => CreateSeries(base, step);

        /// <summary>
        /// svuint32_t svindex_u32(uint32_t base, uint32_t step)
        ///   INDEX Zresult.S, #base, #step
        ///   INDEX Zresult.S, #base, Wstep
        ///   INDEX Zresult.S, Wbase, #step
        ///   INDEX Zresult.S, Wbase, Wstep
        /// </summary>
        public static unsafe Vector<uint> CreateSeries(uint base, uint step) => CreateSeries(base, step);

        /// <summary>
        /// svuint64_t svindex_u64(uint64_t base, uint64_t step)
        ///   INDEX Zresult.D, #base, #step
        ///   INDEX Zresult.D, #base, Xstep
        ///   INDEX Zresult.D, Xbase, #step
        ///   INDEX Zresult.D, Xbase, Xstep
        /// </summary>
        public static unsafe Vector<ulong> CreateSeries(ulong base, ulong step) => CreateSeries(base, step);


        ///  CreateWhileLessThanMask : While incrementing scalar is less than

        /// <summary>
        /// svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask(int left, int right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask(long left, long right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask(uint left, uint right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanMask(ulong left, ulong right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask(int left, int right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask(long left, long right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask(uint left, uint right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanMask(ulong left, ulong right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask(int left, int right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask(long left, long right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask(uint left, uint right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanMask(ulong left, ulong right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2)
        ///   WHILELT Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask(int left, int right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2)
        ///   WHILELT Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask(long left, long right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELO Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask(uint left, uint right) => CreateWhileLessThanMask(left, right);

        /// <summary>
        /// svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELO Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanMask(ulong left, ulong right) => CreateWhileLessThanMask(left, right);


        ///  CreateWhileLessThanOrEqualMask : While incrementing scalar is less than or equal to

        /// <summary>
        /// svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask(int left, int right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask(long left, long right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.B, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask(uint left, uint right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.B, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<byte> CreateWhileLessThanOrEqualMask(ulong left, ulong right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask(int left, int right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask(long left, long right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.H, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask(uint left, uint right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.H, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ushort> CreateWhileLessThanOrEqualMask(ulong left, ulong right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask(int left, int right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask(long left, long right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.S, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask(uint left, uint right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.S, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<uint> CreateWhileLessThanOrEqualMask(ulong left, ulong right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2)
        ///   WHILELE Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask(int left, int right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2)
        ///   WHILELE Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask(long left, long right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2)
        ///   WHILELS Presult.D, Wop1, Wop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask(uint left, uint right) => CreateWhileLessThanOrEqualMask(left, right);

        /// <summary>
        /// svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2)
        ///   WHILELS Presult.D, Xop1, Xop2
        /// </summary>
        public static unsafe Vector<ulong> CreateWhileLessThanOrEqualMask(ulong left, ulong right) => CreateWhileLessThanOrEqualMask(left, right);


        ///  Divide : Divide

        /// <summary>
        /// svint32_t svdiv[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svdiv[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svdiv[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Divide(Vector<int> left, Vector<int> right) => Divide(left, right);

        /// <summary>
        /// svint64_t svdiv[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svdiv[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svdiv[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Divide(Vector<long> left, Vector<long> right) => Divide(left, right);

        /// <summary>
        /// svuint32_t svdiv[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svdiv[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svdiv[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; UDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; UDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Divide(Vector<uint> left, Vector<uint> right) => Divide(left, right);

        /// <summary>
        /// svuint64_t svdiv[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svdiv[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svdiv[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; UDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; UDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Divide(Vector<ulong> left, Vector<ulong> right) => Divide(left, right);

        /// <summary>
        /// svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FDIVR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FDIV Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FDIVR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Divide(Vector<float> left, Vector<float> right) => Divide(left, right);

        /// <summary>
        /// svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FDIVR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FDIV Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FDIVR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Divide(Vector<double> left, Vector<double> right) => Divide(left, right);



        ///  DotProduct : Dot product

        /// <summary>
        /// svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3)
        ///   SDOT Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; SDOT Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<int> DotProduct(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3) => DotProduct(op1, op2, op3);

        /// <summary>
        /// svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index)
        ///   SDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; SDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<int> DotProduct(Vector<int> op1, Vector<sbyte> op2, Vector<sbyte> op3, ulong imm_index) => DotProduct(op1, op2, op3, imm_index);

        /// <summary>
        /// svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3)
        ///   SDOT Ztied1.D, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; SDOT Zresult.D, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<long> DotProduct(Vector<long> op1, Vector<short> op2, Vector<short> op3) => DotProduct(op1, op2, op3);

        /// <summary>
        /// svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)
        ///   SDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; SDOT Zresult.D, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<long> DotProduct(Vector<long> op1, Vector<short> op2, Vector<short> op3, ulong imm_index) => DotProduct(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)
        ///   UDOT Ztied1.S, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; UDOT Zresult.S, Zop2.B, Zop3.B
        /// </summary>
        public static unsafe Vector<uint> DotProduct(Vector<uint> op1, Vector<byte> op2, Vector<byte> op3) => DotProduct(op1, op2, op3);

        /// <summary>
        /// svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index)
        ///   UDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]
        ///   MOVPRFX Zresult, Zop1; UDOT Zresult.S, Zop2.B, Zop3.B[imm_index]
        /// </summary>
        public static unsafe Vector<uint> DotProduct(Vector<uint> op1, Vector<byte> op2, Vector<byte> op3, ulong imm_index) => DotProduct(op1, op2, op3, imm_index);

        /// <summary>
        /// svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3)
        ///   UDOT Ztied1.D, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; UDOT Zresult.D, Zop2.H, Zop3.H
        /// </summary>
        public static unsafe Vector<ulong> DotProduct(Vector<ulong> op1, Vector<ushort> op2, Vector<ushort> op3) => DotProduct(op1, op2, op3);

        /// <summary>
        /// svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)
        ///   UDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]
        ///   MOVPRFX Zresult, Zop1; UDOT Zresult.D, Zop2.H, Zop3.H[imm_index]
        /// </summary>
        public static unsafe Vector<ulong> DotProduct(Vector<ulong> op1, Vector<ushort> op2, Vector<ushort> op3, ulong imm_index) => DotProduct(op1, op2, op3, imm_index);


        ///  DuplicateSelectedScalarToVector : Broadcast a scalar value

        /// <summary>
        /// svint8_t svdup[_n]_s8(int8_t op)
        ///   DUP Zresult.B, #op
        ///   FDUP Zresult.B, #op
        ///   DUPM Zresult.B, #op
        ///   DUP Zresult.B, Wop
        ///   DUP Zresult.B, Zop.B[0]
        /// svint8_t svdup[_n]_s8_m(svint8_t inactive, svbool_t pg, int8_t op)
        ///   CPY Ztied.B, Pg/M, #op
        ///   FCPY Ztied.B, Pg/M, #op
        ///   CPY Ztied.B, Pg/M, Wop
        ///   CPY Ztied.B, Pg/M, Bop
        /// svint8_t svdup[_n]_s8_x(svbool_t pg, int8_t op)
        ///   CPY Zresult.B, Pg/Z, #op
        ///   DUP Zresult.B, #op
        ///   FCPY Zresult.B, Pg/M, #op
        ///   FDUP Zresult.B, #op
        ///   DUPM Zresult.B, #op
        ///   DUP Zresult.B, Wop
        ///   DUP Zresult.B, Zop.B[0]
        /// svint8_t svdup[_n]_s8_z(svbool_t pg, int8_t op)
        ///   CPY Zresult.B, Pg/Z, #op
        ///   DUP Zresult.B, #0; FCPY Zresult.B, Pg/M, #op
        ///   DUP Zresult.B, #0; CPY Zresult.B, Pg/M, Wop
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CPY Zresult.B, Pg/M, Bop
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(sbyte value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svint8_t svdup_lane[_s8](svint8_t data, uint8_t index)
        ///   DUP Zresult.B, Zdata.B[index]
        ///   TBL Zresult.B, Zdata.B, Zindex.B
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint8_t svdupq_lane[_s8](svint8_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint16_t svdup[_n]_s16(int16_t op)
        ///   DUP Zresult.H, #op
        ///   FDUP Zresult.H, #op
        ///   DUPM Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svint16_t svdup[_n]_s16_m(svint16_t inactive, svbool_t pg, int16_t op)
        ///   CPY Ztied.H, Pg/M, #op
        ///   FCPY Ztied.H, Pg/M, #op
        ///   CPY Ztied.H, Pg/M, Wop
        ///   CPY Ztied.H, Pg/M, Hop
        /// svint16_t svdup[_n]_s16_x(svbool_t pg, int16_t op)
        ///   CPY Zresult.H, Pg/Z, #op
        ///   DUP Zresult.H, #op
        ///   FCPY Zresult.H, Pg/M, #op
        ///   FDUP Zresult.H, #op
        ///   DUPM Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svint16_t svdup[_n]_s16_z(svbool_t pg, int16_t op)
        ///   CPY Zresult.H, Pg/Z, #op
        ///   DUP Zresult.H, #0; FCPY Zresult.H, Pg/M, #op
        ///   DUP Zresult.H, #0; CPY Zresult.H, Pg/M, Wop
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CPY Zresult.H, Pg/M, Hop
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(short value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svint16_t svdup_lane[_s16](svint16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, ushort index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint16_t svdupq_lane[_s16](svint16_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint32_t svdup[_n]_s32(int32_t op)
        ///   DUP Zresult.S, #op
        ///   FDUP Zresult.S, #op
        ///   DUPM Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svint32_t svdup[_n]_s32_m(svint32_t inactive, svbool_t pg, int32_t op)
        ///   CPY Ztied.S, Pg/M, #op
        ///   FCPY Ztied.S, Pg/M, #op
        ///   CPY Ztied.S, Pg/M, Wop
        ///   CPY Ztied.S, Pg/M, Sop
        /// svint32_t svdup[_n]_s32_x(svbool_t pg, int32_t op)
        ///   CPY Zresult.S, Pg/Z, #op
        ///   DUP Zresult.S, #op
        ///   FCPY Zresult.S, Pg/M, #op
        ///   FDUP Zresult.S, #op
        ///   DUPM Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svint32_t svdup[_n]_s32_z(svbool_t pg, int32_t op)
        ///   CPY Zresult.S, Pg/Z, #op
        ///   DUP Zresult.S, #0; FCPY Zresult.S, Pg/M, #op
        ///   DUP Zresult.S, #0; CPY Zresult.S, Pg/M, Wop
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CPY Zresult.S, Pg/M, Sop
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(int value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svint32_t svdup_lane[_s32](svint32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        ///   TBL Zresult.S, Zdata.S, Zindex.S
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, uint index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint32_t svdupq_lane[_s32](svint32_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svint64_t svdup[_n]_s64(int64_t op)
        ///   DUP Zresult.D, #op
        ///   FDUP Zresult.D, #op
        ///   DUPM Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svint64_t svdup[_n]_s64_m(svint64_t inactive, svbool_t pg, int64_t op)
        ///   CPY Ztied.D, Pg/M, #op
        ///   FCPY Ztied.D, Pg/M, #op
        ///   CPY Ztied.D, Pg/M, Xop
        ///   CPY Ztied.D, Pg/M, Dop
        /// svint64_t svdup[_n]_s64_x(svbool_t pg, int64_t op)
        ///   CPY Zresult.D, Pg/Z, #op
        ///   DUP Zresult.D, #op
        ///   FCPY Zresult.D, Pg/M, #op
        ///   FDUP Zresult.D, #op
        ///   DUPM Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svint64_t svdup[_n]_s64_z(svbool_t pg, int64_t op)
        ///   CPY Zresult.D, Pg/Z, #op
        ///   DUP Zresult.D, #0; FCPY Zresult.D, Pg/M, #op
        ///   DUP Zresult.D, #0; CPY Zresult.D, Pg/M, Xop
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CPY Zresult.D, Pg/M, Dop
        /// </summary>
        public static unsafe Vector<long> DuplicateSelectedScalarToVector(long value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svint64_t svdup_lane[_s64](svint64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        ///   TBL Zresult.D, Zdata.D, Zindex.D
        /// svint64_t svdupq_lane[_s64](svint64_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint8_t svdup[_n]_u8(uint8_t op)
        ///   DUP Zresult.B, #op
        ///   FDUP Zresult.B, #op
        ///   DUPM Zresult.B, #op
        ///   DUP Zresult.B, Wop
        ///   DUP Zresult.B, Zop.B[0]
        /// svuint8_t svdup[_n]_u8_m(svuint8_t inactive, svbool_t pg, uint8_t op)
        ///   CPY Ztied.B, Pg/M, #(int8_t)op
        ///   FCPY Ztied.B, Pg/M, #op
        ///   CPY Ztied.B, Pg/M, Wop
        ///   CPY Ztied.B, Pg/M, Bop
        /// svuint8_t svdup[_n]_u8_x(svbool_t pg, uint8_t op)
        ///   CPY Zresult.B, Pg/Z, #(int8_t)op
        ///   DUP Zresult.B, #op
        ///   FCPY Zresult.B, Pg/M, #op
        ///   FDUP Zresult.B, #op
        ///   DUPM Zresult.B, #op
        ///   DUP Zresult.B, Wop
        ///   DUP Zresult.B, Zop.B[0]
        /// svuint8_t svdup[_n]_u8_z(svbool_t pg, uint8_t op)
        ///   CPY Zresult.B, Pg/Z, #(int8_t)op
        ///   DUP Zresult.B, #0; FCPY Zresult.B, Pg/M, #op
        ///   DUP Zresult.B, #0; CPY Zresult.B, Pg/M, Wop
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CPY Zresult.B, Pg/M, Bop
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(byte value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index)
        ///   DUP Zresult.B, Zdata.B[index]
        ///   TBL Zresult.B, Zdata.B, Zindex.B
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint8_t svdupq_lane[_u8](svuint8_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint16_t svdup[_n]_u16(uint16_t op)
        ///   DUP Zresult.H, #op
        ///   FDUP Zresult.H, #op
        ///   DUPM Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svuint16_t svdup[_n]_u16_m(svuint16_t inactive, svbool_t pg, uint16_t op)
        ///   CPY Ztied.H, Pg/M, #(int16_t)op
        ///   FCPY Ztied.H, Pg/M, #op
        ///   CPY Ztied.H, Pg/M, Wop
        ///   CPY Ztied.H, Pg/M, Hop
        /// svuint16_t svdup[_n]_u16_x(svbool_t pg, uint16_t op)
        ///   CPY Zresult.H, Pg/Z, #(int16_t)op
        ///   DUP Zresult.H, #op
        ///   FCPY Zresult.H, Pg/M, #op
        ///   FDUP Zresult.H, #op
        ///   DUPM Zresult.H, #op
        ///   DUP Zresult.H, Wop
        ///   DUP Zresult.H, Zop.H[0]
        /// svuint16_t svdup[_n]_u16_z(svbool_t pg, uint16_t op)
        ///   CPY Zresult.H, Pg/Z, #(int16_t)op
        ///   DUP Zresult.H, #0; FCPY Zresult.H, Pg/M, #op
        ///   DUP Zresult.H, #0; CPY Zresult.H, Pg/M, Wop
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CPY Zresult.H, Pg/M, Hop
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(ushort value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index)
        ///   DUP Zresult.H, Zdata.H[index]
        ///   TBL Zresult.H, Zdata.H, Zindex.H
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, ushort index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint16_t svdupq_lane[_u16](svuint16_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint32_t svdup[_n]_u32(uint32_t op)
        ///   DUP Zresult.S, #op
        ///   FDUP Zresult.S, #op
        ///   DUPM Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svuint32_t svdup[_n]_u32_m(svuint32_t inactive, svbool_t pg, uint32_t op)
        ///   CPY Ztied.S, Pg/M, #(int32_t)op
        ///   FCPY Ztied.S, Pg/M, #op
        ///   CPY Ztied.S, Pg/M, Wop
        ///   CPY Ztied.S, Pg/M, Sop
        /// svuint32_t svdup[_n]_u32_x(svbool_t pg, uint32_t op)
        ///   CPY Zresult.S, Pg/Z, #(int32_t)op
        ///   DUP Zresult.S, #op
        ///   FCPY Zresult.S, Pg/M, #op
        ///   FDUP Zresult.S, #op
        ///   DUPM Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svuint32_t svdup[_n]_u32_z(svbool_t pg, uint32_t op)
        ///   CPY Zresult.S, Pg/Z, #(int32_t)op
        ///   DUP Zresult.S, #0; FCPY Zresult.S, Pg/M, #op
        ///   DUP Zresult.S, #0; CPY Zresult.S, Pg/M, Wop
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CPY Zresult.S, Pg/M, Sop
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(uint value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        ///   TBL Zresult.S, Zdata.S, Zindex.S
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, uint index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint32_t svdupq_lane[_u32](svuint32_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svuint64_t svdup[_n]_u64(uint64_t op)
        ///   DUP Zresult.D, #op
        ///   FDUP Zresult.D, #op
        ///   DUPM Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svuint64_t svdup[_n]_u64_m(svuint64_t inactive, svbool_t pg, uint64_t op)
        ///   CPY Ztied.D, Pg/M, #(int64_t)op
        ///   FCPY Ztied.D, Pg/M, #op
        ///   CPY Ztied.D, Pg/M, Xop
        ///   CPY Ztied.D, Pg/M, Dop
        /// svuint64_t svdup[_n]_u64_x(svbool_t pg, uint64_t op)
        ///   CPY Zresult.D, Pg/Z, #(int64_t)op
        ///   DUP Zresult.D, #op
        ///   FCPY Zresult.D, Pg/M, #op
        ///   FDUP Zresult.D, #op
        ///   DUPM Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svuint64_t svdup[_n]_u64_z(svbool_t pg, uint64_t op)
        ///   CPY Zresult.D, Pg/Z, #(int64_t)op
        ///   DUP Zresult.D, #0; FCPY Zresult.D, Pg/M, #op
        ///   DUP Zresult.D, #0; CPY Zresult.D, Pg/M, Xop
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CPY Zresult.D, Pg/M, Dop
        /// </summary>
        public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(ulong value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        ///   TBL Zresult.D, Zdata.D, Zindex.D
        /// svuint64_t svdupq_lane[_u64](svuint64_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat32_t svdup[_n]_f32(float32_t op)
        ///   DUP Zresult.S, #op
        ///   FDUP Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svfloat32_t svdup[_n]_f32_m(svfloat32_t inactive, svbool_t pg, float32_t op)
        ///   CPY Ztied.S, Pg/M, #bitcast<int32_t>(op)
        ///   FCPY Ztied.S, Pg/M, #op
        ///   CPY Ztied.S, Pg/M, Wop
        ///   CPY Ztied.S, Pg/M, Sop
        /// svfloat32_t svdup[_n]_f32_x(svbool_t pg, float32_t op)
        ///   CPY Zresult.S, Pg/Z, #bitcast<int32_t>(op)
        ///   DUP Zresult.S, #op
        ///   FCPY Zresult.S, Pg/M, #op
        ///   FDUP Zresult.S, #op
        ///   DUP Zresult.S, Wop
        ///   DUP Zresult.S, Zop.S[0]
        /// svfloat32_t svdup[_n]_f32_z(svbool_t pg, float32_t op)
        ///   CPY Zresult.S, Pg/Z, #bitcast<int32_t>(op)
        ///   DUP Zresult.S, #0; FCPY Zresult.S, Pg/M, #op
        ///   DUP Zresult.S, #0; CPY Zresult.S, Pg/M, Wop
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CPY Zresult.S, Pg/M, Sop
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(float value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index)
        ///   DUP Zresult.S, Zdata.S[index]
        ///   TBL Zresult.S, Zdata.S, Zindex.S
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, uint index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat32_t svdupq_lane[_f32](svfloat32_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, ulong index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        /// svfloat64_t svdup[_n]_f64(float64_t op)
        ///   DUP Zresult.D, #op
        ///   FDUP Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svfloat64_t svdup[_n]_f64_m(svfloat64_t inactive, svbool_t pg, float64_t op)
        ///   CPY Ztied.D, Pg/M, #bitcast<int64_t>(op)
        ///   FCPY Ztied.D, Pg/M, #op
        ///   CPY Ztied.D, Pg/M, Xop
        ///   CPY Ztied.D, Pg/M, Dop
        /// svfloat64_t svdup[_n]_f64_x(svbool_t pg, float64_t op)
        ///   CPY Zresult.D, Pg/Z, #bitcast<int64_t>(op)
        ///   DUP Zresult.D, #op
        ///   FCPY Zresult.D, Pg/M, #op
        ///   FDUP Zresult.D, #op
        ///   DUP Zresult.D, Xop
        ///   DUP Zresult.D, Zop.D[0]
        /// svfloat64_t svdup[_n]_f64_z(svbool_t pg, float64_t op)
        ///   CPY Zresult.D, Pg/Z, #bitcast<int64_t>(op)
        ///   DUP Zresult.D, #0; FCPY Zresult.D, Pg/M, #op
        ///   DUP Zresult.D, #0; CPY Zresult.D, Pg/M, Xop
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CPY Zresult.D, Pg/M, Dop
        /// </summary>
        public static unsafe Vector<double> DuplicateSelectedScalarToVector(double value) => DuplicateSelectedScalarToVector(value);

        /// <summary>
        /// svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index)
        ///   DUP Zresult.D, Zdata.D[index]
        ///   TBL Zresult.D, Zdata.D, Zindex.D
        /// svfloat64_t svdupq_lane[_f64](svfloat64_t data, uint64_t index)
        ///   DUP Zresult.Q, Zdata.Q[index]
        ///   TBL Zresult.D, Zdata.D, Zindices_d.D
        /// </summary>
        public static unsafe Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, ulong index) => DuplicateSelectedScalarToVector(data, index);


        ///  ExtractAfterLast : Extract element after last

        /// <summary>
        /// int8_t svlasta[_s8](svbool_t pg, svint8_t op)
        ///   LASTA Wresult, Pg, Zop.B
        ///   LASTA Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte ExtractAfterLast(Vector<sbyte> value) => ExtractAfterLast(value);

        /// <summary>
        /// int16_t svlasta[_s16](svbool_t pg, svint16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short ExtractAfterLast(Vector<short> value) => ExtractAfterLast(value);

        /// <summary>
        /// int32_t svlasta[_s32](svbool_t pg, svint32_t op)
        ///   LASTA Wresult, Pg, Zop.S
        ///   LASTA Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int ExtractAfterLast(Vector<int> value) => ExtractAfterLast(value);

        /// <summary>
        /// int64_t svlasta[_s64](svbool_t pg, svint64_t op)
        ///   LASTA Xresult, Pg, Zop.D
        ///   LASTA Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long ExtractAfterLast(Vector<long> value) => ExtractAfterLast(value);

        /// <summary>
        /// uint8_t svlasta[_u8](svbool_t pg, svuint8_t op)
        ///   LASTA Wresult, Pg, Zop.B
        ///   LASTA Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte ExtractAfterLast(Vector<byte> value) => ExtractAfterLast(value);

        /// <summary>
        /// uint16_t svlasta[_u16](svbool_t pg, svuint16_t op)
        ///   LASTA Wresult, Pg, Zop.H
        ///   LASTA Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort ExtractAfterLast(Vector<ushort> value) => ExtractAfterLast(value);

        /// <summary>
        /// uint32_t svlasta[_u32](svbool_t pg, svuint32_t op)
        ///   LASTA Wresult, Pg, Zop.S
        ///   LASTA Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint ExtractAfterLast(Vector<uint> value) => ExtractAfterLast(value);

        /// <summary>
        /// uint64_t svlasta[_u64](svbool_t pg, svuint64_t op)
        ///   LASTA Xresult, Pg, Zop.D
        ///   LASTA Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong ExtractAfterLast(Vector<ulong> value) => ExtractAfterLast(value);

        /// <summary>
        /// float32_t svlasta[_f32](svbool_t pg, svfloat32_t op)
        ///   LASTA Wresult, Pg, Zop.S
        ///   LASTA Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float ExtractAfterLast(Vector<float> value) => ExtractAfterLast(value);

        /// <summary>
        /// float64_t svlasta[_f64](svbool_t pg, svfloat64_t op)
        ///   LASTA Xresult, Pg, Zop.D
        ///   LASTA Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double ExtractAfterLast(Vector<double> value) => ExtractAfterLast(value);


        ///  ExtractLast : Extract last element

        /// <summary>
        /// int8_t svlastb[_s8](svbool_t pg, svint8_t op)
        ///   LASTB Wresult, Pg, Zop.B
        ///   LASTB Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte ExtractLast(Vector<sbyte> value) => ExtractLast(value);

        /// <summary>
        /// int16_t svlastb[_s16](svbool_t pg, svint16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short ExtractLast(Vector<short> value) => ExtractLast(value);

        /// <summary>
        /// int32_t svlastb[_s32](svbool_t pg, svint32_t op)
        ///   LASTB Wresult, Pg, Zop.S
        ///   LASTB Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int ExtractLast(Vector<int> value) => ExtractLast(value);

        /// <summary>
        /// int64_t svlastb[_s64](svbool_t pg, svint64_t op)
        ///   LASTB Xresult, Pg, Zop.D
        ///   LASTB Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long ExtractLast(Vector<long> value) => ExtractLast(value);

        /// <summary>
        /// uint8_t svlastb[_u8](svbool_t pg, svuint8_t op)
        ///   LASTB Wresult, Pg, Zop.B
        ///   LASTB Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte ExtractLast(Vector<byte> value) => ExtractLast(value);

        /// <summary>
        /// uint16_t svlastb[_u16](svbool_t pg, svuint16_t op)
        ///   LASTB Wresult, Pg, Zop.H
        ///   LASTB Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort ExtractLast(Vector<ushort> value) => ExtractLast(value);

        /// <summary>
        /// uint32_t svlastb[_u32](svbool_t pg, svuint32_t op)
        ///   LASTB Wresult, Pg, Zop.S
        ///   LASTB Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint ExtractLast(Vector<uint> value) => ExtractLast(value);

        /// <summary>
        /// uint64_t svlastb[_u64](svbool_t pg, svuint64_t op)
        ///   LASTB Xresult, Pg, Zop.D
        ///   LASTB Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong ExtractLast(Vector<ulong> value) => ExtractLast(value);

        /// <summary>
        /// float32_t svlastb[_f32](svbool_t pg, svfloat32_t op)
        ///   LASTB Wresult, Pg, Zop.S
        ///   LASTB Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float ExtractLast(Vector<float> value) => ExtractLast(value);

        /// <summary>
        /// float64_t svlastb[_f64](svbool_t pg, svfloat64_t op)
        ///   LASTB Xresult, Pg, Zop.D
        ///   LASTB Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double ExtractLast(Vector<double> value) => ExtractLast(value);


        ///  ExtractVector : Extract vector from pair of vectors

        /// <summary>
        /// svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3
        /// </summary>
        public static unsafe Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3
        /// </summary>
        public static unsafe Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 2
        /// </summary>
        public static unsafe Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 4
        /// </summary>
        public static unsafe Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, ulong index) => ExtractVector(upper, lower, index);

        /// <summary>
        /// svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        ///   EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8
        ///   MOVPRFX Zresult, Zop1; EXT Zresult.B, Zresult.B, Zop2.B, #imm3 * 8
        /// </summary>
        public static unsafe Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, ulong index) => ExtractVector(upper, lower, index);


        ///  FalseMask : Set all predicate elements to false

        /// <summary>
        /// svbool_t svpfalse[_b]()
        ///   PFALSE Presult.B
        /// </summary>
        public static unsafe Vector<byte> FalseMask() => FalseMask();


        ///  FloatingPointExponentialAccelerator : Floating-point exponential accelerator

        /// <summary>
        /// svfloat32_t svexpa[_f32](svuint32_t op)
        ///   FEXPA Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> FloatingPointExponentialAccelerator(Vector<uint> value) => FloatingPointExponentialAccelerator(value);

        /// <summary>
        /// svfloat64_t svexpa[_f64](svuint64_t op)
        ///   FEXPA Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> FloatingPointExponentialAccelerator(Vector<ulong> value) => FloatingPointExponentialAccelerator(value);


        ///  FusedMultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   FMAD Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   FMAD Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAD Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; FMAD Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.S, Zop2.S, Zop3.S[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right, ulong imm_index) => FusedMultiplyAdd(addend, left, right, imm_index);

        /// <summary>
        /// svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   FMAD Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   FMAD Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAD Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; FMAD Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        ///   FMLA Ztied1.D, Zop2.D, Zop3.D[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLA Zresult.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right, ulong imm_index) => FusedMultiplyAdd(addend, left, right, imm_index);


        ///  FusedMultiplyAddNegate : Negated multiply-add, addend first

        /// <summary>
        /// svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   FNMAD Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   FNMAD Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FNMLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FNMAD Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; FNMAD Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplyAddNegate(Vector<float> op1, Vector<float> op2, Vector<float> op3) => FusedMultiplyAddNegate(op1, op2, op3);

        /// <summary>
        /// svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   FNMAD Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   FNMAD Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FNMLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FNMAD Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; FNMAD Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplyAddNegate(Vector<double> op1, Vector<double> op2, Vector<double> op3) => FusedMultiplyAddNegate(op1, op2, op3);


        ///  FusedMultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   FMSB Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   FMSB Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMSB Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; FMSB Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.S, Zop2.S, Zop3.S[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.S, Zop2.S, Zop3.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right, ulong imm_index) => FusedMultiplySubtract(minuend, left, right, imm_index);

        /// <summary>
        /// svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   FMSB Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   FMSB Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMSB Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; FMSB Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)
        ///   FMLS Ztied1.D, Zop2.D, Zop3.D[imm_index]
        ///   MOVPRFX Zresult, Zop1; FMLS Zresult.D, Zop2.D, Zop3.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right, ulong imm_index) => FusedMultiplySubtract(minuend, left, right, imm_index);


        ///  FusedMultiplySubtractNegate : Negated multiply-subtract, minuend first

        /// <summary>
        /// svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   FNMSB Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   FNMSB Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FNMLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FNMSB Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; FNMSB Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> FusedMultiplySubtractNegate(Vector<float> op1, Vector<float> op2, Vector<float> op3) => FusedMultiplySubtractNegate(op1, op2, op3);

        /// <summary>
        /// svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   FNMSB Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   FNMSB Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FNMLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FNMSB Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; FNMSB Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> FusedMultiplySubtractNegate(Vector<double> op1, Vector<double> op2, Vector<double> op3) => FusedMultiplySubtractNegate(op1, op2, op3);


        ///  GatherPrefetchBytes : Prefetch bytes

        /// <summary>
        /// void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, const void *base, Vector<int> offsets, enum SvePrefetchType op) => GatherPrefetchBytes(mask, void, offsets, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, const void *base, Vector<long> offsets, enum SvePrefetchType op) => GatherPrefetchBytes(mask, void, offsets, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, Vector<uint> bases, enum SvePrefetchType op) => GatherPrefetchBytes(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, const void *base, Vector<uint> offsets, enum SvePrefetchType op) => GatherPrefetchBytes(mask, void, offsets, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather[_u32base]_offset(svbool_t pg, svuint32_t bases, int64_t offset, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.S, #offset]
        ///   PRFB op, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, Vector<uint> bases, long offset, enum SvePrefetchType op) => GatherPrefetchBytes(mask, bases, offset, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, Vector<ulong> bases, enum SvePrefetchType op) => GatherPrefetchBytes(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)
        ///   PRFB op, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, const void *base, Vector<ulong> offsets, enum SvePrefetchType op) => GatherPrefetchBytes(mask, void, offsets, SvePrefetchType);

        /// <summary>
        /// void svprfb_gather[_u64base]_offset(svbool_t pg, svuint64_t bases, int64_t offset, enum svprfop op)
        ///   PRFB op, Pg, [Zbases.D, #offset]
        ///   PRFB op, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void GatherPrefetchBytes(Vector<byte> mask, Vector<ulong> bases, long offset, enum SvePrefetchType op) => GatherPrefetchBytes(mask, bases, offset, SvePrefetchType);


        ///  GatherPrefetchInt16 : Prefetch halfwords

        /// <summary>
        /// void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, const void *base, Vector<int> indices, enum SvePrefetchType op) => GatherPrefetchInt16(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, const void *base, Vector<long> indices, enum SvePrefetchType op) => GatherPrefetchInt16(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, Vector<uint> bases, enum SvePrefetchType op) => GatherPrefetchInt16(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, const void *base, Vector<uint> indices, enum SvePrefetchType op) => GatherPrefetchInt16(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather[_u32base]_index(svbool_t pg, svuint32_t bases, int64_t index, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.S, #index * 2]
        ///   PRFB op, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, Vector<uint> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt16(mask, bases, index, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, Vector<ulong> bases, enum SvePrefetchType op) => GatherPrefetchInt16(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFH op, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, const void *base, Vector<ulong> indices, enum SvePrefetchType op) => GatherPrefetchInt16(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfh_gather[_u64base]_index(svbool_t pg, svuint64_t bases, int64_t index, enum svprfop op)
        ///   PRFH op, Pg, [Zbases.D, #index * 2]
        ///   PRFB op, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void GatherPrefetchInt16(Vector<ushort> mask, Vector<ulong> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt16(mask, bases, index, SvePrefetchType);


        ///  GatherPrefetchInt32 : Prefetch words

        /// <summary>
        /// void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, Vector<uint> bases, enum SvePrefetchType op) => GatherPrefetchInt32(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, const void *base, Vector<int> indices, enum SvePrefetchType op) => GatherPrefetchInt32(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, const void *base, Vector<long> indices, enum SvePrefetchType op) => GatherPrefetchInt32(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, Vector<ulong> bases, enum SvePrefetchType op) => GatherPrefetchInt32(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, const void *base, Vector<uint> indices, enum SvePrefetchType op) => GatherPrefetchInt32(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFW op, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, const void *base, Vector<ulong> indices, enum SvePrefetchType op) => GatherPrefetchInt32(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather[_u32base]_index(svbool_t pg, svuint32_t bases, int64_t index, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.S, #index * 4]
        ///   PRFB op, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, Vector<uint> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt32(mask, bases, index, SvePrefetchType);

        /// <summary>
        /// void svprfw_gather[_u64base]_index(svbool_t pg, svuint64_t bases, int64_t index, enum svprfop op)
        ///   PRFW op, Pg, [Zbases.D, #index * 4]
        ///   PRFB op, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void GatherPrefetchInt32(Vector<uint> mask, Vector<ulong> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt32(mask, bases, index, SvePrefetchType);


        ///  GatherPrefetchInt64 : Prefetch doublewords

        /// <summary>
        /// void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, Vector<uint> bases, enum SvePrefetchType op) => GatherPrefetchInt64(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, Vector<ulong> bases, enum SvePrefetchType op) => GatherPrefetchInt64(mask, bases, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, const void *base, Vector<int> indices, enum SvePrefetchType op) => GatherPrefetchInt64(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, const void *base, Vector<long> indices, enum SvePrefetchType op) => GatherPrefetchInt64(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, const void *base, Vector<uint> indices, enum SvePrefetchType op) => GatherPrefetchInt64(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)
        ///   PRFD op, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, const void *base, Vector<ulong> indices, enum SvePrefetchType op) => GatherPrefetchInt64(mask, void, indices, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather[_u32base]_index(svbool_t pg, svuint32_t bases, int64_t index, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.S, #index * 8]
        ///   PRFB op, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, Vector<uint> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt64(mask, bases, index, SvePrefetchType);

        /// <summary>
        /// void svprfd_gather[_u64base]_index(svbool_t pg, svuint64_t bases, int64_t index, enum svprfop op)
        ///   PRFD op, Pg, [Zbases.D, #index * 8]
        ///   PRFB op, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void GatherPrefetchInt64(Vector<ulong> mask, Vector<ulong> bases, long index, enum SvePrefetchType op) => GatherPrefetchInt64(mask, bases, index, SvePrefetchType);


        ///  GatherVector : Unextended load

        /// <summary>
        /// svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, const int *base, Vector<int> offsets) => GatherVector(mask, int, offsets);

        /// <summary>
        /// svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, const int *base, Vector<uint> offsets) => GatherVector(mask, int, offsets);

        /// <summary>
        /// svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, const int *base, Vector<int> indices) => GatherVector(mask, int, indices);

        /// <summary>
        /// svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, const int *base, Vector<uint> indices) => GatherVector(mask, int, indices);

        /// <summary>
        /// svint32_t svld1_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svint32_t svld1_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, Vector<uint> bases, long index) => GatherVector(mask, bases, index);

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, const long *base, Vector<long> offsets) => GatherVector(mask, long, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, const long *base, Vector<ulong> offsets) => GatherVector(mask, long, offsets);

        /// <summary>
        /// svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, const long *base, Vector<long> indices) => GatherVector(mask, long, indices);

        /// <summary>
        /// svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, const long *base, Vector<ulong> indices) => GatherVector(mask, long, indices);

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, Vector<ulong> bases, long index) => GatherVector(mask, bases, index);

        /// <summary>
        /// svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, const uint *base, Vector<int> offsets) => GatherVector(mask, uint, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, const uint *base, Vector<uint> offsets) => GatherVector(mask, uint, offsets);

        /// <summary>
        /// svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, const uint *base, Vector<int> indices) => GatherVector(mask, uint, indices);

        /// <summary>
        /// svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, const uint *base, Vector<uint> indices) => GatherVector(mask, uint, indices);

        /// <summary>
        /// svuint32_t svld1_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svuint32_t svld1_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> bases, long index) => GatherVector(mask, bases, index);

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, const ulong *base, Vector<long> offsets) => GatherVector(mask, ulong, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, const ulong *base, Vector<ulong> offsets) => GatherVector(mask, ulong, offsets);

        /// <summary>
        /// svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, const ulong *base, Vector<long> indices) => GatherVector(mask, ulong, indices);

        /// <summary>
        /// svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, const ulong *base, Vector<ulong> indices) => GatherVector(mask, ulong, indices);

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVector(mask, bases, index);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, const float *base, Vector<int> offsets) => GatherVector(mask, float, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, const float *base, Vector<int> indices) => GatherVector(mask, float, indices);

        /// <summary>
        /// svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, const float *base, Vector<uint> offsets) => GatherVector(mask, float, offsets);

        /// <summary>
        /// svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        ///   LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, const float *base, Vector<uint> indices) => GatherVector(mask, float, indices);

        /// <summary>
        /// svfloat32_t svld1_gather[_u32base]_offset_f32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svfloat32_t svld1_gather[_u32base]_index_f32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LD1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, Vector<uint> bases, long index) => GatherVector(mask, bases, index);

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, const double *base, Vector<long> offsets) => GatherVector(mask, double, offsets);

        /// <summary>
        /// svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, const double *base, Vector<long> indices) => GatherVector(mask, double, indices);

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> bases) => GatherVector(mask, bases);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, const double *base, Vector<ulong> offsets) => GatherVector(mask, double, offsets);

        /// <summary>
        /// svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        ///   LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, const double *base, Vector<ulong> indices) => GatherVector(mask, double, indices);

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_offset_f64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> bases, long offset) => GatherVector(mask, bases, offset);

        /// <summary>
        /// svfloat64_t svld1_gather[_u64base]_index_f64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LD1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, Vector<ulong> bases, long index) => GatherVector(mask, bases, index);


        ///  GatherVectorByteSignExtend : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtend(Vector<int> mask, Vector<uint> bases) => GatherVectorByteSignExtend(mask, bases);

        /// <summary>
        /// svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtend(Vector<int> mask, const sbyte *base, Vector<int> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtend(Vector<int> mask, const sbyte *base, Vector<uint> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svint32_t svld1sb_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1SB Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1SB Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtend(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorByteSignExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorByteSignExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtend(Vector<long> mask, const sbyte *base, Vector<long> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtend(Vector<long> mask, const sbyte *base, Vector<ulong> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svint64_t svld1sb_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SB Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorByteSignExtend(mask, bases, offset);

        /// <summary>
        /// svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtend(Vector<uint> mask, Vector<uint> bases) => GatherVectorByteSignExtend(mask, bases);

        /// <summary>
        /// svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtend(Vector<uint> mask, const sbyte *base, Vector<int> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtend(Vector<uint> mask, const sbyte *base, Vector<uint> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svuint32_t svld1sb_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1SB Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1SB Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtend(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorByteSignExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorByteSignExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtend(Vector<ulong> mask, const sbyte *base, Vector<long> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtend(Vector<ulong> mask, const sbyte *base, Vector<ulong> offsets) => GatherVectorByteSignExtend(mask, sbyte, offsets);

        /// <summary>
        /// svuint64_t svld1sb_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SB Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SB Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorByteSignExtend(mask, bases, offset);


        ///  GatherVectorByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> bases) => GatherVectorByteSignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint32_t svldff1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtendFirstFaulting(Vector<int> mask, const sbyte *base, Vector<int> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svint32_t svldff1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtendFirstFaulting(Vector<int> mask, const sbyte *base, Vector<uint> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svint32_t svldff1sb_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1SB Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorByteSignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorByteSignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtendFirstFaulting(Vector<long> mask, const sbyte *base, Vector<long> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svint64_t svldff1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtendFirstFaulting(Vector<long> mask, const sbyte *base, Vector<ulong> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svint64_t svldff1sb_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SB Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorByteSignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint32_t svldff1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases) => GatherVectorByteSignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint32_t svldff1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtendFirstFaulting(Vector<uint> mask, const sbyte *base, Vector<int> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svuint32_t svldff1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtendFirstFaulting(Vector<uint> mask, const sbyte *base, Vector<uint> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svuint32_t svldff1sb_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1SB Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorByteSignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorByteSignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtendFirstFaulting(Vector<ulong> mask, const sbyte *base, Vector<long> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svuint64_t svldff1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtendFirstFaulting(Vector<ulong> mask, const sbyte *base, Vector<ulong> offsets) => GatherVectorByteSignExtendFirstFaulting(mask, sbyte, offsets);

        /// <summary>
        /// svuint64_t svldff1sb_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SB Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorByteSignExtendFirstFaulting(mask, bases, offset);


        ///  GatherVectorByteZeroExtend : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> bases) => GatherVectorByteZeroExtend(mask, bases);

        /// <summary>
        /// svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, const byte *base, Vector<int> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, const byte *base, Vector<uint> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svint32_t svld1ub_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1B Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1B Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorByteZeroExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorByteZeroExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, const byte *base, Vector<long> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, const byte *base, Vector<ulong> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svint64_t svld1ub_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1B Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorByteZeroExtend(mask, bases, offset);

        /// <summary>
        /// svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> bases) => GatherVectorByteZeroExtend(mask, bases);

        /// <summary>
        /// svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, const byte *base, Vector<int> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, const byte *base, Vector<uint> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svuint32_t svld1ub_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1B Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1B Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorByteZeroExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorByteZeroExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, const byte *base, Vector<long> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, const byte *base, Vector<ulong> offsets) => GatherVectorByteZeroExtend(mask, byte, offsets);

        /// <summary>
        /// svuint64_t svld1ub_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1B Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1B Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorByteZeroExtend(mask, bases, offset);


        ///  GatherVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> bases) => GatherVectorByteZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint32_t svldff1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, const byte *base, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svint32_t svldff1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, const byte *base, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svint32_t svldff1ub_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1B Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorByteZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorByteZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, const byte *base, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svint64_t svldff1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, const byte *base, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svint64_t svldff1ub_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1B Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1B Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorByteZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint32_t svldff1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases) => GatherVectorByteZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint32_t svldff1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, const byte *base, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svuint32_t svldff1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, const byte *base, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svuint32_t svldff1ub_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1B Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorByteZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorByteZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, const byte *base, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svuint64_t svldff1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, const byte *base, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, byte, offsets);

        /// <summary>
        /// svuint64_t svldff1ub_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1B Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1B Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorByteZeroExtendFirstFaulting(mask, bases, offset);


        ///  GatherVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint32_t svldff1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svint32_t svldff1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, const int *base, Vector<int> offsets) => GatherVectorFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svint32_t svldff1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, const int *base, Vector<uint> offsets) => GatherVectorFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svint32_t svldff1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, const int *base, Vector<int> indices) => GatherVectorFirstFaulting(mask, int, indices);

        /// <summary>
        /// svint32_t svldff1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, const int *base, Vector<uint> indices) => GatherVectorFirstFaulting(mask, int, indices);

        /// <summary>
        /// svint32_t svldff1_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint32_t svldff1_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);

        /// <summary>
        /// svint64_t svldff1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, const long *base, Vector<long> offsets) => GatherVectorFirstFaulting(mask, long, offsets);

        /// <summary>
        /// svint64_t svldff1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, const long *base, Vector<ulong> offsets) => GatherVectorFirstFaulting(mask, long, offsets);

        /// <summary>
        /// svint64_t svldff1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, const long *base, Vector<long> indices) => GatherVectorFirstFaulting(mask, long, indices);

        /// <summary>
        /// svint64_t svldff1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, const long *base, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, long, indices);

        /// <summary>
        /// svint64_t svldff1_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint32_t svldff1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, const uint *base, Vector<int> offsets) => GatherVectorFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, const uint *base, Vector<uint> offsets) => GatherVectorFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svuint32_t svldff1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, const uint *base, Vector<int> indices) => GatherVectorFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svuint32_t svldff1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, const uint *base, Vector<uint> indices) => GatherVectorFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svuint32_t svldff1_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint32_t svldff1_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint64_t svldff1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, const ulong *base, Vector<long> offsets) => GatherVectorFirstFaulting(mask, ulong, offsets);

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, const ulong *base, Vector<ulong> offsets) => GatherVectorFirstFaulting(mask, ulong, offsets);

        /// <summary>
        /// svuint64_t svldff1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, const ulong *base, Vector<long> indices) => GatherVectorFirstFaulting(mask, ulong, indices);

        /// <summary>
        /// svuint64_t svldff1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, const ulong *base, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, ulong, indices);

        /// <summary>
        /// svuint64_t svldff1_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, const float *base, Vector<int> offsets) => GatherVectorFirstFaulting(mask, float, offsets);

        /// <summary>
        /// svfloat32_t svldff1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, const float *base, Vector<int> indices) => GatherVectorFirstFaulting(mask, float, indices);

        /// <summary>
        /// svfloat32_t svldff1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, const float *base, Vector<uint> offsets) => GatherVectorFirstFaulting(mask, float, offsets);

        /// <summary>
        /// svfloat32_t svldff1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, const float *base, Vector<uint> indices) => GatherVectorFirstFaulting(mask, float, indices);

        /// <summary>
        /// svfloat32_t svldff1_gather[_u32base]_offset_f32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svfloat32_t svldff1_gather[_u32base]_index_f32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #index * 4]
        ///   LDFF1W Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, const double *base, Vector<long> offsets) => GatherVectorFirstFaulting(mask, double, offsets);

        /// <summary>
        /// svfloat64_t svldff1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, const double *base, Vector<long> indices) => GatherVectorFirstFaulting(mask, double, indices);

        /// <summary>
        /// svfloat64_t svldff1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> bases) => GatherVectorFirstFaulting(mask, bases);

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, const double *base, Vector<ulong> offsets) => GatherVectorFirstFaulting(mask, double, offsets);

        /// <summary>
        /// svfloat64_t svldff1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, const double *base, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, double, indices);

        /// <summary>
        /// svfloat64_t svldff1_gather[_u64base]_offset_f64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> bases, long offset) => GatherVectorFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svfloat64_t svldff1_gather[_u64base]_index_f64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1D Zresult.D, Pg/Z, [Zbases.D, #index * 8]
        ///   LDFF1D Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> bases, long index) => GatherVectorFirstFaulting(mask, bases, index);


        ///  GatherVectorInt16SignExtend : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> bases) => GatherVectorInt16SignExtend(mask, bases);

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, const short *base, Vector<int> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, const short *base, Vector<uint> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, const short *base, Vector<int> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, const short *base, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svint32_t svld1sh_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorInt16SignExtend(mask, bases, offset);

        /// <summary>
        /// svint32_t svld1sh_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LD1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> bases, long index) => GatherVectorInt16SignExtend(mask, bases, index);

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt16SignExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, const short *base, Vector<long> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, const short *base, Vector<ulong> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, const short *base, Vector<long> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, const short *base, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt16SignExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1sh_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LD1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt16SignExtend(mask, bases, index);

        /// <summary>
        /// svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> bases) => GatherVectorInt16SignExtend(mask, bases);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, const short *base, Vector<int> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, const short *base, Vector<uint> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, const short *base, Vector<int> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, const short *base, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svuint32_t svld1sh_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorInt16SignExtend(mask, bases, offset);

        /// <summary>
        /// svuint32_t svld1sh_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1SH Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LD1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> bases, long index) => GatherVectorInt16SignExtend(mask, bases, index);

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt16SignExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, const short *base, Vector<long> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, const short *base, Vector<ulong> offsets) => GatherVectorInt16SignExtend(mask, short, offsets);

        /// <summary>
        /// svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, const short *base, Vector<long> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, const short *base, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, short, indices);

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt16SignExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1sh_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1SH Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LD1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt16SignExtend(mask, bases, index);


        ///  GatherVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> bases) => GatherVectorInt16SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, const short *base, Vector<int> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, const short *base, Vector<uint> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svint32_t svldff1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, const short *base, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svint32_t svldff1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, const short *base, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svint32_t svldff1sh_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint32_t svldff1sh_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long index) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svint64_t svldff1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt16SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, const short *base, Vector<long> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, const short *base, Vector<ulong> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svint64_t svldff1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, const short *base, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svint64_t svldff1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, const short *base, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svint64_t svldff1sh_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1sh_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint32_t svldff1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases) => GatherVectorInt16SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, const short *base, Vector<int> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, const short *base, Vector<uint> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, const short *base, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svuint32_t svldff1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, const short *base, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svuint32_t svldff1sh_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint32_t svldff1sh_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long index) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint64_t svldff1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt16SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, const short *base, Vector<long> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, const short *base, Vector<ulong> offsets) => GatherVectorInt16SignExtendFirstFaulting(mask, short, offsets);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, const short *base, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svuint64_t svldff1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, const short *base, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, short, indices);

        /// <summary>
        /// svuint64_t svldff1sh_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1sh_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt16SignExtendFirstFaulting(mask, bases, index);


        ///  GatherVectorInt16ZeroExtend : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, Vector<uint> bases) => GatherVectorInt16ZeroExtend(mask, bases);

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, const ushort *base, Vector<int> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, const ushort *base, Vector<uint> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, const ushort *base, Vector<int> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, const ushort *base, Vector<uint> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svint32_t svld1uh_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorInt16ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svint32_t svld1uh_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LD1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtend(Vector<int> mask, Vector<uint> bases, long index) => GatherVectorInt16ZeroExtend(mask, bases, index);

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt16ZeroExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, const ushort *base, Vector<long> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, const ushort *base, Vector<ulong> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, const ushort *base, Vector<long> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, const ushort *base, Vector<ulong> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt16ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1uh_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LD1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtend(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt16ZeroExtend(mask, bases, index);

        /// <summary>
        /// svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, Vector<uint> bases) => GatherVectorInt16ZeroExtend(mask, bases);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, const ushort *base, Vector<int> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, const ushort *base, Vector<uint> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, const ushort *base, Vector<int> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, const ushort *base, Vector<uint> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svuint32_t svld1uh_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LD1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorInt16ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svuint32_t svld1uh_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LD1H Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LD1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtend(Vector<uint> mask, Vector<uint> bases, long index) => GatherVectorInt16ZeroExtend(mask, bases, index);

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt16ZeroExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, const ushort *base, Vector<long> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, const ushort *base, Vector<ulong> offsets) => GatherVectorInt16ZeroExtend(mask, ushort, offsets);

        /// <summary>
        /// svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, const ushort *base, Vector<long> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, const ushort *base, Vector<ulong> indices) => GatherVectorInt16ZeroExtend(mask, ushort, indices);

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt16ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1uh_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1H Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LD1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt16ZeroExtend(mask, bases, index);


        ///  GatherVectorInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> bases) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, const ushort *base, Vector<int> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, const ushort *base, Vector<uint> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svint32_t svldff1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, const ushort *base, Vector<int> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svint32_t svldff1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, const ushort *base, Vector<uint> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svint32_t svldff1uh_gather[_u32base]_offset_s32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long offset) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint32_t svldff1uh_gather[_u32base]_index_s32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LDFF1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> bases, long index) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svint64_t svldff1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, const ushort *base, Vector<long> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, const ushort *base, Vector<ulong> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svint64_t svldff1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, const ushort *base, Vector<long> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svint64_t svldff1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, const ushort *base, Vector<ulong> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svint64_t svldff1uh_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1uh_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LDFF1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint32_t svldff1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, const ushort *base, Vector<int> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, const ushort *base, Vector<uint> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, const ushort *base, Vector<int> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svuint32_t svldff1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, const ushort *base, Vector<uint> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svuint32_t svldff1uh_gather[_u32base]_offset_u32(svbool_t pg, svuint32_t bases, int64_t offset)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #offset]
        ///   LDFF1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long offset) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint32_t svldff1uh_gather[_u32base]_index_u32(svbool_t pg, svuint32_t bases, int64_t index)
        ///   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #index * 2]
        ///   LDFF1H Zresult.S, Pg/Z, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> bases, long index) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint64_t svldff1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, const ushort *base, Vector<long> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, const ushort *base, Vector<ulong> offsets) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, offsets);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, const ushort *base, Vector<long> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svuint64_t svldff1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, const ushort *base, Vector<ulong> indices) => GatherVectorInt16ZeroExtendFirstFaulting(mask, ushort, indices);

        /// <summary>
        /// svuint64_t svldff1uh_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1uh_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1H Zresult.D, Pg/Z, [Zbases.D, #index * 2]
        ///   LDFF1H Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt16ZeroExtendFirstFaulting(mask, bases, index);


        ///  GatherVectorInt32SignExtend : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt32SignExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, const int *base, Vector<long> offsets) => GatherVectorInt32SignExtend(mask, int, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, const int *base, Vector<ulong> offsets) => GatherVectorInt32SignExtend(mask, int, offsets);

        /// <summary>
        /// svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, const int *base, Vector<long> indices) => GatherVectorInt32SignExtend(mask, int, indices);

        /// <summary>
        /// svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, const int *base, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, int, indices);

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt32SignExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1sw_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LD1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt32SignExtend(mask, bases, index);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt32SignExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, const int *base, Vector<long> offsets) => GatherVectorInt32SignExtend(mask, int, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, const int *base, Vector<ulong> offsets) => GatherVectorInt32SignExtend(mask, int, offsets);

        /// <summary>
        /// svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, const int *base, Vector<long> indices) => GatherVectorInt32SignExtend(mask, int, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, const int *base, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, int, indices);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt32SignExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1sw_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1SW Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LD1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt32SignExtend(mask, bases, index);


        ///  GatherVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt32SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, const int *base, Vector<long> offsets) => GatherVectorInt32SignExtendFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, const int *base, Vector<ulong> offsets) => GatherVectorInt32SignExtendFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, const int *base, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, int, indices);

        /// <summary>
        /// svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, const int *base, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, int, indices);

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt32SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1sw_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt32SignExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt32SignExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, const int *base, Vector<long> offsets) => GatherVectorInt32SignExtendFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, const int *base, Vector<ulong> offsets) => GatherVectorInt32SignExtendFirstFaulting(mask, int, offsets);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, const int *base, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, int, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, const int *base, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, int, indices);

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt32SignExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1sw_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt32SignExtendFirstFaulting(mask, bases, index);


        ///  GatherVectorInt32ZeroExtend : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt32ZeroExtend(mask, bases);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, const uint *base, Vector<long> offsets) => GatherVectorInt32ZeroExtend(mask, uint, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, const uint *base, Vector<ulong> offsets) => GatherVectorInt32ZeroExtend(mask, uint, offsets);

        /// <summary>
        /// svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, const uint *base, Vector<long> indices) => GatherVectorInt32ZeroExtend(mask, uint, indices);

        /// <summary>
        /// svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, const uint *base, Vector<ulong> indices) => GatherVectorInt32ZeroExtend(mask, uint, indices);

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt32ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svint64_t svld1uw_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LD1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtend(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt32ZeroExtend(mask, bases, index);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt32ZeroExtend(mask, bases);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, const uint *base, Vector<long> offsets) => GatherVectorInt32ZeroExtend(mask, uint, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, const uint *base, Vector<ulong> offsets) => GatherVectorInt32ZeroExtend(mask, uint, offsets);

        /// <summary>
        /// svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, const uint *base, Vector<long> indices) => GatherVectorInt32ZeroExtend(mask, uint, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, const uint *base, Vector<ulong> indices) => GatherVectorInt32ZeroExtend(mask, uint, indices);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LD1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt32ZeroExtend(mask, bases, offset);

        /// <summary>
        /// svuint64_t svld1uw_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LD1W Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LD1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt32ZeroExtend(mask, bases, index);


        ///  GatherVectorInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, const uint *base, Vector<long> offsets) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, const uint *base, Vector<ulong> offsets) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, const uint *base, Vector<long> indices) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, const uint *base, Vector<ulong> indices) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_offset_s64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long offset) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svint64_t svldff1uw_gather[_u64base]_index_s64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LDFF1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> bases, long index) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases, index);

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, const uint *base, Vector<long> offsets) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, const uint *base, Vector<ulong> offsets) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, offsets);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, const uint *base, Vector<long> indices) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, const uint *base, Vector<ulong> indices) => GatherVectorInt32ZeroExtendFirstFaulting(mask, uint, indices);

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_offset_u64(svbool_t pg, svuint64_t bases, int64_t offset)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #offset]
        ///   LDFF1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long offset) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases, offset);

        /// <summary>
        /// svuint64_t svldff1uw_gather[_u64base]_index_u64(svbool_t pg, svuint64_t bases, int64_t index)
        ///   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #index * 4]
        ///   LDFF1W Zresult.D, Pg/Z, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> bases, long index) => GatherVectorInt32ZeroExtendFirstFaulting(mask, bases, index);


        ///  GetActiveElementCount : Count set predicate bits

        /// <summary>
        /// uint64_t svcntp_b8(svbool_t pg, svbool_t op)
        ///   CNTP Xresult, Pg, Pop.B
        /// </summary>
        public static unsafe ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from) => GetActiveElementCount(mask, from);

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


        ///  GetFFR : Read FFR, returning predicate of succesfully loaded elements

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<sbyte> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<short> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<int> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<long> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<byte> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<ushort> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<uint> GetFFR() => GetFFR();

        /// <summary>
        /// svbool_t svrdffr()
        ///   RDFFR Presult.B
        /// svbool_t svrdffr_z(svbool_t pg)
        ///   RDFFR Presult.B, Pg/Z
        /// </summary>
        public static unsafe Vector<ulong> GetFFR() => GetFFR();


        ///  InsertIntoShiftedVector : Insert scalar into shifted vector

        /// <summary>
        /// svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2)
        ///   INSR Ztied1.B, Wop2
        ///   INSR Ztied1.B, Bop2
        /// </summary>
        public static unsafe Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right) => InsertIntoShiftedVector(left, right);

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
        /// svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2)
        ///   INSR Ztied1.B, Wop2
        ///   INSR Ztied1.B, Bop2
        /// </summary>
        public static unsafe Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right) => InsertIntoShiftedVector(left, right);

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

        /// <summary>
        /// svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2)
        ///   INSR Ztied1.S, Wop2
        ///   INSR Ztied1.S, Sop2
        /// </summary>
        public static unsafe Vector<float> InsertIntoShiftedVector(Vector<float> left, float right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        /// svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2)
        ///   INSR Ztied1.D, Xop2
        ///   INSR Ztied1.D, Dop2
        /// </summary>
        public static unsafe Vector<double> InsertIntoShiftedVector(Vector<double> left, double right) => InsertIntoShiftedVector(left, right);


        ///  LeadingSignCount : Count leading sign bits

        /// <summary>
        /// svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        ///   CLS Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CLS Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op)
        ///   CLS Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CLS Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CLS Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingSignCount(Vector<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        ///   CLS Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CLS Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op)
        ///   CLS Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CLS Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CLS Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingSignCount(Vector<short> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        ///   CLS Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CLS Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op)
        ///   CLS Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CLS Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CLS Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingSignCount(Vector<int> value) => LeadingSignCount(value);

        /// <summary>
        /// svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        ///   CLS Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CLS Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op)
        ///   CLS Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CLS Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CLS Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingSignCount(Vector<long> value) => LeadingSignCount(value);


        ///  LeadingZeroCount : Count leading zero bits

        /// <summary>
        /// svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        ///   CLZ Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.B, Pg/M, Zop.B
        /// svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op)
        ///   CLZ Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.B, Pg/M, Zop.B
        /// svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CLZ Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   CLZ Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.B, Pg/M, Zop.B
        /// svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op)
        ///   CLZ Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.B, Pg/M, Zop.B
        /// svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CLZ Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> LeadingZeroCount(Vector<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        ///   CLZ Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.H, Pg/M, Zop.H
        /// svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op)
        ///   CLZ Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.H, Pg/M, Zop.H
        /// svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CLZ Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   CLZ Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.H, Pg/M, Zop.H
        /// svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op)
        ///   CLZ Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.H, Pg/M, Zop.H
        /// svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CLZ Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> LeadingZeroCount(Vector<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        ///   CLZ Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.S, Pg/M, Zop.S
        /// svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op)
        ///   CLZ Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.S, Pg/M, Zop.S
        /// svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CLZ Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   CLZ Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.S, Pg/M, Zop.S
        /// svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op)
        ///   CLZ Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.S, Pg/M, Zop.S
        /// svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CLZ Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> LeadingZeroCount(Vector<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        ///   CLZ Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.D, Pg/M, Zop.D
        /// svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op)
        ///   CLZ Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.D, Pg/M, Zop.D
        /// svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CLZ Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   CLZ Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CLZ Zresult.D, Pg/M, Zop.D
        /// svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op)
        ///   CLZ Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CLZ Zresult.D, Pg/M, Zop.D
        /// svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CLZ Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> LeadingZeroCount(Vector<ulong> value) => LeadingZeroCount(value);


        ///  LoadVector : Unextended load

        /// <summary>
        /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, const sbyte *base) => LoadVector(mask, sbyte);

        /// <summary>
        /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, const short *base) => LoadVector(mask, short);

        /// <summary>
        /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, const int *base) => LoadVector(mask, int);

        /// <summary>
        /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, const long *base) => LoadVector(mask, long);

        /// <summary>
        /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, const byte *base) => LoadVector(mask, byte);

        /// <summary>
        /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, const ushort *base) => LoadVector(mask, ushort);

        /// <summary>
        /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, const uint *base) => LoadVector(mask, uint);

        /// <summary>
        /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, const ulong *base) => LoadVector(mask, ulong);

        /// <summary>
        /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, const float *base) => LoadVector(mask, float);

        /// <summary>
        /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, const double *base) => LoadVector(mask, double);


        ///  LoadVector128AndReplicateToVector : Load and replicate 128 bits of data

        /// <summary>
        /// svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base)
        ///   LD1RQB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1RQB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1RQB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, const sbyte *base) => LoadVector128AndReplicateToVector(mask, sbyte);

        /// <summary>
        /// svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, const short *base) => LoadVector128AndReplicateToVector(mask, short);

        /// <summary>
        /// svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, const int *base) => LoadVector128AndReplicateToVector(mask, int);

        /// <summary>
        /// svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, const long *base) => LoadVector128AndReplicateToVector(mask, long);

        /// <summary>
        /// svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1RQB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1RQB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1RQB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, const byte *base) => LoadVector128AndReplicateToVector(mask, byte);

        /// <summary>
        /// svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1RQH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1RQH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, const ushort *base) => LoadVector128AndReplicateToVector(mask, ushort);

        /// <summary>
        /// svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, const uint *base) => LoadVector128AndReplicateToVector(mask, uint);

        /// <summary>
        /// svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, const ulong *base) => LoadVector128AndReplicateToVector(mask, ulong);

        /// <summary>
        /// svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base)
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1RQW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1RQW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, const float *base) => LoadVector128AndReplicateToVector(mask, float);

        /// <summary>
        /// svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base)
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1RQD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1RQD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, const double *base) => LoadVector128AndReplicateToVector(mask, double);


        ///  LoadVectorByteSignExtendFirstFaulting : Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1sb_s16(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteSignExtendFirstFaulting(Vector<short> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);

        /// <summary>
        /// svint32_t svldff1sb_s32(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteSignExtendFirstFaulting(Vector<int> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);

        /// <summary>
        /// svint64_t svldff1sb_s64(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteSignExtendFirstFaulting(Vector<long> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);

        /// <summary>
        /// svuint16_t svldff1sb_u16(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteSignExtendFirstFaulting(Vector<ushort> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);

        /// <summary>
        /// svuint32_t svldff1sb_u32(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteSignExtendFirstFaulting(Vector<uint> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);

        /// <summary>
        /// svuint64_t svldff1sb_u64(svbool_t pg, const int8_t *base)
        ///   LDFF1SB Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteSignExtendFirstFaulting(Vector<ulong> mask, const sbyte *base) => LoadVectorByteSignExtendFirstFaulting(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteSignExtendNonFaultingToInt16(Vector<short> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToInt16(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteSignExtendNonFaultingToInt32(Vector<int> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToInt32(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteSignExtendNonFaultingToInt64(Vector<long> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToInt64(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToUInt16 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteSignExtendNonFaultingToUInt16(Vector<ushort> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToUInt16(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToUInt32 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteSignExtendNonFaultingToUInt32(Vector<uint> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToUInt32(mask, sbyte);


        ///  LoadVectorByteSignExtendNonFaultingToUInt64 : Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base)
        ///   LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteSignExtendNonFaultingToUInt64(Vector<ulong> mask, const sbyte *base) => LoadVectorByteSignExtendNonFaultingToUInt64(mask, sbyte);


        ///  LoadVectorByteSignExtendToInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteSignExtendToInt16(Vector<short> mask, const sbyte *base) => LoadVectorByteSignExtendToInt16(mask, sbyte);


        ///  LoadVectorByteSignExtendToInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteSignExtendToInt32(Vector<int> mask, const sbyte *base) => LoadVectorByteSignExtendToInt32(mask, sbyte);


        ///  LoadVectorByteSignExtendToInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteSignExtendToInt64(Vector<long> mask, const sbyte *base) => LoadVectorByteSignExtendToInt64(mask, sbyte);


        ///  LoadVectorByteSignExtendToUInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteSignExtendToUInt16(Vector<ushort> mask, const sbyte *base) => LoadVectorByteSignExtendToUInt16(mask, sbyte);


        ///  LoadVectorByteSignExtendToUInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteSignExtendToUInt32(Vector<uint> mask, const sbyte *base) => LoadVectorByteSignExtendToUInt32(mask, sbyte);


        ///  LoadVectorByteSignExtendToUInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteSignExtendToUInt64(Vector<ulong> mask, const sbyte *base) => LoadVectorByteSignExtendToUInt64(mask, sbyte);


        ///  LoadVectorByteZeroExtendFirstFaulting : Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint16_t svldff1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);

        /// <summary>
        /// svint32_t svldff1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);

        /// <summary>
        /// svint64_t svldff1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);

        /// <summary>
        /// svuint16_t svldff1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);

        /// <summary>
        /// svuint32_t svldff1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);

        /// <summary>
        /// svuint64_t svldff1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, const byte *base) => LoadVectorByteZeroExtendFirstFaulting(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendNonFaultingToInt16(Vector<short> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToInt16(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendNonFaultingToInt32(Vector<int> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToInt32(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendNonFaultingToInt64(Vector<long> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToInt64(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToUInt16 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendNonFaultingToUInt16(Vector<ushort> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToUInt16(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToUInt32 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendNonFaultingToUInt32(Vector<uint> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToUInt32(mask, byte);


        ///  LoadVectorByteZeroExtendNonFaultingToUInt64 : Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendNonFaultingToUInt64(Vector<ulong> mask, const byte *base) => LoadVectorByteZeroExtendNonFaultingToUInt64(mask, byte);


        ///  LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, const byte *base) => LoadVectorByteZeroExtendToInt16(mask, byte);


        ///  LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, const byte *base) => LoadVectorByteZeroExtendToInt32(mask, byte);


        ///  LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, const byte *base) => LoadVectorByteZeroExtendToInt64(mask, byte);


        ///  LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, const byte *base) => LoadVectorByteZeroExtendToUInt16(mask, byte);


        ///  LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, const byte *base) => LoadVectorByteZeroExtendToUInt32(mask, byte);


        ///  LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, const byte *base) => LoadVectorByteZeroExtendToUInt64(mask, byte);


        ///  LoadVectorFirstFaulting : Unextended load, first-faulting

        /// <summary>
        /// svint8_t svldff1[_s8](svbool_t pg, const int8_t *base)
        ///   LDFF1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, const sbyte *base) => LoadVectorFirstFaulting(mask, sbyte);

        /// <summary>
        /// svint16_t svldff1[_s16](svbool_t pg, const int16_t *base)
        ///   LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, const short *base) => LoadVectorFirstFaulting(mask, short);

        /// <summary>
        /// svint32_t svldff1[_s32](svbool_t pg, const int32_t *base)
        ///   LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, const int *base) => LoadVectorFirstFaulting(mask, int);

        /// <summary>
        /// svint64_t svldff1[_s64](svbool_t pg, const int64_t *base)
        ///   LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]
        /// </summary>
        public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, const long *base) => LoadVectorFirstFaulting(mask, long);

        /// <summary>
        /// svuint8_t svldff1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDFF1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, const byte *base) => LoadVectorFirstFaulting(mask, byte);

        /// <summary>
        /// svuint16_t svldff1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDFF1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, const ushort *base) => LoadVectorFirstFaulting(mask, ushort);

        /// <summary>
        /// svuint32_t svldff1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, const uint *base) => LoadVectorFirstFaulting(mask, uint);

        /// <summary>
        /// svuint64_t svldff1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, const ulong *base) => LoadVectorFirstFaulting(mask, ulong);

        /// <summary>
        /// svfloat32_t svldff1[_f32](svbool_t pg, const float32_t *base)
        ///   LDFF1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, const float *base) => LoadVectorFirstFaulting(mask, float);

        /// <summary>
        /// svfloat64_t svldff1[_f64](svbool_t pg, const float64_t *base)
        ///   LDFF1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]
        /// </summary>
        public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, const double *base) => LoadVectorFirstFaulting(mask, double);


        ///  LoadVectorInt16SignExtendFirstFaulting : Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1sh_s32(svbool_t pg, const int16_t *base)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, const short *base) => LoadVectorInt16SignExtendFirstFaulting(mask, short);

        /// <summary>
        /// svint64_t svldff1sh_s64(svbool_t pg, const int16_t *base)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, const short *base) => LoadVectorInt16SignExtendFirstFaulting(mask, short);

        /// <summary>
        /// svuint32_t svldff1sh_u32(svbool_t pg, const int16_t *base)
        ///   LDFF1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, const short *base) => LoadVectorInt16SignExtendFirstFaulting(mask, short);

        /// <summary>
        /// svuint64_t svldff1sh_u64(svbool_t pg, const int16_t *base)
        ///   LDFF1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, const short *base) => LoadVectorInt16SignExtendFirstFaulting(mask, short);


        ///  LoadVectorInt16SignExtendNonFaultingToInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendNonFaultingToInt32(Vector<int> mask, const short *base) => LoadVectorInt16SignExtendNonFaultingToInt32(mask, short);


        ///  LoadVectorInt16SignExtendNonFaultingToInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendNonFaultingToInt64(Vector<long> mask, const short *base) => LoadVectorInt16SignExtendNonFaultingToInt64(mask, short);


        ///  LoadVectorInt16SignExtendNonFaultingToUInt32 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendNonFaultingToUInt32(Vector<uint> mask, const short *base) => LoadVectorInt16SignExtendNonFaultingToUInt32(mask, short);


        ///  LoadVectorInt16SignExtendNonFaultingToUInt64 : Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base)
        ///   LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendNonFaultingToUInt64(Vector<ulong> mask, const short *base) => LoadVectorInt16SignExtendNonFaultingToUInt64(mask, short);


        ///  LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, const short *base) => LoadVectorInt16SignExtendToInt32(mask, short);


        ///  LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, const short *base) => LoadVectorInt16SignExtendToInt64(mask, short);


        ///  LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, const short *base) => LoadVectorInt16SignExtendToUInt32(mask, short);


        ///  LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, const short *base) => LoadVectorInt16SignExtendToUInt64(mask, short);


        ///  LoadVectorInt16ZeroExtendFirstFaulting : Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint32_t svldff1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LDFF1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16ZeroExtendFirstFaulting(Vector<int> mask, const ushort *base) => LoadVectorInt16ZeroExtendFirstFaulting(mask, ushort);

        /// <summary>
        /// svint64_t svldff1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LDFF1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16ZeroExtendFirstFaulting(Vector<long> mask, const ushort *base) => LoadVectorInt16ZeroExtendFirstFaulting(mask, ushort);

        /// <summary>
        /// svuint32_t svldff1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LDFF1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16ZeroExtendFirstFaulting(Vector<uint> mask, const ushort *base) => LoadVectorInt16ZeroExtendFirstFaulting(mask, ushort);

        /// <summary>
        /// svuint64_t svldff1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LDFF1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16ZeroExtendFirstFaulting(Vector<ulong> mask, const ushort *base) => LoadVectorInt16ZeroExtendFirstFaulting(mask, ushort);


        ///  LoadVectorInt16ZeroExtendNonFaultingToInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16ZeroExtendNonFaultingToInt32(Vector<int> mask, const ushort *base) => LoadVectorInt16ZeroExtendNonFaultingToInt32(mask, ushort);


        ///  LoadVectorInt16ZeroExtendNonFaultingToInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16ZeroExtendNonFaultingToInt64(Vector<long> mask, const ushort *base) => LoadVectorInt16ZeroExtendNonFaultingToInt64(mask, ushort);


        ///  LoadVectorInt16ZeroExtendNonFaultingToUInt32 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16ZeroExtendNonFaultingToUInt32(Vector<uint> mask, const ushort *base) => LoadVectorInt16ZeroExtendNonFaultingToUInt32(mask, ushort);


        ///  LoadVectorInt16ZeroExtendNonFaultingToUInt64 : Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16ZeroExtendNonFaultingToUInt64(Vector<ulong> mask, const ushort *base) => LoadVectorInt16ZeroExtendNonFaultingToUInt64(mask, ushort);


        ///  LoadVectorInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16ZeroExtendToInt32(Vector<int> mask, const ushort *base) => LoadVectorInt16ZeroExtendToInt32(mask, ushort);


        ///  LoadVectorInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16ZeroExtendToInt64(Vector<long> mask, const ushort *base) => LoadVectorInt16ZeroExtendToInt64(mask, ushort);


        ///  LoadVectorInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16ZeroExtendToUInt32(Vector<uint> mask, const ushort *base) => LoadVectorInt16ZeroExtendToUInt32(mask, ushort);


        ///  LoadVectorInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16ZeroExtendToUInt64(Vector<ulong> mask, const ushort *base) => LoadVectorInt16ZeroExtendToUInt64(mask, ushort);


        ///  LoadVectorInt32SignExtendFirstFaulting : Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1sw_s64(svbool_t pg, const int32_t *base)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, const int *base) => LoadVectorInt32SignExtendFirstFaulting(mask, int);

        /// <summary>
        /// svuint64_t svldff1sw_u64(svbool_t pg, const int32_t *base)
        ///   LDFF1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, const int *base) => LoadVectorInt32SignExtendFirstFaulting(mask, int);


        ///  LoadVectorInt32SignExtendNonFaultingToInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base)
        ///   LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendNonFaultingToInt64(Vector<long> mask, const int *base) => LoadVectorInt32SignExtendNonFaultingToInt64(mask, int);


        ///  LoadVectorInt32SignExtendNonFaultingToUInt64 : Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base)
        ///   LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendNonFaultingToUInt64(Vector<ulong> mask, const int *base) => LoadVectorInt32SignExtendNonFaultingToUInt64(mask, int);


        ///  LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, const int *base) => LoadVectorInt32SignExtendToInt64(mask, int);


        ///  LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, const int *base) => LoadVectorInt32SignExtendToUInt64(mask, int);


        ///  LoadVectorInt32ZeroExtendFirstFaulting : Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        /// svint64_t svldff1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LDFF1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32ZeroExtendFirstFaulting(Vector<long> mask, const uint *base) => LoadVectorInt32ZeroExtendFirstFaulting(mask, uint);

        /// <summary>
        /// svuint64_t svldff1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LDFF1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32ZeroExtendFirstFaulting(Vector<ulong> mask, const uint *base) => LoadVectorInt32ZeroExtendFirstFaulting(mask, uint);


        ///  LoadVectorInt32ZeroExtendNonFaultingToInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32ZeroExtendNonFaultingToInt64(Vector<long> mask, const uint *base) => LoadVectorInt32ZeroExtendNonFaultingToInt64(mask, uint);


        ///  LoadVectorInt32ZeroExtendNonFaultingToUInt64 : Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        /// svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32ZeroExtendNonFaultingToUInt64(Vector<ulong> mask, const uint *base) => LoadVectorInt32ZeroExtendNonFaultingToUInt64(mask, uint);


        ///  LoadVectorInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32ZeroExtendToInt64(Vector<long> mask, const uint *base) => LoadVectorInt32ZeroExtendToInt64(mask, uint);


        ///  LoadVectorInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32ZeroExtendToUInt64(Vector<ulong> mask, const uint *base) => LoadVectorInt32ZeroExtendToUInt64(mask, uint);


        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(Vector<sbyte> mask, const sbyte *base) => LoadVectorNonFaulting(mask, sbyte);

        /// <summary>
        /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(Vector<short> mask, const short *base) => LoadVectorNonFaulting(mask, short);

        /// <summary>
        /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(Vector<int> mask, const int *base) => LoadVectorNonFaulting(mask, int);

        /// <summary>
        /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(Vector<long> mask, const long *base) => LoadVectorNonFaulting(mask, long);

        /// <summary>
        /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(Vector<byte> mask, const byte *base) => LoadVectorNonFaulting(mask, byte);

        /// <summary>
        /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(Vector<ushort> mask, const ushort *base) => LoadVectorNonFaulting(mask, ushort);

        /// <summary>
        /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(Vector<uint> mask, const uint *base) => LoadVectorNonFaulting(mask, uint);

        /// <summary>
        /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(Vector<ulong> mask, const ulong *base) => LoadVectorNonFaulting(mask, ulong);

        /// <summary>
        /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(Vector<float> mask, const float *base) => LoadVectorNonFaulting(mask, float);

        /// <summary>
        /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(Vector<double> mask, const double *base) => LoadVectorNonFaulting(mask, double);


        ///  LoadVectorNonTemporal : Unextended load, non-temporal

        /// <summary>
        /// svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base)
        ///   LDNT1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, const sbyte *base) => LoadVectorNonTemporal(mask, sbyte);

        /// <summary>
        /// svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, const short *base) => LoadVectorNonTemporal(mask, short);

        /// <summary>
        /// svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, const int *base) => LoadVectorNonTemporal(mask, int);

        /// <summary>
        /// svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, const long *base) => LoadVectorNonTemporal(mask, long);

        /// <summary>
        /// svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDNT1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, const byte *base) => LoadVectorNonTemporal(mask, byte);

        /// <summary>
        /// svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDNT1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, const ushort *base) => LoadVectorNonTemporal(mask, ushort);

        /// <summary>
        /// svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, const uint *base) => LoadVectorNonTemporal(mask, uint);

        /// <summary>
        /// svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, const ulong *base) => LoadVectorNonTemporal(mask, ulong);

        /// <summary>
        /// svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base)
        ///   LDNT1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, const float *base) => LoadVectorNonTemporal(mask, float);

        /// <summary>
        /// svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base)
        ///   LDNT1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, const double *base) => LoadVectorNonTemporal(mask, double);


        ///  LoadVectorx2 : Load two-element tuples into two vectors

        /// <summary>
        /// svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base)
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xarray, Xindex]
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>) LoadVectorx2(Vector<sbyte> mask, const sbyte *base) => LoadVectorx2(mask, sbyte);

        /// <summary>
        /// svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>) LoadVectorx2(Vector<short> mask, const short *base) => LoadVectorx2(mask, short);

        /// <summary>
        /// svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>) LoadVectorx2(Vector<int> mask, const int *base) => LoadVectorx2(mask, int);

        /// <summary>
        /// svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>) LoadVectorx2(Vector<long> mask, const long *base) => LoadVectorx2(mask, long);

        /// <summary>
        /// svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base)
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xarray, Xindex]
        ///   LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>) LoadVectorx2(Vector<byte> mask, const byte *base) => LoadVectorx2(mask, byte);

        /// <summary>
        /// svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base)
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>) LoadVectorx2(Vector<ushort> mask, const ushort *base) => LoadVectorx2(mask, ushort);

        /// <summary>
        /// svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>) LoadVectorx2(Vector<uint> mask, const uint *base) => LoadVectorx2(mask, uint);

        /// <summary>
        /// svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>) LoadVectorx2(Vector<ulong> mask, const ulong *base) => LoadVectorx2(mask, ulong);

        /// <summary>
        /// svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base)
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>) LoadVectorx2(Vector<float> mask, const float *base) => LoadVectorx2(mask, float);

        /// <summary>
        /// svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base)
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>) LoadVectorx2(Vector<double> mask, const double *base) => LoadVectorx2(mask, double);


        ///  LoadVectorx3 : Load three-element tuples into three vectors

        /// <summary>
        /// svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base)
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xarray, Xindex]
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx3(Vector<sbyte> mask, const sbyte *base) => LoadVectorx3(mask, sbyte);

        /// <summary>
        /// svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>) LoadVectorx3(Vector<short> mask, const short *base) => LoadVectorx3(mask, short);

        /// <summary>
        /// svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>) LoadVectorx3(Vector<int> mask, const int *base) => LoadVectorx3(mask, int);

        /// <summary>
        /// svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>) LoadVectorx3(Vector<long> mask, const long *base) => LoadVectorx3(mask, long);

        /// <summary>
        /// svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base)
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xarray, Xindex]
        ///   LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx3(Vector<byte> mask, const byte *base) => LoadVectorx3(mask, byte);

        /// <summary>
        /// svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base)
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx3(Vector<ushort> mask, const ushort *base) => LoadVectorx3(mask, ushort);

        /// <summary>
        /// svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx3(Vector<uint> mask, const uint *base) => LoadVectorx3(mask, uint);

        /// <summary>
        /// svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx3(Vector<ulong> mask, const ulong *base) => LoadVectorx3(mask, ulong);

        /// <summary>
        /// svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base)
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>) LoadVectorx3(Vector<float> mask, const float *base) => LoadVectorx3(mask, float);

        /// <summary>
        /// svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base)
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>) LoadVectorx3(Vector<double> mask, const double *base) => LoadVectorx3(mask, double);


        ///  LoadVectorx4 : Load four-element tuples into four vectors

        /// <summary>
        /// svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base)
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xarray, Xindex]
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) LoadVectorx4(Vector<sbyte> mask, const sbyte *base) => LoadVectorx4(mask, sbyte);

        /// <summary>
        /// svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) LoadVectorx4(Vector<short> mask, const short *base) => LoadVectorx4(mask, short);

        /// <summary>
        /// svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) LoadVectorx4(Vector<int> mask, const int *base) => LoadVectorx4(mask, int);

        /// <summary>
        /// svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) LoadVectorx4(Vector<long> mask, const long *base) => LoadVectorx4(mask, long);

        /// <summary>
        /// svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base)
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xarray, Xindex]
        ///   LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) LoadVectorx4(Vector<byte> mask, const byte *base) => LoadVectorx4(mask, byte);

        /// <summary>
        /// svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base)
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) LoadVectorx4(Vector<ushort> mask, const ushort *base) => LoadVectorx4(mask, ushort);

        /// <summary>
        /// svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) LoadVectorx4(Vector<uint> mask, const uint *base) => LoadVectorx4(mask, uint);

        /// <summary>
        /// svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) LoadVectorx4(Vector<ulong> mask, const ulong *base) => LoadVectorx4(mask, ulong);

        /// <summary>
        /// svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base)
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) LoadVectorx4(Vector<float> mask, const float *base) => LoadVectorx4(mask, float);

        /// <summary>
        /// svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base)
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) LoadVectorx4(Vector<double> mask, const double *base) => LoadVectorx4(mask, double);


        ///  MaskGetFirstSet : Find next active predicate

        /// <summary>
        /// svbool_t svpnext_b8(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<byte> MaskGetFirstSet(Vector<byte> mask, Vector<byte> from) => MaskGetFirstSet(mask, from);

        /// <summary>
        /// svbool_t svpnext_b16(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.H, Pg, Ptied.H
        /// </summary>
        public static unsafe Vector<ushort> MaskGetFirstSet(Vector<ushort> mask, Vector<ushort> from) => MaskGetFirstSet(mask, from);

        /// <summary>
        /// svbool_t svpnext_b32(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.S, Pg, Ptied.S
        /// </summary>
        public static unsafe Vector<uint> MaskGetFirstSet(Vector<uint> mask, Vector<uint> from) => MaskGetFirstSet(mask, from);

        /// <summary>
        /// svbool_t svpnext_b64(svbool_t pg, svbool_t op)
        ///   PNEXT Ptied.D, Pg, Ptied.D
        /// </summary>
        public static unsafe Vector<ulong> MaskGetFirstSet(Vector<ulong> mask, Vector<ulong> from) => MaskGetFirstSet(mask, from);


        ///  MaskSetFirst : Set the first active predicate element to true

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<sbyte> MaskSetFirst(Vector<sbyte> mask, Vector<sbyte> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<short> MaskSetFirst(Vector<short> mask, Vector<short> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<int> MaskSetFirst(Vector<int> mask, Vector<int> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<long> MaskSetFirst(Vector<long> mask, Vector<long> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<byte> MaskSetFirst(Vector<byte> mask, Vector<byte> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<ushort> MaskSetFirst(Vector<ushort> mask, Vector<ushort> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<uint> MaskSetFirst(Vector<uint> mask, Vector<uint> from) => MaskSetFirst(mask, from);

        /// <summary>
        /// svbool_t svpfirst[_b](svbool_t pg, svbool_t op)
        ///   PFIRST Ptied.B, Pg, Ptied.B
        /// </summary>
        public static unsafe Vector<ulong> MaskSetFirst(Vector<ulong> mask, Vector<ulong> from) => MaskSetFirst(mask, from);


        ///  MaskTestAnyTrue : Test whether any active element is true

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<sbyte> mask, Vector<sbyte> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<short> mask, Vector<short> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<int> mask, Vector<int> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<long> mask, Vector<long> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<byte> mask, Vector<byte> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<ushort> mask, Vector<ushort> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<uint> mask, Vector<uint> from) => MaskTestAnyTrue(mask, from);

        /// <summary>
        /// bool svptest_any(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestAnyTrue(Vector<ulong> mask, Vector<ulong> from) => MaskTestAnyTrue(mask, from);


        ///  MaskTestFirstTrue : Test whether the first active element is true

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<sbyte> mask, Vector<sbyte> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<short> mask, Vector<short> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<int> mask, Vector<int> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<long> mask, Vector<long> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<byte> mask, Vector<byte> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<ushort> mask, Vector<ushort> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<uint> mask, Vector<uint> from) => MaskTestFirstTrue(mask, from);

        /// <summary>
        /// bool svptest_first(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestFirstTrue(Vector<ulong> mask, Vector<ulong> from) => MaskTestFirstTrue(mask, from);


        ///  MaskTestLastTrue : Test whether the last active element is true

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<sbyte> mask, Vector<sbyte> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<short> mask, Vector<short> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<int> mask, Vector<int> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<long> mask, Vector<long> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<byte> mask, Vector<byte> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<ushort> mask, Vector<ushort> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<uint> mask, Vector<uint> from) => MaskTestLastTrue(mask, from);

        /// <summary>
        /// bool svptest_last(svbool_t pg, svbool_t op)
        ///   PTEST
        /// </summary>
        public static unsafe bool MaskTestLastTrue(Vector<ulong> mask, Vector<ulong> from) => MaskTestLastTrue(mask, from);


        ///  Max : Maximum

        /// <summary>
        /// svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SMAX Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right) => Max(left, right);

        /// <summary>
        /// svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SMAX Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Max(Vector<short> left, Vector<short> right) => Max(left, right);

        /// <summary>
        /// svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SMAX Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Max(Vector<int> left, Vector<int> right) => Max(left, right);

        /// <summary>
        /// svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SMAX Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Max(Vector<long> left, Vector<long> right) => Max(left, right);

        /// <summary>
        /// svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; UMAX Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; UMAX Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Max(Vector<byte> left, Vector<byte> right) => Max(left, right);

        /// <summary>
        /// svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; UMAX Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; UMAX Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right) => Max(left, right);

        /// <summary>
        /// svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; UMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; UMAX Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Max(Vector<uint> left, Vector<uint> right) => Max(left, right);

        /// <summary>
        /// svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; UMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; UMAX Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right) => Max(left, right);

        /// <summary>
        /// svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMAX Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAX Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Max(Vector<float> left, Vector<float> right) => Max(left, right);

        /// <summary>
        /// svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMAX Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAX Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Max(Vector<double> left, Vector<double> right) => Max(left, right);


        ///  MaxAcross : Maximum reduction to scalar

        /// <summary>
        /// int8_t svmaxv[_s8](svbool_t pg, svint8_t op)
        ///   SMAXV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte MaxAcross(Vector<sbyte> value) => MaxAcross(value);

        /// <summary>
        /// int16_t svmaxv[_s16](svbool_t pg, svint16_t op)
        ///   SMAXV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short MaxAcross(Vector<short> value) => MaxAcross(value);

        /// <summary>
        /// int32_t svmaxv[_s32](svbool_t pg, svint32_t op)
        ///   SMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int MaxAcross(Vector<int> value) => MaxAcross(value);

        /// <summary>
        /// int64_t svmaxv[_s64](svbool_t pg, svint64_t op)
        ///   SMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long MaxAcross(Vector<long> value) => MaxAcross(value);

        /// <summary>
        /// uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op)
        ///   UMAXV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte MaxAcross(Vector<byte> value) => MaxAcross(value);

        /// <summary>
        /// uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op)
        ///   UMAXV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort MaxAcross(Vector<ushort> value) => MaxAcross(value);

        /// <summary>
        /// uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op)
        ///   UMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint MaxAcross(Vector<uint> value) => MaxAcross(value);

        /// <summary>
        /// uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op)
        ///   UMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong MaxAcross(Vector<ulong> value) => MaxAcross(value);

        /// <summary>
        /// float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMAXV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float MaxAcross(Vector<float> value) => MaxAcross(value);

        /// <summary>
        /// float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMAXV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double MaxAcross(Vector<double> value) => MaxAcross(value);


        ///  MaxNumber : Maximum number

        /// <summary>
        /// svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMAXNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMAXNM Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> MaxNumber(Vector<float> left, Vector<float> right) => MaxNumber(left, right);

        /// <summary>
        /// svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMAXNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMAXNM Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> MaxNumber(Vector<double> left, Vector<double> right) => MaxNumber(left, right);


        ///  MaxNumberAcross : Maximum number reduction to scalar

        /// <summary>
        /// float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMAXNMV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float MaxNumberAcross(Vector<float> value) => MaxNumberAcross(value);

        /// <summary>
        /// float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMAXNMV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double MaxNumberAcross(Vector<double> value) => MaxNumberAcross(value);


        ///  Min : Minimum

        /// <summary>
        /// svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SMIN Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right) => Min(left, right);

        /// <summary>
        /// svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SMIN Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Min(Vector<short> left, Vector<short> right) => Min(left, right);

        /// <summary>
        /// svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SMIN Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Min(Vector<int> left, Vector<int> right) => Min(left, right);

        /// <summary>
        /// svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SMIN Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Min(Vector<long> left, Vector<long> right) => Min(left, right);

        /// <summary>
        /// svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; UMIN Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; UMIN Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Min(Vector<byte> left, Vector<byte> right) => Min(left, right);

        /// <summary>
        /// svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; UMIN Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; UMIN Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right) => Min(left, right);

        /// <summary>
        /// svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; UMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; UMIN Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Min(Vector<uint> left, Vector<uint> right) => Min(left, right);

        /// <summary>
        /// svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; UMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; UMIN Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right) => Min(left, right);

        /// <summary>
        /// svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMIN Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMIN Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Min(Vector<float> left, Vector<float> right) => Min(left, right);

        /// <summary>
        /// svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMIN Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMIN Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Min(Vector<double> left, Vector<double> right) => Min(left, right);


        ///  MinAcross : Minimum reduction to scalar

        /// <summary>
        /// int8_t svminv[_s8](svbool_t pg, svint8_t op)
        ///   SMINV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte MinAcross(Vector<sbyte> value) => MinAcross(value);

        /// <summary>
        /// int16_t svminv[_s16](svbool_t pg, svint16_t op)
        ///   SMINV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short MinAcross(Vector<short> value) => MinAcross(value);

        /// <summary>
        /// int32_t svminv[_s32](svbool_t pg, svint32_t op)
        ///   SMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int MinAcross(Vector<int> value) => MinAcross(value);

        /// <summary>
        /// int64_t svminv[_s64](svbool_t pg, svint64_t op)
        ///   SMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long MinAcross(Vector<long> value) => MinAcross(value);

        /// <summary>
        /// uint8_t svminv[_u8](svbool_t pg, svuint8_t op)
        ///   UMINV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte MinAcross(Vector<byte> value) => MinAcross(value);

        /// <summary>
        /// uint16_t svminv[_u16](svbool_t pg, svuint16_t op)
        ///   UMINV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort MinAcross(Vector<ushort> value) => MinAcross(value);

        /// <summary>
        /// uint32_t svminv[_u32](svbool_t pg, svuint32_t op)
        ///   UMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint MinAcross(Vector<uint> value) => MinAcross(value);

        /// <summary>
        /// uint64_t svminv[_u64](svbool_t pg, svuint64_t op)
        ///   UMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong MinAcross(Vector<ulong> value) => MinAcross(value);

        /// <summary>
        /// float32_t svminv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMINV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float MinAcross(Vector<float> value) => MinAcross(value);

        /// <summary>
        /// float64_t svminv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMINV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double MinAcross(Vector<double> value) => MinAcross(value);


        ///  MinNumber : Minimum number

        /// <summary>
        /// svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMINNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMINNM Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMINNM Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> MinNumber(Vector<float> left, Vector<float> right) => MinNumber(left, right);

        /// <summary>
        /// svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMINNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMINNM Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMINNM Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> MinNumber(Vector<double> left, Vector<double> right) => MinNumber(left, right);


        ///  MinNumberAcross : Minimum number reduction to scalar

        /// <summary>
        /// float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op)
        ///   FMINNMV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe float MinNumberAcross(Vector<float> value) => MinNumberAcross(value);

        /// <summary>
        /// float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op)
        ///   FMINNMV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe double MinNumberAcross(Vector<double> value) => MinNumberAcross(value);



        ///  Multiply : Multiply

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
        /// svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index)
        ///   FMUL Zresult.S, Zop1.S, Zop2.S[imm_index]
        /// </summary>
        public static unsafe Vector<float> Multiply(Vector<float> left, Vector<float> right, ulong index) => Multiply(left, right, index);

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

        /// <summary>
        /// svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index)
        ///   FMUL Zresult.D, Zop1.D, Zop2.D[imm_index]
        /// </summary>
        public static unsafe Vector<double> Multiply(Vector<double> left, Vector<double> right, ulong index) => Multiply(left, right, index);


        ///  MultiplyAdd : Multiply-add, addend first

        /// <summary>
        /// svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MAD Ztied2.B, Pg/M, Zop3.B, Zop1.B
        ///   MAD Ztied3.B, Pg/M, Zop2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MAD Zresult.B, Pg/M, Zop3.B, Zop1.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop3.B; MAD Zresult.B, Pg/M, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MAD Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   MAD Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MAD Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; MAD Zresult.H, Pg/M, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MAD Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   MAD Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MAD Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; MAD Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MAD Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   MAD Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MAD Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; MAD Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MAD Ztied2.B, Pg/M, Zop3.B, Zop1.B
        ///   MAD Ztied3.B, Pg/M, Zop2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLA Zresult.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MAD Zresult.B, Pg/M, Zop3.B, Zop1.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop3.B; MAD Zresult.B, Pg/M, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MAD Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   MAD Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLA Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MAD Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; MAD Zresult.H, Pg/M, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MAD Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   MAD Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLA Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MAD Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; MAD Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MAD Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   MAD Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLA Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MAD Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; MAD Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => MultiplyAdd(addend, left, right);




        ///  MultiplyAddRotateComplex : Complex multiply-add with rotate

        /// <summary>
        /// svfloat32_t svcmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation
        /// svfloat32_t svcmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation
        /// svfloat32_t svcmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FCMLA Zresult.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplex(Vector<float> op1, Vector<float> op2, Vector<float> op3, ulong imm_rotation) => MultiplyAddRotateComplex(op1, op2, op3, imm_rotation);

        /// <summary>
        /// svfloat32_t svcmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index, uint64_t imm_rotation)
        ///   FCMLA Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.S, Zop2.S, Zop3.S[imm_index], #imm_rotation
        /// </summary>
        public static unsafe Vector<float> MultiplyAddRotateComplex(Vector<float> op1, Vector<float> op2, Vector<float> op3, ulong imm_index, ulong imm_rotation) => MultiplyAddRotateComplex(op1, op2, op3, imm_index, imm_rotation);

        /// <summary>
        /// svfloat64_t svcmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation
        /// svfloat64_t svcmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        ///   FCMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation
        ///   MOVPRFX Zresult, Zop1; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation
        /// svfloat64_t svcmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FCMLA Zresult.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation
        /// </summary>
        public static unsafe Vector<double> MultiplyAddRotateComplex(Vector<double> op1, Vector<double> op2, Vector<double> op3, ulong imm_rotation) => MultiplyAddRotateComplex(op1, op2, op3, imm_rotation);


        ///  MultiplyExtended : Multiply extended (∞×0=2)

        /// <summary>
        /// svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FMULX Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FMULX Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FMULX Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right) => MultiplyExtended(left, right);

        /// <summary>
        /// svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FMULX Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FMULX Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FMULX Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right) => MultiplyExtended(left, right);


        ///  MultiplyReturningHighHalf : Multiply, returning high half

        /// <summary>
        /// svint8_t svmulh[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMULH Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmulh[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SMULH Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SMULH Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svmulh[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SMULH Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> MultiplyReturningHighHalf(Vector<sbyte> left, Vector<sbyte> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svint16_t svmulh[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMULH Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmulh[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SMULH Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SMULH Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svmulh[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SMULH Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> MultiplyReturningHighHalf(Vector<short> left, Vector<short> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svint32_t svmulh[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMULH Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmulh[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SMULH Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SMULH Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svmulh[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SMULH Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> MultiplyReturningHighHalf(Vector<int> left, Vector<int> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svint64_t svmulh[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMULH Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmulh[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SMULH Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SMULH Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; SMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svmulh[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SMULH Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> MultiplyReturningHighHalf(Vector<long> left, Vector<long> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svuint8_t svmulh[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMULH Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmulh[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   UMULH Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   UMULH Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svmulh[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; UMULH Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; UMULH Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> MultiplyReturningHighHalf(Vector<byte> left, Vector<byte> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svuint16_t svmulh[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMULH Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmulh[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   UMULH Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   UMULH Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svmulh[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; UMULH Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; UMULH Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> MultiplyReturningHighHalf(Vector<ushort> left, Vector<ushort> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svuint32_t svmulh[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMULH Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmulh[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   UMULH Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   UMULH Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svmulh[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; UMULH Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; UMULH Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> MultiplyReturningHighHalf(Vector<uint> left, Vector<uint> right) => MultiplyReturningHighHalf(left, right);

        /// <summary>
        /// svuint64_t svmulh[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMULH Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmulh[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   UMULH Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   UMULH Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; UMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svmulh[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; UMULH Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; UMULH Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> MultiplyReturningHighHalf(Vector<ulong> left, Vector<ulong> right) => MultiplyReturningHighHalf(left, right);


        ///  MultiplySubtract : Multiply-subtract, minuend first

        /// <summary>
        /// svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MSB Ztied2.B, Pg/M, Zop3.B, Zop1.B
        ///   MSB Ztied3.B, Pg/M, Zop2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MSB Zresult.B, Pg/M, Zop3.B, Zop1.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop3.B; MSB Zresult.B, Pg/M, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MSB Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   MSB Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MSB Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; MSB Zresult.H, Pg/M, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MSB Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   MSB Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MSB Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; MSB Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MSB Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   MSB Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MSB Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; MSB Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B
        ///   MSB Ztied2.B, Pg/M, Zop3.B, Zop1.B
        ///   MSB Ztied3.B, Pg/M, Zop2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        /// svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; MLS Zresult.B, Pg/M, Zop2.B, Zop3.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; MSB Zresult.B, Pg/M, Zop3.B, Zop1.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop3.B; MSB Zresult.B, Pg/M, Zop2.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H
        ///   MSB Ztied2.H, Pg/M, Zop3.H, Zop1.H
        ///   MSB Ztied3.H, Pg/M, Zop2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        /// svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; MLS Zresult.H, Pg/M, Zop2.H, Zop3.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; MSB Zresult.H, Pg/M, Zop3.H, Zop1.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop3.H; MSB Zresult.H, Pg/M, Zop2.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S
        ///   MSB Ztied2.S, Pg/M, Zop3.S, Zop1.S
        ///   MSB Ztied3.S, Pg/M, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        /// svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; MLS Zresult.S, Pg/M, Zop2.S, Zop3.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; MSB Zresult.S, Pg/M, Zop3.S, Zop1.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop3.S; MSB Zresult.S, Pg/M, Zop2.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D
        ///   MSB Ztied2.D, Pg/M, Zop3.D, Zop1.D
        ///   MSB Ztied3.D, Pg/M, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        /// svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; MLS Zresult.D, Pg/M, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; MSB Zresult.D, Pg/M, Zop3.D, Zop1.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop3.D; MSB Zresult.D, Pg/M, Zop2.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right) => MultiplySubtract(minuend, left, right);




        ///  Negate : Negate

        /// <summary>
        /// svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   NEG Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; NEG Zresult.B, Pg/M, Zop.B
        /// svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op)
        ///   NEG Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; NEG Zresult.B, Pg/M, Zop.B
        /// svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; NEG Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> Negate(Vector<sbyte> value) => Negate(value);

        /// <summary>
        /// svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   NEG Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; NEG Zresult.H, Pg/M, Zop.H
        /// svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op)
        ///   NEG Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; NEG Zresult.H, Pg/M, Zop.H
        /// svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; NEG Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> Negate(Vector<short> value) => Negate(value);

        /// <summary>
        /// svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   NEG Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; NEG Zresult.S, Pg/M, Zop.S
        /// svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op)
        ///   NEG Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; NEG Zresult.S, Pg/M, Zop.S
        /// svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; NEG Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> Negate(Vector<int> value) => Negate(value);

        /// <summary>
        /// svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   NEG Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; NEG Zresult.D, Pg/M, Zop.D
        /// svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op)
        ///   NEG Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; NEG Zresult.D, Pg/M, Zop.D
        /// svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; NEG Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> Negate(Vector<long> value) => Negate(value);

        /// <summary>
        /// svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FNEG Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FNEG Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FNEG Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FNEG Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FNEG Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> Negate(Vector<float> value) => Negate(value);

        /// <summary>
        /// svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FNEG Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FNEG Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FNEG Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FNEG Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FNEG Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> Negate(Vector<double> value) => Negate(value);




        ///  Not : Bitwise invert

        /// <summary>
        /// svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   NOT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.B, Pg/M, Zop.B
        /// svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op)
        ///   NOT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; NOT Zresult.B, Pg/M, Zop.B
        /// svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; NOT Zresult.B, Pg/M, Zop.B
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<sbyte> Not(Vector<sbyte> value) => Not(value);

        /// <summary>
        /// svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   NOT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.H, Pg/M, Zop.H
        /// svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op)
        ///   NOT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; NOT Zresult.H, Pg/M, Zop.H
        /// svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; NOT Zresult.H, Pg/M, Zop.H
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<short> Not(Vector<short> value) => Not(value);

        /// <summary>
        /// svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   NOT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.S, Pg/M, Zop.S
        /// svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op)
        ///   NOT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; NOT Zresult.S, Pg/M, Zop.S
        /// svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; NOT Zresult.S, Pg/M, Zop.S
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<int> Not(Vector<int> value) => Not(value);

        /// <summary>
        /// svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   NOT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.D, Pg/M, Zop.D
        /// svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op)
        ///   NOT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; NOT Zresult.D, Pg/M, Zop.D
        /// svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; NOT Zresult.D, Pg/M, Zop.D
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<long> Not(Vector<long> value) => Not(value);

        /// <summary>
        /// svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   NOT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op)
        ///   NOT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; NOT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; NOT Zresult.B, Pg/M, Zop.B
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<byte> Not(Vector<byte> value) => Not(value);

        /// <summary>
        /// svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   NOT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op)
        ///   NOT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; NOT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; NOT Zresult.H, Pg/M, Zop.H
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<ushort> Not(Vector<ushort> value) => Not(value);

        /// <summary>
        /// svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   NOT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op)
        ///   NOT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; NOT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; NOT Zresult.S, Pg/M, Zop.S
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<uint> Not(Vector<uint> value) => Not(value);

        /// <summary>
        /// svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   NOT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; NOT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op)
        ///   NOT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; NOT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; NOT Zresult.D, Pg/M, Zop.D
        /// svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)
        ///   EOR Presult.B, Pg/Z, Pop.B, Pg.B
        /// </summary>
        public static unsafe Vector<ulong> Not(Vector<ulong> value) => Not(value);


        ///  Or : Bitwise inclusive OR

        /// <summary>
        /// svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ORR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; ORR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right) => Or(left, right);

        /// <summary>
        /// svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ORR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; ORR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> Or(Vector<short> left, Vector<short> right) => Or(left, right);

        /// <summary>
        /// svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ORR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; ORR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> Or(Vector<int> left, Vector<int> right) => Or(left, right);

        /// <summary>
        /// svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ORR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; ORR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> Or(Vector<long> left, Vector<long> right) => Or(left, right);

        /// <summary>
        /// svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   ORR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ORR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ORR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; ORR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> Or(Vector<byte> left, Vector<byte> right) => Or(left, right);

        /// <summary>
        /// svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   ORR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ORR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ORR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; ORR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right) => Or(left, right);

        /// <summary>
        /// svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   ORR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ORR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ORR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; ORR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> Or(Vector<uint> left, Vector<uint> right) => Or(left, right);

        /// <summary>
        /// svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   ORR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ORR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   ORR Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ORR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; ORR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t svorr[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right) => Or(left, right);


        ///  OrAcross : Bitwise inclusive OR reduction to scalar

        /// <summary>
        /// int8_t svorv[_s8](svbool_t pg, svint8_t op)
        ///   ORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte OrAcross(Vector<sbyte> value) => OrAcross(value);

        /// <summary>
        /// int16_t svorv[_s16](svbool_t pg, svint16_t op)
        ///   ORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short OrAcross(Vector<short> value) => OrAcross(value);

        /// <summary>
        /// int32_t svorv[_s32](svbool_t pg, svint32_t op)
        ///   ORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int OrAcross(Vector<int> value) => OrAcross(value);

        /// <summary>
        /// int64_t svorv[_s64](svbool_t pg, svint64_t op)
        ///   ORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long OrAcross(Vector<long> value) => OrAcross(value);

        /// <summary>
        /// uint8_t svorv[_u8](svbool_t pg, svuint8_t op)
        ///   ORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte OrAcross(Vector<byte> value) => OrAcross(value);

        /// <summary>
        /// uint16_t svorv[_u16](svbool_t pg, svuint16_t op)
        ///   ORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort OrAcross(Vector<ushort> value) => OrAcross(value);

        /// <summary>
        /// uint32_t svorv[_u32](svbool_t pg, svuint32_t op)
        ///   ORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint OrAcross(Vector<uint> value) => OrAcross(value);

        /// <summary>
        /// uint64_t svorv[_u64](svbool_t pg, svuint64_t op)
        ///   ORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong OrAcross(Vector<ulong> value) => OrAcross(value);


        ///  OrNot : Bitwise NOR

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> OrNot(Vector<sbyte> left, Vector<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> OrNot(Vector<short> left, Vector<short> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> OrNot(Vector<int> left, Vector<int> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> OrNot(Vector<long> left, Vector<long> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> OrNot(Vector<byte> left, Vector<byte> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> OrNot(Vector<ushort> left, Vector<ushort> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> OrNot(Vector<uint> left, Vector<uint> right) => OrNot(left, right);

        /// <summary>
        /// svbool_t svnor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   NOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// svbool_t svorn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   ORN Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> OrNot(Vector<ulong> left, Vector<ulong> right) => OrNot(left, right);


        ///  PopCount : Count nonzero bits

        /// <summary>
        /// svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)
        ///   CNT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op)
        ///   CNT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CNT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CNT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<sbyte> value) => PopCount(value);

        /// <summary>
        /// svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   CNT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op)
        ///   CNT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; CNT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; CNT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> PopCount(Vector<byte> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op)
        ///   CNT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<short> value) => PopCount(value);

        /// <summary>
        /// svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   CNT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op)
        ///   CNT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; CNT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; CNT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> PopCount(Vector<ushort> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op)
        ///   CNT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<int> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   CNT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<float> value) => PopCount(value);

        /// <summary>
        /// svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   CNT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op)
        ///   CNT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; CNT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; CNT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> PopCount(Vector<uint> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op)
        ///   CNT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<long> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   CNT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<double> value) => PopCount(value);

        /// <summary>
        /// svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   CNT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op)
        ///   CNT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; CNT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; CNT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> PopCount(Vector<ulong> value) => PopCount(value);


        ///  PrefetchBytes : Prefetch bytes

        /// <summary>
        /// void svprfb(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFB op, Pg, [Xarray, Xindex]
        ///   PRFB op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchBytes(Vector<byte> mask, const void *base, enum SvePrefetchType op) => PrefetchBytes(mask, void, SvePrefetchType);


        ///  PrefetchInt16 : Prefetch halfwords

        /// <summary>
        /// void svprfh(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFH op, Pg, [Xarray, Xindex, LSL #1]
        ///   PRFH op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt16(Vector<ushort> mask, const void *base, enum SvePrefetchType op) => PrefetchInt16(mask, void, SvePrefetchType);


        ///  PrefetchInt32 : Prefetch words

        /// <summary>
        /// void svprfw(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFW op, Pg, [Xarray, Xindex, LSL #2]
        ///   PRFW op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt32(Vector<uint> mask, const void *base, enum SvePrefetchType op) => PrefetchInt32(mask, void, SvePrefetchType);


        ///  PrefetchInt64 : Prefetch doublewords

        /// <summary>
        /// void svprfd(svbool_t pg, const void *base, enum svprfop op)
        ///   PRFD op, Pg, [Xarray, Xindex, LSL #3]
        ///   PRFD op, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void PrefetchInt64(Vector<ulong> mask, const void *base, enum SvePrefetchType op) => PrefetchInt64(mask, void, SvePrefetchType);


        ///  PropagateBreak : Propagate break to next partition

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<sbyte> PropagateBreak(Vector<sbyte> left, Vector<sbyte> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<short> PropagateBreak(Vector<short> left, Vector<short> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<int> PropagateBreak(Vector<int> left, Vector<int> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<long> PropagateBreak(Vector<long> left, Vector<long> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<byte> PropagateBreak(Vector<byte> left, Vector<byte> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<ushort> PropagateBreak(Vector<ushort> left, Vector<ushort> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<uint> PropagateBreak(Vector<uint> left, Vector<uint> right) => PropagateBreak(left, right);

        /// <summary>
        /// svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B
        /// </summary>
        public static unsafe Vector<ulong> PropagateBreak(Vector<ulong> left, Vector<ulong> right) => PropagateBreak(left, right);


        ///  ReciprocalEstimate : Reciprocal estimate

        /// <summary>
        /// svfloat32_t svrecpe[_f32](svfloat32_t op)
        ///   FRECPE Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalEstimate(Vector<float> value) => ReciprocalEstimate(value);

        /// <summary>
        /// svfloat64_t svrecpe[_f64](svfloat64_t op)
        ///   FRECPE Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalEstimate(Vector<double> value) => ReciprocalEstimate(value);


        ///  ReciprocalExponent : Reciprocal exponent

        /// <summary>
        /// svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRECPX Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRECPX Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRECPX Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRECPX Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRECPX Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalExponent(Vector<float> value) => ReciprocalExponent(value);

        /// <summary>
        /// svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRECPX Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRECPX Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRECPX Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRECPX Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRECPX Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalExponent(Vector<double> value) => ReciprocalExponent(value);


        ///  ReciprocalSqrtEstimate : Reciprocal square root estimate

        /// <summary>
        /// svfloat32_t svrsqrte[_f32](svfloat32_t op)
        ///   FRSQRTE Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtEstimate(Vector<float> value) => ReciprocalSqrtEstimate(value);

        /// <summary>
        /// svfloat64_t svrsqrte[_f64](svfloat64_t op)
        ///   FRSQRTE Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtEstimate(Vector<double> value) => ReciprocalSqrtEstimate(value);


        ///  ReciprocalSqrtStep : Reciprocal square root step

        /// <summary>
        /// svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   FRSQRTS Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right) => ReciprocalSqrtStep(left, right);

        /// <summary>
        /// svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   FRSQRTS Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right) => ReciprocalSqrtStep(left, right);


        ///  ReciprocalStep : Reciprocal step

        /// <summary>
        /// svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   FRECPS Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right) => ReciprocalStep(left, right);

        /// <summary>
        /// svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   FRECPS Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right) => ReciprocalStep(left, right);


        ///  ReverseBits : Reverse bits

        /// <summary>
        /// svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)
        ///   RBIT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.B, Pg/M, Zop.B
        /// svint8_t svrbit[_s8]_x(svbool_t pg, svint8_t op)
        ///   RBIT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.B, Pg/M, Zop.B
        /// svint8_t svrbit[_s8]_z(svbool_t pg, svint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; RBIT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> ReverseBits(Vector<sbyte> value) => ReverseBits(value);

        /// <summary>
        /// svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   RBIT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.H, Pg/M, Zop.H
        /// svint16_t svrbit[_s16]_x(svbool_t pg, svint16_t op)
        ///   RBIT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.H, Pg/M, Zop.H
        /// svint16_t svrbit[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; RBIT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> ReverseBits(Vector<short> value) => ReverseBits(value);

        /// <summary>
        /// svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   RBIT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.S, Pg/M, Zop.S
        /// svint32_t svrbit[_s32]_x(svbool_t pg, svint32_t op)
        ///   RBIT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.S, Pg/M, Zop.S
        /// svint32_t svrbit[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; RBIT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseBits(Vector<int> value) => ReverseBits(value);

        /// <summary>
        /// svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   RBIT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.D, Pg/M, Zop.D
        /// svint64_t svrbit[_s64]_x(svbool_t pg, svint64_t op)
        ///   RBIT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.D, Pg/M, Zop.D
        /// svint64_t svrbit[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; RBIT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseBits(Vector<long> value) => ReverseBits(value);

        /// <summary>
        /// svuint8_t svrbit[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)
        ///   RBIT Ztied.B, Pg/M, Zop.B
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op)
        ///   RBIT Ztied.B, Pg/M, Ztied.B
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.B, Pg/M, Zop.B
        /// svuint8_t svrbit[_u8]_z(svbool_t pg, svuint8_t op)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop.B; RBIT Zresult.B, Pg/M, Zop.B
        /// </summary>
        public static unsafe Vector<byte> ReverseBits(Vector<byte> value) => ReverseBits(value);

        /// <summary>
        /// svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   RBIT Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svrbit[_u16]_x(svbool_t pg, svuint16_t op)
        ///   RBIT Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.H, Pg/M, Zop.H
        /// svuint16_t svrbit[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; RBIT Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseBits(Vector<ushort> value) => ReverseBits(value);

        /// <summary>
        /// svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   RBIT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrbit[_u32]_x(svbool_t pg, svuint32_t op)
        ///   RBIT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrbit[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; RBIT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseBits(Vector<uint> value) => ReverseBits(value);

        /// <summary>
        /// svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   RBIT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; RBIT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrbit[_u64]_x(svbool_t pg, svuint64_t op)
        ///   RBIT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; RBIT Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrbit[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; RBIT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseBits(Vector<ulong> value) => ReverseBits(value);


        ///  ReverseBytesWithinElements : Reverse bytes within elements

        /// <summary>
        /// svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)
        ///   REVB Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.H, Pg/M, Zop.H
        /// svint16_t svrevb[_s16]_x(svbool_t pg, svint16_t op)
        ///   REVB Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; REVB Zresult.H, Pg/M, Zop.H
        /// svint16_t svrevb[_s16]_z(svbool_t pg, svint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; REVB Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<short> ReverseBytesWithinElements(Vector<short> value) => ReverseBytesWithinElements(value);

        /// <summary>
        /// svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   REVB Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.S, Pg/M, Zop.S
        /// svint32_t svrevb[_s32]_x(svbool_t pg, svint32_t op)
        ///   REVB Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; REVB Zresult.S, Pg/M, Zop.S
        /// svint32_t svrevb[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; REVB Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseBytesWithinElements(Vector<int> value) => ReverseBytesWithinElements(value);

        /// <summary>
        /// svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVB Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevb[_s64]_x(svbool_t pg, svint64_t op)
        ///   REVB Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVB Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevb[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVB Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseBytesWithinElements(Vector<long> value) => ReverseBytesWithinElements(value);

        /// <summary>
        /// svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)
        ///   REVB Ztied.H, Pg/M, Zop.H
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.H, Pg/M, Zop.H
        /// svuint16_t svrevb[_u16]_x(svbool_t pg, svuint16_t op)
        ///   REVB Ztied.H, Pg/M, Ztied.H
        ///   MOVPRFX Zresult, Zop; REVB Zresult.H, Pg/M, Zop.H
        /// svuint16_t svrevb[_u16]_z(svbool_t pg, svuint16_t op)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop.H; REVB Zresult.H, Pg/M, Zop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseBytesWithinElements(Vector<ushort> value) => ReverseBytesWithinElements(value);

        /// <summary>
        /// svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   REVB Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrevb[_u32]_x(svbool_t pg, svuint32_t op)
        ///   REVB Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; REVB Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrevb[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; REVB Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseBytesWithinElements(Vector<uint> value) => ReverseBytesWithinElements(value);

        /// <summary>
        /// svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVB Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVB Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevb[_u64]_x(svbool_t pg, svuint64_t op)
        ///   REVB Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVB Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevb[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVB Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseBytesWithinElements(Vector<ulong> value) => ReverseBytesWithinElements(value);


        ///  ReverseElement : Reverse all elements

        /// <summary>
        /// svint8_t svrev[_s8](svint8_t op)
        ///   REV Zresult.B, Zop.B
        /// </summary>
        public static unsafe Vector<sbyte> ReverseElement(Vector<sbyte> value) => ReverseElement(value);

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
        /// svuint8_t svrev[_u8](svuint8_t op)
        ///   REV Zresult.B, Zop.B
        /// svbool_t svrev_b8(svbool_t op)
        ///   REV Presult.B, Pop.B
        /// </summary>
        public static unsafe Vector<byte> ReverseElement(Vector<byte> value) => ReverseElement(value);

        /// <summary>
        /// svuint16_t svrev[_u16](svuint16_t op)
        ///   REV Zresult.H, Zop.H
        /// svbool_t svrev_b16(svbool_t op)
        ///   REV Presult.H, Pop.H
        /// </summary>
        public static unsafe Vector<ushort> ReverseElement(Vector<ushort> value) => ReverseElement(value);

        /// <summary>
        /// svuint32_t svrev[_u32](svuint32_t op)
        ///   REV Zresult.S, Zop.S
        /// svbool_t svrev_b32(svbool_t op)
        ///   REV Presult.S, Pop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseElement(Vector<uint> value) => ReverseElement(value);

        /// <summary>
        /// svuint64_t svrev[_u64](svuint64_t op)
        ///   REV Zresult.D, Zop.D
        /// svbool_t svrev_b64(svbool_t op)
        ///   REV Presult.D, Pop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseElement(Vector<ulong> value) => ReverseElement(value);

        /// <summary>
        /// svfloat32_t svrev[_f32](svfloat32_t op)
        ///   REV Zresult.S, Zop.S
        /// </summary>
        public static unsafe Vector<float> ReverseElement(Vector<float> value) => ReverseElement(value);

        /// <summary>
        /// svfloat64_t svrev[_f64](svfloat64_t op)
        ///   REV Zresult.D, Zop.D
        /// </summary>
        public static unsafe Vector<double> ReverseElement(Vector<double> value) => ReverseElement(value);


        ///  ReverseInt16WithinElements : Reverse halfwords within elements

        /// <summary>
        /// svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)
        ///   REVH Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; REVH Zresult.S, Pg/M, Zop.S
        /// svint32_t svrevh[_s32]_x(svbool_t pg, svint32_t op)
        ///   REVH Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; REVH Zresult.S, Pg/M, Zop.S
        /// svint32_t svrevh[_s32]_z(svbool_t pg, svint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; REVH Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<int> ReverseInt16WithinElements(Vector<int> value) => ReverseInt16WithinElements(value);

        /// <summary>
        /// svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVH Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVH Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevh[_s64]_x(svbool_t pg, svint64_t op)
        ///   REVH Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVH Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevh[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVH Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseInt16WithinElements(Vector<long> value) => ReverseInt16WithinElements(value);

        /// <summary>
        /// svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)
        ///   REVH Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; REVH Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrevh[_u32]_x(svbool_t pg, svuint32_t op)
        ///   REVH Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; REVH Zresult.S, Pg/M, Zop.S
        /// svuint32_t svrevh[_u32]_z(svbool_t pg, svuint32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; REVH Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<uint> ReverseInt16WithinElements(Vector<uint> value) => ReverseInt16WithinElements(value);

        /// <summary>
        /// svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVH Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVH Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevh[_u64]_x(svbool_t pg, svuint64_t op)
        ///   REVH Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVH Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevh[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVH Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseInt16WithinElements(Vector<ulong> value) => ReverseInt16WithinElements(value);


        ///  ReverseInt32WithinElements : Reverse words within elements

        /// <summary>
        /// svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)
        ///   REVW Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVW Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevw[_s64]_x(svbool_t pg, svint64_t op)
        ///   REVW Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVW Zresult.D, Pg/M, Zop.D
        /// svint64_t svrevw[_s64]_z(svbool_t pg, svint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVW Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<long> ReverseInt32WithinElements(Vector<long> value) => ReverseInt32WithinElements(value);

        /// <summary>
        /// svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)
        ///   REVW Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; REVW Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevw[_u64]_x(svbool_t pg, svuint64_t op)
        ///   REVW Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; REVW Zresult.D, Pg/M, Zop.D
        /// svuint64_t svrevw[_u64]_z(svbool_t pg, svuint64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; REVW Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<ulong> ReverseInt32WithinElements(Vector<ulong> value) => ReverseInt32WithinElements(value);


        ///  RoundAwayFromZero : Round to nearest, ties away from zero

        /// <summary>
        /// svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTA Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRINTA Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTA Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRINTA Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTA Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> RoundAwayFromZero(Vector<float> value) => RoundAwayFromZero(value);

        /// <summary>
        /// svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTA Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRINTA Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTA Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRINTA Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTA Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> RoundAwayFromZero(Vector<double> value) => RoundAwayFromZero(value);


        ///  RoundToNearest : Round to nearest, ties to even

        /// <summary>
        /// svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTN Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRINTN Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTN Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRINTN Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTN Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> RoundToNearest(Vector<float> value) => RoundToNearest(value);

        /// <summary>
        /// svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTN Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRINTN Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTN Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRINTN Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTN Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> RoundToNearest(Vector<double> value) => RoundToNearest(value);


        ///  RoundToNegativeInfinity : Round towards -∞

        /// <summary>
        /// svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTM Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRINTM Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTM Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRINTM Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTM Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> RoundToNegativeInfinity(Vector<float> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTM Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRINTM Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTM Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRINTM Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTM Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> RoundToNegativeInfinity(Vector<double> value) => RoundToNegativeInfinity(value);


        ///  RoundToPositiveInfinity : Round towards +∞

        /// <summary>
        /// svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTP Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRINTP Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTP Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRINTP Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTP Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> RoundToPositiveInfinity(Vector<float> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTP Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRINTP Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTP Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRINTP Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTP Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> RoundToPositiveInfinity(Vector<double> value) => RoundToPositiveInfinity(value);


        ///  RoundToZero : Round towards zero

        /// <summary>
        /// svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FRINTZ Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FRINTZ Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FRINTZ Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FRINTZ Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FRINTZ Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> RoundToZero(Vector<float> value) => RoundToZero(value);

        /// <summary>
        /// svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FRINTZ Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FRINTZ Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FRINTZ Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FRINTZ Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FRINTZ Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> RoundToZero(Vector<double> value) => RoundToZero(value);




        ///  SaturatingDecrementByActiveElementCount : Saturating decrement by active element count

        /// <summary>
        /// svint16_t svqdecp[_s16](svint16_t op, svbool_t pg)
        ///   SQDECP Ztied.H, Pg
        ///   MOVPRFX Zresult, Zop; SQDECP Zresult.H, Pg
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> op, Vector<short> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// svint32_t svqdecp[_s32](svint32_t op, svbool_t pg)
        ///   SQDECP Ztied.S, Pg
        ///   MOVPRFX Zresult, Zop; SQDECP Zresult.S, Pg
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> op, Vector<int> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// svint64_t svqdecp[_s64](svint64_t op, svbool_t pg)
        ///   SQDECP Ztied.D, Pg
        ///   MOVPRFX Zresult, Zop; SQDECP Zresult.D, Pg
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> op, Vector<long> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.B, Wtied
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int op, Vector<byte> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.B
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long op, Vector<byte> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.B
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint op, Vector<byte> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong op, Vector<byte> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.H, Wtied
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int op, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.H
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long op, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.H
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint op, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong op, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg)
        ///   UQDECP Ztied.H, Pg
        ///   MOVPRFX Zresult, Zop; UQDECP Zresult.H, Pg
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> op, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.S, Wtied
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int op, Vector<uint> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.S
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long op, Vector<uint> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.S
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint op, Vector<uint> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong op, Vector<uint> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg)
        ///   UQDECP Ztied.S, Pg
        ///   MOVPRFX Zresult, Zop; UQDECP Zresult.S, Pg
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> op, Vector<uint> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.D, Wtied
        /// </summary>
        public static unsafe int SaturatingDecrementByActiveElementCount(int op, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg)
        ///   SQDECP Xtied, Pg.D
        /// </summary>
        public static unsafe long SaturatingDecrementByActiveElementCount(long op, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg)
        ///   UQDECP Wtied, Pg.D
        /// </summary>
        public static unsafe uint SaturatingDecrementByActiveElementCount(uint op, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg)
        ///   UQDECP Xtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingDecrementByActiveElementCount(ulong op, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg)
        ///   UQDECP Ztied.D, Pg
        ///   MOVPRFX Zresult, Zop; UQDECP Zresult.D, Pg
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> op, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(op, from);


        ///  SaturatingDecrementByteElementCount : Saturating decrement by number of byte elements

        /// <summary>
        /// int32_t svqdecb[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQDECB Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementByteElementCount(int op, ulong imm_factor) => SaturatingDecrementByteElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqdecb[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQDECB Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementByteElementCount(long op, ulong imm_factor) => SaturatingDecrementByteElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqdecb[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQDECB Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementByteElementCount(uint op, ulong imm_factor) => SaturatingDecrementByteElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqdecb[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQDECB Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementByteElementCount(ulong op, ulong imm_factor) => SaturatingDecrementByteElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECB Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementByteElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementByteElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECB Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementByteElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementByteElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementByteElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingDecrementInt16ElementCount : Saturating decrement by number of halfword elements

        /// <summary>
        /// int32_t svqdech[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQDECH Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt16ElementCount(int op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqdech[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQDECH Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt16ElementCount(long op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqdech[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQDECH Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt16ElementCount(uint op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqdech[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQDECH Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt16ElementCount(ulong op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt16ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt16ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt16ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt16ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint16_t svqdech[_s16](svint16_t op, uint64_t imm_factor)
        ///   SQDECH Ztied.H, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECH Zresult.H, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementInt16ElementCount(Vector<short> op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECH Ztied.H, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECH Zresult.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingDecrementInt16ElementCount(Vector<short> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint16_t svqdech[_u16](svuint16_t op, uint64_t imm_factor)
        ///   UQDECH Ztied.H, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECH Zresult.H, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementInt16ElementCount(Vector<ushort> op, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECH Ztied.H, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECH Zresult.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingDecrementInt16ElementCount(Vector<ushort> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt16ElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingDecrementInt32ElementCount : Saturating decrement by number of word elements

        /// <summary>
        /// int32_t svqdecw[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQDECW Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt32ElementCount(int op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqdecw[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQDECW Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt32ElementCount(long op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqdecw[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQDECW Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt32ElementCount(uint op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqdecw[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQDECW Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt32ElementCount(ulong op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt32ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt32ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt32ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt32ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint32_t svqdecw[_s32](svint32_t op, uint64_t imm_factor)
        ///   SQDECW Ztied.S, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECW Zresult.S, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementInt32ElementCount(Vector<int> op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECW Ztied.S, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECW Zresult.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingDecrementInt32ElementCount(Vector<int> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint32_t svqdecw[_u32](svuint32_t op, uint64_t imm_factor)
        ///   UQDECW Ztied.S, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECW Zresult.S, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementInt32ElementCount(Vector<uint> op, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECW Ztied.S, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECW Zresult.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingDecrementInt32ElementCount(Vector<uint> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt32ElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingDecrementInt64ElementCount : Saturating decrement by number of doubleword elements

        /// <summary>
        /// int32_t svqdecd[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQDECD Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt64ElementCount(int op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqdecd[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQDECD Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt64ElementCount(long op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqdecd[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQDECD Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt64ElementCount(uint op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqdecd[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQDECD Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt64ElementCount(ulong op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingDecrementInt64ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingDecrementInt64ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingDecrementInt64ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingDecrementInt64ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint64_t svqdecd[_s64](svint64_t op, uint64_t imm_factor)
        ///   SQDECD Ztied.D, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECD Zresult.D, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementInt64ElementCount(Vector<long> op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQDECD Ztied.D, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQDECD Zresult.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingDecrementInt64ElementCount(Vector<long> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint64_t svqdecd[_u64](svuint64_t op, uint64_t imm_factor)
        ///   UQDECD Ztied.D, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECD Zresult.D, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementInt64ElementCount(Vector<ulong> op, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQDECD Ztied.D, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQDECD Zresult.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingDecrementInt64ElementCount(Vector<ulong> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingDecrementInt64ElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingIncrementByActiveElementCount : Saturating increment by active element count

        /// <summary>
        /// svint16_t svqincp[_s16](svint16_t op, svbool_t pg)
        ///   SQINCP Ztied.H, Pg
        ///   MOVPRFX Zresult, Zop; SQINCP Zresult.H, Pg
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> op, Vector<short> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// svint32_t svqincp[_s32](svint32_t op, svbool_t pg)
        ///   SQINCP Ztied.S, Pg
        ///   MOVPRFX Zresult, Zop; SQINCP Zresult.S, Pg
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> op, Vector<int> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// svint64_t svqincp[_s64](svint64_t op, svbool_t pg)
        ///   SQINCP Ztied.D, Pg
        ///   MOVPRFX Zresult, Zop; SQINCP Zresult.D, Pg
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> op, Vector<long> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.B, Wtied
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int op, Vector<byte> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.B
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long op, Vector<byte> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.B
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint op, Vector<byte> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.B
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong op, Vector<byte> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.H, Wtied
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int op, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.H
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long op, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.H
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint op, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.H
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong op, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg)
        ///   UQINCP Ztied.H, Pg
        ///   MOVPRFX Zresult, Zop; UQINCP Zresult.H, Pg
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> op, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.S, Wtied
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int op, Vector<uint> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.S
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long op, Vector<uint> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.S
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint op, Vector<uint> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.S
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong op, Vector<uint> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg)
        ///   UQINCP Ztied.S, Pg
        ///   MOVPRFX Zresult, Zop; UQINCP Zresult.S, Pg
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> op, Vector<uint> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.D, Wtied
        /// </summary>
        public static unsafe int SaturatingIncrementByActiveElementCount(int op, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg)
        ///   SQINCP Xtied, Pg.D
        /// </summary>
        public static unsafe long SaturatingIncrementByActiveElementCount(long op, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg)
        ///   UQINCP Wtied, Pg.D
        /// </summary>
        public static unsafe uint SaturatingIncrementByActiveElementCount(uint op, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg)
        ///   UQINCP Xtied, Pg.D
        /// </summary>
        public static unsafe ulong SaturatingIncrementByActiveElementCount(ulong op, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(op, from);

        /// <summary>
        /// svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg)
        ///   UQINCP Ztied.D, Pg
        ///   MOVPRFX Zresult, Zop; UQINCP Zresult.D, Pg
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> op, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(op, from);


        ///  SaturatingIncrementByteElementCount : Saturating increment by number of byte elements

        /// <summary>
        /// int32_t svqincb[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQINCB Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementByteElementCount(int op, ulong imm_factor) => SaturatingIncrementByteElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqincb[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQINCB Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementByteElementCount(long op, ulong imm_factor) => SaturatingIncrementByteElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqincb[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQINCB Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementByteElementCount(uint op, ulong imm_factor) => SaturatingIncrementByteElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqincb[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQINCB Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementByteElementCount(ulong op, ulong imm_factor) => SaturatingIncrementByteElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCB Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementByteElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementByteElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCB Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementByteElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementByteElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCB Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementByteElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementByteElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingIncrementInt16ElementCount : Saturating increment by number of halfword elements

        /// <summary>
        /// int32_t svqinch[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQINCH Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt16ElementCount(int op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqinch[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQINCH Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt16ElementCount(long op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqinch[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQINCH Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt16ElementCount(uint op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqinch[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQINCH Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt16ElementCount(ulong op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt16ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt16ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt16ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt16ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint16_t svqinch[_s16](svint16_t op, uint64_t imm_factor)
        ///   SQINCH Ztied.H, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCH Zresult.H, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementInt16ElementCount(Vector<short> op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCH Ztied.H, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCH Zresult.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<short> SaturatingIncrementInt16ElementCount(Vector<short> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint16_t svqinch[_u16](svuint16_t op, uint64_t imm_factor)
        ///   UQINCH Ztied.H, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCH Zresult.H, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementInt16ElementCount(Vector<ushort> op, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, imm_factor);

        /// <summary>
        /// svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCH Ztied.H, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCH Zresult.H, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ushort> SaturatingIncrementInt16ElementCount(Vector<ushort> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt16ElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingIncrementInt32ElementCount : Saturating increment by number of word elements

        /// <summary>
        /// int32_t svqincw[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQINCW Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt32ElementCount(int op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqincw[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQINCW Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt32ElementCount(long op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqincw[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQINCW Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt32ElementCount(uint op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqincw[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQINCW Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt32ElementCount(ulong op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt32ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt32ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt32ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt32ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint32_t svqincw[_s32](svint32_t op, uint64_t imm_factor)
        ///   SQINCW Ztied.S, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCW Zresult.S, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementInt32ElementCount(Vector<int> op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCW Ztied.S, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCW Zresult.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<int> SaturatingIncrementInt32ElementCount(Vector<int> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint32_t svqincw[_u32](svuint32_t op, uint64_t imm_factor)
        ///   UQINCW Ztied.S, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCW Zresult.S, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementInt32ElementCount(Vector<uint> op, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, imm_factor);

        /// <summary>
        /// svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCW Ztied.S, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCW Zresult.S, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<uint> SaturatingIncrementInt32ElementCount(Vector<uint> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt32ElementCount(op, SveMaskPattern, imm_factor);


        ///  SaturatingIncrementInt64ElementCount : Saturating increment by number of doubleword elements

        /// <summary>
        /// int32_t svqincd[_n_s32](int32_t op, uint64_t imm_factor)
        ///   SQINCD Xtied, Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt64ElementCount(int op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// int64_t svqincd[_n_s64](int64_t op, uint64_t imm_factor)
        ///   SQINCD Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt64ElementCount(long op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// uint32_t svqincd[_n_u32](uint32_t op, uint64_t imm_factor)
        ///   UQINCD Wtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt64ElementCount(uint op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// uint64_t svqincd[_n_u64](uint64_t op, uint64_t imm_factor)
        ///   UQINCD Xtied, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt64ElementCount(ulong op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Xtied, Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe int SaturatingIncrementInt64ElementCount(int op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe long SaturatingIncrementInt64ElementCount(long op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Wtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe uint SaturatingIncrementInt64ElementCount(uint op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Xtied, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe ulong SaturatingIncrementInt64ElementCount(ulong op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svint64_t svqincd[_s64](svint64_t op, uint64_t imm_factor)
        ///   SQINCD Ztied.D, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCD Zresult.D, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementInt64ElementCount(Vector<long> op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   SQINCD Ztied.D, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; SQINCD Zresult.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<long> SaturatingIncrementInt64ElementCount(Vector<long> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);

        /// <summary>
        /// svuint64_t svqincd[_u64](svuint64_t op, uint64_t imm_factor)
        ///   UQINCD Ztied.D, ALL, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCD Zresult.D, ALL, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementInt64ElementCount(Vector<ulong> op, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, imm_factor);

        /// <summary>
        /// svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)
        ///   UQINCD Ztied.D, pattern, MUL #imm_factor
        ///   MOVPRFX Zresult, Zop; UQINCD Zresult.D, pattern, MUL #imm_factor
        /// </summary>
        public static unsafe Vector<ulong> SaturatingIncrementInt64ElementCount(Vector<ulong> op, enum SveMaskPattern pattern, ulong imm_factor) => SaturatingIncrementInt64ElementCount(op, SveMaskPattern, imm_factor);


        ///  Scale : Adjust exponent

        /// <summary>
        /// svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2)
        ///   FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2)
        ///   FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSCALE Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> Scale(Vector<float> left, Vector<int> right) => Scale(left, right);

        /// <summary>
        /// svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2)
        ///   FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2)
        ///   FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSCALE Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> Scale(Vector<double> left, Vector<long> right) => Scale(left, right);


        ///  Scatter : Non-truncating store

        /// <summary>
        /// void svst1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, Vector<uint> bases, Vector<int> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[s32]offset[_s32](svbool_t pg, int32_t *base, svint32_t offsets, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int *base, Vector<int> offsets, Vector<int> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int *base, Vector<uint> offsets, Vector<int> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s32]index[_s32](svbool_t pg, int32_t *base, svint32_t indices, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int *base, Vector<int> indices, Vector<int> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter_[u32]index[_s32](svbool_t pg, int32_t *base, svuint32_t indices, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int *base, Vector<uint> indices, Vector<int> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_offset[_s32](svbool_t pg, svuint32_t bases, int64_t offset, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, Vector<uint> bases, long offset, Vector<int> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_index[_s32](svbool_t pg, svuint32_t bases, int64_t index, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #index * 4]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, Vector<uint> bases, long index, Vector<int> data) => Scatter(mask, bases, index, data);

        /// <summary>
        /// void svst1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, Vector<ulong> bases, Vector<long> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long *base, Vector<long> offsets, Vector<long> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long *base, Vector<ulong> offsets, Vector<long> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s64]index[_s64](svbool_t pg, int64_t *base, svint64_t indices, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long *base, Vector<long> indices, Vector<long> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter_[u64]index[_s64](svbool_t pg, int64_t *base, svuint64_t indices, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long *base, Vector<ulong> indices, Vector<long> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_offset[_s64](svbool_t pg, svuint64_t bases, int64_t offset, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, Vector<ulong> bases, long offset, Vector<long> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_index[_s64](svbool_t pg, svuint64_t bases, int64_t index, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #index * 8]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, Vector<ulong> bases, long index, Vector<long> data) => Scatter(mask, bases, index, data);

        /// <summary>
        /// void svst1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, Vector<uint> bases, Vector<uint> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[s32]offset[_u32](svbool_t pg, uint32_t *base, svint32_t offsets, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint *base, Vector<int> offsets, Vector<uint> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint *base, Vector<uint> offsets, Vector<uint> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s32]index[_u32](svbool_t pg, uint32_t *base, svint32_t indices, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint *base, Vector<int> indices, Vector<uint> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter_[u32]index[_u32](svbool_t pg, uint32_t *base, svuint32_t indices, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint *base, Vector<uint> indices, Vector<uint> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_offset[_u32](svbool_t pg, svuint32_t bases, int64_t offset, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, Vector<uint> bases, long offset, Vector<uint> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_index[_u32](svbool_t pg, svuint32_t bases, int64_t index, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #index * 4]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, Vector<uint> bases, long index, Vector<uint> data) => Scatter(mask, bases, index, data);

        /// <summary>
        /// void svst1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> bases, Vector<ulong> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong *base, Vector<long> offsets, Vector<ulong> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong *base, Vector<ulong> offsets, Vector<ulong> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s64]index[_u64](svbool_t pg, uint64_t *base, svint64_t indices, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong *base, Vector<long> indices, Vector<ulong> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter_[u64]index[_u64](svbool_t pg, uint64_t *base, svuint64_t indices, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong *base, Vector<ulong> indices, Vector<ulong> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_offset[_u64](svbool_t pg, svuint64_t bases, int64_t offset, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> bases, long offset, Vector<ulong> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_index[_u64](svbool_t pg, svuint64_t bases, int64_t index, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #index * 8]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, Vector<ulong> bases, long index, Vector<ulong> data) => Scatter(mask, bases, index, data);

        /// <summary>
        /// void svst1_scatter_[s32]offset[_f32](svbool_t pg, float32_t *base, svint32_t offsets, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float *base, Vector<int> offsets, Vector<float> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s32]index[_f32](svbool_t pg, float32_t *base, svint32_t indices, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, SXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float *base, Vector<int> indices, Vector<float> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, Vector<uint> bases, Vector<float> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float *base, Vector<uint> offsets, Vector<float> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u32]index[_f32](svbool_t pg, float32_t *base, svuint32_t indices, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xbase, Zindices.S, UXTW #2]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float *base, Vector<uint> indices, Vector<float> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_offset[_f32](svbool_t pg, svuint32_t bases, int64_t offset, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, Vector<uint> bases, long offset, Vector<float> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u32base]_index[_f32](svbool_t pg, svuint32_t bases, int64_t index, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Zbases.S, #index * 4]
        ///   ST1W Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, Vector<uint> bases, long index, Vector<float> data) => Scatter(mask, bases, index, data);

        /// <summary>
        /// void svst1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double *base, Vector<long> offsets, Vector<double> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[s64]index[_f64](svbool_t pg, float64_t *base, svint64_t indices, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double *base, Vector<long> indices, Vector<double> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, Vector<ulong> bases, Vector<double> data) => Scatter(mask, bases, data);

        /// <summary>
        /// void svst1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double *base, Vector<ulong> offsets, Vector<double> data) => Scatter(mask, *base, offsets, data);

        /// <summary>
        /// void svst1_scatter_[u64]index[_f64](svbool_t pg, float64_t *base, svuint64_t indices, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xbase, Zindices.D, LSL #3]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double *base, Vector<ulong> indices, Vector<double> data) => Scatter(mask, *base, indices, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_offset[_f64](svbool_t pg, svuint64_t bases, int64_t offset, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, Vector<ulong> bases, long offset, Vector<double> data) => Scatter(mask, bases, offset, data);

        /// <summary>
        /// void svst1_scatter[_u64base]_index[_f64](svbool_t pg, svuint64_t bases, int64_t index, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Zbases.D, #index * 8]
        ///   ST1D Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, Vector<ulong> bases, long index, Vector<double> data) => Scatter(mask, bases, index, data);


        ///  ScatterInt32NarrowToInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToInt16(Vector<int> mask, Vector<uint> bases, Vector<int> data) => ScatterInt32NarrowToInt16(mask, bases, data);

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_s32](svbool_t pg, int16_t *base, svint32_t offsets, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToInt16(Vector<int> mask, short *base, Vector<int> offsets, Vector<int> data) => ScatterInt32NarrowToInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s32]index[_s32](svbool_t pg, int16_t *base, svint32_t indices, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToInt16(Vector<int> mask, short *base, Vector<int> indices, Vector<int> data) => ScatterInt32NarrowToInt16(mask, *base, indices, data);

        /// <summary>
        /// void svst1h_scatter_[s32]offset[_u32](svbool_t pg, uint16_t *base, svint32_t offsets, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToInt16(Vector<uint> mask, ushort *base, Vector<int> offsets, Vector<uint> data) => ScatterInt32NarrowToInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s32]index[_u32](svbool_t pg, uint16_t *base, svint32_t indices, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zindices.S, SXTW #1]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToInt16(Vector<uint> mask, ushort *base, Vector<int> indices, Vector<uint> data) => ScatterInt32NarrowToInt16(mask, *base, indices, data);


        ///  ScatterInt32NarrowToSByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToSByte(Vector<int> mask, Vector<uint> bases, Vector<int> data) => ScatterInt32NarrowToSByte(mask, bases, data);

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_s32](svbool_t pg, int8_t *base, svint32_t offsets, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToSByte(Vector<int> mask, sbyte *base, Vector<int> offsets, Vector<int> data) => ScatterInt32NarrowToSByte(mask, *base, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[s32]offset[_u32](svbool_t pg, uint8_t *base, svint32_t offsets, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]
        /// </summary>
        public static unsafe void ScatterInt32NarrowToSByte(Vector<uint> mask, byte *base, Vector<int> offsets, Vector<uint> data) => ScatterInt32NarrowToSByte(mask, *base, offsets, data);


        ///  ScatterInt64NarrowToInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt16(Vector<long> mask, Vector<ulong> bases, Vector<long> data) => ScatterInt64NarrowToInt16(mask, bases, data);

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt16(Vector<long> mask, short *base, Vector<long> offsets, Vector<long> data) => ScatterInt64NarrowToInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s64]index[_s64](svbool_t pg, int16_t *base, svint64_t indices, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt16(Vector<long> mask, short *base, Vector<long> indices, Vector<long> data) => ScatterInt64NarrowToInt16(mask, *base, indices, data);

        /// <summary>
        /// void svst1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt16(Vector<ulong> mask, ushort *base, Vector<long> offsets, Vector<ulong> data) => ScatterInt64NarrowToInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[s64]index[_u64](svbool_t pg, uint16_t *base, svint64_t indices, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt16(Vector<ulong> mask, ushort *base, Vector<long> indices, Vector<ulong> data) => ScatterInt64NarrowToInt16(mask, *base, indices, data);


        ///  ScatterInt64NarrowToInt32 : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt32(Vector<long> mask, Vector<ulong> bases, Vector<long> data) => ScatterInt64NarrowToInt32(mask, bases, data);

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt32(Vector<long> mask, int *base, Vector<long> offsets, Vector<long> data) => ScatterInt64NarrowToInt32(mask, *base, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[s64]index[_s64](svbool_t pg, int32_t *base, svint64_t indices, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt32(Vector<long> mask, int *base, Vector<long> indices, Vector<long> data) => ScatterInt64NarrowToInt32(mask, *base, indices, data);

        /// <summary>
        /// void svst1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt32(Vector<ulong> mask, uint *base, Vector<long> offsets, Vector<ulong> data) => ScatterInt64NarrowToInt32(mask, *base, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[s64]index[_u64](svbool_t pg, uint32_t *base, svint64_t indices, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToInt32(Vector<ulong> mask, uint *base, Vector<long> indices, Vector<ulong> data) => ScatterInt64NarrowToInt32(mask, *base, indices, data);


        ///  ScatterInt64NarrowToSByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToSByte(Vector<long> mask, Vector<ulong> bases, Vector<long> data) => ScatterInt64NarrowToSByte(mask, bases, data);

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToSByte(Vector<long> mask, sbyte *base, Vector<long> offsets, Vector<long> data) => ScatterInt64NarrowToSByte(mask, *base, offsets, data);

        /// <summary>
        /// void svst1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterInt64NarrowToSByte(Vector<ulong> mask, byte *base, Vector<long> offsets, Vector<ulong> data) => ScatterInt64NarrowToSByte(mask, *base, offsets, data);


        ///  ScatterTruncate16UInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter[_u32base]_offset[_s32](svbool_t pg, svuint32_t bases, int64_t offset, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1H Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<int> mask, Vector<uint> bases, long offset, Vector<int> data) => ScatterTruncate16UInt16(mask, bases, offset, data);

        /// <summary>
        /// void svst1h_scatter[_u32base]_index[_s32](svbool_t pg, svuint32_t bases, int64_t index, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #index * 2]
        ///   ST1H Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<int> mask, Vector<uint> bases, long index, Vector<int> data) => ScatterTruncate16UInt16(mask, bases, index, data);

        /// <summary>
        /// void svst1h_scatter[_u64base]_offset[_s64](svbool_t pg, svuint64_t bases, int64_t offset, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1H Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<long> mask, Vector<ulong> bases, long offset, Vector<long> data) => ScatterTruncate16UInt16(mask, bases, offset, data);

        /// <summary>
        /// void svst1h_scatter[_u64base]_index[_s64](svbool_t pg, svuint64_t bases, int64_t index, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #index * 2]
        ///   ST1H Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<long> mask, Vector<ulong> bases, long index, Vector<long> data) => ScatterTruncate16UInt16(mask, bases, index, data);

        /// <summary>
        /// void svst1h_scatter[_u32base]_offset[_u32](svbool_t pg, svuint32_t bases, int64_t offset, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1H Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<uint> mask, Vector<uint> bases, long offset, Vector<uint> data) => ScatterTruncate16UInt16(mask, bases, offset, data);

        /// <summary>
        /// void svst1h_scatter[_u32base]_index[_u32](svbool_t pg, svuint32_t bases, int64_t index, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #index * 2]
        ///   ST1H Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<uint> mask, Vector<uint> bases, long index, Vector<uint> data) => ScatterTruncate16UInt16(mask, bases, index, data);

        /// <summary>
        /// void svst1h_scatter[_u64base]_offset[_u64](svbool_t pg, svuint64_t bases, int64_t offset, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1H Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<ulong> mask, Vector<ulong> bases, long offset, Vector<ulong> data) => ScatterTruncate16UInt16(mask, bases, offset, data);

        /// <summary>
        /// void svst1h_scatter[_u64base]_index[_u64](svbool_t pg, svuint64_t bases, int64_t index, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #index * 2]
        ///   ST1H Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate16UInt16(Vector<ulong> mask, Vector<ulong> bases, long index, Vector<ulong> data) => ScatterTruncate16UInt16(mask, bases, index, data);


        ///  ScatterTruncate32UInt32 : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter[_u64base]_offset[_s64](svbool_t pg, svuint64_t bases, int64_t offset, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1W Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate32UInt32(Vector<long> mask, Vector<ulong> bases, long offset, Vector<long> data) => ScatterTruncate32UInt32(mask, bases, offset, data);

        /// <summary>
        /// void svst1w_scatter[_u64base]_index[_s64](svbool_t pg, svuint64_t bases, int64_t index, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #index * 4]
        ///   ST1W Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate32UInt32(Vector<long> mask, Vector<ulong> bases, long index, Vector<long> data) => ScatterTruncate32UInt32(mask, bases, index, data);

        /// <summary>
        /// void svst1w_scatter[_u64base]_offset[_u64](svbool_t pg, svuint64_t bases, int64_t offset, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1W Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate32UInt32(Vector<ulong> mask, Vector<ulong> bases, long offset, Vector<ulong> data) => ScatterTruncate32UInt32(mask, bases, offset, data);

        /// <summary>
        /// void svst1w_scatter[_u64base]_index[_u64](svbool_t pg, svuint64_t bases, int64_t index, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #index * 4]
        ///   ST1W Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate32UInt32(Vector<ulong> mask, Vector<ulong> bases, long index, Vector<ulong> data) => ScatterTruncate32UInt32(mask, bases, index, data);


        ///  ScatterTruncate8Byte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter[_u32base]_offset[_s32](svbool_t pg, svuint32_t bases, int64_t offset, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1B Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate8Byte(Vector<int> mask, Vector<uint> bases, long offset, Vector<int> data) => ScatterTruncate8Byte(mask, bases, offset, data);

        /// <summary>
        /// void svst1b_scatter[_u64base]_offset[_s64](svbool_t pg, svuint64_t bases, int64_t offset, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1B Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate8Byte(Vector<long> mask, Vector<ulong> bases, long offset, Vector<long> data) => ScatterTruncate8Byte(mask, bases, offset, data);

        /// <summary>
        /// void svst1b_scatter[_u32base]_offset[_u32](svbool_t pg, svuint32_t bases, int64_t offset, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Zbases.S, #offset]
        ///   ST1B Zdata.S, Pg, [Xoffset, Zbases.S, UXTW]
        /// </summary>
        public static unsafe void ScatterTruncate8Byte(Vector<uint> mask, Vector<uint> bases, long offset, Vector<uint> data) => ScatterTruncate8Byte(mask, bases, offset, data);

        /// <summary>
        /// void svst1b_scatter[_u64base]_offset[_u64](svbool_t pg, svuint64_t bases, int64_t offset, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Zbases.D, #offset]
        ///   ST1B Zdata.D, Pg, [Xoffset, Zbases.D]
        /// </summary>
        public static unsafe void ScatterTruncate8Byte(Vector<ulong> mask, Vector<ulong> bases, long offset, Vector<ulong> data) => ScatterTruncate8Byte(mask, bases, offset, data);


        ///  ScatterUInt32NarrowToByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToByte(Vector<int> mask, sbyte *base, Vector<uint> offsets, Vector<int> data) => ScatterUInt32NarrowToByte(mask, *base, offsets, data);

        /// <summary>
        /// void svst1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToByte(Vector<uint> mask, Vector<uint> bases, Vector<uint> data) => ScatterUInt32NarrowToByte(mask, bases, data);

        /// <summary>
        /// void svst1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToByte(Vector<uint> mask, byte *base, Vector<uint> offsets, Vector<uint> data) => ScatterUInt32NarrowToByte(mask, *base, offsets, data);


        ///  ScatterUInt32NarrowToUInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToUInt16(Vector<int> mask, short *base, Vector<uint> offsets, Vector<int> data) => ScatterUInt32NarrowToUInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u32]index[_s32](svbool_t pg, int16_t *base, svuint32_t indices, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToUInt16(Vector<int> mask, short *base, Vector<uint> indices, Vector<int> data) => ScatterUInt32NarrowToUInt16(mask, *base, indices, data);

        /// <summary>
        /// void svst1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Zbases.S, #0]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToUInt16(Vector<uint> mask, Vector<uint> bases, Vector<uint> data) => ScatterUInt32NarrowToUInt16(mask, bases, data);

        /// <summary>
        /// void svst1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToUInt16(Vector<uint> mask, ushort *base, Vector<uint> offsets, Vector<uint> data) => ScatterUInt32NarrowToUInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u32]index[_u32](svbool_t pg, uint16_t *base, svuint32_t indices, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xbase, Zindices.S, UXTW #1]
        /// </summary>
        public static unsafe void ScatterUInt32NarrowToUInt16(Vector<uint> mask, ushort *base, Vector<uint> indices, Vector<uint> data) => ScatterUInt32NarrowToUInt16(mask, *base, indices, data);


        ///  ScatterUInt64NarrowToByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToByte(Vector<long> mask, sbyte *base, Vector<ulong> offsets, Vector<long> data) => ScatterUInt64NarrowToByte(mask, *base, offsets, data);

        /// <summary>
        /// void svst1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToByte(Vector<ulong> mask, Vector<ulong> bases, Vector<ulong> data) => ScatterUInt64NarrowToByte(mask, bases, data);

        /// <summary>
        /// void svst1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToByte(Vector<ulong> mask, byte *base, Vector<ulong> offsets, Vector<ulong> data) => ScatterUInt64NarrowToByte(mask, *base, offsets, data);


        ///  ScatterUInt64NarrowToUInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt16(Vector<long> mask, short *base, Vector<ulong> offsets, Vector<long> data) => ScatterUInt64NarrowToUInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u64]index[_s64](svbool_t pg, int16_t *base, svuint64_t indices, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt16(Vector<long> mask, short *base, Vector<ulong> indices, Vector<long> data) => ScatterUInt64NarrowToUInt16(mask, *base, indices, data);

        /// <summary>
        /// void svst1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt16(Vector<ulong> mask, Vector<ulong> bases, Vector<ulong> data) => ScatterUInt64NarrowToUInt16(mask, bases, data);

        /// <summary>
        /// void svst1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt16(Vector<ulong> mask, ushort *base, Vector<ulong> offsets, Vector<ulong> data) => ScatterUInt64NarrowToUInt16(mask, *base, offsets, data);

        /// <summary>
        /// void svst1h_scatter_[u64]index[_u64](svbool_t pg, uint16_t *base, svuint64_t indices, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xbase, Zindices.D, LSL #1]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt16(Vector<ulong> mask, ushort *base, Vector<ulong> indices, Vector<ulong> data) => ScatterUInt64NarrowToUInt16(mask, *base, indices, data);


        ///  ScatterUInt64NarrowToUInt32 : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt32(Vector<long> mask, int *base, Vector<ulong> offsets, Vector<long> data) => ScatterUInt64NarrowToUInt32(mask, *base, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[u64]index[_s64](svbool_t pg, int32_t *base, svuint64_t indices, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt32(Vector<long> mask, int *base, Vector<ulong> indices, Vector<long> data) => ScatterUInt64NarrowToUInt32(mask, *base, indices, data);

        /// <summary>
        /// void svst1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Zbases.D, #0]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt32(Vector<ulong> mask, Vector<ulong> bases, Vector<ulong> data) => ScatterUInt64NarrowToUInt32(mask, bases, data);

        /// <summary>
        /// void svst1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt32(Vector<ulong> mask, uint *base, Vector<ulong> offsets, Vector<ulong> data) => ScatterUInt64NarrowToUInt32(mask, *base, offsets, data);

        /// <summary>
        /// void svst1w_scatter_[u64]index[_u64](svbool_t pg, uint32_t *base, svuint64_t indices, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xbase, Zindices.D, LSL #2]
        /// </summary>
        public static unsafe void ScatterUInt64NarrowToUInt32(Vector<ulong> mask, uint *base, Vector<ulong> indices, Vector<ulong> data) => ScatterUInt64NarrowToUInt32(mask, *base, indices, data);


        ///  SetFFR : Initialize the first-fault register to all-true

        /// <summary>
        /// void svsetffr()
        ///   SETFFR 
        /// </summary>
        public static unsafe void SetFFR() => SetFFR();


        ///  ShiftLeftLogical : Logical shift left

        /// <summary>
        /// svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   LSLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSLR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSL Zresult.B, Zop1.B, Zop2.D
        /// svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   LSLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSLR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSL Zresult.H, Zop1.H, Zop2.D
        /// svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   LSLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSLR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSL Zresult.S, Zop1.S, Zop2.D
        /// svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   LSLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSLR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   LSLR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSLR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSL Zresult.B, Zop1.B, Zop2.D
        /// svuint8_t svlsl_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSL Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   LSLR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSLR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSL Zresult.H, Zop1.H, Zop2.D
        /// svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSL Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   LSLR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSLR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSL Zresult.S, Zop1.S, Zop2.D
        /// svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSL Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        /// svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   LSLR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSL Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSLR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right) => ShiftLeftLogical(left, right);


        ///  ShiftRightArithmetic : Arithmetic shift right

        /// <summary>
        /// svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   ASRR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; ASRR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   ASR Zresult.B, Zop1.B, Zop2.D
        /// svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASR Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   ASRR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; ASRR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   ASR Zresult.H, Zop1.H, Zop2.D
        /// svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASR Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   ASRR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; ASRR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   ASR Zresult.S, Zop1.S, Zop2.D
        /// svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASR Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        /// svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   ASRR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ASR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; ASRR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);


        ///  ShiftRightArithmeticDivide : Arithmetic shift right for divide by immediate

        /// <summary>
        /// svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.B, Pg/M, Zresult.B, #imm2
        /// svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.B, Pg/M, Zresult.B, #imm2
        /// svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; ASRD Zresult.B, Pg/M, Zresult.B, #imm2
        /// </summary>
        public static unsafe Vector<sbyte> ShiftRightArithmeticDivide(Vector<sbyte> op1, ulong imm2) => ShiftRightArithmeticDivide(op1, imm2);

        /// <summary>
        /// svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.H, Pg/M, Zresult.H, #imm2
        /// svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.H, Pg/M, Zresult.H, #imm2
        /// svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; ASRD Zresult.H, Pg/M, Zresult.H, #imm2
        /// </summary>
        public static unsafe Vector<short> ShiftRightArithmeticDivide(Vector<short> op1, ulong imm2) => ShiftRightArithmeticDivide(op1, imm2);

        /// <summary>
        /// svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.S, Pg/M, Zresult.S, #imm2
        /// svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.S, Pg/M, Zresult.S, #imm2
        /// svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; ASRD Zresult.S, Pg/M, Zresult.S, #imm2
        /// </summary>
        public static unsafe Vector<int> ShiftRightArithmeticDivide(Vector<int> op1, ulong imm2) => ShiftRightArithmeticDivide(op1, imm2);

        /// <summary>
        /// svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.D, Pg/M, Zresult.D, #imm2
        /// svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2
        ///   MOVPRFX Zresult, Zop1; ASRD Zresult.D, Pg/M, Zresult.D, #imm2
        /// svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; ASRD Zresult.D, Pg/M, Zresult.D, #imm2
        /// </summary>
        public static unsafe Vector<long> ShiftRightArithmeticDivide(Vector<long> op1, ulong imm2) => ShiftRightArithmeticDivide(op1, imm2);


        ///  ShiftRightLogical : Logical shift right

        /// <summary>
        /// svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   LSRR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; LSRR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D
        ///   LSR Zresult.B, Zop1.B, Zop2.D
        /// svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; LSR Zresult.B, Pg/M, Zresult.B, Zop2.D
        /// </summary>
        public static unsafe Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   LSRR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; LSRR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D
        ///   LSR Zresult.H, Zop1.H, Zop2.D
        /// svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; LSR Zresult.H, Pg/M, Zresult.H, Zop2.D
        /// </summary>
        public static unsafe Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   LSRR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; LSRR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D
        ///   LSR Zresult.S, Zop1.S, Zop2.D
        /// svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; LSR Zresult.S, Pg/M, Zresult.S, Zop2.D
        /// </summary>
        public static unsafe Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        /// svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   LSRR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; LSR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; LSRR Zresult.D, Pg/M, Zresult.D, Zop1.D
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


        ///  Splice : Splice two vectors under predicate control

        /// <summary>
        /// svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.B, Pg, Zresult.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => Splice(mask, left, right);

        /// <summary>
        /// svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H
        /// </summary>
        public static unsafe Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right) => Splice(mask, left, right);

        /// <summary>
        /// svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right) => Splice(mask, left, right);

        /// <summary>
        /// svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.B, Pg, Zresult.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.H, Pg, Zresult.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => Splice(mask, left, right);

        /// <summary>
        /// svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.S, Pg, Zresult.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right) => Splice(mask, left, right);

        /// <summary>
        /// svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SPLICE Zresult.D, Pg, Zresult.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right) => Splice(mask, left, right);


        ///  Sqrt : Square root

        /// <summary>
        /// svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)
        ///   FSQRT Ztied.S, Pg/M, Zop.S
        ///   MOVPRFX Zresult, Zinactive; FSQRT Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op)
        ///   FSQRT Ztied.S, Pg/M, Ztied.S
        ///   MOVPRFX Zresult, Zop; FSQRT Zresult.S, Pg/M, Zop.S
        /// svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop.S; FSQRT Zresult.S, Pg/M, Zop.S
        /// </summary>
        public static unsafe Vector<float> Sqrt(Vector<float> value) => Sqrt(value);

        /// <summary>
        /// svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)
        ///   FSQRT Ztied.D, Pg/M, Zop.D
        ///   MOVPRFX Zresult, Zinactive; FSQRT Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op)
        ///   FSQRT Ztied.D, Pg/M, Ztied.D
        ///   MOVPRFX Zresult, Zop; FSQRT Zresult.D, Pg/M, Zop.D
        /// svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop.D; FSQRT Zresult.D, Pg/M, Zop.D
        /// </summary>
        public static unsafe Vector<double> Sqrt(Vector<double> value) => Sqrt(value);


        ///  Store : Non-truncating store

        /// <summary>
        /// void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        ///   ST1B Zdata.B, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<sbyte> mask, sbyte *base, Vector<sbyte> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        ///   ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<short> mask, short *base, Vector<short> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        ///   ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<int> mask, int *base, Vector<int> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        ///   ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<long> mask, long *base, Vector<long> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        ///   ST1B Zdata.B, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<byte> mask, byte *base, Vector<byte> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        ///   ST1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<ushort> mask, ushort *base, Vector<ushort> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        ///   ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<uint> mask, uint *base, Vector<uint> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        ///   ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<ulong> mask, ulong *base, Vector<ulong> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        ///   ST1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<float> mask, float *base, Vector<float> data) => Store(mask, *base, data);

        /// <summary>
        /// void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        ///   ST1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Store(Vector<double> mask, double *base, Vector<double> data) => Store(mask, *base, data);


        ///  StoreInt16NarrowToSByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data)
        ///   ST1B Zdata.H, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt16NarrowToSByte(Vector<short> mask, sbyte *base, Vector<short> data) => StoreInt16NarrowToSByte(mask, *base, data);


        ///  StoreInt32NarrowToInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data)
        ///   ST1H Zdata.S, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt32NarrowToInt16(Vector<int> mask, short *base, Vector<int> data) => StoreInt32NarrowToInt16(mask, *base, data);


        ///  StoreInt32NarrowToSByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data)
        ///   ST1B Zdata.S, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt32NarrowToSByte(Vector<int> mask, sbyte *base, Vector<int> data) => StoreInt32NarrowToSByte(mask, *base, data);


        ///  StoreInt64NarrowToInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data)
        ///   ST1H Zdata.D, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt64NarrowToInt16(Vector<long> mask, short *base, Vector<long> data) => StoreInt64NarrowToInt16(mask, *base, data);


        ///  StoreInt64NarrowToInt32 : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data)
        ///   ST1W Zdata.D, Pg, [Xarray, Xindex, LSL #2]
        ///   ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt64NarrowToInt32(Vector<long> mask, int *base, Vector<long> data) => StoreInt64NarrowToInt32(mask, *base, data);


        ///  StoreInt64NarrowToSByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data)
        ///   ST1B Zdata.D, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreInt64NarrowToSByte(Vector<long> mask, sbyte *base, Vector<long> data) => StoreInt64NarrowToSByte(mask, *base, data);


        ///  StoreNonTemporal : Non-truncating store, non-temporal

        /// <summary>
        /// void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data)
        ///   STNT1B Zdata.B, Pg, [Xarray, Xindex]
        ///   STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte *base, Vector<sbyte> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data)
        ///   STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<short> mask, short *base, Vector<short> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data)
        ///   STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<int> mask, int *base, Vector<int> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data)
        ///   STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<long> mask, long *base, Vector<long> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)
        ///   STNT1B Zdata.B, Pg, [Xarray, Xindex]
        ///   STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<byte> mask, byte *base, Vector<byte> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)
        ///   STNT1H Zdata.H, Pg, [Xarray, Xindex, LSL #1]
        ///   STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort *base, Vector<ushort> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)
        ///   STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<uint> mask, uint *base, Vector<uint> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)
        ///   STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong *base, Vector<ulong> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)
        ///   STNT1W Zdata.S, Pg, [Xarray, Xindex, LSL #2]
        ///   STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<float> mask, float *base, Vector<float> data) => StoreNonTemporal(mask, *base, data);

        /// <summary>
        /// void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)
        ///   STNT1D Zdata.D, Pg, [Xarray, Xindex, LSL #3]
        ///   STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<double> mask, double *base, Vector<double> data) => StoreNonTemporal(mask, *base, data);


        ///  StoreUInt16NarrowToByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data)
        ///   ST1B Zdata.H, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt16NarrowToByte(Vector<ushort> mask, byte *base, Vector<ushort> data) => StoreUInt16NarrowToByte(mask, *base, data);


        ///  StoreUInt32NarrowToByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data)
        ///   ST1B Zdata.S, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt32NarrowToByte(Vector<uint> mask, byte *base, Vector<uint> data) => StoreUInt32NarrowToByte(mask, *base, data);


        ///  StoreUInt32NarrowToUInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data)
        ///   ST1H Zdata.S, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt32NarrowToUInt16(Vector<uint> mask, ushort *base, Vector<uint> data) => StoreUInt32NarrowToUInt16(mask, *base, data);


        ///  StoreUInt64NarrowToByte : Truncate to 8 bits and store

        /// <summary>
        /// void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data)
        ///   ST1B Zdata.D, Pg, [Xarray, Xindex]
        ///   ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt64NarrowToByte(Vector<ulong> mask, byte *base, Vector<ulong> data) => StoreUInt64NarrowToByte(mask, *base, data);


        ///  StoreUInt64NarrowToUInt16 : Truncate to 16 bits and store

        /// <summary>
        /// void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data)
        ///   ST1H Zdata.D, Pg, [Xarray, Xindex, LSL #1]
        ///   ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt64NarrowToUInt16(Vector<ulong> mask, ushort *base, Vector<ulong> data) => StoreUInt64NarrowToUInt16(mask, *base, data);


        ///  StoreUInt64NarrowToUInt32 : Truncate to 32 bits and store

        /// <summary>
        /// void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data)
        ///   ST1W Zdata.D, Pg, [Xarray, Xindex, LSL #2]
        ///   ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void StoreUInt64NarrowToUInt32(Vector<ulong> mask, uint *base, Vector<ulong> data) => StoreUInt64NarrowToUInt32(mask, *base, data);


        ///  Storex2 : Store two vectors into two-element tuples

        /// <summary>
        /// void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data)
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xarray, Xindex]
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<sbyte> mask, sbyte *base, (Vector<sbyte> data1, Vector<sbyte> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<short> mask, short *base, (Vector<short> data1, Vector<short> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<int> mask, int *base, (Vector<int> data1, Vector<int> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<long> mask, long *base, (Vector<long> data1, Vector<long> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data)
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xarray, Xindex]
        ///   ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<byte> mask, byte *base, (Vector<byte> data1, Vector<byte> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data)
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<ushort> mask, ushort *base, (Vector<ushort> data1, Vector<ushort> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<uint> mask, uint *base, (Vector<uint> data1, Vector<uint> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<ulong> mask, ulong *base, (Vector<ulong> data1, Vector<ulong> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data)
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<float> mask, float *base, (Vector<float> data1, Vector<float> data2)) => Storex2(mask, *base, data1,);

        /// <summary>
        /// void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data)
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex2(Vector<double> mask, double *base, (Vector<double> data1, Vector<double> data2)) => Storex2(mask, *base, data1,);


        ///  Storex3 : Store three vectors into three-element tuples

        /// <summary>
        /// void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data)
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xarray, Xindex]
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<sbyte> mask, sbyte *base, (Vector<sbyte> data1, Vector<sbyte> data2, Vector<sbyte> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<short> mask, short *base, (Vector<short> data1, Vector<short> data2, Vector<short> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<int> mask, int *base, (Vector<int> data1, Vector<int> data2, Vector<int> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<long> mask, long *base, (Vector<long> data1, Vector<long> data2, Vector<long> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data)
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xarray, Xindex]
        ///   ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<byte> mask, byte *base, (Vector<byte> data1, Vector<byte> data2, Vector<byte> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data)
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<ushort> mask, ushort *base, (Vector<ushort> data1, Vector<ushort> data2, Vector<ushort> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<uint> mask, uint *base, (Vector<uint> data1, Vector<uint> data2, Vector<uint> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<ulong> mask, ulong *base, (Vector<ulong> data1, Vector<ulong> data2, Vector<ulong> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data)
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<float> mask, float *base, (Vector<float> data1, Vector<float> data2, Vector<float> data3)) => Storex3(mask, *base, data1,);

        /// <summary>
        /// void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data)
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex3(Vector<double> mask, double *base, (Vector<double> data1, Vector<double> data2, Vector<double> data3)) => Storex3(mask, *base, data1,);


        ///  Storex4 : Store four vectors into four-element tuples

        /// <summary>
        /// void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data)
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xarray, Xindex]
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<sbyte> mask, sbyte *base, (Vector<sbyte> data1, Vector<sbyte> data2, Vector<sbyte> data3, Vector<sbyte> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<short> mask, short *base, (Vector<short> data1, Vector<short> data2, Vector<short> data3, Vector<short> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<int> mask, int *base, (Vector<int> data1, Vector<int> data2, Vector<int> data3, Vector<int> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<long> mask, long *base, (Vector<long> data1, Vector<long> data2, Vector<long> data3, Vector<long> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data)
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xarray, Xindex]
        ///   ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<byte> mask, byte *base, (Vector<byte> data1, Vector<byte> data2, Vector<byte> data3, Vector<byte> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data)
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xarray, Xindex, LSL #1]
        ///   ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<ushort> mask, ushort *base, (Vector<ushort> data1, Vector<ushort> data2, Vector<ushort> data3, Vector<ushort> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<uint> mask, uint *base, (Vector<uint> data1, Vector<uint> data2, Vector<uint> data3, Vector<uint> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<ulong> mask, ulong *base, (Vector<ulong> data1, Vector<ulong> data2, Vector<ulong> data3, Vector<ulong> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data)
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xarray, Xindex, LSL #2]
        ///   ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<float> mask, float *base, (Vector<float> data1, Vector<float> data2, Vector<float> data3, Vector<float> data4)) => Storex4(mask, *base, data1,);

        /// <summary>
        /// void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data)
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xarray, Xindex, LSL #3]
        ///   ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe void Storex4(Vector<double> mask, double *base, (Vector<double> data1, Vector<double> data2, Vector<double> data3, Vector<double> data4)) => Storex4(mask, *base, data1,);


        ///  Subtract : Subtract

        /// <summary>
        /// svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SUB Zresult.B, Zop1.B, Zop2.B
        /// svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SUB Zresult.H, Zop1.H, Zop2.H
        /// svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> Subtract(Vector<short> left, Vector<short> right) => Subtract(left, right);

        /// <summary>
        /// svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SUB Zresult.S, Zop1.S, Zop2.S
        /// svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> Subtract(Vector<int> left, Vector<int> right) => Subtract(left, right);

        /// <summary>
        /// svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SUB Zresult.D, Zop1.D, Zop2.D
        /// svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> Subtract(Vector<long> left, Vector<long> right) => Subtract(left, right);

        /// <summary>
        /// svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUBR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SUB Zresult.B, Zop1.B, Zop2.B
        /// svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUB Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> Subtract(Vector<byte> left, Vector<byte> right) => Subtract(left, right);

        /// <summary>
        /// svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUBR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SUB Zresult.H, Zop1.H, Zop2.H
        /// svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUB Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right) => Subtract(left, right);

        /// <summary>
        /// svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SUB Zresult.S, Zop1.S, Zop2.S
        /// svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> Subtract(Vector<uint> left, Vector<uint> right) => Subtract(left, right);

        /// <summary>
        /// svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SUB Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right) => Subtract(left, right);

        /// <summary>
        /// svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FSUBR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   FSUB Zresult.S, Zop1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSUB Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FSUBR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> Subtract(Vector<float> left, Vector<float> right) => Subtract(left, right);

        /// <summary>
        /// svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FSUBR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   FSUB Zresult.D, Zop1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSUB Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FSUBR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> Subtract(Vector<double> left, Vector<double> right) => Subtract(left, right);


        ///  SubtractReversed : Subtract reversed

        /// <summary>
        /// svint8_t svsubr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t svsubr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   SUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SUB Zresult.B, Zop2.B, Zop1.B
        /// svint8_t svsubr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUB Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<sbyte> SubtractReversed(Vector<sbyte> left, Vector<sbyte> right) => SubtractReversed(left, right);

        /// <summary>
        /// svint16_t svsubr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t svsubr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   SUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SUB Zresult.H, Zop2.H, Zop1.H
        /// svint16_t svsubr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUB Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<short> SubtractReversed(Vector<short> left, Vector<short> right) => SubtractReversed(left, right);

        /// <summary>
        /// svint32_t svsubr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t svsubr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   SUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SUB Zresult.S, Zop2.S, Zop1.S
        /// svint32_t svsubr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUB Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<int> SubtractReversed(Vector<int> left, Vector<int> right) => SubtractReversed(left, right);

        /// <summary>
        /// svint64_t svsubr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t svsubr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   SUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SUB Zresult.D, Zop2.D, Zop1.D
        /// svint64_t svsubr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUB Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<long> SubtractReversed(Vector<long> left, Vector<long> right) => SubtractReversed(left, right);

        /// <summary>
        /// svuint8_t svsubr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t svsubr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   SUBR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   SUB Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   SUB Zresult.B, Zop2.B, Zop1.B
        /// svuint8_t svsubr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; SUBR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; SUB Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// </summary>
        public static unsafe Vector<byte> SubtractReversed(Vector<byte> left, Vector<byte> right) => SubtractReversed(left, right);

        /// <summary>
        /// svuint16_t svsubr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t svsubr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   SUBR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   SUB Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   SUB Zresult.H, Zop2.H, Zop1.H
        /// svuint16_t svsubr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; SUBR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; SUB Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// </summary>
        public static unsafe Vector<ushort> SubtractReversed(Vector<ushort> left, Vector<ushort> right) => SubtractReversed(left, right);

        /// <summary>
        /// svuint32_t svsubr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t svsubr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   SUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   SUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   SUB Zresult.S, Zop2.S, Zop1.S
        /// svuint32_t svsubr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; SUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; SUB Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<uint> SubtractReversed(Vector<uint> left, Vector<uint> right) => SubtractReversed(left, right);

        /// <summary>
        /// svuint64_t svsubr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; SUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t svsubr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   SUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   SUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   SUB Zresult.D, Zop2.D, Zop1.D
        /// svuint64_t svsubr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; SUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; SUB Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<ulong> SubtractReversed(Vector<ulong> left, Vector<ulong> right) => SubtractReversed(left, right);

        /// <summary>
        /// svfloat32_t svsubr[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsubr[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   FSUBR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   FSUB Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   FSUB Zresult.S, Zop2.S, Zop1.S
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svfloat32_t svsubr[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; FSUBR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; FSUB Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// </summary>
        public static unsafe Vector<float> SubtractReversed(Vector<float> left, Vector<float> right) => SubtractReversed(left, right);

        /// <summary>
        /// svfloat64_t svsubr[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsubr[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   FSUBR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   FSUB Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   FSUB Zresult.D, Zop2.D, Zop1.D
        ///   MOVPRFX Zresult, Zop1; FSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svfloat64_t svsubr[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; FSUBR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; FSUB Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// </summary>
        public static unsafe Vector<double> SubtractReversed(Vector<double> left, Vector<double> right) => SubtractReversed(left, right);


        ///  SubtractSaturate : Saturating subtract

        /// <summary>
        /// svint8_t svqsub[_s8](svint8_t op1, svint8_t op2)
        ///   SQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

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
        /// svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2)
        ///   UQSUB Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

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


        ///  TransposeEven : Interleave even elements from two inputs

        /// <summary>
        /// svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2)
        ///   TRN1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right) => TransposeEven(left, right);

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
        /// svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN1 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svtrn1_b8(svbool_t op1, svbool_t op2)
        ///   TRN1 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN1 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svtrn1_b16(svbool_t op1, svbool_t op2)
        ///   TRN1 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN1 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svtrn1_b32(svbool_t op1, svbool_t op2)
        ///   TRN1 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right) => TransposeEven(left, right);

        /// <summary>
        /// svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN1 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svtrn1_b64(svbool_t op1, svbool_t op2)
        ///   TRN1 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TransposeEven(Vector<float> left, Vector<float> right) => TransposeEven(left, right);

        /// <summary>
        /// svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TransposeEven(Vector<double> left, Vector<double> right) => TransposeEven(left, right);


        ///  TransposeOdd : Interleave odd elements from two inputs

        /// <summary>
        /// svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2)
        ///   TRN2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right) => TransposeOdd(left, right);

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
        /// svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN2 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svtrn2_b8(svbool_t op1, svbool_t op2)
        ///   TRN2 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN2 Zresult.H, Zop1.H, Zop2.H
        /// svbool_t svtrn2_b16(svbool_t op1, svbool_t op2)
        ///   TRN2 Presult.H, Pop1.H, Pop2.H
        /// </summary>
        public static unsafe Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN2 Zresult.S, Zop1.S, Zop2.S
        /// svbool_t svtrn2_b32(svbool_t op1, svbool_t op2)
        ///   TRN2 Presult.S, Pop1.S, Pop2.S
        /// </summary>
        public static unsafe Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right) => TransposeOdd(left, right);

        /// <summary>
        /// svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN2 Zresult.D, Zop1.D, Zop2.D
        /// svbool_t svtrn2_b64(svbool_t op1, svbool_t op2)
        ///   TRN2 Presult.D, Pop1.D, Pop2.D
        /// </summary>
        public static unsafe Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TransposeOdd(Vector<float> left, Vector<float> right) => TransposeOdd(left, right);

        /// <summary>
        /// svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TransposeOdd(Vector<double> left, Vector<double> right) => TransposeOdd(left, right);


        ///  TrigonometricMultiplyAddCoefficient : Trigonometric multiply-add coefficient

        /// <summary>
        /// svfloat32_t svtmad[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)
        ///   FTMAD Ztied1.S, Ztied1.S, Zop2.S, #imm3
        ///   MOVPRFX Zresult, Zop1; FTMAD Zresult.S, Zresult.S, Zop2.S, #imm3
        /// </summary>
        public static unsafe Vector<float> TrigonometricMultiplyAddCoefficient(Vector<float> op1, Vector<float> op2, ulong imm3) => TrigonometricMultiplyAddCoefficient(op1, op2, imm3);

        /// <summary>
        /// svfloat64_t svtmad[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)
        ///   FTMAD Ztied1.D, Ztied1.D, Zop2.D, #imm3
        ///   MOVPRFX Zresult, Zop1; FTMAD Zresult.D, Zresult.D, Zop2.D, #imm3
        /// </summary>
        public static unsafe Vector<double> TrigonometricMultiplyAddCoefficient(Vector<double> op1, Vector<double> op2, ulong imm3) => TrigonometricMultiplyAddCoefficient(op1, op2, imm3);


        ///  TrigonometricSelectCoefficient : Trigonometric select coefficient

        /// <summary>
        /// svfloat32_t svtssel[_f32](svfloat32_t op1, svuint32_t op2)
        ///   FTSSEL Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TrigonometricSelectCoefficient(Vector<float> left, Vector<uint> right) => TrigonometricSelectCoefficient(left, right);

        /// <summary>
        /// svfloat64_t svtssel[_f64](svfloat64_t op1, svuint64_t op2)
        ///   FTSSEL Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TrigonometricSelectCoefficient(Vector<double> left, Vector<ulong> right) => TrigonometricSelectCoefficient(left, right);


        ///  TrigonometricStartingValue : Trigonometric starting value

        /// <summary>
        /// svfloat32_t svtsmul[_f32](svfloat32_t op1, svuint32_t op2)
        ///   FTSMUL Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> TrigonometricStartingValue(Vector<float> left, Vector<uint> right) => TrigonometricStartingValue(left, right);

        /// <summary>
        /// svfloat64_t svtsmul[_f64](svfloat64_t op1, svuint64_t op2)
        ///   FTSMUL Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> TrigonometricStartingValue(Vector<double> left, Vector<ulong> right) => TrigonometricStartingValue(left, right);


        ///  TrueMask : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_b8()
        ///   PTRUE Presult.B, ALL
        /// svbool_t svptrue_b16()
        ///   PTRUE Presult.H, ALL
        /// svbool_t svptrue_b32()
        ///   PTRUE Presult.S, ALL
        /// svbool_t svptrue_b64()
        ///   PTRUE Presult.D, ALL
        /// </summary>
        public static unsafe Vector<byte> TrueMask() => TrueMask();

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// svbool_t svptrue_pat_b16(enum svpattern pattern)
        ///   PTRUE Presult.H, pattern
        /// svbool_t svptrue_pat_b32(enum svpattern pattern)
        ///   PTRUE Presult.S, pattern
        /// svbool_t svptrue_pat_b64(enum svpattern pattern)
        ///   PTRUE Presult.D, pattern
        /// </summary>
        public static unsafe Vector<byte> TrueMask(enum SveMaskPattern pattern) => TrueMask(SveMaskPattern);


        ///  UnzipEven : Concatenate even elements from two inputs

        /// <summary>
        /// svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2)
        ///   UZP1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right) => UnzipEven(left, right);

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
        /// svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP1 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svuzp1_b8(svbool_t op1, svbool_t op2)
        ///   UZP1 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right) => UnzipEven(left, right);

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

        /// <summary>
        /// svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> UnzipEven(Vector<float> left, Vector<float> right) => UnzipEven(left, right);

        /// <summary>
        /// svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> UnzipEven(Vector<double> left, Vector<double> right) => UnzipEven(left, right);


        ///  UnzipOdd : Concatenate odd elements from two inputs

        /// <summary>
        /// svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2)
        ///   UZP2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right) => UnzipOdd(left, right);

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
        /// svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP2 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svuzp2_b8(svbool_t op1, svbool_t op2)
        ///   UZP2 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right) => UnzipOdd(left, right);

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

        /// <summary>
        /// svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> UnzipOdd(Vector<float> left, Vector<float> right) => UnzipOdd(left, right);

        /// <summary>
        /// svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> UnzipOdd(Vector<double> left, Vector<double> right) => UnzipOdd(left, right);


        ///  VectorTableLookup : Table lookup in single-vector table

        /// <summary>
        /// svint8_t svtbl[_s8](svint8_t data, svuint8_t indices)
        ///   TBL Zresult.B, Zdata.B, Zindices.B
        /// </summary>
        public static unsafe Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint16_t svtbl[_s16](svint16_t data, svuint16_t indices)
        ///   TBL Zresult.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint32_t svtbl[_s32](svint32_t data, svuint32_t indices)
        ///   TBL Zresult.S, Zdata.S, Zindices.S
        /// </summary>
        public static unsafe Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svint64_t svtbl[_s64](svint64_t data, svuint64_t indices)
        ///   TBL Zresult.D, Zdata.D, Zindices.D
        /// </summary>
        public static unsafe Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices)
        ///   TBL Zresult.B, Zdata.B, Zindices.B
        /// </summary>
        public static unsafe Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices)
        ///   TBL Zresult.H, Zdata.H, Zindices.H
        /// </summary>
        public static unsafe Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices)
        ///   TBL Zresult.S, Zdata.S, Zindices.S
        /// </summary>
        public static unsafe Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices)
        ///   TBL Zresult.D, Zdata.D, Zindices.D
        /// </summary>
        public static unsafe Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices)
        ///   TBL Zresult.S, Zdata.S, Zindices.S
        /// </summary>
        public static unsafe Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        /// svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices)
        ///   TBL Zresult.D, Zdata.D, Zindices.D
        /// </summary>
        public static unsafe Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices) => VectorTableLookup(data, indices);


        ///  WriteFFR : Write to the first-fault register

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<sbyte> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<short> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<int> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<long> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<byte> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<ushort> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<uint> value) => WriteFFR(value);

        /// <summary>
        /// void svwrffr(svbool_t op)
        ///   WRFFR Pop.B
        /// </summary>
        public static unsafe void WriteFFR(Vector<ulong> value) => WriteFFR(value);


        ///  Xor : Bitwise exclusive OR

        /// <summary>
        /// svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   EOR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; EOR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right) => Xor(left, right);

        /// <summary>
        /// svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   EOR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; EOR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<short> Xor(Vector<short> left, Vector<short> right) => Xor(left, right);

        /// <summary>
        /// svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   EOR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; EOR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<int> Xor(Vector<int> left, Vector<int> right) => Xor(left, right);

        /// <summary>
        /// svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   EOR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; EOR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<long> Xor(Vector<long> left, Vector<long> right) => Xor(left, right);

        /// <summary>
        /// svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B
        /// svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B
        ///   EOR Ztied2.B, Pg/M, Ztied2.B, Zop1.B
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)
        ///   MOVPRFX Zresult.B, Pg/Z, Zop1.B; EOR Zresult.B, Pg/M, Zresult.B, Zop2.B
        ///   MOVPRFX Zresult.B, Pg/Z, Zop2.B; EOR Zresult.B, Pg/M, Zresult.B, Zop1.B
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> Xor(Vector<byte> left, Vector<byte> right) => Xor(left, right);

        /// <summary>
        /// svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H
        /// svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H
        ///   EOR Ztied2.H, Pg/M, Ztied2.H, Zop1.H
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)
        ///   MOVPRFX Zresult.H, Pg/Z, Zop1.H; EOR Zresult.H, Pg/M, Zresult.H, Zop2.H
        ///   MOVPRFX Zresult.H, Pg/Z, Zop2.H; EOR Zresult.H, Pg/M, Zresult.H, Zop1.H
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right) => Xor(left, right);

        /// <summary>
        /// svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S
        /// svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S
        ///   EOR Ztied2.S, Pg/M, Ztied2.S, Zop1.S
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)
        ///   MOVPRFX Zresult.S, Pg/Z, Zop1.S; EOR Zresult.S, Pg/M, Zresult.S, Zop2.S
        ///   MOVPRFX Zresult.S, Pg/Z, Zop2.S; EOR Zresult.S, Pg/M, Zresult.S, Zop1.S
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<uint> Xor(Vector<uint> left, Vector<uint> right) => Xor(left, right);

        /// <summary>
        /// svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   MOVPRFX Zresult, Zop1; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D
        /// svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D
        ///   EOR Ztied2.D, Pg/M, Ztied2.D, Zop1.D
        ///   EOR Zresult.D, Zop1.D, Zop2.D
        /// svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)
        ///   MOVPRFX Zresult.D, Pg/Z, Zop1.D; EOR Zresult.D, Pg/M, Zresult.D, Zop2.D
        ///   MOVPRFX Zresult.D, Pg/Z, Zop2.D; EOR Zresult.D, Pg/M, Zresult.D, Zop1.D
        /// svbool_t sveor[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)
        ///   EOR Presult.B, Pg/Z, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right) => Xor(left, right);


        ///  XorAcross : Bitwise exclusive OR reduction to scalar

        /// <summary>
        /// int8_t sveorv[_s8](svbool_t pg, svint8_t op)
        ///   EORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe sbyte XorAcross(Vector<sbyte> value) => XorAcross(value);

        /// <summary>
        /// int16_t sveorv[_s16](svbool_t pg, svint16_t op)
        ///   EORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe short XorAcross(Vector<short> value) => XorAcross(value);

        /// <summary>
        /// int32_t sveorv[_s32](svbool_t pg, svint32_t op)
        ///   EORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe int XorAcross(Vector<int> value) => XorAcross(value);

        /// <summary>
        /// int64_t sveorv[_s64](svbool_t pg, svint64_t op)
        ///   EORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe long XorAcross(Vector<long> value) => XorAcross(value);

        /// <summary>
        /// uint8_t sveorv[_u8](svbool_t pg, svuint8_t op)
        ///   EORV Bresult, Pg, Zop.B
        /// </summary>
        public static unsafe byte XorAcross(Vector<byte> value) => XorAcross(value);

        /// <summary>
        /// uint16_t sveorv[_u16](svbool_t pg, svuint16_t op)
        ///   EORV Hresult, Pg, Zop.H
        /// </summary>
        public static unsafe ushort XorAcross(Vector<ushort> value) => XorAcross(value);

        /// <summary>
        /// uint32_t sveorv[_u32](svbool_t pg, svuint32_t op)
        ///   EORV Sresult, Pg, Zop.S
        /// </summary>
        public static unsafe uint XorAcross(Vector<uint> value) => XorAcross(value);

        /// <summary>
        /// uint64_t sveorv[_u64](svbool_t pg, svuint64_t op)
        ///   EORV Dresult, Pg, Zop.D
        /// </summary>
        public static unsafe ulong XorAcross(Vector<ulong> value) => XorAcross(value);


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
        /// svbool_t svunpklo[_b](svbool_t op)
        ///   PUNPKLO Presult.H, Pop.B
        /// </summary>
        public static unsafe Vector<ushort> ZeroExtendWideningLower(Vector<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint32_t svunpklo[_u32](svuint16_t op)
        ///   UUNPKLO Zresult.S, Zop.H
        /// svbool_t svunpklo[_b](svbool_t op)
        ///   PUNPKLO Presult.H, Pop.B
        /// </summary>
        public static unsafe Vector<uint> ZeroExtendWideningLower(Vector<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// svuint64_t svunpklo[_u64](svuint32_t op)
        ///   UUNPKLO Zresult.D, Zop.S
        /// svbool_t svunpklo[_b](svbool_t op)
        ///   PUNPKLO Presult.H, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningLower(Vector<uint> value) => ZeroExtendWideningLower(value);


        ///  ZeroExtendWideningUpper : Unpack and extend high half

        /// <summary>
        /// svuint16_t svunpkhi[_u16](svuint8_t op)
        ///   UUNPKHI Zresult.H, Zop.B
        /// svbool_t svunpkhi[_b](svbool_t op)
        ///   PUNPKHI Presult.H, Pop.B
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
        /// svbool_t svunpkhi[_b](svbool_t op)
        ///   PUNPKHI Presult.H, Pop.B
        /// </summary>
        public static unsafe Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value) => ZeroExtendWideningUpper(value);


        ///  ZipHigh : Interleave elements from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP2 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right) => ZipHigh(left, right);

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
        /// svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP2 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svzip2_b8(svbool_t op1, svbool_t op2)
        ///   ZIP2 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right) => ZipHigh(left, right);

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

        /// <summary>
        /// svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP2 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ZipHigh(Vector<float> left, Vector<float> right) => ZipHigh(left, right);

        /// <summary>
        /// svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP2 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ZipHigh(Vector<double> left, Vector<double> right) => ZipHigh(left, right);


        ///  ZipLow : Interleave elements from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP1 Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right) => ZipLow(left, right);

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
        /// svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP1 Zresult.B, Zop1.B, Zop2.B
        /// svbool_t svzip1_b8(svbool_t op1, svbool_t op2)
        ///   ZIP1 Presult.B, Pop1.B, Pop2.B
        /// </summary>
        public static unsafe Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right) => ZipLow(left, right);

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

        /// <summary>
        /// svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP1 Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<float> ZipLow(Vector<float> left, Vector<float> right) => ZipLow(left, right);

        /// <summary>
        /// svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<double> ZipLow(Vector<double> left, Vector<double> right) => ZipLow(left, right);

    }
}


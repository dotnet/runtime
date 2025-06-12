// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SVE hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    [Experimental(Experimentals.ArmSveDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class Sve : AdvSimd
    {
        internal Sve() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM SVE hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }


        // Absolute value

        /// <summary>
        ///   <para>svfloat64_t svabs[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svabs[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svabs[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FABS Ztied.D, Pg/M, Zop.D</para>
        ///   <para>  FABS Ztied.D, Pg/M, Ztied.D</para>
        /// </summary>
        public static Vector<double> Abs(Vector<double> value) => Abs(value);

        /// <summary>
        ///   <para>svint16_t svabs[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svabs[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svabs[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  ABS Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> Abs(Vector<short> value) => Abs(value);

        /// <summary>
        ///   <para>svint32_t svabs[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svabs[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svabs[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  ABS Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> Abs(Vector<int> value) => Abs(value);

        /// <summary>
        ///   <para>svint64_t svabs[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svabs[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svabs[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  ABS Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> Abs(Vector<long> value) => Abs(value);

        /// <summary>
        ///   <para>svint8_t svabs[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svabs[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svabs[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  ABS Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> Abs(Vector<sbyte> value) => Abs(value);

        /// <summary>
        ///   <para>svfloat32_t svabs[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svabs[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svabs[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FABS Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> Abs(Vector<float> value) => Abs(value);


        // Absolute compare greater than

        /// <summary>
        ///   <para>svbool_t svacgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FACGT Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> AbsoluteCompareGreaterThan(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svacgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FACGT Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> AbsoluteCompareGreaterThan(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThan(left, right);


        // Absolute compare greater than or equal to

        /// <summary>
        ///   <para>svbool_t svacge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FACGE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> AbsoluteCompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svacge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FACGE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> AbsoluteCompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareGreaterThanOrEqual(left, right);


        // Absolute compare less than

        /// <summary>
        ///   <para>svbool_t svaclt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FACLT Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> AbsoluteCompareLessThan(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svaclt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FACLT Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> AbsoluteCompareLessThan(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThan(left, right);


        // Absolute compare less than or equal to

        /// <summary>
        ///   <para>svbool_t svacle[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FACLE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> AbsoluteCompareLessThanOrEqual(Vector<double> left, Vector<double> right) => AbsoluteCompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svacle[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FACLE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> AbsoluteCompareLessThanOrEqual(Vector<float> left, Vector<float> right) => AbsoluteCompareLessThanOrEqual(left, right);


        // Absolute difference

        /// <summary>
        ///   <para>svuint8_t svabd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svabd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> AbsoluteDifference(Vector<byte> left, Vector<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svfloat64_t svabd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svabd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svabd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> AbsoluteDifference(Vector<double> left, Vector<double> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svint16_t svabd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svabd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svabd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  SABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> AbsoluteDifference(Vector<short> left, Vector<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svint32_t svabd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svabd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svabd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  SABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> AbsoluteDifference(Vector<int> left, Vector<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svint64_t svabd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svabd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svabd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  SABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> AbsoluteDifference(Vector<long> left, Vector<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svint8_t svabd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svabd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svabd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  SABD Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> AbsoluteDifference(Vector<sbyte> left, Vector<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svfloat32_t svabd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svabd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svabd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> AbsoluteDifference(Vector<float> left, Vector<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svuint16_t svabd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svabd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svabd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UABD Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> AbsoluteDifference(Vector<ushort> left, Vector<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svuint32_t svabd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svabd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svabd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UABD Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> AbsoluteDifference(Vector<uint> left, Vector<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        ///   <para>svuint64_t svabd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svabd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svabd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UABD Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> AbsoluteDifference(Vector<ulong> left, Vector<ulong> right) => AbsoluteDifference(left, right);


        // Add

        /// <summary>
        ///   <para>svuint8_t svadd[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svadd[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svadd[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  ADD Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> Add(Vector<byte> left, Vector<byte> right) => Add(left, right);

        /// <summary>
        ///   <para>svfloat64_t svadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FADD Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> Add(Vector<double> left, Vector<double> right) => Add(left, right);

        /// <summary>
        ///   <para>svint16_t svadd[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svadd[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svadd[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  ADD Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> Add(Vector<short> left, Vector<short> right) => Add(left, right);

        /// <summary>
        ///   <para>svint32_t svadd[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svadd[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svadd[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  ADD Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> Add(Vector<int> left, Vector<int> right) => Add(left, right);

        /// <summary>
        ///   <para>svint64_t svadd[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svadd[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svadd[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  ADD Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> Add(Vector<long> left, Vector<long> right) => Add(left, right);

        /// <summary>
        ///   <para>svint8_t svadd[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svadd[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svadd[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  ADD Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> Add(Vector<sbyte> left, Vector<sbyte> right) => Add(left, right);

        /// <summary>
        ///   <para>svfloat32_t svadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FADD Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> Add(Vector<float> left, Vector<float> right) => Add(left, right);

        /// <summary>
        ///   <para>svuint16_t svadd[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svadd[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svadd[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  ADD Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> Add(Vector<ushort> left, Vector<ushort> right) => Add(left, right);

        /// <summary>
        ///   <para>svuint32_t svadd[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svadd[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svadd[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  ADD Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> Add(Vector<uint> left, Vector<uint> right) => Add(left, right);

        /// <summary>
        ///   <para>svuint64_t svadd[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svadd[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svadd[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  ADD Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> Add(Vector<ulong> left, Vector<ulong> right) => Add(left, right);


        // Add reduction

        /// <summary>
        ///   <para>float64_t svaddv[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FADDV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> AddAcross(Vector<double> value) => AddAcross(value);

        /// <summary>
        ///   <para>int64_t svaddv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  SADDV Dresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<long> AddAcross(Vector<short> value) => AddAcross(value);

        /// <summary>
        ///   <para>int64_t svaddv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  SADDV Dresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<long> AddAcross(Vector<int> value) => AddAcross(value);

        /// <summary>
        ///   <para>int64_t svaddv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  SADDV Dresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<long> AddAcross(Vector<sbyte> value) => AddAcross(value);

        /// <summary>
        ///   <para>int64_t svaddv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  UADDV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> AddAcross(Vector<long> value) => AddAcross(value);

        /// <summary>
        ///   <para>float32_t svaddv[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FADDV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> AddAcross(Vector<float> value) => AddAcross(value);

        /// <summary>
        ///   <para>uint64_t svaddv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  UADDV Dresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<ulong> AddAcross(Vector<byte> value) => AddAcross(value);

        /// <summary>
        ///   <para>uint64_t svaddv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  UADDV Dresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ulong> AddAcross(Vector<ushort> value) => AddAcross(value);

        /// <summary>
        ///   <para>uint64_t svaddv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  UADDV Dresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<ulong> AddAcross(Vector<uint> value) => AddAcross(value);

        /// <summary>
        ///   <para>uint64_t svaddv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  UADDV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> AddAcross(Vector<ulong> value) => AddAcross(value);


        // Complex add with rotate

        /// <summary>
        ///   <para>svfloat64_t svcadd[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)</para>
        ///   <para>svfloat64_t svcadd[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)</para>
        ///   <para>svfloat64_t svcadd[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, uint64_t imm_rotation)</para>
        ///   <para>  FCADD Ztied1.D, Pg/M, Ztied1.D, Zop2.D, #imm_rotation</para>
        /// </summary>
        public static Vector<double> AddRotateComplex(Vector<double> left, Vector<double> right, [ConstantExpected(Min = 0, Max = (byte)(1))] byte rotation) => AddRotateComplex(left, right, rotation);

        /// <summary>
        ///   <para>svfloat32_t svcadd[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)</para>
        ///   <para>svfloat32_t svcadd[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)</para>
        ///   <para>svfloat32_t svcadd[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, uint64_t imm_rotation)</para>
        ///   <para>  FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation</para>
        ///   <para>  FCADD Ztied1.S, Pg/M, Ztied1.S, Zop2.S, #imm_rotation</para>
        /// </summary>
        public static Vector<float> AddRotateComplex(Vector<float> left, Vector<float> right, [ConstantExpected(Min = 0, Max = (byte)(1))] byte rotation) => AddRotateComplex(left, right, rotation);


        // Saturating add

        /// <summary>
        ///   <para>svuint8_t svqadd[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UQADD Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> AddSaturate(Vector<byte> left, Vector<byte> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svint16_t svqadd[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  SQADD Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> AddSaturate(Vector<short> left, Vector<short> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svint32_t svqadd[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  SQADD Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> AddSaturate(Vector<int> left, Vector<int> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svint64_t svqadd[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  SQADD Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> AddSaturate(Vector<long> left, Vector<long> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svint8_t svqadd[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  SQADD Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> AddSaturate(Vector<sbyte> left, Vector<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svuint16_t svqadd[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UQADD Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> AddSaturate(Vector<ushort> left, Vector<ushort> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svuint32_t svqadd[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UQADD Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> AddSaturate(Vector<uint> left, Vector<uint> right) => AddSaturate(left, right);

        /// <summary>
        ///   <para>svuint64_t svqadd[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UQADD Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> AddSaturate(Vector<ulong> left, Vector<ulong> right) => AddSaturate(left, right);


        // Add reduction (strictly-ordered)

        /// <summary>
        ///   <para>float64_t svadda[_f64](svbool_t pg, float64_t initial, svfloat64_t op)</para>
        ///   <para>  FADDA Dtied, Pg, Dtied, Zop.D</para>
        /// </summary>
        public static Vector<double> AddSequentialAcross(Vector<double> initial, Vector<double> value) => AddSequentialAcross(initial, value);

        /// <summary>
        ///   <para>float32_t svadda[_f32](svbool_t pg, float32_t initial, svfloat32_t op)</para>
        ///   <para>  FADDA Stied, Pg, Stied, Zop.S</para>
        /// </summary>
        public static Vector<float> AddSequentialAcross(Vector<float> initial, Vector<float> value) => AddSequentialAcross(initial, value);


        // Bitwise AND

        /// <summary>
        ///   <para>svuint8_t svand[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svand[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svand[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<byte> And(Vector<byte> left, Vector<byte> right) => And(left, right);

        /// <summary>
        ///   <para>svint16_t svand[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svand[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svand[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<short> And(Vector<short> left, Vector<short> right) => And(left, right);

        /// <summary>
        ///   <para>svint32_t svand[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svand[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svand[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<int> And(Vector<int> left, Vector<int> right) => And(left, right);

        /// <summary>
        ///   <para>svint64_t svand[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svand[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svand[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> And(Vector<long> left, Vector<long> right) => And(left, right);

        /// <summary>
        ///   <para>svint8_t svand[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svand[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svand[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> And(Vector<sbyte> left, Vector<sbyte> right) => And(left, right);

        /// <summary>
        ///   <para>svuint16_t svand[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svand[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svand[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> And(Vector<ushort> left, Vector<ushort> right) => And(left, right);

        /// <summary>
        ///   <para>svuint32_t svand[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svand[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svand[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<uint> And(Vector<uint> left, Vector<uint> right) => And(left, right);

        /// <summary>
        ///   <para>svuint64_t svand[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svand[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svand[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  AND Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> And(Vector<ulong> left, Vector<ulong> right) => And(left, right);


        // Bitwise AND reduction to scalar

        /// <summary>
        ///   <para>uint8_t svandv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  ANDV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<byte> AndAcross(Vector<byte> value) => AndAcross(value);

        /// <summary>
        ///   <para>int16_t svandv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  ANDV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<short> AndAcross(Vector<short> value) => AndAcross(value);

        /// <summary>
        ///   <para>int32_t svandv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  ANDV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> AndAcross(Vector<int> value) => AndAcross(value);

        /// <summary>
        ///   <para>int64_t svandv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  ANDV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> AndAcross(Vector<long> value) => AndAcross(value);

        /// <summary>
        ///   <para>int8_t svandv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  ANDV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> AndAcross(Vector<sbyte> value) => AndAcross(value);

        /// <summary>
        ///   <para>uint16_t svandv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  ANDV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ushort> AndAcross(Vector<ushort> value) => AndAcross(value);

        /// <summary>
        ///   <para>uint32_t svandv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  ANDV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> AndAcross(Vector<uint> value) => AndAcross(value);

        /// <summary>
        ///   <para>uint64_t svandv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  ANDV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> AndAcross(Vector<ulong> value) => AndAcross(value);


        // Bitwise clear

        /// <summary>
        ///   <para>svuint8_t svbic[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svbic[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svbic[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<byte> BitwiseClear(Vector<byte> left, Vector<byte> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svint16_t svbic[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svbic[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svbic[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<short> BitwiseClear(Vector<short> left, Vector<short> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svint32_t svbic[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svbic[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svbic[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<int> BitwiseClear(Vector<int> left, Vector<int> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svint64_t svbic[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svbic[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svbic[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> BitwiseClear(Vector<long> left, Vector<long> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svint8_t svbic[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svbic[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svbic[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  BIC Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> BitwiseClear(Vector<sbyte> left, Vector<sbyte> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svuint16_t svbic[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svbic[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svbic[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  BIC Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> BitwiseClear(Vector<ushort> left, Vector<ushort> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svuint32_t svbic[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svbic[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svbic[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  BIC Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<uint> BitwiseClear(Vector<uint> left, Vector<uint> right) => BitwiseClear(left, right);

        /// <summary>
        ///   <para>svuint64_t svbic[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svbic[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svbic[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  BIC Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  BIC Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> BitwiseClear(Vector<ulong> left, Vector<ulong> right) => BitwiseClear(left, right);


        // Logically invert boolean condition

        /// <summary>
        ///   <para>svuint8_t svcnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svcnot[_u8]_x(svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svcnot[_u8]_z(svbool_t pg, svuint8_t op)</para>
        ///   <para>  CNOT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> BooleanNot(Vector<byte> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svint16_t svcnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svcnot[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svcnot[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  CNOT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> BooleanNot(Vector<short> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svint32_t svcnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svcnot[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svcnot[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  CNOT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> BooleanNot(Vector<int> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svint64_t svcnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svcnot[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svcnot[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  CNOT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> BooleanNot(Vector<long> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svint8_t svcnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svcnot[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svcnot[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  CNOT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> BooleanNot(Vector<sbyte> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svuint16_t svcnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svcnot[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svcnot[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  CNOT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> BooleanNot(Vector<ushort> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svuint32_t svcnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svcnot[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svcnot[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  CNOT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> BooleanNot(Vector<uint> value) => BooleanNot(value);

        /// <summary>
        ///   <para>svuint64_t svcnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svcnot[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svcnot[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  CNOT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> BooleanNot(Vector<ulong> value) => BooleanNot(value);


        // Shuffle active elements of vector to the right and fill with zero

        /// <summary>
        ///   <para>svfloat64_t svcompact[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  COMPACT Zresult.D, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> Compact(Vector<double> mask, Vector<double> value) => Compact(mask, value);

        /// <summary>
        ///   <para>svint32_t svcompact[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  COMPACT Zresult.S, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> Compact(Vector<int> mask, Vector<int> value) => Compact(mask, value);

        /// <summary>
        ///   <para>svint64_t svcompact[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  COMPACT Zresult.D, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> Compact(Vector<long> mask, Vector<long> value) => Compact(mask, value);

        /// <summary>
        ///   <para>svfloat32_t svcompact[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  COMPACT Zresult.S, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> Compact(Vector<float> mask, Vector<float> value) => Compact(mask, value);

        /// <summary>
        ///   <para>svuint32_t svcompact[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  COMPACT Zresult.S, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> Compact(Vector<uint> mask, Vector<uint> value) => Compact(mask, value);

        /// <summary>
        ///   <para>svuint64_t svcompact[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  COMPACT Zresult.D, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> Compact(Vector<ulong> mask, Vector<ulong> value) => Compact(mask, value);


        // Compare equal to

        /// <summary>
        ///   <para>svbool_t svcmpeq[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> CompareEqual(Vector<byte> left, Vector<byte> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMEQ Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> CompareEqual(Vector<double> left, Vector<double> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> CompareEqual(Vector<short> left, Vector<short> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareEqual(Vector<short> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> CompareEqual(Vector<int> left, Vector<int> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareEqual(Vector<int> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> CompareEqual(Vector<long> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPEQ Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareEqual(Vector<sbyte> left, Vector<long> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMEQ Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> CompareEqual(Vector<float> left, Vector<float> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPEQ Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> CompareEqual(Vector<ushort> left, Vector<ushort> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPEQ Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> CompareEqual(Vector<uint> left, Vector<uint> right) => CompareEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpeq[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPEQ Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> CompareEqual(Vector<ulong> left, Vector<ulong> right) => CompareEqual(left, right);


        // Compare greater than

        /// <summary>
        ///   <para>svbool_t svcmpgt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHI Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> CompareGreaterThan(Vector<byte> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMGT Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> CompareGreaterThan(Vector<double> left, Vector<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> CompareGreaterThan(Vector<short> left, Vector<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPGT Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareGreaterThan(Vector<short> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> CompareGreaterThan(Vector<int> left, Vector<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPGT Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareGreaterThan(Vector<int> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPGT Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> CompareGreaterThan(Vector<long> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPGT Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareGreaterThan(Vector<sbyte> left, Vector<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMGT Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> CompareGreaterThan(Vector<float> left, Vector<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHI Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> CompareGreaterThan(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHI Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> CompareGreaterThan(Vector<uint> left, Vector<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpgt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHI Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> CompareGreaterThan(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThan(left, right);


        // Compare greater than or equal to

        /// <summary>
        ///   <para>svbool_t svcmpge[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHS Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> CompareGreaterThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMGE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> CompareGreaterThanOrEqual(Vector<double> left, Vector<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPGE Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareGreaterThanOrEqual(Vector<short> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPGE Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareGreaterThanOrEqual(Vector<int> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPGE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> CompareGreaterThanOrEqual(Vector<long> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPGE Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareGreaterThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMGE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> CompareGreaterThanOrEqual(Vector<float> left, Vector<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHS Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> CompareGreaterThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHS Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> CompareGreaterThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpge[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHS Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> CompareGreaterThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareGreaterThanOrEqual(left, right);


        // Compare less than

        /// <summary>
        ///   <para>svbool_t svcmplt[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPHI Presult.B, Pg/Z, Zop2.B, Zop1.B</para>
        /// </summary>
        public static Vector<byte> CompareLessThan(Vector<byte> left, Vector<byte> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLO Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> CompareLessThan(Vector<byte> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMGT Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> CompareLessThan(Vector<double> left, Vector<double> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPGT Presult.H, Pg/Z, Zop2.H, Zop1.H</para>
        /// </summary>
        public static Vector<short> CompareLessThan(Vector<short> left, Vector<short> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPLT Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareLessThan(Vector<short> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPGT Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<int> CompareLessThan(Vector<int> left, Vector<int> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPLT Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareLessThan(Vector<int> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPGT Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<long> CompareLessThan(Vector<long> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPGT Presult.B, Pg/Z, Zop2.B, Zop1.B</para>
        /// </summary>
        public static Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPLT Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareLessThan(Vector<sbyte> left, Vector<long> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMGT Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> CompareLessThan(Vector<float> left, Vector<float> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPHI Presult.H, Pg/Z, Zop2.H, Zop1.H</para>
        /// </summary>
        public static Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLO Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> CompareLessThan(Vector<ushort> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPHI Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<uint> CompareLessThan(Vector<uint> left, Vector<uint> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLO Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> CompareLessThan(Vector<uint> left, Vector<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        ///   <para>svbool_t svcmplt[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHI Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<ulong> CompareLessThan(Vector<ulong> left, Vector<ulong> right) => CompareLessThan(left, right);


        // Compare less than or equal to

        /// <summary>
        ///   <para>svbool_t svcmple[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPHS Presult.B, Pg/Z, Zop2.B, Zop1.B</para>
        /// </summary>
        public static Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_u8](svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLS Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> CompareLessThanOrEqual(Vector<byte> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMGE Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> CompareLessThanOrEqual(Vector<double> left, Vector<double> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPGE Presult.H, Pg/Z, Zop2.H, Zop1.H</para>
        /// </summary>
        public static Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPLE Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareLessThanOrEqual(Vector<short> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPGE Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPLE Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareLessThanOrEqual(Vector<int> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPGE Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<long> CompareLessThanOrEqual(Vector<long> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPGE Presult.B, Pg/Z, Zop2.B, Zop1.B</para>
        /// </summary>
        public static Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPLE Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareLessThanOrEqual(Vector<sbyte> left, Vector<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMGE Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> CompareLessThanOrEqual(Vector<float> left, Vector<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPHS Presult.H, Pg/Z, Zop2.H, Zop1.H</para>
        /// </summary>
        public static Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_u16](svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLS Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> CompareLessThanOrEqual(Vector<ushort> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPHS Presult.S, Pg/Z, Zop2.S, Zop1.S</para>
        /// </summary>
        public static Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple_wide[_u32](svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  CMPLS Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> CompareLessThanOrEqual(Vector<uint> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        ///   <para>svbool_t svcmple[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPHS Presult.D, Pg/Z, Zop2.D, Zop1.D</para>
        /// </summary>
        public static Vector<ulong> CompareLessThanOrEqual(Vector<ulong> left, Vector<ulong> right) => CompareLessThanOrEqual(left, right);


        // Compare not equal to

        /// <summary>
        ///   <para>svbool_t svcmpne[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> CompareNotEqualTo(Vector<byte> left, Vector<byte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMNE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> CompareNotEqualTo(Vector<double> left, Vector<double> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> CompareNotEqualTo(Vector<short> left, Vector<short> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne_wide[_s16](svbool_t pg, svint16_t op1, svint64_t op2)</para>
        ///   <para>  CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> CompareNotEqualTo(Vector<short> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> CompareNotEqualTo(Vector<int> left, Vector<int> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne_wide[_s32](svbool_t pg, svint32_t op1, svint64_t op2)</para>
        ///   <para>  CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> CompareNotEqualTo(Vector<int> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> CompareNotEqualTo(Vector<long> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<sbyte> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne_wide[_s8](svbool_t pg, svint8_t op1, svint64_t op2)</para>
        ///   <para>  CMPNE Presult.B, Pg/Z, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> CompareNotEqualTo(Vector<sbyte> left, Vector<long> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMNE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> CompareNotEqualTo(Vector<float> left, Vector<float> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  CMPNE Presult.H, Pg/Z, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> CompareNotEqualTo(Vector<ushort> left, Vector<ushort> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  CMPNE Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> CompareNotEqualTo(Vector<uint> left, Vector<uint> right) => CompareNotEqualTo(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpne[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  CMPNE Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> CompareNotEqualTo(Vector<ulong> left, Vector<ulong> right) => CompareNotEqualTo(left, right);


        // Compare unordered with

        /// <summary>
        ///   <para>svbool_t svcmpuo[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FCMUO Presult.D, Pg/Z, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> CompareUnordered(Vector<double> left, Vector<double> right) => CompareUnordered(left, right);

        /// <summary>
        ///   <para>svbool_t svcmpuo[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FCMUO Presult.S, Pg/Z, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> CompareUnordered(Vector<float> left, Vector<float> right) => CompareUnordered(left, right);


        // Compute vector addresses for 16-bit data

        /// <summary>
        ///   <para>svuint32_t svadrh[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]</para>
        /// </summary>
        public static Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint32_t svadrh[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #1]</para>
        /// </summary>
        public static Vector<uint> Compute16BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrh[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]</para>
        /// </summary>
        public static Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute16BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrh[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #1]</para>
        /// </summary>
        public static Vector<ulong> Compute16BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute16BitAddresses(bases, indices);


        // Compute vector addresses for 32-bit data

        /// <summary>
        ///   <para>svuint32_t svadrw[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]</para>
        /// </summary>
        public static Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint32_t svadrw[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #2]</para>
        /// </summary>
        public static Vector<uint> Compute32BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrw[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]</para>
        /// </summary>
        public static Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute32BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrw[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #2]</para>
        /// </summary>
        public static Vector<ulong> Compute32BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute32BitAddresses(bases, indices);


        // Compute vector addresses for 64-bit data

        /// <summary>
        ///   <para>svuint32_t svadrd[_u32base]_[s32]index(svuint32_t bases, svint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]</para>
        /// </summary>
        public static Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint32_t svadrd[_u32base]_[u32]index(svuint32_t bases, svuint32_t indices)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zindices.S, LSL #3]</para>
        /// </summary>
        public static Vector<uint> Compute64BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrd[_u64base]_[s64]index(svuint64_t bases, svint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]</para>
        /// </summary>
        public static Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute64BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrd[_u64base]_[u64]index(svuint64_t bases, svuint64_t indices)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zindices.D, LSL #3]</para>
        /// </summary>
        public static Vector<ulong> Compute64BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute64BitAddresses(bases, indices);


        // Compute vector addresses for 8-bit data

        /// <summary>
        ///   <para>svuint32_t svadrb[_u32base]_[s32]offset(svuint32_t bases, svint32_t offsets)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zoffsets.S]</para>
        /// </summary>
        public static Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<int> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint32_t svadrb[_u32base]_[u32]offset(svuint32_t bases, svuint32_t offsets)</para>
        ///   <para>  ADR Zresult.S, [Zbases.S, Zoffsets.S]</para>
        /// </summary>
        public static Vector<uint> Compute8BitAddresses(Vector<uint> bases, Vector<uint> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrb[_u64base]_[s64]offset(svuint64_t bases, svint64_t offsets)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zoffsets.D]</para>
        /// </summary>
        public static Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<long> indices) => Compute8BitAddresses(bases, indices);

        /// <summary>
        ///   <para>svuint64_t svadrb[_u64base]_[u64]offset(svuint64_t bases, svuint64_t offsets)</para>
        ///   <para>  ADR Zresult.D, [Zbases.D, Zoffsets.D]</para>
        /// </summary>
        public static Vector<ulong> Compute8BitAddresses(Vector<ulong> bases, Vector<ulong> indices) => Compute8BitAddresses(bases, indices);


        // Conditionally extract element after last

        /// <summary>
        ///   <para>svuint8_t svclasta[_u8](svbool_t pg, svuint8_t defaultScalar, svuint8_t data)</para>
        ///   <para>  CLASTA Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ConditionalExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint8_t svclasta[_n_u8](svbool_t pg, uint8_t defaultValue, svuint8_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static byte ConditionalExtractAfterLastActiveElement(Vector<byte> mask, byte defaultValue, Vector<byte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t defaultScalar, svfloat64_t data)</para>
        ///   <para>  CLASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<double> ConditionalExtractAfterLastActiveElement(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>float64_t svclasta[_n_f64](svbool_t pg, float64_t defaultValue, svfloat64_t data)</para>
        ///   <para>  CLASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static double ConditionalExtractAfterLastActiveElement(Vector<double> mask, double defaultValue, Vector<double> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint16_t svclasta[_s16](svbool_t pg, svint16_t defaultScalar, svint16_t data)</para>
        ///   <para>  CLASTA Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<short> ConditionalExtractAfterLastActiveElement(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int16_t svclasta[_n_s16](svbool_t pg, int16_t defaultValue, svint16_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static short ConditionalExtractAfterLastActiveElement(Vector<short> mask, short defaultValue, Vector<short> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint32_t svclasta[_s32](svbool_t pg, svint32_t defaultScalar, svint32_t data)</para>
        ///   <para>  CLASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<int> ConditionalExtractAfterLastActiveElement(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int32_t svclasta[_n_s32](svbool_t pg, int32_t defaultValue, svint32_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static int ConditionalExtractAfterLastActiveElement(Vector<int> mask, int defaultValue, Vector<int> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint64_t svclasta[_s64](svbool_t pg, svint64_t defaultScalar, svint64_t data)</para>
        ///   <para>  CLASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<long> ConditionalExtractAfterLastActiveElement(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int64_t svclasta[_n_s64](svbool_t pg, int64_t defaultValue, svint64_t data)</para>
        ///   <para>  CLASTA Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static long ConditionalExtractAfterLastActiveElement(Vector<long> mask, long defaultValue, Vector<long> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint8_t svclasta[_s8](svbool_t pg, svint8_t defaultScalar, svint8_t data)</para>
        ///   <para>  CLASTA Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int8_t svclasta[_n_s8](svbool_t pg, int8_t defaultValue, svint8_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static sbyte ConditionalExtractAfterLastActiveElement(Vector<sbyte> mask, sbyte defaultValue, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t defaultScalar, svfloat32_t data)</para>
        ///   <para>  CLASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<float> ConditionalExtractAfterLastActiveElement(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>float32_t svclasta[_n_f32](svbool_t pg, float32_t defaultValue, svfloat32_t data)</para>
        ///   <para>  CLASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static float ConditionalExtractAfterLastActiveElement(Vector<float> mask, float defaultValue, Vector<float> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint16_t svclasta[_u16](svbool_t pg, svuint16_t defaultScalar, svuint16_t data)</para>
        ///   <para>  CLASTA Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint16_t svclasta[_n_u16](svbool_t pg, uint16_t defaultValue, svuint16_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static ushort ConditionalExtractAfterLastActiveElement(Vector<ushort> mask, ushort defaultValue, Vector<ushort> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint32_t svclasta[_u32](svbool_t pg, svuint32_t defaultScalar, svuint32_t data)</para>
        ///   <para>  CLASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ConditionalExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint32_t svclasta[_n_u32](svbool_t pg, uint32_t defaultValue, svuint32_t data)</para>
        ///   <para>  CLASTA Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static uint ConditionalExtractAfterLastActiveElement(Vector<uint> mask, uint defaultValue, Vector<uint> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint64_t svclasta[_u64](svbool_t pg, svuint64_t defaultScalar, svuint64_t data)</para>
        ///   <para>  CLASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint64_t svclasta[_n_u64](svbool_t pg, uint64_t defaultValue, svuint64_t data)</para>
        ///   <para>  CLASTA Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static ulong ConditionalExtractAfterLastActiveElement(Vector<ulong> mask, ulong defaultValue, Vector<ulong> data) => ConditionalExtractAfterLastActiveElement(mask, defaultValue, data);


        // Conditionally extract element after last

        /// <summary>
        ///   <para>svuint8_t svclasta[_u8](svbool_t pg, svuint8_t defaultValues, svuint8_t data)</para>
        ///   <para>  CLASTA Ztied.B, Pg, Ztied.B, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultValues, Vector<byte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svfloat64_t svclasta[_f64](svbool_t pg, svfloat64_t defaultValues, svfloat64_t data)</para>
        ///   <para>  CLASTA Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<double> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultValues, Vector<double> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint16_t svclasta[_s16](svbool_t pg, svint16_t defaultValues, svint16_t data)</para>
        ///   <para>  CLASTA Ztied.H, Pg, Ztied.H, Zdata.H</para>
        /// </summary>
        public static Vector<short> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultValues, Vector<short> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint32_t svclasta[_s32](svbool_t pg, svint32_t defaultValues, svint32_t data)</para>
        ///   <para>  CLASTA Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<int> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultValues, Vector<int> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint64_t svclasta[_s64](svbool_t pg, svint64_t defaultValues, svint64_t data)</para>
        ///   <para>  CLASTA Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<long> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultValues, Vector<long> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint8_t svclasta[_s8](svbool_t pg, svint8_t defaultValues, svint8_t data)</para>
        ///   <para>  CLASTA Ztied.B, Pg, Ztied.B, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultValues, Vector<sbyte> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svfloat32_t svclasta[_f32](svbool_t pg, svfloat32_t defaultValues, svfloat32_t data)</para>
        ///   <para>  CLASTA Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<float> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultValues, Vector<float> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint16_t svclasta[_u16](svbool_t pg, svuint16_t defaultValues, svuint16_t data)</para>
        ///   <para>  CLASTA Ztied.H, Pg, Ztied.H, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultValues, Vector<ushort> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint32_t svclasta[_u32](svbool_t pg, svuint32_t defaultValues, svuint32_t data)</para>
        ///   <para>  CLASTA Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultValues, Vector<uint> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint64_t svclasta[_u64](svbool_t pg, svuint64_t defaultValues, svuint64_t data)</para>
        ///   <para>  CLASTA Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ConditionalExtractAfterLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultValues, Vector<ulong> data) => ConditionalExtractAfterLastActiveElementAndReplicate(mask, defaultValues, data);


        // Conditionally extract last element

        /// <summary>
        ///   <para>svuint8_t svclastb[_u8](svbool_t pg, svuint8_t defaultScalar, svuint8_t data)</para>
        ///   <para>  CLASTB Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ConditionalExtractLastActiveElement(Vector<byte> mask, Vector<byte> defaultScalar, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint8_t svclastb[_n_u8](svbool_t pg, uint8_t defaultValue, svuint8_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static byte ConditionalExtractLastActiveElement(Vector<byte> mask, byte defaultValue, Vector<byte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t defaultScalar, svfloat64_t data)</para>
        ///   <para>  CLASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<double> ConditionalExtractLastActiveElement(Vector<double> mask, Vector<double> defaultScalar, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>float64_t svclastb[_n_f64](svbool_t pg, float64_t defaultValue, svfloat64_t data)</para>
        ///   <para>  CLASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static double ConditionalExtractLastActiveElement(Vector<double> mask, double defaultValue, Vector<double> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint16_t svclastb[_s16](svbool_t pg, svint16_t defaultScalar, svint16_t data)</para>
        ///   <para>  CLASTB Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<short> ConditionalExtractLastActiveElement(Vector<short> mask, Vector<short> defaultScalar, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int16_t svclastb[_n_s16](svbool_t pg, int16_t defaultValue, svint16_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static short ConditionalExtractLastActiveElement(Vector<short> mask, short defaultValue, Vector<short> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint32_t svclastb[_s32](svbool_t pg, svint32_t defaultScalar, svint32_t data)</para>
        ///   <para>  CLASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<int> ConditionalExtractLastActiveElement(Vector<int> mask, Vector<int> defaultScalar, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int32_t svclastb[_n_s32](svbool_t pg, int32_t defaultValue, svint32_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static int ConditionalExtractLastActiveElement(Vector<int> mask, int defaultValue, Vector<int> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint64_t svclastb[_s64](svbool_t pg, svint64_t defaultScalar, svint64_t data)</para>
        ///   <para>  CLASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<long> ConditionalExtractLastActiveElement(Vector<long> mask, Vector<long> defaultScalar, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int64_t svclastb[_n_s64](svbool_t pg, int64_t defaultValue, svint64_t data)</para>
        ///   <para>  CLASTB Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static long ConditionalExtractLastActiveElement(Vector<long> mask, long defaultValue, Vector<long> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svint8_t svclastb[_s8](svbool_t pg, svint8_t defaultScalar, svint8_t data)</para>
        ///   <para>  CLASTB Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ConditionalExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> defaultScalar, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>int8_t svclastb[_n_s8](svbool_t pg, int8_t defaultValue, svint8_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static sbyte ConditionalExtractLastActiveElement(Vector<sbyte> mask, sbyte defaultValue, Vector<sbyte> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t defaultScalar, svfloat32_t data)</para>
        ///   <para>  CLASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<float> ConditionalExtractLastActiveElement(Vector<float> mask, Vector<float> defaultScalar, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>float32_t svclastb[_n_f32](svbool_t pg, float32_t defaultValue, svfloat32_t data)</para>
        ///   <para>  CLASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static float ConditionalExtractLastActiveElement(Vector<float> mask, float defaultValue, Vector<float> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint16_t svclastb[_u16](svbool_t pg, svuint16_t defaultScalar, svuint16_t data)</para>
        ///   <para>  CLASTB Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ConditionalExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> defaultScalar, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint16_t svclastb[_n_u16](svbool_t pg, uint16_t defaultValue, svuint16_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static ushort ConditionalExtractLastActiveElement(Vector<ushort> mask, ushort defaultValue, Vector<ushort> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint32_t svclastb[_u32](svbool_t pg, svuint32_t defaultScalar, svuint32_t data)</para>
        ///   <para>  CLASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ConditionalExtractLastActiveElement(Vector<uint> mask, Vector<uint> defaultScalar, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint32_t svclastb[_n_u32](svbool_t pg, uint32_t defaultValue, svuint32_t data)</para>
        ///   <para>  CLASTB Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static uint ConditionalExtractLastActiveElement(Vector<uint> mask, uint defaultValue, Vector<uint> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);

        /// <summary>
        ///   <para>svuint64_t svclastb[_u64](svbool_t pg, svuint64_t defaultScalar, svuint64_t data)</para>
        ///   <para>  CLASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ConditionalExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> defaultScalar, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultScalar, data);

        /// <summary>
        ///   <para>uint64_t svclastb[_n_u64](svbool_t pg, uint64_t defaultValue, svuint64_t data)</para>
        ///   <para>  CLASTB Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static ulong ConditionalExtractLastActiveElement(Vector<ulong> mask, ulong defaultValue, Vector<ulong> data) => ConditionalExtractLastActiveElement(mask, defaultValue, data);


        // Conditionally extract last element

        /// <summary>
        ///   <para>svuint8_t svclastb[_u8](svbool_t pg, svuint8_t defaultValues, svuint8_t data)</para>
        ///   <para>  CLASTB Ztied.B, Pg, Ztied.B, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ConditionalExtractLastActiveElementAndReplicate(Vector<byte> mask, Vector<byte> defaultValues, Vector<byte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svfloat64_t svclastb[_f64](svbool_t pg, svfloat64_t defaultValues, svfloat64_t data)</para>
        ///   <para>  CLASTB Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<double> ConditionalExtractLastActiveElementAndReplicate(Vector<double> mask, Vector<double> defaultValues, Vector<double> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint16_t svclastb[_s16](svbool_t pg, svint16_t defaultValues, svint16_t data)</para>
        ///   <para>  CLASTB Ztied.H, Pg, Ztied.H, Zdata.H</para>
        /// </summary>
        public static Vector<short> ConditionalExtractLastActiveElementAndReplicate(Vector<short> mask, Vector<short> defaultValues, Vector<short> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint32_t svclastb[_s32](svbool_t pg, svint32_t defaultValues, svint32_t data)</para>
        ///   <para>  CLASTB Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<int> ConditionalExtractLastActiveElementAndReplicate(Vector<int> mask, Vector<int> defaultValues, Vector<int> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint64_t svclastb[_s64](svbool_t pg, svint64_t defaultValues, svint64_t data)</para>
        ///   <para>  CLASTB Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<long> ConditionalExtractLastActiveElementAndReplicate(Vector<long> mask, Vector<long> defaultValues, Vector<long> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svint8_t svclastb[_s8](svbool_t pg, svint8_t defaultValues, svint8_t data)</para>
        ///   <para>  CLASTB Ztied.B, Pg, Ztied.B, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ConditionalExtractLastActiveElementAndReplicate(Vector<sbyte> mask, Vector<sbyte> defaultValues, Vector<sbyte> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svfloat32_t svclastb[_f32](svbool_t pg, svfloat32_t defaultValues, svfloat32_t data)</para>
        ///   <para>  CLASTB Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<float> ConditionalExtractLastActiveElementAndReplicate(Vector<float> mask, Vector<float> defaultValues, Vector<float> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint16_t svclastb[_u16](svbool_t pg, svuint16_t defaultValues, svuint16_t data)</para>
        ///   <para>  CLASTB Ztied.H, Pg, Ztied.H, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ConditionalExtractLastActiveElementAndReplicate(Vector<ushort> mask, Vector<ushort> defaultValues, Vector<ushort> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint32_t svclastb[_u32](svbool_t pg, svuint32_t defaultValues, svuint32_t data)</para>
        ///   <para>  CLASTB Ztied.S, Pg, Ztied.S, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ConditionalExtractLastActiveElementAndReplicate(Vector<uint> mask, Vector<uint> defaultValues, Vector<uint> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);

        /// <summary>
        ///   <para>svuint64_t svclastb[_u64](svbool_t pg, svuint64_t defaultValues, svuint64_t data)</para>
        ///   <para>  CLASTB Ztied.D, Pg, Ztied.D, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ConditionalExtractLastActiveElementAndReplicate(Vector<ulong> mask, Vector<ulong> defaultValues, Vector<ulong> data) => ConditionalExtractLastActiveElementAndReplicate(mask, defaultValues, data);


        // Conditionally select elements

        /// <summary>
        ///   <para>svuint8_t svsel[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.B, Pg, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> ConditionalSelect(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svfloat64_t svsel[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  SEL Zresult.D, Pg, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> ConditionalSelect(Vector<double> mask, Vector<double> left, Vector<double> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svint16_t svsel[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.H, Pg, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> ConditionalSelect(Vector<short> mask, Vector<short> left, Vector<short> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svint32_t svsel[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.S, Pg, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> ConditionalSelect(Vector<int> mask, Vector<int> left, Vector<int> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svint64_t svsel[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.D, Pg, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> ConditionalSelect(Vector<long> mask, Vector<long> left, Vector<long> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svint8_t svsel[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.B, Pg, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> ConditionalSelect(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svfloat32_t svsel[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  SEL Zresult.S, Pg, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> ConditionalSelect(Vector<float> mask, Vector<float> left, Vector<float> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svuint16_t svsel[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.H, Pg, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> ConditionalSelect(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svuint32_t svsel[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.S, Pg, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> ConditionalSelect(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => ConditionalSelect(mask, left, right);

        /// <summary>
        ///   <para>svuint64_t svsel[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svbool_t svsel[_b](svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  SEL Zresult.D, Pg, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> ConditionalSelect(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => ConditionalSelect(mask, left, right);


        // Floating-point convert

        /// <summary>
        ///   <para>svfloat64_t svcvt_f64[_s32]_m(svfloat64_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  SCVTF Zresult.D, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<double> ConvertToDouble(Vector<int> value) => ConvertToDouble(value);

        /// <summary>
        ///   <para>svfloat64_t svcvt_f64[_s64]_m(svfloat64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  SCVTF Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> ConvertToDouble(Vector<long> value) => ConvertToDouble(value);

        /// <summary>
        ///   <para>svfloat64_t svcvt_f64[_f32]_m(svfloat64_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FCVT Zresult.D, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<double> ConvertToDouble(Vector<float> value) => ConvertToDouble(value);

        /// <summary>
        ///   <para>svfloat64_t svcvt_f64[_u32]_m(svfloat64_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  UCVTF Zresult.D, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<double> ConvertToDouble(Vector<uint> value) => ConvertToDouble(value);

        /// <summary>
        ///   <para>svfloat64_t svcvt_f64[_u64]_m(svfloat64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svfloat64_t svcvt_f64[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  UCVTF Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> ConvertToDouble(Vector<ulong> value) => ConvertToDouble(value);


        // Floating-point convert

        /// <summary>
        ///   <para>svint32_t svcvt_s32[_f64]_m(svint32_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svint32_t svcvt_s32[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svint32_t svcvt_s32[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FCVTZS Zresult.S, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<int> ConvertToInt32(Vector<double> value) => ConvertToInt32(value);

        /// <summary>
        ///   <para>svint32_t svcvt_s32[_f32]_m(svint32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svint32_t svcvt_s32[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svint32_t svcvt_s32[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FCVTZS Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> ConvertToInt32(Vector<float> value) => ConvertToInt32(value);


        // Floating-point convert

        /// <summary>
        ///   <para>svint64_t svcvt_s64[_f64]_m(svint64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svint64_t svcvt_s64[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svint64_t svcvt_s64[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FCVTZS Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> ConvertToInt64(Vector<double> value) => ConvertToInt64(value);

        /// <summary>
        ///   <para>svint64_t svcvt_s64[_f32]_m(svint64_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svint64_t svcvt_s64[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svint64_t svcvt_s64[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FCVTZS Zresult.D, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<long> ConvertToInt64(Vector<float> value) => ConvertToInt64(value);


        // Floating-point convert

        /// <summary>
        ///   <para>svfloat32_t svcvt_f32[_f64]_m(svfloat32_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FCVT Zresult.S, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<float> ConvertToSingle(Vector<double> value) => ConvertToSingle(value);

        /// <summary>
        ///   <para>svfloat32_t svcvt_f32[_s32]_m(svfloat32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  SCVTF Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> ConvertToSingle(Vector<int> value) => ConvertToSingle(value);

        /// <summary>
        ///   <para>svfloat32_t svcvt_f32[_s64]_m(svfloat32_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  SCVTF Zresult.S, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<float> ConvertToSingle(Vector<long> value) => ConvertToSingle(value);

        /// <summary>
        ///   <para>svfloat32_t svcvt_f32[_u32]_m(svfloat32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  UCVTF Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> ConvertToSingle(Vector<uint> value) => ConvertToSingle(value);

        /// <summary>
        ///   <para>svfloat32_t svcvt_f32[_u64]_m(svfloat32_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svfloat32_t svcvt_f32[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  UCVTF Zresult.S, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<float> ConvertToSingle(Vector<ulong> value) => ConvertToSingle(value);


        // Floating-point convert

        /// <summary>
        ///   <para>svuint32_t svcvt_u32[_f64]_m(svuint32_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint32_t svcvt_u32[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint32_t svcvt_u32[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FCVTZU Zresult.S, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<uint> ConvertToUInt32(Vector<double> value) => ConvertToUInt32(value);

        /// <summary>
        ///   <para>svuint32_t svcvt_u32[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint32_t svcvt_u32[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint32_t svcvt_u32[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FCVTZU Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ConvertToUInt32(Vector<float> value) => ConvertToUInt32(value);


        // Floating-point convert

        /// <summary>
        ///   <para>svuint64_t svcvt_u64[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint64_t svcvt_u64[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint64_t svcvt_u64[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FCVTZU Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ConvertToUInt64(Vector<double> value) => ConvertToUInt64(value);

        /// <summary>
        ///   <para>svuint64_t svcvt_u64[_f32]_m(svuint64_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint64_t svcvt_u64[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint64_t svcvt_u64[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FCVTZU Zresult.D, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<ulong> ConvertToUInt64(Vector<float> value) => ConvertToUInt64(value);


        // Count the number of 16-bit elements in a vector

        /// <summary>
        ///   <para>uint64_t svcnth_pat(enum svpattern pattern)</para>
        ///   <para>  CNTH Xresult, pattern</para>
        /// </summary>
        public static ulong Count16BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count16BitElements(pattern);


        // Count the number of 32-bit elements in a vector

        /// <summary>
        ///   <para>uint64_t svcntw_pat(enum svpattern pattern)</para>
        ///   <para>  CNTW Xresult, pattern</para>
        /// </summary>
        public static ulong Count32BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count32BitElements(pattern);


        // Count the number of 64-bit elements in a vector

        /// <summary>
        ///   <para>uint64_t svcntd_pat(enum svpattern pattern)</para>
        ///   <para>  CNTD Xresult, pattern</para>
        /// </summary>
        public static ulong Count64BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count64BitElements(pattern);


        // Count the number of 8-bit elements in a vector

        /// <summary>
        ///   <para>uint64_t svcntb_pat(enum svpattern pattern)</para>
        ///   <para>  CNTB Xresult, pattern</para>
        /// </summary>
        public static ulong Count8BitElements([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => Count8BitElements(pattern);


        // Break after first true condition

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<byte> CreateBreakAfterMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<short> CreateBreakAfterMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<int> CreateBreakAfterMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<long> CreateBreakAfterMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<sbyte> CreateBreakAfterMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<ushort> CreateBreakAfterMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<uint> CreateBreakAfterMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakAfterMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrka[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKA Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<ulong> CreateBreakAfterMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakAfterMask(totalMask, fromMask);


        // Break after first true condition, propagating from previous partition

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<byte> CreateBreakAfterPropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<short> CreateBreakAfterPropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<int> CreateBreakAfterPropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<long> CreateBreakAfterPropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<sbyte> CreateBreakAfterPropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<ushort> CreateBreakAfterPropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<uint> CreateBreakAfterPropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakAfterPropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpa[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPA Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<ulong> CreateBreakAfterPropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakAfterPropagateMask(mask, left, right);


        // Break before first true condition

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<byte> CreateBreakBeforeMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<short> CreateBreakBeforeMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<int> CreateBreakBeforeMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<long> CreateBreakBeforeMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<sbyte> CreateBreakBeforeMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<ushort> CreateBreakBeforeMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<uint> CreateBreakBeforeMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkb[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  BRKB Presult.B, Pg/Z, Pop.B</para>
        /// </summary>
        public static Vector<ulong> CreateBreakBeforeMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakBeforeMask(totalMask, fromMask);


        // Break before first true condition, propagating from previous partition

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<byte> CreateBreakBeforePropagateMask(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<short> CreateBreakBeforePropagateMask(Vector<short> mask, Vector<short> left, Vector<short> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<int> CreateBreakBeforePropagateMask(Vector<int> mask, Vector<int> left, Vector<int> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<long> CreateBreakBeforePropagateMask(Vector<long> mask, Vector<long> left, Vector<long> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<sbyte> CreateBreakBeforePropagateMask(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<ushort> CreateBreakBeforePropagateMask(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<uint> CreateBreakBeforePropagateMask(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => CreateBreakBeforePropagateMask(mask, left, right);

        /// <summary>
        ///   <para>svbool_t svbrkpb[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKPB Presult.B, Pg/Z, Pop1.B, Pop2.B</para>
        /// </summary>
        public static Vector<ulong> CreateBreakBeforePropagateMask(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => CreateBreakBeforePropagateMask(mask, left, right);


        // Propagate break to next partition

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<byte> CreateBreakPropagateMask(Vector<byte> totalMask, Vector<byte> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<short> CreateBreakPropagateMask(Vector<short> totalMask, Vector<short> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<int> CreateBreakPropagateMask(Vector<int> totalMask, Vector<int> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<long> CreateBreakPropagateMask(Vector<long> totalMask, Vector<long> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<sbyte> CreateBreakPropagateMask(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<ushort> CreateBreakPropagateMask(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<uint> CreateBreakPropagateMask(Vector<uint> totalMask, Vector<uint> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svbrkn[_b]_z(svbool_t pg, svbool_t op1, svbool_t op2)</para>
        ///   <para>  BRKN Ptied2.B, Pg/Z, Pop1.B, Ptied2.B</para>
        /// </summary>
        public static Vector<ulong> CreateBreakPropagateMask(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateBreakPropagateMask(totalMask, fromMask);


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<byte> CreateFalseMaskByte() => CreateFalseMaskByte();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<double> CreateFalseMaskDouble() => CreateFalseMaskDouble();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<short> CreateFalseMaskInt16() => CreateFalseMaskInt16();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<int> CreateFalseMaskInt32() => CreateFalseMaskInt32();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<long> CreateFalseMaskInt64() => CreateFalseMaskInt64();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<sbyte> CreateFalseMaskSByte() => CreateFalseMaskSByte();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<float> CreateFalseMaskSingle() => CreateFalseMaskSingle();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<ushort> CreateFalseMaskUInt16() => CreateFalseMaskUInt16();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<uint> CreateFalseMaskUInt32() => CreateFalseMaskUInt32();


        // Set all predicate elements to false

        /// <summary>
        ///   <para>svbool_t svpfalse[_b]()</para>
        ///   <para>  PFALSE Presult.B</para>
        /// </summary>
        public static Vector<ulong> CreateFalseMaskUInt64() => CreateFalseMaskUInt64();


        // Set the first active predicate element to true

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<byte> CreateMaskForFirstActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<short> CreateMaskForFirstActiveElement(Vector<short> totalMask, Vector<short> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<int> CreateMaskForFirstActiveElement(Vector<int> totalMask, Vector<int> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<long> CreateMaskForFirstActiveElement(Vector<long> totalMask, Vector<long> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<sbyte> CreateMaskForFirstActiveElement(Vector<sbyte> totalMask, Vector<sbyte> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<ushort> CreateMaskForFirstActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<uint> CreateMaskForFirstActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpfirst[_b](svbool_t pg, svbool_t op)</para>
        ///   <para>  PFIRST Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<ulong> CreateMaskForFirstActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateMaskForFirstActiveElement(totalMask, fromMask);


        // Find next active predicate

        /// <summary>
        ///   <para>svbool_t svpnext_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  PNEXT Ptied.B, Pg, Ptied.B</para>
        /// </summary>
        public static Vector<byte> CreateMaskForNextActiveElement(Vector<byte> totalMask, Vector<byte> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpnext_b16(svbool_t pg, svbool_t op)</para>
        ///   <para>  PNEXT Ptied.H, Pg, Ptied.H</para>
        /// </summary>
        public static Vector<ushort> CreateMaskForNextActiveElement(Vector<ushort> totalMask, Vector<ushort> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpnext_b32(svbool_t pg, svbool_t op)</para>
        ///   <para>  PNEXT Ptied.S, Pg, Ptied.S</para>
        /// </summary>
        public static Vector<uint> CreateMaskForNextActiveElement(Vector<uint> totalMask, Vector<uint> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);

        /// <summary>
        ///   <para>svbool_t svpnext_b64(svbool_t pg, svbool_t op)</para>
        ///   <para>  PNEXT Ptied.D, Pg, Ptied.D</para>
        /// </summary>
        public static Vector<ulong> CreateMaskForNextActiveElement(Vector<ulong> totalMask, Vector<ulong> fromMask) => CreateMaskForNextActiveElement(totalMask, fromMask);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskByte(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskDouble(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt16(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt32(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskInt64(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSByte(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b8(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.B, pattern</para>
        /// </summary>
        public static Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskSingle(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b16(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.H, pattern</para>
        /// </summary>
        public static Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt16(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b32(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.S, pattern</para>
        /// </summary>
        public static Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt32(pattern);


        // Set predicate elements to true

        /// <summary>
        ///   <para>svbool_t svptrue_pat_b64(enum svpattern pattern)</para>
        ///   <para>  PTRUE Presult.D, pattern</para>
        /// </summary>
        public static Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => CreateTrueMaskUInt64(pattern);


        // While incrementing scalar is less than

        /// <summary>
        ///   <para>svbool_t svwhilelt_b16[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELT Presult.H, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanMask16Bit(int left, int right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b16[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELT Presult.H, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanMask16Bit(long left, long right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b16[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELO Presult.H, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanMask16Bit(uint left, uint right) => CreateWhileLessThanMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b16[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELO Presult.H, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanMask16Bit(ulong left, ulong right) => CreateWhileLessThanMask16Bit(left, right);


        // While incrementing scalar is less than

        /// <summary>
        ///   <para>svbool_t svwhilelt_b32[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELT Presult.S, Wop1, Wop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanMask32Bit(int left, int right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b32[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELT Presult.S, Xop1, Xop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanMask32Bit(long left, long right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b32[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELO Presult.S, Wop1, Wop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanMask32Bit(uint left, uint right) => CreateWhileLessThanMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b32[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELO Presult.S, Xop1, Xop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanMask32Bit(ulong left, ulong right) => CreateWhileLessThanMask32Bit(left, right);


        // While incrementing scalar is less than

        /// <summary>
        ///   <para>svbool_t svwhilelt_b64[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELT Presult.D, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanMask64Bit(int left, int right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b64[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELT Presult.D, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanMask64Bit(long left, long right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b64[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELO Presult.D, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanMask64Bit(uint left, uint right) => CreateWhileLessThanMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b64[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELO Presult.D, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanMask64Bit(ulong left, ulong right) => CreateWhileLessThanMask64Bit(left, right);


        // While incrementing scalar is less than

        /// <summary>
        ///   <para>svbool_t svwhilelt_b8[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELT Presult.B, Wop1, Wop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanMask8Bit(int left, int right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b8[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELT Presult.B, Xop1, Xop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanMask8Bit(long left, long right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b8[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELO Presult.B, Wop1, Wop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanMask8Bit(uint left, uint right) => CreateWhileLessThanMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilelt_b8[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELO Presult.B, Xop1, Xop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanMask8Bit(ulong left, ulong right) => CreateWhileLessThanMask8Bit(left, right);


        // While incrementing scalar is less than or equal to

        /// <summary>
        ///   <para>svbool_t svwhilele_b16[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELE Presult.H, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(int left, int right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b16[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELE Presult.H, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(long left, long right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b16[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELS Presult.H, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask16Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b16[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELS Presult.H, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ushort> CreateWhileLessThanOrEqualMask16Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask16Bit(left, right);


        // While incrementing scalar is less than or equal to

        /// <summary>
        ///   <para>svbool_t svwhilele_b32[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELE Presult.S, Wop1, Wop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanOrEqualMask32Bit(int left, int right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b32[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELE Presult.S, Xop1, Xop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanOrEqualMask32Bit(long left, long right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b32[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELS Presult.S, Wop1, Wop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanOrEqualMask32Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask32Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b32[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELS Presult.S, Xop1, Xop2</para>
        /// </summary>
        public static Vector<uint> CreateWhileLessThanOrEqualMask32Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask32Bit(left, right);


        // While incrementing scalar is less than or equal to

        /// <summary>
        ///   <para>svbool_t svwhilele_b64[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELE Presult.D, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(int left, int right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b64[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELE Presult.D, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(long left, long right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b64[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELS Presult.D, Wop1, Wop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask64Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b64[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELS Presult.D, Xop1, Xop2</para>
        /// </summary>
        public static Vector<ulong> CreateWhileLessThanOrEqualMask64Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask64Bit(left, right);


        // While incrementing scalar is less than or equal to

        /// <summary>
        ///   <para>svbool_t svwhilele_b8[_s32](int32_t op1, int32_t op2)</para>
        ///   <para>  WHILELE Presult.B, Wop1, Wop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanOrEqualMask8Bit(int left, int right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b8[_s64](int64_t op1, int64_t op2)</para>
        ///   <para>  WHILELE Presult.B, Xop1, Xop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanOrEqualMask8Bit(long left, long right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b8[_u32](uint32_t op1, uint32_t op2)</para>
        ///   <para>  WHILELS Presult.B, Wop1, Wop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanOrEqualMask8Bit(uint left, uint right) => CreateWhileLessThanOrEqualMask8Bit(left, right);

        /// <summary>
        ///   <para>svbool_t svwhilele_b8[_u64](uint64_t op1, uint64_t op2)</para>
        ///   <para>  WHILELS Presult.B, Xop1, Xop2</para>
        /// </summary>
        public static Vector<byte> CreateWhileLessThanOrEqualMask8Bit(ulong left, ulong right) => CreateWhileLessThanOrEqualMask8Bit(left, right);


        // Divide

        /// <summary>
        ///   <para>svfloat64_t svdiv[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svdiv[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svdiv[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FDIV Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> Divide(Vector<double> left, Vector<double> right) => Divide(left, right);

        /// <summary>
        ///   <para>svfloat32_t svdiv[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svdiv[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svdiv[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FDIV Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> Divide(Vector<float> left, Vector<float> right) => Divide(left, right);


        // Dot product

        /// <summary>
        ///   <para>svint32_t svdot[_s32](svint32_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>  SDOT Ztied1.S, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<int> DotProduct(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right) => DotProduct(addend, left, right);

        /// <summary>
        ///   <para>svint64_t svdot[_s64](svint64_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>  SDOT Ztied1.D, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<long> DotProduct(Vector<long> addend, Vector<short> left, Vector<short> right) => DotProduct(addend, left, right);

        /// <summary>
        ///   <para>svuint32_t svdot[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>  UDOT Ztied1.S, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<uint> DotProduct(Vector<uint> addend, Vector<byte> left, Vector<byte> right) => DotProduct(addend, left, right);

        /// <summary>
        ///   <para>svuint64_t svdot[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>  UDOT Ztied1.D, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<ulong> DotProduct(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right) => DotProduct(addend, left, right);


        // Dot product

        /// <summary>
        ///   <para>svint32_t svdot_lane[_s32](svint32_t op1, svint8_t op2, svint8_t op3, uint64_t imm_index)</para>
        ///   <para>  SDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]</para>
        /// </summary>
        public static Vector<int> DotProductBySelectedScalar(Vector<int> addend, Vector<sbyte> left, Vector<sbyte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        ///   <para>svint64_t svdot_lane[_s64](svint64_t op1, svint16_t op2, svint16_t op3, uint64_t imm_index)</para>
        ///   <para>  SDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]</para>
        /// </summary>
        public static Vector<long> DotProductBySelectedScalar(Vector<long> addend, Vector<short> left, Vector<short> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        ///   <para>svuint32_t svdot_lane[_u32](svuint32_t op1, svuint8_t op2, svuint8_t op3, uint64_t imm_index)</para>
        ///   <para>  UDOT Ztied1.S, Zop2.B, Zop3.B[imm_index]</para>
        /// </summary>
        public static Vector<uint> DotProductBySelectedScalar(Vector<uint> addend, Vector<byte> left, Vector<byte> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        ///   <para>svuint64_t svdot_lane[_u64](svuint64_t op1, svuint16_t op2, svuint16_t op3, uint64_t imm_index)</para>
        ///   <para>  UDOT Ztied1.D, Zop2.H, Zop3.H[imm_index]</para>
        /// </summary>
        public static Vector<ulong> DotProductBySelectedScalar(Vector<ulong> addend, Vector<ushort> left, Vector<ushort> right, [ConstantExpected] byte rightIndex) => DotProductBySelectedScalar(addend, left, right, rightIndex);


        // Broadcast a scalar value

        /// <summary>
        ///   <para>svuint8_t svdup_lane[_u8](svuint8_t data, uint8_t index)</para>
        ///   <para>  DUP Zresult.B, Zdata.B[index]</para>
        /// </summary>
        public static Vector<byte> DuplicateSelectedScalarToVector(Vector<byte> data, [ConstantExpected(Min = 0, Max = (byte)(63))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svfloat64_t svdup_lane[_f64](svfloat64_t data, uint64_t index)</para>
        ///   <para>  DUP Zresult.D, Zdata.D[index]</para>
        /// </summary>
        public static Vector<double> DuplicateSelectedScalarToVector(Vector<double> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svint16_t svdup_lane[_s16](svint16_t data, uint16_t index)</para>
        ///   <para>  DUP Zresult.H, Zdata.H[index]</para>
        /// </summary>
        public static Vector<short> DuplicateSelectedScalarToVector(Vector<short> data, [ConstantExpected(Min = 0, Max = (byte)(31))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svint32_t svdup_lane[_s32](svint32_t data, uint32_t index)</para>
        ///   <para>  DUP Zresult.S, Zdata.S[index]</para>
        /// </summary>
        public static Vector<int> DuplicateSelectedScalarToVector(Vector<int> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svint64_t svdup_lane[_s64](svint64_t data, uint64_t index)</para>
        ///   <para>  DUP Zresult.D, Zdata.D[index]</para>
        /// </summary>
        public static Vector<long> DuplicateSelectedScalarToVector(Vector<long> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svint8_t svdup_lane[_s8](svint8_t data, uint8_t index)</para>
        ///   <para>  DUP Zresult.B, Zdata.B[index]</para>
        /// </summary>
        public static Vector<sbyte> DuplicateSelectedScalarToVector(Vector<sbyte> data, [ConstantExpected(Min = 0, Max = (byte)(63))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svfloat32_t svdup_lane[_f32](svfloat32_t data, uint32_t index)</para>
        ///   <para>  DUP Zresult.S, Zdata.S[index]</para>
        /// </summary>
        public static Vector<float> DuplicateSelectedScalarToVector(Vector<float> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svuint16_t svdup_lane[_u16](svuint16_t data, uint16_t index)</para>
        ///   <para>  DUP Zresult.H, Zdata.H[index]</para>
        /// </summary>
        public static Vector<ushort> DuplicateSelectedScalarToVector(Vector<ushort> data, [ConstantExpected(Min = 0, Max = (byte)(31))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svuint32_t svdup_lane[_u32](svuint32_t data, uint32_t index)</para>
        ///   <para>  DUP Zresult.S, Zdata.S[index]</para>
        /// </summary>
        public static Vector<uint> DuplicateSelectedScalarToVector(Vector<uint> data, [ConstantExpected(Min = 0, Max = (byte)(15))] byte index) => DuplicateSelectedScalarToVector(data, index);

        /// <summary>
        ///   <para>svuint64_t svdup_lane[_u64](svuint64_t data, uint64_t index)</para>
        ///   <para>  DUP Zresult.D, Zdata.D[index]</para>
        /// </summary>
        public static Vector<ulong> DuplicateSelectedScalarToVector(Vector<ulong> data, [ConstantExpected(Min = 0, Max = (byte)(7))] byte index) => DuplicateSelectedScalarToVector(data, index);


        // Extract element after last

        /// <summary>
        ///   <para>svuint8_t svlasta[_u8](svbool_t pg, svuint8_t data)</para>
        ///   <para>  LASTA Btied, Pg, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ExtractAfterLastActiveElement(Vector<byte> mask, Vector<byte> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint8_t svlasta[_n_u8](svbool_t pg, svuint8_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static byte ExtractAfterLastActiveElementScalar(Vector<byte> mask, Vector<byte> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svfloat64_t svlasta[_f64](svbool_t pg, svfloat64_t data)</para>
        ///   <para>  LASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<double> ExtractAfterLastActiveElement(Vector<double> mask, Vector<double> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>float64_t svlasta[_n_f64](svbool_t pg, svfloat64_t data)</para>
        ///   <para>  LASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static double ExtractAfterLastActiveElementScalar(Vector<double> mask, Vector<double> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint16_t svlasta[_s16](svbool_t pg, svint16_t data)</para>
        ///   <para>  LASTA Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<short> ExtractAfterLastActiveElement(Vector<short> mask, Vector<short> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int16_t svlasta[_n_s16](svbool_t pg, svint16_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static short ExtractAfterLastActiveElementScalar(Vector<short> mask, Vector<short> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint32_t svlasta[_s32](svbool_t pg, svint32_t data)</para>
        ///   <para>  LASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<int> ExtractAfterLastActiveElement(Vector<int> mask, Vector<int> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int32_t svlasta[_n_s32](svbool_t pg, svint32_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static int ExtractAfterLastActiveElementScalar(Vector<int> mask, Vector<int> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint64_t svlasta[_s64](svbool_t pg, svint64_t data)</para>
        ///   <para>  LASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<long> ExtractAfterLastActiveElement(Vector<long> mask, Vector<long> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int64_t svlasta[_n_s64](svbool_t pg, svint64_t data)</para>
        ///   <para>  LASTA Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static long ExtractAfterLastActiveElementScalar(Vector<long> mask, Vector<long> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint8_t svlasta[_s8](svbool_t pg, svint8_t data)</para>
        ///   <para>  LASTA Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ExtractAfterLastActiveElement(Vector<sbyte> mask, Vector<sbyte> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int8_t svlasta[_n_s8](svbool_t pg, svint8_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static sbyte ExtractAfterLastActiveElementScalar(Vector<sbyte> mask, Vector<sbyte> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svfloat32_t svlasta[_f32](svbool_t pg, svfloat32_t data)</para>
        ///   <para>  LASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<float> ExtractAfterLastActiveElement(Vector<float> mask, Vector<float> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>float32_t svlasta[_n_f32](svbool_t pg, svfloat32_t data)</para>
        ///   <para>  LASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static float ExtractAfterLastActiveElementScalar(Vector<float> mask, Vector<float> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint16_t svlasta[_u16](svbool_t pg, svuint16_t data)</para>
        ///   <para>  LASTA Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ExtractAfterLastActiveElement(Vector<ushort> mask, Vector<ushort> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint16_t svlasta[_n_u16](svbool_t pg, svuint16_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static ushort ExtractAfterLastActiveElementScalar(Vector<ushort> mask, Vector<ushort> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint32_t svlasta[_u32](svbool_t pg, svuint32_t data)</para>
        ///   <para>  LASTA Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ExtractAfterLastActiveElement(Vector<uint> mask, Vector<uint> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint32_t svlasta[_n_u32](svbool_t pg, svuint32_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static uint ExtractAfterLastActiveElementScalar(Vector<uint> mask, Vector<uint> data) => ExtractAfterLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint64_t svlasta[_u64](svbool_t pg, svuint64_t data)</para>
        ///   <para>  LASTA Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ExtractAfterLastActiveElement(Vector<ulong> mask, Vector<ulong> data) => ExtractAfterLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint64_t svlasta[_n_u64](svbool_t pg, svuint64_t data)</para>
        ///   <para>  LASTA Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static ulong ExtractAfterLastActiveElementScalar(Vector<ulong> mask, Vector<ulong> data) => ExtractAfterLastActiveElementScalar(mask, data);


        // Extract last element

        /// <summary>
        ///   <para>svuint8_t svlastb[_u8](svbool_t pg, svuint8_t data)</para>
        ///   <para>  LASTB Btied, Pg, Zdata.B</para>
        /// </summary>
        public static Vector<byte> ExtractLastActiveElement(Vector<byte> mask, Vector<byte> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint8_t svlastb[_n_u8](svbool_t pg, svuint8_t data)</para>
        ///   <para>  LASTA Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static byte ExtractLastActiveElementScalar(Vector<byte> mask, Vector<byte> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svfloat64_t svlastb[_f64](svbool_t pg, svfloat64_t data)</para>
        ///   <para>  LASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<double> ExtractLastActiveElement(Vector<double> mask, Vector<double> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>float64_t svlastb[_n_f64](svbool_t pg, svfloat64_t data)</para>
        ///   <para>  LASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static double ExtractLastActiveElementScalar(Vector<double> mask, Vector<double> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint16_t svlastb[_s16](svbool_t pg, svint16_t data)</para>
        ///   <para>  LASTB Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<short> ExtractLastActiveElement(Vector<short> mask, Vector<short> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int16_t svlastb[_n_s16](svbool_t pg, svint16_t data)</para>
        ///   <para>  LASTB Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static short ExtractLastActiveElementScalar(Vector<short> mask, Vector<short> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint32_t svlastb[_s32](svbool_t pg, svint32_t data)</para>
        ///   <para>  LASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<int> ExtractLastActiveElement(Vector<int> mask, Vector<int> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int32_t svlastb[_n_s32](svbool_t pg, svint32_t data)</para>
        ///   <para>  LASTB Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static int ExtractLastActiveElementScalar(Vector<int> mask, Vector<int> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint64_t svlastb[_s64](svbool_t pg, svint64_t data)</para>
        ///   <para>  LASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<long> ExtractLastActiveElement(Vector<long> mask, Vector<long> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int64_t svlastb[_n_s64](svbool_t pg, svint64_t data)</para>
        ///   <para>  LASTB Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static long ExtractLastActiveElementScalar(Vector<long> mask, Vector<long> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svint8_t svlastb[_s8](svbool_t pg, svint8_t data)</para>
        ///   <para>  LASTB Btied, Pg, Btied, Zdata.B</para>
        /// </summary>
        public static Vector<sbyte> ExtractLastActiveElement(Vector<sbyte> mask, Vector<sbyte> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>int8_t svlastb[_n_s8](svbool_t pg, svint8_t data)</para>
        ///   <para>  LASTB Wtied, Pg, Wtied, Zdata.B</para>
        /// </summary>
        public static sbyte ExtractLastActiveElementScalar(Vector<sbyte> mask, Vector<sbyte> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svfloat32_t svlastb[_f32](svbool_t pg, svfloat32_t data)</para>
        ///   <para>  LASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<float> ExtractLastActiveElement(Vector<float> mask, Vector<float> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>float32_t svlastb[_n_f32](svbool_t pg, svfloat32_t data)</para>
        ///   <para>  LASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static float ExtractLastActiveElementScalar(Vector<float> mask, Vector<float> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint16_t svlastb[_u16](svbool_t pg, svuint16_t data)</para>
        ///   <para>  LASTB Htied, Pg, Htied, Zdata.H</para>
        /// </summary>
        public static Vector<ushort> ExtractLastActiveElement(Vector<ushort> mask, Vector<ushort> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint16_t svlastb[_n_u16](svbool_t pg, svuint16_t data)</para>
        ///   <para>  LASTB Wtied, Pg, Wtied, Zdata.H</para>
        /// </summary>
        public static ushort ExtractLastActiveElementScalar(Vector<ushort> mask, Vector<ushort> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint32_t svlastb[_u32](svbool_t pg, svuint32_t data)</para>
        ///   <para>  LASTB Stied, Pg, Stied, Zdata.S</para>
        /// </summary>
        public static Vector<uint> ExtractLastActiveElement(Vector<uint> mask, Vector<uint> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint32_t svlastb[_n_u32](svbool_t pg, svuint32_t data)</para>
        ///   <para>  LASTB Wtied, Pg, Wtied, Zdata.S</para>
        /// </summary>
        public static uint ExtractLastActiveElementScalar(Vector<uint> mask, Vector<uint> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint64_t svlastb[_u64](svbool_t pg, svuint64_t data)</para>
        ///   <para>  LASTB Dtied, Pg, Dtied, Zdata.D</para>
        /// </summary>
        public static Vector<ulong> ExtractLastActiveElement(Vector<ulong> mask, Vector<ulong> data) => ExtractLastActiveElement(mask, data);

        /// <summary>
        ///   <para>uint64_t svlastb[_n_u64](svbool_t pg, svuint64_t data)</para>
        ///   <para>  LASTB Xtied, Pg, Xtied, Zdata.D</para>
        /// </summary>
        public static ulong ExtractLastActiveElementScalar(Vector<ulong> mask, Vector<ulong> data) => ExtractLastActiveElementScalar(mask, data);

        /// <summary>
        ///   <para>svuint8_t svext[_u8](svuint8_t op1, svuint8_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3</para>
        /// </summary>
        public static Vector<byte> ExtractVector(Vector<byte> upper, Vector<byte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svfloat64_t svext[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8</para>
        /// </summary>
        public static Vector<double> ExtractVector(Vector<double> upper, Vector<double> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svint16_t svext[_s16](svint16_t op1, svint16_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2</para>
        /// </summary>
        public static Vector<short> ExtractVector(Vector<short> upper, Vector<short> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svint32_t svext[_s32](svint32_t op1, svint32_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4</para>
        /// </summary>
        public static Vector<int> ExtractVector(Vector<int> upper, Vector<int> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svint64_t svext[_s64](svint64_t op1, svint64_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8</para>
        /// </summary>
        public static Vector<long> ExtractVector(Vector<long> upper, Vector<long> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svint8_t svext[_s8](svint8_t op1, svint8_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3</para>
        /// </summary>
        public static Vector<sbyte> ExtractVector(Vector<sbyte> upper, Vector<sbyte> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svfloat32_t svext[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4</para>
        /// </summary>
        public static Vector<float> ExtractVector(Vector<float> upper, Vector<float> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svuint16_t svext[_u16](svuint16_t op1, svuint16_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 2</para>
        /// </summary>
        public static Vector<ushort> ExtractVector(Vector<ushort> upper, Vector<ushort> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svuint32_t svext[_u32](svuint32_t op1, svuint32_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 4</para>
        /// </summary>
        public static Vector<uint> ExtractVector(Vector<uint> upper, Vector<uint> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);

        /// <summary>
        ///   <para>svuint64_t svext[_u64](svuint64_t op1, svuint64_t op2, uint64_t imm3)</para>
        ///   <para>  EXT Ztied1.B, Ztied1.B, Zop2.B, #imm3 * 8</para>
        /// </summary>
        public static Vector<ulong> ExtractVector(Vector<ulong> upper, Vector<ulong> lower, [ConstantExpected] byte index) => ExtractVector(upper, lower, index);


        // Floating-point exponential accelerator

        /// <summary>
        ///   <para>svfloat64_t svexpa[_f64](svuint64_t op)</para>
        ///   <para>  FEXPA Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<double> FloatingPointExponentialAccelerator(Vector<ulong> value) => FloatingPointExponentialAccelerator(value);

        /// <summary>
        ///   <para>svfloat32_t svexpa[_f32](svuint32_t op)</para>
        ///   <para>  FEXPA Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<float> FloatingPointExponentialAccelerator(Vector<uint> value) => FloatingPointExponentialAccelerator(value);


        // Multiply-add, addend first

        /// <summary>
        ///   <para>svfloat64_t svmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>  FMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<double> FusedMultiplyAdd(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svfloat32_t svmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>  FMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<float> FusedMultiplyAdd(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAdd(addend, left, right);


        // Multiply-add, addend first

        /// <summary>
        ///   <para>svfloat64_t svmla_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)</para>
        ///   <para>  FMLA Ztied1.D, Zop2.D, Zop3.D[imm_index]</para>
        /// </summary>
        public static Vector<double> FusedMultiplyAddBySelectedScalar(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);

        /// <summary>
        ///   <para>svfloat32_t svmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)</para>
        ///   <para>  FMLA Ztied1.S, Zop2.S, Zop3.S[imm_index]</para>
        /// </summary>
        public static Vector<float> FusedMultiplyAddBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplyAddBySelectedScalar(addend, left, right, rightIndex);


        // Negated multiply-add, addend first

        /// <summary>
        ///   <para>svfloat64_t svnmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svnmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svnmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>  FNMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<double> FusedMultiplyAddNegated(Vector<double> addend, Vector<double> left, Vector<double> right) => FusedMultiplyAddNegated(addend, left, right);

        /// <summary>
        ///   <para>svfloat32_t svnmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svnmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svnmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>  FNMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<float> FusedMultiplyAddNegated(Vector<float> addend, Vector<float> left, Vector<float> right) => FusedMultiplyAddNegated(addend, left, right);


        // Multiply-subtract, minuend first

        /// <summary>
        ///   <para>svfloat64_t svmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>  FMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<double> FusedMultiplySubtract(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svfloat32_t svmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>  FMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<float> FusedMultiplySubtract(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtract(minuend, left, right);


        // Multiply-subtract, minuend first

        /// <summary>
        ///   <para>svfloat64_t svmls_lane[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_index)</para>
        ///   <para>  FMLS Ztied1.D, Zop2.D, Zop3.D[imm_index]</para>
        /// </summary>
        public static Vector<double> FusedMultiplySubtractBySelectedScalar(Vector<double> minuend, Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);

        /// <summary>
        ///   <para>svfloat32_t svmls_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index)</para>
        ///   <para>  FMLS Ztied1.S, Zop2.S, Zop3.S[imm_index]</para>
        /// </summary>
        public static Vector<float> FusedMultiplySubtractBySelectedScalar(Vector<float> minuend, Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => FusedMultiplySubtractBySelectedScalar(minuend, left, right, rightIndex);


        // Negated multiply-subtract, minuend first

        /// <summary>
        ///   <para>svfloat64_t svnmls[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svnmls[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>svfloat64_t svnmls[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)</para>
        ///   <para>  FNMLS Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<double> FusedMultiplySubtractNegated(Vector<double> minuend, Vector<double> left, Vector<double> right) => FusedMultiplySubtractNegated(minuend, left, right);

        /// <summary>
        ///   <para>svfloat32_t svnmls[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svnmls[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>svfloat32_t svnmls[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3)</para>
        ///   <para>  FNMLS Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<float> FusedMultiplySubtractNegated(Vector<float> minuend, Vector<float> left, Vector<float> right) => FusedMultiplySubtractNegated(minuend, left, right);


        // Prefetch halfwords

        /// <summary>
        ///   <para>void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFH op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static void GatherPrefetch16Bit(Vector<short> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch16Bit(Vector<short> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<short> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfh_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFH op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static void GatherPrefetch16Bit(Vector<ushort> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch16Bit(Vector<ushort> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfh_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe void GatherPrefetch16Bit(Vector<ushort> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch16Bit(mask, address, indices, prefetchType);


        // Prefetch words

        /// <summary>
        ///   <para>void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFW op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static void GatherPrefetch32Bit(Vector<int> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch32Bit(Vector<int> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<int> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfw_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFW op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch32Bit(Vector<uint> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfw_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe void GatherPrefetch32Bit(Vector<uint> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch32Bit(mask, address, indices, prefetchType);


        // Prefetch doublewords

        /// <summary>
        ///   <para>void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFD op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static unsafe void GatherPrefetch64Bit(Vector<long> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch64Bit(Vector<long> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<long> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[s32]index(svbool_t pg, const void *base, svint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.S, SXTW #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<int> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[s64]index(svbool_t pg, const void *base, svint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<long> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        // <summary>
        //   <para>void svprfd_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFD op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[u32]index(svbool_t pg, const void *base, svuint32_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.S, UXTW #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<uint> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch64Bit(Vector<ulong> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfd_gather_[u64]index(svbool_t pg, const void *base, svuint64_t indices, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe void GatherPrefetch64Bit(Vector<ulong> mask, void* address, Vector<ulong> indices, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch64Bit(mask, address, indices, prefetchType);


        // Prefetch bytes

        /// <summary>
        ///   <para>void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        // <summary>
        //   <para>void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFB op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch8Bit(Vector<byte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<byte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[s32]offset(svbool_t pg, const void *base, svint32_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<int> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[s64]offset(svbool_t pg, const void *base, svint64_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<long> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        // <summary>
        //   <para>void svprfb_gather[_u32base](svbool_t pg, svuint32_t bases, enum svprfop op)</para>
        //   <para>  PRFB op, Pg, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<uint> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[u32]offset(svbool_t pg, const void *base, svuint32_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<uint> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather[_u64base](svbool_t pg, svuint64_t bases, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void GatherPrefetch8Bit(Vector<sbyte> mask, Vector<ulong> addresses, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, addresses, prefetchType);

        /// <summary>
        ///   <para>void svprfb_gather_[u64]offset(svbool_t pg, const void *base, svuint64_t offsets, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void GatherPrefetch8Bit(Vector<sbyte> mask, void* address, Vector<ulong> offsets, [ConstantExpected] SvePrefetchType prefetchType) => GatherPrefetch8Bit(mask, address, offsets, prefetchType);


        // Unextended load

        /// <summary>
        ///   <para>svfloat64_t svld1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svfloat64_t svld1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<double> GatherVector(Vector<double> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svfloat64_t svld1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVector(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svint32_t svld1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svint32_t svld1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVector(Vector<int> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svld1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVector(Vector<int> mask, int* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVector(Vector<long> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVector(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svfloat32_t svld1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svfloat32_t svld1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<float> GatherVector(Vector<float> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svfloat32_t svld1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVector(Vector<float> mask, float* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svld1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVector(mask, address, indices);

        // <summary>
        // svuint32_t svld1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVector(Vector<uint> mask, Vector<uint> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svld1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVector(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVector(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVector(Vector<ulong> mask, Vector<ulong> addresses) => GatherVector(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVector(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVector(mask, address, indices);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svint32_t svld1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svld1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtend(Vector<int> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtend(Vector<long> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svld1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<int> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svld1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtend(Vector<uint> mask, byte* address, Vector<uint> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<long> indices) => GatherVectorByteZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtend(Vector<ulong> mask, byte* address, Vector<ulong> indices) => GatherVectorByteZeroExtend(mask, address, indices);


        //  Load 8-bit data and zero-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1ub_gather_[s32]offset_s32(svbool_t pg, const uint8_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        // <summary>
        // svint32_t svldff1ub_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svldff1ub_gather_[u32]offset_s32(svbool_t pg, const uint8_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1ub_gather_[s64]offset_s64(svbool_t pg, const uint8_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1ub_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1ub_gather_[u64]offset_s64(svbool_t pg, const uint8_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1ub_gather_[s32]offset_u32(svbool_t pg, const uint8_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<int> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        // <summary>
        // svuint32_t svldff1ub_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LDFF1B Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svldff1ub_gather_[u32]offset_u32(svbool_t pg, const uint8_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address, Vector<uint> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1ub_gather_[s64]offset_u64(svbool_t pg, const uint8_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<long> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1ub_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorByteZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1ub_gather_[u64]offset_u64(svbool_t pg, const uint8_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address, Vector<ulong> offsets) => GatherVectorByteZeroExtendFirstFaulting(mask, address, offsets);


        //  Unextended load, first-faulting

        /// <summary>
        ///   <para>svfloat64_t svldff1_gather_[s64]index[_f64](svbool_t pg, const float64_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svfloat64_t svldff1_gather[_u64base]_f64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<double> GatherVectorFirstFaulting(Vector<double> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svfloat64_t svldff1_gather_[u64]index[_f64](svbool_t pg, const float64_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorFirstFaulting(Vector<double> mask, double* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);

        // <summary>
        // svint32_t svldff1_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svldff1_gather_[s32]index[_s32](svbool_t pg, const int32_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint32_t svldff1_gather_[u32]index[_s32](svbool_t pg, const int32_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorFirstFaulting(Vector<int> mask, int* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1_gather_[s64]index[_s64](svbool_t pg, const int64_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1_gather_[u64]index[_s64](svbool_t pg, const int64_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorFirstFaulting(Vector<long> mask, long* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svfloat32_t svldff1_gather_[s32]index[_f32](svbool_t pg, const float32_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        // <summary>
        // svfloat32_t svldff1_gather[_u32base]_f32(svbool_t pg, svuint32_t bases)
        //   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<float> GatherVectorFirstFaulting(Vector<float> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svfloat32_t svldff1_gather_[u32]index[_f32](svbool_t pg, const float32_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorFirstFaulting(Vector<float> mask, float* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        // <summary>
        // svuint32_t svldff1_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LDFF1W Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svldff1_gather_[s32]index[_u32](svbool_t pg, const uint32_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svldff1_gather_[u32]index[_u32](svbool_t pg, const uint32_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1_gather_[s64]index[_u64](svbool_t pg, const uint64_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> indices) => GatherVectorFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1_gather_[u64]index[_u64](svbool_t pg, const uint64_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> indices) => GatherVectorFirstFaulting(mask, address, indices);


        //  Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svint32_t svld1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svld1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtend(Vector<int> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtend(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svld1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1SH Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svld1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtend(Vector<uint> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtend(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtend(mask, address, indices);


        //  Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1sh_gather_[s32]index_s32(svbool_t pg, const int16_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        // <summary>
        //   <para>svint32_t svldff1sh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)</para>
        //   <para>  LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svldff1sh_gather_[u32]index_s32(svbool_t pg, const int16_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1sh_gather_[s64]index_s64(svbool_t pg, const int16_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1sh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1sh_gather_[u64]index_s64(svbool_t pg, const int16_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svldff1sh_gather_[s32]index_u32(svbool_t pg, const int16_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        // <summary>
        //   <para>svuint32_t svldff1sh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)</para>
        //   <para>  LDFF1SH Zresult.S, Pg/Z, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svldff1sh_gather_[u32]index_u32(svbool_t pg, const int16_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_gather_[s64]index_u64(svbool_t pg, const int16_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt16SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_gather_[u64]index_u64(svbool_t pg, const int16_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> indices) => GatherVectorInt16SignExtendFirstFaulting(mask, address, indices);


        //  Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svint32_t svld1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svld1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtend(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtend(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtend(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtend(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtend(mask, address, offsets);


        //  Load 16-bit data and sign-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1sh_gather_[s32]offset_s32(svbool_t pg, const int16_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svldff1sh_gather_[u32]offset_s32(svbool_t pg, const int16_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<int> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1sh_gather_[s64]offset_s64(svbool_t pg, const int16_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1sh_gather_[u64]offset_s64(svbool_t pg, const int16_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1sh_gather_[s32]offset_u32(svbool_t pg, const int16_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<int> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1sh_gather_[u32]offset_u32(svbool_t pg, const int16_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<uint> mask, short* address, Vector<uint> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_gather_[s64]offset_u64(svbool_t pg, const int16_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<long> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_gather_[u64]offset_u64(svbool_t pg, const int16_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, short* address, Vector<ulong> offsets) => GatherVectorInt16WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);


        //  Load 32-bit data and sign-extend

        /// <summary>
        ///   <para>svint64_t svld1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtend(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtend(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtend(mask, address, indices);


        //  Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        ///   <para>svint64_t svldff1sw_gather_[s64]index_s64(svbool_t pg, const int32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1sw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1sw_gather_[u64]index_s64(svbool_t pg, const int32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_gather_[s64]index_u64(svbool_t pg, const int32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorInt32SignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_gather_[u64]index_u64(svbool_t pg, const int32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> indices) => GatherVectorInt32SignExtendFirstFaulting(mask, address, indices);


        //  Load 32-bit data and sign-extend

        /// <summary>
        ///   <para>svint64_t svld1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtend(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtend(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtend(mask, address, offsets);


        //  Load 32-bit data and sign-extend, first-faulting

        /// <summary>
        ///   <para>svint64_t svldff1sw_gather_[s64]offset_s64(svbool_t pg, const int32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1sw_gather_[u64]offset_s64(svbool_t pg, const int32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<long> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_gather_[s64]offset_u64(svbool_t pg, const int32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<long> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_gather_[u64]offset_u64(svbool_t pg, const int32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(Vector<ulong> mask, int* address, Vector<ulong> offsets) => GatherVectorInt32WithByteOffsetsSignExtendFirstFaulting(mask, address, offsets);


        //  Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svint32_t svld1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svld1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtend(Vector<int> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtend(Vector<long> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svld1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<int> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1SB Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svld1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtend(Vector<uint> mask, sbyte* address, Vector<uint> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<long> indices) => GatherVectorSByteSignExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtend(Vector<ulong> mask, sbyte* address, Vector<ulong> indices) => GatherVectorSByteSignExtend(mask, address, indices);


        //  Load 8-bit data and sign-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1sb_gather_[s32]offset_s32(svbool_t pg, const int8_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<int> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        // <summary>
        //   <para>svint32_t svldff1sb_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)</para>
        //   <para>  LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svldff1sb_gather_[u32]offset_s32(svbool_t pg, const int8_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1sb_gather_[s64]offset_s64(svbool_t pg, const int8_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1sb_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1sb_gather_[u64]offset_s64(svbool_t pg, const int8_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1sb_gather_[s32]offset_u32(svbool_t pg, const int8_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<int> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        // <summary>
        //   <para>svuint32_t svldff1sb_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)</para>
        //   <para>  LDFF1SB Zresult.S, Pg/Z, [Zbases.S, #0]</para>
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svldff1sb_gather_[u32]offset_u32(svbool_t pg, const int8_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address, Vector<uint> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sb_gather_[s64]offset_u64(svbool_t pg, const int8_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<long> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1sb_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorSByteSignExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1sb_gather_[u64]offset_u64(svbool_t pg, const int8_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address, Vector<ulong> offsets) => GatherVectorSByteSignExtendFirstFaulting(mask, address, offsets);


        //  Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svint32_t svld1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svld1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtend(mask, address, offsets);


        //  Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1uh_gather_[s32]offset_s32(svbool_t pg, const uint16_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svldff1uh_gather_[u32]offset_s32(svbool_t pg, const uint16_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1uh_gather_[s64]offset_s64(svbool_t pg, const uint16_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1uh_gather_[u64]offset_s64(svbool_t pg, const uint16_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1uh_gather_[s32]offset_u32(svbool_t pg, const uint16_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1uh_gather_[u32]offset_u32(svbool_t pg, const uint16_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_gather_[s64]offset_u64(svbool_t pg, const uint16_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_gather_[u64]offset_u64(svbool_t pg, const uint16_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> offsets) => GatherVectorUInt16WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);


        //  Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svint32_t svld1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        // <summary>
        // svint32_t svld1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svld1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtend(Vector<int> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtend(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svld1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        // <summary>
        // svuint32_t svld1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LD1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svld1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtend(Vector<uint> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtend(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtend(mask, address, indices);


        //  Load 16-bit data and zero-extend, first-faulting

        /// <summary>
        ///   <para>svint32_t svldff1uh_gather_[s32]index_s32(svbool_t pg, const uint16_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        // <summary>
        // svint32_t svldff1uh_gather[_u32base]_s32(svbool_t pg, svuint32_t bases)
        //   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint32_t svldff1uh_gather_[u32]index_s32(svbool_t pg, const uint16_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1uh_gather_[s64]index_s64(svbool_t pg, const uint16_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1uh_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1uh_gather_[u64]index_s64(svbool_t pg, const uint16_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint32_t svldff1uh_gather_[s32]index_u32(svbool_t pg, const uint16_t *base, svint32_t indices)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, SXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<int> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        // <summary>
        // svuint32_t svldff1uh_gather[_u32base]_u32(svbool_t pg, svuint32_t bases)
        //   LDFF1H Zresult.S, Pg/Z, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint32_t svldff1uh_gather_[u32]index_u32(svbool_t pg, const uint16_t *base, svuint32_t indices)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, Zindices.S, UXTW #1]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address, Vector<uint> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_gather_[s64]index_u64(svbool_t pg, const uint16_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<long> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_gather_[u64]index_u64(svbool_t pg, const uint16_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address, Vector<ulong> indices) => GatherVectorUInt16ZeroExtendFirstFaulting(mask, address, indices);


        //  Load 32-bit data and zero-extend

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtend(mask, address, offsets);


        //  Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[s64]offset_s64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[u64]offset_s64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[s64]offset_u64(svbool_t pg, const uint32_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[u64]offset_u64(svbool_t pg, const uint32_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> offsets) => GatherVectorUInt32WithByteOffsetsZeroExtendFirstFaulting(mask, address, offsets);


        //  Load 32-bit data and zero-extend

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        // <summary>
        // svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        //   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtend(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svld1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtend(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        // <summary>
        // svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        //   LD1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtend(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtend(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svld1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtend(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtend(mask, address, indices);


        //  Load 32-bit data and zero-extend, first-faulting

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        // <summary>
        // svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)
        //   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<int> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[s64]index_s64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather[_u64base]_s64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svint64_t svldff1uw_gather_[u64]index_s64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<int> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        // <summary>
        // svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)
        //   LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]
        // </summary>
        // Removed as per #103297
        // public static Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, Vector<uint> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[s64]index_u64(svbool_t pg, const uint32_t *base, svint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<long> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather[_u64base]_u64(svbool_t pg, svuint64_t bases)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Zbases.D, #0]</para>
        /// </summary>
        public static Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, Vector<ulong> addresses) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, addresses);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_gather_[u64]index_u64(svbool_t pg, const uint32_t *base, svuint64_t indices)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, Zindices.D, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address, Vector<ulong> indices) => GatherVectorUInt32ZeroExtendFirstFaulting(mask, address, indices);


        ///   <para> Unextended load, first-faulting</para>

        /// <summary>
        ///   <para>svfloat64_t svldff1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat64_t svldff1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsetFirstFaulting(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svldff1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svldff1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsetFirstFaulting(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svldff1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsetFirstFaulting(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat32_t svldff1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat32_t svldff1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsetFirstFaulting(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svldff1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsetFirstFaulting(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svldff1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsetFirstFaulting(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorWithByteOffsetFirstFaulting(mask, address, offsets);


        ///   <para> Unextended load</para>

        /// <summary>
        ///   <para>svfloat64_t svld1_gather_[s64]offset[_f64](svbool_t pg, const float64_t *base, svint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat64_t svld1_gather_[u64]offset[_f64](svbool_t pg, const float64_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<double> GatherVectorWithByteOffsets(Vector<double> mask, double* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svld1_gather_[s32]offset[_s32](svbool_t pg, const int32_t *base, svint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svint32_t svld1_gather_[u32]offset[_s32](svbool_t pg, const int32_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<int> GatherVectorWithByteOffsets(Vector<int> mask, int* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1_gather_[s64]offset[_s64](svbool_t pg, const int64_t *base, svint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svint64_t svld1_gather_[u64]offset[_s64](svbool_t pg, const int64_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<long> GatherVectorWithByteOffsets(Vector<long> mask, long* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat32_t svld1_gather_[s32]offset[_f32](svbool_t pg, const float32_t *base, svint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svfloat32_t svld1_gather_[u32]offset[_f32](svbool_t pg, const float32_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<float> GatherVectorWithByteOffsets(Vector<float> mask, float* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1_gather_[s32]offset[_u32](svbool_t pg, const uint32_t *base, svint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<int> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svuint32_t svld1_gather_[u32]offset[_u32](svbool_t pg, const uint32_t *base, svuint32_t offsets)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe Vector<uint> GatherVectorWithByteOffsets(Vector<uint> mask, uint* address, Vector<uint> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1_gather_[s64]offset[_u64](svbool_t pg, const uint64_t *base, svint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<long> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);

        /// <summary>
        ///   <para>svuint64_t svld1_gather_[u64]offset[_u64](svbool_t pg, const uint64_t *base, svuint64_t offsets)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe Vector<ulong> GatherVectorWithByteOffsets(Vector<ulong> mask, ulong* address, Vector<ulong> offsets) => GatherVectorWithByteOffsets(mask, address, offsets);


        // Count set predicate bits

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<byte> mask, Vector<byte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<double> mask, Vector<double> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<short> mask, Vector<short> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<int> mask, Vector<int> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<long> mask, Vector<long> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<sbyte> mask, Vector<sbyte> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b8(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.B</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<float> mask, Vector<float> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b16(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.H</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<ushort> mask, Vector<ushort> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b32(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.S</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<uint> mask, Vector<uint> from) => GetActiveElementCount(mask, from);

        /// <summary>
        ///   <para>uint64_t svcntp_b64(svbool_t pg, svbool_t op)</para>
        ///   <para>  CNTP Xresult, Pg, Pop.D</para>
        /// </summary>
        public static ulong GetActiveElementCount(Vector<ulong> mask, Vector<ulong> from) => GetActiveElementCount(mask, from);


        // Read FFR, returning predicate of successfully loaded elements

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<byte> GetFfrByte() => GetFfrByte();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<short> GetFfrInt16() => GetFfrInt16();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<int> GetFfrInt32() => GetFfrInt32();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<long> GetFfrInt64() => GetFfrInt64();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<sbyte> GetFfrSByte() => GetFfrSByte();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<ushort> GetFfrUInt16() => GetFfrUInt16();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<uint> GetFfrUInt32() => GetFfrUInt32();

        /// <summary>
        ///   <para>svbool_t svrdffr()</para>
        ///   <para>  RDFFR Presult.B</para>
        /// </summary>
        public static Vector<ulong> GetFfrUInt64() => GetFfrUInt64();


        // Insert scalar into shifted vector

        /// <summary>
        ///   <para>svuint8_t svinsr[_n_u8](svuint8_t op1, uint8_t op2)</para>
        ///   <para>  INSR Ztied1.B, Wop2</para>
        ///   <para>  INSR Ztied1.B, Bop2</para>
        /// </summary>
        public static Vector<byte> InsertIntoShiftedVector(Vector<byte> left, byte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svfloat64_t svinsr[_n_f64](svfloat64_t op1, float64_t op2)</para>
        ///   <para>  INSR Ztied1.D, Xop2</para>
        ///   <para>  INSR Ztied1.D, Dop2</para>
        /// </summary>
        public static Vector<double> InsertIntoShiftedVector(Vector<double> left, double right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svint16_t svinsr[_n_s16](svint16_t op1, int16_t op2)</para>
        ///   <para>  INSR Ztied1.H, Wop2</para>
        ///   <para>  INSR Ztied1.H, Hop2</para>
        /// </summary>
        public static Vector<short> InsertIntoShiftedVector(Vector<short> left, short right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svint32_t svinsr[_n_s32](svint32_t op1, int32_t op2)</para>
        ///   <para>  INSR Ztied1.S, Wop2</para>
        ///   <para>  INSR Ztied1.S, Sop2</para>
        /// </summary>
        public static Vector<int> InsertIntoShiftedVector(Vector<int> left, int right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svint64_t svinsr[_n_s64](svint64_t op1, int64_t op2)</para>
        ///   <para>  INSR Ztied1.D, Xop2</para>
        ///   <para>  INSR Ztied1.D, Dop2</para>
        /// </summary>
        public static Vector<long> InsertIntoShiftedVector(Vector<long> left, long right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svint8_t svinsr[_n_s8](svint8_t op1, int8_t op2)</para>
        ///   <para>  INSR Ztied1.B, Wop2</para>
        ///   <para>  INSR Ztied1.B, Bop2</para>
        /// </summary>
        public static Vector<sbyte> InsertIntoShiftedVector(Vector<sbyte> left, sbyte right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svfloat32_t svinsr[_n_f32](svfloat32_t op1, float32_t op2)</para>
        ///   <para>  INSR Ztied1.S, Wop2</para>
        ///   <para>  INSR Ztied1.S, Sop2</para>
        /// </summary>
        public static Vector<float> InsertIntoShiftedVector(Vector<float> left, float right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svuint16_t svinsr[_n_u16](svuint16_t op1, uint16_t op2)</para>
        ///   <para>  INSR Ztied1.H, Wop2</para>
        ///   <para>  INSR Ztied1.H, Hop2</para>
        /// </summary>
        public static Vector<ushort> InsertIntoShiftedVector(Vector<ushort> left, ushort right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svuint32_t svinsr[_n_u32](svuint32_t op1, uint32_t op2)</para>
        ///   <para>  INSR Ztied1.S, Wop2</para>
        ///   <para>  INSR Ztied1.S, Sop2</para>
        /// </summary>
        public static Vector<uint> InsertIntoShiftedVector(Vector<uint> left, uint right) => InsertIntoShiftedVector(left, right);

        /// <summary>
        ///   <para>svuint64_t svinsr[_n_u64](svuint64_t op1, uint64_t op2)</para>
        ///   <para>  INSR Ztied1.D, Xop2</para>
        ///   <para>  INSR Ztied1.D, Dop2</para>
        /// </summary>
        public static Vector<ulong> InsertIntoShiftedVector(Vector<ulong> left, ulong right) => InsertIntoShiftedVector(left, right);


        // Count leading sign bits

        /// <summary>
        ///   <para>svuint8_t svcls[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svcls[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svcls[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  CLS Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> LeadingSignCount(Vector<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        ///   <para>svuint16_t svcls[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svcls[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svcls[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  CLS Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> LeadingSignCount(Vector<short> value) => LeadingSignCount(value);

        /// <summary>
        ///   <para>svuint32_t svcls[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svcls[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svcls[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  CLS Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> LeadingSignCount(Vector<int> value) => LeadingSignCount(value);

        /// <summary>
        ///   <para>svuint64_t svcls[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svcls[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svcls[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  CLS Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> LeadingSignCount(Vector<long> value) => LeadingSignCount(value);


        // Count leading zero bits

        /// <summary>
        ///   <para>svuint8_t svclz[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svclz[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svclz[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  CLZ Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> LeadingZeroCount(Vector<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint8_t svclz[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svclz[_u8]_x(svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svclz[_u8]_z(svbool_t pg, svuint8_t op)</para>
        ///   <para>  CLZ Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> LeadingZeroCount(Vector<byte> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint16_t svclz[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svclz[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svclz[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  CLZ Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> LeadingZeroCount(Vector<short> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint16_t svclz[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svclz[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svclz[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  CLZ Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> LeadingZeroCount(Vector<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint32_t svclz[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svclz[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svclz[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  CLZ Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> LeadingZeroCount(Vector<int> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint32_t svclz[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svclz[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svclz[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  CLZ Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> LeadingZeroCount(Vector<uint> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint64_t svclz[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svclz[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svclz[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  CLZ Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> LeadingZeroCount(Vector<long> value) => LeadingZeroCount(value);

        /// <summary>
        ///   <para>svuint64_t svclz[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svclz[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svclz[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  CLZ Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> LeadingZeroCount(Vector<ulong> value) => LeadingZeroCount(value);


        // Unextended load

        /// <summary>
        ///   <para>svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.B, Pg/Z, [Xarray, Xindex]</para>
        ///   <para>  LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svint16_t svld1[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]</para>
        ///   <para>  LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svint32_t svld1[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svint64_t svld1[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svint8_t svld1[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1B Zresult.B, Pg/Z, [Xarray, Xindex]</para>
        ///   <para>  LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]</para>
        ///   <para>  LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]</para>
        ///   <para>  LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address) => LoadVector(mask, address);

        /// <summary>
        ///   <para>svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]</para>
        ///   <para>  LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address) => LoadVector(mask, address);


        // Load and replicate 128 bits of data

        /// <summary>
        ///   <para>svuint8_t svld1rq[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1RQB Zresult.B, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<byte> LoadVector128AndReplicateToVector(Vector<byte> mask, byte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svfloat64_t svld1rq[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LD1RQD Zresult.D, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<double> LoadVector128AndReplicateToVector(Vector<double> mask, double* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svint16_t svld1rq[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1RQH Zresult.H, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVector128AndReplicateToVector(Vector<short> mask, short* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svint32_t svld1rq[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD1RQW Zresult.S, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVector128AndReplicateToVector(Vector<int> mask, int* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svint64_t svld1rq[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LD1RQD Zresult.D, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVector128AndReplicateToVector(Vector<long> mask, long* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svint8_t svld1rq[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1RQB Zresult.B, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector128AndReplicateToVector(Vector<sbyte> mask, sbyte* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svfloat32_t svld1rq[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LD1RQW Zresult.S, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<float> LoadVector128AndReplicateToVector(Vector<float> mask, float* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svuint16_t svld1rq[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1RQH Zresult.H, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVector128AndReplicateToVector(Vector<ushort> mask, ushort* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svuint32_t svld1rq[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD1RQW Zresult.S, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVector128AndReplicateToVector(Vector<uint> mask, uint* address) => LoadVector128AndReplicateToVector(mask, address);

        /// <summary>
        ///   <para>svuint64_t svld1rq[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LD1RQD Zresult.D, Pg/Z, [Xbase, #0]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVector128AndReplicateToVector(Vector<ulong> mask, ulong* address) => LoadVector128AndReplicateToVector(mask, address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint16_t svldnf1ub_s16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteNonFaultingZeroExtendToInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt16(address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint32_t svldnf1ub_s32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteNonFaultingZeroExtendToInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt32(address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1ub_s64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteNonFaultingZeroExtendToInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToInt64(address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint16_t svldnf1ub_u16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteNonFaultingZeroExtendToUInt16(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt16(address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint32_t svldnf1ub_u32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteNonFaultingZeroExtendToUInt32(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt32(address);


        // Load 8-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1ub_u64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteNonFaultingZeroExtendToUInt64(byte* address) => LoadVectorByteNonFaultingZeroExtendToUInt64(address);


        /// <summary>
        ///   <para>svint16_t svldff1ub_s16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendFirstFaulting(Vector<short> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint32_t svldff1ub_s32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendFirstFaulting(Vector<int> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint64_t svldff1ub_s64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendFirstFaulting(Vector<long> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint16_t svldff1ub_u16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.H, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendFirstFaulting(Vector<ushort> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldff1ub_u32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.S, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendFirstFaulting(Vector<uint> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1ub_u64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.D, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendFirstFaulting(Vector<ulong> mask, byte* address) => LoadVectorByteZeroExtendFirstFaulting(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address) => LoadVectorByteZeroExtendToInt16(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address) => LoadVectorByteZeroExtendToInt32(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address) => LoadVectorByteZeroExtendToInt64(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address) => LoadVectorByteZeroExtendToUInt16(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address) => LoadVectorByteZeroExtendToUInt32(mask, address);


        // Load 8-bit data and zero-extend

        /// <summary>
        ///   <para>svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address) => LoadVectorByteZeroExtendToUInt64(mask, address);


        // Unextended load, first-faulting

        /// <summary>
        ///   <para>svuint8_t svldff1[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<byte> LoadVectorFirstFaulting(Vector<byte> mask, byte* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svfloat64_t svldff1[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<double> LoadVectorFirstFaulting(Vector<double> mask, double* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint16_t svldff1[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorFirstFaulting(Vector<short> mask, short* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint32_t svldff1[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorFirstFaulting(Vector<int> mask, int* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint64_t svldff1[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorFirstFaulting(Vector<long> mask, long* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint8_t svldff1[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1B Zresult.B, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorFirstFaulting(Vector<sbyte> mask, sbyte* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svfloat32_t svldff1[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<float> LoadVectorFirstFaulting(Vector<float> mask, float* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint16_t svldff1[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDFF1H Zresult.H, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorFirstFaulting(Vector<ushort> mask, ushort* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldff1[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDFF1W Zresult.S, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorFirstFaulting(Vector<uint> mask, uint* address) => LoadVectorFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LDFF1D Zresult.D, Pg/Z, [Xbase, XZR, LSL #3]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorFirstFaulting(Vector<ulong> mask, ulong* address) => LoadVectorFirstFaulting(mask, address);
        // Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint32_t svldnf1sh_s32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16NonFaultingSignExtendToInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToInt32(address);


        // Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1sh_s64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16NonFaultingSignExtendToInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToInt64(address);


        // Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint32_t svldnf1sh_u32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNF1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16NonFaultingSignExtendToUInt32(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt32(address);


        // Load 16-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1sh_u64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNF1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16NonFaultingSignExtendToUInt64(short* address) => LoadVectorInt16NonFaultingSignExtendToUInt64(address);


        ///   <para> Load 16-bit data and sign-extend, first-faulting</para>

        /// <summary>
        ///   <para>svint32_t svldff1sh_s32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendFirstFaulting(Vector<int> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint64_t svldff1sh_s64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendFirstFaulting(Vector<long> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldff1sh_u32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDFF1SH Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendFirstFaulting(Vector<uint> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1sh_u64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDFF1SH Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendFirstFaulting(Vector<ulong> mask, short* address) => LoadVectorInt16SignExtendFirstFaulting(mask, address);


        // Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address) => LoadVectorInt16SignExtendToInt32(mask, address);


        // Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address) => LoadVectorInt16SignExtendToInt64(mask, address);


        // Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address) => LoadVectorInt16SignExtendToUInt32(mask, address);


        // Load 16-bit data and sign-extend

        /// <summary>
        ///   <para>svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address) => LoadVectorInt16SignExtendToUInt64(mask, address);


        // Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1sw_s64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32NonFaultingSignExtendToInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToInt64(address);


        // Load 32-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1sw_u64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDNF1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32NonFaultingSignExtendToUInt64(int* address) => LoadVectorInt32NonFaultingSignExtendToUInt64(address);


        ///   <para> Load 32-bit data and sign-extend, first-faulting</para>

        /// <summary>
        ///   <para>svint64_t svldff1sw_s64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendFirstFaulting(Vector<long> mask, int* address) => LoadVectorInt32SignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1sw_u64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDFF1SW Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendFirstFaulting(Vector<ulong> mask, int* address) => LoadVectorInt32SignExtendFirstFaulting(mask, address);


        // Load 32-bit data and sign-extend

        /// <summary>
        ///   <para>svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address) => LoadVectorInt32SignExtendToInt64(mask, address);


        // Load 32-bit data and sign-extend

        /// <summary>
        ///   <para>svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address) => LoadVectorInt32SignExtendToUInt64(mask, address);


        // Unextended load, non-faulting

        /// <summary>
        ///   <para>svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(byte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(double* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(short* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(int* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(long* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(sbyte* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(float* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(ushort* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(uint* address) => LoadVectorNonFaulting(address);

        /// <summary>
        ///   <para>svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(ulong* address) => LoadVectorNonFaulting(address);


        // Unextended load, non-temporal

        /// <summary>
        ///   <para>svuint8_t svldnt1[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonTemporal(Vector<byte> mask, byte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svfloat64_t svldnt1[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonTemporal(Vector<double> mask, double* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svint16_t svldnt1[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonTemporal(Vector<short> mask, short* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svint32_t svldnt1[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonTemporal(Vector<int> mask, int* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svint64_t svldnt1[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonTemporal(Vector<long> mask, long* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svint8_t svldnt1[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNT1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonTemporal(Vector<sbyte> mask, sbyte* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svfloat32_t svldnt1[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonTemporal(Vector<float> mask, float* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svuint16_t svldnt1[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNT1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonTemporal(Vector<ushort> mask, ushort* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldnt1[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDNT1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonTemporal(Vector<uint> mask, uint* address) => LoadVectorNonTemporal(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldnt1[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LDNT1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonTemporal(Vector<ulong> mask, ulong* address) => LoadVectorNonTemporal(mask, address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint16_t svldnf1sb_s16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteNonFaultingSignExtendToInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt16(address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint32_t svldnf1sb_s32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteNonFaultingSignExtendToInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt32(address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1sb_s64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteNonFaultingSignExtendToInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToInt64(address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint16_t svldnf1sb_u16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteNonFaultingSignExtendToUInt16(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt16(address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint32_t svldnf1sb_u32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteNonFaultingSignExtendToUInt32(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt32(address);


        // Load 8-bit data and sign-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1sb_u64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDNF1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteNonFaultingSignExtendToUInt64(sbyte* address) => LoadVectorSByteNonFaultingSignExtendToUInt64(address);


        ///   <para> Load 8-bit data and sign-extend, first-faulting</para>

        /// <summary>
        ///   <para>svint16_t svldff1sb_s16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendFirstFaulting(Vector<short> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint32_t svldff1sb_s32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendFirstFaulting(Vector<int> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint64_t svldff1sb_s64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendFirstFaulting(Vector<long> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint16_t svldff1sb_u16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.H, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendFirstFaulting(Vector<ushort> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldff1sb_u32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.S, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendFirstFaulting(Vector<uint> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1sb_u64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LDFF1SB Zresult.D, Pg/Z, [Xbase, XZR]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendFirstFaulting(Vector<ulong> mask, sbyte* address) => LoadVectorSByteSignExtendFirstFaulting(mask, address);


        //  Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address) => LoadVectorSByteSignExtendToInt16(mask, address);


        // Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address) => LoadVectorSByteSignExtendToInt32(mask, address);


        // Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address) => LoadVectorSByteSignExtendToInt64(mask, address);


        // Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt16(mask, address);


        // Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt32(mask, address);


        // Load 8-bit data and sign-extend

        /// <summary>
        ///   <para>svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address) => LoadVectorSByteSignExtendToUInt64(mask, address);


        // Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint32_t svldnf1uh_s32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16NonFaultingZeroExtendToInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt32(address);


        // Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1uh_s64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16NonFaultingZeroExtendToInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToInt64(address);


        // Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint32_t svldnf1uh_u32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNF1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16NonFaultingZeroExtendToUInt32(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt32(address);


        // Load 16-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1uh_u64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDNF1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16NonFaultingZeroExtendToUInt64(ushort* address) => LoadVectorUInt16NonFaultingZeroExtendToUInt64(address);


        /// <summary>
        ///   <para>svint32_t svldff1uh_s32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<int> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svint64_t svldff1uh_s64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<long> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint32_t svldff1uh_u32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDFF1H Zresult.S, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<uint> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1uh_u64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LDFF1H Zresult.D, Pg/Z, [Xbase, XZR, LSL #1]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendFirstFaulting(Vector<ulong> mask, ushort* address) => LoadVectorUInt16ZeroExtendFirstFaulting(mask, address);


        // Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt32(mask, address);


        // Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address) => LoadVectorUInt16ZeroExtendToInt64(mask, address);


        // Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt32(mask, address);


        // Load 16-bit data and zero-extend

        /// <summary>
        ///   <para>svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address) => LoadVectorUInt16ZeroExtendToUInt64(mask, address);


        // Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svint64_t svldnf1uw_s64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32NonFaultingZeroExtendToInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToInt64(address);


        // Load 32-bit data and zero-extend, non-faulting

        /// <summary>
        ///   <para>svuint64_t svldnf1uw_u64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDNF1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32NonFaultingZeroExtendToUInt64(uint* address) => LoadVectorUInt32NonFaultingZeroExtendToUInt64(address);


        /// <summary>
        ///   <para>svint64_t svldff1uw_s64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<long> mask, uint* address) => LoadVectorUInt32ZeroExtendFirstFaulting(mask, address);

        /// <summary>
        ///   <para>svuint64_t svldff1uw_u64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LDFF1W Zresult.D, Pg/Z, [Xbase, XZR, LSL #2]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendFirstFaulting(Vector<ulong> mask, uint* address) => LoadVectorUInt32ZeroExtendFirstFaulting(mask, address);


        // Load 32-bit data and zero-extend

        /// <summary>
        ///   <para>svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address) => LoadVectorUInt32ZeroExtendToInt64(mask, address);


        // Load 32-bit data and zero-extend

        /// <summary>
        ///   <para>svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address) => LoadVectorUInt32ZeroExtendToUInt64(mask, address);


        // Load two-element tuples into two vectors

        /// <summary>
        ///   <para>svuint8x2_t svld2[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>) Load2xVectorAndUnzip(Vector<byte> mask, byte* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat64x2_t svld2[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>) Load2xVectorAndUnzip(Vector<double> mask, double* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint16x2_t svld2[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>) Load2xVectorAndUnzip(Vector<short> mask, short* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint32x2_t svld2[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>) Load2xVectorAndUnzip(Vector<int> mask, int* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint64x2_t svld2[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>) Load2xVectorAndUnzip(Vector<long> mask, long* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint8x2_t svld2[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD2B {Zresult0.B, Zresult1.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>) Load2xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat32x2_t svld2[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>) Load2xVectorAndUnzip(Vector<float> mask, float* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint16x2_t svld2[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD2H {Zresult0.H, Zresult1.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>) Load2xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint32x2_t svld2[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD2W {Zresult0.S, Zresult1.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>) Load2xVectorAndUnzip(Vector<uint> mask, uint* address) => Load2xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint64x2_t svld2[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LD2D {Zresult0.D, Zresult1.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>) Load2xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load2xVectorAndUnzip(mask, address);


        // Load three-element tuples into three vectors

        /// <summary>
        ///   <para>svuint8x3_t svld3[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>) Load3xVectorAndUnzip(Vector<byte> mask, byte* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat64x3_t svld3[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>) Load3xVectorAndUnzip(Vector<double> mask, double* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint16x3_t svld3[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>) Load3xVectorAndUnzip(Vector<short> mask, short* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint32x3_t svld3[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>) Load3xVectorAndUnzip(Vector<int> mask, int* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint64x3_t svld3[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>) Load3xVectorAndUnzip(Vector<long> mask, long* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint8x3_t svld3[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD3B {Zresult0.B - Zresult2.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) Load3xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat32x3_t svld3[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>) Load3xVectorAndUnzip(Vector<float> mask, float* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint16x3_t svld3[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD3H {Zresult0.H - Zresult2.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>) Load3xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint32x3_t svld3[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD3W {Zresult0.S - Zresult2.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>) Load3xVectorAndUnzip(Vector<uint> mask, uint* address) => Load3xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint64x3_t svld3[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LD3D {Zresult0.D - Zresult2.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>) Load3xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load3xVectorAndUnzip(mask, address);


        // Load four-element tuples into four vectors

        /// <summary>
        ///   <para>svuint8x4_t svld4[_u8](svbool_t pg, const uint8_t *base)</para>
        ///   <para>  LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<byte>, Vector<byte>, Vector<byte>, Vector<byte>) Load4xVectorAndUnzip(Vector<byte> mask, byte* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat64x4_t svld4[_f64](svbool_t pg, const float64_t *base)</para>
        ///   <para>  LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<double>, Vector<double>, Vector<double>, Vector<double>) Load4xVectorAndUnzip(Vector<double> mask, double* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint16x4_t svld4[_s16](svbool_t pg, const int16_t *base)</para>
        ///   <para>  LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<short>, Vector<short>, Vector<short>, Vector<short>) Load4xVectorAndUnzip(Vector<short> mask, short* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint32x4_t svld4[_s32](svbool_t pg, const int32_t *base)</para>
        ///   <para>  LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<int>, Vector<int>, Vector<int>, Vector<int>) Load4xVectorAndUnzip(Vector<int> mask, int* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint64x4_t svld4[_s64](svbool_t pg, const int64_t *base)</para>
        ///   <para>  LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<long>, Vector<long>, Vector<long>, Vector<long>) Load4xVectorAndUnzip(Vector<long> mask, long* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svint8x4_t svld4[_s8](svbool_t pg, const int8_t *base)</para>
        ///   <para>  LD4B {Zresult0.B - Zresult3.B}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<sbyte>, Vector<sbyte>, Vector<sbyte>, Vector<sbyte>) Load4xVectorAndUnzip(Vector<sbyte> mask, sbyte* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svfloat32x4_t svld4[_f32](svbool_t pg, const float32_t *base)</para>
        ///   <para>  LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<float>, Vector<float>, Vector<float>, Vector<float>) Load4xVectorAndUnzip(Vector<float> mask, float* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint16x4_t svld4[_u16](svbool_t pg, const uint16_t *base)</para>
        ///   <para>  LD4H {Zresult0.H - Zresult3.H}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ushort>, Vector<ushort>, Vector<ushort>, Vector<ushort>) Load4xVectorAndUnzip(Vector<ushort> mask, ushort* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint32x4_t svld4[_u32](svbool_t pg, const uint32_t *base)</para>
        ///   <para>  LD4W {Zresult0.S - Zresult3.S}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<uint>, Vector<uint>, Vector<uint>, Vector<uint>) Load4xVectorAndUnzip(Vector<uint> mask, uint* address) => Load4xVectorAndUnzip(mask, address);

        /// <summary>
        ///   <para>svuint64x4_t svld4[_u64](svbool_t pg, const uint64_t *base)</para>
        ///   <para>  LD4D {Zresult0.D - Zresult3.D}, Pg/Z, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe (Vector<ulong>, Vector<ulong>, Vector<ulong>, Vector<ulong>) Load4xVectorAndUnzip(Vector<ulong> mask, ulong* address) => Load4xVectorAndUnzip(mask, address);


        // Maximum

        /// <summary>
        ///   <para>svuint8_t svmax[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmax[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmax[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  UMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        /// </summary>
        public static Vector<byte> Max(Vector<byte> left, Vector<byte> right) => Max(left, right);

        /// <summary>
        ///   <para>svfloat64_t svmax[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmax[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmax[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  FMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> Max(Vector<double> left, Vector<double> right) => Max(left, right);

        /// <summary>
        ///   <para>svint16_t svmax[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmax[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmax[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  SMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  SMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<short> Max(Vector<short> left, Vector<short> right) => Max(left, right);

        /// <summary>
        ///   <para>svint32_t svmax[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmax[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmax[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  SMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  SMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<int> Max(Vector<int> left, Vector<int> right) => Max(left, right);

        /// <summary>
        ///   <para>svint64_t svmax[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmax[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmax[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  SMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  SMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<long> Max(Vector<long> left, Vector<long> right) => Max(left, right);

        /// <summary>
        ///   <para>svint8_t svmax[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmax[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmax[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  SMAX Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  SMAX Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        /// </summary>
        public static Vector<sbyte> Max(Vector<sbyte> left, Vector<sbyte> right) => Max(left, right);

        /// <summary>
        ///   <para>svfloat32_t svmax[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmax[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmax[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  FMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> Max(Vector<float> left, Vector<float> right) => Max(left, right);

        /// <summary>
        ///   <para>svuint16_t svmax[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmax[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmax[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UMAX Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  UMAX Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<ushort> Max(Vector<ushort> left, Vector<ushort> right) => Max(left, right);

        /// <summary>
        ///   <para>svuint32_t svmax[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmax[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmax[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UMAX Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  UMAX Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<uint> Max(Vector<uint> left, Vector<uint> right) => Max(left, right);

        /// <summary>
        ///   <para>svuint64_t svmax[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmax[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmax[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UMAX Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  UMAX Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<ulong> Max(Vector<ulong> left, Vector<ulong> right) => Max(left, right);


        // Maximum reduction to scalar

        /// <summary>
        ///   <para>uint8_t svmaxv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  UMAXV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<byte> MaxAcross(Vector<byte> value) => MaxAcross(value);

        /// <summary>
        ///   <para>float64_t svmaxv[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FMAXV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> MaxAcross(Vector<double> value) => MaxAcross(value);

        /// <summary>
        ///   <para>int16_t svmaxv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  SMAXV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<short> MaxAcross(Vector<short> value) => MaxAcross(value);

        /// <summary>
        ///   <para>int32_t svmaxv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  SMAXV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> MaxAcross(Vector<int> value) => MaxAcross(value);

        /// <summary>
        ///   <para>int64_t svmaxv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  SMAXV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> MaxAcross(Vector<long> value) => MaxAcross(value);

        /// <summary>
        ///   <para>int8_t svmaxv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  SMAXV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> MaxAcross(Vector<sbyte> value) => MaxAcross(value);

        /// <summary>
        ///   <para>float32_t svmaxv[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FMAXV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> MaxAcross(Vector<float> value) => MaxAcross(value);

        /// <summary>
        ///   <para>uint16_t svmaxv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  UMAXV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ushort> MaxAcross(Vector<ushort> value) => MaxAcross(value);

        /// <summary>
        ///   <para>uint32_t svmaxv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  UMAXV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> MaxAcross(Vector<uint> value) => MaxAcross(value);

        /// <summary>
        ///   <para>uint64_t svmaxv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  UMAXV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> MaxAcross(Vector<ulong> value) => MaxAcross(value);


        // Maximum number

        /// <summary>
        ///   <para>svfloat64_t svmaxnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmaxnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmaxnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMAXNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  FMAXNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> MaxNumber(Vector<double> left, Vector<double> right) => MaxNumber(left, right);

        /// <summary>
        ///   <para>svfloat32_t svmaxnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmaxnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmaxnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMAXNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  FMAXNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> MaxNumber(Vector<float> left, Vector<float> right) => MaxNumber(left, right);


        // Maximum number reduction to scalar

        /// <summary>
        ///   <para>float64_t svmaxnmv[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FMAXNMV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> MaxNumberAcross(Vector<double> value) => MaxNumberAcross(value);

        /// <summary>
        ///   <para>float32_t svmaxnmv[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FMAXNMV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> MaxNumberAcross(Vector<float> value) => MaxNumberAcross(value);


        // Minimum

        /// <summary>
        ///   <para>svuint8_t svmin[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmin[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmin[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  UMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        /// </summary>
        public static Vector<byte> Min(Vector<byte> left, Vector<byte> right) => Min(left, right);

        /// <summary>
        ///   <para>svfloat64_t svmin[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmin[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmin[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  FMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> Min(Vector<double> left, Vector<double> right) => Min(left, right);

        /// <summary>
        ///   <para>svint16_t svmin[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmin[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmin[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  SMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  SMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<short> Min(Vector<short> left, Vector<short> right) => Min(left, right);

        /// <summary>
        ///   <para>svint32_t svmin[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmin[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmin[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  SMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  SMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<int> Min(Vector<int> left, Vector<int> right) => Min(left, right);

        /// <summary>
        ///   <para>svint64_t svmin[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmin[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmin[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  SMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  SMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<long> Min(Vector<long> left, Vector<long> right) => Min(left, right);

        /// <summary>
        ///   <para>svint8_t svmin[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmin[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmin[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  SMIN Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  SMIN Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        /// </summary>
        public static Vector<sbyte> Min(Vector<sbyte> left, Vector<sbyte> right) => Min(left, right);

        /// <summary>
        ///   <para>svfloat32_t svmin[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmin[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmin[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  FMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> Min(Vector<float> left, Vector<float> right) => Min(left, right);

        /// <summary>
        ///   <para>svuint16_t svmin[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmin[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmin[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UMIN Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  UMIN Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<ushort> Min(Vector<ushort> left, Vector<ushort> right) => Min(left, right);

        /// <summary>
        ///   <para>svuint32_t svmin[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmin[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmin[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UMIN Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  UMIN Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<uint> Min(Vector<uint> left, Vector<uint> right) => Min(left, right);

        /// <summary>
        ///   <para>svuint64_t svmin[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmin[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmin[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UMIN Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  UMIN Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<ulong> Min(Vector<ulong> left, Vector<ulong> right) => Min(left, right);


        // Minimum reduction to scalar

        /// <summary>
        ///   <para>uint8_t svminv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  UMINV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<byte> MinAcross(Vector<byte> value) => MinAcross(value);

        /// <summary>
        ///   <para>float64_t svminv[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FMINV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> MinAcross(Vector<double> value) => MinAcross(value);

        /// <summary>
        ///   <para>int16_t svminv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  SMINV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<short> MinAcross(Vector<short> value) => MinAcross(value);

        /// <summary>
        ///   <para>int32_t svminv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  SMINV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> MinAcross(Vector<int> value) => MinAcross(value);

        /// <summary>
        ///   <para>int64_t svminv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  SMINV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> MinAcross(Vector<long> value) => MinAcross(value);

        /// <summary>
        ///   <para>int8_t svminv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  SMINV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> MinAcross(Vector<sbyte> value) => MinAcross(value);

        /// <summary>
        ///   <para>float32_t svminv[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FMINV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> MinAcross(Vector<float> value) => MinAcross(value);

        /// <summary>
        ///   <para>uint16_t svminv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  UMINV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ushort> MinAcross(Vector<ushort> value) => MinAcross(value);

        /// <summary>
        ///   <para>uint32_t svminv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  UMINV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> MinAcross(Vector<uint> value) => MinAcross(value);

        /// <summary>
        ///   <para>uint64_t svminv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  UMINV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> MinAcross(Vector<ulong> value) => MinAcross(value);


        // Minimum number

        /// <summary>
        ///   <para>svfloat64_t svminnm[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svminnm[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svminnm[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMINNM Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  FMINNM Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<double> MinNumber(Vector<double> left, Vector<double> right) => MinNumber(left, right);

        /// <summary>
        ///   <para>svfloat32_t svminnm[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svminnm[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svminnm[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMINNM Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  FMINNM Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> MinNumber(Vector<float> left, Vector<float> right) => MinNumber(left, right);


        // Minimum number reduction to scalar

        /// <summary>
        ///   <para>float64_t svminnmv[_f64](svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FMINNMV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<double> MinNumberAcross(Vector<double> value) => MinNumberAcross(value);

        /// <summary>
        ///   <para>float32_t svminnmv[_f32](svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FMINNMV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<float> MinNumberAcross(Vector<float> value) => MinNumberAcross(value);


        // Multiply

        /// <summary>
        ///   <para>svuint8_t svmul[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmul[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        ///   <para>svuint8_t svmul[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        /// </summary>
        public static Vector<byte> Multiply(Vector<byte> left, Vector<byte> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svfloat64_t svmul[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmul[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  FMUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        ///   <para>svfloat64_t svmul[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        /// </summary>
        public static Vector<double> Multiply(Vector<double> left, Vector<double> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svint16_t svmul[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmul[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svmul[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<short> Multiply(Vector<short> left, Vector<short> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svint32_t svmul[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmul[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svmul[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<int> Multiply(Vector<int> left, Vector<int> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svint64_t svmul[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmul[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svmul[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<long> Multiply(Vector<long> left, Vector<long> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svint8_t svmul[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmul[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svmul[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  MUL Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        ///   <para>  MUL Ztied2.B, Pg/M, Ztied2.B, Zop1.B</para>
        /// </summary>
        public static Vector<sbyte> Multiply(Vector<sbyte> left, Vector<sbyte> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svfloat32_t svmul[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmul[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmul[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  FMUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<float> Multiply(Vector<float> left, Vector<float> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svuint16_t svmul[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmul[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svmul[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  MUL Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        ///   <para>  MUL Ztied2.H, Pg/M, Ztied2.H, Zop1.H</para>
        /// </summary>
        public static Vector<ushort> Multiply(Vector<ushort> left, Vector<ushort> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svuint32_t svmul[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmul[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svmul[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  MUL Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        ///   <para>  MUL Ztied2.S, Pg/M, Ztied2.S, Zop1.S</para>
        /// </summary>
        public static Vector<uint> Multiply(Vector<uint> left, Vector<uint> right) => Multiply(left, right);

        /// <summary>
        ///   <para>svuint64_t svmul[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmul[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svmul[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  MUL Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        ///   <para>  MUL Ztied2.D, Pg/M, Ztied2.D, Zop1.D</para>
        /// </summary>
        public static Vector<ulong> Multiply(Vector<ulong> left, Vector<ulong> right) => Multiply(left, right);


        // Multiply-add, addend first

        /// <summary>
        ///   <para>svuint8_t svmla[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>svuint8_t svmla[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>svuint8_t svmla[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>  MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<byte> MultiplyAdd(Vector<byte> addend, Vector<byte> left, Vector<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svint16_t svmla[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>svint16_t svmla[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>svint16_t svmla[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>  MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<short> MultiplyAdd(Vector<short> addend, Vector<short> left, Vector<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svint32_t svmla[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>svint32_t svmla[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>svint32_t svmla[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>  MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<int> MultiplyAdd(Vector<int> addend, Vector<int> left, Vector<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svint64_t svmla[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>svint64_t svmla[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>svint64_t svmla[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>  MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<long> MultiplyAdd(Vector<long> addend, Vector<long> left, Vector<long> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svint8_t svmla[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>svint8_t svmla[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>svint8_t svmla[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>  MLA Ztied1.B, Pg/M, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<sbyte> MultiplyAdd(Vector<sbyte> addend, Vector<sbyte> left, Vector<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svuint16_t svmla[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>svuint16_t svmla[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>svuint16_t svmla[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>  MLA Ztied1.H, Pg/M, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<ushort> MultiplyAdd(Vector<ushort> addend, Vector<ushort> left, Vector<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svuint32_t svmla[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>svuint32_t svmla[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>svuint32_t svmla[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>  MLA Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<uint> MultiplyAdd(Vector<uint> addend, Vector<uint> left, Vector<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        ///   <para>svuint64_t svmla[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>svuint64_t svmla[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>svuint64_t svmla[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>  MLA Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<ulong> MultiplyAdd(Vector<ulong> addend, Vector<ulong> left, Vector<ulong> right) => MultiplyAdd(addend, left, right);


        // Complex multiply-add with rotate

        /// <summary>
        ///   <para>svfloat64_t svcmla[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)</para>
        ///   <para>svfloat64_t svcmla[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)</para>
        ///   <para>svfloat64_t svcmla[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2, svfloat64_t op3, uint64_t imm_rotation)</para>
        ///   <para>  FCMLA Ztied1.D, Pg/M, Zop2.D, Zop3.D, #imm_rotation</para>
        /// </summary>
        public static Vector<double> MultiplyAddRotateComplex(Vector<double> addend, Vector<double> left, Vector<double> right, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);

        /// <summary>
        ///   <para>svfloat32_t svcmla[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)</para>
        ///   <para>svfloat32_t svcmla[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)</para>
        ///   <para>svfloat32_t svcmla[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_rotation)</para>
        ///   <para>  FCMLA Ztied1.S, Pg/M, Zop2.S, Zop3.S, #imm_rotation</para>
        /// </summary>
        public static Vector<float> MultiplyAddRotateComplex(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => MultiplyAddRotateComplex(addend, left, right, rotation);


        // Complex multiply-add with rotate

        /// <summary>
        ///   <para>svfloat32_t svcmla_lane[_f32](svfloat32_t op1, svfloat32_t op2, svfloat32_t op3, uint64_t imm_index, uint64_t imm_rotation)</para>
        ///   <para>  FCMLA Ztied1.S, Zop2.S, Zop3.S[imm_index], #imm_rotation</para>
        /// </summary>
        public static Vector<float> MultiplyAddRotateComplexBySelectedScalar(Vector<float> addend, Vector<float> left, Vector<float> right, [ConstantExpected(Min = 0, Max = (byte)(1))] byte rightIndex, [ConstantExpected(Min = 0, Max = (byte)(3))] byte rotation) => MultiplyAddRotateComplexBySelectedScalar(addend, left, right, rightIndex, rotation);


        // Multiply

        /// <summary>
        ///   <para>svfloat64_t svmul_lane[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm_index)</para>
        ///   <para>  FMUL Zresult.D, Zop1.D, Zop2.D[imm_index]</para>
        /// </summary>
        public static Vector<double> MultiplyBySelectedScalar(Vector<double> left, Vector<double> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);

        /// <summary>
        ///   <para>svfloat32_t svmul_lane[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm_index)</para>
        ///   <para>  FMUL Zresult.S, Zop1.S, Zop2.S[imm_index]</para>
        /// </summary>
        public static Vector<float> MultiplyBySelectedScalar(Vector<float> left, Vector<float> right, [ConstantExpected] byte rightIndex) => MultiplyBySelectedScalar(left, right, rightIndex);


        // Multiply extended (∞×0=2)

        /// <summary>
        ///   <para>svfloat64_t svmulx[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmulx[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svmulx[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FMULX Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> MultiplyExtended(Vector<double> left, Vector<double> right) => MultiplyExtended(left, right);

        /// <summary>
        ///   <para>svfloat32_t svmulx[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmulx[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svmulx[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FMULX Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> MultiplyExtended(Vector<float> left, Vector<float> right) => MultiplyExtended(left, right);


        // Multiply-subtract, minuend first

        /// <summary>
        ///   <para>svuint8_t svmls[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>svuint8_t svmls[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>svuint8_t svmls[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2, svuint8_t op3)</para>
        ///   <para>  MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<byte> MultiplySubtract(Vector<byte> minuend, Vector<byte> left, Vector<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svint16_t svmls[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>svint16_t svmls[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>svint16_t svmls[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2, svint16_t op3)</para>
        ///   <para>  MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<short> MultiplySubtract(Vector<short> minuend, Vector<short> left, Vector<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svint32_t svmls[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>svint32_t svmls[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>svint32_t svmls[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2, svint32_t op3)</para>
        ///   <para>  MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<int> MultiplySubtract(Vector<int> minuend, Vector<int> left, Vector<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svint64_t svmls[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>svint64_t svmls[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>svint64_t svmls[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2, svint64_t op3)</para>
        ///   <para>  MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<long> MultiplySubtract(Vector<long> minuend, Vector<long> left, Vector<long> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svint8_t svmls[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>svint8_t svmls[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>svint8_t svmls[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2, svint8_t op3)</para>
        ///   <para>  MLS Ztied1.B, Pg/M, Zop2.B, Zop3.B</para>
        /// </summary>
        public static Vector<sbyte> MultiplySubtract(Vector<sbyte> minuend, Vector<sbyte> left, Vector<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svuint16_t svmls[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>svuint16_t svmls[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>svuint16_t svmls[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2, svuint16_t op3)</para>
        ///   <para>  MLS Ztied1.H, Pg/M, Zop2.H, Zop3.H</para>
        /// </summary>
        public static Vector<ushort> MultiplySubtract(Vector<ushort> minuend, Vector<ushort> left, Vector<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svuint32_t svmls[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>svuint32_t svmls[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>svuint32_t svmls[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2, svuint32_t op3)</para>
        ///   <para>  MLS Ztied1.S, Pg/M, Zop2.S, Zop3.S</para>
        /// </summary>
        public static Vector<uint> MultiplySubtract(Vector<uint> minuend, Vector<uint> left, Vector<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        ///   <para>svuint64_t svmls[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>svuint64_t svmls[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>svuint64_t svmls[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2, svuint64_t op3)</para>
        ///   <para>  MLS Ztied1.D, Pg/M, Zop2.D, Zop3.D</para>
        /// </summary>
        public static Vector<ulong> MultiplySubtract(Vector<ulong> minuend, Vector<ulong> left, Vector<ulong> right) => MultiplySubtract(minuend, left, right);


        // Negate

        /// <summary>
        ///   <para>svfloat64_t svneg[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svneg[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svneg[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FNEG Ztied.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> Negate(Vector<double> value) => Negate(value);

        /// <summary>
        ///   <para>svint16_t svneg[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svneg[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svneg[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  NEG Ztied.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> Negate(Vector<short> value) => Negate(value);

        /// <summary>
        ///   <para>svint32_t svneg[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svneg[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svneg[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  NEG Ztied.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> Negate(Vector<int> value) => Negate(value);

        /// <summary>
        ///   <para>svint64_t svneg[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svneg[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svneg[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  NEG Ztied.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> Negate(Vector<long> value) => Negate(value);

        /// <summary>
        ///   <para>svint8_t svneg[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svneg[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svneg[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  NEG Ztied.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> Negate(Vector<sbyte> value) => Negate(value);

        /// <summary>
        ///   <para>svfloat32_t svneg[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svneg[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svneg[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FNEG Ztied.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> Negate(Vector<float> value) => Negate(value);


        // Bitwise invert

        /// <summary>
        ///   <para>svuint8_t svnot[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svnot[_u8]_x(svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svnot[_u8]_z(svbool_t pg, svuint8_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> Not(Vector<byte> value) => Not(value);

        /// <summary>
        ///   <para>svint16_t svnot[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svnot[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svnot[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> Not(Vector<short> value) => Not(value);

        /// <summary>
        ///   <para>svint32_t svnot[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svnot[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svnot[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> Not(Vector<int> value) => Not(value);

        /// <summary>
        ///   <para>svint64_t svnot[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svnot[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svnot[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> Not(Vector<long> value) => Not(value);

        /// <summary>
        ///   <para>svint8_t svnot[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svnot[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svnot[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> Not(Vector<sbyte> value) => Not(value);

        /// <summary>
        ///   <para>svuint16_t svnot[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svnot[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svnot[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> Not(Vector<ushort> value) => Not(value);

        /// <summary>
        ///   <para>svuint32_t svnot[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svnot[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svnot[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> Not(Vector<uint> value) => Not(value);

        /// <summary>
        ///   <para>svuint64_t svnot[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svnot[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svnot[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>svbool_t svnot[_b]_z(svbool_t pg, svbool_t op)</para>
        ///   <para>  NOT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> Not(Vector<ulong> value) => Not(value);


        // Bitwise inclusive OR

        /// <summary>
        ///   <para>svuint8_t svorr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svorr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svorr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<byte> Or(Vector<byte> left, Vector<byte> right) => Or(left, right);

        /// <summary>
        ///   <para>svint16_t svorr[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svorr[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svorr[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<short> Or(Vector<short> left, Vector<short> right) => Or(left, right);

        /// <summary>
        ///   <para>svint32_t svorr[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svorr[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svorr[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<int> Or(Vector<int> left, Vector<int> right) => Or(left, right);

        /// <summary>
        ///   <para>svint64_t svorr[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svorr[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svorr[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> Or(Vector<long> left, Vector<long> right) => Or(left, right);

        /// <summary>
        ///   <para>svint8_t svorr[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svorr[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svorr[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> Or(Vector<sbyte> left, Vector<sbyte> right) => Or(left, right);

        /// <summary>
        ///   <para>svuint16_t svorr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svorr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svorr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> Or(Vector<ushort> left, Vector<ushort> right) => Or(left, right);

        /// <summary>
        ///   <para>svuint32_t svorr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svorr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svorr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<uint> Or(Vector<uint> left, Vector<uint> right) => Or(left, right);

        /// <summary>
        ///   <para>svuint64_t svorr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svorr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svorr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  ORR Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> Or(Vector<ulong> left, Vector<ulong> right) => Or(left, right);


        // Bitwise inclusive OR reduction to scalar

        /// <summary>
        ///   <para>uint8_t svorv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  ORV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<byte> OrAcross(Vector<byte> value) => OrAcross(value);

        /// <summary>
        ///   <para>int16_t svorv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  ORV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<short> OrAcross(Vector<short> value) => OrAcross(value);

        /// <summary>
        ///   <para>int32_t svorv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  ORV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> OrAcross(Vector<int> value) => OrAcross(value);

        /// <summary>
        ///   <para>int64_t svorv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  ORV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> OrAcross(Vector<long> value) => OrAcross(value);

        /// <summary>
        ///   <para>int8_t svorv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  ORV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> OrAcross(Vector<sbyte> value) => OrAcross(value);

        /// <summary>
        ///   <para>uint16_t svorv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  ORV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ushort> OrAcross(Vector<ushort> value) => OrAcross(value);

        /// <summary>
        ///   <para>uint32_t svorv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  ORV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> OrAcross(Vector<uint> value) => OrAcross(value);

        /// <summary>
        ///   <para>uint64_t svorv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  ORV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> OrAcross(Vector<ulong> value) => OrAcross(value);


        // Count nonzero bits

        /// <summary>
        ///   <para>svuint8_t svcnt[_s8]_m(svuint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svcnt[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svuint8_t svcnt[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  CNT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> PopCount(Vector<sbyte> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint8_t svcnt[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svcnt[_u8]_x(svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svcnt[_u8]_z(svbool_t pg, svuint8_t op)</para>
        ///   <para>  CNT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> PopCount(Vector<byte> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint16_t svcnt[_s16]_m(svuint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svcnt[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svuint16_t svcnt[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  CNT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> PopCount(Vector<short> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint16_t svcnt[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svcnt[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svcnt[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  CNT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> PopCount(Vector<ushort> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint32_t svcnt[_s32]_m(svuint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svcnt[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svuint32_t svcnt[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  CNT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> PopCount(Vector<int> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint32_t svcnt[_f32]_m(svuint32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint32_t svcnt[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svuint32_t svcnt[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  CNT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> PopCount(Vector<float> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint32_t svcnt[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svcnt[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svcnt[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  CNT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> PopCount(Vector<uint> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint64_t svcnt[_f64]_m(svuint64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint64_t svcnt[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svuint64_t svcnt[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  CNT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> PopCount(Vector<double> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint64_t svcnt[_s64]_m(svuint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svcnt[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svuint64_t svcnt[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  CNT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> PopCount(Vector<long> value) => PopCount(value);

        /// <summary>
        ///   <para>svuint64_t svcnt[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svcnt[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svcnt[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  CNT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> PopCount(Vector<ulong> value) => PopCount(value);


        // Prefetch halfwords

        /// <summary>
        ///   <para>void svprfh(svbool_t pg, const void *base, enum svprfop op)</para>
        ///   <para>  PRFH op, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void Prefetch16Bit(Vector<ushort> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => Prefetch16Bit(mask, address, prefetchType);


        // Prefetch words

        /// <summary>
        ///   <para>void svprfw(svbool_t pg, const void *base, enum svprfop op)</para>
        ///   <para>  PRFW op, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void Prefetch32Bit(Vector<uint> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => Prefetch32Bit(mask, address, prefetchType);


        // Prefetch doublewords

        /// <summary>
        ///   <para>void svprfd(svbool_t pg, const void *base, enum svprfop op)</para>
        ///   <para>  PRFD op, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void Prefetch64Bit(Vector<ulong> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => Prefetch64Bit(mask, address, prefetchType);


        // Prefetch bytes

        /// <summary>
        ///   <para>void svprfb(svbool_t pg, const void *base, enum svprfop op)</para>
        ///   <para>  PRFB op, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void Prefetch8Bit(Vector<byte> mask, void* address, [ConstantExpected] SvePrefetchType prefetchType) => Prefetch8Bit(mask, address, prefetchType);


        // Reciprocal estimate

        /// <summary>
        ///   <para>svfloat64_t svrecpe[_f64](svfloat64_t op)</para>
        ///   <para>  FRECPE Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<double> ReciprocalEstimate(Vector<double> value) => ReciprocalEstimate(value);

        /// <summary>
        ///   <para>svfloat32_t svrecpe[_f32](svfloat32_t op)</para>
        ///   <para>  FRECPE Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<float> ReciprocalEstimate(Vector<float> value) => ReciprocalEstimate(value);


        // Reciprocal exponent

        /// <summary>
        ///   <para>svfloat64_t svrecpx[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrecpx[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrecpx[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRECPX Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> ReciprocalExponent(Vector<double> value) => ReciprocalExponent(value);

        /// <summary>
        ///   <para>svfloat32_t svrecpx[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrecpx[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrecpx[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRECPX Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> ReciprocalExponent(Vector<float> value) => ReciprocalExponent(value);


        // Reciprocal square root estimate

        /// <summary>
        ///   <para>svfloat64_t svrsqrte[_f64](svfloat64_t op)</para>
        ///   <para>  FRSQRTE Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<double> ReciprocalSqrtEstimate(Vector<double> value) => ReciprocalSqrtEstimate(value);

        /// <summary>
        ///   <para>svfloat32_t svrsqrte[_f32](svfloat32_t op)</para>
        ///   <para>  FRSQRTE Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<float> ReciprocalSqrtEstimate(Vector<float> value) => ReciprocalSqrtEstimate(value);


        // Reciprocal square root step

        /// <summary>
        ///   <para>svfloat64_t svrsqrts[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FRSQRTS Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> ReciprocalSqrtStep(Vector<double> left, Vector<double> right) => ReciprocalSqrtStep(left, right);

        /// <summary>
        ///   <para>svfloat32_t svrsqrts[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FRSQRTS Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> ReciprocalSqrtStep(Vector<float> left, Vector<float> right) => ReciprocalSqrtStep(left, right);


        // Reciprocal step

        /// <summary>
        ///   <para>svfloat64_t svrecps[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FRECPS Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> ReciprocalStep(Vector<double> left, Vector<double> right) => ReciprocalStep(left, right);

        /// <summary>
        ///   <para>svfloat32_t svrecps[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FRECPS Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> ReciprocalStep(Vector<float> left, Vector<float> right) => ReciprocalStep(left, right);


        // Reverse bits

        /// <summary>
        ///   <para>svuint8_t svrbit[_u8]_m(svuint8_t inactive, svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svrbit[_u8]_x(svbool_t pg, svuint8_t op)</para>
        ///   <para>svuint8_t svrbit[_u8]_z(svbool_t pg, svuint8_t op)</para>
        ///   <para>  RBIT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<byte> ReverseBits(Vector<byte> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svint16_t svrbit[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svrbit[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svrbit[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  RBIT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> ReverseBits(Vector<short> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svint32_t svrbit[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrbit[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrbit[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  RBIT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> ReverseBits(Vector<int> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svint64_t svrbit[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrbit[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrbit[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  RBIT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> ReverseBits(Vector<long> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svint8_t svrbit[_s8]_m(svint8_t inactive, svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svrbit[_s8]_x(svbool_t pg, svint8_t op)</para>
        ///   <para>svint8_t svrbit[_s8]_z(svbool_t pg, svint8_t op)</para>
        ///   <para>  RBIT Zresult.B, Pg/M, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> ReverseBits(Vector<sbyte> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svuint16_t svrbit[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svrbit[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svrbit[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  RBIT Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> ReverseBits(Vector<ushort> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svuint32_t svrbit[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrbit[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrbit[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  RBIT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ReverseBits(Vector<uint> value) => ReverseBits(value);

        /// <summary>
        ///   <para>svuint64_t svrbit[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrbit[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrbit[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  RBIT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ReverseBits(Vector<ulong> value) => ReverseBits(value);


        // Reverse all elements

        /// <summary>
        ///   <para>svuint8_t svrev[_u8](svuint8_t op)</para>
        ///   <para>  REV Zresult.B, Zop.B</para>
        /// </summary>
        public static Vector<byte> ReverseElement(Vector<byte> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svfloat64_t svrev[_f64](svfloat64_t op)</para>
        ///   <para>  REV Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<double> ReverseElement(Vector<double> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svint16_t svrev[_s16](svint16_t op)</para>
        ///   <para>  REV Zresult.H, Zop.H</para>
        /// </summary>
        public static Vector<short> ReverseElement(Vector<short> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svint32_t svrev[_s32](svint32_t op)</para>
        ///   <para>  REV Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<int> ReverseElement(Vector<int> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svint64_t svrev[_s64](svint64_t op)</para>
        ///   <para>  REV Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<long> ReverseElement(Vector<long> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svint8_t svrev[_s8](svint8_t op)</para>
        ///   <para>  REV Zresult.B, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> ReverseElement(Vector<sbyte> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svfloat32_t svrev[_f32](svfloat32_t op)</para>
        ///   <para>  REV Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<float> ReverseElement(Vector<float> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svuint16_t svrev[_u16](svuint16_t op)</para>
        ///   <para>  REV Zresult.H, Zop.H</para>
        /// </summary>
        public static Vector<ushort> ReverseElement(Vector<ushort> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svuint32_t svrev[_u32](svuint32_t op)</para>
        ///   <para>  REV Zresult.S, Zop.S</para>
        /// </summary>
        public static Vector<uint> ReverseElement(Vector<uint> value) => ReverseElement(value);

        /// <summary>
        ///   <para>svuint64_t svrev[_u64](svuint64_t op)</para>
        ///   <para>  REV Zresult.D, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ReverseElement(Vector<ulong> value) => ReverseElement(value);


        // Reverse halfwords within elements

        /// <summary>
        ///   <para>svint32_t svrevh[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrevh[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrevh[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  REVH Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> ReverseElement16(Vector<int> value) => ReverseElement16(value);

        /// <summary>
        ///   <para>svint64_t svrevh[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevh[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevh[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  REVH Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> ReverseElement16(Vector<long> value) => ReverseElement16(value);

        /// <summary>
        ///   <para>svuint32_t svrevh[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrevh[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrevh[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  REVH Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ReverseElement16(Vector<uint> value) => ReverseElement16(value);

        /// <summary>
        ///   <para>svuint64_t svrevh[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevh[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevh[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  REVH Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ReverseElement16(Vector<ulong> value) => ReverseElement16(value);


        // Reverse words within elements

        /// <summary>
        ///   <para>svint64_t svrevw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevw[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevw[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  REVW Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> ReverseElement32(Vector<long> value) => ReverseElement32(value);

        /// <summary>
        ///   <para>svuint64_t svrevw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevw[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevw[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  REVW Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ReverseElement32(Vector<ulong> value) => ReverseElement32(value);


        // Reverse bytes within elements

        /// <summary>
        ///   <para>svint16_t svrevb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svrevb[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svrevb[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  REVB Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> ReverseElement8(Vector<short> value) => ReverseElement8(value);

        /// <summary>
        ///   <para>svint32_t svrevb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrevb[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svrevb[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  REVB Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> ReverseElement8(Vector<int> value) => ReverseElement8(value);

        /// <summary>
        ///   <para>svint64_t svrevb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevb[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svrevb[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  REVB Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> ReverseElement8(Vector<long> value) => ReverseElement8(value);

        /// <summary>
        ///   <para>svuint16_t svrevb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svrevb[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svrevb[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  REVB Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> ReverseElement8(Vector<ushort> value) => ReverseElement8(value);

        /// <summary>
        ///   <para>svuint32_t svrevb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrevb[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svrevb[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  REVB Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ReverseElement8(Vector<uint> value) => ReverseElement8(value);

        /// <summary>
        ///   <para>svuint64_t svrevb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevb[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svrevb[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  REVB Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ReverseElement8(Vector<ulong> value) => ReverseElement8(value);


        // Round to nearest, ties away from zero

        /// <summary>
        ///   <para>svfloat64_t svrinta[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrinta[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrinta[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRINTA Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> RoundAwayFromZero(Vector<double> value) => RoundAwayFromZero(value);

        /// <summary>
        ///   <para>svfloat32_t svrinta[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrinta[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrinta[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRINTA Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> RoundAwayFromZero(Vector<float> value) => RoundAwayFromZero(value);


        // Round to nearest, ties to even

        /// <summary>
        ///   <para>svfloat64_t svrintn[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintn[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintn[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRINTN Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> RoundToNearest(Vector<double> value) => RoundToNearest(value);

        /// <summary>
        ///   <para>svfloat32_t svrintn[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintn[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintn[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRINTN Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> RoundToNearest(Vector<float> value) => RoundToNearest(value);


        // Round towards -∞

        /// <summary>
        ///   <para>svfloat64_t svrintm[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintm[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintm[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRINTM Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> RoundToNegativeInfinity(Vector<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        ///   <para>svfloat32_t svrintm[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintm[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintm[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRINTM Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> RoundToNegativeInfinity(Vector<float> value) => RoundToNegativeInfinity(value);


        // Round towards +∞

        /// <summary>
        ///   <para>svfloat64_t svrintp[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintp[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintp[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRINTP Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> RoundToPositiveInfinity(Vector<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        ///   <para>svfloat32_t svrintp[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintp[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintp[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRINTP Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> RoundToPositiveInfinity(Vector<float> value) => RoundToPositiveInfinity(value);


        // Round towards zero

        /// <summary>
        ///   <para>svfloat64_t svrintz[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintz[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svrintz[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FRINTZ Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> RoundToZero(Vector<double> value) => RoundToZero(value);

        /// <summary>
        ///   <para>svfloat32_t svrintz[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintz[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svrintz[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FRINTZ Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> RoundToZero(Vector<float> value) => RoundToZero(value);


        // Saturating decrement by number of halfword elements

        /// <summary>
        ///   <para>int32_t svqdech_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECH Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingDecrementBy16BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqdech_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECH Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingDecrementBy16BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqdech_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECH Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingDecrementBy16BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqdech_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECH Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingDecrementBy16BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint16_t svqdech_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECH Ztied.H, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<short> SaturatingDecrementBy16BitElementCount(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint16_t svqdech_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECH Ztied.H, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<ushort> SaturatingDecrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy16BitElementCount(value, scale, pattern);


        // Saturating decrement by number of word elements

        /// <summary>
        ///   <para>int32_t svqdecw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECW Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingDecrementBy32BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqdecw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECW Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingDecrementBy32BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqdecw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECW Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingDecrementBy32BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqdecw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECW Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingDecrementBy32BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint32_t svqdecw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECW Ztied.S, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<int> SaturatingDecrementBy32BitElementCount(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint32_t svqdecw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECW Ztied.S, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<uint> SaturatingDecrementBy32BitElementCount(Vector<uint> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy32BitElementCount(value, scale, pattern);


        // Saturating decrement by number of doubleword elements

        /// <summary>
        ///   <para>int32_t svqdecd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECD Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingDecrementBy64BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqdecd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECD Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingDecrementBy64BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqdecd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECD Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingDecrementBy64BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqdecd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECD Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingDecrementBy64BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint64_t svqdecd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECD Ztied.D, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<long> SaturatingDecrementBy64BitElementCount(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint64_t svqdecd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECD Ztied.D, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<ulong> SaturatingDecrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy64BitElementCount(value, scale, pattern);


        // Saturating decrement by number of byte elements

        /// <summary>
        ///   <para>int32_t svqdecb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECB Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingDecrementBy8BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqdecb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQDECB Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingDecrementBy8BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqdecb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECB Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingDecrementBy8BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqdecb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQDECB Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingDecrementBy8BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingDecrementBy8BitElementCount(value, scale, pattern);


        // Saturating decrement by active element count

        /// <summary>
        ///   <para>int32_t svqdecp[_n_s32]_b8(int32_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.B, Wtied</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(int value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqdecp[_n_s64]_b8(int64_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.B</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(long value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqdecp[_n_u32]_b8(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Wtied, Pg.B</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqdecp[_n_u64]_b8(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Xtied, Pg.B</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint16_t svqdecp[_s16](svint16_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Ztied.H, Pg</para>
        /// </summary>
        public static Vector<short> SaturatingDecrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint32_t svqdecp[_s32](svint32_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Ztied.S, Pg</para>
        /// </summary>
        public static Vector<int> SaturatingDecrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint64_t svqdecp[_s64](svint64_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Ztied.D, Pg</para>
        /// </summary>
        public static Vector<long> SaturatingDecrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqdecp[_n_s32]_b16(int32_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.H, Wtied</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqdecp[_n_s64]_b16(int64_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.H</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqdecp[_n_u32]_b16(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Wtied, Pg.H</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqdecp[_n_u64]_b16(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Xtied, Pg.H</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint16_t svqdecp[_u16](svuint16_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Ztied.H, Pg</para>
        /// </summary>
        public static Vector<ushort> SaturatingDecrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqdecp[_n_s32]_b32(int32_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.S, Wtied</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(int value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqdecp[_n_s64]_b32(int64_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.S</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(long value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqdecp[_n_u32]_b32(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Wtied, Pg.S</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqdecp[_n_u64]_b32(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Xtied, Pg.S</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint32_t svqdecp[_u32](svuint32_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Ztied.S, Pg</para>
        /// </summary>
        public static Vector<uint> SaturatingDecrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqdecp[_n_s32]_b64(int32_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.D, Wtied</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqdecp[_n_s64]_b64(int64_t op, svbool_t pg)</para>
        ///   <para>  SQDECP Xtied, Pg.D</para>
        /// </summary>
        public static long SaturatingDecrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqdecp[_n_u32]_b64(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Wtied, Pg.D</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqdecp[_n_u64]_b64(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Xtied, Pg.D</para>
        /// </summary>
        public static ulong SaturatingDecrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint64_t svqdecp[_u64](svuint64_t op, svbool_t pg)</para>
        ///   <para>  UQDECP Ztied.D, Pg</para>
        /// </summary>
        public static Vector<ulong> SaturatingDecrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingDecrementByActiveElementCount(value, from);


        // Saturating increment by number of halfword elements

        /// <summary>
        ///   <para>int32_t svqinch_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCH Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingIncrementBy16BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqinch_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCH Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingIncrementBy16BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqinch_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCH Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingIncrementBy16BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqinch_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCH Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingIncrementBy16BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint16_t svqinch_pat[_s16](svint16_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCH Ztied.H, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<short> SaturatingIncrementBy16BitElementCount(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint16_t svqinch_pat[_u16](svuint16_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCH Ztied.H, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<ushort> SaturatingIncrementBy16BitElementCount(Vector<ushort> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy16BitElementCount(value, scale, pattern);


        // Saturating increment by number of word elements

        /// <summary>
        ///   <para>int32_t svqincw_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCW Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingIncrementBy32BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqincw_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCW Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingIncrementBy32BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqincw_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCW Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingIncrementBy32BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqincw_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCW Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingIncrementBy32BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint32_t svqincw_pat[_s32](svint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCW Ztied.S, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<int> SaturatingIncrementBy32BitElementCount(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint32_t svqincw_pat[_u32](svuint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCW Ztied.S, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<uint> SaturatingIncrementBy32BitElementCount(Vector<uint> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy32BitElementCount(value, scale, pattern);


        // Saturating increment by number of doubleword elements

        /// <summary>
        ///   <para>int32_t svqincd_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCD Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingIncrementBy64BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqincd_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCD Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingIncrementBy64BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqincd_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCD Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingIncrementBy64BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqincd_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCD Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingIncrementBy64BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svint64_t svqincd_pat[_s64](svint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCD Ztied.D, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<long> SaturatingIncrementBy64BitElementCount(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>svuint64_t svqincd_pat[_u64](svuint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCD Ztied.D, pattern, MUL #imm_factor</para>
        /// </summary>
        public static Vector<ulong> SaturatingIncrementBy64BitElementCount(Vector<ulong> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy64BitElementCount(value, scale, pattern);


        // Saturating increment by number of byte elements

        /// <summary>
        ///   <para>int32_t svqincb_pat[_n_s32](int32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCB Xtied, Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static int SaturatingIncrementBy8BitElementCount(int value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>int64_t svqincb_pat[_n_s64](int64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  SQINCB Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static long SaturatingIncrementBy8BitElementCount(long value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint32_t svqincb_pat[_n_u32](uint32_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCB Wtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static uint SaturatingIncrementBy8BitElementCount(uint value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);

        /// <summary>
        ///   <para>uint64_t svqincb_pat[_n_u64](uint64_t op, enum svpattern pattern, uint64_t imm_factor)</para>
        ///   <para>  UQINCB Xtied, pattern, MUL #imm_factor</para>
        /// </summary>
        public static ulong SaturatingIncrementBy8BitElementCount(ulong value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte scale, [ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) => SaturatingIncrementBy8BitElementCount(value, scale, pattern);


        // Saturating increment by active element count

        /// <summary>
        ///   <para>int32_t svqincp[_n_s32]_b8(int32_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.B, Wtied</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(int value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqincp[_n_s64]_b8(int64_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.B</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(long value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqincp[_n_u32]_b8(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Wtied, Pg.B</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(uint value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqincp[_n_u64]_b8(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Xtied, Pg.B</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<byte> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint16_t svqincp[_s16](svint16_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Ztied.H, Pg</para>
        /// </summary>
        public static Vector<short> SaturatingIncrementByActiveElementCount(Vector<short> value, Vector<short> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint32_t svqincp[_s32](svint32_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Ztied.S, Pg</para>
        /// </summary>
        public static Vector<int> SaturatingIncrementByActiveElementCount(Vector<int> value, Vector<int> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svint64_t svqincp[_s64](svint64_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Ztied.D, Pg</para>
        /// </summary>
        public static Vector<long> SaturatingIncrementByActiveElementCount(Vector<long> value, Vector<long> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqincp[_n_s32]_b16(int32_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.H, Wtied</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(int value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqincp[_n_s64]_b16(int64_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.H</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(long value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqincp[_n_u32]_b16(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Wtied, Pg.H</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(uint value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqincp[_n_u64]_b16(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Xtied, Pg.H</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint16_t svqincp[_u16](svuint16_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Ztied.H, Pg</para>
        /// </summary>
        public static Vector<ushort> SaturatingIncrementByActiveElementCount(Vector<ushort> value, Vector<ushort> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqincp[_n_s32]_b32(int32_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.S, Wtied</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(int value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqincp[_n_s64]_b32(int64_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.S</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(long value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqincp[_n_u32]_b32(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Wtied, Pg.S</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(uint value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqincp[_n_u64]_b32(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Xtied, Pg.S</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint32_t svqincp[_u32](svuint32_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Ztied.S, Pg</para>
        /// </summary>
        public static Vector<uint> SaturatingIncrementByActiveElementCount(Vector<uint> value, Vector<uint> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int32_t svqincp[_n_s32]_b64(int32_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.D, Wtied</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(int value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>int64_t svqincp[_n_s64]_b64(int64_t op, svbool_t pg)</para>
        ///   <para>  SQINCP Xtied, Pg.D</para>
        /// </summary>
        public static long SaturatingIncrementByActiveElementCount(long value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint32_t svqincp[_n_u32]_b64(uint32_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Wtied, Pg.D</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(uint value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>uint64_t svqincp[_n_u64]_b64(uint64_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Xtied, Pg.D</para>
        /// </summary>
        public static ulong SaturatingIncrementByActiveElementCount(ulong value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);

        /// <summary>
        ///   <para>svuint64_t svqincp[_u64](svuint64_t op, svbool_t pg)</para>
        ///   <para>  UQINCP Ztied.D, Pg</para>
        /// </summary>
        public static Vector<ulong> SaturatingIncrementByActiveElementCount(Vector<ulong> value, Vector<ulong> from) => SaturatingIncrementByActiveElementCount(value, from);


        // Adjust exponent

        /// <summary>
        ///   <para>svfloat64_t svscale[_f64]_m(svbool_t pg, svfloat64_t op1, svint64_t op2)</para>
        ///   <para>svfloat64_t svscale[_f64]_x(svbool_t pg, svfloat64_t op1, svint64_t op2)</para>
        ///   <para>svfloat64_t svscale[_f64]_z(svbool_t pg, svfloat64_t op1, svint64_t op2)</para>
        ///   <para>  FSCALE Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> Scale(Vector<double> left, Vector<long> right) => Scale(left, right);

        /// <summary>
        ///   <para>svfloat32_t svscale[_f32]_m(svbool_t pg, svfloat32_t op1, svint32_t op2)</para>
        ///   <para>svfloat32_t svscale[_f32]_x(svbool_t pg, svfloat32_t op1, svint32_t op2)</para>
        ///   <para>svfloat32_t svscale[_f32]_z(svbool_t pg, svfloat32_t op1, svint32_t op2)</para>
        ///   <para>  FSCALE Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> Scale(Vector<float> left, Vector<int> right) => Scale(left, right);


        // Non-truncating store

        /// <summary>
        ///   <para>void svst1_scatter_[s64]offset[_f64](svbool_t pg, float64_t *base, svint64_t offsets, svfloat64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<long> indicies, Vector<double> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter[_u64base_f64](svbool_t pg, svuint64_t bases, svfloat64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter(Vector<double> mask, Vector<ulong> addresses, Vector<double> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u64]offset[_f64](svbool_t pg, float64_t *base, svuint64_t offsets, svfloat64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<double> mask, double* address, Vector<ulong> indicies, Vector<double> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter_[s32]offset[_s32](svbool_t pg, int32_t *base, svint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<int> indicies, Vector<int> data) => Scatter(mask, address, indicies, data);

        // <summary>
        // void svst1_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        //   ST1W Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u32]offset[_s32](svbool_t pg, int32_t *base, svuint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<int> mask, int* address, Vector<uint> indicies, Vector<int> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter_[s64]offset[_s64](svbool_t pg, int64_t *base, svint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<long> indicies, Vector<long> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u64]offset[_s64](svbool_t pg, int64_t *base, svuint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<long> mask, long* address, Vector<ulong> indicies, Vector<long> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter_[s32]offset[_f32](svbool_t pg, float32_t *base, svint32_t offsets, svfloat32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<int> indicies, Vector<float> data) => Scatter(mask, address, indicies, data);

        // <summary>
        // void svst1_scatter[_u32base_f32](svbool_t pg, svuint32_t bases, svfloat32_t data)
        //   ST1W Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter(Vector<float> mask, Vector<uint> addresses, Vector<float> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u32]offset[_f32](svbool_t pg, float32_t *base, svuint32_t offsets, svfloat32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<float> mask, float* address, Vector<uint> indicies, Vector<float> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter_[s32]offset[_u32](svbool_t pg, uint32_t *base, svint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<int> indicies, Vector<uint> data) => Scatter(mask, address, indicies, data);

        // <summary>
        // void svst1_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        //   ST1W Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u32]offset[_u32](svbool_t pg, uint32_t *base, svuint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<uint> mask, uint* address, Vector<uint> indicies, Vector<uint> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter_[s64]offset[_u64](svbool_t pg, uint64_t *base, svint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<long> indicies, Vector<ulong> data) => Scatter(mask, address, indicies, data);

        /// <summary>
        ///   <para>void svst1_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1_scatter_[u64]offset[_u64](svbool_t pg, uint64_t *base, svuint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter(Vector<ulong> mask, ulong* address, Vector<ulong> indicies, Vector<ulong> data) => Scatter(mask, address, indicies, data);


        // Truncate to 16 bits and store

        // <summary>
        // void svst1h_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        //   ST1H Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter16BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1h_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter16BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter16BitNarrowing(mask, addresses, data);

        // <summary>
        // void svst1h_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        //   ST1H Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter16BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter16BitNarrowing(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1h_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter16BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter16BitNarrowing(mask, addresses, data);


        // Truncate to 16 bits and store

        /// <summary>
        ///   <para>void svst1h_scatter_[s32]offset[_s32](svbool_t pg, int16_t *base, svint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<int> offsets, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[u32]offset[_s32](svbool_t pg, int16_t *base, svuint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<int> mask, short* address, Vector<uint> offsets, Vector<int> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[s64]offset[_s64](svbool_t pg, int16_t *base, svint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<long> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[u64]offset[_s64](svbool_t pg, int16_t *base, svuint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<long> mask, short* address, Vector<ulong> offsets, Vector<long> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[s32]offset[_u32](svbool_t pg, uint16_t *base, svint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<int> offsets, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[u32]offset[_u32](svbool_t pg, uint16_t *base, svuint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<uint> mask, ushort* address, Vector<uint> offsets, Vector<uint> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[s64]offset[_u64](svbool_t pg, uint16_t *base, svint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<long> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1h_scatter_[u64]offset[_u64](svbool_t pg, uint16_t *base, svuint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter16BitWithByteOffsetsNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter16BitWithByteOffsetsNarrowing(mask, address, offsets, data);


        // Truncate to 32 bits and store

        /// <summary>
        ///   <para>void svst1w_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter32BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter32BitNarrowing(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1w_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter32BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter32BitNarrowing(mask, addresses, data);


        // Truncate to 32 bits and store

        /// <summary>
        ///   <para>void svst1w_scatter_[s64]offset[_s64](svbool_t pg, int32_t *base, svint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<long> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1w_scatter_[u64]offset[_s64](svbool_t pg, int32_t *base, svuint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<long> mask, int* address, Vector<ulong> offsets, Vector<long> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1w_scatter_[s64]offset[_u64](svbool_t pg, uint32_t *base, svint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<long> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1w_scatter_[u64]offset[_u64](svbool_t pg, uint32_t *base, svuint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter32BitWithByteOffsetsNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter32BitWithByteOffsetsNarrowing(mask, address, offsets, data);


        // Truncate to 8 bits and store

        // <summary>
        // void svst1b_scatter[_u32base_s32](svbool_t pg, svuint32_t bases, svint32_t data)
        //   ST1B Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter8BitNarrowing(Vector<int> mask, Vector<uint> addresses, Vector<int> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1b_scatter[_u64base_s64](svbool_t pg, svuint64_t bases, svint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter8BitNarrowing(Vector<long> mask, Vector<ulong> addresses, Vector<long> data) => Scatter8BitNarrowing(mask, addresses, data);

        // <summary>
        // void svst1b_scatter[_u32base_u32](svbool_t pg, svuint32_t bases, svuint32_t data)
        //   ST1B Zdata.S, Pg, [Zbases.S, #0]
        // </summary>
        // Removed as per #103297
        // public static void Scatter8BitNarrowing(Vector<uint> mask, Vector<uint> addresses, Vector<uint> data) => Scatter8BitNarrowing(mask, addresses, data);

        /// <summary>
        ///   <para>void svst1b_scatter[_u64base_u64](svbool_t pg, svuint64_t bases, svuint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Zbases.D, #0]</para>
        /// </summary>
        public static void Scatter8BitNarrowing(Vector<ulong> mask, Vector<ulong> addresses, Vector<ulong> data) => Scatter8BitNarrowing(mask, addresses, data);


        // Truncate to 8 bits and store

        /// <summary>
        ///   <para>void svst1b_scatter_[s32]offset[_s32](svbool_t pg, int8_t *base, svint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<int> offsets, Vector<int> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[u32]offset[_s32](svbool_t pg, int8_t *base, svuint32_t offsets, svint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<int> mask, sbyte* address, Vector<uint> offsets, Vector<int> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[s64]offset[_s64](svbool_t pg, int8_t *base, svint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<long> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[u64]offset[_s64](svbool_t pg, int8_t *base, svuint64_t offsets, svint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<long> mask, sbyte* address, Vector<ulong> offsets, Vector<long> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[s32]offset[_u32](svbool_t pg, uint8_t *base, svint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, SXTW]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<int> offsets, Vector<uint> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[u32]offset[_u32](svbool_t pg, uint8_t *base, svuint32_t offsets, svuint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, Zoffsets.S, UXTW]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<uint> mask, byte* address, Vector<uint> offsets, Vector<uint> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[s64]offset[_u64](svbool_t pg, uint8_t *base, svint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<long> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);

        /// <summary>
        ///   <para>void svst1b_scatter_[u64]offset[_u64](svbool_t pg, uint8_t *base, svuint64_t offsets, svuint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, Zoffsets.D]</para>
        /// </summary>
        public static unsafe void Scatter8BitWithByteOffsetsNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> offsets, Vector<ulong> data) => Scatter8BitWithByteOffsetsNarrowing(mask, address, offsets, data);


        // Write to the first-fault register

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<byte> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<short> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<int> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<long> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<sbyte> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<ushort> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<uint> value) => SetFfr(value);

        /// <summary>
        ///   <para>void svwrffr(svbool_t op)</para>
        ///   <para>  WRFFR Pop.B</para>
        /// </summary>
        public static void SetFfr(Vector<ulong> value) => SetFfr(value);


        // Logical shift left

        /// <summary>
        ///   <para>svuint8_t svlsl[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svlsl[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svlsl[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint8_t svlsl_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>svuint8_t svlsl_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D</para>
        ///   <para>  LSL Zresult.B, Zop1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> ShiftLeftLogical(Vector<byte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint16_t svlsl[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>svint16_t svlsl[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>svint16_t svlsl[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>  LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint16_t svlsl_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>svint16_t svlsl_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>svint16_t svlsl_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> ShiftLeftLogical(Vector<short> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint32_t svlsl[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>svint32_t svlsl[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>svint32_t svlsl[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>  LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> ShiftLeftLogical(Vector<int> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint32_t svlsl_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>svint32_t svlsl_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>svint32_t svlsl_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> ShiftLeftLogical(Vector<int> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint64_t svlsl[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>svint64_t svlsl[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>svint64_t svlsl[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> ShiftLeftLogical(Vector<long> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint8_t svlsl[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>svint8_t svlsl[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>svint8_t svlsl[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>  LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<byte> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svint8_t svlsl_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>svint8_t svlsl_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>svint8_t svlsl_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.B, Pg/M, Ztied1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> ShiftLeftLogical(Vector<sbyte> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint16_t svlsl[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svlsl[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svlsl[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ushort> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint16_t svlsl_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>svuint16_t svlsl_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>svuint16_t svlsl_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.H, Pg/M, Ztied1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> ShiftLeftLogical(Vector<ushort> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint32_t svlsl[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svlsl[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svlsl[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<uint> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint32_t svlsl_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>svuint32_t svlsl_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>svuint32_t svlsl_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.S, Pg/M, Ztied1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> ShiftLeftLogical(Vector<uint> left, Vector<ulong> right) => ShiftLeftLogical(left, right);

        /// <summary>
        ///   <para>svuint64_t svlsl[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svlsl[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svlsl[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  LSL Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> ShiftLeftLogical(Vector<ulong> left, Vector<ulong> right) => ShiftLeftLogical(left, right);


        // Arithmetic shift right

        /// <summary>
        ///   <para>svint16_t svasr[_s16]_m(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>svint16_t svasr[_s16]_x(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>svint16_t svasr[_s16]_z(svbool_t pg, svint16_t op1, svuint16_t op2)</para>
        ///   <para>  ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ushort> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint16_t svasr_wide[_s16]_m(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>svint16_t svasr_wide[_s16]_x(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>svint16_t svasr_wide[_s16]_z(svbool_t pg, svint16_t op1, svuint64_t op2)</para>
        ///   <para>  ASR Ztied1.H, Pg/M, Ztied1.H, Zop2.D</para>
        /// </summary>
        public static Vector<short> ShiftRightArithmetic(Vector<short> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint32_t svasr[_s32]_m(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>svint32_t svasr[_s32]_x(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>svint32_t svasr[_s32]_z(svbool_t pg, svint32_t op1, svuint32_t op2)</para>
        ///   <para>  ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<uint> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint32_t svasr_wide[_s32]_m(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>svint32_t svasr_wide[_s32]_x(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>svint32_t svasr_wide[_s32]_z(svbool_t pg, svint32_t op1, svuint64_t op2)</para>
        ///   <para>  ASR Ztied1.S, Pg/M, Ztied1.S, Zop2.D</para>
        /// </summary>
        public static Vector<int> ShiftRightArithmetic(Vector<int> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint64_t svasr[_s64]_m(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>svint64_t svasr[_s64]_x(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>svint64_t svasr[_s64]_z(svbool_t pg, svint64_t op1, svuint64_t op2)</para>
        ///   <para>  ASR Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> ShiftRightArithmetic(Vector<long> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint8_t svasr[_s8]_m(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>svint8_t svasr[_s8]_x(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>svint8_t svasr[_s8]_z(svbool_t pg, svint8_t op1, svuint8_t op2)</para>
        ///   <para>  ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<byte> right) => ShiftRightArithmetic(left, right);

        /// <summary>
        ///   <para>svint8_t svasr_wide[_s8]_m(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>svint8_t svasr_wide[_s8]_x(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>svint8_t svasr_wide[_s8]_z(svbool_t pg, svint8_t op1, svuint64_t op2)</para>
        ///   <para>  ASR Ztied1.B, Pg/M, Ztied1.B, Zop2.D</para>
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmetic(Vector<sbyte> left, Vector<ulong> right) => ShiftRightArithmetic(left, right);


        // Arithmetic shift right for divide by immediate

        /// <summary>
        ///   <para>svint16_t svasrd[_n_s16]_m(svbool_t pg, svint16_t op1, uint64_t imm2)</para>
        ///   <para>svint16_t svasrd[_n_s16]_x(svbool_t pg, svint16_t op1, uint64_t imm2)</para>
        ///   <para>svint16_t svasrd[_n_s16]_z(svbool_t pg, svint16_t op1, uint64_t imm2)</para>
        ///   <para>  ASRD Ztied1.H, Pg/M, Ztied1.H, #imm2</para>
        /// </summary>
        public static Vector<short> ShiftRightArithmeticForDivide(Vector<short> value, [ConstantExpected(Min = 1, Max = (byte)(16))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        ///   <para>svint32_t svasrd[_n_s32]_m(svbool_t pg, svint32_t op1, uint64_t imm2)</para>
        ///   <para>svint32_t svasrd[_n_s32]_x(svbool_t pg, svint32_t op1, uint64_t imm2)</para>
        ///   <para>svint32_t svasrd[_n_s32]_z(svbool_t pg, svint32_t op1, uint64_t imm2)</para>
        ///   <para>  ASRD Ztied1.S, Pg/M, Ztied1.S, #imm2</para>
        /// </summary>
        public static Vector<int> ShiftRightArithmeticForDivide(Vector<int> value, [ConstantExpected(Min = 1, Max = (byte)(32))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        ///   <para>svint64_t svasrd[_n_s64]_m(svbool_t pg, svint64_t op1, uint64_t imm2)</para>
        ///   <para>svint64_t svasrd[_n_s64]_x(svbool_t pg, svint64_t op1, uint64_t imm2)</para>
        ///   <para>svint64_t svasrd[_n_s64]_z(svbool_t pg, svint64_t op1, uint64_t imm2)</para>
        ///   <para>  ASRD Ztied1.D, Pg/M, Ztied1.D, #imm2</para>
        /// </summary>
        public static Vector<long> ShiftRightArithmeticForDivide(Vector<long> value, [ConstantExpected(Min = 1, Max = (byte)(64))] byte control) => ShiftRightArithmeticForDivide(value, control);

        /// <summary>
        ///   <para>svint8_t svasrd[_n_s8]_m(svbool_t pg, svint8_t op1, uint64_t imm2)</para>
        ///   <para>svint8_t svasrd[_n_s8]_x(svbool_t pg, svint8_t op1, uint64_t imm2)</para>
        ///   <para>svint8_t svasrd[_n_s8]_z(svbool_t pg, svint8_t op1, uint64_t imm2)</para>
        ///   <para>  ASRD Ztied1.B, Pg/M, Ztied1.B, #imm2</para>
        /// </summary>
        public static Vector<sbyte> ShiftRightArithmeticForDivide(Vector<sbyte> value, [ConstantExpected(Min = 1, Max = (byte)(8))] byte control) => ShiftRightArithmeticForDivide(value, control);


        // Logical shift right

        /// <summary>
        ///   <para>svuint8_t svlsr[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svlsr[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svlsr[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<byte> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint8_t svlsr_wide[_u8]_m(svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>svuint8_t svlsr_wide[_u8]_x(svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>svuint8_t svlsr_wide[_u8]_z(svbool_t pg, svuint8_t op1, svuint64_t op2)</para>
        ///   <para>  LSR Ztied1.B, Pg/M, Ztied1.B, Zop2.D</para>
        /// </summary>
        public static Vector<byte> ShiftRightLogical(Vector<byte> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint16_t svlsr[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svlsr[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svlsr[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ushort> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint16_t svlsr_wide[_u16]_m(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>svuint16_t svlsr_wide[_u16]_x(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>svuint16_t svlsr_wide[_u16]_z(svbool_t pg, svuint16_t op1, svuint64_t op2)</para>
        ///   <para>  LSR Ztied1.H, Pg/M, Ztied1.H, Zop2.D</para>
        /// </summary>
        public static Vector<ushort> ShiftRightLogical(Vector<ushort> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint32_t svlsr[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svlsr[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svlsr[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<uint> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint32_t svlsr_wide[_u32]_m(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>svuint32_t svlsr_wide[_u32]_x(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>svuint32_t svlsr_wide[_u32]_z(svbool_t pg, svuint32_t op1, svuint64_t op2)</para>
        ///   <para>  LSR Ztied1.S, Pg/M, Ztied1.S, Zop2.D</para>
        /// </summary>
        public static Vector<uint> ShiftRightLogical(Vector<uint> left, Vector<ulong> right) => ShiftRightLogical(left, right);

        /// <summary>
        ///   <para>svuint64_t svlsr[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svlsr[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svlsr[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  LSR Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> ShiftRightLogical(Vector<ulong> left, Vector<ulong> right) => ShiftRightLogical(left, right);


        // Sign-extend the low 16 bits

        /// <summary>
        ///   <para>svint32_t svexth[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svexth[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svexth[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  SXTH Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> SignExtend16(Vector<int> value) => SignExtend16(value);

        /// <summary>
        ///   <para>svint64_t svexth[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svexth[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svexth[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  SXTH Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> SignExtend16(Vector<long> value) => SignExtend16(value);


        // Sign-extend the low 32 bits

        /// <summary>
        ///   <para>svint64_t svextw[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svextw[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svextw[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  SXTW Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> SignExtend32(Vector<long> value) => SignExtend32(value);


        // Sign-extend the low 8 bits

        /// <summary>
        ///   <para>svint16_t svextb[_s16]_m(svint16_t inactive, svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svextb[_s16]_x(svbool_t pg, svint16_t op)</para>
        ///   <para>svint16_t svextb[_s16]_z(svbool_t pg, svint16_t op)</para>
        ///   <para>  SXTB Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<short> SignExtend8(Vector<short> value) => SignExtend8(value);

        /// <summary>
        ///   <para>svint32_t svextb[_s32]_m(svint32_t inactive, svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svextb[_s32]_x(svbool_t pg, svint32_t op)</para>
        ///   <para>svint32_t svextb[_s32]_z(svbool_t pg, svint32_t op)</para>
        ///   <para>  SXTB Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<int> SignExtend8(Vector<int> value) => SignExtend8(value);

        /// <summary>
        ///   <para>svint64_t svextb[_s64]_m(svint64_t inactive, svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svextb[_s64]_x(svbool_t pg, svint64_t op)</para>
        ///   <para>svint64_t svextb[_s64]_z(svbool_t pg, svint64_t op)</para>
        ///   <para>  SXTB Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<long> SignExtend8(Vector<long> value) => SignExtend8(value);


        // Unpack and extend low half

        /// <summary>
        ///   <para>svint16_t svunpklo[_s16](svint8_t op)</para>
        ///   <para>  SUNPKLO Zresult.H, Zop.B</para>
        /// </summary>
        public static Vector<short> SignExtendWideningLower(Vector<sbyte> value) => SignExtendWideningLower(value);

        /// <summary>
        ///   <para>svint32_t svunpklo[_s32](svint16_t op)</para>
        ///   <para>  SUNPKLO Zresult.S, Zop.H</para>
        /// </summary>
        public static Vector<int> SignExtendWideningLower(Vector<short> value) => SignExtendWideningLower(value);

        /// <summary>
        ///   <para>svint64_t svunpklo[_s64](svint32_t op)</para>
        ///   <para>  SUNPKLO Zresult.D, Zop.S</para>
        /// </summary>
        public static Vector<long> SignExtendWideningLower(Vector<int> value) => SignExtendWideningLower(value);


        // Unpack and extend high half

        /// <summary>
        ///   <para>svint16_t svunpkhi[_s16](svint8_t op)</para>
        ///   <para>  SUNPKHI Zresult.H, Zop.B</para>
        /// </summary>
        public static Vector<short> SignExtendWideningUpper(Vector<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        ///   <para>svint32_t svunpkhi[_s32](svint16_t op)</para>
        ///   <para>  SUNPKHI Zresult.S, Zop.H</para>
        /// </summary>
        public static Vector<int> SignExtendWideningUpper(Vector<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        ///   <para>svint64_t svunpkhi[_s64](svint32_t op)</para>
        ///   <para>  SUNPKHI Zresult.D, Zop.S</para>
        /// </summary>
        public static Vector<long> SignExtendWideningUpper(Vector<int> value) => SignExtendWideningUpper(value);


        // Splice two vectors under predicate control

        /// <summary>
        ///   <para>svuint8_t svsplice[_u8](svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> Splice(Vector<byte> mask, Vector<byte> left, Vector<byte> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svfloat64_t svsplice[_f64](svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> Splice(Vector<double> mask, Vector<double> left, Vector<double> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svint16_t svsplice[_s16](svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> Splice(Vector<short> mask, Vector<short> left, Vector<short> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svint32_t svsplice[_s32](svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> Splice(Vector<int> mask, Vector<int> left, Vector<int> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svint64_t svsplice[_s64](svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> Splice(Vector<long> mask, Vector<long> left, Vector<long> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svint8_t svsplice[_s8](svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  SPLICE Ztied1.B, Pg, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> Splice(Vector<sbyte> mask, Vector<sbyte> left, Vector<sbyte> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svfloat32_t svsplice[_f32](svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> Splice(Vector<float> mask, Vector<float> left, Vector<float> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svuint16_t svsplice[_u16](svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  SPLICE Ztied1.H, Pg, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> Splice(Vector<ushort> mask, Vector<ushort> left, Vector<ushort> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svuint32_t svsplice[_u32](svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  SPLICE Ztied1.S, Pg, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> Splice(Vector<uint> mask, Vector<uint> left, Vector<uint> right) => Splice(mask, left, right);

        /// <summary>
        ///   <para>svuint64_t svsplice[_u64](svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  SPLICE Ztied1.D, Pg, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> Splice(Vector<ulong> mask, Vector<ulong> left, Vector<ulong> right) => Splice(mask, left, right);


        // Square root

        /// <summary>
        ///   <para>svfloat64_t svsqrt[_f64]_m(svfloat64_t inactive, svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svsqrt[_f64]_x(svbool_t pg, svfloat64_t op)</para>
        ///   <para>svfloat64_t svsqrt[_f64]_z(svbool_t pg, svfloat64_t op)</para>
        ///   <para>  FSQRT Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<double> Sqrt(Vector<double> value) => Sqrt(value);

        /// <summary>
        ///   <para>svfloat32_t svsqrt[_f32]_m(svfloat32_t inactive, svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svsqrt[_f32]_x(svbool_t pg, svfloat32_t op)</para>
        ///   <para>svfloat32_t svsqrt[_f32]_z(svbool_t pg, svfloat32_t op)</para>
        ///   <para>  FSQRT Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<float> Sqrt(Vector<float> value) => Sqrt(value);


        // Non-truncating store

        /// <summary>
        ///   <para>void svst1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)</para>
        ///   <para>  ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, Vector<byte> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_u8](svbool_t pg, uint8_t *base, svuint8x2_t data)</para>
        ///   <para>  ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_u8](svbool_t pg, uint8_t *base, svuint8x3_t data)</para>
        ///   <para>  ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_u8](svbool_t pg, uint8_t *base, svuint8x4_t data)</para>
        ///   <para>  ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<byte> mask, byte* address, (Vector<byte> Value1, Vector<byte> Value2, Vector<byte> Value3, Vector<byte> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, Vector<double> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_f64](svbool_t pg, float64_t *base, svfloat64x2_t data)</para>
        ///   <para>  ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_f64](svbool_t pg, float64_t *base, svfloat64x3_t data)</para>
        ///   <para>  ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_f64](svbool_t pg, float64_t *base, svfloat64x4_t data)</para>
        ///   <para>  ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<double> mask, double* address, (Vector<double> Value1, Vector<double> Value2, Vector<double> Value3, Vector<double> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_s16](svbool_t pg, int16_t *base, svint16_t data)</para>
        ///   <para>  ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, Vector<short> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_s16](svbool_t pg, int16_t *base, svint16x2_t data)</para>
        ///   <para>  ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_s16](svbool_t pg, int16_t *base, svint16x3_t data)</para>
        ///   <para>  ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_s16](svbool_t pg, int16_t *base, svint16x4_t data)</para>
        ///   <para>  ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<short> mask, short* address, (Vector<short> Value1, Vector<short> Value2, Vector<short> Value3, Vector<short> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_s32](svbool_t pg, int32_t *base, svint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, Vector<int> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_s32](svbool_t pg, int32_t *base, svint32x2_t data)</para>
        ///   <para>  ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_s32](svbool_t pg, int32_t *base, svint32x3_t data)</para>
        ///   <para>  ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_s32](svbool_t pg, int32_t *base, svint32x4_t data)</para>
        ///   <para>  ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<int> mask, int* address, (Vector<int> Value1, Vector<int> Value2, Vector<int> Value3, Vector<int> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_s64](svbool_t pg, int64_t *base, svint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, Vector<long> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_s64](svbool_t pg, int64_t *base, svint64x2_t data)</para>
        ///   <para>  ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_s64](svbool_t pg, int64_t *base, svint64x3_t data)</para>
        ///   <para>  ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_s64](svbool_t pg, int64_t *base, svint64x4_t data)</para>
        ///   <para>  ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<long> mask, long* address, (Vector<long> Value1, Vector<long> Value2, Vector<long> Value3, Vector<long> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_s8](svbool_t pg, int8_t *base, svint8_t data)</para>
        ///   <para>  ST1B Zdata.B, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_s8](svbool_t pg, int8_t *base, svint8x2_t data)</para>
        ///   <para>  ST2B {Zdata0.B, Zdata1.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_s8](svbool_t pg, int8_t *base, svint8x3_t data)</para>
        ///   <para>  ST3B {Zdata0.B - Zdata2.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_s8](svbool_t pg, int8_t *base, svint8x4_t data)</para>
        ///   <para>  ST4B {Zdata0.B - Zdata3.B}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<sbyte> mask, sbyte* address, (Vector<sbyte> Value1, Vector<sbyte> Value2, Vector<sbyte> Value3, Vector<sbyte> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, Vector<float> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_f32](svbool_t pg, float32_t *base, svfloat32x2_t data)</para>
        ///   <para>  ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_f32](svbool_t pg, float32_t *base, svfloat32x3_t data)</para>
        ///   <para>  ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_f32](svbool_t pg, float32_t *base, svfloat32x4_t data)</para>
        ///   <para>  ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<float> mask, float* address, (Vector<float> Value1, Vector<float> Value2, Vector<float> Value3, Vector<float> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)</para>
        ///   <para>  ST1H Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, Vector<ushort> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_u16](svbool_t pg, uint16_t *base, svuint16x2_t data)</para>
        ///   <para>  ST2H {Zdata0.H, Zdata1.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_u16](svbool_t pg, uint16_t *base, svuint16x3_t data)</para>
        ///   <para>  ST3H {Zdata0.H - Zdata2.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_u16](svbool_t pg, uint16_t *base, svuint16x4_t data)</para>
        ///   <para>  ST4H {Zdata0.H - Zdata3.H}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ushort> mask, ushort* address, (Vector<ushort> Value1, Vector<ushort> Value2, Vector<ushort> Value3, Vector<ushort> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)</para>
        ///   <para>  ST1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, Vector<uint> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_u32](svbool_t pg, uint32_t *base, svuint32x2_t data)</para>
        ///   <para>  ST2W {Zdata0.S, Zdata1.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_u32](svbool_t pg, uint32_t *base, svuint32x3_t data)</para>
        ///   <para>  ST3W {Zdata0.S - Zdata2.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_u32](svbool_t pg, uint32_t *base, svuint32x4_t data)</para>
        ///   <para>  ST4W {Zdata0.S - Zdata3.S}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<uint> mask, uint* address, (Vector<uint> Value1, Vector<uint> Value2, Vector<uint> Value3, Vector<uint> Value4) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)</para>
        ///   <para>  ST1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, Vector<ulong> data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst2[_u64](svbool_t pg, uint64_t *base, svuint64x2_t data)</para>
        ///   <para>  ST2D {Zdata0.D, Zdata1.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst3[_u64](svbool_t pg, uint64_t *base, svuint64x3_t data)</para>
        ///   <para>  ST3D {Zdata0.D - Zdata2.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3) data) => StoreAndZip(mask, address, data);

        /// <summary>
        ///   <para>void svst4[_u64](svbool_t pg, uint64_t *base, svuint64x4_t data)</para>
        ///   <para>  ST4D {Zdata0.D - Zdata3.D}, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreAndZip(Vector<ulong> mask, ulong* address, (Vector<ulong> Value1, Vector<ulong> Value2, Vector<ulong> Value3, Vector<ulong> Value4) data) => StoreAndZip(mask, address, data);


        // Truncate to 8 bits and store

        /// <summary>
        ///   <para>void svst1b[_s16](svbool_t pg, int8_t *base, svint16_t data)</para>
        ///   <para>  ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<short> mask, sbyte* address, Vector<short> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1b[_s32](svbool_t pg, int8_t *base, svint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, sbyte* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1h[_s32](svbool_t pg, int16_t *base, svint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<int> mask, short* address, Vector<int> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1b[_s64](svbool_t pg, int8_t *base, svint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, sbyte* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1h[_s64](svbool_t pg, int16_t *base, svint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, short* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1w[_s64](svbool_t pg, int32_t *base, svint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<long> mask, int* address, Vector<long> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1b[_u16](svbool_t pg, uint8_t *base, svuint16_t data)</para>
        ///   <para>  ST1B Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ushort> mask, byte* address, Vector<ushort> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1b[_u32](svbool_t pg, uint8_t *base, svuint32_t data)</para>
        ///   <para>  ST1B Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, byte* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1h[_u32](svbool_t pg, uint16_t *base, svuint32_t data)</para>
        ///   <para>  ST1H Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<uint> mask, ushort* address, Vector<uint> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1b[_u64](svbool_t pg, uint8_t *base, svuint64_t data)</para>
        ///   <para>  ST1B Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, byte* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1h[_u64](svbool_t pg, uint16_t *base, svuint64_t data)</para>
        ///   <para>  ST1H Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, ushort* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);

        /// <summary>
        ///   <para>void svst1w[_u64](svbool_t pg, uint32_t *base, svuint64_t data)</para>
        ///   <para>  ST1W Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNarrowing(Vector<ulong> mask, uint* address, Vector<ulong> data) => StoreNarrowing(mask, address, data);


        // Non-truncating store, non-temporal

        /// <summary>
        ///   <para>void svstnt1[_u8](svbool_t pg, uint8_t *base, svuint8_t data)</para>
        ///   <para>  STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<byte> mask, byte* address, Vector<byte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_f64](svbool_t pg, float64_t *base, svfloat64_t data)</para>
        ///   <para>  STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<double> mask, double* address, Vector<double> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_s16](svbool_t pg, int16_t *base, svint16_t data)</para>
        ///   <para>  STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<short> mask, short* address, Vector<short> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_s32](svbool_t pg, int32_t *base, svint32_t data)</para>
        ///   <para>  STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<int> mask, int* address, Vector<int> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_s64](svbool_t pg, int64_t *base, svint64_t data)</para>
        ///   <para>  STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<long> mask, long* address, Vector<long> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_s8](svbool_t pg, int8_t *base, svint8_t data)</para>
        ///   <para>  STNT1B Zdata.B, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<sbyte> mask, sbyte* address, Vector<sbyte> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_f32](svbool_t pg, float32_t *base, svfloat32_t data)</para>
        ///   <para>  STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<float> mask, float* address, Vector<float> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_u16](svbool_t pg, uint16_t *base, svuint16_t data)</para>
        ///   <para>  STNT1H Zdata.H, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ushort> mask, ushort* address, Vector<ushort> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_u32](svbool_t pg, uint32_t *base, svuint32_t data)</para>
        ///   <para>  STNT1W Zdata.S, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<uint> mask, uint* address, Vector<uint> data) => StoreNonTemporal(mask, address, data);

        /// <summary>
        ///   <para>void svstnt1[_u64](svbool_t pg, uint64_t *base, svuint64_t data)</para>
        ///   <para>  STNT1D Zdata.D, Pg, [Xbase, #0, MUL VL]</para>
        /// </summary>
        public static unsafe void StoreNonTemporal(Vector<ulong> mask, ulong* address, Vector<ulong> data) => StoreNonTemporal(mask, address, data);


        // Subtract

        /// <summary>
        ///   <para>svuint8_t svsub[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svsub[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t svsub[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> Subtract(Vector<byte> left, Vector<byte> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svfloat64_t svsub[_f64]_m(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svsub[_f64]_x(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>svfloat64_t svsub[_f64]_z(svbool_t pg, svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  FSUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> Subtract(Vector<double> left, Vector<double> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svint16_t svsub[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svsub[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t svsub[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> Subtract(Vector<short> left, Vector<short> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svint32_t svsub[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svsub[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t svsub[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> Subtract(Vector<int> left, Vector<int> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svint64_t svsub[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svsub[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t svsub[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> Subtract(Vector<long> left, Vector<long> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svint8_t svsub[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svsub[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t svsub[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  SUB Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> Subtract(Vector<sbyte> left, Vector<sbyte> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svfloat32_t svsub[_f32]_m(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svsub[_f32]_x(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>svfloat32_t svsub[_f32]_z(svbool_t pg, svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  FSUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> Subtract(Vector<float> left, Vector<float> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svuint16_t svsub[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svsub[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t svsub[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  SUB Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> Subtract(Vector<ushort> left, Vector<ushort> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svuint32_t svsub[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svsub[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t svsub[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  SUB Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> Subtract(Vector<uint> left, Vector<uint> right) => Subtract(left, right);

        /// <summary>
        ///   <para>svuint64_t svsub[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svsub[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t svsub[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  SUB Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> Subtract(Vector<ulong> left, Vector<ulong> right) => Subtract(left, right);


        // Saturating subtract

        /// <summary>
        ///   <para>svuint8_t svqsub[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UQSUB Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> SubtractSaturate(Vector<byte> left, Vector<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svint16_t svqsub[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  SQSUB Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> SubtractSaturate(Vector<short> left, Vector<short> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svint32_t svqsub[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  SQSUB Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> SubtractSaturate(Vector<int> left, Vector<int> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svint64_t svqsub[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  SQSUB Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> SubtractSaturate(Vector<long> left, Vector<long> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svint8_t svqsub[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  SQSUB Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> SubtractSaturate(Vector<sbyte> left, Vector<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svuint16_t svqsub[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UQSUB Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> SubtractSaturate(Vector<ushort> left, Vector<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svuint32_t svqsub[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UQSUB Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> SubtractSaturate(Vector<uint> left, Vector<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        ///   <para>svuint64_t svqsub[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UQSUB Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> SubtractSaturate(Vector<ulong> left, Vector<ulong> right) => SubtractSaturate(left, right);


        // Test whether any active element is true

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<byte> mask, Vector<byte> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<short> mask, Vector<short> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<int> mask, Vector<int> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<long> mask, Vector<long> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<sbyte> mask, Vector<sbyte> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<ushort> mask, Vector<ushort> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<uint> mask, Vector<uint> rightMask) => TestAnyTrue(mask, rightMask);

        /// <summary>
        ///   <para>bool svptest_any(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestAnyTrue(Vector<ulong> mask, Vector<ulong> rightMask) => TestAnyTrue(mask, rightMask);


        // Test whether the first active element is true

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<byte> leftMask, Vector<byte> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<short> leftMask, Vector<short> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<int> leftMask, Vector<int> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<long> leftMask, Vector<long> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<uint> leftMask, Vector<uint> rightMask) => TestFirstTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_first(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestFirstTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) => TestFirstTrue(leftMask, rightMask);


        // Test whether the last active element is true

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<byte> leftMask, Vector<byte> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<short> leftMask, Vector<short> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<int> leftMask, Vector<int> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<long> leftMask, Vector<long> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<sbyte> leftMask, Vector<sbyte> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<ushort> leftMask, Vector<ushort> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<uint> leftMask, Vector<uint> rightMask) => TestLastTrue(leftMask, rightMask);

        /// <summary>
        ///   <para>bool svptest_last(svbool_t pg, svbool_t op)</para>
        ///   <para>  PTEST</para>
        /// </summary>
        public static bool TestLastTrue(Vector<ulong> leftMask, Vector<ulong> rightMask) => TestLastTrue(leftMask, rightMask);


        // Interleave even elements from two inputs

        /// <summary>
        ///   <para>svuint8_t svtrn1[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  TRN1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> TransposeEven(Vector<byte> left, Vector<byte> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svfloat64_t svtrn1[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  TRN1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> TransposeEven(Vector<double> left, Vector<double> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svint16_t svtrn1[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  TRN1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> TransposeEven(Vector<short> left, Vector<short> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svint32_t svtrn1[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  TRN1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> TransposeEven(Vector<int> left, Vector<int> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svint64_t svtrn1[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  TRN1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> TransposeEven(Vector<long> left, Vector<long> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svint8_t svtrn1[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  TRN1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> TransposeEven(Vector<sbyte> left, Vector<sbyte> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svfloat32_t svtrn1[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  TRN1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> TransposeEven(Vector<float> left, Vector<float> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svuint16_t svtrn1[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  TRN1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> TransposeEven(Vector<ushort> left, Vector<ushort> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svuint32_t svtrn1[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  TRN1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> TransposeEven(Vector<uint> left, Vector<uint> right) => TransposeEven(left, right);

        /// <summary>
        ///   <para>svuint64_t svtrn1[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  TRN1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> TransposeEven(Vector<ulong> left, Vector<ulong> right) => TransposeEven(left, right);


        // Interleave odd elements from two inputs

        /// <summary>
        ///   <para>svuint8_t svtrn2[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  TRN2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> TransposeOdd(Vector<byte> left, Vector<byte> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svfloat64_t svtrn2[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  TRN2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> TransposeOdd(Vector<double> left, Vector<double> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svint16_t svtrn2[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  TRN2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> TransposeOdd(Vector<short> left, Vector<short> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svint32_t svtrn2[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  TRN2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> TransposeOdd(Vector<int> left, Vector<int> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svint64_t svtrn2[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  TRN2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> TransposeOdd(Vector<long> left, Vector<long> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svint8_t svtrn2[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  TRN2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> TransposeOdd(Vector<sbyte> left, Vector<sbyte> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svfloat32_t svtrn2[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  TRN2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> TransposeOdd(Vector<float> left, Vector<float> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svuint16_t svtrn2[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  TRN2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> TransposeOdd(Vector<ushort> left, Vector<ushort> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svuint32_t svtrn2[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  TRN2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> TransposeOdd(Vector<uint> left, Vector<uint> right) => TransposeOdd(left, right);

        /// <summary>
        ///   <para>svuint64_t svtrn2[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  TRN2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> TransposeOdd(Vector<ulong> left, Vector<ulong> right) => TransposeOdd(left, right);


        // Trigonometric multiply-add coefficient

        /// <summary>
        ///   <para>svfloat64_t svtmad[_f64](svfloat64_t op1, svfloat64_t op2, uint64_t imm3)</para>
        ///   <para>  FTMAD Ztied1.D, Ztied1.D, Zop2.D, #imm3</para>
        /// </summary>
        public static Vector<double> TrigonometricMultiplyAddCoefficient(Vector<double> left, Vector<double> right, [ConstantExpected(Min = 0, Max = (byte)(7))] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);

        /// <summary>
        ///   <para>svfloat32_t svtmad[_f32](svfloat32_t op1, svfloat32_t op2, uint64_t imm3)</para>
        ///   <para>  FTMAD Ztied1.S, Ztied1.S, Zop2.S, #imm3</para>
        /// </summary>
        public static Vector<float> TrigonometricMultiplyAddCoefficient(Vector<float> left, Vector<float> right, [ConstantExpected(Min = 0, Max = (byte)(7))] byte control) => TrigonometricMultiplyAddCoefficient(left, right, control);


        // Trigonometric select coefficient

        /// <summary>
        ///   <para>svfloat64_t svtssel[_f64](svfloat64_t op1, svuint64_t op2)</para>
        ///   <para>  FTSSEL Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> TrigonometricSelectCoefficient(Vector<double> value, Vector<ulong> selector) => TrigonometricSelectCoefficient(value, selector);

        /// <summary>
        ///   <para>svfloat32_t svtssel[_f32](svfloat32_t op1, svuint32_t op2)</para>
        ///   <para>  FTSSEL Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> TrigonometricSelectCoefficient(Vector<float> value, Vector<uint> selector) => TrigonometricSelectCoefficient(value, selector);


        // Trigonometric starting value

        /// <summary>
        ///   <para>svfloat64_t svtsmul[_f64](svfloat64_t op1, svuint64_t op2)</para>
        ///   <para>  FTSMUL Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> TrigonometricStartingValue(Vector<double> value, Vector<ulong> sign) => TrigonometricStartingValue(value, sign);

        /// <summary>
        ///   <para>svfloat32_t svtsmul[_f32](svfloat32_t op1, svuint32_t op2)</para>
        ///   <para>  FTSMUL Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> TrigonometricStartingValue(Vector<float> value, Vector<uint> sign) => TrigonometricStartingValue(value, sign);


        // Concatenate even elements from two inputs

        /// <summary>
        ///   <para>svuint8_t svuzp1[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UZP1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> UnzipEven(Vector<byte> left, Vector<byte> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svfloat64_t svuzp1[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  UZP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> UnzipEven(Vector<double> left, Vector<double> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svint16_t svuzp1[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  UZP1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> UnzipEven(Vector<short> left, Vector<short> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svint32_t svuzp1[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  UZP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> UnzipEven(Vector<int> left, Vector<int> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svint64_t svuzp1[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  UZP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> UnzipEven(Vector<long> left, Vector<long> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svint8_t svuzp1[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  UZP1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> UnzipEven(Vector<sbyte> left, Vector<sbyte> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svfloat32_t svuzp1[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  UZP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> UnzipEven(Vector<float> left, Vector<float> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svuint16_t svuzp1[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UZP1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> UnzipEven(Vector<ushort> left, Vector<ushort> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svuint32_t svuzp1[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UZP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> UnzipEven(Vector<uint> left, Vector<uint> right) => UnzipEven(left, right);

        /// <summary>
        ///   <para>svuint64_t svuzp1[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UZP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> UnzipEven(Vector<ulong> left, Vector<ulong> right) => UnzipEven(left, right);


        // Concatenate odd elements from two inputs

        /// <summary>
        ///   <para>svuint8_t svuzp2[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  UZP2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> UnzipOdd(Vector<byte> left, Vector<byte> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svfloat64_t svuzp2[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  UZP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> UnzipOdd(Vector<double> left, Vector<double> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svint16_t svuzp2[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  UZP2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> UnzipOdd(Vector<short> left, Vector<short> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svint32_t svuzp2[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  UZP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> UnzipOdd(Vector<int> left, Vector<int> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svint64_t svuzp2[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  UZP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> UnzipOdd(Vector<long> left, Vector<long> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svint8_t svuzp2[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  UZP2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> UnzipOdd(Vector<sbyte> left, Vector<sbyte> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svfloat32_t svuzp2[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  UZP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> UnzipOdd(Vector<float> left, Vector<float> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svuint16_t svuzp2[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  UZP2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> UnzipOdd(Vector<ushort> left, Vector<ushort> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svuint32_t svuzp2[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  UZP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> UnzipOdd(Vector<uint> left, Vector<uint> right) => UnzipOdd(left, right);

        /// <summary>
        ///   <para>svuint64_t svuzp2[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  UZP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> UnzipOdd(Vector<ulong> left, Vector<ulong> right) => UnzipOdd(left, right);


        // Table lookup in single-vector table

        /// <summary>
        ///   <para>svuint8_t svtbl[_u8](svuint8_t data, svuint8_t indices)</para>
        ///   <para>  TBL Zresult.B, {Zdata.B}, Zindices.B</para>
        /// </summary>
        public static Vector<byte> VectorTableLookup(Vector<byte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svfloat64_t svtbl[_f64](svfloat64_t data, svuint64_t indices)</para>
        ///   <para>  TBL Zresult.D, {Zdata.D}, Zindices.D</para>
        /// </summary>
        public static Vector<double> VectorTableLookup(Vector<double> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svint16_t svtbl[_s16](svint16_t data, svuint16_t indices)</para>
        ///   <para>  TBL Zresult.H, {Zdata.H}, Zindices.H</para>
        /// </summary>
        public static Vector<short> VectorTableLookup(Vector<short> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svint32_t svtbl[_s32](svint32_t data, svuint32_t indices)</para>
        ///   <para>  TBL Zresult.S, {Zdata.S}, Zindices.S</para>
        /// </summary>
        public static Vector<int> VectorTableLookup(Vector<int> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svint64_t svtbl[_s64](svint64_t data, svuint64_t indices)</para>
        ///   <para>  TBL Zresult.D, {Zdata.D}, Zindices.D</para>
        /// </summary>
        public static Vector<long> VectorTableLookup(Vector<long> data, Vector<ulong> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svint8_t svtbl[_s8](svint8_t data, svuint8_t indices)</para>
        ///   <para>  TBL Zresult.B, {Zdata.B}, Zindices.B</para>
        /// </summary>
        public static Vector<sbyte> VectorTableLookup(Vector<sbyte> data, Vector<byte> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svfloat32_t svtbl[_f32](svfloat32_t data, svuint32_t indices)</para>
        ///   <para>  TBL Zresult.S, {Zdata.S}, Zindices.S</para>
        /// </summary>
        public static Vector<float> VectorTableLookup(Vector<float> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svuint16_t svtbl[_u16](svuint16_t data, svuint16_t indices)</para>
        ///   <para>  TBL Zresult.H, {Zdata.H}, Zindices.H</para>
        /// </summary>
        public static Vector<ushort> VectorTableLookup(Vector<ushort> data, Vector<ushort> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svuint32_t svtbl[_u32](svuint32_t data, svuint32_t indices)</para>
        ///   <para>  TBL Zresult.S, {Zdata.S}, Zindices.S</para>
        /// </summary>
        public static Vector<uint> VectorTableLookup(Vector<uint> data, Vector<uint> indices) => VectorTableLookup(data, indices);

        /// <summary>
        ///   <para>svuint64_t svtbl[_u64](svuint64_t data, svuint64_t indices)</para>
        ///   <para>  TBL Zresult.D, {Zdata.D}, Zindices.D</para>
        /// </summary>
        public static Vector<ulong> VectorTableLookup(Vector<ulong> data, Vector<ulong> indices) => VectorTableLookup(data, indices);


        // Bitwise exclusive OR

        /// <summary>
        ///   <para>svuint8_t sveor[_u8]_m(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t sveor[_u8]_x(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>svuint8_t sveor[_u8]_z(svbool_t pg, svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> Xor(Vector<byte> left, Vector<byte> right) => Xor(left, right);

        /// <summary>
        ///   <para>svint16_t sveor[_s16]_m(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t sveor[_s16]_x(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>svint16_t sveor[_s16]_z(svbool_t pg, svint16_t op1, svint16_t op2)</para>
        ///   <para>  EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> Xor(Vector<short> left, Vector<short> right) => Xor(left, right);

        /// <summary>
        ///   <para>svint32_t sveor[_s32]_m(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t sveor[_s32]_x(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>svint32_t sveor[_s32]_z(svbool_t pg, svint32_t op1, svint32_t op2)</para>
        ///   <para>  EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> Xor(Vector<int> left, Vector<int> right) => Xor(left, right);

        /// <summary>
        ///   <para>svint64_t sveor[_s64]_m(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t sveor[_s64]_x(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>svint64_t sveor[_s64]_z(svbool_t pg, svint64_t op1, svint64_t op2)</para>
        ///   <para>  EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> Xor(Vector<long> left, Vector<long> right) => Xor(left, right);

        /// <summary>
        ///   <para>svint8_t sveor[_s8]_m(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t sveor[_s8]_x(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>svint8_t sveor[_s8]_z(svbool_t pg, svint8_t op1, svint8_t op2)</para>
        ///   <para>  EOR Ztied1.B, Pg/M, Ztied1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> Xor(Vector<sbyte> left, Vector<sbyte> right) => Xor(left, right);

        /// <summary>
        ///   <para>svuint16_t sveor[_u16]_m(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t sveor[_u16]_x(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>svuint16_t sveor[_u16]_z(svbool_t pg, svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  EOR Ztied1.H, Pg/M, Ztied1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> Xor(Vector<ushort> left, Vector<ushort> right) => Xor(left, right);

        /// <summary>
        ///   <para>svuint32_t sveor[_u32]_m(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t sveor[_u32]_x(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>svuint32_t sveor[_u32]_z(svbool_t pg, svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  EOR Ztied1.S, Pg/M, Ztied1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> Xor(Vector<uint> left, Vector<uint> right) => Xor(left, right);

        /// <summary>
        ///   <para>svuint64_t sveor[_u64]_m(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t sveor[_u64]_x(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>svuint64_t sveor[_u64]_z(svbool_t pg, svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  EOR Ztied1.D, Pg/M, Ztied1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> Xor(Vector<ulong> left, Vector<ulong> right) => Xor(left, right);


        // Bitwise exclusive OR reduction to scalar

        /// <summary>
        ///   <para>uint8_t sveorv[_u8](svbool_t pg, svuint8_t op)</para>
        ///   <para>  EORV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<byte> XorAcross(Vector<byte> value) => XorAcross(value);

        /// <summary>
        ///   <para>int16_t sveorv[_s16](svbool_t pg, svint16_t op)</para>
        ///   <para>  EORV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<short> XorAcross(Vector<short> value) => XorAcross(value);

        /// <summary>
        ///   <para>int32_t sveorv[_s32](svbool_t pg, svint32_t op)</para>
        ///   <para>  EORV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<int> XorAcross(Vector<int> value) => XorAcross(value);

        /// <summary>
        ///   <para>int64_t sveorv[_s64](svbool_t pg, svint64_t op)</para>
        ///   <para>  EORV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<long> XorAcross(Vector<long> value) => XorAcross(value);

        /// <summary>
        ///   <para>int8_t sveorv[_s8](svbool_t pg, svint8_t op)</para>
        ///   <para>  EORV Bresult, Pg, Zop.B</para>
        /// </summary>
        public static Vector<sbyte> XorAcross(Vector<sbyte> value) => XorAcross(value);

        /// <summary>
        ///   <para>uint16_t sveorv[_u16](svbool_t pg, svuint16_t op)</para>
        ///   <para>  EORV Hresult, Pg, Zop.H</para>
        /// </summary>
        public static Vector<ushort> XorAcross(Vector<ushort> value) => XorAcross(value);

        /// <summary>
        ///   <para>uint32_t sveorv[_u32](svbool_t pg, svuint32_t op)</para>
        ///   <para>  EORV Sresult, Pg, Zop.S</para>
        /// </summary>
        public static Vector<uint> XorAcross(Vector<uint> value) => XorAcross(value);

        /// <summary>
        ///   <para>uint64_t sveorv[_u64](svbool_t pg, svuint64_t op)</para>
        ///   <para>  EORV Dresult, Pg, Zop.D</para>
        /// </summary>
        public static Vector<ulong> XorAcross(Vector<ulong> value) => XorAcross(value);


        // Zero-extend the low 16 bits

        /// <summary>
        ///   <para>svuint32_t svexth[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svexth[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svexth[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  UXTH Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ZeroExtend16(Vector<uint> value) => ZeroExtend16(value);

        /// <summary>
        ///   <para>svuint64_t svexth[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svexth[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svexth[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  UXTH Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ZeroExtend16(Vector<ulong> value) => ZeroExtend16(value);


        // Zero-extend the low 32 bits

        /// <summary>
        ///   <para>svuint64_t svextw[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svextw[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svextw[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  UXTW Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ZeroExtend32(Vector<ulong> value) => ZeroExtend32(value);


        // Zero-extend the low 8 bits

        /// <summary>
        ///   <para>svuint16_t svextb[_u16]_m(svuint16_t inactive, svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svextb[_u16]_x(svbool_t pg, svuint16_t op)</para>
        ///   <para>svuint16_t svextb[_u16]_z(svbool_t pg, svuint16_t op)</para>
        ///   <para>  UXTB Zresult.H, Pg/M, Zop.H</para>
        /// </summary>
        public static Vector<ushort> ZeroExtend8(Vector<ushort> value) => ZeroExtend8(value);

        /// <summary>
        ///   <para>svuint32_t svextb[_u32]_m(svuint32_t inactive, svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svextb[_u32]_x(svbool_t pg, svuint32_t op)</para>
        ///   <para>svuint32_t svextb[_u32]_z(svbool_t pg, svuint32_t op)</para>
        ///   <para>  UXTB Zresult.S, Pg/M, Zop.S</para>
        /// </summary>
        public static Vector<uint> ZeroExtend8(Vector<uint> value) => ZeroExtend8(value);

        /// <summary>
        ///   <para>svuint64_t svextb[_u64]_m(svuint64_t inactive, svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svextb[_u64]_x(svbool_t pg, svuint64_t op)</para>
        ///   <para>svuint64_t svextb[_u64]_z(svbool_t pg, svuint64_t op)</para>
        ///   <para>  UXTB Zresult.D, Pg/M, Zop.D</para>
        /// </summary>
        public static Vector<ulong> ZeroExtend8(Vector<ulong> value) => ZeroExtend8(value);


        // Unpack and extend low half

        /// <summary>
        ///   <para>svuint16_t svunpklo[_u16](svuint8_t op)</para>
        ///   <para>  UUNPKLO Zresult.H, Zop.B</para>
        /// </summary>
        public static Vector<ushort> ZeroExtendWideningLower(Vector<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        ///   <para>svuint32_t svunpklo[_u32](svuint16_t op)</para>
        ///   <para>  UUNPKLO Zresult.S, Zop.H</para>
        /// </summary>
        public static Vector<uint> ZeroExtendWideningLower(Vector<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        ///   <para>svuint64_t svunpklo[_u64](svuint32_t op)</para>
        ///   <para>  UUNPKLO Zresult.D, Zop.S</para>
        /// </summary>
        public static Vector<ulong> ZeroExtendWideningLower(Vector<uint> value) => ZeroExtendWideningLower(value);


        // Unpack and extend high half

        /// <summary>
        ///   <para>svuint16_t svunpkhi[_u16](svuint8_t op)</para>
        ///   <para>  UUNPKHI Zresult.H, Zop.B</para>
        /// </summary>
        public static Vector<ushort> ZeroExtendWideningUpper(Vector<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        ///   <para>svuint32_t svunpkhi[_u32](svuint16_t op)</para>
        ///   <para>  UUNPKHI Zresult.S, Zop.H</para>
        /// </summary>
        public static Vector<uint> ZeroExtendWideningUpper(Vector<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        ///   <para>svuint64_t svunpkhi[_u64](svuint32_t op)</para>
        ///   <para>  UUNPKHI Zresult.D, Zop.S</para>
        /// </summary>
        public static Vector<ulong> ZeroExtendWideningUpper(Vector<uint> value) => ZeroExtendWideningUpper(value);


        // Interleave elements from high halves of two inputs

        /// <summary>
        ///   <para>svuint8_t svzip2[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  ZIP2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> ZipHigh(Vector<byte> left, Vector<byte> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svfloat64_t svzip2[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  ZIP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> ZipHigh(Vector<double> left, Vector<double> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svint16_t svzip2[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  ZIP2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> ZipHigh(Vector<short> left, Vector<short> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svint32_t svzip2[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  ZIP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> ZipHigh(Vector<int> left, Vector<int> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svint64_t svzip2[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  ZIP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> ZipHigh(Vector<long> left, Vector<long> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svint8_t svzip2[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  ZIP2 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> ZipHigh(Vector<sbyte> left, Vector<sbyte> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svfloat32_t svzip2[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  ZIP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> ZipHigh(Vector<float> left, Vector<float> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svuint16_t svzip2[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  ZIP2 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> ZipHigh(Vector<ushort> left, Vector<ushort> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svuint32_t svzip2[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  ZIP2 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> ZipHigh(Vector<uint> left, Vector<uint> right) => ZipHigh(left, right);

        /// <summary>
        ///   <para>svuint64_t svzip2[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  ZIP2 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> ZipHigh(Vector<ulong> left, Vector<ulong> right) => ZipHigh(left, right);


        // Interleave elements from low halves of two inputs

        /// <summary>
        ///   <para>svuint8_t svzip1[_u8](svuint8_t op1, svuint8_t op2)</para>
        ///   <para>  ZIP1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<byte> ZipLow(Vector<byte> left, Vector<byte> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svfloat64_t svzip1[_f64](svfloat64_t op1, svfloat64_t op2)</para>
        ///   <para>  ZIP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<double> ZipLow(Vector<double> left, Vector<double> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svint16_t svzip1[_s16](svint16_t op1, svint16_t op2)</para>
        ///   <para>  ZIP1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<short> ZipLow(Vector<short> left, Vector<short> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svint32_t svzip1[_s32](svint32_t op1, svint32_t op2)</para>
        ///   <para>  ZIP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<int> ZipLow(Vector<int> left, Vector<int> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svint64_t svzip1[_s64](svint64_t op1, svint64_t op2)</para>
        ///   <para>  ZIP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<long> ZipLow(Vector<long> left, Vector<long> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svint8_t svzip1[_s8](svint8_t op1, svint8_t op2)</para>
        ///   <para>  ZIP1 Zresult.B, Zop1.B, Zop2.B</para>
        /// </summary>
        public static Vector<sbyte> ZipLow(Vector<sbyte> left, Vector<sbyte> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svfloat32_t svzip1[_f32](svfloat32_t op1, svfloat32_t op2)</para>
        ///   <para>  ZIP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<float> ZipLow(Vector<float> left, Vector<float> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svuint16_t svzip1[_u16](svuint16_t op1, svuint16_t op2)</para>
        ///   <para>  ZIP1 Zresult.H, Zop1.H, Zop2.H</para>
        /// </summary>
        public static Vector<ushort> ZipLow(Vector<ushort> left, Vector<ushort> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svuint32_t svzip1[_u32](svuint32_t op1, svuint32_t op2)</para>
        ///   <para>  ZIP1 Zresult.S, Zop1.S, Zop2.S</para>
        /// </summary>
        public static Vector<uint> ZipLow(Vector<uint> left, Vector<uint> right) => ZipLow(left, right);

        /// <summary>
        ///   <para>svuint64_t svzip1[_u64](svuint64_t op1, svuint64_t op2)</para>
        ///   <para>  ZIP1 Zresult.D, Zop1.D, Zop2.D</para>
        /// </summary>
        public static Vector<ulong> ZipLow(Vector<ulong> left, Vector<ulong> right) => ZipLow(left, right);

    }
}

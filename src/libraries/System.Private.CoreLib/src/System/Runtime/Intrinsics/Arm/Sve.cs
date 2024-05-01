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
    [System.Runtime.Versioning.RequiresPreviewFeaturesAttribute("Sve is in preview.")]
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

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

    }
}

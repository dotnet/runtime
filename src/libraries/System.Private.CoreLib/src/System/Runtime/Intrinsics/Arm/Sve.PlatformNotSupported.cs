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
    [CLSCompliant(false)]
    [System.Runtime.Versioning.RequiresPreviewFeaturesAttribute("Sve is in preview.")]
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
    abstract class Sve : AdvSimd
    {
        internal Sve() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
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


        ///  CreateTrueMaskByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<byte> CreateTrueMaskByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskDouble : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<double> CreateTrueMaskDouble([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<short> CreateTrueMaskInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<int> CreateTrueMaskInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<long> CreateTrueMaskInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskSByte : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<sbyte> CreateTrueMaskSByte([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskSingle : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b8(enum svpattern pattern)
        ///   PTRUE Presult.B, pattern
        /// </summary>
        public static unsafe Vector<float> CreateTrueMaskSingle([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt16 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b16(enum svpattern pattern)
        ///   PTRUE Presult.H, pattern
        /// </summary>
        public static unsafe Vector<ushort> CreateTrueMaskUInt16([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt32 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b32(enum svpattern pattern)
        ///   PTRUE Presult.S, pattern
        /// </summary>
        public static unsafe Vector<uint> CreateTrueMaskUInt32([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }


        ///  CreateTrueMaskUInt64 : Set predicate elements to true

        /// <summary>
        /// svbool_t svptrue_pat_b64(enum svpattern pattern)
        ///   PTRUE Presult.D, pattern
        /// </summary>
        public static unsafe Vector<ulong> CreateTrueMaskUInt64([ConstantExpected] SveMaskPattern pattern = SveMaskPattern.All) { throw new PlatformNotSupportedException(); }

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

        ///  LoadVector : Unextended load

        /// <summary>
        /// svint8_t svld1[_s8](svbool_t pg, const int8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svld1[_s16](svbool_t pg, const int16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVector(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1[_s32](svbool_t pg, const int32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVector(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1[_s64](svbool_t pg, const int64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVector(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svld1[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVector(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svld1[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVector(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1[_f32](svbool_t pg, const float32_t *base)
        ///   LD1W Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVector(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1[_f64](svbool_t pg, const float64_t *base)
        ///   LD1D Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVector(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint16_t svld1ub_s16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorByteZeroExtendToInt16(Vector<short> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1ub_s32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorByteZeroExtendToInt32(Vector<int> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1ub_s64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorByteZeroExtendToInt64(Vector<long> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt16 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint16_t svld1ub_u16(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorByteZeroExtendToUInt16(Vector<ushort> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt32 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1ub_u32(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorByteZeroExtendToUInt32(Vector<uint> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorByteZeroExtendToUInt64 : Load 8-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1ub_u64(svbool_t pg, const uint8_t *base)
        ///   LD1B Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorByteZeroExtendToUInt64(Vector<ulong> mask, byte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sh_s32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorInt16SignExtendToInt32(Vector<int> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sh_s64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt16SignExtendToInt64(Vector<long> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToUInt32 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sh_u32(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorInt16SignExtendToUInt32(Vector<uint> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt16SignExtendToUInt64 : Load 16-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sh_u64(svbool_t pg, const int16_t *base)
        ///   LD1SH Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt16SignExtendToUInt64(Vector<ulong> mask, short* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32SignExtendToInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sw_s64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorInt32SignExtendToInt64(Vector<long> mask, int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorInt32SignExtendToUInt64 : Load 32-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sw_u64(svbool_t pg, const int32_t *base)
        ///   LD1SW Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorInt32SignExtendToUInt64(Vector<ulong> mask, int* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint16_t svld1sb_s16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorSByteSignExtendToInt16(Vector<short> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint32_t svld1sb_s32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorSByteSignExtendToInt32(Vector<int> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svint64_t svld1sb_s64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorSByteSignExtendToInt64(Vector<long> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt16 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint16_t svld1sb_u16(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorSByteSignExtendToUInt16(Vector<ushort> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt32 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint32_t svld1sb_u32(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorSByteSignExtendToUInt32(Vector<uint> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorSByteSignExtendToUInt64 : Load 8-bit data and sign-extend

        /// <summary>
        /// svuint64_t svld1sb_u64(svbool_t pg, const int8_t *base)
        ///   LD1SB Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorSByteSignExtendToUInt64(Vector<ulong> mask, sbyte* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint32_t svld1uh_s32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorUInt16ZeroExtendToInt32(Vector<int> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uh_s64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt16ZeroExtendToInt64(Vector<long> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToUInt32 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint32_t svld1uh_u32(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorUInt16ZeroExtendToUInt32(Vector<uint> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt16ZeroExtendToUInt64 : Load 16-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uh_u64(svbool_t pg, const uint16_t *base)
        ///   LD1H Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt16ZeroExtendToUInt64(Vector<ulong> mask, ushort* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32ZeroExtendToInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svint64_t svld1uw_s64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorUInt32ZeroExtendToInt64(Vector<long> mask, uint* address) { throw new PlatformNotSupportedException(); }


        ///  LoadVectorUInt32ZeroExtendToUInt64 : Load 32-bit data and zero-extend

        /// <summary>
        /// svuint64_t svld1uw_u64(svbool_t pg, const uint32_t *base)
        ///   LD1W Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorUInt32ZeroExtendToUInt64(Vector<ulong> mask, uint* address) { throw new PlatformNotSupportedException(); }


    }
}

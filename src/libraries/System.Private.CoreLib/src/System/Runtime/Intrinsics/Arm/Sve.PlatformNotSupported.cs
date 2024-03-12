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


    }
}

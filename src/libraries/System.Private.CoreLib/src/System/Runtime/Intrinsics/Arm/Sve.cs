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

        ///  LoadVectorNonFaulting : Unextended load, non-faulting

        /// <summary>
        /// svint8_t svldnf1[_s8](svbool_t pg, const int8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVectorNonFaulting(Vector<sbyte> mask, sbyte* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svint16_t svldnf1[_s16](svbool_t pg, const int16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<short> LoadVectorNonFaulting(Vector<short> mask, short* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svint32_t svldnf1[_s32](svbool_t pg, const int32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<int> LoadVectorNonFaulting(Vector<int> mask, int* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svint64_t svldnf1[_s64](svbool_t pg, const int64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<long> LoadVectorNonFaulting(Vector<long> mask, long* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svuint8_t svldnf1[_u8](svbool_t pg, const uint8_t *base)
        ///   LDNF1B Zresult.B, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<byte> LoadVectorNonFaulting(Vector<byte> mask, byte* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svuint16_t svldnf1[_u16](svbool_t pg, const uint16_t *base)
        ///   LDNF1H Zresult.H, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ushort> LoadVectorNonFaulting(Vector<ushort> mask, ushort* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svuint32_t svldnf1[_u32](svbool_t pg, const uint32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<uint> LoadVectorNonFaulting(Vector<uint> mask, uint* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svuint64_t svldnf1[_u64](svbool_t pg, const uint64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<ulong> LoadVectorNonFaulting(Vector<ulong> mask, ulong* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svfloat32_t svldnf1[_f32](svbool_t pg, const float32_t *base)
        ///   LDNF1W Zresult.S, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<float> LoadVectorNonFaulting(Vector<float> mask, float* address) => LoadVectorNonFaulting(mask, address);

        /// <summary>
        /// svfloat64_t svldnf1[_f64](svbool_t pg, const float64_t *base)
        ///   LDNF1D Zresult.D, Pg/Z, [Xbase, #0, MUL VL]
        /// </summary>
        public static unsafe Vector<double> LoadVectorNonFaulting(Vector<double> mask, double* address) => LoadVectorNonFaulting(mask, address);


    }
}

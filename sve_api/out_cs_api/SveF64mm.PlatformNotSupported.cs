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
    public abstract class SveF64mm : AdvSimd
    {
        internal SveF64mm() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp1q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svuzp1q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ConcatenateEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svuzp1q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ConcatenateEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svuzp1q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ConcatenateEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svuzp1q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ConcatenateEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svuzp1q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svuzp1q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ConcatenateEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svuzp1q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svuzp1q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ConcatenateEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svuzp1q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ConcatenateEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp2q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svuzp2q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> ConcatenateOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svuzp2q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> ConcatenateOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svuzp2q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> ConcatenateOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svuzp2q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> ConcatenateOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svuzp2q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svuzp2q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> ConcatenateOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svuzp2q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svuzp2q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> ConcatenateOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svuzp2q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> ConcatenateOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn1q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svtrn1q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleaveEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svtrn1q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleaveEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svtrn1q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleaveEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svtrn1q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleaveEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svtrn1q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleaveEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svtrn1q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleaveEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svtrn1q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleaveEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svtrn1q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> InterleaveEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtrn1q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> InterleaveEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svzip2q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svzip2q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svzip2q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svzip2q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svzip2q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svzip2q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svzip2q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svzip2q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svzip2q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svzip1q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svzip1q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svzip1q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svzip1q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svzip1q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svzip1q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svzip1q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svzip1q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svzip1q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn2q[_s8](svint8_t op1, svint8_t op2)
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svtrn2q[_s16](svint16_t op1, svint16_t op2)
        /// </summary>
        public static unsafe Vector<short> InterleaveOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svtrn2q[_s32](svint32_t op1, svint32_t op2)
        /// </summary>
        public static unsafe Vector<int> InterleaveOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svtrn2q[_s64](svint64_t op1, svint64_t op2)
        /// </summary>
        public static unsafe Vector<long> InterleaveOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svtrn2q[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> InterleaveOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svtrn2q[_u16](svuint16_t op1, svuint16_t op2)
        /// </summary>
        public static unsafe Vector<ushort> InterleaveOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svtrn2q[_u32](svuint32_t op1, svuint32_t op2)
        /// </summary>
        public static unsafe Vector<uint> InterleaveOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svtrn2q[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> InterleaveOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svtrn2q[_f32](svfloat32_t op1, svfloat32_t op2)
        /// </summary>
        public static unsafe Vector<float> InterleaveOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svtrn2q[_f64](svfloat64_t op1, svfloat64_t op2)
        /// </summary>
        public static unsafe Vector<double> InterleaveOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) { throw new PlatformNotSupportedException(); }


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svint8_t svld1ro[_s8](svbool_t pg, const int8_t *base)
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector256AndReplicateToVector(Vector<sbyte> mask, sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint16_t svld1ro[_s16](svbool_t pg, const int16_t *base)
        /// </summary>
        public static unsafe Vector<short> LoadVector256AndReplicateToVector(Vector<short> mask, short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint32_t svld1ro[_s32](svbool_t pg, const int32_t *base)
        /// </summary>
        public static unsafe Vector<int> LoadVector256AndReplicateToVector(Vector<int> mask, int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svint64_t svld1ro[_s64](svbool_t pg, const int64_t *base)
        /// </summary>
        public static unsafe Vector<long> LoadVector256AndReplicateToVector(Vector<long> mask, long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svld1ro[_u8](svbool_t pg, const uint8_t *base)
        /// </summary>
        public static unsafe Vector<byte> LoadVector256AndReplicateToVector(Vector<byte> mask, byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svld1ro[_u16](svbool_t pg, const uint16_t *base)
        /// </summary>
        public static unsafe Vector<ushort> LoadVector256AndReplicateToVector(Vector<ushort> mask, ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svld1ro[_u32](svbool_t pg, const uint32_t *base)
        /// </summary>
        public static unsafe Vector<uint> LoadVector256AndReplicateToVector(Vector<uint> mask, uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svld1ro[_u64](svbool_t pg, const uint64_t *base)
        /// </summary>
        public static unsafe Vector<ulong> LoadVector256AndReplicateToVector(Vector<ulong> mask, ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat32_t svld1ro[_f32](svbool_t pg, const float32_t *base)
        /// </summary>
        public static unsafe Vector<float> LoadVector256AndReplicateToVector(Vector<float> mask, float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svfloat64_t svld1ro[_f64](svbool_t pg, const float64_t *base)
        /// </summary>
        public static unsafe Vector<double> LoadVector256AndReplicateToVector(Vector<double> mask, double* address) { throw new PlatformNotSupportedException(); }


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svfloat64_t svmmla[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        /// </summary>
        public static unsafe Vector<double> MatrixMultiplyAccumulate(Vector<double> op1, Vector<double> op2, Vector<double> op3) { throw new PlatformNotSupportedException(); }

    }
}


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


        ///  ConcatenateEvenInt128FromTwoInputs : Concatenate even quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp1q[_s8](svint8_t op1, svint8_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svuzp1q[_s16](svint16_t op1, svint16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> ConcatenateEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svuzp1q[_s32](svint32_t op1, svint32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> ConcatenateEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svuzp1q[_s64](svint64_t op1, svint64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> ConcatenateEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svuzp1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> ConcatenateEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svuzp1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svuzp1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> ConcatenateEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svuzp1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svuzp1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svuzp1q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> ConcatenateEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svuzp1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> ConcatenateEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) => ConcatenateEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svuzp1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> ConcatenateEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) => ConcatenateEvenInt128FromTwoInputs(left, right);


        ///  ConcatenateOddInt128FromTwoInputs : Concatenate odd quadwords from two inputs

        /// <summary>
        /// svint8_t svuzp2q[_s8](svint8_t op1, svint8_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> ConcatenateOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svuzp2q[_s16](svint16_t op1, svint16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> ConcatenateOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svuzp2q[_s32](svint32_t op1, svint32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> ConcatenateOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svuzp2q[_s64](svint64_t op1, svint64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> ConcatenateOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svuzp2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> ConcatenateOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svuzp2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> ConcatenateOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svuzp2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> ConcatenateOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svuzp2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> ConcatenateOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svuzp2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> ConcatenateOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svuzp2q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> ConcatenateOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svuzp2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> ConcatenateOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) => ConcatenateOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svuzp2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   UZP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> ConcatenateOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) => ConcatenateOddInt128FromTwoInputs(left, right);


        ///  InterleaveEvenInt128FromTwoInputs : Interleave even quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn1q[_s8](svint8_t op1, svint8_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveEvenInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svtrn1q[_s16](svint16_t op1, svint16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> InterleaveEvenInt128FromTwoInputs(Vector<short> left, Vector<short> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svtrn1q[_s32](svint32_t op1, svint32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> InterleaveEvenInt128FromTwoInputs(Vector<int> left, Vector<int> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svtrn1q[_s64](svint64_t op1, svint64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> InterleaveEvenInt128FromTwoInputs(Vector<long> left, Vector<long> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svtrn1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> InterleaveEvenInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svtrn1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> InterleaveEvenInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svtrn1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> InterleaveEvenInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svtrn1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> InterleaveEvenInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svtrn1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveEvenInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svtrn1q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> InterleaveEvenInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svtrn1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> InterleaveEvenInt128FromTwoInputs(Vector<float> left, Vector<float> right) => InterleaveEvenInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svtrn1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> InterleaveEvenInt128FromTwoInputs(Vector<double> left, Vector<double> right) => InterleaveEvenInt128FromTwoInputs(left, right);


        ///  InterleaveInt128FromHighHalvesOfTwoInputs : Interleave quadwords from high halves of two inputs

        /// <summary>
        /// svint8_t svzip2q[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint16_t svzip2q[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<short> left, Vector<short> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint32_t svzip2q[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<int> left, Vector<int> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint64_t svzip2q[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<long> left, Vector<long> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svzip2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svzip2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svzip2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svzip2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svzip2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svzip2q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svzip2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<float> left, Vector<float> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svzip2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromHighHalvesOfTwoInputs(Vector<double> left, Vector<double> right) => InterleaveInt128FromHighHalvesOfTwoInputs(left, right);


        ///  InterleaveInt128FromLowHalvesOfTwoInputs : Interleave quadwords from low halves of two inputs

        /// <summary>
        /// svint8_t svzip1q[_s8](svint8_t op1, svint8_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint16_t svzip1q[_s16](svint16_t op1, svint16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<short> left, Vector<short> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint32_t svzip1q[_s32](svint32_t op1, svint32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<int> left, Vector<int> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svint64_t svzip1q[_s64](svint64_t op1, svint64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<long> left, Vector<long> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svzip1q[_u8](svuint8_t op1, svuint8_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svzip1q[_u16](svuint16_t op1, svuint16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svzip1q[_u32](svuint32_t op1, svuint32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svzip1q[_u64](svuint64_t op1, svuint64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svzip1q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svzip1q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<half> left, Vector<half> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svzip1q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<float> left, Vector<float> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svzip1q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   ZIP1 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> InterleaveInt128FromLowHalvesOfTwoInputs(Vector<double> left, Vector<double> right) => InterleaveInt128FromLowHalvesOfTwoInputs(left, right);


        ///  InterleaveOddInt128FromTwoInputs : Interleave odd quadwords from two inputs

        /// <summary>
        /// svint8_t svtrn2q[_s8](svint8_t op1, svint8_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<sbyte> InterleaveOddInt128FromTwoInputs(Vector<sbyte> left, Vector<sbyte> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint16_t svtrn2q[_s16](svint16_t op1, svint16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<short> InterleaveOddInt128FromTwoInputs(Vector<short> left, Vector<short> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint32_t svtrn2q[_s32](svint32_t op1, svint32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<int> InterleaveOddInt128FromTwoInputs(Vector<int> left, Vector<int> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svint64_t svtrn2q[_s64](svint64_t op1, svint64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<long> InterleaveOddInt128FromTwoInputs(Vector<long> left, Vector<long> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint8_t svtrn2q[_u8](svuint8_t op1, svuint8_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<byte> InterleaveOddInt128FromTwoInputs(Vector<byte> left, Vector<byte> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint16_t svtrn2q[_u16](svuint16_t op1, svuint16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ushort> InterleaveOddInt128FromTwoInputs(Vector<ushort> left, Vector<ushort> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint32_t svtrn2q[_u32](svuint32_t op1, svuint32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<uint> InterleaveOddInt128FromTwoInputs(Vector<uint> left, Vector<uint> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svuint64_t svtrn2q[_u64](svuint64_t op1, svuint64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<ulong> InterleaveOddInt128FromTwoInputs(Vector<ulong> left, Vector<ulong> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svbfloat16_t svtrn2q[_bf16](svbfloat16_t op1, svbfloat16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<bfloat16> InterleaveOddInt128FromTwoInputs(Vector<bfloat16> left, Vector<bfloat16> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat16_t svtrn2q[_f16](svfloat16_t op1, svfloat16_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<half> InterleaveOddInt128FromTwoInputs(Vector<half> left, Vector<half> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat32_t svtrn2q[_f32](svfloat32_t op1, svfloat32_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<float> InterleaveOddInt128FromTwoInputs(Vector<float> left, Vector<float> right) => InterleaveOddInt128FromTwoInputs(left, right);

        /// <summary>
        /// svfloat64_t svtrn2q[_f64](svfloat64_t op1, svfloat64_t op2)
        ///   TRN2 Zresult.Q, Zop1.Q, Zop2.Q
        /// </summary>
        public static unsafe Vector<double> InterleaveOddInt128FromTwoInputs(Vector<double> left, Vector<double> right) => InterleaveOddInt128FromTwoInputs(left, right);


        ///  LoadVector256AndReplicateToVector : Load and replicate 256 bits of data

        /// <summary>
        /// svint8_t svld1ro[_s8](svbool_t pg, const int8_t *base)
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1ROB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<sbyte> LoadVector256AndReplicateToVector(Vector<sbyte> mask, const sbyte *base) => LoadVector256AndReplicateToVector(mask, sbyte);

        /// <summary>
        /// svint16_t svld1ro[_s16](svbool_t pg, const int16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<short> LoadVector256AndReplicateToVector(Vector<short> mask, const short *base) => LoadVector256AndReplicateToVector(mask, short);

        /// <summary>
        /// svint32_t svld1ro[_s32](svbool_t pg, const int32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<int> LoadVector256AndReplicateToVector(Vector<int> mask, const int *base) => LoadVector256AndReplicateToVector(mask, int);

        /// <summary>
        /// svint64_t svld1ro[_s64](svbool_t pg, const int64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<long> LoadVector256AndReplicateToVector(Vector<long> mask, const long *base) => LoadVector256AndReplicateToVector(mask, long);

        /// <summary>
        /// svuint8_t svld1ro[_u8](svbool_t pg, const uint8_t *base)
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, Xindex]
        ///   LD1ROB Zresult.B, Pg/Z, [Xarray, #index]
        ///   LD1ROB Zresult.B, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<byte> LoadVector256AndReplicateToVector(Vector<byte> mask, const byte *base) => LoadVector256AndReplicateToVector(mask, byte);

        /// <summary>
        /// svuint16_t svld1ro[_u16](svbool_t pg, const uint16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ushort> LoadVector256AndReplicateToVector(Vector<ushort> mask, const ushort *base) => LoadVector256AndReplicateToVector(mask, ushort);

        /// <summary>
        /// svuint32_t svld1ro[_u32](svbool_t pg, const uint32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<uint> LoadVector256AndReplicateToVector(Vector<uint> mask, const uint *base) => LoadVector256AndReplicateToVector(mask, uint);

        /// <summary>
        /// svuint64_t svld1ro[_u64](svbool_t pg, const uint64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<ulong> LoadVector256AndReplicateToVector(Vector<ulong> mask, const ulong *base) => LoadVector256AndReplicateToVector(mask, ulong);

        /// <summary>
        /// svbfloat16_t svld1ro[_bf16](svbool_t pg, const bfloat16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<bfloat16> LoadVector256AndReplicateToVector(Vector<bfloat16> mask, const bfloat16 *base) => LoadVector256AndReplicateToVector(mask, bfloat16);

        /// <summary>
        /// svfloat16_t svld1ro[_f16](svbool_t pg, const float16_t *base)
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, Xindex, LSL #1]
        ///   LD1ROH Zresult.H, Pg/Z, [Xarray, #index * 2]
        ///   LD1ROH Zresult.H, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<half> LoadVector256AndReplicateToVector(Vector<half> mask, const half *base) => LoadVector256AndReplicateToVector(mask, half);

        /// <summary>
        /// svfloat32_t svld1ro[_f32](svbool_t pg, const float32_t *base)
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, Xindex, LSL #2]
        ///   LD1ROW Zresult.S, Pg/Z, [Xarray, #index * 4]
        ///   LD1ROW Zresult.S, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<float> LoadVector256AndReplicateToVector(Vector<float> mask, const float *base) => LoadVector256AndReplicateToVector(mask, float);

        /// <summary>
        /// svfloat64_t svld1ro[_f64](svbool_t pg, const float64_t *base)
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, Xindex, LSL #3]
        ///   LD1ROD Zresult.D, Pg/Z, [Xarray, #index * 8]
        ///   LD1ROD Zresult.D, Pg/Z, [Xbase, #0]
        /// </summary>
        public static unsafe Vector<double> LoadVector256AndReplicateToVector(Vector<double> mask, const double *base) => LoadVector256AndReplicateToVector(mask, double);


        ///  MatrixMultiplyAccumulate : Matrix multiply-accumulate

        /// <summary>
        /// svfloat64_t svmmla[_f64](svfloat64_t op1, svfloat64_t op2, svfloat64_t op3)
        ///   FMMLA Ztied1.D, Zop2.D, Zop3.D
        ///   MOVPRFX Zresult, Zop1; FMMLA Zresult.D, Zop2.D, Zop3.D
        /// </summary>
        public static unsafe Vector<double> MatrixMultiplyAccumulate(Vector<double> op1, Vector<double> op2, Vector<double> op3) => MatrixMultiplyAccumulate(op1, op2, op3);

    }
}


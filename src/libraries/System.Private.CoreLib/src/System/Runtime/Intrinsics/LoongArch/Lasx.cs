// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LASX-256bits hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Lasx : Lsx
    {
        internal Lasx() { }

        public static new bool IsSupported { get => IsSupported; }

        /// <summary>
        /// int8x32_t xvadd_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Add(Vector256<sbyte> left, Vector256<sbyte> right) => Add(left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: TODO Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Add(Vector256<byte> left, Vector256<byte> right) => Add(left, right);

        /// <summary>
        /// int16x16_t xvadd_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Add(Vector256<short> left, Vector256<short> right) => Add(left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: TODO Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Add(Vector256<ushort> left, Vector256<ushort> right) => Add(left, right);

        /// <summary>
        /// int32x8_t xvadd_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVADD.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Add(Vector256<int> left, Vector256<int> right) => Add(left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Add(Vector256<uint> left, Vector256<uint> right) => Add(left, right);

        /// <summary>
        /// int64x4_t xvadd_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Add(Vector256<long> left, Vector256<long> right) => Add(left, right);

        /// <summary>
        /// uint64x4_t TODO_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Add(Vector256<ulong> left, Vector256<ulong> right) => Add(left, right);

        /// <summary>
        /// float32x8_t xvfadd_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFADD.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Add(Vector256<float> left, Vector256<float> right) => Add(left, right);

        /// <summary>
        /// float64x4_t xvfadd_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Add(Vector256<double> left, Vector256<double> right) => Add(left, right);

        /// <summary>
        /// int8x32_t xvsub_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Subtract(Vector256<sbyte> left, Vector256<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: TODO Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Subtract(Vector256<byte> left, Vector256<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x16_t xvsub_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Subtract(Vector256<short> left, Vector256<short> right) => Subtract(left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: TODO Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Subtract(Vector256<ushort> left, Vector256<ushort> right) => Subtract(left, right);

        /// <summary>
        /// int32x8_t xvsub_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSUB.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Subtract(Vector256<int> left, Vector256<int> right) => Subtract(left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Subtract(Vector256<uint> left, Vector256<uint> right) => Subtract(left, right);

        /// <summary>
        /// int64x4_t xvsub_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Subtract(Vector256<long> left, Vector256<long> right) => Subtract(left, right);

        /// <summary>
        /// uint64x4_t TODO_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Subtract(Vector256<ulong> left, Vector256<ulong> right) => Subtract(left, right);

        /// <summary>
        /// float32x8_t xvfsub_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFSUB.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Subtract(Vector256<float> left, Vector256<float> right) => Subtract(left, right);

        /// <summary>
        /// float64x4_t xvfsub_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Subtract(Vector256<double> left, Vector256<double> right) => Subtract(left, right);

        /// <summary>
        /// int8x32_t xvmul_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMUL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Multiply(Vector256<sbyte> left, Vector256<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: TODO Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Multiply(Vector256<byte> left, Vector256<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x16_t xvmul_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMUL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Multiply(Vector256<short> left, Vector256<short> right) => Multiply(left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: TODO Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Multiply(Vector256<ushort> left, Vector256<ushort> right) => Multiply(left, right);

        /// <summary>
        /// int32x8_t xvmul_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMULW Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Multiply(Vector256<int> left, Vector256<int> right) => Multiply(left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Multiply(Vector256<uint> left, Vector256<uint> right) => Multiply(left, right);

        /// <summary>
        /// int64x4_t xvmul_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Multiply(Vector256<long> left, Vector256<long> right) => Multiply(left, right);

        /// <summary>
        /// uint64x4_t TODO_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Multiply(Vector256<ulong> left, Vector256<ulong> right) => Multiply(left, right);

        /// <summary>
        /// float32x8_t xvfmul_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFMUL.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Multiply(Vector256<float> left, Vector256<float> right) => Multiply(left, right);

        /// <summary>
        /// float64x4_t xvfmul_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Multiply(Vector256<double> left, Vector256<double> right) => Multiply(left, right);

        /// <summary>
        /// int8x32_t xvdiv_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVDIV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Divide(Vector256<sbyte> left, Vector256<sbyte> right) => Divide(left, right);

        /// <summary>
        /// uint8x32_t xvdiv_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVDIV.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Divide(Vector256<byte> left, Vector256<byte> right) => Divide(left, right);

        /// <summary>
        /// int16x16_t xvdiv_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVDIV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Divide(Vector256<short> left, Vector256<short> right) => Divide(left, right);

        /// <summary>
        /// uint16x16_t xvdiv_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVDIV.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Divide(Vector256<ushort> left, Vector256<ushort> right) => Divide(left, right);

        /// <summary>
        /// int32x8_t xvdiv_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVDIV.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Divide(Vector256<int> left, Vector256<int> right) => Divide(left, right);

        /// <summary>
        /// uint32x8_t xvdiv_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVDIV.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Divide(Vector256<uint> left, Vector256<uint> right) => Divide(left, right);

        /// <summary>
        /// int64x4_t xvdiv_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVDIV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Divide(Vector256<long> left, Vector256<long> right) => Divide(left, right);

        /// <summary>
        /// uint64x4_t xvdiv_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVDIV.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Divide(Vector256<ulong> left, Vector256<ulong> right) => Divide(left, right);

        /// <summary>
        /// float32x8_t xvfdiv_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFDIV.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Divide(Vector256<float> left, Vector256<float> right) => Divide(left, right);

        /// <summary>
        /// float64x4_t xvfdiv_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFDIV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Divide(Vector256<double> left, Vector256<double> right) => Divide(left, right);

        /// <summary>
        /// float32x8_t xvfmadd_s_f32 (float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFMADD.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> FusedMultiplyAdd(Vector256<float> addend, Vector256<float> left, Vector256<float> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// float64x4_t xvfmadd_d_f64 (float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFMADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> FusedMultiplyAdd(Vector256<double> addend, Vector256<double> left, Vector256<double> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// int8x32_t xvmadd_b_s8 (int8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> MultiplyAdd(Vector256<sbyte> addend, Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: TODO Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> MultiplyAdd(Vector256<byte> addend, Vector256<byte> left, Vector256<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int16x16_t xvmadd_h_s16 (int16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> MultiplyAdd(Vector256<short> addend, Vector256<short> left, Vector256<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: TODO Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> MultiplyAdd(Vector256<ushort> addend, Vector256<ushort> left, Vector256<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int32x8_t xvmadd_w_s32 (int32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMADD.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> MultiplyAdd(Vector256<int> addend, Vector256<int> left, Vector256<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> MultiplyAdd(Vector256<uint> addend, Vector256<uint> left, Vector256<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int8x32_t TODO_s8 (int8x32_t a, uint8x32_t b)
        ///   LASX: TODO Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, int8x32_t b)
        ///   LASX: TODO Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x16_t TODO_s16 (int16x16_t a, uint16x16_t b)
        ///   LASX: TODO Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, int16x16_t b)
        ///   LASX: TODO Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x8_t TODO_s32 (int32x8_t a, uint32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<int> AddSaturate(Vector256<int> left, Vector256<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, int32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<uint> AddSaturate(Vector256<uint> left, Vector256<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x4_t TODO_s64 (int64x4_t a, uint64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> AddSaturate(Vector256<long> left, Vector256<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x4_t TODO_u64 (uint64x4_t a, int64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<ulong> AddSaturate(Vector256<ulong> left, Vector256<long> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x32_t xvsadd_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSADD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AddSaturate(Vector256<sbyte> left, Vector256<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x16_t xvsadd_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSADD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> AddSaturate(Vector256<short> left, Vector256<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x8_t xvsadd_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSADD.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> AddSaturate(Vector256<int> left, Vector256<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x4_t xvsadd_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSADD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> AddSaturate(Vector256<long> left, Vector256<long> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x32_t xvsadd_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSADD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AddSaturate(Vector256<byte> left, Vector256<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x16_t xvsadd_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSADD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AddSaturate(Vector256<ushort> left, Vector256<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x8_t xvsadd_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSADD.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> AddSaturate(Vector256<uint> left, Vector256<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x4_t xvsadd_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSADD.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> AddSaturate(Vector256<ulong> left, Vector256<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x32_t xvmsub_b_s8 (int8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> MultiplySubtract(Vector256<sbyte> minuend, Vector256<sbyte> left, Vector256<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: TODO Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> MultiplySubtract(Vector256<byte> minuend, Vector256<byte> left, Vector256<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x16_t xvmsub_h_s16 (int16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> MultiplySubtract(Vector256<short> minuend, Vector256<short> left, Vector256<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: TODO Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> MultiplySubtract(Vector256<ushort> minuend, Vector256<ushort> left, Vector256<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int32x8_t xvmsub_w_s32 (int32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMSUB.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> MultiplySubtract(Vector256<int> minuend, Vector256<int> left, Vector256<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint32x8_t TODO_u32 (uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> MultiplySubtract(Vector256<uint> minuend, Vector256<uint> left, Vector256<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x16_t xvmaddwev_h_b_s8 (int16x16_t a, int8x8_t b, int8x8_t c)
        ///   LASX: XVMADDWEV.H.B Xd.16H, Xj.8B, Xk.8B
        /// </summary>
        public static Vector256<short> MultiplyWideningLowerAndAdd(Vector256<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint16x16_t xvmaddwev_h_bu_u8 (uint16x16_t a, uint8x8_t b, uint8x8_t c)
        ///   LASX: XVMADDWEV.H.BU Xd.16H, Xj.8B, Xk.8B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningLowerAndAdd(Vector256<ushort> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int32x8_t xvmaddwev_w_h_s16 (int32x8_t a, int16x4_t b, int16x4_t c)
        ///   LASX: XVMADDWEV.W.H Xd.8S, Xj.4H, Xk.4H
        /// </summary>
        public static Vector256<int> MultiplyWideningLowerAndAdd(Vector256<int> addend, Vector128<short> left, Vector128<short> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint32x8_t xvmaddwev_w_hu_u16 (uint32x8_t a, uint16x4_t b, uint16x4_t c)
        ///   LASX: XVMADDWEV.W.HU Xd.8S, Xj.4H, Xk.4H
        /// </summary>
        public static Vector256<uint> MultiplyWideningLowerAndAdd(Vector256<uint> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int64x4_t xvmaddwev_d_w_s32 (int64x4_t a, int32x2_t b, int32x2_t c)
        ///   LASX: XVMADDWEV.D.W Xd.4D, Xj.2S, Xk.2S
        /// </summary>
        public static Vector256<long> MultiplyWideningLowerAndAdd(Vector256<long> addend, Vector128<int> left, Vector128<int> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint64x4_t xvmaddwev_d_wu_u32 (uint64x4_t a, uint32x2_t b, uint32x2_t c)
        ///   LASX: XVMADDWEV.D.WU Xd.4D, Xj.2S, Xk.2S
        /// </summary>
        public static Vector256<ulong> MultiplyWideningLowerAndAdd(Vector256<ulong> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int16x16_t xvmaddwod_h_b_s8 (int16x16_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVMADDWOD.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningUpperAndAdd(Vector256<short> addend, Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint16x16_t xvmaddwod_h_bu_u8 (uint16x16_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVMADDWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningUpperAndAdd(Vector256<ushort> addend, Vector256<byte> left, Vector256<byte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int32x8_t xvmaddwod_w_h_s16 (int32x8_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVMADDWOD.W.H Xd.8S, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningUpperAndAdd(Vector256<int> addend, Vector256<short> left, Vector256<short> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint32x8_t xvmaddwod_w_hu_u16 (uint32x8_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVMADDWOD.W.HU Xd.8S, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningUpperAndAdd(Vector256<uint> addend, Vector256<ushort> left, Vector256<ushort> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int64x4_t xvmaddwod_d_w_s32 (int64x4_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVMADDWOD.D.W Xd.4D, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<long> MultiplyWideningUpperAndAdd(Vector256<long> addend, Vector256<int> left, Vector256<int> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint64x4_t xvmaddwod_d_wu_u32 (uint64x4_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVMADDWOD.D.WU Xd.4D, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<ulong> MultiplyWideningUpperAndAdd(Vector256<ulong> addend, Vector256<uint> left, Vector256<uint> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint8x32_t xvseq_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSEQ.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareEqual(Vector256<sbyte> left, Vector256<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x32_t xvseq_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSEQ.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareEqual(Vector256<byte> left, Vector256<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x16_t xvseq_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSEQ.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareEqual(Vector256<short> left, Vector256<short> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x16_t xvseq_h_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSEQ.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareEqual(Vector256<ushort> left, Vector256<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x8_t xvseq_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSEQ.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareEqual(Vector256<int> left, Vector256<int> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x8_t xvseq_w_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSEQ.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> CompareEqual(Vector256<uint> left, Vector256<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x4_t xvseq_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareEqual(Vector256<long> left, Vector256<long> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x4_t xvseq_d_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x8_t xvfcmp_ceq_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CEQ.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareEqual(Vector256<float> left, Vector256<float> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x4_t xvfcmp_ceq_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CEQ.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareEqual(Vector256<double> left, Vector256<double> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x32_t xvslt_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSLT.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareLessThan(Vector256<sbyte> left, Vector256<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x32_t xvslt_bu_s8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLT.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareLessThan(Vector256<byte> left, Vector256<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x16_t xvslt_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSLT.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareLessThan(Vector256<short> left, Vector256<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x16_t xvslt_hu_s16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLT.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareLessThan(Vector256<ushort> left, Vector256<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x8_t xvslt_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSLT.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareLessThan(Vector256<int> left, Vector256<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x8_t xvslt_wu_s32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLT.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> CompareLessThan(Vector256<uint> left, Vector256<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x4_t xvslt_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareLessThan(Vector256<long> left, Vector256<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x4_t xvslt_du_s64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLT.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareLessThan(Vector256<ulong> left, Vector256<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x8_t xvfcmp_clt_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLT.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareLessThan(Vector256<float> left, Vector256<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x4_t xvfcmp_clt_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareLessThan(Vector256<double> left, Vector256<double> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x32_t xvsle_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSLE.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareLessThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x32_t xvsle_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLE.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareLessThanOrEqual(Vector256<byte> left, Vector256<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x16_t xvsle_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSLE.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareLessThanOrEqual(Vector256<short> left, Vector256<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x16_t xvsle_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLE.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareLessThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvsle_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSLE.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareLessThanOrEqual(Vector256<int> left, Vector256<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvsle_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLE.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> CompareLessThanOrEqual(Vector256<uint> left, Vector256<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvsle_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareLessThanOrEqual(Vector256<long> left, Vector256<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvsle_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLE.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareLessThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvfcmp_cle_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLE.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareLessThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvfcmp_cle_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareLessThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x32_t xvsle_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSLE.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThan(Vector256<sbyte> left, Vector256<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x32_t xvsle_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLE.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareGreaterThan(Vector256<byte> left, Vector256<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x16_t xvsle_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSLE.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareGreaterThan(Vector256<short> left, Vector256<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x16_t xvsle_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLE.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareGreaterThan(Vector256<ushort> left, Vector256<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x8_t xvsle_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSLE.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareGreaterThan(Vector256<int> left, Vector256<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x8_t xvsle_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLE.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> CompareGreaterThan(Vector256<uint> left, Vector256<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x4_t xvsle_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareGreaterThan(Vector256<long> left, Vector256<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x4_t xvsle_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLE.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareGreaterThan(Vector256<ulong> left, Vector256<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x8_t xvfcmp_cle_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLE.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareGreaterThan(Vector256<float> left, Vector256<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x4_t xvfcmp_cle_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLE.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareGreaterThan(Vector256<double> left, Vector256<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x32_t xvslt_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSLT.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> CompareGreaterThanOrEqual(Vector256<sbyte> left, Vector256<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x32_t xvslt_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLT.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> CompareGreaterThanOrEqual(Vector256<byte> left, Vector256<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x16_t xvslt_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSLT.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> CompareGreaterThanOrEqual(Vector256<short> left, Vector256<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x16_t xvslt_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSLT.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> CompareGreaterThanOrEqual(Vector256<ushort> left, Vector256<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvslt_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSLT.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> CompareGreaterThanOrEqual(Vector256<int> left, Vector256<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvslt_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSLT.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> CompareGreaterThanOrEqual(Vector256<uint> left, Vector256<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvslt_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> CompareGreaterThanOrEqual(Vector256<long> left, Vector256<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvslt_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSLT.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> CompareGreaterThanOrEqual(Vector256<ulong> left, Vector256<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x8_t xvfcmp_clt_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFCMP.CLT.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> CompareGreaterThanOrEqual(Vector256<float> left, Vector256<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x4_t xvfcmp_clt_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFCMP.CLT.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> CompareGreaterThanOrEqual(Vector256<double> left, Vector256<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// int8x32_t xvmax_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMAX.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Max(Vector256<sbyte> left, Vector256<sbyte> right) => Max(left, right);

        /// <summary>
        /// uint8x32_t xvmax_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMAX.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Max(Vector256<byte> left, Vector256<byte> right) => Max(left, right);

        /// <summary>
        /// int16x16_t xvmax_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMAX.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Max(Vector256<short> left, Vector256<short> right) => Max(left, right);

        /// <summary>
        /// uint16x16_t xvmax_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMAX.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Max(Vector256<ushort> left, Vector256<ushort> right) => Max(left, right);

        /// <summary>
        /// int32x8_t xvmax_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMAX.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Max(Vector256<int> left, Vector256<int> right) => Max(left, right);

        /// <summary>
        /// uint32x8_t xvmax_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMAX.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Max(Vector256<uint> left, Vector256<uint> right) => Max(left, right);

        /// <summary>
        /// int64x4_t xvmax_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMAX.D Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<long> Max(Vector256<long> left, Vector256<long> right) => Max(left, right);

        /// <summary>
        /// uint64x4_t xvmax_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMAX.DU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<ulong> Max(Vector256<ulong> left, Vector256<ulong> right) => Max(left, right);

        /// <summary>
        /// float32x8_t xvfmax_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFMAX.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Max(Vector256<float> left, Vector256<float> right) => Max(left, right);

        /// <summary>
        /// float64x4_t xvfmax_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFMAX.d Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Max(Vector256<double> left, Vector256<double> right) => Max(left, right);

        /// <summary>
        /// int8x32_t xvmin_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMIN.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Min(Vector256<sbyte> left, Vector256<sbyte> right) => Min(left, right);

        /// <summary>
        /// uint8x32_t xvmin_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMIN.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Min(Vector256<byte> left, Vector256<byte> right) => Min(left, right);

        /// <summary>
        /// int16x16_t xvmin_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMIN.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Min(Vector256<short> left, Vector256<short> right) => Min(left, right);

        /// <summary>
        /// uint16x16_t xvmin_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMIN.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Min(Vector256<ushort> left, Vector256<ushort> right) => Min(left, right);

        /// <summary>
        /// int32x8_t xvmin_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMIN.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Min(Vector256<int> left, Vector256<int> right) => Min(left, right);

        /// <summary>
        /// uint32x8_t xvmin_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMIN.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Min(Vector256<uint> left, Vector256<uint> right) => Min(left, right);

        /// <summary>
        /// int64x4_t xvmin_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMIN.D Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<long> Min(Vector256<long> left, Vector256<long> right) => Min(left, right);

        /// <summary>
        /// uint64x4_t xvmin_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMIN.DU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<ulong> Min(Vector256<ulong> left, Vector256<ulong> right) => Min(left, right);

        /// <summary>
        /// float32x8_t xvfmin_s_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVFMIN.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> Min(Vector256<float> left, Vector256<float> right) => Min(left, right);

        /// <summary>
        /// float64x4_t xvfmin_d_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVFMIN.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> Min(Vector256<double> left, Vector256<double> right) => Min(left, right);

        /// <summary>
        /// int8x32_t xvbitsel_v_s8 (uint8x32_t a, int8x32_t b, int8x32_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> BitwiseSelect(Vector256<sbyte> select, Vector256<sbyte> left, Vector256<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x32_t xvbitsel_v_u8 (uint8x32_t a, uint8x32_t b, uint8x32_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> BitwiseSelect(Vector256<byte> select, Vector256<byte> left, Vector256<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x16_t xvbitsel_v_s16 (uint16x16_t a, int16x16_t b, int16x16_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> BitwiseSelect(Vector256<short> select, Vector256<short> left, Vector256<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x16_t xvbitsel_v_u16 (uint16x16_t a, uint16x16_t b, uint16x16_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> BitwiseSelect(Vector256<ushort> select, Vector256<ushort> left, Vector256<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x8_t xvbitsel_v_s32 (uint32x8_t a, int32x8_t b, int32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> BitwiseSelect(Vector256<int> select, Vector256<int> left, Vector256<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x8_t xvbitsel_v_u32 (uint32x8_t a, uint32x8_t b, uint32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> BitwiseSelect(Vector256<uint> select, Vector256<uint> left, Vector256<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x4_t xvbitsel_v_s64 (uint64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> BitwiseSelect(Vector256<long> select, Vector256<long> left, Vector256<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x4_t xvbitsel_v_u64 (uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> BitwiseSelect(Vector256<ulong> select, Vector256<ulong> left, Vector256<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x8_t xvbitsel_v_f32 (uint32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> BitwiseSelect(Vector256<float> select, Vector256<float> left, Vector256<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x4_t xvbitsel_v_f64 (uint64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVBITSEL.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> BitwiseSelect(Vector256<double> select, Vector256<double> left, Vector256<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x32_t xvabsd_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVABSD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AbsoluteDifference(Vector256<sbyte> left, Vector256<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint8x32_t xvabsd_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVABSD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AbsoluteDifference(Vector256<byte> left, Vector256<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int16x16_t xvabsd_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVABSD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AbsoluteDifference(Vector256<short> left, Vector256<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint16x16_t xvabsd_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVABSD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> AbsoluteDifference(Vector256<ushort> left, Vector256<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int32x8_t xvabsd_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVABSD.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> AbsoluteDifference(Vector256<int> left, Vector256<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint32x8_t xvabsd_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVABSD.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> AbsoluteDifference(Vector256<uint> left, Vector256<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int64x4_t xvabsd_d_s64 (uint64x4_t a, int64x4_t b, int64x4_t c)
        ///   LASX: XVABSD.D Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AbsoluteDifference(Vector256<long> left, Vector256<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint64x4_t xvabsd_du_u64 (uint64x4_t a, uint64x4_t b, uint64x4_t c)
        ///   LASX: XVABSD.DU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AbsoluteDifference(Vector256<ulong> left, Vector256<ulong> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float32x8_t TODO_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: TODO Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> AbsoluteDifference(Vector256<float> left, Vector256<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float64x4_t TODO_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: TODO Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> AbsoluteDifference(Vector256<double> left, Vector256<double> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int8x32_t xvld_s8 (int8_t const * ptr)
        ///   LASX: XVLD Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<sbyte> LoadVector256(sbyte* address) => LoadVector256(address);

        /// <summary>
        /// uint8x32_t xvld_u8 (uint8_t const * ptr)
        ///   LASX: XVLD Xd.32B, Rj, si12
        /// </summary>
        public static unsafe Vector256<byte> LoadVector256(byte* address) => LoadVector256(address);

        /// <summary>
        /// int16x16_t xvld_s16 (int16_t const * ptr)
        ///   LASX: XVLD Xd.16H, Rj, si12
        /// </summary>
        public static unsafe Vector256<short> LoadVector256(short* address) => LoadVector256(address);

        /// <summary>
        /// uint16x16_t xvld_s16 (uint16_t const * ptr)
        ///   LASX: XVLD Xd.16H, Rj, si12
        /// </summary>
        public static unsafe Vector256<ushort> LoadVector256(ushort* address) => LoadVector256(address);

        /// <summary>
        /// int32x8_t xvld_s32 (int32_t const * ptr)
        ///   LASX: XVLD Xd.8S, Rj, si12
        /// </summary>
        public static unsafe Vector256<int> LoadVector256(int* address) => LoadVector256(address);

        /// <summary>
        /// uint32x8_t xvld_s32 (uint32_t const * ptr)
        ///   LASX: XVLD Xd.8S, Rj, si12
        /// </summary>
        public static unsafe Vector256<uint> LoadVector256(uint* address) => LoadVector256(address);

        /// <summary>
        /// int64x4_t xvld_s64 (int64_t const * ptr)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<long> LoadVector256(long* address) => LoadVector256(address);

        /// <summary>
        /// uint64x4_t xvld_u64 (uint64_t const * ptr)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<ulong> LoadVector256(ulong* address) => LoadVector256(address);

        /// <summary>
        /// float32x8_t xvld_f32 (float32_t const * ptr)
        ///   LASX: XVLD Xd.8S, Rj, si12
        /// </summary>
        public static unsafe Vector256<float> LoadVector256(float* address) => LoadVector256(address);

        /// <summary>
        /// float64x4_t xvld_f64 (float64_t const * ptr)
        ///   LASX: XVLD Xd.4D, Rj, si12
        /// </summary>
        public static unsafe Vector256<double> LoadVector256(double* address) => LoadVector256(address);

        /// <summary>
        /// float32x8_t xvfrecip_s_f32 (float32x8_t a)
        ///   LASX: XVFRECIP.S Xd.8S Xj.8S
        /// </summary>
        public static Vector256<float> Reciprocal(Vector256<float> value) => Reciprocal(value);

        /// <summary>
        /// float64x4_t xvfrecip_d_f64 (float64x4_t a)
        ///   LASX: XVFRECIP.D Xd.4D Xj.4D
        /// </summary>
        public static Vector256<double> Reciprocal(Vector256<double> value) => Reciprocal(value);

        /// <summary>
        /// float32x8_t xvfrsqrt_s_f32 (float32x8_t a)
        ///   LASX: XVFRSQRT.S Xd.8S Xj.8S
        /// </summary>
        public static Vector256<float> ReciprocalSqrt(Vector256<float> value) => ReciprocalSqrt(value);

        /// <summary>
        /// float64x4_t xvfrsqrt_d_f64 (float64x4_t a)
        ///   LASX: XVFRSQRT.D Xd.4D Xj.4D
        /// </summary>
        public static Vector256<double> ReciprocalSqrt(Vector256<double> value) => ReciprocalSqrt(value);

        /// <summary>
        /// void xvst_s8 (int8_t * ptr, int8x32_t val)
        ///   LASX: XVST { Xd.32B }, Rj, si12
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector256<sbyte> source) => Store(address, source);

        /// <summary>
        /// void xvst_u8 (uint8_t * ptr, uint8x32_t val)
        ///   LASX: XVST { Xd.32B }, Rj, si12
        /// </summary>
        public static unsafe void Store(byte* address, Vector256<byte> source) => Store(address, source);

        /// <summary>
        /// void xvst_s16 (int16_t * ptr, int16x16_t val)
        ///   LASX: XVST { Xd.16H }, Rj, si12
        /// </summary>
        public static unsafe void Store(short* address, Vector256<short> source) => Store(address, source);

        /// <summary>
        /// void xvst_u16 (uint16_t * ptr, uint16x16_t val)
        ///   LASX: XVST { Xd.16H }, Rj, si12
        /// </summary>
        public static unsafe void Store(ushort* address, Vector256<ushort> source) => Store(address, source);

        /// <summary>
        /// void xvst_s32 (int32_t * ptr, int32x8_t val)
        ///   LASX: XVST { Xd.8S }, Rj, si12
        /// </summary>
        public static unsafe void Store(int* address, Vector256<int> source) => Store(address, source);

        /// <summary>
        /// void xvst_u32 (uint32_t * ptr, uint32x8_t val)
        ///   LASX: XVST { Xd.8S }, Rj, si12
        /// </summary>
        public static unsafe void Store(uint* address, Vector256<uint> source) => Store(address, source);

        /// <summary>
        /// void xvst_s64 (int64_t * ptr, int64x4_t val)
        ///   LASX: XVST { Xd.4D }, Rj, si12
        /// </summary>
        public static unsafe void Store(long* address, Vector256<long> source) => Store(address, source);

        /// <summary>
        /// void xvst_u64 (uint64_t * ptr, uint64x4_t val)
        ///   LASX: XVST { Xd.4D }, Rj, si12
        /// </summary>
        public static unsafe void Store(ulong* address, Vector256<ulong> source) => Store(address, source);

        /// <summary>
        /// void xvst_f32 (float32_t * ptr, float32x8_t val)
        ///   LASX: XVST { Xd.8S }, Rj, si12
        /// </summary>
        public static unsafe void Store(float* address, Vector256<float> source) => Store(address, source);

        /// <summary>
        /// void xvst_f64 (float64_t * ptr, float64x4_t val)
        ///   LASX: XVST { Xd.4D }, Rj, si12
        /// </summary>
        public static unsafe void Store(double* address, Vector256<double> source) => Store(address, source);

        /// <summary>
        /// int8x32_t xvneg_b_s8 (int8x32_t a)
        ///   LASX: XVNEG.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> Negate(Vector256<sbyte> value) => Negate(value);

        /// <summary>
        /// int16x16_t xvneg_h_s16 (int16x16_t a)
        ///   LASX: XVNEG.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> Negate(Vector256<short> value) => Negate(value);

        /// <summary>
        /// int32x8_t xvneg_w_s32 (int32x8_t a)
        ///   LASX: XVNEG.W Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<int> Negate(Vector256<int> value) => Negate(value);

        /// <summary>
        /// int64x4_t xvneg_d_s64 (int64x4_t a)
        ///   LASX: XVNEG.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> Negate(Vector256<long> value) => Negate(value);

        /// <summary>
        /// float32x8_t TODO_f32 (float32x8_t a)
        ///   LASX: TODO Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Negate(Vector256<float> value) => Negate(value);

        /// <summary>
        /// float64x4_t TODO_f64 (float64x4_t a)
        ///   LASX: TODO Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Negate(Vector256<double> value) => Negate(value);

        /// <summary>
        /// float32x8_t xvfmsub_s_f32 (float32x8_t a, float32x8_t b, float32x8_t c)
        ///   LASX: XVFMSUB.S Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<float> FusedMultiplySubtract(Vector256<float> minuend, Vector256<float> left, Vector256<float> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// float64x4_t xvfmsub_d_f64 (float64x4_t a, float64x4_t b, float64x4_t c)
        ///   LASX: XVFMSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<double> FusedMultiplySubtract(Vector256<double> minuend, Vector256<double> left, Vector256<double> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x16_t xvmulwod_h_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMULWOD.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningUpper(Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint16x16_t xvmulwod_h_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMULWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningUpper(Vector256<byte> left, Vector256<byte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int32x8_t xvmulwod_w_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMULWOD.W.H Xd.8S, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningUpper(Vector256<short> left, Vector256<short> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint32x8_t xvmulwod_w_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMULWOD.W.HU Xd.8S, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningUpper(Vector256<ushort> left, Vector256<ushort> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int64x4_t xvmulwod_d_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMULWOD.D.W Xd.4D, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<long> MultiplyWideningUpper(Vector256<int> left, Vector256<int> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint64x4_t xvmulwod_d_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMULWOD.D.WU Xd.4D, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<ulong> MultiplyWideningUpper(Vector256<uint> left, Vector256<uint> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int8x32_t xvssub_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSSUB.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> SubtractSaturate(Vector256<sbyte> left, Vector256<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint8x32_t xvssub_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSSUB.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> SubtractSaturate(Vector256<byte> left, Vector256<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int16x16_t xvssub_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVSSUB.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> SubtractSaturate(Vector256<short> left, Vector256<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint16x16_t xvssub_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVSSUB.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> SubtractSaturate(Vector256<ushort> left, Vector256<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int32x8_t xvssub_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVSSUB.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> SubtractSaturate(Vector256<int> left, Vector256<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint32x8_t xvssub_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVSSUB.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> SubtractSaturate(Vector256<uint> left, Vector256<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int64x4_t xvssub_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVSSUB.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> SubtractSaturate(Vector256<long> left, Vector256<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint64x4_t xvssub_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVSSUB.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> SubtractSaturate(Vector256<ulong> left, Vector256<ulong> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int8x32_t xvavg_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVAVG.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Average(Vector256<sbyte> left, Vector256<sbyte> right) => Average(left, right);

        /// <summary>
        /// uint8x32_t xvavg_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVAVG.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Average(Vector256<byte> left, Vector256<byte> right) => Average(left, right);

        /// <summary>
        /// int16x16_t xvavg_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVAVG.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Average(Vector256<short> left, Vector256<short> right) => Average(left, right);

        /// <summary>
        /// uint16x16_t xvavg_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVAVG.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Average(Vector256<ushort> left, Vector256<ushort> right) => Average(left, right);

        /// <summary>
        /// int32x8_t xvavg_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVAVG.W Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<int> Average(Vector256<int> left, Vector256<int> right) => Average(left, right);

        /// <summary>
        /// uint32x8_t xvavg_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAVG.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Average(Vector256<uint> left, Vector256<uint> right) => Average(left, right);

        /// <summary>
        /// int64x4_t xvavg_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVAVG.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Average(Vector256<long> left, Vector256<long> right) => Average(left, right);

        /// <summary>
        /// uint64x4_t xvavg_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAVG.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Average(Vector256<ulong> left, Vector256<ulong> right) => Average(left, right);

        /// <summary>
        /// int16x16_t xvexth_h_b_s8 (int8x32_t a)
        ///   LASX: XVEXTH.H.B Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> SignExtendWideningUpper(Vector256<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int32x8_t xvexth_w_h_s16 (int16x16_t a)
        ///   LASX: XVEXTH.W.H Xd.8S, Xj.16H
        /// </summary>
        public static Vector256<int> SignExtendWideningUpper(Vector256<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int64x4_t xvexth_d_w_s32 (int32x8_t a)
        ///   LASX: XVEXTH.D.W Xd.4D, Xj.8S
        /// </summary>
        public static Vector256<long> SignExtendWideningUpper(Vector256<int> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// uint16x16_t xvexth_HU_BU_u8 (uint8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> ZeroExtendWideningUpper(Vector256<sbyte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint16x16_t xvexth_HU_BU_u8 (uint8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningUpper(Vector256<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x8_t xvexth_WU_HU_u16 (uint16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8S, Xj.16H
        /// </summary>
        public static Vector256<int> ZeroExtendWideningUpper(Vector256<short> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x8_t xvexth_WU_HU_u16 (uint16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8S, Xj.16H
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningUpper(Vector256<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x4_t xvexth_DU_WU_u32 (uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8S
        /// </summary>
        public static Vector256<long> ZeroExtendWideningUpper(Vector256<int> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x4_t xvexth_DU_WU_u32 (uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8S
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpper(Vector256<uint> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int8x32_t xvand_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> And(Vector256<sbyte> left, Vector256<sbyte> right) => And(left, right);

        /// <summary>
        /// uint8x32_t xvand_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> And(Vector256<byte> left, Vector256<byte> right) => And(left, right);

        /// <summary>
        /// int16x16_t xvand_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> And(Vector256<short> left, Vector256<short> right) => And(left, right);

        /// <summary>
        /// uint16x16_t xvand_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> And(Vector256<ushort> left, Vector256<ushort> right) => And(left, right);

        /// <summary>
        /// int32x8_t xvand_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> And(Vector256<int> left, Vector256<int> right) => And(left, right);

        /// <summary>
        /// uint32x8_t xvand_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> And(Vector256<uint> left, Vector256<uint> right) => And(left, right);

        /// <summary>
        /// int64x4_t xvand_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> And(Vector256<long> left, Vector256<long> right) => And(left, right);

        /// <summary>
        /// uint64x4_t xvand_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> And(Vector256<ulong> left, Vector256<ulong> right) => And(left, right);

        /// <summary>
        /// float32x8_t xvand_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> And(Vector256<float> left, Vector256<float> right) => And(left, right);

        /// <summary>
        /// float64x4_t xvand_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVAND.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> And(Vector256<double> left, Vector256<double> right) => And(left, right);

        /// <summary>
        /// int8x32_t xvandn_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> AndNot(Vector256<sbyte> left, Vector256<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// uint8x32_t xvandn_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> AndNot(Vector256<byte> left, Vector256<byte> right) => AndNot(left, right);

        /// <summary>
        /// int16x16_t xvandn_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> AndNot(Vector256<short> left, Vector256<short> right) => AndNot(left, right);

        /// <summary>
        /// uint16x16_t xvandn_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> AndNot(Vector256<ushort> left, Vector256<ushort> right) => AndNot(left, right);

        /// <summary>
        /// int32x8_t xvandn_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> AndNot(Vector256<int> left, Vector256<int> right) => AndNot(left, right);

        /// <summary>
        /// uint32x8_t xvandn_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> AndNot(Vector256<uint> left, Vector256<uint> right) => AndNot(left, right);

        /// <summary>
        /// int64x4_t xvandn_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> AndNot(Vector256<long> left, Vector256<long> right) => AndNot(left, right);

        /// <summary>
        /// uint64x4_t xvandn_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> AndNot(Vector256<ulong> left, Vector256<ulong> right) => AndNot(left, right);

        /// <summary>
        /// float32x8_t xvandn_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> AndNot(Vector256<float> left, Vector256<float> right) => AndNot(left, right);

        /// <summary>
        /// float64x4_t xvandn_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVANDN.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> AndNot(Vector256<double> left, Vector256<double> right) => AndNot(left, right);

        /// <summary>
        /// int8x32_t xvor_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Or(Vector256<sbyte> left, Vector256<sbyte> right) => Or(left, right);

        /// <summary>
        /// uint8x32_t xvor_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Or(Vector256<byte> left, Vector256<byte> right) => Or(left, right);

        /// <summary>
        /// int16x16_t xvor_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> Or(Vector256<short> left, Vector256<short> right) => Or(left, right);

        /// <summary>
        /// uint16x16_t xvor_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> Or(Vector256<ushort> left, Vector256<ushort> right) => Or(left, right);

        /// <summary>
        /// int32x8_t xvor_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> Or(Vector256<int> left, Vector256<int> right) => Or(left, right);

        /// <summary>
        /// uint32x8_t xvor_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> Or(Vector256<uint> left, Vector256<uint> right) => Or(left, right);

        /// <summary>
        /// int64x4_t xvor_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> Or(Vector256<long> left, Vector256<long> right) => Or(left, right);

        /// <summary>
        /// uint64x4_t xvor_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> Or(Vector256<ulong> left, Vector256<ulong> right) => Or(left, right);

        /// <summary>
        /// float32x8_t xvor_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> Or(Vector256<float> left, Vector256<float> right) => Or(left, right);

        /// <summary>
        /// float64x4_t xvor_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> Or(Vector256<double> left, Vector256<double> right) => Or(left, right);

        /// <summary>
        /// int8x32_t xvxor_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Xor(Vector256<sbyte> left, Vector256<sbyte> right) => Xor(left, right);

        /// <summary>
        /// uint8x32_t xvxor_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Xor(Vector256<byte> left, Vector256<byte> right) => Xor(left, right);

        /// <summary>
        /// int16x16_t xvxor_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> Xor(Vector256<short> left, Vector256<short> right) => Xor(left, right);

        /// <summary>
        /// uint16x16_t xvxor_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> Xor(Vector256<ushort> left, Vector256<ushort> right) => Xor(left, right);

        /// <summary>
        /// int32x8_t xvxor_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<int> Xor(Vector256<int> left, Vector256<int> right) => Xor(left, right);

        /// <summary>
        /// uint32x8_t xvxor_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<uint> Xor(Vector256<uint> left, Vector256<uint> right) => Xor(left, right);

        /// <summary>
        /// int64x4_t xvxor_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<long> Xor(Vector256<long> left, Vector256<long> right) => Xor(left, right);

        /// <summary>
        /// uint64x4_t xvxor_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ulong> Xor(Vector256<ulong> left, Vector256<ulong> right) => Xor(left, right);

        /// <summary>
        /// float32x8_t xvxor_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<float> Xor(Vector256<float> left, Vector256<float> right) => Xor(left, right);

        /// <summary>
        /// float64x4_t xvxor_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVXOR.V Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<double> Xor(Vector256<double> left, Vector256<double> right) => Xor(left, right);

        /// <summary>
        /// int8x32_t xvslli_b_s8 (int8x32_t a, const int n)
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical(Vector256<sbyte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x32_t xvslli_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical(Vector256<byte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int16x16_t xvslli_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint16x16_t xvslli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x8_t xvslli_w_s32 (uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x8_t xvslli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int64x4_t xvslli_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint64x4_t xvslli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x32_t xvsrli_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x32_t xvsrli_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x16_t xvsrli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x16_t xvsrli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x8_t xvsrli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x8_t xvsrli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x4_t xvsrli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x4_t xvsrli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x32_t xvsrlri_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRounded(Vector256<sbyte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x32_t xvsrlri_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRounded(Vector256<byte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x16_t xvsrlri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRounded(Vector256<short> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x16_t xvsrlri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRounded(Vector256<ushort> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x8_t xvsrlri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRounded(Vector256<int> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x8_t xvsrlri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRounded(Vector256<uint> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x4_t xvsrlri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<long> ShiftRightLogicalRounded(Vector256<long> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x4_t xvsrlri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalRounded(Vector256<ulong> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// int8x32_t xvsrai_b_s8 (int8x32_t a, const int n)
        ///   LASX: XVSRAI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int16x16_t xvsrai_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSRAI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int32x8_t xvsrai_w_s32 (int32x8_t a, const int n)
        ///   LASX: XVSRAI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int64x4_t xvsrai_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSRAI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int8x32_t xvsrari_b_s8 (int8x32_t a, const int n)
        ///   LASX: XVSRARI.B Xd.32B, Xj.32B, #n
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRounded(Vector256<sbyte> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int16x16_t xvsrari_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSRARI.H Xd.16H, Xj.16H, #n
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRounded(Vector256<short> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int32x8_t xvsrari_w_s32 (int32x8_t a, const int n)
        ///   LASX: XVSRARI.W Xd.8S, Xj.8S, #n
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRounded(Vector256<int> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int64x4_t xvsrari_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSRARI.D Xd.4D, Xj.4D, #n
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticRounded(Vector256<long> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int8x32_t xvadda_b_s8 (int8x32_t a)
        ///   LASX: XVADDA.B Xd.32B, Xd.32B, 0
        /// </summary>
        public static Vector256<byte> Abs(Vector256<sbyte> value) => Abs(value);

        /// <summary>
        /// int16x16_t xvadda_h_s16 (int16x16_t a)
        ///   LASX: XVADDA.H Xd.16H, Xd.16H, 0
        /// </summary>
        public static Vector256<ushort> Abs(Vector256<short> value) => Abs(value);

        /// <summary>
        /// int32x8_t xvadda_w_s32 (int32x8_t a)
        ///   LASX: XVADDA.W Xd.8S, Xd.8S, 0
        /// </summary>
        public static Vector256<uint> Abs(Vector256<int> value) => Abs(value);

        /// <summary>
        /// int64x4_t xvadda_d_s64 (int64xx_t a)
        ///   LASX: XVADDA.D Xd.4D, Xd.4D, 0
        /// </summary>
        public static Vector256<ulong> Abs(Vector256<long> value) => Abs(value);

        /// <summary>
        /// float32x8_t xvbitclri_w_f32 (float32x8_t a)
        ///   LASX: XVBITCLRI.W Xd.8S, Xd.8S, 31
        /// </summary>
        public static Vector256<float> Abs(Vector256<float> value) => Abs(value);

        /// <summary>
        /// float64x4_t xvbitclri_d_f64 (float64x4_t a)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, 63
        /// </summary>
        public static Vector256<double> Abs(Vector256<double> value) => Abs(value);

        /// <summary>
        /// float32x8_t xvfsqrt_s_f32 (float32x8_t a)
        ///   LASX: XVFSQRT.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Sqrt(Vector256<float> value) => Sqrt(value);

        /// <summary>
        /// float64x4_t xvfsqrt_d_f64 (float64x4_t a)
        ///   LASX: XVFSQRT.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Sqrt(Vector256<double> value) => Sqrt(value);

        /// <summary>
        /// float32x8_t xvfrintrm_s_f32 (float32x8_t a)
        ///   LASX: XVFRINTRM.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Floor(Vector256<float> value) => Floor(value);

        /// <summary>
        /// float64x4_t xvfrintrm_d_f64 (float64x4_t a)
        ///   LASX: XVFRINTRM.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Floor(Vector256<double> value) => Floor(value);

        /// <summary>
        /// float32x8_t xvfrintrp_s_f32 (float32x8_t a)
        ///   LASX: XVFRINTRP.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> Ceiling(Vector256<float> value) => Ceiling(value);

        /// <summary>
        /// float64x4_t xvfrintrp_d_f64 (float64x4_t a)
        ///   LASX: XVFRINTRP.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> Ceiling(Vector256<double> value) => Ceiling(value);

        /// <summary>
        /// float32x8_t xvfrintrz_s_f32 (float32x8_t a)
        ///   LASX: XVFRINTRZ.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToZero(Vector256<float> value) => RoundToZero(value);

        /// <summary>
        /// float64x4_t xvfrintrz_d_f64 (float64x4_t a)
        ///   LASX: XVFRINTRZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToZero(Vector256<double> value) => RoundToZero(value);

        /// <summary>
        /// float32x8_t xvfrintrm_s_f32 (float32x8_t a)
        ///   LASX: XVFRINTRM.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToNegativeInfinity(Vector256<float> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float64x4_t xvfrintrm_d_f64 (float64x4_t a)
        ///   LASX: XVFRINTRM.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToNegativeInfinity(Vector256<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float32x8_t xvfrintrp_s_f32 (float32x8_t a)
        ///   LASX: XVFRINTRP.S Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> RoundToPositiveInfinity(Vector256<float> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// float64x4_t xvfrintrp_d_f64 (float64x4_t a)
        ///   LASX: XVFRINTRP.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> RoundToPositiveInfinity(Vector256<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// int8x32_t TODO_s8 (int8_t a, int8x32_t v, const int imm)
        ///   LASX: TODO Xd.B[imm], Rj, imm
        /// </summary>
        public static Vector256<sbyte> Insert(Vector256<sbyte> vector, byte index, sbyte data) => Insert(vector, index, data);

        /// <summary>
        /// uint8x32_t TODO_u8 (uint8_t a, uint8x32_t v, const int imm)
        ///   LASX: TODO Xd.B[imm], Rj, imm
        /// </summary>
        public static Vector256<byte> Insert(Vector256<byte> vector, byte index, byte data) => Insert(vector, index, data);

        /// <summary>
        /// int16x16_t TODO_s16 (int16_t a, int16x16_t v, const int imm)
        ///   LASX: TODO Xd.H[imm], Rj, imm
        /// </summary>
        public static Vector256<short> Insert(Vector256<short> vector, byte index, short data) => Insert(vector, index, data);

        /// <summary>
        /// uint16x16_t TODO_u16 (uint16_t a, uint16x16_t v, const int imm)
        ///   LASX: TODO Xd.H[imm], Rj, imm
        /// </summary>
        public static Vector256<ushort> Insert(Vector256<ushort> vector, byte index, ushort data) => Insert(vector, index, data);

        /// <summary>
        /// int32x8_t xvinsgr2vr_w_s32 (int32_t a, int32x8_t v, const int imm)
        ///   LASX: XVINSGR2VR.W Xd.S[imm], Rj, imm
        /// </summary>
        public static Vector256<int> Insert(Vector256<int> vector, byte index, int data) => Insert(vector, index, data);

        /// <summary>
        /// uint32x8_t xvinsgr2vr_w_u32 (uint32_t a, uint32x8_t v, const int imm)
        ///   LASX: XVINSGR2VR.W Xd.S[imm], Rj, imm
        /// </summary>
        public static Vector256<uint> Insert(Vector256<uint> vector, byte index, uint data) => Insert(vector, index, data);

        /// <summary>
        /// int64x4_t xvinsgr2vr_d_s64 (int64_t a, int64x4_t v, const int imm)
        ///   LASX: XVINSGR2VR.D Xd.D[imm], Rj, imm
        /// </summary>
        public static Vector256<long> Insert(Vector256<long> vector, byte index, long data) => Insert(vector, index, data);

        /// <summary>
        /// uint64x4_t xvinsgr2vr_d_u64 (uint64_t a, uint64x4_t v, const int imm)
        ///   LASX: XVINSGR2VR.D Xd.D[imm], Rj, imm
        /// </summary>
        public static Vector256<ulong> Insert(Vector256<ulong> vector, byte index, ulong data) => Insert(vector, index, data);

        /// <summary>
        /// float32x8_t xvinsve0_w_f32 (float32_t a, float32x8_t v, const int imm)
        ///   LASX: XVINSVE0.W Xd.S[imm], Xj.S[0], imm
        /// </summary>
        public static Vector256<float> Insert(Vector256<float> vector, byte index, float data) => Insert(vector, index, data);

        /// <summary>
        /// float64x4_t xvinsve0_d_f64 (float64_t a, float64x4_t v, const int imm)
        ///   LASX: XVINSVE0.D Xd.D[imm], Xj.D[0], imm
        /// </summary>
        public static Vector256<double> Insert(Vector256<double> vector, byte index, double data) => Insert(vector, index, data);

        /// <summary>
        /// int8x32_t xvreplgr2vr_b_s8 (int8_t value)
        ///   LASX: XVREPLGR2VR.B Xd.32B, Rj
        /// </summary>
        public static Vector256<sbyte> DuplicateToVector256(sbyte value) => DuplicateToVector256(value);

        /// <summary>
        /// uint8x32_t xvreplgr2vr_b_u8 (uint8_t value)
        ///   LASX: XVREPLGR2VR.B Xd.32B, Rj
        /// </summary>
        public static Vector256<byte> DuplicateToVector256(byte value) => DuplicateToVector256(value);

        /// <summary>
        /// int16x16_t xvreplgr2vr_h_s16 (int16_t value)
        ///   LASX: XVREPLGR2VR.H Xd.16H, Rj
        /// </summary>
        public static Vector256<short> DuplicateToVector256(short value) => DuplicateToVector256(value);

        /// <summary>
        /// uint16x16_t xvreplgr2vr_h_u16 (uint16_t value)
        ///   LASX: XVREPLGR2VR.H Xd.16H, Rj
        /// </summary>
        public static Vector256<ushort> DuplicateToVector256(ushort value) => DuplicateToVector256(value);

        /// <summary>
        /// int32x8_t xvreplgr2vr_w_s32 (int32_t value)
        ///   LASX: XVREPLGR2VR.W Xd.8S, Rj
        /// </summary>
        public static Vector256<int> DuplicateToVector256(int value) => DuplicateToVector256(value);

        /// <summary>
        /// uint32x8_t xvreplgr2vr_w_u32 (uint32_t value)
        ///   LASX: XVREPLGR2VR.W Xd.8S, Rj
        /// </summary>
        public static Vector256<uint> DuplicateToVector256(uint value) => DuplicateToVector256(value);

        /// <summary>
        /// int64x4_t xvreplgr2vr_d_s64 (int64_t value)
        ///   LASX: XVREPLGR2VR.D Xd.4D, Rj
        /// </summary>
        public static Vector256<long> DuplicateToVector256(long value) => DuplicateToVector256(value);

        /// <summary>
        /// uint64x4_t xvreplgr2vr_d_u64 (uint64_t value)
        ///   LASX: XVREPLGR2VR.D Xd.4D, Rj
        /// </summary>
        public static Vector256<ulong> DuplicateToVector256(ulong value) => DuplicateToVector256(value);

        /// <summary>
        /// float32x8_t xvreplve0_w_f32 (float32_t value)
        ///   LASX: XVREPLVE0.W Xd.8S, Xj.S[0]
        /// </summary>
        public static Vector256<float> DuplicateToVector256(float value) => DuplicateToVector256(value);

        /// <summary>
        /// float64x4_t xvreplve0_d_f64 (float64_t value)
        ///   LASX: XVREPLVE0.D Xd.4D, Xj.D[0]
        /// </summary>
        public static Vector256<double> DuplicateToVector256(double value) => DuplicateToVector256(value);

        /// <summary>
        /// float32x8_t xvffint_s_w_f32_s32 (int32x8_t a)
        ///   LASX: XVFFINT.S.W Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> ConvertToSingle(Vector256<int> value) => ConvertToSingle(value);

        /// <summary>
        /// float32x8_t xvffint_s_wu_f32_u32 (uint32x8_t a)
        ///   LASX: XVFFINT.S.WU Xd.8S, Xj.8S
        /// </summary>
        public static Vector256<float> ConvertToSingle(Vector256<uint> value) => ConvertToSingle(value);

        /// <summary>
        /// float64x4_t xvffint_d_l_f64_s64 (int64x4_t a)
        ///   LASX: XVFFINT.D.L Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> ConvertToDouble(Vector256<long> value) => ConvertToDouble(value);

        /// <summary>
        /// float64x4_t xvffint_d_lu_f64_u64 (uint64x4_t a)
        ///   LASX: XVFFINT.D.LU Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<double> ConvertToDouble(Vector256<ulong> value) => ConvertToDouble(value);

        /// <summary>
        /// bool xvsetnez_v_u8 (uint8x32_t value)
        ///   LASX: XVSETNEZ.V cd, Xj.32B
        /// </summary>
        public static bool HasElementsNotZero(Vector256<byte> value) => HasElementsNotZero(value);

        /// <summary>
        /// bool xvseteqz_v_u8 (uint8x32_t value)
        ///   LASX: XVSETEQZ.V cd, Xj.32B
        /// </summary>
        public static bool AllElementsIsZero(Vector256<byte> value) => AllElementsIsZero(value);

        /// <summary>
        /// bool xvsetallnez_b_s8 (int8x32_t value)
        ///   LASX: XVSETALLNEZ.B cd, Xj.32B
        /// </summary>
        public static bool AllElementsNotZero(Vector256<sbyte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_b_u8 (uint8x32_t value)
        ///   LASX: XVSETALLNEZ.B cd, Xj.32B
        /// </summary>
        public static bool AllElementsNotZero(Vector256<byte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_h_s16 (int16x16_t value)
        ///   LASX: XVSETALLNEZ.H cd, Xj.16H
        /// </summary>
        public static bool AllElementsNotZero(Vector256<short> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_h_u16 (uint16x16_t value)
        ///   LASX: XVSETALLNEZ.H cd, Xj.16H
        /// </summary>
        public static bool AllElementsNotZero(Vector256<ushort> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_w_s32 (int32x8_t value)
        ///   LASX: XVSETALLNEZ.W cd, Xj.8W
        /// </summary>
        public static bool AllElementsNotZero(Vector256<int> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_w_u32 (uint32x8_t value)
        ///   LASX: XVSETALLNEZ.W cd, Xj.8W
        /// </summary>
        public static bool AllElementsNotZero(Vector256<uint> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_w_s64 (int64x8_t value)
        ///   LASX: XVSETALLNEZ.D cd, Xj.4D
        /// </summary>
        public static bool AllElementsNotZero(Vector256<long> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetallnez_w_u64 (uint64x8_t value)
        ///   LASX: XVSETALLNEZ.D cd, Xj.4D
        /// </summary>
        public static bool AllElementsNotZero(Vector256<ulong> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool xvsetanyeqz_b_s8 (int8x32_t value)
        ///   LASX: XVSETANYEQZ.B cd, Xj.32B
        /// </summary>
        public static bool HasElementsIsZero(Vector256<sbyte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_b_u8 (uint8x32_t value)
        ///   LASX: XVSETANYEQZ.B cd, Xj.32B
        /// </summary>
        public static bool HasElementsIsZero(Vector256<byte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_h_s16 (int16x16_t value)
        ///   LASX: XVSETANYEQZ.H cd, Xj.16H
        /// </summary>
        public static bool HasElementsIsZero(Vector256<short> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_h_u16 (uint16x16_t value)
        ///   LASX: XVSETANYEQZ.H cd, Xj.16H
        /// </summary>
        public static bool HasElementsIsZero(Vector256<ushort> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_w_s32 (int32x8_t value)
        ///   LASX: XVSETANYEQZ.W cd, Xj.8W
        /// </summary>
        public static bool HasElementsIsZero(Vector256<int> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_w_u32 (uint32x8_t value)
        ///   LASX: XVSETANYEQZ.W cd, Xj.8W
        /// </summary>
        public static bool HasElementsIsZero(Vector256<uint> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_w_s64 (int64x8_t value)
        ///   LASX: XVSETANYEQZ.D cd, Xj.4D
        /// </summary>
        public static bool HasElementsIsZero(Vector256<long> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool xvsetanyeqz_w_u64 (uint64x8_t value)
        ///   LASX: XVSETANYEQZ.D cd, Xj.4D
        /// </summary>
        public static bool HasElementsIsZero(Vector256<ulong> value) => HasElementsIsZero(value);

        /// <summary>
        /// int8x32_t xvpcnt_b_s8 (int8x32_t a)
        ///   LASX: XVPCNT_B Xd, Xj
        /// </summary>
        public static Vector256<sbyte> PopCount(Vector256<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x32_t xvpcnt_b_u8 (uint8x32_t a)
        ///   LASX: XVPCNT_B Xd, Xj
        /// </summary>
        public static Vector256<byte> PopCount(Vector256<byte> value) => PopCount(value);

        /// <summary>
        /// int16x16_t xvpcnt_h_s16 (int16x16_t a)
        ///   LASX: XVPCNT_H Xd, Xj
        /// </summary>
        public static Vector256<short> PopCount(Vector256<short> value) => PopCount(value);

        /// <summary>
        /// uint16x16_t xvpcnt_h_u16 (uint16x16_t a)
        ///   LASX: XVPCNT_H Xd, Xj
        /// </summary>
        public static Vector256<ushort> PopCount(Vector256<ushort> value) => PopCount(value);

        /// <summary>
        /// int32x8_t xvpcnt_w_s32 (int32x8_t a)
        ///   LASX: XVPCNT_W Xd, Xj
        /// </summary>
        public static Vector256<int> PopCount(Vector256<int> value) => PopCount(value);

        /// <summary>
        /// uint32x8_t xvpcnt_w_u32 (uint32x8_t a)
        ///   LASX: XVPCNT_W Xd, Xj
        /// </summary>
        public static Vector256<uint> PopCount(Vector256<uint> value) => PopCount(value);

        /// <summary>
        /// int64x4_t xvpcnt_d_s64 (int64x4_t a)
        ///   LASX: XVPCNT_D Xd, Xj
        /// </summary>
        public static Vector256<long> PopCount(Vector256<long> value) => PopCount(value);

        /// <summary>
        /// uint64x4_t xvpcnt_d_u64 (uint64x4_t a)
        ///   LASX: XVPCNT_D Xd, Xj
        /// </summary>
        public static Vector256<ulong> PopCount(Vector256<ulong> value) => PopCount(value);

        /// <summary>
        ///  uint8x32_t xvrepl128vei_u8(uint8x32_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_B Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<byte> VectorElementReplicate(Vector256<byte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int8x32_t xvrepl128vei_s8(int8x32_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_B Xd.32B, Xj.32B, ui4
        /// </summary>
        public static Vector256<sbyte> VectorElementReplicate(Vector256<sbyte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int16x16_t xvrepl128vei_s16(int16x16_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEI_H Xd.16H, Xj.16H, ui3
        /// </summary>
        public static Vector256<short> VectorElementReplicate(Vector256<short> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint16x16_t xvrepl128vei_u16(uint16x16_t vector, uint8_t idx)
        ///   LASX: XVREPLVEI_H Xd.16H, Xj.16H, ui3
        /// </summary>
        public static Vector256<ushort> VectorElementReplicate(Vector256<ushort> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int32x8_t xvrepl128vei_s32(int32x8_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_W Xd.8W, Xj.8W, ui2
        /// </summary>
        public static Vector256<int> VectorElementReplicate(Vector256<int> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint32x8_t xvrepl128vei_u32(uint32x8_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_W Xd.8W, Xj.8W, ui2
        /// </summary>
        public static Vector256<uint> VectorElementReplicate(Vector256<uint> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int64x4_t xvrepl128vei_s64(int64x4_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_D Xd.4D, Xj.4D, ui1
        /// </summary>
        public static Vector256<long> VectorElementReplicate(Vector256<long> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint64x4_t xvrepl128vei_u64(uint64x4_t vector, uint8_t idx)
        ///   LASX: XVREPL128VEII_D Xd.4D, Xj.4D, ui1
        /// </summary>
        public static Vector256<ulong> VectorElementReplicate(Vector256<ulong> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        // TODO:----------------------------------
    }
}

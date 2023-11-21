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
        /// uint8x32_t xvmul_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMUL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Multiply(Vector256<byte> left, Vector256<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x16_t xvmul_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMUL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Multiply(Vector256<short> left, Vector256<short> right) => Multiply(left, right);

        /// <summary>
        /// uint16x16_t xvmul_h_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMUL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Multiply(Vector256<ushort> left, Vector256<ushort> right) => Multiply(left, right);

        /// <summary>
        /// int32x8_t xvmul_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMULW Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Multiply(Vector256<int> left, Vector256<int> right) => Multiply(left, right);

        /// <summary>
        /// uint32x8_t xvmul_w_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMUL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Multiply(Vector256<uint> left, Vector256<uint> right) => Multiply(left, right);

        /// <summary>
        /// int64x4_t xvmul_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMUL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Multiply(Vector256<long> left, Vector256<long> right) => Multiply(left, right);

        /// <summary>
        /// uint64x4_t xvmul_d_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMUL.D Xd.4D, Xj.4D, Xk.4D
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
        /// int8x32_t xvmuh_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMUH.B Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> MultiplyHight(Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint8x32_t xvmuh_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMUH.BU Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> MultiplyHight(Vector256<byte> left, Vector256<byte> right) => MultiplyHight(left, right);

        /// <summary>
        /// int16x16_t xvmuh_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMUH.H Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> MultiplyHight(Vector256<short> left, Vector256<short> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint16x16_t xvmuh_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMUH.HU Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> MultiplyHight(Vector256<ushort> left, Vector256<ushort> right) => MultiplyHight(left, right);

        /// <summary>
        /// int32x8_t xvmuh_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: VMUL.W Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> MultiplyHight(Vector256<int> left, Vector256<int> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint32x8_t xvmuh_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMUH.WU Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> MultiplyHight(Vector256<uint> left, Vector256<uint> right) => MultiplyHight(left, right);

        /// <summary>
        /// int64x4_t xvmuh_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMUH.D Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> MultiplyHight(Vector256<long> left, Vector256<long> right) => MultiplyHight(left, right);

        /// <summary>
        /// uint64x4_t xvmuh_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMUH.DU Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyHight(Vector256<ulong> left, Vector256<ulong> right) => MultiplyHight(left, right);

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
        ///   LASX: XVDIV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Divide(Vector256<int> left, Vector256<int> right) => Divide(left, right);

        /// <summary>
        /// uint32x8_t xvdiv_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVDIV.WU Xd.8W, Xj.8W, Xk.8W
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
        /// int8x32_t xvmod_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMOD.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> Modulo(Vector256<sbyte> left, Vector256<sbyte> right) => Modulo(left, right);

        /// <summary>
        /// uint8x32_t xvmod_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMOD.BU Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> Modulo(Vector256<byte> left, Vector256<byte> right) => Modulo(left, right);

        /// <summary>
        /// int16x16_t xvmod_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMOD.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> Modulo(Vector256<short> left, Vector256<short> right) => Modulo(left, right);

        /// <summary>
        /// uint16x16_t xvmod_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMOD.HU Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> Modulo(Vector256<ushort> left, Vector256<ushort> right) => Modulo(left, right);

        /// <summary>
        /// int32x8_t xvmod_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMOD.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> Modulo(Vector256<int> left, Vector256<int> right) => Modulo(left, right);

        /// <summary>
        /// uint32x8_t xvmod_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMOD.WU Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> Modulo(Vector256<uint> left, Vector256<uint> right) => Modulo(left, right);

        /// <summary>
        /// int64x4_t xvmod_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMOD.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Modulo(Vector256<long> left, Vector256<long> right) => Modulo(left, right);

        /// <summary>
        /// uint64x4_t xvmod_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMOD.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Modulo(Vector256<ulong> left, Vector256<ulong> right) => Modulo(left, right);

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
        ///   LASX: XVST { Xd.8W }, Rj, si12
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
        ///   LASX: XVNEG.W Xd.8W, Xj.8W
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
        public static Vector256<short> MultiplyWideningOdd(Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int32x8_t xvmulwod_w_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMULWOD.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningOdd(Vector256<short> left, Vector256<short> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int64x4_t xvmulwod_d_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMULWOD.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<int> left, Vector256<int> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int128x2_t xvmulwod_q_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMULWOD.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<long> left, Vector256<long> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int16x16_t xvmulwev_h_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVMULWEV.H.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningEven(Vector256<sbyte> left, Vector256<sbyte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int32x8_t xvmulwev_w_h_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVMULWEV.W.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningEven(Vector256<short> left, Vector256<short> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int64x4_t xvmulwev_d_w_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVMULWEV.D.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<int> left, Vector256<int> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int128x2_t xvmulwev_q_d_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVMULWEV.Q.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<long> left, Vector256<long> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint16x16_t xvmulwod_hu_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMULWOD.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningOdd(Vector256<byte> left, Vector256<byte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint32x8_t xvmulwod_wu_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMULWOD.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningOdd(Vector256<ushort> left, Vector256<ushort> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint64x4_t xvmulwod_du_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMULWOD.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> MultiplyWideningOdd(Vector256<uint> left, Vector256<uint> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint128x2_t xvmulwod_qu_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMULWOD.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyWideningOdd(Vector256<ulong> left, Vector256<ulong> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// uint16x16_t xvmulwev_hu_bu_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVMULWEV.H.BU Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<ushort> MultiplyWideningEven(Vector256<byte> left, Vector256<byte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint32x8_t xvmulwev_wu_hu_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVMULWEV.W.HU Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<uint> MultiplyWideningEven(Vector256<ushort> left, Vector256<ushort> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint64x4_t xvmulwev_du_wu_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVMULWEV.D.WU Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<ulong> MultiplyWideningEven(Vector256<uint> left, Vector256<uint> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// uint128x2_t xvmulwev_qu_du_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVMULWEV.Q.DU Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> MultiplyWideningEven(Vector256<ulong> left, Vector256<ulong> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int16x16_t xvmulwod_h_bu_s8 (uint8x32_t a, int8x32_t b)
        ///   LASX: XVMULWOD.H.BU.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningOdd(Vector256<byte> left, Vector256<sbyte> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int32x8_t xvmulwod_w_hu_s16 (uint16x16_t a, int16x16_t b)
        ///   LASX: XVMULWOD.W.HU.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningOdd(Vector256<ushort> left, Vector256<short> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int64x4_t xvmulwod_d_wu_s32 (uint32x8_t a, int32x8_t b)
        ///   LASX: XVMULWOD.D.WU.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<uint> left, Vector256<int> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int128x2_t xvmulwod_q_du_s64 (uint64x4_t a, int64x4_t b)
        ///   LASX: XVMULWOD.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningOdd(Vector256<ulong> left, Vector256<long> right) => MultiplyWideningOdd(left, right);

        /// <summary>
        /// int16x16_t xvmulwev_h_bu_s8 (uint8x32_t a, int8x32_t b)
        ///   LASX: XVMULWEV.H.BU.B Xd.16H, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<short> MultiplyWideningEven(Vector256<byte> left, Vector256<sbyte> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int32x8_t xvmulwev_w_hu_s16 (uint16x16_t a, int16x16_t b)
        ///   LASX: XVMULWEV.W.HU.H Xd.8W, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<int> MultiplyWideningEven(Vector256<ushort> left, Vector256<short> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int64x4_t xvmulwev_d_wu_s32 (uint32x8_t a, int32x8_t b)
        ///   LASX: XVMULWEV.D.WU.W Xd.4D, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<uint> left, Vector256<int> right) => MultiplyWideningEven(left, right);

        /// <summary>
        /// int128x2_t xvmulwev_q_du_s64 (uint64x4_t a, int64x4_t b)
        ///   LASX: XVMULWEV.Q.DU.D Xd.2Q, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> MultiplyWideningEven(Vector256<ulong> left, Vector256<long> right) => MultiplyWideningEven(left, right);

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
        /// uint32x8_t xvavg_wu_u32(uint32x8_t a, uint32x8_t b)
        ///   LASX: XVAVG.WU Xd.8S, Xj.8S, Xk.8S
        /// </summary>
        public static Vector256<uint> Average(Vector256<uint> left, Vector256<uint> right) => Average(left, right);

        /// <summary>
        /// int64x4_t xvavg_d_s64(int64x4_t a, int64x4_t b)
        ///   LASX: XVAVG.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> Average(Vector256<long> left, Vector256<long> right) => Average(left, right);

        /// <summary>
        /// uint64x4_t xvavg_du_u64(uint64x4_t a, uint64x4_t b)
        ///   LASX: XVAVG.DU Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> Average(Vector256<ulong> left, Vector256<ulong> right) => Average(left, right);

        /// <summary>
        /// int16x16_t xvsllwil_h_b_s8(int8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.H.B Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> SignExtendWideningLowerAndShiftLeftEach128(Vector256<sbyte> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int16x16_t xvsllwil_h_b_u8(uint8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.H.B Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> SignExtendWideningLowerAndShiftLeftEach128(Vector256<byte> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int32x8_t xvsllwil_w_h_s16(int16x4_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.W.H Xd.8W, Xj.4H, ui4
        /// </summary>
        public static Vector256<int> SignExtendWideningLowerAndShiftLeftEach128(Vector256<short> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int32x8_t xvsllwil_w_h_u16(uint16x4_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.W.H Xd.8W, Xj.4H, ui4
        /// </summary>
        public static Vector256<int> SignExtendWideningLowerAndShiftLeftEach128(Vector256<ushort> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int64x4_t xvsllwil_d_w_s32(int32x2_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.D.W Xd.4D, Xj.2W, ui5
        /// </summary>
        public static Vector256<long> SignExtendWideningLowerAndShiftLeftEach128(Vector256<int> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int64x4_t xvsllwil_d_w_u32(uint32x2_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.D.W Xd.4D, Xj.2W, ui5
        /// </summary>
        public static Vector256<long> SignExtendWideningLowerAndShiftLeftEach128(Vector256<uint> value, byte shift) => SignExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// uint16x16_t xvsllwil_hu_bu_u8(uint8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.HU.BU Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<byte> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int16x16_t xvsllwil_hu_bu_s8(int8x32_t a, uint8_t ui3)
        ///   LASX: XVSLLWIL.HU.BU Xd.16H, Xj.32B, ui3
        /// </summary>
        public static Vector256<short> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<sbyte> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// uint32x8_t xvsllwil_wu_hu_u16(uint16x16_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.WU.HU Xd.8W, Xj.16H, ui4
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<ushort> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int32x8_t xvsllwil_wu_hu_s16(int16x16_t a, uint8_t ui4)
        ///   LASX: XVSLLWIL.WU.HU Xd.8W, Xj.16H, ui4
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<short> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// uint64x4_t xvsllwil_du_wu_u32(uint32x8_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.DU.WU Xd.4D, Xj.8W, ui5
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<uint> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// int64x4_t xvsllwil_du_wu_s32(int32x8_t a, uint8_t ui5)
        ///   LASX: XVSLLWIL.DU.WU Xd.4D, Xj.8W, ui5
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLowerAndShiftLeftEach128(Vector256<int> value, byte shift) => ZeroExtendWideningLowerAndShiftLeftEach128(value, shift);

        /// <summary>
        /// uint128x2_t xvextl_qu_du_s64(int64x4_t a)
        ///   LASX: XVEXTL.QU.DU Xd.2Q, Xj.D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerEach128(Vector256<long> value) => ZeroExtendWideningLowerEach128(value);

        /// <summary>
        /// uint128x2_t xvextl_qu_du_u64(uint64x4_t a)
        ///   LASX: XVEXTL.QU.DU Xd.2Q, Xj.D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLowerEach128(Vector256<ulong> value) => ZeroExtendWideningLowerEach128(value);

        /// <summary>
        /// int16x16_t vext2xv_h_b_s8(int8x16_t a)
        ///   LASX: VEXT2XV.H.B Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> SignExtendWideningLower(Vector128<sbyte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int16x16_t vext2xv_h_b_u8(uint8x16_t a)
        ///   LASX: VEXT2XV.H.B Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> SignExtendWideningLower(Vector128<byte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_w_b_s8(int8x8_t a)
        ///   LASX: VEXT2XV.W.B Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector64<sbyte> value, byte shift) => SignExtendWideningLower(value, shift);
        //public static Vector256<int> SignExtendWideningLower(Vector128<sbyte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_w_b_u8(uint8x8_t a)
        ///   LASX: VEXT2XV.W.B Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector64<byte> value, byte shift) => SignExtendWideningLower(value, shift);
        //public static Vector256<int> SignExtendWideningLower(Vector128<byte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_b_s8(int8x4_t a)
        ///   LASX: VEXT2XV.D.B Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<sbyte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_b_u8(uint8x4_t a)
        ///   LASX: VEXT2XV.D.B Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<byte> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_w_h_s16(int16x8_t a)
        ///   LASX: VEXT2XV.W.H Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector128<short> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_w_h_u16(uint16x8_t a)
        ///   LASX: VEXT2XV.W.H Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> SignExtendWideningLower(Vector128<ushort> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_h_u16(int16x4_t a)
        ///   LASX: VEXT2XV.D.H Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<short> value, byte shift) => SignExtendWideningLower(value, shift);
        //public static Vector256<long> SignExtendWideningLower(Vector128<short> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_h_u16(uint16x4_t a)
        ///   LASX: VEXT2XV.D.H Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector64<ushort> value, byte shift) => SignExtendWideningLower(value, shift);
        //public static Vector256<long> SignExtendWideningLower(Vector128<ushort> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_w_s32(int32x2_t a)
        ///   LASX: VEXT2XV.D.W Xd.4D, Xj.2W
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector128<int> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_d_w_u32(uint32x2_t a)
        ///   LASX: VEXT2XV.D.W Xd.4D, Xj.2W
        /// </summary>
        public static Vector256<long> SignExtendWideningLower(Vector128<uint> value, byte shift) => SignExtendWideningLower(value, shift);

        /// <summary>
        /// uint16x16_t vext2xv_hu_bu_u8(uint8x16_t a)
        ///   LASX: VEXT2XV.HU.BU Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningLower(Vector128<byte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int16x16_t vext2xv_hu_bu_s8(int8x16_t a)
        ///   LASX: VEXT2XV.HU.BU Xd.16H, Xj.16B
        /// </summary>
        public static Vector256<short> ZeroExtendWideningLower(Vector128<sbyte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// uint32x8_t vext2xv_wu_bu_u8(uint8x8_t a)
        ///   LASX: VEXT2XV.WU.BU Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLower(Vector64<byte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_wu_bu_s8(int8x8_t a)
        ///   LASX: VEXT2XV.WU.BU Xd.8W, Xj.8B
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLower(Vector64<sbyte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// uint64x4_t vext2xv_du_bu_u8(uint8x4_t a)
        ///   LASX: VEXT2XV.DU.BU Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector64<byte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_du_bu_s8(int8x4_t a)
        ///   LASX: VEXT2XV.DU.BU Xd.4D, Xj.4B
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector64<sbyte> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// uint32x8_t vext2xv_wu_hu_u16(uint16x8_t a)
        ///   LASX: VEXT2XV.WU.HU Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningLower(Vector128<ushort> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int32x8_t vext2xv_wu_hu_s16(int16x8_t a)
        ///   LASX: VEXT2XV.WU.HU Xd.8W, Xj.8H
        /// </summary>
        public static Vector256<int> ZeroExtendWideningLower(Vector128<short> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// uint64x4_t vext2xv_du_hu_u16(uint16x4_t a)
        ///   LASX: VEXT2XV.DU.HU Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector64<ushort> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_du_hu_s16(int16x4_t a)
        ///   LASX: VEXT2XV.DU.HU Xd.4D, Xj.4H
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector64<short> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// uint64x4_t vext2xv_du_wu_u32(uint32x4_t a)
        ///   LASX: VEXT2XV.DU.WU Xd.4D, Xj.4W
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningLower(Vector128<uint> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int64x4_t vext2xv_du_wu_s32(int32x4_t a)
        ///   LASX: VEXT2XV.DU.WU Xd.4D, Xj.4W
        /// </summary>
        public static Vector256<long> ZeroExtendWideningLower(Vector128<int> value, byte shift) => ZeroExtendWideningLower(value, shift);

        /// <summary>
        /// int16x16_t xvexth_h_b_s8(int8x32_t a)
        ///   LASX: XVEXTH.H.B Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> SignExtendWideningUpperEach128(Vector256<sbyte> value) => SignExtendWideningUpperEach128(value);

        /// <summary>
        /// int32x8_t xvexth_w_h_s16(int16x16_t a)
        ///   LASX: XVEXTH.W.H Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<int> SignExtendWideningUpperEach128(Vector256<short> value) => SignExtendWideningUpperEach128(value);

        /// <summary>
        /// int64x4_t xvexth_d_w_s32(int32x8_t a)
        ///   LASX: XVEXTH.D.W Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<long> SignExtendWideningUpperEach128(Vector256<int> value) => SignExtendWideningUpperEach128(value);

        /// <summary>
        /// int128x2_t xvexth_d_w_s64(int64x4_t a)
        ///   LASX: XVEXTH.Q.D Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<long> SignExtendWideningUpperEach128(Vector256<long> value) => SignExtendWideningUpperEach128(value);

        /// <summary>
        /// int16x16_t xvexth_HU_BU_s8(int8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<short> ZeroExtendWideningUpperEach128(Vector256<sbyte> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// uint16x16_t xvexth_HU_BU_u8(uint8x32_t a)
        ///   LASX: XVEXTH.HU.BU Xd.16H, Xj.32B
        /// </summary>
        public static Vector256<ushort> ZeroExtendWideningUpperEach128(Vector256<byte> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// int32x8_t xvexth_WU_HU_s16(int16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<int> ZeroExtendWideningUpperEach128(Vector256<short> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// uint32x8_t xvexth_WU_HU_u16(uint16x16_t a)
        ///   LASX: XVEXTH.WU.HU Xd.8W, Xj.16H
        /// </summary>
        public static Vector256<uint> ZeroExtendWideningUpperEach128(Vector256<ushort> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// int64x4_t xvexth_DU_WU_s32(uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<long> ZeroExtendWideningUpperEach128(Vector256<int> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// uint64x4_t xvexth_DU_WU_u32(uint32x8_t a)
        ///   LASX: XVEXTH.DU.WU Xd.4D, Xj.8W
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<uint> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// int128x2_t xvexth_DU_WU_s64(int64x4_t a)
        ///   LASX: XVEXTH.QU.DU Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<long> value) => ZeroExtendWideningUpperEach128(value);

        /// <summary>
        /// uint128x2_t xvexth_DU_WU_u64(uint64x4_t a)
        ///   LASX: XVEXTH.QU.DU Xd.2Q, Xj.4D
        /// </summary>
        public static Vector256<ulong> ZeroExtendWideningUpperEach128(Vector256<ulong> value) => ZeroExtendWideningUpperEach128(value);

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
        /// uint8x32_t xvnori_b_u8 (uint8x32_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<byte> Not(Vector256<byte> value) => Not(value);

        /// <summary>
        /// float64x4_t xvnori_b_f64 (float64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<double> Not(Vector256<double> value) => Not(value);

        /// <summary>
        /// int16x16_t xvnori_b_s16 (int16x16_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<short> Not(Vector256<short> value) => Not(value);

        /// <summary>
        /// int32x8_t xvnori_b_s32 (int32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<int> Not(Vector256<int> value) => Not(value);

        /// <summary>
        /// int64x4_t xvnori_b_s64 (int64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<long> Not(Vector256<long> value) => Not(value);

        /// <summary>
        /// int8x32_t xvnori_b_s8 (int8x32_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<sbyte> Not(Vector256<sbyte> value) => Not(value);

        /// <summary>
        /// float32x8_t xvnori_b_f32 (float32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<float> Not(Vector256<float> value) => Not(value);

        /// <summary>
        /// uint16x16_t xvnori_b_u16 (uint16x16_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<ushort> Not(Vector256<ushort> value) => Not(value);

        /// <summary>
        /// uint32x8_t xvnori_b_u32 (uint32x8_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// </summary>
        public static Vector256<uint> Not(Vector256<uint> value) => Not(value);

        /// <summary>
        /// uint64x4_t xvnori_b_u64 (uint64x4_t a)
        ///   LASX: XVNORI.B Vd.32B, Vj.32B, 0
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector256<ulong> Not(Vector256<ulong> value) => Not(value);

        /// <summary>
        /// int8x32_t xvnor_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVNOR.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> NotOr(Vector256<sbyte> left, Vector256<sbyte> right) => NotOr(left, right);

        /// <summary>
        /// uint8x32_t xvnor_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVNOR.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> NotOr(Vector256<byte> left, Vector256<byte> right) => NotOr(left, right);

        /// <summary>
        /// int16x16_t xvnor_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVNOR.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> NotOr(Vector256<short> left, Vector256<short> right) => NotOr(left, right);

        /// <summary>
        /// uint16x16_t xvnor_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVNOR.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> NotOr(Vector256<ushort> left, Vector256<ushort> right) => NotOr(left, right);

        /// <summary>
        /// int32x8_t xvnor_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVNOR.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> NotOr(Vector256<int> left, Vector256<int> right) => NotOr(left, right);

        /// <summary>
        /// uint32x8_t xvnor_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVNOR.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> NotOr(Vector256<uint> left, Vector256<uint> right) => NotOr(left, right);

        /// <summary>
        /// int64x4_t xvnor_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> NotOr(Vector256<long> left, Vector256<long> right) => NotOr(left, right);

        /// <summary>
        /// uint64x4_t xvnor_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> NotOr(Vector256<ulong> left, Vector256<ulong> right) => NotOr(left, right);

        /// <summary>
        /// float32x8_t xvnor_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVNOR.V Vd.8S, Vj.8S, Vk.8S
        /// </summary>
        public static Vector256<float> NotOr(Vector256<float> left, Vector256<float> right) => NotOr(left, right);

        /// <summary>
        /// float64x4_t xvnor_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVNOR.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<double> NotOr(Vector256<double> left, Vector256<double> right) => NotOr(left, right);

        /// <summary>
        /// uint8x8_t xvorn_u8 (uint8x8_t a, uint8x8_t b)
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x1_t xvorn_f64 (float64x1_t a, float64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x4_t xvorn_s16 (int16x4_t a, int16x4_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x2_t xvorn_s32 (int32x2_t a, int32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x1_t xvorn_s64 (int64x1_t a, int64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t xvorn_s8 (int8x8_t a, int8x8_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x2_t xvorn_f32 (float32x2_t a, float32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x4_t xvorn_u16 (uint16x4_t a, uint16x4_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x2_t xvorn_u32 (uint32x2_t a, uint32x2_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x1_t xvorn_u64 (uint64x1_t a, uint64x1_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) => OrNot(left, right);

        /// <summary>
        /// int8x32_t xvorn_v_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<sbyte> OrNot(Vector256<sbyte> left, Vector256<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// uint8x32_t xvorn_v_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVORN.V Vd.32B, Vj.32B, Vk.32B
        /// </summary>
        public static Vector256<byte> OrNot(Vector256<byte> left, Vector256<byte> right) => OrNot(left, right);

        /// <summary>
        /// int16x16_t xvor_v_s16 (int16x16_t a, int16x16_t b)
        ///   LASX: XVORN.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<short> OrNot(Vector256<short> left, Vector256<short> right) => OrNot(left, right);

        /// <summary>
        /// uint16x16_t xvor_v_u16 (uint16x16_t a, uint16x16_t b)
        ///   LASX: XVORN.V Vd.16H, Vj.16H, Vk.16H
        /// </summary>
        public static Vector256<ushort> OrNot(Vector256<ushort> left, Vector256<ushort> right) => OrNot(left, right);

        /// <summary>
        /// int32x8_t xvorn_v_s32 (int32x8_t a, int32x8_t b)
        ///   LASX: XVORN.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<int> OrNot(Vector256<int> left, Vector256<int> right) => OrNot(left, right);

        /// <summary>
        /// uint32x8_t xvorn_v_u32 (uint32x8_t a, uint32x8_t b)
        ///   LASX: XVORN.V Vd.8W, Vj.8W, Vk.8W
        /// </summary>
        public static Vector256<uint> OrNot(Vector256<uint> left, Vector256<uint> right) => OrNot(left, right);

        /// <summary>
        /// int64x4_t xvorn_v_s64 (int64x4_t a, int64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<long> OrNot(Vector256<long> left, Vector256<long> right) => OrNot(left, right);

        /// <summary>
        /// uint64x4_t xvorn_v_u64 (uint64x4_t a, uint64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<ulong> OrNot(Vector256<ulong> left, Vector256<ulong> right) => OrNot(left, right);

        /// <summary>
        /// float32x8_t xvorn_v_f32 (float32x8_t a, float32x8_t b)
        ///   LASX: XVORN.V Vd.8S, Vj.8S, Vk.8S
        /// </summary>
        public static Vector256<float> OrNot(Vector256<float> left, Vector256<float> right) => OrNot(left, right);

        /// <summary>
        /// float64x4_t xvorn_v_f64 (float64x4_t a, float64x4_t b)
        ///   LASX: XVORN.V Vd.4D, Vj.4D, Vk.4D
        /// </summary>
        public static Vector256<double> OrNot(Vector256<double> left, Vector256<double> right) => OrNot(left, right);

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
        /// int8x32_t xvslli_b_s8 (int8x32_t a, const int n) //qiaoqiao.ok
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical(Vector256<sbyte> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvslli_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSLLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical(Vector256<byte> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int16x16_t xvslli_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint16x16_t xvslli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSLLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x8_t xvslli_w_s32 (uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x8_t xvslli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSLLI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int64x4_t xvslli_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint64x4_t xvslli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSLLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, const byte shift) => ShiftLeftLogical(value, shift);

        /// int8x32_t xvsll_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSLL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftLeftLogical(Vector256<sbyte> value, Vector256<sbyte> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvsll_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSLL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftLeftLogical(Vector256<byte> value, Vector256<byte> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int16x16 xvsll_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX: XVSLL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftLeftLogical(Vector256<short> value, Vector256<short> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint16x16 xvsll_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSLL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftLeftLogical(Vector256<ushort> value, Vector256<ushort> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int32x8 xvsll_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSLL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftLeftLogical(Vector256<int> value, Vector256<int> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint32x8 xvsll_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSLL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftLeftLogical(Vector256<uint> value, Vector256<uint> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// int64x4 xvsll_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSLL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftLeftLogical(Vector256<long> value, Vector256<long> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint64x4 xvsll_d_u64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSLL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftLeftLogical(Vector256<ulong> value, Vector256<ulong> shift) => ShiftLeftLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvsrli_b_u8 (uint8x32_t a, const int n) //qiaoqiao.ok
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvsrli_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x16_t xvsrli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x16_t xvsrli_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x8_t xvsrli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x8_t xvsrli_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x4_t xvsrli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, const byte shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x4_t xvsrli_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, const byte shift) => ShiftRightLogical(value, shift);

        /// int8x32_t xvsrl_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSRL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogical(Vector256<sbyte> value, Vector256<sbyte> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvsrl_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSRL.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftRightLogical(Vector256<byte> value, Vector256<byte> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int16x16 xvsrl_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightLogical(Vector256<short> value, Vector256<short> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint16x16 xvsrl_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRL.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftRightLogical(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int32x8 xvsrl_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightLogical(Vector256<int> value, Vector256<int> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint32x8 xvsrl_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRL.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftRightLogical(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// int64x4 xvsrl_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightLogical(Vector256<long> value, Vector256<long> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint64x4 xvsrl_d_u64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRL.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftRightLogical(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogical(value, shift);

        /// <summary>
        /// uint8x32_t xvsrlri_b_u8 (uint8x32_t a, const int n) //qiaoqiao.ok.
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRounded(Vector256<sbyte> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x32_t xvsrlri_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVSRLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRounded(Vector256<byte> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x16_t xvsrlri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRounded(Vector256<short> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x16_t xvsrlri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVSRLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRounded(Vector256<ushort> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x8_t xvsrlri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRounded(Vector256<int> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x8_t xvsrlri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVSRLRI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRounded(Vector256<uint> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x4_t xvsrlri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightLogicalRounded(Vector256<long> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x4_t xvsrlri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVSRLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalRounded(Vector256<ulong> value, const byte shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int8x32_t xvsrlr_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSRLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRounded(Vector256<sbyte> value, Vector256<sbyte> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x32_t xvsrlr_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVSRLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRounded(Vector256<byte> value, Vector256<byte> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int16x16 xvsrlr_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRounded(Vector256<short> value, Vector256<short> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint16x16 xvsrlr_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRounded(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int32x8 xvsrlr_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRounded(Vector256<int> value, Vector256<int> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint32x8 xvsrlr_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRounded(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// int64x4 xvsrlr_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightLogicalRounded(Vector256<long> value, Vector256<long> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint64x4 xvsrlr_d_u64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> ShiftRightLogicalRounded(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalRounded(value, shift);

        /// <summary>
        /// uint8x32_t xvsrlrni_b_h_u16 (uint16x16_t left, uint16x16_t right, const int n)  qiaoqiao.ok.
        ///   LASX: XVSRLRNI.B.H Xd, Xj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int8x32_t xvsrlrni_b_h_s16 (int16x16_t left, int16x16_t right, const int n)
        ///   LASX: XVSRLRNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvsrlrni_h_w_s32 (int32x8_t left, int32x8_t right, const int n)
        ///   LASX: XVSRLRNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvsrlrni_h_w_u32 (uint32x8_t left, uint32x8_t right, const int n)
        ///   LASX: XVSRLRNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvsrlrni_w_d_s64 (int64x4_t left, int64x4_t right, const int n)
        ///   LASX: XVSRLRNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvsrlrni_w_d_u64 (uint64x4_t left, uint64x4_t right, const int n)
        ///   LASX: XVSRLRNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvsrlrni_d_q_s128 (int128x2_t left, int128x2_t right, const int n)
        /////   LASX: XVSRLRNI.D.Q Xd, Xj, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvsrlrn_b_h_s16 (int16x16_t value, int16x16_t shift)  qiaoqiao.ok.
        ///   LASX: XVSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint8x16_t xvsrlrn_b_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int16x8_t xvsrlrn_h_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint16x8_t xvsrlrn_h_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvsrlrn_w_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvsrlrn_w_d_s64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int8x16_t xvsrarni_b_h_s16 (int16x16_t left, int16x16_t right, const int n)
        ///   LASX: XVSRARNI.B.H Xd, Xj, ui4    ///NOTE: The Vd is both input and output, so the left shoule be ref type!!!
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvsrarni_h_w_s32 (int32x8_t left, int32x8_t right, const int n)
        ///   LASX: XVSRARNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvsrarni_w_d_s64 (int64x4_t left, int64x4_t right, const int n)
        ///   LASX: XVSRARNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvsrarni_d_q_s128 (int128x2_t left, int128x2_t right, const int n)
        /////   LASX: XVSRARNI.D.Q Xd, Xj, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvsrarn_b_h_s16 (int16x16_t value, int16x16_t shift)  qiaoqiao.ok.
        ///   LASX: XVSRARN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int16x8_t xvsrarn_h_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint16x8_t xvsrarn_h_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvsrarn_w_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRARN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticRoundedNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int8x32_t xvsrai_b_s8 (int8x32_t a, const int n)//qiaoqiao.ok.
        ///   LASX: XVSRAI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int16x16_t xvsrai_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSRAI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int32x8_t xvsrai_w_s32 (int32x8_t a, const int n)
        ///   LASX: XVSRAI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int64x4_t xvsrai_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSRAI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, const byte shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int8x32_txvsra_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX:  XVSRA.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmetic(Vector256<sbyte> value, Vector256<sbyte> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int16x16xvsra_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX:  XVSRA.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightArithmetic(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int32x8xvsra_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX:  XVSRA.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightArithmetic(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int64x4xvsra_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX:  XVSRA.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightArithmetic(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmetic(value, shift);

        /// <summary>
        /// int8x32_t xvsrari_b_s8 (int8x32_t a, const int n) //qiaoqiao.ok.
        ///   LASX: XVSRARI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRounded(Vector256<sbyte> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int16x16_t xvsrari_h_s16 (int16x16_t a, const int n)
        ///   LASX: XVSRARI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRounded(Vector256<short> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int32x8_t xvsrari_w_s32 (int32x8_t a, const int n)
        ///   LASX: XVSRARI.W Xd.8W, Xj.8W, ui5
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRounded(Vector256<int> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int64x4_t xvsrari_d_s64 (int64x4_t a, const int n)
        ///   LASX: XVSRARI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticRounded(Vector256<long> value, const byte shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int8x32_t xvsrar_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVSRAR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRounded(Vector256<sbyte> value, Vector256<sbyte> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int16x16 xvsrar_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX: XVSRAR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRounded(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int32x8 xvsrar_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRAR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRounded(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// int64x4 xvsrar_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRAR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> ShiftRightArithmeticRounded(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticRounded(value, shift);

        /// <summary>
        /// uint8x32_t xvrotri_b_u8 (uint8x32_t a, const int n) //qiaoqiao.ok.
        ///   LASX: XVROTRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> RotateRight(Vector256<sbyte> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint8x32_t xvrotri_b_u8 (uint8x32_t a, const int n)
        ///   LASX: XVROTRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> RotateRight(Vector256<byte> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x16_t xvrotri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVROTRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> RotateRight(Vector256<short> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x16_t xvrotri_h_u16 (uint16x16_t a, const int n)
        ///   LASX: XVROTRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> RotateRight(Vector256<ushort> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x8_t xvrotri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVROTRI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x8_t xvrotri_w_u32 (uint32x8_t a, const int n)
        ///   LASX: XVROTRI.W Xd.8S, Xj.8S, ui5
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x4_t xvrotri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVROTRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x4_t xvrotri_d_u64 (uint64x4_t a, const int n)
        ///   LASX: XVROTRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, const byte shift) => RotateRight(value, shift);

        /// <summary>
        /// int8x32_t xvrotr_b_s8 (int8x32_t a, int8x32_t b)
        ///   LASX: XVROTR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> RotateRight(Vector256<sbyte> value, Vector256<sbyte> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint8x32_t xvrotr_b_u8 (uint8x32_t a, uint8x32_t b)
        ///   LASX: XVROTR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> RotateRight(Vector256<byte> value, Vector256<byte> shift) => RotateRight(value, shift);

        /// <summary>
        /// int16x16 xvrotr_h_s16 (int16x16_t value, int16x16_t shift)
        ///   LASX: XVROTR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> RotateRight(Vector256<short> value, Vector256<short> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint16x16 xvrotr_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVROTR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> RotateRight(Vector256<ushort> value, Vector256<ushort> shift) => RotateRight(value, shift);

        /// <summary>
        /// int32x8 xvrotr_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVROTR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> RotateRight(Vector256<int> value, Vector256<int> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint32x8 xvrotr_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVROTR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> RotateRight(Vector256<uint> value, Vector256<uint> shift) => RotateRight(value, shift);

        /// <summary>
        /// int64x4 xvrotr_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVROTR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> RotateRight(Vector256<long> value, Vector256<long> shift) => RotateRight(value, shift);

        /// <summary>
        /// uint64x4 xvrotr_d_u64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVROTR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> RotateRight(Vector256<ulong> value, Vector256<ulong> shift) => RotateRight(value, shift);

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
        /// int8x32 xvsrlni_b_h_s16 (int16x16_t left, int16x16_t right, shift)
        ///   LASX: XVSRLNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalNarrowingLowerEach128(Vector256<short> left, Vector256<short> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x32 xvsrlni_b_h_u16 (uint16x16_t left, uint16x16_t right, shift)
        ///   LASX: XVSRLNI.B.H Xd, Xj, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingLowerEach128(Vector256<ushort> left, Vector256<ushort> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16 xvsrlni_h_w_s32 (int32x8_t left, int32x8_t right, shift)
        ///   LASX: XVSRLNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalNarrowingLowerEach128(Vector256<int> left, Vector256<int> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16 xvsrlni_h_w_u32 (uint32x8_t left, uint32x8_t right, shift)
        ///   LASX: XVSRLNI.H.W Xd, Xj, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingLowerEach128(Vector256<uint> left, Vector256<uint> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8 xvsrlni_w_d_s64 (int64x4_t left, int64x4_t right, shift)
        ///   LASX: XVSRLNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalNarrowingLowerEach128(Vector256<long> left, Vector256<long> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8 xvsrlni_w_d_u64 (uint64x4_t left, uint64x4_t right, shift)
        ///   LASX: XVSRLNI.W.D Xd, Xj, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulong> left, Vector256<ulong> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        ///// <summary>
        ///// uint64x4 xvsrlni_d_q(uint128x2_t left, uint128x2_t right, shift)
        /////   LASX: XVSRLNI.D.Q Xd.2Q, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, byte shift) => ShiftRightLogicalNarrowingLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x16_t xvsrln_b_h_u16 (uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSRLN.B.H Xd.8B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int16x8_t xvsrln_h_w_s32 (int32x8_t value, int32x8_t shift)
        ///   LASX: XVSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightLogicalNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint16x8_t xvsrln_h_w_u32 (uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvsrln_w_d_s64 (int64x4_t value, int64x4_t shift)
        ///   LASX: XVSRLN.W.D Xd.4W, Xj.4D, Xk.2D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightLogicalNarrowingLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvsrln_w_d_u64 (uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSRLN.W.D Xd.4W, Xj.4D, Xk.2D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalNarrowingLowerEach128(value, shift);

        /// <summary>
        /// int32x8_t xvssrlni_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrlni_b_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.B.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvssrlni_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrlni_h_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvssrlni_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrlni_w_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvssrlni_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRLNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvssrln_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRLN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint8x16_t xvssrln_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int16x4_t xvssrln_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x4_t xvssrln_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvssrln_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRLN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssrln_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x16_t xvssrlni_bu_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrlni_hu_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrlni_wu_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        ///// <summary>
        ///// uint64x4_t xvssrlni_du_q(uint128x2_t left, uint128x2_t right, const byte n)
        /////   LASX: XVSSRLNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x16_t xvssrln_bu_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint16x4_t xvssrln_hu_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssrln_wu_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// int16x16_t xvssran_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRANI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvssran_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRANI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvssran_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRANI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvssran_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRANI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvssran_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRAN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int16x4_t xvssran_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRAN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvssran_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRAN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x16_t xvssrani_bu_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRANI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrani_hu_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRANI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrani_wu_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRANI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        ///// <summary>
        ///// uint64x4_t xvssrani_du_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRANI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x16_t xvssran_bu_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRAN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint16x4_t xvssran_hu_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRAN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssran_wu_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRAN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// int16x16_t xvssrlrni_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrlrni_b_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.B.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvssrlrni_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrlrni_h_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvssrlrni_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrlrni_w_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvssrlrni_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRLRNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvssrlrn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint8x16_t xvssrlrn_b_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLRN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int16x4_t xvssrlrn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x4_t xvssrlrn_h_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLRN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvssrlrn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssrlrn_w_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLRN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x16_t xvssrlrni_bu_h(uint16x16_t left, uint16x16_t right, const byte n)
        ///   LASX: XVSSRLRNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> left, Vector256<ushort> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrlrni_hu_w(uint32x8_t left, uint32x8_t right, const byte n)
        ///   LASX: XVSSRLRNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<uint> left, Vector256<uint> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrlrni_wu_d(uint64x4_t left, uint64x4_t right, const byte n)
        ///   LASX: XVSSRLRNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> left, Vector256<ulong> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        ///// <summary>
        ///// uint64x4_t xvssrlrni_du_q(uint128x2_t left, uint128x2_t right, const byte n)
        /////   LASX: XVSSRLRNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulonglong> left, Vector256<ulonglong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x16_t xvssrlrn_bu_h(uint16x16_t value, uint16x16_t shift)
        ///   LASX: XVSSRLRN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ushort> value, Vector256<ushort> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint16x4_t xvssrlrn_hu_w(uint32x8_t value, uint32x8_t shift)
        ///   LASX: XVSSRLRN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<uint> value, Vector256<uint> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssrlrn_wu_d(uint64x4_t value, uint64x4_t shift)
        ///   LASX: XVSSRLRN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<ulong> value, Vector256<ulong> shift) => ShiftRightLogicalRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// int16x16_t xvssrarn_b_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRARNI.B.H Xd.32B, Xj.16H, ui4  ///NOTE: the Vd is both input and output.
        /// </summary>
        public static Vector256<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int16x16_t xvssrarn_h_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRARNI.H.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<short> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int32x8_t xvssrarn_w_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRARNI.W.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<int> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(left, right, shift);

        ///// <summary>
        ///// int64x4_t xvssrarn_d_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRARNI.D.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<long> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(left, right, shift);

        /// <summary>
        /// int8x16_t xvssrarn_b_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRARN.B.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int16x4_t xvssrarn_h_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRARN.H.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// int32x4_t xvssrarn_w_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRARN.W.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticRoundedNarrowingSaturateLowerEach128(value, shift);

        /// <summary>
        /// uint16x16_t xvssrarni_bu_h(int16x16_t left, int16x16_t right, const byte n)
        ///   LASX: XVSSRARNI.BU.H Xd.32B, Xj.16H, ui4
        /// </summary>
        public static Vector256<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<short> left, Vector256<short> right, [ConstantExpected(Min = 0, Max = (byte)(15))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint16x16_t xvssrarni_hu_w(int32x8_t left, int32x8_t right, const byte n)
        ///   LASX: XVSSRARNI.HU.W Xd.16H, Xj.8W, ui5
        /// </summary>
        public static Vector256<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<int> left, Vector256<int> right, [ConstantExpected(Min = 0, Max = (byte)(31))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint32x8_t xvssrarni_wu_d(int64x4_t left, int64x4_t right, const byte n)
        ///   LASX: XVSSRARNI.WU.D Xd.8W, Xj.4D, ui6
        /// </summary>
        public static Vector256<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<long> left, Vector256<long> right, [ConstantExpected(Min = 0, Max = (byte)(63))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        ///// <summary>
        ///// uint64x4_t xvssrarni_du_q(int128x2_t left, int128x2_t right, const byte n)
        /////   LASX: XVSSRARNI.DU.Q Xd.4D, Xj.2Q, ui7
        ///// </summary>
        //public static Vector256<ulong> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<longlong> left, Vector256<longlong> right, [ConstantExpected(Min = 0, Max = (byte)(127))] byte shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(left, right, shift);

        /// <summary>
        /// uint8x16_t xvssrarn_bu_h(int16x16_t value, int16x16_t shift)
        ///   LASX: XVSSRARN.BU.H Xd.16B, Xj.16H, Xk.16H
        /// </summary>
        public static Vector128<byte> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<short> value, Vector256<short> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint16x8_t xvssrarn_hu_w(int32x8_t value, int32x8_t shift)
        ///   LASX: XVSSRARN.HU.W Xd.8H, Xj.8W, Xk.8W
        /// </summary>
        public static Vector128<ushort> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<int> value, Vector256<int> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// uint32x4_t xvssrarn_wu_d(int64x4_t value, int64x4_t shift)
        ///   LASX: XVSSRARN.WU.D Xd.4W, Xj.4D, Xk.4D
        /// </summary>
        public static Vector128<uint> ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(Vector256<long> value, Vector256<long> shift) => ShiftRightArithmeticRoundedNarrowingSaturateUnsignedLowerEach128(value, shift);

        /// <summary>
        /// int8x32_t xvclo_b(int8x32_t a)
        ///   LASX: XVCLO.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> LeadingSignCount(Vector256<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x16_t xvclo_h(int16x16_t a)
        ///   LASX: XVCLO.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> LeadingSignCount(Vector256<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x8_t xvclo_w(int32x8_t a)
        ///   LASX: XVCLO.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> LeadingSignCount(Vector256<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int64x4_t xvclo_d(int64x4_t a)
        ///   LASX: XVCLO.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> LeadingSignCount(Vector256<long> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x32_t xvclz_b(int8x32_t a)
        ///   LASX: XVCLZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<sbyte> LeadingZeroCount(Vector256<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x32_t xvclz_b(uint8x32_t a)
        ///   LASX: XVCLZ.B Xd.32B, Xj.32B
        /// </summary>
        public static Vector256<byte> LeadingZeroCount(Vector256<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x16_t xvclz_h(int16x16_t a)
        ///   LASX: XVCLZ.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<short> LeadingZeroCount(Vector256<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x16_t xvclz_h(uint16x16_t a)
        ///   LASX: XVCLZ.H Xd.16H, Xj.16H
        /// </summary>
        public static Vector256<ushort> LeadingZeroCount(Vector256<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x8_t xvclz_w(int32x8_t a)
        ///   LASX: XVCLZ.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<int> LeadingZeroCount(Vector256<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x8_t xvclz_w(uint32x8_t a)
        ///   LASX: XVCLZ.W Xd.8W, Xj.8W
        /// </summary>
        public static Vector256<uint> LeadingZeroCount(Vector256<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// int64x4_t xvclz_d(int64x4_t a)
        ///   LASX: XVCLZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<long> LeadingZeroCount(Vector256<long> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint64x4_t xvclz_d(uint64x4_t a)
        ///   LASX: XVCLZ.D Xd.4D, Xj.4D
        /// </summary>
        public static Vector256<ulong> LeadingZeroCount(Vector256<ulong> value) => LeadingZeroCount(value);

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

        /// <summary>
        /// int8x32_t xvbitclri_b(int8x32_t a, const int n)
        ///   LASX: XVBITCLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitClear(Vector256<sbyte> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint8x32_t xvbitclri_b(uint8x32_t a, const int n)
        ///   LASX: XVBITCLRI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitClear(Vector256<byte> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int16x16_t xvbitclri_h(int16x16_t a, const int n)
        ///   LASX: XVBITCLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitClear(Vector256<short> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint16x16_t xvbitclri_h(uint16x16_t a, const int n)
        ///   LASX: XVBITCLRI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitClear(Vector256<ushort> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x8_t xvbitclri_w(uint32x8_t a, const int n)
        ///   LASX: XVBITCLRI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitClear(Vector256<int> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x8_t xvbitclri_w(uint32x8_t a, const int n)
        ///   LASX: XVBITCLRI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitClear(Vector256<uint> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int64x4_t xvbitclri_d(int64x4_t a, const int n)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitClear(Vector256<long> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint64x4_t xvbitclri_d(uint64x4_t a, const int n)
        ///   LASX: XVBITCLRI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitClear(Vector256<ulong> value, const byte index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int8x32_t xvbitclr_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITCLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitClear(Vector256<sbyte> value, Vector256<sbyte> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint8x32_t xvbitclr_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITCLR.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitClear(Vector256<byte> value, Vector256<byte> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int16x16_t xvbitclr_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITCLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitClear(Vector256<short> value, Vector256<short> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint16x16_t xvbitclr_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITCLR.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitClear(Vector256<ushort> value, Vector256<ushort> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int32x8_t xvbitclr_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITCLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitClear(Vector256<int> value, Vector256<int> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint32x8_t xvbitclr_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITCLR.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitClear(Vector256<uint> value, Vector256<uint> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int64x4_t xvbitclr_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITCLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitClear(Vector256<long> value, Vector256<long> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// uint64x4_t xvbitclr_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITCLR.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitClear(Vector256<ulong> value, Vector256<ulong> index) => VectorElementBitClear(value, index);

        /// <summary>
        /// int8x32_t xvbitseti_b(int8x32_t a, const int n)
        ///   LASX: XVBITSETI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitSet(Vector256<sbyte> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint8x32_t xvbitseti_b(uint8x32_t a, const int n)
        ///   LASX: XVBITSETI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitSet(Vector256<byte> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int16x16_t xvbitseti_h(int16x16_t a, const int n)
        ///   LASX: XVBITSETI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitSet(Vector256<short> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint16x16_t xvbitseti_h(uint16x16_t a, const int n)
        ///   LASX: XVBITSETI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitSet(Vector256<ushort> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x8_t xvbitseti_w(uint32x8_t a, const int n)
        ///   LASX: XVBITSETI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitSet(Vector256<int> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x8_t xvbitseti_w(uint32x8_t a, const int n)
        ///   LASX: XVBITSETI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitSet(Vector256<uint> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int64x4_t xvbitseti_d(int64x4_t a, const int n)
        ///   LASX: XVBITSETI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitSet(Vector256<long> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint64x4_t xvbitseti_d(uint64x4_t a, const int n)
        ///   LASX: XVBITSETI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitSet(Vector256<ulong> value, const byte index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int8x32_t xvbitset_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITSET.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitSet(Vector256<sbyte> value, Vector256<sbyte> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint8x32_t xvbitset_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITSET.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitSet(Vector256<byte> value, Vector256<byte> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int16x16_t xvbitset_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITSET.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitSet(Vector256<short> value, Vector256<short> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint16x16_t xvbitset_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITSET.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitSet(Vector256<ushort> value, Vector256<ushort> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int32x8_t xvbitset_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITSET.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitSet(Vector256<int> value, Vector256<int> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint32x8_t xvbitset_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITSET.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitSet(Vector256<uint> value, Vector256<uint> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int64x4_t xvbitset_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITSET.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitSet(Vector256<long> value, Vector256<long> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// uint64x4_t xvbitset_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITSET.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitSet(Vector256<ulong> value, Vector256<ulong> index) => VectorElementBitSet(value, index);

        /// <summary>
        /// int8x32_t xvbitrevi_b(int8x32_t a, const int n)
        ///   LASX: XVBITREVI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<sbyte> VectorElementBitRevert(Vector256<sbyte> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint8x32_t xvbitrevi_b(uint8x32_t a, const int n)
        ///   LASX: XVBITREVI.B Xd.32B, Xj.32B, ui3
        /// </summary>
        public static Vector256<byte> VectorElementBitRevert(Vector256<byte> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int16x16_t xvbitrevi_h(int16x16_t a, const int n)
        ///   LASX: XVBITREVI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<short> VectorElementBitRevert(Vector256<short> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint16x16_t xvbitrevi_h(uint16x16_t a, const int n)
        ///   LASX: XVBITREVI.H Xd.16H, Xj.16H, ui4
        /// </summary>
        public static Vector256<ushort> VectorElementBitRevert(Vector256<ushort> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x8_t xvbitrevi_w(uint32x8_t a, const int n)
        ///   LASX: XVBITREVI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<int> VectorElementBitRevert(Vector256<int> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x8_t xvbitrevi_w(uint32x8_t a, const int n)
        ///   LASX: XVBITREVI.W Xd.4S, Xj.4S, ui5
        /// </summary>
        public static Vector256<uint> VectorElementBitRevert(Vector256<uint> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int64x4_t xvbitrevi_d(int64x4_t a, const int n)
        ///   LASX: XVBITREVI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<long> VectorElementBitRevert(Vector256<long> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint64x4_t xvbitrevi_d(uint64x4_t a, const int n)
        ///   LASX: XVBITREVI.D Xd.4D, Xj.4D, ui6
        /// </summary>
        public static Vector256<ulong> VectorElementBitRevert(Vector256<ulong> value, const byte index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int8x32_t xvbitrev_b(int8x32_t a, int8x32_t b)
        ///   LASX: XVBITREV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<sbyte> VectorElementBitRevert(Vector256<sbyte> value, Vector256<sbyte> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint8x32_t xvbitrev_b(uint8x32_t a, uint8x32_t b)
        ///   LASX: XVBITREV.B Xd.32B, Xj.32B, Xk.32B
        /// </summary>
        public static Vector256<byte> VectorElementBitRevert(Vector256<byte> value, Vector256<byte> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int16x16_t xvbitrev_h(int16x16_t value, int16x16_t index)
        ///   LASX: XVBITREV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<short> VectorElementBitRevert(Vector256<short> value, Vector256<short> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint16x16_t xvbitrev_h(uint16x16_t value, uint16x16_t index)
        ///   LASX: XVBITREV.H Xd.16H, Xj.16H, Xk.16H
        /// </summary>
        public static Vector256<ushort> VectorElementBitRevert(Vector256<ushort> value, Vector256<ushort> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int32x8_t xvbitrev_w(int32x8_t value, int32x8_t index)
        ///   LASX: XVBITREV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<int> VectorElementBitRevert(Vector256<int> value, Vector256<int> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint32x8_t xvbitrev_w(uint32x8_t value, uint32x8_t index)
        ///   LASX: XVBITREV.W Xd.8W, Xj.8W, Xk.8W
        /// </summary>
        public static Vector256<uint> VectorElementBitRevert(Vector256<uint> value, Vector256<uint> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// int64x4_t xvbitrev_d(int64x4_t value, int64x4_t index)
        ///   LASX: XVBITREV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<long> VectorElementBitRevert(Vector256<long> value, Vector256<long> index) => VectorElementBitRevert(value, index);

        /// <summary>
        /// uint64x4_t xvbitrev_d(uint64x4_t value, uint64x4_t index)
        ///   LASX: XVBITREV.D Xd.4D, Xj.4D, Xk.4D
        /// </summary>
        public static Vector256<ulong> VectorElementBitRevert(Vector256<ulong> value, Vector256<ulong> index) => VectorElementBitRevert(value, index);

        // TODO:----------------------------------
    }
}

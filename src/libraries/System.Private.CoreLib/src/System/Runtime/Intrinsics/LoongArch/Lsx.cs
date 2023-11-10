// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.LoongArch
{
    /// <summary>
    /// This class provides access to the LSX-128bits hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Lsx : LoongArchBase
    {
        internal Lsx() { }

        public static new bool IsSupported { get => IsSupported; }

        /// <summary>
        /// int8x16_t vadd_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) => Add(left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: TODO Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) => Add(left, right);

        /// <summary>
        /// int16x8_t vadd_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) => Add(left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: TODO Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);

        /// <summary>
        /// int32x4_t vadd_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VADD.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) => Add(left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) => Add(left, right);

        /// <summary>
        /// int64x2_t vadd_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) => Add(left, right);

        /// <summary>
        /// uint64x2_t TODO_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) => Add(left, right);

        /// <summary>
        /// float32x4_t vfadd_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFADD.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) => Add(left, right);

        /// <summary>
        /// float64x2_t vfadd_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

        /// <summary>
        /// int8x16_t vsub_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: TODO Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vsub_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: TODO Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);

        /// <summary>
        /// int32x4_t vsub_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSUB.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);

        /// <summary>
        /// int64x2_t vsub_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);

        /// <summary>
        /// uint64x2_t TODO_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);

        /// <summary>
        /// float32x4_t vfsub_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFSUB.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) => Subtract(left, right);

        /// <summary>
        /// float64x2_t vfsub_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Subtract(left, right);

        /// <summary>
        /// int8x16_t vmul_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VMUL.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Multiply(Vector128<sbyte> left, Vector128<sbyte> right) => Multiply(left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: TODO Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Multiply(Vector128<byte> left, Vector128<byte> right) => Multiply(left, right);

        /// <summary>
        /// int16x8_t vmul_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VMUL.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Multiply(Vector128<short> left, Vector128<short> right) => Multiply(left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: TODO Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Multiply(Vector128<ushort> left, Vector128<ushort> right) => Multiply(left, right);

        /// <summary>
        /// int32x4_t vmul_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VMULW Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Multiply(Vector128<int> left, Vector128<int> right) => Multiply(left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Multiply(Vector128<uint> left, Vector128<uint> right) => Multiply(left, right);

        /// <summary>
        /// int64x2_t vmul_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VMUL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Multiply(Vector128<long> left, Vector128<long> right) => Multiply(left, right);

        /// <summary>
        /// uint64x2_t TODO_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Multiply(Vector128<ulong> left, Vector128<ulong> right) => Multiply(left, right);

        /// <summary>
        /// float32x4_t vfmul_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFMUL.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Multiply(Vector128<float> left, Vector128<float> right) => Multiply(left, right);

        /// <summary>
        /// float64x2_t vfmul_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFMUL.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Multiply(Vector128<double> left, Vector128<double> right) => Multiply(left, right);

        /// <summary>
        /// int8x16_t vdiv_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VDIV.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Divide(Vector128<sbyte> left, Vector128<sbyte> right) => Divide(left, right);

        /// <summary>
        /// uint8x16_t vdiv_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VDIV.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Divide(Vector128<byte> left, Vector128<byte> right) => Divide(left, right);

        /// <summary>
        /// int16x8_t vdiv_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VDIV.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Divide(Vector128<short> left, Vector128<short> right) => Divide(left, right);

        /// <summary>
        /// uint16x8_t vdiv_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VDIV.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Divide(Vector128<ushort> left, Vector128<ushort> right) => Divide(left, right);

        /// <summary>
        /// int32x4_t vdiv_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VDIV.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Divide(Vector128<int> left, Vector128<int> right) => Divide(left, right);

        /// <summary>
        /// uint32x4_t vdiv_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VDIV.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Divide(Vector128<uint> left, Vector128<uint> right) => Divide(left, right);

        /// <summary>
        /// int64x2_t vdiv_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VDIV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Divide(Vector128<long> left, Vector128<long> right) => Divide(left, right);

        /// <summary>
        /// uint64x2_t vdiv_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VDIV.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Divide(Vector128<ulong> left, Vector128<ulong> right) => Divide(left, right);

        /// <summary>
        /// float32x4_t vfdiv_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFDIV.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Divide(Vector128<float> left, Vector128<float> right) => Divide(left, right);

        /// <summary>
        /// float64x2_t vfdiv_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFDIV.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Divide(Vector128<double> left, Vector128<double> right) => Divide(left, right);

        /// <summary>
        /// float32x4_t vfmadd_s_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFMADD.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> FusedMultiplyAdd(Vector128<float> addend, Vector128<float> left, Vector128<float> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// float64x2_t vfmadd_d_f64 (float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFMADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> FusedMultiplyAdd(Vector128<double> addend, Vector128<double> left, Vector128<double> right) => FusedMultiplyAdd(addend, left, right);

        /// <summary>
        /// int8x16_t vmadd_b_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> MultiplyAdd(Vector128<sbyte> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: TODO Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> MultiplyAdd(Vector128<byte> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmadd_h_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> MultiplyAdd(Vector128<short> addend, Vector128<short> left, Vector128<short> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: TODO Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> MultiplyAdd(Vector128<ushort> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmadd_w_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMADD.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> MultiplyAdd(Vector128<int> addend, Vector128<int> left, Vector128<int> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> MultiplyAdd(Vector128<uint> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyAdd(addend, left, right);

        /// <summary>
        /// int8x16_t TODO_s8 (int8x16_t a, uint8x16_t b)
        ///   LSX: TODO Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, int8x16_t b)
        ///   LSX: TODO Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x8_t TODO_s16 (int16x8_t a, uint16x8_t b)
        ///   LSX: TODO Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, int16x8_t b)
        ///   LSX: TODO Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x4_t TODO_s32 (int32x4_t a, uint32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<int> AddSaturate(Vector128<int> left, Vector128<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, int32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<uint> AddSaturate(Vector128<uint> left, Vector128<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x2_t TODO_s64 (int64x2_t a, uint64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> AddSaturate(Vector128<long> left, Vector128<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x2_t TODO_u64 (uint64x2_t a, int64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<ulong> AddSaturate(Vector128<ulong> left, Vector128<long> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x16_t vsadd_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSADD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> AddSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => AddSaturate(left, right);

        /// <summary>
        /// int16x8_t vsadd_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSADD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> AddSaturate(Vector128<short> left, Vector128<short> right) => AddSaturate(left, right);

        /// <summary>
        /// int32x4_t vsadd_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSADD.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> AddSaturate(Vector128<int> left, Vector128<int> right) => AddSaturate(left, right);

        /// <summary>
        /// int64x2_t vsadd_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSADD.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> AddSaturate(Vector128<long> left, Vector128<long> right) => AddSaturate(left, right);

        /// <summary>
        /// uint8x16_t vsadd_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSADD.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AddSaturate(Vector128<byte> left, Vector128<byte> right) => AddSaturate(left, right);

        /// <summary>
        /// uint16x8_t vsadd_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSADD.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AddSaturate(Vector128<ushort> left, Vector128<ushort> right) => AddSaturate(left, right);

        /// <summary>
        /// uint32x4_t vsadd_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSADD.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> AddSaturate(Vector128<uint> left, Vector128<uint> right) => AddSaturate(left, right);

        /// <summary>
        /// uint64x2_t vsadd_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSADD.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> AddSaturate(Vector128<ulong> left, Vector128<ulong> right) => AddSaturate(left, right);

        /// <summary>
        /// int8x16_t vadd_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VHADDW.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> HorizontalAdd(Vector128<sbyte> left, Vector128<sbyte> right) => HorizontalAdd(left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VHADDW.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> HorizontalAdd(Vector128<byte> left, Vector128<byte> right) => HorizontalAdd(left, right);

        /// <summary>
        /// int16x8_t vadd_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VHADDW.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> HorizontalAdd(Vector128<short> left, Vector128<short> right) => HorizontalAdd(left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VHADDW.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> HorizontalAdd(Vector128<ushort> left, Vector128<ushort> right) => HorizontalAdd(left, right);

        /// <summary>
        /// int32x4_t vadd_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VHADDW.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> HorizontalAdd(Vector128<int> left, Vector128<int> right) => HorizontalAdd(left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VHADDW.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> HorizontalAdd(Vector128<uint> left, Vector128<uint> right) => HorizontalAdd(left, right);

        /// <summary>
        /// int64x2_t vadd_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VHADDW.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> HorizontalAdd(Vector128<long> left, Vector128<long> right) => HorizontalAdd(left, right);

        /// <summary>
        /// uint64x2_t TODO_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VHADDW.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> HorizontalAdd(Vector128<ulong> left, Vector128<ulong> right) => HorizontalAdd(left, right);

        //// TODO: LA-SIMD: add HorizontalSubtract for LA64.

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.B Vd.16B, Vj.16B, Vk.16B
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static sbyte HorizontalSum(Vector128<sbyte> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.B Vd.16B, Vj.16B, Vk.16B
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static byte HorizontalSum(Vector128<byte> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.H Vd.8H, Vj.8H, Vk.8H
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static short HorizontalSum(Vector128<short> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.H Vd.8H, Vj.8H, Vk.8H
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static ushort HorizontalSum(Vector128<ushort> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.W Vd.4S, Vj.4S, Vk.4S
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static int HorizontalSum(Vector128<int> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.W Vd.4S, Vj.4S, Vk.4S
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static uint HorizontalSum(Vector128<uint> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.D Vd.2D, Vj.2D, Vk.2D
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static long HorizontalSum(Vector128<long> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VHADDW.D Vd.2D, Vj.2D, Vk.2D
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static ulong HorizontalSum(Vector128<ulong> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: sum_all_float_elements witin value.
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static float HorizontalSum(Vector128<float> value) => HorizontalSum(value);

        /// <summary>
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: sum_all_double_elements witin value.
        ///   LSX: Vector128.ToScalar
        /// </summary>
        public static double HorizontalSum(Vector128<double> value) => HorizontalSum(value);

        /// <summary>
        /// int8x16_t vmsub_b_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> MultiplySubtract(Vector128<sbyte> minuend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint8x16_t TODO_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: TODO Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> MultiplySubtract(Vector128<byte> minuend, Vector128<byte> left, Vector128<byte> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmsub_h_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> MultiplySubtract(Vector128<short> minuend, Vector128<short> left, Vector128<short> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint16x8_t TODO_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: TODO Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> MultiplySubtract(Vector128<ushort> minuend, Vector128<ushort> left, Vector128<ushort> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int32x4_t vmsub_w_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMSUB.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> MultiplySubtract(Vector128<int> minuend, Vector128<int> left, Vector128<int> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// uint32x4_t TODO_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> MultiplySubtract(Vector128<uint> minuend, Vector128<uint> left, Vector128<uint> right) => MultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmaddwev_h_b_s8 (int16x8_t a, int8x8_t b, int8x8_t c)
        ///   LSX: VMADDWEV.H.B Vd.8H, Vj.8B, Vk.8B
        /// </summary>
        public static Vector128<short> MultiplyWideningLowerAndAdd(Vector128<short> addend, Vector64<sbyte> left, Vector64<sbyte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmaddwev_h_bu_u8 (uint16x8_t a, uint8x8_t b, uint8x8_t c)
        ///   LSX: VMADDWEV.H.BU Vd.8H, Vj.8B, Vk.8B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningLowerAndAdd(Vector128<ushort> addend, Vector64<byte> left, Vector64<byte> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmaddwev_w_h_s16 (int32x4_t a, int16x4_t b, int16x4_t c)
        ///   LSX: VMADDWEV.W.H Vd.4S, Vj.4H, Vk.4H
        /// </summary>
        public static Vector128<int> MultiplyWideningLowerAndAdd(Vector128<int> addend, Vector64<short> left, Vector64<short> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmaddwev_w_hu_u16 (uint32x4_t a, uint16x4_t b, uint16x4_t c)
        ///   LSX: VMADDWEV.W.HU Vd.4S, Vj.4H, Vk.4H
        /// </summary>
        public static Vector128<uint> MultiplyWideningLowerAndAdd(Vector128<uint> addend, Vector64<ushort> left, Vector64<ushort> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmaddwev_d_w_s32 (int64x2_t a, int32x2_t b, int32x2_t c)
        ///   LSX: VMADDWEV.D.W Vd.2D, Vj.2S, Vk.2S
        /// </summary>
        public static Vector128<long> MultiplyWideningLowerAndAdd(Vector128<long> addend, Vector64<int> left, Vector64<int> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmaddwev_d_wu_u32 (uint64x2_t a, uint32x2_t b, uint32x2_t c)
        ///   LSX: VMADDWEV.D.WU Vd.2D, Vj.2S, Vk.2S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningLowerAndAdd(Vector128<ulong> addend, Vector64<uint> left, Vector64<uint> right) => MultiplyWideningLowerAndAdd(addend, left, right);

        /// <summary>
        /// int16x8_t vmaddwod_h_b_s8 (int16x8_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VMADDWOD.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpperAndAdd(Vector128<short> addend, Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint16x8_t vmaddwod_h_bu_u8 (uint16x8_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VMADDWOD.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpperAndAdd(Vector128<ushort> addend, Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int32x4_t vmaddwod_w_h_s16 (int32x4_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VMADDWOD.W.H Vd.4S, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpperAndAdd(Vector128<int> addend, Vector128<short> left, Vector128<short> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint32x4_t vmaddwod_w_hu_u16 (uint32x4_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VMADDWOD.W.HU Vd.4S, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpperAndAdd(Vector128<uint> addend, Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// int64x2_t vmaddwod_d_w_s32 (int64x2_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VMADDWOD.D.W Vd.2D, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<long> MultiplyWideningUpperAndAdd(Vector128<long> addend, Vector128<int> left, Vector128<int> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint64x2_t vmaddwod_d_wu_u32 (uint64x2_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VMADDWOD.D.WU Vd.2D, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpperAndAdd(Vector128<ulong> addend, Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpperAndAdd(addend, left, right);

        /// <summary>
        /// uint8x16_t vseq_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSEQ.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x16_t vseq_b_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSEQ.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareEqual(Vector128<byte> left, Vector128<byte> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x8_t vseq_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSEQ.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareEqual(Vector128<short> left, Vector128<short> right) => CompareEqual(left, right);

        /// <summary>
        /// uint16x8_t vseq_h_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSEQ.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vseq_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSEQ.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareEqual(Vector128<int> left, Vector128<int> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vseq_w_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSEQ.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> CompareEqual(Vector128<uint> left, Vector128<uint> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x2_t vseq_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareEqual(Vector128<long> left, Vector128<long> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x2_t vseq_d_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareEqual(left, right);

        /// <summary>
        /// uint32x4_t vfcmp_ceq_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CEQ.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> CompareEqual(Vector128<float> left, Vector128<float> right) => CompareEqual(left, right);

        /// <summary>
        /// uint64x2_t vfcmp_ceq_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CEQ.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> CompareEqual(Vector128<double> left, Vector128<double> right) => CompareEqual(left, right);

        /// <summary>
        /// uint8x16_t vslt_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSLT.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x16_t vslt_bu_s8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLT.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareLessThan(Vector128<byte> left, Vector128<byte> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vslt_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSLT.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareLessThan(Vector128<short> left, Vector128<short> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint16x8_t vslt_hu_s16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLT.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThan(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vslt_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSLT.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareLessThan(Vector128<int> left, Vector128<int> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vslt_wu_s32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLT.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> CompareLessThan(Vector128<uint> left, Vector128<uint> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x2_t vslt_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThan(Vector128<long> left, Vector128<long> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x2_t vslt_du_s64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLT.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareLessThan(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint32x4_t vfcmp_clt_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLT.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> CompareLessThan(Vector128<float> left, Vector128<float> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint64x2_t vfcmp_clt_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> CompareLessThan(Vector128<double> left, Vector128<double> right) => CompareLessThan(left, right);

        /// <summary>
        /// uint8x16_t vsle_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSLE.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareLessThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vsle_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLE.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareLessThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vsle_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSLE.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareLessThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vsle_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLE.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareLessThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vsle_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSLE.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareLessThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vsle_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLE.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> CompareLessThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vsle_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareLessThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vsle_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLE.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareLessThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vfcmp_cle_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLE.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> CompareLessThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vfcmp_cle_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> CompareLessThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareLessThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vsle_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSLE.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThan(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x16_t vsle_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLE.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThan(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vsle_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSLE.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThan(Vector128<short> left, Vector128<short> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint16x8_t vsle_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLE.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThan(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vsle_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSLE.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThan(Vector128<int> left, Vector128<int> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vsle_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLE.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> CompareGreaterThan(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x2_t vsle_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThan(Vector128<long> left, Vector128<long> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x2_t vsle_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLE.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareGreaterThan(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint32x4_t vfcmp_cle_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLE.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> CompareGreaterThan(Vector128<float> left, Vector128<float> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint64x2_t vfcmp_cle_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLE.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> CompareGreaterThan(Vector128<double> left, Vector128<double> right) => CompareGreaterThan(left, right);

        /// <summary>
        /// uint8x16_t vslt_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSLT.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> CompareGreaterThanOrEqual(Vector128<sbyte> left, Vector128<sbyte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint8x16_t vslt_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSLT.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> CompareGreaterThanOrEqual(Vector128<byte> left, Vector128<byte> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vslt_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSLT.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> CompareGreaterThanOrEqual(Vector128<short> left, Vector128<short> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint16x8_t vslt_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSLT.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> CompareGreaterThanOrEqual(Vector128<ushort> left, Vector128<ushort> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vslt_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSLT.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> CompareGreaterThanOrEqual(Vector128<int> left, Vector128<int> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vslt_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSLT.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> CompareGreaterThanOrEqual(Vector128<uint> left, Vector128<uint> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vslt_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> CompareGreaterThanOrEqual(Vector128<long> left, Vector128<long> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vslt_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSLT.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> CompareGreaterThanOrEqual(Vector128<ulong> left, Vector128<ulong> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint32x4_t vfcmp_clt_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFCMP.CLT.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> CompareGreaterThanOrEqual(Vector128<float> left, Vector128<float> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// uint64x2_t vfcmp_clt_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFCMP.CLT.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> CompareGreaterThanOrEqual(Vector128<double> left, Vector128<double> right) => CompareGreaterThanOrEqual(left, right);

        /// <summary>
        /// int8x16_t vmax_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VMAX.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Max(Vector128<sbyte> left, Vector128<sbyte> right) => Max(left, right);

        /// <summary>
        /// uint8x16_t vmax_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VMAX.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Max(Vector128<byte> left, Vector128<byte> right) => Max(left, right);

        /// <summary>
        /// int16x8_t vmax_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VMAX.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Max(Vector128<short> left, Vector128<short> right) => Max(left, right);

        /// <summary>
        /// uint16x8_t vmax_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VMAX.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Max(Vector128<ushort> left, Vector128<ushort> right) => Max(left, right);

        /// <summary>
        /// int32x4_t vmax_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VMAX.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Max(Vector128<int> left, Vector128<int> right) => Max(left, right);

        /// <summary>
        /// uint32x4_t vmax_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VMAX.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Max(Vector128<uint> left, Vector128<uint> right) => Max(left, right);

        /// <summary>
        /// int64x2_t vmax_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VMAX.D Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<long> Max(Vector128<long> left, Vector128<long> right) => Max(left, right);

        /// <summary>
        /// uint64x2_t vmax_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VMAX.DU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<ulong> Max(Vector128<ulong> left, Vector128<ulong> right) => Max(left, right);

        /// <summary>
        /// float32x4_t vfmax_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFMAX.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Max(Vector128<float> left, Vector128<float> right) => Max(left, right);

        /// <summary>
        /// float64x2_t vfmax_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFMAX.d Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Max(Vector128<double> left, Vector128<double> right) => Max(left, right);

        /// <summary>
        /// int8x16_t vmin_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VMIN.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Min(Vector128<sbyte> left, Vector128<sbyte> right) => Min(left, right);

        /// <summary>
        /// uint8x16_t vmin_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VMIN.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Min(Vector128<byte> left, Vector128<byte> right) => Min(left, right);

        /// <summary>
        /// int16x8_t vmin_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VMIN.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Min(Vector128<short> left, Vector128<short> right) => Min(left, right);

        /// <summary>
        /// uint16x8_t vmin_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VMIN.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Min(Vector128<ushort> left, Vector128<ushort> right) => Min(left, right);

        /// <summary>
        /// int32x4_t vmin_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VMIN.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Min(Vector128<int> left, Vector128<int> right) => Min(left, right);

        /// <summary>
        /// uint32x4_t vmin_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VMIN.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Min(Vector128<uint> left, Vector128<uint> right) => Min(left, right);

        /// <summary>
        /// int64x2_t vmin_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VMIN.D Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<long> Min(Vector128<long> left, Vector128<long> right) => Min(left, right);

        /// <summary>
        /// uint64x2_t vmin_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VMIN.DU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<ulong> Min(Vector128<ulong> left, Vector128<ulong> right) => Min(left, right);

        /// <summary>
        /// float32x4_t vfmin_s_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VFMIN.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> Min(Vector128<float> left, Vector128<float> right) => Min(left, right);

        /// <summary>
        /// float64x2_t vfmin_d_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VFMIN.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> Min(Vector128<double> left, Vector128<double> right) => Min(left, right);

        /// <summary>
        /// int8x16_t vbitsel_v_s8 (uint8x16_t a, int8x16_t b, int8x16_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> BitwiseSelect(Vector128<sbyte> select, Vector128<sbyte> left, Vector128<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x16_t vbitsel_v_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> BitwiseSelect(Vector128<byte> select, Vector128<byte> left, Vector128<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x8_t vbitsel_v_s16 (uint16x8_t a, int16x8_t b, int16x8_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> BitwiseSelect(Vector128<short> select, Vector128<short> left, Vector128<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x8_t vbitsel_v_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> select, Vector128<ushort> left, Vector128<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x4_t vbitsel_v_s32 (uint32x4_t a, int32x4_t b, int32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> BitwiseSelect(Vector128<int> select, Vector128<int> left, Vector128<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x4_t vbitsel_v_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> BitwiseSelect(Vector128<uint> select, Vector128<uint> left, Vector128<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x2_t vbitsel_v_s64 (uint64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> BitwiseSelect(Vector128<long> select, Vector128<long> left, Vector128<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x2_t vbitsel_v_u64 (uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> BitwiseSelect(Vector128<ulong> select, Vector128<ulong> left, Vector128<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x4_t vbitsel_v_f32 (uint32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> BitwiseSelect(Vector128<float> select, Vector128<float> left, Vector128<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x2_t vbitsel_v_f64 (uint64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VBITSEL.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> BitwiseSelect(Vector128<double> select, Vector128<double> left, Vector128<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x16_t vabsd_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VABSD.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<sbyte> left, Vector128<sbyte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint8x16_t vabsd_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VABSD.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AbsoluteDifference(Vector128<byte> left, Vector128<byte> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int16x8_t vabsd_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VABSD.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<short> left, Vector128<short> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint16x8_t vabsd_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VABSD.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> AbsoluteDifference(Vector128<ushort> left, Vector128<ushort> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int32x4_t vabsd_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VABSD.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<int> left, Vector128<int> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint32x4_t vabsd_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VABSD.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> AbsoluteDifference(Vector128<uint> left, Vector128<uint> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int64x2_t vabsd_d_s64 (uint64x2_t a, int64x2_t b, int64x2_t c)
        ///   LSX: VABSD.D Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AbsoluteDifference(Vector128<long> left, Vector128<long> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// uint64x2_t vabsd_du_u64 (uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   LSX: VABSD.DU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AbsoluteDifference(Vector128<ulong> left, Vector128<ulong> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float32x4_t TODO_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: TODO Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> AbsoluteDifference(Vector128<float> left, Vector128<float> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// float64x2_t TODO_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: TODO Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> AbsoluteDifference(Vector128<double> left, Vector128<double> right) => AbsoluteDifference(left, right);

        /// <summary>
        /// int8x16_t vld_s8 (int8_t const * ptr)
        ///   LSX: VLD Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) => LoadVector128(address);

        /// <summary>
        /// uint8x16_t vld_u8 (uint8_t const * ptr)
        ///   LSX: VLD Vd.16B, Rj, si12
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) => LoadVector128(address);

        /// <summary>
        /// int16x8_t vld_s16 (int16_t const * ptr)
        ///   LSX: VLD Vd.8H, Rj, si12
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) => LoadVector128(address);

        /// <summary>
        /// uint16x8_t vld_s16 (uint16_t const * ptr)
        ///   LSX: VLD Vd.8H, Rj, si12
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => LoadVector128(address);

        /// <summary>
        /// int32x4_t vld_s32 (int32_t const * ptr)
        ///   LSX: VLD Vd.4S, Rj, si12
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) => LoadVector128(address);

        /// <summary>
        /// uint32x4_t vld_s32 (uint32_t const * ptr)
        ///   LSX: VLD Vd.4S, Rj, si12
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) => LoadVector128(address);

        /// <summary>
        /// int64x2_t vld_s64 (int64_t const * ptr)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) => LoadVector128(address);

        /// <summary>
        /// uint64x2_t vld_u64 (uint64_t const * ptr)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) => LoadVector128(address);

        /// <summary>
        /// float32x4_t vld_f32 (float32_t const * ptr)
        ///   LSX: VLD Vd.4S, Rj, si12
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) => LoadVector128(address);

        /// <summary>
        /// float64x2_t vld_f64 (float64_t const * ptr)
        ///   LSX: VLD Vd.2D, Rj, si12
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) => LoadVector128(address);

        /// <summary>
        /// float32x4_t vfrecip_s_f32 (float32x4_t a)
        ///   LSX: VFRECIP.S Vd.4S Vj.4S
        /// </summary>
        public static Vector128<float> Reciprocal(Vector128<float> value) => Reciprocal(value);

        /// <summary>
        /// float64x2_t vfrecip_d_f64 (float64x2_t a)
        ///   LSX: VFRECIP.D Vd.2D Vj.2D
        /// </summary>
        public static Vector128<double> Reciprocal(Vector128<double> value) => Reciprocal(value);

        /// <summary>
        /// float32x4_t vfrsqrt_s_f32 (float32x4_t a)
        ///   LSX: VFRSQRT.S Vd.4S Vj.4S
        /// </summary>
        public static Vector128<float> ReciprocalSqrt(Vector128<float> value) => ReciprocalSqrt(value);

        /// <summary>
        /// float64x2_t vfrsqrt_d_f64 (float64x2_t a)
        ///   LSX: VFRSQRT.D Vd.2D Vj.2D
        /// </summary>
        public static Vector128<double> ReciprocalSqrt(Vector128<double> value) => ReciprocalSqrt(value);

        /// <summary>
        /// void vst_s8 (int8_t * ptr, int8x16_t val)
        ///   LSX: VST { Vd.16B }, Rj, si12
        /// </summary>
        public static unsafe void Store(sbyte* address, Vector128<sbyte> source) => Store(address, source);

        /// <summary>
        /// void vst_u8 (uint8_t * ptr, uint8x16_t val)
        ///   LSX: VST { Vd.16B }, Rj, si12
        /// </summary>
        public static unsafe void Store(byte* address, Vector128<byte> source) => Store(address, source);

        /// <summary>
        /// void vst_s16 (int16_t * ptr, int16x8_t val)
        ///   LSX: VST { Vd.8H }, Rj, si12
        /// </summary>
        public static unsafe void Store(short* address, Vector128<short> source) => Store(address, source);

        /// <summary>
        /// void vst_u16 (uint16_t * ptr, uint16x8_t val)
        ///   LSX: VST { Vd.8H }, Rj, si12
        /// </summary>
        public static unsafe void Store(ushort* address, Vector128<ushort> source) => Store(address, source);

        /// <summary>
        /// void vst_s32 (int32_t * ptr, int32x4_t val)
        ///   LSX: VST { Vd.4S }, Rj, si12
        /// </summary>
        public static unsafe void Store(int* address, Vector128<int> source) => Store(address, source);

        /// <summary>
        /// void vst_u32 (uint32_t * ptr, uint32x4_t val)
        ///   LSX: VST { Vd.4S }, Rj, si12
        /// </summary>
        public static unsafe void Store(uint* address, Vector128<uint> source) => Store(address, source);

        /// <summary>
        /// void vst_s64 (int64_t * ptr, int64x2_t val)
        ///   LSX: VST { Vd.2D }, Rj, si12
        /// </summary>
        public static unsafe void Store(long* address, Vector128<long> source) => Store(address, source);

        /// <summary>
        /// void vst_u64 (uint64_t * ptr, uint64x2_t val)
        ///   LSX: VST { Vd.2D }, Rj, si12
        /// </summary>
        public static unsafe void Store(ulong* address, Vector128<ulong> source) => Store(address, source);

        /// <summary>
        /// void vst_f32 (float32_t * ptr, float32x4_t val)
        ///   LSX: VST { Vd.4S }, Rj, si12
        /// </summary>
        public static unsafe void Store(float* address, Vector128<float> source) => Store(address, source);

        /// <summary>
        /// void vst_f64 (float64_t * ptr, float64x2_t val)
        ///   LSX: VST { Vd.2D }, Rj, si12
        /// </summary>
        public static unsafe void Store(double* address, Vector128<double> source) => Store(address, source);

        /// <summary>
        /// int8x16_t vneg_b_s8 (int8x16_t a)
        ///   LSX: VNEG.B Vd.16B, Vj.16B
        /// </summary>
        public static Vector128<sbyte> Negate(Vector128<sbyte> value) => Negate(value);

        /// <summary>
        /// int16x8_t vneg_h_s16 (int16x8_t a)
        ///   LSX: VNEG.H Vd.8H, Vj.8H
        /// </summary>
        public static Vector128<short> Negate(Vector128<short> value) => Negate(value);

        /// <summary>
        /// int32x4_t vneg_w_s32 (int32x4_t a)
        ///   LSX: VNEG.W Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<int> Negate(Vector128<int> value) => Negate(value);

        /// <summary>
        /// int64x2_t vneg_d_s64 (int64x2_t a)
        ///   LSX: VNEG.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<long> Negate(Vector128<long> value) => Negate(value);

        /// <summary>
        /// float32x4_t TODO_f32 (float32x4_t a)
        ///   LSX: TODO Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Negate(Vector128<float> value) => Negate(value);

        /// <summary>
        /// float64x2_t TODO_f64 (float64x2_t a)
        ///   LSX: TODO Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Negate(Vector128<double> value) => Negate(value);

        /// <summary>
        /// float32x4_t vfmsub_s_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        ///   LSX: VFMSUB.S Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> FusedMultiplySubtract(Vector128<float> minuend, Vector128<float> left, Vector128<float> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// float64x2_t vfmsub_d_f64 (float64x2_t a, float64x2_t b, float64x2_t c)
        ///   LSX: VFMSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> FusedMultiplySubtract(Vector128<double> minuend, Vector128<double> left, Vector128<double> right) => FusedMultiplySubtract(minuend, left, right);

        /// <summary>
        /// int16x8_t vmulwod_h_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VMULWOD.H.B Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> MultiplyWideningUpper(Vector128<sbyte> left, Vector128<sbyte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint16x8_t vmulwod_h_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VMULWOD.H.BU Vd.8H, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> MultiplyWideningUpper(Vector128<byte> left, Vector128<byte> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int32x4_t vmulwod_w_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VMULWOD.W.H Vd.4S, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<int> MultiplyWideningUpper(Vector128<short> left, Vector128<short> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint32x4_t vmulwod_w_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VMULWOD.W.HU Vd.4S, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<uint> MultiplyWideningUpper(Vector128<ushort> left, Vector128<ushort> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int64x2_t vmulwod_d_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VMULWOD.D.W Vd.2D, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<long> MultiplyWideningUpper(Vector128<int> left, Vector128<int> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// uint64x2_t vmulwod_d_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VMULWOD.D.WU Vd.2D, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<ulong> MultiplyWideningUpper(Vector128<uint> left, Vector128<uint> right) => MultiplyWideningUpper(left, right);

        /// <summary>
        /// int8x16_t vssub_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VSSUB.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> SubtractSaturate(Vector128<sbyte> left, Vector128<sbyte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint8x16_t vssub_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VSSUB.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> SubtractSaturate(Vector128<byte> left, Vector128<byte> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int16x8_t vssub_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VSSUB.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> SubtractSaturate(Vector128<short> left, Vector128<short> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint16x8_t vssub_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VSSUB.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> SubtractSaturate(Vector128<ushort> left, Vector128<ushort> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int32x4_t vssub_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VSSUB.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> SubtractSaturate(Vector128<int> left, Vector128<int> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint32x4_t vssub_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VSSUB.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> SubtractSaturate(Vector128<uint> left, Vector128<uint> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int64x2_t vssub_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VSSUB.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> SubtractSaturate(Vector128<long> left, Vector128<long> right) => SubtractSaturate(left, right);

        /// <summary>
        /// uint64x2_t vssub_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VSSUB.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> SubtractSaturate(Vector128<ulong> left, Vector128<ulong> right) => SubtractSaturate(left, right);

        /// <summary>
        /// int8x16_t vavg_b_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VAVG.B Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Average(Vector128<sbyte> left, Vector128<sbyte> right) => Average(left, right);

        /// <summary>
        /// uint8x16_t vavg_bu_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VAVG.BU Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Average(Vector128<byte> left, Vector128<byte> right) => Average(left, right);

        /// <summary>
        /// int16x8_t vavg_h_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VAVG.H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> Average(Vector128<short> left, Vector128<short> right) => Average(left, right);

        /// <summary>
        /// uint16x8_t vavg_hu_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VAVG.HU Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> Average(Vector128<ushort> left, Vector128<ushort> right) => Average(left, right);

        /// <summary>
        /// int32x4_t vavg_w_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VAVG.W Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<int> Average(Vector128<int> left, Vector128<int> right) => Average(left, right);

        /// <summary>
        /// uint32x4_t vavg_wu_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VAVG.WU Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<uint> Average(Vector128<uint> left, Vector128<uint> right) => Average(left, right);

        /// <summary>
        /// int64x2_t vavg_d_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VAVG.D Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> Average(Vector128<long> left, Vector128<long> right) => Average(left, right);

        /// <summary>
        /// uint64x2_t vavg_du_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VAVG.DU Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> Average(Vector128<ulong> left, Vector128<ulong> right) => Average(left, right);

        /// <summary>
        /// int16x8_t vext2xv_h_b_s8 (int8x16_t a)
        ///   LSX: VEXT2XV.H.B Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> SignExtendWideningLower(Vector128<sbyte> value) => SignExtendWideningLower(value);

        ///// <summary>
        ///// int32x4_t vext2xv_w_b_s8 (int8x16_t a)
        /////   LSX: VEXT2XV.W.B Vd.4W, Vj.16B
        ///// </summary>
        //public static Vector128<int> SignExtendWideningLower(Vector128<sbyte> value) => SignExtendWideningLower(value);

        ///// <summary>
        ///// int64x2_t vext2xv_d_b_s8 (int8x16_t a)
        /////   LSX: VEXT2XV.D.B Vd.2D, Vj.16B
        ///// </summary>
        //public static Vector128<long> SignExtendWideningLower(Vector128<sbyte> value) => SignExtendWideningLower(value);

        /// <summary>
        /// int32x4_t vext2xv_w_h_s16 (int16x8_t a)
        ///   LSX: VEXT2XV.W.H Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<int> SignExtendWideningLower(Vector128<short> value) => SignExtendWideningLower(value);

        ///// <summary>
        ///// int64x2_t vext2xv_d_h_s16 (int16x8_t a)
        /////   LSX: VEXT2XV.D.H Vd.2D, Vj.8H
        ///// </summary>
        //public static Vector128<long> SignExtendWideningLower(Vector128<short> value) => SignExtendWideningLower(value);

        /// <summary>
        /// int64x2_t vext2xv_d_w_s32 (int32x4_t a)
        ///   LSX: VEXT2XV.D.W Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<long> SignExtendWideningLower(Vector128<int> value) => SignExtendWideningLower(value);

        /// <summary>
        /// uint16x8_t vext2xv_hu_bu_u8 (uint8x16_t a)
        ///   LSX: VEXT2XV.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningLower(Vector128<byte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// int16x8_t vext2xv_hu_bu_u8 (int8x16_t a)
        ///   LSX: VEXT2XV.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> ZeroExtendWideningLower(Vector128<sbyte> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint32x4_t vext2xv_wu_hu_u16 (uint16x8_t a)
        ///   LSX: VEXT2XV.WU.HU Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningLower(Vector128<ushort> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// int32x4_t vext2xv_wu_hu_u16 (int16x8_t a)
        ///   LSX: VEXT2XV.WU.HU Vd.4W, Vj.8H
        /// </summary>
        public static Vector128<int> ZeroExtendWideningLower(Vector128<short> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// uint64x2_t vext2xv_du_wu_u32 (uint32x4_t a)
        ///   LSX: VEXT2XV.DU.WU Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningLower(Vector128<uint> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// int64x2_t vext2xv_du_wu_u32 (int32x4_t a)
        ///   LSX: VEXT2XV.DU.WU Vd.2D, Vj.4W
        /// </summary>
        public static Vector128<long> ZeroExtendWideningLower(Vector128<int> value) => ZeroExtendWideningLower(value);

        /// <summary>
        /// int16x8_t vexth_h_b_s8 (int8x16_t a)
        ///   LSX: VEXTH.H.B Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> SignExtendWideningUpper(Vector128<sbyte> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int32x4_t vexth_w_h_s16 (int16x8_t a)
        ///   LSX: VEXTH.W.H Vd.4S, Vj.8H
        /// </summary>
        public static Vector128<int> SignExtendWideningUpper(Vector128<short> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// int64x2_t vexth_d_w_s32 (int32x4_t a)
        ///   LSX: VEXTH.D.W Vd.2D, Vj.4S
        /// </summary>
        public static Vector128<long> SignExtendWideningUpper(Vector128<int> value) => SignExtendWideningUpper(value);

        /// <summary>
        /// uint16x8_t vexth_HU_BU_u8 (uint8x16_t a)
        ///   LSX: VEXTH.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<short> ZeroExtendWideningUpper(Vector128<sbyte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint16x8_t vexth_HU_BU_u8 (uint8x16_t a)
        ///   LSX: VEXTH.HU.BU Vd.8H, Vj.16B
        /// </summary>
        public static Vector128<ushort> ZeroExtendWideningUpper(Vector128<byte> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x4_t vexth_WU_HU_u16 (uint16x8_t a)
        ///   LSX: VEXTH.WU.HU Vd.4S, Vj.8H
        /// </summary>
        public static Vector128<int> ZeroExtendWideningUpper(Vector128<short> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint32x4_t vexth_WU_HU_u16 (uint16x8_t a)
        ///   LSX: VEXTH.WU.HU Vd.4S, Vj.8H
        /// </summary>
        public static Vector128<uint> ZeroExtendWideningUpper(Vector128<ushort> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x2_t vexth_DU_WU_u32 (uint32x4_t a)
        ///   LSX: VEXTH.DU.WU Vd.2D, Vj.4S
        /// </summary>
        public static Vector128<long> ZeroExtendWideningUpper(Vector128<int> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// uint64x2_t vexth_DU_WU_u32 (uint32x4_t a)
        ///   LSX: VEXTH.DU.WU Vd.2D, Vj.4S
        /// </summary>
        public static Vector128<ulong> ZeroExtendWideningUpper(Vector128<uint> value) => ZeroExtendWideningUpper(value);

        /// <summary>
        /// int8x16_t vand_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);

        /// <summary>
        /// uint8x16_t vand_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);

        /// <summary>
        /// int16x8_t vand_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);

        /// <summary>
        /// uint16x8_t vand_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);

        /// <summary>
        /// int32x4_t vand_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);

        /// <summary>
        /// uint32x4_t vand_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);

        /// <summary>
        /// int64x2_t vand_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);

        /// <summary>
        /// uint64x2_t vand_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);

        /// <summary>
        /// float32x4_t vand_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        /// float64x2_t vand_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VAND.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        /// int8x16_t vandn_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// uint8x16_t vandn_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) => AndNot(left, right);

        /// <summary>
        /// int16x8_t vandn_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) => AndNot(left, right);

        /// <summary>
        /// uint16x8_t vandn_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);

        /// <summary>
        /// int32x4_t vandn_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) => AndNot(left, right);

        /// <summary>
        /// uint32x4_t vandn_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) => AndNot(left, right);

        /// <summary>
        /// int64x2_t vandn_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) => AndNot(left, right);

        /// <summary>
        /// uint64x2_t vandn_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) => AndNot(left, right);

        /// <summary>
        /// float32x4_t vandn_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) => AndNot(left, right);

        /// <summary>
        /// float64x2_t vandn_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VANDN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);

        /// <summary>
        /// int8x16_t vor_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);

        /// <summary>
        /// uint8x16_t vor_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);

        /// <summary>
        /// int16x8_t vor_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);

        /// <summary>
        /// uint16x8_t vor_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);

        /// <summary>
        /// int32x4_t vor_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);

        /// <summary>
        /// uint32x4_t vor_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);

        /// <summary>
        /// int64x2_t vor_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);

        /// <summary>
        /// uint64x2_t vor_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);

        /// <summary>
        /// float32x4_t vor_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        /// float64x2_t vor_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        /// int8x16_t vor_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VNOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> NotOr(Vector128<sbyte> left, Vector128<sbyte> right) => NotOr(left, right);

        /// <summary>
        /// uint8x16_t vor_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VNOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> NotOr(Vector128<byte> left, Vector128<byte> right) => NotOr(left, right);

        /// <summary>
        /// int16x8_t vor_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VNOR.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> NotOr(Vector128<short> left, Vector128<short> right) => NotOr(left, right);

        /// <summary>
        /// uint16x8_t vor_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VNOR.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> NotOr(Vector128<ushort> left, Vector128<ushort> right) => NotOr(left, right);

        /// <summary>
        /// int32x4_t vor_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VNOR.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> NotOr(Vector128<int> left, Vector128<int> right) => NotOr(left, right);

        /// <summary>
        /// uint32x4_t vor_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VNOR.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> NotOr(Vector128<uint> left, Vector128<uint> right) => NotOr(left, right);

        /// <summary>
        /// int64x2_t vor_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> NotOr(Vector128<long> left, Vector128<long> right) => NotOr(left, right);

        /// <summary>
        /// uint64x2_t vor_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> NotOr(Vector128<ulong> left, Vector128<ulong> right) => NotOr(left, right);

        /// <summary>
        /// float32x4_t vor_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VNOR.V Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> NotOr(Vector128<float> left, Vector128<float> right) => NotOr(left, right);

        /// <summary>
        /// float64x2_t vor_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VNOR.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> NotOr(Vector128<double> left, Vector128<double> right) => NotOr(left, right);

        /// <summary>
        /// int8x16_t vor_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// uint8x16_t vor_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VORN.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) => OrNot(left, right);

        /// <summary>
        /// int16x8_t vor_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VORN.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) => OrNot(left, right);

        /// <summary>
        /// uint16x8_t vor_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VORN.V Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) => OrNot(left, right);

        /// <summary>
        /// int32x4_t vor_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VORN.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) => OrNot(left, right);

        /// <summary>
        /// uint32x4_t vor_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VORN.V Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) => OrNot(left, right);

        /// <summary>
        /// int64x2_t vor_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) => OrNot(left, right);

        /// <summary>
        /// uint64x2_t vor_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> OrNot(Vector128<ulong> left, Vector128<ulong> right) => OrNot(left, right);

        /// <summary>
        /// float32x4_t vor_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VORN.V Vd.4S, Vj.4S, Vk.4S
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) => OrNot(left, right);

        /// <summary>
        /// float64x2_t vor_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VORN.V Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) => OrNot(left, right);

        /// <summary>
        /// int8x16_t vxor_v_s8 (int8x16_t a, int8x16_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);

        /// <summary>
        /// uint8x16_t vxor_v_u8 (uint8x16_t a, uint8x16_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);

        /// <summary>
        /// int16x8_t vxor_v_s16 (int16x8_t a, int16x8_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);

        /// <summary>
        /// uint16x8_t vxor_v_u16 (uint16x8_t a, uint16x8_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);

        /// <summary>
        /// int32x4_t vxor_v_s32 (int32x4_t a, int32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);

        /// <summary>
        /// uint32x4_t vxor_v_u32 (uint32x4_t a, uint32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);

        /// <summary>
        /// int64x2_t vxor_v_s64 (int64x2_t a, int64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);

        /// <summary>
        /// uint64x2_t vxor_v_u64 (uint64x2_t a, uint64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);

        /// <summary>
        /// float32x4_t vxor_v_f32 (float32x4_t a, float32x4_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);

        /// <summary>
        /// float64x2_t vxor_v_f64 (float64x2_t a, float64x2_t b)
        ///   LSX: VXOR.V Vd.16B, Vj.16B, Vk.16B
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t vslli_b_s8 (int8x16_t a, const int n)
        ///   LSX: VSLLI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftLeftLogical(Vector128<sbyte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x16_t vslli_b_u8 (uint8x16_t a, const int n)
        ///   LSX: VSLLI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftLeftLogical(Vector128<byte> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int16x8_t vslli_h_s16 (int16x8_t a, const int n)
        ///   LSX: VSLLI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<short> ShiftLeftLogical(Vector128<short> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint16x8_t vslli_h_u16 (uint16x8_t a, const int n)
        ///   LSX: VSLLI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftLeftLogical(Vector128<ushort> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x4_t vslli_w_s32 (uint32x4_t a, const int n)
        ///   LSX: VSLLI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<int> ShiftLeftLogical(Vector128<int> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint32x4_t vslli_w_u32 (uint32x4_t a, const int n)
        ///   LSX: VSLLI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftLeftLogical(Vector128<uint> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// int64x2_t vslli_d_s64 (int64x2_t a, const int n)
        ///   LSX: VSLLI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<long> ShiftLeftLogical(Vector128<long> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint64x2_t vslli_d_u64 (uint64x2_t a, const int n)
        ///   LSX: VSLLI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftLeftLogical(Vector128<ulong> value, byte count) => ShiftLeftLogical(value, count);

        /// <summary>
        /// uint8x16_t vsrli_b_u8 (uint8x16_t a, const int n)
        ///   LSX: VSRLI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogical(Vector128<sbyte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x16_t vsrli_b_u8 (uint8x16_t a, const int n)
        ///   LSX: VSRLI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogical(Vector128<byte> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x8_t vsrli_h_u16 (uint16x8_t a, const int n)
        ///   LSX: VSRLI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogical(Vector128<short> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint16x8_t vsrli_h_u16 (uint16x8_t a, const int n)
        ///   LSX: VSRLI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogical(Vector128<ushort> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x4_t vsrli_w_u32 (uint32x4_t a, const int n)
        ///   LSX: VSRLI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogical(Vector128<int> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint32x4_t vsrli_w_u32 (uint32x4_t a, const int n)
        ///   LSX: VSRLI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogical(Vector128<uint> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x2_t vsrli_d_u64 (uint64x2_t a, const int n)
        ///   LSX: VSRLI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogical(Vector128<long> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint64x2_t vsrli_d_u64 (uint64x2_t a, const int n)
        ///   LSX: VSRLI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogical(Vector128<ulong> value, byte count) => ShiftRightLogical(value, count);

        /// <summary>
        /// uint8x16_t vsrlri_b_u8 (uint8x16_t a, const int n)
        ///   LSX: VSRLRI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightLogicalRounded(Vector128<sbyte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint8x16_t vsrlri_b_u8 (uint8x16_t a, const int n)
        ///   LSX: VSRLRI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalRounded(Vector128<byte> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vsrlri_h_u16 (uint16x8_t a, const int n)
        ///   LSX: VSRLRI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightLogicalRounded(Vector128<short> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint16x8_t vsrlri_h_u16 (uint16x8_t a, const int n)
        ///   LSX: VSRLRI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalRounded(Vector128<ushort> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vsrlri_w_u32 (uint32x4_t a, const int n)
        ///   LSX: VSRLRI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightLogicalRounded(Vector128<int> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint32x4_t vsrlri_w_u32 (uint32x4_t a, const int n)
        ///   LSX: VSRLRI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalRounded(Vector128<uint> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vsrlri_d_u64 (uint64x2_t a, const int n)
        ///   LSX: VSRLRI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightLogicalRounded(Vector128<long> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// uint64x2_t vsrlri_d_u64 (uint64x2_t a, const int n)
        ///   LSX: VSRLRI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<ulong> ShiftRightLogicalRounded(Vector128<ulong> value, byte count) => ShiftRightLogicalRounded(value, count);

        /// <summary>
        /// int8x16_t vsrai_b_s8 (int8x16_t a, const int n)
        ///   LSX: VSRAI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmetic(Vector128<sbyte> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int16x8_t vsrai_h_s16 (int16x8_t a, const int n)
        ///   LSX: VSRAI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmetic(Vector128<short> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int32x4_t vsrai_w_s32 (int32x4_t a, const int n)
        ///   LSX: VSRAI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmetic(Vector128<int> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int64x2_t vsrai_d_s64 (int64x2_t a, const int n)
        ///   LSX: VSRAI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmetic(Vector128<long> value, byte count) => ShiftRightArithmetic(value, count);

        /// <summary>
        /// int8x16_t vsrari_b_s8 (int8x16_t a, const int n)
        ///   LSX: VSRARI.B Vd.16B, Vj.16B, #n
        /// </summary>
        public static Vector128<sbyte> ShiftRightArithmeticRounded(Vector128<sbyte> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int16x8_t vsrari_h_s16 (int16x8_t a, const int n)
        ///   LSX: VSRARI.H Vd.8H, Vj.8H, #n
        /// </summary>
        public static Vector128<short> ShiftRightArithmeticRounded(Vector128<short> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int32x4_t vsrari_w_s32 (int32x4_t a, const int n)
        ///   LSX: VSRARI.W Vd.4S, Vj.4S, #n
        /// </summary>
        public static Vector128<int> ShiftRightArithmeticRounded(Vector128<int> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int64x2_t vsrari_d_s64 (int64x2_t a, const int n)
        ///   LSX: VSRARI.D Vd.2D, Vj.2D, #n
        /// </summary>
        public static Vector128<long> ShiftRightArithmeticRounded(Vector128<long> value, byte count) => ShiftRightArithmeticRounded(value, count);

        /// <summary>
        /// int8x16_t vadda_b_s8 (int8x16_t a)
        ///   LSX: VADDA.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);

        /// <summary>
        /// int16x8_t vadda_h_s16 (int16x8_t a)
        ///   LSX: VADDA.H Vd.8H, Vj.8H, 0
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);

        /// <summary>
        /// int32x4_t vadda_w_s32 (int32x4_t a)
        ///   LSX: VADDA.W Vd.4S, Vj.4S, 0
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        /// int64x2_t vdda_d_s64 (int64x2_t a)
        ///   LSX: VADDA.D Vd.2D, Vj.2D, 0
        /// </summary>
        public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);

        /// <summary>
        /// float32x4_t vbitclri_w_f32 (float32x4_t a)
        ///   LSX: VBITCLRI.W Vd.4S, Vj.4S, 31
        /// </summary>
        public static Vector128<float> Abs(Vector128<float> value) => Abs(value);

        /// <summary>
        /// float64x2_t vbitclri_d_f64 (float64x2_t a)
        ///   LSX: VBITCLRI.D Vd.2D, Vj.2D, 63
        /// </summary>
        public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

        /// <summary>
        /// float32x4_t vfsqrt_s_f32 (float32x4_t a)
        ///   LSX: VFSQRT.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Sqrt(Vector128<float> value) => Sqrt(value);

        /// <summary>
        /// float64x2_t vfsqrt_d_f64 (float64x2_t a)
        ///   LSX: VFSQRT.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Sqrt(Vector128<double> value) => Sqrt(value);

        /// <summary>
        /// float32x4_t vfrintrm_s_f32 (float32x4_t a)
        ///   LSX: VFRINTRM.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Floor(Vector128<float> value) => Floor(value);

        /// <summary>
        /// float64x2_t vfrintrm_d_f64 (float64x2_t a)
        ///   LSX: VFRINTRM.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Floor(Vector128<double> value) => Floor(value);

        /// <summary>
        /// float32x4_t vfrintrp_s_f32 (float32x4_t a)
        ///   LSX: VFRINTRP.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> Ceiling(Vector128<float> value) => Ceiling(value);

        /// <summary>
        /// float64x2_t vfrintrp_d_f64 (float64x2_t a)
        ///   LSX: VFRINTRP.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> Ceiling(Vector128<double> value) => Ceiling(value);

        /// <summary>
        /// float32x4_t vfrintrz_s_f32 (float32x4_t a)
        ///   LSX: VFRINTRZ.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToZero(Vector128<float> value) => RoundToZero(value);

        /// <summary>
        /// float64x2_t vfrintrz_d_f64 (float64x2_t a)
        ///   LSX: VFRINTRZ.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToZero(Vector128<double> value) => RoundToZero(value);

        /// <summary>
        /// float32x4_t vfrintrm_s_f32 (float32x4_t a)
        ///   LSX: VFRINTRM.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToNegativeInfinity(Vector128<float> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float64x2_t vfrintrm_d_f64 (float64x2_t a)
        ///   LSX: VFRINTRM.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToNegativeInfinity(Vector128<double> value) => RoundToNegativeInfinity(value);

        /// <summary>
        /// float32x4_t vfrintrp_s_f32 (float32x4_t a)
        ///   LSX: VFRINTRP.S Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> RoundToPositiveInfinity(Vector128<float> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// float64x2_t vfrintrp_d_f64 (float64x2_t a)
        ///   LSX: VFRINTRP.D Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> RoundToPositiveInfinity(Vector128<double> value) => RoundToPositiveInfinity(value);

        /// <summary>
        /// int8x16_t vinsgr2vr_b_s8 (int8_t a, int8x16_t v, const int imm)
        ///   LSX: VINSGR2VR.B Vd.B[imm], Rj, imm
        /// </summary>
        public static Vector128<sbyte> Insert(Vector128<sbyte> vector, byte index, sbyte data) => Insert(vector, index, data);

        /// <summary>
        /// uint8x16_t vinsgr2vr_b_u8 (uint8_t a, uint8x16_t v, const int imm)
        ///   LSX: VINSGR2VR.B Vd.B[imm], Rj, imm
        /// </summary>
        public static Vector128<byte> Insert(Vector128<byte> vector, byte index, byte data) => Insert(vector, index, data);

        /// <summary>
        /// int16x8_t vinsgr2vr_h_s16 (int16_t a, int16x8_t v, const int imm)
        ///   LSX: VINSGR2VR.H Vd.H[imm], Rj, imm
        /// </summary>
        public static Vector128<short> Insert(Vector128<short> vector, byte index, short data) => Insert(vector, index, data);

        /// <summary>
        /// uint16x8_t vinsgr2vr_h_u16 (uint16_t a, uint16x8_t v, const int imm)
        ///   LSX: VINSGR2VR.H Vd.H[imm], Rj, imm
        /// </summary>
        public static Vector128<ushort> Insert(Vector128<ushort> vector, byte index, ushort data) => Insert(vector, index, data);

        /// <summary>
        /// int32x4_t vinsgr2vr_w_s32 (int32_t a, int32x4_t v, const int imm)
        ///   LSX: VINSGR2VR.W Vd.S[imm], Rj, imm
        /// </summary>
        public static Vector128<int> Insert(Vector128<int> vector, byte index, int data) => Insert(vector, index, data);

        /// <summary>
        /// uint32x4_t vinsgr2vr_w_u32 (uint32_t a, uint32x4_t v, const int imm)
        ///   LSX: VINSGR2VR.W Vd.S[imm], Rj, imm
        /// </summary>
        public static Vector128<uint> Insert(Vector128<uint> vector, byte index, uint data) => Insert(vector, index, data);

        /// <summary>
        /// int64x2_t vinsgr2vr_d_s64 (int64_t a, int64x2_t v, const int imm)
        ///   LSX: VINSGR2VR.D Vd.D[imm], Rj, imm
        /// </summary>
        public static Vector128<long> Insert(Vector128<long> vector, byte index, long data) => Insert(vector, index, data);

        /// <summary>
        /// uint64x2_t vinsgr2vr_d_u64 (uint64_t a, uint64x2_t v, const int imm)
        ///   LSX: VINSGR2VR.D Vd.D[imm], Rj, imm
        /// </summary>
        public static Vector128<ulong> Insert(Vector128<ulong> vector, byte index, ulong data) => Insert(vector, index, data);

        /// <summary>
        /// float32x4_t xvinsve0_w_f32 (float32_t a, float32x4_t v, const int imm)
        ///   LSX: XVINSVE0.W Vd.S[imm], Vj.S[0], imm
        /// </summary>
        public static Vector128<float> Insert(Vector128<float> vector, byte index, float data) => Insert(vector, index, data);

        /// <summary>
        /// float64x2_t xvinsve0_d_f64 (float64_t a, float64x2_t v, const int imm)
        ///   LSX: XVINSVE0.D Vd.D[imm], Vj.D[0], imm
        /// </summary>
        public static Vector128<double> Insert(Vector128<double> vector, byte index, double data) => Insert(vector, index, data);

        /// <summary>
        /// int8x16_t vreplgr2vr_b_s8 (int8_t value)
        ///   LSX: VREPLGR2VR.B Vd.16B, Rj
        /// </summary>
        public static Vector128<sbyte> DuplicateToVector128(sbyte value) => DuplicateToVector128(value);

        /// <summary>
        /// uint8x16_t vreplgr2vr_b_u8 (uint8_t value)
        ///   LSX: VREPLGR2VR.B Vd.16B, Rj
        /// </summary>
        public static Vector128<byte> DuplicateToVector128(byte value) => DuplicateToVector128(value);

        /// <summary>
        /// int16x8_t vreplgr2vr_h_s16 (int16_t value)
        ///   LSX: VREPLGR2VR.H Vd.8H, Rj
        /// </summary>
        public static Vector128<short> DuplicateToVector128(short value) => DuplicateToVector128(value);

        /// <summary>
        /// uint16x8_t vreplgr2vr_h_u16 (uint16_t value)
        ///   LSX: VREPLGR2VR.H Vd.8H, Rj
        /// </summary>
        public static Vector128<ushort> DuplicateToVector128(ushort value) => DuplicateToVector128(value);

        /// <summary>
        /// int32x4_t vreplgr2vr_w_s32 (int32_t value)
        ///   LSX: VREPLGR2VR.W Vd.4S, Rj
        /// </summary>
        public static Vector128<int> DuplicateToVector128(int value) => DuplicateToVector128(value);

        /// <summary>
        /// uint32x4_t vreplgr2vr_w_u32 (uint32_t value)
        ///   LSX: VREPLGR2VR.W Vd.4S, Rj
        /// </summary>
        public static Vector128<uint> DuplicateToVector128(uint value) => DuplicateToVector128(value);

        /// <summary>
        /// int64x2_t vreplgr2vr_d_s64 (int64_t value)
        ///   LSX: VREPLGR2VR.D Vd.2D, Rj
        /// </summary>
        public static Vector128<long> DuplicateToVector128(long value) => DuplicateToVector128(value);

        /// <summary>
        /// uint64x2_t vreplgr2vr_d_u64 (uint64_t value)
        ///   LSX: VREPLGR2VR.D Vd.2D, Rj
        /// </summary>
        public static Vector128<ulong> DuplicateToVector128(ulong value) => DuplicateToVector128(value);

        /// <summary>
        /// float32x4_t xvreplve0_w_f32 (float32_t value)
        ///   LSX: XVREPLVE0.W Vd.4S, Vj.S[0]
        /// </summary>
        public static Vector128<float> DuplicateToVector128(float value) => DuplicateToVector128(value);

        /// <summary>
        /// float64x2_t xvreplve0_d_f64 (float64_t value)
        ///   LSX: XVREPLVE0.D Vd.2D, Vj.D[0]
        /// </summary>
        public static Vector128<double> DuplicateToVector128(double value) => DuplicateToVector128(value);

        /// <summary>
        /// float32x4_t vffint_s_w_f32_s32 (int32x4_t a)
        ///   LSX: VFFINT.S.W Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> ConvertToSingle(Vector128<int> value) => ConvertToSingle(value);

        /// <summary>
        /// float32x4_t vffint_s_wu_f32_u32 (uint32x4_t a)
        ///   LSX: VFFINT.S.WU Vd.4S, Vj.4S
        /// </summary>
        public static Vector128<float> ConvertToSingle(Vector128<uint> value) => ConvertToSingle(value);

        /// <summary>
        /// float64x2_t vffint_d_l_f64_s64 (int64x2_t a)
        ///   LSX: VFFINT.D.L Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> ConvertToDouble(Vector128<long> value) => ConvertToDouble(value);

        /// <summary>
        /// float64x2_t vffint_d_lu_f64_u64 (uint64x2_t a)
        ///   LSX: VFFINT.D.LU Vd.2D, Vj.2D
        /// </summary>
        public static Vector128<double> ConvertToDouble(Vector128<ulong> value) => ConvertToDouble(value);

        /// <summary>
        /// int8_t vfsrtpi_b_u8 (uint8x16_t value)
        ///   LSX: VFSRTPI.B Vd.16B, Vj.16B, 0
        /// </summary>
        public static byte FirstNegativeInteger(Vector128<byte> value) => FirstNegativeInteger(value);

        /// <summary>
        /// int16_t vfsrtpi_h_u16 (uint16x8_t value)
        ///   LSX: VFSRTPI.H Vd.8H, Vj.8H, 0
        /// </summary>
        public static ushort FirstNegativeInteger(Vector128<ushort> value) => FirstNegativeInteger(value);

        /// <summary>
        /// bool vsetnez_v_u8 (uint8x16_t value)
        ///   LSX: VSETNEZ.V cd, Vj.16B
        /// </summary>
        public static bool HasElementsNotZero(Vector128<byte> value) => HasElementsNotZero(value);

        /// <summary>
        /// bool vseteqz_v_u8 (uint8x16_t value)
        ///   LSX: VSETEQZ.V cd, Vj.16B
        /// </summary>
        public static bool AllElementsIsZero(Vector128<byte> value) => AllElementsIsZero(value);

        /// <summary>
        /// bool vsetallnez_b_s8 (int8x16_t value)
        ///   LSX: VSETALLNEZ.B cd, Vj.16B
        /// </summary>
        public static bool AllElementsNotZero(Vector128<sbyte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_b_u8 (uint8x16_t value)
        ///   LSX: VSETALLNEZ.B cd, Vj.16B
        /// </summary>
        public static bool AllElementsNotZero(Vector128<byte> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_h_s16 (int16x8_t value)
        ///   LSX: VSETALLNEZ.H cd, Vj.8H
        /// </summary>
        public static bool AllElementsNotZero(Vector128<short> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_h_u16 (uint16x8_t value)
        ///   LSX: VSETALLNEZ.H cd, Vj.8H
        /// </summary>
        public static bool AllElementsNotZero(Vector128<ushort> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w_s32 (int32x4_t value)
        ///   LSX: VSETALLNEZ.W cd, Vj.4W
        /// </summary>
        public static bool AllElementsNotZero(Vector128<int> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w_u32 (uint32x4_t value)
        ///   LSX: VSETALLNEZ.W cd, Vj.4W
        /// </summary>
        public static bool AllElementsNotZero(Vector128<uint> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w_s64 (int64x2_t value)
        ///   LSX: VSETALLNEZ.D cd, Vj.2D
        /// </summary>
        public static bool AllElementsNotZero(Vector128<long> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetallnez_w_u64 (uint64x2_t value)
        ///   LSX: VSETALLNEZ.D cd, Vj.2D
        /// </summary>
        public static bool AllElementsNotZero(Vector128<ulong> value) => AllElementsNotZero(value);

        /// <summary>
        /// bool vsetanyeqz_b_s8 (int8x16_t value)
        ///   LSX: VSETANYEQZ.B cd, Vj.16B
        /// </summary>
        public static bool HasElementsIsZero(Vector128<sbyte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_b_u8 (uint8x16_t value)
        ///   LSX: VSETANYEQZ.B cd, Vj.16B
        /// </summary>
        public static bool HasElementsIsZero(Vector128<byte> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_h_s16 (int16x8_t value)
        ///   LSX: VSETANYEQZ.H cd, Vj.8H
        /// </summary>
        public static bool HasElementsIsZero(Vector128<short> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_h_u16 (uint16x8_t value)
        ///   LSX: VSETANYEQZ.H cd, Vj.8H
        /// </summary>
        public static bool HasElementsIsZero(Vector128<ushort> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w_s32 (int32x4_t value)
        ///   LSX: VSETANYEQZ.W cd, Vj.4W
        /// </summary>
        public static bool HasElementsIsZero(Vector128<int> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w_u32 (uint32x4_t value)
        ///   LSX: VSETANYEQZ.W cd, Vj.4W
        /// </summary>
        public static bool HasElementsIsZero(Vector128<uint> value) => HasElementsIsZero(value);

        /// <summary>
        /// bool vsetanyeqz_w_s64 (int64x2_t value)
        ///   LSX: VSETANYEQZ.D cd, Vj.2D
        /// </summary>
        public static bool HasElementsIsZero(Vector128<long> value) => HasElementsIsZero(value);
        /// <summary>
        /// bool vsetanyeqz_w_u64 (uint64x2_t value)
        ///   LSX: VSETANYEQZ.D cd, Vj.2D
        /// </summary>
        public static bool HasElementsIsZero(Vector128<ulong> value) => HasElementsIsZero(value);

        /// <summary>
        /// ulong vsrlni_b_h_16 (int16x8_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.B.H Vd, Vj, ui4
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<short> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// ulong vsrlni_b_h_u16 (uint16x8_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.B.H Vd, Vj, ui4
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<ushort> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// ulong vsrlni_h_w_s32 (int32x4_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<int> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// ulong vsrlni_h_w_u32 (uint32x4_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<uint> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// ulong vsrlni_w_d_s64 (int64x2_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<long> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// ulong vsrlni_w_d_u64 (uint64x2_t value, shift)
        ///   NOTE: this is implemented by multi instructions.
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        ///   LSX: Vd Scalar to uint64.
        /// </summary>
        public static ulong ShiftRightLogicalNarrowingLowerScalar(Vector128<ulong> value, byte shift) => ShiftRightLogicalNarrowingLowerScalar(value, shift);

        /// <summary>
        /// uint8x16 vsrlni_b_h_u16 (uint16x8_t left, uint16x8_t right, shift)
        ///   LSX: VSRLNI.B.H Vd, Vj, ui4
        /// </summary>
        public static Vector128<byte> ShiftRightLogicalNarrowingLower(Vector128<ushort> left, Vector128<ushort> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int16x8 vsrlni_h_w_s32 (int32x4_t left, int32x4_t right, shift)
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<short> ShiftRightLogicalNarrowingLower(Vector128<int> left, Vector128<int> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// uint16x8 vsrlni_h_w_u32 (uint32x4_t left, uint32x4_t right, shift)
        ///   LSX: VSRLNI.H.W Vd, Vj, ui5
        /// </summary>
        public static Vector128<ushort> ShiftRightLogicalNarrowingLower(Vector128<uint> left, Vector128<uint> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int32x4 vsrlni_w_d_s64 (int64x2_t left, int64x2_t right, shift)
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<int> ShiftRightLogicalNarrowingLower(Vector128<long> left, Vector128<long> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// uint32x4 vsrlni_w_d_u64 (uint64x2_t left, uint64x2_t right, shift)
        ///   LSX: VSRLNI.W.D Vd, Vj, ui6
        /// </summary>
        public static Vector128<uint> ShiftRightLogicalNarrowingLower(Vector128<ulong> left, Vector128<ulong> right, byte shift) => ShiftRightLogicalNarrowingLower(left, right, shift);

        /// <summary>
        /// int8x16_t vpcnt_b_s8 (int8x16_t a)
        ///   LSX: VPCNT_B Vd, Vj
        /// </summary>
        public static Vector128<sbyte> PopCount(Vector128<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x16_t vpcnt_b_u8 (uint8x16_t a)
        ///   LSX: VPCNT_B Vd, Vj
        /// </summary>
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        /// <summary>
        /// int16x8_t vpcnt_h_s16 (int16x8_t a)
        ///   LSX: VPCNT_H Vd, Vj
        /// </summary>
        public static Vector128<short> PopCount(Vector128<short> value) => PopCount(value);

        /// <summary>
        /// uint16x8_t vpcnt_h_u16 (uint16x8_t a)
        ///   LSX: VPCNT_H Vd, Vj
        /// </summary>
        public static Vector128<ushort> PopCount(Vector128<ushort> value) => PopCount(value);

        /// <summary>
        /// int32x4_t vpcnt_w_s32 (int32x4_t a)
        ///   LSX: VPCNT_W Vd, Vj
        /// </summary>
        public static Vector128<int> PopCount(Vector128<int> value) => PopCount(value);

        /// <summary>
        /// uint32x4_t vpcnt_w_u32 (uint32x4_t a)
        ///   LSX: VPCNT_W Vd, Vj
        /// </summary>
        public static Vector128<uint> PopCount(Vector128<uint> value) => PopCount(value);

        /// <summary>
        /// int64x2_t vpcnt_d_s64 (int64x2_t a)
        ///   LSX: VPCNT_D Vd, Vj
        /// </summary>
        public static Vector128<long> PopCount(Vector128<long> value) => PopCount(value);

        /// <summary>
        /// uint64x2_t vpcnt_d_u64 (uint64x2_t a)
        ///   LSX: VPCNT_D Vd, Vj
        /// </summary>
        public static Vector128<ulong> PopCount(Vector128<ulong> value) => PopCount(value);

        /// <summary>
        ///  uint8x16_t vshuffle_u8(uint8x16_t vec, uint8x16_t idx)
        ///   LSX: VSHUF_B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<byte> VectorShuffle(Vector128<byte> vector, Vector128<byte> byteIndexes) => VectorElementReplicate(vector, byteIndexes);

        /// <summary>
        ///  int8x16_t vshuffle_s8(int8x16_t vec, int8x16_t idx)
        ///   LSX: VSHUF_B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<sbyte> VectorShuffle(Vector128<sbyte> vector, Vector128<sbyte> byteIndexes) => VectorElementReplicate(vector, byteIndexes);

        /// <summary>
        ///  uint8x16_t vshuffle_u8(uint8x16_t vec0, uint8x16_t vec1, uint8x16_t idx)
        ///   LSX: VSHUF_B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<byte> VectorShuffle(Vector128<byte> vector0, Vector128<byte> vector1, Vector128<byte> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  int8x16_t vshuffle_s8(int8x16_t vec0, int8x16_t vec1, int8x16_t idx)
        ///   LSX: VSHUF_B Vd.16B, Vj.16B, Vk.16B, Va.16B
        /// </summary>
        public static Vector128<sbyte> VectorShuffle(Vector128<sbyte> vector0, Vector128<sbyte> vector1, Vector128<sbyte> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  int16x8_t vshuffle_s16(int16x8_t vec, int16x8_t idx)
        ///   LSX: VSHUF_H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorShuffle(Vector128<short> vector, Vector128<short> byteIndexes) => VectorElementReplicate(vector, byteIndexes);

        /// <summary>
        ///  uint16x8_t vshuffle_u16(uint16x8_t vec, uint16x8_t idx)
        ///   LSX: VSHUF_H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorShuffle(Vector128<ushort> vector, Vector128<ushort> byteIndexes) => VectorElementReplicate(vector, byteIndexes);

        /// <summary>
        ///  int16x8_t vshuffle_s16(int16x8_t vec0, int16x8_t vec1, int16x8_t idx)
        ///   LSX: VSHUF_H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<short> VectorShuffle(Vector128<short> vector0, Vector128<short> vector1, Vector128<short> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  uint16x8_t vshuffle_u16(uint16x8_t vecj, uint16x8_t veck, uint16x8_t idx)
        ///   LSX: VSHUF_H Vd.8H, Vj.8H, Vk.8H
        /// </summary>
        public static Vector128<ushort> VectorShuffle(Vector128<ushort> vector0, Vector128<ushort> vector1, Vector128<ushort> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  int32x4_t vshuffle_s32(int32x4_t vec0, int32x4_t vec1, int32x4_t idx)
        ///   LSX: VSHUF_H Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<int> VectorShuffle(Vector128<int> vector0, Vector128<int> vector1, Vector128<int> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  uint32x4_t vshuffle_u32(uint32x4_t vecj, uint32x4_t veck, uint32x4_t idx)
        ///   LSX: VSHUF_H Vd.4W, Vj.4W, Vk.4W
        /// </summary>
        public static Vector128<uint> VectorShuffle(Vector128<uint> vector0, Vector128<uint> vector1, Vector128<uint> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  int64x2_t vshuffle_s64(int64x2_t vec0, int64x2_t vec1, int64x2_t idx)
        ///   LSX: VSHUF_H Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<long> VectorShuffle(Vector128<long> vector0, Vector128<long> vector1, Vector128<long> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  uint64x2_t vshuffle_u64(uint64x2_t vecj, uint64x2_t veck, uint64x2_t idx)
        ///   LSX: VSHUF_H Vd.2D, Vj.2D, Vk.2D
        /// </summary>
        public static Vector128<ulong> VectorShuffle(Vector128<ulong> vector0, Vector128<ulong> vector1, Vector128<ulong> byteIndexes) => VectorShuffle(vector0, vector1, byteIndexes);

        /// <summary>
        ///  uint8x16_t vreplve_u8(uint8x16_t vector, uint8_t idx)
        ///   LSX: VREPLVE_B Vd.16B, Vj.16B, rk
        ///   LSX: VREPLVEI_B Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<byte> VectorElementReplicate(Vector128<byte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int8x16_t vreplve_s8(int8x16_t vector, uint8_t idx)
        ///   LSX: VREPLVE_B Vd.16B, Vj.16B, rk
        ///   LSX: VREPLVEI_B Vd.16B, Vj.16B, ui4
        /// </summary>
        public static Vector128<sbyte> VectorElementReplicate(Vector128<sbyte> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int16x8_t vreplve_s16(int16x8_t vector, uint8_t idx)
        ///   LSX: VREPLVE_H Vd.8H, Vj.8H, rk
        ///   LSX: VREPLVEI_H Vd.8H, Vj.8H, ui3
        /// </summary>
        public static Vector128<short> VectorElementReplicate(Vector128<short> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint16x8_t vreplve_u16(uint16x8_t vector, uint8_t idx)
        ///   LSX: VREPLVE_H Vd.8H, Vj.8H, rk
        ///   LSX: VREPLVEI_H Vd.8H, Vj.8H, ui3
        /// </summary>
        public static Vector128<ushort> VectorElementReplicate(Vector128<ushort> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int32x4_t vreplve_s32(int32x4_t vector, uint8_t idx)
        ///   LSX: VREPLVE_W Vd.4W, Vj.4W, rk
        ///   LSX: VREPLVEI_W Vd.4W, Vj.4W, ui2
        /// </summary>
        public static Vector128<int> VectorElementReplicate(Vector128<int> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint32x4_t vreplve_u32(uint32x4_t vector, uint8_t idx)
        ///   LSX: VREPLVE_W Vd.4W, Vj.4W, rk
        ///   LSX: VREPLVEI_W Vd.4W, Vj.4W, ui2
        /// </summary>
        public static Vector128<uint> VectorElementReplicate(Vector128<uint> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  int64x2_t vreplve_s64(int64x2_t vector, uint8_t idx)
        ///   LSX: VREPLVE_D Vd.2D, Vj.2D, rk
        ///   LSX: VREPLVEI_D Vd.2D, Vj.2D, ui1
        /// </summary>
        public static Vector128<long> VectorElementReplicate(Vector128<long> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);

        /// <summary>
        ///  uint64x2_t vreplve_u32(uint64x2_t vector, uint8_t idx)
        ///   LSX: VREPLVE_D Vd.2D, Vj.2D, rk
        ///   LSX: VREPLVEI_D Vd.2D, Vj.2D, ui1
        /// </summary>
        public static Vector128<ulong> VectorElementReplicate(Vector128<ulong> vector, byte elementIndexe) => VectorElementReplicate(vector, elementIndexe);



        // TODO: other liking vsrani .......
        // TODO:----------------------------------

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM AdvSIMD hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class AdvSimd : ArmBase
    {
        internal AdvSimd() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// float64x2_t vabsq_f64 (float64x2_t a)
            ///   A64: FABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> Abs(Vector128<double> value) => Abs(value);

            /// <summary>
            /// int64x2_t vabsq_s64 (int64x2_t a)
            ///   A64: ABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) => Abs(value);

            // /// <summary>
            // /// int64x1_t vabs_s64 (int64x1_t a)
            // ///   A64: ABS Dd, Dn
            // /// </summary>
            // public static Vector64<ulong> AbsScalar(Vector64<long> value) => AbsScalar(value);

            /// <summary>
            /// float64x2_t vaddq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FADD Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) => Add(left, right);

            /// <summary>
            /// uint8_t vaddv_u8(uint8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static byte AddAcross(Vector64<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddv_s16(int16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static short AddAcross(Vector64<short> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddv_s8(int8x8_t a)
            ///   A64: ADDV Bd, Vn.8B
            /// </summary>
            public static sbyte AddAcross(Vector64<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddv_u16(uint16x4_t a)
            ///   A64: ADDV Hd, Vn.4H
            /// </summary>
            public static ushort AddAcross(Vector64<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint8_t vaddvq_u8(uint8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static byte AddAcross(Vector128<byte> value) => AddAcross(value);

            /// <summary>
            /// int16_t vaddvq_s16(int16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static short AddAcross(Vector128<short> value) => AddAcross(value);

            /// <summary>
            /// int32_t vaddvq_s32(int32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static int AddAcross(Vector128<int> value) => AddAcross(value);

            /// <summary>
            /// int8_t vaddvq_s8(int8x16_t a)
            ///   A64: ADDV Bd, Vn.16B
            /// </summary>
            public static sbyte AddAcross(Vector128<sbyte> value) => AddAcross(value);

            /// <summary>
            /// uint16_t vaddvq_u16(uint16x8_t a)
            ///   A64: ADDV Hd, Vn.8H
            /// </summary>
            public static ushort AddAcross(Vector128<ushort> value) => AddAcross(value);

            /// <summary>
            /// uint32_t vaddvq_u32(uint32x4_t a)
            ///   A64: ADDV Sd, Vn.4S
            /// </summary>
            public static uint AddAcross(Vector128<uint> value) => AddAcross(value);

            /// <summary>
            /// float64x2_t vsubq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FSUB Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) => Add(left, right);

            /// <summary>
            /// uint8x8_t vrbit_u8 (uint8x8_t a)
            ///   A64: RBIT Vd.8B, Vn.8B
            /// </summary>
            public static Vector64<byte> ReverseElementBits(Vector64<byte> value) => ReverseElementBits(value);

            /// <summary>
            /// int8x8_t vrbit_s8 (int8x8_t a)
            ///   A64: RBIT Vd.8B, Vn.8B
            /// </summary>
            public static Vector64<sbyte> ReverseElementBits(Vector64<sbyte> value) => ReverseElementBits(value);

            /// <summary>
            /// uint8x16_t vrbitq_u8 (uint8x16_t a)
            ///   A64: RBIT Vd.16B, Vn.16B
            /// </summary>
            public static Vector128<byte> ReverseElementBits(Vector128<byte> value) => ReverseElementBits(value);

            /// <summary>
            /// int8x16_t vrbitq_s8 (int8x16_t a)
            ///   A64: RBIT Vd.16B, Vn.16B
            /// </summary>
            public static Vector128<sbyte> ReverseElementBits(Vector128<sbyte> value) => ReverseElementBits(value);

            /// <summary>
            /// uint8x8_t vuzp1_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: UZP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> UnzipEven(Vector64<byte> left, Vector64<byte> right) => UnzipEven(left, right);

            /// <summary>
            /// int16x4_t vuzp1_s16(int16x4_t a, int16x4_t b)
            ///   A64: UZP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> UnzipEven(Vector64<short> left, Vector64<short> right) => UnzipEven(left, right);

            /// <summary>
            /// int32x2_t vuzp1_s32(int32x2_t a, int32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> UnzipEven(Vector64<int> left, Vector64<int> right) => UnzipEven(left, right);

            /// <summary>
            /// int8x8_t vuzp1_s8(int8x8_t a, int8x8_t b)
            ///   A64: UZP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> UnzipEven(Vector64<sbyte> left, Vector64<sbyte> right) => UnzipEven(left, right);

            /// <summary>
            /// float32x2_t vuzp1_f32(float32x2_t a, float32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> UnzipEven(Vector64<float> left, Vector64<float> right) => UnzipEven(left, right);

            /// <summary>
            /// uint16x4_t vuzp1_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: UZP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> UnzipEven(Vector64<ushort> left, Vector64<ushort> right) => UnzipEven(left, right);

            /// <summary>
            /// uint32x2_t vuzp1_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: UZP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> UnzipEven(Vector64<uint> left, Vector64<uint> right) => UnzipEven(left, right);

            /// <summary>
            /// uint8x16_t vuzp1q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: UZP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> UnzipEven(Vector128<byte> left, Vector128<byte> right) => UnzipEven(left, right);

            /// <summary>
            /// float64x2_t vuzp1q_f64(float64x2_t a, float64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> UnzipEven(Vector128<double> left, Vector128<double> right) => UnzipEven(left, right);

            /// <summary>
            /// int16x8_t vuzp1q_s16(int16x8_t a, int16x8_t b)
            ///   A64: UZP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> UnzipEven(Vector128<short> left, Vector128<short> right) => UnzipEven(left, right);

            /// <summary>
            /// int32x4_t vuzp1q_s32(int32x4_t a, int32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> UnzipEven(Vector128<int> left, Vector128<int> right) => UnzipEven(left, right);

            /// <summary>
            /// int64x2_t vuzp1q_s64(int64x2_t a, int64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> UnzipEven(Vector128<long> left, Vector128<long> right) => UnzipEven(left, right);

            /// <summary>
            /// int8x16_t vuzp1q_u8(int8x16_t a, int8x16_t b)
            ///   A64: UZP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> UnzipEven(Vector128<sbyte> left, Vector128<sbyte> right) => UnzipEven(left, right);

            /// <summary>
            /// float32x4_t vuzp1q_f32(float32x4_t a, float32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> UnzipEven(Vector128<float> left, Vector128<float> right) => UnzipEven(left, right);

            /// <summary>
            /// uint16x8_t vuzp1q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: UZP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> UnzipEven(Vector128<ushort> left, Vector128<ushort> right) => UnzipEven(left, right);

            /// <summary>
            /// uint32x4_t vuzp1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: UZP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> UnzipEven(Vector128<uint> left, Vector128<uint> right) => UnzipEven(left, right);

            /// <summary>
            /// uint64x2_t vuzp1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: UZP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> UnzipEven(Vector128<ulong> left, Vector128<ulong> right) => UnzipEven(left, right);

            /// <summary>
            /// uint8x8_t vuzp2_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: UZP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> UnzipOdd(Vector64<byte> left, Vector64<byte> right) => UnzipOdd(left, right);

            /// <summary>
            /// int16x4_t vuzp2_s16(int16x4_t a, int16x4_t b)
            ///   A64: UZP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> UnzipOdd(Vector64<short> left, Vector64<short> right) => UnzipOdd(left, right);

            /// <summary>
            /// int32x2_t vuzp2_s32(int32x2_t a, int32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> UnzipOdd(Vector64<int> left, Vector64<int> right) => UnzipOdd(left, right);

            /// <summary>
            /// int8x8_t vuzp2_s8(int8x8_t a, int8x8_t b)
            ///   A64: UZP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> UnzipOdd(Vector64<sbyte> left, Vector64<sbyte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float32x2_t vuzp2_f32(float32x2_t a, float32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> UnzipOdd(Vector64<float> left, Vector64<float> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint16x4_t vuzp2_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: UZP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> UnzipOdd(Vector64<ushort> left, Vector64<ushort> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint32x2_t vuzp2_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: UZP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> UnzipOdd(Vector64<uint> left, Vector64<uint> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint8x16_t vuzp2q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: UZP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> UnzipOdd(Vector128<byte> left, Vector128<byte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float64x2_t vuzp2q_f64(float64x2_t a, float64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> UnzipOdd(Vector128<double> left, Vector128<double> right) => UnzipOdd(left, right);

            /// <summary>
            /// int16x8_t vuzp2q_s16(int16x8_t a, int16x8_t b)
            ///   A64: UZP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> UnzipOdd(Vector128<short> left, Vector128<short> right) => UnzipOdd(left, right);

            /// <summary>
            /// int32x4_t vuzp2q_s32(int32x4_t a, int32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> UnzipOdd(Vector128<int> left, Vector128<int> right) => UnzipOdd(left, right);

            /// <summary>
            /// int64x2_t vuzp2q_s64(int64x2_t a, int64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> UnzipOdd(Vector128<long> left, Vector128<long> right) => UnzipOdd(left, right);

            /// <summary>
            /// int8x16_t vuzp2q_u8(int8x16_t a, int8x16_t b)
            ///   A64: UZP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> UnzipOdd(Vector128<sbyte> left, Vector128<sbyte> right) => UnzipOdd(left, right);

            /// <summary>
            /// float32x4_t vuzp2q_f32(float32x4_t a, float32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> UnzipOdd(Vector128<float> left, Vector128<float> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint16x8_t vuzp2q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: UZP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> UnzipOdd(Vector128<ushort> left, Vector128<ushort> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint32x4_t vuzp2q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: UZP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> UnzipOdd(Vector128<uint> left, Vector128<uint> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint64x2_t vuzp2q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: UZP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> UnzipOdd(Vector128<ulong> left, Vector128<ulong> right) => UnzipOdd(left, right);

            /// <summary>
            /// uint8x8_t vzip2_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: ZIP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> ZipHigh(Vector64<byte> left, Vector64<byte> right) => ZipHigh(left, right);

            /// <summary>
            /// int16x4_t vzip2_s16(int16x4_t a, int16x4_t b)
            ///   A64: ZIP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> ZipHigh(Vector64<short> left, Vector64<short> right) => ZipHigh(left, right);

            /// <summary>
            /// int32x2_t vzip2_s32(int32x2_t a, int32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> ZipHigh(Vector64<int> left, Vector64<int> right) => ZipHigh(left, right);

            /// <summary>
            /// int8x8_t vzip2_s8(int8x8_t a, int8x8_t b)
            ///   A64: ZIP2 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> ZipHigh(Vector64<sbyte> left, Vector64<sbyte> right) => ZipHigh(left, right);

            /// <summary>
            /// float32x2_t vzip2_f32(float32x2_t a, float32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> ZipHigh(Vector64<float> left, Vector64<float> right) => ZipHigh(left, right);

            /// <summary>
            /// uint16x4_t vzip2_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: ZIP2 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> ZipHigh(Vector64<ushort> left, Vector64<ushort> right) => ZipHigh(left, right);

            /// <summary>
            /// uint32x2_t vzip2_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: ZIP2 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> ZipHigh(Vector64<uint> left, Vector64<uint> right) => ZipHigh(left, right);

            /// <summary>
            /// uint8x16_t vzip2q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: ZIP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> ZipHigh(Vector128<byte> left, Vector128<byte> right) => ZipHigh(left, right);

            /// <summary>
            /// float64x2_t vzip2q_f64(float64x2_t a, float64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ZipHigh(Vector128<double> left, Vector128<double> right) => ZipHigh(left, right);

            /// <summary>
            /// int16x8_t vzip2q_s16(int16x8_t a, int16x8_t b)
            ///   A64: ZIP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> ZipHigh(Vector128<short> left, Vector128<short> right) => ZipHigh(left, right);

            /// <summary>
            /// int32x4_t vzip2q_s32(int32x4_t a, int32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> ZipHigh(Vector128<int> left, Vector128<int> right) => ZipHigh(left, right);

            /// <summary>
            /// int64x2_t vzip2q_s64(int64x2_t a, int64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> ZipHigh(Vector128<long> left, Vector128<long> right) => ZipHigh(left, right);

            /// <summary>
            /// int8x16_t vzip2q_u8(int8x16_t a, int8x16_t b)
            ///   A64: ZIP2 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> ZipHigh(Vector128<sbyte> left, Vector128<sbyte> right) => ZipHigh(left, right);

            /// <summary>
            /// float32x4_t vzip2q_f32(float32x4_t a, float32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> ZipHigh(Vector128<float> left, Vector128<float> right) => ZipHigh(left, right);

            /// <summary>
            /// uint16x8_t vzip2q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: ZIP2 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> ZipHigh(Vector128<ushort> left, Vector128<ushort> right) => ZipHigh(left, right);

            /// <summary>
            /// uint32x4_t vzip2q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: ZIP2 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> ZipHigh(Vector128<uint> left, Vector128<uint> right) => ZipHigh(left, right);

            /// <summary>
            /// uint64x2_t vzip2q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: ZIP2 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> ZipHigh(Vector128<ulong> left, Vector128<ulong> right) => ZipHigh(left, right);

            /// <summary>
            /// uint8x8_t vzip1_u8(uint8x8_t a, uint8x8_t b)
            ///   A64: ZIP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<byte> ZipLow(Vector64<byte> left, Vector64<byte> right) => ZipLow(left, right);

            /// <summary>
            /// int16x4_t vzip1_s16(int16x4_t a, int16x4_t b)
            ///   A64: ZIP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<short> ZipLow(Vector64<short> left, Vector64<short> right) => ZipLow(left, right);

            /// <summary>
            /// int32x2_t vzip1_s32(int32x2_t a, int32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<int> ZipLow(Vector64<int> left, Vector64<int> right) => ZipLow(left, right);

            /// <summary>
            /// int8x8_t vzip1_s8(int8x8_t a, int8x8_t b)
            ///   A64: ZIP1 Vd.8B, Vn.8B, Vm.8B
            /// </summary>
            public static Vector64<sbyte> ZipLow(Vector64<sbyte> left, Vector64<sbyte> right) => ZipLow(left, right);

            /// <summary>
            /// float32x2_t vzip1_f32(float32x2_t a, float32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<float> ZipLow(Vector64<float> left, Vector64<float> right) => ZipLow(left, right);

            /// <summary>
            /// uint16x4_t vzip1_u16(uint16x4_t a, uint16x4_t b)
            ///   A64: ZIP1 Vd.4H, Vn.4H, Vm.4H
            /// </summary>
            public static Vector64<ushort> ZipLow(Vector64<ushort> left, Vector64<ushort> right) => ZipLow(left, right);

            /// <summary>
            /// uint32x2_t vzip1_u32(uint32x2_t a, uint32x2_t b)
            ///   A64: ZIP1 Vd.2S, Vn.2S, Vm.2S
            /// </summary>
            public static Vector64<uint> ZipLow(Vector64<uint> left, Vector64<uint> right) => ZipLow(left, right);

            /// <summary>
            /// uint8x16_t vzip1q_u8(uint8x16_t a, uint8x16_t b)
            ///   A64: ZIP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<byte> ZipLow(Vector128<byte> left, Vector128<byte> right) => ZipLow(left, right);

            /// <summary>
            /// float64x2_t vzip1q_f64(float64x2_t a, float64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> ZipLow(Vector128<double> left, Vector128<double> right) => ZipLow(left, right);

            /// <summary>
            /// int16x8_t vzip1q_s16(int16x8_t a, int16x8_t b)
            ///   A64: ZIP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<short> ZipLow(Vector128<short> left, Vector128<short> right) => ZipLow(left, right);

            /// <summary>
            /// int32x4_t vzip1q_s32(int32x4_t a, int32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<int> ZipLow(Vector128<int> left, Vector128<int> right) => ZipLow(left, right);

            /// <summary>
            /// int64x2_t vzip1q_s64(int64x2_t a, int64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<long> ZipLow(Vector128<long> left, Vector128<long> right) => ZipLow(left, right);

            /// <summary>
            /// int8x16_t vzip1q_u8(int8x16_t a, int8x16_t b)
            ///   A64: ZIP1 Vd.16B, Vn.16B, Vm.16B
            /// </summary>
            public static Vector128<sbyte> ZipLow(Vector128<sbyte> left, Vector128<sbyte> right) => ZipLow(left, right);

            /// <summary>
            /// float32x4_t vzip1q_f32(float32x4_t a, float32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<float> ZipLow(Vector128<float> left, Vector128<float> right) => ZipLow(left, right);

            /// <summary>
            /// uint16x8_t vzip1q_u16(uint16x8_t a, uint16x8_t b)
            ///   A64: ZIP1 Vd.8H, Vn.8H, Vm.8H
            /// </summary>
            public static Vector128<ushort> ZipLow(Vector128<ushort> left, Vector128<ushort> right) => ZipLow(left, right);

            /// <summary>
            /// uint32x4_t vzip1q_u32(uint32x4_t a, uint32x4_t b)
            ///   A64: ZIP1 Vd.4S, Vn.4S, Vm.4S
            /// </summary>
            public static Vector128<uint> ZipLow(Vector128<uint> left, Vector128<uint> right) => ZipLow(left, right);

            /// <summary>
            /// uint64x2_t vzip1q_u64(uint64x2_t a, uint64x2_t b)
            ///   A64: ZIP1 Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<ulong> ZipLow(Vector128<ulong> left, Vector128<ulong> right) => ZipLow(left, right);
        }

        /// <summary>
        /// int8x8_t vabs_s8 (int8x8_t a)
        ///   A32: VABS.S8 Dd, Dm
        ///   A64: ABS Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> Abs(Vector64<sbyte> value) => Abs(value);

        /// <summary>
        /// int16x4_t vabs_s16 (int16x4_t a)
        ///   A32: VABS.S16 Dd, Dm
        ///   A64: ABS Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<ushort> Abs(Vector64<short> value) => Abs(value);

        /// <summary>
        /// int32x2_t vabs_s32 (int32x2_t a)
        ///   A32: VABS.S32 Dd, Dm
        ///   A64: ABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> Abs(Vector64<int> value) => Abs(value);

        /// <summary>
        /// float32x2_t vabs_f32 (float32x2_t a)
        ///   A32: VABS.F32 Dd, Dm
        ///   A64: FABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> Abs(Vector64<float> value) => Abs(value);

        /// <summary>
        /// int8x16_t vabsq_s8 (int8x16_t a)
        ///   A32: VABS.S8 Qd, Qm
        ///   A64: ABS Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) => Abs(value);

        /// <summary>
        /// int16x8_t vabsq_s16 (int16x8_t a)
        ///   A32: VABS.S16 Qd, Qm
        ///   A64: ABS Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) => Abs(value);

        /// <summary>
        /// int32x4_t vabsq_s32 (int32x4_t a)
        ///   A32: VABS.S32 Qd, Qm
        ///   A64: ABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) => Abs(value);

        /// <summary>
        /// float32x4_t vabsq_f32 (float32x4_t a)
        ///   A32: VABS.F32 Qd, Qm
        ///   A64: FABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> Abs(Vector128<float> value) => Abs(value);

        // /// <summary>
        // /// float64x1_t vabs_f64 (float64x1_t a)
        // ///   A32: VABS.F64 Dd, Dm
        // ///   A64: FABS Dd, Dn
        // /// </summary>
        // public static Vector64<double> AbsScalar(Vector64<double> value) => Abs(value);

        /// <summary>
        ///   A32: VABS.F32 Sd, Sm
        ///   A64: FABS Sd, Sn
        /// </summary>
        public static Vector64<float> AbsScalar(Vector64<float> value) => AbsScalar(value);

        /// <summary>
        /// uint8x8_t vadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Add(Vector64<byte> left, Vector64<byte> right) => Add(left, right);

        /// <summary>
        /// int16x4_t vadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Add(Vector64<short> left, Vector64<short> right) => Add(left, right);

        /// <summary>
        /// int32x2_t vadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Add(Vector64<int> left, Vector64<int> right) => Add(left, right);

        /// <summary>
        /// int8x8_t vadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Add(Vector64<sbyte> left, Vector64<sbyte> right) => Add(left, right);

        /// <summary>
        /// float32x2_t vadd_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VADD.F32 Dd, Dn, Dm
        ///   A64: FADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Add(Vector64<float> left, Vector64<float> right) => Add(left, right);

        /// <summary>
        /// uint16x4_t vadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Add(Vector64<ushort> left, Vector64<ushort> right) => Add(left, right);

        /// <summary>
        /// uint32x2_t vadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Add(Vector64<uint> left, Vector64<uint> right) => Add(left, right);

        /// <summary>
        /// uint8x16_t vaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) => Add(left, right);

        /// <summary>
        /// int16x8_t vaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) => Add(left, right);

        /// <summary>
        /// int32x4_t vaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) => Add(left, right);

        /// <summary>
        /// int64x2_t vaddq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) => Add(left, right);

        /// <summary>
        /// int8x16_t vaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) => Add(left, right);

        /// <summary>
        /// float32x4_t vaddq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VADD.F32 Qd, Qn, Qm
        ///   A64: FADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) => Add(left, right);

        /// <summary>
        /// uint16x8_t vaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) => Add(left, right);

        /// <summary>
        /// uint32x4_t vaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) => Add(left, right);

        /// <summary>
        /// uint64x2_t vaddq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) => Add(left, right);

        // /// <summary>
        // /// float64x1_t vadd_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VADD.F64 Dd, Dn, Dm
        // ///   A64: FADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<double> AddScalar(Vector64<double> left, Vector64<double> right) => Add(left, right);

        // /// <summary>
        // /// int64x1_t vadd_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VADD.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<long> AddScalar(Vector64<long> left, Vector64<long> right) => AddScalar(left, right);

        // /// <summary>
        // /// uint64x1_t vadd_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VADD.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<ulong> AddScalar(Vector64<ulong> left, Vector64<ulong> right) => AddScalar(left, right);

        /// <summary>
        ///   A32: VADD.F32 Sd, Sn, Sm
        ///   A64:
        /// </summary>
        public static Vector64<float> AddScalar(Vector64<float> left, Vector64<float> right) => AddScalar(left, right);

        /// <summary>
        /// uint8x8_t vand_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> And(Vector64<byte> left, Vector64<byte> right) => And(left, right);

        // /// <summary>
        // /// float64x1_t vand_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> And(Vector64<double> left, Vector64<double> right) => And(left, right);

        /// <summary>
        /// int16x4_t vand_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> And(Vector64<short> left, Vector64<short> right) => And(left, right);

        /// <summary>
        /// int32x2_t vand_s32(int32x2_t a, int32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> And(Vector64<int> left, Vector64<int> right) => And(left, right);

        // /// <summary>
        // /// int64x1_t vand_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> And(Vector64<long> left, Vector64<long> right) => And(left, right);

        /// <summary>
        /// int8x8_t vand_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> And(Vector64<sbyte> left, Vector64<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x2_t vand_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> And(Vector64<float> left, Vector64<float> right) => And(left, right);

        /// <summary>
        /// uint16x4_t vand_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> And(Vector64<ushort> left, Vector64<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x2_t vand_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> And(Vector64<uint> left, Vector64<uint> right) => And(left, right);

        // /// <summary>
        // /// uint64x1_t vand_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> And(Vector64<ulong> left, Vector64<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x16_t vand_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) => And(left, right);

        /// <summary>
        /// float64x2_t vand_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) => And(left, right);

        /// <summary>
        /// int16x8_t vand_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) => And(left, right);

        /// <summary>
        /// int32x4_t vand_s32(int32x4_t a, int32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) => And(left, right);

        /// <summary>
        /// int64x2_t vand_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) => And(left, right);

        /// <summary>
        /// int8x16_t vand_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) => And(left, right);

        /// <summary>
        /// float32x4_t vand_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) => And(left, right);

        /// <summary>
        /// uint16x8_t vand_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) => And(left, right);

        /// <summary>
        /// uint32x4_t vand_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) => And(left, right);

        /// <summary>
        /// uint64x2_t vand_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) => And(left, right);

        /// <summary>
        /// uint8x8_t vbic_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> AndNot(Vector64<byte> left, Vector64<byte> right) => AndNot(left, right);

        // /// <summary>
        // /// float64x1_t vbic_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> AndNot(Vector64<double> left, Vector64<double> right) => AndNot(left, right);

        /// <summary>
        /// int16x4_t vbic_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> AndNot(Vector64<short> left, Vector64<short> right) => AndNot(left, right);

        /// <summary>
        /// int32x2_t vbic_s32(int32x2_t a, int32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> AndNot(Vector64<int> left, Vector64<int> right) => AndNot(left, right);

        // /// <summary>
        // /// int64x1_t vbic_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> AndNot(Vector64<long> left, Vector64<long> right) => AndNot(left, right);

        /// <summary>
        /// int8x8_t vbic_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> AndNot(Vector64<sbyte> left, Vector64<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// float32x2_t vbic_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> AndNot(Vector64<float> left, Vector64<float> right) => AndNot(left, right);

        /// <summary>
        /// uint16x4_t vbic_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> AndNot(Vector64<ushort> left, Vector64<ushort> right) => AndNot(left, right);

        /// <summary>
        /// uint32x2_t vbic_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> AndNot(Vector64<uint> left, Vector64<uint> right) => AndNot(left, right);

        // /// <summary>
        // /// uint64x1_t vbic_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> AndNot(Vector64<ulong> left, Vector64<ulong> right) => AndNot(left, right);

        /// <summary>
        /// uint8x16_t vbic_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) => AndNot(left, right);

        /// <summary>
        /// float64x2_t vbic_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) => AndNot(left, right);

        /// <summary>
        /// int16x8_t vbic_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) => AndNot(left, right);

        /// <summary>
        /// int32x4_t vbic_s32(int32x4_t a, int32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) => AndNot(left, right);

        /// <summary>
        /// int64x2_t vbic_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) => AndNot(left, right);

        /// <summary>
        /// int8x16_t vbic_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) => AndNot(left, right);

        /// <summary>
        /// float32x4_t vbic_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) => AndNot(left, right);

        /// <summary>
        /// uint16x8_t vbic_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) => AndNot(left, right);

        /// <summary>
        /// uint32x4_t vbic_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) => AndNot(left, right);

        /// <summary>
        /// uint64x2_t vbic_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) => AndNot(left, right);

        /// <summary>
        /// uint8x8_t vbsl_u8 (uint8x8_t a, uint8x8_t b, uint8x8_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> BitwiseSelect(Vector64<byte> select, Vector64<byte> left, Vector64<byte> right) => BitwiseSelect(select, left, right);

        // /// <summary>
        // /// float64x1_t vbsl_f64 (float64x1_t a, float64x1_t b, float64x1_t c)
        // ///   A32: VBSL Dd, Dn, Dm
        // ///   A64: BSL Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<double> BitwiseSelect(Vector64<double> select, Vector64<double> left, Vector64<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x4_t vbsl_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> BitwiseSelect(Vector64<short> select, Vector64<short> left, Vector64<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x2_t vbsl_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> BitwiseSelect(Vector64<int> select, Vector64<int> left, Vector64<int> right) => BitwiseSelect(select, left, right);

        // /// <summary>
        // /// int64x1_t vbsl_s64 (int64x1_t a, int64x1_t b, int64x1_t c)
        // ///   A32: VBSL Dd, Dn, Dm
        // ///   A64: BSL Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> BitwiseSelect(Vector64<long> select, Vector64<long> left, Vector64<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x8_t vbsl_s8 (int8x8_t a, int8x8_t b, int8x8_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> BitwiseSelect(Vector64<sbyte> select, Vector64<sbyte> left, Vector64<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x2_t vbsl_f32 (float32x2_t a, float32x2_t b, float32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<float> BitwiseSelect(Vector64<float> select, Vector64<float> left, Vector64<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x4_t vbsl_u16 (uint16x4_t a, uint16x4_t b, uint16x4_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> BitwiseSelect(Vector64<ushort> select, Vector64<ushort> left, Vector64<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x2_t vbsl_u32 (uint32x2_t a, uint32x2_t b, uint32x2_t c)
        ///   A32: VBSL Dd, Dn, Dm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> BitwiseSelect(Vector64<uint> select, Vector64<uint> left, Vector64<uint> right) => BitwiseSelect(select, left, right);

        // /// <summary>
        // /// uint64x1_t vbsl_u64 (uint64x1_t a, uint64x1_t b, uint64x1_t c)
        // ///   A32: VBSL Dd, Dn, Dm
        // ///   A64: BSL Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> BitwiseSelect(Vector64<ulong> select, Vector64<ulong> left, Vector64<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint8x16_t vbslq_u8 (uint8x16_t a, uint8x16_t b, uint8x16_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> BitwiseSelect(Vector128<byte> select, Vector128<byte> left, Vector128<byte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float64x2_t vbslq_f64 (float64x2_t a, float64x2_t b, float64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<double> BitwiseSelect(Vector128<double> select, Vector128<double> left, Vector128<double> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int16x8_t vbslq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> BitwiseSelect(Vector128<short> select, Vector128<short> left, Vector128<short> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int32x4_t vbslq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> BitwiseSelect(Vector128<int> select, Vector128<int> left, Vector128<int> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int64x2_t vbslq_s64 (int64x2_t a, int64x2_t b, int64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> BitwiseSelect(Vector128<long> select, Vector128<long> left, Vector128<long> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x16_t vbslq_s8 (int8x16_t a, int8x16_t b, int8x16_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> BitwiseSelect(Vector128<sbyte> select, Vector128<sbyte> left, Vector128<sbyte> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// float32x4_t vbslq_f32 (float32x4_t a, float32x4_t b, float32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<float> BitwiseSelect(Vector128<float> select, Vector128<float> left, Vector128<float> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint16x8_t vbslq_u16 (uint16x8_t a, uint16x8_t b, uint16x8_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> BitwiseSelect(Vector128<ushort> select, Vector128<ushort> left, Vector128<ushort> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint32x4_t vbslq_u32 (uint32x4_t a, uint32x4_t b, uint32x4_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> BitwiseSelect(Vector128<uint> select, Vector128<uint> left, Vector128<uint> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// uint64x2_t vbslq_u64 (uint64x2_t a, uint64x2_t b, uint64x2_t c)
        ///   A32: VBSL Qd, Qn, Qm
        ///   A64: BSL Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> BitwiseSelect(Vector128<ulong> select, Vector128<ulong> left, Vector128<ulong> right) => BitwiseSelect(select, left, right);

        /// <summary>
        /// int8x8_t vcls_s8 (int8x8_t a)
        ///   A32: VCLS Dd, Dm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector64<sbyte> LeadingSignCount(Vector64<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x4_t vcls_s16 (int16x4_t a)
        ///   A32: VCLS Dd, Dm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector64<short> LeadingSignCount(Vector64<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x2_t vcls_s32 (int32x2_t a)
        ///   A32: VCLS Dd, Dm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector64<int> LeadingSignCount(Vector64<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x16_t vclsq_s8 (int8x16_t a)
        ///   A32: VCLS Qd, Qm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector128<sbyte> LeadingSignCount(Vector128<sbyte> value) => LeadingSignCount(value);

        /// <summary>
        /// int16x8_t vclsq_s16 (int16x8_t a)
        ///   A32: VCLS Qd, Qm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector128<short> LeadingSignCount(Vector128<short> value) => LeadingSignCount(value);

        /// <summary>
        /// int32x4_t vclsq_s32 (int32x4_t a)
        ///   A32: VCLS Qd, Qm
        ///   A64: CLS Vd, Vn
        /// </summary>
        public static Vector128<int> LeadingSignCount(Vector128<int> value) => LeadingSignCount(value);

        /// <summary>
        /// int8x8_t vclz_s8 (int8x8_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<sbyte> LeadingZeroCount(Vector64<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x8_t vclz_u8 (uint8x8_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<byte> LeadingZeroCount(Vector64<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x4_t vclz_s16 (int16x4_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<short> LeadingZeroCount(Vector64<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x4_t vclz_u16 (uint16x4_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<ushort> LeadingZeroCount(Vector64<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x2_t vclz_s32 (int32x2_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<int> LeadingZeroCount(Vector64<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x2_t vclz_u32 (uint32x2_t a)
        ///   A32: VCLZ Dd, Dm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector64<uint> LeadingZeroCount(Vector64<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// int8x16_t vclzq_s8 (int8x16_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<sbyte> LeadingZeroCount(Vector128<sbyte> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x16_t vclzq_u8 (uint8x16_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<byte> LeadingZeroCount(Vector128<byte> value) => LeadingZeroCount(value);

        /// <summary>
        /// int16x8_t vclzq_s16 (int16x8_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<short> LeadingZeroCount(Vector128<short> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint16x8_t vclzq_u16 (uint16x8_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<ushort> LeadingZeroCount(Vector128<ushort> value) => LeadingZeroCount(value);

        /// <summary>
        /// int32x4_t vclzq_s32 (int32x4_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<int> LeadingZeroCount(Vector128<int> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint32x4_t vclzq_u32 (uint32x4_t a)
        ///   A32: VCLZ Qd, Qm
        ///   A64: CLZ Vd, Vn
        /// </summary>
        public static Vector128<uint> LeadingZeroCount(Vector128<uint> value) => LeadingZeroCount(value);

        /// <summary>
        /// uint8x8_t vld1_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<byte> LoadVector64(byte* address) => LoadVector64(address);

        /// <summary>
        /// int16x4_t vld1_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<short> LoadVector64(short* address) => LoadVector64(address);

        /// <summary>
        /// int32x2_t vld1_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<int> LoadVector64(int* address) => LoadVector64(address);

        /// <summary>
        /// int8x8_t vld1_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<sbyte> LoadVector64(sbyte* address) => LoadVector64(address);

        /// <summary>
        /// float32x2_t vld1_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<float> LoadVector64(float* address) => LoadVector64(address);

        /// <summary>
        /// uint16x4_t vld1_u16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<ushort> LoadVector64(ushort* address) => LoadVector64(address);

        /// <summary>
        /// uint32x2_t vld1_u32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<uint> LoadVector64(uint* address) => LoadVector64(address);

        /// <summary>
        /// uint8x16_t vld1q_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) => LoadVector128(address);

        /// <summary>
        /// float64x2_t vld1q_f64 (float64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) => LoadVector128(address);

        /// <summary>
        /// int16x8_t vld1q_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) => LoadVector128(address);

        /// <summary>
        /// int32x4_t vld1q_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) => LoadVector128(address);

        /// <summary>
        /// int64x2_t vld1q_s64 (int64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) => LoadVector128(address);

        /// <summary>
        /// int8x16_t vld1q_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) => LoadVector128(address);

        /// <summary>
        /// float32x4_t vld1q_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) => LoadVector128(address);

        /// <summary>
        /// uint16x8_t vld1q_s16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) => LoadVector128(address);

        /// <summary>
        /// uint32x4_t vld1q_s32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) => LoadVector128(address);

        /// <summary>
        /// uint64x2_t vld1q_u64 (uint64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) => LoadVector128(address);

        /// <summary>
        /// uint8x8_t vmvn_u8 (uint8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Not(Vector64<byte> value) => Not(value);

        // /// <summary>
        // /// float64x1_t vmvn_f64 (float64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Not(Vector64<double> value) => Not(value);

        /// <summary>
        /// int16x4_t vmvn_s16 (int16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Not(Vector64<short> value) => Not(value);

        /// <summary>
        /// int32x2_t vmvn_s32(int32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Not(Vector64<int> value) => Not(value);

        // /// <summary>
        // /// int64x1_t vmvn_s64 (int64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Not(Vector64<long> value) => Not(value);

        /// <summary>
        /// int8x8_t vmvn_s8 (int8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Not(Vector64<sbyte> value) => Not(value);

        /// <summary>
        /// float32x2_t vmvn_f32 (float32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Not(Vector64<float> value) => Not(value);

        /// <summary>
        /// uint16x4_t vmvn_u16 (uint16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Not(Vector64<ushort> value) => Not(value);

        /// <summary>
        /// uint32x2_t vmvn_u32 (uint32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Not(Vector64<uint> value) => Not(value);

        // /// <summary>
        // /// uint64x1_t vmvn_u64 (uint64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Not(Vector64<ulong> value) => Not(value);

        /// <summary>
        /// uint8x16_t vmvn_u8 (uint8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Not(Vector128<byte> value) => Not(value);

        /// <summary>
        /// float64x2_t vmvn_f64 (float64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Not(Vector128<double> value) => Not(value);

        /// <summary>
        /// int16x8_t vmvn_s16 (int16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Not(Vector128<short> value) => Not(value);

        /// <summary>
        /// int32x4_t vmvn_s32(int32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Not(Vector128<int> value) => Not(value);

        /// <summary>
        /// int64x2_t vmvn_s64 (int64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Not(Vector128<long> value) => Not(value);

        /// <summary>
        /// int8x16_t vmvn_s8 (int8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Not(Vector128<sbyte> value) => Not(value);

        /// <summary>
        /// float32x4_t vmvn_f32 (float32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Not(Vector128<float> value) => Not(value);

        /// <summary>
        /// uint16x8_t vmvn_u16 (uint16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Not(Vector128<ushort> value) => Not(value);

        /// <summary>
        /// uint32x4_t vmvn_u32 (uint32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Not(Vector128<uint> value) => Not(value);

        /// <summary>
        /// uint64x2_t vmvn_u64 (uint64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Not(Vector128<ulong> value) => Not(value);

        /// <summary>
        /// uint8x8_t vorr_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Or(Vector64<byte> left, Vector64<byte> right) => Or(left, right);

        // /// <summary>
        // /// float64x1_t vorr_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Or(Vector64<double> left, Vector64<double> right) => Or(left, right);

        /// <summary>
        /// int16x4_t vorr_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Or(Vector64<short> left, Vector64<short> right) => Or(left, right);

        /// <summary>
        /// int32x2_t vorr_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Or(Vector64<int> left, Vector64<int> right) => Or(left, right);

        // /// <summary>
        // /// int64x1_t vorr_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Or(Vector64<long> left, Vector64<long> right) => Or(left, right);

        /// <summary>
        /// int8x8_t vorr_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Or(Vector64<sbyte> left, Vector64<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x2_t vorr_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Or(Vector64<float> left, Vector64<float> right) => Or(left, right);

        /// <summary>
        /// uint16x4_t vorr_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Or(Vector64<ushort> left, Vector64<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x2_t vorr_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Or(Vector64<uint> left, Vector64<uint> right) => Or(left, right);

        // /// <summary>
        // /// uint64x1_t vorr_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Or(Vector64<ulong> left, Vector64<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x16_t vorr_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) => Or(left, right);

        /// <summary>
        /// float64x2_t vorr_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) => Or(left, right);

        /// <summary>
        /// int16x8_t vorr_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) => Or(left, right);

        /// <summary>
        /// int32x4_t vorr_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) => Or(left, right);

        /// <summary>
        /// int64x2_t vorr_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) => Or(left, right);

        /// <summary>
        /// int8x16_t vorr_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) => Or(left, right);

        /// <summary>
        /// float32x4_t vorr_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) => Or(left, right);

        /// <summary>
        /// uint16x8_t vorr_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) => Or(left, right);

        /// <summary>
        /// uint32x4_t vorr_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) => Or(left, right);

        /// <summary>
        /// uint64x2_t vorr_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) => Or(left, right);

        /// <summary>
        /// uint8x8_t vorn_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) => OrNot(left, right);

        // /// <summary>
        // /// float64x1_t vorn_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x4_t vorn_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x2_t vorn_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) => OrNot(left, right);

        // /// <summary>
        // /// int64x1_t vorn_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t vorn_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x2_t vorn_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x4_t vorn_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x2_t vorn_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) => OrNot(left, right);

        // /// <summary>
        // /// uint64x1_t vorn_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) => OrNot(left, right);

        /// <summary>
        /// uint8x16_t vorn_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) => OrNot(left, right);

        /// <summary>
        /// float64x2_t vorn_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) => OrNot(left, right);

        /// <summary>
        /// int16x8_t vorn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) => OrNot(left, right);

        /// <summary>
        /// int32x4_t vorn_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) => OrNot(left, right);

        /// <summary>
        /// int64x2_t vorn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) => OrNot(left, right);

        /// <summary>
        /// int8x16_t vorn_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) => OrNot(left, right);

        /// <summary>
        /// float32x4_t vorn_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) => OrNot(left, right);

        /// <summary>
        /// uint16x8_t vorn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) => OrNot(left, right);

        /// <summary>
        /// uint32x4_t vorn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) => OrNot(left, right);

        /// <summary>
        /// uint64x2_t vorn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> OrNot(Vector128<ulong> left, Vector128<ulong> right) => OrNot(left, right);

        /// <summary>
        /// int8x8_t vcnt_s8 (int8x8_t a)
        ///   A32: VCNT Dd, Dm
        ///   A64: CNT Vd, Vn
        /// </summary>
        public static Vector64<sbyte> PopCount(Vector64<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x8_t vcnt_u8 (uint8x8_t a)
        ///   A32: VCNT Dd, Dm
        ///   A64: CNT Vd, Vn
        /// </summary>
        public static Vector64<byte> PopCount(Vector64<byte> value) => PopCount(value);

        /// <summary>
        /// int8x16_t vcntq_s8 (int8x16_t a)
        ///   A32: VCNT Qd, Qm
        ///   A64: CNT Vd, Vn
        /// </summary>
        public static Vector128<sbyte> PopCount(Vector128<sbyte> value) => PopCount(value);

        /// <summary>
        /// uint8x16_t vcntq_u8 (uint8x16_t a)
        ///   A32: VCNT Qd, Qm
        ///   A64: CNT Vd, Vn
        /// </summary>
        public static Vector128<byte> PopCount(Vector128<byte> value) => PopCount(value);

        /// <summary>
        /// uint8x8_t vsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Subtract(Vector64<byte> left, Vector64<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x4_t vsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Subtract(Vector64<short> left, Vector64<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x2_t vsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Subtract(Vector64<int> left, Vector64<int> right) => Subtract(left, right);

        /// <summary>
        /// int8x8_t vsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Subtract(Vector64<sbyte> left, Vector64<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// float32x2_t vsub_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VSUB.F32 Dd, Dn, Dm
        ///   A64: FSUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Subtract(Vector64<float> left, Vector64<float> right) => Subtract(left, right);

        /// <summary>
        /// uint16x4_t vsub_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Subtract(Vector64<ushort> left, Vector64<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x2_t vsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Subtract(Vector64<uint> left, Vector64<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint8x16_t vsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) => Subtract(left, right);

        /// <summary>
        /// int16x8_t vsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) => Subtract(left, right);

        /// <summary>
        /// int32x4_t vsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) => Subtract(left, right);

        /// <summary>
        /// int64x2_t vsubq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) => Subtract(left, right);

        /// <summary>
        /// int8x16_t vsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) => Subtract(left, right);

        /// <summary>
        /// float32x4_t vsubq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VSUB.F32 Qd, Qn, Qm
        ///   A64: FSUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) => Subtract(left, right);

        /// <summary>
        /// uint16x8_t vsubq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) => Subtract(left, right);

        /// <summary>
        /// uint32x4_t vsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) => Subtract(left, right);

        /// <summary>
        /// uint64x2_t vsubq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) => Subtract(left, right);

        // /// <summary>
        // /// float64x1_t vsub_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VSUB.F64 Dd, Dn, Dm
        // ///   A64: FSUB Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<double> SubtractScalar(Vector64<double> left, Vector64<double> right) => Subtract(left, right);

        // /// <summary>
        // /// int64x1_t vsub_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<long> SubtractScalar(Vector64<long> left, Vector64<long> right) => SubtractScalar(left, right);

        // /// <summary>
        // /// uint64x1_t vsub_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<ulong> SubtractScalar(Vector64<ulong> left, Vector64<ulong> right) => SubtractScalar(left, right);

        /// <summary>
        ///   A32: VSUB.F32 Sd, Sn, Sm
        ///   A64:
        /// </summary>
        public static Vector64<float> SubtractScalar(Vector64<float> left, Vector64<float> right) => SubtractScalar(left, right);

        /// <summary>
        /// uint8x8_t veor_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Xor(Vector64<byte> left, Vector64<byte> right) => Xor(left, right);

        // /// <summary>
        // /// float64x1_t veor_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Xor(Vector64<double> left, Vector64<double> right) => Xor(left, right);

        /// <summary>
        /// int16x4_t veor_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Xor(Vector64<short> left, Vector64<short> right) => Xor(left, right);

        /// <summary>
        /// int32x2_t veor_s32(int32x2_t a, int32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Xor(Vector64<int> left, Vector64<int> right) => Xor(left, right);

        // /// <summary>
        // /// int64x1_t veor_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Xor(Vector64<long> left, Vector64<long> right) => Xor(left, right);

        /// <summary>
        /// int8x8_t veor_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Xor(Vector64<sbyte> left, Vector64<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x2_t veor_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Xor(Vector64<float> left, Vector64<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x4_t veor_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Xor(Vector64<ushort> left, Vector64<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x2_t veor_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Xor(Vector64<uint> left, Vector64<uint> right) => Xor(left, right);

        // /// <summary>
        // /// uint64x1_t veor_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Xor(Vector64<ulong> left, Vector64<ulong> right) => Xor(left, right);

        /// <summary>
        /// uint8x16_t veor_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) => Xor(left, right);

        /// <summary>
        /// float64x2_t veor_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) => Xor(left, right);

        /// <summary>
        /// int16x8_t veor_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) => Xor(left, right);

        /// <summary>
        /// int32x4_t veor_s32(int32x4_t a, int32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) => Xor(left, right);

        /// <summary>
        /// int64x2_t veor_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) => Xor(left, right);

        /// <summary>
        /// int8x16_t veor_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) => Xor(left, right);

        /// <summary>
        /// float32x4_t veor_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) => Xor(left, right);

        /// <summary>
        /// uint16x8_t veor_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) => Xor(left, right);

        /// <summary>
        /// uint32x4_t veor_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) => Xor(left, right);

        /// <summary>
        /// uint64x2_t veor_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) => Xor(left, right);
    }
}

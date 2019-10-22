// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM AdvSIMD hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class AdvSimd : ArmBase
    {
        internal AdvSimd() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// float64x2_t vabsq_f64 (float64x2_t a)
            ///   A64: FABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<double> Abs(Vector128<double> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int64x2_t vabsq_s64 (int64x2_t a)
            ///   A64: ABS Vd.2D, Vn.2D
            /// </summary>
            public static Vector128<ulong> Abs(Vector128<long> value) { throw new PlatformNotSupportedException(); }

            // /// <summary>
            // /// int64x1_t vabs_s64 (int64x1_t a)
            // ///   A64: ABS Dd, Dn
            // /// </summary>
            // public static Vector64<ulong> AbsScalar(Vector64<long> value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// float64x2_t vaddq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FADD Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Add(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// float64x2_t vsubq_f64 (float64x2_t a, float64x2_t b)
            ///   A64: FSUB Vd.2D, Vn.2D, Vm.2D
            /// </summary>
            public static Vector128<double> Subtract(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// int8x8_t vabs_s8 (int8x8_t a)
        ///   A32: VABS.S8 Dd, Dm
        ///   A64: ABS Vd.8B, Vn.8B
        /// </summary>
        public static Vector64<byte> Abs(Vector64<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vabs_s16 (int16x4_t a)
        ///   A32: VABS.S16 Dd, Dm
        ///   A64: ABS Vd.4H, Vn.4H
        /// </summary>
        public static Vector64<ushort> Abs(Vector64<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vabs_s32 (int32x2_t a)
        ///   A32: VABS.S32 Dd, Dm
        ///   A64: ABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<uint> Abs(Vector64<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vabs_f32 (float32x2_t a)
        ///   A32: VABS.F32 Dd, Dm
        ///   A64: FABS Vd.2S, Vn.2S
        /// </summary>
        public static Vector64<float> Abs(Vector64<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vabsq_s8 (int8x16_t a)
        ///   A32: VABS.S8 Qd, Qm
        ///   A64: ABS Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Abs(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vabsq_s16 (int16x8_t a)
        ///   A32: VABS.S16 Qd, Qm
        ///   A64: ABS Vd.8H, Vn.8H
        /// </summary>
        public static Vector128<ushort> Abs(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vabsq_s32 (int32x4_t a)
        ///   A32: VABS.S32 Qd, Qm
        ///   A64: ABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> Abs(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vabsq_f32 (float32x4_t a)
        ///   A32: VABS.F32 Qd, Qm
        ///   A64: FABS Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<float> Abs(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vabs_f64 (float64x1_t a)
        // ///   A32: VABS.F64 Dd, Dm
        // ///   A64: FABS Dd, Dn
        // /// </summary>
        // public static Vector64<double> AbsScalar(Vector64<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: VABS.F32 Sd, Sm
        ///   A64: FABS Sd, Sn
        /// </summary>
        public static Vector64<float> AbsScalar(Vector64<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vadd_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Add(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vadd_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Add(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vadd_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Add(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vadd_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VADD.I8 Dd, Dn, Dm
        ///   A64: ADD Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Add(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vadd_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VADD.F32 Dd, Dn, Dm
        ///   A64: FADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Add(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vadd_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VADD.I16 Dd, Dn, Dm
        ///   A64: ADD Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Add(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vadd_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VADD.I32 Dd, Dn, Dm
        ///   A64: ADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Add(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vaddq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Add(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vaddq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Add(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vaddq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Add(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vaddq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Add(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vaddq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VADD.I8 Qd, Qn, Qm
        ///   A64: ADD Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Add(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vaddq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VADD.F32 Qd, Qn, Qm
        ///   A64: FADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Add(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vaddq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VADD.I16 Qd, Qn, Qm
        ///   A64: ADD Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Add(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vaddq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VADD.I32 Qd, Qn, Qm
        ///   A64: ADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Add(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vaddq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VADD.I64 Qd, Qn, Qm
        ///   A64: ADD Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Add(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vadd_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VADD.F64 Dd, Dn, Dm
        // ///   A64: FADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<double> AddScalar(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vadd_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VADD.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<long> AddScalar(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vadd_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VADD.I64 Dd, Dn, Dm
        // ///   A64: ADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<ulong> AddScalar(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: VADD.F32 Sd, Sn, Sm
        ///   A64:
        /// </summary>
        public static Vector64<float> AddScalar(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vand_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> And(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vand_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> And(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vand_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> And(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vand_s32(int32x2_t a, int32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> And(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vand_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> And(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vand_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> And(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vand_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> And(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vand_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> And(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vand_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> And(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vand_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VAND Dd, Dn, Dm
        // ///   A64: AND Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> And(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vand_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> And(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vand_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> And(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vand_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> And(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vand_s32(int32x4_t a, int32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> And(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vand_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> And(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vand_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> And(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vand_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> And(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vand_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> And(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vand_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> And(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vand_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VAND Dd, Dn, Dm
        ///   A64: AND Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> And(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vbic_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> AndNot(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vbic_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> AndNot(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vbic_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> AndNot(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vbic_s32(int32x2_t a, int32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> AndNot(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vbic_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> AndNot(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vbic_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> AndNot(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vbic_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> AndNot(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vbic_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> AndNot(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vbic_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> AndNot(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vbic_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VBIC Dd, Dn, Dm
        // ///   A64: BIC Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> AndNot(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vbic_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> AndNot(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vbic_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> AndNot(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vbic_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> AndNot(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vbic_s32(int32x4_t a, int32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> AndNot(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vbic_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> AndNot(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vbic_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> AndNot(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vbic_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> AndNot(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vbic_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> AndNot(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vbic_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> AndNot(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vbic_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VBIC Dd, Dn, Dm
        ///   A64: BIC Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> AndNot(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vld1_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<byte> LoadVector64(byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vld1_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<short> LoadVector64(short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vld1_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<int> LoadVector64(int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vld1_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, [Rn]
        ///   A64: LD1 Vt.8B, [Xn]
        /// </summary>
        public static unsafe Vector64<sbyte> LoadVector64(sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vld1_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<float> LoadVector64(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vld1_u16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, [Rn]
        ///   A64: LD1 Vt.4H, [Xn]
        /// </summary>
        public static unsafe Vector64<ushort> LoadVector64(ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vld1_u32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, [Rn]
        ///   A64: LD1 Vt.2S, [Xn]
        /// </summary>
        public static unsafe Vector64<uint> LoadVector64(uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vld1q_u8 (uint8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<byte> LoadVector128(byte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vld1q_f64 (float64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<double> LoadVector128(double* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vld1q_s16 (int16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<short> LoadVector128(short* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vld1q_s32 (int32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<int> LoadVector128(int* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vld1q_s64 (int64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<long> LoadVector128(long* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vld1q_s8 (int8_t const * ptr)
        ///   A32: VLD1.8 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.16B, [Xn]
        /// </summary>
        public static unsafe Vector128<sbyte> LoadVector128(sbyte* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vld1q_f32 (float32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<float> LoadVector128(float* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vld1q_s16 (uint16_t const * ptr)
        ///   A32: VLD1.16 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.8H, [Xn]
        /// </summary>
        public static unsafe Vector128<ushort> LoadVector128(ushort* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vld1q_s32 (uint32_t const * ptr)
        ///   A32: VLD1.32 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.4S, [Xn]
        /// </summary>
        public static unsafe Vector128<uint> LoadVector128(uint* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vld1q_u64 (uint64_t const * ptr)
        ///   A32: VLD1.64 Dd, Dd+1, [Rn]
        ///   A64: LD1 Vt.2D, [Xn]
        /// </summary>
        public static unsafe Vector128<ulong> LoadVector128(ulong* address) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vmvn_u8 (uint8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Not(Vector64<byte> value) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vmvn_f64 (float64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Not(Vector64<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vmvn_s16 (int16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Not(Vector64<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vmvn_s32(int32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Not(Vector64<int> value) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vmvn_s64 (int64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Not(Vector64<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vmvn_s8 (int8x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Not(Vector64<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vmvn_f32 (float32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Not(Vector64<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vmvn_u16 (uint16x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Not(Vector64<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vmvn_u32 (uint32x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Not(Vector64<uint> value) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vmvn_u64 (uint64x1_t a)
        // ///   A32: VMVN Dd, Dn, Dm
        // ///   A64: MVN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Not(Vector64<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vmvn_u8 (uint8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Not(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vmvn_f64 (float64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Not(Vector128<double> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vmvn_s16 (int16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Not(Vector128<short> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vmvn_s32(int32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Not(Vector128<int> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vmvn_s64 (int64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Not(Vector128<long> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vmvn_s8 (int8x16_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Not(Vector128<sbyte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vmvn_f32 (float32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Not(Vector128<float> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vmvn_u16 (uint16x8_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Not(Vector128<ushort> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vmvn_u32 (uint32x4_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Not(Vector128<uint> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vmvn_u64 (uint64x2_t a)
        ///   A32: VMVN Dd, Dn, Dm
        ///   A64: MVN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Not(Vector128<ulong> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vorr_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Or(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vorr_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Or(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vorr_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Or(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vorr_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Or(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vorr_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Or(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vorr_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Or(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vorr_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Or(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vorr_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Or(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vorr_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Or(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vorr_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORR Dd, Dn, Dm
        // ///   A64: ORR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Or(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vorr_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Or(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vorr_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Or(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vorr_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Or(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vorr_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Or(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vorr_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Or(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vorr_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Or(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vorr_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Or(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vorr_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Or(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vorr_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Or(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vorr_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORR Dd, Dn, Dm
        ///   A64: ORR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Or(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vorn_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> OrNot(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vorn_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> OrNot(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vorn_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> OrNot(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vorn_s32(int32x2_t a, int32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> OrNot(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vorn_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> OrNot(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vorn_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> OrNot(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vorn_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> OrNot(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vorn_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> OrNot(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vorn_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> OrNot(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vorn_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VORN Dd, Dn, Dm
        // ///   A64: ORN Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> OrNot(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vorn_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> OrNot(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t vorn_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> OrNot(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vorn_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> OrNot(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vorn_s32(int32x4_t a, int32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> OrNot(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vorn_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> OrNot(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vorn_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> OrNot(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vorn_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> OrNot(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vorn_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> OrNot(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vorn_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> OrNot(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vorn_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VORN Dd, Dn, Dm
        ///   A64: ORN Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> OrNot(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t vsub_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: SUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<byte> Subtract(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vsub_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: SUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> Subtract(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vsub_s32 (int32x2_t a, int32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: SUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> Subtract(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t vsub_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VSUB.I8 Dd, Dn, Dm
        ///   A64: SUB Vd.8B, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<sbyte> Subtract(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t vsub_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VSUB.F32 Dd, Dn, Dm
        ///   A64: FADD Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<float> Subtract(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t vsub_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VSUB.I16 Dd, Dn, Dm
        ///   A64: SUB Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<ushort> Subtract(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vsub_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VSUB.I32 Dd, Dn, Dm
        ///   A64: SUB Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<uint> Subtract(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vsubq_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: SUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<byte> Subtract(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vsubq_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: SUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> Subtract(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vsubq_s32 (int32x4_t a, int32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: SUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> Subtract(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t vsubq_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: SUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> Subtract(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t vsubq_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VSUB.I8 Qd, Qn, Qm
        ///   A64: SUB Vd.16B, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<sbyte> Subtract(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t vsubq_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VSUB.F32 Qd, Qn, Qm
        ///   A64: FADD Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<float> Subtract(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t vsubq_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VSUB.I16 Qd, Qn, Qm
        ///   A64: SUB Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<ushort> Subtract(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vsubq_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VSUB.I32 Qd, Qn, Qm
        ///   A64: SUB Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> Subtract(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t vsubq_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VSUB.I64 Qd, Qn, Qm
        ///   A64: SUB Vd.2D, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> Subtract(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t vsub_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VSUB.F64 Dd, Dn, Dm
        // ///   A64: FADD Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<double> SubtractScalar(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t vsub_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: SUB Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<long> SubtractScalar(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t vsub_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VSUB.I64 Dd, Dn, Dm
        // ///   A64: SUB Dd, Dn, Dm
        // /// </summary>
        // public static Vector64<ulong> SubtractScalar(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   A32: VSUB.F32 Sd, Sn, Sm
        ///   A64:
        /// </summary>
        public static Vector64<float> SubtractScalar(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x8_t veor_u8 (uint8x8_t a, uint8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<byte> Xor(Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// float64x1_t veor_f64 (float64x1_t a, float64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        // /// </summary>
        // public static Vector64<double> Xor(Vector64<double> left, Vector64<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t veor_s16 (int16x4_t a, int16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<short> Xor(Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t veor_s32(int32x2_t a, int32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<int> Xor(Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// int64x1_t veor_s64 (int64x1_t a, int64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<long> Xor(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x8_t veor_s8 (int8x8_t a, int8x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<sbyte> Xor(Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x2_t veor_f32 (float32x2_t a, float32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector64<float> Xor(Vector64<float> left, Vector64<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x4_t veor_u16 (uint16x4_t a, uint16x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<ushort> Xor(Vector64<ushort> left, Vector64<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t veor_u32 (uint32x2_t a, uint32x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector64<uint> Xor(Vector64<uint> left, Vector64<uint> right) { throw new PlatformNotSupportedException(); }

        // /// <summary>
        // /// uint64x1_t veor_u64 (uint64x1_t a, uint64x1_t b)
        // ///   A32: VEOR Dd, Dn, Dm
        // ///   A64: EOR Vd, Vn, Vm
        // /// </summary>
        // public static Vector64<ulong> Xor(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t veor_u8 (uint8x16_t a, uint8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<byte> Xor(Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float64x2_t veor_f64 (float64x2_t a, float64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<double> Xor(Vector128<double> left, Vector128<double> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t veor_s16 (int16x8_t a, int16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<short> Xor(Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t veor_s32(int32x4_t a, int32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<int> Xor(Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int64x2_t veor_s64 (int64x2_t a, int64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<long> Xor(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int8x16_t veor_s8 (int8x16_t a, int8x16_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<sbyte> Xor(Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// float32x4_t veor_f32 (float32x4_t a, float32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// The above native signature does not exist. We provide this additional overload for consistency with the other scalar APIs.
        /// </summary>
        public static Vector128<float> Xor(Vector128<float> left, Vector128<float> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint16x8_t veor_u16 (uint16x8_t a, uint16x8_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ushort> Xor(Vector128<ushort> left, Vector128<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t veor_u32 (uint32x4_t a, uint32x4_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<uint> Xor(Vector128<uint> left, Vector128<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint64x2_t veor_u64 (uint64x2_t a, uint64x2_t b)
        ///   A32: VEOR Dd, Dn, Dm
        ///   A64: EOR Vd, Vn, Vm
        /// </summary>
        public static Vector128<ulong> Xor(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }
    }
}

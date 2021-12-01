// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARMv8.2-DotProd hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Dp : AdvSimd
    {
        internal Dp() { }

        public static new bool IsSupported { [Intrinsic] get => false; }

        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// int32x2_t vdot_s32 (int32x2_t r, int8x8_t a, int8x8_t b)
        ///   A32: VSDOT.S8 Dd, Dn, Dm
        ///   A64: SDOT Vd.2S, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<int> DotProduct(Vector64<int> addend, Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vdot_u32 (uint32x2_t r, uint8x8_t a, uint8x8_t b)
        ///   A32: VUDOT.U8 Dd, Dn, Dm
        ///   A64: UDOT Vd.2S, Vn.8B, Vm.8B
        /// </summary>
        public static Vector64<uint> DotProduct(Vector64<uint> addend, Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vdotq_s32 (int32x4_t r, int8x16_t a, int8x16_t b)
        ///   A32: VSDOT.S8 Qd, Qn, Qm
        ///   A64: SDOT Vd.4S, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<int> DotProduct(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vdotq_u32 (uint32x4_t r, uint8x16_t a, uint8x16_t b)
        ///   A32: VUDOT.U8 Qd, Qn, Qm
        ///   A64: UDOT Vd.4S, Vn.16B, Vm.16B
        /// </summary>
        public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vdot_lane_s32 (int32x2_t r, int8x8_t a, int8x8_t b, const int lane)
        ///   A32: VSDOT.S8 Dd, Dn, Dm[lane]
        ///   A64: SDOT Vd.2S, Vn.8B, Vm.4B[lane]
        /// </summary>
        public static Vector64<int> DotProductBySelectedQuadruplet(Vector64<int> addend, Vector64<sbyte> left, Vector64<sbyte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vdot_laneq_s32 (int32x2_t r, int8x8_t a, int8x16_t b, const int lane)
        ///   A32: VSDOT.S8 Dd, Dn, Dm[lane]
        ///   A64: SDOT Vd.2S, Vn.8B, Vm.4B[lane]
        /// </summary>
        public static Vector64<int> DotProductBySelectedQuadruplet(Vector64<int> addend, Vector64<sbyte> left, Vector128<sbyte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vdot_lane_u32 (uint32x2_t r, uint8x8_t a, uint8x8_t b, const int lane)
        ///   A32: VUDOT.U8 Dd, Dn, Dm[lane]
        ///   A64: UDOT Vd.2S, Vn.8B, Vm.4B[lane]
        /// </summary>
        public static Vector64<uint> DotProductBySelectedQuadruplet(Vector64<uint> addend, Vector64<byte> left, Vector64<byte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x2_t vdot_laneq_u32 (uint32x2_t r, uint8x8_t a, uint8x16_t b, const int lane)
        ///   A32: VUDOT.U8 Dd, Dn, Dm[lane]
        ///   A64: UDOT Vd.2S, Vn.8B, Vm.4B[lane]
        /// </summary>
        public static Vector64<uint> DotProductBySelectedQuadruplet(Vector64<uint> addend, Vector64<byte> left, Vector128<byte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vdotq_laneq_s32 (int32x4_t r, int8x16_t a, int8x16_t b, const int lane)
        ///   A32: VSDOT.S8 Qd, Qn, Dm[lane]
        ///   A64: SDOT Vd.4S, Vn.16B, Vm.4B[lane]
        /// </summary>
        public static Vector128<int> DotProductBySelectedQuadruplet(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vdotq_lane_s32 (int32x4_t r, int8x16_t a, int8x8_t b, const int lane)
        ///   A32: VSDOT.S8 Qd, Qn, Dm[lane]
        ///   A64: SDOT Vd.4S, Vn.16B, Vm.4B[lane]
        /// </summary>
        public static Vector128<int> DotProductBySelectedQuadruplet(Vector128<int> addend, Vector128<sbyte> left, Vector64<sbyte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vdotq_laneq_u32 (uint32x4_t r, uint8x16_t a, uint8x16_t b, const int lane)
        ///   A32: VUDOT.U8 Qd, Qn, Dm[lane]
        ///   A64: UDOT Vd.4S, Vn.16B, Vm.4B[lane]
        /// </summary>
        public static Vector128<uint> DotProductBySelectedQuadruplet(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vdotq_lane_u32 (uint32x4_t r, uint8x16_t a, uint8x8_t b, const int lane)
        ///   A32: VUDOT.U8 Qd, Qn, Dm[lane]
        ///   A64: UDOT Vd.4S, Vn.16B, Vm.4B[lane]
        /// </summary>
        public static Vector128<uint> DotProductBySelectedQuadruplet(Vector128<uint> addend, Vector128<byte> left, Vector64<byte> right, byte rightScaledIndex) { throw new PlatformNotSupportedException(); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARMv8.2-DotProd hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Dp : AdvSimd
    {
        internal Dp() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARMv8.2-DotProd hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>int32x2_t vdot_s32 (int32x2_t r, int8x8_t a, int8x8_t b)</para>
        ///   <para>  A32: VSDOT.S8 Dd, Dn, Dm</para>
        ///   <para>  A64: SDOT Vd.2S, Vn.8B, Vm.8B</para>
        /// </summary>
        public static Vector64<int> DotProduct(Vector64<int> addend, Vector64<sbyte> left, Vector64<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x2_t vdot_u32 (uint32x2_t r, uint8x8_t a, uint8x8_t b)</para>
        ///   <para>  A32: VUDOT.U8 Dd, Dn, Dm</para>
        ///   <para>  A64: UDOT Vd.2S, Vn.8B, Vm.8B</para>
        /// </summary>
        public static Vector64<uint> DotProduct(Vector64<uint> addend, Vector64<byte> left, Vector64<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int32x4_t vdotq_s32 (int32x4_t r, int8x16_t a, int8x16_t b)</para>
        ///   <para>  A32: VSDOT.S8 Qd, Qn, Qm</para>
        ///   <para>  A64: SDOT Vd.4S, Vn.16B, Vm.16B</para>
        /// </summary>
        public static Vector128<int> DotProduct(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vdotq_u32 (uint32x4_t r, uint8x16_t a, uint8x16_t b)</para>
        ///   <para>  A32: VUDOT.U8 Qd, Qn, Qm</para>
        ///   <para>  A64: UDOT Vd.4S, Vn.16B, Vm.16B</para>
        /// </summary>
        public static Vector128<uint> DotProduct(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int32x2_t vdot_lane_s32 (int32x2_t r, int8x8_t a, int8x8_t b, const int lane)</para>
        ///   <para>  A32: VSDOT.S8 Dd, Dn, Dm[lane]</para>
        ///   <para>  A64: SDOT Vd.2S, Vn.8B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector64<int> DotProductBySelectedQuadruplet(Vector64<int> addend, Vector64<sbyte> left, Vector64<sbyte> right, [ConstantExpected(Max = (byte)(7))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int32x2_t vdot_laneq_s32 (int32x2_t r, int8x8_t a, int8x16_t b, const int lane)</para>
        ///   <para>  A32: VSDOT.S8 Dd, Dn, Dm[lane]</para>
        ///   <para>  A64: SDOT Vd.2S, Vn.8B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector64<int> DotProductBySelectedQuadruplet(Vector64<int> addend, Vector64<sbyte> left, Vector128<sbyte> right, [ConstantExpected(Max = (byte)(15))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x2_t vdot_lane_u32 (uint32x2_t r, uint8x8_t a, uint8x8_t b, const int lane)</para>
        ///   <para>  A32: VUDOT.U8 Dd, Dn, Dm[lane]</para>
        ///   <para>  A64: UDOT Vd.2S, Vn.8B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector64<uint> DotProductBySelectedQuadruplet(Vector64<uint> addend, Vector64<byte> left, Vector64<byte> right, [ConstantExpected(Max = (byte)(7))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x2_t vdot_laneq_u32 (uint32x2_t r, uint8x8_t a, uint8x16_t b, const int lane)</para>
        ///   <para>  A32: VUDOT.U8 Dd, Dn, Dm[lane]</para>
        ///   <para>  A64: UDOT Vd.2S, Vn.8B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector64<uint> DotProductBySelectedQuadruplet(Vector64<uint> addend, Vector64<byte> left, Vector128<byte> right, [ConstantExpected(Max = (byte)(15))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int32x4_t vdotq_laneq_s32 (int32x4_t r, int8x16_t a, int8x16_t b, const int lane)</para>
        ///   <para>  A32: VSDOT.S8 Qd, Qn, Dm[lane]</para>
        ///   <para>  A64: SDOT Vd.4S, Vn.16B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector128<int> DotProductBySelectedQuadruplet(Vector128<int> addend, Vector128<sbyte> left, Vector128<sbyte> right, [ConstantExpected(Max = (byte)(15))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>int32x4_t vdotq_lane_s32 (int32x4_t r, int8x16_t a, int8x8_t b, const int lane)</para>
        ///   <para>  A32: VSDOT.S8 Qd, Qn, Dm[lane]</para>
        ///   <para>  A64: SDOT Vd.4S, Vn.16B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector128<int> DotProductBySelectedQuadruplet(Vector128<int> addend, Vector128<sbyte> left, Vector64<sbyte> right, [ConstantExpected(Max = (byte)(7))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vdotq_laneq_u32 (uint32x4_t r, uint8x16_t a, uint8x16_t b, const int lane)</para>
        ///   <para>  A32: VUDOT.U8 Qd, Qn, Dm[lane]</para>
        ///   <para>  A64: UDOT Vd.4S, Vn.16B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector128<uint> DotProductBySelectedQuadruplet(Vector128<uint> addend, Vector128<byte> left, Vector128<byte> right, [ConstantExpected(Max = (byte)(15))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vdotq_lane_u32 (uint32x4_t r, uint8x16_t a, uint8x8_t b, const int lane)</para>
        ///   <para>  A32: VUDOT.U8 Qd, Qn, Dm[lane]</para>
        ///   <para>  A64: UDOT Vd.4S, Vn.16B, Vm.4B[lane]</para>
        /// </summary>
        public static Vector128<uint> DotProductBySelectedQuadruplet(Vector128<uint> addend, Vector128<byte> left, Vector64<byte> right, [ConstantExpected(Max = (byte)(7))] byte rightScaledIndex) { throw new PlatformNotSupportedException(); }
    }
}

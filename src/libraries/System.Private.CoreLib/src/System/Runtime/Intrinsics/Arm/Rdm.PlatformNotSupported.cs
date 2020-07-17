// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARMv8.1-RDMA hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Rdm : AdvSimd
    {
        internal Rdm() { }

        public static new bool IsSupported { [Intrinsic] get => false; }

        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// int16_t vqrdmlahh_s16 (int16_t a, int16_t b, int16_t c)
            ///   A64: SQRDMLAH Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingAndAddSaturateHighScalar(Vector64<short> addend, Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlahs_s32 (int32_t a, int32_t b, int32_t c)
            ///   A64: SQRDMLAH Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingAndAddSaturateHighScalar(Vector64<int> addend, Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int16_t vqrdmlshh_s16 (int16_t a, int16_t b, int16_t c)
            ///   A64: SQRDMLSH Hd, Hn, Hm
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingAndSubtractSaturateHighScalar(Vector64<short> addend, Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlshs_s32 (int32_t a, int32_t b, int32_t c)
            ///   A64: SQRDMLSH Sd, Sn, Sm
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingAndSubtractSaturateHighScalar(Vector64<int> addend, Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int16_t vqrdmlahh_lane_s16 (int16_t a, int16_t b, int16x4_t v, const int lane)
            ///   A64: SQRDMLAH Hd, Hn, Vm.H[lane]
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(Vector64<short> addend, Vector64<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int16_t vqrdmlahh_laneq_s16 (int16_t a, int16_t b, int16x8_t v, const int lane)
            ///   A64: SQRDMLAH Hd, Hn, Vm.H[lane]
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(Vector64<short> addend, Vector64<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlahs_lane_s32 (int32_t a, int32_t b, int32x2_t v, const int lane)
            ///   A64: SQRDMLAH Sd, Sn, Vm.S[lane]
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(Vector64<int> addend, Vector64<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlahs_laneq_s32 (int32_t a, int32_t b, int32x4_t v, const int lane)
            ///   A64: SQRDMLAH Sd, Sn, Vm.S[lane]
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh(Vector64<int> addend, Vector64<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int16_t vqrdmlshh_lane_s16 (int16_t a, int16_t b, int16x4_t v, const int lane)
            ///   A64: SQRDMLSH Hd, Hn, Vm.H[lane]
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(Vector64<short> minuend, Vector64<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int16_t vqrdmlshh_laneq_s16 (int16_t a, int16_t b, int16x8_t v, const int lane)
            ///   A64: SQRDMLSH Hd, Hn, Vm.H[lane]
            /// </summary>
            public static Vector64<short> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(Vector64<short> minuend, Vector64<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlshs_lane_s32 (int32_t a, int32_t b, int32x2_t v, const int lane)
            ///   A64: SQRDMLSH Sd, Sn, Vm.S[lane]
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(Vector64<int> minuend, Vector64<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// int32_t vqrdmlshs_laneq_s32 (int32_t a, int32_t b, int32x4_t v, const int lane)
            ///   A64: SQRDMLSH Sd, Sn, Vm.S[lane]
            /// </summary>
            public static Vector64<int> MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh(Vector64<int> minuend, Vector64<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// int16x4_t vqrdmlah_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VQRDMLAH.S16 Dd, Dn, Dm
        ///   A64: SQRDMLAH Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingAndAddSaturateHigh(Vector64<short> addend, Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlah_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VQRDMLAH.S32 Dd, Dn, Dm
        ///   A64: SQRDMLAH Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingAndAddSaturateHigh(Vector64<int> addend, Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlahq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VQRDMLAH.S16 Qd, Qn, Qm
        ///   A64: SQRDMLAH Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingAndAddSaturateHigh(Vector128<short> addend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlahq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VQRDMLAH.S32 Qd, Qn, Qm
        ///   A64: SQRDMLAH Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingAndAddSaturateHigh(Vector128<int> addend, Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vqrdmlsh_s16 (int16x4_t a, int16x4_t b, int16x4_t c)
        ///   A32: VQRDMLSH.S16 Dd, Dn, Dm
        ///   A64: SQRDMLSH Vd.4H, Vn.4H, Vm.4H
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingAndSubtractSaturateHigh(Vector64<short> minuend, Vector64<short> left, Vector64<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlsh_s32 (int32x2_t a, int32x2_t b, int32x2_t c)
        ///   A32: VQRDMLSH.S32 Dd, Dn, Dm
        ///   A64: SQRDMLSH Vd.2S, Vn.2S, Vm.2S
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingAndSubtractSaturateHigh(Vector64<int> minuend, Vector64<int> left, Vector64<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlshq_s16 (int16x8_t a, int16x8_t b, int16x8_t c)
        ///   A32: VQRDMLSH.S16 Qd, Qn, Qm
        ///   A64: SQRDMLSH Vd.8H, Vn.8H, Vm.8H
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingAndSubtractSaturateHigh(Vector128<short> minuend, Vector128<short> left, Vector128<short> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlshq_s32 (int32x4_t a, int32x4_t b, int32x4_t c)
        ///   A32: VQRDMLSH.S32 Qd, Qn, Qm
        ///   A64: SQRDMLSH Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingAndSubtractSaturateHigh(Vector128<int> minuend, Vector128<int> left, Vector128<int> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vqrdmlah_lane_s16 (int16x4_t a, int16x4_t b, int16x4_t v, const int lane)
        ///   A32: VQRDMLAH.S16 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLAH Vd.4H, Vn.4H, Vm.H[lane]
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector64<short> addend, Vector64<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vqrdmlah_laneq_s16 (int16x4_t a, int16x4_t b, int16x8_t v, const int lane)
        ///   A32: VQRDMLAH.S16 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLAH Vd.4H, Vn.4H, Vm.H[lane]
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector64<short> addend, Vector64<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlah_lane_s32 (int32x2_t a, int32x2_t b, int32x2_t v, const int lane)
        ///   A32: VQRDMLAH.S32 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLAH Vd.2S, Vn.2S, Vm.S[lane]
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector64<int> addend, Vector64<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlah_laneq_s32 (int32x2_t a, int32x2_t b, int32x4_t v, const int lane)
        ///   A32: VQRDMLAH.S32 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLAH Vd.2S, Vn.2S, Vm.S[lane]
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector64<int> addend, Vector64<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlahq_lane_s16 (int16x8_t a, int16x8_t b, int16x4_t v, const int lane)
        ///   A32: VQRDMLAH.S16 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLAH Vd.8H, Vn.8H, Vm.H[lane]
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector128<short> addend, Vector128<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlahq_laneq_s16 (int16x8_t a, int16x8_t b, int16x8_t v, const int lane)
        ///   A32: VQRDMLAH.S16 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLAH Vd.8H, Vn.8H, Vm.H[lane]
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector128<short> addend, Vector128<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlahq_lane_s32 (int32x4_t a, int32x4_t b, int32x2_t v, const int lane)
        ///   A32: VQRDMLAH.S32 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLAH Vd.4S, Vn.4S, Vm.S[lane]
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector128<int> addend, Vector128<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlahq_laneq_s32 (int32x4_t a, int32x4_t b, int32x4_t v, const int lane)
        ///   A32: VQRDMLAH.S32 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLAH Vd.4S, Vn.4S, Vm.S[lane]
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh(Vector128<int> addend, Vector128<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vqrdmlsh_lane_s16 (int16x4_t a, int16x4_t b, int16x4_t v, const int lane)
        ///   A32: VQRDMLSH.S16 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLSH Vd.4H, Vn.4H, Vm.H[lane]
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector64<short> minuend, Vector64<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x4_t vqrdmlsh_laneq_s16 (int16x4_t a, int16x4_t b, int16x8_t v, const int lane)
        ///   A32: VQRDMLSH.S16 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLSH Vd.4H, Vn.4H, Vm.H[lane]
        /// </summary>
        public static Vector64<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector64<short> minuend, Vector64<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlsh_lane_s32 (int32x2_t a, int32x2_t b, int32x2_t v, const int lane)
        ///   A32: VQRDMLSH.S32 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLSH Vd.2S, Vn.2S, Vm.S[lane]
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector64<int> minuend, Vector64<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x2_t vqrdmlsh_laneq_s32 (int32x2_t a, int32x2_t b, int32x4_t v, const int lane)
        ///   A32: VQRDMLSH.S32 Dd, Dn, Dm[lane]
        ///   A64: SQRDMLSH Vd.2S, Vn.2S, Vm.S[lane]
        /// </summary>
        public static Vector64<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector64<int> minuend, Vector64<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlshq_lane_s16 (int16x8_t a, int16x8_t b, int16x4_t v, const int lane)
        ///   A32: VQRDMLSH.S16 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLSH Vd.8H, Vn.8H, Vm.H[lane]
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector128<short> minuend, Vector128<short> left, Vector64<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int16x8_t vqrdmlshq_laneq_s16 (int16x8_t a, int16x8_t b, int16x8_t v, const int lane)
        ///   A32: VQRDMLSH.S16 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLSH Vd.8H, Vn.8H, Vm.H[lane]
        /// </summary>
        public static Vector128<short> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector128<short> minuend, Vector128<short> left, Vector128<short> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlshq_lane_s32 (int32x4_t a, int32x4_t b, int32x2_t v, const int lane)
        ///   A32: VQRDMLSH.S32 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLSH Vd.4S, Vn.4S, Vm.S[lane]
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector128<int> minuend, Vector128<int> left, Vector64<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// int32x4_t vqrdmlshq_laneq_s32 (int32x4_t a, int32x4_t b, int32x4_t v, const int lane)
        ///   A32: VQRDMLSH.S32 Qd, Qn, Dm[lane]
        ///   A64: SQRDMLSH Vd.4S, Vn.4S, Vm.S[lane]
        /// </summary>
        public static Vector128<int> MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh(Vector128<int> minuend, Vector128<int> left, Vector128<int> right, byte rightIndex) { throw new PlatformNotSupportedException(); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SHA256 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Sha256 : ArmBase
    {
        internal Sha256() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM SHA256 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// uint32x4_t vsha256hq_u32 (uint32x4_t hash_abcd, uint32x4_t hash_efgh, uint32x4_t wk)
        ///   A32: SHA256H.32 Qd, Qn, Qm
        ///   A64: SHA256H Qd, Qn, Vm.4S
        /// </summary>
        public static Vector128<uint> HashUpdate1(Vector128<uint> hash_abcd, Vector128<uint> hash_efgh, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vsha256h2q_u32 (uint32x4_t hash_efgh, uint32x4_t hash_abcd, uint32x4_t wk)
        ///   A32: SHA256H2.32 Qd, Qn, Qm
        ///   A64: SHA256H2 Qd, Qn, Vm.4S
        /// </summary>
        public static Vector128<uint> HashUpdate2(Vector128<uint> hash_efgh, Vector128<uint> hash_abcd, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vsha256su0q_u32 (uint32x4_t w0_3, uint32x4_t w4_7)
        ///   A32: SHA256SU0.32 Qd, Qm
        ///   A64: SHA256SU0 Vd.4S, Vn.4S
        /// </summary>
        public static Vector128<uint> ScheduleUpdate0(Vector128<uint> w0_3, Vector128<uint> w4_7) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32x4_t vsha256su1q_u32 (uint32x4_t w0_3, uint32x4_t w8_11, uint32x4_t w12_15)
        ///   A32: SHA256SU1.32 Qd, Qn, Qm
        ///   A64: SHA256SU1 Vd.4S, Vn.4S, Vm.4S
        /// </summary>
        public static Vector128<uint> ScheduleUpdate1(Vector128<uint> w0_3, Vector128<uint> w8_11, Vector128<uint> w12_15) { throw new PlatformNotSupportedException(); }
    }
}

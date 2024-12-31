// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SHA1 hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Sha1 : ArmBase
    {
        internal Sha1() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM SHA1 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>uint32_t vsha1h_u32 (uint32_t hash_e)</para>
        ///   <para>  A32: SHA1H.32 Qd, Qm</para>
        ///   <para>  A64: SHA1H Sd, Sn</para>
        /// </summary>
        public static Vector64<uint> FixedRotate(Vector64<uint> hash_e) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vsha1cq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)</para>
        ///   <para>  A32: SHA1C.32 Qd, Qn, Qm</para>
        ///   <para>  A64: SHA1C Qd, Sn, Vm.4S</para>
        /// </summary>
        public static Vector128<uint> HashUpdateChoose(Vector128<uint> hash_abcd, Vector64<uint> hash_e, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vsha1mq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)</para>
        ///   <para>  A32: SHA1M.32 Qd, Qn, Qm</para>
        ///   <para>  A64: SHA1M Qd, Sn, Vm.4S</para>
        /// </summary>
        public static Vector128<uint> HashUpdateMajority(Vector128<uint> hash_abcd, Vector64<uint> hash_e, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vsha1pq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)</para>
        ///   <para>  A32: SHA1P.32 Qd, Qn, Qm</para>
        ///   <para>  A64: SHA1P Qd, Sn, Vm.4S</para>
        /// </summary>
        public static Vector128<uint> HashUpdateParity(Vector128<uint> hash_abcd, Vector64<uint> hash_e, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vsha1su0q_u32 (uint32x4_t w0_3, uint32x4_t w4_7, uint32x4_t w8_11)</para>
        ///   <para>  A32: SHA1SU0.32 Qd, Qn, Qm</para>
        ///   <para>  A64: SHA1SU0 Vd.4S, Vn.4S, Vm.4S</para>
        /// </summary>
        public static Vector128<uint> ScheduleUpdate0(Vector128<uint> w0_3, Vector128<uint> w4_7, Vector128<uint> w8_11) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint32x4_t vsha1su1q_u32 (uint32x4_t tw0_3, uint32x4_t w12_15)</para>
        ///   <para>  A32: SHA1SU1.32 Qd, Qm</para>
        ///   <para>  A64: SHA1SU1 Vd.4S, Vn.4S</para>
        /// </summary>
        public static Vector128<uint> ScheduleUpdate1(Vector128<uint> tw0_3, Vector128<uint> w12_15) { throw new PlatformNotSupportedException(); }
    }
}

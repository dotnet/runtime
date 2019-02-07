// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Arm.Arm64
{
    /// <summary>
    /// This class provides access to the Arm64 SHA1 Crypto intrinsics
    ///
    /// Arm64 CPU indicate support for this feature by setting
    /// ID_AA64ISAR0_EL1.SHA1 is 1 or better
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public static class Sha1
    {
        public static bool IsSupported {  get => IsSupported; }

        /// <summary>
        /// Performs SHA1 hash update choose form.
        /// vsha1cq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)
        /// </summary>
        public static Vector128<uint> HashChoose(Vector128<uint> hash_abcd, uint hash_e, Vector128<uint>wk) => HashChoose(hash_abcd, hash_e, wk);

        /// <summary>
        /// Performs SHA1 hash update majority form.
        /// vsha1mq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)
        /// </summary>
        public static Vector128<uint> HashMajority(Vector128<uint> hash_abcd, uint hash_e, Vector128<uint>wk) => HashMajority(hash_abcd, hash_e, wk);

        /// <summary>
        /// Performs SHA1 hash update parity form.
        /// vsha1pq_u32 (uint32x4_t hash_abcd, uint32_t hash_e, uint32x4_t wk)
        /// </summary>
        public static Vector128<uint> HashParity(Vector128<uint> hash_abcd, uint hash_e, Vector128<uint>wk) => HashParity(hash_abcd, hash_e, wk);

        /// <summary>
        /// Performs SHA1 fixed rotate
        /// vsha1h_u32 (uint32_t hash_e)
        /// </summary>
        public static uint FixedRotate(uint hash_e) => FixedRotate(hash_e);

        /// <summary>
        /// Performs SHA1 schedule update 0
        /// vsha1su0q_u32 (uint32x4_t w0_3, uint32x4_t w4_7, uint32x4_t w8_11)
        /// </summary>
        public static Vector128<uint> SchedulePart1(Vector128<uint> w0_3, Vector128<uint> w4_7, Vector128<uint> w8_11) => SchedulePart1(w0_3, w4_7, w8_11);

        /// <summary>
        /// Performs SHA1 schedule update 1
        /// vsha1su1q_u32 (uint32x4_t tw0_3, uint32x4_t w12_15)
        /// </summary>
        public static Vector128<uint> SchedulePart2(Vector128<uint> tw0_3, Vector128<uint> w12_15)  => SchedulePart2(tw0_3, w12_15);
    }
}

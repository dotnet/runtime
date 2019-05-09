// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Arm.Arm64
{
    /// <summary>
    /// This class provides access to the Arm64 SHA256 Crypto intrinsics
    ///
    /// Arm64 CPU indicate support for this feature by setting
    /// ID_AA64ISAR0_EL1.SHA2 is 1 or better
    /// </summary>
    [CLSCompliant(false)]
    public static class Sha256
    {
        public static bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>
        /// Performs SHA256 hash update (part 1).
        /// vsha256hq_u32 (uint32x4_t hash_abcd, uint32x4_t hash_efgh, uint32x4_t wk)
        /// </summary>
        public static Vector128<uint> HashLower(Vector128<uint> hash_abcd, Vector128<uint> hash_efgh, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// Performs SHA256 hash update (part 2).
        /// vsha256h2q_u32 (uint32x4_t hash_efgh, uint32x4_t hash_abcd, uint32x4_t wk)
        /// </summary>
        public static Vector128<uint> HashUpper(Vector128<uint> hash_efgh, Vector128<uint> hash_abcd, Vector128<uint> wk) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// Performs SHA256 schedule update 0
        /// vsha256su0q_u32 (uint32x4_t w0_3, uint32x4_t w4_7)
        /// </summary>
        public static Vector128<uint> SchedulePart1(Vector128<uint> w0_3, Vector128<uint> w4_7) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// Performs SHA256 schedule update 1
        /// vsha256su1q_u32 (uint32x4_t w0_3, uint32x4_t w8_11, uint32x4_t w12_15)
        /// </summary>
        public static Vector128<uint> SchedulePart2(Vector128<uint> w0_3, Vector128<uint> w8_11, Vector128<uint> w12_15) { throw new PlatformNotSupportedException(); }
    }
}

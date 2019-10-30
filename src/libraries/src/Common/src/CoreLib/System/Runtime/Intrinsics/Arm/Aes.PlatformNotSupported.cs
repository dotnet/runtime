// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0060 // unused parameters
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM AES hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Aes : ArmBase
    {
        internal Aes() { }

        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>
        /// uint8x16_t vaesdq_u8 (uint8x16_t data, uint8x16_t key)
        ///   A32: AESD.8 Qd, Qm
        ///   A64: AESD Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vaeseq_u8 (uint8x16_t data, uint8x16_t key)
        ///   A32: AESE.8 Qd, Qm
        ///   A64: AESE Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vaesimcq_u8 (uint8x16_t data)
        ///   A32: AESIMC.8 Qd, Qm
        ///   A64: AESIMC Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint8x16_t vaesmcq_u8 (uint8x16_t data)
        ///   A32: AESMC.8 Qd, Qm
        ///   A64: AESMC V>.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> MixColumns(Vector128<byte> value) { throw new PlatformNotSupportedException(); }
    }
}

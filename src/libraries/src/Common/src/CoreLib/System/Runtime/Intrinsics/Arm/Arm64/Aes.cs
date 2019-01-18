// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Runtime.Intrinsics.Arm.Arm64
{
    /// <summary>
    /// This class provides access to the Arm64 AES Crypto intrinsics
    ///
    /// Arm64 CPU indicate support for this feature by setting
    /// ID_AA64ISAR0_EL1.AES is 1 or better
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public static class Aes
    {
        public static bool IsSupported { get => IsSupported; }
        /// <summary>
        /// Performs AES single round decryption
        /// vaesdq_u8 (uint8x16_t data, uint8x16_t key)
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) => Decrypt(value, roundKey);

        /// <summary>
        /// Performs AES single round encryption
        /// vaeseq_u8 (uint8x16_t data, uint8x16_t key)
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) => Encrypt(value, roundKey);

        /// <summary>
        /// Performs AES  Mix Columns
        /// vaesmcq_u8 (uint8x16_t data)
        /// </summary>
        public static Vector128<byte> MixColumns(Vector128<byte> value) => MixColumns(value);

        /// <summary>
        /// Performs AES inverse mix columns
        /// vaesimcq_u8  (uint8x16_t data)
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) => InverseMixColumns(value);
    }
}

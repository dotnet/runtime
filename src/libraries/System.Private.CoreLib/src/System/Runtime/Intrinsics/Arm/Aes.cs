// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM AES hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Aes : ArmBase
    {
        internal Aes() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        /// <summary>
        /// uint8x16_t vaesdq_u8 (uint8x16_t data, uint8x16_t key)
        ///   A32: AESD.8 Qd, Qm
        ///   A64: AESD Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) => Decrypt(value, roundKey);

        /// <summary>
        /// uint8x16_t vaeseq_u8 (uint8x16_t data, uint8x16_t key)
        ///   A32: AESE.8 Qd, Qm
        ///   A64: AESE Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) => Encrypt(value, roundKey);

        /// <summary>
        /// uint8x16_t vaesimcq_u8 (uint8x16_t data)
        ///   A32: AESIMC.8 Qd, Qm
        ///   A64: AESIMC Vd.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) => InverseMixColumns(value);

        /// <summary>
        /// uint8x16_t vaesmcq_u8 (uint8x16_t data)
        ///   A32: AESMC.8 Qd, Qm
        ///   A64: AESMC V>.16B, Vn.16B
        /// </summary>
        public static Vector128<byte> MixColumns(Vector128<byte> value) => MixColumns(value);

        /// <summary>
        /// poly128_t vmull_p64 (poly64_t a, poly64_t b)
        ///   A32: VMULL.P8 Qd, Dn, Dm
        ///   A64: PMULL Vd.1Q, Vn.1D, Vm.1D
        /// </summary>
        public static Vector128<long> PolynomialMultiplyWideningLower(Vector64<long> left, Vector64<long> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// poly128_t vmull_p64 (poly64_t a, poly64_t b)
        ///   A32: VMULL.P8 Qd, Dn, Dm
        ///   A64: PMULL Vd.1Q, Vn.1D, Vm.1D
        /// </summary>
        public static Vector128<ulong> PolynomialMultiplyWideningLower(Vector64<ulong> left, Vector64<ulong> right) => PolynomialMultiplyWideningLower(left, right);

        /// <summary>
        /// poly128_t vmull_high_p64 (poly64x2_t a, poly64x2_t b)
        ///   A32: VMULL.P8 Qd, Dn+1, Dm+1
        ///   A64: PMULL2 Vd.1Q, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<long> PolynomialMultiplyWideningUpper(Vector128<long> left, Vector128<long> right) => PolynomialMultiplyWideningUpper(left, right);

        /// <summary>
        /// poly128_t vmull_high_p64 (poly64x2_t a, poly64x2_t b)
        ///   A32: VMULL.P8 Qd, Dn+1, Dm+1
        ///   A64: PMULL2 Vd.1Q, Vn.2D, Vm.2D
        /// </summary>
        public static Vector128<ulong> PolynomialMultiplyWideningUpper(Vector128<ulong> left, Vector128<ulong> right) => PolynomialMultiplyWideningUpper(left, right);
    }
}

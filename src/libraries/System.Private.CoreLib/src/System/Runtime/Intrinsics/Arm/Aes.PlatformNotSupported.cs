// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM AES hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Aes : ArmBase
    {
        internal Aes() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM AES hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        ///   <para>uint8x16_t vaesdq_u8 (uint8x16_t data, uint8x16_t key)</para>
        ///   <para>  A32: AESD.8 Qd, Qm</para>
        ///   <para>  A64: AESD Vd.16B, Vn.16B</para>
        /// </summary>
        public static Vector128<byte> Decrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint8x16_t vaeseq_u8 (uint8x16_t data, uint8x16_t key)</para>
        ///   <para>  A32: AESE.8 Qd, Qm</para>
        ///   <para>  A64: AESE Vd.16B, Vn.16B</para>
        /// </summary>
        public static Vector128<byte> Encrypt(Vector128<byte> value, Vector128<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint8x16_t vaesimcq_u8 (uint8x16_t data)</para>
        ///   <para>  A32: AESIMC.8 Qd, Qm</para>
        ///   <para>  A64: AESIMC Vd.16B, Vn.16B</para>
        /// </summary>
        public static Vector128<byte> InverseMixColumns(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>uint8x16_t vaesmcq_u8 (uint8x16_t data)</para>
        ///   <para>  A32: AESMC.8 Qd, Qm</para>
        ///   <para>  A64: AESMC V>.16B, Vn.16B</para>
        /// </summary>
        public static Vector128<byte> MixColumns(Vector128<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>poly128_t vmull_p64 (poly64_t a, poly64_t b)</para>
        ///   <para>  A32: VMULL.P8 Qd, Dn, Dm</para>
        ///   <para>  A64: PMULL Vd.1Q, Vn.1D, Vm.1D</para>
        /// </summary>
        public static Vector128<long> PolynomialMultiplyWideningLower(Vector64<long> left, Vector64<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>poly128_t vmull_p64 (poly64_t a, poly64_t b)</para>
        ///   <para>  A32: VMULL.P8 Qd, Dn, Dm</para>
        ///   <para>  A64: PMULL Vd.1Q, Vn.1D, Vm.1D</para>
        /// </summary>
        public static Vector128<ulong> PolynomialMultiplyWideningLower(Vector64<ulong> left, Vector64<ulong> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>poly128_t vmull_high_p64 (poly64x2_t a, poly64x2_t b)</para>
        ///   <para>  A32: VMULL.P8 Qd, Dn+1, Dm+1</para>
        ///   <para>  A64: PMULL2 Vd.1Q, Vn.2D, Vm.2D</para>
        /// </summary>
        public static Vector128<long> PolynomialMultiplyWideningUpper(Vector128<long> left, Vector128<long> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>poly128_t vmull_high_p64 (poly64x2_t a, poly64x2_t b)</para>
        ///   <para>  A32: VMULL.P8 Qd, Dn+1, Dm+1</para>
        ///   <para>  A64: PMULL2 Vd.1Q, Vn.2D, Vm.2D</para>
        /// </summary>
        public static Vector128<ulong> PolynomialMultiplyWideningUpper(Vector128<ulong> left, Vector128<ulong> right) { throw new PlatformNotSupportedException(); }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SveAes hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    [Experimental(Experimentals.ArmSveDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class SveAes : ArmBase
    {
        internal SveAes() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the ARM SveAes hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }
        }

        /// <summary>
        /// svuint8_t svaesd[_u8](svuint8_t op1, svuint8_t op2)
        ///   AESD Ztied1.B, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> Decrypt(Vector<byte> value, Vector<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svaese[_u8](svuint8_t op1, svuint8_t op2)
        ///   AESE Ztied1.B, Ztied1.B, Zop2.B
        /// </summary>
        public static Vector<byte> Encrypt(Vector<byte> value, Vector<byte> roundKey) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svaesimc[_u8](svuint8_t op1)
        ///   AESIMC Ztied1.B, Ztied1.B
        /// </summary>
        public static Vector<byte> InverseMixColumns(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint8_t svaesmc[_u8](svuint8_t op1)
        ///   AESMC Ztied1.B, Ztied1.B
        /// </summary>
        public static Vector<byte> MixColumns(Vector<byte> value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svpmullb[_u16](svuint8_t op1, svuint8_t op2)
        ///   PMULLB Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> PolynomialMultiplyWideningEven(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svpmullb[_u64](svuint32_t op1, svuint32_t op2)
        ///   PMULLB Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> PolynomialMultiplyWideningEven(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svpmullt[_u16](svuint8_t op1, svuint8_t op2)
        ///   PMULLT Zresult.H, Zop1.B, Zop2.B
        /// </summary>
        public static Vector<ushort> PolynomialMultiplyWideningOdd(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svpmullt[_u64](svuint32_t op1, svuint32_t op2)
        ///   PMULLT Zresult.D, Zop1.S, Zop2.S
        /// </summary>
        public static Vector<ulong> PolynomialMultiplyWideningOdd(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }
    }
}

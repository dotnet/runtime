// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class SveAes : AdvSimd
    {
        internal SveAes() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class Arm64 : AdvSimd.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { get => IsSupported; }
        }

        ///  AesInverseMixColumns : AES inverse mix columns

        /// <summary>
        /// svuint8_t svaesimc[_u8](svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> AesInverseMixColumns(Vector<byte> value) { throw new PlatformNotSupportedException(); }


        ///  AesMixColumns : AES mix columns

        /// <summary>
        /// svuint8_t svaesmc[_u8](svuint8_t op)
        /// </summary>
        public static unsafe Vector<byte> AesMixColumns(Vector<byte> value) { throw new PlatformNotSupportedException(); }


        ///  AesSingleRoundDecryption : AES single round decryption

        /// <summary>
        /// svuint8_t svaesd[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AesSingleRoundDecryption(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }


        ///  AesSingleRoundEncryption : AES single round encryption

        /// <summary>
        /// svuint8_t svaese[_u8](svuint8_t op1, svuint8_t op2)
        /// </summary>
        public static unsafe Vector<byte> AesSingleRoundEncryption(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }


        ///  PolynomialMultiplyWideningLower : Polynomial multiply long (bottom)

        /// <summary>
        /// svuint64_t svpmullb_pair[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningLower(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  PolynomialMultiplyWideningUpper : Polynomial multiply long (top)

        /// <summary>
        /// svuint64_t svpmullt_pair[_u64](svuint64_t op1, svuint64_t op2)
        /// </summary>
        public static unsafe Vector<ulong> PolynomialMultiplyWideningUpper(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

    }
}


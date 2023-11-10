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
    public abstract class SveSm4 : AdvSimd
    {
        internal SveSm4() { }

        public static new bool IsSupported { get => IsSupported; }


        ///  Sm4EncryptionAndDecryption : SM4 encryption and decryption

        /// <summary>
        /// svuint32_t svsm4e[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4E Ztied1.S, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Sm4EncryptionAndDecryption(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }


        ///  Sm4KeyUpdates : SM4 key updates

        /// <summary>
        /// svuint32_t svsm4ekey[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4EKEY Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Sm4KeyUpdates(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

    }
}


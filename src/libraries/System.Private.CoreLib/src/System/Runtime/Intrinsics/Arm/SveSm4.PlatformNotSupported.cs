// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SveSm4 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    [Experimental(Experimentals.ArmSveDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    public abstract class SveSm4 : ArmBase
    {
        internal SveSm4() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM SveSm4 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }


        // SM4 encryption and decryption

        /// <summary>
        /// svuint32_t svsm4e[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4E Ztied1.S, Ztied1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> Encode(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }


        // SM4 key updates

        /// <summary>
        /// svuint32_t svsm4ekey[_u32](svuint32_t op1, svuint32_t op2)
        ///   SM4EKEY Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> KeyUpdate(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM SveSha3 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class SveSha3 : ArmBase
    {
        internal SveSha3() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM SveSha3 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }
        }


        // Bitwise rotate left by 1 and exclusive OR

        /// <summary>
        /// svint64_t svrax1[_s64](svint64_t op1, svint64_t op2)
        ///   RAX1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<long> BitwiseRotateLeftBy1AndXor(Vector<long> xor, Vector<long> rol1) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svrax1[_u64](svuint64_t op1, svuint64_t op2)
        ///   RAX1 Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static Vector<ulong> BitwiseRotateLeftBy1AndXor(Vector<ulong> xor, Vector<ulong> rol1) { throw new PlatformNotSupportedException(); }
    }
}

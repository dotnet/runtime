// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM Sm4 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Sm4 : ArmBase
    {
        internal Sm4() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM Sm4 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
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
        /// uint32x4_t vsm4eq_u32(uint32x4_t a, uint32x4_t b)
        ///   SM4E Vd.4S,Vn.4S
        /// </summary>
        public static unsafe Vector128<uint> Encode(Vector128<uint> value, Vector128<uint> roundKeys) => Encode(value, roundKeys);

        // SM4 key updates

        /// <summary>
        /// uint32x4_t vsm4ekeyq_u32(uint32x4_t a, uint32x4_t b)
        ///   SM4EKEY Vd.4S,Vn.4S,Vm.4S
        /// </summary>
        public static unsafe Vector128<uint> KeyUpdate(Vector128<uint> value, Vector128<uint> constant) => KeyUpdate(value, constant);

    }
}

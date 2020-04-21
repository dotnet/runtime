// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to the x86 base hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    internal static class X86Base
    {
        internal static bool IsSupported { get => IsSupported; }

        [Intrinsic]
        internal static class X64
        {
            internal static bool IsSupported { get => IsSupported; }

            /// <summary>
            /// unsigned char _BitScanForward64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSF reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            internal static ulong BitScanForward(ulong value) => BitScanForward(value);

            /// <summary>
            /// unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSR reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            internal static ulong BitScanReverse(ulong value) => BitScanReverse(value);
        }

        /// <summary>
        /// unsigned char _BitScanForward (unsigned __int32* index, unsigned __int32 a)
        ///   BSF reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        internal static uint BitScanForward(uint value) => BitScanForward(value);

        /// <summary>
        /// unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)
        ///   BSR reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        internal static uint BitScanReverse(uint value) => BitScanReverse(value);
    }
}

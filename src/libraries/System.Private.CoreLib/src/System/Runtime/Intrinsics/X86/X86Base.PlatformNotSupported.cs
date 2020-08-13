// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to the x86 base hardware instructions via intrinsics
    /// </summary>
    public abstract partial class X86Base
    {
        internal X86Base() { }

        public static bool IsSupported { [Intrinsic] get => false; }

        public abstract class X64
        {
            internal X64() { }

            public static bool IsSupported { [Intrinsic] get => false; }

            /// <summary>
            /// unsigned char _BitScanForward64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSF reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            /// <remarks>
            /// This method is to remain internal.
            /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanForward(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSR reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            /// <remarks>
            /// This method is to remain internal.
            /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanReverse(ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// unsigned char _BitScanForward (unsigned __int32* index, unsigned __int32 a)
        ///   BSF reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        /// <remarks>
        /// This method is to remain internal.
        /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
        /// </remarks>
        internal static uint BitScanForward(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)
        ///   BSR reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        /// <remarks>
        /// This method is to remain internal.
        /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
        /// </remarks>
        internal static uint BitScanReverse(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// void __cpuidex(int cpuInfo[4], int function_id, int subfunction_id);
        ///   CPUID
        /// </summary>
        public static (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId) { throw new PlatformNotSupportedException(); }
    }
}

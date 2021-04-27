// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to the x86 base hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    public abstract partial class X86Base
    {
        internal X86Base() { }

        public static bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public abstract class X64
        {
            internal X64() { }

            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            /// unsigned char _BitScanForward64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSF reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            /// <remarks>
            /// This method is to remain internal.
            /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanForward(ulong value) => BitScanForward(value);

            /// <summary>
            /// unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSR reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            /// <remarks>
            /// This method is to remain internal.
            /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanReverse(ulong value) => BitScanReverse(value);
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
        internal static uint BitScanForward(uint value) => BitScanForward(value);

        /// <summary>
        /// unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)
        ///   BSR reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        /// <remarks>
        /// This method is to remain internal.
        /// Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.
        /// </remarks>
        internal static uint BitScanReverse(uint value) => BitScanReverse(value);

        /// <summary>
        /// void __cpuidex(int cpuInfo[4], int function_id, int subfunction_id);
        ///   CPUID
        /// </summary>
        public static unsafe (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId)
        {
            int* cpuInfo = stackalloc int[4];
            __cpuidex(cpuInfo, functionId, subFunctionId);
            return (cpuInfo[0], cpuInfo[1], cpuInfo[2], cpuInfo[3]);
        }
    }
}

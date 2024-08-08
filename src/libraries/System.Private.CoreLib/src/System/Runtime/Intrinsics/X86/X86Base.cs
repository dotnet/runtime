// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to the x86 base hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
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
            /// Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanForward(ulong value) => BitScanForward(value);

            /// <summary>
            /// unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)
            ///   BSR reg reg/m64
            /// The above native signature does not directly correspond to the managed signature.
            /// </summary>
            /// <remarks>
            /// This method is to remain internal.
            /// Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.
            /// </remarks>
            internal static ulong BitScanReverse(ulong value) => BitScanReverse(value);

            /// <summary>
            /// unsigned __int64 _udiv128(unsigned __int64 highdividend, unsigned __int64 lowdividend, unsigned __int64 divisor, unsigned __int64* remainder)
            ///   DIV reg/m64
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (ulong Quotient, ulong Remainder) DivRem(ulong lower, ulong upper, ulong divisor) => DivRem(lower, upper, divisor);

            /// <summary>
            /// __int64 _div128(__int64 highdividend, __int64 lowdividend, __int64 divisor, __int64* remainder)
            ///   DIV reg/m64
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (long Quotient, long Remainder) DivRem(ulong lower, long upper, long divisor) => DivRem(lower, upper, divisor);
        }

        /// <summary>
        /// unsigned char _BitScanForward (unsigned __int32* index, unsigned __int32 a)
        ///   BSF reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        /// <remarks>
        /// This method is to remain internal.
        /// Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.
        /// </remarks>
        internal static uint BitScanForward(uint value) => BitScanForward(value);

        /// <summary>
        /// unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)
        ///   BSR reg reg/m32
        /// The above native signature does not directly correspond to the managed signature.
        /// </summary>
        /// <remarks>
        /// This method is to remain internal.
        /// Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.
        /// </remarks>
        internal static uint BitScanReverse(uint value) => BitScanReverse(value);

        /// <summary>
        /// void __cpuidex (int cpuInfo[4], int function_id, int subfunction_id);
        ///   CPUID
        /// </summary>
        public static unsafe (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId)
        {
            int* cpuInfo = stackalloc int[4];
            __cpuidex(cpuInfo, functionId, subFunctionId);
            return (cpuInfo[0], cpuInfo[1], cpuInfo[2], cpuInfo[3]);
        }

        /// <summary>
        /// unsigned _udiv64(unsigned __int64 dividend, unsigned divisor, unsigned* remainder)
        ///   DIV reg/m32
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (uint Quotient, uint Remainder) DivRem(uint lower, uint upper, uint divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        /// int _div64(__int64 dividend, int divisor, int* remainder)
        ///   IDIV reg/m32
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (int Quotient, int Remainder) DivRem(uint lower, int upper, int divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        ///   IDIV reg/m
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nuint Quotient, nuint Remainder) DivRem(nuint lower, nuint upper, nuint divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        ///   IDIV reg/m
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nint Quotient, nint Remainder) DivRem(nuint lower, nint upper, nint divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        /// void _mm_pause (void);
        ///   PAUSE
        /// </summary>
        public static void Pause() => Pause();
    }
}

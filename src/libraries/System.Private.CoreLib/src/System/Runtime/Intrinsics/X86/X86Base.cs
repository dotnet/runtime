// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 base hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract partial class X86Base
    {
        internal X86Base() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the x86 base hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public abstract class X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>unsigned char _BitScanForward64 (unsigned __int32* index, unsigned __int64 a)</para>
            ///   <para>  BSF reg reg/m64</para>
            ///   <para>The above native signature does not directly correspond to the managed signature.</para>
            /// </summary>
            /// <remarks>
            ///   <para>This method is to remain internal.</para>
            ///   <para>Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.</para>
            /// </remarks>
            internal static ulong BitScanForward(ulong value) => BitScanForward(value);

            /// <summary>
            ///   <para>unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)</para>
            ///   <para>  BSR reg reg/m64</para>
            ///   <para>The above native signature does not directly correspond to the managed signature.</para>
            /// </summary>
            /// <remarks>
            ///   <para>This method is to remain internal.</para>
            ///   <para>Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.</para>
            /// </remarks>
            internal static ulong BitScanReverse(ulong value) => BitScanReverse(value);

            /// <summary>
            ///   <para>unsigned __int64 _udiv128(unsigned __int64 highdividend, unsigned __int64 lowdividend, unsigned __int64 divisor, unsigned __int64* remainder)</para>
            ///   <para>  DIV reg/m64</para>
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (ulong Quotient, ulong Remainder) DivRem(ulong lower, ulong upper, ulong divisor) => DivRem(lower, upper, divisor);

            /// <summary>
            ///   <para>__int64 _div128(__int64 highdividend, __int64 lowdividend, __int64 divisor, __int64* remainder)</para>
            ///   <para>  DIV reg/m64</para>
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (long Quotient, long Remainder) DivRem(ulong lower, long upper, long divisor) => DivRem(lower, upper, divisor);
        }

        /// <summary>
        ///   <para>unsigned char _BitScanForward (unsigned __int32* index, unsigned __int32 a)</para>
        ///   <para>  BSF reg reg/m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        /// <remarks>
        ///   <para>This method is to remain internal.</para>
        ///   <para>Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.</para>
        /// </remarks>
        internal static uint BitScanForward(uint value) => BitScanForward(value);

        /// <summary>
        ///   <para>unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)</para>
        ///   <para>  BSR reg reg/m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        /// <remarks>
        ///   <para>This method is to remain internal.</para>
        ///   <para>Its functionality is exposed in the public <see cref="Numerics.BitOperations" /> class.</para>
        /// </remarks>
        internal static uint BitScanReverse(uint value) => BitScanReverse(value);

        /// <summary>
        ///   <para>void __cpuidex (int cpuInfo[4], int function_id, int subfunction_id);</para>
        ///   <para>  CPUID</para>
        /// </summary>
        public static unsafe (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId)
        {
            int* cpuInfo = stackalloc int[4];
            __cpuidex(cpuInfo, functionId, subFunctionId);
            return (cpuInfo[0], cpuInfo[1], cpuInfo[2], cpuInfo[3]);
        }

        /// <summary>
        ///   <para>unsigned _udiv64(unsigned __int64 dividend, unsigned divisor, unsigned* remainder)</para>
        ///   <para>  DIV reg/m32</para>
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (uint Quotient, uint Remainder) DivRem(uint lower, uint upper, uint divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        ///   <para>int _div64(__int64 dividend, int divisor, int* remainder)</para>
        ///   <para>  IDIV reg/m32</para>
        /// </summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (int Quotient, int Remainder) DivRem(uint lower, int upper, int divisor) => DivRem(lower, upper, divisor);

        /// <summary>  IDIV reg/m</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nuint Quotient, nuint Remainder) DivRem(nuint lower, nuint upper, nuint divisor) => DivRem(lower, upper, divisor);

        /// <summary>  IDIV reg/m</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nint Quotient, nint Remainder) DivRem(nuint lower, nint upper, nint divisor) => DivRem(lower, upper, divisor);

        /// <summary>
        ///   <para>void _mm_pause (void);</para>
        ///   <para>  PAUSE</para>
        /// </summary>
        public static void Pause() => Pause();
    }
}

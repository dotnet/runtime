// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to the x86 base hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract partial class X86Base
    {
        internal X86Base() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static bool IsSupported { [Intrinsic] get => false; }

        /// <summary>Provides access to the x86 base hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public abstract class X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static bool IsSupported { [Intrinsic] get => false; }

            /// <summary>
            ///   <para>unsigned char _BitScanForward64 (unsigned __int32* index, unsigned __int64 a)</para>
            ///   <para>  BSF reg reg/m64</para>
            ///   <para>The above native signature does not directly correspond to the managed signature.</para>
            /// </summary>
            /// <remarks>
            ///   <para>This method is to remain internal.</para>
            ///   <para>Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.</para>
            /// </remarks>
            internal static ulong BitScanForward(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned char _BitScanReverse64 (unsigned __int32* index, unsigned __int64 a)</para>
            ///   <para>  BSR reg reg/m64</para>
            ///   <para>The above native signature does not directly correspond to the managed signature.</para>
            /// </summary>
            /// <remarks>
            ///   <para>This method is to remain internal.</para>
            ///   <para>Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.</para>
            /// </remarks>
            internal static ulong BitScanReverse(ulong value) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned __int64 _udiv128(unsigned __int64 highdividend, unsigned __int64 lowdividend, unsigned __int64 divisor, unsigned __int64* remainder)</para>
            ///   <para>  DIV reg/m64</para>
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (ulong Quotient, ulong Remainder) DivRem(ulong lower, ulong upper, ulong divisor) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>__int64 _div128(__int64 highdividend, __int64 lowdividend, __int64 divisor, __int64* remainder)</para>
            ///   <para>  DIV reg/m64</para>
            /// </summary>
            [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
            public static (long Quotient, long Remainder) DivRem(ulong lower, long upper, long divisor) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>unsigned _umul128(unsigned __int64 Multiplier, unsigned __int64  Multiplicand, unsigned __int64 * HighProduct)</para>
            ///   <para>  MUL reg/m64</para>
            ///   <para>  MULX reg reg reg/m64 (if BMI2 is supported)</para>
            /// </summary>
            /// <remarks>
            ///   <para>Its functionality is exposed by the public <see cref="Math.BigMul(ulong, ulong, out ulong)" />.</para>
            ///   <para>Can emit either mul or mulx depending on hardware</para>
            /// </remarks>
            internal static (ulong Lower, ulong Upper) BigMul(ulong left, ulong right) { throw new PlatformNotSupportedException(); }

            /// <summary>
            ///   <para>  IMUL reg/m64</para>
            /// </summary>
            /// <remarks>
            ///   <para>Its functionality is exposed in the public <see cref="Math" /> class.</para>
            /// </remarks>
            internal static (long Lower, long Upper) BigMul(long left, long right) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   <para>unsigned char _BitScanForward (unsigned __int32* index, unsigned __int32 a)</para>
        ///   <para>  BSF reg reg/m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        /// <remarks>
        ///   <para>This method is to remain internal.</para>
        ///   <para>Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.</para>
        /// </remarks>
        internal static uint BitScanForward(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>unsigned char _BitScanReverse (unsigned __int32* index, unsigned __int32 a)</para>
        ///   <para>  BSR reg reg/m32</para>
        ///   <para>The above native signature does not directly correspond to the managed signature.</para>
        /// </summary>
        /// <remarks>
        ///   <para>This method is to remain internal.</para>
        ///   <para>Its functionality is exposed in the public <see cref="System.Numerics.BitOperations" /> class.</para>
        /// </remarks>
        internal static uint BitScanReverse(uint value) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void __cpuidex (int cpuInfo[4], int function_id, int subfunction_id);</para>
        ///   <para>  CPUID</para>
        /// </summary>
        public static (int Eax, int Ebx, int Ecx, int Edx) CpuId(int functionId, int subFunctionId) { throw new PlatformNotSupportedException(); }

        /// <summary>  DIV reg/m32</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (uint Quotient, uint Remainder) DivRem(uint lower, uint upper, uint divisor) { throw new PlatformNotSupportedException(); }

        /// <summary>  IDIV reg/m32</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (int Quotient, int Remainder) DivRem(uint lower, int upper, int divisor) { throw new PlatformNotSupportedException(); }

        /// <summary>  IDIV reg/m</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nuint Quotient, nuint Remainder) DivRem(nuint lower, nuint upper, nuint divisor) { throw new PlatformNotSupportedException(); }

        /// <summary>  IDIV reg/m</summary>
        [Experimental(Experimentals.X86BaseDivRemDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
        public static (nint Quotient, nint Remainder) DivRem(nuint lower, nint upper, nint divisor) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  MUL reg/m32</para>
        ///   <para>  MULX reg reg reg/m32 (if BMI2 is supported)</para>
        /// </summary>
        internal static (uint Lower, uint Upper) BigMul(uint left, uint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  IMUL reg/m32</para>
        /// </summary>
        internal static (int Lower, int Upper) BigMul(int left, int right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>  MUL reg/m</para>
        ///   <para>  MULX reg reg reg/m (if BMI2 is supported)</para>
        /// </summary>
        internal static (nuint Lower, nuint Upper) BigMul(nuint left, nuint right) { throw new PlatformNotSupportedException(); }

        /// <summary>  IMUL reg/m</summary>
        internal static (nint Lower, nint Upper) BigMul(nint left, nint right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        ///   <para>void _mm_pause (void);</para>
        ///   <para>  PAUSE</para>
        /// </summary>
        public static void Pause() { throw new PlatformNotSupportedException(); }
    }
}

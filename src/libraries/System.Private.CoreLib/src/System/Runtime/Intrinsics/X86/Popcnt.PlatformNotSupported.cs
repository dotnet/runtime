// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>Provides access to X86 POPCNT hardware instructions via intrinsics.</summary>
    [CLSCompliant(false)]
    public abstract class Popcnt : Sse42
    {
        internal Popcnt() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { [Intrinsic] get { return false; } }

        /// <summary>Provides access to the x86 POPCNT hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        public new abstract class X64 : Sse42.X64
        {
            internal X64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            ///   <para>__int64 _mm_popcnt_u64 (unsigned __int64 a)</para>
            ///   <para>  POPCNT r64, r/m64</para>
            ///   <para>This intrinsic is only available on 64-bit processes</para>
            /// </summary>
            public static ulong PopCount(ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        ///   <para>int _mm_popcnt_u32 (unsigned int a)</para>
        ///   <para>  POPCNT r32, r/m32</para>
        /// </summary>
        public static uint PopCount(uint value) { throw new PlatformNotSupportedException(); }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>This class provides access to X86 POPCNT hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Popcnt : Sse42
    {
        internal Popcnt() { }

        /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>This class provides access to the x86 POPCNT hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class X64 : Sse42.X64
        {
            internal X64() { }

            /// <summary>Gets <c>true</c> if the APIs in this class are supported; otherwise, <c>false</c> which indicates they will throw <see cref="PlatformNotSupportedException" />.</summary>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            /// __int64 _mm_popcnt_u64 (unsigned __int64 a)
            ///   POPCNT r64, r/m64
            /// This intrinsic is only available on 64-bit processes
            /// </summary>
            public static ulong PopCount(ulong value) => PopCount(value);
        }

        /// <summary>
        /// int _mm_popcnt_u32 (unsigned int a)
        ///   POPCNT r32, r/m32
        /// </summary>
        public static uint PopCount(uint value) => PopCount(value);
    }
}

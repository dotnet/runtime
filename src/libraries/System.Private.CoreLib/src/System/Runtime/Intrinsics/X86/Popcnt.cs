// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel POPCNT hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Popcnt : Sse42
    {
        internal Popcnt() { }

        public static new bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class X64 : Sse42.X64
        {
            internal X64() { }

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

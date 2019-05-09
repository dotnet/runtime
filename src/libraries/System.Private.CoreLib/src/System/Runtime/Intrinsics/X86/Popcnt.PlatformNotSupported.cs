// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel POPCNT hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public abstract class Popcnt : Sse42
    {
        internal Popcnt() { }

        public new static bool IsSupported { [Intrinsic] get { return false; } }

        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }

            public new static bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// __int64 _mm_popcnt_u64 (unsigned __int64 a)
            ///   POPCNT reg64, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static ulong PopCount(ulong value) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// int _mm_popcnt_u32 (unsigned int a)
        ///   POPCNT reg, reg/m32
        /// </summary>
        public static uint PopCount(uint value) { throw new PlatformNotSupportedException(); }
    }
}

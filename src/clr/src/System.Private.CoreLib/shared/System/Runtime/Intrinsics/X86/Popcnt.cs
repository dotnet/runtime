// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

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

        public new static bool IsSupported { get => IsSupported; }

        [Intrinsic]
        public new abstract class X64 : Sse41.X64
        {
            internal X64() { }
            public new static bool IsSupported { get => IsSupported; }
            /// <summary>
            /// __int64 _mm_popcnt_u64 (unsigned __int64 a)
            ///   POPCNT reg64, reg/m64
            /// This intrinisc is only available on 64-bit processes
            /// </summary>
            public static ulong PopCount(ulong value) => PopCount(value);
        }

        /// <summary>
        /// int _mm_popcnt_u32 (unsigned int a)
        ///   POPCNT reg, reg/m32
        /// </summary>
        public static uint PopCount(uint value) => PopCount(value);
    }
}

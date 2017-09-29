// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Runtime.Intrinsics.X86
{
    /// <summary>
    /// This class provides access to Intel POPCNT hardware instructions via intrinsics
    /// </summary>
    [CLSCompliant(false)]
    public static class Popcnt
    {
        public static bool IsSupported { get => IsSupported; }

        /// <summary>
        /// int _mm_popcnt_u32 (unsigned int a)
        /// </summary>
        public static int PopCount(uint value) => PopCount(value);
        /// <summary>
        /// __int64 _mm_popcnt_u64 (unsigned __int64 a)
        /// </summary>
        public static long PopCount(ulong value) => PopCount(value);
    }
}

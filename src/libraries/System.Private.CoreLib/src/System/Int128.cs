// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>Represents a 128-bit signed integer.</summary>
    [Intrinsic]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Int128
    {
#if BIGENDIAN
        private readonly ulong _upper;
        private readonly ulong _lower;
#else
        private readonly ulong _lower;
        private readonly ulong _upper;
#endif

        /// <summary>Initializes a new instance of the <see cref="Int128" /> struct.</summary>
        /// <param name="upper">The upper 64-bits of the 128-bit value.</param>
        /// <param name="lower">The lower 64-bits of the 128-bit value.</param>
        [CLSCompliant(false)]
        public Int128(ulong upper, ulong lower)
        {
            _lower = lower;
            _upper = upper;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Internal
{
    /// <summary>A class for common padding constants and eventually routines.</summary>
    internal static class PaddingHelpers
    {
        /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
#if TARGET_ARM64
        internal const int CACHE_LINE_SIZE = 128;
#else
        internal const int CACHE_LINE_SIZE = 64;
#endif
    }

    /// <summary>Padding structure used to minimize false sharing</summary>
    [StructLayout(LayoutKind.Explicit, Size = PaddingHelpers.CACHE_LINE_SIZE - sizeof(int))]
    internal struct PaddingFor32
    {
    }
}

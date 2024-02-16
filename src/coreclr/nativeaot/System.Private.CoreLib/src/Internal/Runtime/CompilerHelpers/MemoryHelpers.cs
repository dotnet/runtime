// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to implement memcpy and memset intrinsics with null checks.
    /// </summary>
    internal static class MemoryHelpers
    {
        private static unsafe void MemSet(byte* dest, int value, nuint size)
        {
            if (size > 0)
            {
                _ = *dest;
                RuntimeImports.memset(dest, value, size);
            }
        }

        private static unsafe void MemCopy(byte* dest, byte* src, nuint size)
        {
            if (size > 0)
            {
                _ = *dest;
                _ = *src;
                RuntimeImports.memcpy(dest, src, size);
            }
        }
    }
}

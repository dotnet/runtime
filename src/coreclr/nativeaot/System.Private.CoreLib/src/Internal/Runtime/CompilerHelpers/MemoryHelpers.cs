// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Runtime.CompilerServices;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// These methods are used to implement memcpy and memset intrinsics with null checks.
    /// </summary>
    internal static class MemoryHelpers
    {
        private static unsafe void MemSet(ref byte dest, byte value, nuint size)
        {
            if (size > 0)
            {
                // Implicit nullcheck
                _ = Unsafe.ReadUnaligned<byte>(ref dest);
                SpanHelpers.Fill(ref dest, size, value);
            }
        }

        private static unsafe void MemCopy(ref byte dest, ref byte src, nuint size)
        {
            if (size > 0)
            {
                // Implicit nullchecks
                _ = Unsafe.ReadUnaligned<byte>(ref dest);
                _ = Unsafe.ReadUnaligned<byte>(ref src);
                Buffer.Memmove(ref dest, ref src, size);
            }
        }
    }
}

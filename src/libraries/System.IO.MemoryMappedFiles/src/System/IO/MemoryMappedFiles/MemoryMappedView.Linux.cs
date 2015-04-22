// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO.MemoryMappedFiles
{
    internal partial class MemoryMappedView
    {
        /// <summary>Attempt to prevent the specified pages from being copied into forked processes.</summary>
        /// <param name="addr">The starting address.</param>
        /// <param name="length">The length.</param>
        static partial void DisableForkingIfPossible(IntPtr addr, IntPtr length)
        {
            if ((long)length > 0)
            {
                int adviseResult = Interop.libc.madvise(addr, length, Interop.libc.MemoryMappedAdvice.MADV_DONTFORK);
                Debug.Assert(adviseResult == 0); // In release, ignore failures from advise; it's just a hint, anyway.
            }
        }
    }
}
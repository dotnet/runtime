// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public static class Interlocked
    {
        [Intrinsic]
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
        }

        [Intrinsic]
        public static void MemoryBarrier()
        {
            RuntimeImports.MemoryBarrier();
        }
    }
}

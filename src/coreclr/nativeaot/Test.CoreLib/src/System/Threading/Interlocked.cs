// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    public static class Interlocked
    {
        [Intrinsic]
        public static IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand)
        {
#if TARGET_64BIT
            return (IntPtr)Interlocked.CompareExchange(ref Unsafe.As<IntPtr, long>(ref location1), (long)value, (long)comparand);
#else
            return (IntPtr)Interlocked.CompareExchange(ref Unsafe.As<IntPtr, int>(ref location1), (int)value, (int)comparand);
#endif
        }

        [Intrinsic]
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
        }

        [Intrinsic]
        public static long CompareExchange(ref long location1, long value, long comparand)
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

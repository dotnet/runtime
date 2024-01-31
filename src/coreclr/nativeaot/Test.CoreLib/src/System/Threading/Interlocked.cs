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
            return (IntPtr)CompareExchange(ref Unsafe.As<IntPtr, long>(ref location1), (long)value, (long)comparand);
#else
            return (IntPtr)CompareExchange(ref Unsafe.As<IntPtr, int>(ref location1), (int)value, (int)comparand);
#endif
        }

        [Intrinsic]
        public static byte CompareExchange(ref byte location1, byte value, byte comparand)
        {
#if TARGET_X86 || TARGET_AMD64 || TARGET_ARM64 || TARGET_ARM
            return CompareExchange(ref location1, value, comparand);
#else
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
#endif
        }

        [Intrinsic]
        public static short CompareExchange(ref short location1, short value, short comparand)
        {
#if TARGET_X86 || TARGET_AMD64 || TARGET_ARM64 || TARGET_ARM
            return CompareExchange(ref location1, value, comparand);
#else
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
#endif
        }

        [Intrinsic]
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
#if TARGET_X86 || TARGET_AMD64 || TARGET_ARM64 || TARGET_ARM || TARGET_RISCV64
            return CompareExchange(ref location1, value, comparand);
#else
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
#endif
        }

        [Intrinsic]
        public static long CompareExchange(ref long location1, long value, long comparand)
        {
#if TARGET_AMD64 || TARGET_ARM64 || TARGET_RISCV64
            return CompareExchange(ref location1, value, comparand);
#else
            return RuntimeImports.InterlockedCompareExchange(ref location1, value, comparand);
#endif
        }

        [Intrinsic]
        public static void MemoryBarrier() => MemoryBarrier();
    }
}

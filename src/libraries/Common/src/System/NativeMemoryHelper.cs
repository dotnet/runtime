// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
    internal static class NativeMemoryHelper
    {
        public static unsafe IntPtr Alloc(int size)
        {
#if NET6_0_OR_GREATER
            return (nint)NativeMemory.Alloc((uint)size);
#else
            return Marshal.AllocHGlobal(size);
#endif
        }

        public static unsafe IntPtr AllocStringUnicode(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            var byteCount = (s.Length + 1) * 2;

            // Overflow checking
            if (byteCount < s.Length)
            {
                [StackTraceHidden]
                static void ThrowArgumentOutOfRangeException(string argument)
                {
                    throw new ArgumentOutOfRangeException(argument);
                }

                ThrowArgumentOutOfRangeException(nameof(s));
            }

#if NET6_0_OR_GREATER
            char* memory = (char*)NativeMemory.Alloc((uint)byteCount);
#else
            char* memory = (char*)(nint)Marshal.AllocHGlobal(byteCount);
#endif

            s.CopyTo(new Span<char>(memory, s.Length));
            memory[s.Length] = '\0';
            return (nint)memory;
        }


        public static unsafe void Free(IntPtr pointer)
        {
#if NET6_0_OR_GREATER
            NativeMemory.Free((void*)(nint)pointer);
#else
            Marshal.FreeHGlobal(pointer);
#endif
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    internal static unsafe class NativeMemoryHelper
    {
        public static IntPtr Alloc(int byteCount)
        {
#if NET6_0_OR_GREATER
            return (nint)NativeMemory.Alloc((uint)byteCount);
#else
            return Marshal.AllocHGlobal(byteCount);
#endif
        }

        public static IntPtr Realloc(IntPtr pointer, int byteCount)
        {
#if NET6_0_OR_GREATER
            return (nint)NativeMemory.Realloc((void*)(nint)pointer, (uint)byteCount);
#else
            return Marshal.ReAllocHGlobal(pointer, (nint)byteCount);
#endif
        }

        public static IntPtr AllocStringUnicode(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            var byteCount = (s.Length + 1) * 2;

            // Overflow checking
            if (byteCount < s.Length)
            {
                ThrowArgumentOutOfRangeException(nameof(s));
            }

#if NET6_0_OR_GREATER
            char* memory = (char*)NativeMemory.Alloc((uint)byteCount);
            s.CopyTo(new Span<char>(memory, s.Length));
#else
            char* memory = (char*)(nint)Marshal.AllocHGlobal(byteCount);
            // Avoid pulling in System.Memory for netstandard2.0 targets.
            fixed (char* str = s)
            {
                Buffer.MemoryCopy(str, memory, byteCount, s.Length * 2);
            }
#endif

            memory[s.Length] = '\0';
            return (nint)memory;
        }

        public static IntPtr AllocStringAnsi(string? s)
        {
            if (s is null)
            {
                return IntPtr.Zero;
            }

            long longByteCount = (s.Length + 1) * (long)Marshal.SystemMaxDBCSCharSize;
            int byteCount = (int)longByteCount;

            // Overflow checking
            if (byteCount != longByteCount)
            {
                ThrowArgumentOutOfRangeException(nameof(s));
            }

#if NET6_0_OR_GREATER
            byte* memory = (byte*)NativeMemory.Alloc((uint)byteCount);
#else
            byte* memory = (byte*)(nint)Marshal.AllocHGlobal(byteCount);
#endif

            fixed (char* str = s)
            {
                int convertedBytes = Encoding.UTF8.GetBytes(str, s.Length, memory, byteCount);
                memory[convertedBytes] = 0;
            }

            return (nint)memory;
        }


        public static void Free(IntPtr pointer)
        {
#if NET6_0_OR_GREATER
            NativeMemory.Free((void*)(nint)pointer);
#else
            Marshal.FreeHGlobal(pointer);
#endif
        }


#if NET6_0_OR_GREATER
        [StackTraceHidden]
#endif
        [DoesNotReturn]
        private static void ThrowArgumentOutOfRangeException(string argument)
        {
            throw new ArgumentOutOfRangeException(argument);
        }
    }
}

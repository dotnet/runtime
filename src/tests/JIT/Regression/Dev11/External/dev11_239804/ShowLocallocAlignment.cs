// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Check that localloc return properly aligned memory.
// The JIT guarantees that the localloc return value is at least as aligned as the platform stack alignment, which is:
//   x86 Windows: 4 bytes
//   x86 Linux: 16 bytes
//   x64: 16 bytes
//   arm32: 8 bytes
//   arm64: 16 bytes

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ShowLocallocAlignment
{
    public struct Struct1 { public int F1; }
    public struct Struct2 { public int F1; public int F2; }

    internal static class App
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CallTarget1(int arg1, int arg2, int arg3, int arg4, int arg5) { return; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CallTarget2(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6) { return; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void* SnapLocallocBufferAddress1(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6)
        {
            App.CallTarget1(arg1, arg2, arg3, arg4, arg5);
            double* buffer = stackalloc double[16];
            return (void*)buffer;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private unsafe static void* SnapLocallocBufferAddress2(int arg1, int arg2, int arg3, int arg4, int arg5, int arg6)
        {
            App.CallTarget2(arg1, arg2, arg3, arg4, arg5, arg6);
            double* buffer = stackalloc double[16];
            return (void*)buffer;
        }

        private unsafe static int RunAlignmentCheckScenario()
        {
            UInt64 address1;
            UInt64 address2;
            UInt64 required_alignment;
            bool fAligned1;
            bool fAligned2;
            void* ptr1;
            void* ptr2;

            required_alignment = 16; // Default to the biggest alignment required
            if (OperatingSystem.IsWindows() && (RuntimeInformation.ProcessArchitecture == Architecture.X86))
            {
                required_alignment = 4;
            }
            else if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            {
                required_alignment = 8;
            }

            ptr1 = App.SnapLocallocBufferAddress1(1, 2, 3, 4, 5, 6);
            ptr2 = App.SnapLocallocBufferAddress2(1, 2, 3, 4, 5, 6);

            address1 = unchecked((UInt64)(new IntPtr(ptr1)).ToInt64());
            address2 = unchecked((UInt64)(new IntPtr(ptr2)).ToInt64());

            fAligned1 = ((address1 % required_alignment) == 0);
            fAligned2 = ((address2 % required_alignment) == 0);

            Console.Write(
                "\r\n" +
                "Address1: {0} ({1:x16})\r\n" +
                "Address2: {2} ({3:x16})\r\n" +
                "Required alignment: {4}\r\n" +
                "\r\n",

                (fAligned1 ? "Aligned" : "Misaligned"), address1,
                (fAligned2 ? "Aligned" : "Misaligned"), address2,
                required_alignment
            );

            if (fAligned1 && fAligned2)
            {
                Console.Write("Test passed.\r\n");
                return 100;
            }
            else
            {
                Console.Write("Test failed.\r\n");
            }
            return 101;
        }

        private static int Main()
        {
            return App.RunAlignmentCheckScenario();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// This test exhibits a case where the JIT was passing a 3-byte struct
// by loading 4-bytes, which is not always safe.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_19288
{
    static int returnVal = 100;

    [StructLayout(LayoutKind.Sequential)]
    public struct PixelData
    {
        public byte blue;
        public byte green;
        public byte red;
    }

    public unsafe class MyClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe void CheckPointer(PixelData p)
        {
            if (&p == null)
            {
                Console.WriteLine("FAIL");
                returnVal = -1;
            }
        }

        unsafe int DoStuff()
        {
            IntPtr pBase = Marshal.AllocCoTaskMem(0x40000 * 3);
            PixelData* foo = (PixelData*)(pBase + 511 * (512 * sizeof(PixelData)) + 511 * sizeof(PixelData));

            CheckPointer(*foo);

            return 0;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            try
            {
                new MyClass().DoStuff();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed with exception: " + e.Message);
                returnVal = -1;
            }
            return returnVal;
        }
    }
}

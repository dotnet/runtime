// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System;
using Xunit;

[StructLayout(LayoutKind.Explicit, Size = 153)]
internal struct A
{
    [FieldOffset(0)]
    internal bool i;
};

public class Test
{
    [Fact]
    public static unsafe int TestEntryPoint()
    {
        int i = sizeof(A);
        int j = Marshal.SizeOf(typeof(A));

        if (i == 153 && j == 153)
        {
            Console.WriteLine("PASS");
            return 100;  
        }
        else
        {
            Console.WriteLine("FAIL: sizeof and Marshal.SizeOf should have both returned 153.");
            Console.WriteLine("ACTUAL: sizeof(A) = " + i + ", Marshal.SizeOf(A) = " + j); 
            return 101;
        }
    }
}

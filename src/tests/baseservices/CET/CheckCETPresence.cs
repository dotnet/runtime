// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

public static class Program
{
    [DllImport("cet_check.dll")]
    private static extern long ReadShadowStackPointer();
    
    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("Checking whether codeflow enforcement technology (CET) is active");
        long ssp = ReadShadowStackPointer();
        Console.WriteLine("Shadow stack pointer: 0x{0:x16}", ssp);
        // Non-zero shadow stack pointer value confirms that CET is active on the runtime processor.
        return ssp != 0 ? 100 : 101;
    }
}

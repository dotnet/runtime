// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_101046
{
    [Fact]
    public static void TestEntryPoint()
    {
        ushort value = unchecked((ushort)-123);
        Test(ref value);
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Test(ref ushort p)
    {
        int result = Runtime101046Native.ReturnExtendedShort((short)p);
        Assert.Equal(-123, result);
    }
}

internal static unsafe class Runtime101046Native
{
    [DllImport(nameof(Runtime101046Native))]
    public static extern int ReturnExtendedShort(short s);
}

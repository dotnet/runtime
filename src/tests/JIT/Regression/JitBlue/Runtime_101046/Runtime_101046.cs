// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Reference source for the .il file

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class Runtime_101046
{
    [Fact]
    public static int TestEntryPoint()
    {
        ushort value = unchecked((ushort)-123);
        return Test(ref value) == -123 ? 100 : 101;
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test(ref ushort p)
    {
        return Runtime101046Native.ReturnExtendedShort((short)p); // this (short) cast is removed in the .il version
    }
}

internal static unsafe class Runtime101046Native
{
    [DllImport(nameof(Runtime101046Native))]
    public static extern int ReturnExtendedShort(short s);
}

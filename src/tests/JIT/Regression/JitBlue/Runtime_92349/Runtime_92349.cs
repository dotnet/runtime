// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public static class Runtime_92349
{
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    unsafe static void Test(byte* pValue)
    {
        *pValue = (byte)Sse2.ConvertToInt32(Vector128.Create(-10, 0, 0, 0));
    }

    [Fact]
    unsafe static void EntryPoint()
    {
        ulong value = 0;
        Test((byte*)Unsafe.AsPointer(ref value));
        Assert.True(value == 246);
    }
}
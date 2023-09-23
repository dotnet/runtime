// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
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
    public unsafe static void EntryPoint()
    {
        if (Sse2.IsSupported)
        {
            ulong value = 0;
            Test((byte*)Unsafe.AsPointer(ref value));
            Assert.True(value == 246);
        }
    }
}

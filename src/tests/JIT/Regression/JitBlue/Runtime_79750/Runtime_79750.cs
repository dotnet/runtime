// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_79750
{
    [Fact]
    public static int TestEntryPoint()
    {
        byte dest = 0;
        byte source = 100;
        uint size = GetSize();
        Unsafe.CopyBlock(ref dest, ref GetAddr(ref source), size);

        return dest;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref byte GetAddr(ref byte a) => ref a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint GetSize() => 1;
}
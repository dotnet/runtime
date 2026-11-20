// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_137798
{
    [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl), SuppressGCTransition]
    private static extern int abs(int value);

    [Fact]
    [PlatformSpecific(TestPlatforms.Windows)]
    public static void TestEntryPoint()
    {
        Assert.Equal(42, Test(-42));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(int x) => Consume(abs(x));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Consume(int value) => value;
}

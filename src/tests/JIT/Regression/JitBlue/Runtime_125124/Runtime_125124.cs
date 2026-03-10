// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// Tests that the JIT does not reorder the null check before evaluating
// the right-hand side of a store to a field with a large offset.
[StructLayout(LayoutKind.Sequential)]
public class Runtime_125124
{
    private static bool s_barCalled;

    private LargeStruct _large;
    public int Field;

    [InlineArray(0x10000)]
    private struct LargeStruct
    {
        public byte B;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            Get().Field = Bar();
        }
        catch (NullReferenceException)
        {
        }

        Assert.True(s_barCalled);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bar()
    {
        s_barCalled = true;
        return 123;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Runtime_125124 Get() => null;
}

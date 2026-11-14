// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Two tests of the same field or array length fold to one test with one load. CSE normally turns the
// repeated load into a local first; it is disabled here so the fold itself has to recognize the load.
public class RangeFoldSharedLoads
{
    private int _field;
    private byte _state;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int FieldPair(RangeFoldSharedLoads o)
    {
        // ARM64: ldr
        // ARM64-NOT: ldr
        // X64-NOT: cmp dword ptr
        if (o._field != 5 && o._field != 6)
            return 1;
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ByteFieldPair(RangeFoldSharedLoads o)
    {
        // ARM64: ldrb
        // ARM64-NOT: ldrb
        // X64-NOT: cmp byte ptr
        if (o._state == 1 || o._state == 2)
            return 1;
        return 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LengthRange(int[] a)
    {
        // ARM64: ldr
        // ARM64-NOT: ldr
        // X64-NOT: cmp dword ptr
        if (a.Length >= 4 && a.Length <= 8)
            return 1;
        return 2;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        foreach (int value in new[] { 4, 5, 6, 7 })
        {
            Assert.Equal(value != 5 && value != 6 ? 1 : 2, FieldPair(new RangeFoldSharedLoads { _field = value }));
        }
        foreach (byte state in new byte[] { 0, 1, 2, 3 })
        {
            Assert.Equal(state == 1 || state == 2 ? 1 : 2, ByteFieldPair(new RangeFoldSharedLoads { _state = state }));
        }
        foreach (int length in new[] { 3, 4, 8, 9 })
        {
            Assert.Equal(length >= 4 && length <= 8 ? 1 : 2, LengthRange(new int[length]));
        }
    }
}

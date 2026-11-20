// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using Xunit;

public class Runtime_136305
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector4 v = new Vector4(10, 1, 12, 13);
        Assert.Equal(1f, TestVector4(ref v, 3));

        Vector128<int>[] arr = { Vector128.Create(10, 1, 12, 13) };
        Assert.Equal(1, TestSpan(arr, 3));
    }

    // GetElement must read the vector before the index expression modifies its memory.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float TestVector4(ref Vector4 v, float newValue) =>
        v.AsVector128().GetElement((int)Interlocked.Exchange(ref v.Y, newValue));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TestSpan(Vector128<int>[] arr, int newValue)
    {
        Span<int> span = MemoryMarshal.Cast<Vector128<int>, int>(arr);
        return arr[0].GetElement(Interlocked.Exchange(ref span[1], newValue));
    }
}

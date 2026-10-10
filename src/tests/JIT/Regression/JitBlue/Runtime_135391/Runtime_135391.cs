// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

// AVX-512 vblendm*/vpblendm* with a memory operand were sized as if they
// carried the imm8 of the AVX vblendv* form, overestimating them by a byte.
public class Runtime_135391
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector512<int>[] arr = new Vector512<int>[2];
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = Vector512.Create(i);
        }

        Vector512<int> b = Vector512.Create(-1);

        Assert.Equal(Vector512.Create(-99), TestIndir(arr, b, 100));
        Assert.Equal(Vector512.Create(-93), TestConstant(arr, b, 100));
        Assert.Equal(Vector512.Create(-98), TestLocal(arr, b, 100));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(ref Vector512<int> v) => v += Vector512<int>.One;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<int> TestIndir(Vector512<int>[] arr, Vector512<int> b, int n)
    {
        Vector512<int> acc = Vector512.ConditionalSelect(Vector512.GreaterThan(arr[0], b), arr[1], b);
        for (int i = 0; i < n; i++)
        {
            acc += b;
        }
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<int> TestConstant(Vector512<int>[] arr, Vector512<int> b, int n)
    {
        Vector512<int> acc = Vector512.ConditionalSelect(Vector512.GreaterThan(arr[0], b), Vector512.Create(7), b);
        for (int i = 0; i < n; i++)
        {
            acc += b;
        }
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<int> TestLocal(Vector512<int>[] arr, Vector512<int> b, int n)
    {
        Vector512<int> t = arr[1];
        Consume(ref t);
        Vector512<int> acc = Vector512.ConditionalSelect(Vector512.GreaterThan(arr[0], b), t, b);
        for (int i = 0; i < n; i++)
        {
            acc += b;
        }
        return acc;
    }
}

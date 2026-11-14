// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The AVX10.2 saturating conversions (vcvtt{ss,sd}2{s,us}is) write a GPR but did
// not end the GC liveness of that register, so an integer result could be
// reported as a live object reference.
public class Runtime_135387
{
    private static object s_o = new object();

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(123, TestInt32(new int[1], 123.0f));
        Assert.Equal(123u, TestUInt32(new int[1], 123.0));
        Assert.Equal(123L, TestInt64(new int[1], 123.0));
        Assert.Equal(123UL, TestUInt64(new int[1], 123.0f));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Get() => s_o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int TestInt32(int[] arr, float f)
    {
        object o = Get();
        GC.KeepAlive(o);
        int r = (int)f;
        int sum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            sum += arr[i];
        }
        return sum + r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static uint TestUInt32(int[] arr, double d)
    {
        object o = Get();
        GC.KeepAlive(o);
        uint r = (uint)d;
        uint sum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            sum += (uint)arr[i];
        }
        return sum + r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long TestInt64(int[] arr, double d)
    {
        object o = Get();
        GC.KeepAlive(o);
        long r = (long)d;
        long sum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            sum += arr[i];
        }
        return sum + r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong TestUInt64(int[] arr, float f)
    {
        object o = Get();
        GC.KeepAlive(o);
        ulong r = (ulong)f;
        ulong sum = 0;
        for (int i = 0; i < arr.Length; i++)
        {
            sum += (ulong)arr[i];
        }
        return sum + r;
    }
}

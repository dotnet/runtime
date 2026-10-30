// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Unit test for null check assertion.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Sample3
{
    private sealed class Box
    {
        public int Value = 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool ArrayRange(int[] array, int count)
    {
        // X64: cmp {{.*}}dword ptr
        if (array == null)
            return false;
        return !(0u > (uint)array.Length || (uint)count > (uint)array.Length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedArrayLength(int[] array)
    {
        // ARM64: ldr{{s?}}b wzr,
        return 0u > (uint)array.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedStringLength(string text) => 0u > (uint)text.Length;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool UnusedField(Box box) => 0u > (uint)box.Value;

    [Fact]
    public static void UnusedValues()
    {
        foreach (int count in new[] { -1, 0, 8, 9, int.MaxValue })
            Assert.Equal(count >= 0 && count <= 8, ArrayRange(new int[8], count));
        Assert.False(ArrayRange(null, 0));
        Assert.False(UnusedArrayLength(new int[1]));
        Assert.False(UnusedStringLength("x"));
        Assert.False(UnusedField(new Box()));
        Assert.Throws<NullReferenceException>(() => UnusedArrayLength(null));
        Assert.Throws<NullReferenceException>(() => UnusedStringLength(null));
        Assert.Throws<NullReferenceException>(() => UnusedField(null));
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void func(object o)
    {
        if (o == null)
            throw new Exception();
        o.GetType();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            func(new Object());
            Console.WriteLine("Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Returns
{
    [Fact]
    public static void TestEntryPoint()
    {
        Returns(new C()).Wait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task Returns(C c)
    {
        for (int i = 0; i < 20000; i++)
        {
            S<long> val = await ReturnsStruct();

            AssertEqual(42, val.A);
            AssertEqual(4242, val.B);
            AssertEqual(424242, val.C);
            AssertEqual(42424242, val.D);

            c.Val = default;
            c.Val = await ReturnsStruct();

            AssertEqual(42, c.Val.A);
            AssertEqual(4242, c.Val.B);
            AssertEqual(424242, c.Val.C);
            AssertEqual(42424242, c.Val.D);

            S<string> strings = await ReturnsStructGC();
            AssertEqual("A", strings.A);
            AssertEqual("B", strings.B);
            AssertEqual("C", strings.C);
            AssertEqual("D", strings.D);

            S<byte> bytes = await ReturnsBytes();
            AssertEqual(4, bytes.A);
            AssertEqual(40, bytes.B);
            AssertEqual(42, bytes.C);
            AssertEqual(45, bytes.D);

            string str = await ReturnsString();
            AssertEqual("a string!", str);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEqual<T>(T expected, T actual)
    {
        Assert.Equal(expected, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<S<long>> ReturnsStruct()
    {
        await Task.Yield();
        return new S<long> { A = 42, B = 4242, C = 424242, D = 42424242 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<S<string>> ReturnsStructGC()
    {
        await Task.Yield();
        return new S<string> { A = "A", B = "B", C = "C", D = "D" };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<S<byte>> ReturnsBytes()
    {
        await Task.Yield();
        return new S<byte> { A = 4, B = 40, C = 42, D = 45 };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<string> ReturnsString()
    {
        await Task.Yield();
        return "a string!";
    }

    private struct S<T> { public T A, B, C, D; }

    private class C { public S<long> Val; }
}

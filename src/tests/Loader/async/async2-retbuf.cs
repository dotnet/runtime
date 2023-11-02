// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Async2Retbuf
{
    [Fact]
    public static void TestEntryPoint()
    {
        Retbuf(new C()).Wait();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async2 void Retbuf(C c)
    {
        for (int i = 0; i < 20000; i++)
        {
            S val = await ReturnsStruct();

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
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void AssertEqual(long expected, long actual)
    {
        Assert.Equal(expected, actual);
    }

    private static async2 S ReturnsStruct()
    {
        await Task.Yield();
        return new S { A = 42, B = 4242, C = 424242, D = 42424242 };
    }

    private struct S { public long A, B, C, D; }

    private class C { public S Val; }
}

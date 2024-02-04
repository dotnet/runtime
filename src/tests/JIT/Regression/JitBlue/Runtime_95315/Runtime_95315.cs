// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_95315
{
    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 200; j++)
            {
                Assert.Throws<IndexOutOfRangeException>(() => Bar(new int[10], new C()));
            }

            Thread.Sleep(100);
        }

        Assert.Throws<IndexOutOfRangeException>(() => Bar(new int[1], new C()));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bar(int[] arr, I iface)
    {
        if (arr == null)
        {
            return;
        }

        iface.Foo();

        int i = 10000000;
        do
        {
            Console.WriteLine(arr[i]);
            i++;
        } while (i < arr.Length);
    }
}

internal interface I
{
    void Foo();
}

internal class C : I
{
    public void Foo()
    {
    }
}
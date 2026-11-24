// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

public class Runtime_138142
{
    static void Call<T>(int index, T state, Action<int, T> action) => action(index, state);

    static void StoreReference()
    {
        object[] array = new object[1];
        Call(0, array, static (i, a) => a[i] = a);
    }

    static void StoreNumber()
    {
        int[] array = new int[1];
        Call(0, array, static (i, a) => a[i] = i);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int round = 0; round < 20; round++)
        {
            for (int i = 0; i < 2000; i++)
                StoreReference();

            Thread.Sleep(20);
        }

        for (int round = 0; round < 100; round++)
        {
            for (int i = 0; i < 2000; i++)
                StoreNumber();

            Thread.Sleep(5);
        }
    }
}

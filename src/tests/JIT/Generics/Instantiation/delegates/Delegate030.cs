// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

internal delegate T GenDelegate<T>(T p1, out T p2);

internal struct Foo<T>
{
    static public T Function(T i, out T j)
    {
        j = i;
        return i;
    }
}

public class Test_Delegate030
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i, j;
        GenDelegate<int> MyDelegate = new GenDelegate<int>(Foo<int>.Function);
        i = MyDelegate(10, out j);

        if ((i != 10) || (j != 10))
        {
            Console.WriteLine("Failed Sync Invocation");
            return 1;
        }

        Console.WriteLine("Test Passes");
        return 100;
    }
}


// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

internal delegate T GenDelegate<T>(T p1, out T p2);

internal class Foo
{
    public int Function(int i, out int j)
    {
        j = i;
        return i;
    }
}

internal class Test
{
    public static int Main()
    {
        int i, j;
        Foo inst = new Foo();
        GenDelegate<int> MyDelegate = new GenDelegate<int>(inst.Function);
        i = MyDelegate(10, out j);

        if ((i != 10) || (j != 10))
        {
            Console.WriteLine("Failed Sync Invokation");
            return 1;
        }

        Console.WriteLine("Test Passes");
        return 100;
    }
}


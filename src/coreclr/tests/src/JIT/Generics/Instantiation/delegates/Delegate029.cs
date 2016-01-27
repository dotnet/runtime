// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

internal delegate T GenDelegate<T>(T p1, out T p2);

internal struct Foo
{
    static public int Function(int i, out int j)
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
        GenDelegate<int> MyDelegate = new GenDelegate<int>(Foo.Function);
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


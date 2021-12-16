// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

struct MyStruct
{
    int x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    int IncrementAndReturn()
    {
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        Volatile.Write(ref x, Volatile.Read(ref x) + 1);
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test()
    {
        for (int i = 0; i < 10_000_000; i++)
        {
            if (new MyStruct().IncrementAndReturn() != 5)
                throw new InvalidOperationException("oops");
        }
    }
}

class Program
{
    static int Main()
    {
        Console.WriteLine("Running...");
        Parallel.For(0, 200, _ => MyStruct.Test());
        Console.WriteLine("Done");
        return 100;
    }
}

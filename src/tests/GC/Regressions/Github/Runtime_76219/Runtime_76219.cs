// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public class Runtime_76219
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public static int Main()
    {
        for (int i = 0; i < 100; i++)
        {
            string alloc = i.ToString();

            Test();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
        return 100;
    }

    [MethodImpl(MethodImplOptions.Synchronized | MethodImplOptions.NoInlining)]
    static void Test()
    {
        CallConsume("hello");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CallConsume(string str)
    {
        lock (str)
        {
            Consume(str);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(string str)
    {
    }
}

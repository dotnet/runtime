// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Factorial

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Test
{
    public static int Main(string[] args)
    {
        Test app = new Test();
        app.Run(args);
        return (100);
    }

    [Fact]
    public static void TestEntryPoint() {
        Test app = new Test();
        app.Run(new string[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int Run(string[] args)
    {
        long i;

        if (args.Length == 0)
        {
            i = 17;
        }
        else if (args.Length == 1)
        {
            i = Convert.ToInt64(args[0]);
        }
        else
        {
            usage();
            return (1);
        }
        Console.Out.WriteLine("Factorial of " + i.ToString() + " is " + Fact(i).ToString());
        return (0);
    }

    private long Fact(long i)
    {
        if (i <= 1L)
            return (i);
        return (i * Fact(i - 1L));
    }

    private void usage()
    {
        Console.Out.WriteLine("usage: Fact [number]");
    }
}

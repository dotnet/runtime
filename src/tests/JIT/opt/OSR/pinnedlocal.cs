// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

// Run under COMPlus_GCStress=3

class PinnedLocal
{
    static int F(char c)
    {
        return (int) c;
    }

    public static unsafe int Main()
    {
        string ss = "goodbye, world\n";
        string s = "hello, world\n";
        int r = 0;
        fixed(char* p = s)
        {
            for (int i = 0; i < 100_000; i++)
            {
                r += F(p[i % s.Length]);

                if ((i % 10_000) == 0)
                {
                    GC.Collect(2);
                    ss = new String('a', 100);
                }
            }

            Console.WriteLine($"r is {r}");
            return r - 9000061 + 100;
        }
    }
}

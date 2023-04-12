// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class foo
{
    static double nan = 0.0 / 0.0;
    static double d = 1.1;
    [Fact]
    public static int TestEntryPoint()
    {

#pragma warning disable 1718
        if (!(nan != d))
#pragma warning restore
        {
            Console.Write("Test # 1 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (!(nan != nan))
#pragma warning restore
        {
            Console.Write("Test # 2 failed.");
            return 1;
        }
        if (nan == d)
        {
            Console.Write("Test # 3 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (nan == nan)
#pragma warning restore
        {
            Console.Write("Test # 4 failed.");
            return 1;
        }
        if (nan > d)
        {
            Console.Write("Test # 5 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (nan > nan)
#pragma warning restore
        {
            Console.Write("Test # 6 failed.");
            return 1;
        }
        if (nan >= d)
        {
            Console.Write("Test # 7 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (nan >= nan)
#pragma warning restore
        {
            Console.Write("Test # 8 failed.");
            return 1;
        }
        if (nan <= d)
        {
            Console.Write("Test # 9 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (nan <= nan)
#pragma warning restore
        {
            Console.Write("Test # 10 failed.");
            return 1;
        }
        if (nan < d)
        {
            Console.Write("Test # 11 failed.");
            return 1;
        }
#pragma warning disable 1718
        if (nan < nan)
#pragma warning restore
        {
            Console.Write("Test # 12 failed.");
            return 1;
        }
        Console.Write("Tests passed.");
        return 100;
    }
}

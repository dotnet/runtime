// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

internal class MainApp
{
    private static int s_a = 0;

    public static bool Foo_Inline(bool b)
    {
        Console.WriteLine(b ? "t" : "f");
        return b;
    }

    public static int Main()
    {
        try
        {
            bool bresult;

            bresult = Foo_Inline(s_a == 0);
            if (bresult)
            {
                return 100;
            }
            else
            {
                return 101;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}



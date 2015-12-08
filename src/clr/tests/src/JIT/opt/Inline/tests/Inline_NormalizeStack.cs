// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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



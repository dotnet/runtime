// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{

    static int c = 1;

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Bar_Inline(int v)
    {
        Console.WriteLine("Entering Bar_Inline: v=");
        int ret = v * 2;
        return ret;
    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Foo_Inline(int v)
    {

        int ret = Bar_Inline(v + 1) * 4;

        Console.WriteLine(ret);
        return ret;
    }

    public static int Main()
    {
        try
        {
            Foo_Inline(c);
            Console.WriteLine(Foo_Inline(c));
            Console.WriteLine(Foo_Inline(c) + 1 + Foo_Inline(c));
            Console.WriteLine(Foo_Inline(c) + 1 + Foo_Inline(c) + 2 + Foo_Inline(c));
            Console.WriteLine("Test Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Test failed: " + e.Message);
            return 101;
        }

    }

}



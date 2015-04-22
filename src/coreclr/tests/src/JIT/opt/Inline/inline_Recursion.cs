// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{

    static int s1 = 10;
    static int s2 = 5;

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int B(int v)
    {
        int ret = 0;

        if (v != 0)
        {
            ret = A(v - 1);
        }
        Console.WriteLine(ret);
        return ret;
    }


    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int A(int v)
    {
        int ret = 0;

        if (v != 0)
        {
            ret = B(v - 1);
        }
        Console.WriteLine(ret);
        return ret;
    }

    public static int Main()
    {
        try
        {
            A(s1);

            A(10);

            B(s2);
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }

}



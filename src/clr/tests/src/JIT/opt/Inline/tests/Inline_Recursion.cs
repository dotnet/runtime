// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

internal class MainApp
{
    private static int s_s1 = 10;
    private static int s_s2 = 5;

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
            A(s_s1);

            A(10);

            B(s_s2);
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}



// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{
    private static int s_s = 1;

    public static int Method_WithMultipleReturn_Inline()
    {
        Console.WriteLine("In Method_WithMultipleReturn_Inline");
        Console.WriteLine(s_s);
        if (s_s != 1)
        {
            return 200;
        }
        else
        {
            return 100;
        }
    }

    public static int Method_WithOneReturn_Inline()
    {
        Console.WriteLine("In Method_WithOneReturn_Inline");
        Console.WriteLine(s_s);
        if (s_s == 1)
        {
            return 100;
        }
        else
        {
            return 200;
        }
    }

    public static int Method_ConstantProp_Inline(int i)
    {
        Console.WriteLine("In Method_ConstantProp_Inline");
        int v;
        if (i == 1)
        {
            v = 200;
        }
        else
        {
            v = 100;
        }
        Console.WriteLine(v);
        return v;
    }


    public static int SmallFunc_Inline()
    {
        return 111;
    }

    public static int Method_QMark_Inline(int i)
    {
        Console.WriteLine(s_s);
        Console.WriteLine("In Method_QMark");

        int v = (i == 1) ? SmallFunc_Inline() : 222;
        Console.WriteLine(v);
        return v;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            int returncode = 0;
            Console.WriteLine(s_s);
            if ((111 == Method_QMark_Inline(s_s)) && (100 == Method_WithMultipleReturn_Inline()) && (100 == Method_WithOneReturn_Inline()) && (100 == Method_ConstantProp_Inline(2)))
            {
                returncode = 100;
            }

            else
            {
                returncode = 101;
            }
            return returncode;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}



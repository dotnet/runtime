// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp
{
    private static int s_c = 1;

    public static int TestNewInline_D_Inline(int v)
    {
        int ret = v * 4;

        Console.WriteLine(ret);
        return ret;
    }



    public static int TestNewInline_C_Inline(int v)
    {
        int ret = TestNewInline_D_Inline(v + 3) * 3;

        Console.WriteLine(ret);
        return ret;
    }


    public static int TestNewInline_B_Inline(int v)
    {
        int ret = TestNewInline_C_Inline(v + 2) * 2;
        Console.WriteLine(ret);

        return ret;
    }

    public static int TestNewInline_A_Inline(int v)
    {
        int ret = TestNewInline_B_Inline(v + 1) * 1;
        Console.WriteLine(ret);
        return ret;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            int result;
            result = TestNewInline_A_Inline(s_c);
            if (result == 168)
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



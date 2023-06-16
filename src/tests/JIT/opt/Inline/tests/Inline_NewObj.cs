// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class MainApp_Inline
{
    private int _v;

    public MainApp_Inline(int i)
    {
        switch (i)
        {
            case 1:
                _v = 100;
                break;

            case 2:
                _v = 200;
                break;

            case 3:
                _v = 300;
                break;

            case 4:
                _v = 400;
                break;

            default:
                _v = 999;
                break;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine(new MainApp_Inline(800)._v);
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine(new MainApp_Inline(i)._v);
            }
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}



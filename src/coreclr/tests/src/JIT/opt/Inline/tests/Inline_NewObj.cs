// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;

internal class MainApp_Inline
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

    public static int Main()
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



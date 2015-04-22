// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// !!!!!!!!!!!!!!! set complus_jitinlinesize=600 to see MainApp.ctor get inlined !!!!!!!!!!!

using System;
using System.Runtime.CompilerServices;

class MainApp_Inline
{

    int v;

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public MainApp_Inline(int i)
    {
        switch (i)
        {
            case 1:
                v = 100;
                break;

            case 2:
                v = 200;
                break;

            case 3:
                v = 300;
                break;

            case 4:
                v = 400;
                break;

            default:
                v = 999;
                break;

        }
    }

    public static int Main()
    {
        // Should see the constant folding here.
        Console.WriteLine(new MainApp_Inline(800).v);
        try
        {

            for (int i = 1; i <= 5; i++)
            {
                Console.WriteLine(new MainApp_Inline(i).v);
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



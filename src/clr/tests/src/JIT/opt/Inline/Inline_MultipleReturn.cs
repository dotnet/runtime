// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{

    static int s = 1;

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Method_WithMultipleReturn_Inline()
    {
        Console.WriteLine("In Method_WithMultipleReturn_Inline");
        Console.WriteLine(s);
        if (s != 1)
        {

            return 200;
        }
        else
        {

            return 100;
        }
    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Method_WithOneReturn_Inline()
    {
        Console.WriteLine("In Method_WithOneReturn_Inline");
        Console.WriteLine(s);
        if (s == 1)
        {

            return 100;
        }
        else
        {

            return 200;
        }

    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
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


    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int SmallFunc_Inline()
    {
        return 111;
    }

    //[MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Method_QMark_Inline(int i)
    {
        Console.WriteLine(s);
        Console.WriteLine("In Method_QMark");

        int v = (i == 1) ? SmallFunc_Inline() : 222;
        Console.WriteLine(v);
        return v;
    }

    public static int Main()
    {
        try
        {
            int returncode = 0;
            Console.WriteLine(s);
            if ((111 == Method_QMark_Inline(s)) && (100 == Method_WithMultipleReturn_Inline()) && (100 == Method_WithOneReturn_Inline()) && (100 == Method_ConstantProp_Inline(2)))
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



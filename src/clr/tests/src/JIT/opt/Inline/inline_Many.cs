// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;

class MainApp
{

    static int c = 1;

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int TestNewInline_D_Inline(int v)
    {

        int ret = v * 4;

        Console.WriteLine(ret);
        return ret;
    }



    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int TestNewInline_C_Inline(int v)
    {

        int ret = TestNewInline_D_Inline(v + 3) * 3;

        Console.WriteLine(ret);
        return ret;
    }


    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int TestNewInline_B_Inline(int v)
    {

        int ret = TestNewInline_C_Inline(v + 2) * 2;
        Console.WriteLine(ret);

        return ret;
    }

    // [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int TestNewInline_A_Inline(int v)
    {

        int ret = TestNewInline_B_Inline(v + 1) * 1;
        Console.WriteLine(ret);
        return ret;
    }

    public static int Main()
    {
        try
        {
            int result;
            result = TestNewInline_A_Inline(c);
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



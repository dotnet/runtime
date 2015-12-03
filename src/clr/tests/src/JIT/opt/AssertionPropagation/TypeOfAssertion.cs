// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//Unit test for typeof assertion.

using System;

internal class Sample10
{
    private static int s_s = 1;

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int AddOne(int i)
    {
        return i + 1;
    }

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static int func(object o)
    {
        if (o.GetType() == typeof(int))
        {
            return AddOne((int)o);
        }
        return 0;
    }

    private static int Main()
    {
        try
        {
            int result = (func(s_s));
            if (result == 2)
            {
                Console.WriteLine("Result:" + result);
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Result:" + result);
                Console.WriteLine("Failed");
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

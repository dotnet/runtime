// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Runtime.CompilerServices;
class Program
{
    static int Main()
    {
        try
        {
            Test(null);

            Console.WriteLine("!!!!!!!!!!!!!!!!!  TEST PASSED !!!!!!!!!!!!!!!!!!!!");
            return 100;
        }
        catch (NullReferenceException)
        {
            Console.WriteLine("!!!!!!!!!!!!!!!!!  TEST FAILED !!!!!!!!!!!!!!!!!!!!");
            return 101;
        }
        catch
        {
            Console.WriteLine("!!!!!!!!!!!!!!!!!  TEST FAILED !!!!!!!!!!!!!!!!!!!!");
            Console.WriteLine("Did not even get a NullReferenceException, need to know why!");
            return 666;
        }

    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test(string x)
    {
        for (int i = 0; i < 10; ++i)
        {
            if (String.IsNullOrEmpty(x))
            { }
        }
    }
}

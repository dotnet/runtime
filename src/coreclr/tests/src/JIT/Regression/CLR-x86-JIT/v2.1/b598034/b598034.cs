// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
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

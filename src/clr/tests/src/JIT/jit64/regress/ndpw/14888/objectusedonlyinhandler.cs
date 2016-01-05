// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

internal class Test
{
    public static int Main()
    {
        int exitCode = 1;
        String teststring = new String('a', 5);
        try
        {
            Console.WriteLine("Starting Test");
            throw new Exception();
        }
        catch
        {
            Thread.Sleep(5000);
            Console.WriteLine("Invoking GC");
            GC.Collect();
            Thread.Sleep(5000);
            GC.WaitForPendingFinalizers();
            if (teststring.Equals("aaaaa"))
            {
                Console.WriteLine("Pass");
                exitCode = 100;
            }
            else
            {
                Console.WriteLine("Fail");
            }
            Console.WriteLine(teststring);
        }
        return exitCode;
    }
}

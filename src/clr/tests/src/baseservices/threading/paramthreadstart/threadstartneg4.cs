// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

class ThreadStartInt
{
    public static int Main()
    {
        ThreadStartInt tsi = new ThreadStartInt();
        return tsi.Run();
    }

    private int Run()
    {
        int iRet = -1;
        try
        {
            Thread t = new Thread((ParameterizedThreadStart)null);
            t.Start(12345);
            Console.WriteLine("No exception thrown!");
        }
        catch(ArgumentNullException)
        {
            // Expected
            iRet = 100;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Unexpected exception thrown: " + ex.ToString());
        }
        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}
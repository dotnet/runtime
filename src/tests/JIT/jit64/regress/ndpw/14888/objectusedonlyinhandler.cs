// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

public class Test_objectusedonlyinhandler
{
    [Fact]
    public static int TestEntryPoint()
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

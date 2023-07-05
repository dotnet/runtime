// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* 

Expected behavior:
64 bit = False
Hello from Main!
Hello from Crash!

Actual behavior:
64 bit = False
Hello from Main!
Process is terminated due to StackOverflowException.
*/

using System;
using System.IO;
using Xunit;

namespace Test_volatileFromFinally
{
public class Test
{
    private static volatile bool s_someField = false;

    private static int s_result = 101;

    private static void Crash()
    {
        try
        {
            Console.WriteLine("Hello from Crash!");
            s_result = 100;
        }

        finally
        {
            var unused = new bool[] { s_someField };
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        //Console.WriteLine("64 bit = {0}", Environment.Is64BitProcess);

        Console.WriteLine("Hello from Main!");

        Crash();
        return s_result;
    }
}

}

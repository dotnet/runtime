// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

internal class Test
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

    private static int Main(string[] args)
    {
        //Console.WriteLine("64 bit = {0}", Environment.Is64BitProcess);

        Console.WriteLine("Hello from Main!");

        Crash();
        return s_result;
    }
}


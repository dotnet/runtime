// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Security;
using Xunit;

public class Directory_AnsiChars
{
    [Fact]
    public static void runTest()
    {
        bool bTestPassed = false;
        try
        {
            String strPath = Directory.GetCurrentDirectory();
            String s = "..\u2044*";
            String[] d = Directory.GetFiles(".", s);
            if (d.Length != 0)
            {
                //print out all the files.
                for (int i = 0; i < d.Length; i++)
                    Console.WriteLine(d[i]);
                bTestPassed = false;
            }
            else
            {
                Console.WriteLine(@"Skipped test. Make sure you have a parent directory (not root) with somes files before running this test");
                bTestPassed = true;
            }
        }
        catch (Exception e)
        {
            bTestPassed = false;
            Console.WriteLine("unexpected exception occured... Exception message:" + e.ToString());
        }

        if (!bTestPassed)
        {
            Console.WriteLine("Test FAILED");
        }

        Assert.True(bTestPassed);
    }
}
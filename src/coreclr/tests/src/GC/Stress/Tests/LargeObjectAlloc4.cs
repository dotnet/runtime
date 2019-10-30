// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This test should need to allocated a maximum of 20 MB and so, should pass without
// OOM Exception. On RTM as the largeobjects were never committed, this test would 
// fail after a few loops.

using System;
public class Test
{
    public static int Main()
    {
        Int32 basesize;
        Int32[] largeobjarr;
        int loop = 0;

        TestLibrary.Logging.WriteLine("Test should pass with ExitCode 100");

        while (loop < 50)
        {
            TestLibrary.Logging.WriteLine("loop: {0}", loop);
            basesize = 4096;
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    //TestLibrary.Logging.WriteLine("In loop {0}, Allocating array of {1} bytes\n",i,basesize*4);
                    largeobjarr = new Int32[basesize];
                    basesize += 4096;
                }
            }
            catch (Exception e)
            {
                TestLibrary.Logging.WriteLine("Exception caught: {0}", e);
                return 1;
            }
            loop++;
        }

        TestLibrary.Logging.WriteLine("Test Passed");
        return 100;
    }
}


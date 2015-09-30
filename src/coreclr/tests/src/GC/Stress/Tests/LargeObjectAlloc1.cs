// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Allocate arrays from 20KB to 25MB, 200 times
// If memory is low, after every loop, the large objects should be collected
// and committed from the LargeObjectHeap


using System;
public class Test
{
    public static int Main(string[] args)
    {
        int loop = 0;
        byte[] junk;

        TestLibrary.Logging.WriteLine("Test should return ExitCode 100\n");
        while (loop <= 200)
        {
            TestLibrary.Logging.Write(string.Format("LOOP: {0}", loop));
            for (int size = 20000; size <= 5242880 * 5; size += 1024 * 1024)
            {
                try
                {
                    junk = new byte[size];
                    //TestLibrary.Logging.WriteLine("Allocated Size: {0}",size);
                    TestLibrary.Logging.Write(".");
                }
                catch (Exception e)
                {
                    TestLibrary.Logging.WriteLine("Failure to allocate " + size + " at loop " + loop);
                    TestLibrary.Logging.WriteLine("Caught Exception: {0}", e);
                    return 1;
                }
            }
            loop++;
            TestLibrary.Logging.WriteLine("\n");
        }
        TestLibrary.Logging.WriteLine("Test Passed");
        return 100;
    }
}
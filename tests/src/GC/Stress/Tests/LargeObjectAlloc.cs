// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Bug#94878 Problem with the Large Object Allocator
// Repro test from MStanton 

using System;
using System.Threading;
using System.Security;

internal class Mainy
{
    public static void DoWork()
    {
        int k = 0;
        while (k < 3)
        {
            TestLibrary.Logging.WriteLine("{0}: Restarting run {1}", Thread.CurrentThread.Name, k);
            int[] largeArray = new int[1000000];
            for (int i = 0; i <= 100; i++)
            {
                int[] saveArray = largeArray;
                largeArray = new int[largeArray.Length + 100000];
                saveArray = null;
                //TestLibrary.Logging.WriteLine("{0} at size {1}",Thread.CurrentThread.Name,largeArray.Length.ToString());
            }
            k++;
        }
    }

    public static int Main(String[] args)
    {
        long Threads = 1;

        if (args.Length > 1)
        {
            TestLibrary.Logging.WriteLine("usage: LargeObjectAlloc <number of threads>");
            return 1;
        }
        else if (args.Length == 1)
        {
            Threads = Int64.Parse(args[0]);
        }

        TestLibrary.Logging.WriteLine("LargeObjectAlloc started with {0} threads. Control-C to exit", Threads.ToString());

        Thread myThread = null;
        for (long i = 0; i < Threads; i++)
        {
            myThread = new Thread(new ThreadStart(Mainy.DoWork));
            myThread.Name = i.ToString();
            myThread.Start();
        }

        TestLibrary.Logging.WriteLine("All threads started");
        myThread.Join();


        TestLibrary.Logging.WriteLine("Test Passed");
        return 100;
    }
}


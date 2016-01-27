// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Allocate 120MB of live large objects (2MB at a time) on n threads,
// where n is the number of processors on the machine.
//This test must be run with server GC.

using System;
using System.Threading;

internal class MyThread
{
    private int _index;
    private int _allocPerThreadMB;

    public MyThread(int i, int allocPerThreadMB)
    {
        _index = i;
        _allocPerThreadMB = allocPerThreadMB;
    }

    public void DoWork()
    {
        byte[][] largeArray = new byte[_allocPerThreadMB / 2][];
        for (int i = 0; i < _allocPerThreadMB / 2; i++)
        {
            largeArray[i] = new byte[2 * 1024 * 1024];  // 2 MB
            largeArray[i][i + 100] = Convert.ToByte(Math.Min(i, byte.MaxValue));
        }
        int sum = 0;
        for (int i = 0; i < _allocPerThreadMB / 2; i++)
            sum += Convert.ToInt32(largeArray[i][i + 100]);
        Console.WriteLine("Thread {0}: finished", _index);
    }
}

internal class B115557
{
    private static int s_allocPerThreadMB = 120;

    public static int Main(String[] args)
    {
        //check if total allocation size is not too much for x86
        //Allocate at most 1700MB on x86 to avoid the risk of getting OOM.
        int ProcCount = Environment.ProcessorCount;
        //if (!Environment.Is64BitProcess && ((AllocPerThreadMB * ProcCount) > 1700))
        {
            s_allocPerThreadMB = 1700 / ProcCount;
        }

        Console.WriteLine("Allocating {0}MB per thread...", s_allocPerThreadMB);

        MyThread t;
        Thread[] threads = new Thread[ProcCount];

        Console.WriteLine("Starting {0} threads...", threads.Length);

        for (int i = 0; i < threads.Length; i++)
        {
            t = new MyThread(i, s_allocPerThreadMB);
            threads[i] = new Thread(t.DoWork);
            threads[i].Start();
        }

        //Wait for tasks to finish
        foreach (Thread _thread in threads)
            _thread.Join();

        Thread.Sleep(100);
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}

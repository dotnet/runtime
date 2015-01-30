// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Allocate 120MB of live large objects (2MB at a time) on n threads,
// where n is the number of processors on the machine.
//This test must be run with server GC.
using System;
using System.Threading;

class MyThread
{
    private int index;
    private int AllocPerThreadMB;

    public MyThread(int i, int allocPerThreadMB)
    {
        index = i;
        AllocPerThreadMB = allocPerThreadMB;
    }

    public void DoWork()
    {
        byte[][] largeArray = new byte[AllocPerThreadMB/2][];
        for (int i = 0; i < AllocPerThreadMB/2; i++)
        {
            largeArray[i] = new byte[2* 1024* 1024];  // 2 MB
            largeArray[i][i+100] = Convert.ToByte(i);
        }
        int sum = 0;
        for (int i=0;i<AllocPerThreadMB/2;i++)
            sum += Convert.ToInt32(largeArray[i][i+100]);
        Console.WriteLine("Thread {0}: finished", index);
    }
}

class B115557
{
    static int AllocPerThreadMB = 120;

    public static int Main(String[] args)
    {
        //check if total allocation size is not too much for x86
        //Allocate at most 1700MB on x86 to avoid the risk of getting OOM.
        int ProcCount = Environment.ProcessorCount;
        //if (!Environment.Is64BitProcess && ((AllocPerThreadMB * ProcCount) > 1700))
        {
                AllocPerThreadMB = 1700 / ProcCount;
        }

        Console.WriteLine("Allocating {0}MB per thread...", AllocPerThreadMB);

        MyThread t;
        Thread [] threads = new Thread[ProcCount];

        Console.WriteLine("Starting {0} threads...", threads.Length);

        for(int i = 0; i<threads.Length; i++)
        {
            t = new MyThread(i, AllocPerThreadMB);
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

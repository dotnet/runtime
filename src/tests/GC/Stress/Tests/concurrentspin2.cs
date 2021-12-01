// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.Threading;

internal class PriorityTest
{
    private byte[][] _old;
    private byte[][] _med;
    private Random _rand;

    private int _oldDataSize;
    private int _medDataSize;
    private int _iterCount;
    private int _meanAllocSize;
    private int _medTime;
    private int _youngTime;


    public PriorityTest(int oldDataSize, int medDataSize,
                        int iterCount, int meanAllocSize,
                        int medTime, int youngTime)
    {
        _rand = new Random(314159);
        _oldDataSize = oldDataSize;
        _medDataSize = medDataSize;
        _iterCount = iterCount;
        _meanAllocSize = meanAllocSize;
        _medTime = medTime;
        _youngTime = youngTime;
    }

    // creates initial arrays
    private void AllocTest(int oldDataSize, int medDataSize, int meanAllocSize)
    {
        _old = new byte[oldDataSize][];
        _med = new byte[medDataSize][];

        for (int i = 0; i < _old.Length; i++)
        {
            _old[i] = new byte[meanAllocSize];
        }

        for (int i = 0; i < _med.Length; i++)
        {
            _med[i] = new byte[meanAllocSize];
        }
    }

    // churns data in the heap by replacing byte arrays with new ones of random length
    // this should induce concurrent GCs
    private void SteadyState(int oldDataSize, int medDataSize,
                        int iterCount, int meanAllocSize,
                        int medTime, int youngTime)
    {
        for (int i = 0; i < iterCount; i++)
        {
            byte[] newarray = new byte[meanAllocSize];

            if ((i % medTime) == 0)
            {
                _old[_rand.Next(0, _old.Length)] = newarray;
            }
            if ((i % youngTime) == 0)
            {
                _med[_rand.Next(0, _med.Length)] = newarray;
            }
            //if (((i % 5000) == 0) && (Thread.CurrentThread.Priority != ThreadPriority.Lowest))
            //{
            //    Thread.Sleep(200);
            //}
        }
    }

    // method that runs the test
    public void RunTest()
    {
        for (int iteration = 0; iteration < _iterCount; iteration++)
        {
            AllocTest(_oldDataSize, _medDataSize, _meanAllocSize);

            SteadyState(_oldDataSize, _medDataSize,
                _iterCount, _meanAllocSize,
                _medTime, _youngTime);

            if (((iteration + 1) % 20) == 0)
                Console.WriteLine("Thread: {1} Finished iteration {0}", iteration, System.Threading.Thread.CurrentThread.Name);
        }
    }
}


internal class ConcurrentRepro
{
    public static void Usage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("\t<num iterations> <num threads>");
    }

    public static int[] ParseArgs(string[] args)
    {
        int[] parameters = new int[2];

        // set defaults
        parameters[0] = 100;
        parameters[1] = 4;

        if (args.Length == 0)
        {
            //use defaults
            Console.WriteLine("Using defaults: 100 iterations, 4 threads");
            return parameters;
        }
        if (args.Length == parameters.Length)
        {
            for (int i = 0; i < args.Length; i++)
            {
                int j = 0;
                if (!int.TryParse(args[i], out j))
                {
                    Usage();
                    return null;
                }
                parameters[i] = j;
            }

            return parameters;
        }

        // incorrect number of arguments        
        Usage();
        return null;
    }


    public static int Main(string[] args)
    {
        // parse arguments
        int[] parameters = ParseArgs(args);
        if (parameters == null)
        {
            return 0;
        }

        // set process affinity to 1 to repro bug easier
        //Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)1;


        PriorityTest priorityTest = new PriorityTest(1000000, 5000, parameters[0], 17, 30, 3);
        ThreadStart startDelegate = new ThreadStart(priorityTest.RunTest);

        // create threads
        Thread[] threads = new Thread[parameters[1]];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(startDelegate);
            threads[i].IsBackground = true;
            threads[i].Name = String.Format("Thread{0}", i);
            //if (i % 2 == 0)
            //{
            //    threads[i].Priority = ThreadPriority.Lowest;
            //}
            threads[i].Start();
        }

        // wait for threads to complete
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i].Join();
        }

        return 100;
    }
}



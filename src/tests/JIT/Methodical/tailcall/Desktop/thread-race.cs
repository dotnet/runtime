// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

internal class Repro
{
    private static volatile bool s_threadsCompleted;
    private static volatile Mutex s_myMutex;
    private static volatile int[] s_threadSum;
    private const int FibSeriesMax = 35;
    private const int FibSeriesMin = 20;

    public static int Main()
    {
        // Compute the expected value for a single thread
        int expectedSingle = 0;
        for (int i = FibSeriesMin; i <= FibSeriesMax; i++)
        {
            expectedSingle += fib(0, i);
        }

        ThreadStart ts = new ThreadStart(FibThread);
        Thread t1 = new Thread(ts);
        Thread t2 = new Thread(ts);
        Thread t3 = new Thread(ts);
        long threadValue;

        s_threadsCompleted = false;
        s_threadSum = new int[3];
        s_threadSum[0] = 0;
        s_threadSum[1] = 0;
        s_threadSum[2] = 0;
        s_myMutex = new Mutex();

        t1.Start();
        t2.Start();
        t3.Start();

        while (!s_threadsCompleted)
        {
            GC.Collect();
        }

        t1.Join();
        t2.Join();
        t3.Join();

        threadValue = (s_threadSum[0] + s_threadSum[1] + s_threadSum[2]);

        if (((long)expectedSingle * 3) != threadValue)
        {
            Console.WriteLine("FALSE: {0} != {1}", ((long)expectedSingle * 3), threadValue);
            return 0;
        }
        else
        {
            Console.WriteLine("PASS");
            return 100;
        }
    }

    public static void FibThread()
    {
        int sum = 0;
        const int length = FibSeriesMax;

        for (int i = FibSeriesMin; i <= length; i++)
        {
            sum += fib(0, i);
            Console.WriteLine("" + i + ": " + sum);
        }

        s_threadsCompleted = true;

        s_myMutex.WaitOne();
        if (0 == s_threadSum[0]) s_threadSum[0] = sum;
        if (0 == s_threadSum[1]) s_threadSum[1] = sum;
        if (0 == s_threadSum[2]) s_threadSum[2] = sum;
        s_myMutex.ReleaseMutex();
    }

    public static int fib(int sum, int num)
    {
        if (num <= 2)
        {
            return simple(sum, num);
        }

        return fib(fib(sum, num - 1), num - 2);
    }

    public static int simple(int sum, int num)
    {
        if (num == 0)
        {
            return sum + 0;
        }

        if (num == 1)
        {
            return sum + 1;
        }

        if (num == 2)
        {
            return sum + 3;
        }

        return 555555;
    }
}

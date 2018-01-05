// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Threading;

delegate void MonitorDelegate(object monitor);
delegate void MonitorDelegateTS(object monitor,int timeout);

class TestHelper
{
    private int m_iSharedData;
    private int m_iRequestedEntries;
    public ManualResetEvent m_Event;
    public bool m_bError;
    private Random m_rng = new Random(0);

    public bool Error
    {        
        set
        {
            lock(typeof(TestHelper))
            {
                m_bError = value;
            }
        }
        get
        {
            lock(typeof(TestHelper))
            {
                return m_bError;
            }
        }
    }

    public TestHelper(int num)
    {
        m_Event = new ManualResetEvent(false);
        m_iSharedData = 0;
        m_iRequestedEntries = num;
        m_bError = false;
    }
    
    public void DoWork()
    {
        int snapshot = m_iSharedData;
        Delayer.Delay(Delayer.RandomShortDelay(m_rng));
        m_iSharedData++;
        Delayer.Delay(Delayer.RandomShortDelay(m_rng));
        if(m_iSharedData != snapshot + 1)
        {
            Error = true;
            Console.WriteLine("Failure!!!");
        }
        if(m_iSharedData == m_iRequestedEntries)
            m_Event.Set();
    }

    public void Consumer(object monitor)
    {
        lock(monitor)
        {
            DoWork();
        }    
    }

    public void ConsumerTryEnter(object monitor, int timeout)
    {
        bool tookLock = false;

        Monitor.TryEnter(monitor, timeout, ref tookLock);

        while (!tookLock)
        {
            Thread.Sleep(0);
            Monitor.TryEnter(monitor, timeout, ref tookLock);
        }

        try
        {
            DoWork();
        }
        finally
        {
            Monitor.Exit(monitor);
        }
    }

    private static class Delayer
    {
        private static uint[] s_delayValues = new uint[32];

        public static uint RandomShortDelay(Random rng) => (uint)rng.Next(4, 10);
        public static uint RandomMediumDelay(Random rng) => (uint)rng.Next(10, 15);
        public static uint RandomLongDelay(Random rng) => (uint)rng.Next(15, 20);

        public static void Delay(uint n)
        {
            Thread.Sleep(0);
            s_delayValues[16] += Fib(n);
        }

        private static uint Fib(uint n)
        {
            if (n <= 1)
                return n;
            return Fib(n - 2) + Fib(n - 1);
        }
    }
}

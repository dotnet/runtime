// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#region Using directives

 

using System;

using System.Collections.Generic;

using System.Text;

using System.Threading;

 

#endregion

 

namespace UnregisterWaitNativeBug
{
    class Program
    {
        private int ret = 0;
        private RegisteredWaitHandle[] regWait;
        private readonly AutoResetEvent expectedWaitsCompleted = new AutoResetEvent(false);

        static int Main(string[] args)
        {
            Program p = new Program();
            p.Run();
            Console.WriteLine(100 == p.ret ? "Test Passed" : "Test Failed");
            return p.ret;
        }

        public void Run()
        {
            int size = 100;
            AutoResetEvent[] are = new AutoResetEvent[size];
            regWait = new RegisteredWaitHandle[size];

            for (int i = 0; i < size; i++)
            {
                are[i] = new AutoResetEvent(false);
                regWait[i] = ThreadPool.RegisterWaitForSingleObject((WaitHandle)are[i], new WaitOrTimerCallback(TheCallBack), are[i], -1, false);
            }

            for (int i = 0; i < size; i++)
            {
                are[i].Set();
                are[i] = null;
            }

            if (expectedWaitsCompleted.WaitOne(30_000))
            {
                // Wait a bit longer to verify that there are not any additional wait completions
                Thread.Sleep(50);
            }

            for (int i = 0; i < size; i++)
            {
                regWait[i].Unregister(are[i]);
            }
        }

        public void TheCallBack(object foo, bool state) 
        {
            if (Interlocked.Increment(ref ret) == 100)
            {
                expectedWaitsCompleted.Set();
            }
        }
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Profiler.Tests
{
    class GCBasicTests
    {
        static readonly Guid GcBasicEventsProfilerGuid = new Guid("A040B953-EDE7-42D9-9077-AA69BB2BE024");

        public static void DoWork() 
        {
            // Allocate POH objects
            var arr0 = GC.AllocateUninitializedArray<int>(100000, true);
            var arr1 = GC.AllocateArray<int>(200000, true);

            int k = 0;
            while(k < 3) 
            {
                Console.WriteLine("{0}: Restarting run {1}",Thread.CurrentThread.Name,k);
                int[] largeArray = new int[1000000];
                for (int i = 0; i <= 100; i++)
                {
                    int[] saveArray = largeArray;
                    largeArray = new int[largeArray.Length + 100000];
                    saveArray = null;
                    //Console.WriteLine("{0} at size {1}",Thread.CurrentThread.Name,largeArray.Length.ToString());
                }
                
                k++;
           }

            GC.KeepAlive(arr0);
            GC.KeepAlive(arr1);
        }

        public static int RunTest(String[] args) 
        {
            long Threads = 1;

            if(args.Length > 2)
            {
                Console.WriteLine("usage: LargeObjectAlloc runtest <number of threads>");
                return 1;
            }
            else if(args.Length == 2)
            {
                Threads = Int64.Parse(args[1]);
            }

            Console.WriteLine("LargeObjectAlloc started with {0} threads. Control-C to exit",
                Threads.ToString());

            Thread myThread = null;
            for(long i = 0; i < Threads; i++)
            {
                myThread = new Thread(new ThreadStart(DoWork));
                myThread.Name = i.ToString();
                myThread.Start();
            }

            Console.WriteLine("All threads started");
            myThread.Join();

            Console.WriteLine("Test Passed");
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "GCCallbacksBasic",
                                          profilerClsid: GcBasicEventsProfilerGuid);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Versioning;

namespace Sample
{
    public class Test
    {
        public static void Main(string[] args)
        {
            Console.WriteLine ("Hello, World!");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [SupportedOSPlatform("browser")] // ask the analyzer to warn if we use APIs not supported on browser-wasm
        public static int TestMeaning()
        {
            List<int> myList = new List<int>{ 1, 2, 3, 4 };
            Console.WriteLine(myList);

            Thread t = new Thread(new ThreadStart(ThreadFuncTest));
            t.Start();

            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Main Thread is doing stuff too... " + i.ToString());
                Thread.Sleep(100);
                GC.Collect();
            }

            return 42;
        }

        public static void ThreadFuncTest()
        {
            Console.WriteLine("Hello from another thread");

            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine("Sleeping from another thread: " + i.ToString());
                Thread.Sleep(100);
                GC.Collect();
            }
        }
    }
}

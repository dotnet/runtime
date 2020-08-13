// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Profiler.Tests
{
    class AllocObject
    {
        public static readonly int ArraySize = 100;
        int[] m_array = null;

        public AllocObject(bool poh)
        {
            if (poh)
            {
                m_array = GC.AllocateArray<int>(ArraySize, true);
            }
            else
            {
                m_array = new int[ArraySize];
            }
        }
    }

    class GCTests
    {
        static readonly Guid GCProfilerGuid = new Guid("BCD8186F-1EEC-47E9-AFA7-396F879382C3");

        public static int RunTest(String[] args) 
        {
            int numAllocators = 1024;
            int[] root1 = GC.AllocateArray<int>(AllocObject.ArraySize, true);
            int[] root2 = GC.AllocateArray<int>(AllocObject.ArraySize, true);
            AllocObject[] objs = new AllocObject[numAllocators];

            Random rand = new Random();
            int numPoh = 0;
            int numReg = 0;
            for (int i = 0; i < 10000; ++i)
            {
                int pos = rand.Next(0, numAllocators);

                bool poh = rand.NextDouble() > 0.5;
                objs[pos] = new AllocObject(poh);

                if (poh)
                {
                    ++numPoh;
                }
                else
                {
                    ++numReg;
                }

                if (i % 1000 == 0)
                {
                    GC.Collect();
                    Console.WriteLine ($"Did {i} iterations Allocated={GC.GetAllocatedBytesForCurrentThread()}");
                }


                int[] m_array = new int[100];
            }

            Console.WriteLine($"{numPoh} POH allocs and {numReg} normal allocs.");
            GC.KeepAlive(root1);
            GC.KeepAlive(root2);
            return 100;
        }

        public static int Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("RunTest", StringComparison.OrdinalIgnoreCase))
            {
                return RunTest(args);
            }

            return ProfilerTestRunner.Run(profileePath: System.Reflection.Assembly.GetExecutingAssembly().Location,
                                          testName: "GCTests",
                                          profilerClsid: GCProfilerGuid);
        }
    }
}

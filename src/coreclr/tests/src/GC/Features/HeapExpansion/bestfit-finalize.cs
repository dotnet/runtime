// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//This is modeled after a server executing requests
//which pin some of their newly allocated objects.
using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security;

namespace Fragment
{
    [SecuritySafeCritical]
    public class Request
    {
        Object[] survivors;
        GCHandle pin;

        [SecuritySafeCritical]
        public Request()
        {
            survivors = new Object[1 + (int)(Test.AllocationVolume*Test.SurvivalRate)/100];
            int i = 0;
            int volume = 0;

            //allocate half of the request size.
            while (volume < Test.AllocationVolume/2)
            {
                volume += AllocHalfVolume(++i, Test.SurvivalRate);
            }

            //allocate one pinned buffer
            pin = GCHandle.Alloc(new byte[100], GCHandleType.Pinned);

            //allocate the rest of the request
            while (volume < Test.AllocationVolume)
            {
                volume += AllocHalfVolume(++i, Test.SurvivalRate);
            }

        }

        // unpins and releases the pinned buffer
        [SecuritySafeCritical]
        ~Request()
        {
            pin.Free();
        }

        [SecuritySafeCritical]
        private int AllocHalfVolume(int index, float survFraction)
        {
            int allocSurv = Test.Rand.Next(100, 2000 + 2*index);
            int alloc = (int)(allocSurv / survFraction) - allocSurv;

            // create garbage
            int garbage=0;
            while (garbage < alloc)
            {
                int size = Test.Rand.Next(10, 200+2*garbage);
                Object x = new byte[size];
                garbage+=size;
            }
            survivors[index] = new byte[allocSurv];
            return allocSurv + alloc;
        }

    }

    public class Test
    {

        public static Random Rand;
        public static int NumRequests = 0;
        public static int AllocationVolume = 0;
        public static float SurvivalRate = 0.6f;

        public void Go()
        {
            int steadyStateFactor = 5;
            Request[] requests = new Request[NumRequests];
            int instRequests = 0;
            int totalReqs = 0;
            int nreqsToSteady = 0;
            bool done = false;

            while (!done)
            {
                totalReqs++;
                int i = Rand.Next(0, NumRequests);
                if (requests[i] != null)
                {
                    requests[i] = null;
                }
                else
                {
                    instRequests++;
                }
                requests[i] = new Request();

                if (instRequests == NumRequests)
                {
                    if (nreqsToSteady == 0)
                    {
                        nreqsToSteady = totalReqs;
                        Console.WriteLine ("Took {0} iterations to reach steady state", nreqsToSteady);
                    }
                    else if (totalReqs == steadyStateFactor*nreqsToSteady)
                    {
                        done = true;
                    }
                }
            }

            for (int i = 0; i < NumRequests; i++)
            {
                requests[i] = null;
            }

        }


        public static void Usage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("Fragment <num iterations> <num requests> <allocation volume> [random seed]");
        }


        static public int Main (String[] args)
        {
            int numIterations = 0;
            int randomSeed = 0;

            switch (args.Length)
            {
                case 0:
                    // use defaults
                    numIterations = 1;
                    NumRequests = 1200;
                    AllocationVolume = 100000;
                    randomSeed = (int)DateTime.Now.Ticks;
                    Console.WriteLine("Using defaults: {0} {1} {2}", numIterations, NumRequests, AllocationVolume);

                    break;
                case 3:
                case 4:
                    if ( (!Int32.TryParse(args[0], out numIterations)) ||
                         (!Int32.TryParse(args[1], out NumRequests)) ||
                         (!Int32.TryParse(args[2], out AllocationVolume)) )
                    {
                        goto default;
                    }

                    if (args.Length==4)
                    {
                        if (!Int32.TryParse(args[3], out randomSeed))
                        {
                            goto default;
                        }
                    }
                    else
                    {
                        randomSeed = (int)DateTime.Now.Ticks;
                    }

                    break;
                default:
                    Usage();
                    return 1;
            }

            Console.WriteLine("Using random seed: {0}", randomSeed );
            Rand = new Random(randomSeed);

            try
            {
                for (int j=0; j<numIterations; j++)
                {
                    Test t = new Test();
                    t.Go();
                }
            }
            catch (OutOfMemoryException)
            {
                Console.WriteLine("OOM");
                Console.WriteLine(GC.GetTotalMemory(false));
                Console.WriteLine("Test Failed");
                return 1;
            }

            Console.WriteLine("Test Passed");
            return 100;
        }
    }
}
